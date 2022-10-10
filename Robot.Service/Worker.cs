using System.Data;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Robot.Service.Framework;
//using uPLibrary.Networking.M2Mqtt;
//using uPLibrary.Networking.M2Mqtt.Messages;

namespace Robot.Service
{
    public class Worker : BackgroundService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string StockInfoTemplate = "https://stooq.com/q/l/?s={0}&f=sd2t2ohlcv&h&e=csv";
        private readonly ILogger<Worker> _logger;
        private readonly AppSettings _appSettings;
        private MqttFactory _mqttFactory = new MqttFactory();
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
                    string responseString = null;
                    var topic = message.ApplicationMessage.Topic;
                    var payload = message.ApplicationMessage.ConvertPayloadToString();
                    var regexTest = Regex.Match(payload, "\\/(stock=)((.)+)");
                    if (regexTest.Success)
                    {
                        var stock = regexTest.Groups[2].Value;

                        try
                        {
                            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(StockInfoTemplate, stock));

                            var httpClient = _httpClientFactory.CreateClient();
                            var httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

                            if (httpResponseMessage.IsSuccessStatusCode)
                            {
                                using var contentStream =
                                    await httpResponseMessage.Content.ReadAsStreamAsync();
                                var dataTable = ConvertCSVtoDataTable(contentStream);
                                if (dataTable.Rows.Count > 0 && (string)dataTable.Rows[0]["Date"] != "N/D")
                                {
                                    responseString = $"{stock} opened at {dataTable.Rows[0]["Open"]}, had a high of {dataTable.Rows[0]["High"]}, a low of {dataTable.Rows[0]["Low"]}, and a close of {dataTable.Rows[0]["Close"]} per share.";
                                } else
                                {
                                    responseString = $"No data found for stock {stock}.";
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            responseString = $"Error consulting stock for {stock}: {ex.Message}";
                        }
                        finally
                        {
                            await _mqttClient.PublishStringAsync(topic, JsonConvert.SerializeObject(new
                            {
                                timeStamp = DateTime.UtcNow.ToString("o"),
                                message = responseString,
                                owner = new
                                {
                                    id = _appSettings.ApplicationId,
                                    name = "RoboStock"
                                }
                            }));
                        }
                    }
                };

                await _mqttClient.ConnectAsync(options, CancellationToken.None);

                // Subscribe to all chats in namespace
                var mqttSubscribeOptions = _mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => { f.WithTopic($"Application/{_appSettings.ApplicationId}/+"); })
                    .Build();

                await _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
            }

            //var client = new MqttClient("broker.emqx.io");
            //// register to message received 
            //client.MqttMsgPublishReceived += (sender, args) =>
            //{

            //};

            //string clientId = Guid.NewGuid().ToString();
            //client.Connect(clientId);

            //// subscribe to the topic "/home/temperature" with QoS 2 
            //client.Subscribe(new[] { $"Application/${_appSettings.ApplicationId}/+" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            while (!stoppingToken.IsCancellationRequested)
            {
                //    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                //    await Task.Delay(1000, stoppingToken);
            }
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