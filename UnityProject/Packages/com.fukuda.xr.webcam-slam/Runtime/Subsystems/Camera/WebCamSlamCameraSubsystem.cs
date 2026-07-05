using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.WebCamSlam.Subsystems
{
    /// <summary>
    /// WebCam SLAM implementation of
    /// [XRCameraSubsystem](xref:UnityEngine.XR.ARSubsystems.XRCameraSubsystem).
    /// </summary>
    /// <remarks>
    /// Captures a <see cref="WebCamTexture"/> via <see cref="WebCamFrameSource"/> and exposes it to
    /// <c>ARCameraBackground</c> as an <see cref="XRTextureDescriptor"/>, following the same structure
    /// as AR Foundation's XR Simulation <c>CameraTextureProvider</c>. Phase A does not support CPU
    /// image access (<c>TryAcquireLatestCpuImage</c>).
    /// </remarks>
    public sealed class WebCamSlamCameraSubsystem : XRCameraSubsystem
    {
        internal const string subsystemId = "WebCamSlam-Camera";

        const string k_BackgroundShaderName = "Unlit/WebCamSlamBackground";

        /// <summary>
        /// The shader property name for the webcam color texture.
        /// </summary>
        internal const string textureSinglePropertyName = "_MainTex";

        class WebCamSlamProvider : Provider
        {
            WebCamFrameSource m_FrameSource;
            Material m_CameraMaterial;
            XRCameraConfiguration m_XRCameraConfiguration;
            XRCameraIntrinsics m_XRCameraIntrinsics;
            int m_TexturePropertyNameId;

            public override Feature currentCamera => Feature.WorldFacingCamera;

            public override Material cameraMaterial => m_CameraMaterial;

            public override bool permissionGranted => true;

            public override XRCameraConfiguration? currentConfiguration
            {
                get => m_XRCameraConfiguration;
                set
                {
                    if (value == null)
                        throw new ArgumentNullException(nameof(value), "cannot set the camera configuration to null");

                    m_XRCameraConfiguration = value.Value;
                }
            }

            public override XRSupportedCameraBackgroundRenderingMode supportedBackgroundRenderingMode =>
                XRSupportedCameraBackgroundRenderingMode.BeforeOpaques;

            public override XRCameraBackgroundRenderingMode currentBackgroundRenderingMode =>
                XRCameraBackgroundRenderingMode.BeforeOpaques;

            public WebCamSlamProvider()
            {
                m_TexturePropertyNameId = Shader.PropertyToID(textureSinglePropertyName);

                var backgroundShader = Shader.Find(k_BackgroundShaderName);
                if (backgroundShader == null)
                    Debug.LogError($"WebCam SLAM: could not find background shader '{k_BackgroundShaderName}'.");
                else
                    m_CameraMaterial = CreateCameraMaterial(k_BackgroundShaderName);
            }

            WebCamSlamSettings GetSettings()
            {
                return WebCamSlamLoader.activeSettings != null
                    ? WebCamSlamLoader.activeSettings
                    : ScriptableObject.CreateInstance<WebCamSlamSettings>();
            }

            public override void Start()
            {
                var settings = GetSettings();
                m_FrameSource = WebCamFrameSource.Create(settings);

                m_XRCameraConfiguration = new XRCameraConfiguration(
                    IntPtr.Zero,
                    new Vector2Int(settings.requestedWidth, settings.requestedHeight));

                m_XRCameraIntrinsics = ApproximateIntrinsics(
                    settings.requestedWidth,
                    settings.requestedHeight,
                    settings.verticalFovDegrees);
            }

            public override void Stop()
            {
            }

            public override void Destroy()
            {
                if (m_FrameSource != null)
                {
                    UnityEngine.Object.Destroy(m_FrameSource.gameObject);
                    m_FrameSource = null;
                }
            }

            public override NativeArray<XRCameraConfiguration> GetConfigurations(
                XRCameraConfiguration defaultCameraConfiguration, Allocator allocator)
            {
                var configs = new NativeArray<XRCameraConfiguration>(1, allocator);
                configs[0] = m_XRCameraConfiguration;
                return configs;
            }

            public override NativeArray<XRTextureDescriptor> GetTextureDescriptors(
                XRTextureDescriptor defaultDescriptor, Allocator allocator)
            {
                var descriptors = new XRTextureDescriptor[1];

                if (m_FrameSource != null && m_FrameSource.isReady)
                {
                    Shader.SetGlobalTexture(m_TexturePropertyNameId, m_FrameSource.outputTexture);

                    descriptors[0] = new XRTextureDescriptor(
                        nativeTexture: m_FrameSource.outputNativeTexturePtr,
                        width: m_FrameSource.outputWidth,
                        height: m_FrameSource.outputHeight,
                        mipmapCount: 0,
                        format: GraphicsFormatUtility.GetTextureFormat(m_FrameSource.outputTexture.graphicsFormat),
                        propertyNameId: m_TexturePropertyNameId,
                        depth: 0,
                        dimension: TextureDimension.Tex2D);
                }
                else
                {
                    descriptors[0] = default;
                }

                return new NativeArray<XRTextureDescriptor>(descriptors, allocator);
            }

            public override bool TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                if (m_FrameSource == null || !m_FrameSource.isReady)
                {
                    cameraFrame = default;
                    return false;
                }

                var properties = XRCameraFrameProperties.Timestamp;

                cameraFrame = new XRCameraFrame(
                    timestamp: m_FrameSource.lastFrameTimestampNs,
                    averageBrightness: default,
                    averageColorTemperature: default,
                    colorCorrection: default,
                    projectionMatrix: Matrix4x4.identity,
                    displayMatrix: Matrix4x4.identity,
                    trackingState: TrackingState.Tracking,
                    nativePtr: m_FrameSource.outputNativeTexturePtr,
                    properties: properties,
                    averageIntensityInLumens: default,
                    exposureDuration: default,
                    exposureOffset: default,
                    mainLightIntensityInLumens: default,
                    mainLightColor: default,
                    mainLightDirection: default,
                    ambientSphericalHarmonics: default,
                    cameraGrain: default,
                    noiseIntensity: default);

                return true;
            }

            public override bool TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                cameraIntrinsics = m_XRCameraIntrinsics;
                return true;
            }

            /// <summary>
            /// Derives an approximate <see cref="XRCameraIntrinsics"/> from a requested resolution and vertical
            /// field of view. This is a rough stand-in until real lens calibration is implemented; see
            /// <see cref="Utils.CameraIntrinsicsMath"/> for the pure math this wraps.
            /// </summary>
            internal static XRCameraIntrinsics ApproximateIntrinsics(int width, int height, float verticalFovDegrees)
            {
                var focalLengthPixels = Utils.CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(verticalFovDegrees, height);
                var focalLength = new Vector2(focalLengthPixels, focalLengthPixels);
                var principalPoint = new Vector2(width * 0.5f, height * 0.5f);
                var resolution = new Vector2Int(width, height);
                return new XRCameraIntrinsics(focalLength, principalPoint, resolution);
            }
        }

        /// <summary>
        /// Registers the camera subsystem descriptor with <see cref="SubsystemManager"/>. Also
        /// callable directly from tests (via <c>InternalsVisibleTo</c>), since EditMode test runs
        /// do not reliably re-trigger <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/>.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void RegisterDescriptor()
        {
            XRCameraSubsystemDescriptor.Register(new XRCameraSubsystemDescriptor.Cinfo
            {
                id = subsystemId,
                providerType = typeof(WebCamSlamProvider),
                subsystemTypeOverride = typeof(WebCamSlamCameraSubsystem),
                supportsCameraConfigurations = true,
                supportsCameraImage = false,
                supportsAverageBrightness = false,
                supportsAverageColorTemperature = false,
                supportsColorCorrection = false,
                supportsAverageIntensityInLumens = false,
                supportsFocusModes = false,
                supportsFaceTrackingAmbientIntensityLightEstimation = false,
                supportsFaceTrackingHDRLightEstimation = false,
                supportsWorldTrackingAmbientIntensityLightEstimation = false,
                supportsWorldTrackingHDRLightEstimation = false,
                supportsCameraGrain = false,
                supportsExifData = false,
            });
        }
    }
}
