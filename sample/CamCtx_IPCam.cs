using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace FaceSDKSample.sample
{
    // UI 에서 입력한 URL 을 그대로 접속에 사용하므로, 이 클래스는 파싱 결과 보관 전용이다.
    // (URL 재조립은 하지 않는다. 재조립하면 포트/경로가 원본과 달라진다)
    public class UrlInfo
    {
        public string Protocol;
        public string Username;
        public string Password;
        public string Host;
        public int Port;
        public string Path;
    }

    public static class UrlParser
    {
        public static UrlInfo Parse(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return null;

            UrlInfo info = new UrlInfo();

            info.Protocol = uri.Scheme;

            info.Host = uri.Host;
            info.Port = uri.IsDefaultPort ? GetDefaultPort(uri.Scheme) : uri.Port;
            info.Path = uri.AbsolutePath;

            if (!string.IsNullOrEmpty(uri.UserInfo))
            {
                var parts = uri.UserInfo.Split(':');
                info.Username = parts.Length > 0 ? parts[0] : "";
                info.Password = parts.Length > 1 ? parts[1] : "";
            }

            return info;
        }

        // URL 에 계정 정보가 있으면 비밀번호를 가린다. (에러 / 로그 표시용)
        //
        // '@' 와 ':' 탐색을 authority 구간(스킴 뒤 ~ 첫 '/' 앞)으로 한정한다.
        // 경로까지 훑으면 PW 에 '@' 가 들어간 URL(rtsp://a:p@ss@host)이 부분 노출된다.
        public static string MaskPassword(string url)
        {
            if (string.IsNullOrEmpty(url))
                return url;

            int scheme_end = url.IndexOf("://");
            if (scheme_end < 0)
                return url;

            int authority_beg = scheme_end + 3;

            int authority_end = url.IndexOf('/', authority_beg);
            if (authority_end < 0)
                authority_end = url.Length;

            if (authority_end <= authority_beg)
                return url;

            // PW 에 '@' 가 섞일 수 있다. 계정 구분자는 authority 안의 마지막 '@' 다.
            int at = url.LastIndexOf('@', authority_end - 1, authority_end - authority_beg);
            if (at < 0)
                return url;

            int colon = url.IndexOf(':', authority_beg, at - authority_beg);
            if (colon < 0)
                return url;

            return url.Substring(0, colon + 1) + "***" + url.Substring(at);
        }

        public static int GetDefaultPort(string scheme)
        {
            switch (scheme.ToLower())
            {
                case "rtsp": return 554;
                case "ws": return 80;
                case "wss": return 443;
                case "http": return 80;
                case "https": return 443;
                default: return -1;
            }
        }        
    }


    public class CamCtx_IPCam : CamCtx
    {
        public static readonly string DEV_TYPE = "ipcam";

        // 카메라 클라이언트 콘솔 로그 수준. 0:ERR 만, 1:+INFO, 2:+DEBUG
        // 이 값을 지정하지 않으면 클라이언트 기본값이 0 이라 연결 과정 로그가 전혀 남지 않는다.
#if DEBUG
        private const int CLIENT_LOG_LEVEL = 2;
#else
        private const int CLIENT_LOG_LEVEL = 0;
#endif

        public string url_ = "";
        public UrlInfo url_info_;

        public IPCam335N_RTSPClient rtsp_client_;
        public IPCam335N_MJpegWSClient mjpeg_client_;

        public struct CamFrame
        {
            public UInt64 frame_cnt;
            public Mat mat_img;
            //public BGRImg bgr_img;
            public long epoch_ms;
        };

        // stat
        public long stat_recv_frame_cnt_sec {
            get {
                if (cap_remote_proto_ == MJPEG_WS.proto)
                {
                    if (mjpeg_client_ != null)
                    {
                        return mjpeg_client_.stat_recv_frame_cnt_sec;
                    }
                }
                else if (cap_remote_proto_ == RTSP.proto)
                {
                    if (rtsp_client_ != null)
                    {
                        return rtsp_client_.stat_recv_frame_cnt_sec;
                    }
                }

                return 0;
            }
        }

        public long stat_recv_bytes_sec {
            get {
                if (cap_remote_proto_ == MJPEG_WS.proto)
                {
                    if (mjpeg_client_ != null)
                    {
                        return mjpeg_client_.stat_recv_bytes_sec;
                    }
                }
                else if (cap_remote_proto_ == RTSP.proto)
                {
                    if (rtsp_client_ != null)
                    {
                        return rtsp_client_.stat_recv_bytes_sec;
                    }
                }

                return 0;
            }
        }


        // queue
        Queue<CamFrame> ip_cam_frames_ = new Queue<CamFrame>();
               
        void ClearFrames()
        {
            ip_cam_frames_?.Clear();
        }

        private static byte[] MatToByteArray(Mat img, out int width, out int height, out int channels)
        {
            width = img.Width;
            height = img.Height;
            channels = img.Channels();

            int size = (int)(img.Total() * img.ElemSize());
            byte[] pixels = new byte[size];

            System.Runtime.InteropServices.Marshal.Copy(img.Data, pixels, 0, size);
            return pixels;
        }

        protected Mat cap_frame_blink_;

        public Mat ValidatedBlinkImg(int w, int h)
        {
            if (cap_frame_blink_ == null ||
                cap_frame_blink_.Width != w || cap_frame_blink_.Height != h)
            {
                cap_frame_blink_ = new Mat(w, h, MatType.CV_8UC3, Scalar.Black);
            }

            return cap_frame_blink_;
        }

        //
        // URL 문자열 헬퍼
        //

        // URL 에 계정 정보가 없으면 주입한다. (RTSP Digest/Basic 인증용)
        // 이미 "user@" 가 들어 있거나 id 가 비면 원본을 그대로 돌려준다.
        private static string InjectCredentials(string url, string user_id, string user_pw)
        {
            if (string.IsNullOrWhiteSpace(user_id) || string.IsNullOrEmpty(url))
                return url;

            int scheme_end = url.IndexOf("://");
            if (scheme_end < 0 || url.IndexOf('@', scheme_end) >= 0)
                return url;

            string cred = Uri.EscapeDataString(user_id);

            if (!string.IsNullOrWhiteSpace(user_pw))
                cred += ":" + Uri.EscapeDataString(user_pw);

            return url.Substring(0, scheme_end + 3) + cred + "@" + url.Substring(scheme_end + 3);
        }

        //
        // CamCtx
        //

        protected override bool OnOpenCapture(int cap_w, int cap_h,
            string cap_stream, bool cap_flip_horizontal)
        {
            if (cap_remote_proto_ == "")
            {
                SetLastErrMsg("invalid state, cap_remote_proto_ is empty");
                return false;
            }

            if(url_info_ == null)
            {
                SetLastErrMsg($"failed to parse url, url={url_}");
                return false;
            }


            string err_desc = "";

            if (cap_remote_proto_ == RTSP.proto)
            {
                rtsp_client_ = new IPCam335N_RTSPClient(url_, cap_stream);
                rtsp_client_.SetLogLevel(CLIENT_LOG_LEVEL);

                if (!rtsp_client_.Connect(out err_desc, 3000))
                {
                    // Connect() 실패 시 클라이언트가 코드와 사유를 모두 저장해 둔다.
                    SetLastErrMsg(rtsp_client_.GetLastErrDesc());

                    rtsp_client_.Dispose();
                    rtsp_client_ = null;

                    return false;
                }

                rtsp_client_.SetRecvFrame((_frame_cnt, _data_type, _data, _epoch_ms) =>
                {
                    if (_data_type == "cv_mat")
                    {
                        if (_data == null)
                            return;

                        Mat img = (_data as Mat);

                        ip_cam_frames_.Enqueue(new CamFrame
                        {
                            frame_cnt = _frame_cnt,
                            mat_img = img,
                            epoch_ms = _epoch_ms
                        });
                    }
                });

                cap_input_fps_ = rtsp_client_.video_info.fps.ToString();

                cap_codec_ = rtsp_client_.video_info.codec;
                cap_width_ = rtsp_client_.video_info.width;
                cap_height_ = rtsp_client_.video_info.height;

            }
            else if (cap_remote_proto_ == MJPEG_WS.proto)
            {
                mjpeg_client_ = new IPCam335N_MJpegWSClient(url_, cap_stream);
                mjpeg_client_.SetLogLevel(CLIENT_LOG_LEVEL);

                if (!mjpeg_client_.Connect(out err_desc, 3000))
                {
                    // Connect() 실패 시 클라이언트가 코드와 사유를 모두 저장해 둔다.
                    SetLastErrMsg(mjpeg_client_.GetLastErrDesc());

                    mjpeg_client_.Dispose();
                    mjpeg_client_ = null;

                    return false;
                }

                mjpeg_client_.SetRecvFrame((_frame_cnt, _data_type, _data, _epoch_ms) =>
                {
                    if (_data_type == "jpeg_bytes")
                    {
                        if (_data == null || (_data as byte[]).Length == 0)
                            return;

                        Mat img = Cv2.ImDecode((_data as byte[]), ImreadModes.Color); //  BGR 8UC3

                        ip_cam_frames_.Enqueue(new CamFrame
                        {
                            frame_cnt = _frame_cnt,
                            mat_img = img,
                            epoch_ms = _epoch_ms
                        });
                    }                    
                });

                cap_input_fps_ = mjpeg_client_.video_info.fps.ToString();

                cap_codec_ = mjpeg_client_.video_info.codec;
                cap_width_ = mjpeg_client_.video_info.width;
                cap_height_ = mjpeg_client_.video_info.height;
            }
            else
            {
                SetLastErrMsg($"unsupported remote proto, cap_remote_proto_={cap_remote_proto_}");
                return false;
            }

            //
            // cam_ctx common params
            //

            //cap_wnd_name_ = cap_open_prm_.cap_wnd_name;
            cap_stream_ = cap_stream;            

            cap_flip_horizontal_ = cap_flip_horizontal;

            SetLastErrMsg("");

            return true;
        }

        protected override void OnCloseCapture()
        {
            if (cap_frame_blink_ != null)
            {
                cap_frame_blink_.Dispose();
                cap_frame_blink_ = null;
            }

            if (ip_cam_frames_ != null)
            {
                ip_cam_frames_.Clear();
            }

            if (mjpeg_client_ != null)
            {
                mjpeg_client_.Dispose();
                mjpeg_client_ = null;
            }

            if (rtsp_client_ != null)
            {
                rtsp_client_.Dispose();
                rtsp_client_ = null;
            }
        }

        protected override bool OnIsErr()
        {
            if (last_err_msg_ != "")
                return true;

            if (cap_remote_proto_ == MJPEG_WS.proto)
            {
                if (mjpeg_client_ == null || !mjpeg_client_.IsOpened())
                    return true;
            }
            else if (cap_remote_proto_ == RTSP.proto)
            {
                if (rtsp_client_ == null || !rtsp_client_.IsOpened())
                    return true;
            }

            return false;
        }

        protected override bool OnCaptureFrame(int timeout_ms = 1000, int cap_fail_sleep_ms = 100)
        {
            //if(mjpeg_client_ == null || 
            //    !mjpeg_client_.IsOpened())
            //{
            //    return false;
            //}

            if (cap_frame_ == null)
                cap_frame_ = new Mat();


            if (ip_cam_frames_.Count > 0)
            {
                CamFrame frame = ip_cam_frames_.Dequeue();

                cap_frame_ = frame.mat_img;

                //cap_img_bgr_width = frame.bgr_img.width;
                //cap_img_bgr_height = frame.bgr_img.height;
                //cap_img_bgr_pixels = frame.bgr_img.pixels_bgr;

                if (cap_flip_horizontal)
                {
                    if (cap_frame_tmp_ == null)
                        cap_frame_tmp_ = new Mat();

                    // flip into tmp
                    Cv2.Flip(cap_frame_, cap_frame_tmp_, FlipMode.Y);

                    // swap: cap_frame_ becomes flipped
                    Mat swap = cap_frame_;
                    cap_frame_ = cap_frame_tmp_;
                    cap_frame_tmp_ = swap;
                }

            }
            else
            {
                cap_frame_ = ValidatedBlinkImg(cap_width, cap_height);
            }                                

            return true;
        }

        protected override bool OnTick()
        {
            string err_desc_rtsp;
            string err_desc_mjpeg;

            rtsp_client_?.Tick(out err_desc_rtsp);
            mjpeg_client_?.Tick(out err_desc_mjpeg);

            return true;
        }

        public override string GetInfo(string key = "")
        {
            if (key == "desc_w_h_stream_fps")
            {
                if (mjpeg_client_ != null) {
                    var v = mjpeg_client_.video_info;
                    return $"{v.width};{v.height};{cap_stream};{v.fps}";
                }
                else if (rtsp_client_ != null)
                {
                    var v = rtsp_client_.video_info;
                    return $"{v.width};{v.height};{cap_stream};{v.fps}";
                }

                return $"0;0;0;0";
            }
            else if (key == "desc_recv_frmcnt_bytes_per_sec")
            {
                return $"{stat_recv_frame_cnt_sec};{stat_recv_bytes_sec}";
            }


            //
            // default
            //
            
            string url = "";

            if (mjpeg_client_ != null)
            {
                url = mjpeg_client_.GetURL();
            }
            else if (rtsp_client_ != null)
            {
                url = rtsp_client_.GetURL();
            }

            if (string.IsNullOrEmpty(url))
            {
                // 연결 실패 시 클라이언트가 null 이므로, 시도했던 URL 이라도 보여준다.
                url = url_;
            }

            return $"dev_type={cap_dev_type_};stream={cap_stream};url={UrlParser.MaskPassword(url)}";
        }

        public override int Exec(string key = "", string prm1 = "", object prm2 = null) {

            if(key == "clear_cap_frame")
            {
                ip_cam_frames_?.Clear();
            }

            return 0;
        }


        //
        // CamCtx_IPCam
        //        

        public struct RTSP
        {
            public static readonly string proto = "rtsp";
        };

        public struct MJPEG_WS
        {
            public static readonly string proto = "mjpeg_ws";
        };

        public CamCtx_IPCam(RTSP rtsp, string proto, string ip, string port,
            string user_id, string user_pw,
            string model = CamCtx.CAP_DEV_MODEL_UNKNOWN,
            string url_override = "")
        {
            cap_dev_type_ = DEV_TYPE;
            cap_dev_model_ = model;
            cap_remote_proto_ = RTSP.proto;

            if (!string.IsNullOrWhiteSpace(url_override))
            {
                // UI 에서 입력한 URL 을 그대로 사용한다.
                url_ = url_override.Trim();
            }
            else
            {
                url_ = $"{proto}://{ip}";

                if (!string.IsNullOrWhiteSpace(port))
                {
                    url_ += $":{port}";
                }
            }

            url_ = InjectCredentials(url_, user_id, user_pw);
            url_info_ = UrlParser.Parse(url_);
        }

        public CamCtx_IPCam(MJPEG_WS mjpeg_ws, string proto, string ip, string port,
            string model = CamCtx.CAP_DEV_MODEL_UNKNOWN,
            string url_override = "")
        {
            cap_dev_type_ = DEV_TYPE;
            cap_dev_model_ = model;
            cap_remote_proto_ = MJPEG_WS.proto;

            if (!string.IsNullOrWhiteSpace(url_override))
            {
                // UI 에서 입력한 URL 을 그대로 사용한다.
                url_ = url_override.Trim();
            }
            else
            {
                url_ = $"{proto}://{ip}";
                if (!string.IsNullOrWhiteSpace(port))
                {
                    url_ += $":{port}";
                }
            }

            url_info_ = UrlParser.Parse(url_);
        }

    }
}
