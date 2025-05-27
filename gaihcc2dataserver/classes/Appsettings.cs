
using System.Runtime.CompilerServices;

namespace gaihcc2dataserver.classes
{
    public class AppSettings
    {
        public System system { get; set;}
        public AppParams app {get; set;}
        public EnvParams env {get; set;}
        public MiscParams misc {get; set;}
        public ApiParams api {get; set;}
        public GenParams parameters {get; set;}
    }

    public class System
    {
        public string appName { get; set; }
        public string version { get; set; }
        public string log_level { get; set; }
        public string variable_file { get; set; }
        public int buffer_size { get; set; }
    }
    public class AppParams
    {
        public string client { get; set; }
        public string api_host { get; set; }
        public string api_suffix { get; set; }
        public string api_protocol { get; set; }
        public string api_port { get; set; }
        public string time_period { get; set; }
        public string webhook_host { get; set; }
        public string webhook_protocol { get; set; }
        public string webhook_suffix { get; set; }
        public string webhook_port { get; set; }
        public string webhook_level { get; set; }
        public string on_time { get; set; }
        public string off_time { get; set; }
        public string interleave { get; set; }
        public string trend_port_anyip {get; set;}
    }
    public class EnvParams
    {
        public string client { get; set; }
        public string api_host { get; set; }
        public string api_suffix { get; set; }
        public string api_protocol { get; set; }
        public string api_port { get; set; }
        public string time_period { get; set; }
        public string on_time { get; set; }
        public string off_time { get; set; }
        public string interleave { get; set; }
        public string trend_port_anyip {get; set;}
    }

    public class MiscParams
    {
        public int retry_time {get; set;}
        public int dequeue_loop_time {get; set;}
    }
    public class ApiParam 
    {
        public string command {get; set;}
        public string operation {get; set;}
        public string header_type {get; set;}
        public string header {get; set;}
        public bool response_required {get; set;}
    }
    public class ApiParams 
    {
        public ApiParam set_connection {get; set;}
        public ApiParam get_all_connections {get; set;}
        public ApiParam delete_connection {get; set;}
        public ApiParam get_subscriptions {get; set;} 
        public ApiParam delete_subscription {get; set;} 
        public ApiParam set_subscription {get; set;} 
        public ApiParam set_publication {get; set;}
        public ApiParam set_subscription_read {get; set;}
        public ApiParam set_publication_publish {get; set;}
    }
    public class GenParams
    {
        public int threadpool_min_size {get; set;}
    }

}