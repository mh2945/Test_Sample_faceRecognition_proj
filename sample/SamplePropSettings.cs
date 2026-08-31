using Alchera.FaceSDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace FaceSDKSample.SampleProp    
{
    //
    // Prop
    //

    public abstract class Prop
    {
        public abstract Prop GetProp(string name);

        public class FloatArrayConverter : TypeConverter
        {
            public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
                => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

            public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            {
                if (destinationType == typeof(string) && value is float[] arr)
                    return string.Join(", ", arr.Select(f => f.ToString("0.###", CultureInfo.InvariantCulture)));
                return base.ConvertTo(context, culture, value, destinationType);
            }
        }

        public static void SetPropertyGridLabelWidth(PropertyGrid grid, int width)
        {
            var gridView = grid.GetType()
                .GetField("gridView", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(grid);

            if (gridView == null) return;

            var method = gridView.GetType()
                .GetMethod("MoveSplitterTo", BindingFlags.NonPublic | BindingFlags.Instance);

            method?.Invoke(gridView, new object[] { width });
        }

        public class NumericUpDownEditor : UITypeEditor
        {
            public override UITypeEditorEditStyle GetEditStyle(
                ITypeDescriptorContext context)
            {
                return UITypeEditorEditStyle.DropDown;
            }

            public override object EditValue(
                ITypeDescriptorContext context,
                IServiceProvider provider,
                object value)
            {
                var editorService = (IWindowsFormsEditorService)
                    provider.GetService(typeof(IWindowsFormsEditorService));

                if (editorService != null)
                {
                    NumericUpDown nud = new NumericUpDown();
                    nud.Minimum = 1;
                    nud.Maximum = int.MaxValue;
                    nud.Value = Convert.ToDecimal(value);

                    editorService.DropDownControl(nud);
                    return (int)nud.Value;
                }

                return value;
            }
        }

        public class ListStringConverter : StringConverter
        {
            private readonly string[] _values;

            public ListStringConverter(string[] values)
            {
                _values = values;
            }

            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;

            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(_values);
            }
        }

        public class DynamicListConverter : StringConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) => true;
            public override bool GetStandardValuesExclusive(ITypeDescriptorContext context) => true;

            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                if (context?.PropertyDescriptor == null)
                    return new StandardValuesCollection(Array.Empty<string>());

                var attr = (ListValuesAttribute)context.PropertyDescriptor
                    .Attributes[typeof(ListValuesAttribute)];

                return new StandardValuesCollection(attr?.Values ?? Array.Empty<string>());
            }
        }

        [AttributeUsage(AttributeTargets.Property)]
        public class ListValuesAttribute : Attribute
        {
            public string[] Values { get; }
            public ListValuesAttribute(params string[] values) => Values = values;
        }
    }

    public class Settings : Prop
    {
        public override Prop GetProp(string name)
        {
            return name == "Settings" ? this : null;
        }


        //
        // caputre
        //

        [ReadOnly(true)]
        [DisplayName("cap-type"), Category("[CAP-10] Capture Config")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("camera", "camera-nvr", "mov", "image")]
        public string CAP_TYPE { get; set; } = "camera";

        [DisplayName("cap-index-rgb"), Category("[CAP-10] Capture Config")]
        public int CAP_INDEX_RGB { get; set; } = 0;

        [Browsable(false)]
        [DisplayName("cap-index-ir"), Category("[CAP-10] Capture Config")]
        public int CAP_INDEX_IR { get; set; } = -1;

        
        [DisplayName("cap-prefer-width"), Category("[CAP-10] Capture Config")]
        public int CAP_PREFER_WIDTH { get; set; } = 1280;
        
        [DisplayName("cap-prefer-height"), Category("[CAP-10] Capture Config")]
        public int CAP_PREFER_HEIGHT { get; set; } = 720;

        [DisplayName("cap-flip-horizontal"), Category("[CAP-10] Capture Config")]
        public bool CAP_FLIP_HOR { get; set; } = false;


        [DisplayName("cap-width"), Category("[CAP-16] Desired Capture Resolution")]
        public int CAP_WIDTH { get; set; } = 1280;
                
        [DisplayName("cap-height"), Category("[CAP-16] Desired Capture Resolution")]
        public int CAP_HEIGHT { get; set; } = 720;


        public class _TC_OPT_CAP_OPENCV_CAP_API : TypeConverter
        {
            public override bool GetStandardValuesSupported(ITypeDescriptorContext context) { return true; } // value that showd at listbox
            public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
            {
                return new StandardValuesCollection(new string[] { "ANY", "MSMF", "DSHOW", "FFMPEG" });
            }
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                if (sourceType == typeof(string))
                    return true;
                return base.CanConvertFrom(context, sourceType);
            }
            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                if (value is string)
                    return value;
                return base.ConvertFrom(context, culture, value);
            }
        }

        //private string opt_cap_opencv_cap_api_ = "ANY";

        //[DisplayName("cap api"), Category("[CAP-11] Capture API"), TypeConverter(typeof(_TC_OPT_CAP_OPENCV_CAP_API))]
        //public string CAP_CAPTURE_API // OPENCV_CAP_API
        //{
        //    get { return opt_cap_opencv_cap_api_; }
        //    set { opt_cap_opencv_cap_api_ = value; }
        //}
        public string CAP_CAPTURE_API = "ANY";


        [DisplayName("degree"), Category("[CAP-20] Rotate Src")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("0", "90", "180", "270")]
        public string CAP_SRC_ROTATE { get; set; } = "0";
    
        [DisplayName("enable"), Category("[CAP-30] Check Src Minimum Siz")]
        public bool CAP_SRC_ENABLE_CHECK_MINIMUM_SIZ { get; set; } = true;
        [DisplayName("minWidth"), Category("[CAP-30] Check Src Minimum Siz")]
        public int CAP_SRC_MIN_WIDTH { get; set; } = LivenessThreshold.SRC_IMG_W_MIN;
        [DisplayName("minHeight"), Category("[CAP-30] Check Src Minimum Siz")]
        public int CAP_SRC_MIN_HEIGHT { get; set; } = LivenessThreshold.SRC_IMG_H_MIN;

        [DisplayName("enable"), Category("[CAP-40] Crop Src Margin By Aspect(H/W)")]
        public bool CAP_SRC_ENABLE_CROP_MARGIN_BY_ASPECT { get; set; } = true;

        [DisplayName("aspect_height"), Category("[CAP-40] Crop Src Margin By Aspect(H/W)")]
        public float CAP_SRC_CROP_MARGIN_ASPECT_HEIGHT { get; set; } = LivenessThreshold.SRC_CROP_ASPECT_HEIGHT;

        [DisplayName("aspect_width"), Category("[CAP-40] Crop Src Margin By Aspect(H/W)")]
        public float CAP_SRC_CROP_MARGIN_ASPECT_WIDTH { get; set; } = LivenessThreshold.SRC_CROP_ASPECT_WIDTH;


        //
        // Web Http Proxy
        //

        [DisplayName("use"), Category("[SVR-10] Http Proxy")]
        public bool  SVR_HTTP_PROXY_USE { get; set; } = false;
        [DisplayName("proxy-url"), Category("[SVR-10] Http Proxy")]
        public string SVR_HTTP_PROXY_URL { get; set; } = "http://127.0.0.1:8000";

        //
        // FaceServer
        //

        [DisplayName("api-url"), Category("[SVR-20] FaceServer")]
        public string SVR_FACESVR_API_URL { get; set; } = "https://face-server.example.com";

        [DisplayName("encrypt-type"), Category("[SVR-21] API Payload Encrypt")]
        [TypeConverter(typeof(DynamicListConverter))]
        [ListValues("NONE", "RSA+AES", "AES")]
        public string SVR_FACESVR_API_IMG_ENCRYPT_TYPE { get; set; } = "NONE";

        [DisplayName("rsa-aes-enc-ts"), Category("[SVR-21] API Payload Encrypt")]
        public string SVR_FACESVR_API_IMG_ENCRYPT_RSA_AES_TS { get; set; } = "00000000000000";

        [DisplayName("rsa-aes-pub-b64"), Category("[SVR-21] API Payload Encrypt")]
        public string SVR_FACESVR_API_IMG_ENCRYPT_RSA_AES_PUB_B64 { get; set; } = "";

        [DisplayName("auto-save-enc-img-file"), Category("[SVR-21] API Payload Encrypt")]
        public bool SVR_FACESVR_API_IMG_ENCRYPT_AUTO_SAVE_TO_FILE { get; set; } = false;


        //
        // Demo-UI
        //

        //[DisplayName("max_scr_brightness"), Category("Screen Brightness MAX"), Description("On the liveness screen, the screen brightness is kept at maximum.")]
        //public bool MAX_BRIGHTNESS_SCREEN_ON_LIVENESS { get; set; } = false;

        [DisplayName("show_top_msg"), Category("[UI-90] ETC")]
        public bool UI_ETC_TOP_MSG_SHOW { get; set; } = true;

        [DisplayName("color_histogram"), Category("[UI-90] ETC")]
        public bool UI_ETC_COLOR_HISTOGRAM_SHOW { get; set; } = false;

        [DisplayName("face_center_guide"), Category("[UI-90] ETC")]
        public bool UI_ETC_GUIDE_FACE_GUIDE_SHOW { get; set; } = false;


        //
        // Detected Face UI
        //

        //[DisplayName("draw_face_info"), Category("[UI-91] Detected Face UI")]
        //public bool UI_DETECTED_FACE_DRAW_INFO { get; set; } = true;


        //[DisplayName("draw_landmark"), Category("[UI-91] Detected Face UI")]
        //public bool UI_DETECTED_FACE_DRAW_LANDMARK { get; set; } = true;


        //
        // record property
        //

        [DisplayName("filename_prefix"), Category("[X-REC] Record")]
        public string UI_REC_FILENAME_PREFIX { get; set; } = "fsdk-";

        [DisplayName("fps"), Category("[X-REC] Record")]
        public int UI_REC_FPS { get; set; } = 30;

        [DisplayName("codec"), Category("[X-REC] Record")]
        public string UI_REC_CODEC { get; set; } = "h264";

        [DisplayName("rolling_siz_mb"), Category("[X-REC] Record")]
        public int UI_REC_ROLL_SIZ_MB { get; set; } = 100; // 100 MB


        //
        // hidden property
        //

        [Browsable(false)]
        public string EXEC_PATH { get; set; } = "";
        [Browsable(false)]
        public string COMPARE_IMG_PATH { get; set; } = "imgs\\compare";

        // rec
        [Browsable(false)]
        public string REC_PATH { get; } = "rec";
        [Browsable(false)]
        public string REC_FILE_TS_FMT { get; } = "yyyyMMdd-HHmmss";
    }
}
