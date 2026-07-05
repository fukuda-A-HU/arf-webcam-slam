# WebCam SLAM XR Plugin

A desktop XR provider for AR Foundation 6.x that uses an ordinary webcam as its only input.

## Phase A

This package currently provides:

- A camera background (`ARCameraBackground`) fed from a `WebCamTexture`.
- A debug "fly" camera pose (WASD + Q/E + right-mouse-drag look), delivered through the Input
  System's standard `HandheldARInputDevice` layout, so the stock `TrackedPoseDriver` binding works
  unmodified.
- No real tracking (SLAM), and no plane/point-cloud/raycast/anchor subsystems yet.

## Setup

1. Install the package (see the repository README for the Git URL).
2. Open **Edit > Project Settings > XR Plug-in Management**.
3. Select the **Standalone** tab and enable **WebCam SLAM** as a provider.
4. (Optional) Adjust settings under the "WebCam SLAM" section of the same window: preferred device
   name, requested capture resolution/frame rate, mirroring, and the approximate vertical field of
   view used to derive camera intrinsics.
5. Import the **Basic** sample from the package's Samples tab in the Package Manager window, or use
   the `com.fukuda.xr.webcam-slam` API directly in your own scene (see
   `Samples~/Basic/SampleSceneBootstrap.cs` for a minimal reference setup).
6. Enter Play Mode. Your webcam feed should appear as the AR camera background, and you can fly the
   camera around with WASD / Q / E / right-mouse-drag.

## Known limitations (Phase A)

- Camera pose is not derived from the video at all; it is a manually driven debug pose.
- No CPU camera image access (`ARCameraManager.TryAcquireLatestCpuImage` will fail).
- No plane detection, point clouds, raycasting, or anchors.
- Camera intrinsics are a rough approximation from a single configured vertical FOV value, not a
  real calibration.
