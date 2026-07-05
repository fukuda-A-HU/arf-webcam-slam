# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [0.1.0] - 2026-07-05

### Added

- Phase A: package skeleton, `WebCamSlamLoader` registered with XR Plug-in Management.
- Camera background subsystem that blits a `WebCamTexture` into a `RenderTexture` and exposes it to `ARCameraBackground` via `XRTextureDescriptor`.
- Session subsystem stub reporting a permanently-tracking session (no real tracking yet).
- Debug pose source: a `HandheldARInputDevice` driven by a keyboard/mouse fly controller through `InputState.Change`, compatible with the stock `TrackedPoseDriver` binding.
- Basic sample scene bootstrap.
