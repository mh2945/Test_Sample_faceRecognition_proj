using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

namespace FaceSDKSample.sample
{
    public class IPCam335N_MJpegWSClient : IDisposable
    {
        public void Dispose()
        {
            CleanupSocket();
        }

        //
        // stat
        //
        
        private long stat_last_recv_frame_cnt_ = 0;
        private long stat_last_recv_bytes_ = 0;
        private long stat_last_update_utc_ms_ = 0;

        private long stat_recv_frame_cnt_sec_ = 0;
        public long stat_recv_frame_cnt_sec { get { return stat_recv_frame_cnt_sec_; } }

        private long stat_recv_bytes_sec_ = 0;
        public long stat_recv_bytes_sec { get { return stat_recv_bytes_sec_; } }

        public void update_stat()
        {
            long cur_utc_ms = GetEpochMS();
            long dt = cur_utc_ms - stat_last_update_utc_ms_;

            if (dt >= 1000)
            {
                long sec = dt / 1000;                               

                stat_recv_frame_cnt_sec_ = stat_last_recv_frame_cnt_ / sec;
                stat_recv_bytes_sec_ = stat_last_recv_bytes_ / sec;

                stat_last_update_utc_ms_ = cur_utc_ms;

                stat_last_recv_frame_cnt_ = 0;
                stat_last_recv_bytes_ = 0;
            }
        }


        //
        // callback
        //

        // <frameidx, jpeg bytes, recv_epoch_ms>
        public delegate void OnRecvFrame(UInt64 frame_cnt, string type, object jpeg, long utc_ms);
        private OnRecvFrame on_recv_frame_;

        public void SetRecvFrame(OnRecvFrame cb)
        {
            on_recv_frame_ = cb;
        }

        //
        // util
        //

        public static long GetEpochMS()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public static bool IsJpeg(byte[] d)
        {
            return d != null &&
                   d.Length > 4 &&
                   d[0] == 0xFF &&
                   d[1] == 0xD8 &&
                   d[d.Length - 2] == 0xFF &&
                   d[d.Length - 1] == 0xD9;
        }

        public static Exception UnwrapTaskException(AggregateException ex)
        {
            if (ex == null)
                return new Exception("unknown task error");

            if (ex.InnerExceptions != null && ex.InnerExceptions.Count > 0)
                return ex.InnerExceptions[0];

            return ex;
        }

        //
        // fields
        //

        protected String last_err_desc_ = "";
        protected ERR last_err_ = ERR.UNKNOWN;

        public void SetLastErr(ERR err=ERR.OK, String err_desc = "")
        {
            last_err_ = err;
            last_err_desc_ = err_desc;
        }

        public ERR GetLastErr()
        {
            return last_err_;
        }

        public String GetLastErrDesc()
        {
            return $"{(int)last_err_}({last_err_}), {last_err_desc_}";
        }


        private readonly Uri uri_;
        private readonly string stream_;

        private ClientWebSocket ws_;
        //private State state_ = State.Disconnected;

        private int reconnect_delay_sec_ = 2;
        private DateTime next_reconnect_utc_;

        private float recv_timeout_sec_ = 20.0f;
        private byte[] recv_buf_ = new byte[1024 * 1024];
        private DateTime last_recv_utc_;
        private UInt64 recv_frame_idx_ = 0;

        // heartbeat
        private DateTime last_ping_utc_;
        private readonly float HEARTBEAT_INTERVAL_SEC = 20.0f;

        private int log_level_ = 0;
        public int log_level { get { return log_level_; } }
        public void SetLogLevel(int level) { log_level_ = level; }


        //
        // videoInfo
        //

        public class VideoInfo
        {
            public string codec = "";
            public int fps = 0;
            public int width = 0;
            public int height = 0;
        }

        VideoInfo video_info_ = new VideoInfo();
        public VideoInfo video_info { get  { return video_info_; } }

        public static VideoInfo ParseVideoInfo(string json)
        {
            return new VideoInfo
            {
                codec = GetVString(json, "\"codec\""),
                fps = GetVInt(json, "\"fps\""),
                width = GetVInt(json, "\"width\""),
                height = GetVInt(json, "\"height\"")
            };
        }

        private static string GetVString(string s, string key)
        {
            int i = s.IndexOf(key);
            if (i < 0) return null;

            i = s.IndexOf(':', i) + 1;

            // skip spaces + quote
            while (s[i] == ' ' || s[i] == '\"') i++;

            int start = i;

            while (s[i] != '\"') i++;

            return s.Substring(start, i - start);
        }

        private static int GetVInt(string s, string key)
        {
            int i = s.IndexOf(key);
            if (i < 0) return 0;

            i = s.IndexOf(':', i) + 1;

            while (s[i] == ' ') i++;

            int start = i;

            while (i < s.Length && char.IsDigit(s[i])) i++;

            return int.Parse(s.Substring(start, i - start));
        }
        

        //
        // state
        //

        public enum State
        {
            Disconnected,
            Running
        }

        public enum ERR
        {
            OK                       = 0,
            UNKNOWN                  = 1,
            EXCEPT                   = 2,

            SOCK_CLOSED              = 3,
            PENDING_CONNECT          = 4,

            CONN_TASK = 10,
            CONN_TIMEOUT             = 11,
            CONN_FAULTED             = 12,
            CONN_CANCELED            = 13,
            CONN_EXCEPT              = 14,

            SEND_TASK                = 20,
            SEND_TIMEOUT             = 21,
            SEND_FAULTED             = 22,
            SEND_CANCELED            = 23,
            SEND_EXCEPT              = 24,


            RECV_TASK                = 30,
            RECV_TIMEOUT             = 31,
            RECV_EXCEPT              = 32,
                        

            WS_MSG_CLOSE = 100,

            WS_START_VIDEO_ACK1        = 200,
            WS_START_VIDEO_ACK2        = 201,
        }

        //
        // IPCam335N_MJpegWSClient
        //

        // stream_index: 0: main, 1: second
        public IPCam335N_MJpegWSClient(string ws_url, string stream)
        {
            // URL 에 이미 경로가 있으면 그대로 쓰고, 없을 때만 기본 경로 "/ws" 를 붙인다.
            Uri given = new Uri(ws_url);

            uri_ = string.IsNullOrEmpty(given.AbsolutePath) || given.AbsolutePath == "/"
                ? new Uri(ws_url.TrimEnd('/') + "/ws")
                : given;

            stream_ = stream;
            next_reconnect_utc_ = DateTime.MinValue;
        }

        public string GetURL()
        {
            return uri_.ToString();
        }

        //
        // public
        //

        public bool IsOpened()
        {
            if (!IsSocketOpen())
                return false;

            return true;
        }

        private ERR ConnectAndStartVideoSync(out string err_desc, out string video_info, int conn_timeout_ms=3000, int send_recv_timeout_ms=1000)
        {
            err_desc = "";
            video_info = "";

            try
            {
                var connect_task = ws_.ConnectAsync(uri_, CancellationToken.None);

                if (!connect_task.Wait(conn_timeout_ms))
                {
                    err_desc = $"connect timeout ({conn_timeout_ms}ms)";
                    return ERR.CONN_TIMEOUT;
                }

                if (connect_task.IsFaulted)
                {
                    // connect_task.Exception 에 실제 소켓 오류가 들어 있다.
                    err_desc = "connect faulted: " + UnwrapTaskException(connect_task.Exception).Message;
                    return ERR.CONN_FAULTED;
                }

                if (connect_task.IsCanceled)
                {
                    err_desc = "connect canceled";
                    return ERR.CONN_CANCELED;
                }
            }
            catch (AggregateException ex)
            {
                err_desc = "connect aggregate exception:" + UnwrapTaskException(ex).Message;
                return ERR.CONN_EXCEPT;
            }
            catch (Exception ex)
            {
                err_desc = "connect exception:" + ex.Message;
                return ERR.CONN_EXCEPT;
            }

            if (!IsSocketOpen())
            {
                err_desc = $"socket not open after connect, ws_state={(ws_ != null ? ws_.State.ToString() : "null")}";
                return ERR.SOCK_CLOSED;
            }

            LogDebug("[ConnectAndStartVideoSync] connected.. " + UrlParser.MaskPassword(uri_.ToString()));

            //
            // send start_video
            //
            int stream_index = int.Parse(stream_);

            string cmd = "{\"start_video\":{\"ch\":" + stream_index + "}}";
            var send_task = MakeWSTaskSendText(ws_, cmd);

            if (send_task == null)
            {
                err_desc = "start_video send task creation failed";
                return ERR.SEND_TASK;
            }

            try
            {
                if (!send_task.Wait(send_recv_timeout_ms))
                {
                    err_desc = $"start_video send timeout ({send_recv_timeout_ms}ms)";
                    return ERR.SEND_TIMEOUT;
                }

                if (send_task.IsFaulted)
                {
                    err_desc = "start_video send faulted: " + UnwrapTaskException(send_task.Exception).Message;
                    return ERR.SEND_FAULTED;
                }

                if (send_task.IsCanceled)
                {
                    err_desc = "start_video send canceled";
                    return ERR.SEND_CANCELED;
                }
            }
            catch (AggregateException ex)
            {
                err_desc = "start_video send aggregate exception:" + UnwrapTaskException(ex).Message;
                return ERR.SEND_EXCEPT;
            }
            catch (Exception ex)
            {
                err_desc = "start_video send exception: " + ex.Message;
                return ERR.SEND_EXCEPT;
            }

            LogInfo("[ConnectAndStartVideoSync] START_VIDEO command sent. stream=" + stream_);

            //
            // 3) receive start_video response
            //

            // Ack #1

            byte[] start_video_ack;  // 200
            WebSocketMessageType start_video_ack_type;

            ERR recv_err = ReceiveOneMessageSync(send_recv_timeout_ms, 
                out start_video_ack, out start_video_ack_type);

            if (recv_err != ERR.OK)
            {
                err_desc = "[ack#1] receiving error, err=" + recv_err;
                return ERR.WS_START_VIDEO_ACK1;
            }

            if(start_video_ack_type != WebSocketMessageType.Text)
            {
                err_desc = "[ack#1] receiving error, response is not text, type=" + start_video_ack_type;
                return ERR.WS_START_VIDEO_ACK1;
            }

            string start_video_ack_msg = Encoding.UTF8.GetString(start_video_ack);
            
            LogDebug("[ConnectAndStartVideoSync] START_VIDEO ACK #1:" + start_video_ack_msg);

            if (start_video_ack_msg != "200")
            {
                err_desc = "[ack#1] ack not match, expect=200, recv=" + start_video_ack_msg;
                return ERR.WS_START_VIDEO_ACK1;
            }            

            // Ack #2

            byte[] start_video_info; // {"video": {"codec":"h264","fps":10,"width":1920,"height":1080}}
            WebSocketMessageType start_video_info_type;

            recv_err = ReceiveOneMessageSync(send_recv_timeout_ms, 
                out start_video_info, out start_video_info_type);

            if (recv_err != ERR.OK)
            {
                err_desc = "[ack#2] receiving error, err=" + recv_err;
                return ERR.WS_START_VIDEO_ACK2;
            }

            // ack#2 의 타입을 검사해야 한다. (ack#1 의 start_video_ack_type 을 보고 있었음)
            if (start_video_info_type != WebSocketMessageType.Text)
            {
                err_desc = "[ack#2] receiving error, response is not text, type=" + start_video_info_type;
                return ERR.WS_START_VIDEO_ACK2;
            }

            string start_video_info_msg = Encoding.UTF8.GetString(start_video_info);
            LogDebug("[ConnectAndStartVideoSync] START_VIDEO ACK #2:" + start_video_info_msg);

            video_info = start_video_info_msg;

            //
            // fin
            //

            last_recv_utc_ = DateTime.UtcNow;            

            return ERR.OK;
        }

        public bool Connect(out string err_desc, int timeout_ms=3000)
        {            
            err_desc = "";

            Uri url = uri_;

            // 로그 / 오류 문구에는 PW 를 가린 URL 만 남긴다. 접속에는 원본 uri_ 를 쓴다.
            string url_masked = UrlParser.MaskPassword(url.ToString());

            LogDebug("[Connect] try connecting.. uri=" + url_masked);

            CleanupSocket();
            ws_ = new ClientWebSocket();

            string video_info = "";
                        
            ERR conn_err = ConnectAndStartVideoSync(out err_desc, out video_info, timeout_ms);

            if (conn_err != ERR.OK)
            {
                // 코드와 이름은 GetLastErrDesc() 가 붙이므로 여기서는 사유만 남긴다.
                err_desc = $"conn_url={url_masked}, {err_desc}";

                SetLastErr(conn_err, err_desc);
                LogErr(err_desc);

                SetConnectionLost();
                return false;
            }

            SetLastErr();

            //
            // connected
            //

            LogInfo($"[Connect] connected, url={url_masked}");
            LogInfo($"[Connect] > video_info={video_info}");

            video_info_ = ParseVideoInfo(video_info);

            last_recv_utc_ = DateTime.UtcNow;
            last_ping_utc_ = DateTime.MinValue;

            return true;
        }

        public ERR Tick(out string err_desc)
        {
            update_stat();

            err_desc = "";

            if(!IsSocketOpen())
            {
                TickConnectionLost();
                return ERR.PENDING_CONNECT;
            }

            try 
            {
                TickHeartbeat();                
                TickReceive();
            }
            catch (Exception ex)
            {
                SetLastErr(ERR.EXCEPT, ex.Message);
                LogErr(GetLastErrDesc());
                
                SetConnectionLost();

                return ERR.EXCEPT;
            }

            return ERR.OK;
        }


        //
        // heartbeat
        //

        private ERR TickHeartbeat(int timeout_ms = 1000)
        {
            double sec = (DateTime.UtcNow - last_ping_utc_).TotalSeconds;

            if (sec < HEARTBEAT_INTERVAL_SEC)
                return 0;

            var task = MakeWSTaskSendText(ws_, "{\"ping_pong\":{\"ping\":1}}");
            if (task == null)
                return ERR.SEND_TASK;

            bool completed = task.Wait(timeout_ms);
            if (!completed)
                return ERR.SEND_TIMEOUT;

            if (task.IsFaulted)
                return ERR.SEND_FAULTED;

            if (task.IsCanceled)
                return ERR.SEND_CANCELED;

            last_ping_utc_ = DateTime.UtcNow;

            LogDebug("[TickHeartbeat] sending heartbeat ping");

            return ERR.OK;
        }

        //
        // receive
        //

        private ERR TickReceive(int timeout_ms=2000)
        {
            byte[] data;
            WebSocketMessageType msg_type;

            ERR recv_err = ReceiveOneMessageSync(timeout_ms, out data, out msg_type);
            if (recv_err != ERR.OK)
            {
                return recv_err;
            }

            last_recv_utc_ = DateTime.UtcNow;

            if (msg_type == WebSocketMessageType.Text)
            {
                string txt = Encoding.UTF8.GetString(data);
                LogDebug($"[TickReceive] TEXT={txt}");
            }
            else if (msg_type == WebSocketMessageType.Binary)
            {
                if (IsJpeg(data))
                {
                    if (log_level > 2)
                    {
                        LogDebug($"[TickReceive] JPEG, frame={recv_frame_idx_}, epoch={GetEpochMS()}, len={data.Length}");
                    }

                    on_recv_frame_?.Invoke(recv_frame_idx_, "jpeg_bytes", data, GetEpochMS());
                    recv_frame_idx_++;

                    stat_last_recv_bytes_ += data.Length;
                    stat_last_recv_frame_cnt_++;
                }
                else
                {
                    if (log_level > 2) { 
                        LogDebug($"[TickReceive] BIN non-jpeg, frame={recv_frame_idx_}, epoch={GetEpochMS()}, len=" + data.Length);
                    }
                }
            }
            else if (msg_type == WebSocketMessageType.Close)
            {
                return ERR.SOCK_CLOSED;
            }

            return ERR.OK;
        }

        private ERR ReceiveOneMessageSync(int timeout_ms, out byte[] data, out WebSocketMessageType msg_type)
        {
            data = null;
            msg_type = WebSocketMessageType.Text;

            if (!IsSocketOpen())
                return ERR.SOCK_CLOSED;

            using (var ms = new MemoryStream())
            {
                WebSocketReceiveResult result = null;

                try
                {
                    using (var cts = new CancellationTokenSource(timeout_ms))
                    {
                        do
                        {
                            result = ws_.ReceiveAsync(
                                new ArraySegment<byte>(recv_buf_),
                                cts.Token
                            ).GetAwaiter().GetResult();

                            if (result.MessageType == WebSocketMessageType.Close)
                                return ERR.WS_MSG_CLOSE;

                            if (result.Count > 0)
                                ms.Write(recv_buf_, 0, result.Count);

                        } while (!result.EndOfMessage);
                    }
                }
                catch (OperationCanceledException)
                {
                    return ERR.RECV_TIMEOUT;
                }
                catch (Exception ex)
                {
                    LogErr("[ReceiveOneMessageSync] recv exception: " + ex.Message);
                    return ERR.RECV_EXCEPT;
                }

                data = ms.ToArray();
                msg_type = result.MessageType;

                return ERR.OK;
            }
        }

        private bool IsRecvTimeoutted()
        {
            double idle = (DateTime.UtcNow - last_recv_utc_).TotalSeconds;

            if (idle > recv_timeout_sec_)
            {
                LogErr("[IsRecvTimeoutted] recv timeout, idle_sec=" + idle);
                return true;
            }

            return false;
        }

        //
        // send
        //

        public System.Threading.Tasks.Task MakeWSTaskSendText(ClientWebSocket ws, string text)
        {
            if (IsSocketOpen())
            {
                byte[] buf = Encoding.UTF8.GetBytes(text);

                return ws.SendAsync(
                    new ArraySegment<byte>(buf),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }

            return null;
        }

        //
        // socket
        //

        private bool IsSocketOpen()
        {
            if (ws_ == null || ws_.State != WebSocketState.Open)
                return false;

            return true;
        }

        private void CleanupSocket()
        {
            try
            {
                if (ws_ != null)
                {
                    ws_.Dispose();
                    ws_ = null;
                }
            }
            catch
            {
                ws_ = null;
            }
        }

        //
        // event
        //

        private void SetConnectionLost()
        {
            if (next_reconnect_utc_ != DateTime.MinValue)
                return; // already starting

            LogDebug("[OnConnectionLost] connection lost. auto reconnect after " +
                     reconnect_delay_sec_ + " sec");

            CleanupSocket();

            last_ping_utc_ = DateTime.MinValue;
            video_info_ = new VideoInfo();

            next_reconnect_utc_ = DateTime.UtcNow.AddSeconds(reconnect_delay_sec_);
            reconnect_delay_sec_ = Math.Min(reconnect_delay_sec_ * 2, 15);
        }

        private void TickConnectionLost()
        {
            if(IsSocketOpen())
            {
                SetLastErr();
                next_reconnect_utc_ = DateTime.MinValue;
                return;
            }

            if(DateTime.UtcNow > next_reconnect_utc_)
            {
                string err_desc;

                if(!Connect(out err_desc)) {
                    SetConnectionLost();

                    // Connect() 가 이미 정확한 코드를 저장했다. CONN_EXCEPT 로 덮어쓰지 않는다.
                    return;
                }

                SetLastErr();
                next_reconnect_utc_ = DateTime.MinValue;

                return;
            }
        }

        //
        // logging
        //

        private void LogInfo(string msg)
        {
            if (log_level_ >= 1)
            {
                Console.WriteLine("[INFO] " + msg);
            }
        }

        private void LogDebug(string msg)
        {
            if (log_level_ >= 2)
            {
                Console.WriteLine("[DEBUG] " + msg);
            }
        }

        private void LogErr(string msg)
        {
            if (log_level_ >= 0)
            {
                Console.WriteLine("[ERR] " + msg);
            }
        }
    }

}
