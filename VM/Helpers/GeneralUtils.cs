using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Converters;
using System.Diagnostics;
using System.Windows.Media;
using Microsoft.Win32;
using System.Security.Policy;

namespace StoryManager.VM.Helpers
{
    public static class GeneralUtils
    {
        public static Task<string> ExecuteGetAsync(string url)
            => ExecuteGetAsync(url, CancellationToken.None);
        public static async Task<string> ExecuteGetAsync(string url, CancellationToken ct)
        {
            HttpClientHandler Handler = new() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };
            using (HttpClient Client = new(Handler))
            {
                HttpResponseMessage Response = await Client.GetAsync(url, ct);
                Response.EnsureSuccessStatusCode();
                string Result = await Response.Content.ReadAsStringAsync();
                return Result;
            }
        }

        public static string SerializeJson<T>(T obj, bool indented)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = indented ? Formatting.Indented : Formatting.None;
            serializer.NullValueHandling = NullValueHandling.Ignore;
            //serializer.DefaultValueHandling = DefaultValueHandling.Ignore;

            using (StringWriter stringOutput = new StringWriter())
            {
                using (JsonWriter writer = new JsonTextWriter(stringOutput))
                {
                    serializer.Serialize(writer, obj);
                    return stringOutput.ToString();
                }
            }
        }

        public static T DeserializeJson<T>(string json)
        {
            var resolver = new DefaultContractResolver(); // Cache for performance
            var settings = new JsonSerializerSettings
            {
                ContractResolver = resolver,
                Converters = { new IgnoreUnexpectedArraysConverter(resolver) },
            };
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public static string ToSafePathName(string path)
        {
            string safePath = path;
            foreach (var c in Path.GetInvalidPathChars())
            {
                safePath = safePath.Replace(c, '-');
            }
            return safePath;
        }

        /// <param name="allowPeriods">If <see langword="true"/>, '.' characters will also be replaced. Periods are not valid in Windows folder names, but are valid in filenames</param>
        public static string ToSafeFilename(string filename, bool allowPeriods = true)
        {
            string safeFilename = filename;
            foreach (var c in Path.GetInvalidFileNameChars())
                safeFilename = safeFilename.Replace(c, '-');
            if (!allowPeriods)
                safeFilename = safeFilename.Replace('.', '-');
            return safeFilename;
        }

        public static string Truncate(this string str, int maxLength, bool useEllipsisIfTruncated)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            else if (str.Length <= maxLength)
                return str;
            else
                return $"{str.Substring(0, maxLength)}{(useEllipsisIfTruncated ? "..." : "")}";
        }

        public static void ShellExecute(string Filename)
        {
            try
            {
                var processInfo = new ProcessStartInfo() { FileName = Filename, UseShellExecute = true };
                using (Process.Start(processInfo)) { }
            }
            catch { }
        }

        /// <param name="PreferIncognito">If <see langword="true"/>, the given <paramref name="Url"/> will be opened in incognito mode if the user's default web browser is Google Chrome.</param>
        public static void OpenUrl(string Url, bool PreferIncognito)
        {
            try
            {
                if (PreferIncognito)
                {
                    string DefaultBrowser = GetSystemDefaultBrowser();
                    if (DefaultBrowser.Contains("chrome", StringComparison.CurrentCultureIgnoreCase))
                    {
                        using (var process = new Process())
                        {
                            process.StartInfo.FileName = DefaultBrowser;
                            process.StartInfo.Arguments = Url + " --incognito";
                            process.Start();
                        }

                        return;
                    }
                }

                ShellExecute(Url);
            }
            catch { }
        }

        public static string GetRGBAHexString(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

        private static string CachedSystemDefaultBrowser = null;
        //Adapted from: https://stackoverflow.com/a/62006560
        /// <summary>Returns the full path to the system-default browser .exe</summary>
        public static string GetSystemDefaultBrowser()
        {
            if (CachedSystemDefaultBrowser != null)
                return CachedSystemDefaultBrowser;

            string name = "";
            RegistryKey regKey = null;

            try
            {
                var regDefault = Registry.CurrentUser.OpenSubKey(Path.Combine("Software", "Microsoft", "Windows", "CurrentVersion", "Explorer", "FileExts", ".htm", "UserChoice"), false);
                var stringDefault = regDefault.GetValue("ProgId") as string;

                regKey = Registry.ClassesRoot.OpenSubKey(Path.Combine(stringDefault, "shell", "open", "command"), false);
                name = regKey.GetValue(null).ToString().ToLower().Replace("\"", "");

                if (!name.EndsWith("exe"))
                    name = name.Substring(0, name.LastIndexOf(".exe") + 4);

            }
            catch (Exception ex)
            {
                name = $"ERROR: An exception of type: {ex.GetType()} occurred in method: {ex.TargetSite}";
            }
            finally { regKey?.Close(); }

            CachedSystemDefaultBrowser = name;
            return name;
        }
    }
}
