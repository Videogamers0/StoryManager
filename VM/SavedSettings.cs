using Newtonsoft.Json;
using StoryManager.VM;
using StoryManager.VM.Helpers;
using StoryManager.VM.Literotica;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace StoryManager
{
    [DataContract(Name = "StoryManagerSettings", Namespace = "")]
    public class SavedSettings
    {
        [DataMember(Name = "OpenedCount")]
        public int OpenedCount { get; set; }

        [DataMember(Name = "Theme")]
        public Theme Theme { get; set; }

        [DataMember(Name = "WindowLeftPosition")]
        public double? WindowLeftPosition { get; set; }
        [DataMember(Name = "WindowTopPosition")]
        public double? WindowTopPosition { get; set; }

        [JsonIgnore]
        public bool HasPosition => WindowLeftPosition.HasValue && WindowTopPosition.HasValue;

        [DataMember(Name = "WindowWidth")]
        public double? WindowWidth { get; set; }
        [DataMember(Name = "WindowHeight")]
        public double? WindowHeight { get; set; }

        [DataMember(Name = "SidebarWidth")]
        public double? SidebarWidth { get; set; }

        [DataMember(Name = "FontSize")]
        public int? FontSize { get; set; }

        [DataMember(Name = "FontFamily")]
        public string FontFamily { get; set; }

        [DataMember(Name = "ForegroundColor")]
        public string ForegroundColor { get; set; }
        [DataMember(Name = "BackgroundColor")]
        public string BackgroundColor { get; set; }
        [DataMember(Name = "HighlightColor")]
        public string HighlightColor { get; set; }

        [DataMember(Name = "StoriesBaseFolder")]
        public string StoriesBaseFolder { get; set; }

        [DataMember(Name = "RecentSelectedStory")]
        public string RecentSelectedStory { get; set; }

        [DataMember(Name = "HistorySize")]
        public int? HistorySize { get; set; }
        [DataMember(Name = "HistoryList")]
        public List<string> HistoryList { get; set; }

        [DataMember(Name = "GroupAllByAuthor")]
        public bool GroupAllByAuthor { get; set; }
        [DataMember(Name = "GroupFavoritesByAuthor")]
        public bool GroupFavoritesByAuthor { get; set; }

        [DataMember(Name = "SortingMode")]
        public SortBy SortingMode { get; set; }

        [DataMember(Name = "Keywords")]
        public string Keywords { get; set; }

        [DataMember(Name = "AuthorSettings")]
        public List<AuthorSettings> AuthorSettings { get; set; }
        [DataMember(Name = "StorySettings")]
        public List<StorySettings> StorySettings { get; set; }
        [DataMember(Name = "StoryMetadata")]
        public List<SerializableStory> StoryMetadata { get; set; }

        [DataMember(Name = "ShowCategory")]
        public bool ShowCategory { get; set; }
        [DataMember(Name = "ShowDateApproved")]
        public bool ShowDateApproved { get; set; }
        [DataMember(Name = "ShowReadState")]
        public bool ShowReadState { get; set; }
        [DataMember(Name = "ShowOverallRating")]
        public bool ShowOverallRating { get; set; }
        [DataMember(Name = "ShowPageCount")]
        public bool ShowPageCount { get; set; }
        [DataMember(Name = "ShowWordCount")]
        public bool ShowWordCount { get; set; }
        [DataMember(Name = "ShowUserRating")]
        public bool ShowUserRating { get; set; }
        [DataMember(Name = "ShowDateDownloaded")]
        public bool ShowDateDownloaded { get; set; }

        [DataMember(Name = "SaveAfterDownloading")]
        public bool SaveAfterDownloading { get; set; }

        [DataMember(Name = "WarnIfClosingUnsavedStory")]
        public bool WarnIfClosingUnsavedStory { get; set; }

        public SavedSettings()
        {
            InitializeDefaults();
        }

        private void InitializeDefaults()
        {
            OpenedCount = 1;

            Theme = Theme.DarkMode;

            WindowLeftPosition = null;
            WindowTopPosition = null;
            WindowWidth = null;
            WindowHeight = null;

            SidebarWidth = null;

            FontSize = null;
            FontFamily = null;

            ForegroundColor = null;
            BackgroundColor = null;
            HighlightColor = null;

            StoriesBaseFolder = null;

            RecentSelectedStory = null;

            HistorySize = null;
            HistoryList = new();

            GroupAllByAuthor = true;
            GroupFavoritesByAuthor = true;

            SortingMode = SortBy.Title;

            Keywords = "";

            AuthorSettings = new();
            StorySettings = new();
            StoryMetadata = new();

            ShowCategory = false;
            ShowDateApproved = false;
            ShowReadState = true;
            ShowOverallRating = false;
            ShowPageCount = false;
            ShowWordCount = false;
            ShowUserRating = false;
            ShowDateDownloaded = false;

            SaveAfterDownloading = true;

            WarnIfClosingUnsavedStory = true;
        }

        public int GetFontSize(int DefaultValue) => FontSize ?? DefaultValue;

        public string GetFontFamily(string DefaultValue) => FontFamily ?? DefaultValue;

        public Color GetForegroundColor(Color DefaultValue) => ForegroundColor == null ? DefaultValue : (Color)ColorConverter.ConvertFromString(ForegroundColor);
        public Color GetBackgroundColor(Color DefaultValue) => BackgroundColor == null ? DefaultValue : (Color)ColorConverter.ConvertFromString(BackgroundColor);
        public Color GetHighlightColor(Color DefaultValue) => HighlightColor == null ? DefaultValue : (Color)ColorConverter.ConvertFromString(HighlightColor);

        #region Serialization
#if XML_Settings
        internal const string FileExt = ".xml";
#else
        internal const string FileExt = ".json";
#endif

#if LEGACY_SETTINGS_PATH
        internal static readonly string AssemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        internal static readonly string DefaultSettingsFilename = $"{nameof(StoryManager)} Settings{FileExt}";
        internal static string GetDefaultSettingsDirectory() => AssemblyPath;
        internal static string GetDefaultSettingsPath() => Path.Combine(AssemblyPath, DefaultSettingsFilename);
#else
        internal static readonly string DefaultSettingsFilename = $"settings{FileExt}";
        internal static string GetDefaultSettingsDirectory() => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), nameof(StoryManager));
        internal static string GetDefaultSettingsPath() => Path.Combine(GetDefaultSettingsDirectory(), DefaultSettingsFilename);
#endif

        /// <summary>Saves this settings instance to the default file path. See also: <see cref="Save(string)"/></summary>
        /// <returns>True if successful</returns>
        internal bool Save() => Save(GetDefaultSettingsPath());
        internal bool Save(string FilePath)
        {
#if XML_Settings
            XMLSerializer.Serialize(this, FilePath, out bool Success, out Exception Error);
            if (!Success)
                Debug.WriteLine(Error.ToString());
            return Success;
#else
            try
            {
                string Json = GeneralUtils.SerializeJson(this, true);
                File.WriteAllText(FilePath, Json);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return false;
            }
#endif
        }

        /// <param name="Exists">True if the default settings file was found, false if a new empty settings object is returned</param>
        internal static SavedSettings Load(out bool Exists)
        {
#if XML_Settings
            Settings Settings = XMLSerializer.Deserialize<Settings>(GetDefaultSettingsPath());
            if (Settings == null)
            {
                Settings = new Settings();
                Exists = false;
            }
            else
                Exists = true;
            return Settings;
#else
            string Path = GetDefaultSettingsPath();
            Exists = File.Exists(Path);
            if (Exists)
            {
                SavedSettings Settings = GeneralUtils.DeserializeJson<SavedSettings>(File.ReadAllText(Path));
                return Settings;
            }
            else
                return new SavedSettings();
#endif
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) => InitializeDefaults();
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
#endregion Serialization
    }

    [DataContract(Name = "AuthorSettings", Namespace = "")]
    public class AuthorSettings
    {
        [DataMember(Name = "Author")]
        public string Author { get; set; }

        [DataMember(Name = "UserRating")]
        public double? UserRating { get; set; }

        [DataMember(Name = "UserNotes")]
        public string UserNotes { get; set; }

        [DataMember(Name = "IsFavorited")]
        public bool IsFavorited { get; set; }
        [DataMember(Name = "IsIgnored")]
        public bool IsIgnored { get; set; }
        [DataMember(Name = "IsRead")]
        public bool IsRead { get; set; }
        [DataMember(Name = "IsExpanded")]
        public bool IsExpanded { get; set; }

        public AuthorSettings()
        {
            InitializeDefaults();
        }

        public AuthorSettings(AuthorGroup Group)
            : this()
        {
            Author = Group.AuthorName;

            UserRating = Group.UserRating;
            UserNotes = Group.UserNotes;

            IsFavorited = Group.IsFavorited;
            IsIgnored = Group.IsIgnored;
            IsRead = Group.IsRead;
            IsExpanded = Group.IsExpanded;
        }

        public void ApplyTo(AuthorGroup Group)
        {
            if (Group == null)
                return;

            Group.UserRating = UserRating;
            Group.UserNotes = UserNotes;

            Group.IsFavorited = IsFavorited;
            Group.IsIgnored = IsIgnored;
            Group.IsRead = IsRead;
            Group.IsExpanded = IsExpanded;
        }

        private void InitializeDefaults()
        {
            Author = null;

            UserRating = null;
            UserNotes = null;

            IsFavorited = false;
            IsIgnored = false;
            IsRead = false;
            IsExpanded = true;
        }

        #region Serialization
        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) => InitializeDefaults();
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
        #endregion Serialization
    }

    [DataContract(Name = "StorySettings", Namespace = "")]
    public class StorySettings
    {
        [DataMember(Name = "Author")]
        public string Author { get; set; }
        [DataMember(Name = "Title")]
        public string Title { get; set; }

        [DataMember(Name = "Rating")]
        public double? UserRating { get; set; }

        [DataMember(Name = "UserNotes")]
        public string UserNotes { get; set; }

        [DataMember(Name = "UserTags")]
        public List<string> UserTags { get; set; }

        [DataMember(Name = "IsFavorited")]
        public bool IsFavorited { get; set; }
        [DataMember(Name = "IsIgnored")]
        public bool IsIgnored { get; set; }
        [DataMember(Name = "IsRead")]
        public bool IsRead { get; set; }

        /// <summary>If <see langword="true" />, indicates that the user has added this story to their 'Read-later' list</summary>
        [DataMember(Name = "QueuedAt")]
        public DateTime? QueuedAt { get; set; }
        [DataMember(Name = "LastOpenedAt")]
        public DateTime? LastOpenedAt { get; set; }
        [DataMember(Name = "DownloadedAt")]
        public DateTime? DownloadedAt { get; set; }

        [DataMember(Name = "RecentPosition")]
        public Bookmark RecentPosition { get; set; }

        public StorySettings()
        {
            InitializeDefaults();
        }

        public StorySettings(LiteroticaStory Story)
            : this()
        {
            Author = Story.AuthorName;
            Title = Story.Title;

            UserRating = Story.UserRating;
            UserNotes = Story.UserNotes;
            UserTags = new(); //TODO

            IsFavorited = Story.IsStoryFavorited;
            IsIgnored = Story.IsIgnored;
            IsRead = Story.IsRead;

            QueuedAt = Story.QueuedAt;
            LastOpenedAt = Story.LastOpenedAt;
            DownloadedAt = Story.DownloadedAt;

            RecentPosition = Story.RecentPosition;
        }

        public void ApplyTo(LiteroticaStory Story)
        {
            if (Story == null)
                return;

            Story.UserRating = UserRating;
            Story.UserNotes = UserNotes;
            //Story.UserTags = UserTags //TODO

            Story.IsStoryFavorited = IsFavorited;
            Story.IsIgnored = IsIgnored;
            Story.IsRead = IsRead;

            Story.QueuedAt = QueuedAt;
            Story.LastOpenedAt = LastOpenedAt;

            Story.RecentPosition = RecentPosition;
        }

        private void InitializeDefaults()
        {
            Author = null;
            Title = null;

            UserRating = null;
            UserNotes = "";

            UserTags = new();

            IsFavorited = false;
            IsIgnored = false;
            IsRead = false;

            QueuedAt = null;
            LastOpenedAt = null;
            DownloadedAt = null;

            RecentPosition = null;
        }

        #region Serialization
        [OnSerializing]
        private void OnSerializing(StreamingContext sc) { }
        [OnSerialized]
        private void OnSerialized(StreamingContext sc) { }
        [OnDeserializing]
        private void OnDeserializing(StreamingContext sc) => InitializeDefaults();
        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc) { }
        #endregion Serialization
    }
}
