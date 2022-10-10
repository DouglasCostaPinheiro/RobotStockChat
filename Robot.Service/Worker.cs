using System.Data;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Robot.Service.Framework;

namespace Robot.Service
{
    public class Worker : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string StockInfoTemplate = "https://stooq.com/q/l/?s={0}&f=sd2t2ohlcv&h&e=csv";
        private readonly ILogger<Worker> _logger;
        private readonly AppSettings _appSettings;
        private MqttFactory _mqttFactory = new();
        private IMqttClient _mqttClient;

        public Worker(ILogger<Worker> logger, AppSettings appSettings, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _appSettings = appSettings;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_mqttClient == null)
            {
                var options = new MqttClientOptionsBuilder()
                    .WithClientId(Guid.NewGuid().ToString())
                    .WithWebSocketServer("wss://broker.emqx.io:8084/mqtt")
                    .Build();

                _mqttClient = _mqttFactory.CreateMqttClient();

                _mqttClient.ApplicationMessageReceivedAsync += async (message) =>
                {
                    var topic = message.ApplicationMessage.Topic;
                    var payload = JsonConvert.DeserializeObject<JObject>(message.ApplicationMessage.ConvertPayloadToString());
                    var responseMessage = await GetMessageForStock(payload.GetValue("message").Value<string>());

                    if (responseMessage != null)
                    {
                        await _mqttClient.PublishStringAsync(topic, JsonConvert.SerializeObject(new
                        {
                            timeStamp = DateTime.UtcNow.ToString("o"),
                            message = responseMessage,
                            owner = new
                            {
                                id = _appSettings.ApplicationId,
                                name = "RoboStock"
                            }
                        }), cancellationToken: CancellationToken.None);
                    }
                };

                await _mqttClient.ConnectAsync(options, CancellationToken.None);

                // Subscribe to all chats in namespace
                var mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => { f.WithTopic($"Application/{_appSettings.ApplicationId}/+"); })
                    .Build();

                await _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            }
        }

        public async Task<string?> GetMessageForStock(string messagePayload)
        {
            string? responseString = null;
            var regexTest = Regex.Match(messagePayload, "^\\/(stock=)((.)+)$");
            if (!regexTest.Success) return responseString;
            
            var stock = regexTest.Groups[2].Value;

            try
            {
                var httpRequestMessage =
                    new HttpRequestMessage(HttpMethod.Get, string.Format(StockInfoTemplate, stock));

                var httpClient = _httpClientFactory.CreateClient();
                var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

                if (httpResponseMessage.IsSuccessStatusCode)
                {
                    using var contentStream =
                        await httpResponseMessage.Content.ReadAsStreamAsync();
                    var dataTable = ConvertCSVtoDataTable(contentStream);
                    if (dataTable.Rows.Count > 0 && (string) dataTable.Rows[0]["Date"] != "N/D")
                    {
                        responseString =
                            $"{stock} opened at {dataTable.Rows[0]["Open"]}, had a high of {dataTable.Rows[0]["High"]}, a low of {dataTable.Rows[0]["Low"]}, and a close of {dataTable.Rows[0]["Close"]} per share.";
                    }
                    else
                    {
                        responseString = $"No data found for stock {stock}.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"Error consulting stock for {stock}: {ex.Message}{Environment.NewLine}{ex.StackTrace}");
                responseString = $"Error consulting stock for {stock}: {ex.Message}";
            }

            return responseString;
        }

        private static DataTable ConvertCSVtoDataTable(Stream stream)
        {
            DataTable dt = new DataTable();
            using (StreamReader sr = new StreamReader(stream))
            {
                string[] headers = sr.ReadLine().Split(',');
                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    DataRow dr = dt.NewRow();
                    for (int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i];
                    }
                    dt.Rows.Add(dr);
                }

            }


            return dt;
        }
    }
}