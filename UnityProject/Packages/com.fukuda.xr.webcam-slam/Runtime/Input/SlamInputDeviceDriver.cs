using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR.ARSubsystems;

using InputSystemApi = UnityEngine.InputSystem.InputSystem;

namespace UnityEngine.XR.WebCamSlam.Input
{
    /// <summary>
    /// Adds a <see cref="HandheldARInputDevice"/> to the Input System and drives its
    /// <c>devicePosition</c>/<c>deviceRotation</c> controls every frame from an <see cref="IPoseSource"/>,
    /// using plain <see cref="InputState.Change{TState}(InputControl, TState, InputUpdateType, InputEventPtr)"/>
    /// calls rather than a native input provider. This lets the stock <c>TrackedPoseDriver</c> binding
    /// (<c>&lt;HandheldARInputDevice&gt;/devicePosition|deviceRotation</c>) work without modification.
    /// </summary>
    public class SlamInputDeviceDriver : System.IDisposable
    {
        HandheldARInputDevice m_Device;
        IPoseSource m_PoseSource;
        bool m_Disposed;

        /// <summary>
        /// The <see cref="HandheldARInputDevice"/> added to the Input System by this driver.
        /// </summary>
        public HandheldARInputDevice device => m_Device;

        /// <summary>
        /// Creates the input device and starts driving it from <paramref name="poseSource"/>.
        /// </summary>
        /// <param name="poseSource">The pose source to read from every frame.</param>
        public SlamInputDeviceDriver(IPoseSource poseSource)
        {
            m_PoseSource = poseSource;

            // HandheldARInputDevice is marked [InputControlLayout(isGenericTypeOfDevice = true)], which
            // means the Input System auto-registers a layout for the type itself during its normal
            // assembly scan. Registering it again here is a harmless no-op if it's already known
            // (the Input System deduplicates by layout name) and guarantees AddDevice<T> below
            // succeeds even if that automatic scan hasn't run yet.
            InputSystemApi.RegisterLayout<HandheldARInputDevice>();

            m_Device = InputSystemApi.AddDevice<HandheldARInputDevice>("WebCam SLAM Camera Pose");

            InputSystemApi.onBeforeUpdate += OnBeforeUpdate;
        }

        void OnBeforeUpdate()
        {
            if (m_Disposed || m_Device == null || m_PoseSource == null)
                return;

            m_PoseSource.Tick(Time.unscaledDeltaTime);

            InputState.Change(m_Device.devicePosition, m_PoseSource.position);
            InputState.Change(m_Device.deviceRotation, m_PoseSource.rotation);
        }

        /// <summary>
        /// Stops driving the device and removes it from the Input System.
        /// </summary>
        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            InputSystemApi.onBeforeUpdate -= OnBeforeUpdate;

            if (m_Device != null)
            {
                InputSystemApi.RemoveDevice(m_Device);
                m_Device = null;
            }
        }
    }
}
