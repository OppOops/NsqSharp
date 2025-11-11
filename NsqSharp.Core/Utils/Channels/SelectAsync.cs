using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace NsqSharp.Utils.Channels
{
    public class SelectAsyncCase
    {

        private int _index = 0;
        private readonly List<IConsumer> readers = new();
        private Action? _default;

        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.Zero;

        public SelectAsyncCase CaseReceive<T>(Channel<T> c, Action<T>? func = null)
            where T : notnull
        {
            return CaseReceive(c.Reader, func);
        }

        public SelectAsyncCase CaseReceive<T>(Channel<T> c, Func<T, CancellationToken, Task>? func = null)
            where T : notnull
        {
            return CaseReceive(c.Reader, func);
        }

        public SelectAsyncCase CaseReceive<T>(ChannelReader<T> reader, Action<T>? func = null)
            where T : notnull
        {
            var idx = _index++;
            func ??= (t) => { };
            this.readers.Add(new GeneralChannelReader<T>(reader, (v) => func((T)v)));
            return this;
        }

        public SelectAsyncCase CaseReceive<T>(ChannelReader<T> c, Func<T, CancellationToken, Task>? func = null)
            where T : notnull
        {
            var idx = _index++;
            func ??= (t, ct) => Task.CompletedTask;
            this.readers.Add(new GeneralChannelReader<T>(c,
                (v, ct) => func((T)v, ct)));
            return this;
        }

        public void Default(Action func)
        {
            _default = func;
            TryExecute();
        }

        public void TryExecute()
        {
            var reader = readers.FirstOrDefault(x => x.TryReadConsume(this.DefaultTimeout));
            if (reader != null)
                return;
            _default?.Invoke();
        }

        public async Task<bool> TryExecuteAsync(bool invokeDefault = true, CancellationToken token = default)
        {
            Task? task = readers
                .Select(x => x.TryReadConsumeAsync(token))
                .FirstOrDefault(x=>x!=null);
            if (task == null)
            {
                if(invokeDefault)
                    _default?.Invoke();
                return false;
            }
            await task;
            return true;
        }

        private async Task<bool> TryExecuteNoDefaultAsync(CancellationToken token = default)
        {
            Task? task = readers
                .Select(x => x.TryReadConsumeAsync(token))
                .FirstOrDefault(x => x != null);
            if(task != null)
            {
                await task;
                return true;
            }
            return false;
        }

        public async Task ExecuteAsync(CancellationToken token = default)
        {
            // quick pick
            if(await TryExecuteNoDefaultAsync(token))
                return;

            // wait until one is ready
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            try
            {
                await Task.WhenAny(readers.Select(x => x.WaitToReadAsync(cts.Token).AsTask()));
                if (token.IsCancellationRequested)
                    return;
            }
            finally
            {
                cts.Cancel(); // cancel other waits
            }

            await TryExecuteNoDefaultAsync(token);
        }

        private interface IConsumer
        {
            Task Completion { get; }

            bool CanCount { get; }

            bool CanPeek { get; }

            int Count { get; }

            bool TryReadConsume(TimeSpan timeout);

            Task? TryReadConsumeAsync(CancellationToken token = default);

            ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default);
        }

        private class GeneralChannelReader<T> : IConsumer
            where T : notnull
        {
            public ChannelReader<T> Reader { get; }

            public Task Completion => Reader.Completion;

            public bool CanCount => Reader.CanCount;

            public bool CanPeek => Reader.CanPeek;

            public int Count => Reader.Count;

            private readonly Action<object>? Invoker;
            private readonly Func<object, CancellationToken, Task>? AsyncInvoker;
            private readonly bool IsSyncInvoke;

            public GeneralChannelReader(ChannelReader<T> reader, 
                Action<object> syncInvoke)
            {
                Reader = reader;
                Invoker = syncInvoke;
                IsSyncInvoke = true;
            }

            public GeneralChannelReader(ChannelReader<T> reader,
                Func<object, CancellationToken, Task> asyncInvoker)
            {
                Reader = reader;
                AsyncInvoker = asyncInvoker;
                IsSyncInvoke = false;
            }

            public bool TryReadConsume(TimeSpan defaultTimeout)
            {
                var res = Reader.TryRead(out var itemT);
                var item = (object)itemT!;
                if (IsSyncInvoke)
                {
                    Invoker?.Invoke(item);
                }
                else if(defaultTimeout > TimeSpan.Zero)
                {
                    var cts = new CancellationTokenSource(defaultTimeout);
                    AsyncInvoker?.Invoke(item, cts.Token).Wait();
                }
                else
                {
                    AsyncInvoker?.Invoke(item, CancellationToken.None).Wait();
                }
                return res;
            }

            public Task? TryReadConsumeAsync(CancellationToken token = default)
            {
                var hasIncomingMessage = Reader.TryRead(out var itemT);
                if(!hasIncomingMessage)
                    return null;

                var item = (object)itemT!;
                if (IsSyncInvoke)
                {
                    Invoker?.Invoke(item);
                    return Task.CompletedTask;
                }
                else
                {
                    return AsyncInvoker?.Invoke(item, token);
                }
            }

            public ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
            {
                return Reader.WaitToReadAsync(cancellationToken);
            }   
        }
    }


}
