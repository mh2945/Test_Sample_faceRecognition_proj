using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceSDKSample.sample
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;

    public class FeatureExplorerDlg : Form
    {
        private const int WM_APP = 0x8000;
        private const int WMU_COMPARE = WM_APP + 1;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private TextBox txt_dir_left_;
        private TextBox txt_dir_right_;

        private Button btn_load_left_;
        private Button btn_load_right_;

        private Button btn_compare_;
        private Button btn_close_;

        private ListView lv_left_;
        private ListView lv_right_;

        private readonly string dir_path_left_;
        private readonly string dir_path_right_;

        public FeatureExplorerDlg(string title, string dirLeft, string dirRight)
        {
            if (string.IsNullOrWhiteSpace(dirLeft))
                throw new ArgumentException("dirLeft is null or empty.", nameof(dirLeft));
            if (string.IsNullOrWhiteSpace(dirRight))
                throw new ArgumentException("dirRight is null or empty.", nameof(dirRight));

            dir_path_left_ = dirLeft;
            dir_path_right_ = dirRight;

            InitializeComponent();

            Text = title;

            txt_dir_left_.Text = dir_path_left_;
            txt_dir_right_.Text = dir_path_right_;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WMU_COMPARE)
            {
                CompareSelected();
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

            ClientSize = new Size(1100, 620);

            // Layout constants
            int margin = 10;
            int top = 10;
            int rowH = 28;
            int gap = 10;

            int totalW = ClientSize.Width;
            int totalH = ClientSize.Height;

            int halfW = (totalW - margin * 2 - gap) / 2;

            int leftX = margin;
            int rightX = margin + halfW + gap;

            // Top row: left dir + load
            txt_dir_left_ = new TextBox
            {
                Location = new Point(leftX, top),
                Size = new Size(halfW - 70, rowH),
                ReadOnly = true
            };

            btn_load_left_ = new Button
            {
                Text = "Load",
                Location = new Point(leftX + (halfW - 60), top),
                Size = new Size(60, rowH)
            };
            btn_load_left_.Click += (_, __) => LoadFilesToListView(dir_path_left_, lv_left_);

            // Top row: right dir + load
            txt_dir_right_ = new TextBox
            {
                Location = new Point(rightX, top),
                Size = new Size(halfW - 70, rowH),
                ReadOnly = true
            };

            btn_load_right_ = new Button
            {
                Text = "Load",
                Location = new Point(rightX + (halfW - 60), top),
                Size = new Size(60, rowH)
            };
            btn_load_right_.Click += (_, __) => LoadFilesToListView(dir_path_right_, lv_right_);

            // Middle: two ListViews
            int listTop = top + rowH + 8;
            int bottomAreaH = 46; // compare/close button row
            int listH = totalH - listTop - bottomAreaH - margin;

            lv_left_ = CreateFilesListView(new Point(leftX, listTop), new Size(halfW, listH));
            lv_right_ = CreateFilesListView(new Point(rightX, listTop), new Size(halfW, listH));

            // Bottom buttons
            int bottomY = listTop + listH + 8;

            btn_compare_ = new Button
            {
                Text = "Compare",
                Location = new Point(totalW - margin - 200, bottomY),
                Size = new Size(95, 30)
            };
            // Compare triggers WMU_COMPARE via PostMessage (as you requested)
            btn_compare_.Click += (_, __) => PostMessage(this.Handle, WMU_COMPARE, IntPtr.Zero, IntPtr.Zero);

            btn_close_ = new Button
            {
                Text = "Close",
                Location = new Point(totalW - margin - 100, bottomY),
                Size = new Size(95, 30),
                DialogResult = DialogResult.OK
            };

            Controls.AddRange(new Control[]
            {
            txt_dir_left_, btn_load_left_,
            txt_dir_right_, btn_load_right_,
            lv_left_, lv_right_,
            btn_compare_, btn_close_
            });

            AcceptButton = btn_compare_;
            CancelButton = btn_close_;

            // Context menus for both listviews
            InitializeContextMenu(lv_left_);
            InitializeContextMenu(lv_right_);

            // Initial load (optional)
            LoadFilesToListView(dir_path_left_, lv_left_);
            LoadFilesToListView(dir_path_right_, lv_right_);
        }

        private static ListView CreateFilesListView(Point location, Size size)
        {
            var lv = new ListView
            {
                Location = location,
                Size = size,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };

            lv.Columns.Add("File Name", 240, HorizontalAlignment.Left);
            lv.Columns.Add("Size (bytes)", 110, HorizontalAlignment.Right);
            lv.Columns.Add("Resolution", 120, HorizontalAlignment.Left);
            lv.Columns.Add("Last Write", 160, HorizontalAlignment.Left);

            return lv;
        }

        private void InitializeContextMenu(ListView lv)
        {
            var menu = new ContextMenuStrip();

            var miCompare = new ToolStripMenuItem("Compare");
            miCompare.Click += (_, __) =>
            {
                // Signal compare by posting message to dialog message queue
                PostMessage(this.Handle, WMU_COMPARE, IntPtr.Zero, IntPtr.Zero);
            };

            menu.Items.Add(miCompare);
            lv.ContextMenuStrip = menu;
        }

        private void LoadFilesToListView(string dirPath, ListView lv)
        {
            lv.BeginUpdate();
            lv.Items.Clear();

            try
            {
                if (!Directory.Exists(dirPath))
                {
                    MessageBox.Show(
                        this,
                        $"Directory not found:\n{dirPath}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                var exts = new[] { ".png", ".jpg", ".jpeg" };

                var files = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => exts.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    var fi = new FileInfo(file);

                    string resolution = "-";
                    try
                    {
                        using (var img = Image.FromFile(file))
                            resolution = $"{img.Width} x {img.Height}";
                    }
                    catch
                    {
                        // ignore broken/locked image
                    }

                    var item = new ListViewItem(fi.Name);
                    item.SubItems.Add(fi.Length.ToString());
                    item.SubItems.Add(resolution);
                    item.SubItems.Add(fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"));
                    item.Tag = fi.FullName; // Full path stored here

                    lv.Items.Add(item);
                }

                if (lv.Items.Count == 0)
                {
                    lv.Items.Add(new ListViewItem("(no png/jpg files)"));
                }
            }
            finally
            {
                lv.EndUpdate();
            }
        }

        private void CompareSelected()
        {
            var leftPath = GetSelectedFilePath(lv_left_);
            var rightPath = GetSelectedFilePath(lv_right_);

            if (string.IsNullOrEmpty(leftPath) || string.IsNullOrEmpty(rightPath))
            {
                MessageBox.Show(
                    this,
                    "Select one file in each list (left and right) before comparing.",
                    "Compare",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new FeatureExpCompareDlg("FeatureCompare", leftPath, rightPath))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // confirmed
                }
            }

            //MessageBox.Show(
            //    this,
            //    $"Left:\n{leftPath}\n\nRight:\n{rightPath}",
            //    "Compare Selection",
            //    MessageBoxButtons.OK,
            //    MessageBoxIcon.Information);
        }

        private static string GetSelectedFilePath(ListView lv)
        {
            if (lv.SelectedItems.Count <= 0)
                return null;

            return lv.SelectedItems[0].Tag as string;
        }
    }
}
