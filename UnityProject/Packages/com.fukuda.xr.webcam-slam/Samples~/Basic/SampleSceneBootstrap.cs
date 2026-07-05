using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.WebCamSlam.Samples
{
    /// <summary>
    /// Builds a minimal AR Foundation scene at runtime that is wired up to the WebCam SLAM provider:
    /// an <see cref="ARSession"/>, an <see cref="XROrigin"/> with an <see cref="ARCameraManager"/>,
    /// <see cref="ARCameraBackground"/>, and a <see cref="TrackedPoseDriver"/> bound to
    /// <c>&lt;HandheldARInputDevice&gt;/devicePosition|deviceRotation</c>. Press Space to place a cube
    /// 1.5m in front of the camera, as a quick sanity check that the debug pose is moving the camera.
    /// </summary>
    /// <remarks>
    /// Add this component to a single empty <see cref="GameObject"/> in an otherwise empty scene.
    /// Requires XR Plug-in Management to have "WebCam SLAM" enabled for Standalone.
    /// </remarks>
    public class SampleSceneBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Distance, in meters, in front of the camera at which Space spawns a cube.")]
        float m_PlacementDistance = 1.5f;

        [SerializeField]
        [Tooltip("Prefab to spawn when Space is pressed. If unset, a default primitive cube is created.")]
        GameObject m_PlacementPrefab;

        Camera m_ArCamera;

        void Start()
        {
            var sessionGo = new GameObject("AR Session");
            sessionGo.AddComponent<ARSession>();

            var origin = BuildXROrigin();
            m_ArCamera = origin.Camera;
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                PlaceCubeInFrontOfCamera();
        }

        XROrigin BuildXROrigin()
        {
            var originGo = new GameObject("XR Origin");
            var origin = originGo.AddComponent<XROrigin>();

            var offsetGo = new GameObject("Camera Offset");
            offsetGo.transform.SetParent(originGo.transform, false);

            var cameraGo = new GameObject("AR Camera",
                typeof(Camera),
                typeof(AudioListener),
                typeof(ARCameraManager),
                typeof(ARCameraBackground),
                typeof(TrackedPoseDriver));
            cameraGo.transform.SetParent(offsetGo.transform, false);

            var camera = cameraGo.GetComponent<Camera>();
            camera.tag = "MainCamera";
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;

            ConfigureTrackedPoseDriver(cameraGo.GetComponent<TrackedPoseDriver>());

            origin.CameraFloorOffsetObject = offsetGo;
            origin.Camera = camera;

            return origin;
        }

        static void ConfigureTrackedPoseDriver(TrackedPoseDriver trackedPoseDriver)
        {
            // Matches the binding AR Foundation's own "XR Origin (Mobile AR)" menu item wires up:
            // a device-agnostic primary binding, with the WebCam SLAM debug device as a fallback,
            // so no custom binding code is required for the debug pose to reach the camera.
            var positionAction = new InputAction("Position", expectedControlType: "Vector3");
            positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");

            var rotationAction = new InputAction("Rotation", expectedControlType: "Quaternion");
            rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");

            trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
            trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
        }

        void PlaceCubeInFrontOfCamera()
        {
            if (m_ArCamera == null)
                return;

            var pose = m_ArCamera.transform;
            var spawnPosition = pose.position + pose.forward * m_PlacementDistance;

            var cube = m_PlacementPrefab != null
                ? Instantiate(m_PlacementPrefab, spawnPosition, Quaternion.identity)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            cube.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        }
    }
}
