using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace gaihcc2dataserver.classes;

public class JsonArrayChecker
{
    public static bool IsJsonArray(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }
        try
        {
            var token = JToken.Parse(input);
            return token.Type == JTokenType.Array;
        }
        catch (JsonReaderException)
        {
            // Not valid JSON
            return false;
        }
    }
}
