using OpenCvSharp;
using System;
using System.Collections.Generic;
using Point = OpenCvSharp.Point;
using Size = OpenCvSharp.Size;

namespace FaceSDKSample.sample
{
    public partial class DemoClientServer : Demo
    {
        public static Point DrawBestLiveShots(BestLivShots best_liv_shots, 
            Mat render, Point pt_base, int shot_siz_w, int shot_siz_h)
        {
            Size shot_siz = new Size(shot_siz_w, shot_siz_h);
            Scalar shot_txt_color = new Scalar(255, 0, 0);

            if (best_liv_shots.shot_cnt > 0)
            {
                best_liv_shots.Draw(render, pt_base, 0, "#1", shot_siz, shot_txt_color, 0.4);
            }

            if (best_liv_shots.shot_cnt > 1)
            {
                best_liv_shots.Draw(render, pt_base + new Point(5 + shot_siz.Width, 0), 1, "#2", shot_siz, shot_txt_color, 0.4);
            }

            if (best_liv_shots.shot_cnt > 2)
            {
                best_liv_shots.Draw(render, pt_base + new Point(0, shot_siz.Height), 2, "#3", shot_siz, shot_txt_color, 0.4);
            }

            if (best_liv_shots.shot_cnt > 3)
            {
                best_liv_shots.Draw(render, pt_base + new Point(5 + shot_siz.Width, shot_siz.Height), 3, "#4", shot_siz, shot_txt_color, 0.4);
            }

            pt_base += new Point(0, shot_siz.Height + 150);

            return pt_base;
        }
    }
}
