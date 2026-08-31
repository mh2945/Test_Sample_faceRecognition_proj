using Alchera.FaceSDK.FaceServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alchera.FaceSDK.FaceSDK;

namespace FaceSDKSample.sample
{
    public partial class DemoClientServer : Demo
    {
        public class RecContext
        {
            public bool rec_is_recording = false;
            public Action<long, string, int> rec_status_callback = null;
            public string rec_file_path = "";
            public int rec_fps = 30;
            public long rec_rolling_siz_mb = 1024;
            public VideoRec rec_recorder = null;
        }

        public class RunContext
        {
            public RecContext rec_ctx;

            public static long get_utc_ms()
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public static string get_local_time_str()
            {
               long utc_ms = get_utc_ms();
               var dto = DateTimeOffset.FromUnixTimeMilliseconds(utc_ms);
               return dto.LocalDateTime.ToString("yyyyMMdd HH:mm:ss.fff");
            }


            public readonly UserPrm user_prm;

            RunContext() { }
            public RunContext(UserPrm user_prm)
            {
                this.user_prm = user_prm;

                ui_liv_msg = (_msg) => {
                    user_prm.PostMsg(2000, "api_liv_muliti", (object)_msg);
                };
                ui_fin_liveness_check_act = (_cate, _prm) => {
                    user_prm.PostMsg(3000, _cate, _prm);
                };

                ui_api_compare_msg = (_msg) => {
                    user_prm.PostMsg(2000, "api_compare", (object)_msg);
                };

                ui_fin_compare_check_act = (_cate, _prm) => {
                    user_prm.PostMsg(3000, _cate, _prm);
                };
            }
            
            //
            // time
            //

            public long cur_frame_utc = get_utc_ms();                        

            //
            // ui/api - liveness multiframe
            //
            // do not clear in loop
            public Action<string> ui_liv_msg; 
            public Action<string, object> ui_fin_liveness_check_act;

                // clear
            public BestLivShots best_liv_shots = new BestLivShots();
            public List<string> ui_liv_multi_st = new List<string>();            
            public Task ui_finish_liveness_multiframe_task;

            public Api api_liveness_best4;
            public Task<Api> api_liveness_multiframe_task;


            //
            // ui/api - face compare
            //

            public bool cmp_prepare_img_performed = false;
            public string cmp_prepare_img_err_msg = "";

            // do not clear
            public Action<string> ui_api_compare_msg;
            public Action<string, object> ui_fin_compare_check_act;

                // clear per stage
            public List<string> ui_api_compare_st = new List<string>();            
            public Task ui_finish_compare_task;


            public BlobImg cmp_src_bgr_from_img_file;

            // stg.faceserver/compare/validation-config
            public float threshold_compare_face_w_siz_ratio_min = 0.25F;
            public float threshold_compare_face_w_siz_ratio_max = 1.0F;

            // [mode=ClientOnly]
            // [mode=FaceServer] cropped and resized face bgr img from image_file
            public BlobImg cmp_src_bgr;
            public Box cmp_src_face_box; // from compare image file
            public bool cmp_src_bgr_err = false;

            // [mode=ClientOnly]
            // [mode=FaceServer] entire lastest bestshot camera image
            // > (no-cropped, crop and verified will be performed at faceserver)
            public BlobImg cmp_dst_bgr;
            public Box cmp_dst_face_box;
            public bool cmp_dst_bgr_err = false;

            // [mode=FaceServer]
            public Api api_compare;
            public Task<Api> api_compare_task;
        }

    }
}
