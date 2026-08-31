using Alchera.FaceSDK;
using Alchera.FaceSDK.FaceServer;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Alchera.FaceSDK.FaceSDK;
using static FaceSDKSample.sample.CamCtx;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FaceSDKSample.sample
{
    public partial class DemoClientServer : Demo
    {
        public const string _REV = "260330164710";
        public const string _REF = "";
        public const string _NAME = "ClientServer";

        bool exit_called_ = false;

        //
        // user parameter
        //

        public class UserPrm
        {
            public SampleFaceSDKCtx fsdk_;
            public CamCtx cam_;            

            public SampleProp.Settings prop_settings_;
            public SampleProp.DemoClientServerProp prop_demo_;
            public SampleProp.SamplePropFaceInfo prop_faceinfo_;

            public class Msg
            {
                public int idx_ = 0;
                public Object prm0_ = null;
                public Object prm1_ = null;
                public Object prm2_ = null;

                Msg() { }
                public Msg(int idx, object prm0 = null, object prm1 = null, object prm2 = null)
                {
                    idx_ = idx;
                    prm0_ = prm0;
                    prm1_ = prm1;
                    prm2_ = prm2;
                }
            }

            Queue<Msg> msgq_ = new Queue<Msg>();
            readonly object msgq_lock_ = new object();

            public void PostMsg(int idx, object prm0 = null, object prm1 = null, object prm2 = null)
            {
                lock (msgq_lock_)
                {
                    msgq_.Enqueue(new Msg(idx, prm0, prm1, prm2));
                }
            }

            public Msg PopMsg()
            {
                Msg rst = null;
                lock (msgq_lock_)
                {
                    if (msgq_.Count() != 0)
                    {
                        rst = msgq_.Dequeue();
                    }
                }
                return rst;
            }

            public Action ui_onstart_ = () => { };
            public Action ui_onloop_ = () => { };
            public Action ui_onexit_ = () => { };
        }

        public DemoClientServer(UserPrm user_prm)
            : base(_NAME, user_prm)
        {
        }

        ~DemoClientServer()
        {
            Exit();
        }

        public class BestLivShots
        {
            public readonly int MAX_SHOT_CNT;

            public struct Shot
            {
                public byte[] bgr_pixels;
                public int width;
                public int height;
                public float confidence;
                public Face detected_face;
            };

            List<Shot> shots_ = new List<Shot>();
            public int shot_cnt { get { return shots_.Count; } }

            public int last_shot_idx
            {
                get { return shots_.Count > 0 ? (shots_.Count - 1) : -1; }
            }

            public Shot GetShot(int index)
            {
                if (index < 0 || index >= shots_.Count)
                    throw new IndexOutOfRangeException();

                return shots_[index];
            }

            public BlobImg this[int index]
            {
                get
                {
                    if (index < 0 || index >= shots_.Count)
                        throw new IndexOutOfRangeException();

                    return new BlobImg(
                        shots_[index].bgr_pixels,
                        shots_[index].width,
                        shots_[index].height);
                }
            }

            public int[] get_recent_shot_indices(int max_shot_idx_cnt)
            {
                int[] idx = new int[max_shot_idx_cnt];
                int last_shot_idx = shot_cnt - 1;

                for (int i = 0; i < max_shot_idx_cnt; i++)
                {
                    idx[i] = (last_shot_idx + shot_cnt - (max_shot_idx_cnt - 1 - i)) % shot_cnt;
                }

                return idx;
            }

            public BestLivShots(int max_shot_cnt=4)
            {
                MAX_SHOT_CNT = max_shot_cnt;
            }

            ~BestLivShots()
            {
                Clear();
            }

            public void Clear()
            {
                shots_.Clear();
            }

            public bool Push(byte[] bgr_pixels, int w, int h, float ui_liv_confidence, Face face)
            {
                if (shot_cnt + 1 > MAX_SHOT_CNT)
                    return false;

                byte[] cloned_pixels = bgr_pixels.ToArray(); // deep copy

                shots_.Add(new Shot
                {
                    bgr_pixels = cloned_pixels,
                    width = w,
                    height = h,
                    confidence = ui_liv_confidence,
                    detected_face = face,
                });

                return true;
            }

            public bool IsFull()
            {
                return shot_cnt < MAX_SHOT_CNT ? false : true;
            }

            //
            // Draw
            //

            public void Draw(Mat render_mat, Point relative_pos, int shot_idx, string caption_prefix, Size shot_siz,
                Scalar fnt_color, double fnt_scl = 0.7, int fnt_thick = 1)
            {
                Shot s = shots_[shot_idx];

                Mat mat_liv_ori = CamCtx.MakeMatFromBGR(s.bgr_pixels, s.width, s.height);
                Mat mat_liv_resized = CamCtx.ResizeMat(mat_liv_ori, shot_siz);

                string caption = caption_prefix + " Liv=" + s.confidence.ToString("F3");

                CamCtx.DrawText(mat_liv_resized, caption, new Point(5, 15), fnt_color, fnt_scl, fnt_thick);

                Mat mat_roi = new Mat(render_mat, new Rect(relative_pos.X, relative_pos.Y, mat_liv_resized.Cols, mat_liv_resized.Rows));

                mat_liv_resized.CopyTo(mat_roi);
            }

        }

        public static bool CheckCompareInputImgWithFaceWidthRatio(ref string err_msg, 
            float face_width, int input_img_width, float thres_ratio_min, float thres_ratio_max)
        {
            err_msg = "";
            bool face_w_ratio_err = false;

            if (face_width < input_img_width * thres_ratio_min)
            {
                err_msg = $" face width is under, face_w={face_width} < ratio={input_img_width * thres_ratio_min}";
                return false;
            }

            if (face_width > input_img_width * thres_ratio_max)
            {
                err_msg = $" face width is bigger, face_w={face_width} > ratio={input_img_width * thres_ratio_max}";
                return false;
            }

            return true;
        }


        protected static void DrawCompareImg(string title, Mat render_frame, Point relative_pos, BlobImg img,
            Box face_box, Size draw_siz, Scalar fnt_color, double fnt_scl = 0.7, int fnt_thick = 1)
        { 
            Mat mat_liv_ori = CamCtx.MakeMatFromBGR(img.data_, img.width_, img.height_);
            Mat mat_liv_resized = CamCtx.ResizeMat(mat_liv_ori, draw_siz);

            string caption1 = title + "[IN] w=" + img.width_ + " ,h=" + img.height_;
            CamCtx.DrawText(mat_liv_resized, caption1, new Point(5, 15), fnt_color, fnt_scl, fnt_thick);

            string caption2 = "[FBOX] w=" + (int)face_box.w + ",h=" + (int)face_box.h;
            CamCtx.DrawText(mat_liv_resized, caption2, new Point(5, 30), fnt_color, fnt_scl, fnt_thick);

            //--------------------------------

            // clip boundary
            Rect chk_roi = new Rect(relative_pos.X, relative_pos.Y,
                mat_liv_resized.Width, mat_liv_resized.Height);

            int clip_width = Math.Min(chk_roi.Width, render_frame.Width - chk_roi.X);
            int clip_height = Math.Min(chk_roi.Height, render_frame.Height - chk_roi.Y);

            Mat mat_dst = new Mat(render_frame, 
                new Rect(relative_pos.X, relative_pos.Y, clip_width, clip_height));
            
            // -------------------------------
            mat_liv_resized.CopyTo(mat_dst);
        }

        public bool ClearBGRLivenessPrevCheckCache(FaceSDK fsdk)
        {
            if (fsdk == null)
                return false;

            var rst = fsdk.ResetImgLivenessCheck();

            if(rst.IsErr())
            {
                Console.WriteLine("[ERR][ResetImgLivenessCheck()] err=" + rst.GetLastErrStr());
                return false;
            }

            return true;
        }

        private static string FormatBytesPerSec(long bytesPerSec)
        {
            const double KB = 1024.0;
            const double MB = 1024.0 * 1024.0;

            if (bytesPerSec >= MB)
                return $"{bytesPerSec / MB:0.##} MB/s";

            if (bytesPerSec >= KB)
                return $"{bytesPerSec / KB:0.##} KB/s";

            return $"{bytesPerSec} Bytes/s";
        }


        enum DemoStage
        {
            Init,
            DoPassiveLivenessAndCollectLivenessBestShot,
            BGRImageLivenessMulitframe,
            FaceCompare,
            FinalResult,
            Reset,
        };

        DemoStage stage_ = DemoStage.Init;

        void SetDemoStage(DemoStage new_stage)
        {
            stage_ = new_stage;
        }

        DemoStage GetDemoStage()
        {
            return stage_;
        }

        //
        // stat
        //

        public long stat_last_tick_cnt = 0;
        public long stat_last_ren_cv_mat_bytes = 0;

        public long stat_last_update_utc_ms = 0;                

        public long stat_tick_cnt_sec = 0;
        public long stat_ren_cv_mat_bytes_sec = 0;

        public void update_stat()
        {
            long cur_utc_ms = get_utc_ms();
            long dt = cur_utc_ms - stat_last_update_utc_ms;

            if (dt >= 1000)
            {
                long sec = dt / 1000;

                stat_tick_cnt_sec = stat_last_tick_cnt / sec;
                stat_ren_cv_mat_bytes_sec = stat_last_ren_cv_mat_bytes / sec;

                stat_last_update_utc_ms = cur_utc_ms;                
                stat_last_tick_cnt = 0;
            }
        }


        //
        // run
        //

        protected override void Run()
        {
            Cv2.DestroyAllWindows(); // clear previous opencvsharp window

            UserPrm user_prm = (UserPrm)GetUserPrm();
            RunContext ctx = new RunContext(user_prm);
            RecContext rec_ctx = new RecContext();

            DispatchMessage(ctx);

            user_prm.cam_.Exec("clear_cap_frame");

            SetDemoStage(DemoStage.Reset);

            while (IsRunning())
            {
                stat_last_tick_cnt++;
                update_stat();

                SampleProp.Settings prop_settings = user_prm.prop_settings_;
                SampleProp.DemoClientServerProp prop_demo = user_prm.prop_demo_;
                SampleProp.SamplePropFaceInfo prop_faceinfo = user_prm.prop_faceinfo_;

                DispatchMessage(ctx);

                if (user_prm.prop_settings_ == null || user_prm.prop_demo_ == null)
                {
                    System.Threading.Thread.Sleep(1000);
                    continue;
                }

                if (GetDemoStage() == DemoStage.Reset)
                {
                    ctx = new RunContext(user_prm);
					ctx.rec_ctx = rec_ctx;

                    // [clear bgr-liveness cache]
                    // If you input new face info after call 'fsdk.GetImgLiveness()' 4 times,
                    // you must call ResetImgLivenessCheck() to delete cache! 
                    //
                    // OR 
                    //
                    // USE fsdk.GetImgLivenessMulti() instead using fsdk.GetImgLiveness()
                    // > fsdk.GetImgLivenessMulti() doesn't need to manage cache
                    // > so you don't need to care calling `fsdk.ResetImgLivenessCheck()`
                    user_prm.fsdk_.facesdk_.ResetImgLivenessCheck();

                    int LIV_BGR_BESTSHOT_CNT = user_prm.prop_demo_.LIV_BGR_BESTSHOT_CNT;

                    if (LIV_BGR_BESTSHOT_CNT < 1)
                    {
                        throw new Exception($"Invalid LIV_BGR_BESTSHOT_CNT={LIV_BGR_BESTSHOT_CNT}, must be >= 1");
                    }

                    ctx.best_liv_shots = new BestLivShots(LIV_BGR_BESTSHOT_CNT);


                    SetDemoStage(DemoStage.DoPassiveLivenessAndCollectLivenessBestShot);
                    continue;
                }
                

                CamCtx cam = user_prm.cam_;

                cam.SetCaptureFlipHorizontal(prop_settings.CAP_FLIP_HOR);

                FaceSDK fsdk = user_prm.fsdk_.facesdk_;

                Point pt_liv_ui = new Point(10, 10);
                double liv_ui_fnt_scl = 0.4;

                int ui_finish_liveness_multiframe_task_dur_sec
                    = prop_demo.LIV_BGR_IMG_LIV_UI_AUTO_FIN_SEC;

                //
                // faceserver info
                //

                string API_ENC_IMG_TYPE = prop_settings.SVR_FACESVR_API_IMG_ENCRYPT_TYPE;

                // 원래는 CURL GET HTTP://<SVR-IP>/getPublicKey 로 얻은 공개키와 타임스탬프값 이용
                // > 아래는 테스트용 샘플값
                string API_ENC_TIMESTAMP = prop_settings.SVR_FACESVR_API_IMG_ENCRYPT_RSA_AES_TS; // ex: "00000000000000"
                string API_ENC_PUB_KEY_B64 = prop_settings.SVR_FACESVR_API_IMG_ENCRYPT_RSA_AES_PUB_B64; // pkcs-x509.ski

                string api_svr_url;
                Uri api_svr_uri = null;

                if (!Uri.TryCreate(prop_settings.SVR_FACESVR_API_URL, UriKind.Absolute, out api_svr_uri))
                {
                    prop_settings.SVR_FACESVR_API_URL = "https://face-server.example.com";
                    Uri.TryCreate(prop_settings.SVR_FACESVR_API_URL, UriKind.Absolute, out api_svr_uri);

                    // Thresholds are served by the FaceServer validation-config endpoints.
                }
                api_svr_url = api_svr_uri.ToString();

                bool USE_HTTP_PROXY = prop_settings.SVR_HTTP_PROXY_USE;
                Uri HTTP_PROXY_URL = null;

                if (USE_HTTP_PROXY)
                {
                    if (!Uri.TryCreate(prop_settings.SVR_HTTP_PROXY_URL, UriKind.Absolute, out HTTP_PROXY_URL))
                        HTTP_PROXY_URL = new Uri("http://127.0.0.1:8000");
                }

                //bool save_debug_crop_img_to_bmp_file = false;


                const float FACE_EYE_MOUTH_OCCLUSION_THRESHOLD = 0.5f;

                const float FACE_EYE_CLOSED_STATE_THRESHOLD = 2.4f;  //  eye is closed:   conf <= 2.4f

                const float FACE_DETECT_LANDMARK_CONFIDENCE_THRESHOLD = 0.90f;

                bool LIV_BGR_IMG_CONTINOUS_CHECK = prop_demo.LIV_BGR_BESTSHOT_CONTINUOUS_FOUR_BGR_QUALITY_CHECK;
                                

                //
                // Capture
                //                                

                cam.Tick();

                int cap_img_bgr_width = 0;
                int cap_img_bgr_height = 0;
                byte[] cap_img_bgr_pixels = null;

                bool proc_cap = false;

                {
                    if(cam.CaptureFrame())
                    {
                        CamCtx.BGRImg cam_img_bgr = cam.GetCaptureFrameAsBGRImg();

                        cap_img_bgr_width = cam_img_bgr.width;
                        cap_img_bgr_height = cam_img_bgr.height;
                        cap_img_bgr_pixels = cam_img_bgr.pixels_bgr;

                        proc_cap = true;                    
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }


                if (proc_cap)
                {
                    // [false] detect face in single image
                    // [true] detect face in continuous image (ex: camera image)
                    bool IS_CONTINOUS_IMG_FROM_CAMERA = false;
                    bool use_continuous_img_face_detect = IS_CONTINOUS_IMG_FROM_CAMERA;

                    // 0] check minimum image size
                    bool check_minimum_img_siz = prop_settings.CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ;
                    int cam_img_siz_minimum_threshold_width_min = prop_settings.CAP_SRC_MIN_WIDTH;
                    int cam_img_siz_minimum_threshold_height_min = prop_settings.CAP_SRC_MIN_HEIGHT;

                    // 1] crop image for better image liveness
                    bool crop_cap_img_margin_by_aspect = prop_settings.CAP_SRC_ENABLE_CROP_MARGIN_BY_ASPECT;

                    float crop_img_aspect_h_w
                        = prop_settings.CAP_SRC_CROP_MARGIN_ASPECT_HEIGHT / prop_settings.CAP_SRC_CROP_MARGIN_ASPECT_WIDTH;

                    // 2] check face width/height ratio for liveness
                    bool check_valid_face_width_ratio_for_liveness = prop_demo.LIV_CHK_FACE_WIDTH_RATIO;

                    float threshold_liveness_face_ratio_width_min;
                    float threshold_liveness_face_ratio_width_max;

                    if (crop_cap_img_margin_by_aspect)
                    {
                        threshold_liveness_face_ratio_width_min = prop_demo.LIV_THRESHOLD_MIN_RATIO_WIDTH_FOR_CROP_IMG;
                        threshold_liveness_face_ratio_width_max = prop_demo.LIV_THRESHOLD_MAX_RATIO_WIDTH_FOR_CROP_IMG;
                    }
                    else
                    {
                        threshold_liveness_face_ratio_width_min = prop_demo.LIV_THRESHOLD_MIN_RATIO_WIDTH_FOR_NORMAL_IMG;
                        threshold_liveness_face_ratio_width_max = prop_demo.LIV_THRESHOLD_MAX_RATIO_WIDTH_FOR_NORMAL_IMG;
                    }


                    //
                    // check captured source size
                    //

                    String check_minimum_img_siz_err_msg = "";

                    if (check_minimum_img_siz)
                    {
                        if (cap_img_bgr_width < cam_img_siz_minimum_threshold_width_min)
                        {
                            check_minimum_img_siz_err_msg = "[ERR] cam img WIDTH is TOO SMALL !" +
                                "> w=" + cap_img_bgr_width + " < [thres] w=" + cam_img_siz_minimum_threshold_width_min;
                        }

                        if (cap_img_bgr_height < cam_img_siz_minimum_threshold_height_min)
                        {
                            check_minimum_img_siz_err_msg = "[ERR] cam img HEIGHT is TOO SMALL !" +
                                "> h=" + cap_img_bgr_height + " < [thres] h=" + cam_img_siz_minimum_threshold_height_min;
                        }
                    }


                    //
                    // rotate captured source
                    //

                    if (prop_settings.CAP_SRC_ROTATE != "0")
                    {
                        CamCtx.FixedRotate fixed_rot
                            = CamCtx.ConvFixedRotate(prop_settings.CAP_SRC_ROTATE);

                        CamCtx.BGRImg fixed_rotated_src
                            = CamCtx.FixedRotateBGRImage(fixed_rot,
                                cap_img_bgr_pixels, cap_img_bgr_width, cap_img_bgr_height);

                        if (fixed_rotated_src == null)
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        cap_img_bgr_pixels = fixed_rotated_src.pixels_bgr;
                        cap_img_bgr_width = fixed_rotated_src.width;
                        cap_img_bgr_height = fixed_rotated_src.height;
                    }


                    //
                    // crop left/right side of source image with aspect
                    //

                    if (crop_cap_img_margin_by_aspect)
                    {
                        BlobImg crop_img_rst = ImgUtil.CropImgByAspectRatio(cap_img_bgr_pixels,
                                cap_img_bgr_width, cap_img_bgr_height, crop_img_aspect_h_w);

                        cap_img_bgr_pixels = crop_img_rst.data_;
                        cap_img_bgr_width = crop_img_rst.width_;
                        cap_img_bgr_height = crop_img_rst.height_;
                    }

                    //
                    // exchange left right for (left,right)
                    //


                    //
                    // UI
                    //


                    Mat ui_render_frame = null;

                    ui_render_frame = CamCtx.MakeMatFromBGR(
                        cap_img_bgr_pixels, cap_img_bgr_width, cap_img_bgr_height);

                    // for debug
                    RGBHistogramData rgb_hgrm = new RGBHistogramData();
                    rgb_hgrm = CamCtx.MakeHistogramMat(ui_render_frame, 380, 300);



                    //
                    // common ui
                    //

                    if (prop_settings.UI_ETC_TOP_MSG_SHOW)
                    {
                        pt_liv_ui += new Point(0, 40);

                        //String ui_top_msg1 = "[VER] FaceSDK C#=" + FaceSDK.VER_STR;
                        //CamCtx.DrawText(ui_render_frame, ui_top_msg1,
                        //     pt_liv_ui, new Scalar(255, 0, 0), liv_ui_fnt_scl * 0.8);

                        //CamCtx.DrawText(ui_render_frame, "FSDKC=" + FaceSDK.GetFSDKCVer(),
                        //     pt_liv_ui += new Point(0, 15), new Scalar(255, 0, 0), liv_ui_fnt_scl * 0.8);

                        CamCtx.DrawText(ui_render_frame, FaceSDK.GetFSDKCVer(),
                               pt_liv_ui, new Scalar(255, 0, 0), liv_ui_fnt_scl * 0.8);

                        String ui_top_msg2 = "";
                        String ui_top_msg3 = "";

                        if (ui_render_frame != null)
                        {
                            ui_top_msg2 = "[Cap] ROT=" + prop_settings.CAP_SRC_ROTATE
                                + ", W=" + ui_render_frame.Width + ", H=" + ui_render_frame.Height;

                            if (crop_cap_img_margin_by_aspect)
                            {
                                ui_top_msg3 += "> Cropped Left/Right, Ratio(H/W): " + crop_img_aspect_h_w;
                            }
                            else
                            {
                                ui_top_msg3 = "> Use Original (NO CROP L/R)";
                            }
                        }
                        else
                        {
                            ui_top_msg2 = "[ERR] no cam input frame!";
                        }


                        CamCtx.DrawText(ui_render_frame, ui_top_msg2,
                             pt_liv_ui += new Point(0, 20), new Scalar(255, 0, 128), liv_ui_fnt_scl * 0.8);
                        CamCtx.DrawText(ui_render_frame, ui_top_msg3,
                             pt_liv_ui += new Point(0, 20), new Scalar(255, 0, 255), liv_ui_fnt_scl * 0.8);

                        pt_liv_ui += new Point(0, 20);
                    }
                    else
                    {
                        pt_liv_ui += new Point(0, 40);
                    }

                    switch (stage_)
                    {
                        case DemoStage.DoPassiveLivenessAndCollectLivenessBestShot:
                            {
                                int txt_row_pad = 20;

                                bool check_center_face_position_in_img = prop_demo.LIV_CHK_CENTER_FACE_W_H_POS_IN_IMG;
                                float threshold_center_face_w_h_pos_ratio = prop_demo.LIV_THRESHOLD_RATIO_CENTER_FACE_W_H_POS_IN_IMG;

                                bool check_face_yaw_pitch_roll = prop_demo.LIV_CHK_FACE_YAW_PITCH_ROLL;
                                float[] threshold_face_yaw = prop_demo.LIV_THRESHOLD_FACE_YAW_RANGE;
                                float[] threshold_face_pitch = prop_demo.LIV_THRESHOLD_FACE_PITCH_RANGE;
                                float[] threshold_face_roll = prop_demo.LIV_THRESHOLD_FACE_ROLL_RANGE;

                                bool check_face_is_masked = prop_demo.LIV_CHK_FACE_IS_MASKED;

                                // check face feature quality (for extract face feature)
                                bool check_feature_face_quality = prop_demo.LIV_CHK_FEATURE_QUALITY;
                                float[] threshold_feature_quality
                                    = new float[] { prop_demo.LIV_THRESHOLD_FEATURE_QUALITY_UNMASKED_MIN,
                                                prop_demo.LIV_THRESHOLD_FEATURE_QUALITY_MASKED_MIN };

                                // check face antispoofing quality (FAS)
                                bool check_fas_quality = prop_demo.LIV_CHK_FAS_QUALITY;
                                float threshold_fas_quality = prop_demo.LIV_THRESHOLD_FAS_QUALITY_MIN;

                                // BGR Image liveness
                                float threshold_bgr_img_liveness = prop_demo.LIV_THRESHOLD_BGR_IMAGE_LIVENESS_MIN;

                                // true=allow only one face during liveness operation 
                                bool check_multiple_faces = prop_demo.LIV_CHK_MULTIPLE_FACES;

                                string ui_liv_rst_prefix = "Liveness(";
                                ui_liv_rst_prefix += LIV_BGR_IMG_CONTINOUS_CHECK ? "cont-4-bgr" : "single-bgr";
                                ui_liv_rst_prefix += ")";

                                if (check_minimum_img_siz_err_msg == "")
                                {
                                    //
                                    // Do Passive Liveness
                                    //
                                    if (LIV_BGR_IMG_CONTINOUS_CHECK == false) {
                                        // > clear previous bgr img check liveness result cache
                                        ClearBGRLivenessPrevCheckCache(fsdk);
                                    }

                                    PassiveLivness.Result liv_rst = PassiveLivness.DoPassiveLiveness(
                                        fsdk,
                                        cap_img_bgr_pixels, cap_img_bgr_width, cap_img_bgr_height,
                                        threshold_liveness_face_ratio_width_min,
                                        threshold_liveness_face_ratio_width_max,
                                        use_continuous_img_face_detect,
                                        check_valid_face_width_ratio_for_liveness,
                                        check_center_face_position_in_img,
                                        threshold_center_face_w_h_pos_ratio,
                                        check_face_yaw_pitch_roll,
                                        threshold_face_yaw,
                                        threshold_face_pitch,
                                        threshold_face_roll,
                                        check_face_is_masked,
                                        check_feature_face_quality,
                                        threshold_feature_quality,
                                        check_fas_quality,
                                        threshold_fas_quality,
                                        threshold_bgr_img_liveness,
                                        check_multiple_faces
                                        );

                                    if (liv_rst.detected_face_cnt_ != 0)
                                    {
                                        FaceSDK.Box face_box = liv_rst.detected_face_.box;
                                        FaceSDK.Pose pose = liv_rst.detected_face_.pose;

                                        CamCtx.DrawBoxXYWH(ui_render_frame, new Point(face_box.x, face_box.y),
                                            new Point(face_box.w, face_box.h), new Scalar(0, 255, 0));


                                        if (prop_faceinfo.FACE_INFO_ENABLE)
                                        {
                                            Point pt_face_box_ui = new Point(face_box.x - 110, face_box.y + 30);
                                            double pt_face_box_ui_base_ft_scl = 0.4;

                                            if (prop_faceinfo.FACE_INFO_POS)
                                            {
                                                CamCtx.DrawText(ui_render_frame, $"x: {face_box.x:F1},y: {face_box.y:F1}", pt_face_box_ui += new Point(0, 15), new Scalar(0, 128, 255), pt_face_box_ui_base_ft_scl * 0.9f);
                                                CamCtx.DrawText(ui_render_frame, $"w: {face_box.w:F1}", pt_face_box_ui += new Point(0, 15), new Scalar(255, 0, 255), pt_face_box_ui_base_ft_scl * 0.9f);
                                                CamCtx.DrawText(ui_render_frame, $"h: {face_box.h:F1}", pt_face_box_ui += new Point(0, 15), new Scalar(255, 0, 255), pt_face_box_ui_base_ft_scl * 0.9f);

                                                pt_face_box_ui += new Point(0, 10);
                                            }

                                            if (prop_faceinfo.FACE_INFO_YAW_PITCH_ROLL)
                                            {
                                                CamCtx.DrawText(ui_render_frame, $"yaw: {pose.yaw:F1}", pt_face_box_ui += new Point(0, 15), new Scalar(0, 255, 255), pt_face_box_ui_base_ft_scl * 0.9f);
                                                CamCtx.DrawText(ui_render_frame, $"pit: {pose.pitch:F1}", pt_face_box_ui += new Point(0, 15), new Scalar(0, 255, 255), pt_face_box_ui_base_ft_scl * 0.9f);
                                                CamCtx.DrawText(ui_render_frame, $"roll: {pose.roll:F1}", pt_face_box_ui += new Point(0, 15), new Scalar(0, 255, 255), pt_face_box_ui_base_ft_scl * 0.9f);

                                                CamCtx.DrawText(ui_render_frame, "mask: " + (liv_rst.is_face_masked_ ? "masked" : "no"),
                                                    pt_face_box_ui += new Point(0, 15), new Scalar(128, 255, 255), pt_face_box_ui_base_ft_scl * 0.9f);
                                            }

                                            //
                                            // Compute Landmark Confidence
                                            //

                                            if (prop_faceinfo.FACE_INFO_LANDMARK)
                                            {
                                                FaceSDK.Rst face_landmark_confidence
                                                    = fsdk.ComputeLandmarkConfidence(cap_img_bgr_pixels, cap_img_bgr_width,
                                                        cap_img_bgr_height, ref liv_rst.detected_face_);

                                                string face_landmark_confidence_msg;
                                                Scalar face_landmark_confidence_color = new Scalar(128, 255, 128);

                                                if (face_landmark_confidence.IsOk())
                                                {
                                                    face_landmark_confidence_msg
                                                        = $"[LAND] {face_landmark_confidence.face_landmark_confidence:F2} ";

                                                    if (face_landmark_confidence.face_landmark_confidence < FACE_DETECT_LANDMARK_CONFIDENCE_THRESHOLD)
                                                    {
                                                        face_landmark_confidence_color = new Scalar(0, 0, 255);
                                                    }
                                                }
                                                else
                                                {
                                                    face_landmark_confidence_color = new Scalar(0, 0, 255);
                                                    face_landmark_confidence_msg = "[LAND] ERR!," + face_landmark_confidence.GetLastErrDesc();
                                                }

                                                CamCtx.DrawText(ui_render_frame, face_landmark_confidence_msg,
                                                   pt_face_box_ui += new Point(0, 15), face_landmark_confidence_color, pt_face_box_ui_base_ft_scl * 0.9f);
                                            }

                                            //
                                            // BGR Liveness
                                            //

                                            if (prop_faceinfo.FACE_INFO_BGR_LIVENESS)
                                            {
                                                FaceSDK.Rst face_bgr_liv_confidence = fsdk.GetImgLiveness(cap_img_bgr_pixels, cap_img_bgr_width,
                                                    cap_img_bgr_height, ref liv_rst.detected_face_);

                                                // no need to call fsdk.ResetImgLivenessCheck() until face is changed to new

                                                string face_bgr_liv_conf_msg;
                                                Scalar face_bgr_liv_conf_color = new Scalar(128, 255, 128);
                                                // PropertyGrid 에서 조정한 값을 따른다. (상수 직접 참조 시 UI 변경이 반영되지 않음)
                                                float face_bgr_liv_threshold = prop_demo.LIV_THRESHOLD_BGR_IMAGE_LIVENESS_MIN;

                                                if (face_bgr_liv_confidence.IsOk())
                                                {
                                                    face_bgr_liv_conf_msg
                                                        = $"[BGR(<{face_bgr_liv_threshold:F2})] {face_bgr_liv_confidence.img_liveness:F2} ";

                                                    if (face_bgr_liv_confidence.img_liveness < face_bgr_liv_threshold)
                                                    {
                                                        face_bgr_liv_conf_color = new Scalar(0, 0, 255);
                                                    }
                                                }
                                                else
                                                {
                                                    face_bgr_liv_conf_color = new Scalar(0, 0, 255);
                                                    face_bgr_liv_conf_msg = "[BGR] ERR!," + face_bgr_liv_confidence.GetLastErrDesc();
                                                }

                                                CamCtx.DrawText(ui_render_frame, face_bgr_liv_conf_msg,
                                                   pt_face_box_ui += new Point(0, 15), face_bgr_liv_conf_color, pt_face_box_ui_base_ft_scl * 0.9f);

                                            }


                                            //
                                            // Determine if the eyes and nose of the face are covered.
                                            //

                                            if (prop_faceinfo.FACE_INFO_FACE_OCCLUSION)
                                            {
                                                FaceSDK.Rst face_eye_mouth_coverd_state
                                                = fsdk.DetectFaceOcclusion(cap_img_bgr_pixels, cap_img_bgr_width,
                                                    cap_img_bgr_height, ref liv_rst.detected_face_);

                                                string face_eye_mouth_coverd_state_msg;
                                                Scalar face_eye_mouth_coverd_state_color = new Scalar(128, 255, 128);

                                                if (face_eye_mouth_coverd_state.IsOk())
                                                {
                                                    if (face_eye_mouth_coverd_state.face_occlu_conf_left_eye > FACE_EYE_MOUTH_OCCLUSION_THRESHOLD)
                                                        face_eye_mouth_coverd_state_color = new Scalar(0, 0, 255);
                                                    else
                                                        face_eye_mouth_coverd_state_color = new Scalar(128, 255, 128);

                                                    CamCtx.DrawText(ui_render_frame,
                                                        face_eye_mouth_coverd_state_msg = $"[OCC-EL] {face_eye_mouth_coverd_state.face_occlu_conf_left_eye:F2}",
                                                        pt_face_box_ui += new Point(0, 15), face_eye_mouth_coverd_state_color, pt_face_box_ui_base_ft_scl * 0.9f);


                                                    if (face_eye_mouth_coverd_state.face_occlu_conf_right_eye > FACE_EYE_MOUTH_OCCLUSION_THRESHOLD)
                                                        face_eye_mouth_coverd_state_color = new Scalar(0, 0, 255);
                                                    else
                                                        face_eye_mouth_coverd_state_color = new Scalar(128, 255, 128);

                                                    CamCtx.DrawText(ui_render_frame,
                                                        face_eye_mouth_coverd_state_msg = $"[OCC-ER] {face_eye_mouth_coverd_state.face_occlu_conf_right_eye:F2}",
                                                        pt_face_box_ui += new Point(0, 15), face_eye_mouth_coverd_state_color, pt_face_box_ui_base_ft_scl * 0.9f);


                                                    if (face_eye_mouth_coverd_state.face_occlu_conf_mouth > FACE_EYE_MOUTH_OCCLUSION_THRESHOLD)
                                                        face_eye_mouth_coverd_state_color = new Scalar(0, 0, 255);
                                                    else
                                                        face_eye_mouth_coverd_state_color = new Scalar(128, 255, 128);

                                                    CamCtx.DrawText(ui_render_frame,
                                                        face_eye_mouth_coverd_state_msg = $"[OCC-MO] {face_eye_mouth_coverd_state.face_occlu_conf_mouth:F2}",
                                                        pt_face_box_ui += new Point(0, 15), face_eye_mouth_coverd_state_color, pt_face_box_ui_base_ft_scl * 0.9f);

                                                }
                                                else
                                                {
                                                    face_eye_mouth_coverd_state_color = new Scalar(0, 0, 255);
                                                    face_eye_mouth_coverd_state_msg = "[OCC] ERR!," + face_eye_mouth_coverd_state.GetLastErrDesc();

                                                    CamCtx.DrawText(ui_render_frame, face_eye_mouth_coverd_state_msg,
                                                       pt_face_box_ui += new Point(0, 15), face_eye_mouth_coverd_state_color, pt_face_box_ui_base_ft_scl * 0.9f);
                                                }
                                            }

                                            //
                                            // Fine Occlusion
                                            //
                                            if (prop_faceinfo.FACE_INFO_FINE_OCCLUSION)
                                            {
                                                //FaceSDK.Rst face_fine_occlusion_score = fsdk.DetectFineOcclusion(
                                                //    cap_img_bgr_pixels, cap_img_bgr_width,
                                                //    cap_img_bgr_height, ref liv_rst.detected_face_);

                                                //string face_fine_occ_score_msg;
                                                //Scalar face_fine_occ_score_color = new Scalar(128, 255, 128);

                                                //if (face_fine_occlusion_score.IsOk())
                                                //{
                                                //    if (LivenessThreshold.IsFaceFineOcclusion(face_fine_occlusion_score.face_fine_occlu_score))
                                                //    {
                                                //        // occlusioned
                                                //        face_fine_occ_score_color = new Scalar(0, 0, 255);
                                                //    }
                                                //    else
                                                //    {
                                                //        face_fine_occ_score_color = new Scalar(128, 255, 128);
                                                //    }
                                                //}
                                                //else
                                                //{
                                                //    face_fine_occ_score_color = new Scalar(0, 0, 255);
                                                //    face_fine_occ_score_msg = "[FINE-OCC] ERR!," + face_fine_occlusion_score.GetLastErrDesc();
                                                //}

                                                //CamCtx.DrawText(ui_render_frame,
                                                //        face_fine_occ_score_msg = $"[FINE-OCC] {face_fine_occlusion_score.face_fine_occlu_score:F2}",
                                                //        pt_face_box_ui += new Point(0, 15), face_fine_occ_score_color, pt_face_box_ui_base_ft_scl * 0.9f);

                                            }

                                            //
                                            // Extract the closed eyes from the faces.
                                            //

                                            if (prop_faceinfo.FACE_INFO_CLOSED_EYE_STATE)
                                            {
                                                string face_closed_eye_state_msg_left;
                                                string face_closed_eye_state_msg_right;

                                                Scalar face_closed_eye_state_color_left = new Scalar(64, 256, 128);
                                                Scalar face_closed_eye_state_color_right = new Scalar(64, 256, 128);

                                                FaceSDK.Rst face_closed_eye_state
                                                    = fsdk.DetectClosedEyes(cap_img_bgr_pixels, cap_img_bgr_width,
                                                          cap_img_bgr_height, ref liv_rst.detected_face_);

                                                if (face_closed_eye_state.IsOk())
                                                {
                                                    face_closed_eye_state_msg_left = $"[CEyeState] L: ";

                                                    if (face_closed_eye_state.img_face_eye_state_left_eyelid_dist <= FACE_EYE_CLOSED_STATE_THRESHOLD)
                                                    {
                                                        face_closed_eye_state_color_left = new Scalar(0, 0, 255);
                                                        face_closed_eye_state_msg_left += "Closed";
                                                    }
                                                    else
                                                    {
                                                        face_closed_eye_state_color_left = new Scalar(64, 256, 128);
                                                        face_closed_eye_state_msg_left += "Normal";
                                                    }

                                                    face_closed_eye_state_msg_left += $" , dist={face_closed_eye_state.img_face_eye_state_left_eyelid_dist:F2}";


                                                    face_closed_eye_state_msg_right = $"[CEyeState] R: ";

                                                    if (face_closed_eye_state.img_face_eye_state_right_eyelid_dist <= FACE_EYE_CLOSED_STATE_THRESHOLD)
                                                    {
                                                        face_closed_eye_state_color_right = new Scalar(0, 0, 255);
                                                        face_closed_eye_state_msg_right += "Closed";
                                                    }
                                                    else
                                                    {
                                                        face_closed_eye_state_color_right = new Scalar(64, 256, 128);
                                                        face_closed_eye_state_msg_right += "Normal";
                                                    }

                                                    face_closed_eye_state_msg_right += $" , dist={face_closed_eye_state.img_face_eye_state_right_eyelid_dist:F2}";
                                                }
                                                else
                                                {
                                                    face_closed_eye_state_color_left = new Scalar(0, 0, 255);
                                                    face_closed_eye_state_color_right = new Scalar(0, 0, 255);

                                                    face_closed_eye_state_msg_left = "[CEyeState] ERR! , desc=" + face_closed_eye_state.GetLastErrDesc();
                                                    face_closed_eye_state_msg_right = "[CEyeState] ERR! , desc=" + face_closed_eye_state.GetLastErrDesc();
                                                }

                                                CamCtx.DrawText(ui_render_frame, face_closed_eye_state_msg_left,
                                                   pt_face_box_ui += new Point(0, 15), face_closed_eye_state_color_left, pt_face_box_ui_base_ft_scl * 0.9f);

                                                CamCtx.DrawText(ui_render_frame, face_closed_eye_state_msg_right,
                                                   pt_face_box_ui += new Point(0, 15), face_closed_eye_state_color_right, pt_face_box_ui_base_ft_scl * 0.9f);
                                            }

                                            //
                                            // face count
                                            //

                                            CamCtx.DrawText(ui_render_frame, "detected face cnt=" + (liv_rst.detected_face_cnt_),
                                                pt_face_box_ui += new Point(0, 15), new Scalar(128, 255, 255), pt_face_box_ui_base_ft_scl);

                                            if (prop_faceinfo.FACE_INFO_DRAW_LANDMARK_PT)
                                            {
                                                DrawFaceLandmark(ui_render_frame, liv_rst.detected_face_);
                                            }

                                            if (prop_faceinfo.FACE_INFO_CROPFACE_ENABLE)
                                            {
                                                FaceSDK.FaceCropAreaRst rst_face_area_crop = FaceSDK.CalcFaceCropArea(
                                                    cap_img_bgr_pixels, cap_img_bgr_width, cap_img_bgr_height,
                                                    face_box,
                                                    prop_faceinfo.FACE_INFO_CROPFACE_FACE_WH_MARGIN_RATIO,
                                                    prop_faceinfo.FACE_INFO_CROPFACE_RESIZE_WIDTH_AFTER_CROP);

                                                string crop_msg = "";

                                                if (rst_face_area_crop.is_valid)
                                                {
                                                    CamCtx.DrawBoxXYWH(ui_render_frame, new Point(rst_face_area_crop.x, rst_face_area_crop.y),
                                                        new Point(rst_face_area_crop.w, rst_face_area_crop.h), new Scalar(128, 0, 0), 1);

                                                    crop_msg = string.Format("CropFace: x={0}, y={1}, w={2}, h={3}",
                                                        rst_face_area_crop.x, rst_face_area_crop.y, rst_face_area_crop.w, rst_face_area_crop.h);
                                                    CamCtx.DrawText(ui_render_frame, crop_msg,
                                                        pt_face_box_ui += new Point(0, 25), new Scalar(255, 0, 0), pt_face_box_ui_base_ft_scl);

                                                    crop_msg = string.Format("> face-wh-margin-ratio: {0}", rst_face_area_crop.margin_ratio);
                                                    CamCtx.DrawText(ui_render_frame, crop_msg,
                                                        pt_face_box_ui += new Point(0, 15), new Scalar(255, 0, 0), pt_face_box_ui_base_ft_scl);

                                                    crop_msg = string.Format("> resize width after crop: w={0}, h={1}",
                                                        rst_face_area_crop.resize_w, rst_face_area_crop.resize_h);
                                                    CamCtx.DrawText(ui_render_frame, crop_msg,
                                                        pt_face_box_ui += new Point(0, 15), new Scalar(255, 0, 0), pt_face_box_ui_base_ft_scl);

                                                    if (prop_faceinfo.FACE_INFO_CROPFACE_DRAW_RESIZED)
                                                    {
                                                        BlobImg cropped_and_resized_face_img = FaceSDK.CropFaceFromImg(cap_img_bgr_pixels,
                                                            cap_img_bgr_width, cap_img_bgr_height, rst_face_area_crop);

                                                        if (cropped_and_resized_face_img != null)
                                                        {
                                                            CamCtx.DrawImg(ui_render_frame,
                                                                new Point(10, 10),
                                                                cropped_and_resized_face_img.data_,
                                                                cropped_and_resized_face_img.width_,
                                                                cropped_and_resized_face_img.height_);

                                                            //Alchera.FaceSDK.ImgUtil.SaveBGRAsBmp("cropface-" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".bmp",
                                                            //    cropped_and_resized_face_img.data_,
                                                            //    cropped_and_resized_face_img.width_,
                                                            //    cropped_and_resized_face_img.height_);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    crop_msg = "CropFace: Calc CropFace Area Is FAILED!!";

                                                    CamCtx.DrawText(ui_render_frame, crop_msg,
                                                        pt_face_box_ui += new Point(0, 20), new Scalar(0, 0, 255), pt_face_box_ui_base_ft_scl);
                                                }
                                            }

                                        }
                                    }
                                    else
                                    {
                                        ; // no face is detected
                                    }
                                                                       

                                    if (liv_rst.is_liveness_ok_ || prop_demo.LIV_BGR_IMG_LIV_API_TYPE == "None")
                                    {
                                        if (prop_demo.LIV_BGR_IMG_LIV_API_TYPE == "None")
                                        {
                                            CamCtx.DrawText(ui_render_frame, $"{ui_liv_rst_prefix}: Skipped By API=None", pt_liv_ui, new Scalar(0, 0, 255), liv_ui_fnt_scl);

                                        }
                                        else
                                        {
                                            CamCtx.DrawText(ui_render_frame, $"{ui_liv_rst_prefix}: OK", pt_liv_ui, new Scalar(0, 255, 0), liv_ui_fnt_scl);
                                            CamCtx.DrawText(ui_render_frame, "> liv confidence=" + liv_rst.liveness_confidence_,
                                                    pt_liv_ui += new Point(0, txt_row_pad + 10), new Scalar(255, 0, 0), liv_ui_fnt_scl);
                                        }

                                        //
                                        // collect liveness bestshot
                                        //

                                        {
                                            if (!ctx.best_liv_shots.IsFull())
                                            {
                                                ctx.best_liv_shots.Push(cap_img_bgr_pixels, cap_img_bgr_width, cap_img_bgr_height,
                                                liv_rst.liveness_confidence_, liv_rst.detected_face_);
                                            }

                                            string bestshot_state = $"{ui_liv_rst_prefix}: BestShot: ";
                                            bestshot_state += ctx.best_liv_shots.shot_cnt + " Collected";

                                            CamCtx.DrawText(ui_render_frame, bestshot_state,
                                                pt_liv_ui += new Point(0, txt_row_pad), new Scalar(128, 255, 128), liv_ui_fnt_scl);

                                            if (ctx.best_liv_shots.IsFull())
                                            {
                                                SetDemoStage(DemoStage.BGRImageLivenessMulitframe);
                                            }
                                        }

                                    }
                                    else
                                    {
                                        ctx.best_liv_shots.Clear();

                                        string liv_fail_msg = $"{ui_liv_rst_prefix}: Fail";
                                        liv_fail_msg += " , err=" + liv_rst.liveness_err_.ToString();

                                        CamCtx.DrawText(ui_render_frame, liv_fail_msg, pt_liv_ui, new Scalar(0, 0, 255), liv_ui_fnt_scl);

                                        CamCtx.DrawText(ui_render_frame, liv_rst.last_err_msg1_, pt_liv_ui += new Point(0, txt_row_pad + 10), new Scalar(0, 0, 255), liv_ui_fnt_scl);
                                        CamCtx.DrawText(ui_render_frame, liv_rst.last_err_msg2_, pt_liv_ui += new Point(0, txt_row_pad), new Scalar(0, 0, 255), liv_ui_fnt_scl);
                                        CamCtx.DrawText(ui_render_frame, liv_rst.last_err_msg3_, pt_liv_ui += new Point(0, txt_row_pad), new Scalar(0, 0, 255), liv_ui_fnt_scl);
                                    }

                                }
                                else
                                {
                                    CamCtx.DrawText(ui_render_frame, $"{ui_liv_rst_prefix} Rst: Camera Minimum Src Size is Invalidl!", pt_liv_ui, new Scalar(0, 0, 255), liv_ui_fnt_scl);
                                    CamCtx.DrawText(ui_render_frame, " > " + check_minimum_img_siz_err_msg, pt_liv_ui += new Point(0, txt_row_pad), new Scalar(255, 0, 128), liv_ui_fnt_scl);
                                }

                            }
                            break;


                        case DemoStage.BGRImageLivenessMulitframe:
                            {
                                if (prop_demo.LIV_BGR_IMG_LIV_API_TYPE == "None")
                                {
                                    SetDemoStage(DemoStage.FaceCompare);
                                    break;
                                }

                                string bestshot_state = "Liveness BestShot : Ready !,  count=" + ctx.best_liv_shots.shot_cnt;

                                CamCtx.DrawText(ui_render_frame, bestshot_state,
                                    pt_liv_ui, new Scalar(0, 255, 0), liv_ui_fnt_scl + 0.1);

                                //Point pt_base = pt_liv_ui + new Point(0, 10);
                                pt_liv_ui = DrawBestLiveShots(ctx.best_liv_shots, ui_render_frame,
                                    pt_liv_ui + new Point(0, 10), 130, 130);


                                if (prop_demo.LIV_BGR_IMG_LIV_API_TYPE == "ClientOnly")
                                {
                                    if (ctx.ui_finish_liveness_multiframe_task == null)
                                    {
                                        ctx.ui_liv_multi_st.Clear();
                                        ctx.ui_liv_msg("[I][ClientOnly] calling GetImgLivenessMulti()..");

                                        // use last shot face
                                        int last_shot_idx = ctx.best_liv_shots.shot_cnt - 1;
                                        if(last_shot_idx < 0)
                                        {
                                            throw new InvalidOperationException("invalid state, last_shot_idx can't be under 0");
                                        }

                                        Face face = ctx.best_liv_shots.GetShot(last_shot_idx).detected_face;

                                        Stopwatch cli_liv_multi_sw = new Stopwatch();
                                        cli_liv_multi_sw.Start();

                                        int[] shot_idx = ctx.best_liv_shots.get_recent_shot_indices(4);

                                        FaceSDK.Rst cli_liv_multi_rst
                                            = fsdk.GetImgLivenessMulti(
                                                ctx.best_liv_shots[shot_idx[0]].data_, ctx.best_liv_shots[shot_idx[0]].width_, ctx.best_liv_shots[shot_idx[0]].height_,
                                                ctx.best_liv_shots[shot_idx[1]].data_, ctx.best_liv_shots[shot_idx[1]].width_, ctx.best_liv_shots[shot_idx[1]].height_,
                                                ctx.best_liv_shots[shot_idx[2]].data_, ctx.best_liv_shots[shot_idx[2]].width_, ctx.best_liv_shots[shot_idx[2]].height_,
                                                ctx.best_liv_shots[shot_idx[3]].data_, ctx.best_liv_shots[shot_idx[3]].width_, ctx.best_liv_shots[shot_idx[3]].height_,
                                                ref face);

                                        cli_liv_multi_sw.Stop();

                                        string client_only_liv_rst_msg;

                                        ctx.ui_liv_msg("[I] GetImgLivenessMulti() call, elapsed= "
                                            + cli_liv_multi_sw.ElapsedMilliseconds + " MS");

                                        ctx.ui_liv_msg("[I] ");

                                        if (cli_liv_multi_rst.IsOk())
                                        {
                                            float img_liv_confidence = cli_liv_multi_rst.img_liveness;
                                            client_only_liv_rst_msg = "confidence=" + img_liv_confidence;

                                            ctx.ui_liv_msg("[OK] GetImgLivenessMulti() is Success");
                                            ctx.ui_liv_msg("[OK] Result)");

                                            float img_liv_threshold = prop_demo.LIV_THRESHOLD_BGR_IMAGE_LIVENESS_MIN;

                                            if (img_liv_confidence > img_liv_threshold)
                                            {
                                                ctx.ui_liv_msg($"[OK] bgr image liveness quality is GOOD!");
                                                ctx.ui_liv_msg($"[OK] > confidence({img_liv_confidence}) > thres({img_liv_threshold})");
                                            }
                                            else
                                            {
                                                ctx.ui_liv_msg($"[FAIL] bgr image liveness is NOT GOOD!");
                                                ctx.ui_liv_msg($"[FAIL] > confidence({img_liv_confidence}) <= thres({img_liv_threshold})");
                                            }
                                        }
                                        else
                                        {
                                            client_only_liv_rst_msg = "last_err=" + cli_liv_multi_rst.last_err_desc;

                                            ctx.ui_liv_msg("[FAIL] GetImgLivenessMulti() Failed! ");
                                            ctx.ui_liv_msg("[FAIL] > err=" + cli_liv_multi_rst.last_err_desc);
                                        }


                                        ctx.ui_finish_liveness_multiframe_task =
                                            ((Func<int, string, Action<string>, Action<string, object>, Task>)
                                            (async (_delay_time_ms, _cli_liv_rst, _ui_msg, _finish) =>
                                            {
                                                while (_delay_time_ms > 0)
                                                {
                                                    await Task.Delay(1000);
                                                    _ui_msg("[ALARM] Finishing ClientOnly BGR Liveness Mulitframe.." + (int)(_delay_time_ms / 1000));
                                                    _delay_time_ms -= 1000;
                                                }

                                                _finish("clientonly_liv_muliti", _cli_liv_rst);

                                            }))(ui_finish_liveness_multiframe_task_dur_sec * 1000,
                                                    client_only_liv_rst_msg,
                                                    ctx.ui_liv_msg,
                                                    ctx.ui_fin_liveness_check_act);
                                    }

                                }
                                else if (prop_demo.LIV_BGR_IMG_LIV_API_TYPE == "FaceServer")
                                {
                                    //
                                    // request server api - liveness check with 4 bestshot   
                                    //

                                    if (ctx.api_liveness_multiframe_task == null)
                                    {
                                        ctx.api_liveness_best4 = null;
                                        ctx.ui_liv_multi_st.Clear();

                                        ctx.ui_liv_msg("[I] Prepare Server LivenessMultiframe Rest API..");

                                        // build api and send request to server
                                        ctx.api_liveness_multiframe_task =
                                            ((Func<string, Action<string>, Task<Api>>)
                                            (async (_api_svr_url, _ui_msg) =>
                                            {
                                                // USE_HTTP_PROXY, HTTP_PROXY_URL can be omitted, use for debugging.
                                                RestConn _conn = new RestConn(_api_svr_url, USE_HTTP_PROXY, HTTP_PROXY_URL);
                                                LivenessMulitFrameApi _api = new LivenessMulitFrameApi();

                                                int[] shot_idx = ctx.best_liv_shots.get_recent_shot_indices(4);

                                                BlobImg[] _liv_img_frames = new BlobImg[] {
                                                    ctx.best_liv_shots[shot_idx[0]], ctx.best_liv_shots[shot_idx[1]],
                                                    ctx.best_liv_shots[shot_idx[2]], ctx.best_liv_shots[shot_idx[3]]};

                                                PayloadEncrypt img_encryptor = null;

                                                if (API_ENC_IMG_TYPE == "RSA+AES")
                                                {
                                                    img_encryptor = PayloadEncrypt.Create(
                                                        new PayloadEncrypt.EncRSAAES(API_ENC_PUB_KEY_B64, API_ENC_TIMESTAMP));
                                                }

                                                _ui_msg("[SV-REQ] Sending api request to server!");
                                                _ui_msg("[SV-REQ] > url=" + _conn.GetURL() + ", conn_err=" + _conn.GetLastErr());
                                                _ui_msg("[SV-REQ] > api=" + _api.path);
                                                _ui_msg("[SV-REQ] > api img encrypt mode=" + API_ENC_IMG_TYPE);

                                                if (API_ENC_IMG_TYPE != "NONE")
                                                {
                                                    if (img_encryptor.IsErr())
                                                    {
                                                        _ui_msg("[FAIL] > error on img encryptor, err="
                                                            + img_encryptor.GetLastErr() + ", desc=" + img_encryptor.GetLastErrDesc());
                                                    }
                                                }

                                                if (!_api.MakeReq(_liv_img_frames, img_encryptor))
                                                {
                                                    _ui_msg("[FAIL] > failed to make api request body=" + _api.GetLastErrDesc());
                                                    return _api;
                                                }

                                                if (API_ENC_IMG_TYPE != "NONE")
                                                {
                                                    if (!img_encryptor.IsErr())
                                                    {
                                                        _ui_msg("[SV-REQ] > api img encrypt enc time_ms=" + img_encryptor.GetLastEncElapsedMS());
                                                    }
                                                }

                                                await _conn.SendRequestAsync(_api);

                                                return _api;

                                            }))(api_svr_url, ctx.ui_liv_msg);
                                    }

                                    if (ctx.api_liveness_multiframe_task.IsCompleted)
                                    {
                                        if (ctx.api_liveness_best4 == null)
                                        {
                                            ctx.api_liveness_best4
                                                = ctx.api_liveness_multiframe_task.GetAwaiter().GetResult();

                                            // check api request server connection
                                            if (ctx.api_liveness_best4.IsErr())
                                            {
                                                ctx.ui_liv_msg("[SV-FAIL] Server API Request Connect Is Failed!");
                                                ctx.ui_liv_msg("[SV-FAIL] > err=" + ctx.api_liveness_best4.GetLastErr());
                                                ctx.ui_liv_msg("[SV-FAIL] > api_desc=" + ctx.api_liveness_best4.GetLastErrDesc());
                                            }
                                            else
                                            {
                                                ctx.ui_liv_msg("[SV-OK] API Request is OK, elapsed="
                                                    + ctx.api_liveness_best4.GetSvrRequestElapsedMS() + " MS");

                                                if (ctx.api_liveness_best4.IsHttpResponseOK())
                                                {
                                                    ctx.ui_liv_msg("[HTTP-OK] api=" + ctx.api_liveness_best4.path + " is OK!");

                                                    LivenessMulitFrameApi liv_api = ctx.api_liveness_best4 as LivenessMulitFrameApi;
                                                    bool is_liveness_pass = liv_api.IsLivenessPass();

                                                    string ui_liv_rst_prefix;

                                                    if (is_liveness_pass)
                                                        ui_liv_rst_prefix = "[OK][LIV-PASS] ";
                                                    else
                                                        ui_liv_rst_prefix = "[FAIL][LIV-FAIL] ";

                                                    LivenessMulitFrameApi.Res res
                                                        = ctx.api_liveness_best4.GetResponse() as LivenessMulitFrameApi.Res;

                                                    ctx.ui_liv_msg(ui_liv_rst_prefix + "> " + $"is_live = {res.is_live}");
                                                    ctx.ui_liv_msg(ui_liv_rst_prefix + "> " + $"confidence = {res.confidence}");
                                                    ctx.ui_liv_msg(ui_liv_rst_prefix + "> " + $"threshold = {res.threshold_info.auto_approve.min}");
                                                    ctx.ui_liv_msg(ui_liv_rst_prefix + "> " + $"ret-code = {res.return_msg.return_code}");
                                                    ctx.ui_liv_msg(ui_liv_rst_prefix + "> " + $"ret-msg = {res.return_msg.return_msg}");
                                                    ctx.ui_liv_msg(ui_liv_rst_prefix + "> " + $"evaluated(is_live, confidence)="
                                                        + Api.EvaluateLivenessConfidence(res.is_live, res.confidence));
                                                }
                                                else
                                                {
                                                    ctx.ui_liv_msg("[HTTP-FAIL] api=" + ctx.api_liveness_best4.path + " is Failed!");
                                                    ctx.ui_liv_msg("[HTTP-FAIL] > status=" + ctx.api_liveness_best4.GetHttpResponseCode());
                                                    ctx.ui_liv_msg("[HTTP-FAIL] > payload=" + ctx.api_liveness_best4.GetResponse());
                                                }
                                            }

                                            ctx.ui_finish_liveness_multiframe_task =
                                                ((Func<int, Api, Action<string>, Action<string, object>, Task>)
                                                (async (_delay_time_ms, _api_rst, _ui_msg, _finish) =>
                                                {
                                                    while (_delay_time_ms > 0)
                                                    {
                                                        await Task.Delay(1000);
                                                        _ui_msg("[ALARM] Finishing Liveness Check.. " + (int)(_delay_time_ms / 1000));
                                                        _delay_time_ms -= 1000;
                                                    }

                                                    _finish("api_liv_muliti", _api_rst);


                                                }))(ui_finish_liveness_multiframe_task_dur_sec * 1000,
                                                        ctx.api_liveness_best4, ctx.ui_liv_msg,
                                                        ctx.ui_fin_liveness_check_act);
                                        }

                                    }

                                } // end-of-'FaceServer'

                                CamCtx.DrawLogBox(ui_render_frame, pt_liv_ui, new Point(380, 300),
                                    new Scalar(255, 255, 0), new Scalar(0, 0, 0), 3, ctx.ui_liv_multi_st, 0.33);
                            }
                            break;


                        case DemoStage.FaceCompare:
                            {
                                if (prop_demo.COMPARE_API_TYPE == "None")
                                {
                                    SetDemoStage(DemoStage.FinalResult);
                                    break;
                                }

                                //
                                // prepare image
                                //

                                /*
                                       compare image1:
                                       1] A cropped face image, the entire face is clearly visible
                                       2] detect face (minimum 112x112) from photo image
                                       3] add margin

                                       compare image2: 
                                       1] bestshot 4th(lastest)
                                */

                                if (!ctx.cmp_prepare_img_performed)
                                {
                                    // prepare src compare image from compare_img_path
                                    string cmp_img_file_path = prop_settings.COMPARE_IMG_PATH + "\\"
                                        + prop_demo.COMPARE_SRC_IMG_FILENAME;

                                    CamCtx.BGRImg cmp_bgr_img_file = CamCtx.MakeBGRFromImg(cmp_img_file_path);
                                    ctx.cmp_src_bgr_from_img_file = new BlobImg(cmp_bgr_img_file.pixels_bgr,
                                        cmp_bgr_img_file.width, cmp_bgr_img_file.height);

                                    Box face_box_from_original_img_file;
                                    Box face_box_from_cropped_face;

                                    BlobImg cmp1_cropped_face_bgr = CropFaceFromImg(fsdk,
                                        ctx.cmp_src_bgr_from_img_file,
                                        out face_box_from_original_img_file,
                                        out face_box_from_cropped_face,
                                        prop_faceinfo.FACE_INFO_CROPFACE_FACE_WH_MARGIN_RATIO,
                                        prop_faceinfo.FACE_INFO_CROPFACE_RESIZE_WIDTH_AFTER_CROP);

                                    ctx.cmp_src_bgr = cmp1_cropped_face_bgr;
                                    ctx.cmp_src_face_box = face_box_from_cropped_face;

                                    if(ctx.best_liv_shots.last_shot_idx < 0)
                                    {
                                        throw new InvalidOperationException("invalid state, ctx.best_liv_shots.last_shot_idx can't be under 0");
                                    }

                                    if (prop_demo.COMPARE_API_TYPE == "FaceServer")
                                    {
                                        // prepare dst compare image from lastest best live shot 
                                        BlobImg cmp2_bgr = ctx.best_liv_shots[ctx.best_liv_shots.last_shot_idx]; // use lastest bestshot image
                                        Box cmp2_face_box = ctx.best_liv_shots.GetShot(ctx.best_liv_shots.last_shot_idx).detected_face.box;

                                        ctx.cmp_dst_bgr = cmp2_bgr;
                                        ctx.cmp_dst_face_box = cmp2_face_box;
                                    }
                                    else if (prop_demo.COMPARE_API_TYPE == "ClientOnly")
                                    {
                                        // prepare dst compare image from lastest best live shot 
                                        BlobImg cmp2_bgr = ctx.best_liv_shots[ctx.best_liv_shots.last_shot_idx]; // use lastest bestshot image
                                        Box cmp2_face_box = ctx.best_liv_shots.GetShot(ctx.best_liv_shots.last_shot_idx).detected_face.box;

                                        Box face_box_from_cmp2_bgr;
                                        Box face_box_from_cmp2_bgr_cropped_face;

                                        BlobImg cmp2_cropped_face_bgr = CropFaceFromImg(fsdk,
                                            cmp2_bgr,
                                            out face_box_from_cmp2_bgr,
                                            out face_box_from_cmp2_bgr_cropped_face,
                                            prop_faceinfo.FACE_INFO_CROPFACE_FACE_WH_MARGIN_RATIO,
                                            prop_faceinfo.FACE_INFO_CROPFACE_RESIZE_WIDTH_AFTER_CROP);

                                        ctx.cmp_dst_bgr = cmp2_cropped_face_bgr;
                                        ctx.cmp_dst_face_box = face_box_from_cmp2_bgr_cropped_face;
                                    }
                                    else
                                    {
                                        Environment.FailFast("Fatal, implementation not yet!");
                                    }

                                    ctx.cmp_prepare_img_performed = true;

                                    // evaluate compare image status
                                    if (ctx.cmp_src_bgr == null || ctx.cmp_dst_bgr == null)
                                    {
                                        ctx.cmp_prepare_img_err_msg = "last_err=failed to preapre compare image";

                                        ctx.ui_api_compare_st.Clear();
                                        ctx.ui_api_compare_msg("[I] Prepare Compare Face Image..");

                                        if (ctx.cmp_src_bgr == null)
                                        {
                                            ctx.ui_api_compare_msg("[E] src image is invalid..");
                                        }

                                        if (ctx.cmp_dst_bgr == null)
                                        {
                                            ctx.ui_api_compare_msg("[E] dst image is invalid..");
                                        }

                                        if (ctx.ui_finish_compare_task == null)
                                        {
                                            ctx.ui_finish_compare_task =
                                                ((Func<int, Action<string>, Action<string, object>, Task>)
                                                (async (_delay_time_sec, _ui_msg, _finish) =>
                                                {
                                                    while (_delay_time_sec > 0)
                                                    {
                                                        await Task.Delay(1000);
                                                        _ui_msg("[ALARM] Face Ratio of Compare Images is Invalid.. Finish .. " + (int)(_delay_time_sec));
                                                        _delay_time_sec -= 1;
                                                    }

                                                    _finish("compare_prepare_img", ctx.cmp_prepare_img_err_msg);

                                                }))(prop_demo.COMPARE_UI_AUTO_FIN_SEC,
                                                        ctx.ui_api_compare_msg, ctx.ui_fin_compare_check_act);
                                        }

                                        break;
                                    }

                                }

                                //
                                // check compare image face width ratio
                                //

                                if (ctx.cmp_src_bgr == null && ctx.cmp_src_bgr_from_img_file != null)
                                {
                                    // no face is detected from src compare image
                                    CamCtx.DrawImgScaled(ui_render_frame,
                                        new Point(10, pt_liv_ui.Y),
                                        new Size(180, 180),
                                        ctx.cmp_src_bgr_from_img_file.data_,
                                        ctx.cmp_src_bgr_from_img_file.width_,
                                        ctx.cmp_src_bgr_from_img_file.height_);

                                    CamCtx.DrawText(ui_render_frame,
                                        "<No Face Is Detected!>",
                                        new Point(15, pt_liv_ui.Y + 60),
                                        new Scalar(0, 0, 255),
                                        0.4f);

                                    pt_liv_ui += new Point(0, 190);
                                }
                                else if (ctx.cmp_src_bgr != null && ctx.cmp_dst_bgr != null)
                                {
                                    string chk_src_face_ratio_w_rst_msg = "";
                                    string chk_dst_face_ratio_w_rst_msg = "";

                                    if (!CheckCompareInputImgWithFaceWidthRatio(
                                        ref chk_src_face_ratio_w_rst_msg,
                                        ctx.cmp_src_face_box.w,
                                        ctx.cmp_src_bgr.width_,
                                        ctx.threshold_compare_face_w_siz_ratio_min,
                                        ctx.threshold_compare_face_w_siz_ratio_max))
                                    {
                                        chk_src_face_ratio_w_rst_msg = "[E] [SRC-IMG] " + chk_src_face_ratio_w_rst_msg;
                                    }

                                    if (!CheckCompareInputImgWithFaceWidthRatio(
                                        ref chk_dst_face_ratio_w_rst_msg,
                                        ctx.cmp_dst_face_box.w,
                                        ctx.cmp_dst_bgr.width_,
                                        ctx.threshold_compare_face_w_siz_ratio_min,
                                        ctx.threshold_compare_face_w_siz_ratio_max))
                                    {
                                        chk_dst_face_ratio_w_rst_msg = "[E] [DEST-IMG] " + chk_dst_face_ratio_w_rst_msg;
                                    }

                                    DrawCompareImg("SRC:", ui_render_frame, new Point(10, pt_liv_ui.Y),
                                        ctx.cmp_src_bgr, ctx.cmp_src_face_box,
                                        new Size(180, 180), new Scalar(128, 128, 255), 0.35f);

                                    DrawCompareImg("DST:", ui_render_frame, new Point(185, pt_liv_ui.Y),
                                        ctx.cmp_dst_bgr, ctx.cmp_dst_face_box,
                                        new Size(180, 180), new Scalar(255, 0, 255), 0.35f);

                                    pt_liv_ui += new Point(0, 190);

                                    //  face width ration is invalid => abort compare
                                    if (chk_src_face_ratio_w_rst_msg != "" || chk_dst_face_ratio_w_rst_msg != "")
                                    {
                                        ctx.cmp_prepare_img_err_msg = "last_err=invalid face_ratio is detected from compare image";

                                        if (ctx.ui_finish_compare_task == null)
                                        {
                                            ctx.ui_finish_compare_task =
                                                ((Func<int, string, string, Action<string>, Action<string, object>, Task>)
                                                (async (_delay_time_sec, _chk_src_face_ratio_w, _chk_dst_face_ratio_w, _ui_msg, _finish) =>
                                                {
                                                    _ui_msg(_chk_src_face_ratio_w);
                                                    _ui_msg(_chk_dst_face_ratio_w);

                                                    while (_delay_time_sec > 0)
                                                    {
                                                        await Task.Delay(1000);
                                                        _ui_msg("[ALARM] Face Ratio of Compare Images is Invalid.. Finish .. " + (int)(_delay_time_sec));
                                                        _delay_time_sec -= 1;
                                                    }

                                                    _finish("compare_chk_faceratiow", ctx.cmp_prepare_img_err_msg);

                                                }))(prop_demo.COMPARE_UI_AUTO_FIN_SEC,
                                                        chk_src_face_ratio_w_rst_msg,
                                                        chk_dst_face_ratio_w_rst_msg,
                                                        ctx.ui_api_compare_msg,
                                                        ctx.ui_fin_compare_check_act);
                                        }
                                    }
                                }

                                if (ctx.cmp_prepare_img_err_msg == "")
                                {
                                    //
                                    // images is valid, start compare
                                    //

                                    if (prop_demo.COMPARE_API_TYPE == "ClientOnly")
                                    {
                                        if (ctx.ui_finish_compare_task == null)
                                        {
                                            ctx.ui_api_compare_st.Clear();
                                            ctx.ui_api_compare_msg("[I] Prepare ClientOnly Compare ..");

                                            string src_feat_vec_err = "";
                                            string dst_feat_vec_err = "";
                                            string feature_dist_err = "";

                                            FaceSDK.RstFeatureVec src_feat_vec_rst;
                                            FaceSDK.RstFeatureVec dst_feat_vec_rst;
                                            float computed_feature_dist = 0.0f;

                                            Stopwatch sw_extract_feat_vec1 = new Stopwatch();
                                            Stopwatch sw_extract_feat_vec2 = new Stopwatch();
                                            Stopwatch sw_compute_feat_vec_dst = new Stopwatch();

                                            sw_extract_feat_vec1.Start();
                                            src_feat_vec_rst = fsdk.ExtractFeatureVecFromImg(
                                                ctx.cmp_src_bgr.data_, ctx.cmp_src_bgr.width_, ctx.cmp_src_bgr.height_);
                                            sw_extract_feat_vec1.Stop();

                                            if (src_feat_vec_rst.IsErr())
                                            {
                                                src_feat_vec_err = $"[E] err={src_feat_vec_rst.last_err}, err_desc={src_feat_vec_rst.last_err_desc}";
                                            }

                                            sw_extract_feat_vec2.Start();
                                            dst_feat_vec_rst = fsdk.ExtractFeatureVecFromImg(
                                                ctx.cmp_dst_bgr.data_, ctx.cmp_dst_bgr.width_, ctx.cmp_dst_bgr.height_);
                                            sw_extract_feat_vec2.Stop();

                                            if (dst_feat_vec_rst.IsErr())
                                            {
                                                dst_feat_vec_err = $"[E] err={dst_feat_vec_rst.last_err}, err_desc={dst_feat_vec_rst.last_err_desc}";
                                            }

                                            //
                                            // compute distance
                                            //

                                            if (src_feat_vec_err == "" && dst_feat_vec_err == "")
                                            {
                                                FaceSDK.Rst feature_dist_rst
                                                    = fsdk.CompareFeatureVecs(src_feat_vec_rst.feature_vec, dst_feat_vec_rst.feature_vec);

                                                if (feature_dist_rst.IsErr())
                                                {
                                                    feature_dist_err = $"[E] err={feature_dist_rst.last_err}, err_desc={feature_dist_rst.last_err_desc}";
                                                }
                                                else
                                                {
                                                    computed_feature_dist = feature_dist_rst.GetFeatureDist();
                                                }
                                            }
                                            else
                                            {
                                                feature_dist_err = $"[E] error on extract feature";
                                            }

                                            //
                                            // fin
                                            //

                                            if (feature_dist_err == "")
                                            {
                                                ctx.ui_api_compare_msg("[OK] ClientOnly Compare Operation is OK..");
                                                ctx.ui_api_compare_msg("[OK] Compare Result)");

                                                // FaceSDK.Params.THRESHOLD_FEATURE
                                                float compare_match_thres = prop_demo.COMPARE_MATCH_THRESHOLD;

                                                if (computed_feature_dist < compare_match_thres)
                                                {
                                                    ctx.ui_api_compare_msg($"[OK] MATCH! ");
                                                    ctx.ui_api_compare_msg($"[OK] > dist({computed_feature_dist}) < thres({compare_match_thres})");
                                                }
                                                else
                                                {
                                                    ctx.ui_api_compare_msg($"[FAIL] NOT MATCH! ");
                                                    ctx.ui_api_compare_msg($"[FAIL] > dist({computed_feature_dist}) >= thres({compare_match_thres})");
                                                }


                                                ctx.ui_api_compare_msg("[I]");
                                                ctx.ui_api_compare_msg($"[I] Total Elapsed) {sw_extract_feat_vec1.ElapsedMilliseconds + sw_extract_feat_vec2.ElapsedMilliseconds + sw_compute_feat_vec_dst.ElapsedMilliseconds} MS");
                                                ctx.ui_api_compare_msg($"[I] > Extract FeatVec1= {sw_extract_feat_vec1.ElapsedMilliseconds} MS");
                                                ctx.ui_api_compare_msg($"[I] > Extract FeatVec2= {sw_extract_feat_vec2.ElapsedMilliseconds} MS");
                                                ctx.ui_api_compare_msg($"[I] > Compute FeatVec= {sw_compute_feat_vec_dst.ElapsedMilliseconds} MS");
                                            }
                                            else
                                            {
                                                ctx.ui_api_compare_msg("[E] ClientOnly Compare Operation is Failed..");
                                                ctx.ui_api_compare_msg("[E] > compute_feat_dist_err=" + feature_dist_err);
                                                ctx.ui_api_compare_msg("[E] > extract_feat_vec_src_err=" + src_feat_vec_err);
                                                ctx.ui_api_compare_msg("[E] > extract_feat_vec_dst_err=" + dst_feat_vec_err);
                                            }

                                            ctx.ui_finish_compare_task =
                                                ((Func<int, Action<string>, Action<string, object>, Task>)
                                                (async (_delay_time_sec, _ui_msg, _finish) =>
                                                {
                                                    while (_delay_time_sec > 0)
                                                    {
                                                        await Task.Delay(1000);
                                                        _ui_msg("[ALARM] Finishing ClientOnly Compare .. " + (int)(_delay_time_sec));
                                                        _delay_time_sec -= 1;
                                                    }

                                                    _finish("compare_clienonly_done", "");

                                                }))(prop_demo.COMPARE_UI_AUTO_FIN_SEC, ctx.ui_api_compare_msg,
                                                    ctx.ui_fin_compare_check_act);

                                        }

                                    }
                                    else if (prop_demo.COMPARE_API_TYPE == "FaceServer")
                                    {
                                        if (!ctx.cmp_prepare_img_performed)
                                            break;

                                        if (ctx.api_compare_task == null)
                                        {
                                            ctx.ui_api_compare_st.Clear();
                                            ctx.ui_api_compare_msg("[I] Prepare Server Face Compare API..");

                                            //
                                            // request faceserver compare api
                                            //

                                            ctx.api_compare_task =
                                                ((Func<string, Action<string>, Task<Api>>)
                                                 (async (_api_svr_url, _ui_msg) =>
                                                 {
                                                     // USE_HTTP_PROXY, HTTP_PROXY_URL can be omitted, use for debugging.
                                                     RestConn _conn = new RestConn(_api_svr_url, USE_HTTP_PROXY, HTTP_PROXY_URL);
                                                     FaceCompareApi _api = new FaceCompareApi();

                                                     PayloadEncrypt img_encryptor = null;

                                                     if (API_ENC_IMG_TYPE == "RSA+AES")
                                                     {
                                                         img_encryptor = PayloadEncrypt.Create(
                                                             new PayloadEncrypt.EncRSAAES(API_ENC_PUB_KEY_B64, API_ENC_TIMESTAMP));
                                                     }

                                                     _ui_msg("[SV-REQ] Sending api request to server!");
                                                     _ui_msg("[SV-REQ] > url=" + _conn.GetURL() + ", conn_err=" + _conn.GetLastErr());
                                                     _ui_msg("[SV-REQ] > api=" + _api.path);
                                                     _ui_msg("[SV-REQ] > api img encrypt mode=" + API_ENC_IMG_TYPE);

                                                     if (API_ENC_IMG_TYPE != "NONE")
                                                     {
                                                         if (img_encryptor.IsErr())
                                                         {
                                                             _ui_msg("[FAIL] > error on img encryptor, err="
                                                                 + img_encryptor.GetLastErr() + ", desc=" + img_encryptor.GetLastErrDesc());
                                                         }
                                                     }

                                                     if (!_api.MakeReq(ctx.cmp_src_bgr, ctx.cmp_dst_bgr, img_encryptor))
                                                     {
                                                         _ui_msg("[FAIL] > failed to make api request body=" + _api.GetLastErrDesc());
                                                         return _api;
                                                     }

                                                     if (API_ENC_IMG_TYPE != "NONE")
                                                     {
                                                         if (!img_encryptor.IsErr())
                                                         {
                                                             _ui_msg("[SV-REQ] > api img encrypt enc time_ms=" + img_encryptor.GetLastEncElapsedMS());
                                                         }
                                                     }

                                                     await _conn.SendRequestAsync(_api);

                                                     return _api;

                                                 }))(api_svr_url, ctx.ui_api_compare_msg);
                                        }

                                        if (ctx.api_compare_task.IsCompleted)
                                        {
                                            if (ctx.api_compare == null)
                                            {
                                                ctx.api_compare = ctx.api_compare_task.GetAwaiter().GetResult();

                                                if (ctx.api_compare.IsErr()) // check api request to server is performed
                                                {
                                                    ctx.ui_api_compare_msg("[SV-FAIL] Server API Request Connect Is Failed!");
                                                    ctx.ui_api_compare_msg("[SV-FAIL] > err=" + ctx.api_compare.GetLastErr());
                                                    ctx.ui_api_compare_msg("[SV-FAIL] > api_desc=" + ctx.api_compare.GetLastErrDesc());
                                                }
                                                else
                                                {
                                                    ctx.ui_api_compare_msg("[SV-OK] API Request is OK, elapsed="
                                                        + ctx.api_compare.GetSvrRequestElapsedMS() + " MS");

                                                    if (ctx.api_compare.IsHttpResponseOK())
                                                    {
                                                        ctx.ui_api_compare_msg("[HTTP-OK] api=" + ctx.api_compare.path + " is OK!");

                                                        FaceCompareApi.Res res
                                                            = ctx.api_compare.GetResponse() as FaceCompareApi.Res;

                                                        ctx.ui_api_compare_msg("[HTTP-OK] >" + $"SimilarityConidence = {res.similarity_confidence}");
                                                        ctx.ui_api_compare_msg("[HTTP-OK] >" + $"Match-Rst = {res.match_result}");
                                                        if (res.match_result != 0)
                                                        {
                                                            ctx.ui_api_compare_msg("[HTTP-OK] >" + $"Match-Threshold = {res.threshold_info.auto_approve.min}");
                                                        }
                                                        ctx.ui_api_compare_msg("[HTTP-OK] >" + $"Ret-Code = {res.return_msg.return_code}");
                                                        ctx.ui_api_compare_msg("[HTTP-OK] >" + $"Ret-Msg = {res.return_msg.return_msg}");
                                                    }
                                                    else
                                                    {
                                                        ctx.ui_api_compare_msg("[HTTP-FAIL] api=" + ctx.api_compare.path + " is Failed!");
                                                        ctx.ui_api_compare_msg("[HTTP-FAIL] > status=" + ctx.api_compare.GetHttpResponseCode());
                                                        ctx.ui_api_compare_msg("[HTTP-FAIL] > payload=" + ctx.api_compare.GetResponse());
                                                    }
                                                }

                                                ctx.ui_finish_compare_task =
                                                    ((Func<int, Api, Action<string>, Action<string, object>, Task>)
                                                    (async (_delay_time_sec, _api_rst, _ui_msg, _finish) =>
                                                    {
                                                        while (_delay_time_sec > 0)
                                                        {
                                                            await Task.Delay(1000);
                                                            _ui_msg("[ALARM] Finishing SVR API - Compare .. " + (int)(_delay_time_sec));
                                                            _delay_time_sec -= 1;
                                                        }

                                                        ctx.ui_finish_compare_task = null;
                                                        ctx.api_compare_task = null;
                                                        ctx.ui_api_compare_st.Clear();

                                                        _finish("compare_api_done", _api_rst);

                                                    }))(prop_demo.COMPARE_UI_AUTO_FIN_SEC, ctx.api_compare,
                                                            ctx.ui_api_compare_msg, ctx.ui_fin_compare_check_act);
                                            }
                                        }
                                    }
                                }

                                CamCtx.DrawLogBox(ui_render_frame,
                                    pt_liv_ui, new Point(380, 300),
                                    new Scalar(255, 255, 0), new Scalar(0, 0, 0), 3,
                                    ctx.ui_api_compare_st,
                                    0.33);
                            }
                            break;

                        case DemoStage.FinalResult:
                            {
                                SetDemoStage(DemoStage.Reset);
                            }
                            break;

                        default:
                            break;
                    }

                    //
                    // color histogram
                    //

                    if (prop_settings.UI_ETC_COLOR_HISTOGRAM_SHOW)
                    {
                        CamCtx.RenderHistogram(ui_render_frame, rgb_hgrm, new Point(10, (cap_img_bgr_height - 350)));
                    }


                    //
                    // timestamp, fps
                    //

                    string ts = DateTime.Now.ToString("yyyyMMddHHmmss");
                    string top_msg1 = $"{ts} tick/s={stat_tick_cnt_sec}, CVMat={FormatBytesPerSec(stat_ren_cv_mat_bytes_sec)}";
                    CamCtx.DrawText(ui_render_frame, top_msg1, new Point(10, 13), new Scalar(0, 0, 255), liv_ui_fnt_scl);

                    string cam_model = user_prm.cam_.cap_dev_type;
                    string cam_whsf = user_prm.cam_.GetInfo("desc_w_h_stream_fps");
                    string cam_remote_proto = user_prm.cam_.cap_remote_proto;

                    string top_msg2 = "";

                    if (cam_model == CamCtx_IPCam.DEV_TYPE)
                    {
                        string recv_stat = "";
                        string recv_stat_info = user_prm.cam_.GetInfo("desc_recv_frmcnt_bytes_per_sec");

                        string[] parts = recv_stat_info.Split(';');

                        if (parts.Length == 2 &&
                            long.TryParse(parts[0], out long frames_sec) &&
                            long.TryParse(parts[1], out long bytes_sec))
                        {
                            // KB: network bytes,  / 1000
                            // KiB: memory, image bytes  / 1024
                            recv_stat = $"{frames_sec} Frm/s, {FormatBytesPerSec(bytes_sec)}";
                        }


                        top_msg2 = $"[{cam_model}-{cam_remote_proto} {cam_whsf}, Recv={recv_stat}";                        
                    }
                    else
                    {
                        top_msg2 = $"[{cam_model}-{cam_remote_proto}] {cam_whsf} ";
                    }

                    CamCtx.DrawText(ui_render_frame, top_msg2, new Point(10, 30), new Scalar(255, 64, 255), liv_ui_fnt_scl * 0.9);

                    //
                    // record
                    //

                    if (ctx.rec_ctx.rec_is_recording)
                    {
                        if (ctx.rec_ctx.rec_recorder == null)
                        {
                            VideoRec rec = new FaceSDKSample.VideoRec(
                                ctx.rec_ctx.rec_file_path,
                                ui_render_frame.Width, ui_render_frame.Height,
                                ctx.rec_ctx.rec_fps,
                                ctx.rec_ctx.rec_rolling_siz_mb);

                            if (rec == null)
                            {
                                Console.WriteLine("[ERR] failed to create VideoRec, file_path=", ctx.rec_ctx.rec_file_path);
                                ctx.rec_ctx.rec_is_recording = false;
                            }
                            else
                            {
                                if (!rec.Roll())
                                {
                                    Console.WriteLine("[ERR] failed to create VideoRec, file_path=", ctx.rec_ctx.rec_file_path);
                                    Console.WriteLine("[ERR] > err_desc=", rec.last_err_desc);
                                    ctx.rec_ctx.rec_is_recording = false;
                                }

                                ctx.rec_ctx.rec_recorder = rec;
                            }
                        }

                        if (ctx.rec_ctx.rec_recorder != null)
                        {
                            if (ctx.rec_ctx.rec_recorder.AddFrame(ui_render_frame))
                            {
                                if (ctx.rec_ctx.rec_status_callback != null)
                                {
                                    long rec_file_siz = ctx.rec_ctx.rec_recorder.GetLastReadFileSiz() / (1024 * 1024);

                                    ctx.rec_ctx.rec_status_callback(
                                        rec_file_siz,
                                        ctx.rec_ctx.rec_recorder.GetCurFileName(),
                                        1);
                                }

                            }
                            else
                            {
                                if (ctx.rec_ctx.rec_status_callback != null)
                                {
                                    ctx.rec_ctx.rec_status_callback(
                                        0,
                                        ctx.rec_ctx.rec_recorder.GetCurFileName(),
                                        -1);
                                }

                                //Console.WriteLine("[ERR] failed to add frame to VideoRec, file_path=",
                                //    ctx.rec_recorder.GetCurFileName() + ", err_desc=" + ctx.rec_recorder.last_err_desc);

                                //ctx.rec_ctx.rec_is_recording = false;
                            }
                        }

                    }

                    if (ctx.rec_ctx.rec_is_recording)
                    {
                        if (RunContext.get_utc_ms() % 2 == 0)
                        {
                            CamCtx.DrawText(ui_render_frame, "[R!]",
                              new Point(cap_img_bgr_width - 70, 15), new Scalar(0, 0, 255), liv_ui_fnt_scl * 1.2);
                        }
                    }


                    // draw face ceter guide for check center position
                    if (prop_settings.UI_ETC_GUIDE_FACE_GUIDE_SHOW)
                        CamCtx.RenderCicleHoleMat(ui_render_frame, 0.35f, 0.8f);

                    //save_debug_crop_img_to_bmp_file

                    long cur_ren_cv_mat_bytes = 0;

                    if (ui_render_frame != null && !ui_render_frame.Empty() ) {
                        cur_ren_cv_mat_bytes = (long)ui_render_frame.Total() * ui_render_frame.ElemSize();
                        
                        cam.RenderFrame(ui_render_frame);
                    }

                    stat_last_ren_cv_mat_bytes += cur_ren_cv_mat_bytes;

                } // -if (cam.CaptureFrame())-
                else
                {
                    // 1] change camera index
                    // 2] change camera capture api (ANY, MSMF, DSHOW, FFMPEG ..)
                }

                
            }

            //
            // exit
            //

            Exit();
        }

        void Exit()
        {
            if (exit_called_)
            {
                return;
            }

            UserPrm user_prm = (UserPrm)GetUserPrm();
            user_prm.ui_onexit_();

            exit_called_ = true;
        }

        public static BlobImg CropFaceFromImg(FaceSDK facesdk, BlobImg bgr_img,
            out Box detected_face_box, out Box detected_face_box_from_crop_face,
            float face_width_hegit_margin_ratio = 0.33F /*FaceSDK.Params.CROPFACE_WH_MARGION_RATIO[1]*/,
            int face_resize_width_after_crop = 200 /*FaceSDK.Params.CROPFACE_RESIZE_WIDTH_AFTER_CROP[1]*/)
        {
            detected_face_box = new Box();
            detected_face_box_from_crop_face = new Box();

            FaceSDK.Face detected_face;
            FaceSDK.Rst detect_rst = facesdk.DetectFace(bgr_img.data_, bgr_img.width_, bgr_img.height_);

            if (detect_rst.IsErr())
            {
                Console.WriteLine("[ERR][CropFaceFromImg] failed to detect face, err=" + detect_rst.GetLastErrStr());
                return null;
            }

            if (detect_rst.GetFaceCnt() == 0)
            {
                Console.WriteLine("[ERR][CropFaceFromImg] failed to detect face, face_cnt=0");
                return null;
            }

            detected_face = detect_rst.GetFace();
            detected_face_box = detected_face.box;

            FaceSDK.FaceCropAreaRst face_area_crop
                = FaceSDK.CalcFaceCropArea(bgr_img.data_, bgr_img.width_, bgr_img.height_,
                        detected_face.box, face_width_hegit_margin_ratio, face_resize_width_after_crop);

            if (!face_area_crop.is_valid)
            {
                Console.WriteLine("[ERR][CropFaceFromImg] failed to retrieve crop face area");
                return null;
            }

            BlobImg cropped_and_resized_face_img
                = FaceSDK.CropFaceFromImg(bgr_img.data_, bgr_img.width_, bgr_img.height_, face_area_crop);

            //
            // from cropped face
            //

            FaceSDK.Rst detected_face_from_cropped_face_rst 
                = facesdk.DetectFace(cropped_and_resized_face_img.data_, 
                        cropped_and_resized_face_img.width_, 
                        cropped_and_resized_face_img.height_);

            if (detected_face_from_cropped_face_rst.IsErr())
            {
                Console.WriteLine("[ERR][CropFaceFromImg] failed to detect face from cropped face, err=" 
                    + detected_face_from_cropped_face_rst.GetLastErrStr());
                return null;
            }

            if (detected_face_from_cropped_face_rst.GetFaceCnt() == 0)
            {
                Console.WriteLine("[ERR][CropFaceFromImg] failed to detect face from cropped face, face_cnt=0");
                return null;
            }

            FaceSDK.Face detected_face_from_cropped_face = detected_face_from_cropped_face_rst.GetFace();
            detected_face_box_from_crop_face = detected_face_from_cropped_face.box;

            return cropped_and_resized_face_img;
        }


        public static bool DrawCompareImgAndCheckFaceWRatio(Mat render_mat, Point relative_pos, BlobImg img,
            Size draw_siz, string caption1, string caption2,
            float face_w_ratio, float thres_face_w_ratio_min, float thres_face_w_ratio_max,
            Scalar fnt_color, double fnt_scl = 0.7, int fnt_thick = 1)
        {
            Mat mat_liv_ori = CamCtx.MakeMatFromBGR(img.data_, img.width_, img.height_);
            Mat mat_liv_resized = CamCtx.ResizeMat(mat_liv_ori, draw_siz);

            caption1 += " w=" + img.width_ + " ,h=" + img.height_;

            CamCtx.DrawText(mat_liv_resized, caption1, new Point(5, 15),
                fnt_color, fnt_scl, fnt_thick);

            CamCtx.DrawText(mat_liv_resized, caption2, new Point(5, 30),
                fnt_color, fnt_scl, fnt_thick);

            CamCtx.DrawText(mat_liv_resized, "face w-ratio=" + face_w_ratio, new Point(5, 45),
                fnt_color, fnt_scl, fnt_thick);

            bool face_w_ratio_err = false;

            if (face_w_ratio < thres_face_w_ratio_min)
            {
                CamCtx.DrawText(mat_liv_resized, "[ERR][MIN] w_ratio < [" + thres_face_w_ratio_min + "]", new Point(5, 57),
                    new Scalar(0, 0, 255), fnt_scl, fnt_thick);

                face_w_ratio_err = true;
            }
            else if (face_w_ratio < thres_face_w_ratio_min)
            {
                CamCtx.DrawText(mat_liv_resized, "[ERR][MAX] w_ratio > [" + thres_face_w_ratio_max + "]", new Point(5, 57),
                    new Scalar(0, 0, 255), fnt_scl, fnt_thick);

                face_w_ratio_err = true;
            }

            Mat mat_roi = new Mat(render_mat,
                 new Rect(relative_pos.X, relative_pos.Y, mat_liv_resized.Cols, mat_liv_resized.Rows));

            mat_liv_resized.CopyTo(mat_roi);

            return face_w_ratio_err;
        }

        public static void DrawFaceLandmark(Mat mat, in Face face)
        {
            FaceSDK.Point[] landmark_pts = face.landmark.ToArray();

            for (int i = 0; i < landmark_pts.Length; i++) {
                CamCtx.DrawBoxXYWH(mat, 
                    new Point(landmark_pts[i].x, landmark_pts[i].y),
                    new Point(1,1),
                    new Scalar(64,128,64)
                );
            }
        }


    }
}
