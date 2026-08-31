using FaceSDKSample.sample;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;


namespace FaceSDKSample
{
    public partial class Form1 : Form
    {
        SampleProp.Settings settings_ = new SampleProp.Settings();
        SampleProp.DemoClientServerProp demo_prop_ = new SampleProp.DemoClientServerProp();
        SampleProp.SamplePropFaceInfo faceinfo_prop_ = new SampleProp.SamplePropFaceInfo();

        List<CamCtx.CamResResolver.Res> resolved_cap_resolutions_ = new List<CamCtx.CamResResolver.Res>();

        // IP 카메라 모델 프로파일. ipcam_models.json 에서 읽고 [+] / [-] 로 편집한다.
        List<IPCamModelProfile> ip_cam_models_ = new List<IPCamModelProfile>();

        // cap_src_ipcam_on_host_ip_changed() 가 PORT / URL 칸을 대입하면 TextChanged 가 떠서
        // 같은 함수로 다시 들어온다. 그 재진입을 막는다.
        bool updating_ip_cam_url_ = false;

        Timer compare_face_label_blink_timer_ = new Timer();

        void CompareFaceLabelBlinkTimer_Tick(object sender, EventArgs e)
        {
            lbl_select_face_cmp_img.Visible = !lbl_select_face_cmp_img.Visible;
        }

        void SetSelectFaceCompareImgLabelBlinkState(bool blink)
        {
            if(blink)
            {
                compare_face_label_blink_timer_.Start();
                lbl_select_face_cmp_img.Visible = true;
            }
            else
            {
                compare_face_label_blink_timer_.Stop();
                lbl_select_face_cmp_img.Visible = false;
            }
        }

        public string last_face_compare_img_path = "";

        private void button4_Click(object sender, EventArgs e)
        {
            if (last_face_compare_img_path == "")
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                last_face_compare_img_path = Path.Combine(exeDir, "imgs\\compare");
            }

            string folderPath = last_face_compare_img_path;

            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            Process.Start("explorer.exe", folderPath);
        }

        public void ReLoadFaceCompareImg(string img_path = "")
        {
            imglst_face_compare.Images.Clear();
            imglst_face_compare.ImageSize = new Size(64, 64);
            imglst_face_compare.ColorDepth = ColorDepth.Depth32Bit;

            lv_face_compare_img.Items.Clear();

            if (img_path == "")
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                img_path = Path.Combine(exeDir, "imgs\\compare");
            }

            if (Directory.Exists(img_path))
            {
                string[] files = Directory.GetFiles(img_path, "*.*", SearchOption.TopDirectoryOnly);

                foreach (var file in files)
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();

                    if (ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp")
                    {
                        try
                        {
                            Image img = Image.FromFile(file);
                            const int ExifOrientationId = 0x0112;

                            if (img.PropertyIdList.Contains(ExifOrientationId))
                            {
                                var prop = img.GetPropertyItem(ExifOrientationId);
                                int val = BitConverter.ToUInt16(prop.Value, 0);

                                switch (val)
                                {
                                    case 3:
                                        img.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                        break;
                                    case 6:
                                        img.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                        break;
                                    case 8:
                                        img.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                        break;
                                }

                                img.RemovePropertyItem(ExifOrientationId);
                            }

                            imglst_face_compare.Images.Add(img);

                            string name = Path.GetFileName(file);
                            lv_face_compare_img.Items.Add(new ListViewItem(name, imglst_face_compare.Images.Count - 1));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed To load Image: {file} ({ex.Message})");
                        }
                    }
                }

                last_face_compare_img_path = img_path;
            } 
            else
            {
                last_face_compare_img_path = "";
            }

            settings_.COMPARE_IMG_PATH = last_face_compare_img_path;

            lv_face_compare_img.View = View.LargeIcon;
            lv_face_compare_img.LargeImageList = imglst_face_compare;
            lv_face_compare_img.MultiSelect = false;
        }

        public Form1()
        {
            InitializeComponent();
            Text = "Alchera FaceSDK Sample - " + Sample.NAME + "-" + Sample.REV + "_" + Sample.ID;

            settings_.EXEC_PATH = AppDomain.CurrentDomain.BaseDirectory;

            if (settings_.EXEC_PATH.EndsWith("\\"))
                settings_.EXEC_PATH = settings_.EXEC_PATH.TrimEnd('\\');

            ppg_settings.SelectedObject = settings_;
            ppg_settings.PropertyValueChanged += PPG_Settings_PropertyValueChanged;

            ppg_liveness.SelectedObject = demo_prop_;
            ppg_liveness.PropertyValueChanged += PPG_Liveness_PropertyValueChanged;

            ppg_face_info.SelectedObject = faceinfo_prop_;
            ppg_face_info.PropertyValueChanged += PPG_UI_PropertyValueChanged;

            SampleProp.SamplePropFaceInfo.SetPropertyGridLabelWidth(ppg_face_info, 0);

            ReLoadFaceCompareImg();

            compare_face_label_blink_timer_.Interval = 500;
            compare_face_label_blink_timer_.Tick += CompareFaceLabelBlinkTimer_Tick;
            compare_face_label_blink_timer_.Stop();                           


        }

        private void PPG_Settings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Sample.inst.PostDemoPropEvent(ppg_settings.SelectedObject as SampleProp.Prop,
                "Changed", s, e);

            string propName = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;
            object newValue = e.ChangedItem.Value;

            SampleProp.Settings prop_settings
                = ppg_settings.SelectedObject as SampleProp.Settings;

            switch (propName)
            {
                case "CAP_INDEX_RGB":
                case "CAP_WIDTH":
                case "CAP_HEIGHT":
                    {
                        usb_cam_cap_info.Text = $"Desired: {prop_settings.CAP_WIDTH}x{prop_settings.CAP_HEIGHT}, idx-rgb={prop_settings.CAP_INDEX_RGB}";
                    }
                    break;

                default:
                    break;
            }

      
        }

        

        private void PPG_UI_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Sample.inst.PostDemoPropEvent(ppg_face_info.SelectedObject as SampleProp.Prop,
                "Changed", s, e);
        }


        private void PPG_Liveness_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            Sample.inst.PostDemoPropEvent(ppg_liveness.SelectedObject as SampleProp.Prop, 
                "Changed", s, e);
        }

        private void btn_load_facesdk_Click(object sender, EventArgs e)
        {
            btn_load_facesdk.Enabled = false;

            try
            {
                Sample.inst.CreateFaceSDKCtx();

                MessageBox.Show("[INFO] Successfully Loaded FaceSDK!, elapsed=" + Sample.fsdk.elapsed_ms_init_ + " MS", 
                    "FaceSDK", MessageBoxButtons.OK);

                btn_select_camera_res.Enabled = true;
                btn_open_usb_cam.Enabled = true;
                cap_src_ip_cam_open.Enabled = true; 
            }
            catch (Sample.SampleException ex_samp)
            {
                btn_load_facesdk.Enabled = true;

                String err_msg = "[ERR] " + ex_samp.Message;
                err_msg += "\n\nCheck below at model path=" + Sample.inst.GetFaceSDKModelPath();
                err_msg += "\n\n1) license.cer file existence";
                err_msg += "\n2) LicenseActivation(run LicenseGen.exe in model path)";
                
                MessageBox.Show(err_msg, "[ERR] Failed to initialize FaceSDK", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            btn_load_facesdk.Enabled = true;
            btn_open_usb_cam.Enabled = false;
            btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = false;
            btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled = false;

            SampleProp.Settings prop_settings = ppg_settings.SelectedObject as SampleProp.Settings;

            usb_cam_cap_info.Text = $"Desired: {prop_settings.CAP_WIDTH}x{prop_settings.CAP_HEIGHT}, idx-rgb={prop_settings.CAP_INDEX_RGB}";

            LoadIPCamModels();

            // select second stream
            cap_src_ip_cam_stream_name.SelectedIndex = 1;
            
            cap_src_ip_cam_frame_proto.SelectedIndex = 0;

            cap_src_select.SelectedIndex = 1;

        }

        private void btn_open_usb_cam_Click(object sender, EventArgs e)
        {
            btn_open_usb_cam.Enabled = false;

            SampleProp.Settings prop_settings 
                = ppg_settings.SelectedObject as SampleProp.Settings;

            SampleProp.DemoClientServerProp prop_liv
                = ppg_liveness.SelectedObject as SampleProp.DemoClientServerProp;
                        
            string open_cap_index = prop_settings.CAP_INDEX_RGB.ToString();
            int open_cap_width = prop_settings.CAP_WIDTH;
            int open_cap_height = prop_settings.CAP_HEIGHT;
            bool open_cap_flip_horizontal = prop_settings.CAP_FLIP_HOR;

            //
            // select resolution
            //

            if (resolved_cap_resolutions_.Count > 0)
            {
                // filter by prefer capture size
                var filter_cap_res = CamCtx.CamResResolver.filter_resolution(
                    resolved_cap_resolutions_,
                    prop_settings.CAP_PREFER_WIDTH, prop_settings.CAP_PREFER_HEIGHT);

                string[] cap_resolutions
                    = CamCtx.CamResResolver.ToStringAry(filter_cap_res);

                if (cap_resolutions.Length == 0)
                {
                    string err_msg = "[ERR] No Suitable Camera Resolution For Cap Prefer Width/Height!";
                    err_msg += $"\n > current prefer cap width={prop_settings.CAP_PREFER_WIDTH}, height={prop_settings.CAP_PREFER_HEIGHT}";
                    err_msg += $"\n\n Please Adjust cap-prefer-width, cap-prefer-height !";

                    MessageBox.Show(err_msg, "[ERR] Camera Resolution", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btn_open_usb_cam.Enabled = true;
                    return;
                }

                string selected_cap_res = ListSelectMsgBox.Show("Select Resolution",
                    "Select Capture Resolution (Filtered By Prefer W=" + prop_settings.CAP_PREFER_WIDTH + ", H=" + prop_settings.CAP_PREFER_HEIGHT + ") ",
                    cap_resolutions);

                if (selected_cap_res == null)
                {
                    btn_open_usb_cam.Enabled = true;
                    return; // canceled selection
                }

                if (!CamCtx.CamResResolver.ParseAsWidthxHeight(selected_cap_res, out open_cap_width, out open_cap_height))
                {
                    String err_msg = "[ERR] invalid cap resoultion string, err=" + selected_cap_res;
                    MessageBox.Show(err_msg, "[ERR] CAMERA OPEN", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    btn_open_usb_cam.Enabled = true;
                    return;
                }
            }

            //
            // open camera
            //

            try
            {
                string cap_api_vendor = "UNKNOWN"; // UNKNOWN, MSMF, DSHOW, FFMPEG
                string cap_dev_model_name = "UNKNOWN";
                CamCtx cam_ctx = new CamCtx_USBCam(cap_api_vendor, cap_dev_model_name);

                string open_cap_stream = open_cap_index.ToString();
                Sample.inst.OpenCapture(cam_ctx, 
                    open_cap_width, open_cap_height, open_cap_index.ToString(), 
                    open_cap_flip_horizontal);

                Sample.inst.GetCaptureInfo(out open_cap_index, out open_cap_width, out open_cap_height);

                MessageBox.Show("[INFO] Successfully Opened Capture Device, capture_idx=" + open_cap_index
                    + "\n > Cap Width=" + open_cap_width + " , Height=" + open_cap_height
                    + "\n > Open Elapsed=" + Sample.cam.elapsed_sec_cam_open_ + " Sec"
                    +"\n\n" + cam_ctx.GetInfo(),
                    "Cam Capure Index=" + open_cap_index + " is Opened", MessageBoxButtons.OK);

                prop_settings.CAP_WIDTH = open_cap_width;
                prop_settings.CAP_HEIGHT = open_cap_height;

                if (prop_settings.CAP_WIDTH >= 1024 && prop_settings.CAP_HEIGHT >= 720)
                {
                    prop_settings.CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ = true;
                    prop_settings.CAP_SRC_ENABLE_CROP_MARGIN_BY_ASPECT = true;
                }
                else
                {
                    // skip minimum frame size, crop margin width
                    prop_settings.CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ = false;
                    prop_settings.CAP_SRC_ENABLE_CROP_MARGIN_BY_ASPECT = false;
                }

                // local, input frame per sec > 10 , always use continous
                prop_liv.LIV_BGR_BESTSHOT_CONTINUOUS_FOUR_BGR_QUALITY_CHECK = true;

                ppg_settings.Refresh();
                ppg_liveness.Refresh();

                usb_cam_cap_info.Text = $"Opened: {open_cap_width}x{open_cap_height}, idx-rgb={open_cap_index}";

                SetSelectFaceCompareImgLabelBlinkState(true);
                lv_face_compare_img.Enabled = true;

            }
            catch (Sample.SampleException ex_samp)
            {
                btn_open_usb_cam.Enabled = true;

                String err_msg = "[ERR] " + ex_samp.Message;
                err_msg += "\n camera index=" + open_cap_index;
                err_msg += "\n cap width=" + open_cap_width + ", height=" + open_cap_height;
                err_msg += "\n\nCheck Camera Connection or Camera Index and prefer cap width, height..";

                MessageBox.Show(err_msg, "[ERR] CAMERA OPEN", MessageBoxButtons.OK, MessageBoxIcon.Error);

                btn_open_usb_cam.Enabled = true;
            }

        }

        private void btn_demo_passive_liveness_best4_feature_faceserver_start_Click(object sender, EventArgs e)
        {
            btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = false;

            if(Sample.cam == null)
            {
                MessageBox.Show("[ERR] Capture Device is not opened!!, click open capature", 
                    "[ERR] Capture Device is not opened!!", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = true;

                return;
            }


            Sample.inst.StartDemo(DemoClientServer._NAME,
                () =>
                {
                    if (btn_demo_passive_liveness_best4_feature_faceserver_start.InvokeRequired)
                    {
                        // do not use this.Invoke
                        this.BeginInvoke((MethodInvoker)delegate () {
                            btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled = false;
                        });
                    }
                    else
                    {
                        btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled = false;
                    }

                },
                ppg_settings.SelectedObject as SampleProp.Settings,
                ppg_liveness.SelectedObject as SampleProp.DemoClientServerProp,
                ppg_face_info.SelectedObject as SampleProp.SamplePropFaceInfo
                );


            btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled = true;
        }

        private void StopDemo()
        {
            if (btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled == false)
                return;

            btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled = false;

            Sample.inst.StopDemo(DemoClientServer._NAME,
                (_elasped_ms, _status) =>
                {
                    if (exit_msg_box_ == null)
                    {
                        ShowExitMessageBox();
                        while (!exit_msg_box_.IsHandleCreated)
                        {
                            Thread.Sleep(100);
                        }
                    }

                    if (exit_msg_box_ != null)
                    {
                        bool invoke_req = exit_msg_box_.InvokeRequired;

                        switch (_status)
                        {
                            case 0: // program is exited 
                                {
                                    if (invoke_req)
                                    {
                                        exit_msg_box_.BeginInvoke(new Action(() => {
                                            exit_msg_box_.Close();
                                            exit_msg_box_ = null;
                                        }));
                                    }
                                    else
                                    {
                                        exit_msg_box_.Invoke(new Action(() =>
                                        {
                                            exit_msg_box_.Close();
                                            exit_msg_box_ = null;
                                        }));
                                    }                                    
                                }
                                break;

                            case 1:  // exiting..
                                {
                                    if (invoke_req)
                                    {
                                        exit_msg_box_.BeginInvoke(new Action(() => {
                                            if(exit_msg_box_!= null)
                                                exit_msg_box_.Controls["status"].Text = "Elapse Time(Sec)=" + (int)(_elasped_ms / 1000);
                                        }));
                                    }
                                    else
                                    {
                                        exit_msg_box_.Invoke(new Action(() =>
                                        {
                                            if(exit_msg_box_ != null)
                                                exit_msg_box_.Controls["status"].Text = "Elapse Time(Sec)=" + (int)(_elasped_ms / 1000);
                                        }));
                                    }
                                }
                                break;

                            case -1:
                                {
                                    // timeoutted, failed
                                    if (invoke_req)
                                    {
                                        exit_msg_box_.BeginInvoke(new Action(() => {
                                            if (exit_msg_box_ != null)
                                                exit_msg_box_.Controls["status"].Text = "Timeout occured on Exiting.., Elapse Time(Sec)=" + (int)(_elasped_ms / 1000);
                                        }));
                                    }
                                    else
                                    {
                                        exit_msg_box_.Invoke(new Action(() => {
                                            if (exit_msg_box_ != null)
                                                exit_msg_box_.Controls["status"].Text = "Timeout occured on Exiting..,  Elapse Time(Sec)=" + (int)(_elasped_ms / 1000);
                                        }));
                                    }
                                }
                                break;

                            default:
                                break;
                        }
                    }
                },
                30);

            btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = true;
            cap_src_ip_cam_open.Enabled = true;
        }

        private async void btn_demo_passive_liveness_best4_feature_faceserver_stop_Click(object sender, EventArgs e)
        {
            StopDemo();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void lv_face_compare_img_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lv_face_compare_img.SelectedItems.Count > 0)
            {
                SetSelectFaceCompareImgLabelBlinkState(false);
                lbl_select_face_cmp_img.Visible = true;
                lbl_select_face_cmp_img.Text = "[Compare] " + lv_face_compare_img.SelectedItems[0].Text;                
                btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = true;

                demo_prop_.COMPARE_SRC_IMG_FILENAME = lv_face_compare_img.SelectedItems[0].Text;
                ppg_liveness.Refresh();
            } 
            else
            {
                SetSelectFaceCompareImgLabelBlinkState(true);
                btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = false;
            }
        }

        

        private void btn_reload_compare_img_Click(object sender, EventArgs e)
        {
            ReLoadFaceCompareImg(last_face_compare_img_path);
        }

        private void btn_check_camera_res_Click(object sender, EventArgs e)
        {
            btn_select_camera_res.Enabled = false;

            string msg = "";
            SampleProp.Settings prop_settings 
                = ppg_settings.SelectedObject as SampleProp.Settings;

            Stopwatch sw_resolve_resolution = new Stopwatch();
            sw_resolve_resolution.Start();
            var cam_res_lst = CamCtx.CamResResolver.GetAllCapResolutions(prop_settings.CAP_INDEX_RGB);
            sw_resolve_resolution.Stop();
            
            if (cam_res_lst == null || cam_res_lst.Count == 0)
            {
                msg += "[ERR] failed to retrive camera resolution list !!";

                resolved_cap_resolutions_ = new List<CamCtx.CamResResolver.Res>();

                btn_open_usb_cam.Enabled = false;
                btn_select_camera_res.Enabled = true;
            }
            else
            {
                msg += "[INFO] Detected Camera Resolutions)";

                foreach (var res in cam_res_lst)
                {
                    msg += "\n" + $"{res.w} x {res.h}";
                }

                resolved_cap_resolutions_ = cam_res_lst;
                btn_open_usb_cam.Enabled = true;
                btn_select_camera_res.Enabled = false;
            }

            msg += $"\n\n > elapsed time = {sw_resolve_resolution.ElapsedMilliseconds} MS";
            msg += $"\n > cap_index_rgb={prop_settings.CAP_INDEX_RGB} ";
            //msg += $"\n > cap_prefer_w={prop_settings.CAP_PREFER_WIDTH} ";
            //msg += $"\n > cap_prefer_h={prop_settings.CAP_PREFER_HEIGHT} ";

            MessageBox.Show(msg,"FaceSDK - Camera Resolution", MessageBoxButtons.OK);
        }


        //
        // resolution select dialog
        //
        public static class ListSelectMsgBox
        {
            public static string Show(string title, string message, string[] items)
            {
                if(items == null || items.Length == 0) { 
                    return null; 
                }

                Form form = new Form();
                Label label = new Label();
                ListBox listBox = new ListBox();
                Button buttonOk = new Button();
                Button buttonCancel = new Button();

                form.Text = title;
                label.Text = message;

                listBox.Items.AddRange(items);
                listBox.SelectedIndex = 0;
                listBox.SelectionMode = SelectionMode.One;

                buttonOk.Text = "OK";
                buttonCancel.Text = "Cancel";
                buttonOk.DialogResult = DialogResult.OK;
                buttonCancel.DialogResult = DialogResult.Cancel;

                label.SetBounds(9, 10, 372, 13);
                listBox.SetBounds(12, 36, 372, 120);
                buttonOk.SetBounds(228, 165, 75, 23);
                buttonCancel.SetBounds(309, 165, 75, 23);

                label.AutoSize = true;
                form.ClientSize = new System.Drawing.Size(396, 200);
                form.Controls.AddRange(new Control[] { label, listBox, buttonOk, buttonCancel });
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterScreen;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.AcceptButton = buttonOk;
                form.CancelButton = buttonCancel;

                listBox.DoubleClick += (s, e) =>
                {
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };

                var dialogResult = form.ShowDialog();
                if (dialogResult == DialogResult.OK && listBox.SelectedItem != null)
                    return listBox.SelectedItem.ToString();

                return null;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopDemo();
        }

        private Form exit_msg_box_;
        private void ShowExitMessageBox()
        {
            exit_msg_box_ = new Form
            {
                Text = "Exiting..",
                Width = 300,
                Height = 120,
                StartPosition = FormStartPosition.CenterParent
            };

            Label lbl = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = "Exiting demo program.. please wait.."
            };
            exit_msg_box_.Controls.Add(lbl);

            Label lbl_status = new Label
            {
                Name = "status",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Text = "Elasped Time="
            };
            exit_msg_box_.Controls.Add(lbl_status);

            new Thread(() => Application.Run(exit_msg_box_)).Start();
        }

        //
        // Record
        //

        private void btn_rec_start_Click(object sender, EventArgs e)
        {
            btn_rec_start.Enabled = false;

            string rec_path = GetRecPath();

            if (rec_path == "")
            {
                MessageBox.Show(
                    "Can't create rec path=" + rec_path,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                btn_rec_start.Enabled = true;
                return;
            }


            if (!Sample.inst.RecordDemo(DemoClientServer._NAME,
                (_siz_vid, _file_path, _status) =>
                {
                    if (lbl_rec_st == null)
                    {
                        return;
                    }

                    if (lbl_rec_st.InvokeRequired)
                    {
                        // do not use this.Invoke
                        this.BeginInvoke((MethodInvoker)delegate () {
                            lbl_rec_st.Text = $"siz: {_siz_vid} MB, {_file_path}";
                        });
                    }
                    else
                    {
                        lbl_rec_st.Text = $"siz: {_siz_vid} MB, {_file_path}";
                    }

                    if (_status == -1)
                    {
                        if (btn_rec_stop.InvokeRequired)
                        {
                            this.BeginInvoke((MethodInvoker)delegate () {
                                btn_rec_stop.PerformClick();
                            });
                        }
                        else
                        {
                            btn_rec_stop.PerformClick();
                        }
                    }

                },
                "start",
                ppg_settings.SelectedObject as SampleProp.Settings
                ))
            {
                btn_rec_start.Enabled = true;
                btn_rec_stop.Enabled = false;
                return;
            }

            btn_rec_start.Enabled = false;
            btn_rec_stop.Enabled = true;
        }

        private void _stop_record()
        {
            Sample.inst.RecordDemo(DemoClientServer._NAME,
                /*(_siz_vid, _file_path, _status) =>
                {
                    if (lbl_rec_st == null)
                    {
                        return;
                    }

                    bool lbl_rec_st_invoke_req = lbl_rec_st.InvokeRequired;

                    //switch (_status)
                    //{
                    //} 

                }*/
                null,
                "stop",
                ppg_settings.SelectedObject as SampleProp.Settings
                );
        }

        private void btn_rec_stop_Click(object sender, EventArgs e)
        {
            btn_rec_stop.Enabled = false;
            _stop_record();
            btn_rec_start.Enabled = true;
        }

        private void btn_rec_open_folder_Click(object sender, EventArgs e)
        {
            string rec_path = GetRecPath();

            if (rec_path == "")
            {
                MessageBox.Show(
                    "Can't create rec path=" + rec_path,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return;
            }

            Process.Start("explorer.exe", rec_path);
        }

        private string GetRecPath(bool create_on_not_exist = true)
        {
            string rec_path = settings_.EXEC_PATH + "\\" + settings_.REC_PATH;

            if (!System.IO.Directory.Exists(rec_path))
            {
                if (create_on_not_exist)
                    System.IO.Directory.CreateDirectory(rec_path);
            }

            if (!System.IO.Directory.Exists(rec_path))
            {
                return "";
            }

            return rec_path;
        }

        private void btn_get_img_score_Click(object sender, EventArgs e)
        {
            if(Sample.fsdk == null)
            {
                MessageBox.Show("Press Load FaceSDK First", "ERROR", MessageBoxButtons.OK);
                return;
            }

            SampleProp.Settings prop_settings = settings_;
            SampleProp.DemoClientServerProp prop_demo = demo_prop_;

            string img_path = prop_settings.COMPARE_IMG_PATH + "\\"
                       + prop_demo.COMPARE_SRC_IMG_FILENAME;

            float liv_score;
            float fas_score;

            string err_desc;

            bool is_ok;

            is_ok = Sample.ImgScore_GetScore(out err_desc, out fas_score,
                out liv_score, img_path);

            string filename = prop_demo.COMPARE_SRC_IMG_FILENAME;
            string title = "Img Scores";
            string msg = "file: " + filename;

            if (is_ok)
            {
                title = "[OK] " + title;
                msg = "[OK] " + msg
                    + "\n"
                    + "\nFAS Antispoofing Score=" + fas_score
                    + "\nBGR Liveness Score=" + liv_score;
            }
            else
            {
                title = "[ERR] " + title;
                msg = "[ERR] " + msg + "\nFailed To Get Score \n" + err_desc;
            }

            MessageBox.Show(msg, title, MessageBoxButtons.OK);
        }

        private void btn_feature_explorer_Click(object sender, EventArgs e)
        {
            SampleProp.Settings prop_settings = settings_;
            string startup_path = prop_settings.COMPARE_IMG_PATH;

            FeatureExplorer.show_dlg(this, startup_path);
        }

        private void cap_src_ipcam_open_Click(object sender, EventArgs e)
        {
            // (cap_src_select.SelectedTab.Name == cap_src_ip_cam.Name
            if (cap_src_select.SelectedTab != cap_src_ip_cam)
                return;

            cap_src_ip_cam_open.Enabled = false;

            SampleProp.Settings prop_settings 
                = ppg_settings.SelectedObject as SampleProp.Settings;

            SampleProp.DemoClientServerProp prop_liv
                = ppg_liveness.SelectedObject as SampleProp.DemoClientServerProp;

            //
            // open ip camera
            //

            string ip_cam_tab_selected_name = cap_src_ip_cam_frame_proto.SelectedTab.Text;

            string ip_cam_model = cap_src_ip_cam_model.SelectedItem?.ToString() ?? "";
            
            string ip_cam_proto = ip_cam_tab_selected_name == "MJPEG" ? "ws" : "rtsp";
            string ip_cam_ip = cap_src_ip_cam_ip.Text;
            string ip_cam_port = cap_src_ip_cam_port.Text;

            // textBox1 = ID, textBox2 = PW (RTSP Digest/Basic 인증용)
            string ip_cam_id = textBox1.Text.Trim();
            string ip_cam_pw = textBox2.Text.Trim();

            // URL 텍스트박스를 실제 접속 주소로 사용한다.
            // 비어 있거나 "NOT IMPL" 이면 ip/port 조립 방식으로 폴백한다.
            string ip_cam_url = (ip_cam_tab_selected_name == "MJPEG"
                ? cap_src_ip_cam_mjpeg_url.Text
                : cap_src_ip_cam_rtsp_url.Text).Trim();

            // URL 템플릿이 없는 모델이다. ip/port 로 재조립해 엉뚱한 주소에 붙지 않도록 막는다.
            if (ip_cam_url == IPCamModelStore.URL_NOT_IMPL)
            {
                MessageBox.Show(
                    $"[ERR] Model '{ip_cam_model}' has no URL template for {ip_cam_tab_selected_name}.\n\n"
                    + "Enter the URL directly, or select the CUSTOM model.",
                    "[ERR] IPCAMERA OPEN", MessageBoxButtons.OK, MessageBoxIcon.Error);

                cap_src_ip_cam_open.Enabled = true;
                return;
            }

            bool ip_cam_flip_horizontal = prop_settings.CAP_FLIP_HOR;
            // main, second
            string ip_cam_stream = cap_src_ip_cam_stream_name.SelectedItem.ToString(); // 1, 2

            int ip_cam_width = 0;
            int ip_cam_height = 0;
                        
            CamCtx cam_ctx = null;
                        

            try
            {
                if (ip_cam_tab_selected_name == "MJPEG")
                {
                    cam_ctx = new CamCtx_IPCam(new CamCtx_IPCam.MJPEG_WS(),
                        ip_cam_proto, ip_cam_ip, ip_cam_port, ip_cam_model, ip_cam_url);
                }
                else if (ip_cam_tab_selected_name == "RTSP")
                {
                    cam_ctx = new CamCtx_IPCam(new CamCtx_IPCam.RTSP(),
                        ip_cam_proto, ip_cam_ip, ip_cam_port, ip_cam_id, ip_cam_pw,
                        ip_cam_model, ip_cam_url);
                }
                else
                {
                    throw new Exception($"invalid param, ip_cam_tab_selected_name, {ip_cam_tab_selected_name}");
                }
                
                Sample.inst.OpenCapture(cam_ctx , -1, -1, ip_cam_stream, ip_cam_flip_horizontal);
                Sample.inst.GetCaptureInfo(out ip_cam_stream, out ip_cam_width, out ip_cam_height);                               

                MessageBox.Show("[INFO] Successfully Opened Capture Device=IPCamera"
                    + "\n > Cap Stream=" + ip_cam_stream
                    + "\n > Cap W=" + ip_cam_width + " , H=" + ip_cam_height
                    + "\n > Open Elapsed=" + Sample.cam.elapsed_sec_cam_open_ + " Sec"
                    + "\n\n" + cam_ctx.GetInfo(),
                    "IPCAMERA, Stream=" + ip_cam_stream,
                    MessageBoxButtons.OK);

                prop_settings.CAP_WIDTH = ip_cam_width;
                prop_settings.CAP_HEIGHT = ip_cam_height;

                if (prop_settings.CAP_WIDTH >= 1024 && prop_settings.CAP_HEIGHT >= 720)
                {
                    //prop_settings.CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ = true;
                    prop_settings.CAP_SRC_ENABLE_CROP_MARGIN_BY_ASPECT = true;
                }
                else
                {
                    // skip minimum frame size, crop margin width
                    //prop_settings.CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ = false;
                    prop_settings.CAP_SRC_ENABLE_CROP_MARGIN_BY_ASPECT = false;
                }

                prop_settings.CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ = false;

                //if(cam_ctx.cap_input_fps == "" || int.Parse(cam_ctx.cap_input_fps) < 10) {}

                ppg_settings.Refresh();
                ppg_liveness.Refresh();

                SetSelectFaceCompareImgLabelBlinkState(true);
                lv_face_compare_img.Enabled = true;

            }
            catch (Sample.SampleException ex_samp)
            {
                String err_msg = "[ERR] " + ex_samp.Message;
                err_msg += "\n ip camera stream=" + ip_cam_stream;
                err_msg += "\n\n Check IP Camera Network Connection or stream name";
                err_msg += $"\n\n{(cam_ctx != null ? cam_ctx.GetInfo() : "cam_ctx=null")} ";

                MessageBox.Show(err_msg, "[ERR] IPCAMERA OPEN", MessageBoxButtons.OK, MessageBoxIcon.Error);

                cap_src_ip_cam_open.Enabled = true;
            }
        }

        private void cap_src_select_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(cap_src_select.SelectedTab == cap_src_usb_cam)
            {

            }
            else if (cap_src_select.SelectedTab == cap_src_ip_cam)
            {

            }
        }

        private void cap_src_ipcam_ip_on_keypress(object sender, KeyPressEventArgs e)
        {
            // IPv4 주소뿐 아니라 호스트명도 받는다. (임의 벤더 카메라 수용)
            if (!char.IsControl(e.KeyChar) && !char.IsLetterOrDigit(e.KeyChar)
                && e.KeyChar != '.' && e.KeyChar != '-' && e.KeyChar != '_')
            {
                e.Handled = true;
            }
        }

        private void cap_src_ipcam_port_on_keypress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
            
        }

        private void cap_src_ipcam_ip_on_text_changed(object sender, EventArgs e)
        {
            cap_src_ipcam_on_host_ip_changed();
        }

        private void cap_src_ipcam_port_on_text_changed(object sender, EventArgs e)
        {            
            cap_src_ipcam_on_host_ip_changed();
        }

        //
        // IP 카메라 모델 프로파일
        //

        // 현재 MJPEG 탭이 선택되어 있는지. (탭 0 = MJPEG, 탭 1 = RTSP)
        private bool IsIPCamMJpegTabSelected()
        {
            return cap_src_ip_cam_frame_proto.SelectedIndex == 0;
        }

        private IPCamModelProfile GetSelectedIPCamModel()
        {
            if (cap_src_ip_cam_model.SelectedIndex == -1)
                return null;

            return IPCamModelStore.Find(ip_cam_models_, cap_src_ip_cam_model.SelectedItem.ToString());
        }

        // 프로파일 목록을 파일에서 읽어 콤보를 채운다.
        private void LoadIPCamModels()
        {
            string err_desc;
            ip_cam_models_ = IPCamModelStore.Load(out err_desc);

            if (!string.IsNullOrEmpty(err_desc))
            {
                MessageBox.Show(err_desc, "[WARN] IP-CAM MODEL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            RebuildIPCamModelCombo(null);
        }

        // 콤보 항목을 다시 만들고 select_name 을 고른다. (없으면 첫 항목)
        private void RebuildIPCamModelCombo(string select_name)
        {
            cap_src_ip_cam_model.BeginUpdate();
            cap_src_ip_cam_model.Items.Clear();

            foreach (IPCamModelProfile p in ip_cam_models_)
            {
                cap_src_ip_cam_model.Items.Add(p.name);
            }

            cap_src_ip_cam_model.EndUpdate();

            if (cap_src_ip_cam_model.Items.Count == 0)
                return;

            int idx = select_name == null ? -1 : cap_src_ip_cam_model.Items.IndexOf(select_name);

            // SelectedIndex 대입이 SelectedIndexChanged 를 띄워 URL 칸까지 갱신한다.
            cap_src_ip_cam_model.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void cap_src_ip_cam_model_on_selected_index_changed(object sender, EventArgs e)
        {
            // 모델이 바뀌었으므로 PORT 도 그 모델의 기본값으로 되돌린다.
            cap_src_ipcam_on_host_ip_changed(true);
        }

        private void cap_src_ip_cam_model_add_Click(object sender, EventArgs e)
        {
            // 현재 UI 값에서 템플릿을 역산해 미리 채운다.
            string ip = cap_src_ip_cam_ip.Text.Trim();
            string port = cap_src_ip_cam_port.Text.Trim();

            IPCamModelProfile seed = new IPCamModelProfile
            {
                name = "",
                mjpeg_port = IsIPCamMJpegTabSelected() ? port : "",
                rtsp_port = IsIPCamMJpegTabSelected() ? "" : port,
                mjpeg_url = IPCamModelProfile.ToUrlTemplate(cap_src_ip_cam_mjpeg_url.Text, ip, port),
                rtsp_url = IPCamModelProfile.ToUrlTemplate(cap_src_ip_cam_rtsp_url.Text, ip, port),
            };

            using (IPCamModelEditDlg dlg = new IPCamModelEditDlg("Add IP-CAM Model", seed))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                IPCamModelProfile added = dlg.Result;
                IPCamModelProfile dup = IPCamModelStore.Find(ip_cam_models_, added.name);

                if (dup != null)
                {
                    DialogResult ask = MessageBox.Show(
                        $"Model '{dup.name}' already exists. Overwrite?",
                        "IP-CAM MODEL", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (ask != DialogResult.Yes)
                        return;

                    ip_cam_models_[ip_cam_models_.IndexOf(dup)] = added;
                }
                else
                {
                    ip_cam_models_.Add(added);
                }

                SaveIPCamModels();
                RebuildIPCamModelCombo(added.name);
            }
        }

        private void cap_src_ip_cam_model_del_Click(object sender, EventArgs e)
        {
            IPCamModelProfile sel = GetSelectedIPCamModel();

            if (sel == null)
                return;

            // 목록이 비면 URL 자동 입력이 통째로 멈춘다. 마지막 하나는 남긴다.
            if (ip_cam_models_.Count <= 1)
            {
                MessageBox.Show("Cannot delete the last model.", "IP-CAM MODEL",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult ask = MessageBox.Show($"Delete model '{sel.name}'?",
                "IP-CAM MODEL", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (ask != DialogResult.Yes)
                return;

            ip_cam_models_.Remove(sel);

            SaveIPCamModels();
            RebuildIPCamModelCombo(null);
        }

        private void SaveIPCamModels()
        {
            string err_desc;

            if (!IPCamModelStore.Save(ip_cam_models_, out err_desc))
            {
                MessageBox.Show(err_desc, "[ERR] IP-CAM MODEL",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 선택된 모델 프로파일에 따라 URL 칸을 채운다.
        //
        // apply_profile_port: 모델 / 탭이 바뀐 경우에만 true. PORT 를 프로파일 기본값으로 되돌린다.
        // TextChanged 경로에서 true 로 부르면 사용자가 방금 친 PORT 를 즉시 덮어쓴다.
        private void cap_src_ipcam_on_host_ip_changed(bool apply_profile_port = false)
        {
            // PORT 대입이 TextChanged 로 되돌아온다. 한 번만 돌게 한다.
            if (updating_ip_cam_url_)
                return;

            IPCamModelProfile model = GetSelectedIPCamModel();

            if (model == null)
                return;

            // 사용자가 직접 입력한 PORT / URL 을 보존하는 모델. (CUSTOM 프로파일)
            if (model.keep_user_input)
                return;

            updating_ip_cam_url_ = true;

            try
            {
                bool is_mjpeg = IsIPCamMJpegTabSelected();

                if (apply_profile_port)
                {
                    string tab_port = model.GetPort(is_mjpeg);

                    if (!string.IsNullOrWhiteSpace(tab_port))
                    {
                        cap_src_ip_cam_port.Text = tab_port;
                    }
                }

                string ip = cap_src_ip_cam_ip.Text;
                string port = cap_src_ip_cam_port.Text;

                cap_src_ip_cam_mjpeg_url.Text = BuildIPCamUrlForUI(model, true, ip, port);
                cap_src_ip_cam_rtsp_url.Text = BuildIPCamUrlForUI(model, false, ip, port);
            }
            finally
            {
                updating_ip_cam_url_ = false;
            }
        }

        // URL 템플릿이 없는 모델은 미구현임을 표시한다.
        private static string BuildIPCamUrlForUI(IPCamModelProfile model, bool is_mjpeg,
            string ip, string port)
        {
            string url = model.BuildUrl(is_mjpeg, ip, port);

            return string.IsNullOrWhiteSpace(url) ? IPCamModelStore.URL_NOT_IMPL : url;
        }

        private void cap_src_ip_cam_frame_proto_on_selected_index_changed(object sender, EventArgs e)
        {
            // MJPEG / RTSP 탭이 바뀌었으므로 PORT 를 그 탭의 기본값으로 되돌린다.
            cap_src_ipcam_on_host_ip_changed(true);
        }
    }
}
