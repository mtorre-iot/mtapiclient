namespace mtapiclient.classes;

public enum Quality
{
    Bad = 0,
    CommunicationFailure = 24,
    SetItemInactive = 28,
    Stale = 64,
    MinimumOutOfRange = 65,
    MaximumOutOfRange = 66,
    Frozen = 67,
    InvalidFactorOffset = 68,
    Good = 192,
    Good_LocalOverride = 216,
    Good_LocalConstant = 219
}

public static class QualityConv
{
    public static Dictionary<string, Quality> GetQualityDict()
    {
        return new Dictionary<string, Quality>() 
        { 
            {"BAD" , Quality.Bad},
            {"GOOD", Quality.Good},
            {"COMMFAILURE", Quality.CommunicationFailure},
            {"SETITEMINACTIVE", Quality.SetItemInactive},
            {"STALE", Quality.Stale},
            {"MINOUTOFRANGE", Quality.MinimumOutOfRange},
            {"MAXOUTOFRANGE", Quality.MaximumOutOfRange},
            {"FROZEN", Quality.Frozen},
            {"INVALIDFACTOROFFSET", Quality.InvalidFactorOffset},
            {"GOODLOCALOVERRIDE", Quality.Good_LocalOverride},
            {"GOODLOCALCONSTANT", Quality.Good_LocalConstant}
        };
    }
    public static string ConvertTagQualityToString(Quality quality)
    {
        string rtn = "BAD";
        var qualityDict = GetQualityDict();
        foreach (var kvp in qualityDict)
        {
            if (kvp.Value == quality)
            {
                rtn = kvp.Key;
            }
        }
        return rtn;
    }

    public static int ConvertQualityToInt(Quality quality)
    {
        return (int)quality;
    }
}        
    
