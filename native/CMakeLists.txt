cmake_minimum_required(VERSION 3.20)

project(webcam_slam LANGUAGES CXX VERSION 0.1.0)

# ---------------------------------------------------------------------------
# Phase B build spike: this currently builds only the dependency-free C API
# stub (src/wcs_api.cpp). The real monocular SLAM backend (stella_vslam + a
# RANSAC plane fitter) will be added behind the WCS_WITH_SLAM option once the
# MSVC/vcpkg dependency build is proven. Keeping the ABI (include/wcs_api.h)
# stable means the Unity P/Invoke layer does not change when that lands.
# ---------------------------------------------------------------------------

option(WCS_WITH_SLAM "Build with the stella_vslam backend (requires vcpkg deps)" OFF)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
  set(CMAKE_BUILD_TYPE Release CACHE STRING "" FORCE)
endif()

add_library(webcam_slam SHARED
  src/wcs_api.cpp
)

target_include_directories(webcam_slam PUBLIC
  ${CMAKE_CURRENT_SOURCE_DIR}/include
)

target_compile_definitions(webcam_slam PRIVATE WCS_BUILD_DLL)

set_target_properties(webcam_slam PROPERTIES
  OUTPUT_NAME "webcam_slam"
  # Keep a flat, predictable output name across platforms (webcam_slam.dll / .dylib / .so).
  PREFIX ""
  MACOSX_RPATH ON
)

if(APPLE)
  set_target_properties(webcam_slam PROPERTIES SUFFIX ".dylib")
endif()

if(WCS_WITH_SLAM)
  message(FATAL_ERROR "WCS_WITH_SLAM is not implemented yet (Phase B native SLAM integration).")
endif()
