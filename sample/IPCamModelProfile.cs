using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace FaceSDKSample.sample
{
    //
    // IP 카메라 모델 프로파일
    //
    // 모델은 접속 동작을 고르지 않는다. MJPEG / RTSP 중 무엇을 쓸지는 UI 탭이 정한다.
    // 모델이 정하는 것은 "URL 칸에 무엇을 미리 채워 넣을지" 뿐이다.
    //
    // URL 템플릿 치환 토큰
    //   {ip}   : IP 칸의 값
    //   {port} : PORT 칸이 비어 있으면 "", 아니면 ":554" 처럼 콜론을 포함한 문자열
    //
    public class IPCamModelProfile
    {
        public string name { get; set; } = "";

        // 탭 전환 시 PORT 칸에 채울 기본값. 비어 있으면 PORT 를 건드리지 않는다.
        public string mjpeg_port { get; set; } = "";
        public string rtsp_port { get; set; } = "";

        // URL 템플릿. 비어 있으면 URL 칸을 건드리지 않는다.
        public string mjpeg_url { get; set; } = "";
        public string rtsp_url { get; set; } = "";

        // true 면 PORT / URL 칸을 덮어쓰지 않는다. (사용자가 직접 입력한 값 보존)
        public bool keep_user_input { get; set; } = false;

        public string GetPort(bool is_mjpeg)
        {
            return is_mjpeg ? mjpeg_port : rtsp_port;
        }

        public string GetUrlTemplate(bool is_mjpeg)
        {
            return is_mjpeg ? mjpeg_url : rtsp_url;
        }

        // 템플릿에 ip / port 를 채워 실제 URL 을 만든다.
        public string BuildUrl(bool is_mjpeg, string ip, string port)
        {
            string tmpl = GetUrlTemplate(is_mjpeg);

            if (string.IsNullOrWhiteSpace(tmpl))
                return "";

            string port_str = string.IsNullOrWhiteSpace(port) ? "" : ":" + port;

            return tmpl.Replace("{ip}", ip ?? "").Replace("{port}", port_str);
        }

        // 실제 URL 에서 ip / port 를 토큰으로 되돌린다. ([+] 로 새 모델을 만들 때 사용)
        //
        // 치환은 authority 구간(스킴 뒤 ~ 첫 '/' 앞)에서만 한다. URL 전체에 걸면 IP 나 ":port"
        // 문자열이 경로에 우연히 나타났을 때 잘못된 템플릿이 만들어지고 그대로 JSON 에 저장된다.
        // "://" 가 없으면 authority 를 특정할 수 없으므로 원문을 그대로 돌려준다.
        public static string ToUrlTemplate(string url, string ip, string port)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "";

            string rst = url.Trim();

            int scheme_end = rst.IndexOf("://");
            if (scheme_end < 0)
                return rst;

            int authority_beg = scheme_end + 3;

            int authority_end = rst.IndexOf('/', authority_beg);
            if (authority_end < 0)
                authority_end = rst.Length;

            string authority = rst.Substring(authority_beg, authority_end - authority_beg);

            // 좁은 쪽(":554")을 먼저 걷어낸다. ip 안에 포트 문자열이 들어 있어도 안전하다.
            if (!string.IsNullOrWhiteSpace(port))
                authority = authority.Replace(":" + port, "{port}");

            if (!string.IsNullOrWhiteSpace(ip))
                authority = authority.Replace(ip, "{ip}");

            return rst.Substring(0, authority_beg) + authority + rst.Substring(authority_end);
        }
    }

    //
    // 프로파일 목록의 파일 입출력.
    //
    // 저장 위치는 exe 옆이다. 이 앱이 models / imgs\compare 를 찾는 방식과 같다.
    //
    public static class IPCamModelStore
    {
        public const string FILE_NAME = "ipcam_models.json";

        // URL 템플릿이 없는 모델을 UI 에 표시할 때 쓰는 문구.
        public const string URL_NOT_IMPL = "NOT IMPL";

        public static string GetFilePath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FILE_NAME);
        }

        // 설정 파일이 없을 때 쓰는 내장 기본값.
        public static List<IPCamModelProfile> GetDefaults()
        {
            return new List<IPCamModelProfile>
            {
                new IPCamModelProfile
                {
                    name = "335N_CAM",
                    mjpeg_port = "80",
                    rtsp_port = "554",
                    mjpeg_url = "ws://{ip}{port}/ws",
                    rtsp_url = "rtsp://{ip}{port}/live/main",
                },
                new IPCamModelProfile
                {
                    name = "HIKVISION",
                    mjpeg_port = "80",
                    rtsp_port = "554",
                    // 실제 장비로 검증하지 않았으므로 템플릿을 비워 둔다.
                    // 필요하면 [+] 로 추가하거나 ipcam_models.json 을 편집한다.
                    mjpeg_url = "",
                    rtsp_url = "",
                },
                new IPCamModelProfile
                {
                    name = "CUSTOM",
                    keep_user_input = true,
                },
            };
        }

        // 파일이 없거나 읽기에 실패하면 내장 기본값을 돌려준다.
        // 실패 사유는 err_desc 에 담아서 호출부가 알릴 수 있게 한다.
        public static List<IPCamModelProfile> Load(out string err_desc)
        {
            err_desc = "";

            string path = GetFilePath();

            try
            {
                if (!File.Exists(path))
                    return GetDefaults();

                var list = JsonConvert.DeserializeObject<List<IPCamModelProfile>>(File.ReadAllText(path));

                if (list != null)
                    list.RemoveAll(p => p == null || string.IsNullOrWhiteSpace(p.name));

                if (list == null || list.Count == 0)
                {
                    err_desc = $"Model list is empty. Using defaults.\nfile={path}";
                    return GetDefaults();
                }

                return list;
            }
            catch (Exception ex)
            {
                err_desc = $"Failed to load model list. Using defaults.\nfile={path}\n{ex.Message}";
                return GetDefaults();
            }
        }

        public static bool Save(List<IPCamModelProfile> list, out string err_desc)
        {
            err_desc = "";

            string path = GetFilePath();

            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(list, Formatting.Indented));
                return true;
            }
            catch (Exception ex)
            {
                err_desc = $"Failed to save model list.\nfile={path}\n{ex.Message}";
                return false;
            }
        }

        public static IPCamModelProfile Find(List<IPCamModelProfile> list, string name)
        {
            if (list == null || string.IsNullOrWhiteSpace(name))
                return null;

            return list.Find(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
