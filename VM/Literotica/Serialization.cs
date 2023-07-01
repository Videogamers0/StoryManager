using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM.Literotica
{
    public class SerializableStory
    {
        public string Version { get; set; } = new Version(1, 0, 0, 0).ToString();

        public LiteroticaAuthor Author { get; set; }
        public string Title { get; set; }
        public ReadOnlyCollection<SerializableChapter> Chapters { get; set; }

        public DateTime DownloadedAt { get; set; }

        public bool IsSummary { get; set; }

        /// <summary>Only intended to be used during deserialization.</summary>
        private SerializableStory() { }

        public SerializableStory(LiteroticaPage InitialPage, IEnumerable<SerializableChapter> Chapters)
        {
            this.Version = MainViewModel.FileVersion.ToString();

            this.Chapters = Chapters.ToList().AsReadOnly();

            Author = InitialPage.submission.author;
            Title = InitialPage.submission.series?.meta.title ?? InitialPage.submission.title;

            DownloadedAt = DateTime.Now;

            IsSummary = false;
        }

        /// <summary>Returns a lightweight copy of the story's data which doesn't contain the content of each page.<para/>
        /// Intended to be used for performance purposes, so a story's metadata can quickly be loaded without loading everything until user selects the story.</summary>
        public SerializableStory AsSummary() => new()
        {
            Version = Version,
            Author = Author,
            Title = Title,
            Chapters = Chapters.Select(x => x.AsSummary()).ToList().AsReadOnly(),
            DownloadedAt = DownloadedAt,
            IsSummary = true
        };

        public override string ToString() => $"{nameof(SerializableStory)}: {Title} by {Author.username}";
    }

    public class SerializableChapter
    {
        [JsonIgnore]
        public string FullUrl => $"https://www.literotica.com/s/{Url}";
        public string Url { get; set; }

        public LiteroticaCategory Category { get; set; }
        public int CategoryId { get; set; }
        public string DateApproved { get; set; }
        public string Description { get; set; }
        public int Id { get; set; }
        public double? Rating { get; set; }
        public ReadOnlyCollection<string> Tags { get; set; }
        public string Title { get; set; }
        public int ViewCount { get; set; }
        public int WordCount { get; set; }

        public ReadOnlyCollection<SerializablePage> Pages { get; set; }

        /// <summary>Only intended to be used during deserialization.</summary>
        private SerializableChapter() { }

        public SerializableChapter(IEnumerable<LiteroticaPage> Pages)
        {
            this.Pages = Pages.Select(x => new SerializablePage(x)).ToList().AsReadOnly();

            LiteroticaPage FirstPage = Pages.First();
            LiteroticaSubmission Submission = FirstPage.submission;

            Url = Submission.url;

            Category = Submission.category_info;
            CategoryId = Submission.category;
            DateApproved = Submission.date_approve;
            Description = Submission.description;
            Id = Submission.id;
            Rating = Submission.rate_all;
            Tags = Submission.tags.Select(x => x.tag).ToList().AsReadOnly();
            Title = Submission.title;
            ViewCount = Submission.view_count;
            WordCount = Submission.words_count;
        }

        public SerializableChapter AsSummary() => new()
        {
            Url = Url,
            Category = Category,
            CategoryId = CategoryId,
            DateApproved = DateApproved,
            Description = Description,
            Id = Id,
            Rating = Rating,
            Tags = Tags.ToList().AsReadOnly(),
            Title = Title,
            ViewCount = ViewCount,
            WordCount = WordCount,
            Pages = null,// Pages.Select(x => x.AsSummary()).ToList().AsReadOnly(),
        };

        public override string ToString() => $"{nameof(SerializableChapter)}: {Title}";
    }

    public class SerializablePage
    {
        public string Content { get; set; }

        /// <summary>Only intended to be used during deserialization.</summary>
        private SerializablePage() { }

        public SerializablePage(LiteroticaPage Page)
        {
            this.Content = Page.pageText;
        }

        public SerializablePage AsSummary() => new() { };
    }
}
