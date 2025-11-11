using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using NsqSharp.Utils;
using NsqSharp.Utils.Channels;
using NUnit.Framework;

namespace NsqSharp.Tests.Utils
{
    [TestFixture]
    public class TickerTest
    {
        // NOTE: the default timer resolution on Windows is 15.6 ms
        private readonly TimeSpan AcceptableError = TimeSpan.FromMilliseconds(15.6);

        [Test]
        public void TestSingleTicker()
        {
            // arrange
            var start = DateTime.Now.ToUniversalTime();
            var ticker = new Ticker(TimeSpan.FromSeconds(1));

            // act
            var sentAt = ticker.C.ReadAsync().AsTask().Result;
            var duration = DateTime.Now.ToUniversalTime() - start;
            var offBy = DateTime.Now.ToUniversalTime() - sentAt;

            ticker.Stop();

            // assert
            Assert.GreaterOrEqual(duration, TimeSpan.FromSeconds(1) - AcceptableError, "duration");
            Assert.Less(duration, TimeSpan.FromSeconds(1.5), "duration");
            Assert.Less(offBy, TimeSpan.FromSeconds(0.5), "offBy");
        }

        [Test]
        public void TestDoubleTicker()
        {
            // arrange
            var start = DateTime.Now.ToUniversalTime();
            var ticker = new Ticker(TimeSpan.FromSeconds(1));

            // act
            
            var sentAt1 = ticker.C.ReadAsync().AsTask().Result;
            var duration1 = DateTime.Now.ToUniversalTime() - start;
            var offBy1 = DateTime.Now.ToUniversalTime() - sentAt1;

            var sentAt2 = ticker.C.ReadAsync().AsTask().Result;
            var duration2 = DateTime.Now.ToUniversalTime() - start;
            var offBy2 = DateTime.Now.ToUniversalTime() - sentAt2;

            ticker.Stop();

            // assert
            Assert.GreaterOrEqual(duration1, TimeSpan.FromSeconds(1) - AcceptableError, "duration1");
            Assert.Less(duration1, TimeSpan.FromSeconds(1.5), "duration1");
            Assert.Less(offBy1, TimeSpan.FromSeconds(0.5), "offBy1");

            Assert.GreaterOrEqual(duration2, TimeSpan.FromSeconds(2) - AcceptableError, "duration2");
            Assert.Less(duration2, TimeSpan.FromSeconds(2.5), "duration2");
            Assert.Less(offBy2, TimeSpan.FromSeconds(0.5), "offBy2");
        }

        [Test]
        public void TestDoubleTickerWithStop()
        {
            // arrange
            var start = DateTime.Now.ToUniversalTime();
            var ticker = new Ticker(TimeSpan.FromSeconds(1));

            // act
            var sentAt1 = ticker.C.ReadAsync().AsTask().Result;
            var duration1 = DateTime.Now.ToUniversalTime() - start;
            var offBy1 = DateTime.Now.ToUniversalTime() - sentAt1;

            ticker.Stop();

            var newTicker = new Ticker(TimeSpan.FromSeconds(5));
            bool? ok2 = null;
            Select
                .CaseReceive(ticker.C, null)
                .CaseReceive(newTicker.C, null)
                .ExecuteAsync().Wait();

            newTicker.Stop();

            // assert
            Assert.GreaterOrEqual(duration1, TimeSpan.FromSeconds(1) - AcceptableError, "duration1");
            Assert.Less(duration1, TimeSpan.FromSeconds(1.5), "duration1");
            Assert.Less(offBy1, TimeSpan.FromSeconds(0.5), "offBy1");
        }

        [Test]
        public void TestTickerLoopWithExitChan()
        {
            var start = DateTime.UtcNow;
            var ticker = new Ticker(TimeSpan.FromSeconds(1));

            var listOfTimes = new List<TimeSpan>();
            var exitChan = Channel.CreateUnbounded<bool>();
            var lookupdRecheckChan = Channel.CreateUnbounded<bool>();
            bool doLoop = true;
            var select = Select
                        .CaseReceive(ticker.C, o => listOfTimes.Add(DateTime.UtcNow - start))
                        .CaseReceive(lookupdRecheckChan, o => listOfTimes.Add(DateTime.UtcNow - start))
                        .CaseReceive(exitChan, o => doLoop = false);
            {
                // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
                while (doLoop)
                {
                    select.ExecuteAsync().Wait();
                    if (listOfTimes.Count >= 10)
                    {
                        GoFunc.Run(() => exitChan.Writer.TryWrite(true), "exit notifier");
                    }
                }
            }

            ticker.Stop();

            var duration = DateTime.UtcNow - start;

            Console.WriteLine("Duration: {0}", duration);
            foreach (var time in listOfTimes)
            {
                Console.WriteLine("Tick: {0}", time);
            }

            Assert.AreEqual(10, listOfTimes.Count, "listOfTimes.Count");
            Assert.GreaterOrEqual(duration, TimeSpan.FromSeconds(10) - AcceptableError, "duration");
            Assert.Less(duration, TimeSpan.FromSeconds(11));
        }

       

        
    }
}
