using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using mtapiclient.common;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Formats.Asn1;
using System.Dynamic;
using System.Security;

namespace mtapiclient.classes;

public class BaseQuery()
{

    public string Build_url(string protocol, string host, int port, string suffix, string command, string first_param, string query)
    {
        string path = $"{suffix}/{command}/{first_param}";
        //if (first_param == String.Empty)
        //{
        //    path = $"{suffix}/{command}";
        //}
        UriBuilder url = new UriBuilder {
            Scheme = protocol,
            Host = host,
            Port = port,
            Path = path,
            //Query = HttpUtility.UrlEncode(query)
            Query = query
        };  
        return url.ToString();
    }
    public Dictionary<string, string> Build_headers(string content_type, string headers)
    {
        return new Dictionary<string, string>{{content_type, headers}}; 
    }
    public async Task<Dictionary<bool, JObject>> Request(string url, string operation, Dictionary<string, string> headers, string payload, bool response_required)
    {
        using (HttpClient client = new HttpClient()) 
        {
            var content = new StringContent(payload, Encoding.UTF8, headers.Values.First());
            var request = new HttpRequestMessage(CommonUtilities.GetHttpMethod(operation), url)
            {
                Content = content
            };

            HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            JObject jsonData = null;

            if (responseBody != String.Empty)
            {
                jsonData = JObject.Parse(responseBody);
            }
            return new Dictionary<bool, JObject>() {{response.IsSuccessStatusCode, jsonData }};
        }
    }
}

public class ConnectionQuery : BaseQuery
{
    public List<Connection> connections {get; set; } 
    public ConnectionQuery()
    {  
        List<Connection>connections = new List<Connection>();
    }
}

public class SubscriptionQuery : BaseQuery
{
    public List<Subscription> subscriptions {get; set; } 
    public SubscriptionQuery()
    {
        this.subscriptions = new List<Subscription>();
    }

    public async Task<string> BuildSubscriptionPayload(JObject subscriptions)
    {
        //
        // Get a list of all topics to be subscribed 
        //
        List<(string,JObject)> subscriptionList =  CommonUtilities.GetJsonObjects(subscriptions);
        //
        // Build the payload
        //
        List<Subscription> payload = new List<Subscription>();
        foreach (var (topicName, payload_sub) in subscriptionList)
        {
            List<(string,JObject)> tagList =  CommonUtilities.GetJsonObjects((JObject)payload_sub["tags"]);
            Subscription sub = new Subscription();
            sub.topic = topicName;
            sub.callbackUrl =(string) payload_sub["callback"];
            foreach (var (tagName, payload_tag) in tagList)
            {
                sub.tags.Add(new Tag((string)payload_tag["name"], (string)payload_tag["type"]));
            }
            payload.Add(sub);
        }
        string payloadStr = JsonConvert.SerializeObject(payload);
        return payloadStr;
    }
}

public class SubscriptionReadQuery : BaseQuery
{
    public List<Subscription> subscriptions {get; set; } 
    public SubscriptionReadQuery()
    {
        this.subscriptions = new List<Subscription>();
    }

    public async Task<string> BuildSubscriptionReadPayload(string topicName, List<string> tagNames)
    {
        List<ReadTopic> payload = new List<ReadTopic> { new ReadTopic(topicName, tagNames) };
        string payloadStr = JsonConvert.SerializeObject(payload);
        return payloadStr;
    }


    public async Task<Dictionary<bool, JArray>> Request(string url, string operation, Dictionary<string, string> headers, string payload, bool response_required)
    {
        using (HttpClient client = new HttpClient()) 
        {
            var content = new StringContent(payload, Encoding.UTF8, headers.Values.First());
            var request = new HttpRequestMessage(CommonUtilities.GetHttpMethod(operation), url)
            {
                Content = content
            };

            HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
            JArray jsonData = null;

            if (responseBody != String.Empty)
            {
                jsonData = JArray.Parse(responseBody);
            }
            return new Dictionary<bool, JArray>() {{response.IsSuccessStatusCode, jsonData }};
        }
    }
}

public class PublicationQuery: BaseQuery
{
    public List<Publication> publications {get; set;}
    public PublicationQuery()
    {
        publications = new List<Publication>();
    }
    public async Task<string> BuildPublicationPayload(JObject publications)
    {
        //
        // Get a list of all topics to be set publication
        //
        List<(string,JObject)> publicationList =  CommonUtilities.GetJsonObjects(publications);
        //
        // Build the payload
        //
        List<Publication> payload = new List<Publication>();
        foreach (var (topicName, payload_pub) in publicationList)
        {
            List<(string,JObject)> tagList =  CommonUtilities.GetJsonObjects((JObject)payload_pub["tags"]);
            Publication pub = new Publication();
            pub.topic = topicName;
            foreach (var (tagName, payload_tag) in tagList)
            {
                pub.tags.Add(new Tag((string)payload_tag["name"], (string)payload_tag["type"]));
            }
            payload.Add(pub);
        }
        string payloadStr = JsonConvert.SerializeObject(payload);
        return payloadStr;   
    }

    public async Task<string> BuildPublicationPublishPayload(string topicName, string tagName, string type, object value, Quality quality, DateTime timestamp)
    {
        List<PublishTopic> payload = new List<PublishTopic> { new PublishTopic(topicName, tagName, type, value, quality, timestamp) };
        string payloadStr = JsonConvert.SerializeObject(payload);
        return payloadStr;
    }
}


public class Connection
{
    public string? clientName {get; set; }
    public string? creationDateTime {get; set; }
    
    public Connection()
    {
        this.clientName = String.Empty;
        this.creationDateTime = String.Empty;
    }
}

public class BaseOperation
{
    public string? topic {get; set;}
    public List<Tag> tags {get; set;}
    public BaseOperation()
    {
        this.topic = null;
        this.tags = new List<Tag>();

    }
}
public class Subscription: BaseOperation
{
    public string? callbackUrl {get; set;}

    public Subscription() : base()
    {
        this.callbackUrl = null;
    }
    public void fromJson(JObject jsonObj)
    {
        this.topic = (string?)jsonObj["topic"];
        this.tags = Tag.getTags(jsonObj);
        this.callbackUrl = (string?)jsonObj["callbackUrl"];
    }
}

public class Publication : BaseOperation
{
    public Publication() : base()
    {
    }
}
public class Tag
{
    public string? name {get; set;} = null;
    public string? type {get; set;} = null;
    public Tag(string name, string type)
    {
        this.name = name;
        this.type = type;
    }
    public static List<Tag> getTags(JObject jsonObj)
    {
        List<Tag> rtn = new List<Tag>();
        foreach (JObject t in jsonObj["tags"])
        {
            rtn.Add(new Tag((string?)t["name"], (string?)t["type"]));
        }
        return rtn;
    }
}

public class ReadTopic
{
    public string topic {get; set;}
    public List<string> tags {get; set;}
    public ReadTopic(string topicName, List<string> tagNames)
    {
        this.topic = topicName;
        this.tags = tagNames;
    }
}

public class PublishTopic
{
    public string topic {get; set;}
    public List<Vqts> vqts {get; set;}
    public PublishTopic(string topic, string tag, string type, object value, Quality quality, DateTime timestamp)
    {
        this.topic = topic;
        Vqt vqt = new Vqt(value, quality, timestamp);
        Vqts vqts1 = new Vqts(tag, type, vqt);
        this.vqts = new List<Vqts>() {vqts1};
    }
}

public class Vqts
{
    public string tag {get; set;}
    public string type {get; set;}
    public Vqt vqt {get; set;}
    public Vqts(string tag, string type, Vqt vqt)
    {
        this.tag = tag;
        this.type = type;
        this.vqt = vqt;
    }
}
public class Vqt
{
    public object v {get; set;}
    public int q {get; set;}
    public string qtext {get; set;}
    public string t {get; set;}
    public Vqt(object v, Quality q, DateTime t)
    {
        this.v = v;
        this.q = QualityConv.ConvertQualityToInt(q);
        this.qtext = QualityConv.ConvertTagQualityToString(q);
        DateTime utcTime = CommonUtilities.ConvertLocalTimeToUtc(t);
        this.t = CommonUtilities.ConvertTimestampToString(utcTime);
    }
}

public class PVqts
{
    public string tag {get; set;}
    public string type {get; set;}
    public PVqt vqt {get; set;}
    public PVqts(string tag, string type, PVqt vqt)
    {
        this.tag = tag;
        this.type = type;
        this.vqt = vqt;
    }
}


public class PVqt
{
    public object v {get; set;}
    public int q {get; set;}
    public string qtext {get; set;}
    public string t {get; set;}
    public PVqt(object v, int q, string qtext, string t)
    {
        this.v = v;
        this.q = q;
        this.qtext = qtext;
        this.t = t;
    }
}

public class Record
{
    public string topic {get ;set;}
    public List<PVqts> vqts {get; set;}
}
