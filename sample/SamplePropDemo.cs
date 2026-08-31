using Alchera.FaceSDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using System.Drawing.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FaceSDKSample.SampleProp
{
    public class DemoClientServerProp : Prop
    {
        public override Prop GetProp(string name)
        {
            return name == "PassiveLivness" ? this : null;
        }


        //
        // liveness
        //

        // use compare thresholds from http://faceserver/compare/validation-config
        // use liveness thresholds from http://faceserver/liveness/validation-config
        //[DisplayName("enable"), Category("[LIV-05] Use FaceServer Validation Thresholds")]
        //public bool LIV_USE_FACESERVER_VALIDATION_THRESHOLDS { get; set; } = false;

        [DisplayName("enable"), Category("[LIV-09] Check Multiple Faces"),
            Description("[true] allow only one face during liveness operation \n[false] allow multiple face during liveness operation")]
        public bool LIV_CHK_MULTIPLE_FACES { get; set; } = true;

        [DisplayName("enable"), Category("[LIV-10] Face Width Ratio (0.25 ~ 1.0)")]
        public bool LIV_CHK_FACE_WIDTH_RATIO { get; set; } = true;

        [DisplayName("[crop] min_w_ratio"), Category("[LIV-10] Face Width Ratio (0.25 ~ 1.0)"), 
            Description("The detected face width ratio relative to the image must be between 0.25 and 1.0") ]
        public float LIV_THRESHOLD_MIN_RATIO_WIDTH_FOR_CROP_IMG { get; set; } = LivenessThreshold.FACE_RATIO_WIDTH_MIN_FOR_CROP_IMG;

        [DisplayName("[crop] max_w_ratio"), Category("[LIV-10] Face Width Ratio (0.25 ~ 1.0)")]
        public float LIV_THRESHOLD_MAX_RATIO_WIDTH_FOR_CROP_IMG { get; set; } = LivenessThreshold.FACE_RATIO_WIDTH_MAX_FOR_CROP_IMG;

        [DisplayName("[original] min_w_ratio"), Category("[LIV-10] Face Width Ratio (0.25 ~ 1.0)")]
        public float LIV_THRESHOLD_MIN_RATIO_WIDTH_FOR_NORMAL_IMG { get; set; } = LivenessThreshold.FACE_RATIO_WIDTH_MIN;
        [DisplayName("[original] max_w_ratio"), Category("[LIV-10] Face Width Ratio (0.25 ~ 1.0)")]
        public float LIV_THRESHOLD_MAX_RATIO_WIDTH_FOR_NORMAL_IMG { get; set; } = LivenessThreshold.FACE_RATIO_WIDTH_MAX;


        [DisplayName("enable"), Category("[LIV-20] Face Center Position")]
        public bool LIV_CHK_CENTER_FACE_W_H_POS_IN_IMG { get; set; } = true;
        [DisplayName("ratio_center_in_img"), Category("[LIV-20] Face Center Position")]
        public float LIV_THRESHOLD_RATIO_CENTER_FACE_W_H_POS_IN_IMG { get; set; } = LivenessThreshold.CENTER_FACE_W_H_POS_RATIO;

        [DisplayName("enable"), Category("[LIV-30] Face Yaw-Pith-Roll")]
        public bool LIV_CHK_FACE_YAW_PITCH_ROLL { get; set; } = true;

        [TypeConverter(typeof(FloatArrayConverter))]
        [DisplayName("yaw(degree) range"), Category("[LIV-30] Face Yaw-Pith-Roll")]
        public float[] LIV_THRESHOLD_FACE_YAW_RANGE { get; set; } = LivenessThreshold.FACE_YAW;

        [TypeConverter(typeof(FloatArrayConverter))]
        [DisplayName("pitch(degree) range"), Category("[LIV-30] Face Yaw-Pith-Roll")]
        public float[] LIV_THRESHOLD_FACE_PITCH_RANGE { get; set; } = LivenessThreshold.FACE_PITCH;

        [TypeConverter(typeof(FloatArrayConverter))]
        [DisplayName("roll(degree) range"), Category("[LIV-30] Face Yaw-Pith-Roll")]
        public float[] LIV_THRESHOLD_FACE_ROLL_RANGE { get; set; } = LivenessThreshold.FACE_ROLL;


        [DisplayName("enable"), Category("[LIV-40] Face is Masked")]
        public bool LIV_CHK_FACE_IS_MASKED { get; set; } = true;
        
        [DisplayName("enable"), Category("[LIV-50] Face Feature Quality")]
        public bool LIV_CHK_FEATURE_QUALITY { get; set; } = true;
        [DisplayName("minUnMasked"), Category("[LIV-50] Face Feature Quality"), Description("Face is UnMasked - Quality For Feature Extraction")]
        public float LIV_THRESHOLD_FEATURE_QUALITY_UNMASKED_MIN { get; set; } = LivenessThreshold.FACE_FEATURE_QUALITY[0];
        [DisplayName("minMasked"), Category("[LIV-50] Face Feature Quality"), Description("Face is Masked - Quality For Feature Extraction")]
        public float LIV_THRESHOLD_FEATURE_QUALITY_MASKED_MIN { get; set; } = LivenessThreshold.FACE_FEATURE_QUALITY[1];


        [DisplayName("enable"), Category("[LIV-60] FAS Quality(light,display)"), 
            Description("checking background light, reflection of display device(check image on display)") ]
        public bool LIV_CHK_FAS_QUALITY { get; set; } = true;
        [DisplayName("min"), Category("[LIV-60] FAS Quality(light,display)")]
        public float LIV_THRESHOLD_FAS_QUALITY_MIN { get; set; } = LivenessThreshold.FACE_ANTISPOOFING_QUALITY;


        //
        // liveness bestshot
        //        
        private int liv_bgr_bestshot_cnt_ = 4;

        [DisplayName("bestshot_cnt"), Category("[LIV-74] BGR Image Liveness BestShot")]
        [DefaultValue(1)]
        [Editor(typeof(NumericUpDownEditor), typeof(UITypeEditor))]
        public int LIV_BGR_BESTSHOT_CNT
        {
            get => liv_bgr_bestshot_cnt_;
            set
            {
                liv_bgr_bestshot_cnt_ = (value < 1) ? 1 : value;
                liv_bgr_bestshot_cnt_ = (value > 4) ? 4 : value;
            }
        }

        // Accurate BGR image liveness results require at least 4 consecutive checks.
        // Therefore, liveness must be validated using four consecutive (previous) BGR image results.
        //
        // [true] use current liveness check result with previous check result
        // [false] drop previous liveness check result
        [DisplayName("continuous-4-bgr"), Category("[LIV-74] BGR Image Liveness BestShot")]
        public bool LIV_BGR_BESTSHOT_CONTINUOUS_FOUR_BGR_QUALITY_CHECK { get; set; } = true;


        //
        // faceserver liveness bestshot4 api
        //

        //[DisplayName("enable"), Category("[LIV-75] BGR Image Liveness")]
        //public bool LIV_BGR_IMG_LIV_ENABLE { get; set; } = true;        

        [DisplayName("api-type"), Category("[LIV-75] BGR Image Liveness")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("ClientOnly", "FaceServer", "None")]
        public string LIV_BGR_IMG_LIV_API_TYPE { get; set; } = "ClientOnly";

        [DisplayName("threshold"), Category("[LIV-75] BGR Image Liveness")]
        public float LIV_THRESHOLD_BGR_IMAGE_LIVENESS_MIN { get; set; } = LivenessThreshold.BGR_IMG_LIVENESS;

        [DisplayName("ui_auto_fin_sec"), Category("[LIV-75] BGR Image Liveness")]
        public int LIV_BGR_IMG_LIV_UI_AUTO_FIN_SEC { get; set; } = 5;


        //
        // faceserver compare api
        //

        [DisplayName("api-type"), Category("[LIV-80] Face Compare")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("ClientOnly", "FaceServer", "None")]
        public string COMPARE_API_TYPE { get; set; } = "ClientOnly";

        [DisplayName("match-threshold"), Category("[LIV-80] Face Compare")]
        public float COMPARE_MATCH_THRESHOLD { get; set; } = FaceSDK.Params.THRESHOLD_FEATURE;

        [DisplayName("cmp_src"), Category("[LIV-80] Face Compare")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("selected_img_file", "liv-bestshot", "captured face")]
        public string COMPARE_SRC_TYPE { get; set; } = "selected_img_file";

        [DisplayName("cmp_dst"), Category("[LIV-80] Face Compare")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("selected_img_file", "liv-bestshot", "captured face")]
        public string COMPARE_DST_TYPE { get; set; } = "bestshot";

        [DisplayName("cmp_img_file"), Category("[LIV-80] Face Compare")]
        public string COMPARE_SRC_IMG_FILENAME { get; set; } = "";  // valid only when LIV_COMPARE_SRC_TYPE="img with liv-bestshot"

        [DisplayName("ui_auto_fin_sec"), Category("[LIV-80] Face Compare")]
        public int COMPARE_UI_AUTO_FIN_SEC { get; set; } = 5;
    }

}
