/*
 * Compile-only stand-ins for the types produced from srcIdl/perftest.idl.
 * They let CI check the hand-written adapter against Rti.ConnextDds 7.7.0
 * without requiring a licensed rtiddsgen installation on the test host.
 * Production builds always use rtiddsgen 4.7.x through srcCs/rtiperftest.csproj.
 */

namespace PerformanceTest
{
    public static class MAX_BOUNDED_SEQ_SIZE { public const int Value = 65_470; }
    public static class MAX_PERFTEST_SAMPLE_SIZE { public const int Value = 2_147_482_620; }
    public static class MAX_CFT_VALUE { public const int Value = 65_535; }
    public static class KEY_SIZE { public const int Value = 4; }
    public static class DEFAULT_THROUGHPUT_BATCH_SIZE { public const uint Value = 8_192; }
    public static class THROUGHPUT_TOPIC_NAME { public const string Value = "Throughput"; }
    public static class LATENCY_TOPIC_NAME { public const string Value = "Latency"; }
    public static class ANNOUNCEMENT_TOPIC_NAME { public const string Value = "Announcement"; }

    public abstract class GeneratedSampleStub<T> : IEquatable<T>
        where T : class
    {
        public byte[] key = new byte[KEY_SIZE.Value];
        public int entity_id;
        public uint seq_num;
        public int timestamp_sec;
        public uint timestamp_usec;
        public int latency_ping;
        public Omg.Types.ISequence<byte> bin_data = new Rti.Types.Sequence<byte>();

        public bool Equals(T other) => ReferenceEquals(this, other);
    }

    public sealed class TestData_t : GeneratedSampleStub<TestData_t> { }
    public sealed class TestDataKeyed_t : GeneratedSampleStub<TestDataKeyed_t> { }
    public sealed class TestDataLarge_t : GeneratedSampleStub<TestDataLarge_t> { }
    public sealed class TestDataKeyedLarge_t : GeneratedSampleStub<TestDataKeyedLarge_t> { }

    public sealed class CompileOnlySerializer<T> : IDisposable
    {
        public long GetSerializedSampleSize(T sample) => 32;
        public void Dispose() { }
    }

    public sealed class TestData_tSupport
    {
        public static TestData_tSupport Instance { get; } = new TestData_tSupport();
        public CompileOnlySerializer<TestData_t> CreateSerializer() => new CompileOnlySerializer<TestData_t>();
    }

    public sealed class TestDataKeyed_tSupport
    {
        public static TestDataKeyed_tSupport Instance { get; } = new TestDataKeyed_tSupport();
        public CompileOnlySerializer<TestDataKeyed_t> CreateSerializer() => new CompileOnlySerializer<TestDataKeyed_t>();
    }

    public sealed class TestDataLarge_tSupport
    {
        public static TestDataLarge_tSupport Instance { get; } = new TestDataLarge_tSupport();
        public CompileOnlySerializer<TestDataLarge_t> CreateSerializer() => new CompileOnlySerializer<TestDataLarge_t>();
    }

    public sealed class TestDataKeyedLarge_tSupport
    {
        public static TestDataKeyedLarge_tSupport Instance { get; } = new TestDataKeyedLarge_tSupport();
        public CompileOnlySerializer<TestDataKeyedLarge_t> CreateSerializer() => new CompileOnlySerializer<TestDataKeyedLarge_t>();
    }
}
