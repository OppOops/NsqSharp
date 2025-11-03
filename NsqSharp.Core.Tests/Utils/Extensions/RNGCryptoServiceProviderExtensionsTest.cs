using System;
using System.Security.Cryptography;
using NsqSharp.Utils.Extensions;
using NUnit.Framework;

namespace NsqSharp.Tests.Utils.Extensions
{
    [TestFixture]
    public class RNGCryptoServiceProviderExtensionsTest
    {
        [Test]
        public void Float64()
        {
            
            for (int i = 0; i < 10000; i++)
            {
                double value = Random.Shared.NextDouble();
                Assert.GreaterOrEqual(value, 0);
                Assert.Less(value, 1);
            }
        }
    }
}
