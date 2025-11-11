using System;
using System.Threading;

namespace NsqSharp.Utils
{
    /// <summary>
    /// A WaitGroup waits for a collection of routines to finish. The main routine calls Add to set the number of routines to
    /// wait for. Then each of the routines runs and calls Done when finished. At the same time, Wait can be used to block until
    /// all routines have finished. See: http://golang.org/pkg/sync/#WaitGroup
    /// </summary>
    public class WaitGroup
    {
        private readonly ResetEventAsyncImplementation _wait = new();

        /// <summary>
        /// Add adds delta, which may be negative, to the WaitGroup counter. If the counter becomes zero, all goroutines blocked
        /// on Wait are released. If the counter goes negative, Add panics.
        ///
        /// Note that calls with a positive delta that occur when the counter is zero must happen before a Wait. Calls with a
        /// negative delta, or calls with a positive delta that start when the counter is greater than zero, may happen at any
        /// time. Typically this means the calls to Add should execute before the statement creating the routine or other event
        /// to be waited for.
        /// </summary>
        /// <param name="delta"></param>
        public void Add(int delta) => _wait.Add(delta);
      
        /// <summary>
        /// Done decrements the WaitGroup counter.
        /// </summary>
        public void Done() => _wait.Add(-1);

        /// <summary>
        /// Wait blocks until the WaitGroup counter is zero.
        /// </summary>
        public void Wait() => _wait.WaitAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        public Task WaitAsync() => _wait.WaitAsync();


        private class ResetEventAsyncImplementation
        {
            private TaskCompletionSource<bool> _tcs = new();

            private int _state = 0;

            public void Add(int delta)
            {
                if (delta == 0) // no change
                    return;
                lock (this)
                {
                    if (_state == 0)
                    {
                        if (delta > 0)
                        {
                            Clear();
                            _state = delta;
                        }
                    }
                    else if (_state + delta <= 0)
                    {
                        Set();
                        _state = 0;
                    }
                    else
                    {
                        _state += delta;
                    }
                }
            }

            public void Set()
            {
                _tcs.TrySetResult(true);
            }

            private void Clear()
            {
                _tcs = new TaskCompletionSource<bool>();
                _state = 0;
            }

            public void Reset()
            {
                lock (this)
                    Clear();
            }

            public async Task WaitAsync()
            {
                await _tcs.Task;
            }
        }
    }
}
