using System;
using System.Drawing;
using System.Windows.Forms;

namespace FaceSDKSample.sample
{
    //
    // IP 카메라 모델 프로파일 추가 / 편집 다이얼로그.
    //
    // Form1.Designer.cs 를 건드리지 않도록 이 앱의 다른 다이얼로그(FeatureExplorerDlg 등)와
    // 같이 코드로 직접 구성한다.
    //
    public class IPCamModelEditDlg : Form
    {
        private TextBox txt_name_;
        private TextBox txt_mjpeg_port_;
        private TextBox txt_mjpeg_url_;
        private TextBox txt_rtsp_port_;
        private TextBox txt_rtsp_url_;
        private CheckBox chk_keep_user_input_;

        private Button btn_ok_;
        private Button btn_cancel_;

        // 확인을 누른 경우에만 값이 채워진다.
        public IPCamModelProfile Result { get; private set; }

        public IPCamModelEditDlg(string title, IPCamModelProfile src)
        {
            InitializeComponent();

            Text = title;

            if (src != null)
            {
                txt_name_.Text = src.name;
                txt_mjpeg_port_.Text = src.mjpeg_port;
                txt_mjpeg_url_.Text = src.mjpeg_url;
                txt_rtsp_port_.Text = src.rtsp_port;
                txt_rtsp_url_.Text = src.rtsp_url;
                chk_keep_user_input_.Checked = src.keep_user_input;
            }
        }

        private void InitializeComponent()
        {
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            // 영문 힌트가 잘리지 않도록 한글판보다 폭과 높이를 넓혔다.
            ClientSize = new Size(520, 288);

            int label_x = 12;
            int field_x = 108;
            int field_w = 400;
            int y = 14;
            const int row_h = 30;

            Func<string, Control> add_row = (caption) =>
            {
                Label lb = new Label
                {
                    Text = caption,
                    Location = new Point(label_x, y + 4),
                    Size = new Size(92, 18),
                    AutoSize = false,
                };

                TextBox tb = new TextBox
                {
                    Location = new Point(field_x, y),
                    Size = new Size(field_w, 23),
                };

                Controls.Add(lb);
                Controls.Add(tb);

                y += row_h;
                return tb;
            };

            txt_name_ = (TextBox)add_row("Name");
            txt_mjpeg_port_ = (TextBox)add_row("MJPEG PORT");
            txt_mjpeg_url_ = (TextBox)add_row("MJPEG URL");
            txt_rtsp_port_ = (TextBox)add_row("RTSP PORT");
            txt_rtsp_url_ = (TextBox)add_row("RTSP URL");

            chk_keep_user_input_ = new CheckBox
            {
                Text = "Keep user input (do not overwrite PORT / URL)",
                Location = new Point(field_x, y),
                Size = new Size(field_w, 20),
            };
            Controls.Add(chk_keep_user_input_);
            y += 26;

            Label lb_hint = new Label
            {
                Text = "URL template tokens:  {ip} = value of the IP field,  {port} = port with a\n"
                     + "leading colon (e.g. \":554\").   e.g.  rtsp://{ip}{port}/live/main\n"
                     + "An empty URL means no auto-fill on that tab.",
                Location = new Point(label_x, y),
                Size = new Size(496, 50),
                ForeColor = SystemColors.GrayText,
            };
            Controls.Add(lb_hint);
            y += 58;

            btn_ok_ = new Button
            {
                Text = "OK",
                Location = new Point(334, y),
                Size = new Size(84, 26),
                DialogResult = DialogResult.None,
            };
            btn_ok_.Click += btn_ok_Click;

            btn_cancel_ = new Button
            {
                Text = "Cancel",
                Location = new Point(424, y),
                Size = new Size(84, 26),
                DialogResult = DialogResult.Cancel,
            };

            Controls.Add(btn_ok_);
            Controls.Add(btn_cancel_);

            AcceptButton = btn_ok_;
            CancelButton = btn_cancel_;
        }

        private void btn_ok_Click(object sender, EventArgs e)
        {
            string name = txt_name_.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Enter a name.", "[ERR] IP-CAM MODEL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txt_name_.Focus();
                return;
            }

            Result = new IPCamModelProfile
            {
                name = name,
                mjpeg_port = txt_mjpeg_port_.Text.Trim(),
                mjpeg_url = txt_mjpeg_url_.Text.Trim(),
                rtsp_port = txt_rtsp_port_.Text.Trim(),
                rtsp_url = txt_rtsp_url_.Text.Trim(),
                keep_user_input = chk_keep_user_input_.Checked,
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
