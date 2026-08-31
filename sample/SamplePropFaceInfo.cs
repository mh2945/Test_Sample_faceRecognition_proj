using Alchera.FaceSDK;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace FaceSDKSample.SampleProp
{
    public class SamplePropFaceInfo : Prop
    {
        public override Prop GetProp(string name)
        {
            return name == "UI" ? this : null;
        }

        [DisplayName("Enable"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_ENABLE { get; set; } = false;

        [DisplayName("Face POS"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_POS { get; set; } = true;

        [DisplayName("Face Yaw/Pitch/Roll"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_YAW_PITCH_ROLL { get; set; } = true;

        [DisplayName("ComputeLandmark"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_LANDMARK { get; set; } = true;

        [DisplayName("DrawLandMarkPoint"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_DRAW_LANDMARK_PT { get; set; } = true;
        
        [DisplayName("FaceOcclusion"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_FACE_OCCLUSION { get; set; } = false;

        [DisplayName("FineOcclusion"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_FINE_OCCLUSION { get; set; } = false;

        [DisplayName("ClosedEyes"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_CLOSED_EYE_STATE { get; set; } = true;

        [DisplayName("BGR-GetImgLiveness"), Category("[10] Draw Face Info")]
        public bool FACE_INFO_BGR_LIVENESS { get; set; } = false;

        //
        // [Cropped Face]
        //

        [DisplayName("enable"), Category("[20] Cropped Face Setting")]
        public bool FACE_INFO_CROPFACE_ENABLE { get; set; } = false;

        [DisplayName("face_wh_margin_ratio"), Category("[20] Cropped Face Setting")]
        public float FACE_INFO_CROPFACE_FACE_WH_MARGIN_RATIO { get; set; } = FaceSDK.Params.CROPFACE_WH_MARGION_RATIO[1];

        [DisplayName("resize_width_after_crop"), Category("[20] Cropped Face Setting")]
        public int FACE_INFO_CROPFACE_RESIZE_WIDTH_AFTER_CROP { get; set; } = FaceSDK.Params.CROPFACE_RESIZE_WIDTH_AFTER_CROP[1];

        [DisplayName("draw_resized"), Category("[20] Cropped Face Setting")]
        public bool FACE_INFO_CROPFACE_DRAW_RESIZED { get; set; } = false;
    }
}
