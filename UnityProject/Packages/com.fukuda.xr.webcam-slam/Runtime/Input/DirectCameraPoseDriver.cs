namespace UnityEngine.XR.WebCamSlam.Input
{
    /// <summary>
    /// Fallback pose driver that moves a <see cref="Camera"/>'s <see cref="Transform"/> directly from
    /// an <see cref="IPoseSource"/>, bypassing the Input System device/<c>TrackedPoseDriver</c> path
    /// entirely. Not used by the default sample setup (which relies on
    /// <see cref="SlamInputDeviceDriver"/> and the stock <c>TrackedPoseDriver</c> binding); attach this
    /// opt-in component instead if a project cannot use the Input System's <c>TrackedPoseDriver</c>.
    /// </summary>
    public class DirectCameraPoseDriver : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The transform to drive. Defaults to this component's own transform if unset.")]
        Transform m_Target;

        IPoseSource m_PoseSource;

        /// <summary>
        /// The pose source this driver reads from every frame. Defaults to a new
        /// <see cref="DebugFlyPoseSource"/> if never assigned.
        /// </summary>
        public IPoseSource poseSource
        {
            get => m_PoseSource ??= new DebugFlyPoseSource();
            set => m_PoseSource = value;
        }

        void Awake()
        {
            if (m_Target == null)
                m_Target = transform;
        }

        void Update()
        {
            poseSource.Tick(Time.unscaledDeltaTime);
            m_Target.SetLocalPositionAndRotation(poseSource.position, poseSource.rotation);
        }
    }
}
