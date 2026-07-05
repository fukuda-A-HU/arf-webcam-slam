# Basic Sample

Minimal AR Foundation scene wired up to the WebCam SLAM provider.

## Setup

1. In **Project Settings > XR Plug-in Management**, enable **WebCam SLAM** for the Standalone tab.
2. Import this sample from the Package Manager window (**WebCam SLAM XR Plugin > Samples > Basic**).
3. Create a new empty scene, create an empty GameObject, and add the `SampleSceneBootstrap` component to it.
4. Enter Play Mode. A webcam permission prompt may appear depending on your OS; allow it.

## What it does

`SampleSceneBootstrap` builds the AR setup at runtime:

- An `ARSession` GameObject.
- An `XROrigin` with a Camera Offset and an AR Camera that has `ARCameraManager`, `ARCameraBackground`,
  and a `TrackedPoseDriver` bound to `<HandheldARInputDevice>/devicePosition|deviceRotation`.

Once running, you should see your webcam feed as the background and be able to fly the camera around
with the debug pose controller:

- **WASD** — move horizontally (relative to where you're looking)
- **Q / E** — move down / up
- **Hold right mouse button + drag** — look around
- **Space** — place a cube 1.5m in front of the camera (useful to confirm the camera pose is moving)
