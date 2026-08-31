namespace FaceSDKSample
{
    partial class Form1
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btn_load_facesdk = new System.Windows.Forms.Button();
            this.btn_demo_passive_liveness_best4_feature_faceserver_start = new System.Windows.Forms.Button();
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop = new System.Windows.Forms.Button();
            this.ppg_liveness = new System.Windows.Forms.PropertyGrid();
            this.ppg_settings = new System.Windows.Forms.PropertyGrid();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lv_face_compare_img = new System.Windows.Forms.ListView();
            this.lbl_select_face_cmp_img = new System.Windows.Forms.Label();
            this.imglst_face_compare = new System.Windows.Forms.ImageList(this.components);
            this.btn_open_cmp_img_folder = new System.Windows.Forms.Button();
            this.btn_reload_compare_img = new System.Windows.Forms.Button();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.cap_src_select = new System.Windows.Forms.TabControl();
            this.cap_src_usb_cam = new System.Windows.Forms.TabPage();
            this.usb_cam_cap_info = new System.Windows.Forms.Label();
            this.btn_select_camera_res = new System.Windows.Forms.Button();
            this.btn_open_usb_cam = new System.Windows.Forms.Button();
            this.cap_src_ip_cam = new System.Windows.Forms.TabPage();
            this.cap_src_ip_cam_stream_name = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.cap_src_ip_cam_port = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cap_src_ip_cam_ip = new System.Windows.Forms.TextBox();
            this.cap_src_ip_cam_open = new System.Windows.Forms.Button();
            this.cap_src_ip_cam_frame_proto = new System.Windows.Forms.TabControl();
            this.cap_src_ip_cam_frame_proto_mjpeg = new System.Windows.Forms.TabPage();
            this.cap_src_ip_cam_mjpeg_url = new System.Windows.Forms.TextBox();
            this.cap_src_ip_cam_frame_proto_rtsp = new System.Windows.Forms.TabPage();
            this.cap_src_ip_cam_rtsp_url = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.cap_src_ip_cam_model = new System.Windows.Forms.ComboBox();
            this.cap_src_ip_cam_model_add = new System.Windows.Forms.Button();
            this.cap_src_ip_cam_model_del = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btn_feature_explorer = new System.Windows.Forms.Button();
            this.btn_get_img_score = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.btn_rec_open_folder = new System.Windows.Forms.Button();
            this.lbl_rec_st = new System.Windows.Forms.Label();
            this.btn_rec_stop = new System.Windows.Forms.Button();
            this.btn_rec_start = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.ppg_face_info = new System.Windows.Forms.PropertyGrid();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.cap_src_select.SuspendLayout();
            this.cap_src_usb_cam.SuspendLayout();
            this.cap_src_ip_cam.SuspendLayout();
            this.cap_src_ip_cam_frame_proto.SuspendLayout();
            this.cap_src_ip_cam_frame_proto_mjpeg.SuspendLayout();
            this.cap_src_ip_cam_frame_proto_rtsp.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // btn_load_facesdk
            // 
            this.btn_load_facesdk.Enabled = false;
            this.btn_load_facesdk.Location = new System.Drawing.Point(6, 20);
            this.btn_load_facesdk.Name = "btn_load_facesdk";
            this.btn_load_facesdk.Size = new System.Drawing.Size(258, 23);
            this.btn_load_facesdk.TabIndex = 0;
            this.btn_load_facesdk.Text = "Load FaceSDK";
            this.btn_load_facesdk.UseVisualStyleBackColor = true;
            this.btn_load_facesdk.Click += new System.EventHandler(this.btn_load_facesdk_Click);
            // 
            // btn_demo_passive_liveness_best4_feature_faceserver_start
            // 
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.Enabled = false;
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.Location = new System.Drawing.Point(11, 20);
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.Name = "btn_demo_passive_liveness_best4_feature_faceserver_start";
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.Size = new System.Drawing.Size(236, 26);
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.TabIndex = 1;
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.Text = "Start PassiveLiveness";
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.UseVisualStyleBackColor = true;
            this.btn_demo_passive_liveness_best4_feature_faceserver_start.Click += new System.EventHandler(this.btn_demo_passive_liveness_best4_feature_faceserver_start_Click);
            // 
            // btn_demo_passive_liveness_best4_feature_faceserver_stop
            // 
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.Enabled = false;
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.Location = new System.Drawing.Point(11, 49);
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.Name = "btn_demo_passive_liveness_best4_feature_faceserver_stop";
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.Size = new System.Drawing.Size(236, 23);
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.TabIndex = 3;
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.Text = "Stop Demo";
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.UseVisualStyleBackColor = true;
            this.btn_demo_passive_liveness_best4_feature_faceserver_stop.Click += new System.EventHandler(this.btn_demo_passive_liveness_best4_feature_faceserver_stop_Click);
            // 
            // ppg_liveness
            // 
            this.ppg_liveness.Font = new System.Drawing.Font("굴림", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ppg_liveness.HelpVisible = false;
            this.ppg_liveness.Location = new System.Drawing.Point(645, 64);
            this.ppg_liveness.Name = "ppg_liveness";
            this.ppg_liveness.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.ppg_liveness.Size = new System.Drawing.Size(297, 656);
            this.ppg_liveness.TabIndex = 6;
            this.ppg_liveness.ToolbarVisible = false;
            // 
            // ppg_settings
            // 
            this.ppg_settings.Font = new System.Drawing.Font("굴림", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ppg_settings.HelpVisible = false;
            this.ppg_settings.Location = new System.Drawing.Point(300, 36);
            this.ppg_settings.Name = "ppg_settings";
            this.ppg_settings.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.ppg_settings.Size = new System.Drawing.Size(338, 684);
            this.ppg_settings.TabIndex = 7;
            this.ppg_settings.ToolbarVisible = false;
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label1.Location = new System.Drawing.Point(300, 11);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(338, 25);
            this.label1.TabIndex = 8;
            this.label1.Text = "Settings";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label2.Location = new System.Drawing.Point(643, 12);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(298, 25);
            this.label2.TabIndex = 9;
            this.label2.Text = "Liveness / Compare";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(643, 40);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(126, 20);
            this.comboBox1.TabIndex = 10;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(776, 40);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(52, 19);
            this.button1.TabIndex = 11;
            this.button1.Text = "Reset";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(889, 39);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(52, 19);
            this.button2.TabIndex = 12;
            this.button2.Text = "Save";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(831, 40);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(52, 19);
            this.button3.TabIndex = 13;
            this.button3.Text = "Load";
            this.button3.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btn_demo_passive_liveness_best4_feature_faceserver_start);
            this.groupBox1.Controls.Add(this.btn_demo_passive_liveness_best4_feature_faceserver_stop);
            this.groupBox1.Location = new System.Drawing.Point(17, 557);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(277, 78);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "[4] Select Demo";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // lv_face_compare_img
            // 
            this.lv_face_compare_img.HideSelection = false;
            this.lv_face_compare_img.Location = new System.Drawing.Point(8, 39);
            this.lv_face_compare_img.MultiSelect = false;
            this.lv_face_compare_img.Name = "lv_face_compare_img";
            this.lv_face_compare_img.Size = new System.Drawing.Size(257, 114);
            this.lv_face_compare_img.TabIndex = 16;
            this.lv_face_compare_img.UseCompatibleStateImageBehavior = false;
            this.lv_face_compare_img.SelectedIndexChanged += new System.EventHandler(this.lv_face_compare_img_SelectedIndexChanged);
            // 
            // lbl_select_face_cmp_img
            // 
            this.lbl_select_face_cmp_img.BackColor = System.Drawing.SystemColors.Control;
            this.lbl_select_face_cmp_img.Font = new System.Drawing.Font("굴림", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lbl_select_face_cmp_img.ForeColor = System.Drawing.Color.Red;
            this.lbl_select_face_cmp_img.Location = new System.Drawing.Point(7, 16);
            this.lbl_select_face_cmp_img.Name = "lbl_select_face_cmp_img";
            this.lbl_select_face_cmp_img.Size = new System.Drawing.Size(241, 23);
            this.lbl_select_face_cmp_img.TabIndex = 17;
            this.lbl_select_face_cmp_img.Text = "Select Compare Img ";
            this.lbl_select_face_cmp_img.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lbl_select_face_cmp_img.Visible = false;
            // 
            // imglst_face_compare
            // 
            this.imglst_face_compare.ColorDepth = System.Windows.Forms.ColorDepth.Depth32Bit;
            this.imglst_face_compare.ImageSize = new System.Drawing.Size(64, 64);
            this.imglst_face_compare.TransparentColor = System.Drawing.Color.Transparent;
            // 
            // btn_open_cmp_img_folder
            // 
            this.btn_open_cmp_img_folder.Location = new System.Drawing.Point(140, 157);
            this.btn_open_cmp_img_folder.Name = "btn_open_cmp_img_folder";
            this.btn_open_cmp_img_folder.Size = new System.Drawing.Size(56, 23);
            this.btn_open_cmp_img_folder.TabIndex = 18;
            this.btn_open_cmp_img_folder.Text = "Open";
            this.btn_open_cmp_img_folder.UseVisualStyleBackColor = true;
            this.btn_open_cmp_img_folder.Click += new System.EventHandler(this.button4_Click);
            // 
            // btn_reload_compare_img
            // 
            this.btn_reload_compare_img.Location = new System.Drawing.Point(198, 159);
            this.btn_reload_compare_img.Name = "btn_reload_compare_img";
            this.btn_reload_compare_img.Size = new System.Drawing.Size(67, 22);
            this.btn_reload_compare_img.TabIndex = 19;
            this.btn_reload_compare_img.Text = "Reload";
            this.btn_reload_compare_img.UseVisualStyleBackColor = true;
            this.btn_reload_compare_img.Click += new System.EventHandler(this.btn_reload_compare_img_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.cap_src_select);
            this.groupBox2.Location = new System.Drawing.Point(17, 77);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(277, 260);
            this.groupBox2.TabIndex = 21;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "[2] Select Capture Source";
            // 
            // cap_src_select
            // 
            this.cap_src_select.Controls.Add(this.cap_src_usb_cam);
            this.cap_src_select.Controls.Add(this.cap_src_ip_cam);
            this.cap_src_select.Location = new System.Drawing.Point(6, 19);
            this.cap_src_select.Name = "cap_src_select";
            this.cap_src_select.SelectedIndex = 0;
            this.cap_src_select.Size = new System.Drawing.Size(265, 235);
            this.cap_src_select.TabIndex = 30;
            this.cap_src_select.SelectedIndexChanged += new System.EventHandler(this.cap_src_select_SelectedIndexChanged);
            // 
            // cap_src_usb_cam
            // 
            this.cap_src_usb_cam.BackColor = System.Drawing.Color.Linen;
            this.cap_src_usb_cam.Controls.Add(this.usb_cam_cap_info);
            this.cap_src_usb_cam.Controls.Add(this.btn_select_camera_res);
            this.cap_src_usb_cam.Controls.Add(this.btn_open_usb_cam);
            this.cap_src_usb_cam.Location = new System.Drawing.Point(4, 22);
            this.cap_src_usb_cam.Name = "cap_src_usb_cam";
            this.cap_src_usb_cam.Padding = new System.Windows.Forms.Padding(3);
            this.cap_src_usb_cam.Size = new System.Drawing.Size(257, 209);
            this.cap_src_usb_cam.TabIndex = 0;
            this.cap_src_usb_cam.Text = "USB-CAM";
            // 
            // usb_cam_cap_info
            // 
            this.usb_cam_cap_info.BackColor = System.Drawing.SystemColors.Control;
            this.usb_cam_cap_info.Font = new System.Drawing.Font("굴림", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.usb_cam_cap_info.ForeColor = System.Drawing.Color.Red;
            this.usb_cam_cap_info.Location = new System.Drawing.Point(6, 7);
            this.usb_cam_cap_info.Name = "usb_cam_cap_info";
            this.usb_cam_cap_info.Size = new System.Drawing.Size(245, 23);
            this.usb_cam_cap_info.TabIndex = 22;
            this.usb_cam_cap_info.Text = "Desired Res:  ";
            this.usb_cam_cap_info.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btn_select_camera_res
            // 
            this.btn_select_camera_res.Enabled = false;
            this.btn_select_camera_res.Font = new System.Drawing.Font("굴림", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btn_select_camera_res.Location = new System.Drawing.Point(162, 33);
            this.btn_select_camera_res.Name = "btn_select_camera_res";
            this.btn_select_camera_res.Size = new System.Drawing.Size(89, 21);
            this.btn_select_camera_res.TabIndex = 21;
            this.btn_select_camera_res.Text = "Resolve All";
            this.btn_select_camera_res.UseVisualStyleBackColor = true;
            // 
            // btn_open_usb_cam
            // 
            this.btn_open_usb_cam.Location = new System.Drawing.Point(4, 180);
            this.btn_open_usb_cam.Name = "btn_open_usb_cam";
            this.btn_open_usb_cam.Size = new System.Drawing.Size(247, 23);
            this.btn_open_usb_cam.TabIndex = 3;
            this.btn_open_usb_cam.Text = "Open USB Cam";
            this.btn_open_usb_cam.UseVisualStyleBackColor = true;
            this.btn_open_usb_cam.Click += new System.EventHandler(this.btn_open_usb_cam_Click);
            // 
            // cap_src_ip_cam
            // 
            this.cap_src_ip_cam.BackColor = System.Drawing.Color.Honeydew;
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_stream_name);
            this.cap_src_ip_cam.Controls.Add(this.label9);
            this.cap_src_ip_cam.Controls.Add(this.label8);
            this.cap_src_ip_cam.Controls.Add(this.textBox2);
            this.cap_src_ip_cam.Controls.Add(this.label7);
            this.cap_src_ip_cam.Controls.Add(this.textBox1);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_port);
            this.cap_src_ip_cam.Controls.Add(this.label6);
            this.cap_src_ip_cam.Controls.Add(this.label5);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_ip);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_open);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_frame_proto);
            this.cap_src_ip_cam.Controls.Add(this.label4);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_model);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_model_add);
            this.cap_src_ip_cam.Controls.Add(this.cap_src_ip_cam_model_del);
            this.cap_src_ip_cam.Location = new System.Drawing.Point(4, 22);
            this.cap_src_ip_cam.Name = "cap_src_ip_cam";
            this.cap_src_ip_cam.Padding = new System.Windows.Forms.Padding(3);
            this.cap_src_ip_cam.Size = new System.Drawing.Size(257, 209);
            this.cap_src_ip_cam.TabIndex = 1;
            this.cap_src_ip_cam.Text = "IP-CAM";
            // 
            // cap_src_ip_cam_stream_name
            // 
            this.cap_src_ip_cam_stream_name.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cap_src_ip_cam_stream_name.FormattingEnabled = true;
            this.cap_src_ip_cam_stream_name.Items.AddRange(new object[] {
            "0",
            "1",
            "2"});
            this.cap_src_ip_cam_stream_name.Location = new System.Drawing.Point(63, 89);
            this.cap_src_ip_cam_stream_name.Name = "cap_src_ip_cam_stream_name";
            this.cap_src_ip_cam_stream_name.Size = new System.Drawing.Size(123, 20);
            this.cap_src_ip_cam_stream_name.TabIndex = 13;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(12, 94);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(45, 12);
            this.label9.TabIndex = 12;
            this.label9.Text = "Stream";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(131, 65);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(23, 12);
            this.label8.TabIndex = 11;
            this.label8.Text = "PW";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(169, 61);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(82, 21);
            this.textBox2.TabIndex = 10;
            this.textBox2.UseSystemPasswordChar = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(11, 65);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(16, 12);
            this.label7.TabIndex = 9;
            this.label7.Text = "ID";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(33, 61);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(86, 21);
            this.textBox1.TabIndex = 8;
            // 
            // cap_src_ip_cam_port
            // 
            this.cap_src_ip_cam_port.Location = new System.Drawing.Point(197, 33);
            this.cap_src_ip_cam_port.Name = "cap_src_ip_cam_port";
            this.cap_src_ip_cam_port.Size = new System.Drawing.Size(54, 21);
            this.cap_src_ip_cam_port.TabIndex = 7;
            this.cap_src_ip_cam_port.TextChanged += new System.EventHandler(this.cap_src_ipcam_port_on_text_changed);
            this.cap_src_ip_cam_port.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cap_src_ipcam_port_on_keypress);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(152, 38);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(38, 12);
            this.label6.TabIndex = 6;
            this.label6.Text = "PORT";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 38);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(16, 12);
            this.label5.TabIndex = 5;
            this.label5.Text = "IP";
            // 
            // cap_src_ip_cam_ip
            // 
            this.cap_src_ip_cam_ip.Location = new System.Drawing.Point(34, 34);
            this.cap_src_ip_cam_ip.Name = "cap_src_ip_cam_ip";
            this.cap_src_ip_cam_ip.Size = new System.Drawing.Size(112, 21);
            this.cap_src_ip_cam_ip.TabIndex = 4;
            this.cap_src_ip_cam_ip.Text = "192.168.0.100";
            this.cap_src_ip_cam_ip.TextChanged += new System.EventHandler(this.cap_src_ipcam_ip_on_text_changed);
            this.cap_src_ip_cam_ip.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.cap_src_ipcam_ip_on_keypress);
            // 
            // cap_src_ip_cam_open
            // 
            this.cap_src_ip_cam_open.Enabled = false;
            this.cap_src_ip_cam_open.Location = new System.Drawing.Point(6, 178);
            this.cap_src_ip_cam_open.Name = "cap_src_ip_cam_open";
            this.cap_src_ip_cam_open.Size = new System.Drawing.Size(245, 24);
            this.cap_src_ip_cam_open.TabIndex = 3;
            this.cap_src_ip_cam_open.Text = "Open IP Camera";
            this.cap_src_ip_cam_open.UseVisualStyleBackColor = true;
            this.cap_src_ip_cam_open.Click += new System.EventHandler(this.cap_src_ipcam_open_Click);
            // 
            // cap_src_ip_cam_frame_proto
            // 
            this.cap_src_ip_cam_frame_proto.Controls.Add(this.cap_src_ip_cam_frame_proto_mjpeg);
            this.cap_src_ip_cam_frame_proto.Controls.Add(this.cap_src_ip_cam_frame_proto_rtsp);
            this.cap_src_ip_cam_frame_proto.Location = new System.Drawing.Point(6, 115);
            this.cap_src_ip_cam_frame_proto.Name = "cap_src_ip_cam_frame_proto";
            this.cap_src_ip_cam_frame_proto.SelectedIndex = 0;
            this.cap_src_ip_cam_frame_proto.Size = new System.Drawing.Size(245, 58);
            this.cap_src_ip_cam_frame_proto.TabIndex = 2;
            this.cap_src_ip_cam_frame_proto.SelectedIndexChanged += new System.EventHandler(this.cap_src_ip_cam_frame_proto_on_selected_index_changed);
            // 
            // cap_src_ip_cam_frame_proto_mjpeg
            // 
            this.cap_src_ip_cam_frame_proto_mjpeg.Controls.Add(this.cap_src_ip_cam_mjpeg_url);
            this.cap_src_ip_cam_frame_proto_mjpeg.Location = new System.Drawing.Point(4, 22);
            this.cap_src_ip_cam_frame_proto_mjpeg.Name = "cap_src_ip_cam_frame_proto_mjpeg";
            this.cap_src_ip_cam_frame_proto_mjpeg.Padding = new System.Windows.Forms.Padding(3);
            this.cap_src_ip_cam_frame_proto_mjpeg.Size = new System.Drawing.Size(237, 32);
            this.cap_src_ip_cam_frame_proto_mjpeg.TabIndex = 0;
            this.cap_src_ip_cam_frame_proto_mjpeg.Text = "MJPEG";
            this.cap_src_ip_cam_frame_proto_mjpeg.UseVisualStyleBackColor = true;
            // 
            // cap_src_ip_cam_mjpeg_url
            // 
            this.cap_src_ip_cam_mjpeg_url.Location = new System.Drawing.Point(6, 4);
            this.cap_src_ip_cam_mjpeg_url.Multiline = true;
            this.cap_src_ip_cam_mjpeg_url.Name = "cap_src_ip_cam_mjpeg_url";
            this.cap_src_ip_cam_mjpeg_url.Size = new System.Drawing.Size(225, 23);
            this.cap_src_ip_cam_mjpeg_url.TabIndex = 0;
            this.cap_src_ip_cam_mjpeg_url.WordWrap = false;
            // 
            // cap_src_ip_cam_frame_proto_rtsp
            // 
            this.cap_src_ip_cam_frame_proto_rtsp.Controls.Add(this.cap_src_ip_cam_rtsp_url);
            this.cap_src_ip_cam_frame_proto_rtsp.Location = new System.Drawing.Point(4, 22);
            this.cap_src_ip_cam_frame_proto_rtsp.Name = "cap_src_ip_cam_frame_proto_rtsp";
            this.cap_src_ip_cam_frame_proto_rtsp.Padding = new System.Windows.Forms.Padding(3);
            this.cap_src_ip_cam_frame_proto_rtsp.Size = new System.Drawing.Size(237, 32);
            this.cap_src_ip_cam_frame_proto_rtsp.TabIndex = 1;
            this.cap_src_ip_cam_frame_proto_rtsp.Text = "RTSP";
            this.cap_src_ip_cam_frame_proto_rtsp.UseVisualStyleBackColor = true;
            // 
            // cap_src_ip_cam_rtsp_url
            // 
            this.cap_src_ip_cam_rtsp_url.Location = new System.Drawing.Point(5, 5);
            this.cap_src_ip_cam_rtsp_url.Multiline = true;
            this.cap_src_ip_cam_rtsp_url.Name = "cap_src_ip_cam_rtsp_url";
            this.cap_src_ip_cam_rtsp_url.Size = new System.Drawing.Size(226, 21);
            this.cap_src_ip_cam_rtsp_url.TabIndex = 0;
            this.cap_src_ip_cam_rtsp_url.WordWrap = false;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 11);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(40, 12);
            this.label4.TabIndex = 1;
            this.label4.Text = "Model";
            // 
            // cap_src_ip_cam_model
            // 
            this.cap_src_ip_cam_model.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cap_src_ip_cam_model.FormattingEnabled = true;
            // 목록은 Form1_Load 에서 ipcam_models.json 을 읽어 채운다.
            this.cap_src_ip_cam_model.Location = new System.Drawing.Point(63, 7);
            this.cap_src_ip_cam_model.Name = "cap_src_ip_cam_model";
            this.cap_src_ip_cam_model.Size = new System.Drawing.Size(134, 20);
            this.cap_src_ip_cam_model.TabIndex = 0;
            this.cap_src_ip_cam_model.SelectedIndexChanged += new System.EventHandler(this.cap_src_ip_cam_model_on_selected_index_changed);
            //
            // cap_src_ip_cam_model_add
            //
            this.cap_src_ip_cam_model_add.Location = new System.Drawing.Point(200, 6);
            this.cap_src_ip_cam_model_add.Name = "cap_src_ip_cam_model_add";
            this.cap_src_ip_cam_model_add.Size = new System.Drawing.Size(24, 22);
            this.cap_src_ip_cam_model_add.TabIndex = 14;
            this.cap_src_ip_cam_model_add.Text = "+";
            this.cap_src_ip_cam_model_add.UseVisualStyleBackColor = true;
            this.cap_src_ip_cam_model_add.Click += new System.EventHandler(this.cap_src_ip_cam_model_add_Click);
            //
            // cap_src_ip_cam_model_del
            //
            this.cap_src_ip_cam_model_del.Location = new System.Drawing.Point(227, 6);
            this.cap_src_ip_cam_model_del.Name = "cap_src_ip_cam_model_del";
            this.cap_src_ip_cam_model_del.Size = new System.Drawing.Size(24, 22);
            this.cap_src_ip_cam_model_del.TabIndex = 15;
            this.cap_src_ip_cam_model_del.Text = "-";
            this.cap_src_ip_cam_model_del.UseVisualStyleBackColor = true;
            this.cap_src_ip_cam_model_del.Click += new System.EventHandler(this.cap_src_ip_cam_model_del_Click);
            //
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.btn_feature_explorer);
            this.groupBox3.Controls.Add(this.btn_get_img_score);
            this.groupBox3.Controls.Add(this.lbl_select_face_cmp_img);
            this.groupBox3.Controls.Add(this.lv_face_compare_img);
            this.groupBox3.Controls.Add(this.btn_reload_compare_img);
            this.groupBox3.Controls.Add(this.btn_open_cmp_img_folder);
            this.groupBox3.Location = new System.Drawing.Point(17, 356);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(279, 190);
            this.groupBox3.TabIndex = 22;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "[3] Select Compare Image";
            // 
            // btn_feature_explorer
            // 
            this.btn_feature_explorer.Location = new System.Drawing.Point(70, 157);
            this.btn_feature_explorer.Name = "btn_feature_explorer";
            this.btn_feature_explorer.Size = new System.Drawing.Size(64, 23);
            this.btn_feature_explorer.TabIndex = 24;
            this.btn_feature_explorer.Text = "Feature";
            this.btn_feature_explorer.UseVisualStyleBackColor = true;
            this.btn_feature_explorer.Click += new System.EventHandler(this.btn_feature_explorer_Click);
            // 
            // btn_get_img_score
            // 
            this.btn_get_img_score.Location = new System.Drawing.Point(8, 157);
            this.btn_get_img_score.Name = "btn_get_img_score";
            this.btn_get_img_score.Size = new System.Drawing.Size(56, 23);
            this.btn_get_img_score.TabIndex = 23;
            this.btn_get_img_score.Text = "Score";
            this.btn_get_img_score.Click += new System.EventHandler(this.btn_get_img_score_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.btn_load_facesdk);
            this.groupBox4.Location = new System.Drawing.Point(16, 12);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(278, 48);
            this.groupBox4.TabIndex = 23;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "[1] FaceSDK";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.btn_rec_open_folder);
            this.groupBox5.Controls.Add(this.lbl_rec_st);
            this.groupBox5.Controls.Add(this.btn_rec_stop);
            this.groupBox5.Controls.Add(this.btn_rec_start);
            this.groupBox5.Location = new System.Drawing.Point(17, 644);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(277, 76);
            this.groupBox5.TabIndex = 26;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Record";
            // 
            // btn_rec_open_folder
            // 
            this.btn_rec_open_folder.Location = new System.Drawing.Point(139, 44);
            this.btn_rec_open_folder.Name = "btn_rec_open_folder";
            this.btn_rec_open_folder.Size = new System.Drawing.Size(100, 23);
            this.btn_rec_open_folder.TabIndex = 27;
            this.btn_rec_open_folder.Text = "Open Folder";
            this.btn_rec_open_folder.UseVisualStyleBackColor = true;
            this.btn_rec_open_folder.Click += new System.EventHandler(this.btn_rec_open_folder_Click);
            // 
            // lbl_rec_st
            // 
            this.lbl_rec_st.Location = new System.Drawing.Point(9, 17);
            this.lbl_rec_st.Name = "lbl_rec_st";
            this.lbl_rec_st.Size = new System.Drawing.Size(231, 24);
            this.lbl_rec_st.TabIndex = 26;
            // 
            // btn_rec_stop
            // 
            this.btn_rec_stop.Enabled = false;
            this.btn_rec_stop.Location = new System.Drawing.Point(68, 44);
            this.btn_rec_stop.Name = "btn_rec_stop";
            this.btn_rec_stop.Size = new System.Drawing.Size(53, 23);
            this.btn_rec_stop.TabIndex = 25;
            this.btn_rec_stop.Text = "Stop";
            this.btn_rec_stop.UseVisualStyleBackColor = true;
            this.btn_rec_stop.Click += new System.EventHandler(this.btn_rec_stop_Click);
            // 
            // btn_rec_start
            // 
            this.btn_rec_start.Location = new System.Drawing.Point(9, 43);
            this.btn_rec_start.Name = "btn_rec_start";
            this.btn_rec_start.Size = new System.Drawing.Size(53, 25);
            this.btn_rec_start.TabIndex = 24;
            this.btn_rec_start.Text = "Start";
            this.btn_rec_start.UseVisualStyleBackColor = true;
            this.btn_rec_start.Click += new System.EventHandler(this.btn_rec_start_Click);
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.label3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.label3.Location = new System.Drawing.Point(948, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(244, 25);
            this.label3.TabIndex = 27;
            this.label3.Text = "Detected Face Info";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ppg_face_info
            // 
            this.ppg_face_info.Font = new System.Drawing.Font("굴림", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.ppg_face_info.HelpVisible = false;
            this.ppg_face_info.Location = new System.Drawing.Point(948, 40);
            this.ppg_face_info.Name = "ppg_face_info";
            this.ppg_face_info.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.ppg_face_info.Size = new System.Drawing.Size(244, 680);
            this.ppg_face_info.TabIndex = 28;
            this.ppg_face_info.ToolbarVisible = false;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Location = new System.Drawing.Point(0, 733);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1192, 22);
            this.statusStrip1.TabIndex = 29;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1192, 755);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.ppg_face_info);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.comboBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ppg_settings);
            this.Controls.Add(this.ppg_liveness);
            this.Name = "Form1";
            this.Text = "FaceSDK Sample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.cap_src_select.ResumeLayout(false);
            this.cap_src_usb_cam.ResumeLayout(false);
            this.cap_src_ip_cam.ResumeLayout(false);
            this.cap_src_ip_cam.PerformLayout();
            this.cap_src_ip_cam_frame_proto.ResumeLayout(false);
            this.cap_src_ip_cam_frame_proto_mjpeg.ResumeLayout(false);
            this.cap_src_ip_cam_frame_proto_mjpeg.PerformLayout();
            this.cap_src_ip_cam_frame_proto_rtsp.ResumeLayout(false);
            this.cap_src_ip_cam_frame_proto_rtsp.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox4.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btn_load_facesdk;
        private System.Windows.Forms.Button btn_demo_passive_liveness_best4_feature_faceserver_start;
        private System.Windows.Forms.Button btn_demo_passive_liveness_best4_feature_faceserver_stop;
        private System.Windows.Forms.PropertyGrid ppg_liveness;
        private System.Windows.Forms.PropertyGrid ppg_settings;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ListView lv_face_compare_img;
        private System.Windows.Forms.Label lbl_select_face_cmp_img;
        private System.Windows.Forms.ImageList imglst_face_compare;
        private System.Windows.Forms.Button btn_open_cmp_img_folder;
        private System.Windows.Forms.Button btn_reload_compare_img;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.Button btn_rec_open_folder;
        private System.Windows.Forms.Label lbl_rec_st;
        private System.Windows.Forms.Button btn_rec_stop;
        private System.Windows.Forms.Button btn_rec_start;
        private System.Windows.Forms.Button btn_get_img_score;
        private System.Windows.Forms.Button btn_feature_explorer;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.PropertyGrid ppg_face_info;
        private System.Windows.Forms.TabControl cap_src_select;
        private System.Windows.Forms.TabPage cap_src_usb_cam;
        private System.Windows.Forms.Label usb_cam_cap_info;
        private System.Windows.Forms.Button btn_select_camera_res;
        private System.Windows.Forms.Button btn_open_usb_cam;
        private System.Windows.Forms.TabPage cap_src_ip_cam;
        private System.Windows.Forms.Button cap_src_ip_cam_open;
        private System.Windows.Forms.TabControl cap_src_ip_cam_frame_proto;
        private System.Windows.Forms.TabPage cap_src_ip_cam_frame_proto_mjpeg;
        private System.Windows.Forms.TabPage cap_src_ip_cam_frame_proto_rtsp;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox cap_src_ip_cam_model;
        private System.Windows.Forms.Button cap_src_ip_cam_model_add;
        private System.Windows.Forms.Button cap_src_ip_cam_model_del;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.TextBox cap_src_ip_cam_port;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox cap_src_ip_cam_ip;
        private System.Windows.Forms.TextBox cap_src_ip_cam_mjpeg_url;
        private System.Windows.Forms.TextBox cap_src_ip_cam_rtsp_url;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.ComboBox cap_src_ip_cam_stream_name;
        private System.Windows.Forms.Label label9;
    }
}

