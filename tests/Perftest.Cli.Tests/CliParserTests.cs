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
            Assert.False(outcome.Parameters.DisableInterfaceTracking);
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
        public void TwoMachineMulticastCommandsAreAccepted()
        {
            const string multicastAddresses =
                "239.255.2.1,239.255.2.2,239.255.2.3";

            CliParseOutcome throughput = CliParser.Parse(new[]
            {
                "-pub", "-domain", "101", "-transport", "UDPv4",
                "-nic", "192.168.2.10", "-peer", "192.168.2.20",
                "-disableInterfaceTracking",
                "-multicast", "-multicastAddr", multicastAddresses,
                "-dataLen", "32768", "-batchSize", "0",
                "-executionTime", "60", "-noPrintIntervals", "-cpu",
                "-outputFormat", "json"
            });

            Assert.True(throughput.ShouldRun);
            Assert.True(throughput.Parameters.Pub);
            Assert.Equal(101, throughput.Parameters.Domain);
            Assert.Equal("UDPv4", throughput.Parameters.Transport);
            Assert.Equal("192.168.2.10", throughput.Parameters.AllowInterfaces);
            Assert.Equal(new[] { "192.168.2.20" }, throughput.Parameters.Peers);
            Assert.True(throughput.Parameters.DisableInterfaceTracking);
            Assert.True(throughput.Parameters.Multicast);
            Assert.Equal(multicastAddresses, throughput.Parameters.MulticastAddr);
            Assert.Equal(32_768UL, throughput.Parameters.DataLen);
            Assert.Equal(0, throughput.Parameters.BatchSize);
            Assert.Equal(60UL, throughput.Parameters.ExecutionTime);
            Assert.True(throughput.Parameters.NoPrintIntervals);
            Assert.True(throughput.Parameters.Cpu);
            Assert.Equal("json", throughput.Parameters.OutputFormat);

            CliParseOutcome latency = CliParser.Parse(new[]
            {
                "-pub", "-domain", "103", "-transport", "UDPv4",
                "-nic", "192.168.2.10", "-peer", "192.168.2.20",
                "-disableInterfaceTracking",
                "-multicast", "-multicastAddr", multicastAddresses,
                "-latencyTest", "-batchSize", "0", "-dataLen", "64",
                "-numIter", "10000", "-noPrintIntervals", "-cpu",
                "-outputFormat", "json"
            });

            Assert.True(latency.ShouldRun);
            Assert.True(latency.Parameters.LatencyTest);
            Assert.Equal(1U, latency.Parameters.LatencyCount);
            Assert.Equal(0, latency.Parameters.BatchSize);
            Assert.Equal(64UL, latency.Parameters.DataLen);
            Assert.Equal(10_000UL, latency.Parameters.NumIter);
            Assert.True(latency.Parameters.DisableInterfaceTracking);
            Assert.Equal(multicastAddresses, latency.Parameters.MulticastAddr);
        }

        [Fact]
        public void WindowsLauncherPreservesCommaDelimitedArguments()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            const string multicastAddresses =
                "239.255.2.1,239.255.2.2,239.255.2.3";
            string templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "perftest_cs.bat");
            string harnessPath = Path.Combine(
                Path.GetTempPath(),
                $"perftest_cs_{Guid.NewGuid():N}.bat");

            try
            {
                File.WriteAllText(
                    harnessPath,
                    File.ReadAllText(templatePath)
                        + Environment.NewLine
                        + "echo [%args%]"
                        + Environment.NewLine);

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = Environment.GetEnvironmentVariable("ComSpec"),
                    Arguments = $"/d /c call \"{harnessPath}\" "
                        + $"-multicastAddr {multicastAddresses} -help",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using System.Diagnostics.Process process =
                    System.Diagnostics.Process.Start(startInfo);
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Assert.Equal(0, process.ExitCode);
                Assert.Contains(multicastAddresses, output);
                Assert.DoesNotContain(
                    "239.255.2.1 239.255.2.2 239.255.2.3",
                    output);
                Assert.True(string.IsNullOrEmpty(error), error);
            }
            finally
            {
                if (File.Exists(harnessPath))
                {
                    File.Delete(harnessPath);
                }
            }
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
