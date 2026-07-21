.. _section-csharp-7-7:

C# Port for Connext Professional 7.7
====================================

The C# implementation is a cross-platform .NET 8 application using the modern
C# API in ``Rti.ConnextDds`` 7.7.0. Its checked-in project pins all managed
package versions. During every production build, the project invokes *RTI Code
Generator* 4.7.x on ``srcIdl/perftest.idl`` and compiles the generated sources
from the MSBuild intermediate directory. Generated code is not written into the
source tree.

Supported scope
---------------

The port supports the core generated-type benchmark paths:

- publisher and subscriber roles, throughput and ping-pong latency modes;
- keyed and unkeyed bounded or unbounded sequence types;
- listener and WaitSet receive modes;
- reliable and best-effort communication, batching, asynchronous publishing,
  content filters, multiple instances, durability, and XML QoS profiles;
- repeated discovery peers, multicast, UDPv4, UDPv6, shared memory, TCP, TLS,
  DTLS, WAN, and the existing security configuration switches; and
- CSV, JSON, and legacy result formats.

DynamicData, FlatData, Zero Copy, custom types, raw transports, Connext Micro,
Connext Cert, and TSS are outside this C# port. ``-dynamicData`` is recognized
and returns a clear unsupported-feature error; options that belong only to the
other implementations are not accepted by the C# command line.

Build and run
-------------

Install the .NET 8 SDK and *Connext Professional* 7.7, then set ``NDDSHOME`` to
the installation directory. The build verifies that the selected
``rtiddsgen`` is version 4.7.x.

.. code-block:: console

    export NDDSHOME=/path/to/rti_connext_dds-7.7.0
    dotnet build srcCs/rtiperftest.csproj --configuration Release

The repository build wrapper is equivalent:

.. code-block:: console

    ./build.sh --nddshome "$NDDSHOME" --cs-build

Run the application through the generated ``bin/release/perftest_cs`` wrapper,
or directly with ``dotnet``:

.. code-block:: console

    dotnet run --project srcCs/rtiperftest.csproj --no-build \
        --configuration Release -- -sub -domain 81 -transport UDPv4

Validation
----------

The command-line regression suite does not require a Connext installation:

.. code-block:: console

    dotnet test tests/Perftest.Cli.Tests/Perftest.Cli.Tests.csproj \
        --configuration Release

A compile-only fixture checks every hand-written C# source file against the
7.7.0 API. It uses minimal generated-type stand-ins and is not a runnable
Perftest binary:

.. code-block:: console

    dotnet build \
        tests/Perftest.ConnextApi.Compile/Perftest.ConnextApi.Compile.csproj \
        --configuration Release

Run that fixture with ``dotnet test`` to also exercise the announcement
sentinels, type-helper isolation, monotonic clock, and secure-transport option
mapping.

After the production project builds, validate a C# pair on an unused domain.
Start the subscriber first, then the publisher in a second terminal:

.. code-block:: console

    bin/release/perftest_cs -sub -domain 81 -transport UDPv4
    bin/release/perftest_cs -pub -domain 81 -transport UDPv4 \
        -dataLen 1024 -numIter 100000

For wire interoperability, repeat the test in both directions with a C++
binary produced from the same IDL and QoS file:

.. code-block:: console

    # C# subscriber, C++ publisher
    bin/release/perftest_cs -sub -domain 82 -transport UDPv4
    bin/<architecture>/release/perftest_cpp -pub -domain 82 \
        -transport UDPv4 -dataLen 1024 -numIter 100000

    # C++ subscriber, C# publisher
    bin/<architecture>/release/perftest_cpp -sub -domain 83 -transport UDPv4
    bin/release/perftest_cs -pub -domain 83 -transport UDPv4 \
        -dataLen 1024 -numIter 100000

A successful run discovers its peer, publishes interval or final statistics,
receives the finalization sample, and exits with status zero. Use UDPv4 for this
baseline so host-specific shared-memory configuration cannot mask a wire-format
or QoS problem.
