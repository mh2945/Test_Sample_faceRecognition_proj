using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FaceSDKSample.sample
{  
    public abstract class CamCtx : IDisposable
    {
        public const String REV = "260325142210";
        public const String OPENCV_SHARP_VER = "4.11.0.20250507";

        public static long get_utc_ms()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        //
        // interface
        //

        protected abstract bool OnOpenCapture(int cap_w, int cap_h, 
            string cap_stream, bool cap_flip_horizontal);
        protected abstract void OnCloseCapture();

        protected abstract bool OnIsErr();
        protected abstract bool OnCaptureFrame(int timeout_ms = 1000, int cap_fail_sleep_ms = 100);

        protected abstract bool OnTick();

        public abstract string GetInfo(string key="");

        public virtual int Exec(string key = "", string prm1 = "", object prm2 = null) { return 0; }
        
        //
        // CamCtx
        //
        
        protected String cap_wnd_name_ = "FaceSDK" + DateTime.UtcNow.ToString("HHmmss")
                + new Random().Next(10, 99).ToString();

        public const string CAP_API_VENDOR_UNKNOWN = "UNKNOWN";
        public const string CAP_DEV_MODEL_UNKNOWN = "UNKNOWN";

        protected string cap_dev_type_ = "";
        public string cap_dev_type { get { return cap_dev_type_; } }

        protected string cap_dev_model_ = "";
        public string cap_dev_model { get { return cap_dev_model_; } }

        protected string cap_codec_ = "MJPG"; // MJPG, H.264, VP90
        protected string cap_api_vendor_ = "UNKNOWN"; // UNKNOWN, MSMF, FFMPEG, GSTREAMER, DSHOW

        // [usbcam]   "mjpeg-local-opencv"
        // [ipcamera] "mjpeg-svr-ws", "rtsp-ffmpeg", "rtsp-openv"
        //protected string cap_method_ = "mjpeg-local-opencv";

        protected string cap_remote_proto_ = "";
        public string cap_remote_proto { get { return cap_remote_proto_; } }

        protected string cap_stream_ = "";
        public string cap_stream { get { return cap_stream_; } }

        protected string cap_input_fps_ = "";
        public string cap_input_fps { get { return cap_input_fps_; } }

        protected int cap_width_ = 1280;
        public int cap_width { get { return cap_width_; } }

        protected int cap_height_ = 720;
        public int cap_height { get { return cap_height_; } }

        protected bool cap_flip_horizontal_ = false;
        public bool cap_flip_horizontal { get { return cap_flip_horizontal_; } }

        //public int capture_index { get { return capture_index_; } }
        public int elapsed_sec_cam_open_ = 0;
        public String last_err_msg_ = "";

        protected Mat cap_frame_;
        protected Mat cap_frame_tmp_;
        
        private bool disposed_called_ = false;

        public void Dispose()
        {
            if (disposed_called_)
                return;

            disposed_called_ = true;
            Close();
        }        
        
        public CamCtx()
        {
            //if (_inst_ != null)
            //{
            //    throw new Sample.SampleException("Cannot create more than one CamOpenCVCtx instance..");
            //}
            //_inst_ = this;
        }

        ~CamCtx()
        {
            Dispose();
            //_inst_ = null;
        }

        protected void SetLastErrMsg(String msg = "")
        {
            last_err_msg_ = msg;
        }

        public String GetLastErrMsg()
        {
            return last_err_msg_;
        }

        public void CloseCapture()
        {            
            if (cap_frame_tmp_ != null)
            {
                cap_frame_tmp_.Dispose();
                cap_frame_tmp_ = null;
            }

            if (cap_frame_ != null)
            {
                cap_frame_.Dispose();
                cap_frame_ = null;
            }

            OnCloseCapture();
        }

        public void Close()
        {
            CloseCapture();
        }

        public bool IsErr()
        {
            if (last_err_msg_ != "")
                return true;

            if (OnIsErr())
                return true;

            return false;
        }

        public bool Tick()
        {
            OnTick();

            return true;
        }

        //
        // CapOpenParm
        //
               

        public bool Open(int cap_w, int cap_h, string cap_stream, bool cap_flip_horizontal)
        {
            Stopwatch sw_cam_open = new Stopwatch();
            sw_cam_open.Start();

            CloseCapture();

            if(!OnOpenCapture(cap_w, cap_h, cap_stream, cap_flip_horizontal))
            {
                return false;
            }

            SetLastErrMsg("");

            sw_cam_open.Stop();

            elapsed_sec_cam_open_ = (int)(sw_cam_open.ElapsedMilliseconds / 1000.0f);

            return true;
        }

        public void SetCaptureFlipHorizontal(bool flip)
        {
            cap_flip_horizontal_ = flip;
        }

        public bool CaptureFrame(int timeout_ms = 1000, int cap_fail_sleep_ms = 100)
        {
            if(!OnCaptureFrame(timeout_ms, cap_fail_sleep_ms))
            {
                return false;
            }

            return true;
        }

        public Mat GetCaptureFrameAsMat()
        {
            return cap_frame_;
        }

        public class BGRImg
        {
            public int channels;
            public byte[] pixels_bgr;
            public int width;
            public int height;

            public BGRImg(byte[] pixels, int width, int height, int channels = 3)
            {
                this.pixels_bgr = pixels;
                this.width = width;
                this.height = height;
                this.channels = channels;
            }
        }

        public BGRImg GetCaptureFrameAsBGRImg()
        {
            if (cap_frame_ == null)
                return null;

            if (cap_frame_.Channels() != 3) // BGR Only
                return null;

            if (cap_frame_.Type() != MatType.CV_8UC3)
                return null;

            if (!cap_frame_.IsContinuous())
                return null;

            int totalBytes = (int)(cap_frame_.Total() * cap_frame_.ElemSize());

            if (totalBytes == 0)
                return null;

            byte[] pixels_bgr = new byte[totalBytes];

            Marshal.Copy(cap_frame_.Data, pixels_bgr, 0, totalBytes);

            return new BGRImg(pixels_bgr, cap_frame_.Width, cap_frame_.Height, cap_frame_.Channels());
        }

        public bool RenderFrame(Mat mat = null, String wnd_name = null)
        {
            if (mat == null)
                return false;

            if (wnd_name == null)
                wnd_name = cap_wnd_name_;

            Cv2.ImShow(wnd_name, mat);
            Cv2.WaitKey(1); // render opencv wnd within 1ms

            return true;
        }

        public static Mat MakeMatFromBGR(byte[] bgr_pixels, int width, int height)
        {
            if (bgr_pixels == null)
                return null;

            Mat mat = new Mat(height, width, MatType.CV_8UC3);

            int len = height * width * mat.Channels();
            if (bgr_pixels.Length != len)
                return null;

            Marshal.Copy(bgr_pixels, 0, mat.Data, bgr_pixels.Length);

            return mat;
        }

        public static Mat ResizeMat(Mat src, Size new_siz)
        {
            if (src == null)
                return null;

            Mat dst = new Mat();
            Cv2.Resize(src, dst, new_siz);

            return dst;
        }


        //
        // Draw
        //

        public static void DrawImg(Mat render_mat, Point pos,
            byte[] img_bgr, int img_w, int img_h)
        {
            Mat mat_img = MakeMatFromBGR(img_bgr, img_w, img_h);

            if (mat_img == null)
                return;

            Rect chk_roi = new Rect(pos.X, pos.Y, mat_img.Width, mat_img.Height);

            int width = Math.Min(chk_roi.Width, render_mat.Width - chk_roi.X);
            int height = Math.Min(chk_roi.Height, render_mat.Height - chk_roi.Y);

            Mat mat_dst = new Mat(render_mat, new Rect(pos.X, pos.Y, width, height));
            mat_img.CopyTo(mat_dst);
        }

        public static void DrawImgScaled(Mat render_mat, Point pos, Size draw_siz,
            byte[] img_bgr, int img_w, int img_h)
        {
            Mat mat_img = MakeMatFromBGR(img_bgr, img_w, img_h);

            if (mat_img == null)
                return;

            Mat mat_img_scaled = CamCtx.ResizeMat(mat_img, draw_siz);

            Rect chk_roi = new Rect(pos.X, pos.Y, mat_img_scaled.Width, mat_img_scaled.Height);

            int width = Math.Min(chk_roi.Width, render_mat.Width - chk_roi.X);
            int height = Math.Min(chk_roi.Height, render_mat.Height - chk_roi.Y);

            Mat mat_dst = new Mat(render_mat, new Rect(pos.X, pos.Y, width, height));
            mat_img_scaled.CopyTo(mat_dst);
        }

        public static void DrawText(Mat mat, String text, OpenCvSharp.Point pos, Scalar color, double font_scl = 1,
            int thickness = 1, LineTypes lineType = LineTypes.AntiAlias)
        {
            DrawText(mat, text, pos.X, pos.Y, color, font_scl, thickness, lineType);
        }

        public static void DrawText(Mat mat, String text, int x, int y, Scalar color, double font_scl = 1,
            int thickness = 1, LineTypes lineType = LineTypes.AntiAlias)
        {
            if (mat == null || text == null || text.Length == 0)
                return;

            Cv2.PutText(mat, text, new Point(x, y), HersheyFonts.HersheyComplex,
                font_scl, color, thickness, lineType);
        }

        public static void DrawBoxXYWH(Mat mat, Point xy, Point wh, Scalar color, int thickness = 2)
        {
            if (mat == null)
                return;

            Cv2.Rectangle(mat, xy, xy + wh, color, thickness);
        }

        public static void DrawLogBox(Mat mat, Point xy, Point wh,
            Scalar border_color, Scalar inner_color, int border_siz,
            List<string> txts, double fnt_scl = 0.5f, int fnt_thickness = 1)
        {
            if (mat == null)
                return;

            Cv2.Rectangle(mat, xy, xy + wh, border_color, border_siz);

            // -1 for fill rectangle
            Cv2.Rectangle(mat,
                xy + new Point(border_siz, border_siz),
                xy + wh - new Point(border_siz, border_siz),
                inner_color,
                -1);

            if (txts != null)
            {
                Point base_txt_pt = new Point(xy.X, xy.Y) + new Point(border_siz, border_siz);
                Point cur_pt = base_txt_pt;
                HersheyFonts font_face = HersheyFonts.HersheyComplex;
                Scalar fnt_clr = new Scalar(255, 255, 255);
                LineTypes fnt_lineType = LineTypes.AntiAlias;

                for (var c = 0; c < txts.Count; c++)
                {
                    var t = txts[c];

                    if(t == null || t == "") continue;

                    Size textSize
                    = Cv2.GetTextSize(t, font_face, fnt_scl, fnt_thickness, out int baseline);

                    cur_pt += new Point(0, textSize.Height + 5);

                    Scalar draw_fnt_clr;
                    double draw_fnt_scl = fnt_scl;

                    if (t.StartsWith("[I]"))
                    {
                        draw_fnt_clr = fnt_clr;
                    }
                    else if (t.StartsWith("[OK]"))
                    {
                        draw_fnt_scl *= 0.95;
                        draw_fnt_clr = new Scalar(0, 255, 0);
                    }
                    else if (t.StartsWith("[FAIL]"))
                    {
                        draw_fnt_scl *= 0.95;
                        draw_fnt_clr = new Scalar(0, 0, 255);
                    }
                    else if (t.StartsWith("[E]"))
                    {
                        draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(0, 0, 255);
                    }
                    else if (t.StartsWith("[EX]"))
                    {
                        draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(0, 64, 255);
                    }
                    else if (t.StartsWith("[SV-REQ]"))
                    {
                        draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(153, 55, 255);
                    }
                    else if (t.StartsWith("[SV-OK]"))
                    {
                        draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(0, 255, 0);
                    }
                    else if (t.StartsWith("[SV-FAIL]"))
                    {
                        //draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(0, 0, 255);
                    }
                    else if (t.StartsWith("[HTTP-OK]"))
                    {
                        draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(128, 255, 0);
                    }
                    else if (t.StartsWith("[HTTP-FAIL]"))
                    {
                        //draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(0, 0, 255);
                    }
                    else if (t.StartsWith("[ALARM]"))
                    {
                        //draw_fnt_scl *= 0.9;
                        draw_fnt_clr = new Scalar(0, 255, 255);
                    }
                    else
                    {
                        draw_fnt_clr = new Scalar(0, 0, 255); ;
                    }

                    Cv2.PutText(mat, t, cur_pt, font_face,
                        draw_fnt_scl, draw_fnt_clr, fnt_thickness, fnt_lineType);
                }
            }
        }


        //
        //
        //

        public static void RenderCicleHoleMat(Mat render_mat, float cicle_radious_ratio = 0.3f,
            float alpha_outer_fill_ratio = 0.5f)
        {
            if (render_mat == null)
                return;

            Mat render_mat_bgra = new Mat(render_mat.Size(), MatType.CV_8UC4);
            Mat circle_mat_bgra = new Mat(render_mat.Size(), MatType.CV_8UC4);

            {
                Mat[] srcChannels = Cv2.Split(render_mat);
                Mat alphaChannel = Mat.Ones(render_mat.Size(), MatType.CV_8U) * 255;
                Mat[] dstChannels = new Mat[] { srcChannels[0], srcChannels[1], srcChannels[2], alphaChannel };

                Cv2.Merge(dstChannels, render_mat_bgra);

                for (int i = 0; i < 3; i++)
                {
                    if (alpha_outer_fill_ratio == 0.0f)
                    {
                        srcChannels[i].SetTo(new Scalar(255));
                    }
                    else
                    {
                        srcChannels[i] /= alpha_outer_fill_ratio;
                    }
                }

                dstChannels = new Mat[] { srcChannels[0], srcChannels[1], srcChannels[2], alphaChannel };

                Cv2.Merge(dstChannels, circle_mat_bgra);
            }

            Point cicle_center = new Point(render_mat.Width / 2, render_mat.Height / 2);
            int circle_radius = (int)((float)render_mat.Width * cicle_radious_ratio);

            Mat mask_circle = circle_mat_bgra.Clone();
            mask_circle.SetTo(new Scalar(255, 255, 255, 255));

            Cv2.Circle(mask_circle, cicle_center, circle_radius, new Scalar(0, 0, 0, 0), -1);

            circle_mat_bgra.CopyTo(render_mat_bgra, mask_circle);

            //////////////////////////////////////////////

            Mat[] channels = Cv2.Split(render_mat_bgra);
            Mat render_mat_bgr = new Mat();
            Cv2.Merge(new Mat[] { channels[0], channels[1], channels[2] }, render_mat_bgr);

            render_mat_bgr.CopyTo(render_mat);
        }


        //
        // RGB Histogrm 
        //

        public class RGBHistogramData {
            public Mat r = new Mat();
            public Mat g = new Mat();
            public Mat b = new Mat();

            public float binW = 0.0f;
            public int histSize = 256;
            public Rangef range = new Rangef(0, 256);

            public int width = 0;
            public int height = 0;

            ~RGBHistogramData()
            {
                r.Dispose();
                g.Dispose();
                b.Dispose();
            }
        }

        public static RGBHistogramData MakeHistogramMat(Mat inputMat, int width, int height)
        {
            RGBHistogramData hist = new RGBHistogramData();

            if (inputMat == null)
                return hist;

            Mat histImg = new Mat(new Size(width, height), MatType.CV_8UC3, Scalar.All(0));

            hist.width = width;
            hist.height = height;

            const int histSize = 256;
            hist.histSize = histSize;

            int[] hdims = { histSize };
            hist.range = new Rangef(0, 256);

            Rangef[] ranges = { hist.range };

            Mat[] bgrPlanes;
            Cv2.Split(inputMat, out bgrPlanes);

            Cv2.CalcHist(new Mat[] { bgrPlanes[2] }, new int[] { 0 }, null, hist.r, 1, hdims, ranges);
            Cv2.CalcHist(new Mat[] { bgrPlanes[1] }, new int[] { 0 }, null, hist.g, 1, hdims, ranges);
            Cv2.CalcHist(new Mat[] { bgrPlanes[0] }, new int[] { 0 }, null, hist.b, 1, hdims, ranges);

            Cv2.Normalize(hist.r, hist.r, 0, histImg.Rows, NormTypes.MinMax);
            Cv2.Normalize(hist.g, hist.g, 0, histImg.Rows, NormTypes.MinMax);
            Cv2.Normalize(hist.b, hist.b, 0, histImg.Rows, NormTypes.MinMax);

            //int binW = (int)Math.Round((double)width / histSize);
            //hist.binWidth = binW;
            //hist.binWidth = 1;
            hist.binW = (float)width / histSize;

            bgrPlanes[0].Dispose();
            bgrPlanes[1].Dispose();
            bgrPlanes[2].Dispose();

            return hist;
        }

        public static void RenderHistogram(Mat renderMat, RGBHistogramData src1, Point pos, int thickness = 2)
        {
            if (renderMat == null || src1 == null)
                return;

            float binW = src1.binW;
            int height = src1.height;

            Mat mat_r = new Mat(new Size(src1.width, src1.height), MatType.CV_8UC4, new Scalar(0, 0, 0, 255));
            Mat mat_g = new Mat(new Size(src1.width, src1.height), MatType.CV_8UC4, new Scalar(0, 0, 0, 255));
            Mat mat_b = new Mat(new Size(src1.width, src1.height), MatType.CV_8UC4, new Scalar(0, 0, 0, 255));

            int pre_x = 0;
            int pre_y = 0;

            for (int i = 1; i < src1.histSize; i++)
            {
                Cv2.Line(mat_r,
                         new Point(pre_x + binW * (i - 1), pre_y + height - (int)Math.Round(src1.r.At<float>(i - 1))),
                         new Point(pre_x + binW * (i), pre_y + height - (int)Math.Round(src1.r.At<float>(i))),
                         new Scalar(0, 0, 255, 255), thickness);

                Cv2.Line(mat_g,
                         new Point(pre_x + binW * (i - 1), pre_y + height - (int)Math.Round(src1.g.At<float>(i - 1))),
                         new Point(pre_x + binW * (i), pre_y + height - (int)Math.Round(src1.g.At<float>(i))),
                         new Scalar(0, 255, 0, 255), thickness);

                Cv2.Line(mat_b,
                         new Point(pre_x + binW * (i - 1), pre_y + height - (int)Math.Round(src1.b.At<float>(i - 1))),
                         new Point(pre_x + binW * (i), pre_y + height - (int)Math.Round(src1.b.At<float>(i))),
                         new Scalar(255, 0, 0, 255), thickness);
            }

            Mat overlay = new Mat(new Size(src1.width, src1.height), MatType.CV_8UC4, new Scalar(0, 0, 0, 255));

            Mat temp = new Mat();
            Cv2.AddWeighted(overlay, 1.0, mat_r, 1.0, 0, temp);
            temp.CopyTo(overlay);

            Cv2.AddWeighted(overlay, 1.0, mat_g, 1.0, 0, temp);
            temp.CopyTo(overlay);

            Cv2.AddWeighted(overlay, 1.0, mat_b, 1.0, 0, temp);
            temp.CopyTo(overlay);

            Mat[] channels = Cv2.Split(overlay);
            Mat bgrOverlay = new Mat();
            Cv2.Merge(new Mat[] { channels[0], channels[1], channels[2] }, bgrOverlay);

            Mat mask = new Mat();
            Cv2.CvtColor(bgrOverlay, mask, ColorConversionCodes.BGR2GRAY);
            Cv2.Threshold(mask, mask, 0, 255, ThresholdTypes.Binary);

            ///////////////////////////////////

            int safe_w = Math.Min(bgrOverlay.Width, renderMat.Width - pos.X);
            int safe_h = Math.Min(bgrOverlay.Height, renderMat.Height - pos.Y);

            Rect roi = new Rect(pos, new Size(safe_w, safe_h));
            Mat dstRoi = new Mat(renderMat, roi);

            bgrOverlay.CopyTo(dstRoi, mask);
        }


        //
        // image util via opencv
        //

        public enum FixedRotate
        {
            INVALID,
            CCW_90,   // +90
            CCW_180,  // +180
            CCW_270,  // +270
            CW_90,    // -90
            CW_180,   // -180
            CW_270,   // -270            
        }

        public static FixedRotate ConvFixedRotate(string angle_string)
        {
            switch (angle_string)
            {
                case "90": return FixedRotate.CCW_90;
                case "180": return FixedRotate.CCW_180;
                case "270": return FixedRotate.CCW_270;
                case "-90": return FixedRotate.CW_90;
                case "-180": return FixedRotate.CW_180;
                case "-270": return FixedRotate.CW_270;
                default:
                    return FixedRotate.INVALID;
            }
        }

        // no interpolation, only rearrage pixel
        public static BGRImg FixedRotateBGRImage(FixedRotate angle, byte[] bgr_pixels, int width, int height)
        {
            if (bgr_pixels == null)
                return null;

            RotateFlags rot_flag;

            switch (angle)
            {
                case FixedRotate.CCW_90:
                    rot_flag = RotateFlags.Rotate90Counterclockwise;
                    break;
                case FixedRotate.CCW_180:
                    rot_flag = RotateFlags.Rotate180;
                    break;
                case FixedRotate.CCW_270:
                    rot_flag = RotateFlags.Rotate90Clockwise;
                    break;
                case FixedRotate.CW_90:
                    rot_flag = RotateFlags.Rotate90Clockwise;
                    break;
                case FixedRotate.CW_180:
                    rot_flag = RotateFlags.Rotate180;
                    break;
                case FixedRotate.CW_270:
                    rot_flag = RotateFlags.Rotate90Counterclockwise;
                    break;
                default:
                    return null;
            }

            Mat src = MakeMatFromBGR(bgr_pixels, width, height);
            Mat dst = new Mat();

            Cv2.Rotate(src, dst, rot_flag);

            int total_pixel_bytes = (int)(dst.Total() * dst.ElemSize());
            byte[] pixels_bgr = new byte[total_pixel_bytes];

            Marshal.Copy(dst.Data, pixels_bgr, 0, total_pixel_bytes);

            return new BGRImg(pixels_bgr, dst.Width, dst.Height, dst.Channels());
        }

        public static BGRImg MakeBGRFromImg(string img_path)
        {
            Mat img = Cv2.ImRead(img_path, ImreadModes.Color);

            if (img.Empty())
            {                
                return null;
            }

            if (img.Channels() == 4)
            {
                Cv2.CvtColor(img, img, ColorConversionCodes.BGRA2BGR);
            }

            int img_width = img.Width;
            int img_height = img.Height;
            int img_data_byte_siz = (int)(img.Total() * img.ElemSize());
            byte[] img_bgr_pixels = new byte[img_data_byte_siz];

            Marshal.Copy(img.Data, img_bgr_pixels, 0, img_data_byte_siz);

            return new BGRImg(img_bgr_pixels, img_width, img_height);
        }


        //
        // cam resolution util
        //

        public class CamResResolver
        {
            public struct Res
            {
                public int w;
                public int h;
            }

            public static readonly List<Res> res_candi_ = new List<Res> {
                //new Res { w=320, h=240 },
                new Res { w=640, h=480 },
                new Res { w=800, h=600 },
                new Res { w=1024, h=768 },
                new Res { w=1280, h=720 },
                new Res { w=1280, h=960 },
                new Res { w=1920, h=1080 },
                new Res { w=1920, h=1088 },
                new Res { w=1080, h=1920 },
                new Res { w=2560, h=1440 },
                //new Res { w=3840, h=2160 },
            };

            public static List<Res> ResolveCapRes(int cap_index = 0, int prefer_res_w = 1280, int prefer_res_h = 720)
            {
                List<Res> rst_cap_res = new List<Res>();

                using (var cap = new VideoCapture(cap_index))
                {
                    if (!cap.IsOpened())
                    {
                        Console.WriteLine("[ERR] Camera open failed!, cap_index=", cap_index);
                        return null;
                    }

                    foreach (var res in CamResResolver.res_candi_)
                    {
                        cap.Set(VideoCaptureProperties.FrameWidth, res.w);
                        cap.Set(VideoCaptureProperties.FrameHeight, res.h);

                        int cap_w = (int)cap.Get(VideoCaptureProperties.FrameWidth);
                        int cap_h = (int)cap.Get(VideoCaptureProperties.FrameHeight);

                        if (Math.Abs(cap_w - res.w) < 10 &&
                            Math.Abs(cap_h - res.h) < 10)
                        {
                            if (cap_w >= prefer_res_w && cap_h >= prefer_res_h)
                            {
                                rst_cap_res.Add(new Res { w = cap_w, h = cap_h });
                            }
                        }
                    }
                }

                return rst_cap_res;
            }

            public static List<Res> GetAllCapResolutions(int cap_index = 0)
            {
                List<Res> rst_cap_res = new List<Res>();

                using (var cap = new VideoCapture(cap_index))
                {
                    if (!cap.IsOpened())
                    {
                        Console.WriteLine("[ERR] Camera open failed!, cap_index=", cap_index);
                        return null;
                    }

                    foreach (var res in CamResResolver.res_candi_)
                    {
                        cap.Set(VideoCaptureProperties.FrameWidth, res.w);
                        cap.Set(VideoCaptureProperties.FrameHeight, res.h);

                        int cap_w = (int)cap.Get(VideoCaptureProperties.FrameWidth);
                        int cap_h = (int)cap.Get(VideoCaptureProperties.FrameHeight);

                        if(-1 == find_resolution(rst_cap_res, cap_w, cap_h))
                            rst_cap_res.Add(new Res { w = cap_w, h = cap_h });
                    }
                }

                return rst_cap_res;
            }

            public static int find_resolution(in List<Res> res,  int w, int h)
            {
                int idx = 0;

                foreach (var cur in res)
                {
                    if (cur.w == w && cur.h == h)
                        return idx;

                    idx++;
                }

                return -1;
            }
            public static List<Res> filter_resolution(in List<Res> res, int min_w, int min_h)
            {
                List<Res> rst = new List<Res>();

                foreach (var cur in res)
                {
                    if (cur.w >= min_w && cur.h >= min_h)
                    {
                        rst.Add(new Res { w = cur.w, h = cur.h });
                    }
                }

                return rst;
            }

            public static string[] ToStringAry(in List<Res> res)
            {
                if(res.Count == 0)
                    return new string[] { };

                string[] rst = new string[res.Count];

                int idx = 0;

                foreach (var cur in res)
                {
                    rst[idx] = $"{cur.w}x{cur.h}";
                    idx++;
                }

                return rst;
            }

            public static bool ParseAsWidthxHeight(in string res_str, out int width, out int height)
            {
                width = 0;
                height = 0;

                if (res_str == "")
                    return false;

                int delm_idx = res_str.IndexOf("x");

                if (delm_idx == -1)
                    return false;

                string w_str = res_str.Substring(0, delm_idx).Trim();
                string h_str = res_str.Substring(delm_idx + 1).Trim();

                if (int.TryParse(w_str, out width) && int.TryParse(h_str, out height))
                    return true;

                return false;
            }


        }



    }

}