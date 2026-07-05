using UnityEngine.InputSystem;

namespace UnityEngine.XR.WebCamSlam.Input
{
    /// <summary>
    /// A keyboard/mouse "fly" camera controller used to debug AR Foundation content before a real
    /// monocular SLAM implementation is available. WASD moves horizontally, Q/E move down/up, and
    /// holding the right mouse button and dragging rotates the view (FPS-style).
    /// </summary>
    public class DebugFlyPoseSource : IPoseSource
    {
        /// <summary>
        /// The starting position, in world space, used when the pose source is constructed.
        /// </summary>
        public static readonly Vector3 startingPosition = new(0f, 1.2f, 0f);

        /// <summary>
        /// Movement speed, in world units per second, applied while a movement key is held.
        /// </summary>
        public float moveSpeed { get; set; } = 2f;

        /// <summary>
        /// Mouse look sensitivity, in degrees per pixel of mouse delta.
        /// </summary>
        public float lookSensitivity { get; set; } = 0.1f;

        /// <inheritdoc/>
        public Vector3 position { get; private set; }

        /// <inheritdoc/>
        public Quaternion rotation { get; private set; }

        float m_Yaw;
        float m_Pitch;

        /// <summary>
        /// Creates a new fly pose source starting at <see cref="startingPosition"/> with no rotation.
        /// </summary>
        public DebugFlyPoseSource()
        {
            position = startingPosition;
            rotation = Quaternion.identity;
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (mouse != null && mouse.rightButton.isPressed)
            {
                var delta = mouse.delta.ReadValue();
                m_Yaw += delta.x * lookSensitivity;
                m_Pitch -= delta.y * lookSensitivity;
                m_Pitch = Mathf.Clamp(m_Pitch, -89f, 89f);
            }

            rotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);

            if (keyboard != null)
            {
                var move = Vector3.zero;

                if (keyboard.wKey.isPressed) move += Vector3.forward;
                if (keyboard.sKey.isPressed) move += Vector3.back;
                if (keyboard.aKey.isPressed) move += Vector3.left;
                if (keyboard.dKey.isPressed) move += Vector3.right;
                if (keyboard.eKey.isPressed) move += Vector3.up;
                if (keyboard.qKey.isPressed) move += Vector3.down;

                if (move.sqrMagnitude > 0f)
                {
                    // Horizontal movement (WASD) is relative to where we're looking (yaw only),
                    // while vertical movement (Q/E) stays in world space.
                    var horizontal = new Vector3(move.x, 0f, move.z);
                    var worldMove = Quaternion.Euler(0f, m_Yaw, 0f) * horizontal + new Vector3(0f, move.y, 0f);
                    position += worldMove.normalized * (moveSpeed * deltaTime);
                }
            }
        }
    }
}
