using OpenCvSharp;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FaceSDKSample.sample
{
    public class IPCam335N_RTSPClient : IDisposable
    {
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


        // <frameidx, jpeg bytes, recv_epoch_ms>
        public delegate void OnRecvFrame(UInt64 frame_cnt, string type, Object cv_mat/* OpenCVSharp.Mat */, long utc_ms);
        private OnRecvFrame on_recv_frame_;

        public void SetRecvFrame(OnRecvFrame cb)
        {
            on_recv_frame_ = cb;
        }
        
        public static long GetEpochMS()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public class VideoInfo
        {
            public string codec = "";
            public int fps = 0;
            public int width = 0;
            public int height = 0;
        }

        VideoInfo video_info_ = new VideoInfo();
        public VideoInfo video_info { get { return video_info_; } }


        //
        // err
        //

        public enum ERR
        {
            OK = 0,
            UNKNOWN = 1,
            EXCEPT = 2,

            SOCK_CLOSED = 3,
            PENDING_CONNECT = 4,

            CONN_EXCEPT = 14,

            RECV_TASK = 30,
            RECV_TIMEOUT = 31,
            RECV_EXCEPT = 32,
        }

        protected String last_err_desc_ = "";
        protected ERR last_err_ = ERR.UNKNOWN;

        public void SetLastErr(ERR err = ERR.OK, String err_desc = "")
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

        private int reconnect_delay_sec_ = 2;
        private DateTime next_reconnect_utc_;

        private DateTime last_recv_utc_;
        private UInt64 recv_frame_idx_ = 0;

        // heartbeat
        private DateTime last_ping_utc_;

        private int log_level_ = 0;
        public int log_level { get { return log_level_; } }
        public void SetLogLevel(int level) { log_level_ = level; }

        // opensharp
        private VideoCapture cap_ = null;

        public void Dispose()
        {
            Cleanup();
        }


        public IPCam335N_RTSPClient(string rtsp_url, string stream)
        {
            // rtsp://id:pw@192.168.0.10:554/Streaming/Channels/101
            //string rtspUrl = "rtsp://username:password@192.168.0.10:554/stream";
            uri_ = new Uri(rtsp_url);
            stream_ = stream;
            next_reconnect_utc_ = DateTime.MinValue;
        }

        public string GetURL()
        {
            string rst = uri_.ToString();

            // URL 에 이미 스트림 경로가 있으면 그대로 사용한다.
            if (!string.IsNullOrEmpty(uri_.AbsolutePath) && uri_.AbsolutePath != "/")
            {
                return rst;
            }

            string path = "";

            path = "live/main";

            //if (stream_ == "" || stream_ == "0")
            //{
            //    path = "live/main";
            //}
            //else if(stream_ == "1")
            //{
            //    path = "live/second";
            //}

            if (!uri_.AbsolutePath.EndsWith("/"))
            {
                rst += "/";
            }

            rst += path;

            return rst;
        }

        //
        // public
        //

        public bool IsOpened()
        {
            if (cap_ == null || !cap_.IsOpened())
                return false;

            return true;
        }

        public bool Connect(out string err_desc, int timeout_ms = 3000)
        {
            err_desc = "";

            string url = GetURL();

            // 로그 / 오류 문구에는 PW 를 가린 URL 만 남긴다. 접속에는 원본 url 을 쓴다.
            string url_masked = UrlParser.MaskPassword(url);

            LogDebug("[Connect] try connecting.. uri=" + url_masked);

            Cleanup();

            VideoCapture cap = null;

            try
            {
                 cap = new VideoCapture();

                 // timeout_ms 는 적용되지 않는다.
                 // OpenCvSharp 4.11 의 VideoCaptureProperties 에는 OpenTimeoutMsec /
                 // ReadTimeoutMsec 항목이 없어서 VideoCapture 로는 타임아웃을 지정할 수 없다.
                 // 필요하면 환경변수 OPENCV_FFMPEG_CAPTURE_OPTIONS 로 FFMPEG 에 직접 넘겨야 한다.

                 // 26037, FFMPEG has problem for id/pass auth
                 // > https://ffmpeg.org/pipermail/ffmpeg-devel/2024-April/325004.html
                 // > https://github.com/bluenviron/mediamtx/discussions/5610
                 // > remove id/password auth in your rtsp server
                 if (!cap.Open(url, VideoCaptureAPIs.FFMPEG))
                 {
                     cap.Dispose();
                     cap = null;

                     throw new Exception($"Failed to connect RTSP stream. {url_masked}");
                }

                LogInfo($"RTSP connected. url={url_masked}");

                using (var frame = new Mat())
                {
                    for (int i = 0; i < 5; i++)
                    {
                        cap.Read(frame); // drop initial incoming frames
                    }

                    if (!cap.Read(frame) || frame.Empty())
                    {
                        LogErr("Frame read failed.");
                        throw new Exception($"Err on retrive frame from RTSP stream. {url_masked}");
                    }
                }
            }               
            catch (Exception ex)
            {
                string _err_msg = $"[Connect] Exception: {ex.Message}";
                err_desc = _err_msg;

                SetLastErr(ERR.CONN_EXCEPT, _err_msg);
                LogErr(_err_msg);

                SetConnectionLost();

                if (cap != null)
                {
                    if (cap.IsOpened())
                        cap.Release();

                    cap.Dispose();
                    cap = null;
                }

                return false;
            }
            finally
            {
                ;
            }

            //
            // connected
            //

            cap_ = cap;

            video_info_ = new VideoInfo();

            video_info_.width = (int)cap.Get(VideoCaptureProperties.FrameWidth);
            video_info_.height = (int)cap.Get(VideoCaptureProperties.FrameHeight);
            video_info_.fps = (int)cap.Get(VideoCaptureProperties.Fps);

            int fourcc = (int)cap.Get(VideoCaptureProperties.FourCC);
            if (fourcc != 0)
            {
                video_info_.codec = new string(new[] {
                    (char)(fourcc & 0xFF),
                    (char)((fourcc >> 8) & 0xFF),
                    (char)((fourcc >> 16) & 0xFF),
                    (char)((fourcc >> 24) & 0xFF)     });
            }
            else
            {
                video_info_.codec = "UNKNOWN";
            }

            LogInfo($"[Connect] connected, url={url_masked}");
            LogInfo($"[Connect] > video_info={video_info}");

            SetLastErr();

            last_recv_utc_ = DateTime.UtcNow;
            last_ping_utc_ = DateTime.MinValue;

            return true;
        }

        public ERR Tick(out string err_desc)
        {
            err_desc = "";            
            update_stat();
            ERR err = ERR.OK;

            if (!IsCapOpen())
            {
                TickConnectionLost();
                return ERR.PENDING_CONNECT;
            }

            try
            {
                err = TickReceive();

                if (err != ERR.OK)
                {
                    throw new Exception($"Err on TickReceive(), err={err}");
                }
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
        // receive
        //

        private ERR TickReceive(int timeout_ms = 2000)
        {
            Mat frame = new Mat();

            if(!cap_.Read(frame) || frame.Empty())
            {
                frame.Dispose();
                return ERR.RECV_EXCEPT;
            }

            // must use frame.Clone()
            on_recv_frame_?.Invoke(recv_frame_idx_, "cv_mat", frame.Clone(), GetEpochMS());
            recv_frame_idx_++;

            // stat
            stat_last_recv_frame_cnt_++;
            //stat_last_recv_bytes_ += frame.Total() * frame.ElemSize();
            stat_last_recv_bytes_ = 0;


            return ERR.OK;
        }

        //private bool IsRecvTimeoutted()
        //{
        //    double idle = (DateTime.UtcNow - last_recv_utc_).TotalSeconds;

        //    if (idle > recv_timeout_sec_)
        //    {
        //        LogErr("[IsRecvTimeoutted] recv timeout, idle_sec=" + idle);
        //        return true;
        //    }

        //    return false;
        //}

        private bool IsCapOpen()
        {
            if (cap_ == null || !cap_.IsOpened())
                return false;

            return true;
        }

        private void Cleanup()
        {
            try
            {
                if (cap_ != null)
                {
                    if (cap_.IsOpened())
                    {
                        cap_.Release();
                    }

                    cap_.Dispose();
                    cap_ = null;
                }
            }
            catch
            {
                cap_ = null;
            }
        }
                
        private void SetConnectionLost()
        {
            if (next_reconnect_utc_ != DateTime.MinValue)
                return; // already starting

            LogDebug("[OnConnectionLost] connection lost. auto reconnect after " +
                     reconnect_delay_sec_ + " sec");

            Cleanup();

            last_ping_utc_ = DateTime.MinValue;
            video_info_ = new VideoInfo();

            next_reconnect_utc_ = DateTime.UtcNow.AddSeconds(reconnect_delay_sec_);
            reconnect_delay_sec_ = Math.Min(reconnect_delay_sec_ * 2, 15);
        }

        private void TickConnectionLost()
        {
            if (IsCapOpen())
            {
                SetLastErr();
                next_reconnect_utc_ = DateTime.MinValue;
                return;
            }

            if (DateTime.UtcNow > next_reconnect_utc_)
            {
                string err_desc;

                if (!Connect(out err_desc))
                {
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

