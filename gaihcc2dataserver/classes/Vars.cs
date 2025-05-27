using System.Net.NetworkInformation;
using gaihcc2dataserver.common;
using Newtonsoft.Json.Linq;

namespace gaihcc2dataserver.classes;

public class Vars
{
    public Vars()
    {
    }    
    //
    // let's read the variables file!
    //
    public JObject Init(AppSettings config)
        {
        string varFilePath = config.system.variable_file;
        try
        {
            return CommonUtilities.ReadVars(varFilePath);
        }
        catch (Exception e)
        {
            throw new Exception($"Cannot read Variable configuration file. Program ABORTED. Error: {e}");
        }
    }
    public static bool ContainsTopic (JObject vars, string topic)
    {
        // Check if this topic was properly subscribed
        JObject subscriptions = (JObject) vars["Model"]["subscriptions"]; 
        List<(string, JObject)> topicList =  CommonUtilities.GetJsonObjects(subscriptions);
        foreach ((string k, JObject v) in topicList)
        {
            if (k == topic)
            {
                return true;
            }
        }
        return false;
    }
}
