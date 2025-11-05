using System.Threading.Channels;

namespace NsqSharp.Utils.Channels
{
    public static class Select
    {
        /// <summary>
        /// Creates a case for receiving from the specific channel.
        /// </summary>
        /// <param name="c">The channel to receive from. Can be <c>null</c>.</param>
        /// <param name="func">The function to execute with the data received from the channel. Can be <c>null</c></param>
        /// <returns>An instance to append another Case, Default, or NoDefault. Select must end with a call to 
        /// Default or NoDefault.</returns>
        public static SelectAsyncCase CaseReceive<T>(Channel<T> c, 
            Action<T>? func = null)
            where T : notnull
        {
            return new SelectAsyncCase().CaseReceive(c, func);
        }

        public static SelectAsyncCase CaseReceive<T>(ChannelReader<T> c,
            Action<T>? func = null)
            where T : notnull
        {
            return new SelectAsyncCase().CaseReceive(c, func);
        }

        public static SelectAsyncCase CaseReceive<T>(Channel<T> c, 
            Func<T, CancellationToken, Task>? func = null)
            where T : notnull
        {
            return new SelectAsyncCase().CaseReceive(c, func);
        }

        public static SelectAsyncCase CaseReceive<T>(ChannelReader<T> c,
            Func<T, CancellationToken, Task>? func = null)
            where T : notnull
        {
            return new SelectAsyncCase().CaseReceive(c, func);
        }
    }
}
