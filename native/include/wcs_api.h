/*
 * wcs_api.h - C ABI boundary for the WebCam SLAM native plugin.
 *
 * This is the single, stable interface that the Unity C# side calls via P/Invoke.
 * All coordinates returned here are already in Unity's left-handed, Y-up space and
 * scaled to meters; the OpenCV->Unity conversion and metric scaling happen inside
 * the native library so C# only ever sees Unity-space values.
 */
#ifndef WCS_API_H
#define WCS_API_H

#include <stdint.h>

#if defined(_WIN32)
#  if defined(WCS_BUILD_DLL)
#    define WCS_API __declspec(dllexport)
#  else
#    define WCS_API __declspec(dllimport)
#  endif
#else
#  define WCS_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

typedef struct wcs_slam wcs_slam;

typedef enum wcs_tracking_state {
    WCS_STATE_INITIALIZING = 0,
    WCS_STATE_TRACKING = 1,
    WCS_STATE_LOST = 2
} wcs_tracking_state;

typedef enum wcs_pixel_format {
    WCS_PIXEL_RGBA32 = 0,
    WCS_PIXEL_BGRA32 = 1,
    WCS_PIXEL_GRAY8 = 2
} wcs_pixel_format;

typedef struct wcs_config {
    int32_t width;
    int32_t height;
    double fps;
    double fx, fy, cx, cy;         /* camera intrinsics (pixels) */
    double k1, k2, p1, p2, k3;     /* distortion (0 = none) */
    int32_t pixel_format;          /* wcs_pixel_format */
    int32_t max_num_keypoints;     /* e.g. 1000 */
    const char* vocab_path;        /* UTF-8 path to orb_vocab.fbow */
} wcs_config;

typedef struct wcs_plane {
    uint64_t id;
    float center[3];               /* Unity space, session origin */
    float rotation[4];             /* quaternion x,y,z,w */
    float extent_x, extent_z;
    int32_t boundary_count;
    int32_t alignment;             /* 0 horiz-up, 1 horiz-down, 2 vertical, 3 arbitrary */
} wcs_plane;

/* Creates a SLAM session. Returns NULL on failure and writes a message into err. */
WCS_API wcs_slam* wcs_create(const wcs_config* cfg, char* err, int32_t err_len);
WCS_API void      wcs_destroy(wcs_slam* h);
WCS_API void      wcs_reset(wcs_slam* h);

/* Feeds one frame. Called from a dedicated C# thread. Returns 1 if consumed. */
WCS_API int32_t   wcs_feed_frame(wcs_slam* h, const uint8_t* pixels, int32_t stride, double timestamp_sec);

/* Non-blocking getters returning the latest cached values (mutex protected). */
WCS_API int32_t            wcs_try_get_pose(wcs_slam* h, float out_pos[3], float out_rot[4]); /* 1 = valid */
WCS_API wcs_tracking_state wcs_get_tracking_state(wcs_slam* h);
WCS_API int32_t            wcs_get_landmarks(wcs_slam* h, float* out_xyz, uint64_t* out_ids, int32_t max);
WCS_API int32_t            wcs_get_planes(wcs_slam* h, wcs_plane* out, int32_t max);
WCS_API int32_t            wcs_get_plane_boundary(wcs_slam* h, uint64_t plane_id, float* out_xz, int32_t max);

WCS_API void        wcs_set_scale(wcs_slam* h, float meters_per_unit);
WCS_API const char* wcs_get_version(void);

#ifdef __cplusplus
}
#endif

#endif /* WCS_API_H */
