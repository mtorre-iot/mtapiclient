using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using gaihcc2dataserver.common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gaihcc2dataserver.classes;

public class ApiClient
{
    public string client {get;}
    private string api_host {get; set;}
    private int api_port {get; set;}
    private string api_protocol {get; set;}
    private string api_suffix {get; set;}
    private int time_period {get; set;}
    private AppSettings config {get; set;}
    public JObject vars {get; set;}
    public int on_time {get; set;}
    public int off_time {get; set;}
    public int interleave {get; set;}
    public int trend_port_anyip {get; set;}

    public ApiClient(JObject vars, AppSettings config)
    {
        //
        // Initialize internal variables
        //
        this.vars = vars;
        this.config = config;
        //
        // Read Environment variables
        //
        this.client = CommonUtilities.GetEnvVariableWithDefault(config.env.client, config.app.client);
        this.api_host = CommonUtilities.GetEnvVariableWithDefault(config.env.api_host, config.app.api_host);
        this.api_protocol = CommonUtilities.GetEnvVariableWithDefault(config.env.api_protocol, config.app.api_protocol);
        this.api_suffix = CommonUtilities.GetEnvVariableWithDefault(config.env.api_suffix, config.app.api_suffix);
        this.api_port = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.api_port, config.app.api_port));
        this.time_period = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.time_period, config.app.time_period));
        this.on_time = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.on_time, config.app.on_time));
        int min_on_time = Convert.ToInt32(config.app.on_time);
        if (this.on_time < min_on_time) { this.on_time = Convert.ToInt32(config.app.on_time); }
        this.off_time = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.off_time, config.app.off_time));
        int min_off_time = Convert.ToInt32(config.app.off_time);
        if (this.off_time < min_off_time) { this.off_time = Convert.ToInt32(config.app.off_time); }
        this.interleave = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.interleave, config.app.interleave));
        this.trend_port_anyip = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.trend_port_anyip, config.app.trend_port_anyip));
        //
        // let's read the variables file!
        //
        // string varFilePath = config.system.variable_file;
        // try
        // {
        //     this.vars = CommonUtilities.ReadVars(varFilePath);
        // }
        // catch (Exception e)
        // {
        //     throw new Exception($"Cannot read Variable configuration file. Program ABORTED. Error: {e}");
        // }
    }

    public async Task<Dictionary<bool, JObject>> Connect(bool openIfExists)
    {
        ConnectionQuery connectionQuery = new ConnectionQuery();
        string command = config.api.set_connection.command;
        string url = connectionQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, client, "openIfExists=" + openIfExists.ToString());

        string header_type = config.api.set_connection.header_type;
        string header = config.api.set_connection.header;    
        Dictionary<string, string> headers = connectionQuery.Build_headers(header_type, header);

        string operation = config.api.set_connection.operation;     
        bool response_required = config.api.set_connection.response_required;     
        Dictionary<bool, JObject> response = await connectionQuery.Request(url, operation, headers, String.Empty, response_required);
        
        return new Dictionary<bool, JObject> {
            {response.Keys.First(), response.Values.First()}
        };
    }

    public async Task<Dictionary<bool, JObject>> Disconnect(bool openIfExists)
    {
        ConnectionQuery connectionQuery = new ConnectionQuery();
        string command = config.api.delete_connection.command;
        string url = connectionQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, client, String.Empty);

        string header_type = config.api.delete_connection.header_type;       
        string header = config.api.delete_connection.header;    
        Dictionary<string, string> headers = connectionQuery.Build_headers(header_type, header);

        string operation = config.api.delete_connection.operation;     
        bool response_required = config.api.delete_connection.response_required; 
        Dictionary<bool, JObject> response = await connectionQuery.Request(url, operation, headers, String.Empty, response_required);
        
        return new Dictionary<bool, JObject> {
            {response.Keys.First(), response.Values.First()}
        };
    }

    public async Task<Dictionary<bool, JObject>> PublicationTopics(bool overrideIfExists)
    {
        PublicationQuery publicationQuery = new PublicationQuery();

        string command = config.api.set_publication.command;
        string url = publicationQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, String.Empty, $"clientName={client}&overwriteIfExists={overrideIfExists}");

        string header_type = config.api.set_publication.header_type;       
        string header = config.api.set_publication.header;    
        Dictionary<string, string> headers = publicationQuery.Build_headers(header_type, header);
        string payload = await publicationQuery.BuildPublicationPayload((JObject) this.vars["Model"]["publications"]);
        string operation = config.api.set_publication.operation;     
        bool response_required = config.api.set_publication.response_required;     
        Dictionary<bool, JObject> response = await publicationQuery.Request(url, operation, headers, payload, response_required);
        
        return new Dictionary<bool, JObject> {
            {response.Keys.First(), response.Values.First()}
        }; 
    }

    public async Task<Dictionary<bool, JArray>> GetSubscriptions()
    {
        SubscriptionQuery subscriptionQuery = new SubscriptionQuery();
        string command = config.api.get_subscriptions.command;
        string url = subscriptionQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, String.Empty, $"clientName={client}");

        string header_type = config.api.get_subscriptions.header_type;       
        string header = config.api.get_subscriptions.header;    
        Dictionary<string, string> headers = subscriptionQuery.Build_headers(header_type, header);
        string operation = config.api.get_subscriptions.operation;     
        bool response_required = config.api.get_subscriptions.response_required;     
        Dictionary<bool, JArray> response = await subscriptionQuery.GetRequest(url, operation, headers, String.Empty, response_required);
        
        return new Dictionary<bool, JArray> {
            {response.Keys.First(), response.Values.First()}
        };
    }

public async Task<Dictionary<bool, JObject>> DeleteSubscription(string topic)
    {
        SubscriptionQuery subscriptionQuery = new SubscriptionQuery();
        string command = config.api.delete_subscription.command;
        string url = subscriptionQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, topic, $"clientName={client}");

        string header_type = config.api.delete_subscription.header_type;
        string header = config.api.delete_subscription.header;    
        Dictionary<string, string> headers = subscriptionQuery.Build_headers(header_type, header);

        string operation = config.api.delete_subscription.operation;     
        bool response_required = config.api.delete_subscription .response_required;     
        Dictionary<bool, JObject> response = await subscriptionQuery.Request(url, operation, headers, String.Empty, response_required);
        
        return new Dictionary<bool, JObject> {
            {response.Keys.First(), response.Values.First()}
        };
    }

    public async Task<Dictionary<bool, JObject>> SubscriptionTopics(bool overrideIfExists)
    {
        SubscriptionQuery subscriptionQuery = new SubscriptionQuery();
        string command = config.api.set_subscription.command;
        string url = subscriptionQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, String.Empty, $"clientName={client}&overwriteIfExists={overrideIfExists}");

        string header_type = config.api.set_subscription.header_type;       
        string header = config.api.set_subscription.header;    
        Dictionary<string, string> headers = subscriptionQuery.Build_headers(header_type, header);
        string payload = await subscriptionQuery.BuildSubscriptionPayload((JObject) this.vars["Model"]["subscriptions"]);
        string operation = config.api.set_subscription.operation;     
        bool response_required = config.api.set_subscription.response_required;     
        Dictionary<bool, JObject> response = await subscriptionQuery.Request(url, operation, headers, payload, response_required);
        
        return new Dictionary<bool, JObject> {
            {response.Keys.First(), response.Values.First()}
        };
    }


    public async Task<Dictionary<bool, JArray>> Read(string topicName, List<string> tagNames)
    {
        SubscriptionReadQuery subscriptionReadQuery = new SubscriptionReadQuery();
        string command = config.api.set_subscription_read.command;
        string url = subscriptionReadQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, String.Empty, $"clientName={client}");

        string header_type = config.api.set_subscription_read.header_type;       
        string header = config.api.set_subscription_read.header;    
        Dictionary<string, string> headers = subscriptionReadQuery.Build_headers(header_type, header);
        string payload = await subscriptionReadQuery.BuildSubscriptionReadPayload(topicName, tagNames);
        string operation = config.api.set_subscription_read.operation;     
        bool response_required = config.api.set_subscription_read.response_required;     
        Dictionary<bool, JArray> response = await subscriptionReadQuery.Request(url, operation, headers, payload, response_required);
        
        return new Dictionary<bool, JArray> {
            {response.Keys.First(), response.Values.First()}
        }; 
    }


    public async Task<Dictionary<bool, JObject>> Publish(string topicName, string tagName, string type, object value, Quality quality, DateTime timestamp)
    {
        PublicationQuery publicationQuery = new PublicationQuery();
        string command = config.api.set_publication_publish.command;
        string url = publicationQuery.Build_url(api_protocol, api_host, api_port, api_suffix, command, String.Empty, $"clientName={client}");

        string header_type = config.api.set_publication_publish.header_type;       
        string header = config.api.set_publication_publish.header;    
        Dictionary<string, string> headers = publicationQuery.Build_headers(header_type, header);
        string payload = await publicationQuery.BuildPublicationPublishPayload(topicName, tagName, type, value, quality, timestamp);
        string operation = config.api.set_publication_publish.operation;     
        bool response_required = config.api.set_publication_publish.response_required;
        Dictionary<bool, JObject> response = await publicationQuery.Request(url, operation, headers, payload, response_required);
        
        return new Dictionary<bool, JObject> {
            {response.Keys.First(), response.Values.First()}
        }; 
    }
}

