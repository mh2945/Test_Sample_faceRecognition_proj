# IP camera integration & native dependency troubleshooting

대상: `FaceSDKSample` (.NET Framework 4.8 / WinForms / x64, `face-sdk-wrapper-samples.csproj`)
작성일: 2026-08-26

IP 카메라 연동 디버깅 과정에서 확인된 4건을 처리했다.

| # | 항목 | 상태 |
|---|---|---|
| 1 | 네이티브 DLL 로딩 실패 해결 | 완료 (재발 방지책은 미적용, 아래 참조) |
| 2 | IP 카메라 모델 목록 확장 (앱 내 추가/삭제) | 완료 |
| 3 | LIV-75 / LIV-80 임계치 갱신 | 완료 |
| 4 | 오류 코드 상세화 및 정리 | 완료 |

---

## 1. `DllNotFoundException: AlcheraFaceSDKCS.dll` 해결

### 증상

DLL 이 출력 폴더에 분명히 존재하는데도 P/Invoke 시점에 `System.DllNotFoundException` 이 발생했다.

### 원인

파일이 없어서가 아니라 **의존성 사슬이 끊겨** 있었다. `LoadLibraryEx` 로 직접 확인한 결과 Win32 error 126.

```
AlcheraFaceSDKCS.dll  ->  AlcheraFaceSDK.dll  ->  torch_cpu.dll, c10.dll  (없음)
```

`DllNotFoundException` 은 "그 DLL 이 없다" 가 아니라 "그 DLL 을 **로드할 수 없다**" 는 뜻이라, 실제로 빠진 것은 두 단계 아래의 LibTorch 런타임이었다.

### 조치

아래 6개를 `FaceSDK/lib/cpu/windows-x86_64/` 에서 출력 폴더(`bin/x64/Debug/`)로 복사했다.

```
torch_cpu.dll   c10.dll   fbgemm.dll   libiomp5md.dll   asmjit.dll   uv.dll
```

복사 후 `LoadLibraryEx` 재확인에서 `AlcheraFaceSDK.dll` / `AlcheraFaceSDKCS.dll` 모두 로드 성공.

### 재발 조건 (미해결 — 조치 필요)

이 문제는 **새 브랜치나 워크트리를 만들 때마다 반복된다.**

- `.gitignore` 27행 `*.dll`, 582행 `bin/` → 네이티브 SDK DLL 은 형상관리 대상이 아니다.
- `face-sdk-wrapper-samples.csproj` 에 이 DLL 들을 출력 폴더로 복사하는 단계(`Content` / `PostBuildEvent`)가 **없다.**

즉 새 작업 공간마다 수작업 복사가 필요하다. csproj 에 PostBuild 복사 단계를 추가하면 해소된다. 이번 범위에서는 적용하지 않았다.

> 참고: csproj 의 x64 설정에 `<Prefer32Bit>true</Prefer32Bit>` 가 남아 있다. `PlatformTarget=x64` 에서는 무시되지만, AnyCPU 로 빌드하면 32비트 프로세스가 되어 같은 증상이 다시 나타난다.

---

## 2. IP 카메라 모델 목록 확장

### 이전 구조

모델 목록이 `Form1.Designer.cs` 에 3개로 하드코딩되어 있었고, 모델별 동작은 `Form1.cs` 의 if/else 체인이었다. 새 벤더 장비를 추가하려면 코드 수정 + 재빌드가 필요했다.

또한 다음 문제가 있었다.

- **URL 칸이 write-only 였다.** 값을 채워 넣기만 하고 열기(Open) 시점에 다시 읽지 않아서, 사용자가 직접 입력한 주소가 무시됐다.
- **ID / PW 가 전달되지 않았다.** RTSP 생성자에 빈 문자열이 넘어가고 있었다.
- **Model 콤보에 이벤트가 없었다.** 모델을 바꿔도 IP 나 PORT 를 건드리기 전까지 URL 미리보기가 갱신되지 않았다.
- **RTSP 기본 포트가 5555** 였다 (표준은 554).

### 변경 후

모델은 **프로파일**이 되었고 `ipcam_models.json` 에 저장된다.

신규 파일:

| 파일 | 역할 |
|---|---|
| `sample/IPCamModelProfile.cs` | 프로파일 자료구조 + JSON 로드/저장 (`IPCamModelStore`) |
| `sample/IPCamModelEditDlg.cs` | 추가/편집 모달. 기존 `FeatureExplorerDlg` 처럼 `.Designer.cs` 없이 코드로 구성 |

UI 는 Model 콤보 오른쪽에 `[+]` / `[-]` 버튼이 붙었다.

- `[+]` : 현재 IP / PORT / URL 값에서 템플릿을 역산해 미리 채운 다이얼로그를 띄운다. 이름만 넣으면 새 모델이 된다. 동명이 있으면 덮어쓸지 묻는다.
- `[-]` : 선택된 모델을 삭제한다. **마지막 하나는 삭제되지 않는다** (목록이 비면 URL 자동 입력이 통째로 멈추기 때문).
- 추가/삭제 즉시 `ipcam_models.json` 에 저장되고 다음 실행에도 유지된다.

### 설정 파일

위치는 exe 옆이다. 이 앱이 `models` / `imgs\compare` 를 찾는 방식과 같다 (`AppDomain.CurrentDomain.BaseDirectory`).

```
bin/x64/Debug/ipcam_models.json
```

**파일이 없으면 내장 기본값으로 동작한다.** 즉 이 파일을 지워도 앱은 정상 동작하며, 기존 3개 모델이 그대로 나온다. 파일이 깨졌으면 경고를 띄우고 기본값으로 폴백한다.

```json
[
  {
    "name": "335N_CAM",
    "mjpeg_port": "80",
    "rtsp_port": "554",
    "mjpeg_url": "ws://{ip}{port}/ws",
    "rtsp_url": "rtsp://{ip}{port}/live/main",
    "keep_user_input": false
  },
  {
    "name": "CUSTOM",
    "mjpeg_port": "",
    "rtsp_port": "",
    "mjpeg_url": "",
    "rtsp_url": "",
    "keep_user_input": true
  }
]
```

| 필드 | 의미 |
|---|---|
| `name` | 콤보에 표시될 이름 |
| `mjpeg_port` / `rtsp_port` | 해당 탭 선택 시 PORT 칸에 채울 기본값. 비우면 PORT 를 건드리지 않는다 |
| `mjpeg_url` / `rtsp_url` | URL 템플릿. 비우면 UI 에 `NOT IMPL` 로 표시된다 |
| `keep_user_input` | `true` 면 PORT / URL 칸을 자동으로 덮어쓰지 않는다 (기존 `CUSTOM` 동작) |

템플릿 치환 토큰은 2개다.

- `{ip}` : IP 칸의 값
- `{port}` : PORT 칸이 비어 있으면 빈 문자열, 아니면 `:554` 처럼 **콜론을 포함한** 문자열

`HIKVISION` 기본 프로파일은 URL 템플릿을 비워 두었다. 실제 장비로 검증한 적이 없어서, 검증되지 않은 주소를 기본값처럼 제공하지 않기 위함이다. 필요하면 `[+]` 로 추가하면 된다.

### 함께 고친 것

- 열기 시점에 **URL 칸을 실제 접속 주소로 사용**한다. 비어 있거나 `NOT IMPL` 이면 기존 ip/port 조립 방식으로 폴백한다.
- **ID / PW 가 실제로 전달된다.** URL 에 계정이 없고 ID 가 입력되어 있으면 `rtsp://user:pw@host/...` 형태로 주입한다 (`CamCtx_IPCam.InjectCredentials`).
- **PW 칸이 마스킹된다** (`UseSystemPasswordChar`). 이전에는 평문으로 노출됐다.
- Model 콤보에 `SelectedIndexChanged` 를 연결해 **선택 즉시 URL 미리보기가 갱신**된다.
- RTSP 기본 포트 `5555` → `554`.
- IP 칸에 **호스트명 입력이 가능**해졌다. 이전에는 숫자와 `.` 만 허용해 DNS 이름을 쓸 수 없었다.
- `cap_src_ipcam_on_host_ip_changed()` 에 **재진입 가드**를 넣었다. 이 함수는 PORT 칸을 대입하고 그 `TextChanged` 가 같은 함수를 다시 부르는데, 지금까지는 "같은 값이면 WinForms 가 이벤트를 안 띄운다"는 우연에 기대고 있었다.

### 검증

`335N_CAM` 프로파일이 만들어내는 URL 이 이전 하드코딩 결과와 **문자열 단위로 동일**함을 확인했다.

| 입력 | 결과 |
|---|---|
| MJPEG, ip=169.254.10.20, port=80 | `ws://169.254.10.20:80/ws` |
| RTSP, ip=169.254.10.20, port=554 | `rtsp://169.254.10.20:554/live/main` |
| RTSP, port 없음 | `rtsp://169.254.10.20/live/main` |

저장 → 재로드 왕복에서 추가 모델과 `keep_user_input` 플래그가 보존되는 것도 확인했다.

---

## 3. LIV-75 / LIV-80 임계치 갱신

Standard 모델로 기준이 바뀌면서 두 임계치를 수정했다.

| 항목 | 위치 | 이전 | 이후 |
|---|---|---|---|
| **LIV-75** BGR Image Liveness | `FaceSDKPassiveLiv.cs:54` `LivenessThreshold.BGR_IMG_LIVENESS` | `0.8808F` | **`0.8598F`** |
| **LIV-80** Face Compare | `FaceSDK.cs:220` `FaceSDK.Params.THRESHOLD_FEATURE` | `0.2630F` | **`0.3466F`** |

`LIV-75` / `LIV-80` 은 상수 이름이 아니라 **PropertyGrid 의 카테고리 라벨**이다 (`sample/SamplePropDemo.cs`). `PropertySort.Categorized` 때문에 표시 순서를 강제하려고 대괄호 접두어를 쓴다.

`SamplePropDemo.cs` 는 수정하지 않았다. 두 PropertyGrid 속성이 위 상수를 초기값으로 참조하므로 자동 반영된다.

### 비교 방향

두 값은 비교 방향이 반대다.

| 항목 | 비교식 | 의미 | 이번 변경 방향 |
|---|---|---|---|
| LIV-75 | `confidence > threshold` → 통과 | 신뢰도 | 하향 → **완화** |
| LIV-80 | `distance < threshold` → 매치 | **거리** | 상향 → **완화** |

LIV-80 은 신뢰도가 아니라 특징 벡터 간 거리다. 따라서 값을 올리면 매칭이 느슨해진다(FAR 증가 방향). 두 값 모두 완화 방향이며, 모델 교체로 점수 분포가 이동한 상황과 일관된다.

### 함께 고친 것

`sample/DemoClientServer.cs:849` 가 PropertyGrid 값이 아니라 상수를 직접 읽고 있었다.

```csharp
// 이전 — UI 에서 임계치를 바꿔도 화면 오버레이 색상 판정은 컴파일 기본값을 씀
const float face_bgr_liv_threshold = LivenessThreshold.BGR_IMG_LIVENESS;

// 이후
float face_bgr_liv_threshold = prop_demo.LIV_THRESHOLD_BGR_IMAGE_LIVENESS_MIN;
```

### 빌드 주의

두 값 모두 `const` 라 **컴파일 시점에 호출부로 인라인된다.** 증분 빌드로는 `DemoClientServer.cs` / `SamplePropDemo.cs` 쪽에 새 값이 전파되지 않으므로 **전체 재빌드(Rebuild)가 필요하다.**

빌드된 어셈블리에서 리플렉션으로 두 값이 실제 반영됐음을 확인했다.

```
LIV-75 BGR_IMG_LIVENESS  = 0.8598
LIV-80 THRESHOLD_FEATURE = 0.3466
```

### 참고 — 기타 로컬 임계치

`FaceSDKPassiveLiv.cs` 의 `LivenessThreshold` 클래스가 로컬 게이트의 단일 출처다. 이번에 바꾸지 않은 값들:

| 상수 | 값 | 게이트 |
|---|---|---|
| `FACE_RATIO_WIDTH_MIN` / `_MAX` | 0.26 / 0.6 | LIV-10 |
| `CENTER_FACE_W_H_POS_RATIO` | 0.1 | LIV-20 |
| `FACE_YAW` / `PITCH` / `ROLL` | {-20,20} / {-10,25} / {-20,20} | LIV-30 |
| `ATTR_FACE_IS_MASKED` | 0.5 | LIV-40 |
| `FACE_FEATURE_QUALITY` | {63.96, 65.86} | LIV-50 |
| `FACE_ANTISPOOFING_QUALITY` | 0.5648 | LIV-60 |

---

## 4. 오류 코드 상세화

### 증상

IP 카메라 연결에 실패하면 원인을 알 수 없는 메시지만 나왔다.

```
[ERR] 1,
```

### 원인

`1` 은 `ERR.UNKNOWN` 으로, `last_err_` 필드의 **초기값이 한 번도 덮어써지지 않은 상태**였다.

1. `IPCam335N_MJpegWSClient.Connect()` / `IPCam335N_RTSPClient.Connect()` 가 실패 경로에서 `SetLastErr()` 를 **호출하지 않았다.**
2. `CamCtx_IPCam.OnOpenCapture` 가 `Connect(out err_desc)` 의 **`err_desc` 를 버렸다.**
3. 실제 소켓 오류가 담긴 `connect_task.Exception` 을 **읽지 않고 버렸다.**
4. 재연결 경로가 `Connect()` 가 저장한 정확한 코드를 `CONN_EXCEPT` 로 **덮어썼다.**
5. `SetLogLevel()` 이 어디에서도 호출되지 않아 `log_level_` 이 계속 0이었다. 두 클라이언트의 `LogInfo` / `LogDebug` 호출이 **전부 런타임에 죽어 있었고** `LogErr` 만 출력됐다.

### 조치

**에러 문자열 포맷을 한 곳으로 모았다.** 코드 번호와 이름을 `GetLastErrDesc()` 가 함께 낸다.

```csharp
// 이전:  "1, "
// 이후:  "14(CONN_EXCEPT), [Connect] Exception: ..."
return $"{(int)last_err_}({last_err_}), {last_err_desc_}";
```

**실패 사유를 실제로 채웠다.** `ConnectAndStartVideoSync` 의 8개 조기 반환 경로가 코드만 남기고 사유는 빈 문자열로 나가고 있었다. 특히 `connect_task.Exception` 은 소켓 오류 원문이 들어 있는 유일한 곳이었다.

```csharp
if (connect_task.IsFaulted)
{
    // connect_task.Exception 에 실제 소켓 오류가 들어 있다.
    err_desc = "connect faulted: " + UnwrapTaskException(connect_task.Exception).Message;
    return ERR.CONN_FAULTED;
}
```

**재연결 시 코드 덮어쓰기를 없앴다.** `TickConnectionLost()` 가 `Connect()` 가 방금 저장한 정확한 코드를 `CONN_EXCEPT` 로 뭉개던 것을 두 파일 모두에서 제거했다.

**로그를 켰다.** `CamCtx_IPCam` 이 클라이언트 생성 직후 로그 수준을 지정한다 (DEBUG 빌드 2, Release 0).

**비밀번호를 가린다.** 계정이 URL 에 주입되므로 오류/정보 표시에는 `MaskUrlPassword()` 를 거친다.

```
url=rtsp://admin:***@192.168.0.100:554/live/main
```

**조용한 실패 경로를 막았다.** `CamCtx_IPCam.OnOpenCapture` 의 `else` 분기가 메시지 없이 `false` 를 반환해, 전파되면 내용 없는 `"[ERR] "` 대화상자가 뜰 수 있었다.

**ack#2 검증 버그를 고쳤다.** `ConnectAndStartVideoSync` 의 `start_video` 응답 2단계 검증에서, ack#2 블록이 ack#1 의 변수(`start_video_ack_type`)를 검사하고 있었다. ack#2 의 메시지 타입이 실제로 검증되지 않았고 오류 문구도 `[ack#1]` 로 잘못 표기됐다.

```csharp
// 이전
if (start_video_ack_type != WebSocketMessageType.Text)
    err_desc = "[ack#1] receiving error, response is not text, ...";

// 이후
if (start_video_info_type != WebSocketMessageType.Text)
    err_desc = "[ack#2] receiving error, response is not text, ...";
```

### ERR 코드 표

`IPCam335N_MJpegWSClient.ERR`

| 값 | 이름 | 의미 |
|---|---|---|
| 0 | `OK` | 정상 |
| 1 | `UNKNOWN` | 초기값. 이 값이 보이면 어딘가 `SetLastErr` 누락 |
| 2 | `EXCEPT` | Tick 중 예외 |
| 3 | `SOCK_CLOSED` | 연결 직후 소켓이 열려 있지 않음 |
| 4 | `PENDING_CONNECT` | 재연결 대기 중 |
| 11 | `CONN_TIMEOUT` | 연결 타임아웃 |
| 12 | `CONN_FAULTED` | 연결 실패 (소켓 오류 원문 포함) |
| 13 | `CONN_CANCELED` | 연결 취소 |
| 14 | `CONN_EXCEPT` | 연결 중 예외 |
| 20~24 | `SEND_*` | `start_video` 전송 실패 |
| 31, 32 | `RECV_TIMEOUT`, `RECV_EXCEPT` | 수신 실패 |
| 100 | `WS_MSG_CLOSE` | 서버가 WebSocket 종료 |
| 200, 201 | `WS_START_VIDEO_ACK1/2` | `start_video` 응답 이상 |

`IPCam335N_RTSPClient.ERR` 는 `OK/UNKNOWN/EXCEPT/SOCK_CLOSED/PENDING_CONNECT/CONN_EXCEPT/RECV_*` 만 사용한다. **두 enum 의 숫자 체계가 완전히 같지는 않으므로**, 코드만 보지 말고 괄호 안 이름을 함께 봐야 한다.

### 정리한 항목

이번 작업으로 생기거나 죽은 코드만 정리했다.

| 대상 | 조치 |
|---|---|
| `CamCtx_IPCam.MergeErrDesc()` | 제거. 클라이언트가 항상 `SetLastErr` 를 호출하게 되면서 중복 방어가 됐다 |
| `UrlInfo.ToUrl()` (45줄) | 제거. 접속에 원본 URL 을 쓰도록 바꾸면서 참조 0건이 됐다. URL 을 재조립하면 포트/경로가 원본과 달라지는 문제도 있었다 |
| RTSP 생성자의 계정 주입 로직 | `InjectCredentials()` 로 추출. `MaskUrlPassword()` 와 나란히 배치 |
| `using System.Text.RegularExpressions` / `using static ...MJpegWSClient` | 미사용. 제거 |
| `Form1.cs` catch 블록 | `Enabled = false` 직후 `= true` 하던 죽은 줄 제거 |
| `"NOT IMPL"` 매직 문자열 | `IPCamModelStore.URL_NOT_IMPL` 상수로 통일 |
| 자기메모식 주석 | 의도 설명으로 교체 |

---

## 알려진 한계 / 미결

### MJPEG 경로는 335N 전용이다

MJPEG 탭은 **모델과 무관하게** 항상 `IPCam335N_MJpegWSClient` 를 쓰고 335N 고유의 `{"start_video":{"ch":N}}` 명령을 보낸다. 따라서 **새로 추가한 모델은 RTSP 에서만 실제로 동작한다.** MJPEG 프로파일을 추가해도 다른 벤더의 WebSocket 프로토콜에는 맞지 않는다.

ID / PW 도 RTSP 생성자에만 전달되고 MJPEG 생성자는 받지 않는다.

### 335N 카메라 접근 주소

테스트에 쓴 335N(`MODEL-A`, MAC `AA:BB:CC:DD:EE:01`, 펌웨어 6.1.1.336)은 카메라 설정 화면에 표시되는 IP 주소로는 접근할 수 없다.

| 항목 | 값 | 접근 가능 |
|---|---|---|
| 설정된 IP 주소 | `10.0.0.50` | ✗ (이 PC 에서 라우팅 불가) |
| Zeroconf IP 주소 | `169.254.10.20` | ✓ |

직결 구간이라 DHCP / NTP 가 없고, 카메라 시각이 `1970-01-01` 로 남아 있는 것도 같은 이유다. **접속에는 Zeroconf 주소를 써야 한다.**

동작 확인된 설정: Model `335N_CAM`, IP `169.254.10.20`, RTSP 탭 (PORT 554 자동) → `1280x960 / h264` 수신, 인증 불필요.

이 펌웨어의 WebSocket 엔드포인트(`ws://169.254.10.20/test.ftweb_stream`)는 MJPEG 스트림이 아니라 **RTSP-over-WebSocket 터널**이었다. `{"start_video":...}` 에 `RTSP/1.0 400 Bad Request` 를 반환하고, `DESCRIBE` 는 `H264/90000` SDP 를 반환한다. 즉 앱의 MJPEG 경로는 이 장비의 다른 펌웨어를 위한 것이다.

### Vendor B 카메라 미검증

`192.168.0.100` (MAC `AA:BB:CC:DD:EE:02`, Vendor B) 는 **계정 미확보로 검증하지 못했다.**

- 80 / 554 포트 열림, 443 닫힘
- RTSP 가 **Digest MD5 인증**을 요구한다. `realm="RTSP_SERVER_aabbccddee02"`
- 335N 계정(`<account>`)으로는 401 이 반환된다
- 인증이 되기 전에는 스트림 경로를 확인할 수 없다

올바른 계정을 확보하면 `[+]` 로 프로파일을 추가하고 ID / PW 칸에 넣어 바로 시도할 수 있다.

### 범위 밖으로 남긴 발견 사항

이번 작업 범위(이번 변경분 정리)를 넘어서므로 손대지 않았다. 별도 티켓 대상.

| 위치 | 내용 |
|---|---|
| `sample/CamHelper/IPCam335N.cs` (20KB) | csproj 에 없는 죽은 파일. `IPCam335N_MJpeg.cs` 와 **동일 네임스페이스에 동일 타입을 중복 정의**한다. 프로젝트에 추가하면 즉시 CS0101 |
| `sample/CamHelper/IPCam335N-260517-01.back` (13KB) | 날짜 백업본이 소스 트리에 있음 |
| 두 클라이언트 | `update_stat`, `GetEpochMS`, `VideoInfo`, `SetLastErr` 3종, `SetConnectionLost`, `TickConnectionLost`, `Log*` 등 **약 180줄이 완전 중복**. 공통 베이스 클래스 추출 대상 |
| `CamCtx_IPCam.OnTick` | `err_desc` 2개와 `ERR` 반환값을 모두 버리고 무조건 `true` 반환 → **런타임 연결 끊김이 UI 에 전혀 보이지 않는다** |
| `CamCtx_IPCam.OnCaptureFrame` | 항상 `true` 반환. 큐가 비면 검은 프레임으로 대체 |
| `IPCam335N_RTSP.cs:355` | `stat_last_recv_bytes_ = 0;` — 누적이 아니라 상수 대입이라 `stat_recv_bytes_sec` 이 **항상 0**. `GetInfo()` 로 표시되는 잘못된 텔레메트리 |
| `IPCam335N_RTSP.Connect()` | `timeout_ms` 가 적용되지 않는다. OpenCvSharp 4.11 의 `VideoCaptureProperties` 에 타임아웃 항목이 없다 (주석으로 명시함) |
| `Form1.Designer.cs` | ID / PW 필드가 기본 이름 `textBox1` / `textBox2` 그대로 |
| `csproj:137` | `<PreBuildEvent>dotnet clean "$(SolutionDir)"</PreBuildEvent>` — 매 빌드마다 전체 clean. 또한 `$(SolutionDir)` 가 없어 **csproj 단독 빌드가 실패**한다 (솔루션으로 빌드해야 함) |

---

## 변경 파일

**신규**

```
sample/IPCamModelProfile.cs
sample/IPCamModelEditDlg.cs
docs/troubleshooting-native-dependency.md
```

**수정**

```
Form1.cs                              모델 프로파일 연동, URL/계정 전달, 재진입 가드, 호스트명 허용
Form1.Designer.cs                     [+]/[-] 버튼, 모델 이벤트 배선, PW 마스킹, 하드코딩 목록 제거
face-sdk-wrapper-samples.csproj       신규 파일 2개 등록
FaceSDK.cs                            LIV-80 임계치
FaceSDKPassiveLiv.cs                  LIV-75 임계치
sample/DemoClientServer.cs            오버레이 임계치가 PropertyGrid 값을 따르도록
sample/CamCtx_IPCam.cs                오류 전파 정리, InjectCredentials/MaskUrlPassword, 로그 레벨, 죽은 코드 제거
sample/CamHelper/IPCam335N_MJpeg.cs   오류 코드/사유 상세화
sample/CamHelper/IPCam335N_RTSP.cs    오류 코드/사유 상세화
```

**빌드 확인**

```
MSBuild face-sdk-wrapper-samples.sln -p:Configuration=Debug -p:Platform=x64 -t:Rebuild
→ 성공. 신규 경고 없음 (기존 경고 3건은 이번 변경과 무관)
```

> `dotnet clean "$(SolutionDir)"` PreBuildEvent 때문에 **csproj 단독 빌드는 실패한다.** 반드시 `.sln` 으로 빌드해야 한다.
> `.NET Framework` 내장 MSBuild 는 `LangVersion 7.3` 을 지원하지 않아 CS1617 이 난다. Visual Studio / Build Tools 의 MSBuild 를 써야 한다.
