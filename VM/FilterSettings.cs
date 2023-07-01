using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM
{
    public class FilterSettings : ViewModelBase
    {
		private bool _HideRead;
		public bool HideRead
		{
			get => _HideRead;
			set
			{
				if (_HideRead != value)
				{
					_HideRead = value;
					NPC(nameof(HideRead));
					InvokeFiltersChanged();
				}
			}
		}

		private bool _HideUnread;
		public bool HideUnread
		{
			get => _HideUnread;
			set
			{
				if (_HideUnread != value)
				{
					_HideUnread = value;
					NPC(nameof(HideUnread));
					InvokeFiltersChanged();
				}
			}
		}

		private bool _HideFavorited;
		public bool HideFavorited
		{
			get => _HideFavorited;
			set
			{
				if (_HideFavorited != value)
				{
					_HideFavorited = value;
					NPC(nameof(HideFavorited));
                    InvokeFiltersChanged();
                }
			}
		}

		private bool _HideUnfavorited;
		public bool HideUnfavorited
		{
			get => _HideUnfavorited;
			set
			{
				if (_HideUnfavorited != value)
				{
					_HideUnfavorited = value;
					NPC(nameof(HideUnfavorited));
                    InvokeFiltersChanged();
                }
			}
		}

		private bool _HideIgnored;
		public bool HideIgnored
		{
			get => _HideIgnored;
			set
			{
				if (_HideIgnored != value)
				{
					_HideIgnored = value;
					NPC(nameof(HideIgnored));
					InvokeFiltersChanged();
				}
			}
		}

		private bool _HideRated;
		public bool HideRated
		{
			get => _HideRated;
			set
			{
				if (_HideRated != value)
				{
					_HideRated = value;
					NPC(nameof(HideRated));
					InvokeFiltersChanged();
				}
			}
		}

		private bool _HideUnrated;
		public bool HideUnrated
		{
			get => _HideUnrated;
			set
			{
				if (_HideUnrated != value)
				{
					_HideUnrated = value;
					NPC(nameof(HideUnrated));
					InvokeFiltersChanged();
				}
			}
		}

		private bool _FilterByRating;
		public bool FilterByRating
		{
			get => _FilterByRating;
			set
			{
				if (_FilterByRating != value)
				{
					_FilterByRating = value;
					NPC(nameof(FilterByRating));
					InvokeFiltersChanged();
				}
			}
		}

		private int _MinRating;
		public int MinRating
		{
			get => _MinRating;
			set
			{
				if (_MinRating != value)
				{
					_MinRating = value;
					NPC(nameof(MinRating));
					if (FilterByRating)
						InvokeFiltersChanged();
				}
			}
		}

		private int _MaxRating;
		public int MaxRating
		{
			get => _MaxRating;
			set
			{
				if (_MaxRating != value)
				{
					_MaxRating = value;
					NPC(nameof(MaxRating));
					if (FilterByRating)
						InvokeFiltersChanged();
				}
			}
		}

		private bool _FilterByWordCount;
		public bool FilterByWordCount
		{
			get => _FilterByWordCount;
			set
			{
				if (_FilterByWordCount != value)
				{
					_FilterByWordCount = value;
					NPC(nameof(FilterByWordCount));
					InvokeFiltersChanged();
				}
			}
		}

		private int _TotalMaxWordCount;
		public int TotalMaxWordCount
		{
			get => _TotalMaxWordCount;
			set
			{
				if (_TotalMaxWordCount != value)
				{
					int PreviousMax = TotalMaxWordCount;
					_TotalMaxWordCount = value;
					NPC(nameof(TotalMaxWordCount));
					if (MaxWordCount == PreviousMax)
						MaxWordCount = TotalMaxWordCount;
				}
			}
		}

		private int _MinWordCount;
		public int MinWordCount
		{
			get => _MinWordCount;
			set
			{
				if (_MinWordCount != value)
				{
					_MinWordCount = value;
					NPC(nameof(MinWordCount));
                    if (FilterByWordCount)
                        InvokeFiltersChanged();
                }
			}
		}

        private int _MaxWordCount;
        public int MaxWordCount
        {
            get => _MaxWordCount;
            set
            {
                if (_MaxWordCount != value)
                {
                    _MaxWordCount = value;
                    NPC(nameof(MaxWordCount));
                    if (FilterByWordCount)
                        InvokeFiltersChanged();
                }
            }
        }

		private bool _FilterByApprovalDate;
		public bool FilterByApprovalDate
		{
			get => _FilterByApprovalDate;
			set
			{
				if (_FilterByApprovalDate != value)
				{
					_FilterByApprovalDate = value;
					NPC(nameof(FilterByApprovalDate));
					InvokeFiltersChanged();
				}
			}
		}

		private DateTime? _MinApprovalDate;
		public DateTime? MinApprovalDate
		{
			get => _MinApprovalDate;
			set
			{
				if (_MinApprovalDate != value)
				{
					_MinApprovalDate = value;
					NPC(nameof(MinApprovalDate));
					if (FilterByApprovalDate)
						InvokeFiltersChanged();
				}
			}
		}

		private bool _FilterByDownloadDate;
		public bool FilterByDownloadDate
		{
			get => _FilterByDownloadDate;
			set
			{
				if (_FilterByDownloadDate != value)
				{
					_FilterByDownloadDate = value;
					NPC(nameof(FilterByDownloadDate));
					InvokeFiltersChanged();
				}
			}
		}

		private DateTime? _MinDownloadDate;
		public DateTime? MinDownloadDate
		{
			get => _MinDownloadDate;
			set
			{
				if (_MinDownloadDate != value)
				{
					_MinDownloadDate = value;
					NPC(nameof(MinDownloadDate));
					if (FilterByDownloadDate)
						InvokeFiltersChanged();
				}
			}
		}

        private void InvokeFiltersChanged() => FiltersChanged?.Invoke(this, EventArgs.Empty);
        public event EventHandler<EventArgs> FiltersChanged;

		public FilterSettings()
		{
			HideRead = false;
			HideUnread = false;

			HideFavorited = false;
			HideUnfavorited = false;

			HideIgnored = true;

			HideRated = false;
			HideUnrated = false;
			FilterByRating = false;
			MinRating = 1;
			MaxRating = 5;

			FilterByWordCount = false;
			TotalMaxWordCount = 100;
			MinWordCount = 0;
			MaxWordCount = TotalMaxWordCount;

			FilterByApprovalDate = false;
			MinApprovalDate = null;
			FilterByDownloadDate = false;
			MinDownloadDate = null;
		}
    }
}
