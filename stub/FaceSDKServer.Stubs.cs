/*
    FaceSDKServer.Stubs.cs — build-only stubs for the FaceServer REST client.

    Mirrors the public shape of the vendored FaceSDKSVR.cs (namespace
    Alchera.FaceSDK.FaceServer) so the application compiles. The real client —
    including the RSA+AES payload encryption backed by AlcheraEncryptCS.dll —
    ships with the SDK package and is not included in this repository.

    Every operation throws; the app cannot run against these stubs.
*/

using System;
using System.Net;
using System.Threading.Tasks;
using static Alchera.FaceSDK.FaceSDK;

namespace Alchera.FaceSDK.FaceServer
{
    public class FaceSvrException : Exception
    {
        public FaceSvrException(string msg) : base(msg) { }
    }

    //
    // payload encryption
    //

    public abstract class PayloadEncrypt
    {
        protected const string NOT_IMPL = "FaceServer stub: the real implementation ships with the SDK package.";

        public enum Err
        {
            NoError = 0,
            FailInitialize = 1,
            InvalidParam = 2,
            FailEncOper = 10,
        }

        private Err last_err_ = Err.FailInitialize;
        private string last_err_desc_ = "";

        protected bool initialized_ = false;
        public bool IsInitialized() { return initialized_; }

        public bool IsErr() { return last_err_ != Err.NoError; }
        public Err GetLastErr() { return last_err_; }

        public void SetLastErr(Err err, string err_desc = "")
        {
            last_err_ = err;
            last_err_desc_ = err_desc ?? "";
        }

        public string GetLastErrDesc() { return last_err_desc_; }

        private long last_enc_elapsed_ = 0;
        public void SetLastEncElapsedMS(long ms = 0) { last_enc_elapsed_ = ms; }
        public long GetLastEncElapsedMS() { return last_enc_elapsed_; }

        public class EncRSAAES
        {
            public EncRSAAES(string pub_key_b64, string timestamp)
            {
                pub_key_b64_ = pub_key_b64;
                timestamp_ = timestamp;
            }

            public readonly string pub_key_b64_; // x509.spki
            public readonly string timestamp_;
        }

        public static PayloadEncrypt Create(EncRSAAES prm)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        // operation
        public abstract bool EncryptAndWrapB64(out string out_enc_b64, byte[] data);
        public virtual bool GetEncParam1(out string out_param) { out_param = null; return false; }
        public virtual bool GetEncParam2(out string out_param) { out_param = null; return false; }
        public virtual bool GetEncParam3(out string out_param) { out_param = null; return false; }

        public static string GenMD5B64(string input)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    };

    //
    // api base
    //

    public abstract class Api
    {
        protected const string NOT_IMPL = "FaceServer stub: the real implementation ships with the SDK package.";

        public enum Error
        {
            NoError = 0,
            InvalidParam,
            FailMakeRequest,
            FailSendRequest,
            FailResponse,
            Unknown = 9999,
        }

        public enum Method
        {
            GET,
            POST,
            PUT,
            DELETE,
        }

        public const string RST_CODE_SUCCESS = "SUCC-0000";
        public const string CT_MultipartFormData = "multipart/form-data";

        public const int API_JPG_ENC_QUALITY_LIVENESS = 100;  // 80: 80%, 100: 100%

        public const string API_ATTACH_ZIP_FILENAME = "multiframe.zip";
        public const string API_ATTACH_ZIP_ENTRY_FILENAME_FMT = "{0}.jpg";
        public const int API_ATTACH_ZIP_ENTRY_FILENAME_START_IDX = 1;

        public const int API_TIMEOUT_DEFAULT_MS = 30000; // 30 seconds

        Error last_err_ = Error.NoError;
        string last_err_desc_ = "";
        long svr_request_elapsed_ms_ = 0;

        readonly Method method_;
        readonly string path_;
        readonly string query_;
        readonly string content_type_;
        int timeout_ms_;

        protected Api(string content_type, int timeout_ms, Method method, string path, string query = "")
        {
            content_type_ = content_type;
            timeout_ms_ = timeout_ms;
            method_ = method;
            path_ = path;
            query_ = query;
        }

        public Method method { get { return method_; } }
        public string path { get { return path_; } }
        public string query { get { return query_; } }
        public string content_type { get { return content_type_; } }

        public int timeout_ms
        {
            get { return timeout_ms_; }
            set { timeout_ms_ = value; }
        }

        public long GetSvrRequestElapsedMS() { return svr_request_elapsed_ms_; }
        public void SetSvrRequestElapsedMS(long elapsed) { svr_request_elapsed_ms_ = elapsed; }

        public bool IsErr() { return last_err_ == Error.NoError ? false : true; }
        public Error GetLastErr() { return last_err_; }
        public string GetLastErrDesc() { return last_err_desc_; }

        public void SetLastErr(Error err = Error.NoError, string err_desc = "", object err_payload = null)
        {
            last_err_ = err;
            last_err_desc_ = err_desc ?? "";
        }

        public void SetLastErrDesc(string err_desc, object err_payload = null)
        {
            last_err_desc_ = err_desc ?? "";
        }

        public enum ResPayloadType
        {
            Json,
            Text,
            Binary,
        }

        public HttpStatusCode res_http_st_code { get { throw new NotImplementedException(NOT_IMPL); } }
        public ResPayloadType res_payload_type { get { throw new NotImplementedException(NOT_IMPL); } }

        public object GetResponse()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public bool IsHttpResponseOK()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public HttpStatusCode GetHttpResponseCode()
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public bool SetResponse(object payload, ResPayloadType payload_type = ResPayloadType.Json,
            HttpStatusCode status = HttpStatusCode.OK)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static BlobImg[] EncodeBGRImgsToJpg(BlobImg[] bgr_imgs, int jpg_quality)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public static BlobImg EncodeBGRImgToJpg(BlobImg bgr_img, int jpg_quality)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        //
        // response payload types
        //

        public class ResThresholdInfo
        {
            public string fin_code { get; set; }
            public string fin_name { get; set; }
            public ResRangeInfo auto_approve { get; set; }
            public ResRangeInfo auto_reject { get; set; }
        }

        public class ResRangeInfo
        {
            public double min { get; set; }
            public double max { get; set; }
        }

        public class ResReturnMsg
        {
            public string return_code { get; set; }
            public string return_msg { get; set; }
        }

        public enum LivenessConfidenceResult
        {
            Pass,
            Fail = -1,
            Fail_With_Display_Detected = -2, // legacy bgr14 support
            Fail_Unknown = -9999,
        }

        public static LivenessConfidenceResult EvaluateLivenessConfidence(bool isLive, double confidence)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    //
    // POST /liveness/multiframe/v2
    //

    public class LivenessMulitFrameApi : Api
    {
        public LivenessMulitFrameApi(int timeout_ms = API_TIMEOUT_DEFAULT_MS)
            : base(Api.CT_MultipartFormData, timeout_ms, Method.POST, "/liveness/multiframe/v2")
        {
        }

        public class Req
        {
            // [Mandatory]
            public int frame_counts;                       // 4, number of image files for measuring liveness
            public string filename_extension = "jpg";
            public bool encrypt_mode = false;

            // [MandatoryIF(encrypt_mode = false)]
            public byte[] images;                          // zip stream: 1.jpg .. 4.jpg

            // [MandatoryIF(encrypt_mode = true)] enc=aes256-cbc, base64
            public string encrypted_images;
        }

        public class Res
        {
            public double confidence { get; set; }
            public bool is_live { get; set; }
            public ResThresholdInfo threshold_info { get; set; }
            public ResReturnMsg return_msg { get; set; }
        }

        public bool MakeReq(BlobImg[] liv_img_frames, PayloadEncrypt img_encryptor = null,
            string filename_extension = "jpg")
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public bool IsLivenessPass()
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    //
    // POST /compare
    //

    public class FaceCompareApi : Api
    {
        public FaceCompareApi(int timeout_ms = API_TIMEOUT_DEFAULT_MS)
            : base(Api.CT_MultipartFormData, timeout_ms, Method.POST, "/compare")
        {
        }

        public class Req
        {
            public bool encrypt_mode = false;

            public byte[] image1;
            public byte[] image2;

            public string encrypted_image1;
            public string encrypted_image2;
        }

        public class Res
        {
            public double similarity_confidence { get; set; }
            public int match_result { get; set; }
            public ResThresholdInfo threshold_info { get; set; }
            public ResReturnMsg return_msg { get; set; }
        }

        public bool MakeReq(BlobImg img1, BlobImg img2, PayloadEncrypt img_encryptor = null,
            bool validate_img1 = false, bool validate_img2 = true)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    //
    // connection
    //

    public class Conn
    {
        protected const string NOT_IMPL = "FaceServer stub: the real implementation ships with the SDK package.";

        public enum Error
        {
            NoError = 0,
            InvalidUri,
            FailConnect,
            Timeout,
            Unknown = 9999,
        }

        Error last_err_ = Error.NoError;

        public Conn(string host, string proto = "http", int port = 80)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Conn(string conn_uri)
        {
            throw new NotImplementedException(NOT_IMPL);
        }

        public Error GetLastErr() { return last_err_; }

        public string GetURL(Api api = null, bool use_cdn_proxy_cache_buster = false)
        {
            throw new NotImplementedException(NOT_IMPL);
        }
    }

    public class RestConn : Conn
    {
        public RestConn(string conn_uri, bool use_proxy = false, Uri proxy_uri = null)
            : base(conn_uri)
        {
        }

        public async Task<Api> SendRequestAsync(Api api)
        {
            await Task.Yield();
            throw new NotImplementedException(NOT_IMPL);
        }
    }
}
