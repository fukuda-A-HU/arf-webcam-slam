using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using UnityEngine.XR.WebCamSlam.Subsystems;

namespace UnityEngine.XR.WebCamSlam
{
    /// <summary>
    /// Manages the lifecycle of the WebCam SLAM session and camera subsystems.
    /// </summary>
    public class WebCamSlamLoader : XRLoaderHelper
    {
        static List<XRSessionSubsystemDescriptor> s_SessionSubsystemDescriptors = new();
        static List<XRCameraSubsystemDescriptor> s_CameraSubsystemDescriptors = new();

        /// <summary>
        /// Well-known <c>Resources</c> path (without extension) that <see cref="WebCamSlamSettings"/>
        /// is loaded from at runtime, if present. Placing a <see cref="WebCamSlamSettings"/> asset at
        /// <c>Assets/Resources/WebCamSlamSettings.asset</c> (in any package or project location that
        /// contributes to Resources) lets users configure the provider without touching code.
        /// </summary>
        internal const string settingsResourcesPath = "WebCamSlamSettings";

        /// <summary>
        /// The settings in effect for the currently active loader instance. Populated by
        /// <see cref="Initialize"/> and consumed by <see cref="WebCamSlamCameraSubsystem"/>, since
        /// subsystem providers are not otherwise given a reference to the loader that created them.
        /// </summary>
        internal static WebCamSlamSettings activeSettings { get; private set; }

        /// <summary>
        /// The settings used by this loader instance. Falls back to a <c>Resources</c>-loaded or
        /// default instance if none was assigned explicitly (e.g. when constructed directly in tests).
        /// </summary>
        public WebCamSlamSettings settings { get; set; }

        /// <summary>
        /// The currently active camera subsystem, if any.
        /// </summary>
        public WebCamSlamCameraSubsystem cameraSubsystem => GetLoadedSubsystem<XRCameraSubsystem>() as WebCamSlamCameraSubsystem;

        /// <summary>
        /// The currently active session subsystem, if any.
        /// </summary>
        public WebCamSlamSessionSubsystem sessionSubsystem => GetLoadedSubsystem<XRSessionSubsystem>() as WebCamSlamSessionSubsystem;

        /// <summary>
        /// Creates the session and camera subsystems.
        /// </summary>
        /// <returns><c>true</c> if the session subsystem was successfully created, otherwise <c>false</c>.</returns>
        public override bool Initialize()
        {
            settings ??= Resources.Load<WebCamSlamSettings>(settingsResourcesPath);
            settings ??= ScriptableObject.CreateInstance<WebCamSlamSettings>();
            activeSettings = settings;

            CreateSubsystem<XRSessionSubsystemDescriptor, XRSessionSubsystem>(s_SessionSubsystemDescriptors, WebCamSlamSessionSubsystem.subsystemId);
            CreateSubsystem<XRCameraSubsystemDescriptor, XRCameraSubsystem>(s_CameraSubsystemDescriptors, WebCamSlamCameraSubsystem.subsystemId);

            var sessionSubsystemInstance = GetLoadedSubsystem<XRSessionSubsystem>();
            if (sessionSubsystemInstance == null)
                Debug.LogError("Failed to load WebCam SLAM session subsystem.");

            return sessionSubsystemInstance != null;
        }

        /// <summary>
        /// Starts the session and camera subsystems.
        /// </summary>
        /// <returns>Always returns <c>true</c>.</returns>
        public override bool Start()
        {
            StartSubsystem<XRSessionSubsystem>();
            StartSubsystem<XRCameraSubsystem>();
            return true;
        }

        /// <summary>
        /// Stops the session and camera subsystems.
        /// </summary>
        /// <returns>Always returns <c>true</c>.</returns>
        public override bool Stop()
        {
            StopSubsystem<XRCameraSubsystem>();
            StopSubsystem<XRSessionSubsystem>();
            return true;
        }

        /// <summary>
        /// Destroys the session and camera subsystems.
        /// </summary>
        /// <returns>Always returns <c>true</c>.</returns>
        public override bool Deinitialize()
        {
            DestroySubsystem<XRCameraSubsystem>();
            DestroySubsystem<XRSessionSubsystem>();

            if (activeSettings == settings)
                activeSettings = null;

            return base.Deinitialize();
        }
    }
}
