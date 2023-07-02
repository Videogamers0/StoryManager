using Microsoft.Web.WebView2.Wpf;
using Prism.Commands;
using StoryManager.UI;
using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using UIOption = Microsoft.VisualBasic.FileIO.UIOption;
using RecycleOption = Microsoft.VisualBasic.FileIO.RecycleOption;
using DeleteDirectoryOption = Microsoft.VisualBasic.FileIO.DeleteDirectoryOption;
using System.Collections.Specialized;
using Newtonsoft.Json.Linq;
using Microsoft.Win32;
using System.Reflection;
using Microsoft.Web.WebView2.Core;
using Octokit;

namespace StoryManager.VM
{
    public enum Theme
    {
        LightMode,
        DarkMode
    }

    public class MainViewModel : ViewModelBase
    {
        public static readonly Version FileVersion = Version.Parse(FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion);
        public string VersionString => FileVersion.ToString();

        public static string GetStoryUniqueKey(string AuthorName, string StoryTitle) => $"{AuthorName}|{StoryTitle}";

        public MainWindow Window { get; }
        public WebView2 WebView => Window.StoryViewerControl.WebView;

        public DelegateCommand<object> BrowserGoBack => new(_ => WebView.GoBack());
        public DelegateCommand<object> BrowserGoForward => new(_ => WebView.GoForward());

        public async Task RefreshFontSizeAsync()
        {
            //Taken from: https://stackoverflow.com/a/55468228
            string script = $@"document.getElementById(""story_content"").style.fontSize = ""{Settings.FontSize}px"";";
            await WebView.ExecuteScriptAsync(script);
        }

        public async Task RefreshForegroundColorAsync()
        {
            string script = $@"document.getElementById(""story_content"").style.color = ""{GeneralUtils.GetRGBAHexString(Settings.ForegroundColor)}"";";
            await WebView.ExecuteScriptAsync(script);
        }

        public async Task RefreshBackgroundColorAsync()
        {
            string script = $@"document.body.style.backgroundColor = ""{GeneralUtils.GetRGBAHexString(Settings.BackgroundColor)}"";";
            await WebView.ExecuteScriptAsync(script);
        }

        public async Task RefreshHighlightColorAsync()
        {
#if NEVER
            //Taken from: https://stackoverflow.com/a/14249194
            string script = $@"
function cssrules() {{{{
    var rules = {{{{}}}};
    for (var i=0; i<document.styleSheets.length; ++i) {{{{
        var cssRules = document.styleSheets[i].cssRules;
        for (var j=0; j<cssRules.length; ++j)
            rules[cssRules[j].selectorText] = cssRules[j];
    }}}}
    return rules;
}}}}

function css_getclass(name) {{{{
    var rules = cssrules();
    if (!rules.hasOwnProperty(name))
        throw 'TODO: deal_with_notfound_case';
    return rules[name];
}}}}

css_getclass('.highlight').style.background=""""{{GetRGBAHexString(ForegroundColor)}}""""
"";";
#else
            //Taken from: https://stackoverflow.com/a/55468228
            string script = $@"[...document.styleSheets[0].cssRules].find(x=> x.selectorText=='.highlight').style.background=""{GeneralUtils.GetRGBAHexString(Settings.HighlightColor)}"";";
#endif
            await WebView.ExecuteScriptAsync(script);
        }

        public async Task RefreshColorsAsync()
        {
            await RefreshForegroundColorAsync();
            await RefreshBackgroundColorAsync();
            await RefreshHighlightColorAsync();
        }

        public async Task HighlightKeywords()
        {
            if (!string.IsNullOrEmpty(Settings.CommaDelimitedKeywords))
                await WebView.ExecuteScriptAsync($"highlight(\"{Settings.CommaDelimitedKeywords}\")");
        }

        public ObservableCollection<LiteroticaStory> Stories { get; }
        public ICollectionView SortedStories { get; }
        public ICollectionView FavoritedStories { get; }
        public ObservableCollection<LiteroticaStory> RecentStories { get; }
        public ICollectionView QueuedStories { get; }

        public bool AreAnyStoriesQueued => Stories?.Any(x => x.IsQueued) == true;

        private void UpdateStoryVisibilities() => Stories?.ToList().ForEach(x => x.UpdateVisibility());

        /// <summary>
        /// Key = the http-encoded title of a story chapter, such as "accidents-happen-1" (<see href="https://www.literotica.com/s/accidents-happen-1"/>)<br/>
        /// Value = the story object that was downloaded for that chapter title<para/>
        /// Since the story objects contain all pages of all chapters, there may be several keys that map to the same story.
        /// </summary>
        private readonly Dictionary<string, LiteroticaStory> StoriesByChapterTitle = new();
        public bool TryGetStory(string ChapterTitle, out LiteroticaStory Story) => StoriesByChapterTitle.TryGetValue(ChapterTitle, out Story);

        private Dictionary<string, AuthorGroup> _AuthorGroups { get; }
        public IReadOnlyDictionary<string, AuthorGroup> AuthorGroups => _AuthorGroups;
        public AuthorGroup GetOrCreateAuthorGroup(LiteroticaAuthor Author)
        {
            if (!_AuthorGroups.TryGetValue(Author.username, out AuthorGroup Group))
            {
                Group = new(this, Author);
                _AuthorGroups.Add(Author.username, Group);
                Settings.GetPreviousSessionAuthorSettings(Group.AuthorName, new()).ApplyTo(Group);
            }

            return Group;
        }

        private readonly LimitedStack<LiteroticaStory> BackHistory = new(10);
        private readonly LimitedStack<LiteroticaStory> ForwardHistory = new(10);

        public DelegateCommand<object> NavigateStoryBack => new(_ =>
        {
            if (BackHistory.TryPop(out LiteroticaStory Story))
            {
                if (SelectedStory != null)
                    ForwardHistory.Push(SelectedStory);
                _ = SetSelectedStoryAsync(Story, false, true);
            }
        });
        public DelegateCommand<object> NavigateStoryForward => new(_ =>
        {
            if (ForwardHistory.TryPop(out LiteroticaStory Story))
            {
                if (SelectedStory != null)
                    BackHistory.Push(SelectedStory);
                _ = SetSelectedStoryAsync(Story, false, true);
            }
        });

        #region Selected Story
        /// <summary>Only intended to be used by data-binding on the UI.<br/>
        /// To set this value programmatically, use <see cref="SetSelectedStoryAsync(LiteroticaStory)"/> instead.</summary>
        public LiteroticaStory BindableSelectedStory
        {
            get => SelectedStory;
            set
            {
                if (value != null)
                    _ = SetSelectedStoryAsync(value, true, true);
            }
        }

        private readonly List<LiteroticaStory> UnsavedWarningExclusions = new();
        private void TryWarnIfUnsavedStory(LiteroticaStory Story)
        {
            if (Settings.WarnIfClosingUnsavedStory && Story?.IsSaved == false && !UnsavedWarningExclusions.Contains(Story))
            {
                string Msg = $"Story \"{Story.Title} by {Story.AuthorName}\" is unsaved.\n\nDo you want to save it?";
                MessageBoxResult Choice = MessageBox.Show(Window, Msg, "Unsaved story", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (Choice == MessageBoxResult.Yes)
                    Story.SaveToDefaultFolder();
                UnsavedWarningExclusions.Add(Story);
            }
        }

        private LiteroticaStory _SelectedStory;
        public LiteroticaStory SelectedStory => _SelectedStory;
        /// <param name="UpdateNavigationHistory">If <seealso langword="true"/>, the current story will be added to <see cref="BackHistory"/> and the <see cref="ForwardHistory"/> will be cleared.</param>
        public async Task SetSelectedStoryAsync(LiteroticaStory Value, bool UpdateNavigationHistory, bool WarnIfUnsaved)
        {
            if (SelectedStory != Value)
            {
                LiteroticaStory Previous = SelectedStory;
                if (WarnIfUnsaved)
                    TryWarnIfUnsavedStory(Previous);

                _SelectedStory = Value;
                NPC(nameof(SelectedStory));
                NPC(nameof(BindableSelectedStory));
                NPC(nameof(IsStorySelected));

                Previous?.NPC(nameof(LiteroticaStory.IsSelected));
                Previous?.Group?.NPC(nameof(AuthorGroup.IsSelected));

                if (UpdateNavigationHistory)
                {
                    if (Previous != null)
                        BackHistory.Push(Previous);
                    ForwardHistory.Clear();
                }

                if (SelectedStory != null)
                {
                    SelectedStory.LastOpenedAt = DateTime.Now;

                    SelectedStory.NPC(nameof(LiteroticaStory.IsSelected));
                    SelectedStory.Group.IsExpanded = true;
                    SelectedStory.Group.NPC(nameof(AuthorGroup.IsSelected));
                    SelectedStory.Load();

                    if (RecentStories.Contains(SelectedStory))
                    {
                        int CurrentIndex = RecentStories.IndexOf(SelectedStory);
                        int DesiredIndex = 0;
                        if (CurrentIndex != DesiredIndex)
                            RecentStories.Move(CurrentIndex, 0);
                    }
                    else
                        RecentStories.Insert(0, SelectedStory);

                    while (RecentStories.Count > Settings.HistorySize)
                        RecentStories.RemoveAt(RecentStories.Count - 1);
                }

                //  Save previous story's scroll position
                if (Previous != null)
                    Previous.RecentPosition = await Bookmark.CreateAsync(WebView);

                //  Load the content of the new story
                bool LoadSuccess = false;
                try
                {
                    if (SelectedStory != null)
                    {
                        string HTML = SelectedStory?.AsHtml(Settings.FontSize, Settings.ForegroundColor, Settings.BackgroundColor, Settings.HighlightColor, false, Settings.CommaDelimitedKeywords) ?? "";
                        try
                        { 
                            WebView.NavigateToString(HTML);
                            LoadSuccess = true;
                        }
                        catch (ArgumentException ex)
                        {
                            //  NavigateToString does not accept strings larger than 2MB, so try loading the content from a file instead.
                            //  See also: https://github.com/MicrosoftEdge/WebView2Feedback/issues/1355

                            const int TwoMB = 1024 * 1024 * 2;
                            int ContentLength = Encoding.Unicode.GetByteCount(HTML);
                            if (ContentLength >= TwoMB && SelectedStory.IsSaved && File.Exists(SelectedStory.StoryHtmlFilePath))
                            {
                                //  Synchronously set the source to the file containing the html content
                                DateTime StartTime = DateTime.Now;
                                TimeSpan Timeout = TimeSpan.FromSeconds(2.0);
                                bool IsSourceChanged = false;
                                void HandleSourceChanged(object sender, EventArgs e)
                                {
                                    IsSourceChanged = true;
                                    WebView.SourceChanged -= HandleSourceChanged;
                                }
                                WebView.SourceChanged += HandleSourceChanged;
                                WebView.Source = new Uri(SelectedStory.StoryHtmlFilePath);
                                //  Wait until the new source is finished loading
                                while (!IsSourceChanged && DateTime.Now.Subtract(StartTime) <= Timeout)
                                    await Task.Delay(TimeSpan.FromMilliseconds(5));

                                LoadSuccess = true;
                            }
                            else
                                throw ex; //TODO should probably write the HTML to a temp file, set the Source, wait until it's done changing, then delete the temp file
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    LoadSuccess = false;
                }

                if (LoadSuccess)
                {
                    //  Try to load the last-known scroll position of the new story
                    if (SelectedStory?.RecentPosition != null)
                        await SelectedStory.RecentPosition.TryLoadAsync(WebView);

                    await RefreshColorsAsync();
                    await HighlightKeywords();
                }
            }
        }

        public bool IsStorySelected => SelectedStory != null;
        #endregion Selected Story

        /// <summary>Attempts to scroll the current document to an HTML element with the given <paramref name="ElementId"/></summary>
        public async Task ScrollToElementAsync(string ElementId)
        {
            //  Taken from: https://stackoverflow.com/a/57308429/11689514
            string Script =
$@"var scrollDiv = document.getElementById(""{ElementId}"").offsetTop;
window.scrollTo({{ top: scrollDiv, behavior: 'smooth'}});";
            await WebView.ExecuteScriptAsync(Script);
        }

        public Settings Settings { get; }
        public Downloader Downloader { get; }
        public StorySearcher Searcher { get; }

        #region Searching
        public bool IsSearchMatch(LiteroticaStory Story)
        {
            if (string.IsNullOrEmpty(CommittedSearchQuery))
                return true;
            else
            {
                return
                    (SearchStoryTitles && Story.Title.Contains(CommittedSearchQuery, StringComparison.CurrentCultureIgnoreCase)) ||
                    (SearchChapterTitles && Story.Summary.Chapters.Any(x => x.Title.Contains(CommittedSearchQuery, StringComparison.CurrentCultureIgnoreCase))) ||
                    (SearchAuthorNames && Story.AuthorName.Contains(CommittedSearchQuery, StringComparison.CurrentCultureIgnoreCase)) ||
                    (SearchTags && Story.Summary.Chapters.Any(x => x.Tags.Contains(CommittedSearchQuery, StringComparer.CurrentCultureIgnoreCase))) ||
                    (SearchDescriptions && Story.Summary.Chapters.Any(x => x.Description.Contains(CommittedSearchQuery, StringComparison.CurrentCultureIgnoreCase)));
            }
        }

        private string _SearchQuery;
        public string SearchQuery
        {
            get => _SearchQuery;
            set
            {
                if (_SearchQuery != value)
                {
                    _SearchQuery = value;
                    NPC(nameof(SearchQuery));
                }
            }
        }

        private string _CommittedSearchQuery;
        public string CommittedSearchQuery
        {
            get => _CommittedSearchQuery;
            private set
            {
                if (_CommittedSearchQuery != value)
                {
                    _CommittedSearchQuery = value;
                    NPC(nameof(CommittedSearchQuery));
                    UpdateStoryVisibilities();
                }
            }
        }

        public DelegateCommand<object> CommitSearch => new(_ => CommittedSearchQuery = SearchQuery);

        #region Settings
        private void HandleSearchSettingChanged()
        {
            if (!string.IsNullOrEmpty(CommittedSearchQuery))
                UpdateStoryVisibilities();
        }

        private bool _SearchStoryTitles;
        public bool SearchStoryTitles
        {
            get => _SearchStoryTitles;
            set
            {
                if (_SearchStoryTitles != value)
                {
                    _SearchStoryTitles = value;
                    NPC(nameof(SearchStoryTitles));
                    HandleSearchSettingChanged();
                }
            }
        }

        private bool _SearchChapterTitles;
        public bool SearchChapterTitles
        {
            get => _SearchChapterTitles;
            set
            {
                if (_SearchChapterTitles != value)
                {
                    _SearchChapterTitles = value;
                    NPC(nameof(SearchChapterTitles));
                    HandleSearchSettingChanged();
                }
            }
        }

        private bool _SearchAuthorNames;
        public bool SearchAuthorNames
        {
            get => _SearchAuthorNames;
            set
            {
                if (_SearchAuthorNames != value)
                {
                    _SearchAuthorNames = value;
                    NPC(nameof(SearchAuthorNames));
                    HandleSearchSettingChanged();
                }
            }
        }

        private bool _SearchTags;
        public bool SearchTags
        {
            get => _SearchTags;
            set
            {
                if (_SearchTags != value)
                {
                    _SearchTags = value;
                    NPC(nameof(SearchTags));
                    HandleSearchSettingChanged();
                }
            }
        }

        private bool _SearchDescriptions;
        public bool SearchDescriptions
        {
            get => _SearchDescriptions;
            set
            {
                if (_SearchDescriptions != value)
                {
                    _SearchDescriptions = value;
                    NPC(nameof(SearchDescriptions));
                    HandleSearchSettingChanged();
                }
            }
        }
        #endregion Settings
        #endregion Searching

        public MainViewModel(MainWindow Window)
        {
            this.Window = Window;

            Settings = new(this);
            Downloader = new(this);
            Searcher = new(this);

            Settings.DisplaySettings.FilterSettings.FiltersChanged += (sender, e) => UpdateStoryVisibilities();

            Window.Left = Settings.PreviousSessionSettings.WindowLeftPosition ?? Window.Left;
            Window.Top = Settings.PreviousSessionSettings.WindowTopPosition ?? Window.Top;
            Window.Width = Settings.PreviousSessionSettings.WindowWidth ?? Window.Width;
            Window.Height = Settings.PreviousSessionSettings.WindowHeight ?? Window.Height;
            if (Settings.PreviousSessionSettings.SidebarWidth.HasValue)
                Window.SidebarColumn.Width = new GridLength(Settings.PreviousSessionSettings.SidebarWidth.Value, GridUnitType.Pixel);
            if (Settings.PreviousSessionSettings.HasPosition)
                Window.WindowStartupLocation = WindowStartupLocation.Manual;

            _AuthorGroups = new();

            bool IsReloadingStories = false;

            Stories = new();
            Stories.CollectionChanged += (sender, e) =>
            {
                void Story_IsQueuedChangedHandler(object sender, bool isQueued)
                {
                    NPC(nameof(AreAnyStoriesQueued));
                }

                //  Update cached data when stories are added or removed to the collection
                if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace && e.NewItems != null)
                {
                    foreach (LiteroticaStory Story in e.NewItems)
                    {
                        foreach (SerializableChapter Chapter in Story.Summary.Chapters)
                            StoriesByChapterTitle[Chapter.Url] = Story;
                        Settings.GetPreviousSessionStorySettings(Story, new()).ApplyTo(Story);

                        Story.OnIsQueuedChanged += Story_IsQueuedChangedHandler;
                    }
                }

                if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace && e.OldItems != null)
                {
                    foreach (LiteroticaStory Story in e.OldItems)
                    {
                        foreach (SerializableChapter Chapter in Story.Summary.Chapters)
                            StoriesByChapterTitle.Remove(Chapter.Url);

                        Story.OnIsQueuedChanged -= Story_IsQueuedChangedHandler;
                    }
                }

                if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace)
                {
                    NPC(nameof(AreAnyStoriesQueued));
                    Settings.DisplaySettings.FilterSettings.TotalMaxWordCount = !Stories.Any() ? 100 : Stories.Max(x => x.WordCount);
                }

                if (e.Action is NotifyCollectionChangedAction.Reset)
                {
                    if (!IsReloadingStories)
                        throw new NotImplementedException();
                }
            };

            PropertyGroupDescription AuthorGroupDescription = new PropertyGroupDescription(nameof(LiteroticaStory.Group));

            SortedStories = CollectionViewHelpers.GetSortedFilteredCollectionView(Stories, x => x.IsVisible, nameof(LiteroticaStory.IsVisible), nameof(LiteroticaStory.AuthorName), nameof(LiteroticaStory.Title));
            SortedStories.GroupDescriptions.Add(AuthorGroupDescription);

            FavoritedStories = CollectionViewHelpers.GetSortedFilteredCollectionView(Stories, x => x.IsStoryFavorited /*|| x.IsAuthorFavorited*/,
                new List<string>() { nameof(LiteroticaStory.IsStoryFavorited), nameof(LiteroticaStory.IsAuthorFavorited) },
                new SortDescription[] { new SortDescription(nameof(LiteroticaStory.AuthorName), ListSortDirection.Ascending), new SortDescription(nameof(LiteroticaStory.Title), ListSortDirection.Ascending) });
            FavoritedStories.GroupDescriptions.Add(AuthorGroupDescription);

            RecentStories = new();

            QueuedStories = CollectionViewHelpers.GetSortedFilteredCollectionView(Stories, x => x.IsQueued, nameof(LiteroticaStory.IsQueued), 
                new SortDescription(nameof(LiteroticaStory.QueuedAt), ListSortDirection.Descending), new SortDescription(nameof(LiteroticaStory.Title), ListSortDirection.Ascending));

            Task LoadStoriesTask = LoadStoriesAsync(true);

            Settings.StoriesDirectoryChanged += (sender, e) =>
            {
                try
                {
                    IsReloadingStories = true;
                    Stories.Clear();
                }
                finally { IsReloadingStories = false; }
                _ = LoadStoriesAsync(false);
            };

            void UpdateSortingAndGrouping(ICollectionView SourceCollection, bool IsGrouping)
            {
                try
                {
                    bool TryFindSortDescription(ICollectionView CV, string PropertyName, out SortDescription Result)
                    {
                        foreach (SortDescription SD in CV.SortDescriptions)
                        {
                            if (SD.PropertyName == PropertyName)
                            {
                                Result = SD;
                                return true;
                            }
                        }

                        Result = default;
                        return false;
                    }

                    if (IsGrouping)
                    {
                        if (!SourceCollection.GroupDescriptions.Contains(AuthorGroupDescription))
                            SourceCollection.GroupDescriptions.Insert(0, AuthorGroupDescription);
                        if (!TryFindSortDescription(SourceCollection, nameof(LiteroticaStory.AuthorName), out _))
                            SourceCollection.SortDescriptions.Insert(0, new SortDescription(nameof(LiteroticaStory.AuthorName), ListSortDirection.Ascending));
                    }
                    else
                    {
                        SourceCollection.GroupDescriptions.Remove(AuthorGroupDescription);
                        if (TryFindSortDescription(SourceCollection, nameof(LiteroticaStory.AuthorName), out SortDescription AuthorNameSortDescription))
                            SourceCollection.SortDescriptions.Remove(AuthorNameSortDescription);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            }

            UpdateSortingAndGrouping(SortedStories, Settings.GroupAllByAuthor);
            Settings.GroupAllByAuthorChanged += (sender, value) => UpdateSortingAndGrouping(SortedStories, value);
            UpdateSortingAndGrouping(FavoritedStories, Settings.GroupFavoritesByAuthor);
            Settings.GroupFavoritesByAuthorChanged += (sender, value) => UpdateSortingAndGrouping(FavoritedStories, value);

            SearchStoryTitles = true;
            SearchChapterTitles = true;
            SearchDescriptions = true;
            SearchAuthorNames = true;
            SearchTags = true;

            async void HandleWindowClosing(object sender, CancelEventArgs e)
            {
                Window.Closing -= HandleWindowClosing;
                e.Cancel = true;
                Window.IsEnabled = false;
                TryWarnIfUnsavedStory(SelectedStory);
                await Settings.SaveAsync(true);
                // In rare cases, Settings.SaveAsync executes synchronously, which causes Window.Close() to fail because the initial Window.Closing event is still invoking
                await Task.Delay(TimeSpan.FromMilliseconds(5));
                Window.Close();
                Window.IsEnabled = true;
            }

            Window.Closing += HandleWindowClosing;

            TryShowGithubAfterInitialize(LoadStoriesTask);

            //_ = CheckForUpdatesAsync(false);
        }

        public DelegateCommand<object> CheckForUpdates => new(_ => _ = CheckForUpdatesAsync(true));

        private bool _IsCheckingForUpdates;
        public bool IsCheckingForUpdates
        {
            get => _IsCheckingForUpdates;
            private set
            {
                if (_IsCheckingForUpdates != value)
                {
                    _IsCheckingForUpdates = value;
                    NPC(nameof(IsCheckingForUpdates));
                }
            }
        }

        private IReadOnlyList<Release> AvailableVersions = null;

        private async Task CheckForUpdatesAsync(bool AlertIfUpToDate)
        {
            if (IsCheckingForUpdates)
                return;

            try
            {
                //https://stackoverflow.com/a/65029587

                IReadOnlyList<Release> releases;
                if (AvailableVersions != null)
                    releases = AvailableVersions;
                else
                {
                    try
                    {
                        IsCheckingForUpdates = true;
                        GitHubClient client = new GitHubClient(new ProductHeaderValue("Videogamers0-StoryManager"));
                        releases = await client.Repository.Release.GetAll("Videogamers0", "StoryManager");
                        AvailableVersions = releases;
                    }
                    finally { IsCheckingForUpdates = false; }
                }

                Version latestGitHubVersion = new Version(releases[0].TagName.Substring(1));
                Version localVersion = FileVersion;
                if (localVersion.CompareTo(latestGitHubVersion) < 0)
                {
                    UpdateAvailable Dialog = new(releases[0]);
                    Dialog.Owner = Window;
                    Dialog.ShowDialog();
                }
                else if (AlertIfUpToDate)
                    MessageBox.Show($"No updates found. You are using the latest version: {FileVersion}", "No updates available");
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private async void TryShowGithubAfterInitialize(Task InitializeTask)
        {
            await InitializeTask;

#if NEVER // Testing counter for how many times you've used the software
            if (Settings.PreviousSessionSettings.OpenedCount % 2 == 0)
#else
            List<int> Milestones = new() { 50, 100, 250, 500, 1000 };
            if (Milestones.Contains(Settings.PreviousSessionSettings.OpenedCount))
#endif
            {
                GithubAd Dialog = new(Settings.PreviousSessionSettings.OpenedCount);
                Dialog.Owner = Window;
                Dialog.ShowDialog();
            }
        }

        private static bool IsDirectoryEmpty(string folder) 
            => Directory.GetDirectories(folder).Length == 0 && Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly).Length == 0;

        internal async Task DeleteStoryAsync(LiteroticaStory Story)
        {
            try
            {
                if (!Story.IsSaved)
                    return;

                string Message = $"Are you sure you want to permanently delete this story:\n\n{Story.Title} by {Story.AuthorName}\n\n" +
                    $"All downloaded files for this story will be deleted, and all metadata (such as user rating, is-favorited, is-read/unread) will be lost.";
                MessageBoxResult Answer = MessageBox.Show(Window, Message, "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (Answer == MessageBoxResult.Yes)
                {
                    foreach (SerializableChapter Chapter in Story.Summary.Chapters)
                        StoriesByChapterTitle.Remove(Chapter.Url);

                    //RecentStories.Remove(Story);
                    Stories.Remove(Story);

                    string StoryFolder = Story.FolderPath;
                    string AuthorFolder = Directory.GetParent(StoryFolder).FullName;
                    if (Directory.Exists(StoryFolder))
                    {
                        //  Delete every file created by this software
                        List<string> ManagedFilenames = new List<string>() {
                            "story.html", "story.json", "story.txt", LiteroticaStory.SummaryFilename
                        };
                        foreach (string Filename in ManagedFilenames)
                            FileSystem.DeleteFile(Path.Combine(StoryFolder, Filename), UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);

                        if (IsDirectoryEmpty(StoryFolder))
                            FileSystem.DeleteDirectory(StoryFolder, DeleteDirectoryOption.ThrowIfDirectoryNonEmpty);
                    }
                    if (Directory.Exists(AuthorFolder) && IsDirectoryEmpty(AuthorFolder))
                        FileSystem.DeleteDirectory(AuthorFolder, DeleteDirectoryOption.ThrowIfDirectoryNonEmpty);

                    Story.FolderPath = null;

                    await SetSelectedStoryAsync(RecentStories.FirstOrDefault(x => x != Story) ?? Stories.FirstOrDefault(), false, false);
                }
            }
            catch (Exception ex) { MessageBox.Show($"Error while deleting story '{Story.Title}':\n\n{ex.ToString()}"); }
        }

        private async Task LoadStoriesAsync(bool IsInitializing)
        {
            Stopwatch sw = Stopwatch.StartNew();

            Dictionary<string, SerializableStory> CachedStories = Settings.PreviousSessionSettings.StoryMetadata
                .DistinctBy(x => GetStoryUniqueKey(x.Author.username, x.Title)).ToDictionary(x => GetStoryUniqueKey(GeneralUtils.ToSafeFilename(x.Author.username), GeneralUtils.ToSafeFilename(x.Title)));

            string BaseFolder = Settings.StoriesDirectory;

#if true //multi-threaded loading for each author folder
            List<Task<List<(SerializableStory data, string folder)>>> StoryTasks = new();

            List<(SerializableStory, string)> LoadStories(string AuthorName, string AuthorFolder)
            {
                List<(SerializableStory, string)> Result = new();

                foreach (string StoryFolder in Directory.GetDirectories(AuthorFolder))
                {
                    string StoryFile = Path.Combine(StoryFolder, "story.json");
                    if (File.Exists(StoryFile))
                    {
                        try
                        {
                            string StoryTitle = new DirectoryInfo(StoryFolder).Name;
                            string Key = GetStoryUniqueKey(GeneralUtils.ToSafeFilename(AuthorName), GeneralUtils.ToSafeFilename(StoryTitle));
                            if (!CachedStories.TryGetValue(Key, out SerializableStory Story))
                            {
                                string SummaryFile = Path.Combine(StoryFolder, LiteroticaStory.SummaryFilename);
                                bool SummaryFileExists = File.Exists(SummaryFile);
                                string JsonFile = SummaryFileExists ? SummaryFile : StoryFile;

                                string Json = File.ReadAllText(JsonFile);
                                Story = GeneralUtils.DeserializeJson<SerializableStory>(Json);

                                if (!SummaryFileExists)
                                {
                                    string Summary = GeneralUtils.SerializeJson(Story.AsSummary(), true);
                                    File.WriteAllText(SummaryFile, Summary);
                                }
                            }

                            Result.Add((Story, StoryFolder));
                        }
                        catch (Exception ex) { MessageBox.Show($"Error loading story from file at '{StoryFile}':\n\n{ex}"); }
                    }
                }

                return Result;
            }

            if (Directory.Exists(BaseFolder))
            {
                foreach (string AuthorFolder in Directory.GetDirectories(BaseFolder))
                {
                    Task<List<(SerializableStory, string)>> AuthorStoriesTask = Task.Run(() => LoadStories(new DirectoryInfo(AuthorFolder).Name, AuthorFolder));
                    StoryTasks.Add(AuthorStoriesTask);
                }
            }

            await Task.WhenAll(StoryTasks);
            foreach (var (data, folder) in StoryTasks.SelectMany(x => x.Result))
            {
                LiteroticaStory Story = new(this, data, folder);
                Stories.Add(Story);
            }
#else
            if (Directory.Exists(BaseFolder))
            {
                foreach (string AuthorFolder in Directory.GetDirectories(BaseFolder))
                {
                    foreach (string StoryFolder in Directory.GetDirectories(AuthorFolder))
                    {
                        string StoryFile = Path.Combine(StoryFolder, "story.json");
                        if (File.Exists(StoryFile))
                        {
                            try
                            {
                                string SummaryFile = Path.Combine(StoryFolder, LiteroticaStory.SummaryFilename);
                                bool SummaryFileExists = File.Exists(SummaryFile);
                                string JsonFile = SummaryFileExists ? SummaryFile : StoryFile;

                                string Json = File.ReadAllText(JsonFile);
                                SerializableStory Story = GeneralUtils.DeserializeJson<SerializableStory>(Json);
                                LiteroticaStory StoryVM = new(this, Story, StoryFolder);
                                Stories.Add(StoryVM);

                                if (!SummaryFileExists)
                                {
                                    string Summary = GeneralUtils.SerializeJson(Story.AsSummary(), true);
                                    File.WriteAllText(SummaryFile, Summary);
                                }
                            }
                            catch (Exception ex) { MessageBox.Show($"Error loading story from file at '{StoryFile}':\n\n{ex}"); }
                        }
                    }
                }
            }
#endif

            sw.Stop();
            Debug.WriteLine($"Loaded {Stories.Count} stories in {sw.Elapsed.TotalSeconds:0.000} seconds.");

            Dictionary<string, LiteroticaStory> IndexedStories = Stories.ToDictionary(x => GetStoryUniqueKey(x.AuthorName, x.Title));

            //  Load the history list
            foreach (string RecentStory in Settings.PreviousSessionSettings.HistoryList)
            {
                if (IndexedStories.TryGetValue(RecentStory, out LiteroticaStory Story))
                {
                    RecentStories.Add(Story);
                }
            }

            string UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(StoryManager), "BrowserCache");
            CoreWebView2Environment Env = await CoreWebView2Environment.CreateAsync(null, UserDataFolder);
            await WebView.EnsureCoreWebView2Async(Env);

            //  Try to auto-select the last-opened story
            if (Settings.PreviousSessionSettings.RecentSelectedStory == null || !IndexedStories.TryGetValue(Settings.PreviousSessionSettings.RecentSelectedStory, out LiteroticaStory ToLoad))
                ToLoad = Stories.FirstOrDefault();
            await SetSelectedStoryAsync(ToLoad, true, false);

            UpdateStoryVisibilities();

            if (IsInitializing && !Stories.Any())
                Downloader.OpenOrActivateWindow();
        }

#if NEVER
        private static string FormatAsstrContent(string content)
        {
            //  Asstr seems to bake the linewrapping directly into the content string
            //  so let's first try to remove that but replacing single linebreaks with spaces
            //  EX: "Hello \nWorld" -> "Hello World", but "Hello \n\nWorld" is unchanged.
            string withoutLinewrapping = Regex.Replace(content, @"(?<!\n)\n([^\n\t])", @" $1");

            //  Some text content is justified, so replace multiple consecutive spaces with a single space
            string singleSpaced = Regex.Replace(withoutLinewrapping, @"( {2,})", @" ");

            //  Pad with an empty line if the next line is the start of a tabbed paragraph
            string doubleLined = Regex.Replace(singleSpaced, @"(?<!\n)\n\t", "\n\n\t");

            return doubleLined;
        }

        public DelegateCommand<object> Test1 => new DelegateCommand<object>(async(object o) =>
        {
            try
            {
                string test = await GeneralUtils.ExecuteGetAsync(@"https://www.asstr.org/~Kristen/01/index01.htm"); //most stories seem to be simple .txt files on asstr.org
                string test2 = FormatAsstrContent(test);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        });
#endif
    }
}
