using System;
using System.Text;
using System.Threading;

namespace UnityEngine.XR.WebCamSlam.Native
{
    /// <summary>
    /// Drives the native <c>webcam_slam</c> SLAM session on a dedicated worker thread and publishes
    /// the latest pose/tracking state in a thread-safe way for the main (or camera) thread to read.
    /// </summary>
    /// <remarks>
    /// Frame submission is intentionally lossy: <see cref="SubmitFrame"/> only ever keeps the most
    /// recently submitted frame. If the worker is still busy feeding a previous frame into the
    /// native SLAM pipeline when a new one arrives, the older, not-yet-consumed frame is simply
    /// replaced and discarded rather than queued, so the pipeline never falls behind the camera by
    /// more than one frame. This mirrors how <c>wcs_feed_frame</c> is meant to be called (latest
    /// frame wins) and keeps latency bounded at the cost of not processing every camera frame.
    /// </remarks>
    public sealed class SlamPipeline : IDisposable
    {
        const int errBufferLength = 256;
        const int joinTimeoutMs = 2000;

        readonly object m_FrameLock = new object();
        readonly object m_StateLock = new object();
        readonly AutoResetEvent m_FrameAvailable = new AutoResetEvent(false);
        readonly Thread m_WorkerThread;

        IntPtr m_Handle;
        volatile bool m_StopRequested;
        bool m_Disposed;

        // Pending frame, guarded by m_FrameLock. Buffers are swapped in by reference from
        // SubmitFrame, not copied, so callers must not mutate a buffer they've handed off until a
        // later SubmitFrame call reclaims it (see SubmitFrame's remarks).
        byte[] m_PendingPixels;
        int m_PendingStride;
        double m_PendingTimestampSec;
        bool m_HasPendingFrame;

        // Latest published pose/state, guarded by m_StateLock.
        readonly float[] m_LatestPos = new float[3];
        readonly float[] m_LatestRot = new float[4];
        bool m_HasLatestPose;
        WcsTrackingState m_TrackingState = WcsTrackingState.Initializing;

        // Scratch buffers reused across worker-thread native calls to avoid per-frame allocations.
        readonly float[] m_ScratchPos = new float[3];
        readonly float[] m_ScratchRot = new float[4];

        /// <summary>
        /// <c>true</c> if <c>wcs_create</c> succeeded and the worker thread is running. When
        /// <c>false</c>, all public members are safe to call but behave as no-ops.
        /// </summary>
        public bool isValid => m_Handle != IntPtr.Zero;

        /// <summary>
        /// The most recently observed tracking state. Defaults to <see cref="WcsTrackingState.Initializing"/>
        /// if the native session failed to create.
        /// </summary>
        public WcsTrackingState trackingState
        {
            get
            {
                lock (m_StateLock)
                {
                    return m_TrackingState;
                }
            }
        }

        /// <summary>
        /// Creates the native SLAM session described by <paramref name="config"/> and, on success,
        /// starts the dedicated worker thread that feeds it frames. If native session creation
        /// fails, the error message reported by the native library is logged and this instance
        /// becomes a permanent no-op (<see cref="isValid"/> is <c>false</c>); no exception is thrown.
        /// </summary>
        public SlamPipeline(WcsConfig config)
        {
            var err = new byte[errBufferLength];
            m_Handle = NativeApi.wcs_create(ref config, err, err.Length);

            if (m_Handle == IntPtr.Zero)
            {
                Debug.LogError($"WebCam SLAM: wcs_create failed: {DecodeErrorMessage(err)}");
                return;
            }

            m_WorkerThread = new Thread(WorkerLoop)
            {
                Name = "WebCamSlam Native Pipeline",
                IsBackground = true,
            };
            m_WorkerThread.Start();
        }

        /// <summary>
        /// Submits the latest camera frame to be fed into the native SLAM pipeline. Safe to call
        /// from the main or camera thread. Only the most recently submitted frame is kept: if the
        /// worker thread hasn't consumed the previous one yet, it is discarded in favor of this one
        /// (see class remarks). Ownership of <paramref name="pixels"/> passes to the pipeline until
        /// the next call to <see cref="SubmitFrame"/> — callers must not mutate the array they pass
        /// in after this call returns, and should treat it as the pipeline's until superseded.
        /// </summary>
        public void SubmitFrame(byte[] pixels, int stride, double timestampSec)
        {
            if (!isValid || pixels == null)
                return;

            lock (m_FrameLock)
            {
                m_PendingPixels = pixels;
                m_PendingStride = stride;
                m_PendingTimestampSec = timestampSec;
                m_HasPendingFrame = true;
            }

            m_FrameAvailable.Set();
        }

        /// <summary>
        /// Reads the latest cached pose. Returns <c>true</c> and populates <paramref name="position"/>
        /// / <paramref name="rotation"/> if a valid pose has been observed; returns <c>false</c>
        /// (leaving the outputs at their default values) otherwise. Safe to call from any thread.
        /// </summary>
        /// <remarks>
        /// The native library returns coordinates already converted to Unity's left-handed, Y-up
        /// space and scaled to meters (per <c>wcs_api.h</c>), so no further transformation is applied
        /// here.
        /// </remarks>
        public bool TryGetPose(out Vector3 position, out Quaternion rotation)
        {
            lock (m_StateLock)
            {
                if (!m_HasLatestPose)
                {
                    position = Vector3.zero;
                    rotation = Quaternion.identity;
                    return false;
                }

                position = new Vector3(m_LatestPos[0], m_LatestPos[1], m_LatestPos[2]);
                rotation = new Quaternion(m_LatestRot[0], m_LatestRot[1], m_LatestRot[2], m_LatestRot[3]);
                return true;
            }
        }

        /// <summary>
        /// Sets the metric scale (meters per Unity unit) used by the native SLAM pipeline.
        /// </summary>
        public void SetScale(float metersPerUnit)
        {
            if (!isValid)
                return;

            NativeApi.wcs_set_scale(m_Handle, metersPerUnit);
        }

        /// <summary>
        /// Resets the native SLAM session, discarding accumulated map state.
        /// </summary>
        public void Reset()
        {
            if (!isValid)
                return;

            NativeApi.wcs_reset(m_Handle);

            lock (m_StateLock)
            {
                m_HasLatestPose = false;
                m_TrackingState = WcsTrackingState.Initializing;
            }
        }

        void WorkerLoop()
        {
            while (!m_StopRequested)
            {
                m_FrameAvailable.WaitOne();

                if (m_StopRequested)
                    break;

                byte[] pixels;
                int stride;
                double timestampSec;

                lock (m_FrameLock)
                {
                    if (!m_HasPendingFrame)
                        continue;

                    pixels = m_PendingPixels;
                    stride = m_PendingStride;
                    timestampSec = m_PendingTimestampSec;
                    m_HasPendingFrame = false;
                    m_PendingPixels = null;
                }

                NativeApi.wcs_feed_frame(m_Handle, pixels, stride, timestampSec);

                var poseValid = NativeApi.wcs_try_get_pose(m_Handle, m_ScratchPos, m_ScratchRot) != 0;
                var state = NativeApi.wcs_get_tracking_state(m_Handle);

                lock (m_StateLock)
                {
                    m_TrackingState = state;
                    if (poseValid)
                    {
                        Array.Copy(m_ScratchPos, m_LatestPos, m_LatestPos.Length);
                        Array.Copy(m_ScratchRot, m_LatestRot, m_LatestRot.Length);
                        m_HasLatestPose = true;
                    }
                }
            }
        }

        static string DecodeErrorMessage(byte[] err)
        {
            var length = Array.IndexOf(err, (byte)0);
            if (length < 0)
                length = err.Length;

            return length == 0 ? "(no error message)" : Encoding.UTF8.GetString(err, 0, length);
        }

        /// <summary>
        /// Stops the worker thread and destroys the native SLAM session. Safe to call multiple
        /// times; subsequent calls are no-ops.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;

            m_StopRequested = true;
            m_FrameAvailable.Set();

            if (m_WorkerThread != null && m_WorkerThread.IsAlive)
            {
                if (!m_WorkerThread.Join(joinTimeoutMs))
                    Debug.LogWarning("WebCam SLAM: worker thread did not stop within the timeout; destroying native handle anyway.");
            }

            if (m_Handle != IntPtr.Zero)
            {
                NativeApi.wcs_destroy(m_Handle);
                m_Handle = IntPtr.Zero;
            }

            m_FrameAvailable.Dispose();
        }
    }
}
