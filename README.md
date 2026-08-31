# Test_Sample_faceRecognition_proj

A .NET Framework 4.8 WinForms application that drives a commercial face-recognition SDK through a
four-stage verification pipeline, from either a USB webcam or an IP camera.

```
passive liveness → best-shot collection → BGR multi-frame liveness → face compare
```

Each stage runs either **locally** (`ClientOnly`) or against a **FaceServer REST endpoint**
(`FaceServer`), selectable per stage at runtime. Camera input comes from OpenCvSharp `VideoCapture`
(USB), RTSP over FFMPEG, or a hand-written **MJPEG-over-WebSocket** client.

---

## What is mine, and what is not

| Path | Lines | Author |
|---|---:|---|
| `sample/` — demo engine, capture layer, IP-cam clients, config model, dialogs | 8,243 | **me** |
| `Form1.cs` + `Form1.Designer.cs` — the single designer-built form | 2,062 | **me** |
| `Program.cs`, `Properties/AssemblyInfo.cs` | 58 | **me** |
| `stub/` — build-only SDK stubs | 1,093 | me (see below) |

**~10,400 lines of application code.** The SDK wrapper itself is vendor source and is *not*
published here; `stub/` contains signature-only declarations so the solution still compiles and can
be read in an IDE. Every stub method throws `NotImplementedException` — see
[Building](#building).

---

## Technical highlights

Concrete places worth reading, rather than a feature list.

**A framework-free actor boundary.** All UI → worker communication goes through one queue
(`sample/DemoClientServer.cs:29`): a `Queue<Msg>` behind a dedicated lock, where `PopMsg` returns
`null` instead of blocking so the demo thread never stalls. The worker drains it at the top of every
frame (`sample/DemoClientServerMsg.cs:8`). Nothing else crosses the thread boundary.

**Async results re-enter a single-threaded state machine.** A stage launches an immediately-invoked
async lambda (`sample/DemoClientServer.cs:1258`); its `_finish` closure posts a `3000` message back
onto the queue, and `DispatchMessage` dispatches it **conditioned on the current stage**
(`sample/DemoClientServerMsg.cs:72`). The result: `Task`-based server calls feed a state machine
that needs no locks anywhere in its stage logic. Task completion is polled non-blockingly from the
render loop — `IsCompleted` is checked before `GetAwaiter().GetResult()`
(`sample/DemoClientServer.cs:1314`, `:1801`), so a slow server never drops a frame.

**Cooperative shutdown you can watch.** `sample/Demo.cs:54` exposes `SetExit()` / `Join(timeout)`;
`sample/SampleDemoMan.cs:45` builds a bounded poll loop on top that reports elapsed time to a
callback and gives up at 30 s. No `Thread.Abort` anywhere.

**UI marshalling that documents its own hazard.** Every demo → UI callback uses `BeginInvoke`, never
`Invoke` (`Form1.cs:416`, `:740`) — `Invoke` would deadlock, because the UI thread may already be
inside `StopDemo`'s join loop. `Form1.cs:679` goes further and runs the "Exiting…" modal on its own
message pump so it keeps repainting while the main pump is busy.

**A hand-written MJPEG-over-WebSocket client** (`sample/CamHelper/IPCam335N_MJpeg.cs`, 791 lines):
sync-over-async receive with a per-call `CancellationTokenSource` and message reassembly (`:603`), a
connect handshake modelled as an error-coded pipeline where all ~14 early-return paths set both a
code and a human-readable reason (`:278`), a small hand-rolled JSON field scanner (`:155`), JPEG
SOI/EOI frame validation (`:72`), a 20 s heartbeat, and reconnect backoff doubling from 2 s to a
15 s cap (`:718`). Errors are a 33-value enum (`:210`) formatted once as `"{int}({NAME}), {desc}"`,
so a missed `SetLastErr` shows up immediately as `1(UNKNOWN)`.

**Password masking that handles the ugly case.** `UrlParser.MaskPassword`
(`sample/CamCtx_IPCam.cs:51`) scans only the URL *authority* segment and splits on the **last** `@`
in it, so `rtsp://a:p@ss@host/x` masks completely while a `user:pw@` appearing in a path is left
alone. Both camera clients compute `url_masked` once at the top of `Connect()` and use it for every
log line, exception, and message box; the raw URL reaches only the socket call.

**Four PropertyGrid extensibility points** (`sample/SamplePropSettings.cs`): a `TypeConverter` that
renders `float[]` ranges as `"-20, 20"` (`:24`); `DynamicListConverter` + a `[ListValues(...)]`
attribute, so any string property becomes a combo box with one line of markup (`:101`); a
`UITypeEditor` that drops a live `NumericUpDown` into the grid cell (`:51`); and a reflection hack
that reaches the private `gridView` field to move the splitter, which WinForms exposes no API for
(`:37`).

**Native memory handled carefully.** `sample/CamCtx.cs:224` validates channel count, `MatType`
*and* `IsContinuous()` before `Marshal.Copy` — the continuity check is the one people forget.
`:264` does the reverse-direction length check before copying into native memory.

**Heavy work off the click handler.** Both explorer dialogs P/Invoke `user32!PostMessage` to send
themselves a private `WM_APP+1` and override `WndProc` to catch it
(`sample/FeatureExplorerDlg.cs:25`, `sample/FeatureExpCompareDlg.cs:28`).

**Custom drawing.** Tag-prefix-coloured log overlay (`sample/CamCtx.cs:356`), BGRA alpha-composited
circular vignette (`:464`), RGB histogram composited without a black backing box (`:541`), and
offscreen aspect-fit painting with `HighQualityBicubic` (`sample/FeatureExpCompareDlg.cs:189`).

**Recording that survives a failed roll.** `sample/SampleRec.cs` builds the next `VideoWriter`
*before* closing the current one, so a roll failure cannot lose the file already being written.

**A re-entrancy guard that has to be there.** `Form1.cs:1213` assigns `.Text` on controls whose
`TextChanged` handlers call straight back into it; the `updating_ip_cam_url_` flag with `try/finally`
is what makes it terminate.

---

## Building

```bash
MSBUILD='C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe'
"$MSBUILD" Test_Sample_faceRecognition_proj.csproj -t:Restore
"$MSBUILD" Test_Sample_faceRecognition_proj.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x64
```

Verified from a clean `git clone` on both `Debug|x64` and `Release|x64`.

- **Restore the `.csproj`, not the `.sln`.** This is a legacy (non-SDK-style) project using
  `PackageReference`; solution-level restore skips it and reports "no projects to restore", after
  which the build fails with CS0246 on every OpenCvSharp type. Once `obj/project.assets.json` exists,
  building the `.sln` works normally — and Visual Studio restores correctly on its own.
- Build **x64** (or x86). The solution maps `Any CPU` to x64 but has no `Any CPU` build entry.
- Use the **Visual Studio BuildTools** MSBuild. The .NET Framework MSBuild under
  `C:\Windows\Microsoft.NET\` rejects `<LangVersion>7.3</LangVersion>` with CS1617.
- NuGet: `Newtonsoft.Json`, `OpenCvSharp4`, `OpenCvSharp4.runtime.win`, `SharpZipLib.NETStandard`.
- One long-standing warning is expected: `sample/DemoClientServer.cs(216)` CS0219.
- Threshold constants are `const` and get inlined at every call site, so changing one needs
  `-t:Rebuild`; an incremental build silently keeps the old number.

### It compiles; it does not run

The app is published without the SDK, so a build produces an executable that throws as soon as it
touches `stub/`. Running for real additionally needs, none of which are in this repository:

- `AlcheraFaceSDKCS.dll` → `AlcheraFaceSDK.dll` → `torch_cpu`, `c10`, `fbgemm`, `libiomp5md`,
  `asmjit`, `uv`, plus `opencv_world455.dll`
- a `models/` directory (~620 MB) and an activated `license.cer`

A missing *dependency* in that chain surfaces as `DllNotFoundException: AlcheraFaceSDKCS.dll`
(Win32 error 126) even though the named DLL is present — the diagnosis is written up in
[`docs/troubleshooting-native-dependency.md`](docs/troubleshooting-native-dependency.md).

### Verifying without launching

There is no test project; verification is normally manual through the UI. Anything that is a `const`
or a pure static helper can be checked by reflection instead, with no native runtime:

```powershell
$asm = [Reflection.Assembly]::Load([IO.File]::ReadAllBytes('bin\x64\Release\FaceSDKSample.exe'))
$asm.GetType('FaceSDKSample.sample.UrlParser').GetMethod('MaskPassword').Invoke($null, @('rtsp://a:b@h/x'))
# -> rtsp://a:***@h/x
```

---

## Conventions

C# **7.3 only** (no switch expressions, nullable reference types, `using var`, or target-typed
`new`). Private fields are `snake_case_`, locals `snake_case`, types and methods `PascalCase`.
UI strings are English; comments may be Korean. All files are LF. New source files must be added to
the `.csproj` by hand — it is not an SDK-style project.

See [`ARCHITECTURE.md`](ARCHITECTURE.md) for the object graph, the state machine, and the message
queue contract.

---

## Note

This is the portion I personally wrote of a work project, published for portfolio review. The
vendor SDK wrapper, the trained models, and the licence material are deliberately excluded, and
server addresses, device identifiers and threshold values have been replaced with placeholders. No
open-source licence is granted.
