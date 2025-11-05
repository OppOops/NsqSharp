using System;
using System.Threading.Tasks;
using NsqSharp.Utils.Channels;

namespace NsqSharp.Utils
{
    /// <summary>
    /// Net package. http://golang.org/pkg/net
    /// </summary>
    public static class Net
    {
        /// <summary>
        /// Dial connects to the address on the named network.
        /// 
        /// Known networks are "tcp" only at this time.
        /// 
        /// Addresses have the form host:port. If host is a literal IPv6 address it must be enclosed in square brackets as in
        /// "[::1]:80" or "[ipv6-host%zone]:80". The functions JoinHostPort and SplitHostPort manipulate addresses in this form.
        /// </summary>
        public static async Task<IConn> DialAsync(string network, string address, CancellationToken token)
        {
            if (network != "tcp")
                throw new ArgumentException("only 'tcp' network is supported", "network");

            // TODO: Make this more robust, support IPv6 splitting
            var split = address.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            string hostname = split[0];
            int port = int.Parse(split[1]);

            return await TcpConn.ConnectAsync(hostname, port, token);
        }

        /// <summary>
        /// DialTimeout acts like Dial but takes a timeout. The timeout includes name resolution, if required.
        /// </summary>
        public static async Task<IConn> DialTimeoutAsync(string network, string address, TimeSpan timeout, CancellationToken token = default)
        {
            if (network != "tcp")
                throw new ArgumentException("only 'tcp' network is supported", nameof(network));
            var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(timeout);
            return await DialAsync(network, address, cts.Token);
        }
    }
}
