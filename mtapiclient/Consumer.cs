using mtapiclient.classes;
using mtapiclient.common;

namespace mtapiclient
{
    public class Consumer
    {
        public Dictionary<(string, string), GAIBuffer<double>> gaiBufferArray;
        private AppSettings config;

        public Consumer(Dictionary<(string, string), GAIBuffer<double>> gaiBufferArray, AppSettings config)
        {
            this.gaiBufferArray = gaiBufferArray;
            this.config = config;
        }
        public async void Start()
        {
            Logger.write(logLevel.info, "HCC2 ZMQ StreamData Consumer Started");
            int interleave = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.interleave, config.app.interleave));
            int number_of_samples = 25 * 200 / interleave;
            //
            // Connect with buffer
            //
            while (true)
            {
                foreach (KeyValuePair<(string topic, string tagName), GAIBuffer<double>> gaiBuffer in gaiBufferArray)
                {
                    try
                    {

                        double[] values = gaiBuffer.Value.Egress(number_of_samples);
                        Logger.write(logLevel.info, $"tag: {gaiBuffer.Key.tagName}, total of {values.Length} values received, Remaining: {gaiBuffer.Value.GetSize()}");
                    }
                    catch (Exception e)
                    {
                        Logger.write(logLevel.debug, $"{e.Message}");
                    }
                }
                Thread.Sleep(500);
            }

        }
    }
}
