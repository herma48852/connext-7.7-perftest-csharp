/*
 * (c) 2005-2026 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

namespace PerformanceTest.Tests
{
    using Xunit;

    public class CliParserTests
    {
        [Fact]
        public void DefaultsAreNotReportedAsExplicit()
        {
            CliParseOutcome outcome = CliParser.Parse(Array.Empty<string>());

            Assert.True(outcome.ShouldRun);
            Assert.Equal(0, outcome.ExitCode);
            Assert.False(outcome.Parameters.Pub);
            Assert.True(outcome.Parameters.Sub);
            Assert.False(outcome.Parameters.DataLenSet);
            Assert.False(outcome.Parameters.NumIterSet);
            Assert.False(outcome.Parameters.InstancesSet);
            Assert.False(outcome.Parameters.BatchSizeSet);
            Assert.False(outcome.Parameters.UnboundedSizeSet);
            Assert.Equal(100UL, outcome.Parameters.DataLen);
            Assert.Equal(100_000_000UL, outcome.Parameters.NumIter);
            Assert.Equal(1U, outcome.Parameters.Instances);
            Assert.Equal(8_192, outcome.Parameters.BatchSize);
            Assert.Equal(-1, outcome.Parameters.WriteInstance);
            Assert.Equal(1, outcome.Parameters.Verbosity);
        }

        [Fact]
        public void ExplicitValuesSetTheirPresenceFlags()
        {
            CliParseOutcome outcome = CliParser.Parse(new[]
            {
                "-pub", "-dataLen", "1024", "-numIter", "50",
                "-instances", "4", "-batchSize", "0", "-unboundedSize", "2048"
            });

            Assert.True(outcome.ShouldRun);
            Assert.True(outcome.Parameters.Pub);
            Assert.True(outcome.Parameters.PubSet);
            Assert.Equal(1024UL, outcome.Parameters.DataLen);
            Assert.True(outcome.Parameters.DataLenSet);
            Assert.Equal(50UL, outcome.Parameters.NumIter);
            Assert.True(outcome.Parameters.NumIterSet);
            Assert.Equal(4U, outcome.Parameters.Instances);
            Assert.True(outcome.Parameters.InstancesSet);
            Assert.Equal(0, outcome.Parameters.BatchSize);
            Assert.True(outcome.Parameters.BatchSizeSet);
            Assert.Equal(2048UL, outcome.Parameters.UnboundedSize);
            Assert.True(outcome.Parameters.UnboundedSizeSet);
        }

        [Fact]
        public void LatencyModeUsesOnePingUnlessExplicitlyOverridden()
        {
            CliParseOutcome defaultLatency = CliParser.Parse(new[] { "-pub", "-latencyTest" });
            CliParseOutcome explicitLatency = CliParser.Parse(
                new[] { "-pub", "-latencyTest", "-latencyCount", "7" });

            Assert.Equal(1U, defaultLatency.Parameters.LatencyCount);
            Assert.False(defaultLatency.Parameters.LatencyCountSet);
            Assert.Equal(7U, explicitLatency.Parameters.LatencyCount);
            Assert.True(explicitLatency.Parameters.LatencyCountSet);
        }

        [Fact]
        public void LegacySubscriberSpellingsAndRepeatedPeersAreAccepted()
        {
            CliParseOutcome outcome = CliParser.Parse(new[]
            {
                "--numSubcribers", "3",
                "-peer", "10.0.0.1",
                "--peer", "10.0.0.2"
            });

            Assert.True(outcome.ShouldRun);
            Assert.Equal(3U, outcome.Parameters.NumSubscribers);
            Assert.True(outcome.Parameters.PeerSet);
            Assert.Equal(new[] { "10.0.0.1", "10.0.0.2" }, outcome.Parameters.Peers);
        }

        [Fact]
        public void InvalidNumericInputReturnsUsageError()
        {
            CliParseOutcome outcome = CliParser.Parse(new[] { "-domain", "not-a-number" });

            Assert.False(outcome.ShouldRun);
            Assert.Equal(2, outcome.ExitCode);
        }

        [Theory]
        [InlineData("-help")]
        [InlineData("--help")]
        [InlineData("-h")]
        [InlineData("-version")]
        [InlineData("--version")]
        public void HelpAndVersionSpellingsExitSuccessfully(string argument)
        {
            CliParseOutcome outcome = CliParser.Parse(new[] { argument });

            Assert.False(outcome.ShouldRun);
            Assert.Equal(0, outcome.ExitCode);
        }

        [Fact]
        public void UnknownOptionReturnsUsageError()
        {
            CliParseOutcome outcome = CliParser.Parse(new[] { "-notARealPerftestOption" });

            Assert.False(outcome.ShouldRun);
            Assert.Equal(2, outcome.ExitCode);
        }

        [Fact]
        public void SecureTransportFileOverridesRemainExplicit()
        {
            CliParseOutcome outcome = CliParser.Parse(new[]
            {
                "-transport", "TLS",
                "-configureTransportCertAuthority", "ca.pem",
                "-configureTransportCertFile", "identity.pem",
                "-configureTransportPrivateKey", "identity-key.pem"
            });

            Assert.True(outcome.ShouldRun);
            Assert.Equal("TLS", outcome.Parameters.Transport);
            Assert.True(outcome.Parameters.TransportSet);
            Assert.Equal("ca.pem", outcome.Parameters.ConfigureTransportCertAuthority);
            Assert.True(outcome.Parameters.ConfigureTransportCertAuthoritySet);
            Assert.Equal("identity.pem", outcome.Parameters.ConfigureTransportCertFile);
            Assert.True(outcome.Parameters.ConfigureTransportCertFileSet);
            Assert.Equal("identity-key.pem", outcome.Parameters.ConfigureTransportPrivateKey);
            Assert.True(outcome.Parameters.ConfigureTransportPrivateKeySet);
        }
    }
}
