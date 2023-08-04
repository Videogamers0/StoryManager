using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM
{
    public class DisplaySettings : ViewModelBase
    {
        public FilterSettings FilterSettings { get; }

        private bool _ShowCategory;
        public bool ShowCategory
        {
            get => _ShowCategory;
            set
            {
                if (_ShowCategory != value)
                {
                    _ShowCategory = value;
                    NPC(nameof(ShowCategory));
                }
            }
        }

        private bool _ShowDateApproved;
        public bool ShowDateApproved
        {
            get => _ShowDateApproved;
            set
            {
                if (_ShowDateApproved != value)
                {
                    _ShowDateApproved = value;
                    NPC(nameof(ShowDateApproved));
                }
            }
        }

        private bool _ShowReadState;
        public bool ShowReadState
        {
            get => _ShowReadState;
            set
            {
                if (_ShowReadState != value)
                {
                    _ShowReadState = value;
                    NPC(nameof(ShowReadState));
                }
            }
        }

        private bool _ShowOverallRating;
        public bool ShowOverallRating
        {
            get => _ShowOverallRating;
            set
            {
                if (_ShowOverallRating != value)
                {
                    _ShowOverallRating = value;
                    NPC(nameof(ShowOverallRating));
                }
            }
        }

        private bool _ShowPageCount;
        public bool ShowPageCount
        {
            get => _ShowPageCount;
            set
            {
                if (_ShowPageCount != value)
                {
                    _ShowPageCount = value;
                    NPC(nameof(ShowPageCount));
                }
            }
        }

        private bool _ShowWordCount;
        public bool ShowWordCount
        {
            get => _ShowWordCount;
            set
            {
                if (_ShowWordCount != value)
                {
                    _ShowWordCount = value;
                    NPC(nameof(ShowWordCount));
                }
            }
        }

        private bool _ShowUserRating;
        public bool ShowUserRating
        {
            get => _ShowUserRating;
            set
            {
                if (_ShowUserRating != value)
                {
                    _ShowUserRating = value;
                    NPC(nameof(ShowUserRating));
                }
            }
        }

        private bool _ShowDateDownloaded;
        public bool ShowDateDownloaded
        {
            get => _ShowDateDownloaded;
            set
            {
                if (_ShowDateDownloaded != value)
                {
                    _ShowDateDownloaded = value;
                    NPC(nameof(ShowDateDownloaded));
                }
            }
        }

        public DisplaySettings()
        {
            FilterSettings = new();

            ShowCategory = false;
            ShowDateApproved = false;
            ShowReadState = true;
            ShowOverallRating = false;
            ShowPageCount = false;
            ShowWordCount = false;
            ShowUserRating = false;
            ShowDateDownloaded = false;
        }

        public DisplaySettings(SavedSettings InheritFrom)
            : this()
        {
            ShowCategory = InheritFrom.ShowCategory;
            ShowDateApproved = InheritFrom.ShowDateApproved;
            ShowReadState = InheritFrom.ShowReadState;
            ShowOverallRating = InheritFrom.ShowOverallRating;
            ShowPageCount = InheritFrom.ShowPageCount;
            ShowWordCount = InheritFrom.ShowWordCount;
            ShowUserRating = InheritFrom.ShowUserRating;
            ShowDateDownloaded = InheritFrom.ShowDateDownloaded;
        }
    }
}
