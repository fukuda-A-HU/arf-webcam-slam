using System;

namespace UnityEngine.XR.WebCamSlam.Subsystems
{
    /// <summary>
    /// Owns the <see cref="WebCamTexture"/> device capture and republishes each frame into a plain
    /// <see cref="RenderTexture"/> that has already been corrected for the source video's rotation
    /// and mirroring. Runs on a hidden, persistent <see cref="GameObject"/> so it keeps capturing
    /// across scene loads for as long as the camera subsystem is running.
    /// </summary>
    class WebCamFrameSource : MonoBehaviour
    {
        const string k_BlitShaderName = "Hidden/WebCamSlamBlit";
        static readonly int k_BlitParamsId = Shader.PropertyToID("_WebCamSlamBlitParams");

        WebCamTexture m_WebCamTexture;
        RenderTexture m_OutputTexture;
        Material m_BlitMaterial;
        WebCamSlamSettings m_Settings;

        long m_LastFrameTimestampNs;
        int m_LastUploadedFrame = -1;

        /// <summary>
        /// The corrected, right-side-up camera frame. Valid once <see cref="isReady"/> is <c>true</c>.
        /// </summary>
        public RenderTexture outputTexture => m_OutputTexture;

        /// <summary>
        /// The native texture pointer for <see cref="outputTexture"/>, captured once at creation time.
        /// </summary>
        public IntPtr outputNativeTexturePtr { get; private set; }

        /// <summary>
        /// Timestamp, in nanoseconds, of the most recently uploaded webcam frame.
        /// </summary>
        public long lastFrameTimestampNs => m_LastFrameTimestampNs;

        /// <summary>
        /// <c>true</c> once the webcam device has produced at least one frame and the output
        /// texture is ready to be consumed.
        /// </summary>
        public bool isReady { get; private set; }

        /// <summary>
        /// The width, in pixels, of <see cref="outputTexture"/>.
        /// </summary>
        public int outputWidth => m_OutputTexture != null ? m_OutputTexture.width : 0;

        /// <summary>
        /// The height, in pixels, of <see cref="outputTexture"/>.
        /// </summary>
        public int outputHeight => m_OutputTexture != null ? m_OutputTexture.height : 0;

        /// <summary>
        /// Creates the hidden game object hosting a <see cref="WebCamFrameSource"/> and starts
        /// capturing according to <paramref name="settings"/>.
        /// </summary>
        public static WebCamFrameSource Create(WebCamSlamSettings settings)
        {
            var go = new GameObject("WebCamSlam Frame Source")
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            Object.DontDestroyOnLoad(go);

            var source = go.AddComponent<WebCamFrameSource>();
            source.Initialize(settings);
            return source;
        }

        void Initialize(WebCamSlamSettings settings)
        {
            m_Settings = settings;

            var shader = Shader.Find(k_BlitShaderName);
            if (shader == null)
            {
                Debug.LogError($"WebCam SLAM: could not find shader '{k_BlitShaderName}'.");
                return;
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
            {
                Debug.LogWarning("WebCam SLAM: no webcam devices found. Camera background will be unavailable.");
                return;
            }

            m_BlitMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };

            var deviceIndex = 0;
            if (!string.IsNullOrEmpty(settings.preferredDeviceName))
            {
                deviceIndex = Array.FindIndex(devices, d => d.name == settings.preferredDeviceName);
                if (deviceIndex < 0)
                {
                    Debug.LogWarning($"WebCam SLAM: preferred device '{settings.preferredDeviceName}' not found. " +
                        $"Falling back to '{devices[0].name}'.");
                    deviceIndex = 0;
                }
            }

            var device = devices[deviceIndex];
            m_WebCamTexture = CreateWebCamTexture(device, settings);
            m_WebCamTexture.Play();
        }

        /// <summary>
        /// Creates a <see cref="WebCamTexture"/> for <paramref name="device"/>, honoring the settings'
        /// requested resolution only if the device actually reports it as supported. Devices that don't
        /// expose <see cref="WebCamDevice.availableResolutions"/> (common for virtual cameras such as
        /// OBS Virtual Camera or SpoutCam) are opened with the device's own default resolution instead,
        /// since forcing an unsupported size makes Unity fail to find a supported resolution and never
        /// deliver a frame.
        /// </summary>
        static WebCamTexture CreateWebCamTexture(WebCamDevice device, WebCamSlamSettings settings)
        {
            var availableResolutions = device.availableResolutions;
            if (availableResolutions == null || availableResolutions.Length == 0)
                return new WebCamTexture(device.name);

            var bestIndex = 0;
            var bestAreaDelta = long.MaxValue;
            for (var i = 0; i < availableResolutions.Length; i++)
            {
                var resolution = availableResolutions[i];
                var widthDelta = (long)resolution.width - settings.requestedWidth;
                var heightDelta = (long)resolution.height - settings.requestedHeight;
                var areaDelta = Math.Abs(widthDelta * resolution.height) + Math.Abs(heightDelta * resolution.width);
                if (areaDelta < bestAreaDelta)
                {
                    bestAreaDelta = areaDelta;
                    bestIndex = i;
                }
            }

            var chosen = availableResolutions[bestIndex];
            var fps = (int)Math.Round(chosen.refreshRateRatio.value);
            return new WebCamTexture(device.name, chosen.width, chosen.height, fps);
        }

        void Update()
        {
            if (m_WebCamTexture == null || !m_WebCamTexture.isPlaying)
                return;

            // WebCamTexture reports didUpdateThisFrame only after the first real frame has arrived,
            // so we don't create the output RenderTexture until we know the real capture dimensions.
            if (!m_WebCamTexture.didUpdateThisFrame)
                return;

            EnsureOutputTexture();

            var rotation = m_WebCamTexture.videoRotationAngle;
            var verticalMirror = m_WebCamTexture.videoVerticallyMirrored;
            var horizontalMirror = m_Settings.mirrorHorizontally;

            m_BlitMaterial.SetVector(k_BlitParamsId, new Vector4(
                rotation,
                verticalMirror ? 1f : 0f,
                horizontalMirror ? 1f : 0f,
                0f));

            Graphics.Blit(m_WebCamTexture, m_OutputTexture, m_BlitMaterial);

            m_LastFrameTimestampNs = (long)(Time.timeAsDouble * 1_000_000_000.0);
            m_LastUploadedFrame = Time.frameCount;
            isReady = true;
        }

        void EnsureOutputTexture()
        {
            var isRotated90or270 = m_WebCamTexture.videoRotationAngle is 90 or 270;
            var width = isRotated90or270 ? m_WebCamTexture.height : m_WebCamTexture.width;
            var height = isRotated90or270 ? m_WebCamTexture.width : m_WebCamTexture.height;

            if (m_OutputTexture != null && m_OutputTexture.width == width && m_OutputTexture.height == height)
                return;

            if (m_OutputTexture != null)
                m_OutputTexture.Release();

            m_OutputTexture = new RenderTexture(width, height, depth: 0, format: RenderTextureFormat.ARGB32)
            {
                name = "WebCamSlam Output Texture",
                hideFlags = HideFlags.HideAndDontSave,
            };
            m_OutputTexture.Create();

            // Per Phase A guidance: fetch the native texture pointer once, right after creation,
            // rather than every frame.
            outputNativeTexturePtr = m_OutputTexture.GetNativeTexturePtr();
        }

        void OnDestroy()
        {
            if (m_WebCamTexture != null)
            {
                m_WebCamTexture.Stop();
                Object.Destroy(m_WebCamTexture);
                m_WebCamTexture = null;
            }

            if (m_OutputTexture != null)
            {
                m_OutputTexture.Release();
                Object.Destroy(m_OutputTexture);
                m_OutputTexture = null;
            }

            if (m_BlitMaterial != null)
            {
                Object.Destroy(m_BlitMaterial);
                m_BlitMaterial = null;
            }
        }
    }
}
