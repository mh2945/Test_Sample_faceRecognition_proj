using System;
using System.Threading;


namespace FaceSDKSample
{
    //
    // Demo
    //

    public abstract class Demo
    {
        public enum CallBackEvt
        {
            None = 0,
            Start = 1,
            Loop = 2,
            Exit = 3,
        }

        //public delegate int CallBack(CallBackEvt type, Demo demo, int result, Object obj);

        readonly Thread thread_;
        public readonly String name_;
        readonly Object user_prm_;
        private bool force_exit_ = false;
        //CallBack callback_;

        Demo() { } // disable

        public Demo(String name, Object user_prm)
        {
            name_ = name;
            user_prm_ = user_prm;

            thread_ = new Thread(new ThreadStart(Run));
            thread_.Name = name_;
        }

        ~Demo()
        {
            if (IsThreadRunning())
            {
                force_exit_ = true;
                thread_.Join();
            }
        }

        public void Start()
        {
            thread_.Start();
        }

        public bool Join(int timeout_ms = 1000)
        {
            SetExit();

            return thread_.Join(timeout_ms);
        }

        public bool IsThreadRunning()
        {
            return thread_.ThreadState == System.Threading.ThreadState.Running ? true : false;
        }

        public bool IsRunning()
        {
            if (!IsThreadRunning())
                return false;

            if (force_exit_)
                return false;

            return true;
        }

        public void SetExit()
        {
            if (force_exit_)
                return;

            force_exit_ = true;
        }

        public bool IsExiting()
        {
            return force_exit_ && !thread_.IsAlive;
        }

        public bool IsTerminated()
        {
            return IsThreadRunning() ? false : true;
        }

        //protected int callback(CallBackEvt type, Demo demo, int result, Object obj)
        //{
        //    if (callback_ == null)
        //        return 0;

        //    return callback_(type, demo, result, obj);
        //}

        //public void SetCallback(CallBack cb) { callback_ = cb; }

        protected abstract void Run();
        
        public Object GetUserPrm() { return user_prm_; }

    }


}
