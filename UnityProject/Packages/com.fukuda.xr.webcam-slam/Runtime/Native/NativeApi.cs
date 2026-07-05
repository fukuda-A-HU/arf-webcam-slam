using System;
using System.Runtime.InteropServices;

namespace UnityEngine.XR.WebCamSlam.Native
{
    /// <summary>
    /// Mirrors <c>wcs_tracking_state</c> from <c>wcs_api.h</c>.
    /// </summary>
    public enum WcsTrackingState
    {
        Initializing = 0,
        Tracking = 1,
        Lost = 2,
    }

    /// <summary>
    /// Mirrors <c>wcs_pixel_format</c> from <c>wcs_api.h</c>.
    /// </summary>
    public enum WcsPixelFormat
    {
        Rgba32 = 0,
        Bgra32 = 1,
        Gray8 = 2,
    }

    /// <summary>
    /// Mirrors <c>wcs_config</c> from <c>wcs_api.h</c>. Field order and types must match the native
    /// struct layout exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WcsConfig
    {
        public int width;
        public int height;
        public double fps;
        public double fx;
        public double fy;
        public double cx;
        public double cy;
        public double k1;
        public double k2;
        public double p1;
        public double p2;
        public double k3;
        public int pixelFormat; // wcs_pixel_format
        public int maxNumKeypoints;
        [MarshalAs(UnmanagedType.LPUTF8Str)] public string vocabPath;
    }

    /// <summary>
    /// Mirrors <c>wcs_plane</c> from <c>wcs_api.h</c>. Field order and types must match the native
    /// struct layout exactly.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WcsPlane
    {
        public ulong id;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] center; // Unity space, session origin

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] rotation; // quaternion x,y,z,w

        public float extentX;
        public float extentZ;
        public int boundaryCount;
        public int alignment; // 0 horiz-up, 1 horiz-down, 2 vertical, 3 arbitrary
    }

    /// <summary>
    /// Raw P/Invoke bindings for the native <c>webcam_slam</c> SLAM library, matching
    /// <c>wcs_api.h</c> exactly. This class deliberately has no dependency on Unity types beyond
    /// what's needed for marshaling (plain arrays, primitives) so the contract stays a faithful
    /// mirror of the C ABI; higher-level consumers (see <see cref="SlamPipeline"/>) are responsible
    /// for building UnityEngine types (Vector3, Quaternion, ...) from the raw values.
    /// </summary>
    internal static class NativeApi
    {
        // No file extension: Unity resolves this to webcam_slam.dll on Windows and
        // libwebcam_slam.dylib on macOS.
        const string dll = "webcam_slam";

        /// <summary>
        /// Creates a SLAM session. Returns <see cref="IntPtr.Zero"/> on failure and writes a
        /// message into <paramref name="err"/>.
        /// </summary>
        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr wcs_create(ref WcsConfig cfg, byte[] err, int errLen);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void wcs_destroy(IntPtr h);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void wcs_reset(IntPtr h);

        /// <summary>
        /// Feeds one frame. Called from a dedicated C# thread. Returns 1 if consumed.
        /// </summary>
        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int wcs_feed_frame(IntPtr h, byte[] pixels, int stride, double timestampSec);

        /// <summary>
        /// Non-blocking getter for the latest cached pose (mutex protected on the native side).
        /// Returns 1 if valid. <paramref name="outPos"/> must have length 3, <paramref name="outRot"/>
        /// length 4.
        /// </summary>
        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int wcs_try_get_pose(IntPtr h, float[] outPos, float[] outRot);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern WcsTrackingState wcs_get_tracking_state(IntPtr h);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int wcs_get_landmarks(IntPtr h, float[] outXyz, ulong[] outIds, int max);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int wcs_get_planes(IntPtr h, [Out] WcsPlane[] outPlanes, int max);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int wcs_get_plane_boundary(IntPtr h, ulong planeId, float[] outXz, int max);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void wcs_set_scale(IntPtr h, float metersPerUnit);

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr wcs_get_version();

        /// <summary>
        /// Thin wrapper around <see cref="wcs_get_version"/> that marshals the returned
        /// null-terminated ANSI string into a managed <see cref="string"/>.
        /// </summary>
        public static string GetVersion()
        {
            var ptr = wcs_get_version();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr);
        }
    }
}
