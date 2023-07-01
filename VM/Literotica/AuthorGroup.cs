using Prism.Commands;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StoryManager.VM.Literotica
{
    public class AuthorGroup : ViewModelBase
    {
        public MainViewModel MVM { get; }

        public LiteroticaAuthor Author { get; }
        public string AuthorName => Author.username;

        public string Url => $"https://www.literotica.com/stories/memberpage.php?uid={Author.userid}&page=submissions";

        public bool IsSelected => MVM.SelectedStory?.AuthorName == AuthorName;

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

        private bool _IsFavorited;
        public bool IsFavorited
        {
            get => _IsFavorited;
            set
            {
                if (_IsFavorited != value)
                {
                    _IsFavorited = value;
                    NPC(nameof(IsFavorited));
                    OnIsFavoritedChanged?.Invoke(this, IsFavorited);
                }
            }
        }

        public event EventHandler<bool> OnIsFavoritedChanged;

        public DelegateCommand<object> AddToFavorites => new(_ => IsFavorited = true);
        public DelegateCommand<object> RemoveFromFavorites => new(_ => IsFavorited = false);

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

        private bool _IsExpanded;
        public bool IsExpanded
        {
            get => _IsExpanded;
            set
            {
                if (_IsExpanded != value)
                {
                    _IsExpanded = value;
                    NPC(nameof(IsExpanded));
                }
            }
        }

        public AuthorGroup(MainViewModel MVM, LiteroticaAuthor author)
        {
            this.MVM = MVM;
            Author = author;

            this.UserRating = null;
            this.UserNotes = "";

            this.IsFavorited = false;
            this.IsIgnored = false;
            this.IsRead = false;
            this.IsExpanded = true;
        }

        public DelegateCommand<object> OpenAuthorWebpage => new(_ => GeneralUtils.OpenUrl(Url, true));
        public DelegateCommand<object> CopyAuthorWebpageToClipboard => new(_ => Clipboard.SetText(Url));
    }
}
