using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Collections.Generic;

//using HCC2EventManager.Common.TagCacheHandlers;
using System.Text;
using Newtonsoft.Json.Linq;
using mtapiclient.classes;

namespace mtapiclient.common
{
    public static class CommonUtilities
    {
        private static Regex regexIPAddress = new Regex(@"(\d+)\.(\d+)\.(\d+)\.(\d+)", RegexOptions.Compiled);

        public static uint MakeIPAddress(string AIPAdressString)
        {
            uint ipaddress = 0;

            var matchIPAddress = regexIPAddress.Match(AIPAdressString);

            if (matchIPAddress.Success)
            {
                var part1 = Byte.TryParse(matchIPAddress.Groups[1].ToString(), out Byte val1) ? val1 : 0;
                var part2 = Byte.TryParse(matchIPAddress.Groups[2].ToString(), out Byte val2) ? val2 : 0;
                var part3 = Byte.TryParse(matchIPAddress.Groups[3].ToString(), out Byte val3) ? val3 : 0;
                var part4 = Byte.TryParse(matchIPAddress.Groups[4].ToString(), out Byte val4) ? val4 : 0;

                ipaddress = ((uint)part1 << 24) + ((uint)part2 << 16) + ((uint)part3 << 8) + (uint)part4;
            }

            return ipaddress;
        }

        public static void ReportThreadInfo()
        {
            ThreadPool.GetMinThreads(out int minThreads, out int minIOCThreads);
            ThreadPool.GetMaxThreads(out int maxThreads, out int maxIOCThreads);
            ThreadPool.GetAvailableThreads(out int availThreads, out int availIOCThreads);
            long queuedCount = ThreadPool.PendingWorkItemCount;

            int usedThreads = maxThreads - availThreads;
            int usedIOCThreads = maxIOCThreads - availIOCThreads;

            Logger.write(logLevel.debug, $"ThreadPool - min: {minThreads}, minIOC: {minIOCThreads} | max: {maxThreads}, maxIOC: {maxIOCThreads} | avail: {availThreads}, availIOC: {availIOCThreads} | queued: {queuedCount} | used: {usedThreads}, usedIOC: {usedIOCThreads}");

            Process.GetCurrentProcess().Refresh();
            Logger.write(logLevel.debug, $"ProcessThreadsCount: {Process.GetCurrentProcess().Threads.Count}");
        }

        public static List<string> RetrieveThreadInfo()
        {
            ThreadPool.GetMinThreads(out int minThreads, out int minIOCThreads);
            ThreadPool.GetMaxThreads(out int maxThreads, out int maxIOCThreads);
            ThreadPool.GetAvailableThreads(out int availThreads, out int availIOCThreads);
            long queuedCount = ThreadPool.PendingWorkItemCount;

            int usedThreads = maxThreads - availThreads;
            int usedIOCThreads = maxIOCThreads - availIOCThreads;

            Process.GetCurrentProcess().Refresh();

            List<string> list = new List<string>();

            list.Add("### Thread Info ###");

            list.Add("ThreadPool");
            list.Add($"min: {minThreads}, minIOC: {minIOCThreads}");
            list.Add($"max: {maxThreads}, maxIOC: {maxIOCThreads}");
            list.Add($"avail: {availThreads}, availIOC: {availIOCThreads}");
            list.Add($"used: {usedThreads}, usedIOC: {usedIOCThreads}");
            list.Add($"queued: {queuedCount}");
            list.Add("");
            list.Add($"ProcessThreadsCount: {Process.GetCurrentProcess().Threads.Count}");
            list.Add("");

            return list;

        }

        public static long DateTimeStringToHCC2Timestamp(string ATimestampStr)
        {
            DateTime timestampParsed = DateTime.TryParse(ATimestampStr, out DateTime ts) ? ts : DateTime.UnixEpoch;
            long result = DateTimeToHCC2Timestamp(timestampParsed);

            return result;
        }

        /// <summary>
        /// Convert DateTime parameter to UTC, then subtract UnixEpock, get TimeSpan since Jan 1, 1970 00:00:00.000Z.
        /// Convert TimeSpan to Ticks (100 ns steps), multiply by 10 to get microseconds.
        /// </summary>
        /// <param name="ATimestamp"></param>
        /// <returns></returns>
        public static long DateTimeToHCC2Timestamp(DateTime ATimestamp)
        {
            //DateTimeOffset time = ATimestamp;  // implicit conversion
            // long timestamp = time.ToUnixTimeMilliseconds() * 1000;  // microseconds since Jan 1, 1970 00:00:00.000Z

            TimeSpan unixtimespan = ATimestamp.ToUniversalTime() - DateTime.UnixEpoch;
            long timestamp = unixtimespan.Ticks / 10;  // Ticks are 100 ns; division by 10 yields microseconds

            return timestamp;
        }

        /// <summary>
        /// Convert microseconds to Ticks by multiplying by 10, then make a TimeSpan. 
        /// Add DateTime constant for UnixEpoch (Jan 1, 1970), make into a DateTimeOffset.
        /// Get the LocalDateTime from the DateTimeOffset.
        /// </summary>
        /// <param name="AMicroseconds"></param>
        /// <returns></returns>
        public static DateTime HCC2TimestampToDateTime(long AMicroseconds)
        {
            // long timestampusec = _owner.TimestampsMicroseconds[index];

            // long timestampmsec = timestampusec / 1000;
            // DateTimeOffset msgtime = DateTimeOffset.FromUnixTimeMilliseconds(timestampmsec);

            TimeSpan tsUnixEpoch = new TimeSpan(AMicroseconds * 10);  // from Ticks, which are 100ns time units (10 Ticks per 1 usec)
            DateTime exacttime = DateTime.UnixEpoch + tsUnixEpoch;
            DateTimeOffset msgtime = exacttime;  // implicit conversion

            return (msgtime.LocalDateTime);
        }

        public static string GetEnvVariableWithDefault(string AEnvVariableName, string ADefault)
        {
            string envVar = Environment.GetEnvironmentVariable(AEnvVariableName);

            if (envVar != null)
                return envVar;

            return ADefault;
        }

        /// <summary>
        /// Is this a TagName that is one of the sub-tags of a multi-tag topic?
        /// </summary>
        /// <param name="ATagName"></param>
        /// <returns></returns>
        public static bool IsCompositeTagName(string ATagName)
        {
            return (ATagName.IndexOf("|.") >= 0);
        }

        /// <summary>
        /// Pull out the part of the TagName that is up to the "|.".
        /// Return full string if pipe not found.
        /// </summary>
        /// <param name="ATagName"></param>
        /// <returns></returns>
        public static string ExtractMultiTagTopicName(string ATagName)
        {
            int pos = ATagName.IndexOf("|.");

            if (pos < 0)
                return ATagName;
            else
                return ATagName.Substring(0, pos + 2);
        }

        /// <summary>
        /// Pull out the part of the TagName that is after the "|.".
        /// Return empty string if pipe not found.
        /// Return empty string if pipe found but no characters after dot.
        /// </summary>
        /// <param name="ATagName"></param>
        /// <returns></returns>
        public static string ExtractMultiTagSubTagName(string ATagName)
        {
            int pos = ATagName.IndexOf("|.");

            if (pos < 0)
                return ATagName;
            else if (ATagName.Length == pos + 2)
                return ATagName;
            return ATagName.Substring(pos + 2, ATagName.Length - pos - 2);
        }

        public static HttpMethod GetHttpMethod(string operation)
        {
            switch (operation.ToUpper())
            {
                case "GET":
                    return HttpMethod.Get;
                case "PUT":
                    return HttpMethod.Put;
                case "POST":
                    return HttpMethod.Post;
                case "PATCH":
                    return HttpMethod.Patch;
                case "DELETE":
                    return HttpMethod.Delete;
                default:
                    throw new ArgumentException("Invalid HTTP operation");
            }
        }

        public static JObject ReadVars(string path)
        {
            string jsonContent = File.ReadAllText(path);
            JObject jsonObj = JObject.Parse(jsonContent);
            return jsonObj;
        }   
        public static List<(string, JObject)> GetJsonObjects(JObject jsonObj)
        {
            List<(string, JObject)> topicObjects = new List<(string, JObject)>();
            foreach (var topic in jsonObj.Properties())
            {
                topicObjects.Add((topic.Name, (JObject) topic.Value));
            }
            return topicObjects;
        }

        public static DateTime ConvertLocalTimeToUtc (DateTime dt)
        {
            return dt.ToUniversalTime();
        }
        public static string ConvertTimestampToString(DateTime timestamp)
        {
            return timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }
        
    }
}
