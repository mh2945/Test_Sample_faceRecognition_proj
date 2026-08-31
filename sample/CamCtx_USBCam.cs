using OpenCvSharp;
using System;

namespace FaceSDKSample.sample
{
    public class CamCtx_USBCam : CamCtx
    {
        public static readonly string DEV_TYPE = "usbcam";

        VideoCapture capture_;
        VideoCaptureAPIs capture_api_ = VideoCaptureAPIs.ANY;

        //
        // CamCtx
        //

        protected override bool OnOpenCapture(int cap_w, int cap_h, 
            string cap_stream, bool cap_flip_horizontal)
        {
            VideoCapture capture = null;

            VideoCaptureAPIs cap_api;

            if (cap_api_vendor_ == "MSMF") {
                cap_api = VideoCaptureAPIs.MSMF;
            } else if (cap_api_vendor_ == "DSHOW") {
                cap_api = VideoCaptureAPIs.DSHOW;
            } else if (cap_api_vendor_ == "FFMPEG") {
                cap_api = VideoCaptureAPIs.FFMPEG;
            } else {
                cap_api = VideoCaptureAPIs.ANY;
            }

            int cap_index = -1;

            try
            {
                cap_index = int.Parse(cap_stream);
                capture = new VideoCapture(cap_index, cap_api);

                if (!capture.IsOpened())
                {
                    throw new Sample.SampleException("Can't Open Camera!, "
                        + "Check Camera Connection, cap_idx=" 
                        + cap_index);
                }

                int fourcc = FourCC.FromString(cap_codec_);

                capture.Set(VideoCaptureProperties.FourCC, fourcc);
                capture.Set(VideoCaptureProperties.FrameWidth, cap_w);
                capture.Set(VideoCaptureProperties.FrameHeight, cap_h);

                // test capture
                Mat cap_frame = new Mat();

                if (!capture.Read(cap_frame))
                {
                    // 1] change camera index
                    // 2] change capture api => ANY, MSMF, DSHOW, FFMEPG ..
                    throw new Sample.SampleException("1] Camera Is Opened, "
                        + "But Can't Capture Camera!" 
                        + ", Change Camera Index!!!, cap_idx=" + cap_index);
                }
            }
            catch (Exception e)
            {
                capture?.Dispose();
                SetLastErrMsg(e.Message);

                return false;
            }

            capture_ = capture;
            capture_api_ = cap_api;

            //
            // cam_ctx common params
            //

            //cap_wnd_name_ = cap_open_prm_.cap_wnd_name;
            cap_stream_ = cap_stream;
            cap_input_fps_ = ""; // local usb
            cap_width_ = capture_.FrameWidth;
            cap_height_ = capture_.FrameHeight;
            cap_flip_horizontal_ = cap_flip_horizontal;

            SetLastErrMsg("");

            return true;
        }

        protected override void OnCloseCapture()
        {
            if (capture_ != null)
            {
                capture_.Dispose();
                capture_ = null;
            }
        }

        protected override bool OnIsErr()
        {
            if (last_err_msg_ != "")
                return true;

            if (capture_ == null || !capture_.IsOpened())
                return true;

            return false;
        }

        protected override bool OnCaptureFrame(int timeout_ms = 1000, int cap_fail_sleep_ms = 100)
        {
            if (capture_ == null || !capture_.IsOpened())
                return false;

            if (cap_frame_ == null)
                cap_frame_ = new Mat();

            if (!capture_.Read(cap_frame_))
            {
                if (cap_fail_sleep_ms > 0)
                    System.Threading.Thread.Sleep(cap_fail_sleep_ms);

                return false;
            }

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

            return true;
        }

        protected override bool OnTick()
        {
            return true;
        }

        public override string GetInfo(string key = "")
        {
            if(key =="desc_w_h_stream_fps")
            {
                return $"{cap_width};{cap_height};{cap_stream};0";
            }

            return $"dev_type={cap_dev_type_};stream={cap_stream}";
        }

        public override int Exec(string key = "", string prm1 = "", object prm2 = null)
        {

            if (key == "clear_cap_frame")
            {
                ;
            }

            return 0;
        }


        //
        // USBCamCtx
        //

        public CamCtx_USBCam(string cap_api_vendor=CamCtx.CAP_API_VENDOR_UNKNOWN,
            string dev_model_name=CamCtx.CAP_DEV_MODEL_UNKNOWN)
        {
            cap_dev_type_ = DEV_TYPE;
            cap_dev_model_ = dev_model_name;
            cap_api_vendor_ = cap_api_vendor;
        }

    }
}

