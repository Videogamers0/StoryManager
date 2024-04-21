using Newtonsoft.Json;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace StoryManager.VM.Literotica
{
    public static class LiteroticaUtils
    {
        private static readonly ReadOnlyCollection<string> Localizations = new List<string>() {
            "german", "dutch", "spanish", "french", "italian", "romanian", "portuguese", "other"
        }.AsReadOnly();

        private static readonly ReadOnlyCollection<string> StoryUriFormats = new List<string>()
        {
            // https://www.literotica.com/s/{title}
            @"^(?i)(https?:\/\/)?(www\.)?literotica\.com\/s\/(?-i)(?<Value>[^\?]+).*$",
            // https://www.{language}.literotica.com/s/{title}
            $@"^(?i)(https?:\/\/)?(www\.)?({string.Join("|", Localizations.Select(Regex.Escape))})\.literotica\.com\/s\/(?-i)(?<Value>[^\?]+).*$",
            // https://www.literotica.com/stories/showstory.php?url={title}
            @"^(?i)(https?:\/\/)?(www\.)?literotica\.com\/stories\/showstory\.php\?url=(?-i)(?<Value>[^\?&]+).*$",
            // https://i.literotica.com/stories/showstory.php?id={id}
            @"^(?i)(https?:\/\/)?i\.literotica\.com\/stories\/showstory\.php\?(?-i)id=(?<Value>[^\?&]+).*$",
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

        //Taken from: https://stackoverflow.com/a/24087164/11689514
        private static List<List<T>> ChunkBy<T>(List<T> source, int chunkSize)
        {
            return source
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public static async Task<SerializableStory> DownloadStory(string url, CancellationToken? ct = null)
        {
            string APIUrl = GetAPIUri(url);
            string Json = await GeneralUtils.ExecuteGetAsync(APIUrl, ct ?? CancellationToken.None);
            LiteroticaPage InitialPage = GeneralUtils.DeserializeJson<LiteroticaPage>(Json);

            List<string> ChapterUrls = InitialPage.chapterFirstPageAPIUrls.ToList();

#if NEVER
            List<Task<SerializableChapter>> ChapterTasks = ChapterUrls.Select(x => DownloadChapter(x)).ToList();
            await Task.WhenAll(ChapterTasks);
#else
            //  Stories with a large # of chapters tend to throw an error while downloading:
            //  ===========================================================================================================================================
            //  System.Net.Http.HttpRequestException: The SSL connection could not be established, see inner exception.
            //  ---> System.IO.IOException: Unable to write data to the transport connection: An existing connection was forcibly closed by the remote host.
            //  ---> System.Net.Sockets.SocketException (100054): An existing connection was forcibly closed by the remote host.
            //  ===========================================================================================================================================
            //  To avoid this, download the story in smaller batches

            const int MaxConnections = 4;
            List<Task<SerializableChapter>> ChapterTasks = new();
            List<List<string>> ChapterChunks = ChunkBy(ChapterUrls, MaxConnections);
            foreach (var Chunk in ChapterChunks)
            {
                async Task DoWork()
                {
                    List<Task<SerializableChapter>> ChunkTasks = Chunk.Select(DownloadChapter).ToList();
                    await Task.WhenAll(ChunkTasks);
                    ChapterTasks.AddRange(ChunkTasks);
                    await Task.Delay(TimeSpan.FromSeconds(0.1));
                }

                const int MaxRetries = 3;
                int CurrentAttempt = 1;
                bool Success = false;
                while (!Success && CurrentAttempt <= MaxRetries)
                {
                    try
                    {
                        await DoWork();
                        Success = true;
                    }
                    catch (System.Net.Http.HttpRequestException ex)
                    {
                        if (ex.InnerException is IOException InnerIOException && InnerIOException.InnerException is SocketException)
                        {
                            //  I don't know why but it still occassionally throws the SocketException. Wait a few seconds and try again...
                            TimeSpan Delay = CurrentAttempt <= 1 ? TimeSpan.FromSeconds(5) : TimeSpan.FromSeconds(30);
                            await Task.Delay(Delay);
                            CurrentAttempt++;
                        }
                        else
                            break;
                    }
                }
            }
#endif
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

        /// <param name="legacyFormat">If <see langword="true"/>, the returned url will be in the older format: 
        /// <code>https://www.literotica.com/stories/memberpage.php?uid={Author.userid}&amp;page=submissions</code><para/>
        /// If <see langword="false"/>, the returned url will be in the newer format: 
        /// <code>https://www.literotica.com/authors/{Author.username}/works/stories</code></param>
        /// <param name="combinedChapters">Only relevant if <paramref name="legacyFormat"/> is <see langword="true"/>. 
        /// If <see langword="true"/>, stories from the same series will be combined into a single entry. (Sets queryParam 'listType' to 'combined')</param>
        /// <param name="userId">Only required if <paramref name="legacyFormat"/> is <see langword="true"/></param>
        /// <param name="username">Only required if <paramref name="legacyFormat"/> is <see langword="false"/></param>
        public static string GetAuthorUrl(bool legacyFormat, int userId, string username, bool combinedChapters = true)
        {
            if (legacyFormat)
                return $"https://www.literotica.com/stories/memberpage.php?uid={userId}&page=submissions";
            else
            {
                string url = $"https://www.literotica.com/authors/{username}/works/stories";
                if (combinedChapters)
                    url += "?listType=combined";
                return url;
            }
        }
    }
}
