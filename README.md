# RTI Perftest for Connext Professional 7.7 — C# / .NET 8

This repository provides a production-ready C# port of **RTI Perftest 4.3** for
**RTI Connext DDS Professional 7.7**. The C# application targets **.NET 8** and
uses the modern `Rti.ConnextDds` 7.7 API.

Perftest measures:

- maximum DDS throughput;
- best-case latency;
- latency while the system is under load; and
- sample loss and CPU use under configurable DDS, transport, and QoS settings.

This project is derived from the
[RTI Community Perftest repository](https://github.com/rticommunity/rtiperftest).
It is maintained independently and is not an official RTI product release.
RTI Connext DDS Professional and the appropriate licenses must be installed
separately.

## Contents

- [How Perftest works](#how-perftest-works)
- [C# feature scope](#c-feature-scope)
- [Prerequisites](#prerequisites)
- [Clone and configure](#clone-and-configure)
- [Build](#build)
- [Quick start](#quick-start)
- [Common benchmark scenarios](#common-benchmark-scenarios)
- [Important command-line options](#important-command-line-options)
- [Transports, discovery, security, and QoS](#transports-discovery-security-and-qos)
- [Understanding the results](#understanding-the-results)
- [C++ interoperability](#c-interoperability)
- [Tests and validation](#tests-and-validation)
- [Connext 7.7 modernization work](#connext-77-modernization-work)
- [Known limitations and troubleshooting](#known-limitations-and-troubleshooting)
- [License and attribution](#license-and-attribution)

## How Perftest works

A Perftest run has two roles:

- The **publisher** sends throughput samples and periodically marks a sample as
  a latency ping.
- The **subscriber** receives throughput samples and echoes latency pings on a
  separate latency topic.

The subscriber reports throughput and loss. The publisher measures the
ping-pong round-trip time and reports the estimated one-way latency. A third
announcement topic coordinates discovery, startup, and orderly shutdown.

The default mode is a throughput test: the publisher writes as quickly as the
configuration permits while collecting periodic latency samples. With
`-latencyTest`, every exchange is synchronized to measure unloaded ping-pong
latency.

Both processes must use:

- the same DDS domain;
- compatible transports and discovery settings;
- the same keyed/unkeyed and bounded/unbounded generated type; and
- compatible reliability, security, and QoS settings.

Start the subscriber before the publisher. Both applications wait for DDS
discovery before sending benchmark data.

## C# feature scope

The Connext 7.7 C# implementation supports the core generated-type Perftest
benchmark paths.

| Capability | C# 7.7 status |
| --- | --- |
| Publisher and subscriber roles | Supported |
| Throughput and ping-pong latency modes | Supported |
| Bounded and unbounded sequences | Supported |
| Keyed data, multiple instances, and content filters | Supported |
| DataAvailable listeners and WaitSet receive loops | Supported |
| Reliable and best-effort delivery | Supported |
| Batching and asynchronous publishing | Supported |
| Volatile, transient-local, transient, and persistent durability | Supported |
| UDPv4, UDPv6, shared memory, TCP, TLS, DTLS, and WAN | Supported when the required RTI components are installed |
| RTI Security and Lightweight Security options | Supported when the required RTI components and licenses are installed |
| CSV, JSON, and legacy output | Supported |
| C++ wire interoperability using the repository IDL | Supported and tested |
| DynamicData | Recognized but intentionally rejected with an unsupported-feature error |
| FlatData, Zero Copy, custom types, and raw transports | Not part of this C# port |
| Connext Micro, Connext Cert, and TSS | Not part of this C# port |

“Full functionality” in this README means full operation of the supported C#
scope above against the Connext Professional 7.7 API. It does not imply that
language-specific C++ or embedded-only features were reimplemented in C#.

## Prerequisites

Install the following before building the C# application:

1. **RTI Connext DDS Professional 7.7.0**, including host tools and the target
   libraries for the machine that will run Perftest.
2. **RTI Code Generator 4.7.x**, supplied with Connext 7.7.
3. **.NET 8 SDK**.
4. Network access to NuGet for the first package restore, unless the packages
   are already cached.
5. The appropriate RTI licenses for Connext and any optional security or
   transport plugins used by the test.

The project pins these managed dependencies:

- `Rti.ConnextDds` 7.7.0
- `System.CommandLine` 2.0.10

Confirm the .NET SDK:

```bash
dotnet --version
```

### Configure the Connext environment

The production build needs `NDDSHOME` and the Connext host tools. Prefer the
environment script supplied by the installed Connext version because it also
configures native library paths.

On the validated Apple Silicon installation, run the script from **Bash**:

```bash
/bin/bash
source /Applications/rti_connext_dds-7.7.0/resource/scripts/rtisetenv_arm64Darwin23clang16.0.bash
```

Do not source that Bash script directly from `zsh`; it uses Bash-specific
variables such as `BASH_SOURCE`. Either enter Bash as shown above or run an
individual command through `bash -lc`:

```bash
bash -lc 'source /Applications/rti_connext_dds-7.7.0/resource/scripts/rtisetenv_arm64Darwin23clang16.0.bash && dotnet build srcCs/rtiperftest.csproj --configuration Release'
```

On Linux, select the script that matches the installed target architecture:

```bash
source /opt/rti_connext_dds-7.7.0/resource/scripts/rtisetenv_<architecture>.bash
```

On Windows, run the matching script from a Command Prompt:

```bat
call C:\path\to\rti_connext_dds-7.7.0\resource\scripts\rtisetenv_<architecture>.bat
```

Verify the environment:

```bash
echo "$NDDSHOME"
"$NDDSHOME/bin/rtiddsgen" -version
```

The build rejects a Code Generator version that is not 4.7.x.

## Clone and configure

Clone this repository and enter its root directory:

```bash
git clone git@github.com:herma48852/connext-7.7-perftest-csharp.git
cd connext-7.7-perftest-csharp
```

All commands in this README assume the current directory is the repository
root. This matters for the default QoS and security file paths.

## Build

### Recommended repository build wrapper

After configuring the Connext environment, build only the C# implementation:

```bash
./build.sh --nddshome "$NDDSHOME" --cs-build
```

The wrapper creates:

```text
./bin/release/perftest_cs
```

On Windows:

```bat
build.bat --nddshome "%NDDSHOME%" --cs-build
```

The Windows launcher is:

```text
bin\release\perftest_cs.bat
```

### Direct .NET build

The project can also be restored and built directly:

```bash
dotnet restore srcCs/rtiperftest.csproj
dotnet build srcCs/rtiperftest.csproj --configuration Release --no-restore
```

The direct build produces a native launcher for the current platform and the
managed application assembly:

```text
./srcCs/bin/Release/net8.0/perftest_cs
srcCs/bin/Release/net8.0/perftest_cs.dll
```

The native launcher is the simplest way to run a direct build:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -help
```

The equivalent framework-dependent command is:

```bash
dotnet srcCs/bin/Release/net8.0/perftest_cs.dll -help
```

Or use `dotnet run`:

```bash
dotnet run --project srcCs/rtiperftest.csproj \
  --configuration Release --no-build -- -help
```

### What happens during a production build

The C# project:

1. validates that `rtiddsgen` is available and reports version 4.7.x;
2. generates C# types from `srcIdl/perftest.idl` with unbounded-sequence
   support;
3. writes generated sources under `srcCs/obj`, not into the source tree;
4. compiles the generated types and handwritten C# implementation together;
5. copies `perftest_qos_profiles.xml` and the required security artifacts into
   the output directory; and
6. treats C# compiler warnings as errors.

Code generation is incremental. Repeated builds do not duplicate generated
sources or produce duplicate-source warnings.

### Clean generated output

Use the repository wrapper:

```bash
./build.sh --clean
```

Or remove only normal .NET build output with:

```bash
dotnet clean srcCs/rtiperftest.csproj --configuration Release
```

## Quick start

Use an unused DDS domain for each independent test. The examples below use
domain `81` and explicitly select UDPv4 to provide a portable baseline.

In terminal 1, start the subscriber:

```bash
./srcCs/bin/Release/net8.0/perftest_cs \
  -sub \
  -domain 81 \
  -transport UDPv4 \
  -noPrintIntervals
```

In terminal 2, start the publisher:

```bash
./srcCs/bin/Release/net8.0/perftest_cs \
  -pub \
  -domain 81 \
  -transport UDPv4 \
  -dataLen 1024 \
  -numIter 100000 \
  -noPrintIntervals
```

Expected behavior:

1. Both processes print their Connext and Perftest versions.
2. The endpoints discover each other.
3. The subscriber announces that it is ready.
4. The publisher sends initialization pings, then benchmark data.
5. The publisher prints latency statistics.
6. The subscriber prints throughput and loss statistics.
7. The finalization handshake completes and the C# applications exit.

The commands above use the launcher produced by `dotnet build`. If the project
was built through `build.sh --cs-build`, the optional
`./bin/release/perftest_cs` wrapper accepts the same arguments. The managed DLL
can also be run explicitly:

```bash
dotnet srcCs/bin/Release/net8.0/perftest_cs.dll -sub -domain 81 -transport UDPv4
dotnet srcCs/bin/Release/net8.0/perftest_cs.dll -pub -domain 81 -transport UDPv4 -dataLen 1024 -numIter 100000
```

Display the complete option list at any time:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -help
```

Both traditional single-dash spellings, such as `-domain`, and double-dash
spellings, such as `--domain`, are accepted.

## Common benchmark scenarios

### Maximum throughput

Throughput mode is the default. Batching defaults to 8192 bytes.

Subscriber:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 82 -transport UDPv4
```

Publisher:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 82 -transport UDPv4 \
  -dataLen 1024 -batchSize 8192 -executionTime 60
```

Use `-batchSize 0` to explicitly disable batching. Batching is not used when
the data size is too large for the configured batch.

### Best-case latency

Specify `-latencyTest` on both sides. Latency mode synchronizes each ping-pong
exchange and implicitly uses a latency count of one unless overridden.

Subscriber:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 83 -transport UDPv4 \
  -latencyTest -noPrintIntervals
```

Publisher:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 83 -transport UDPv4 \
  -latencyTest -dataLen 64 -numIter 100000 -noPrintIntervals
```

Batching is incompatible with latency mode and is disabled automatically when
the batch size was not explicitly supplied.

### Rate-limited publishing

Limit the publication rate to 50,000 samples per second using a spin-based
rate controller:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 84 -transport UDPv4 \
  -dataLen 1024 -pubRate 50000:spin -executionTime 60
```

Use `:sleep` to reduce CPU use when the operating system's sleep precision is
adequate:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 84 -transport UDPv4 \
  -pubRate 1000:sleep -executionTime 60
```

### Best-effort delivery

Specify `-bestEffort` on both sides:

```bash
# Subscriber
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 85 -transport UDPv4 -bestEffort

# Publisher
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 85 -transport UDPv4 \
  -bestEffort -dataLen 1024 -numIter 100000
```

Best-effort mode may report loss under load; that is a benchmark result, not
necessarily an application error.

### WaitSet receive mode

By default, readers use `DataAvailable` callbacks. Use a WaitSet-driven reader
loop with:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 86 -transport UDPv4 \
  -useReadThread -waitsetEventCount 5 -waitsetDelayUsec 100
```

The publisher does not need `-useReadThread` unless its latency reader should
also use the WaitSet path.

### Keyed instances and content filtering

Use the same generated type on both sides. The following subscriber accepts
only keyed instances 2 through 4:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 87 -transport UDPv4 \
  -keyed -instances 8 -cft 2:4
```

The publisher writes round-robin across eight instances:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 87 -transport UDPv4 \
  -keyed -instances 8 -dataLen 1024 -numIter 100000
```

Add `-writeInstance 3` to publish only instance 3.

### Unbounded sequence type

Both sides must specify `-unbounded`:

```bash
# Subscriber
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 88 -transport UDPv4 -unbounded

# Publisher
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 88 -transport UDPv4 \
  -unbounded -unboundedSize 65536 -dataLen 32768 -numIter 10000
```

Large samples may require transport, send-window, and flow-controller tuning.

### Asynchronous publishing

```bash
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 89 -transport UDPv4 \
  -asynchronous -flowController 1Gbps \
  -dataLen 65536 -executionTime 60
```

Supported flow-controller names are `default`, `1Gbps`, and `10Gbps`.

### Multicast

Enable multicast on both sides:

```bash
# Subscriber
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 90 -transport UDPv4 -multicast

# Publisher
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 90 -transport UDPv4 \
  -multicast -dataLen 1024 -executionTime 60
```

Use `-multicastAddr` to supply one address for all topics or a comma-separated
`throughput,latency,announcement` address set.

### Multiple machines without multicast discovery

On a subscriber whose local interface is `192.168.1.154`, with the publisher
running on `192.168.1.156`:

```bash
./srcCs/bin/Release/net8.0/perftest_cs \
  -sub -domain 81 -transport UDPv4 \
  -nic 192.168.1.154 -peer 192.168.1.156
```

Run the corresponding command on the publisher at `192.168.1.156`, reversing
the local interface and peer addresses:

```bash
./srcCs/bin/Release/net8.0/perftest_cs \
  -pub -domain 81 -transport UDPv4 \
  -nic 192.168.1.156 -peer 192.168.1.154 \
  -dataLen 1024 -numIter 100000
```

`-nic` is always the local machine's interface; `-peer` identifies the remote
machine used for discovery and may be repeated. Use `-allowInterfaces` for a
more general interface allow-list. Ensure host firewalls permit Connext DDS
discovery and user traffic for the selected domain.

### Machine-readable output

CSV is the default output format. JSON and the legacy text format are also
available:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 92 -transport UDPv4 \
  -outputFormat json -noPrintIntervals
```

Use `-noOutputHeaders` when integrating CSV output with an existing pipeline.

## Important command-line options

Run `-help` for the authoritative complete list. The most commonly used
options are summarized below.

### Test role and duration

| Option | Meaning | Default |
| --- | --- | --- |
| `-pub` | Run as publisher | Off |
| `-sub` | Run as subscriber | On |
| `-domain <id>` | DDS domain identifier | `1` |
| `-dataLen <bytes>` | Serialized sample size | `100` |
| `-numIter <count>` | Number of samples to publish | `100000000` |
| `-executionTime <seconds>` | Stop after a fixed duration | `0` (disabled) |
| `-latencyTest` | Synchronous ping-pong latency mode | Off |
| `-latencyCount <count>` | Samples or batches between latency pings | `10000`; implicitly `1` in latency mode |
| `-noPrintIntervals` | Print final statistics only | Off |
| `-cpu` | Include process CPU utilization | Off |

### Reliability and writer behavior

| Option | Meaning | Default |
| --- | --- | --- |
| `-bestEffort` | Use best-effort instead of reliable delivery | Reliable |
| `-batchSize <bytes>` | Batch size; explicit `0` disables batching | `8192` |
| `-sendQueueSize <count>` | Reliable send window in samples or batches | `50` |
| `-asynchronous` | Enable asynchronous publishing | Off |
| `-flowController <name>` | `default`, `1Gbps`, or `10Gbps` | `default` |
| `-pubRate <rate>[:spin\|sleep]` | Limit samples per second | Unlimited |
| `-noPositiveAcks` | Disable positive acknowledgments | Off |
| `-keepDurationUsec <usec>` | Minimum keep duration with positive ACKs disabled | `1000` |
| `-enableAutoThrottle` | Enable DataWriter auto-throttling | Off |
| `-enableTurboMode` | Enable DataWriter turbo mode | Off |

### Data and reader configuration

| Option | Meaning | Default |
| --- | --- | --- |
| `-keyed` | Use the keyed generated type | Off |
| `-instances <count>` | Number of keyed instances | `1` |
| `-writeInstance <index>` | Write one instance; `-1` uses round-robin | `-1` |
| `-cft <index\|start:end>` | Subscriber content filter | Not set |
| `-unbounded` | Use the unbounded generated type | Off |
| `-unboundedSize <bytes>` | Unbounded-sequence allocation threshold | `0` |
| `-useReadThread` | Use a WaitSet receive loop | Off |
| `-waitsetEventCount <count>` | WaitSet event count | `5` |
| `-waitsetDelayUsec <usec>` | WaitSet batching delay | `100` |

### Multi-participant tests

| Option | Meaning | Default |
| --- | --- | --- |
| `-numSubscribers <count>` | Subscribers expected by a publisher | `1` |
| `-numPublishers <count>` | Publishers expected by a subscriber | `1` |
| `-sidMultiSubTest <id>` | Unique subscriber ID | `0` |
| `-pidMultiPubTest <id>` | Unique publisher ID | `0` |

The historical misspelling `-numSubcribers` remains accepted for compatibility.

### Output and diagnostics

| Option | Meaning | Default |
| --- | --- | --- |
| `-outputFormat <format>` | `csv`, `json`, or `legacy` | `csv` |
| `-noOutputHeaders` | Suppress output headers | Off |
| `-verbosity <0-3>` | Connext logging: silent, error, warning, or all | `1` |
| `-writerStats` | Print reliable-writer pulled-sample statistics | Off |
| `-help` | Display complete help | — |
| `-version` | Display application and Connext versions | — |

## Transports, discovery, security, and QoS

### Transport selection

Use `-transport` to make the active transport explicit:

```text
UDPv4 | UDPv6 | SHMEM | TCP | TLS | DTLS | WAN
```

Names are case-insensitive. The legacy aliases `-enableTCP`, `-enableUDPv6`,
and `-enableSharedMemory` are also recognized.

Recommended baselines:

- Use `UDPv4` for cross-process and cross-language validation.
- Use `SHMEM` when both processes run on the same host and shared-memory
  performance is the subject of the test.
- Use `TCP`, `TLS`, `DTLS`, or `WAN` only after installing and licensing the
  corresponding RTI transport plugin and configuring its required addresses,
  ports, certificates, or WAN server.

Transport-specific options include:

- `-configureTransportServerBindPort`
- `-configureTransportWan`
- `-configureTransportPublicAddress`
- `-configureTransportCertAuthority`
- `-configureTransportCertFile`
- `-configureTransportPrivateKey`
- `-configureTransportWanServerAddress`
- `-configureTransportWanServerPort`
- `-configureTransportWanId`
- `-configureTransportSecureWan`

### Discovery

By default, discovery behavior comes from `perftest_qos_profiles.xml`. Useful
overrides include:

- `-peer <address>` for an initial peer; repeat the option for multiple peers;
- `-multicast` or `-noMulticast`;
- `-multicastAddr <address-set>`; and
- `-nic` or `-allowInterfaces` to constrain network interfaces.

If processes wait forever at `Waiting to discover`, verify the domain,
transport, interface, peers, multicast routing, and firewall rules on both
sides.

### DDS Security

The build copies the repository's signed governance and permission files and
PEM credentials to the application output. A security run must use compatible
governance on both sides and role-appropriate permissions and identities.

A repository-root example using the included encrypted-data governance file:

```bash
# Subscriber
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 93 -transport UDPv4 \
  -secureGovernanceFile resource/secure/signed_PerftestGovernance_DataEncrypt.xml

# Publisher
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 93 -transport UDPv4 \
  -secureGovernanceFile resource/secure/signed_PerftestGovernance_DataEncrypt.xml \
  -dataLen 1024 -numIter 100000
```

When only governance is supplied, the implementation selects the included
publisher/subscriber permission, certificate, private-key, and CA defaults.
Override them with:

- `-securePermissionsFile`
- `-secureCertAuthority`
- `-secureCertFile`
- `-securePrivateKey`
- `-secureLibrary`
- `-secureEncryptionAlgorithm`
- `-securePSK` and `-securePSKAlgorithm`
- `-secureEnableAAD`
- `-lightWeightSecurity`

The files under `resource/secure` are test credentials. Do not reuse them as
production identity material.

### XML QoS

The default file is:

```text
perftest_qos_profiles.xml
```

The default library is:

```text
PerftestQosLibrary
```

Override them with:

```bash
./srcCs/bin/Release/net8.0/perftest_cs -sub \
  -qosFile /absolute/path/custom_perftest_qos.xml \
  -qosLibrary MyPerftestQosLibrary
```

A custom library must provide profiles compatible with the names used by the
application: `BaseProfileQos`, `ThroughputQos`, `LatencyQos`, and
`AnnouncementQos`.

## Understanding the results

The subscriber's final throughput row contains:

- **Sample Size (Bytes)** — configured serialized sample size;
- **Total Samples** — valid benchmark samples received;
- **Avg Samples/s** — average receive rate;
- **Avg Mbps** — average application data rate;
- **Lost Samples** — sequence gaps detected; and
- **Lost Samples (%)** — loss as a percentage of expected samples.

The publisher's latency row contains:

- average latency;
- standard deviation;
- minimum and maximum latency; and
- 50th, 90th, 99th, 99.99th, and 99.9999th percentiles.

The publisher sends 400 initialization pings before the measured data phase so
discovery and initial endpoint setup do not dominate the results.

For repeatable comparisons:

1. Keep the Connext version, QoS, transport, domain topology, and security
   settings identical between runs.
2. Record CPU model, operating system, network interface, MTU, and switch path.
3. Use `-executionTime` long enough to reach steady state.
4. Run several trials and compare distributions, not just one minimum value.
5. Avoid unrelated CPU, disk, and network load.
6. Pin or isolate CPUs externally when the operating system and deployment
   policy permit it.
7. Use `-noPrintIntervals` when console I/O would distort a short test.

## C++ interoperability

The C# port uses the same `srcIdl/perftest.idl`, topic names, QoS profiles,
announcement sentinels, and key representation as the C++ implementation.

Build the traditional C++ reference implementation for the target platform:

```bash
./build.sh --platform <connext-architecture> --cpp-build
```

For the validated Apple Silicon platform:

```bash
./build.sh --platform arm64Darwin23clang16.0 --cpp-build
```

### C++ publisher to C# subscriber

```bash
# C# subscriber
./srcCs/bin/Release/net8.0/perftest_cs -sub -domain 94 -transport UDPv4 \
  -noPrintIntervals

# C++ publisher
./bin/<connext-architecture>/release/perftest_cpp \
  -pub -domain 94 -transport UDPv4 \
  -dataLen 1024 -numIter 100000 -noPrintIntervals
```

### C# publisher to C++ subscriber

```bash
# C++ subscriber
./bin/<connext-architecture>/release/perftest_cpp \
  -sub -domain 95 -transport UDPv4 -noPrintIntervals

# C# publisher
./srcCs/bin/Release/net8.0/perftest_cs -pub -domain 95 -transport UDPv4 \
  -dataLen 1024 -numIter 100000 -noPrintIntervals
```

Both directions were validated with 100,000 reliable 1,024-byte samples and
zero detected loss. See the native C++ teardown note under
[Known limitations and troubleshooting](#known-limitations-and-troubleshooting).

## Tests and validation

### Command-line regression suite

The CLI tests do not create DDS entities:

```bash
dotnet test tests/Perftest.Cli.Tests/Perftest.Cli.Tests.csproj \
  --configuration Release
```

The suite verifies defaults, explicit-option tracking, latency defaults,
legacy aliases, repeated peers, secure transport overrides, help/version exit
behavior, and invalid input handling.

### Connext 7.7 API compile and core behavior suite

```bash
dotnet test \
  tests/Perftest.ConnextApi.Compile/Perftest.ConnextApi.Compile.csproj \
  --configuration Release
```

This fixture compiles all handwritten C# sources against `Rti.ConnextDds`
7.7.0 while using minimal generated-type stand-ins. Its tests cover:

- C++-compatible announcement sentinels;
- independent samples owned by cloned type helpers;
- secure-transport option propagation; and
- monotonic microsecond timing without arithmetic overflow.

It is a compile and behavior fixture, not a runnable Perftest executable.

### Production build validation

```bash
dotnet build srcCs/rtiperftest.csproj \
  --configuration Release --no-restore
```

The completed validation on Connext Professional 7.7.0 included:

- production build: **0 warnings, 0 errors**;
- immediate repeat build: **0 warnings, 0 errors**;
- CLI tests: **12 passed**;
- Connext API/core tests: **4 passed**;
- C# publisher to C# subscriber: **100,000 samples, zero loss, clean exits**;
- C++ publisher to C# subscriber: **100,000 samples, zero loss, clean exits**;
  and
- C# publisher to C++ subscriber: **100,000 samples, zero loss**, with the
  native-only teardown behavior described below.

The live baseline used UDPv4 on separate, unused DDS domains so host shared
memory settings could not hide an IDL, QoS, or wire-compatibility issue.

## Connext 7.7 modernization work

The original C# source was brought forward from its earlier, incomplete API
state to a buildable and runnable Connext Professional 7.7 implementation.
The work is summarized below.

### 1. Modern .NET and Connext project

- Added a checked-in .NET 8 SDK project at `srcCs/rtiperftest.csproj`.
- Pinned `Rti.ConnextDds` to 7.7.0.
- Pinned `System.CommandLine` to 2.0.10.
- Enabled compiler warnings as errors.
- Copied QoS and security runtime assets to build and publish output.
- Added a stable executable name, `perftest_cs`.

### 2. Connext 7.7 IDL generation

- Integrated RTI Code Generator into MSBuild.
- Required the 4.7.x generator delivered with Connext 7.7.
- Generated bounded, keyed, unbounded, and keyed-unbounded C# types from the
  repository's canonical IDL.
- Kept generated code in the MSBuild intermediate directory.
- Made generation incremental and corrected repeat-build duplicate-source
  behavior.

### 3. Complete command-line layer

- Replaced the legacy parser with an explicit `System.CommandLine` model.
- Preserved familiar Perftest single-dash and double-dash options.
- Preserved the historical `-numSubcribers` typo as an alias while adding the
  correctly spelled `-numSubscribers` form.
- Added repeated `-peer` handling.
- Distinguished implicit defaults from values explicitly supplied by the user,
  which is required for compatible batching, latency, unbounded-sequence, and
  validation behavior.
- Added deterministic exit codes and clear usage errors.

### 4. Connext DDS adapter completion

- Updated participant, publisher, subscriber, topic, reader, and writer
  creation to the modern Connext 7.7 C# APIs.
- Reused a single QoS provider and applied the expected Perftest profile
  mapping.
- Corrected duration conversion so microsecond values are preserved.
- Corrected reader resource-limit handling across multiple readers.
- Added cancellable discovery and ping waits.
- Made shutdown idempotent and safe when initialization fails partway through.
- Ensured `-noDirectCommunication` is honored for durable configurations.

### 5. Correct wire protocol and lifecycle

- Matched the C++ `INITIALIZE_SIZE` and `FINISHED_SIZE` announcement sentinel
  values.
- Tracked active subscriber IDs rather than only incrementing a counter, so
  duplicate announcements cannot corrupt lifecycle state.
- Propagated send failures instead of continuing with false success.
- Added orderly publisher/subscriber finalization and process exit codes.
- Added Ctrl+C cancellation that allows waits to terminate cleanly.

### 6. Timing, latency, and statistics fixes

- Replaced wall-clock timing with an overflow-safe monotonic microsecond clock.
- Corrected the default latency iteration behavior.
- Corrected timer use to be one-shot.
- Prevented divide-by-zero and invalid `0/0` summary output.
- Improved visibility of listener termination state across threads.
- Corrected CPU-monitor timestamp handling.

### 7. Type-helper isolation

- Made every cloned type helper own an independent generated sample.
- Prevented throughput, latency, and announcement readers/writers from
  accidentally sharing mutable generated data.
- Preserved key and payload mapping for bounded, unbounded, keyed, and unkeyed
  types.

### 8. Transport and security completion

- Made transport names case-insensitive.
- Added validation for conflicting legacy and explicit transport selections.
- Completed UDPv4, UDPv6, SHMEM, TCP, TLS, DTLS, and WAN configuration paths.
- Correctly applied repeated discovery peers and multicast settings.
- Correctly propagated secure-transport CA, identity certificate, and private
  key overrides.
- Returned configuration failures instead of silently continuing.

### 9. Build wrappers, tests, and documentation

- Updated `build.sh` and `build.bat` to build the checked-in project and let
  MSBuild own code generation.
- Added Unix and Windows launchers that use `dotnet run --no-build`.
- Updated clean behavior so it removes generated output without deleting the
  checked-in project.
- Added CLI regression tests and a Connext 7.7 compile/core test fixture.
- Added C# build, execution, compatibility, and interoperability documentation.
- Performed real C# and C++ interoperability tests against Connext 7.7.0.

## Known limitations and troubleshooting

### DynamicData, FlatData, Zero Copy, and embedded APIs

The C# port deliberately focuses on generated types in Connext Professional.
`-dynamicData` returns an explicit unsupported-feature error. FlatData, Zero
Copy, raw transport benchmarking, custom types, Connext Micro, Connext Cert,
and TSS remain in their respective native or embedded implementations.

### Shared-memory or Observability IPC permission errors

Connext shared memory and Observability can require local IPC primitives such
as System V semaphores and network `ioctl` operations. A locked-down container
or sandbox may produce errors such as `semctl(): Operation not permitted` even
when `-transport UDPv4` is selected, because an auxiliary service can create
its own participant.

Run the benchmark in a normal host terminal with the required OS permissions.
Use explicit `-transport UDPv4` to remove Perftest shared memory from the
baseline, but remember that separately enabled RTI services may still have
their own transport configuration.

### Code Generator telemetry lock messages

In a restricted environment, `rtiddsgen` may report that it cannot create a
telemetry lock file under the user's RTI configuration directory. This message
comes from the host tool rather than the C# compiler. Verify the final
`dotnet build` exit code and warning/error summary, and run the build with
normal user-directory permissions when possible.

### Waiting indefinitely for discovery

Check all of the following on both processes:

- identical `-domain` values;
- compatible `-transport` values;
- compatible security and QoS settings;
- correct `-peer`, multicast, and interface configuration;
- unique participant IDs where required by the deployment; and
- firewall access for DDS discovery and user-data ports.

### Type or QoS incompatibility

If endpoints discover but do not match, verify that both sides agree on:

- `-keyed`;
- `-unbounded`;
- reliability;
- durability;
- security governance; and
- the QoS library/profile definitions.

Use `-verbosity 2` or `-verbosity 3` to expose Connext compatibility errors.

### Native C++ subscriber teardown on the validated macOS release build

During reverse interoperability testing, the optimized traditional C++
subscriber received all 100,000 samples, printed the correct zero-loss final
summary, and completed the announcement exchange, but could remain alive during
native entity teardown. The same behavior was reproduced with an unmodified
C++ publisher and C++ subscriber, isolating it from the C# wire protocol.

The C# subscriber and publisher exit cleanly in C#-only testing, and the C#
subscriber exits cleanly when driven by the C++ publisher.

### Security file paths

Default security paths are relative to the working directory. Run from the
repository root or pass absolute paths for governance, permissions,
certificates, keys, and CA files.

## Additional documentation

The Sphinx source under `srcDoc` contains the original comprehensive Perftest
documentation plus the Connext 7.7 C# additions:

- [`srcDoc/csharp_7_7.rst`](srcDoc/csharp_7_7.rst)
- [`srcDoc/compilation.rst`](srcDoc/compilation.rst)
- [`srcDoc/execution.rst`](srcDoc/execution.rst)
- [`srcDoc/command_line_parameters.rst`](srcDoc/command_line_parameters.rst)
- [`srcDoc/examples.rst`](srcDoc/examples.rst)
- [`srcDoc/compatibility.rst`](srcDoc/compatibility.rst)

## Repository layout

```text
.
├── build.sh / build.bat            Build wrappers
├── perftest_qos_profiles.xml       Default QoS library and profiles
├── resource/secure                 Test security credentials and policies
├── srcCs                           Connext 7.7 C# implementation
│   ├── ConnextDDS                  DDS adapter and type helpers
│   ├── Harness                     Throughput, latency, and announcements
│   ├── Infrastructure              CLI, parameters, transport, output, timing
│   ├── Interface                   Messaging abstractions
│   └── rtiperftest.csproj          .NET 8 production project
├── srcIdl                          Canonical Perftest IDL
├── srcCpp / srcCpp11               Native interoperability implementations
├── srcDoc                          Full documentation source
└── tests
    ├── Perftest.Cli.Tests
    └── Perftest.ConnextApi.Compile
```

## License and attribution

The original RTI Perftest code and this derivative are distributed under the
Eclipse Public License. See [`LICENSE.md`](LICENSE.md).

Copyright notices and attribution from Real-Time Innovations are retained in
the source. RTI, Connext, and RTI Connext DDS are trademarks of Real-Time
Innovations, Inc. This independently maintained repository does not imply RTI
endorsement or support.
