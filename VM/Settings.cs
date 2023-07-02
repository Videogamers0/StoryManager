using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using StoryManager.UI;
using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Xceed.Wpf.Toolkit.Primitives;

namespace StoryManager.VM
{
    public class Settings : ViewModelBase
    {
        public SavedSettings PreviousSessionSettings { get; }

        public MainViewModel MVM { get; }

        public DisplaySettings DisplaySettings { get; }

        private SettingsWindow _Window;
        public SettingsWindow Window
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
                        if (sender is SettingsWindow window)
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
            }
        }

        private string DefaultStoriesDirectory { get; }
        internal bool IsUsingDefaultStoriesDirectory => StoriesDirectory == DefaultStoriesDirectory;

        private string _StoriesDirectory;
        public string StoriesDirectory
        {
            get => _StoriesDirectory;
            private set
            {
                if (_StoriesDirectory != value)
                {
                    _StoriesDirectory = value;
                    NPC(nameof(StoriesDirectory));
                    StoriesDirectoryChanged?.Invoke(this, StoriesDirectory);
                }
            }
        }

        public event EventHandler<string> StoriesDirectoryChanged;

        #region Theme
        private static readonly Dictionary<Theme, ColorPalette> DefaultColorPalettes = new()
        {
            { Theme.LightMode, new("LightTheme1", Color.FromRgb(10, 10, 10), Color.FromRgb(240, 240, 240), Color.FromArgb(192, 255, 255, 0)) },
            { Theme.DarkMode, new("DarkTheme1", Color.FromRgb(170, 182, 255), Color.FromRgb(32, 32, 32), Color.FromArgb(192, 255, 0, 0)) }
        };

        private Theme _Theme;
        public Theme Theme => _Theme;

        public async Task SetThemeAsync(Theme Value, bool IsInitializing, bool UpdateDocument)
        {
            if (_Theme != Value || IsInitializing)
            {
                bool IsUsingDefaultColors = IsUsingDefaultForegroundColor && IsUsingDefaultBackgroundColor && IsUsingDefaultHighlightColor;

                _Theme = Value;
                NPC(nameof(Theme));
                NPC(nameof(IsLightMode));
                NPC(nameof(IsDarkMode));

                if (IsInitializing || IsUsingDefaultColors)
                {
                    await SetForegroundColorAsync(DefaultColorPalettes[Theme].ForegroundColor, UpdateDocument);
                    await SetBackgroundColorAsync(DefaultColorPalettes[Theme].BackgroundColor, UpdateDocument);
                    await SetHighlightColorAsync(DefaultColorPalettes[Theme].HighlightColor, UpdateDocument);
                }
            }
        }

        /// <summary>Only intended to be used by data-binding on the UI. To set this value programmatically, use <see cref="SetThemeAsync(Theme, bool, bool)"/> instead.</summary>
        public bool IsLightMode
        {
            get => Theme == Theme.LightMode;
            set { if (value) _ = SetThemeAsync(Theme.LightMode, false, true); }
        }

        /// <summary>Only intended to be used by data-binding on the UI. To set this value programmatically, use <see cref="SetThemeAsync(Theme, bool, bool)"/> instead.</summary>
        public bool IsDarkMode
        {
            get => Theme == Theme.DarkMode;
            set { if (value) _ = SetThemeAsync(Theme.DarkMode, false, true); }
        }
        #endregion Theme

        #region FontSize
        public const int DefaultFontSize = 16;
        private bool IsUsingDefaultFontSize => FontSize == DefaultFontSize;

        public int BindableFontSize
        {
            get => FontSize;
            set => _ = SetFontSizeAsync(value, true);
        }

        private int _FontSize;
        public int FontSize => _FontSize;
        public async Task SetFontSizeAsync(int Value, bool UpdateDocument)
        {
            int ActualValue = Math.Clamp(Value, 10, 40);
            if (FontSize != ActualValue)
            {
                _FontSize = ActualValue;
                NPC(nameof(FontSize));
                NPC(nameof(BindableFontSize));

                if (UpdateDocument)
                    await MVM.RefreshFontSizeAsync();
            }
        }
        #endregion FontSize

        #region Colors
        public static IEnumerable<ColorPalette> StaticColorPalettes
        {
            get
            {
                static Color GetGrayscaleColor(byte Value) => Color.FromRgb(Value, Value, Value);

                Color YellowHighlight1 = Color.FromArgb(192, 255, 233, 88);
                Color YellowHighlight2 = Color.FromArgb(192, 220, 192, 56);

                foreach (ColorPalette ThemedPalette in DefaultColorPalettes.Values)
                    yield return ThemedPalette;

                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-{i + 1}", Colors.Black, GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-{i + 1}", GetGrayscaleColor((byte)(i * 16)), GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-{i + 1}", GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), GetGrayscaleColor((byte)(i * 16)), YellowHighlight2);

                Color Red1 = Color.FromRgb(224, 16, 16);
                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-Red-{i + 1}", Red1, GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-Red-{i + 1}", Red1, GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-Red-{i + 1}", Red1, GetGrayscaleColor((byte)(i * 24)), YellowHighlight2);

                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-Green-{i + 1}", Color.FromRgb(0, 140, 0), GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-Green-{i + 1}", Color.FromRgb(0, 180, 0), GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-Green-{i + 1}", Color.FromRgb(16, 224, 16), GetGrayscaleColor((byte)(i * 24)), YellowHighlight2);

                for (int i = 0; i < 3; i++)
                    yield return new($"Gray-Blue-{i + 1}", Color.FromRgb(12, 60, 240), GetGrayscaleColor((byte)(173 + i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"White-Blue-{i + 1}", Color.FromRgb(12, 60, 240), GetGrayscaleColor((byte)(byte.MaxValue - i * 16)), YellowHighlight1);
                for (int i = 0; i < 3; i++)
                    yield return new($"Black-Blue-{i + 1}", Color.FromRgb(16, 120, 232), GetGrayscaleColor((byte)(i * 24)), YellowHighlight2);
            }
        }

        public IReadOnlyList<ColorPalette> PresetColorPalettes { get; } = StaticColorPalettes.ToList();

        public int SelectedColorPaletteIndex
        {
            get => -1;
            set
            {
                if (value != -1)
                {
                    _ = SetColorPaletteAsync(PresetColorPalettes[value], true);
                    Window.Dispatcher.BeginInvoke(() => Window.PresetColorPalettesDropdown.SelectedItem = null, DispatcherPriority.ApplicationIdle);
                }
            }
        }

        private async Task SetColorPaletteAsync(ColorPalette Value, bool UpdateDocument)
        {
            await SetForegroundColorAsync(Value.ForegroundColor, UpdateDocument);
            await SetBackgroundColorAsync(Value.BackgroundColor, UpdateDocument);
            await SetHighlightColorAsync(Value.HighlightColor, UpdateDocument);
        }

        private bool IsUsingDefaultForegroundColor => ForegroundColor == DefaultColorPalettes[Theme].ForegroundColor;
        private bool IsUsingDefaultBackgroundColor => BackgroundColor == DefaultColorPalettes[Theme].BackgroundColor;
        private bool IsUsingDefaultHighlightColor => HighlightColor == DefaultColorPalettes[Theme].HighlightColor;

        public Color BindableForegroundColor
        {
            get => ForegroundColor;
            set => _ = SetForegroundColorAsync(value, true);
        }

        private Color _ForegroundColor;
        public Color ForegroundColor => _ForegroundColor;
        public async Task SetForegroundColorAsync(Color Value, bool UpdateDocument)
        {
            if (ForegroundColor != Value)
            {
                _ForegroundColor = Value;
                NPC(nameof(ForegroundColor));
                NPC(nameof(BindableForegroundColor));

                if (UpdateDocument)
                    await MVM.RefreshForegroundColorAsync();
            }
        }

        public Color BindableBackgroundColor
        {
            get => BackgroundColor;
            set => _ = SetBackgroundColorAsync(value, true);
        }

        private Color _BackgroundColor;
        public Color BackgroundColor => _BackgroundColor;
        public async Task SetBackgroundColorAsync(Color Value, bool UpdateDocument)
        {
            if (BackgroundColor != Value)
            {
                _BackgroundColor = Value;
                NPC(nameof(BackgroundColor));
                NPC(nameof(BindableBackgroundColor));

                if (UpdateDocument)
                    await MVM.RefreshBackgroundColorAsync();
            }
        }

        public Color BindableHighlightColor
        {
            get => HighlightColor;
            set => _ = SetHighlightColorAsync(value, true);
        }

        private Color _HighlightColor;
        public Color HighlightColor => _HighlightColor;
        public async Task SetHighlightColorAsync(Color Value, bool UpdateDocument)
        {
            if (HighlightColor != Value)
            {
                _HighlightColor = Value;
                NPC(nameof(HighlightColor));
                NPC(nameof(BindableHighlightColor));

                if (UpdateDocument)
                    await MVM.RefreshHighlightColorAsync();
            }
        }
        #endregion Colors

        #region Keywords
        /// <summary>Only intended to be used by data-binding on the UI.<br/>
        /// To set this value programmatically, use <see cref="SetCommaDelimitedKeywordsAsync(string, bool)"/> instead.</summary>
        public string BindableCommaDelimitedKeywords
        {
            get => CommaDelimitedKeywords;
            set => _ = SetCommaDelimitedKeywordsAsync(value, true);
        }

        private string _CommaDelimitedKeywords;
        /// <summary>A comma-delimited list of words to highlight in the document.</summary>
        public string CommaDelimitedKeywords => _CommaDelimitedKeywords;
        public async Task SetCommaDelimitedKeywordsAsync(string Value, bool UpdateDocument)
        {
            if (CommaDelimitedKeywords != Value)
            {
                _CommaDelimitedKeywords = Value;
                NPC(nameof(CommaDelimitedKeywords));
                NPC(nameof(BindableCommaDelimitedKeywords));

                if (UpdateDocument)
                    await MVM.HighlightKeywords();
            }
        }

        public IReadOnlyList<string> GetKeywords() => CommaDelimitedKeywords == null ? new List<string>() : CommaDelimitedKeywords.Split(',').ToList();
        #endregion Keywords

        public const int DefaultHistorySize = 25;

        private int _HistorySize;
        public int HistorySize
        {
            get => _HistorySize;
            set
            {
                if (_HistorySize != value)
                {
                    _HistorySize = value;
                    NPC(nameof(HistorySize));
                }
            }
        }

        private bool _GroupAllByAuthor;
        public bool GroupAllByAuthor
        {
            get => _GroupAllByAuthor;
            set
            {
                if (_GroupAllByAuthor != value)
                {
                    _GroupAllByAuthor = value;
                    NPC(nameof(GroupAllByAuthor));
                    GroupAllByAuthorChanged?.Invoke(this, GroupAllByAuthor);
                }
            }
        }

        public event EventHandler<bool> GroupAllByAuthorChanged;

        private bool _GroupFavoritesByAuthor;
        public bool GroupFavoritesByAuthor
        {
            get => _GroupFavoritesByAuthor;
            set
            {
                if (_GroupFavoritesByAuthor != value)
                {
                    _GroupFavoritesByAuthor = value;
                    NPC(nameof(GroupFavoritesByAuthor));
                    GroupFavoritesByAuthorChanged?.Invoke(this, GroupFavoritesByAuthor);
                }
            }
        }

        public event EventHandler<bool> GroupFavoritesByAuthorChanged;

        public Settings(MainViewModel MVM)
        {
            this.MVM = MVM;

            PreviousSessionSettings = SavedSettings.Load(out _);

            DisplaySettings = new(PreviousSessionSettings);

            DefaultStoriesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(StoryManager), "Stories", "Literotica");
            StoriesDirectory = PreviousSessionSettings.StoriesBaseFolder ?? DefaultStoriesDirectory;

            AuthorSettingsByName = PreviousSessionSettings.AuthorSettings.ToDictionary(x => x.Author);
            StorySettingsByAuthorAndTitle = PreviousSessionSettings.StorySettings.DistinctBy(x => MainViewModel.GetStoryUniqueKey(x.Author, x.Title)).ToDictionary(x => MainViewModel.GetStoryUniqueKey(x.Author, x.Title));

            HistorySize = PreviousSessionSettings.HistorySize ?? DefaultHistorySize;

            GroupAllByAuthor = PreviousSessionSettings.GroupAllByAuthor;
            GroupFavoritesByAuthor = PreviousSessionSettings.GroupFavoritesByAuthor;

            _ = SetThemeAsync(PreviousSessionSettings.Theme, true, false);

            _ = SetCommaDelimitedKeywordsAsync(PreviousSessionSettings.Keywords, false);

            _ = SetFontSizeAsync(PreviousSessionSettings.GetFontSize(DefaultFontSize), false);

            _ = SetForegroundColorAsync(PreviousSessionSettings.GetForegroundColor(DefaultColorPalettes[Theme].ForegroundColor), false);
            _ = SetBackgroundColorAsync(PreviousSessionSettings.GetBackgroundColor(DefaultColorPalettes[Theme].BackgroundColor), false);
            _ = SetHighlightColorAsync(PreviousSessionSettings.GetHighlightColor(DefaultColorPalettes[Theme].HighlightColor), false);
        }

        private Dictionary<string, AuthorSettings> AuthorSettingsByName { get; }
        /// <summary>Key is given by <see cref="MainViewModel.GetStoryUniqueKey(string, string)"/></summary>
        private Dictionary<string, StorySettings> StorySettingsByAuthorAndTitle { get; }

        public AuthorSettings GetPreviousSessionAuthorSettings(string AuthorName, AuthorSettings DefaultValue)
        {
            if (AuthorSettingsByName.TryGetValue(AuthorName, out AuthorSettings Result))
                return Result;
            else
                return DefaultValue;
        }

        public StorySettings GetPreviousSessionStorySettings(LiteroticaStory Story, StorySettings DefaultValue)
        {
            string Key = MainViewModel.GetStoryUniqueKey(Story.AuthorName, Story.Title);
            if (StorySettingsByAuthorAndTitle.TryGetValue(Key, out StorySettings StorySettings))
                return StorySettings;
            else
                return DefaultValue;
        }

        public async Task SaveAsync(bool TryCreateBackup)
        {
            if (MVM.SelectedStory != null)
                MVM.SelectedStory.RecentPosition = await Bookmark.CreateAsync(MVM.WebView);

            ColorConverter ColorConverter = new();

            SavedSettings Settings = new()
            {
                OpenedCount = PreviousSessionSettings.OpenedCount + 1,

                Theme = Theme,

                WindowLeftPosition = MVM.Window.Left,
                WindowTopPosition = MVM.Window.Top,
                WindowWidth = MVM.Window.Width,
                WindowHeight = MVM.Window.Height,

                SidebarWidth = MVM.Window.SidebarColumn.ActualWidth,

                FontSize = IsUsingDefaultFontSize ? null : FontSize,

                ForegroundColor = IsUsingDefaultForegroundColor ? null : ColorConverter.ConvertToString(ForegroundColor),
                BackgroundColor = IsUsingDefaultBackgroundColor ? null : ColorConverter.ConvertToString(BackgroundColor),
                HighlightColor = IsUsingDefaultHighlightColor ? null : ColorConverter.ConvertToString(HighlightColor),

                StoriesBaseFolder = IsUsingDefaultStoriesDirectory ? null : StoriesDirectory,

                RecentSelectedStory = MVM.SelectedStory == null ? null : MainViewModel.GetStoryUniqueKey(MVM.SelectedStory.AuthorName, MVM.SelectedStory.Title),

                HistorySize = HistorySize,
                HistoryList = MVM.RecentStories.Select(x => MainViewModel.GetStoryUniqueKey(x.AuthorName, x.Title)).ToList(),

                GroupAllByAuthor = GroupAllByAuthor,
                GroupFavoritesByAuthor = GroupFavoritesByAuthor,

                Keywords = CommaDelimitedKeywords,

                AuthorSettings = MVM.AuthorGroups.Values.Select(x => new AuthorSettings(x)).ToList(),
                StorySettings = MVM.Stories.Select(x => new StorySettings(x)).ToList(),
                StoryMetadata = MVM.Stories.Select(x => x.Summary).ToList(),

                ShowCategory = DisplaySettings.ShowCategory,
                ShowDateApproved = DisplaySettings.ShowDateApproved,
                ShowReadState = DisplaySettings.ShowReadState,
                ShowOverallRating = DisplaySettings.ShowOverallRating,
                ShowUserRating = DisplaySettings.ShowUserRating,
                ShowDateDownloaded = DisplaySettings.ShowDateDownloaded,

                SaveAfterDownloading = MVM.Downloader.SaveAfterDownloading
            };
            Settings.Save();

            if (TryCreateBackup)
            {
                string Folder = SavedSettings.GetDefaultSettingsDirectory();

                const int MaxBackups = 5;
                static string GetBackupFilename(int BackupNumber) => $"settings-bak{BackupNumber}{SavedSettings.FileExt}";

                //  Find the oldest backup to overwrite
                DateTime OldestBackupWriteTime = DateTime.Now;
                string OldestBackupFilePath = null;
                for (int i = 1; i <= MaxBackups; i++)
                {
                    string BackupFilePath = Path.Combine(Folder, GetBackupFilename(i));
                    if (!File.Exists(BackupFilePath))
                    {
                        OldestBackupFilePath = BackupFilePath;
                        break;
                    }
                    else
                    {
                        DateTime WriteTime = File.GetLastWriteTime(BackupFilePath);
                        if (i == 1 || WriteTime < OldestBackupWriteTime)
                        {
                            OldestBackupWriteTime = WriteTime;
                            OldestBackupFilePath = BackupFilePath;
                        }
                    }
                }

                //  Determine how recent the newest backup is
                DateTime? NewestBackupWriteTime = null;
                for (int i = 1; i <= MaxBackups; i++)
                {
                    string BackupFilePath = Path.Combine(Folder, GetBackupFilename(i));
                    if (File.Exists(BackupFilePath))
                    {
                        DateTime WriteTime = File.GetLastWriteTime(BackupFilePath);
                        if (!NewestBackupWriteTime.HasValue || WriteTime > NewestBackupWriteTime.Value)
                            NewestBackupWriteTime = WriteTime;
                    }
                }

                //  Save the backup file to the oldest slot, as long as the oldest slot is empty or the newest backup isn't too recent
                TimeSpan RecencyThreshold = TimeSpan.FromHours(1.0);
                if (!File.Exists(OldestBackupFilePath) || !NewestBackupWriteTime.HasValue || DateTime.Now.Subtract(NewestBackupWriteTime.Value) >= RecencyThreshold)
                {
                    Settings.StoryMetadata = new(); // The metadata is only used to load the stories more quickly. It is also stored in separate files per story so it's not needed for the settings backups.
                    Settings.Save(OldestBackupFilePath);
                }
            }
        }

        public DelegateCommand<object> BrowseStoriesFolder => new(_ =>
        {
            try
            {
                CommonOpenFileDialog FolderBrowser = new CommonOpenFileDialog();
                FolderBrowser.InitialDirectory = StoriesDirectory;
                FolderBrowser.IsFolderPicker = true;
                if (FolderBrowser.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    StoriesDirectory = FolderBrowser.FileName;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        });

        public DelegateCommand<object> OpenSettingsFolder => new(_ => { GeneralUtils.ShellExecute(SavedSettings.GetDefaultSettingsDirectory()); });
    }

    public readonly record struct ColorPalette(string Name, Color ForegroundColor, Color BackgroundColor, Color HighlightColor);
}
