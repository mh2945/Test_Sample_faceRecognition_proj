# Architecture

How the pieces fit together. For build instructions and a tour of the interesting code, see
[`README.md`](README.md).

Everything is .NET Framework 4.8, **C# 7.3 only**, private fields `snake_case_`, locals
`snake_case`, types and methods `PascalCase`.

---

## Startup and ownership

```
Program.Main  →  Sample.inst.Create(cwd)  →  Application.Run(new Form1())
```

`Sample` (a partial class split across `sample/Sample.cs` and `sample/SampleDemoMan.cs`) is a
double-checked-locking singleton acting as a service locator. It holds the two long-lived objects,
reachable as the static `Sample.fsdk` and `Sample.cam`:

- **`SampleFaceSDKCtx`** (`sample/SampleFaceSDKCtx.cs`) — wraps `FaceSDK.Instance()` +
  `Initialize(models)`, times the init with a `Stopwatch`, and throws `SampleException` on failure.
  Created once; recreating is a no-op.
- **`CamCtx`** — the currently open capture. `OpenCapture()` disposes any previous one first.

The working directory matters: `Program.Main` passes `Directory.GetCurrentDirectory()` to
`Sample.Create()`, and the native SDK resolves `models\` relative to the process cwd. `ipcam_models.json`
follows the same exe-relative convention.

`Form1` enforces a three-step gate purely through button enabling:

```
Load FaceSDK  →  open a capture (USB or IP cam)  →  start the demo
```

Each step enables the next; nothing works out of order. `Form1` is the **only** designer-built form
and the only place that touches WinForms controls.

---

## Namespaces do not follow the folders

`sample/` spans three namespaces, and the split is not the directory structure:

| Namespace | Files |
|---|---|
| `FaceSDKSample` | `Sample.cs`, `SampleDemoMan.cs`, `SampleFaceSDKCtx.cs`, `SampleRec.cs`, `Demo.cs` |
| `FaceSDKSample.sample` | `CamCtx*.cs`, `CamHelper/*.cs`, `DemoClientServer*.cs`, `IPCam*.cs`, `FeatureExp*.cs` |
| `FaceSDKSample.SampleProp` | `SampleProp*.cs` |

`Form1.cs` has `using FaceSDKSample.sample;`, so the first two are both visible from the form.
Match the namespace of the file you are extending, not the directory.

---

## The demo thread

`Demo` (`sample/Demo.cs`) is an abstract background-thread host. The subclass overrides `Run()` and
exits cooperatively through `SetExit()` / `IsRunning()` / `Join(timeout)`. The constructor takes a
name and an opaque `user_prm` object retrieved with `GetUserPrm()`.

`DemoClientServer` is the only implementation, split across four partial files:

| File | Role |
|---|---|
| `DemoClientServer.cs` | the `Run()` loop and the `DemoStage` state machine (~2.2k lines) |
| `DemoClientServerCtx.cs` | `RunContext` / `RecContext` |
| `DemoClientServerMsg.cs` | `DispatchMessage()` — the inbound message switch |
| `DemoClientServerUI.cs` | overlay rendering helpers |

### The state machine

`DemoStage` (`sample/DemoClientServer.cs:293`), switched on at `:668` once per frame:

```
Init → DoPassiveLivenessAndCollectLivenessBestShot → BGRImageLivenessMulitframe
     → FaceCompare → FinalResult → Reset → (Init)
```

**The `Reset` stage** (`sample/DemoClientServer.cs:380`) reallocates `RunContext` but deliberately
**re-attaches the same `RecContext`** (`ctx.rec_ctx = rec_ctx` at `:383`), so a recording in
progress survives the loop and keeps running across subjects. It then calls
`ResetImgLivenessCheck()` (`:394`) to drop the SDK's BGR-liveness frame cache — mandatory when
reusing `GetImgLiveness()` across different people, and explained in the comment block at `:387`.
(`GetImgLivenessMulti()` is the cache-free alternative and needs no reset.)

---

## The UI ↔ demo contract

**All UI → demo communication goes through the queue.** `UserPrm.PostMsg(id, prm0, prm1, prm2)`
from the WinForms thread; the demo thread drains it with `DispatchMessage(ctx)` at the top of every
iteration. Message ids are bare integers:

| id | Meaning |
|---|---|
| `1000` | property update |
| `1100` | clear captured frames |
| `2000` | append UI status text |
| `3000` | stage result (sub-dispatched on a `cate` string **and** on the current stage) |
| `4000` | recording start / stop (`prm0` = `"start"` / `"stop"`) |
| `9999` | exit |

Demo → UI goes back through the `ui_onstart_` / `ui_onloop_` / `ui_onexit_` actions on `UserPrm`.
**Those run on the demo thread**, so their bodies must `BeginInvoke` onto the UI thread.
Never touch a control from the demo thread, and never use `Invoke` — the UI thread may already be
blocked inside `StopDemo`'s join loop, so `Invoke` deadlocks.

Property edits reach the demo thread via `Form1.PPG_*_PropertyValueChanged` →
`Sample.PostDemoPropEvent` → message `1000`.

---

## Configuration: PropertyGrid, not a config file

Runtime knobs live in `FaceSDKSample.SampleProp` as plain classes bound to WinForms `PropertyGrid`s.
`Prop` (the abstract base, `sample/SamplePropSettings.cs:20`) also carries the `TypeConverter`s and
the grid-label-width reflection hack.

- **`Settings`** (`SamplePropSettings.cs`) — capture resolution/index/flip, HTTP proxy, FaceServer
  URL, payload-encryption parameters, recording, and paths (`COMPARE_IMG_PATH` defaults to
  `imgs\compare`, plus `REC_PATH`, `EXEC_PATH`)
- **`DemoClientServerProp`** (`SamplePropDemo.cs`) — liveness and compare thresholds and toggles
- **`SamplePropFaceInfo`** (`SamplePropFaceInfo.cs`) — per-overlay display toggles

The `[LIV-xx]` / `[CAP-xx]` prefixes visible in the UI are **`Category(...)` label text, not
identifiers** — searching for "LIV-75" finds the group, not a constant. Grep by property name
(`LIV_THRESHOLD_BGR_IMAGE_LIVENESS_MIN`) or by the category string. The bracket prefix exists to
force ordering under `PropertySort.Categorized`.

**Defaults vs. runtime values.** Defaults live as `const` in `LivenessThreshold` and `FaceSDK.Params`;
the PropertyGrid properties merely use them as initialisers. The value actually in effect is the
PropertyGrid one, so **demo code must read `prop_demo.*`, never the `const` directly** — reading the
constant silently ignores whatever the user set.

`App.config` and `Settings.settings` are unused. The only persisted state is `ipcam_models.json`.

---

## Capture

`CamCtx` (`sample/CamCtx.cs`) is an abstract template. Subclasses implement `OnOpenCapture` /
`OnCaptureFrame` / `OnTick` / `OnCloseCapture` / `OnIsErr` / `GetInfo`, and may override
`Exec(key, prm1, prm2)` (used for `"clear_cap_frame"`).

It doubles as the home for the static OpenCvSharp drawing and image helpers the overlay renderer
uses — `DrawText`, `DrawBoxXYWH`, `DrawLogBox`, `DrawImgScaled`, `MakeBGRFromImg`, `MakeMatFromBGR`,
`RenderCicleHoleMat`, `MakeHistogramMat` / `RenderHistogram`, `FixedRotateBGRImage` — plus the
nested `CamResResolver` (`ResolveCapRes`, `filter_resolution`, `ParseAsWidthxHeight`), which probes
a webcam for the resolutions it actually supports.

- **`CamCtx_USBCam`** — OpenCvSharp `VideoCapture` by device index, backend selectable
  (ANY / MSMF / DSHOW / FFMPEG), with a test read to prove the device really delivers frames.
- **`CamCtx_IPCam`** — protocol is chosen by **which constructor overload is used**, not by a
  parameter: the `RTSP` overload (which also takes `user_id` / `user_pw`) or the `MJPEG_WS`
  overload. Frames arrive on the client's receive callback into a `Queue<CamFrame>`; a reusable
  black "blink" Mat covers queue underrun.

`CamCtx.cap_dev_model_` is display-only — nothing reads it, so camera model names never reach the
capture layer.

---

## IP camera clients (`sample/CamHelper/`)

Both clients share a shape: an `ERR` enum, `SetLastErr` / `GetLastErrDesc()` formatted as
`"{int}({ENUM_NAME}), {desc}"`, `SetLogLevel()`, and a `Tick()`-driven reconnect that backs off from
2 s by doubling, capped at 15 s.

- **`IPCam335N_RTSPClient`** (`IPCam335N_RTSP.cs`) — generic RTSP over `VideoCapture` +
  `VideoCaptureAPIs.FFMPEG`; works with any camera. Drops five warm-up frames, unpacks the FourCC to
  a 4-character codec string, and clones every Mat before handing it to the callback. `timeout_ms`
  is accepted but **not applied** — OpenCvSharp 4.11 has no `OpenTimeoutMsec`; use
  `OPENCV_FFMPEG_CAPTURE_OPTIONS` if you need one.
- **`IPCam335N_MJpegWSClient`** (`IPCam335N_MJpeg.cs`) — **model-specific**: it sends
  `{"start_video":{"ch":N}}` over the WebSocket and expects two acks. Camera models added through
  the UI therefore only really work over RTSP.

Both constructors append a default path (`/ws`, `live/main`) **only when the given URL has none**, so
a hand-typed URL is used verbatim and paths are never doubled.

`TickConnectionLost()` re-calls `Connect()` and must **not** overwrite the error code on failure —
`Connect()` has already stored the precise one.

### Passwords must never reach a log or a dialog

`CamCtx_IPCam.InjectCredentials()` embeds `user:pw@` into the RTSP URL, and `Uri.ToString()` (used
by both clients' `GetURL()`) hands it straight back. Every log line, exception message and info
string that contains a URL goes through:

```csharp
UrlParser.MaskPassword(url)     // sample/CamCtx_IPCam.cs:51 — public static, same namespace
```

It scans only the **authority** segment (after `://`, before the first `/`) and takes the *last* `@`
in it, so `rtsp://a:p@ss@host/x` masks fully and a `user:pw@` that merely appears in a path is left
alone. Both `Connect()` methods compute `url_masked` once at the top and use that everywhere
user- or console-visible; the raw URL goes only to `cap.Open()` / `ConnectAsync()`.
`CamCtx_IPCam.GetInfo()` masks too, because its output reaches an error MessageBox.

---

## IP camera model profiles

`sample/IPCamModelProfile.cs` holds two types:

- **`IPCamModelProfile`** — `name`, per-protocol default port (`mjpeg_port` / `rtsp_port`), a URL
  template with `{ip}` / `{port}` tokens, and `keep_user_input`. `{port}` expands to `":554"`
  *including the colon*, or to `""` when the PORT box is empty.
- **`IPCamModelStore`** — JSON load/save plus the built-in defaults. Persisted to
  **`ipcam_models.json` beside the exe**. A missing or unparsable file falls back to the defaults
  and the reason is *reported*, not swallowed.

`ToUrlTemplate()` reverses a live URL back into a template for the `[+]` dialog. It substitutes
**only inside the authority segment** — a whole-URL `Replace()` corrupts templates whose path
happens to contain the IP or `:port`, and the bad template then gets written to JSON.

A model whose template is empty renders as `IPCamModelStore.URL_NOT_IMPL` in the UI, and `Form1`
**refuses to Open with a message** rather than falling back to re-assembling `ip`/`port` (which used
to silently connect to the wrong address).

### The Form1 panel contract

The model combo, IP/PORT boxes and the two URL boxes are wired together by
`cap_src_ipcam_on_host_ip_changed(bool apply_profile_port = false)` (`Form1.cs:1213`):

- **`apply_profile_port` must stay `false` on the `TextChanged` paths.** Passing `true` there
  re-applies the profile's default port on every keystroke, making PORT uneditable — this was a real
  bug.
- Only the two `SelectedIndexChanged` handlers (model combo, MJPEG/RTSP tab) pass `true`.
- The method assigns `.Text` on controls whose `TextChanged` calls back into it; the
  `updating_ip_cam_url_` re-entry guard is what makes that terminate.
- Initial PORT fill happens through `Form1_Load` → `LoadIPCamModels()` → `RebuildIPCamModelCombo()`
  → `SelectedIndexChanged`.

`cap_src_ipcam_open_Click` uses the URL text box as the actual connect address.

---

## Video recording

`sample/SampleRec.cs` holds `VideoRec` — an OpenCvSharp `VideoWriter` wrapper with size-based file
rolling (`Roll()`, `MakeFileName()` → `<prefix>-001.mp4`). Codec is `mp4v`; `CODEC_H264` exists but
needs `openh264-1.8.0-win64.dll`, which is not shipped. `AddFrame(Mat)` and
`AddBGRFrame(bytes, w, h, redundant_frame_cnt)` feed it. Size checks are throttled to every 2 s, and
`Roll()` builds the next writer *before* closing the old one so a failed roll cannot lose the
current file.

The demo owns the instance in `RecContext.rec_recorder`, created lazily inside the run loop and torn
down by the `"stop"` branch of message `4000`. Because `RecContext` outlives `RunContext`, a
recording keeps running across `Reset`.

---

## Offline feature explorer

`FeatureExplorer.show_dlg()` opens `FeatureExplorerDlg` — two directory panes (defaulting to
`COMPARE_IMG_PATH`) listing images. **Compare** hands the selection to `FeatureExpCompareDlg`, which
runs the SDK's feature extraction and comparison on the pair. Both post a private `WM_APP+1` message
to themselves via `user32!PostMessage` to get the heavy work off the click handler, and override
`WndProc` to catch it.

`FeatureExpCompareDlg` loads both images into 32bpp ARGB offscreen `Bitmap`s — deliberately via the
SDK's OpenCV-backed loader rather than `Image.FromStream` / `new Bitmap(bytes)` — and paints them
aspect-fit with `HighQualityBicubic`.

---

## Traps

- **Hand-coded dialogs.** `FeatureExplorerDlg.cs`, `FeatureExpCompareDlg.cs` and
  `IPCamModelEditDlg.cs` build their controls in the constructor with **no `.Designer.cs` and no
  `.resx`**. Follow that pattern for new dialogs; only `Form1` uses the WinForms designer. When you
  change a label's text, re-check the fixed `Size` / `Location` values — nothing auto-lays-out.
- **New source files must be added to the `.csproj` by hand** (`<Compile Include="..." />`, plus
  `<SubType>Form</SubType>` for a Form) — non-SDK-style project, no globbing.
- **BOM is per-file.** Most sources are UTF-8 **with** BOM; `sample/IPCamModelProfile.cs` and
  `sample/IPCamModelEditDlg.cs` have **none**. Match the file you are editing. All files are LF.
- **UI strings are English; comments may be Korean.** Don't put Korean in a MessageBox or a control
  caption.
- **Threshold constants are `const`** and get inlined at every call site, so value changes require
  `-t:Rebuild` — an incremental build silently keeps the old numbers.
- **`stub/` is not the SDK.** It exists only so the solution compiles; every method throws.
