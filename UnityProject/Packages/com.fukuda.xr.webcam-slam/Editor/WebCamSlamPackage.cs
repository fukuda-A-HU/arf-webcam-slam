using System.Collections.Generic;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.WebCamSlam;

namespace UnityEditor.XR.WebCamSlam
{
    /// <summary>
    /// Registers the WebCam SLAM provider with the XR Plug-in Management settings UI.
    /// </summary>
    class WebCamSlamPackage : IXRPackage
    {
        class WebCamSlamLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }

        class WebCamSlamPackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; }
        }

        static readonly WebCamSlamPackageMetadata s_Metadata = new()
        {
            packageName = "WebCam SLAM XR Plugin",
            packageId = "com.fukuda.xr.webcam-slam",
            settingsType = typeof(WebCamSlamSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>
            {
                new WebCamSlamLoaderMetadata
                {
                    loaderName = "WebCam SLAM",
                    loaderType = typeof(WebCamSlamLoader).FullName,
                    supportedBuildTargets = new List<BuildTargetGroup>
                    {
                        BuildTargetGroup.Standalone,
                    },
                },
            },
        };

        /// <inheritdoc/>
        public IXRPackageMetadata metadata => s_Metadata;

        /// <inheritdoc/>
        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            // Phase A settings only need their built-in defaults; nothing to migrate from an
            // older settings format yet.
            return obj is WebCamSlamSettings;
        }
    }
}
