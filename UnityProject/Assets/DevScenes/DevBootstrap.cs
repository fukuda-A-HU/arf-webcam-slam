using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace WebCamSlam.DevScenes
{
    /// <summary>
    /// Development-only copy of <c>Samples~/Basic/SampleSceneBootstrap.cs</c>.
    /// </summary>
    /// <remarks>
    /// <c>Samples~</c> directories (the trailing <c>~</c> hides them from Unity) cannot be referenced
    /// directly from inside the same project, so this is a thin, asmdef-less duplicate kept in
    /// <c>Assets/DevScenes</c> purely so this repository's own <c>UnityProject</c> has a scene to
    /// manually verify Phase A in Play Mode. If you change this file, consider whether
    /// <c>Samples~/Basic/SampleSceneBootstrap.cs</c> needs the same change.
    /// </remarks>
    public class DevBootstrap : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Distance, in meters, in front of the camera at which Space spawns a cube.")]
        float m_PlacementDistance = 1.5f;

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

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        }
    }
}
