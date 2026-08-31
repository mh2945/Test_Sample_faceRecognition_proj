using Alchera.FaceSDK;
using FaceSDKSample.sample;
using OpenCvSharp;
using System;
using System.Threading;
using static Alchera.FaceSDK.FaceSDK;

namespace FaceSDKSample
{
    //
    // Sample
    //

    public partial class Sample
    {        
        public const String REV = DemoClientServer._REV;
        public const String ID = DemoClientServer._REF;
        public const String NAME = DemoClientServer._NAME;

        static Sample inst_;
        static readonly object single_lock_ = new object();

        String exec_path_;
        bool is_created_;

        SampleFaceSDKCtx fsdk_ctx_;
        CamCtx cam_ctx_;

        Sample()
        {
        }

        ~Sample()
        {
            //lock (single_lock_)
            //{                
            //}
        }

        public bool Create(String exec_path)
        {
            if (is_created_)
                return true;

            exec_path_ = exec_path;
            is_created_ = true;

            return true;
        }

        public static Sample inst
        {
            get
            {
                if (inst_ == null)
                {
                    lock (single_lock_)
                    {
                        if (inst_ == null)
                        {
                            inst_ = new Sample();
                        }
                    }
                }
                return inst_;
            }
        }

        public class SampleException : Exception
        {
            public SampleException(string message)
                : base(message)
            {
            }
        }


        //
        // FaceSDKCtx
        //

        public void CreateFaceSDKCtx()
        {
            if (fsdk_ctx_ != null)
            {
                return;
            }

            fsdk_ctx_ = new SampleFaceSDKCtx(this.exec_path_, "models");
        }

        public String GetFaceSDKModelPath()
        {
            return this.exec_path_ + "\\" + "models";
        }

        public static SampleFaceSDKCtx fsdk
        {
            get
            {
                if (Sample.inst == null)
                    return null;

                return Sample.inst.fsdk_ctx_;
            }
        }

        //
        // CamCtx
        //

        public void OpenCapture(CamCtx cam_ctx, int cam_width=1280, int cam_height=720,
            string cap_stream = "0", bool cap_flip_horizontal=false)
        {
            if (cam_ctx_ != null)
            {
                cam_ctx_.Dispose();
                Thread.Sleep(100);
                cam_ctx_ = null;
            }

            cam_ctx_ = cam_ctx;

            //if(cam_ctx_ == null)
            //{
            //    throw new SampleException($"UnSupported Capture Device.. type={cap_dev_type}, name={cap_dev_name}");
            //}

            //CamCtx.CapOpenParm open_prm = new CamCtx.CapOpenParm();

            //open_prm.cap_dev_type = cap_dev_type;
            //open_prm.cap_dev_name = cap_dev_name;
            //open_prm.cap_stream = cap_stream;

            //open_prm.cap_flip_horizontal = cap_flip_horizontal;
            //open_prm.cap_method_backend = "ANY";
            //open_prm.cap_width = cam_width;
            //open_prm.cap_height = cam_height;

            //String cam_wnd_name = "FaceSDKSample" + Sample.REV + "_" + Sample.ID;
            //open_prm.cap_wnd_name = cam_wnd_name;

            if (!cam_ctx_.Open(cam_width, cam_height, cap_stream, cap_flip_horizontal))
            {
                string ex_msg = cam_ctx_.GetLastErrMsg();
                cam_ctx_.Dispose();
                cam_ctx_ = null;
                throw new SampleException(ex_msg);
            }
        }

        public bool GetCaptureInfo(out string cap_stream, out int cap_width, out int cap_height)
        {
            cap_stream = "0";
            cap_width = 0;
            cap_height = 0;

            if (cam_ctx_ == null || cam_ctx_.IsErr())
                return false;

            cap_stream = cam_ctx_.cap_stream;
            cap_width = cam_ctx_.cap_width;
            cap_height = cam_ctx_.cap_height;

            return true;
        }


        public bool GetCameraWidthHeight(out string cam_stream, out int cam_width, out int cam_height)
        {
            cam_stream = "0";
            cam_width = 0;
            cam_height = 0;

            if (cam_ctx_ == null || cam_ctx_.IsErr())
                return false;

            cam_stream = cam_ctx_.cap_stream;
            cam_width = cam_ctx_.cap_width;
            cam_height = cam_ctx_.cap_height;

            return true;
        }

        public void CloseCamera()
        {
            if (cam_ctx_ != null)
                return;

            cam_ctx_.Close();
        }
               
        public static CamCtx cam
        {
            get
            {
                if (Sample.inst == null)
                    return null;

                return Sample.inst.cam_ctx_;
            }
        }

        //
        // image function
        //

        public static bool ImgScore_GetScore(out string err_str, out float fas_score, 
            out float liv_score, string img_path)
        {
            fas_score = -1.0f;
            liv_score = -1.0f;

            err_str = "";

            CamCtx.BGRImg bgr_img = CamCtx.MakeBGRFromImg(img_path);

            if (bgr_img == null) {
                err_str = "can't read img, path=" + img_path;
                return false;
            }

            bool use_continuous_img_face_detect = false;
            FaceSDK fsdk = Sample.fsdk.facesdk_;            
            
            FaceSDK.Face detected_face;
            
            FaceSDK.Rst detect_rst = fsdk.DetectFace(
                bgr_img.pixels_bgr, bgr_img.width, bgr_img.height, 
                use_continuous_img_face_detect);

            if (detect_rst.IsErr())
            {
                err_str = detect_rst.GetLastErrStr() + ",FaceSDK.DetectFace";
                return false;
            }

            int detected_face_cnt = detect_rst.GetFaceCnt();

            if (detected_face_cnt == 0)
            {
                err_str = "no face is detected";
                return false;
            }

            detected_face = detect_rst.GetFace();

            //
            // fas
            //

            FaceSDK.Rst rst_fas = fsdk.GetImgFaceQualityForLiveness(
                bgr_img.pixels_bgr, bgr_img.width,
                bgr_img.height, ref detected_face);

            if (rst_fas.IsErr())
            {
                err_str = "[FAS] " + rst_fas.GetLastErrStr();
                return false;
            }

            fas_score = rst_fas.img_face_quality_for_liveness;

            //
            // bgr liv
            //

            // You have to call with at least four continues image to get accurate bgr liveness result
            // You must input `face` parameter as detected face information of `img4`
            // > This API doesn't need to manage cache
            // > so you don't need to care calling `fsdkc_antisp_reset_check_liveness()`

            FaceSDK.Rst rst_liv = fsdk.GetImgLivenessMulti(
                bgr_img.pixels_bgr, bgr_img.width, bgr_img.height,
                bgr_img.pixels_bgr, bgr_img.width, bgr_img.height,
                bgr_img.pixels_bgr, bgr_img.width, bgr_img.height,
                bgr_img.pixels_bgr, bgr_img.width, bgr_img.height,
                ref detected_face);

            /* alternative call)
                fsdk.ResetImgLivenessCheck(); // clear previous cache result of 'GetImgLiveness'
                FaceSDK.Rst rst_liv = fsdk.GetImgLiveness(bgr_img.pixels_bgr, bgr_img.width, bgr_img.height, ref detected_face);
                fsdk.ResetImgLivenessCheck();            
             */

            if (rst_liv.IsErr())
            {
                err_str = "[BGR-LIV] " + rst_liv.GetLastErrStr();
                return false;
            }

            liv_score = rst_liv.img_liveness;

            return true;
        }


        //public static bool ImgScore_GetFAS(out string err_str, out float score, string img_path)
        //{
        //    score = -1.0f;
        //    err_str = "";

        //    CamCtx.BGRImg bgr_img = CamCtx.MakeBGRFromImg(img_path);

        //    if (bgr_img == null)
        //    {
        //        err_str = "can't read img, path=" + img_path;
        //        return false;
        //    }

        //    bool use_continuous_img_face_detect = false;
        //    FaceSDK fsdk = Sample.fsdk.facesdk_;

        //    FaceSDK.Face detected_face;

        //    FaceSDK.Rst detect_rst = fsdk.DetectFace(
        //        bgr_img.pixels_bgr, bgr_img.width, bgr_img.height,
        //        use_continuous_img_face_detect);

        //    if (detect_rst.IsErr())
        //    {
        //        err_str = detect_rst.GetLastErrStr() + ",FaceSDK.DetectFace";
        //        return false;
        //    }

        //    int detected_face_cnt = detect_rst.GetFaceCnt();

        //    if (detected_face_cnt == 0)
        //    {
        //        err_str = "no face is detected";
        //        return false;
        //    }

        //    detected_face = detect_rst.GetFace();

        //    FaceSDK.Rst rst = fsdk.GetImgFaceQualityForLiveness(
        //        bgr_img.pixels_bgr, bgr_img.width,
        //        bgr_img.height, ref detected_face);

        //    if (rst.IsErr())
        //    {
        //        err_str = rst.GetLastErrStr();
        //        return false;
        //    }

        //    score = rst.img_face_quality_for_liveness;

        //    return true;
        //}

    }

}
