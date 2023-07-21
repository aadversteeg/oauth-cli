using System;
using WireMock.Server;

namespace Tests.Infrastructure.OAuth
{
    public class WireMockTestsFixture : IDisposable
    {
        public WireMockServer Server { get; }

        public WireMockTestsFixture()
        {
            Server = WireMockServer.Start();
        }

        public void Dispose()
        {
             Server.Stop();
        }
    }
}
