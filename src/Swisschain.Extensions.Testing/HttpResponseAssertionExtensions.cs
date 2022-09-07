using System.Net.Http;
using Serilog.Sinks.InMemory;
using Xunit.Abstractions;

namespace Swisschain.Extensions.Testing
{
    public static class HttpResponseAssertionExtensions
    {
        public static void AssertSuccessStatusCodeOrElseShowServerLogs(this HttpResponseMessage message,
            ITestOutputHelper outputHelper)
        {
            if (!message.IsSuccessStatusCode)
            {
                foreach (var logEvent in InMemorySink.Instance.LogEvents)
                {
                    outputHelper.WriteLine($"[{logEvent.Level}]: {logEvent.RenderMessage()} {logEvent.Exception}");
                }
                message.EnsureSuccessStatusCode();
            }
        }
    }
}
