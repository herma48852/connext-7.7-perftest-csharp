/*
 * (c) 2005-2021 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

namespace PerformanceTest
{
    public class Parameters
    {
        private uint instances;
        private string pubRate;
        private string cft;
        private int instanceHashBuckets;
        public ulong dataLen;
        private ulong numIter;
        public int batchSize;
        public string crcKind;
        public string peer;
        private string transport;
        private string allowInterfaces;
        private string configureTransportVerbosity;
        private string configureTransportPublicAddress;
        private string configureTransportWanServerAddress;
        private string configureTransportWanId;
        private string configureTransportCertFile;
        private string configureTransportPrivateKey;
        private bool noMulticast;
        private string multicastAddr;
        private ulong unboundedSize;
        private ulong messageSizeMax;
        private string configureTransportCertAuthority;
        private int verbosity;
        private bool latencyTest;
        public bool CftSet { get; set; }
        public bool PubRateSet { get; set; }
        public bool InstancesSet { get; set; }
        public bool InstanceHashBucketsSet { get; set; }
        public bool DataLenSet { get; set; }
        public bool NumIterSet { get; set; }
        public bool LatencyCountSet { get; set; }
        public bool BatchSizeSet { get; set; }
        public bool PeerSet { get; set; }
        public bool TransportSet { get; set; }
        public bool AllowInterfacesSet { get; set; }
        public bool ConfigureTransportVerbositySet { get; set; }
        public bool ConfigureTransportPublicAddressSet { get; set; }
        public bool ConfigureTransportWanServerAddressSet { get; set; }
        public bool ConfigureTransportWanIdSet { get; set; }
        public bool NoMulticastSet { get; set; }
        public bool MulticastAddrSet { get; set; }
        public bool UnboundedSizeSet { get; set; }
        public bool messageSizeMaxSet { get; set; }
        public bool ConfigureTransportCertAuthoritySet { get; set; }
        public bool ConfigureTransportCertFileSet { get; set; }
        public bool ConfigureTransportPrivateKeySet { get; set; }
        public bool VerbositySet { get; set; }
        public bool PubSet { get; set; }
        public bool SubSet { get; set; }
        public bool Pub { get; set; }
        public bool Sub { get; set; }
        public int SidMultiSubTest { get; set; }
        public int PidMultiPubTest { get; set; }
        public ulong DataLen
        {
                get => dataLen;
                set => dataLen = value;
        }
        public ulong NumIter { get => numIter; set => numIter = value; }
        public uint Instances
        {
                get => instances;
                set => instances = value;
        }
        public int WriteInstance { get; set; }
        public uint Sleep { get; set; }
        public uint LatencyCount { get; set; }
        public uint NumSubscribers { get; set; }
        public uint NumPublishers { get; set; }
        public bool NoPrintIntervals { get; set; }
        public bool UseReadThread { get; set; }
        public bool LatencyTest
        {
                get => latencyTest;
                set => latencyTest = value;
        }
        public int Verbosity
        {
                get => verbosity;
                set => verbosity = value;
        }
        public string PubRate
        {
                get => pubRate;
                set => pubRate = value;
        }
        public bool Keyed { get; set; }
        public ulong ExecutionTime { get; set; }
        public bool WriterStats { get; set; }
        public bool Cpu { get; set; }
        public string Cft
        {
                get => cft;
                set => cft = value;
        }
        public bool NoOutputHeaders { get; set; }
        public string OutputFormat { get; set; }
        public uint SendQueueSize { get; set; }
        public string QosFile { get; set; }
        public string QosLibrary { get; set; }
        public bool BestEffort { get; set; }
        public int BatchSize
        {
            get => batchSize;
            set => batchSize = value;
        }
        public bool NoPositiveAcks { get; set; }
        public long KeepDurationUsec { get; set; }
        public uint Durability { get; set; }
        public bool DynamicData { get; set; }
        public bool NoDirectCommunication { get; set; }
        public uint WaitsetDelayUsec { get; set; }
        public ulong WaitsetEventCount { get; set; }
        public bool EnableAutoThrottle { get; set; }
        public bool EnableTurboMode { get; set; }
        public bool Crc { get; set; }
        public string CrcKind { get; set; }
        public bool MessageLength { get; set; }
        public bool Asynchronous { get; set; }
        public string FlowController { get; set; }
        public string Peer
        {
            get => peer;
            set => peer = value;
        }
        public string[] Peers { get; set; } = System.Array.Empty<string>();
        public bool Unbounded { get; set; }
        public ulong UnboundedSize
        {
            get => unboundedSize;
            set => unboundedSize = value;
        }
        public ulong MessageSizeMax
        {
            get => messageSizeMax;
            set => messageSizeMax = value;
        }
        public int Domain { get; set; }
        public string Transport
        {
            get => transport;
            set => transport = value;
        }
        public int InstanceHashBuckets
        {
            get => instanceHashBuckets;
            set => instanceHashBuckets = value;
        }
        public string SecureGovernanceFile { get; set; }
        public string SecurePermissionsFile { get; set; }
        public string SecureCertAuthority { get; set; }
        public string SecureCertFile { get; set; }
        public string SecurePrivateKey { get; set; }
        public string SecureLibrary { get; set; }
        public bool LightWeightSecurity { get; set; }
        public string SecureEncryptionAlgo { get; set; }
        public bool SecureEnableAAD{ get; set; }
        public string SecurePSK{ get; set; }
        public string SecurePSKAlgorithm{ get; set; }
        public int SecureDebug { get; set; }
        // Transport arguments
        public bool EnableTCP { get; set; }
        public bool EnableUDPv6 { get; set; }
        public bool EnableSharedMemory { get; set; }
        public string Nic
        {
                get => allowInterfaces;
                set => allowInterfaces = value;
        }
        public string AllowInterfaces
        {
                get => allowInterfaces;
                set => allowInterfaces = value;
        }
        public string ConfigureTransportVerbosity
        {
            get => configureTransportVerbosity;
            set => configureTransportVerbosity = value;
        }
        public string ConfigureTransportServerBindPort { get; set; }
        public bool ConfigureTransportWan { get; set; }
        public string ConfigureTransportPublicAddress
        {
            get => configureTransportPublicAddress;
            set => configureTransportPublicAddress = value;
        }
        public string ConfigureTransportCertAuthority
        {
            get => configureTransportCertAuthority;
            set => configureTransportCertAuthority = value;
        }
        public string ConfigureTransportCertFile
        {
            get => configureTransportCertFile;
            set => configureTransportCertFile = value;
        }
        public string ConfigureTransportPrivateKey
        {
            get => configureTransportPrivateKey;
            set => configureTransportPrivateKey = value;
        }
        public string ConfigureTransportWanServerAddress
        {
            get => configureTransportWanServerAddress;
            set => configureTransportWanServerAddress = value;
        }
        public string ConfigureTransportWanServerPort { get; set; }
        public string ConfigureTransportWanId
        {
            get => configureTransportWanId;
            set => configureTransportWanId = value;
        }
        public bool ConfigureTransportSecureWan { get; set; }
        public bool DisableInterfaceTracking { get; set; }
        public bool Multicast { get; set; }
        public string MulticastAddr
        {
            get => multicastAddr;
            set => multicastAddr = value;
        }
        public bool NoMulticast
        {
            get => noMulticast;
            set => noMulticast = value;
        }
    }
}
