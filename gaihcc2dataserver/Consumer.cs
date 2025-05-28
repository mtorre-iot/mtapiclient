using System.ComponentModel.DataAnnotations;
using gaihcc2dataserver.classes;
using gaihcc2dataserver.common;

namespace gaihcc2dataserver
{
    public class Consumer
    {
        public Dictionary<(string, string), CircularBuffer<double>> gaiBufferArray;
        private AppSettings config;

        public Consumer(Dictionary<(string, string), CircularBuffer<double>> gaiBufferArray, AppSettings config)
        {
            this.gaiBufferArray = gaiBufferArray;
            this.config = config;
        }
        public async void Start()
        {
            Logger.write(logLevel.info, "HCC2 ZMQ StreamData Consumer Started");
            int interleave = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.interleave, config.app.interleave));
            //int number_of_samples = 25 * 200 / interleave; //MAGIC
            int number_of_samples = 40960; //MAGIC
                                           //
                                           // Connect with buffer
                                           //

            while (true)
            {
                List<double[]> all_values = new List<double[]>();

                foreach (KeyValuePair<(string topic, string tagName), CircularBuffer<double>> gaiBuffer in gaiBufferArray)
                {
                    try
                    {
                        double[] values = gaiBuffer.Value.Dequeue(number_of_samples);
                        if (values != null)
                        {
                            all_values.Add(values);
                            (var max, var index) = CommonUtilities.GetMaxAndIndexOfArray<double>(values);
                            //Logger.write(logLevel.info, $"tag: {gaiBuffer.Key.tagName}, total {values.Length} values received, Remaining: {gaiBuffer.Value.GetSize()}, Max: {max}, Index: {index}");
                            Logger.write(logLevel.info, $"tag: {gaiBuffer.Key.tagName}, total {values.Length} values received, Remaining: {gaiBuffer.Value.Count}");
                        }
                        else
                        {
                            Thread.Sleep(100); // MAGIC
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.write(logLevel.debug, $"{e.Message}");
                    }
                }
                if (all_values.Count == 3)
                {
                    using (StreamWriter sw = new StreamWriter("consumer.csv"))
                    {
                        for (int i = 0; i < all_values[0].Length; i++)
                        {
                            foreach (var values in all_values)
                            {
                                sw.Write($"{values[i]}, ");
                            }
                            sw.WriteLine();
                        }
                    }
                }
                Thread.Sleep(500); // MAGIC
            }

        }
    }
}
