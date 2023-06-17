namespace BitDB;

public static class Details
{
    public static string Version => "1.2.0";
    public static string Definition => "Beta";
    public static string FullVersion => Definition != "" ? Version + "-" + Definition : Version;
    public static string CountVersion => "3";
    public static string MajorVersion => "1";
    public static string MinorVersion => "2";
    public static string TweaksVersion => "0";
}
