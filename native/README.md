# Native plugin (`webcam_slam`)

The native library exposes the C ABI in [`include/wcs_api.h`](include/wcs_api.h) that the Unity
package calls through P/Invoke. It currently builds a dependency-free **stub** (see
[`src/wcs_api.cpp`](src/wcs_api.cpp)); the real monocular-SLAM backend
([stella_vslam](https://github.com/stella-cv/stella_vslam)) is integrated behind the
`WCS_WITH_SLAM` CMake option (work in progress).

## Toolchain

- **Windows**: Visual Studio 2022 (MSVC, "Desktop development with C++" workload). The MSVC build
  of stella_vslam is not officially supported upstream, but it compiles and links with the small
  set of patches in [`patches/`](patches) — see below.
- **macOS**: Apple Clang (planned).
- Dependencies are provided by [vcpkg](https://github.com/microsoft/vcpkg) via
  [`vcpkg.json`](vcpkg.json) (OpenCV, g2o, Eigen, yaml-cpp, spdlog, sqlite3).

## Building the stub (proven)

```powershell
cmake -S native -B native/build -G "Visual Studio 17 2022" -A x64
cmake --build native/build --config Release
```

Produces `native/build/Release/webcam_slam.dll` exporting the `wcs_*` C ABI.

## Building with the SLAM backend

Dependencies are built statically so the plugin ships as a single self-contained library and to
avoid the g2o template-export duplicate-symbol issue that appears when g2o is a DLL:

```powershell
# 1. Build dependencies (static, dynamic CRT)
vcpkg install --triplet x64-windows-static-md --x-manifest-root=native

# 2. Configure + build with the SLAM backend
cmake -S native -B native/build `
  -G "Visual Studio 17 2022" -A x64 `
  -DCMAKE_TOOLCHAIN_FILE=<vcpkg>/scripts/buildsystems/vcpkg.cmake `
  -DVCPKG_TARGET_TRIPLET=x64-windows-static-md `
  -DCMAKE_FIND_PACKAGE_PREFER_CONFIG=ON `
  -DWCS_WITH_SLAM=ON
cmake --build native/build --config Release
```

## stella_vslam patches

stella_vslam is fetched at a pinned commit and patched at configure time. The patches are:

- **`patches/0001-use-linear-solver-eigen.patch`** — replaces the `g2o::LinearSolverCSparse`
  usages (in `global_bundle_adjuster.cc` and `graph_optimizer.cc`) with `g2o::LinearSolverEigen`.
  The other optimizers already use the Eigen solver. This removes the dependency on g2o's CSparse
  extension (LGPL), keeping the plugin's third-party licenses permissive.
- **`patches/0002-msvc-build-fixes.patch`** — MSVC build fixes: collapse
  `/source-charset:utf-8 /execution-charset:utf-8` into `/utf-8` (they conflict with the `/utf-8`
  injected by spdlog/fmt), drop the forced `/MT` (mismatches the vcpkg `/MD` CRT), define
  `_USE_MATH_DEFINES` (for `M_PI`) and `NOMINMAX`, and use `find_package(Eigen3 CONFIG)` (the
  bundled `FindEigen3.cmake` fails to parse the version of recent Eigen).

Pinned upstream revisions:

| Component | Commit |
|-----------|--------|
| stella_vslam | `8ac1be4d1fa20e4148b478d7b30788abcbb1d9fe` |
| FBoW (submodule) | `c6e3c29e3332a0b0834021797e2aa4e8eb66a3c1` |

## Vocabulary file

stella_vslam needs an ORB vocabulary (`orb_vocab.fbow`, MIT-licensed, ~40 MB) from
[stella-cv/FBoW_orb_vocab](https://github.com/stella-cv/FBoW_orb_vocab). It is not committed;
the Unity editor tooling downloads it with a SHA-256 check.
