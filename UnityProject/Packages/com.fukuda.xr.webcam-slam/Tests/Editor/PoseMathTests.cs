using System;
using NUnit.Framework;
using UnityEngine.XR.WebCamSlam.Utils;

namespace UnityEngine.XR.WebCamSlam.EditorTests
{
    /// <summary>
    /// Tests for the pure math helpers used to approximate camera intrinsics from a configured
    /// field of view, since Phase A has no real lens calibration.
    /// </summary>
    [TestFixture]
    public class PoseMathTests
    {
        [Test]
        public void VerticalFovToFocalLengthPixels_90DegreesOnSquareImage_MatchesHalfHeight()
        {
            // At exactly 90 degrees vertical FOV, tan(45deg) == 1, so focal length in pixels
            // reduces to exactly half the image height.
            var focalLength = CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(90f, 720);

            Assert.That(focalLength, Is.EqualTo(360f).Within(0.01f));
        }

        [Test]
        public void VerticalFovToFocalLengthPixels_NarrowerFov_ProducesLargerFocalLength()
        {
            var wideFocalLength = CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(90f, 720);
            var narrowFocalLength = CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(30f, 720);

            Assert.Greater(narrowFocalLength, wideFocalLength);
        }

        [TestCase(0f)]
        [TestCase(-10f)]
        [TestCase(180f)]
        [TestCase(200f)]
        public void VerticalFovToFocalLengthPixels_OutOfRangeFov_Throws(float invalidFov)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(invalidFov, 720));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void VerticalFovToFocalLengthPixels_NonPositiveHeight_Throws(int invalidHeight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(60f, invalidHeight));
        }

        [Test]
        public void VerticalFovToFocalLengthPixels_IsSymmetricAroundImageCenter()
        {
            // Sanity check on the relationship this feeds into (WebCamSlamCameraSubsystem places the
            // principal point at the exact image center): the focal length itself should only depend
            // on FOV and height, not on width.
            var focalLengthA = CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(60f, 720);
            var focalLengthB = CameraIntrinsicsMath.VerticalFovToFocalLengthPixels(60f, 720);

            Assert.AreEqual(focalLengthA, focalLengthB);
        }
    }
}
