using Newtonsoft.Json;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM.Literotica
{
    public class LiteroticaPage
    {
        public LiteroticaChapterMeta meta { get; set; }
        public LiteroticaSubmission submission { get; set; }
        public string pageText { get; set; }

        [JsonIgnore]
        public IEnumerable<string> chapterFirstPageUrls
        {
            get
            {
                if (submission.series == null)
                {
                    yield return submission.fullUrl;
                    yield break;
                }

                //  Key = chapter id, Value = the chapter's index
                Dictionary<int, int> orderLookup = new();
                for (int i = 0; i < submission.series.meta.order.Count; i++)
                    orderLookup.Add(submission.series.meta.order[i], i);

                foreach (LiteroticaSeriesItem item in submission.series.items.OrderBy(x => orderLookup[x.id]))
                {
                    yield return item.fullUrl;
                }
            }
        }

        [JsonIgnore]
        public IEnumerable<string> chapterFirstPageAPIUrls => chapterFirstPageUrls.Select(x => LiteroticaUtils.GetAPIUri(x));

        public override string ToString() => $"{nameof(LiteroticaPage)}: {pageText?.Truncate(50, true)}";
    }

    public class LiteroticaChapterMeta
    {
        public int pages_count { get; set; }

        public override string ToString() => $"{nameof(LiteroticaChapterMeta)}: {pages_count}";
    }

    public class LiteroticaSubmission
    {
        public LiteroticaAuthor author { get; set; }
        public string authorname { get; set; }
        public LiteroticaCategory category_info { get; set; }
        public int category { get; set; }
        public int comment_count { get; set; }
        public int contest_winner { get; set; }
        public string date_approve { get; set; }
        public string description { get; set; }
        public int favorite_count { get; set; }
        public int id { get; set; }
        public bool is_hot { get; set; }
        public bool is_new { get; set; }
        public double? rate_all { get; set; }
        public int reading_lists_count { get; set; }
        public List<LiteroticaTag> tags { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string url { get; set; }
        public int view_count { get; set; }
        public bool writers_pick { get; set; }
        public int series_count { get; set; }
        [JsonIgnore]
        public bool IsInSeries => series_count > 1;
        [JsonConverter(typeof(IgnoreUnexpectedArraysConverter<LiteroticaSeries>))]
        public LiteroticaSeries series { get; set; }
        public int words_count { get; set; }

        [JsonIgnore]
        public string fullUrl => $@"https://www.literotica.com/s/{url}";

        public override string ToString() => $"{title} - {description}";
    }

    public class LiteroticaAuthor
    {
        public string biography { get; set; }
        public string joindate { get; set; }
        public int stories_count { get; set; }
        public int submissions_count { get; set; }
        public int userid { get; set; }
        public string username { get; set; }

        public override string ToString() => $"{nameof(LiteroticaAuthor)}: {username} ({userid})";
    }

    public class LiteroticaCategory
    {
        public string type { get; set; }
        public string pageUrl { get; set; }

        public override string ToString() => $"{nameof(LiteroticaCategory)}: {type},{pageUrl}";
    }

    public class LiteroticaTag
    {
        public string id { get; set; }
        public string tag { get; set; }
        public string is_banned { get; set; }

        public override string ToString() => $"{nameof(LiteroticaTag)}: {tag}";
    }

    public class LiteroticaSeries
    {
        public LiteroticaSeriesMeta meta { get; set; }
        public List<LiteroticaSeriesItem> items { get; set; }
    }

    public class LiteroticaSeriesMeta
    {
        public string id { get; set; }
        public string title { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
        public List<int> order { get; set; }

        public override string ToString() => $"{nameof(LiteroticaSeriesMeta)}: {title} ({id})";
    }

    public class LiteroticaSeriesItem
    {
        public int id { get; set; }
        public LiteroticaCategory category_info { get; set; }
        public int category { get; set; }
        public string title { get; set; }
        public string type { get; set; }
        public string url { get; set; }

        [JsonIgnore]
        public string fullUrl => $@"https://www.literotica.com/s/{url}";

        public override string ToString() => $"{title} ({id}|{url})";
    }

    public class LiteroticaSearchResults
    {
        public LiteroticaSearchResultsMeta meta { get; set; }
        public List<LiteroticaSubmission> data { get; set; }
    }

    public class LiteroticaSearchResultsMeta
    {
        public int pageSize { get; set; }
        public int total { get; set; }
    }
}
