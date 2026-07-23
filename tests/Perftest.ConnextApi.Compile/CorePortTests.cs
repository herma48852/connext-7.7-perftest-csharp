/*
 * (c) 2005-2026 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

namespace PerformanceTest.Tests
{
    using Xunit;

    public class CorePortTests
    {
        [Fact]
        public void AnnouncementProtocolMatchesCppSentinels()
        {
            var listener = new AnnouncementListener();
            var message = new TestMessage
            {
                entityId = 3,
                Size = Perftest.INITIALIZE_SIZE
            };

            listener.ProcessMessage(message);
            listener.ProcessMessage(message);
            Assert.Equal(1, listener.ActiveSubscriberCount);

            message.Size = 1;
            listener.ProcessMessage(message);
            Assert.Equal(1, listener.ActiveSubscriberCount);

            message.Size = Perftest.FINISHED_SIZE;
            listener.ProcessMessage(message);
            Assert.Equal(0, listener.ActiveSubscriberCount);
        }

        [Fact]
        public void TypeHelperClonesOwnIndependentSamples()
        {
            var original = new DataTypeHelper(1_024);
            ITypeHelper<TestData_t> clone = original.Clone();
            var message = new TestMessage { Size = 128 };

            TestData_t first = original.MessageToSample(message, 1);
            TestData_t second = clone.MessageToSample(message, 2);

            Assert.NotSame(first, second);
            Assert.Equal(1, first.key[0]);
            Assert.Equal(2, second.key[0]);
        }

        [Fact]
        public void SecureTransportOverridesReachTransportConfiguration()
        {
            CliParseOutcome outcome = CliParser.Parse(new[]
            {
                "-pub", "-transport", "tls",
                "-configureTransportCertAuthority", "ca.pem",
                "-configureTransportCertFile", "identity.pem",
                "-configureTransportPrivateKey", "identity-key.pem"
            });
            var transport = new PerftestTransport();

            Assert.True(transport.ParseTransportOptions(outcome.Parameters));
            string summary = transport.PrintTransportConfigurationSummary();
            Assert.Contains("Kind: TLS", summary);
            Assert.Contains("ca.pem", summary);
            Assert.Contains("identity.pem", summary);
            Assert.Contains("identity-key.pem", summary);
        }

        [Fact]
        public void DisableInterfaceTrackingReachesUdpv4ParticipantQos()
        {
            CliParseOutcome outcome = CliParser.Parse(new[]
            {
                "-sub", "-transport", "UDPv4", "-disableInterfaceTracking"
            });
            var transport = new PerftestTransport();
            var qos = Rti.Dds.Domain.DomainParticipantQos.Default;

            Assert.True(transport.ParseTransportOptions(outcome.Parameters));
            Assert.True(transport.ConfigureTransport(ref qos));
            Assert.True(qos.Property.Value.TryGetValue(
                "dds.transport.UDPv4.builtin.disable_interface_tracking",
                out var property));
            Assert.Equal("true", property.Value);
            Assert.Contains(
                "Interface Tracking: Disabled",
                transport.PrintTransportConfigurationSummary());
        }

        [Fact]
        public void MonotonicClockAdvancesWithoutOverflowing()
        {
            ulong start = Perftest.GetTimeUsec();
            bool advanced = SpinWait.SpinUntil(
                () => Perftest.GetTimeUsec() > start,
                TimeSpan.FromSeconds(1));

            Assert.True(advanced);
        }
    }
}
