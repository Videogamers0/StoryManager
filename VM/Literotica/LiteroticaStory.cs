using Prism.Commands;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace StoryManager.VM.Literotica
{
    //ViewModel for literotica stories
    public class LiteroticaStory : ViewModelBase
    {
        public const string SummaryFilename = "story-metadata.json";

        public MainViewModel MVM { get; }
        public AuthorGroup Group { get; }
        public SerializableStory Summary { get; }
        public int WordCount { get; }

        private string _FolderPath;
        public string FolderPath
        {
            get => _FolderPath;
            set
            {
                if (_FolderPath != value)
                {
                    _FolderPath = value;
                    NPC(nameof(FolderPath));
                    NPC(nameof(RelativeFolderPath));
                    NPC(nameof(IsSaved));
                }
            }
        }

        public string RelativeFolderPath
        {
            get
            {
                //Taken from: https://stackoverflow.com/a/1766773
                Uri path1 = new(MVM.Settings.StoriesDirectory);
                Uri path2 = new(FolderPath);
                string escapedRelativePath = path1.MakeRelativeUri(path2).OriginalString;
                string relativePath = Uri.UnescapeDataString(escapedRelativePath);
                return relativePath;
            }
        }

        public bool IsSaved => !string.IsNullOrEmpty(FolderPath);

        /// <summary>Only valid if <see cref="IsSaved"/> is <see langword="true"/></summary>
        public string SummaryFilePath => Path.Combine(FolderPath, SummaryFilename);
        /// <summary>Only valid if <see cref="IsSaved"/> is <see langword="true"/></summary>
        public string StoryJsonFilePath => Path.Combine(FolderPath, "story.json");
        /// <summary>Only valid if <see cref="IsSaved"/> is <see langword="true"/></summary>
        public string StoryHtmlFilePath => Path.Combine(FolderPath, "story.html");

        public ReadOnlyCollection<LiteroticaCategory> Categories { get; }
        public LiteroticaCategory PrimaryCategory => Categories.First();

        public bool IsSelected => MVM.SelectedStory == this;

        private SerializableStory _Story;
        public SerializableStory Story
        {
            get => _Story;
            private set
            {
                if (_Story != value)
                {
                    _Story = value;
                    NPC(nameof(Story));
                    NPC(nameof(PageCount));

                    //  Initialize the buttons that are used to navigate to particular chapters/pages
                    if (Story == null)
                    {
                        ChapterNavButtons = new List<StoryNavigationButton>().AsReadOnly();
                        PageNavButtons = new List<StoryNavigationButton>().AsReadOnly();
                    }
                    else
                    {
                        List<StoryNavigationButton> TempChapterButtons = new();
                        List<StoryNavigationButton> TempPageButtons = new();

                        int OverallPageIndex = 0;
                        for (int ChapterIndex = 0; ChapterIndex < Story.Chapters.Count; ChapterIndex++)
                        {
                            SerializableChapter Chapter = Story.Chapters[ChapterIndex];
                            for (int PageIndex = 0; PageIndex < Chapter.Pages.Count; PageIndex++)
                            {
                                SerializablePage Page = Chapter.Pages[PageIndex];
                                StoryNavigationButton NavButton = new(this, Chapter, Page, ChapterIndex, PageIndex, OverallPageIndex);
                                TempPageButtons.Add(NavButton);
                                if (PageIndex == 0)
                                    TempChapterButtons.Add(NavButton);
                                OverallPageIndex++;
                            }
                        }

                        ChapterNavButtons = TempChapterButtons.AsReadOnly();
                        PageNavButtons = TempPageButtons.AsReadOnly();
                    }
                }
            }
        }

        public string DateApprovedString => Summary.Chapters.First().DateApproved;
        public DateTime? DateApproved { get; }
        public DateTime DownloadedAt => Summary.DownloadedAt;

        public string AuthorName => Summary.Author.username;
        public string Title => Summary.Title;
        public string FullDescription => string.Join("\n", Summary.Chapters.Select((x, index) => $"Ch{(index + 1):00}: {x.Description}"));
        public int ChapterCount => Summary.Chapters.Count;
        public int PageCount => Story?.Chapters.Sum(x => x.Pages.Count) ?? 0;
        public double AverageRating
        {
            get
            {
                List<SerializableChapter> RatedChapters = Summary.Chapters.Where(x => x.Rating.HasValue).ToList();
                if (!RatedChapters.Any())
                    return 0;
                else
                    return RatedChapters.Sum(x => x.Rating.Value) / RatedChapters.Count;
            }
        }
        public bool HasOverallRating => Summary.Chapters.Any(x => x.Rating.HasValue);


        private ReadOnlyCollection<StoryNavigationButton> _ChapterNavButtons;
        public ReadOnlyCollection<StoryNavigationButton> ChapterNavButtons
        {
            get => _ChapterNavButtons;
            private set
            {
                if (_ChapterNavButtons != value)
                {
                    _ChapterNavButtons = value;
                    NPC(nameof(ChapterNavButtons));
                    NPC(nameof(HasMultipleChapters));
                }
            }
        }

        private ReadOnlyCollection<StoryNavigationButton> _PageNavButtons;
        public ReadOnlyCollection<StoryNavigationButton> PageNavButtons
        {
            get => _PageNavButtons;
            private set
            {
                if (_PageNavButtons != value)
                {
                    _PageNavButtons = value;
                    NPC(nameof(PageNavButtons));
                    NPC(nameof(HasMultiplePages));
                }
            }
        }

        public bool HasMultipleChapters => (ChapterNavButtons?.Count ?? 0) > 1;
        public bool HasMultiplePages => (PageNavButtons?.Count ?? 0) > 1;

        public Task TryScrollToChapter(int ChapterIndex) => MVM.ScrollToElementAsync(GetChapterId(ChapterIndex));
        public Task TryScrollToChapterAndPage(int ChapterIndex, int PageIndex) => MVM.ScrollToElementAsync(GetChapterAndPageId(ChapterIndex, PageIndex));

        /// <summary>Only intended to be used by data-binding on the UI. To get/set this value programmatically, use <see cref="UserRating"/> instead.</summary>
        public double BindableUserRating
        {
            get => UserRating.HasValue ? UserRating.Value : 0;
            set => UserRating = value;
        }

        private double? _UserRating;
        public double? UserRating
        {
            get => _UserRating;
            set
            {
                if (_UserRating != value)
                {
                    _UserRating = value;
                    NPC(nameof(UserRating));
                    NPC(nameof(BindableUserRating));
                    NPC(nameof(IsRated));
                }
            }
        }

        private string _UserNotes;
        public string UserNotes
        {
            get => _UserNotes;
            set
            {
                if (_UserNotes != value)
                {
                    _UserNotes = value;
                    NPC(nameof(UserNotes));
                }
            }
        }

        public bool IsRated => UserRating != null;

        /// <summary><seealso langword="true"/> if the author of this story is favorited. See also: <see cref="IsStoryFavorited"/>, <see cref="AuthorGroup.IsFavorited"/></summary>
        public bool IsAuthorFavorited => Group.IsFavorited;

        private bool _IsStoryFavorited;
        /// <summary><seealso langword="true"/> if this story is favorited. See also: <see cref="IsAuthorFavorited"/></summary>
        public bool IsStoryFavorited
        {
            get => _IsStoryFavorited;
            set
            {
                if (_IsStoryFavorited != value)
                {
                    _IsStoryFavorited = value;
                    NPC(nameof(IsStoryFavorited));
                }
            }
        }

        public DelegateCommand<object> AddToFavorites => new(_ => IsStoryFavorited = true);
        public DelegateCommand<object> RemoveFromFavorites => new(_ => IsStoryFavorited = false);

        private bool _IsIgnored;
        public bool IsIgnored
        {
            get => _IsIgnored;
            set
            {
                if (_IsIgnored != value)
                {
                    _IsIgnored = value;
                    NPC(nameof(IsIgnored));
                    UpdateVisibility();
                }
            }
        }

        public DelegateCommand<object> MarkAsIgnored => new(_ => IsIgnored = true);
        public DelegateCommand<object> UnmarkAsIgnored => new(_ => IsIgnored = false);

        private bool _IsRead;
        public bool IsRead
        {
            get => _IsRead;
            set
            {
                if (_IsRead != value)
                {
                    _IsRead = value;
                    NPC(nameof(IsRead));
                }
            }
        }

        public DelegateCommand<object> MarkAsRead => new(_ => IsRead = true);
        public DelegateCommand<object> MarkAsUnread => new(_ => IsRead = false);

        private DateTime? _QueuedAt;
        /// <summary>The <see cref="DateTime"/> that this story was added to the user's "Read-later" list, or null if this story is not on their "Read-later" list.</summary>
        public DateTime? QueuedAt
        {
            get => _QueuedAt;
            set
            {
                if (_QueuedAt != value)
                {
                    _QueuedAt = value;
                    NPC(nameof(QueuedAt));
                    NPC(nameof(IsQueued));
                    OnIsQueuedChanged?.Invoke(this, IsQueued);
                }
            }
        }

        public bool IsQueued => QueuedAt.HasValue;

        public event EventHandler<bool> OnIsQueuedChanged;

        public DelegateCommand<object> MarkAsQueued => new(_ => QueuedAt = DateTime.Now);
        public DelegateCommand<object> UnmarkAsQueued => new(_ => QueuedAt = null);

        public DateTime? LastOpenedAt { get; set; }

        private Bookmark _RecentPosition;
        public Bookmark RecentPosition
        {
            get => _RecentPosition;
            set
            {
                if (_RecentPosition != value)
                {
                    _RecentPosition = value;
                    NPC(nameof(RecentPosition));
                }
            }
        }

        private bool _IsVisible;
        public bool IsVisible
        {
            get => _IsVisible;
            private set
            {
                if (_IsVisible != value)
                {
                    _IsVisible = value;
                    NPC(nameof(IsVisible));
                }
            }
        }

        public void UpdateVisibility()
        {
            FilterSettings Settings = MVM.Settings.DisplaySettings.FilterSettings;
            IsVisible =
                (!Settings.HideRead || !IsRead) &&
                (!Settings.HideUnread || IsRead) &&
                (!Settings.HideFavorited || !IsStoryFavorited) &&
                (!Settings.HideUnfavorited || IsStoryFavorited) &&
                (!Settings.HideIgnored || !IsIgnored) &&
                (!Settings.HideRated || !UserRating.HasValue) &&
                (!Settings.HideUnrated || UserRating.HasValue) &&
                (!Settings.FilterByRating || (UserRating.HasValue && UserRating.Value >= Settings.MinRating && UserRating.Value <= Settings.MaxRating)) &&
                (!Settings.FilterByWordCount || (WordCount >= Settings.MinWordCount && WordCount <= Settings.MaxWordCount)) &&
                (!Settings.FilterByApprovalDate || !Settings.MinApprovalDate.HasValue || !DateApproved.HasValue || DateApproved.Value >= Settings.MinApprovalDate.Value) &&
                (!Settings.FilterByDownloadDate || !Settings.MinDownloadDate.HasValue || DownloadedAt >= Settings.MinDownloadDate.Value) &&
                MVM.IsSearchMatch(this);
        }

        /// <param name="FolderPath">The local folder path where this story is saved, or null if this story has not been saved locally.</param>
        public LiteroticaStory(MainViewModel MVM, SerializableStory Story, string FolderPath)
        {
            this.MVM = MVM;
            this.FolderPath = FolderPath;

            if (Story.IsSummary)
                Summary = Story;
            else
            {
                Summary = Story.AsSummary();
                this.Story = Story;
            }
            WordCount = Summary.Chapters.Sum(x => x.WordCount);

            if (DateTime.TryParse(DateApprovedString, out DateTime ApprovedAt))
                DateApproved = ApprovedAt;

            Categories = Summary.Chapters.Select(x => x.Category).DistinctBy(x => x.pageUrl).OrderBy(x => x.pageUrl).ToList().AsReadOnly();

            Group = MVM.GetOrCreateAuthorGroup(Story.Author);
            Group.OnIsFavoritedChanged += (sender, e) => { NPC(nameof(IsAuthorFavorited)); };

            UserRating = null;
            UserNotes = null;

            IsIgnored = false;
            IsRead = false;

            QueuedAt = null;
            LastOpenedAt = null;

            RecentPosition = null;

            UpdateVisibility();
        }

        public DelegateCommand<object> SaveLocal => new(_ => SaveToDefaultFolder());
        public void SaveToDefaultFolder() => Save(Downloader.GetLocalFolder(MVM.Settings.StoriesDirectory, Story));
        public void Save(string Folder)
        {
            if (!IsSaved)
            {
                Directory.CreateDirectory(Folder);

                //  Write the story data to 3 files, .html, .txt, and .json
                try
                {
                    File.WriteAllText(Path.Combine(Folder, "story.json"), GeneralUtils.SerializeJson(Story, true));
                    File.WriteAllText(Path.Combine(Folder, SummaryFilename), GeneralUtils.SerializeJson(Story.AsSummary(), true));
                    File.WriteAllText(Path.Combine(Folder, "story.html"), AsHtml(null, null, null, null, null, true, MVM.Settings.CommaDelimitedKeywords));
                    File.WriteAllText(Path.Combine(Folder, "story.txt"), AsPlainText());
                    FolderPath = Folder;
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }

                if (!MVM.Stories.Contains(this))
                    MVM.Stories.Add(this);
            }
        }

        /// <summary>Ensures the full story content has been read from the file. Deserializes it if not.</summary>
        public void Load()
        {
            if (Story == null)
            {
                try
                {
                    string Json = File.ReadAllText(StoryJsonFilePath);
                    this.Story = GeneralUtils.DeserializeJson<SerializableStory>(Json);
                }
                catch (Exception ex) { MessageBox.Show($"Error loading story from file at '{StoryJsonFilePath}':\n\n{ex}"); }
            }
        }

        public DelegateCommand<object> Delete => new(_ => _ = MVM.DeleteStoryAsync(this));

        public DelegateCommand<object> OpenFolder => new(_ => GeneralUtils.ShellExecute(FolderPath));

        private const string DefaultPlaintextDivider = "========================================";

        public string AsPlainText(string Divider = DefaultPlaintextDivider)
        {
            StringBuilder SB = new();
            SB.AppendLine($"{Story.Chapters.First().FullUrl} by {AuthorName}\n\n{Title}");

            for (int i = 0; i < Story.Chapters.Count; i++)
            {
                SerializableChapter Chapter = Story.Chapters[i];
                SB.AppendLine($"\n{Divider}\nChapter {(i + 1).ToString()}: {Chapter.Title}\n{Chapter.Description}\n{Divider}\n");

                for (int j = 0; j < Chapter.Pages.Count; j++)
                {
                    SerializablePage Page = Chapter.Pages[j];
                    SB.AppendLine($"{Divider}\nPage {(j + 1).ToString()}\n{Divider}\n\n{Page.Content}");
                }
            }

            string StoryContent = SB.ToString();
            return StoryContent;
        }

        public static string GetChapterId(int ChapterIndex) => $"story_chapter{ChapterIndex}";
        public static string GetChapterAndPageId(int ChapterIndex, int PageIndex) => $"story_chapter{ChapterIndex}_page{PageIndex}";

        private const string DefaultHtmlDivider = "<hr color=\"Black\" size=\"4px\" style=\"opacity: 0.25\">";

        public string AsHtml(int? FontSize, string FontFamily, Color? ForegroundColor, Color? BackgroundColor, Color? HighlightColor, bool IncludeKeywords, IEnumerable<string> Keywords, string Divider = DefaultHtmlDivider)
            => AsHtml(FontSize, FontFamily, ForegroundColor, BackgroundColor, HighlightColor, IncludeKeywords, string.Join(",", Keywords), Divider);

        public string AsHtml(int? FontSize, string FontFamily, Color? ForegroundColor, Color? BackgroundColor, Color? HighlightColor, bool IncludeKeywords, string CommaDelimitedKeywords, string Divider = DefaultHtmlDivider)
        {
            StringBuilder SB = new();
            string url = Story.Chapters.First().FullUrl;
            string authorUrl = $"https://www.literotica.com/stories/memberpage.php?uid={Story.Author.userid}&page=submissions";
            SB.AppendLine($"<a href='{url}'>{url}</a> by <a href='{authorUrl}'>{AuthorName}</a><h1 align=\"center\">{Title}</h1>");

            for (int i = 0; i < Story.Chapters.Count; i++)
            {
                int ChapterNumber = i + 1;
                SerializableChapter Chapter = Story.Chapters[i];
                if (ChapterNumber != 1)
                    SB.AppendLine("\n");
                SB.AppendLine($"<div align=\"center\">{Divider}<h2 id=\"{GetChapterId(i)}\">Chapter {ChapterNumber}: {Chapter.Title}</h2></div>\n<b>{Chapter.Description}</b>");

                for (int j = 0; j < Chapter.Pages.Count; j++)
                {
                    int PageNumber = j + 1;
                    SerializablePage Page = Chapter.Pages[j];
                    SB.AppendLine($"{Divider}<h3 id=\"{GetChapterAndPageId(i, j)}\">Page {PageNumber}</h3>\n{Page.Content}");
                }
            }

            string StoryContent = SB.ToString();

            string FontSizeString = FontSize.HasValue ? $"font-size: {FontSize.Value}px;" : "";
            string FontFamilyString = FontFamily != null ? $"font-family: {FontFamily};" : "";

            string ForegroundColorString = ForegroundColor.HasValue ? $"color: {GeneralUtils.GetRGBAHexString(ForegroundColor.Value)};" : "";
            string BackgroundColorString = BackgroundColor.HasValue ? $"background-Color: {GeneralUtils.GetRGBAHexString(BackgroundColor.Value)};" : "";
            string HighlightColorString = HighlightColor.HasValue ? GeneralUtils.GetRGBAHexString(HighlightColor.Value) : "yellow";

            string KeywordsContent = !IncludeKeywords ? "" :
$@"
  <input id='keywords_input' style='border-style: solid; border-width: thin; min-width: 600px' value='{CommaDelimitedKeywords}' />
  <button style='background-color:yellow; padding-left:10px; padding-right:10px' onclick=""highlight(document.getElementById('keywords_input').value)"">Highlight</button>
  <br/>
";

            string Html = $@"
<!DOCTYPE html>
<html>
<style>
h1 {{
  margin - top: 0.2em;
  margin-bottom: 0.2em;
}}
h2 {{
  margin - top: 0.15em;
  margin-bottom: 0.15em;
}}
h3 {{
  margin - top: 0.1em;
  margin-bottom: 0.1em;
}}
.highlight {{
  background-color: {HighlightColorString};
}}
</style>
<body style=""margin:25px; {BackgroundColorString}"">
  {KeywordsContent}
  <span id=""story_content"" style=""white-space: pre-wrap; {FontSizeString} {FontFamilyString} {ForegroundColorString}"">{StoryContent}</span>
  {HighlightScript}
</body>
</html>
";

            return Html;
        }

        //  Slightly adapted from: https://stackoverflow.com/a/29798094/11689514
        //  I don't know anything about JavaScript, so I hope my changes didn't screw anything up
        private const string HighlightScript =
            @"<script>
var InstantSearch = {

    ""highlight"": function (container, highlightText)
    {
        var internalHighlighter = function (options)
        {
            var id = {
                container: ""container"",
                tokens: ""tokens"",
                all: ""all"",
                token: ""token"",
                className: ""className"",
                sensitiveSearch: ""sensitiveSearch""
            },
            tokens = options[id.tokens],
            allClassName = options[id.all][id.className],
            allSensitiveSearch = options[id.all][id.sensitiveSearch];

            function checkAndReplace(node, tokenArr, classNameAll, sensitiveSearchAll)
            {
                var nodeVal = node.nodeValue, parentNode = node.parentNode,
                    i, j, curToken, myToken, myClassName, mySensitiveSearch,
                    finalClassName, finalSensitiveSearch,
                    foundIndex, begin, matched, end,
                    textNode, span, isFirst;

                for (i = 0, j = tokenArr.length; i < j; i++)
                {
                    curToken = tokenArr[i];
                    myToken = curToken[id.token];
                    myClassName = curToken[id.className];
                    mySensitiveSearch = curToken[id.sensitiveSearch];

                    finalClassName = (classNameAll ? myClassName + "" "" + classNameAll : myClassName);

                    finalSensitiveSearch = (typeof sensitiveSearchAll !== ""undefined"" ? sensitiveSearchAll : mySensitiveSearch);

                    var actualToken;
                    if (finalSensitiveSearch)
                        actualToken = myToken;
                    else
                        actualToken = myToken.toLowerCase();
                    var regex = new RegExp('\\b' + actualToken + '\\b');

                    isFirst = true;
                    while (true)
                    {
                        if (finalSensitiveSearch)
                            foundIndex = nodeVal.search(regex);
                        else
                            foundIndex = nodeVal.toLowerCase().search(regex);

                        if (foundIndex < 0)
                        {
                            if (isFirst)
                                break;

                            if (nodeVal)
                            {
                                textNode = document.createTextNode(nodeVal);
                                parentNode.insertBefore(textNode, node);
                            }

                            parentNode.removeChild(node);
                            break;
                        }

                        isFirst = false;

                        begin = nodeVal.substring(0, foundIndex);
                        matched = nodeVal.substr(foundIndex, myToken.length);

                        if (begin)
                        {
                            textNode = document.createTextNode(begin);
                            parentNode.insertBefore(textNode, node);
                        }

                        span = document.createElement(""span"");
                        span.className += finalClassName;
                        span.appendChild(document.createTextNode(matched));
                        parentNode.insertBefore(span, node);

                        nodeVal = nodeVal.substring(foundIndex + myToken.length);
                    }
                }
            };

            function iterator(p)
            {
                if (p === null) return;

                var children = Array.prototype.slice.call(p.childNodes), i, cur;

                if (children.length)
                {
                    for (i = 0; i < children.length; i++)
                    {
                        cur = children[i];
                        if (cur.nodeType === 3)
                        {
                            checkAndReplace(cur, tokens, allClassName, allSensitiveSearch);
                        }
                        else if (cur.nodeType === 1)
                        {
                            iterator(cur);
                        }
                    }
                }
            };

            iterator(options[id.container]);
        };

        internalHighlighter(
            {
                container: container
                , all:
                    {
                        className: ""highlighter""
                    }
                , tokens: [
                    {
                        token: highlightText
                        , className: ""highlight""
                        , sensitiveSearch: false
                    }
                ]
            }
        );
    }
};

function highlight(highlightText)
{
	const words = highlightText.split("","");
	for (var i = 0; i < words.length; i++)
	{
        if (words[i].length > 1)
        {
		    var container = document.getElementById(""story_content"");
		    InstantSearch.highlight(container, words[i]);
        }
	}
}
</script>";

        public override string ToString() => $"{nameof(LiteroticaStory)}: {Title}";
    }
}
