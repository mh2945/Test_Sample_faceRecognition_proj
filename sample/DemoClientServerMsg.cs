using Alchera.FaceSDK.FaceServer;
using System;

namespace FaceSDKSample.sample
{
    public partial class DemoClientServer : Demo
    {
        public int DispatchMessage(RunContext ctx)
        {
            while (true)
            {
                UserPrm.Msg m = ctx.user_prm.PopMsg();

                if (m == null)
                    break;

                switch (m.idx_)
                {
                    case 1000:
                        {
                            if (m.prm0_ != null)
                                ctx.user_prm.prop_settings_ = m.prm0_ as SampleProp.Settings;

                            if (m.prm1_ != null)
                                ctx.user_prm.prop_demo_ = m.prm1_ as SampleProp.DemoClientServerProp;

                            if(m.prm2_ != null)
                                ctx.user_prm.prop_faceinfo_ = m.prm2_ as SampleProp.SamplePropFaceInfo;
                        }
                        break;

                    case 1100:
                        {
                            if(ctx.user_prm.cam_ != null)
                            {
                               ctx.user_prm.cam_.Exec("clear_cap_frame");
                            }
                        }
                        break;

                    case 2000:
                        {
                            string cate = "";
                            string msg = "";

                            if (m.prm0_ != null)
                                cate = m.prm0_ as String;

                            if (m.prm1_ != null)
                                msg = m.prm1_ as String;

                            if (cate == "api_liv_muliti")
                            {
                                ctx.ui_liv_multi_st.Add(msg);
                            }
                            else if (cate == "api_compare")
                            {
                                ctx.ui_api_compare_st.Add(msg);
                            }
                        }
                        break;

                    case 3000: 
                        {
                            string cate = "";

                            if (m.prm0_ != null)
                            {
                                cate = m.prm0_ as String;
                            }

                            DemoStage cur_stage = GetDemoStage();

                            if (cur_stage == DemoStage.BGRImageLivenessMulitframe)
                            {
                                switch (cate)
                                {
                                case "clientonly_liv_muliti":
                                    {
                                        String client_only_liv_rst = m.prm1_ as String;

                                        if (client_only_liv_rst.StartsWith("last_err="))
                                        {
                                            // fsdk.bgr_image_liveness failed
                                            // cleanup liveness result
                                            ctx.best_liv_shots.Clear();
                                            ctx.ui_liv_multi_st.Clear();

                                            ctx.ui_finish_liveness_multiframe_task = null;

                                            // set previous stage
                                            SetDemoStage(DemoStage.DoPassiveLivenessAndCollectLivenessBestShot);

                                        }
                                        else
                                        {
                                            SetDemoStage(DemoStage.FaceCompare);
                                            break;
                                        }
                                    }
                                    break;

                                case "api_liv_muliti":
                                    {
                                        LivenessMulitFrameApi api_rst
                                            = m.prm1_ as LivenessMulitFrameApi;

                                        if (api_rst.IsLivenessPass())
                                        {
                                            SetDemoStage(DemoStage.FaceCompare);
                                            break;
                                        }
                                        else
                                        {
                                            // failed server liveness
                                            // cleanup liveness result
                                            ctx.best_liv_shots.Clear();
                                            ctx.ui_liv_multi_st.Clear();

                                            ctx.api_liveness_multiframe_task = null;
                                            ctx.api_liveness_best4 = null;

                                            // set reset stage
                                            SetDemoStage(DemoStage.FinalResult);
                                        }
                                    }
                                    break;

                                default:
                                    break;
                                }
                            }
                            else if (cur_stage == DemoStage.FaceCompare)
                            {
                                switch (cate)
                                {
                                    case "compare_clienonly_done":
                                    case "compare_prepare_img":
                                    case "compare_chk_faceratiow":
                                        {
                                            ctx.api_compare = null;
                                            ctx.ui_api_compare_st.Clear();

                                            SetDemoStage(DemoStage.FinalResult);
                                        }
                                        break;

                                    case "compare_api_done":
                                        {
                                            Api api_rst = m.prm1_ as Api;

                                            if (api_rst != null)
                                            {
                                                if (api_rst.IsHttpResponseOK())
                                                {
                                                    FaceCompareApi.Res res
                                                        = api_rst.GetResponse() as FaceCompareApi.Res;

                                                    //if (res.IsLive)
                                                    //{
                                                    //    SetDemoStage(DemoStage.ReqeustSvrFaceCompare);
                                                    //    break;
                                                    //}
                                                }
                                            }
                                            else
                                            {
                                                // calling api_compare is skipped
                                                // > by no face is detected ..
                                            }

                                            ctx.api_compare = null;
                                            ctx.ui_api_compare_st.Clear();

                                            SetDemoStage(DemoStage.FinalResult);
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }

                        }
                        break;

                    case 4000: // record start
                        {
                            string cate = "";

                            if (m.prm0_ != null)
                                cate = m.prm0_ as String;

                            if (cate == "start")
                            {
                                if (ctx.rec_ctx.rec_is_recording)
                                    break;

                                SampleProp.Settings prop_settings = m.prm1_ as SampleProp.Settings;
                                ctx.rec_ctx.rec_status_callback = m.prm2_ as Action<long, string, int>;

                                string rec_file_path 
                                    = prop_settings.EXEC_PATH + "\\" + prop_settings.REC_PATH 
                                        + "\\" + prop_settings.UI_REC_FILENAME_PREFIX 
                                        + DateTime.Now.ToString(prop_settings.REC_FILE_TS_FMT);

                                ctx.rec_ctx.rec_file_path = rec_file_path;

                                ctx.rec_ctx.rec_fps = prop_settings.UI_REC_FPS;
                                ctx.rec_ctx.rec_rolling_siz_mb = prop_settings.UI_REC_ROLL_SIZ_MB;
                                ctx.rec_ctx.rec_is_recording = true;
                            }
                            else if(cate == "stop")
                            {
                                if (ctx.rec_ctx.rec_recorder != null)
                                {
                                    ctx.rec_ctx.rec_recorder.Close();
                                    ctx.rec_ctx.rec_recorder = null;
                                }
                                                               
                                ctx.rec_ctx.rec_is_recording = false;
                            }

                        }
                        break;

                    case 9999:
                        {
                            if (ctx != null)
                            {
                                if(ctx.user_prm != null)
                                {
                                    if(ctx.user_prm.cam_ != null) {
                                        ctx.user_prm.cam_.Exec("clear_cap_frame");
                                    }
                                }

                                if (ctx.rec_ctx.rec_recorder != null)
                                {
                                    ctx.rec_ctx.rec_recorder.Close();
                                    ctx.rec_ctx.rec_recorder = null;
                                }

                                ctx.rec_ctx.rec_is_recording = false;
                            }
                        }
                        break;

                    default:
                        break;
                }
            }

            return 0;
        }
    }
}
