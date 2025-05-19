using System;
using Microsoft.Extensions.Configuration;
using mtapiclient.classes;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using mtapiclient.common;
using Newtonsoft.Json;

namespace mtapiclient
{
    public class App
    {
        public Dictionary<(string, string), GAIBuffer<double>> GAIBufferArray;
        private CycleTimer cycleTimer;
        private ConcurrentQueue<List<Record>> webhookQueue;
        private JObject vars;
        private AppSettings config;

        public App(ConcurrentQueue<List<Record>> webhookQueue, CycleTimer cycleTimer, JObject vars, AppSettings config)
        {
            this.cycleTimer = cycleTimer;
            this.webhookQueue = webhookQueue;
            this.config = config;
            this.vars = vars;
            this.GAIBufferArray = new Dictionary<(string, string), GAIBuffer<double>>();

        }
        public async void Main()
        {
            Logger.write(logLevel.info,"HCC2 ZMQ API Client Started");

            // Get all topic / tags pairs subscribed

            List<(string, string)> topicTagPairs = CommonUtilities.GetTopicTagPairsFromSubscription(this.vars);
            //
            // Create a stream buffer for each pair
            // Limit only to the ones that include "streamData" in its topic
            //
            foreach (var (topic, tag) in topicTagPairs)
            {
                if (topic.Contains("streamData") == true)
                {
                    var gaibuffer = new GAIBuffer<double>(topic, tag, config.system.buffer_size);  
                    GAIBufferArray.Add((topic, tag), gaibuffer);
                }
            }

            // Start consumer

            Logger.write(logLevel.info,"Program()- Start Consumer");
            Task<int> consumer = Task.Run(() => {
                var consumer = new Consumer(GAIBufferArray, config);
                consumer.Start();
                return 0;
            });

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
                    Logger.write(logLevel.warning, $"Client couldn't be disconnected. Error: {e}. Most likely client does not exist.");
                }
                Logger.write(logLevel.info,$"Client {client.client} diconnected.");

                ////////////////////////////////////////////////////////////////////////////////////////////////////////
                /// Connect Client 
                /// 
                try
                {
                    result = await client.Connect(true);
                }
                catch (Exception e)
                {
                    Logger.write(logLevel.error, $"Client couldn't be able to connect with API server. Error: {e}. Trying to reconnect.");
                    Thread.Sleep(config.misc.retry_time);
                    continue;
                }
                Logger.write(logLevel.info,$"Client: {client.client} Connected.");
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
                        Logger.write(logLevel.info,$"Get all subscriptions for Client: {client.client} Was successful.");
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
                    Logger.write(logLevel.error, $"Client couldn't be able to get subscriptions list. Error: {e}. Trying to reconnect.");
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
                            Logger.write(logLevel.info,$"Subscription for Topic:{topic} - Client: {client.client} Was successful.");
                        }
                        else
                        {
                            throw new Exception("Error: Failed Delete Subscription.");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.write(logLevel.error, $"Client couldn't be able to subscribe to topics. Error: {e}. Trying to reconnect.");
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
                        Logger.write(logLevel.info,$"Topic subscription for Client: {client.client} Was successful.");
                    }
                    else
                    {
                        throw new Exception("Error: Failed Subscription.");
                    }
                }
                catch (Exception e)
                {
                    Logger.write(logLevel.error, $"Client couldn't be able to subscribe to topics. Error: {e}. Trying to reconnect.");
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
                        Logger.write(logLevel.info,$"Topic Publication for Client: {client.client} Was successful.");
                    }
                    else
                    {
                        throw new Exception("Error: Failed Publication.");
                    }
                }
                catch (Exception e)
                {
                    Logger.write(logLevel.error, $"Client couldn't be able to set publication to topics. Error: {e}. Trying to reconnect.");
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
                        //         Logger.write(logLevel.info,$"Read Client: {client.client}, Topic: {topicName} Was successful.");
                        //         IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                        //         foreach (JObject jo in jObjects)
                        //         {
                        //             Logger.write(logLevel.info,jo.ToString());
                        //         }
                        //     }
                        //     else
                        //     {
                        //         throw new Exception("Error: Failed on Read.");
                        //     }
                        // }
                        // catch (Exception e)
                        // {
                        //     Logger.write(logLevel.error, $"Client couldn't be able to read from topic: {topicName}. Error: {e}. Retrying.");
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
                        //         Logger.write(logLevel.info,$"Read Client: {client.client}, Topic: {topicName} Was successful.");
                        //         IEnumerable<JObject> jObjects = result_array.Values.First().Children<JObject>();
                        //         foreach (JObject jo in jObjects)
                        //         {
                        //             Logger.write(logLevel.info,jo.ToString());
                        //         }
                        //     }
                        //     else
                        //     {
                        //         throw new Exception("Error: Failed on Read.");
                        //     }
                        // }
                        // catch (Exception e)
                        // {
                        //     Logger.write(logLevel.error, $"Client couldn't be able to read from topic: {topicName}. Error: {e}. Retrying.");
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
                        //         Logger.write(logLevel.info,$"Publish Client: {client.client}, Topic: {topicName}, Tag: {tag}, Value: {value} Was successful.");
                        //     }
                        //     else
                        //     {
                        //         throw new Exception("Error: Failed on Publish");
                        //     }
                        // }
                        // catch (Exception e)
                        // {
                        //     Logger.write(logLevel.error, $"Client couldn't be able to publish to topic: {topicName}. Error: {e}. Retrying.");
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
                                        foreach (PVqts vqts in record.vqts)
                                    {
                                            double[] values_list = JsonConvert.DeserializeObject<double[]>(vqts.vqt.v.ToString());
                                            //Logger.write(logLevel.warning, $"Tag: {vqts.tag}, Max: {values_list.Max()}, Min: {values_list.Min()}");
                                        try
                                        {
                                            var gaibuf = GAIBufferArray[(record.topic, vqts.tag)];
                                            if (gaibuf != null)
                                            {
                                                gaibuf.Ingress(values_list, client.interleave); // MAGIC
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            Logger.write(logLevel.warning, $"Error trying to get data from buffer. Error: {e.Message}");
                                        }
                                            //     Logger.write(logLevel.warning, $"----vqts");
                                            //     Logger.write(logLevel.warning, $"--------tag:  {vqts.tag}");
                                            //     Logger.write(logLevel.warning, $"--------type:  {vqts.type}");
                                            //     Logger.write(logLevel.warning, $"------------v:  {vqts.vqt.v}");
                                            //     Logger.write(logLevel.warning, $"------------q:  {vqts.vqt.q}");
                                            //     Logger.write(logLevel.warning, $"------------t:  {vqts.vqt.t}");
                                            //     Logger.write(logLevel.warning, "");
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
                                Logger.write(logLevel.error, $"Error trying to dequeue a message. Error: {e}.");
                                break;
                            }
                        }
                        Thread.Sleep(config.misc.dequeue_loop_time);
                    }
            }
        }
    }
}