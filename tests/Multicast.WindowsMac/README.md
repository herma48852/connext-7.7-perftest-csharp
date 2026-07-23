# Windows 11 to Apple Silicon multicast performance test

This manual test measures multicast throughput and synchronous ping-pong
latency between the Windows 11 laptop and Mac mini over the dedicated gigabit
Ethernet switch. The throughput runs use 32 KiB (32,768-byte) samples with
batching explicitly disabled.

## Fixed test topology

| Machine | Operating system | Test interface |
| --- | --- | --- |
| Laptop | Windows 11 | `192.168.2.10/24` |
| Mac mini | macOS / Apple Silicon | `192.168.2.20/24` |

Both interfaces must connect to the same gigabit switch. Wi-Fi addresses are
not part of this test. Every command pins Connext to the dedicated Ethernet
interface with `-nic` and disables runtime interface-change tracking with
`-disableInterfaceTracking`. This is the default and recommended way to run
this manually provisioned, fixed-interface test while leaving Wi-Fi enabled.

> **Warning:** Do not remove `-disableInterfaceTracking` from these commands
> while Wi-Fi is enabled. If the flag is removed, disable Wi-Fi on both endpoints
> before running the test; otherwise Wi-Fi interface events can trigger Connext
> locator-update errors and interrupt multicast communication.

The test uses unicast initial peers for deterministic discovery and these
explicit multicast groups for benchmark traffic:

| Topic | Multicast group |
| --- | --- |
| Throughput | `239.255.2.1` |
| Latency | `239.255.2.2` |
| Announcement | `239.255.2.3` |

Using `-peer` isolates discovery from the measurement. The throughput,
latency, and announcement topics still use multicast because every endpoint
also specifies `-multicast` and `-multicastAddr`.

## 1. Verify the Ethernet link

From Windows:

```bat
ping 192.168.2.20
```

From the Mac mini:

```bash
ping -c 4 192.168.2.10
```

Do not start performance measurements until both commands report replies with
zero packet loss.

## 2. Prepare each runtime terminal

Before every Windows publisher or subscriber command, open a Command Prompt,
load the RTI runtime environment, and change to the cloned repository root:

```bat
call "C:\Program Files\rti_connext_dds-7.7.0\resource\scripts\rtisetenv_x64Win64VS2017.bat"
```

Then change directory to the root of the cloned repository.

Before every Mac publisher or subscriber command, open Bash, load the RTI
runtime environment, and change to the cloned repository root:

```bash
/bin/bash
source /Applications/rti_connext_dds-7.7.0/resource/scripts/rtisetenv_arm64Darwin23clang16.0.bash
```

Then change directory to the root of the cloned repository.

Both repositories must be built before running the test. Confirm each runtime
with `bin\release\perftest_cs.bat -help` on Windows and
`./bin/release/perftest_cs -help` on the Mac.

## 3. Throughput: Windows publisher to Mac subscriber

Start the Mac subscriber first:

```bash
./bin/release/perftest_cs \
  -sub -domain 101 -transport UDPv4 \
  -nic 192.168.2.20 -peer 192.168.2.10 \
  -disableInterfaceTracking \
  -multicast -multicastAddr 239.255.2.1,239.255.2.2,239.255.2.3 \
  -batchSize 0 \
  -noPrintIntervals -cpu -outputFormat json
```

Then start the Windows publisher:

```bat
bin\release\perftest_cs.bat ^
  -pub -domain 101 -transport UDPv4 ^
  -nic 192.168.2.10 -peer 192.168.2.20 ^
  -disableInterfaceTracking ^
  -multicast -multicastAddr "239.255.2.1,239.255.2.2,239.255.2.3" ^
  -dataLen 32768 -batchSize 0 -executionTime 60 ^
  -noPrintIntervals -cpu -outputFormat json
```

Record the publisher latency summary and the subscriber throughput, loss, and
CPU summary.

## 4. Throughput: Mac publisher to Windows subscriber

Start the Windows subscriber first:

```bat
bin\release\perftest_cs.bat ^
  -sub -domain 102 -transport UDPv4 ^
  -nic 192.168.2.10 -peer 192.168.2.20 ^
  -disableInterfaceTracking ^
  -multicast -multicastAddr "239.255.2.1,239.255.2.2,239.255.2.3" ^
  -batchSize 0 ^
  -noPrintIntervals -cpu -outputFormat json
```

Then start the Mac publisher:

```bash
./bin/release/perftest_cs \
  -pub -domain 102 -transport UDPv4 \
  -nic 192.168.2.20 -peer 192.168.2.10 \
  -disableInterfaceTracking \
  -multicast -multicastAddr 239.255.2.1,239.255.2.2,239.255.2.3 \
  -dataLen 32768 -batchSize 0 -executionTime 60 \
  -noPrintIntervals -cpu -outputFormat json
```

Record the same final statistics and compare them with the first direction.

## 5. Latency: Windows publisher to Mac subscriber

Latency is reported by the publisher. Start the Mac subscriber first:

```bash
./bin/release/perftest_cs \
  -sub -domain 103 -transport UDPv4 \
  -nic 192.168.2.20 -peer 192.168.2.10 \
  -disableInterfaceTracking \
  -multicast -multicastAddr 239.255.2.1,239.255.2.2,239.255.2.3 \
  -latencyTest -batchSize 0 -noPrintIntervals -cpu -outputFormat json
```

Then start the Windows publisher:

```bat
bin\release\perftest_cs.bat ^
  -pub -domain 103 -transport UDPv4 ^
  -nic 192.168.2.10 -peer 192.168.2.20 ^
  -disableInterfaceTracking ^
  -multicast -multicastAddr "239.255.2.1,239.255.2.2,239.255.2.3" ^
  -latencyTest -batchSize 0 -dataLen 64 -numIter 10000 ^
  -noPrintIntervals -cpu -outputFormat json
```

Record the Windows publisher's minimum, average, median, percentile, and maximum
latency values.

## 6. Latency: Mac publisher to Windows subscriber

Start the Windows subscriber first:

```bat
bin\release\perftest_cs.bat ^
  -sub -domain 104 -transport UDPv4 ^
  -nic 192.168.2.10 -peer 192.168.2.20 ^
  -disableInterfaceTracking ^
  -multicast -multicastAddr "239.255.2.1,239.255.2.2,239.255.2.3" ^
  -latencyTest -batchSize 0 -noPrintIntervals -cpu -outputFormat json
```

Then start the Mac publisher:

```bash
./bin/release/perftest_cs \
  -pub -domain 104 -transport UDPv4 \
  -nic 192.168.2.20 -peer 192.168.2.10 \
  -disableInterfaceTracking \
  -multicast -multicastAddr 239.255.2.1,239.255.2.2,239.255.2.3 \
  -latencyTest -batchSize 0 -dataLen 64 -numIter 10000 \
  -noPrintIntervals -cpu -outputFormat json
```

Record the Mac publisher's latency distribution and compare it with the reverse
direction.

## 7. Acceptance criteria

The initial baseline passes when:

1. every endpoint reports the intended local NIC, multicast enabled, and
   `Interface Tracking: Disabled`;
2. discovery completes without waiting indefinitely;
3. both 60-second, 32 KiB, unbatched throughput runs finish and report their
   final statistics;
4. reliable throughput reports zero lost samples;
5. both 10,000-sample latency runs finish and report nonzero latency
   distributions; and
6. no endpoint reports a transport, QoS, or finalization error.

This first run establishes the performance baseline; it intentionally does not
impose a throughput or latency threshold. Set regression thresholds only after
several clean runs establish normal variation for this hardware and switch.

## 8. Confirm that benchmark traffic is multicast

During a run, capture the three destination groups on the Mac interface used to
reach Windows:

```bash
sudo tcpdump -ni "$(route -n get 192.168.2.10 | awk '/interface:/{print $2}')" 'dst host 239.255.2.1 or dst host 239.255.2.2 or dst host 239.255.2.3'
```

Seeing packets addressed to those groups confirms that benchmark traffic is
multicast. If discovery succeeds but no multicast packets arrive, check switch
IGMP snooping, host firewalls, interface selection, and the multicast address
arguments on both endpoints.
