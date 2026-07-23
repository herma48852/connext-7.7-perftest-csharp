/*
 * (c) 2005-2026 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

using System.CommandLine;
using System.CommandLine.Parsing;

namespace PerformanceTest
{
    internal sealed class CliParseOutcome
    {
        public Parameters Parameters { get; init; }
        public bool ShouldRun { get; init; }
        public int ExitCode { get; init; }
    }

    /// <summary>
    /// Defines the C# command-line contract explicitly. The previous reflection
    /// binder called property setters for default values, which made defaults
    /// indistinguishable from values supplied by the user.
    /// </summary>
    internal static class CliParser
    {
        private interface IOptionBinding
        {
            void Apply(ParseResult parseResult, Parameters parameters);
        }

        private sealed class OptionBinding<T> : IOptionBinding
        {
            private readonly Option<T> option;
            private readonly Action<Parameters, T, bool> apply;

            public OptionBinding(Option<T> option, Action<Parameters, T, bool> apply)
            {
                this.option = option;
                this.apply = apply;
            }

            public void Apply(ParseResult parseResult, Parameters parameters)
            {
                OptionResult result = parseResult.GetResult(option) as OptionResult;
                bool explicitlySet = result != null && !result.Implicit;
                apply(parameters, parseResult.GetValue(option), explicitlySet);
            }
        }

        public static CliParseOutcome Parse(string[] args)
        {
            string[] normalizedArgs = args.Select(arg => arg switch
            {
                "-help" => "--help",
                "-version" => "--version",
                _ => arg
            }).ToArray();

            var root = new RootCommand(
                "RTI Perftest throughput and latency benchmark for Connext Professional 7.7");
            root.SetAction(_ => 0);
            var bindings = new List<IOptionBinding>();

            Add(root, bindings, "--pub", false, (p, v, set) => { p.Pub = v; p.PubSet = set; },
                "Run as publisher.", "-pub");
            Add(root, bindings, "--sub", true, (p, v, set) => { p.Sub = v; p.SubSet = set; },
                "Run as subscriber (default).", "-sub");
            Add(root, bindings, "--sidMultiSubTest", 0, (p, v, _) => p.SidMultiSubTest = v,
                "Subscriber identifier in a multi-subscriber test.", "-sidMultiSubTest");
            Add(root, bindings, "--pidMultiPubTest", 0, (p, v, _) => p.PidMultiPubTest = v,
                "Publisher identifier in a multi-publisher test.", "-pidMultiPubTest");
            Add(root, bindings, "--dataLen", 100UL, (p, v, set) => { p.DataLen = v; p.DataLenSet = set; },
                "Serialized sample size in bytes (default 100).", "-dataLen");
            Add(root, bindings, "--numIter", 100_000_000UL, (p, v, set) => { p.NumIter = v; p.NumIterSet = set; },
                "Number of samples to publish.", "-numIter");
            Add(root, bindings, "--instances", 1U, (p, v, set) => { p.Instances = v; p.InstancesSet = set; },
                "Number of keyed instances (default 1).", "-instances");
            Add(root, bindings, "--writeInstance", -1, (p, v, _) => p.WriteInstance = v,
                "Publish only the selected instance; -1 uses round-robin.", "-writeInstance");
            Add(root, bindings, "--sleep", 0U, (p, v, _) => p.Sleep = v,
                "Milliseconds to sleep between writes.", "-sleep");
            Add(root, bindings, "--latencyCount", 10000U,
                (p, v, set) => { p.LatencyCount = v; p.LatencyCountSet = set; },
                "Samples or batches between latency pings.", "-latencyCount");
            Add(root, bindings, "--numSubscribers", 1U, (p, v, _) => p.NumSubscribers = v,
                "Number of subscribers expected by the publisher.",
                "-numSubscribers", "--numSubcribers", "-numSubcribers");
            Add(root, bindings, "--numPublishers", 1U, (p, v, _) => p.NumPublishers = v,
                "Number of publishers expected by the subscriber.", "-numPublishers");
            Add(root, bindings, "--noPrintIntervals", false, (p, v, _) => p.NoPrintIntervals = v,
                "Suppress interval statistics.", "-noPrintIntervals");
            Add(root, bindings, "--useReadThread", false, (p, v, _) => p.UseReadThread = v,
                "Use a WaitSet reader loop instead of DataAvailable callbacks.", "-useReadThread");
            Add(root, bindings, "--latencyTest", false, (p, v, _) => p.LatencyTest = v,
                "Run synchronous ping-pong latency mode.", "-latencyTest");
            Add(root, bindings, "--verbosity", 1, (p, v, set) => { p.Verbosity = v; p.VerbositySet = set; },
                "Connext logging verbosity: 0 silent, 1 error, 2 warning, 3 all.", "-verbosity");
            Add(root, bindings, "--pubRate", (string)null,
                (p, v, set) => { p.PubRate = v; p.PubRateSet = set; },
                "Limit publication rate: samples/s[:spin|sleep].", "-pubRate");
            Add(root, bindings, "--keyed", false, (p, v, _) => p.Keyed = v,
                "Use keyed data.", "-keyed");
            Add(root, bindings, "--executionTime", 0UL, (p, v, _) => p.ExecutionTime = v,
                "Maximum test duration in seconds.", "-executionTime");
            Add(root, bindings, "--writerStats", false, (p, v, _) => p.WriterStats = v,
                "Print reliable-writer pulled-sample statistics.", "-writerStats");
            Add(root, bindings, "--cpu", false, (p, v, _) => p.Cpu = v,
                "Print process CPU utilization.", "-cpu");
            Add(root, bindings, "--cft", (string)null,
                (p, v, set) => { p.Cft = v; p.CftSet = set; },
                "Content-filtered instance or inclusive start:end range.", "-cft");
            Add(root, bindings, "--noOutputHeaders", false, (p, v, _) => p.NoOutputHeaders = v,
                "Suppress table and summary headers.", "-noOutputHeaders");
            Add(root, bindings, "--outputFormat", "csv", (p, v, _) => p.OutputFormat = v,
                "Output format: csv, json, or legacy.", "-outputFormat");
            Add(root, bindings, "--sendQueueSize", 50U, (p, v, _) => p.SendQueueSize = v,
                "Reliable send-window size in samples or batches.", "-sendQueueSize");
            Add(root, bindings, "--domain", 1, (p, v, _) => p.Domain = v,
                "DDS domain identifier.", "-domain");
            Add(root, bindings, "--qosFile", "perftest_qos_profiles.xml", (p, v, _) => p.QosFile = v,
                "QoS XML file.", "-qosFile");
            Add(root, bindings, "--qosLibrary", "PerftestQosLibrary", (p, v, _) => p.QosLibrary = v,
                "QoS library name.", "-qosLibrary");
            Add(root, bindings, "--bestEffort", false, (p, v, _) => p.BestEffort = v,
                "Use best-effort reliability.", "-bestEffort");
            Add(root, bindings, "--batchSize", 8_192, (p, v, set) => { p.BatchSize = v; p.BatchSizeSet = set; },
                "Batch size in bytes; explicit 0 disables batching.", "-batchSize");
            Add(root, bindings, "--noPositiveAcks", false, (p, v, _) => p.NoPositiveAcks = v,
                "Disable positive acknowledgments.", "-noPositiveAcks");
            Add(root, bindings, "--keepDurationUsec", 1000L, (p, v, _) => p.KeepDurationUsec = v,
                "Minimum sample keep duration when positive ACKs are disabled.", "-keepDurationUsec");
            Add(root, bindings, "--durability", 0U, (p, v, _) => p.Durability = v,
                "Durability: 0 volatile, 1 transient-local, 2 transient, 3 persistent.", "-durability");
            Add(root, bindings, "--dynamicData", false, (p, v, _) => p.DynamicData = v,
                "Use DynamicData (recognized but unsupported by this port).", "-dynamicData");
            Add(root, bindings, "--noDirectCommunication", false, (p, v, _) => p.NoDirectCommunication = v,
                "Use brokered communication for transient or persistent durability.", "-noDirectCommunication");
            Add(root, bindings, "--waitsetDelayUsec", 100U, (p, v, _) => p.WaitsetDelayUsec = v,
                "WaitSet batching delay in microseconds.", "-waitsetDelayUsec");
            Add(root, bindings, "--waitsetEventCount", 5UL, (p, v, _) => p.WaitsetEventCount = v,
                "WaitSet event count.", "-waitsetEventCount");
            Add(root, bindings, "--enableAutoThrottle", false, (p, v, _) => p.EnableAutoThrottle = v,
                "Enable DataWriter auto-throttling.", "-enableAutoThrottle");
            Add(root, bindings, "--enableTurboMode", false, (p, v, _) => p.EnableTurboMode = v,
                "Enable DataWriter turbo mode.", "-enableTurboMode");
            Add(root, bindings, "--crc", false, (p, v, _) => p.Crc = v,
                "Enable RTPS CRC.", "-crc");
            Add(root, bindings, "--crcKind", "CRC_32_CUSTOM", (p, v, _) => p.CrcKind = v,
                "CRC kind: CRC_32_CUSTOM or CRC_32_LEGACY.", "-crcKind");
            Add(root, bindings, "--enable-message-length", false, (p, v, _) => p.MessageLength = v,
                "Enable the RTPS message-length header extension.", "-enable-message-length");
            Add(root, bindings, "--asynchronous", false, (p, v, _) => p.Asynchronous = v,
                "Use asynchronous publishing.", "-asynchronous");
            Add(root, bindings, "--flowController", "default", (p, v, _) => p.FlowController = v,
                "Asynchronous flow controller: default, 1Gbps, or 10Gbps.", "-flowController");

            var peerOption = Add(root, bindings, "--peer", Array.Empty<string>(),
                (p, v, set) => {
                    p.Peers = v ?? Array.Empty<string>();
                    p.Peer = p.Peers.LastOrDefault();
                    p.PeerSet = set;
                }, "Initial discovery peer; may be repeated.", "-peer");
            peerOption.AllowMultipleArgumentsPerToken = false;

            Add(root, bindings, "--unbounded", false, (p, v, _) => p.Unbounded = v,
                "Use the unbounded-sequence generated type.", "-unbounded");
            Add(root, bindings, "--unboundedSize", 0UL,
                (p, v, set) => { p.UnboundedSize = v; p.UnboundedSizeSet = set; },
                "Unbounded-sequence allocation threshold.", "-unboundedSize");
            Add(root, bindings, "--transport", (string)null,
                (p, v, set) => { p.Transport = v; p.TransportSet = set; },
                "Transport: UDPv4, UDPv6, SHMEM, TCP, TLS, DTLS, or WAN.", "-transport");
            Add(root, bindings, "--instanceHashBuckets", 0,
                (p, v, set) => { p.InstanceHashBuckets = v; p.InstanceHashBucketsSet = set; },
                "Reader instance hash-bucket count.", "-instanceHashBuckets");

            Add(root, bindings, "--secureGovernanceFile", (string)null, (p, v, _) => p.SecureGovernanceFile = v,
                "Security governance document.", "-secureGovernanceFile");
            Add(root, bindings, "--securePermissionsFile", (string)null, (p, v, _) => p.SecurePermissionsFile = v,
                "Security permissions document.", "-securePermissionsFile");
            Add(root, bindings, "--secureCertAuthority", (string)null, (p, v, _) => p.SecureCertAuthority = v,
                "Security certificate authority.", "-secureCertAuthority");
            Add(root, bindings, "--secureCertFile", (string)null, (p, v, _) => p.SecureCertFile = v,
                "Security identity certificate.", "-secureCertFile");
            Add(root, bindings, "--securePrivateKey", (string)null, (p, v, _) => p.SecurePrivateKey = v,
                "Security identity private key.", "-securePrivateKey");
            Add(root, bindings, "--secureLibrary", (string)null, (p, v, _) => p.SecureLibrary = v,
                "Security plugin library override.", "-secureLibrary");
            Add(root, bindings, "--lightWeightSecurity", false, (p, v, _) => p.LightWeightSecurity = v,
                "Use Lightweight Security.", "-lightWeightSecurity");
            Add(root, bindings, "--secureEncryptionAlgorithm", (string)null,
                (p, v, _) => p.SecureEncryptionAlgo = v,
                "Security encryption algorithm.", "-secureEncryptionAlgorithm", "-secureEncryptionAlgo");
            Add(root, bindings, "--secureDebug", -1, (p, v, _) => p.SecureDebug = v,
                "Security plugin logging level.", "-secureDebug");
            Add(root, bindings, "--secureEnableAAD", false, (p, v, _) => p.SecureEnableAAD = v,
                "Enable additional authenticated data.", "-secureEnableAAD");
            Add(root, bindings, "--securePSK", (string)null, (p, v, _) => p.SecurePSK = v,
                "Enable PSK protection with the supplied seed.", "-securePSK");
            Add(root, bindings, "--securePSKAlgorithm", (string)null, (p, v, _) => p.SecurePSKAlgorithm = v,
                "PSK algorithm (default AES256+GCM).", "-securePSKAlgorithm");

            Add(root, bindings, "--enableTCP", false, (p, v, _) => p.EnableTCP = v,
                "Legacy alias for -transport TCP.", "-enableTCP");
            Add(root, bindings, "--enableUDPv6", false, (p, v, _) => p.EnableUDPv6 = v,
                "Legacy alias for -transport UDPv6.", "-enableUDPv6");
            Add(root, bindings, "--enableSharedMemory", false, (p, v, _) => p.EnableSharedMemory = v,
                "Legacy alias for -transport SHMEM.", "-enableSharedMemory");
            Add(root, bindings, "--nic", (string)null,
                (p, v, set) => { if (set) p.AllowInterfaces = v; p.AllowInterfacesSet |= set; },
                "Allowed receive interface.", "-nic");
            Add(root, bindings, "--allowInterfaces", (string)null,
                (p, v, set) => { if (set) p.AllowInterfaces = v; p.AllowInterfacesSet |= set; },
                "Allowed receive interfaces.", "-allowInterfaces");
            Add(root, bindings, "--disableInterfaceTracking", false,
                (p, v, _) => p.DisableInterfaceTracking = v,
                "Disable runtime interface-change tracking for built-in UDPv4.",
                "-disableInterfaceTracking");
            Add(root, bindings, "--configureTransportVerbosity", (string)null,
                (p, v, set) => { p.ConfigureTransportVerbosity = v; p.ConfigureTransportVerbositySet = set; },
                "Transport plugin verbosity.", "-configureTransportVerbosity");
            Add(root, bindings, "--configureTransportServerBindPort", "7400",
                (p, v, _) => p.ConfigureTransportServerBindPort = v,
                "TCP/TLS server bind port.", "-configureTransportServerBindPort");
            Add(root, bindings, "--configureTransportWan", false, (p, v, _) => p.ConfigureTransportWan = v,
                "Use TCP/TLS WAN mode.", "-configureTransportWan");
            Add(root, bindings, "--configureTransportPublicAddress", (string)null,
                (p, v, set) => { p.ConfigureTransportPublicAddress = v; p.ConfigureTransportPublicAddressSet = set; },
                "TCP/TLS public address.", "-configureTransportPublicAddress");
            Add(root, bindings, "--configureTransportCertAuthority", (string)null,
                (p, v, set) => { p.ConfigureTransportCertAuthority = v; p.ConfigureTransportCertAuthoritySet = set; },
                "Secure-transport certificate authority.", "-configureTransportCertAuthority");
            Add(root, bindings, "--configureTransportCertFile", (string)null,
                (p, v, set) => { p.ConfigureTransportCertFile = v; p.ConfigureTransportCertFileSet = set; },
                "Secure-transport identity certificate.", "-configureTransportCertFile");
            Add(root, bindings, "--configureTransportPrivateKey", (string)null,
                (p, v, set) => { p.ConfigureTransportPrivateKey = v; p.ConfigureTransportPrivateKeySet = set; },
                "Secure-transport identity private key.", "-configureTransportPrivateKey");
            Add(root, bindings, "--configureTransportWanServerAddress", (string)null,
                (p, v, set) => { p.ConfigureTransportWanServerAddress = v; p.ConfigureTransportWanServerAddressSet = set; },
                "WAN server address.", "-configureTransportWanServerAddress");
            Add(root, bindings, "--configureTransportWanServerPort", "3478",
                (p, v, _) => p.ConfigureTransportWanServerPort = v,
                "WAN server port.", "-configureTransportWanServerPort");
            Add(root, bindings, "--configureTransportWanId", (string)null,
                (p, v, set) => { p.ConfigureTransportWanId = v; p.ConfigureTransportWanIdSet = set; },
                "WAN transport instance ID.", "-configureTransportWanId");
            Add(root, bindings, "--configureTransportSecureWan", false,
                (p, v, _) => p.ConfigureTransportSecureWan = v,
                "Enable security for Real-Time WAN transport.", "-configureTransportSecureWan");
            Add(root, bindings, "--multicast", false, (p, v, _) => p.Multicast = v,
                "Enable multicast.", "-multicast");
            Add(root, bindings, "--multicastAddr", (string)null,
                (p, v, set) => { p.MulticastAddr = v; p.MulticastAddrSet = set; },
                "One multicast address or throughput,latency,announcement addresses.", "-multicastAddr");
            Add(root, bindings, "--noMulticast", false,
                (p, v, set) => { p.NoMulticast = v; p.NoMulticastSet = set; },
                "Disable multicast.", "-noMulticast");

            ParseResult parseResult = root.Parse(normalizedArgs);
            bool helpOrVersion = normalizedArgs.Any(
                arg => arg is "--help" or "-h" or "-?" or "/?" or "--version");
            if (parseResult.Errors.Count > 0 || helpOrVersion)
            {
                parseResult.Invoke();
                return new CliParseOutcome
                {
                    ShouldRun = false,
                    ExitCode = parseResult.Errors.Count == 0 ? 0 : 2
                };
            }

            var parameters = new Parameters();
            foreach (IOptionBinding binding in bindings)
            {
                binding.Apply(parseResult, parameters);
            }

            if (parameters.LatencyTest && !parameters.LatencyCountSet)
            {
                parameters.LatencyCount = 1;
            }

            return new CliParseOutcome
            {
                Parameters = parameters,
                ShouldRun = true,
                ExitCode = 0
            };
        }

        private static Option<T> Add<T>(
            RootCommand root,
            ICollection<IOptionBinding> bindings,
            string name,
            T defaultValue,
            Action<Parameters, T, bool> apply,
            string description,
            params string[] aliases)
        {
            var option = new Option<T>(name, aliases)
            {
                Description = description,
                DefaultValueFactory = _ => defaultValue
            };
            root.Options.Add(option);
            bindings.Add(new OptionBinding<T>(option, apply));
            return option;
        }
    }
}
