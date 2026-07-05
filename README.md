# WebCam SLAM XR Plugin

A desktop XR provider plugin for [AR Foundation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/manual/index.html) 6.x that uses an ordinary webcam as its only input source. It is designed as a drop-in `XRSessionSubsystem` / `XRCameraSubsystem` provider so existing AR Foundation content can run on a desktop PC with nothing more than a USB or built-in webcam.

## Status: Phase A

This is an early, work-in-progress package. Phase A establishes the end-to-end plumbing for AR Foundation without any real tracking:

- UPM package skeleton (`com.fukuda.xr.webcam-slam`) with an `XRLoaderHelper`-based loader registered in XR Plug-in Management.
- A camera background subsystem that captures a `WebCamTexture`, blits it into a `RenderTexture`, and exposes it to `ARCameraBackground` via `XRTextureDescriptor` (mirroring the structure of AR Foundation's XR Simulation `CameraTextureProvider`).
- A debug pose source: a `HandheldARInputDevice` is added to the Input System and driven every frame from a keyboard/mouse "fly" controller, so the standard `TrackedPoseDriver` binding (`<HandheldARInputDevice>/devicePosition|deviceRotation`) moves the AR camera with no custom binding code.
- No SLAM, no plane/point-cloud/raycast/anchor subsystems, and no native code yet. Real monocular SLAM tracking is planned for a later phase.

## Installation

Add the package via Package Manager using a Git URL pointing at the package subdirectory:

```
https://github.com/fukuda-A-HU/arf-webcam-slam.git?path=UnityProject/Packages/com.fukuda.xr.webcam-slam
```

After installing, enable "WebCam SLAM" as a provider for the Standalone platform in **Project Settings > XR Plug-in Management**.

## Requirements

- Unity 6000.0 (developed against 6000.0.70f1)
- AR Foundation 6.x
- com.unity.xr.management 4.x
- com.unity.inputsystem

## License

MIT, see [LICENSE](LICENSE).
