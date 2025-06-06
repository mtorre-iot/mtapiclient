using gaihcc2dataserver.classes;
using Google.Protobuf.Collections;
using Grpc.Core;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace TrendFileService.Services
{
    #region Extensions & Helpers
    //Extension methods must be defined in a static class
    public static class StringExtensions
    {
        // This is the extension method.
        // The first parameter takes the "this" modifier
        // and specifies the type for which the method is defined.
        public static WorkflowType ToWorklow(this string workflow) => workflow.ToLower() switch
        {
            "single-baseline" => WorkflowType.SingleBaseline,
            "single-deviation" => WorkflowType.SingleDeviation,
            "repeated-baseline" => WorkflowType.RepeatedBaseline,
            "repeated-deviation" => WorkflowType.RepeatedDeviation,
            "baseline-deviation-repeated-baseline" => WorkflowType.BaselineDeviationRepeatedBaseline,
            "deviation-repeated-baseline" => WorkflowType.DeviationRepeatedBaseline,
            "alternate-baseline-deviation" => WorkflowType.AlternateBaselineDeviation,
            _ => WorkflowType.NotDefined // different from the client implementation as the default on the server should be NotDefined
        };
    }
    #endregion

    public class TrendService : Trend.TrendBase
    {
        private readonly ILogger<TrendService>? _logger;

        private static Stopwatch sw = new Stopwatch();
        private static long previousStopWatchMilliSeconds = 0;
        private static WorkflowType Workflow { get; set; } = WorkflowType.NotDefined;
        private readonly object _lockWorklow = new();

        private static bool interruptStream { get; set; } = false;
        private readonly object _lockInterruptStream = new();
        private static Dictionary<(string, string), CircularBuffer<double>> gaiBufferArray;
        private static AppSettings config;

        public static void InitEnvironment()
        {
            if (!sw.IsRunning)
            {
                sw.Start();
            }
        }
        public static void InitEnvironment(Dictionary<(string, string), CircularBuffer<double>> gaibar, AppSettings cfg)
        {
            if (!sw.IsRunning)
            {
                sw.Start();
            }
            gaiBufferArray = gaibar;
            config = cfg;

            Workflow = WorkflowType.RepeatedBaseline;
        }
        public TrendService(ILogger<TrendService>? logger)
        {
            _logger = logger;
        }
        // trend.proto
        public override Task<IdentityResponse> GetIdentity(Empty request, ServerCallContext context)
        {
            const uint ROCKWELL_VENDOR_ID = 1;
            const uint DRIVE_DEVICE_TYPE = 143;
            const int SUPPORTED_PRODUCT_CODE = 10128; //choices here are 10128, 2192, 8080

            _logger?.LogInformation("Connection to get identity");
            return Task.FromResult(new IdentityResponse
            {
                Result = true,
                VendorID = ROCKWELL_VENDOR_ID,
                DeviceType = DRIVE_DEVICE_TYPE,
                ProductCode = SUPPORTED_PRODUCT_CODE
            });
        }

        // trend.proto
        public override Task<LoadTrendResponse> LoadTrend(LoadTrendRequest request, ServerCallContext context)
        {
            _logger?.LogInformation("LoadTrend: {ID}", JsonSerializer.Serialize(request));
            return Task.FromResult(new LoadTrendResponse
            {
                Result = true,
                TrendType = request.TrendType,
                TrendsCount = 1
            });
        }

        // trend.proto
        public override Task<SelectWorkflowResponse> SelectWorkflow(SelectWorkflowRequest request, ServerCallContext context)
        {
            _logger?.LogInformation("SelectWorkflow: {ID}", JsonSerializer.Serialize(request));

            lock (_lockWorklow)
            {
                Workflow = request.Workflow;
            }
            return Task.FromResult(new SelectWorkflowResponse
            {
                Result = true,
                Workflow = Workflow
            });
        }

        // trend.proto
        public override Task<SelectionsResponse> GetSelections(Empty request, ServerCallContext context)
        {
            _logger?.LogInformation("GetSelections: {ID}", JsonSerializer.Serialize(request));
            return Task.FromResult(new SelectionsResponse
            {
                Result = true,
                BaselineName = "Live Data",
                BaselineTrendsCount = (uint)1,
                DeviationName = "Live Data Duplicate",
                DeviationTrendsCount = (uint)1,
                Workflow = Workflow
            });
        }

        // trend.proto
        public override Task<StopStreamResponse> StopDataStream(Empty request, ServerCallContext context)
        {
            _logger?.LogInformation("Stop Data Stream");

            lock (_lockInterruptStream)
            {
                interruptStream = true;
            }

            return Task.FromResult(new StopStreamResponse
            {
                Result = true
            });
        }
/////////////////////////////////////////////////////////////////////////////////////////////////////////
        public static List<List<DataItem>> GetSignalsFromHCC2(double frequency, double amplitude)
        {
            List<List<DataItem>> trends = new List<List<DataItem>>();
            List<DataItem> trend = null;
            List<List<double>> all_values = new List<List<double>>();

            foreach (KeyValuePair<(string topic, string tagName), CircularBuffer<double>> gaiBuffer in gaiBufferArray)
            {
                while (true)
                {
                    double[] values = gaiBuffer.Value.Dequeue(40960); // MAGIC
                    if (values != null)
                    {
                        all_values.Add(values.ToList());
                        break;
                    }
                    else
                    {
                        Thread.Sleep(500); // MAGIC
                    }
                }
            }

            for (int i = 0; i < all_values[0].Count; i++) // MAGIC
            {
                if (i % 4096 == 0)
                {
                    trend = new List<DataItem>();
                    trends.Add(trend);
                }
                var dataItem = new DataItem
                {
                    OutputFrequency = frequency, // + (random.Next(-100, 100) / 10000.0),
                    CurrentA = all_values[0][i],
                    CurrentB = all_values[1][i],
                    CurrentC = all_values[2][i],
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                };
                trend.Add(dataItem);
            }
            return trends;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        public static List<List<DataItem>> GenerateSineSignal(double frequency, double amplitude)
        {
            List<List<DataItem>> trends = new List<List<DataItem>>();
            long currentMilliSeconds = sw.ElapsedMilliseconds;
            previousStopWatchMilliSeconds = currentMilliSeconds - 40960;
            long differenceMilliSeconds = currentMilliSeconds - previousStopWatchMilliSeconds;


            var time = DateTime.UtcNow.AddMilliseconds(-differenceMilliSeconds);
            List<DataItem> trend = new List<DataItem>();
            trends.Add(trend);
            
            for (long i = previousStopWatchMilliSeconds; i < currentMilliSeconds; i++)
            {
                if (trend.Count == 4096)
                {
                    trend = new List<DataItem>();
                    trends.Add(trend);
                }

                double value1 = amplitude * Math.Sin(2 * Math.PI * frequency * (i / 1000.0));
                double value2 = amplitude * Math.Sin((2 * Math.PI * frequency * (i / 1000.0)) - 2.0944);
                double value3 = amplitude * Math.Sin((2 * Math.PI * frequency * (i / 1000.0)) - 4.1888);
                var dataItem = new DataItem
                {
                    OutputFrequency = frequency,
                    CurrentA = value1,
                    CurrentB = value2,
                    CurrentC = value3,
                    TimeStamp = time.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                };
                trend.Add(dataItem);

                time = time.AddMilliseconds(1);
            }

            return trends;
        }

        private async void SaveDataInfile(List<List<DataItem>> dataSet)
        {
            try
            {
                using (StreamWriter stw = new StreamWriter(config.system.data_file)) // MAGIC
                {
                    foreach (var data in dataSet)
                    {
                        foreach (var item in data)
                        {
                            stw.WriteLine($" {item.CurrentA}, {item.CurrentB}, {item.CurrentC}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.write(logLevel.error, $"Error tryting to save data. Error: {e.Message}");
            }
        }

        private List<List<DataItem>> GetHCC2Data()
        {
            try
            {

                Logger.write(logLevel.info, $"Collecting High speed data from HCC2.");
                var trends = GetSignalsFromHCC2(50, 2);  //MAGIC
                if (config.system.data_file_enabled == true)
                {
                    SaveDataInfile(trends);
                }
                return trends;
                //return GenerateSineSignal(50, 2);
            }
            catch (Exception e)
            {
                return new List<List<DataItem>>();
            }
        }

        private async Task ExecuteSingleList(StartStreamRequest request, IServerStreamWriter<DataStreamResponse> responseStream, ServerCallContext context, List<List<DataItem>> trends)
        {
            //TODO: Read HCC2 Data
            trends = GetHCC2Data();

            var count = trends.Count;
            _logger?.LogInformation("Trends count {tcount}", count);

            foreach (var trend in trends)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    return;
                }
                var list = new RepeatedField<DataItem>();
                list.AddRange(trend);
                _logger?.LogInformation("DataStreamResponse: {r}", JsonSerializer.Serialize(new DataStreamResponse
                {
                    Result = true,
                    WorkflowType = Workflow,
                    Items = { list },

                }));
                await responseStream.WriteAsync(new DataStreamResponse
                {
                    Result = true,
                    WorkflowType = Workflow,
                    Items = { list },

                });
                if (request.DelayMilliSec > 0)
                {
                    await Task.Delay(request.DelayMilliSec);
                }
            }
        }

        public async Task ExecuteRepeatedList(StartStreamRequest request, IServerStreamWriter<DataStreamResponse> responseStream, ServerCallContext context, List<List<DataItem>> trends)
        {
            while (true)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await ExecuteSingleList(request, responseStream, context, trends);
                lock (_lockInterruptStream)
                {
                    if (interruptStream)
                    {
                        interruptStream = false;
                        
                        break;
                    }
                }
            }
            _logger?.LogInformation("Stream interrupted");
        }
        public async Task ExecuteAlternatingList(StartStreamRequest request, IServerStreamWriter<DataStreamResponse> responseStream, ServerCallContext context, List<List<DataItem>> trend1, List<List<DataItem>> trend2)
        {
            while (true)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }
                await ExecuteSingleList(request, responseStream, context, trend1);
                if (context.CancellationToken.IsCancellationRequested)
                {
                    break;
                }
                await ExecuteSingleList(request, responseStream, context, trend2);
                lock (_lockInterruptStream)
                {
                    if (interruptStream)
                    {
                        interruptStream = false;
                        break;
                    }
                }
            }
            _logger?.LogInformation("Stream interrupted");
        }

        public async Task ExecuteUnsupported(IServerStreamWriter<DataStreamResponse> responseStream)
        {
            await responseStream.WriteAsync(new DataStreamResponse
            {
                Result = false,
                WorkflowType = Workflow,
                Items = { },
            });
        }

        // trend.proto
        public override async Task StartDataStream(StartStreamRequest request, IServerStreamWriter<DataStreamResponse> responseStream, ServerCallContext context)
        {
            _logger?.LogInformation("StartDataStream: WorkFlow {WorkFlow}, {ID}", Workflow, JsonSerializer.Serialize(request));

            switch (Workflow)
            {
                case WorkflowType.SingleBaseline:
                    await ExecuteSingleList(request, responseStream, context, new List<List<DataItem>>());
                    break;
                case WorkflowType.RepeatedBaseline:
                    await ExecuteRepeatedList(request, responseStream, context, new List<List<DataItem>>());
                    break;
                case WorkflowType.SingleDeviation:
                    await ExecuteSingleList(request, responseStream, context, new List<List<DataItem>>());
                    break;
                case WorkflowType.RepeatedDeviation:
                    await ExecuteRepeatedList(request, responseStream, context, new List<List<DataItem>>());
                    break;
                case WorkflowType.BaselineDeviationRepeatedBaseline:
                    await ExecuteSingleList(request, responseStream, context, new List<List<DataItem>>());
                    await ExecuteSingleList(request, responseStream, context, new List<List<DataItem>>());
                    await ExecuteRepeatedList(request, responseStream, context, new List<List<DataItem>>());
                    break;
                case WorkflowType.DeviationRepeatedBaseline:
                    await ExecuteSingleList(request, responseStream, context, new List<List<DataItem>>());
                    await ExecuteRepeatedList(request, responseStream, context, new List<List<DataItem>>());
                    break;
                case WorkflowType.AlternateBaselineDeviation:
                    await ExecuteAlternatingList(request, responseStream, context, new List<List<DataItem>>(), new List<List<DataItem>>());
                    break;
                case WorkflowType.UnsupportedWorkflow:
                    await ExecuteUnsupported(responseStream);
                    break;
            }
        }
    }
}