using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using mtapiclient.common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace mtapiclient.classes;

public class ApiClient
{
    public string client {get;}
    private string api_host {get; set;}
    private int api_port {get; set;}
    private string api_protocol {get; set;}
    private string api_suffix {get; set;}
    private string wh_host {get; set;}
    private int wh_port {get; set;}
    private string wh_protocol {get; set;}
    private string wh_suffix {get; set;}
    private int time_period {get; set;}
    private AppSettings config {get; set;}
    public JObject vars {get; set;}
    private object forward_vars {get; set;}
    private object reverse_vars {get; set;}
    private bool isConnected {get; set;}
    private object webhook_queue {get; set;}
    private object logger {get; set;}

    public ApiClient(JObject vars, AppSettings config)
    {
        //
        // Initialize internal variables
        //
        this.vars = vars;
        this.isConnected = false;
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
        this.wh_host =  CommonUtilities.GetEnvVariableWithDefault(config.env.webhook_host, config.app.webhook_host);
        this.wh_protocol = CommonUtilities.GetEnvVariableWithDefault(config.env.webhook_protocol, config.app.webhook_protocol);
        this.wh_suffix = CommonUtilities.GetEnvVariableWithDefault(config.env.webhook_suffix, config.app.webhook_suffix);
        this.wh_port = Convert.ToInt32(CommonUtilities.GetEnvVariableWithDefault(config.env.webhook_port, config.app.webhook_port));
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

