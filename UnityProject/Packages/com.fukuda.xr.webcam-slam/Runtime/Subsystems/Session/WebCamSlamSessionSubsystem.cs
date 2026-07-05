using System;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.WebCamSlam.Input;

namespace UnityEngine.XR.WebCamSlam.Subsystems
{
    /// <summary>
    /// WebCam SLAM implementation of
    /// [XRSessionSubsystem](xref:UnityEngine.XR.ARSubsystems.XRSessionSubsystem).
    /// </summary>
    /// <remarks>
    /// Phase A does not perform any real tracking: the session is reported as permanently
    /// <see cref="TrackingState.Tracking"/> as soon as it is started, so that AR Foundation's
    /// end-to-end plumbing (camera background, pose driver) can be exercised without a
    /// SLAM implementation in place yet. This subsystem also owns the lifetime of the debug
    /// pose driver (<see cref="SlamInputDeviceDriver"/>), mirroring how XR Simulation's
    /// <c>SimulationSessionSubsystem</c> owns its camera pose provider.
    /// </remarks>
    public sealed class WebCamSlamSessionSubsystem : XRSessionSubsystem
    {
        internal const string subsystemId = "WebCamSlam-Session";

        class WebCamSlamProvider : Provider
        {
            Guid m_SessionId;
            SlamInputDeviceDriver m_InputDeviceDriver;

            public override TrackingState trackingState => TrackingState.Tracking;

            public override Guid sessionId => m_SessionId;

            public override Promise<SessionAvailability> GetAvailabilityAsync() =>
                Promise<SessionAvailability>.CreateResolvedPromise(SessionAvailability.Installed | SessionAvailability.Supported);

            protected override bool TryInitialize()
            {
                m_SessionId = Guid.NewGuid();
                return true;
            }

            public override void Start()
            {
                var settings = WebCamSlamLoader.activeSettings;
                var poseSource = CreatePoseSource(settings);
                m_InputDeviceDriver = new SlamInputDeviceDriver(poseSource);
            }

            public override void Stop()
            {
                m_InputDeviceDriver?.Dispose();
                m_InputDeviceDriver = null;
            }

            public override void Destroy()
            {
                m_InputDeviceDriver?.Dispose();
                m_InputDeviceDriver = null;
            }

            public override void Reset()
            {
                m_SessionId = Guid.NewGuid();
            }

            public override void Update(XRSessionUpdateParams updateParams)
            {
            }

            static IPoseSource CreatePoseSource(WebCamSlamSettings settings)
            {
                // DebugFly is currently the only pose source; the switch is here so adding a real
                // SLAM-backed IPoseSource in a later phase doesn't require touching the call site.
                var poseSource = settings != null ? settings.poseSource : PoseSource.DebugFly;
                switch (poseSource)
                {
                    case PoseSource.DebugFly:
                    default:
                        return new DebugFlyPoseSource();
                }
            }
        }

        /// <summary>
        /// Registers the session subsystem descriptor with <see cref="SubsystemManager"/>. Also
        /// callable directly from tests (via <c>InternalsVisibleTo</c>), since EditMode test runs
        /// do not reliably re-trigger <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void RegisterDescriptor()
        {
            XRSessionSubsystemDescriptor.Register(new XRSessionSubsystemDescriptor.Cinfo
            {
                id = subsystemId,
                providerType = typeof(WebCamSlamProvider),
                subsystemTypeOverride = typeof(WebCamSlamSessionSubsystem),
                supportsInstall = false,
                supportsMatchFrameRate = false,
            });
        }
    }
}
