/*
    FaceSDK.Stubs.cs — build-only stubs for the Alchera Face SDK C# wrapper.

    This repository publishes the application code only. The real SDK wrapper
    (FaceSDK.cs / FaceSDKPassiveLiv.cs / FaceSDKUtil.cs / FaceSDKSVR.cs) is vendor
    source and ships with the SDK package, so it is not included here.

    These declarations exist so the solution compiles and can be read in an IDE.
    Every method body throws; the app cannot run against these stubs.

    Threshold constants below are neutral placeholders, NOT the tuned production
    values. See the SDK distribution for the real numbers.
*/

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static Alchera.FaceSDK.FaceSDK;

namespace Alchera.FaceSDK
{
    public class FaceSDKException : Exception
    {
        public FaceSDKException(string msg) : base(msg) { }
    }

    public class FaceSDK
    {
        const string NOT_IMPL = "FaceSDK stub: the real implementation ships with the SDK package.";

        public class Meta
        {
            public const int VER_MAJ = 1;
            public const int VER_MIN = 3;
            public const int VER_PATCH = 0;
            public const long VER_BUILD = 0;
            public const int VER_FEATURE = 0;

            public const int REQ_FSDKC_VER_MAJ = 1;
            public const int REQ_FSDKC_VER_MIN = 4;
            public const int REQ_FSDKC_VER_PATCH = 0;
            public const int REQ_FSDKC_VER_FEATURE = 0;
        }

        public static readonly string VER_STR = Meta.VER_MAJ + "." + Meta.VER_MIN + "."
            + Meta.VER_PATCH + "-" + Meta.VER_FEATURE + "." + Meta.VER_BUILD + "-stub";

        public class BlobImg
        {
            public readonly byte[] data_;
            public readonly int width_;
            public readonly int height_;
            public readonly string desc_;  // ex: "bgr-raw", "jpg"

            public BlobImg(byte[] data, int width, int height, string desc = "")
            {
                data_ = data;
                width_ = width;
                height_ = height;
                desc_ = desc;
            }

            public Bitmap MakeBitmap()
            {
                throw new NotImplementedException(NOT_IMPL);
            }

            public static BlobImg MakeBgrBlobImgFromBgrBitmap(Bitmap bgr_bitmap)
            {
                throw new NotImplementedException(NOT_IMPL);
            }
        }

        public class Params
        {
            // placeholder thresholds — see the SDK distribution for real values
            public const float THRESHOLD_ATTR_FACE_IS_MASKED = 0.5F;
            public const float THRESHOLD_FEATURE = 0.5F;

            // [0] for file img (gallery photo), compare
            // [1] for detected face on front camera (android, ios)
            public static readonly float[] CROPFACE_WH_MARGION_RATIO = { 0.25f, 0.33f };
            public static readonly int[] CROPFACE_RESIZE_WIDTH_AFTER_CROP = { 150, 200 };
        }

        public enum Error : int
        {
            NoError,
            NotInitialized,
            CanNotReadModel,
            InvalidLicense,
            LicenseExpired,
            UnknownLicenseError,
            CanNotOpenExtensionFile,
            InvalidExtension,
            DisabledExtension,
            NotPermittedInThisLicense,
            NothingAtInput,
            ModelIsNotInitialized,
            CanNotSetGPUIDAfterInitialization,
            CanNotSetGPUIDWithoutGPU,
            CanNotReadLicense,
            SystemTimeTampered,
            LicenseNotStarted,
            SystemFunctionError
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct Point
        {
            public float x;
            public float y;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct Box
        {
            public float x;
            public float y;
            public float w;
            public float h;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct Pose
        {
            public float yaw;
            public float pitch;
            public float roll;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct LandMark
        {
            public static readonly int RAW_SIZE = Marshal.SizeOf(typeof(LandMark));
            private static readonly System.Reflection.FieldInfo[] FIELDS_CACHE =
                typeof(LandMark).GetFields(System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance);

            public const int PT_CNT = 106;
            // 4.x .net framework does not support 'unsafe fixed Point[106]'
            public Point p0, p1, p2, p3, p4, p5, p6, p7, p8, p9,
                         p10, p11, p12, p13, p14, p15, p16, p17, p18, p19,
                         p20, p21, p22, p23, p24, p25, p26, p27, p28, p29,
                         p30, p31, p32, p33, p34, p35, p36, p37, p38, p39,
                         p40, p41, p42, p43, p44, p45, p46, p47, p48, p49,
                         p50, p51, p52, p53, p54, p55, p56, p57, p58, p59,
                         p60, p61, p62, p63, p64, p65, p66, p67, p68, p69,
                         p70, p71, p72, p73, p74, p75, p76, p77, p78, p79,
                         p80, p81, p82, p83, p84, p85, p86, p87, p88, p89,
                         p90, p91, p92, p93, p94, p95, p96, p97, p98, p99,
                         p100, p101, p102, p103, p104, p105;

            public Point[] ToArray()
            {
                Point[] pts = new Point[PT_CNT];

                for (int i = 0; i < PT_CNT; i++)
                {
                    pts[i] = (Point)FIELDS_CACHE[i].GetValue(this);
                }

                return pts;
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct LandMark5pt
        {
            public static readonly int RAW_SIZE = Marshal.SizeOf(typeof(LandMark5pt));
            private static readonly System.Reflection.FieldInfo[] FIELDS_CACHE =
                typeof(LandMark5pt).GetFields(System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.Instance);

            public const int PT_CNT = 5;
            public Point p0, p1, p2, p3, p4; // for array interop from native

            public Point[] ToArray()
            {
                Point[] pts = new Point[PT_CNT];

                for (int i = 0; i < PT_CNT; i++)
                {
                    pts[i] = (Point)FIELDS_CACHE[i].GetValue(this);
                }

                return pts;
            }
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct Face
        {
            public int id; // -1
            public Box box;
            public LandMark landmark;
            public LandMark5pt landmark_5pt;
            public Pose pose;
        };

        public struct FeatureVec
        {
            public const int DIM_CNT = 512;
            public float[] vec; /* float[512], 512-dimensional face vector */

            public bool IsErr() { return (vec == null || vec.Length < DIM_CNT) ? true : false; }

            public FeatureVec(bool init = true)
            {
                vec = new float[DIM_CNT];
            }

            public string ToBase64()
            {
                if (IsErr())
                    return "";

                byte[] bytes = new byte[vec.Length * sizeof(float)];
                Buffer.BlockCopy(vec, 0, bytes, 0, bytes.Length);

                return Convert.ToBase64String(bytes);
            }
        }

        public struct Rst
        {
            public Error last_err;                  // 0 for ok
            public string last_err_desc;

            public int face_cnt;                    // 0: not detected
            public Face face;

            // extra
            public int face2_cnt;
            public Face face2;

            // detect
            public float face_landmark_confidence;

            // feature
            public float face_feature_quality;
            public float feature_vec_distance;

            // attribute
            public float face_mask_confidence;

            public float face_occlu_conf_left_eye;
            public float face_occlu_conf_right_eye;
            public float face_occlu_conf_mouth;

            public float face_fine_occlu_score;

            // antispoofing
            public float img_liveness;
            public float img_face_quality_for_liveness;

            public float img_face_eye_state_left_eyelid_dist;
            public float img_face_eye_state_right_eyelid_dist;

            public int GetFaceCnt() { return face_cnt; }
            public int GetExtraFaceCnt() { return face2_cnt; }
            public bool IsFaceDetected() { return face_cnt == 0 ? false : true; }
            public Face GetFace() { return face; }
            public Face GetExtraFace() { return face2; }
            public bool IsOk() { return last_err == Error.NoError ? true : false; }
            public bool IsErr() { return last_err == Error.NoError ? false : true; }
            public Error GetLastErr() { return last_err; }
            public string GetLastErrStr() { return last_err.ToString(); }
            public string GetLastErrDesc() { return last_err_desc; }
            public float GetFeatureDist() { return feature_vec_distance; }
            public float GetImgLiveness() { return img_liveness; }

            public bool IsFaceMasked()
            {
                return face_mask_confidence < FaceSDK.Params.THRESHOLD_ATTR_FACE_IS_MASKED;
            }

            public float GetFaceQualityForFeature() { return face_feature_quality; }
            public float GetFaceQualityForLiveness() { return img_face_quality_for_liveness; }
        };

        public struct RstFeatureVec
        {
            public Error last_err;        // 0 for ok
            public string last_err_desc;

            public FeatureVec feature_vec;    // float[512], 512-dimensional face vector

            public bool IsErr()
            {
                if (last_err != 0)
                    return true;

                if (feature_vec.IsErr())
                    return true;

                return false;
            }

            public string ToBase64()
            {
                if (IsErr())
                    return "";

                return feature_vec.ToBase64();
            }
        }

        public struct BoolRst
        {
            public Error last_err;                  // 0 for ok
            public string last_err_desc;

            public bool result;

            public bool GetResult() { return result; }
            public bool IsOk() { return last_err == Error.NoError ? true : false; }
            public bool IsErr() { return last_err == Error.NoError ? false : true; }
            public Error GetLastErr() { return last_err; }
            public string GetLastErrStr() { return last_err.ToString(); }
            public string GetLastErrDesc() { return last_err_desc; }
        }

        public struct FaceCropAreaRst
        {
            public bool is_valid;

            public int x;
            public int y;
            public int w;
            public int h;

            public float margin_ratio;

            public int resize_w; // 0 for no resize
            public int resize_h; // 0 for no resize
        }

        //
        // print helpers
        //

        static public void Print(Pose pose, String prefix = "")
        {
            Console.WriteLine(prefix + " face-pose: yaw="
                + pose.yaw + " pitch=" + pose.pitch + " roll=" + pose.roll);
        }

        static public void Print(Box box, String prefix = "")
        {
            Console.WriteLine(prefix + " face-box: x=" + box.x
                + " y=" + box.y + " w=" + box.w + " h=" + box.h);
        }

        static public void Print(Face face, String prefix = "")
        {
            Print(face.pose, prefix);
            Print(face.box, prefix);
        }

        public static int GetByteSiz(float[] ary)
        {
            if (ary == null)
                return 0;

            return sizeof(float) * ary.Length;
        }

        //
        // lifecycle
        //

        public static string GetFSDKCVer()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static FaceSDK Instance()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static void DestroyInstance()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst Initialize(string mdl_path, string sdk_path)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        //
        // detection / attributes
        //

        public Rst DetectFace(byte[] src1_bgr, int src1_w, int src1_h,
            bool use_continuous_img_detect = false)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst ComputeLandmarkConfidence(byte[] src1_bgr, int src1_w, int src1_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst DetectClosedEyes(byte[] src1_bgr, int src1_w, int src1_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst DetectFaceOcclusion(byte[] src1_bgr, int src1_w, int src1_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst DetectFineOcclusion(byte[] src1_bgr, int src1_w, int src1_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        //
        // feature vector
        //

        public RstFeatureVec ExtractFeatureVecFromImg(byte[] src_bgr, int src_w, int src_h,
            bool use_continuous_img_detect = false)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst CompareFeatureVecs(FeatureVec feature_vec1, FeatureVec feature_vec2)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        //
        // liveness
        //

        public Rst GetImgFaceQualityForLiveness(byte[] src1_bgr, int src1_w, int src1_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst GetImgLiveness(byte[] src1_bgr, int src1_w, int src1_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public BoolRst ResetImgLivenessCheck()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Rst GetImgLivenessMulti(byte[] src1_bgr, int src1_w, int src1_h,
            byte[] src2_bgr, int src2_w, int src2_h, byte[] src3_bgr, int src3_w, int src3_h,
            byte[] src4_bgr, int src4_w, int src4_h, ref Face face)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        //
        // crop face
        //

        public static FaceCropAreaRst CalcFaceCropArea(byte[] src_bgr, int src_w, int src_h, Box face_box,
            float crop_face_w_h_margin_ratio = 1.0f, int resize_width_after_crop = 0)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static BlobImg CropFaceFromImg(byte[] src_bgr, int src_w, int src_h,
            FaceCropAreaRst crop_area)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    //
    // RawImageCV — OpenCV-backed image loading
    // (deliberately not new Bitmap(File.ReadAllBytes(..)) — see FeatureExpCompareDlg)
    //

    public class RawImageCV
    {
        const string NOT_IMPL = "FaceSDK stub: the real implementation ships with the SDK package.";

        public struct RawImgRst
        {
            public bool IsOk() { return last_err == Error.NoError; }
            public bool IsErr() { return last_err != Error.NoError; }
            public Error GetLastErr() { return last_err; }
            public string GetLastErrDesc() { return last_err_desc ?? ""; }

            public Error last_err;
            public string last_err_desc;

            public int w;
            public int h;
            public int bytes_pp;  // bytes_per_pixel => 3: RGB, 1: G

            // bytes per pixel-channel : bpp / pixel_hint_channels
            public int pixel_hint_channels;

            public uint flag;           // 0x1: float image (F16)

            public int pixel_buf_siz;
            public byte[] pixel_buf;
        }

        public struct Rst<T>
        {
            public bool IsOk() { return last_err == Error.NoError; }
            public bool IsErr() { return last_err != Error.NoError; }

            public Error GetLastErr() { return last_err; }
            public string GetLastErrDesc() { return last_err_desc ?? string.Empty; }

            public Error last_err;
            public string last_err_desc;

            public T value;
        }

        public static RawImgRst MakeRawImgFromImgFile(string file_path)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static RawImgRst MakeRawImgFromImgFileBytes(byte[] img_file_bytes)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static Rst<Bitmap> MakeBgrBitmapFromImgFileBytes(byte[] img_file_bytes)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    //
    // ImgUtil
    //

    public class ImgUtil
    {
        const string NOT_IMPL = "FaceSDK stub: the real implementation ships with the SDK package.";

        public static BlobImg CropImgByAspectRatio(byte[] pixels_bgr, int width, int height,
            float target_aspect = 720.0F / 1280.0F)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static void SaveBGRAsBmp(string file_path, byte[] pixels_bgr, int width, int height)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    //
    // Passive liveness gate thresholds.
    // NOTE: placeholder values — the tuned production numbers ship with the SDK.
    //

    public class LivenessThreshold
    {
        // check camera source
        public const int SRC_IMG_W_MIN = 1280;
        public const int SRC_IMG_H_MIN = 720;

        // minimum face width for liveness
        public const float FACE_RATIO_WIDTH_MIN = 0.25F;
        public const float FACE_RATIO_WIDTH_MAX = 0.6F;

        // for left-right cropped img by aspect 720:1280
        public const float SRC_CROP_ASPECT_HEIGHT = 720F;
        public const float SRC_CROP_ASPECT_WIDTH = 1280F;

        public const float FACE_RATIO_WIDTH_MIN_FOR_CROP_IMG = 0.4F;
        public const float FACE_RATIO_WIDTH_MAX_FOR_CROP_IMG = 0.6F;

        // image center positioned ratio for close to the center
        public const float CENTER_FACE_W_H_POS_RATIO = 0.1F;

        // yaw/pitch/roll : min,max
        public static readonly float[] FACE_YAW = { -20.0F, 20.0F };
        public static readonly float[] FACE_PITCH = { -10.0F, 25.0F };
        public static readonly float[] FACE_ROLL = { -20.0F, 20.0F };

        // attribute
        public const float ATTR_FACE_IS_MASKED = FaceSDK.Params.THRESHOLD_ATTR_FACE_IS_MASKED;

        // occlusion: score > ATTR_FACE_FINE_OCCLUSION
        public const float ATTR_FACE_FINE_OCCLUSION = 0.5F;
        public static bool IsFaceFineOcclusion(float score) { return score > ATTR_FACE_FINE_OCCLUSION; }

        // Face Feature Quality, [0] unmasked face, [1] masked face
        public static readonly float[] FACE_FEATURE_QUALITY = { 50.0F, 50.0F };

        // Face Antispoofing Quality - FAS-QUALITY
        public const float FACE_ANTISPOOFING_QUALITY = 0.5F;

        // BGR Image Liveness
        public const float BGR_IMG_LIVENESS = 0.5F;
    }

    public class PassiveLivness
    {
        const string NOT_IMPL = "FaceSDK stub: the real implementation ships with the SDK package.";

        public enum Error : int
        {
            NoError,

            NoFaceDetected,             // no face in given source image
            MultipleFacesDetected,      // only one face is allowed during liveness operation

            FaceIsMasked,               // take off face mask

            // check detected face width/height against the entire image width/height
            FaceWidthRatioSmall,        // move closer to the camera
            FaceWidthRatioLarge,        // move away from the camera

            // to prevent liveness distortion caused by camera distortion, centre the face
            FacePosXRatioToCenter,
            FacePosYRatioToCenter,

            FaceYaw,                    // failed to check face Yaw range
            FacePitch,                  // failed to check face Pitch range
            FaceRoll,                   // failed to check face Roll range

            FaceQuality,                // face detection quality too low to extract a 512D feature vector

            FASQuality,                 // FAS (Face AntiSpoofing) quality too low

            BGRImageQuality,            // BGR image quality too low for liveness operation

            // FaceSDK
            FaceSDKInvalid = 1000,      // Invalid FaceSDK
            FaceSDKOperFail = 1100,     // see FaceSDK last error

            Unknown = 9999,
        }

        public class Result
        {
            public Error liveness_err_ = Error.NoError;
            public string liveness_err_desc_ = "";

            public bool is_liveness_ok_ = false;
            public float liveness_confidence_ = 0.0f;

            public FaceSDK.Face detected_face_;
            public int detected_face_cnt_;

            public bool is_face_masked_ = false;

            public byte[] bgr_pixels_;
            public int bgr_width_;
            public int bgr_height_;

            public String last_err_msg1_ = "";
            public String last_err_msg2_ = "";
            public String last_err_msg3_ = "";

            public void SetErrMsg(Error err = Error.NoError, String msg1 = "", String msg2 = "", String msg3 = "")
            {
                liveness_err_ = err;
                liveness_err_desc_ = msg1;

                is_liveness_ok_ = false;

                last_err_msg1_ = msg1;
                last_err_msg2_ = msg2;
                last_err_msg3_ = msg3;
            }
        }

        public static Result DoPassiveLiveness(
            FaceSDK facesdk,
            byte[] img_pixels_bgr, int img_width, int img_height,
            float threshold_face_width_ratio_min,
            float threshold_face_width_ratio_max,
            bool use_continuous_img_face_detect,
            bool check_valid_face_width_ratio_for_liveness,
            bool check_center_face_position_in_img,
            float threshold_center_face_w_h_pos_ratio,
            bool check_face_yaw_pitch_roll,
            float[] threshold_face_yaw,
            float[] threshold_face_pitch,
            float[] threshold_face_roll,
            bool check_face_is_masked,
            bool check_feature_quality,
            float[] threshold_feature_quality,
            bool check_fas_quality,
            float threshold_fas_quality,
            float threshold_bgr_img_liveness,
            bool check_multiple_faces = true
        )
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }
}
