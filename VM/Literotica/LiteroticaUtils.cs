using Newtonsoft.Json;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace StoryManager.VM.Literotica
{
    public static class LiteroticaUtils
    {
        private static readonly ReadOnlyCollection<string> StoryUriFormats = new List<string>()
        {
            // https://www.literotica.com/s/{title}
            @"^(?i)(https?:\/\/)?(www\.)?literotica\.com\/s\/(?-i)(?<Value>[^\?]+)\.*$",
            // https://www.literotica.com/stories/showstory.php?url={title}
            @"^(?i)(https?:\/\/)?(www\.)?literotica\.com\/stories\/showstory\.php\?url=(?-i)(?<Value>[^\?]+)\.*$",
            // https://i.literotica.com/stories/showstory.php?id={id}
            @"^(?i)(https?:\/\/)?i\.literotica\.com\/stories\/showstory\.php\?(?-i)id=(?<Value>[^\?]+)\.*$",
        }.AsReadOnly();

        /// <summary>Attempts to parse the url-encoded title from the given literotica story <paramref name="url"/><para/>
        /// <see href="https://www.literotica.com/s/accidents-happen-1"/> --> "accidents-happen-1"<br/>
        /// <see href="https://www.literotica.com/stories/showstory.php?url=accidents-happen-1"/> --> "accidents-happen-1"</summary>
        public static bool TryGetStoryTitle(string url, out string title)
        {
            if (string.IsNullOrEmpty(url))
            {
                title = null;
                return false;
            }

            foreach (string format in StoryUriFormats.Take(StoryUriFormats.Count - 1)) // the last format specifies an id instead of a title
            {
                if (Regex.IsMatch(url, format))
                {
                    title = Regex.Match(url, format).Groups["Value"].Value;
                    return true;
                }
            }

            title = null;
            return false;
        }

        public static bool TryGetAPIUri(string url, out string result)
        {
            if (string.IsNullOrEmpty(url))
            {
                result = null;
                return false;
            }

            url = url.Trim();
            if (!url.Contains(".php", StringComparison.CurrentCultureIgnoreCase))
                url = new Uri(url).GetLeftPart(UriPartial.Path);

            foreach (string Pattern in StoryUriFormats)
            {
                if (Regex.IsMatch(url, Pattern))
                {
                    string Value = Regex.Match(url, Pattern).Groups["Value"].Value;
                    result = $"https://www.literotica.com/api/3/stories/{Value}";
                    return true;
                }
            }

            result = null;
            return false;
        }

        public static string GetAPIUri(string url)
        {
            if (!TryGetAPIUri(url, out string result))
                throw new InvalidOperationException($"Invalid literotica story url: {url}");
            return result;
        }

        public static async Task<SerializableStory> DownloadStory(string url, CancellationToken? ct = null)
        {
            string APIUrl = GetAPIUri(url);
            string Json = await GeneralUtils.ExecuteGetAsync(APIUrl, ct ?? CancellationToken.None);
            LiteroticaPage InitialPage = GeneralUtils.DeserializeJson<LiteroticaPage>(Json);

            List<string> ChapterUrls = InitialPage.chapterFirstPageAPIUrls.ToList();
            List<Task<SerializableChapter>> ChapterTasks = ChapterUrls.Select(x => DownloadChapter(x)).ToList();
            await Task.WhenAll(ChapterTasks);
            List<SerializableChapter> Chapters = ChapterTasks.Select(x => x.Result).ToList();
            List<SerializablePage> Pages = Chapters.SelectMany(x => x.Pages).ToList();

            SerializableStory Story = new(InitialPage, Chapters);

            return Story;
        }

        public static async Task<SerializableChapter> DownloadChapter(string url)
        {
            string Json = await GeneralUtils.ExecuteGetAsync(url);
            LiteroticaPage FirstPage = GeneralUtils.DeserializeJson<LiteroticaPage>(Json);
            List<LiteroticaPage> Pages = new() { FirstPage };
            bool IsMultiPage = FirstPage.meta.pages_count > 1;
            if (IsMultiPage)
            {
                //  Download each page individually with ?params={"contentPage":pageNumber} queryString parameters
                List<string> PageUrls = new();
                for (int PageNumber = 2; PageNumber <= FirstPage.meta.pages_count; PageNumber++)
                {
                    string ParamsValue = HttpUtility.UrlEncode($"{{\"contentPage\":{PageNumber}}}");
                    string QueryString = $"?params={ParamsValue}";
                    string PageUrl = $"{GetAPIUri(FirstPage.submission.fullUrl)}{QueryString}";
                    PageUrls.Add(PageUrl);
                }
                List<Task<string>> PageTasks = PageUrls.Select(x => GeneralUtils.ExecuteGetAsync(x)).ToList();
                await Task.WhenAll(PageTasks);
                Pages.AddRange(PageTasks.Select(x => GeneralUtils.DeserializeJson<LiteroticaPage>(x.Result)));
            }

            SerializableChapter Chapter = new(Pages);
            return Chapter;
        }

        /// <param name="query">Do not use a query that has already been url-encoded</param>
        public static async Task<LiteroticaSearchResults> SearchStories(string query, int pageNumber, CancellationToken? ct = null)
        {
            const string endpoint = @"https://www.literotica.com/api/3/search/stories";
            object queryParams = new
            {
                q = query,
                page = pageNumber,
                languages = new int[] { 1 }
            };
            string queryString = $"?params={HttpUtility.UrlEncode(GeneralUtils.SerializeJson(queryParams, false))}";
            string url = $"{endpoint}{queryString}";

            string Json = await GeneralUtils.ExecuteGetAsync(url, ct ?? CancellationToken.None);
            LiteroticaSearchResults result = GeneralUtils.DeserializeJson<LiteroticaSearchResults>(Json);
            return result;
        }
    }
}
