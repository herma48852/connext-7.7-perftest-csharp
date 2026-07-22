/*
 * (c) 2005-2026 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

namespace PerformanceTest.Tests
{
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Text.Json;
    using Xunit;

    public sealed class Udpv4LatencyRegressionTests
    {
        private const int SampleCount = 250;
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(60);

        [Fact]
        [Trait("Category", "ConnextIntegration")]
        public async Task LatencyMeasurementsAreReportedOverUdpv4()
        {
            string repositoryRoot = FindRepositoryRoot();
            string applicationPath = FindApplicationPath(repositoryRoot);
            int domain = 100 + Random.Shared.Next(100);

            using var timeout = new CancellationTokenSource(TestTimeout);
            using Process subscriber = StartPerftest(
                applicationPath,
                repositoryRoot,
                "-sub",
                "-domain", domain.ToString(CultureInfo.InvariantCulture),
                "-transport", "UDPv4",
                "-noPrintIntervals",
                "-outputFormat", "json");

            Task<ProcessResult> subscriberResultTask = ObserveAsync(
                subscriber,
                timeout.Token);

            // Starting the subscriber first makes the test deterministic while DDS
            // discovery still handles any difference in endpoint creation time.
            await Task.Delay(TimeSpan.FromMilliseconds(750), timeout.Token);

            using Process publisher = StartPerftest(
                applicationPath,
                repositoryRoot,
                "-pub",
                "-domain", domain.ToString(CultureInfo.InvariantCulture),
                "-transport", "UDPv4",
                "-latencyTest",
                "-numIter", SampleCount.ToString(CultureInfo.InvariantCulture),
                "-noPrintIntervals",
                "-outputFormat", "json");

            Task<ProcessResult> publisherResultTask = ObserveAsync(
                publisher,
                timeout.Token);

            ProcessResult[] results;
            try
            {
                results = await Task.WhenAll(publisherResultTask, subscriberResultTask);
            }
            catch
            {
                KillIfRunning(publisher);
                KillIfRunning(subscriber);
                throw;
            }

            ProcessResult publisherResult = results[0];
            ProcessResult subscriberResult = results[1];

            AssertProcessSucceeded("publisher", publisherResult);
            AssertProcessSucceeded("subscriber", subscriberResult);
            Assert.Contains("Mode: LATENCY TEST", publisherResult.StandardError);
            Assert.Contains("Kind: UDPv4", publisherResult.StandardError);
            Assert.Contains("Kind: UDPv4", subscriberResult.StandardError);
            Assert.DoesNotContain(
                "No Pong samples have been received",
                publisherResult.StandardError);

            AssertLatencySummary(publisherResult.StandardOutput);
            AssertThroughputSummary(subscriberResult.StandardOutput);
        }

        private static Process StartPerftest(
            string applicationPath,
            string workingDirectory,
            params string[] arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            startInfo.ArgumentList.Add(applicationPath);
            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            var process = new Process { StartInfo = startInfo };
            Assert.True(process.Start(), "Failed to start the Perftest process.");
            return process;
        }

        private static async Task<ProcessResult> ObserveAsync(
            Process process,
            CancellationToken cancellationToken)
        {
            Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
            Task<string> standardError = process.StandardError.ReadToEndAsync();

            try
            {
                await process.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                KillIfRunning(process);
                throw new TimeoutException(
                    $"Perftest process {process.Id} did not exit within {TestTimeout}.");
            }

            return new ProcessResult(
                process.ExitCode,
                await standardOutput,
                await standardError);
        }

        private static void KillIfRunning(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between HasExited and Kill.
            }
        }

        private static void AssertProcessSucceeded(
            string role,
            ProcessResult result)
        {
            Assert.True(
                result.ExitCode == 0,
                $"The {role} exited with code {result.ExitCode}.\n"
                + $"stdout:\n{result.StandardOutput}\n"
                + $"stderr:\n{result.StandardError}");
        }

        private static void AssertLatencySummary(string standardOutput)
        {
            using JsonDocument output = JsonDocument.Parse(standardOutput);
            JsonElement summary = output.RootElement
                .GetProperty("perftest")[0]
                .GetProperty("summary");

            double average = summary.GetProperty("latency_ave").GetDouble();
            double standardDeviation = summary.GetProperty("latency_std").GetDouble();
            ulong minimum = summary.GetProperty("latency_min").GetUInt64();
            ulong maximum = summary.GetProperty("latency_max").GetUInt64();
            ulong percentile50 = summary.GetProperty("latency_50").GetUInt64();
            ulong percentile90 = summary.GetProperty("latency_90").GetUInt64();
            ulong percentile99 = summary.GetProperty("latency_99").GetUInt64();
            ulong percentile9999 = summary.GetProperty("latency_99.99").GetUInt64();
            ulong percentile999999 = summary.GetProperty("latency_99.9999").GetUInt64();

            Assert.True(maximum > 0, "The latency summary contained no measured latency.");
            Assert.InRange(average, (double)minimum, (double)maximum);
            Assert.True(standardDeviation >= 0);
            Assert.True(minimum <= percentile50);
            Assert.True(percentile50 <= percentile90);
            Assert.True(percentile90 <= percentile99);
            Assert.True(percentile99 <= percentile9999);
            Assert.True(percentile9999 <= percentile999999);
            Assert.True(percentile999999 <= maximum);
        }

        private static void AssertThroughputSummary(string standardOutput)
        {
            using JsonDocument output = JsonDocument.Parse(standardOutput);
            JsonElement summary = output.RootElement
                .GetProperty("perftest")[0]
                .GetProperty("summary");

            Assert.Equal(SampleCount, summary.GetProperty("packets").GetInt32());
            Assert.Equal(0, summary.GetProperty("lost").GetInt32());
        }

        private static string FindApplicationPath(string repositoryRoot)
        {
            string configuration = typeof(Udpv4LatencyRegressionTests)
                .Assembly
                .GetCustomAttribute<AssemblyConfigurationAttribute>()
                ?.Configuration ?? "Debug";
            string applicationPath = Path.Combine(
                repositoryRoot,
                "srcCs",
                "bin",
                configuration,
                "net8.0",
                "perftest_cs.dll");

            Assert.True(
                File.Exists(applicationPath),
                $"The Perftest application was not built at '{applicationPath}'.");
            return applicationPath;
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "perftest_qos_profiles.xml"))
                    && Directory.Exists(Path.Combine(directory.FullName, "srcCs")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "Could not locate the repository root from the test output directory.");
        }

        private sealed record ProcessResult(
            int ExitCode,
            string StandardOutput,
            string StandardError);
    }
}
