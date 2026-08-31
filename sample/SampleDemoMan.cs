using FaceSDKSample.sample;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceSDKSample
{
    public partial class Sample
    {
        Demo passsive_liveness_best4_feature_faceserver_;

        public bool RecordDemo(String demo_name, Action<long, string, int> rec_status_callback,
            string rec_cmd, SampleProp.Prop prop_settings)
        {
            if (demo_name == DemoClientServer._NAME)
            {
                //VideoRec rec = new FaceSDKSample.VideoRec(
                //                "fsdksamp_",
                //                1280, 760,
                //                30,
                //                1024);

                //rec.Roll();

                Demo demo = passsive_liveness_best4_feature_faceserver_;

                if (demo == null)
                    return false;

                DemoClientServer.UserPrm user_prm
                   = demo.GetUserPrm() as DemoClientServer.UserPrm;

                user_prm.PostMsg(4000, rec_cmd, prop_settings, rec_status_callback);

                return true;
            }

            return false;
        }

        public readonly Action def_poll_exiting_ = () => { };

        public bool StopDemo(String demo_name, Action<uint, int> poll_exiting, int timeout_sec=30)
        {
            if (demo_name == DemoClientServer._NAME)
            {
                Demo demo = passsive_liveness_best4_feature_faceserver_;

                if (demo == null || demo.IsTerminated() || demo.IsExiting())
                    return true;

                DemoClientServer.UserPrm user_prm
                   = demo.GetUserPrm() as DemoClientServer.UserPrm;

                if(user_prm != null)
                {
                    user_prm.PostMsg(9999, "exiting");
                }

                passsive_liveness_best4_feature_faceserver_.SetExit();

                Stopwatch wait_start = new Stopwatch();
                Stopwatch wait_watch = new Stopwatch();
                wait_watch.Start();

                bool is_timeoutted = false;

                while(true)
                {
                    TimeSpan elapsed = wait_watch.Elapsed - wait_start.Elapsed;

                    poll_exiting((uint)elapsed.TotalMilliseconds, 1);

                    if(passsive_liveness_best4_feature_faceserver_.Join(100))
                    {
                        poll_exiting((uint)((wait_watch.Elapsed - wait_start.Elapsed).TotalMilliseconds), 0);
                        break;
                    }

                    if (elapsed.Duration().Seconds > timeout_sec)
                    {
                        is_timeoutted = true;
                        poll_exiting((uint)((wait_watch.Elapsed - wait_start.Elapsed).TotalMilliseconds), -1);
                        break;
                    }
                }

                wait_watch.Stop();

                if(is_timeoutted)
                {
                    MessageBox.Show("[ERR] Failed to terminate demo=" + demo.name_ 
                        + ": timeout occured, sec="+ timeout_sec,
                        "Error on terminate Demo", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                    
                    return false;
                }
                else
                {
                    passsive_liveness_best4_feature_faceserver_ = null;
                }

                return true;
            }

            return false;
        }

        public bool StartDemo(String demo_name, Action exit_callback,             
            SampleProp.Prop prop_settings, SampleProp.Prop prop_passive_liveness, 
            SampleProp.Prop prop_ui)
        {
            if (demo_name == DemoClientServer._NAME)
            {
                if (passsive_liveness_best4_feature_faceserver_ != null)
                    return false;

                DemoClientServer.UserPrm user_prm
                    = new DemoClientServer.UserPrm
                    {
                        fsdk_ = Sample.fsdk,
                        cam_ = Sample.cam,

                        ui_onexit_ = exit_callback
                    };

                passsive_liveness_best4_feature_faceserver_
                    = new DemoClientServer(user_prm);
                                
                passsive_liveness_best4_feature_faceserver_.Start();

                user_prm.PostMsg(1000, prop_settings, prop_passive_liveness, prop_ui);
                user_prm.PostMsg(1100); // clear all input frames

                return true;
            }

            return false;
        }
                

        //
        // Prop Evenet
        //

        public bool PostDemoPropEvent(SampleProp.Prop prop, string evt_name, object s, object evt_args)
        {
            if(passsive_liveness_best4_feature_faceserver_ != null)
            {
                // e.PropertyName

                DemoClientServer.UserPrm user_prm 
                    = passsive_liveness_best4_feature_faceserver_.GetUserPrm() as DemoClientServer.UserPrm;

                user_prm?.PostMsg(
                    1000, 
                    (prop as SampleProp.Settings != null) ? prop : null,
                    (prop as SampleProp.DemoClientServerProp != null) ? prop : null
                );
            }


            return true;
        }
    }
        

}
