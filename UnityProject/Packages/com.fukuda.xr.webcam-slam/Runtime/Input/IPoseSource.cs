namespace UnityEngine.XR.WebCamSlam.Input
{
    /// <summary>
    /// Supplies a camera pose, in world space, to be forwarded to the Input System every frame by
    /// <see cref="SlamInputDeviceDriver"/>. Phase A only has <see cref="DebugFlyPoseSource"/>; a real
    /// monocular SLAM implementation is expected to implement this interface in a later phase.
    /// </summary>
    public interface IPoseSource
    {
        /// <summary>
        /// The current position, in world space.
        /// </summary>
        Vector3 position { get; }

        /// <summary>
        /// The current rotation, in world space.
        /// </summary>
        Quaternion rotation { get; }

        /// <summary>
        /// Called once per frame (from <c>Update</c>) before <see cref="position"/> and
        /// <see cref="rotation"/> are read, so implementations can process input.
        /// </summary>
        void Tick(float deltaTime);
    }
}
