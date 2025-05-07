using System;
using Serilog;
using Microsoft.Extensions.Configuration;
using mtapiclient.classes;
using Newtonsoft.Json.Linq;

namespace mtapiclient
{
class App
{
    private AppSettings config;
    private Serilog.ILogger logger;
    
    public App(Serilog.ILogger logger, AppSettings config)
    {
        this.config = config;
        this.logger = logger;
    }
    public async void Main()
    {
        logger.Information("HCC2 ZMQ API Engine Started");
        //
        // Print version
        //
        logger.Information($"MTAPI Client version: {config.system.version}");
        
        // Create new Client

        ApiClient client = new ApiClient(config);

        // add new API client 

        //#########################################################################################################
        //
        // OUTER LOOP
        //
        while (true)
        {

            Dictionary<bool, JObject> result = null;    
            try
            {
                result = await client.Disconnect(true);
            }
            catch (Exception e)
            {
                logger.Warning($"Client couldn't be disconnected. Error: {e}. Most likely client does not exist.");
            }
            logger.Information($"Client {client.client} diconnected.");

            try
            {
                result = await client.Connect(true);
            }
            catch (Exception e)
            {
                logger.Error($"Client couldn't be able to connect with API server. Error: {e}. Trying to reconnect.");
                Thread.Sleep(5000);
                continue;
            }
            logger.Information($"Client: {client.client} Connected.");

            try
            {
                result = await client.SubscriptionTopics(true);
                if (result.Keys.First() == true)
                {
                    logger.Information($"Topic subscription for Client: {client.client} Was successful.");
                }
                else
                {
                    throw new Exception("Error: Failed Subscription.");
                }
            }
            catch (Exception e)
            {
                logger.Error($"Client couldn't be able to subscribe to topics. Error: {e}. Trying to reconnect.");
                Thread.Sleep(5000);
                continue;
            }

            try
            {
                result = await client.PublicationTopics(true);
                if (result.Keys.First() == true)
                {
                    logger.Information($"Topic Publication for Client: {client.client} Was successful.");
                }
                else
                {
                    throw new Exception("Error: Failed Publication.");
                }
            }
            catch (Exception e)
            {
                logger.Error($"Client couldn't be able to set publication to topics. Error: {e}. Trying to reconnect.");
                Thread.Sleep(5000);
                continue;
            }
            //#########################################################################################################
            //
            // INNER LOOP
            //
            while (true)
            {
                Dictionary<bool, JArray> result_array;  

                string topicName = "liveValue.diagnostics.this.io.0.temperature.";
                List<string>tagNames =  ["cpu."];
                try
                {
                    result_array = await client.Read(topicName, tagNames);
                    if (result_array.Keys.First() == true)
                    {
                        logger.Information($"Read Client: {client.client}, Topic: {topicName} Was successful.");
                        IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                        foreach (JObject jo in jObjects)
                        {
                            logger.Information(jo.ToString());
                        }
                    }
                    else
                    {
                        throw new Exception("Error: Failed on Read.");
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Client couldn't be able to read from topic: {topicName}. Error: {e}. Retrying.");
                    Thread.Sleep(5000);
                    continue;
                }
                //#########################################################################################
                topicName = "liveValue.diagnostics.this.core.0.temperature|.";
                tagNames =  ["core0.", "core1.", "core2.", "core3."];
                try
                {
                    result_array = await client.Read(topicName, tagNames);
                    if (result_array.Keys.First() == true)
                    {
                        logger.Information($"Read Client: {client.client}, Topic: {topicName} Was successful.");
                        IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                        foreach (JObject jo in jObjects)
                        {
                            logger.Information(jo.ToString());
                        }
                    }
                    else
                    {
                        throw new Exception("Error: Failed on Read.");
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Client couldn't be able to read from topic: {topicName}. Error: {e}. Retrying.");
                    Thread.Sleep(5000);
                    continue;
                }
                //#########################################################################################
                topicName = "liveValue.production.this.modbus.0.server.tcp502.INT.HCC2Internals.";
                string tag =  "RESULT.";
                string type = "FLoat";
                object value  = 55.18;
                Quality quality = Quality.Good;
                DateTime ts = DateTime.Now;

                try
                {
                    result = await client.Publish(topicName, tag, type, value, quality, ts);
                    if (result_array.Keys.First() == true)
                    {
                        logger.Information($"Publish Client: {client.client}, Topic: {topicName}, Tag: {tag}, Value: {value} Was successful.");
                    }
                    else
                    {
                        throw new Exception("Error: Failed on Publish");
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Client couldn't be able to publish to topic: {topicName}. Error: {e}. Retrying.");
                    Thread.Sleep(5000);
                    continue;
                }
                Thread.Sleep(10000); // Test   
            }
        }
    }
}
}