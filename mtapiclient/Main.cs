using System;
using Serilog;
using Microsoft.Extensions.Configuration;
using mtapiclient.classes;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using mtapiclient.common;

namespace mtapiclient
{
class App
{
    private CycleTimer cycleTimer;
    private ConcurrentQueue<List<Record>> webhookQueue;
    private JObject vars;
    private AppSettings config;
    private Serilog.ILogger logger;
    
    public App(Serilog.ILogger logger, ConcurrentQueue<List<Record>> webhookQueue, CycleTimer cycleTimer, JObject vars, AppSettings config)
    {
        this.cycleTimer = cycleTimer;
        this.webhookQueue = webhookQueue;
        this.config = config;
        this.vars = vars;
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

        ApiClient client = new ApiClient(vars, config);

        // add new API client 

        //#########################################################################################################
        //
        // OUTER LOOP
        //
        while (true)       
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Disconnect Client 
            /// 
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

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Connect Client 
            /// 
            try
            {
                result = await client.Connect(true);
            }
            catch (Exception e)
            {
                logger.Error($"Client couldn't be able to connect with API server. Error: {e}. Trying to reconnect.");
                Thread.Sleep(config.misc.retry_time);
                continue;
            }
            logger.Information($"Client: {client.client} Connected.");
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Get All current subscriptions
            ///
            Dictionary<bool, JArray> result_array; 
            List<string> existing_topics = new List<string>();
            try
            {
                result_array = await client.GetSubscriptions();
                if (result_array.Keys.First() == true)
                {
                    logger.Information($"Get all subscriptions for Client: {client.client} Was successful.");
                    IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                        foreach (JObject jo in jObjects)
                        {
                           existing_topics.Add((string)jo["topic"]);
                        }
                }
                else
                {
                    throw new Exception("Error: GetSubscription Failed.");
                }
            }
            catch (Exception e)
            {
                logger.Error($"Client couldn't be able to get subscriptions list. Error: {e}. Trying to reconnect.");
                Thread.Sleep(config.misc.retry_time);
                continue;
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Delete subscriptions 
            /// 
            foreach (string topic in existing_topics)
            {
                try
                {
                    result = await client.DeleteSubscription(topic);
                    if (result.Keys.First() == true)
                    {
                        logger.Information($"Subscription for Topic:{topic} - Client: {client.client} Was successful.");
                    }
                    else
                    {
                        throw new Exception("Error: Failed Delete Subscription.");
                    }
                }
                catch (Exception e)
                {
                    logger.Error($"Client couldn't be able to subscribe to topics. Error: {e}. Trying to reconnect.");
                    Thread.Sleep(config.misc.retry_time);
                    continue;
                }
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Subscribe Topics (vars.json) 
            /// 
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
                Thread.Sleep(config.misc.retry_time);
                continue;
            }
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            /// Set Publication Topics (vars.json) 
            /// 
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
                Thread.Sleep(config.misc.retry_time);
                continue;
            }
            //#########################################################################################################
            //
            // Setup Duty Cycle Timer
            //
            cycleTimer.Start(client.on_time, client.off_time);

            //#########################################################################################################
            //
            // INNER LOOP
            //
            while (true)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                //// Read Topic / Tags 
                //// 
                // Dictionary<bool, JArray> result_array;  

                // string topicName = "liveValue.diagnostics.this.io.0.temperature.";
                // List<string>tagNames =  ["cpu."];
                // try
                // {
                //     result_array = await client.Read(topicName, tagNames);
                //     if (result_array.Keys.First() == true)
                //     {
                //         logger.Information($"Read Client: {client.client}, Topic: {topicName} Was successful.");
                //         IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                //         foreach (JObject jo in jObjects)
                //         {
                //             logger.Information(jo.ToString());
                //         }
                //     }
                //     else
                //     {
                //         throw new Exception("Error: Failed on Read.");
                //     }
                // }
                // catch (Exception e)
                // {
                //     logger.Error($"Client couldn't be able to read from topic: {topicName}. Error: {e}. Retrying.");
                //     Thread.Sleep(config.misc.retry_time);
                //     continue;
                // }
                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                //// Read Topic / Tags 
                //// 
                // topicName = "liveValue.diagnostics.this.core.0.temperature|.";
                // tagNames =  ["core0.", "core1.", "core2.", "core3."];
                // try
                // {
                //     result_array = await client.Read(topicName, tagNames);
                //     if (result_array.Keys.First() == true)
                //     {
                //         logger.Information($"Read Client: {client.client}, Topic: {topicName} Was successful.");
                //         IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                //         foreach (JObject jo in jObjects)
                //         {
                //             logger.Information(jo.ToString());
                //         }
                //     }
                //     else
                //     {
                //         throw new Exception("Error: Failed on Read.");
                //     }
                // }
                // catch (Exception e)
                // {
                //     logger.Error($"Client couldn't be able to read from topic: {topicName}. Error: {e}. Retrying.");
                //     Thread.Sleep(config.misc.retry_time);
                //     continue;
                // }
                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// Publish Topic / Tag / Value
                /// 
                // topicName = "liveValue.production.this.modbus.0.server.tcp502.INT.HCC2Internals.";
                // string tag =  "RESULT.";
                // string type = "FLoat";
                // object value  = 55.18;
                // Quality quality = Quality.Good;
                // DateTime ts = DateTime.Now;

                // try
                // {
                //     result = await client.Publish(topicName, tag, type, value, quality, ts);
                //     if (result_array.Keys.First() == true)
                //     {
                //         logger.Information($"Publish Client: {client.client}, Topic: {topicName}, Tag: {tag}, Value: {value} Was successful.");
                //     }
                //     else
                //     {
                //         throw new Exception("Error: Failed on Publish");
                //     }
                // }
                // catch (Exception e)
                // {
                //     logger.Error($"Client couldn't be able to publish to topic: {topicName}. Error: {e}. Retrying.");
                //     Thread.Sleep(config.misc.retry_time);
                //     continue;
                // }
                //
                // Dequeue messages from Webhook Queue
                //
                while (true)
                {  
                    try
                    {
                        if (webhookQueue.TryDequeue(out List<Record> records) == true)
                        {
                            foreach (Record record in records)
                            {
                                logger.Warning($"Topic: {record.topic}");
                                foreach (PVqts vqts in record.vqts)
                                {
                                    logger.Warning($"----vqts");
                                    logger.Warning($"--------tag:  {vqts.tag}");
                                    logger.Warning($"--------type:  {vqts.type}");
                                    logger.Warning($"------------v:  {vqts.vqt.v}");
                                    logger.Warning($"------------q:  {vqts.vqt.q}");
                                    logger.Warning($"------------t:  {vqts.vqt.t}");
                                    logger.Warning("");
                                }
                            }

                            
                        }
                        else
                        {
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error trying to dequeue a message. Error: {e}.");
                        break;
                    }
                }
                Thread.Sleep(config.misc.dequeue_loop_time);
            }
        }
    }
}
}