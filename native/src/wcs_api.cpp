/*
 * wcs_api.cpp - STUB implementation of the WebCam SLAM C API.
 *
 * This build-spike stub deliberately has NO external dependencies (no stella_vslam,
 * OpenCV, g2o, ...). Its only purpose is to prove that the CMake + MSVC toolchain
 * produces a loadable shared library exporting the wcs_* C ABI, so the Unity P/Invoke
 * layer can be wired and tested before the real monocular SLAM backend is integrated.
 *
 * The real implementation will replace the body of these functions with stella_vslam
 * calls plus the plane fitter; the signatures in wcs_api.h are the frozen contract.
 */
#include "wcs_api.h"

#include <atomic>
#include <cstring>
#include <mutex>

struct wcs_slam {
    std::mutex mutex;
    wcs_config config{};
    std::atomic<int64_t> frame_count{0};
    wcs_tracking_state state{WCS_STATE_INITIALIZING};
    float scale{1.0f};
    float pos[3]{0.0f, 0.0f, 0.0f};
    float rot[4]{0.0f, 0.0f, 0.0f, 1.0f};
};

// Number of fed frames after which the stub pretends initialization succeeded.
static constexpr int64_t kFramesToInitialize = 15;

extern "C" {

WCS_API wcs_slam* wcs_create(const wcs_config* cfg, char* err, int32_t err_len) {
    if (cfg == nullptr) {
        if (err != nullptr && err_len > 0) {
            std::strncpy(err, "wcs_create: cfg is null", static_cast<size_t>(err_len) - 1);
            err[err_len - 1] = '\0';
        }
        return nullptr;
    }
    auto* h = new (std::nothrow) wcs_slam();
    if (h == nullptr) {
        if (err != nullptr && err_len > 0) {
            std::strncpy(err, "wcs_create: out of memory", static_cast<size_t>(err_len) - 1);
            err[err_len - 1] = '\0';
        }
        return nullptr;
    }
    h->config = *cfg;
    return h;
}

WCS_API void wcs_destroy(wcs_slam* h) {
    delete h;
}

WCS_API void wcs_reset(wcs_slam* h) {
    if (h == nullptr) return;
    std::lock_guard<std::mutex> lock(h->mutex);
    h->frame_count.store(0);
    h->state = WCS_STATE_INITIALIZING;
    h->pos[0] = h->pos[1] = h->pos[2] = 0.0f;
    h->rot[0] = h->rot[1] = h->rot[2] = 0.0f;
    h->rot[3] = 1.0f;
}

WCS_API int32_t wcs_feed_frame(wcs_slam* h, const uint8_t* pixels, int32_t stride, double timestamp_sec) {
    (void)pixels;
    (void)stride;
    (void)timestamp_sec;
    if (h == nullptr) return 0;

    const int64_t n = h->frame_count.fetch_add(1) + 1;

    std::lock_guard<std::mutex> lock(h->mutex);
    if (n >= kFramesToInitialize) {
        h->state = WCS_STATE_TRACKING;
        // Stub motion: drift slowly forward so the C# side can observe a changing pose.
        h->pos[2] = static_cast<float>(n - kFramesToInitialize) * 0.01f * h->scale;
    }
    return 1;
}

WCS_API int32_t wcs_try_get_pose(wcs_slam* h, float out_pos[3], float out_rot[4]) {
    if (h == nullptr || out_pos == nullptr || out_rot == nullptr) return 0;
    std::lock_guard<std::mutex> lock(h->mutex);
    if (h->state != WCS_STATE_TRACKING) return 0;
    std::memcpy(out_pos, h->pos, sizeof(h->pos));
    std::memcpy(out_rot, h->rot, sizeof(h->rot));
    return 1;
}

WCS_API wcs_tracking_state wcs_get_tracking_state(wcs_slam* h) {
    if (h == nullptr) return WCS_STATE_LOST;
    std::lock_guard<std::mutex> lock(h->mutex);
    return h->state;
}

WCS_API int32_t wcs_get_landmarks(wcs_slam* h, float* out_xyz, uint64_t* out_ids, int32_t max) {
    (void)h;
    (void)out_xyz;
    (void)out_ids;
    (void)max;
    return 0;
}

WCS_API int32_t wcs_get_planes(wcs_slam* h, wcs_plane* out, int32_t max) {
    (void)h;
    (void)out;
    (void)max;
    return 0;
}

WCS_API int32_t wcs_get_plane_boundary(wcs_slam* h, uint64_t plane_id, float* out_xz, int32_t max) {
    (void)h;
    (void)plane_id;
    (void)out_xz;
    (void)max;
    return 0;
}

WCS_API void wcs_set_scale(wcs_slam* h, float meters_per_unit) {
    if (h == nullptr) return;
    std::lock_guard<std::mutex> lock(h->mutex);
    h->scale = meters_per_unit;
}

WCS_API const char* wcs_get_version(void) {
    return "0.1.0-stub";
}

} // extern "C"
