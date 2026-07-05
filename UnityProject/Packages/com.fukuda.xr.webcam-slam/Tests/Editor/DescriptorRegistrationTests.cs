using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.WebCamSlam.Subsystems;

namespace UnityEngine.XR.WebCamSlam.EditorTests
{
    /// <summary>
    /// Verifies that the WebCam SLAM session and camera subsystem descriptors are registered with
    /// <see cref="SubsystemManager"/> via their <c>[RuntimeInitializeOnLoadMethod]</c> registration
    /// methods, and that a subsystem instance can actually be created from each descriptor.
    /// </summary>
    [TestFixture]
    public class DescriptorRegistrationTests
    {
        // EditMode test runs don't reliably re-trigger RuntimeInitializeLoadType.SubsystemRegistration,
        // so explicitly (re-)invoke the same registration methods [RuntimeInitializeOnLoadMethod] calls
        // in a normal run. Registering an id that's already present is a harmless no-op re-registration.
        [OneTimeSetUp]
        public void EnsureDescriptorsAreRegistered()
        {
            WebCamSlamSessionSubsystem.RegisterDescriptor();
            WebCamSlamCameraSubsystem.RegisterDescriptor();
        }

        [Test]
        public void SessionDescriptor_IsRegistered_AndCreatesSubsystem()
        {
            var descriptors = new List<XRSessionSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);

            var descriptor = descriptors.Find(d => d.id == WebCamSlamSessionSubsystem.subsystemId);
            Assert.IsNotNull(descriptor, $"No session subsystem descriptor registered with id '{WebCamSlamSessionSubsystem.subsystemId}'.");

            var subsystem = descriptor.Create();
            try
            {
                Assert.IsInstanceOf<WebCamSlamSessionSubsystem>(subsystem);
            }
            finally
            {
                subsystem.Destroy();
            }
        }

        [Test]
        public void CameraDescriptor_IsRegistered_AndCreatesSubsystem()
        {
            var descriptors = new List<XRCameraSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);

            var descriptor = descriptors.Find(d => d.id == WebCamSlamCameraSubsystem.subsystemId);
            Assert.IsNotNull(descriptor, $"No camera subsystem descriptor registered with id '{WebCamSlamCameraSubsystem.subsystemId}'.");

            var subsystem = descriptor.Create();
            try
            {
                Assert.IsInstanceOf<WebCamSlamCameraSubsystem>(subsystem);
            }
            finally
            {
                subsystem.Destroy();
            }
        }

        [Test]
        public void CameraDescriptor_DoesNotSupportCpuImages_InPhaseA()
        {
            var descriptors = new List<XRCameraSubsystemDescriptor>();
            SubsystemManager.GetSubsystemDescriptors(descriptors);

            var descriptor = descriptors.Find(d => d.id == WebCamSlamCameraSubsystem.subsystemId);
            Assert.IsNotNull(descriptor);
            Assert.IsFalse(descriptor.supportsCameraImage);
        }
    }
}
