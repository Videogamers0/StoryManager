using Prism.Commands;
using StoryManager.UI;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using WK.Libraries.SharpClipboardNS;

namespace StoryManager.VM.Literotica
{
    public class Downloader : ViewModelBase
    {
        public MainViewModel MVM { get; }

        private DownloadWindow _Window;
        public DownloadWindow Window
        {
            get => _Window;
            private set
            {
                if (_Window != value)
                {
                    _Window = value;
                    NPC(nameof(Window));

                    void OnWindowClosed(object sender, EventArgs e)
                    {
                        if (sender is DownloadWindow window)
                        {
                            window.Closed -= OnWindowClosed;
                            if (Window == window)
                                Window = null;
                        }
                    }

                    if (Window != null)
                        Window.Closed += OnWindowClosed;
                }
            }
        }

        public DelegateCommand<object> OpenWindow => new(_ => OpenOrActivateWindow());
        public void OpenOrActivateWindow()
        {
            if (Window != null)
                Window.Activate();
            else
            {
                Window = new(this);
                Window.Owner = MVM.Window;
                Window.Show();
                StoryUrl = "";
                AuthorUrl = "";
            }
        }

        private string _ProcessingText;
        public string ProcessingText
        {
            get => _ProcessingText;
            private set
            {
                if (_ProcessingText != value)
                {
                    _ProcessingText = value;
                    NPC(nameof(ProcessingText));
                    NPC(nameof(IsProcessing));
                }
            }
        }

        public bool IsProcessing => !string.IsNullOrEmpty(ProcessingText);

        private string _StoryUrl;
        public string StoryUrl
        {
            get => _StoryUrl;
            set
            {
                if (_StoryUrl != value)
                {
                    _StoryUrl = value;
                    NPC(nameof(StoryUrl));
                    NPC(nameof(CanDownloadFromStoryUrl));
                }
            }
        }

        public bool CanDownloadFromStoryUrl => !string.IsNullOrEmpty(StoryUrl);

        private string _ClipboardUrl;
        public string ClipboardUrl
        {
            get => _ClipboardUrl;
            private set
            {
                if (_ClipboardUrl != value)
                {
                    _ClipboardUrl = value;
                    NPC(nameof(ClipboardUrl));
                    NPC(nameof(CanDownloadFromClipboard));
                }
            }
        }

        public bool CanDownloadFromClipboard => !string.IsNullOrEmpty(ClipboardUrl);

        private bool _SaveAfterDownloading;
        public bool SaveAfterDownloading
        {
            get => _SaveAfterDownloading;
            set
            {
                if (_SaveAfterDownloading != value)
                {
                    _SaveAfterDownloading = value;
                    NPC(nameof(SaveAfterDownloading));
                }
            }
        }

        private string _AuthorUrl;
        public string AuthorUrl
        {
            get => _AuthorUrl;
            set
            {
                if (_AuthorUrl != value)
                {
                    _AuthorUrl = value;
                    NPC(nameof(AuthorUrl));
                    NPC(nameof(CanGetStoriesFromAuthorUrl));
                }
            }
        }

        public bool CanGetStoriesFromAuthorUrl => !string.IsNullOrEmpty(AuthorUrl);

        private bool _IsDownloading;
        public bool IsDownloading
        {
            get => _IsDownloading;
            private set
            {
                if (_IsDownloading != value)
                {
                    _IsDownloading = value;
                    NPC(nameof(IsDownloading));
                }
            }
        }

        private CancellationTokenSource _CTS;
        public CancellationTokenSource CTS
        {
            get => _CTS;
            private set
            {
                if (_CTS != value)
                {
                    _CTS = value;
                    NPC(nameof(CTS));
                    NPC(nameof(CT));
                    NPC(nameof(IsCancellable));
                    NPC(nameof(IsCancelling));
                }
            }
        }

        public CancellationToken CT => CTS?.Token ?? CancellationToken.None;
        public bool IsCancellable => CTS != null;
        public bool IsCancelling => CTS?.IsCancellationRequested == true;
        public DelegateCommand<object> CancelAsync => new(_ => { CTS?.Cancel(); NPC(nameof(IsCancelling)); });

        private const string UrlPattern = @"((https?:\/\/)?(www\.))?literotica\.com\/s\/[^""]+";
        private const string TitlePattern = @"(?<Title>.{1,256}?)"; // Must use non-greedy match so it doesn't end up capturing the "</a>" within the title
        //  Sometimes the content of the hyperlink is something like "<span>{Title}</span><!-- // -->" instead of just the story title.
        private static readonly string HyperlinkContentPattern = $@"(<span>{TitlePattern}<\/span>(<!-- \/\/ -->)?|{TitlePattern})";
        private static readonly string HyperlinkPattern = $@"<a( [^>]*)? href=""(?<Url>{UrlPattern})""( [^>]*)?>(?<Content>{HyperlinkContentPattern})<\/a>";
        //  This Regex is intended to parse out the url and story title from strings such as:
        //  "<a class="bb" href="https://www.literotica.com/s/accidents-happen-1">Accidents Happen!</a>"
        private static readonly Regex StoryUrlParser = new(HyperlinkPattern);

        //  This Regex is intended to parse the chapter number from the end of a story url/title, such as:
        //  Input: "https://www.literotica.com/s/it-had-to-be-magic-ch-02" or "it-had-to-be-magic-ch-02"
        //  ChapterNumber: 02
        private static readonly Regex ChapterNumberParser = new(@"-ch-(?<ChapterNumber>\d{2,3})$");

        public ObservableCollection<PendingStory> PendingDownloads { get; }
        public bool IsStoriesListEmpty => PendingDownloads.Count == 0;
        public bool CanDownloadCheckedStories => CheckedStoriesCount > 0;

        private int _CheckedStoriesCount;
        public int CheckedStoriesCount
        {
            get => _CheckedStoriesCount;
            private set
            {
                if (_CheckedStoriesCount != value)
                {
                    _CheckedStoriesCount = value;
                    NPC(nameof(CheckedStoriesCount));
                    NPC(nameof(CanDownloadCheckedStories));
                }
            }
        }

        public DelegateCommand<object> CheckAllStories => new(_ => PendingDownloads.ToList().ForEach(x => x.IsChecked = true));
        public DelegateCommand<object> UncheckAllStories => new(_ => PendingDownloads.ToList().ForEach(x => x.IsChecked = false));

        public Downloader(MainViewModel MVM)
        {
            this.MVM = MVM;

            this.SaveAfterDownloading = true;

            //  Monitor for changes to the Clipboard text
            ClipboardUrl = System.Windows.Forms.Clipboard.GetText()?.Trim();
            SharpClipboard Clipboard = new();
            Clipboard.ObservableFormats.Texts = true;
            Clipboard.ClipboardChanged += (sender, e) =>
            {
                if (e.ContentType == SharpClipboard.ContentTypes.Text)
                {
                    string Value = e.Content as string;
                    if (string.IsNullOrEmpty(Value))
                        Value = System.Windows.Forms.Clipboard.GetText()?.Trim();
                    ClipboardUrl = Value;
                }
                else
                    ClipboardUrl = "";
            };

            PendingDownloads = new();
            PendingDownloads.CollectionChanged += (sender, e) =>
            {
                int TempCheckedStoriesCount = CheckedStoriesCount;

                if (e.Action is NotifyCollectionChangedAction.Add or NotifyCollectionChangedAction.Replace && e.NewItems != null)
                {
                    foreach (PendingStory Item in e.NewItems)
                    {
                        Item.OnCheckStateChanged += Story_CheckStateChangedHandler;
                        if (Item.IsChecked)
                            TempCheckedStoriesCount++;
                    }
                }

                if (e.Action is NotifyCollectionChangedAction.Remove or NotifyCollectionChangedAction.Replace && e.OldItems != null)
                {
                    foreach (PendingStory Item in e.OldItems)
                    {
                        Item.OnCheckStateChanged -= Story_CheckStateChangedHandler;
                        if (Item.IsChecked)
                            TempCheckedStoriesCount--;
                    }
                }

                NPC(nameof(IsStoriesListEmpty));
                CheckedStoriesCount = TempCheckedStoriesCount;

                if (e.Action is NotifyCollectionChangedAction.Reset)
                    CheckedStoriesCount = 0;
            };

            //  Maybe there should be a [Force Download] checkbox setting?
            //      If checked, stories that have already been downloaded will be re-downloaded (such as if there are new chapters since the last time you downloaded it)
        }

        void Story_CheckStateChangedHandler(object sender, bool isChecked) => CheckedStoriesCount += isChecked ? 1 : -1;

        private bool IsAlreadyDownloaded(string Url) => LiteroticaUtils.TryGetStoryTitle(Url, out string ChapterTitle) && MVM.TryGetStory(ChapterTitle, out LiteroticaStory StoryVM);

        public DelegateCommand<object> GetStoriesFromAuthorUrl => new(async(object o) =>
        {
            try
            {
                ProcessingText = AuthorUrl;
                string Content = await GeneralUtils.ExecuteGetAsync(AuthorUrl);

                if (!StoryUrlParser.IsMatch(Content))
                {
                    string Message = $"Failed to retrieve stories from author's page at:\n\n{AuthorUrl}" +
                        $"\n\nThe author url should be in similar format to this example:" +
                        $"\n\nhttps://www.literotica.com/stories/memberpage.php?uid=1133735&page=submissions";
                    MessageBox.Show(Message);
                }
                else
                {
                    foreach (PendingStory Item in PendingDownloads)
                        Item.OnCheckStateChanged -= Story_CheckStateChangedHandler;
                    PendingDownloads.Clear();

                    List<PendingStory> Temp = new();
                    foreach (Match Match in StoryUrlParser.Matches(Content))
                    {
                        string StoryUrl = Match.Groups["Url"].Value;
                        string StoryTitle = Match.Groups["Title"].Value;

                        if (ChapterNumberParser.IsMatch(StoryUrl))
                        {
                            int ChapterNumber = int.Parse(ChapterNumberParser.Match(StoryUrl).Groups["ChapterNumber"].Value);
                            if (ChapterNumber > 1)
                                continue;
                        }

                        Temp.Add(new(StoryUrl, StoryTitle, true));
                    }

                    if (Temp.Any())
                    {
                        Temp.RemoveAll(x => IsAlreadyDownloaded(x.Url));
                        if (!Temp.Any())
                        {
                            MessageBox.Show($"All stories by this author are already downloaded.");
                        }
                        else
                        {
                            foreach (PendingStory Item in Temp)
                                PendingDownloads.Add(Item);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            finally
            {
                ProcessingText = null;
            }
        });

        public DelegateCommand<object> DownloadCheckedStories => new(async(object o) =>
        {
            if (IsDownloading || !CanDownloadCheckedStories)
                return;

            try
            {
                IsDownloading = true;
                ProcessingText = "Preparing to download";

                static bool IsValidUrl(string Url) => LiteroticaUtils.TryGetAPIUri(Url, out _);
                List<PendingStory> ToDownload = PendingDownloads.Where(x => x.IsChecked && IsValidUrl(x.Url) && !IsAlreadyDownloaded(x.Url)).ToList();

                if (ToDownload.Any())
                {
                    try
                    {
                        CTS = new();

                        string PrefixText = $"Downloading {ToDownload.Count} stories.";
                        ProcessingText = PrefixText;

                        foreach (PendingStory Item in ToDownload)
                        {
                            if (IsCancelling)
                                return;

                            ProcessingText = $"{PrefixText}\nDownloading: {Item.Url}";

                            if (!IsAlreadyDownloaded(Item.Url))
                            {
                                SerializableStory Story;
                                try { Story = await LiteroticaUtils.DownloadStory(Item.Url); }
                                catch (TaskCanceledException) { return; }

                                LiteroticaStory StoryVM = StoryVM = new(MVM, Story, null);
                                StoryVM.Save(GetLocalFolder(MVM.Settings.StoriesDirectory, Story));
                            }

                            PendingDownloads.Remove(Item);
                        }
                    }
                    finally { CTS = null; }
                }

                foreach (PendingStory CheckedItem in PendingDownloads.Where(x => x.IsChecked).ToList())
                    PendingDownloads.Remove(CheckedItem);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            finally
            {
                IsDownloading = false;
                ProcessingText = null;
            }
        });

        public DelegateCommand<object> DownloadFromClipboardUrl => new((object o) => _ = DownloadStoryAsync(ClipboardUrl));
        public DelegateCommand<object> DownloadFromStoryUrl => new((object o) => _ = DownloadStoryAsync(StoryUrl));

        /// <summary>
        /// Key = the http-encoded title of a story chapter, such as "accidents-happen-1" (<see href="https://www.literotica.com/s/accidents-happen-1"/>)<br/>
        /// Value = the story object that was downloaded for that chapter title<para/>
        /// Since the story objects contain all pages of all chapters, there may be several keys that map to the same story.
        /// </summary>
        private static readonly Dictionary<string, LiteroticaStory> StoriesByChapterTitle = new();
        private bool TryGetStory(string ChapterTitle, out LiteroticaStory Story) => MVM.TryGetStory(ChapterTitle, out Story) || StoriesByChapterTitle.TryGetValue(ChapterTitle, out Story);

        private async Task DownloadStoryAsync(string StoryUrl)
        {
            if (IsDownloading)
                return;
            else if (!LiteroticaUtils.TryGetAPIUri(StoryUrl, out _))
            {
                if (!string.IsNullOrEmpty(StoryUrl))
                    MessageBox.Show($"Invalid literotica story url format: {StoryUrl}");
                return;
            }

            try
            {
                IsDownloading = true;
                ProcessingText = StoryUrl;

                if (!LiteroticaUtils.TryGetStoryTitle(StoryUrl, out string ChapterTitle) || !TryGetStory(ChapterTitle, out LiteroticaStory StoryVM))
                {
                    SerializableStory Story = await LiteroticaUtils.DownloadStory(StoryUrl);
                    StoryVM = new(MVM, Story, null);
                    if (SaveAfterDownloading)
                        StoryVM.Save(GetLocalFolder(MVM.Settings.StoriesDirectory, Story));

                    //  Cache the story so it won't be downloaded again during this session
                    foreach (SerializableChapter Chapter in Story.Chapters)
                        StoriesByChapterTitle[Chapter.Url] = StoryVM;
                }

                await MVM.SetSelectedStoryAsync(StoryVM, true);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            finally
            {
                IsDownloading = false;
                ProcessingText = null;
            }
        }

        public DelegateCommand<object> DownloadTopStories => new(async(object o) =>
        {
            try
            {
                const string Url = @"https://www.literotica.com/top/most-read-erotic-stories/alltime/?page=1";

                ProcessingText = "Retrieving Top-50 Stories";
                string Content = await GeneralUtils.ExecuteGetAsync(Url);

                static bool IsValidUrl(string Url) => LiteroticaUtils.TryGetAPIUri(Url, out _);

                List<PendingStory> ToDownload = new();
                foreach (Match Match in StoryUrlParser.Matches(Content))
                {
                    string StoryUrl = Match.Groups["Url"].Value;
                    string StoryTitle = Match.Groups["Title"].Value;

                    if (IsValidUrl(StoryUrl) && !IsAlreadyDownloaded(StoryUrl))
                        ToDownload.Add(new(StoryUrl, StoryTitle, true));
                }
                ToDownload = ToDownload.DistinctBy(x => x.Url).ToList();

                if (!ToDownload.Any())
                {
                    MessageBox.Show($"All top stories at {Url} are already downloaded.");
                }
                else
                {
                    IsDownloading = true;
                    ProcessingText = "Preparing to download";

                    try
                    {
                        CTS = new();

                        int RemainingCount = ToDownload.Count;
                        ProcessingText = $"Downloading {RemainingCount} stories.";

                        foreach (PendingStory Item in ToDownload)
                        {
                            if (IsCancelling)
                                return;

                            ProcessingText = $"Downloading {RemainingCount} stories.\nDownloading: {Item.Url}";

                            if (!IsAlreadyDownloaded(Item.Url))
                            {
                                SerializableStory Story;
                                try { Story = await LiteroticaUtils.DownloadStory(Item.Url); }
                                catch (TaskCanceledException) { return; }

                                LiteroticaStory StoryVM = StoryVM = new(MVM, Story, null);
                                StoryVM.Save(GetLocalFolder(MVM.Settings.StoriesDirectory, Story));
                            }

                            RemainingCount--;
                        }
                    }
                    finally { CTS = null; }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
            finally
            {
                IsDownloading = false;
                ProcessingText = null;
            }
        });

        internal static string GetLocalFolder(string BaseFolder, SerializableStory Story) => GetLocalFolder(BaseFolder, Story.Author.username, Story.Title);
        internal static string GetLocalFolder(string BaseFolder, string Author, string Title) => 
            Path.Combine(BaseFolder, GeneralUtils.ToSafeFilename(Author, false), GeneralUtils.ToSafeFilename(Title, false));
    }

    public class PendingStory : ViewModelBase
    {
        public string Url { get; }
        public string Title { get; }

        private bool _IsChecked;
        public bool IsChecked
        {
            get => _IsChecked;
            set
            {
                if (_IsChecked != value)
                {
                    _IsChecked = value;
                    NPC(nameof(IsChecked));
                    OnCheckStateChanged?.Invoke(this, IsChecked);
                }
            }
        }

        public event EventHandler<bool> OnCheckStateChanged;

        public PendingStory(string Url, string Title, bool IsChecked = true)
        {
            this.Url = Url;
            this.Title = Title;
            this.IsChecked = IsChecked;
        }

        public override string ToString() => $"{Title} ({Url})";
    }
}