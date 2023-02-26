using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Unicode;

public class IniHelper
{
    static readonly string CfgPath = AppDomain.CurrentDomain.BaseDirectory + "Data\\config.ini";
    static readonly string DefaultSection = "Default";

    public static string GetValue(string key) { return GetValue(DefaultSection, key); }
    public static string GetValue(string sectionName, string key)
    {
        CheckPath();
        var array = new byte[2048];
        var privateProfileString = GetPrivateProfileString(sectionName, key, "Error", array, 999, CfgPath);
        var foo = Encoding.Default.GetString(array, 0, privateProfileString);
        return foo.Equals("Error") ? null : foo;
    }

    public static bool SetValue(string key, string value) { return SetValue(DefaultSection, key, value); }
    public static bool SetValue(string sectionName, string key, string value)
    {
        CheckPath();
        bool result;
        try
        {
            result = (int)WritePrivateProfileString(sectionName, key, value, CfgPath) > 0;
        }
        catch (Exception ex)
        {
            throw ex;
        }
        return result;
    }

    public static bool RemoveSection(string sectionName, string filePath)
    {
        bool result;
        try
        {
            result = ((int)WritePrivateProfileString(sectionName, null, "", filePath) > 0);
        }
        catch (Exception ex)
        {
            throw ex;
        }
        return result;
    }

    public static bool RemoveKey(string sectionName, string key, string filePath)
    {
        bool result;
        try
        {
            result = ((int)WritePrivateProfileString(sectionName, key, null, filePath) > 0);
        }
        catch (Exception ex)
        {
            throw ex;
        }
        return result;
    }

    public static void CheckPath()
    {
        if (!Directory.Exists(CfgPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CfgPath));
        }
        if (!File.Exists(CfgPath))
        {
            File.Create(CfgPath);
        }
    }

    [System.Runtime.InteropServices.DllImport("kernel32")]
    private static extern int GetPrivateProfileString(string section, string key, string def, byte[] returnBuffer, int size, string filePath);

    [System.Runtime.InteropServices.DllImport("kernel32")]
    private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
}
