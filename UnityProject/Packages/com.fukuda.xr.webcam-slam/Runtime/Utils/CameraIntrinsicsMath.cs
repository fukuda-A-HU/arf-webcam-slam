using System;

namespace UnityEngine.XR.WebCamSlam.Utils
{
    /// <summary>
    /// Pure math helpers for approximating <see cref="UnityEngine.XR.ARSubsystems.XRCameraIntrinsics"/>
    /// from a user-supplied field of view, since Phase A has no real lens calibration.
    /// </summary>
    public static class CameraIntrinsicsMath
    {
        /// <summary>
        /// Converts a vertical field of view (in degrees) and an image height (in pixels) into a
        /// focal length expressed in pixels, using the standard pinhole camera relationship
        /// <c>focalLengthPixels = (height / 2) / tan(verticalFov / 2)</c>.
        /// </summary>
        /// <param name="verticalFovDegrees">The vertical field of view, in degrees. Must be in the open interval (0, 180).</param>
        /// <param name="heightPixels">The image height, in pixels. Must be greater than zero.</param>
        /// <returns>The focal length, in pixels.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="verticalFovDegrees"/> is not in (0, 180), or
        /// <paramref name="heightPixels"/> is not positive.
        /// </exception>
        public static float VerticalFovToFocalLengthPixels(float verticalFovDegrees, int heightPixels)
        {
            if (verticalFovDegrees <= 0f || verticalFovDegrees >= 180f)
                throw new ArgumentOutOfRangeException(nameof(verticalFovDegrees), verticalFovDegrees, "Vertical FOV must be in the open interval (0, 180) degrees.");

            if (heightPixels <= 0)
                throw new ArgumentOutOfRangeException(nameof(heightPixels), heightPixels, "Height must be positive.");

            var halfFovRadians = 0.5f * verticalFovDegrees * Mathf.Deg2Rad;
            return (heightPixels * 0.5f) / Mathf.Tan(halfFovRadians);
        }
    }
}
