using Alchera.FaceSDK;
using System;
using System.Diagnostics;
using static FaceSDKSample.Sample;

namespace FaceSDKSample
{
    //
    // SampleFaceSDKCtx
    //

    public class SampleFaceSDKCtx
    {
        public readonly FaceSDK facesdk_;

        public readonly string exec_path_;
        public readonly string mdl_path_;
        public readonly string sdk_path_;

        public readonly Stopwatch sw_face_init_;
        public readonly float elapsed_ms_init_;

        public SampleFaceSDKCtx(String exec_path, String mdl_path = "models", String sdk_path = "")
        {
            exec_path_ = exec_path;
            mdl_path_ = exec_path + "\\" + mdl_path;
            sdk_path_ = sdk_path;

            facesdk_ = FaceSDK.Instance();

            sw_face_init_ = new Stopwatch();
            sw_face_init_.Start();

            FaceSDK.Rst init_rst = facesdk_.Initialize(mdl_path, sdk_path);

            sw_face_init_.Stop();
            elapsed_ms_init_ = sw_face_init_.ElapsedMilliseconds;

            if (init_rst.IsErr())
            {
                throw new SampleException("failed to initialize FaceSDK, last_err=" +
                    init_rst.GetLastErrStr());
            }
        }

        ~SampleFaceSDKCtx()
        {
            FaceSDK.DestroyInstance();
        }

        private SampleFaceSDKCtx() { } // disable
    }
}
