using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Robot.Service.Framework;

namespace Robot.Service.Test
{
    public class WhenRequestingStockQuota
    {
        private Worker robotWorker;
        private readonly Mock<ILogger<Worker>> _logger = new();
        private readonly Mock<HttpMessageHandler> _httpMessageHandler = new();
        private readonly Mock<IHttpClientFactory> _httpClientFactory = new();

        [Test]
        public async Task ThenRequestReturnsASuccessfulQuota()
        {
            string successContent = @"Symbol,Date,Time,Open,High,Low,Close,Volume
AAPL.US,2022-10-07,22:00:06,142.54,143.1,139.445,140.09,85925559";
            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(successContent)
                })
                .Verifiable();
            var httpClient = new HttpClient(_httpMessageHandler.Object);
            _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            robotWorker = new Worker(_logger.Object,
                new AppSettings() { ApplicationId = Guid.Parse("8284AB03-9CF6-4A6A-8FCD-D49CCDE4B96A") },
                _httpClientFactory.Object);

            Assert.AreEqual(await robotWorker.GetMessageForStock("/stock=AAPL.US"), $"AAPL.US opened at 142.54, had a high of 143.1, a low of 139.445, and a close of 140.09 per share.");
        }

        [Test]
        public async Task ThenRequestReturnsANonExistingStock()
        {
            string successContent = @"Symbol,Date,Time,Open,High,Low,Close,Volume
AAPL.US,N/D,N/D,N/D,N/D,N/D,N/D,N/D";
            _httpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(successContent)
                })
                .Verifiable();
            var httpClient = new HttpClient(_httpMessageHandler.Object);
            _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            robotWorker = new Worker(_logger.Object,
                new AppSettings() { ApplicationId = Guid.Parse("8284AB03-9CF6-4A6A-8FCD-D49CCDE4B96A") },
                _httpClientFactory.Object);

            Assert.AreEqual(await robotWorker.GetMessageForStock("/stock=AAPL.US"), $"No data found for stock AAPL.US.");
        }
    }
}