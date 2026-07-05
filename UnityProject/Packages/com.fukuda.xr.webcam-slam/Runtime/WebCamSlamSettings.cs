using UnityEngine.XR.Management;

namespace UnityEngine.XR.WebCamSlam
{
    /// <summary>
    /// How the AR camera pose is driven while no real tracking implementation is active.
    /// </summary>
    public enum PoseSource
    {
        /// <summary>
        /// Drive the pose with a keyboard/mouse "fly" controller. Useful for desktop debugging
        /// before monocular SLAM tracking is implemented.
        /// </summary>
        DebugFly,
    }

    /// <summary>
    /// Build-time and runtime configuration for the WebCam SLAM XR provider.
    /// An instance of this asset is created and managed by XR Plug-in Management.
    /// </summary>
    [XRConfigurationData("WebCam SLAM", SettingsKey)]
    public class WebCamSlamSettings : ScriptableObject
    {
        /// <summary>
        /// The key used to store/retrieve this settings asset from <c>EditorBuildSettings</c>.
        /// </summary>
        public const string SettingsKey = "com.fukuda.xr.webcam-slam.settings";

        [SerializeField]
        [Tooltip("The name of the webcam device to open. Leave empty to use the system default device.")]
        string m_PreferredDeviceName = "";

        [SerializeField]
        [Tooltip("Requested webcam capture width in pixels.")]
        int m_RequestedWidth = 1280;

        [SerializeField]
        [Tooltip("Requested webcam capture height in pixels.")]
        int m_RequestedHeight = 720;

        [SerializeField]
        [Tooltip("Requested webcam capture frame rate.")]
        int m_RequestedFps = 30;

        [SerializeField]
        [Tooltip("Approximate vertical field of view in degrees, used to derive camera intrinsics until real calibration is implemented.")]
        float m_VerticalFovDegrees = 60f;

        [SerializeField]
        [Tooltip("Mirror the webcam feed horizontally. Most front-facing webcams already mirror in their driver, so this is normally left off.")]
        bool m_MirrorHorizontally = false;

        [SerializeField]
        [Tooltip("How the AR camera pose is driven while no real tracking implementation is active.")]
        PoseSource m_PoseSource = PoseSource.DebugFly;

        [SerializeField]
        [Tooltip("Uniform scale factor applied to positions reported to the Input System pose device, in meters per world unit.")]
        float m_MeterScale = 1.0f;

        /// <summary>
        /// The name of the webcam device to open. Empty means "use the system default device".
        /// </summary>
        public string preferredDeviceName
        {
            get => m_PreferredDeviceName;
            set => m_PreferredDeviceName = value;
        }

        /// <summary>
        /// Requested webcam capture width in pixels.
        /// </summary>
        public int requestedWidth
        {
            get => m_RequestedWidth;
            set => m_RequestedWidth = value;
        }

        /// <summary>
        /// Requested webcam capture height in pixels.
        /// </summary>
        public int requestedHeight
        {
            get => m_RequestedHeight;
            set => m_RequestedHeight = value;
        }

        /// <summary>
        /// Requested webcam capture frame rate.
        /// </summary>
        public int requestedFps
        {
            get => m_RequestedFps;
            set => m_RequestedFps = value;
        }

        /// <summary>
        /// Approximate vertical field of view in degrees, used to derive camera intrinsics
        /// until real calibration is implemented.
        /// </summary>
        public float verticalFovDegrees
        {
            get => m_VerticalFovDegrees;
            set => m_VerticalFovDegrees = value;
        }

        /// <summary>
        /// Mirror the webcam feed horizontally.
        /// </summary>
        public bool mirrorHorizontally
        {
            get => m_MirrorHorizontally;
            set => m_MirrorHorizontally = value;
        }

        /// <summary>
        /// How the AR camera pose is driven while no real tracking implementation is active.
        /// </summary>
        public PoseSource poseSource
        {
            get => m_PoseSource;
            set => m_PoseSource = value;
        }

        /// <summary>
        /// Uniform scale factor applied to positions reported to the Input System pose device,
        /// in meters per world unit.
        /// </summary>
        public float meterScale
        {
            get => m_MeterScale;
            set => m_MeterScale = value;
        }
    }
}
