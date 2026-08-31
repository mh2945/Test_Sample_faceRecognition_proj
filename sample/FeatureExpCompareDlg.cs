using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using System.Windows.Forms;
using Alchera.FaceSDK;

namespace FaceSDKSample.sample
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using System.Drawing.Drawing2D;
    using System.Drawing.Imaging;
    using Alchera.FaceSDK;

    internal class FeatureExpCompareDlg : Form
    {
        private const int WM_APP = 0x8000;
        private const int WMU_COMPARE = WM_APP + 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private PictureBox pb_left_;
        private PictureBox pb_right_;

        private Button btn_ok_;
        private Button btn_close_;
        private Button btn_compare_;

        private readonly string left_path_;
        private readonly string right_path_;

        // Offscreen bitmaps (same size as source images)
        private Bitmap left_offscreen_;
        private Bitmap right_offscreen_;

        public FeatureExpCompareDlg(string title, string leftImagePath, string rightImagePath)
        {
            if (string.IsNullOrWhiteSpace(leftImagePath))
                throw new ArgumentException("leftImagePath is null or empty.", nameof(leftImagePath));
            if (string.IsNullOrWhiteSpace(rightImagePath))
                throw new ArgumentException("rightImagePath is null or empty.", nameof(rightImagePath));

            left_path_ = leftImagePath;
            right_path_ = rightImagePath;

            InitializeComponent();

            Text = string.IsNullOrWhiteSpace(title) ? "Compare" : title;

            LoadOffscreens();
            pb_left_.Invalidate();
            pb_right_.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                left_offscreen_?.Dispose();
                left_offscreen_ = null;

                right_offscreen_?.Dispose();
                right_offscreen_ = null;
            }

            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WMU_COMPARE)
            {
                CompareSelection();
                return; // handled
            }

            base.WndProc(ref m);
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;

            ClientSize = new Size(1200, 720);

            int margin = 10;
            int gap = 10;

            int totalW = ClientSize.Width;
            int totalH = ClientSize.Height;

            int bottomAreaH = 52;
            int imagesH = totalH - margin * 2 - bottomAreaH;

            int halfW = (totalW - margin * 2 - gap) / 2;

            int leftX = margin;
            int rightX = margin + halfW + gap;
            int topY = margin;

            pb_left_ = new PictureBox
            {
                Location = new Point(leftX, topY),
                Size = new Size(halfW, imagesH),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.ControlDark
            };
            pb_left_.Paint += (_, e) => DrawScaledOffscreen(e.Graphics, pb_left_.ClientRectangle, left_offscreen_);

            pb_right_ = new PictureBox
            {
                Location = new Point(rightX, topY),
                Size = new Size(halfW, imagesH),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.ControlDark
            };
            pb_right_.Paint += (_, e) => DrawScaledOffscreen(e.Graphics, pb_right_.ClientRectangle, right_offscreen_);

            int btnY = margin + imagesH + 10;

            btn_compare_ = new Button
            {
                Text = "Compare",
                Size = new Size(90, 30),
                Location = new Point(totalW - margin - 290, btnY)
            };
            btn_compare_.Click += (_, __) => PostMessage(this.Handle, WMU_COMPARE, IntPtr.Zero, IntPtr.Zero);

            btn_ok_ = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Size = new Size(90, 30),
                Location = new Point(totalW - margin - 195, btnY)
            };

            btn_close_ = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.Cancel,
                Size = new Size(90, 30),
                Location = new Point(totalW - margin - 100, btnY)
            };

            Controls.AddRange(new Control[]
            {
            pb_left_,
            pb_right_,
            btn_compare_,
            btn_ok_,
            btn_close_
            });

            AcceptButton = btn_ok_;
            CancelButton = btn_close_;

            // Optional: context menu "Compare" on both picture boxes
            InitializeContextMenu(pb_left_);
            InitializeContextMenu(pb_right_);
        }

        private void InitializeContextMenu(Control ctrl)
        {
            var menu = new ContextMenuStrip();

            var miCompare = new ToolStripMenuItem("Compare");
            miCompare.Click += (_, __) =>
            {
                PostMessage(this.Handle, WMU_COMPARE, IntPtr.Zero, IntPtr.Zero);
            };

            menu.Items.Add(miCompare);
            ctrl.ContextMenuStrip = menu;
        }

        private void LoadOffscreens()
        {
            left_offscreen_?.Dispose();
            right_offscreen_?.Dispose();

            left_offscreen_ = LoadOffscreenBitmap(left_path_);
            right_offscreen_ = LoadOffscreenBitmap(right_path_);
        }

        private static Bitmap LoadOffscreenBitmap(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found.", path);

            // DO NOT USE !
            // 1] Image.FromStream(File.ReadAll('a.png'))
            // 2] new Bitmap(File.ReadAll('a.png'))

            byte[] img_file_bytes = File.ReadAllBytes(path);

            RawImageCV.Rst<Bitmap> img_bitmap_rst 
                = RawImageCV.MakeBgrBitmapFromImgFileBytes(img_file_bytes);

            if(img_bitmap_rst.IsErr())
            {
                throw new FileNotFoundException($"Can't load Image file.. {img_bitmap_rst.GetLastErrDesc()}, path={path}");
            }

            using (Bitmap src = img_bitmap_rst.value)
            {
                var offscreen = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

                using (var g = Graphics.FromImage(offscreen))
                {
                    g.Clear(Color.Transparent);

                    g.DrawImageUnscaled(src, 0, 0);

                    //// 2) Draw a 10x10 red square at center
                    //const int rectW = 10;
                    //const int rectH = 10;

                    //int x = (offscreen.Width - rectW) / 2;
                    //int y = (offscreen.Height - rectH) / 2;

                    //using (var brush = new SolidBrush(Color.Red))
                    //    g.FillRectangle(brush, x, y, rectW, rectH);
                }

                return offscreen;
            }
        }

        private static void DrawScaledOffscreen(Graphics g, Rectangle dstRect, Bitmap offscreen)
        {
            g.Clear(SystemColors.ControlDark);

            if (offscreen == null || dstRect.Width <= 0 || dstRect.Height <= 0)
                return;

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.None;

            // Aspect-fit (recommended for compare UI)
            Rectangle fit = GetAspectFitRect(offscreen.Width, offscreen.Height, dstRect);
            if (fit.Width <= 0 || fit.Height <= 0)
                return;

            g.DrawImage(offscreen, fit);
        }

        private static Rectangle GetAspectFitRect(int srcW, int srcH, Rectangle dst)
        {
            if (srcW <= 0 || srcH <= 0 || dst.Width <= 0 || dst.Height <= 0)
                return Rectangle.Empty;

            double sx = (double)dst.Width / srcW;
            double sy = (double)dst.Height / srcH;
            double s = Math.Min(sx, sy);

            int w = (int)Math.Round(srcW * s);
            int h = (int)Math.Round(srcH * s);
            int x = dst.X + (dst.Width - w) / 2;
            int y = dst.Y + (dst.Height - h) / 2;

            return new Rectangle(x, y, w, h);
        }

        private void CompareSelection()
        {
            // DO NOT USE !
            // 1] Image.FromStream(File.ReadAll('a.png'))
            // 2] new Bitmap(File.ReadAll('a.png'))

            RawImageCV.RawImgRst raw_img_rst1 = RawImageCV.MakeRawImgFromImgFile(left_path_);
            RawImageCV.RawImgRst raw_img_rst2 = RawImageCV.MakeRawImgFromImgFile(right_path_);

            FaceSDK fsdk = FaceSDK.Instance();

            FaceSDK.RstFeatureVec feature_vec1_rst 
                = fsdk.ExtractFeatureVecFromImg(raw_img_rst1.pixel_buf, raw_img_rst1.w, raw_img_rst1.h);

            if (feature_vec1_rst.IsErr())
            {
                MessageBox.Show(this, $"[ERR] err={feature_vec1_rst.last_err}, err_desc={feature_vec1_rst.last_err_desc}",
                                    $"err img-{left_path_}", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }

            FaceSDK.RstFeatureVec feature_vec2_rst 
                = fsdk.ExtractFeatureVecFromImg(raw_img_rst2.pixel_buf, raw_img_rst2.w, raw_img_rst2.h);

            if (feature_vec2_rst.IsErr())
            {
                MessageBox.Show(this, $"[ERR] err={feature_vec2_rst.last_err}, err_desc={feature_vec2_rst.last_err_desc}",
                                    $"err img-{right_path_}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            FaceSDK.Rst feature_dist_rst = fsdk.CompareFeatureVecs(feature_vec1_rst.feature_vec, feature_vec2_rst.feature_vec);

            if (feature_dist_rst.IsErr())
            {
                MessageBox.Show(this, $"[ERR] err={feature_dist_rst.last_err}, err_desc={feature_dist_rst.last_err_desc}",
                    "Compare Dist Err", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return;
            }
            else
            {
                MessageBox.Show(this, $"[INFO] compare dist={feature_dist_rst.GetFeatureDist()}", 
                    "Compare", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

}
