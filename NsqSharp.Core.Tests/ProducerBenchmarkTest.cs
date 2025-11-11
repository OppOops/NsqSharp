using System;
using System.Diagnostics;
using System.Threading.Channels;
using NsqSharp.Api;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests
{
#if !RUN_INTEGRATION_TESTS
    [TestFixture(IgnoreReason = "NSQD Integration Test")]
#else
    [TestFixture]
#endif
    public class ProducerBenchmarkTest
    {
        private static readonly NsqdHttpClient _nsqdHttpClient;
        private static readonly NsqLookupdHttpClient _nsqLookupdHttpClient;

        static ProducerBenchmarkTest()
        {
            _nsqdHttpClient = new NsqdHttpClient("127.0.0.1:4151", TimeSpan.FromSeconds(5));
            _nsqLookupdHttpClient = new NsqLookupdHttpClient("127.0.0.1:4161", TimeSpan.FromSeconds(5));
        }

        [Test]
        public async Task BenchmarkTcp1()
        {
            await BenchmarkTcp(1);
        }

        [Test]
        public async Task BenchmarkTcp2()
        {
            await BenchmarkTcp(2);
        }

        [Test]
        public async Task BenchmarkTcp4()
        {
            await BenchmarkTcp(4);
        }

        [Test]
        public async Task BenchmarkTcp8()
        {
            await BenchmarkTcp(8);
        }

        [Test]
        public async Task BenchmarkHttp1()
        {
            await BenchmarkHttp(1);
        }

        [Test]
        public async Task BenchmarkHttp2()
        {
            await BenchmarkHttp(2);
        }

        [Test]
        public async Task BenchmarkHttp4()
        {
            await BenchmarkHttp(4);
        }

        [Test]
        public async Task BenchmarkHttp8()
        {
            await BenchmarkHttp(8);
        }

        private async Task BenchmarkTcp(int parallel)
        {
            string topicName = "test_benchmark_" + DateTime.Now.UnixNano();

            try
            {
                const int benchmarkNum = 10000;

                byte[] body = new byte[512];

                var p = new Producer("127.0.0.1:4150");
                await p.ConnectAsync();

                var startCh = Channel.CreateUnbounded<bool>();
                var wg = new WaitGroup();

                for (int j = 0; j < parallel; j++)
                {
                    wg.Add(1);
                    _ = Task.Run(async () =>
                    {
                        _ = await startCh.Reader.WaitToReadAsync();
                        for (int i = 0; i < benchmarkNum / parallel; i++)
                        {
                            p.Publish(topicName, body, true);
                        }
                        wg.Done();
                    });
                }

                var stopwatch = Stopwatch.StartNew();
                startCh.Writer.TryComplete();

                var done = Channel.CreateUnbounded<bool>();
                GoFunc.Run(() => { wg.Wait(); done.Writer.TryWrite(true); }, "waiter and done sender");

                bool finished = false;
                await Select
                    .CaseReceive(done, b => finished = b)
                    .CaseReceive(mockNSQD.After(TimeSpan.FromSeconds(10)), b => finished = false)
                    .ExecuteAsync();

                stopwatch.Stop();

                if (!finished)
                {
                    Assert.Fail("timeout");
                }

                Console.WriteLine(string.Format("{0:#,0} sent in {1:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s; Threads: {3}",
                    benchmarkNum, stopwatch.Elapsed, benchmarkNum / stopwatch.Elapsed.TotalSeconds, parallel));

                p.Stop();
            }
            finally
            {
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }

        private async Task BenchmarkHttp(int parallel)
        {
            string topicName = "test_benchmark_" + DateTime.Now.UnixNano();

            try
            {
                const int benchmarkNum = 10000;

                byte[] body = new byte[512];

                var startCh = Channel.CreateUnbounded<bool>();
                var wg = new WaitGroup();

                for (int j = 0; j < parallel; j++)
                {
                    wg.Add(1);
                    _ = Task.Run(async() =>
                    {
                        _ = await startCh.Reader.WaitToReadAsync();
                        for (int i = 0; i < benchmarkNum / parallel; i++)
                        {
                            _nsqdHttpClient.Publish(topicName, body);
                        }
                        wg.Done();
                    });
                }

                var stopwatch = Stopwatch.StartNew();
                startCh.Writer.TryComplete();
                await wg.WaitAsync();
                stopwatch.Stop();

                Console.WriteLine(string.Format("{0:#,0} sent in {1:mm\\:ss\\.fff}; Avg: {2:#,0} msgs/s; Threads: {3}",
                    benchmarkNum, stopwatch.Elapsed, benchmarkNum / stopwatch.Elapsed.TotalSeconds, parallel));
            }
            finally
            {
                _nsqdHttpClient.DeleteTopic(topicName);
                _nsqLookupdHttpClient.DeleteTopic(topicName);
            }
        }
    }
}
