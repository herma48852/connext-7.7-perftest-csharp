/*
 * (c) 2005-2021 Copyright, Real-Time Innovations, Inc. All rights reserved.
 * Subject to Eclipse Public License v1.0; see LICENSE.md for details.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Timers;
using System.Runtime.CompilerServices;

namespace PerformanceTest
{
    public class Perftest : IDisposable
    {
        public Parameters parameters;
        private ulong dataSize = 100;
        private ulong numIter = 100000000;
        private ulong spinLoopCount = 0;
        private ulong sleepNanosec = 0;
        private int latencyCount = -1;
        private int numSubscribers = 1;
        private IMessaging messagingImpl;
        private bool latencyTest = false;
        private bool isReliable = true;
        private ulong pubRate = 0;
        private bool pubRateMethodSpin = true;
        private ulong executionTime = 0;
        private bool displayWriterStats;
        private System.Timers.Timer timer;
        private PerftestPrinter printer;
        private static int subID;
        private static int pubID;
        private static bool printIntervals = true;
        private static bool showCpu;
        private static volatile bool testCompleted;
        internal static bool TestCompleted => testCompleted;
        public readonly TimeSpan timeoutWaitForAckTimeSpan = new TimeSpan(0, 0, 0, 0, 10);
        public static readonly PerftestVersion version = new PerftestVersion(4, 3, 0, 0);

        /*
         * PERFTEST-108
         * If we are performing a latency test, the default number for _NumIter
         * will be 10 times smaller than the default when performing a
         * throughput test. This will allow Perftest to work better in embedded
         * platforms since the _NumIter parameter sets the size of certain
         * arrays in the latency test mode.
         */
        public const ulong numIterDefaultLatencyTest = 10000000;

        public string[] messagingArgv = null;
        public int messagingArgc = 0;

        // Number of bytes sent in messages besides user data

        // Flag used to indicate message is used for initialization only
        public const int INITIALIZE_SIZE = 1234;
        // Flag used to indicate end of test
        public const int FINISHED_SIZE = 1235;

        /*
         * Value used to compare against to check if the latency_min has
         * been reset.
         */
        public const uint LATENCY_RESET_VALUE = uint.MaxValue;

        public const uint CDR_ENCAPSULATION_HEADER_SIZE = 4;

        public static ulong OVERHEAD_BYTES { get; set; } = 28;

#if !PERFTEST_TEST_BUILD
        public static int Main(string[] argv)
        {
            using Perftest app = new Perftest();
            ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                testCompleted = true;
            };

            Console.CancelKeyPress += cancelHandler;
            try
            {
                return app.Run(argv);
            }
            finally
            {
                Console.CancelKeyPress -= cancelHandler;
            }
        }
#endif

        private int Run(string[] argv)
        {
            testCompleted = false;
            PrintVersion();

            try
            {
                if (!ParseConfig(argv))
                {
                    return parseRequestedExit ? parseExitCode : 2;
                }

                ulong maxPerftestSampleSize = Math.Max(dataSize, FINISHED_SIZE);

                if (parameters.UnboundedSizeSet)
                {
                    if (parameters.Keyed)
                    {
                        messagingImpl = new RTIDDSImpl<TestDataKeyedLarge_t>(
                                new DataTypeKeyedLargeHelper(maxPerftestSampleSize));
                    }
                    else
                    {
                        messagingImpl = new RTIDDSImpl<TestDataLarge_t>(
                                new DataTypeLargeHelper(maxPerftestSampleSize));
                    }
                }
                else
                {
                    if (parameters.Keyed)
                    {
                        messagingImpl = new RTIDDSImpl<TestDataKeyed_t>(
                                new DataTypeKeyedHelper(maxPerftestSampleSize));
                    }
                    else
                    {
                        messagingImpl = new RTIDDSImpl<TestData_t>(
                                new DataTypeHelper(maxPerftestSampleSize));
                    }
                }

                if (!messagingImpl.Initialize(parameters))
                {
                    return 1;
                }

                printer = new PerftestPrinter(parameters);

                PrintConfiguration();

                if (parameters.Pub)
                {
                    return Publisher() ? 0 : 1;
                }

                return Subscriber() ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Perftest failed: " + ex.Message);
                if (parameters?.Verbosity >= 3)
                {
                    Console.Error.WriteLine(ex);
                }
                return 1;
            }
        }

        public void Dispose()
        {
            timer?.Stop();
            timer?.Dispose();
            timer = null;

            if (messagingImpl != null)
            {
                messagingImpl.Dispose();
                Console.Error.WriteLine("Test ended.");
                Console.Error.Flush();
            }
            GC.SuppressFinalize(this);
        }

        /*********************************************************
         * ParseParameters
         */
        private bool parseRequestedExit;
        private int parseExitCode;

        private Parameters ParseParameters(string[] args)
        {
            CliParseOutcome outcome = CliParser.Parse(args);
            parseRequestedExit = !outcome.ShouldRun;
            parseExitCode = outcome.ExitCode;
            return outcome.Parameters;
        }

        /*********************************************************
        * ParseConfig
        */
        private bool ParseConfig(string[] argv)
        {
            parameters = ParseParameters(argv);

            if (parseRequestedExit)
            {
                return false;
            }

            if (parameters.PubSet && parameters.SubSet && parameters.Pub && parameters.Sub)
            {
                Console.Error.WriteLine("Specify either -pub or -sub, not both.");
                return false;
            }
            if (parameters.DynamicData)
            {
                Console.Error.WriteLine(
                    "-dynamicData is not supported by the Connext 7.7 C# core port; use generated types.");
                return false;
            }
            if (parameters.NumSubscribers == 0 || parameters.NumPublishers == 0)
            {
                Console.Error.WriteLine("-numSubscribers and -numPublishers must be greater than zero.");
                return false;
            }
            if (parameters.SidMultiSubTest < 0 || parameters.PidMultiPubTest < 0)
            {
                Console.Error.WriteLine(
                    "-sidMultiSubTest and -pidMultiPubTest must be zero or greater.");
                return false;
            }
            if (parameters.NumSubscribers > int.MaxValue
                || parameters.NumPublishers > int.MaxValue)
            {
                Console.Error.WriteLine(
                    "-numSubscribers and -numPublishers must not exceed 2147483647.");
                return false;
            }
            if (parameters.LatencyCount == 0 || parameters.LatencyCount > int.MaxValue)
            {
                Console.Error.WriteLine("-latencyCount must be between 1 and 2147483647.");
                return false;
            }
            if (parameters.SendQueueSize == 0 || parameters.SendQueueSize > int.MaxValue)
            {
                Console.Error.WriteLine("-sendQueueSize must be between 1 and 2147483647.");
                return false;
            }
            if (parameters.InstancesSet && parameters.Instances >= int.MaxValue)
            {
                Console.Error.WriteLine("-instances must be less than 2147483647.");
                return false;
            }
            const long maxKeepDurationUsec = 365L * 24 * 60 * 60 * 1_000_000;
            if (parameters.KeepDurationUsec < 0
                || parameters.KeepDurationUsec > maxKeepDurationUsec)
            {
                Console.Error.WriteLine(
                    "-keepDurationUsec must be between 0 and 31536000000000.");
                return false;
            }
            if (parameters.Domain < 0 || parameters.Domain > 232)
            {
                Console.Error.WriteLine("-domain must be between 0 and 232.");
                return false;
            }
            if (parameters.WaitsetEventCount == 0 || parameters.WaitsetEventCount > int.MaxValue)
            {
                Console.Error.WriteLine("-waitsetEventCount must be between 1 and 2147483647.");
                return false;
            }

            messagingArgv = new String[argv.Length];

            /*
             * PERFTEST-108
             * We add this boolean value to check if we are explicitly changing
             * the number of iterations via command-line paramenter. This will
             * only be used if this is a latency test to decrease or not the
             * default number of iterations.
             */
            if (!parameters.Pub)
            {
                parameters.Sub = true;
                subID = parameters.SidMultiSubTest;
            }
            else
            {
                pubID = parameters.PidMultiPubTest;
            }

            if (parameters.NumIterSet)
            {
                if (parameters.NumIter == 0)
                {
                    Console.Error.Write("-numIter must be > 0\n");
                    return false;
                }
                numIter = parameters.NumIter;
            }

            if (parameters.DataLenSet)
            {
                if (parameters.DataLen < Perftest.OVERHEAD_BYTES)
                {
                    Console.Error.WriteLine("dataLen must be >= " + Perftest.OVERHEAD_BYTES);
                    return false;
                }
                else if (parameters.DataLen > Perftest.GetMaxPerftestSampleSize())
                {
                    Console.Error.WriteLine("dataLen must be <= " + Perftest.GetMaxPerftestSampleSize());
                    return false;
                }
                if (parameters.UnboundedSize == 0 && (int)parameters.DataLen > MAX_BOUNDED_SEQ_SIZE.Value) {
                    parameters.UnboundedSize = Math.Min(
                            (ulong)MAX_BOUNDED_SEQ_SIZE.Value,
                            2 * parameters.DataLen);
                    parameters.UnboundedSizeSet = true;
                }
            }
            else
            {
                parameters.DataLen = 100;
            }

            dataSize = parameters.DataLen;

            if (parameters.UnboundedSizeSet)
            {
                if (parameters.UnboundedSize < Perftest.OVERHEAD_BYTES)
                {
                    Console.Error.WriteLine(
                            "unboundedSize must be >= "
                            + Perftest.OVERHEAD_BYTES
                            + " and is "
                            + parameters.UnboundedSize);
                    return false;
                }
                if (parameters.UnboundedSize > (ulong)MAX_PERFTEST_SAMPLE_SIZE.Value)
                {
                    Console.Error.WriteLine(
                            "unboundedSize must be <= " +
                            MAX_PERFTEST_SAMPLE_SIZE.Value
                            + " and is "
                            + parameters.UnboundedSize);
                    return false;
                }
            }

            if (parameters.Unbounded && !parameters.UnboundedSizeSet)
            {
                parameters.UnboundedSize = 2 * parameters.DataLen;
                parameters.UnboundedSizeSet = true;
            }

            sleepNanosec = (ulong)parameters.Sleep * 1_000_000;
            latencyCount = (int)parameters.LatencyCount;
            numSubscribers = (int)parameters.NumSubscribers;
            printIntervals = !parameters.NoPrintIntervals;
            latencyTest = parameters.LatencyTest;
            isReliable = !parameters.BestEffort;
            displayWriterStats = parameters.WriterStats;

            if(parameters.InstancesSet)
            {
                if (parameters.Instances == 0)
                {
                    Console.Error.Write("instance count cannot be negative or null\n");
                    return false;
                }
            }

            try
            {
                if (!"csv".Equals(parameters.OutputFormat) && !"json".Equals(parameters.OutputFormat)
                    && !"legacy".Equals(parameters.OutputFormat))
                {
                    Console.Error.Write("<format> for outputFormat '" +
                            parameters.OutputFormat + "' is not valid. It must be" +
                            "'csv', 'json' or 'legacy'.\n");
                    return false;
                }
            }
            catch (ArgumentNullException)
            {
                Console.Error.Write("Bad <format>. It must be 'csv'" +
                        ", 'json' or 'legacy'\n");
                return false;
            }

            if (parameters.PubRateSet)
            {
                if (parameters.PubRate.Contains(":"))
                {
                    try
                    {
                        String[] st = parameters.PubRate.Split(':');
                        if (st.Length != 2)
                        {
                            Console.Error.WriteLine(
                                "-pubRate must use the form <samples/s>[:spin|sleep].");
                            return false;
                        }
                        if (!ulong.TryParse(st[0], out pubRate))
                        {
                            Console.Error.Write("Bad number for -pubRate\n");
                            return false;
                        }
                        if ("sleep".Equals(st[1]))
                        {
                            pubRateMethodSpin = false;
                        }
                        else if (!"spin".Equals(st[1]))
                        {
                            Console.Error.Write("<method> for pubRate '" + st[1]
                                    + "' is not valid. It must be 'spin' or 'sleep'.\n");
                            return false;
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        Console.Error.Write("Bad pubRate\n");
                        return false;
                    }
                }
                else
                {
                    if (!ulong.TryParse(parameters.PubRate, out pubRate))
                    {
                        Console.Error.Write("Bad number for -pubRate\n");
                        return false;
                    }
                }

                if (pubRate > 10000000)
                {
                    Console.Error.Write("-pubRate cannot be greater than 10000000.\n");
                    return false;
                }
            }

            executionTime = parameters.ExecutionTime;
            showCpu = parameters.Cpu;

            if (latencyTest)
            {
                if (pubID != 0)
                {
                    Console.Error.Write("Only the publisher with ID = 0 can run the latency test\n");
                    return false;
                }

                // With latency test, latency should be 1
                if (latencyCount == -1)
                {
                    latencyCount = 1;
                }

                /*
                 * PERFTEST-108
                 * If we are in a latency test, the default value for _NumIter
                 * has to be smaller (to avoid certain issues in platforms with
                 * low memory). Therefore, unless we explicitly changed the
                 * _NumIter value we will use a smaller default:
                 * "numIterDefaultLatencyTest"
                 */
                if (!parameters.NumIterSet)
                {
                    numIter = numIterDefaultLatencyTest;
                }
            }

            if (latencyCount == -1)
            {
                latencyCount = 10000;
            }

            if ((numIter > 0) && (numIter < (ulong)latencyCount))
            {
                Console.Error.Write("numIter ({0}) must be greater than latencyCount ({1}).\n",
                              numIter, latencyCount);
                return false;
            }

            //manage the parameter: -pubRate -sleep -spin
            if (parameters.Pub && pubRate > 0)
            {
                if (spinLoopCount > 0)
                {
                    Console.Error.Write("'-spin' is not compatible with -pubRate. " +
                        "Spin/Sleep value will be set by -pubRate.");
                    spinLoopCount = 0;
                }
                if (sleepNanosec > 0)
                {
                    Console.Error.Write("'-sleep' is not compatible with -pubRate. " +
                        "Spin/Sleep value will be set by -pubRate.");
                    sleepNanosec = 0;
                }
            }
            return true;
        }

        private void PrintConfiguration()
        {
            StringBuilder sb = new StringBuilder();

            // Throughput/Latency mode
            if (parameters.Pub)
            {
                sb.Append("\nMode: ");

                if (latencyTest)
                {
                    sb.Append("LATENCY TEST (Ping-Pong test)\n");
                }
                else
                {
                    sb.Append("THROUGHPUT TEST\n");
                    sb.Append("      (Use \"-latencyTest\" for Latency Mode)\n");
                }
            }

            sb.Append("\nPerftest Configuration:\n");

            // Reliable/Best Effort
            sb.Append("\tReliability: ");
            if (isReliable)
            {
                sb.Append("Reliable\n");
            }
            else
            {
                sb.Append("Best Effort\n");
            }

            // Keyed/Unkeyed
            sb.Append("\tKeyed: ");
            if (parameters.Keyed)
            {
                sb.Append("Yes\n");
            }
            else
            {
                sb.Append("No\n");
            }

            // Publisher/Subscriber and Entity ID
            if (parameters.Pub)
            {
                sb.Append("\tPublisher ID: ");
                sb.Append(pubID);
                sb.Append('\n');
            }
            else
            {
                sb.Append("\tSubscriber ID: ");
                sb.Append(subID);
                sb.Append('\n');
            }

            if (parameters.Pub)
            {
                sb.Append("\tLatency count: 1 latency sample every ");
                sb.Append(latencyCount);
                sb.Append('\n');

                // Data Sizes
                sb.Append("\tData Size: ");
                sb.Append(dataSize);
                sb.Append('\n');

                // Batching
                int batchSize = messagingImpl.BatchSize;

                sb.Append("\tBatching: ");
                if (batchSize > 0)
                {
                    sb.Append(batchSize);
                    sb.Append(" Bytes (Use \"-batchSize 0\" to disable batching)\n");
                }
                else if (batchSize == 0)
                {
                    sb.Append("No (Use \"-batchSize\" to setup batching)\n");
                }
                else
                { // < 0 (Meaning, Disabled by RTI Perftest)
                    sb.Append("Disabled by RTI Perftest.\n");
                    if (batchSize == -1)
                    {
                        if (latencyTest)
                        {
                            sb.Append("\t\t  BatchSize disabled for a Latency Test\n");
                        }
                        else
                        {
                            sb.Append("\t\t  BatchSize is smaller than 2 times\n");
                            sb.Append("\t\t  the minimum sample size.\n");
                        }
                    }
                    if (batchSize == -2)
                    {
                        sb.Append("\t\t  BatchSize cannot be used with\n");
                        sb.Append("\t\t  Large Data.\n");
                    }
                    if (batchSize == -3)
                    {
                        sb.Append("\t\t  BatchSize disabled by default.\n");
                        sb.Append("\t\t  when using FlatData.\n");
                    }
                    if (batchSize == -4)
                    {
                        sb.Append("\t\t  BatchSize cannot be combined with\n");
                        sb.Append("\t\t  an explicitly configured -pubRate.\n");
                    }
                }

                // Publication Rate
                sb.Append("\tPublication Rate: ");
                if (pubRate > 0)
                {
                    sb.Append(pubRate);
                    sb.Append(" Samples/s (");
                    if (pubRateMethodSpin)
                    {
                        sb.Append("Spin)\n");
                    }
                    else
                    {
                        sb.Append("Sleep)\n");
                    }
                }
                else
                {
                    sb.Append("Unlimited (Not set)\n");
                }

                // Execution Time or Num Iter
                if (executionTime > 0)
                {
                    sb.Append("\tExecution time: ");
                    sb.Append(executionTime);
                    sb.Append(" seconds\n");
                }
                else
                {
                    sb.Append("\tNumber of samples: ");
                    sb.Append(numIter);
                    sb.Append('\n');
                }
            }

            // Listener/WaitSets
            sb.Append("\tReceive using: ");
            if (parameters.UseReadThread)
            {
                sb.Append("WaitSets\n");
            }
            else
            {
                sb.Append("Listeners\n");
            }

            sb.Append(messagingImpl.PrintConfiguration());

            Console.Error.WriteLine(sb.ToString());
        }

        private bool Subscriber()
        {
            ThroughputListener readerListener = null;
            IMessagingReader reader;
            IMessagingWriter writer;
            IMessagingWriter announcementWriter;

            // create latency pong writer
            writer = messagingImpl.CreateWriter(LATENCY_TOPIC_NAME.Value);

            if (writer == null)
            {
                Console.Error.Write("Problem creating latency writer.\n");
                return false;
            }

            // Check if using callbacks or read thread
            if (!parameters.UseReadThread)
            {
                // create latency pong reader
                readerListener = new ThroughputListener(writer, printer, parameters);
                reader = messagingImpl.CreateReader(THROUGHPUT_TOPIC_NAME.Value, readerListener);
                if (reader == null)
                {
                    Console.Error.Write("Problem creating throughput reader.\n");
                    return false;
                }
            }
            else
            {
                reader = messagingImpl.CreateReader(THROUGHPUT_TOPIC_NAME.Value, null);
                if (reader == null)
                {
                    Console.Error.Write("Problem creating throughput reader.\n");
                    return false;
                }
                readerListener = new ThroughputListener(writer, reader, printer, parameters);
                Task.Run(() => readerListener.ReadThread());
            }

            // Create announcement writer
            announcementWriter =
                    messagingImpl.CreateWriter(ANNOUNCEMENT_TOPIC_NAME.Value);

            if (announcementWriter == null)
            {
                Console.Error.Write("Problem creating announcement writer.\n");
                return false;
            }

            // Synchronize with publishers
            Console.Error.Write("Waiting to discover {0} publishers ...\n", parameters.NumPublishers);
            if (!reader.WaitForWriters((int)parameters.NumPublishers))
            {
                return false;
            }
            // In a multi publisher test, only the first publisher will have a reader.
            if (!writer.WaitForReaders(1)
                || !announcementWriter.WaitForReaders((int)parameters.NumPublishers))
            {
                return false;
            }

            // Send announcement message
            TestMessage message = new TestMessage();
            message.entityId = subID;
            message.Size = INITIALIZE_SIZE;
            if (!announcementWriter.Send(message, false))
            {
                Console.Error.WriteLine("Unable to send the subscriber announcement.");
                return false;
            }
            announcementWriter.Flush();

            Console.Error.Write("Waiting for data ...\n");

            printer.PrintInitialOutput();

            // wait for data
            ulong now, prevTime, delta;
            ulong prevCount = 0;
            ulong prevBytes = 0;
            ulong aveCount = 0;
            int lastDataLength = -1;
            ulong mps, bps;
            double mpsAve = 0.0, bpsAve = 0.0;
            ulong msgSent, bytes, lastMsgs, lastBytes;
            double missingPacketsPercent = 0;

            now = GetTimeUsec();
            while (!testCompleted)
            {
                prevTime = now;
                Thread.Sleep(1000);
                now = GetTimeUsec();

                if (readerListener.endTest)
                {
                    TestMessage messageEndTest = new TestMessage();
                    messageEndTest.entityId = subID;
                    messageEndTest.Size = FINISHED_SIZE;
                    if (!announcementWriter.Send(messageEndTest, false))
                    {
                        Console.Error.WriteLine("Unable to send the final subscriber announcement.");
                        return false;
                    }
                    announcementWriter.Flush();
                    break;
                }

                double outputCpu = 0.0;
                if (readerListener.packetsReceived > 0 && showCpu)
                {
                    outputCpu = readerListener.cpu.GetCpuInstant();
                }

                if (printIntervals)
                {
                    if (lastDataLength != readerListener.lastDataLength)
                    {
                        lastDataLength = readerListener.lastDataLength;
                        prevCount = readerListener.packetsReceived;
                        prevBytes = readerListener.bytesReceived;
                        bpsAve = 0;
                        mpsAve = 0;
                        aveCount = 0;
                        continue;
                    }

                    lastMsgs = readerListener.packetsReceived;
                    lastBytes = readerListener.bytesReceived;
                    msgSent = lastMsgs - prevCount;
                    bytes = lastBytes - prevBytes;
                    prevCount = lastMsgs;
                    prevBytes = lastBytes;
                    delta = now - prevTime;
                    mps = msgSent * 1000000 / delta;
                    bps = bytes * 1000000 / delta;

                    // calculations of overall average of mps and bps
                    ++aveCount;
                    bpsAve += (double)(bps - bpsAve) / (double)aveCount;
                    mpsAve += (double)(mps - mpsAve) / (double)aveCount;

                    // Calculations of missing package percent
                    if (lastMsgs + readerListener.missingPackets == 0)
                    {
                        missingPacketsPercent = 0.0;
                    }
                    else
                    {
                        missingPacketsPercent =
                                readerListener.missingPackets
                                / (float)(lastMsgs
                                    + readerListener.missingPackets);
                    }

                    if (lastMsgs > 0)
                    {
                        printer.PrintThroughputInterval(
                            lastMsgs,
                            mps,
                            mpsAve,
                            bps,
                            bpsAve,
                            readerListener.missingPackets,
                            missingPacketsPercent,
                            outputCpu);
                    }
                }
            }

            printer.PrintFinalOutput();
            Thread.Sleep(1000);
            Console.Error.Write("Finishing test...\n");
            Console.Out.Flush();
            return true;
        }

        private bool Publisher()
        {
            // create throughput/ping writer
            IMessagingWriter throughputWriter = messagingImpl.CreateWriter(THROUGHPUT_TOPIC_NAME.Value);
            if (throughputWriter == null)
            {
                Console.Error.Write("Problem creating throughput writer.\n");
                return false;
            }

            int samplesPerBatch = GetSamplesPerBatch();
            uint numLatency = (uint)(numIter / (ulong)samplesPerBatch / (ulong)latencyCount);

            if (numLatency / (ulong)samplesPerBatch % (ulong)latencyCount > 0)
            {
                numLatency++;
            }

            // in batch mode, we might have to send another ping
            if (samplesPerBatch > 1)
            {
                ++numLatency;
            }

            LatencyListener readerListener = null;
            IMessagingReader reader;

            // Only publisher with ID 0 will send/receive pings
            if (pubID == 0)
            {
                // Check if using callbacks or read thread
                if (!parameters.UseReadThread)
                {
                    // create latency pong reader
                    readerListener = new LatencyListener(
                                latencyTest ? throughputWriter : null,
                                parameters,
                                printer,
                                numLatency);

                    reader = messagingImpl.CreateReader(
                            LATENCY_TOPIC_NAME.Value,
                            readerListener);

                    if (reader == null)
                    {
                        Console.Error.Write("Problem creating latency reader.\n");
                        return false;
                    }
                }
                else
                {
                    reader = messagingImpl.CreateReader(LATENCY_TOPIC_NAME.Value, null);
                    if (reader == null)
                    {
                        Console.Error.Write("Problem creating latency reader.\n");
                        return false;
                    }
                    readerListener = new LatencyListener(
                                reader,
                                latencyTest ? throughputWriter : null,
                                parameters,
                                printer,
                                numLatency);
                    Task.Run(() => readerListener.ReadThread());
                }
            }
            else
            {
                reader = null;
            }
            /* Create Announcement reader
             * A Subscriber will send a message on this channel once it discovers
             * every Publisher
             */
            AnnouncementListener announcementReaderListener = new AnnouncementListener();
            IMessagingReader announcementReader = messagingImpl.CreateReader(
                    ANNOUNCEMENT_TOPIC_NAME.Value,
                    announcementReaderListener);
            if (announcementReader == null)
            {
                Console.Error.Write("Problem creating announcement reader.\n");
                return false;
            }
            ulong spinsPerUsec = 0;
            const ulong sleepUsec = 1000;
            if (pubRate > 0)
            {
                if (pubRateMethodSpin)
                {
                    spinsPerUsec = GetSpinsPerMicrosecond();
                    /* A return value of 0 means accuracy not assured */
                    if (spinsPerUsec == 0)
                    {
                        Console.Error.Write(
                            "Error initializing spin per microsecond. "
                            + "-pubRate cannot be used\n"
                            + "Exiting.\n");
                        return false;
                    }
                    spinLoopCount = 1000000 * spinsPerUsec / pubRate;
                }
                else
                {
                    sleepNanosec = 1000000000 / pubRate;
                }
            }

            Console.Error.WriteLine($"Waiting to discover {numSubscribers} subscribers ...");
            if (!throughputWriter.WaitForReaders(numSubscribers))
            {
                return false;
            }
            // Only publisher with ID 0 will have a reader.
            if ((reader != null && !reader.WaitForWriters(numSubscribers))
                || !announcementReader.WaitForWriters(numSubscribers))
            {
                return false;
            }

            // We have to wait until every Subscriber sends an announcement message
            // indicating that it has discovered every Publisher
            Console.Error.Write("Waiting for subscribers announcement ...\n");
            while (numSubscribers > announcementReaderListener.ActiveSubscriberCount
                && !testCompleted)
            {
                Thread.Sleep(1000);
            }
            if (testCompleted)
            {
                return false;
            }

            // Allocate data and set size
            TestMessage message = new TestMessage();
            message.entityId = pubID;
            message.Size = INITIALIZE_SIZE;

            /*
             * Initial burst of data:
             *
             * The purpose of this initial burst of Data is to ensure that most
             * memory allocations in the critical path are done before the test
             * begins, for both the Writer and the Reader that receives the samples.
             * It will also serve to make sure that all the instances are registered
             * in advance in the subscriber application.
             *
             * We query the MessagingImplementation class to get the suggested sample
             * count that we should send. This number might be based on the reliability
             * protocol implemented by the middleware behind. Then we choose between that
             * number and the number of instances to be sent.
             */

            int initializeSampleCount = Math.Max(
                   messagingImpl.InitialBurstSampleCount,
                   (int)parameters.Instances);

            Console.Error.WriteLine(
                    "Sending " + initializeSampleCount + " initialization pings ...");

            for (int i = 0; i < initializeSampleCount; i++)
            {
                // Send test initialization message
                if (!throughputWriter.Send(message, true))
                {
                    Console.Error.WriteLine("Unable to send an initialization sample.");
                    return false;
                }
            }
            throughputWriter.Flush();

            Console.Error.WriteLine("Publishing data ...");

            printer.PrintInitialOutput();

            // Set data size, account for other bytes in message
            message.Size = (int)(dataSize - OVERHEAD_BYTES);

            // Sleep 1 second, then begin test
            Thread.Sleep(1000);

            int numPings = 0;
            int pingID = -1;
            int currentIndexInBatch = 0;
            int pingIndexInBatch = 0;
            bool sentPing = false;

            ulong timeNow = 0, timeLastCheck = 0, timeDelta = 0;
            ulong pubRateSamplePeriod = 1;
            ulong rate = 0;

            timeLastCheck = GetTimeUsec();

            /* Minimum value for pubRate_sample_period will be 1 so we execute 100 times
               the control loop every second, or every sample if we want to send less
               than 100 samples per second */
            if (pubRate > 100)
            {
                pubRateSamplePeriod = pubRate / 100;
            }

            if (executionTime > 0)
            {
                SetTimeout(executionTime);
            }

            // Main sending loop
            for (ulong loop = 0; loop < numIter && !testCompleted; ++loop)
            {
                if ((pubRate > 0)
                        && (loop > 0)
                        && (loop % pubRateSamplePeriod == 0))
                {
                    timeNow = GetTimeUsec();

                    timeDelta = timeNow - timeLastCheck;
                    timeLastCheck = timeNow;
                    // rate is the amount of loops that have to be executed in the next second to achieve pubRate
                    if (timeDelta == 0)
                    {
                        continue;
                    }
                    rate = pubRateSamplePeriod * 1000000 / timeDelta;

                    if (pubRateMethodSpin)
                    {
                        if (rate > pubRate)
                        {
                            spinLoopCount += spinsPerUsec;
                        }
                        else if (rate < pubRate && spinLoopCount > spinsPerUsec)
                        {
                            spinLoopCount -= spinsPerUsec;
                        }
                        else if (rate < pubRate && spinLoopCount <= spinsPerUsec)
                        {
                            spinLoopCount = 0;
                        }
                    }
                    else
                    {
                        if (rate > pubRate)
                        {
                            sleepNanosec += sleepUsec; //plus 1 MicroSec
                        }
                        else if (rate < pubRate && sleepNanosec > sleepUsec)
                        {
                            sleepNanosec -= sleepUsec; //less 1 MicroSec
                        }
                        else if (rate < pubRate && sleepNanosec <= sleepUsec)
                        {
                            sleepNanosec = 0;
                        }
                    }
                }

                if (spinLoopCount > 0)
                {
                    Spin();
                }

                if (sleepNanosec > 0)
                {
                    double sleepMilliseconds = sleepNanosec / 1_000_000.0;
                    Thread.Sleep(TimeSpan.FromMilliseconds(
                        Math.Min(sleepMilliseconds, int.MaxValue)));
                }

                pingID = -1;

                // only send latency pings if is publisher with ID 0
                // In batch mode, latency pings are sent once every LatencyCount batches
                if ((pubID == 0) && ((loop / (ulong)samplesPerBatch % (ulong)latencyCount) == 0))
                {
                    /* In batch mode only send a single ping in a batch.
                     *
                     * However, the ping is sent in a round robin position within
                     * the batch.  So keep track of which position(index) the
                     * current sample is within the batch, and which position
                     * within the batch should contain the ping. Only send the ping
                     * when both are equal.
                     *
                     * Note when not in batch mode, current_index_in_batch = ping_index_in_batch
                     * always.  And the if() is always true.
                     */
                    if (currentIndexInBatch == pingIndexInBatch && !sentPing)
                    {
                        // Each time ask a different subscriber to echo back
                        pingID = numPings % numSubscribers;
                        ulong now = GetTimeUsec();
                        message.timestampSec = (int)((now >> 32) & 0xFFFFFFFF);
                        message.timestampUsec = (uint)(now & 0xFFFFFFFF);

                        ++numPings;
                        pingIndexInBatch =
                                (pingIndexInBatch + 1) % samplesPerBatch;
                        sentPing = true;

                        if (displayWriterStats && printIntervals)
                        {
                            Console.WriteLine(
                                "Pulled samples: {0,7}",
                                throughputWriter.GetPulledSampleCount());
                        }
                    }
                }

                currentIndexInBatch = (currentIndexInBatch + 1) % samplesPerBatch;

                message.seqNum = (uint)loop;
                message.latencyPing = pingID;
                if (!throughputWriter.Send(message, false))
                {
                    Console.Error.WriteLine("Unable to send a throughput sample.");
                    return false;
                }
                if (latencyTest && sentPing)
                {
                    if (isReliable)
                    {
                        if (!throughputWriter.WaitForPingResponse())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        /* time out in milliseconds */
                        throughputWriter.WaitForPingResponse(TimeSpan.FromMilliseconds(200));
                    }
                }

                // come to the beginning of another batch
                if (currentIndexInBatch == 0)
                {
                    sentPing = false;
                }
            }

            // In case of batching, flush
            throughputWriter.Flush();

            // Test has finished, send end of test message, send multiple
            // times in case of best effort
            // message.size = FINISHED_SIZE;
            message.Size = FINISHED_SIZE;
            int j = 0;
            const int announcementSampleCount = 50;
            while (announcementReaderListener.ActiveSubscriberCount > 0
                    && j < announcementSampleCount)
            {
                if (!throughputWriter.Send(message, true))
                {
                    Console.Error.WriteLine("Unable to send a finalization sample.");
                    return false;
                }
                throughputWriter.Flush();
                try
                {
                    throughputWriter.WaitForAck(timeoutWaitForAckTimeSpan);
                }catch (System.TimeoutException){}
                j++;
            }
            if (pubID == 0)
            {
                readerListener.PrintSummaryLatency(true);
                readerListener.EndTest = true;
            }
            else
            {
                Console.Error.WriteLine("Latency results are only shown when -pidMultiPubTest = 0");
            }

            if (displayWriterStats)
            {
                Console.Error.WriteLine("Pulled samples: {0,7}", throughputWriter.GetPulledSampleCount());
            }

            printer.PrintFinalOutput();
            Console.Error.WriteLine("Finishing test...");
            Console.Out.Flush();
            return true;
        }

        public static ulong GetTimeUsec()
        {
            long ticks = System.Diagnostics.Stopwatch.GetTimestamp();
            long frequency = System.Diagnostics.Stopwatch.Frequency;
            long wholeSeconds = ticks / frequency;
            long remainingTicks = ticks % frequency;
            return (ulong)(wholeSeconds * 1_000_000
                + remainingTicks * 1_000_000 / frequency);
        }

        private static void Timeout(object source, ElapsedEventArgs e)
        {
            testCompleted = true;
        }

        private void SetTimeout(ulong executionTime)
        {
            if (timer == null)
            {
                timer = new System.Timers.Timer();
                timer.Elapsed += Timeout;
                timer.AutoReset = false;
                Console.Error.WriteLine($"Setting timeout to {executionTime} seconds.");
                timer.Interval = executionTime * 1000;
                timer.Enabled = true;
            }
        }

        public int GetSamplesPerBatch()
        {
            int batchSize = messagingImpl.BatchSize;
            int samplesPerBatch;

            if (batchSize > 0)
            {
                samplesPerBatch = batchSize / (int)dataSize;
                if (samplesPerBatch == 0)
                {
                    samplesPerBatch = 1;
                }
            }
            else
            {
                samplesPerBatch = 1;
            }

            return samplesPerBatch;
        }

        public static void PrintVersion()
        {
            PerftestVersion perftestV = version;
            Rti.Config.ProductVersion ddsV = Rti.Dds.Core.ServiceEnvironment.Instance.Version;

            if (perftestV.version.Major == 0
                    && perftestV.version.Minor == 0
                    && perftestV.version.Build == 0)
            {
                Console.Error.Write("RTI Perftest Develop");
            }
            else if (perftestV.version.Major == 9
                    && perftestV.version.Minor == 9
                    && perftestV.version.Build == 9)
            {
                Console.Error.Write("RTI Perftest Master");
            }
            else
            {
                Console.Error.Write(
                        "RTI Perftest "
                        + perftestV.version.Major + "."
                        + perftestV.version.Minor + "."
                        + perftestV.version.Build);
                if (perftestV.version.Revision != 0)
                {
                    Console.Error.Write("." + perftestV.version.Revision);
                }
            }
            Console.Error.WriteLine("\n" + ddsV);
        }

        static public ulong GetMaxPerftestSampleSize()
        {
            return (ulong)MAX_PERFTEST_SAMPLE_SIZE.Value;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static ulong GetSpinsPerMicrosecond()
        {
            ulong spins = 0;
            ulong startTime = GetTimeUsec();
            ulong diff;
            // Start counting how many spins can be made within 1 usec
            do
            {
                double a, b, c;
                a = 1.1;
                b = 3.1415;
                c = a / b * spins;
                spins++;
                diff = GetTimeUsec() - startTime;
            }
            while (diff < 100);

            return spins / 100;
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private void Spin()
        {
            for (ulong i = 0; i < spinLoopCount; i++)
            {
                double a, b, c;
                a = 1.1;
                b = 3.1415;
                c = a / b * i;
            }
        }
    }
} // PerformanceTest Namespace
