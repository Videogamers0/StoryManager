using Prism.Commands;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM.Literotica
{
    public class SearchResult : ViewModelBase
    {
        public StorySearcher Searcher { get; }
        public string Query { get; }
        public int PageNumber { get; }
        public LiteroticaSearchResults Data { get; }

        public int FirstResultIndex => Data.meta.pageSize * (PageNumber - 1) + 1;
        public int LastResultIndex => Data.meta.pageSize * PageNumber;
        public int TotalResults => Data.meta.total;

        public int MaxPageNumber => (TotalResults - 1) / Data.meta.pageSize + 1;
        public bool IsFirstPage => PageNumber == 1;
        public bool IsLastPage => PageNumber == MaxPageNumber;

        public ReadOnlyCollection<SearchResultPage> Pages { get; }

        public SearchResult(StorySearcher Searcher, string Query, int PageNumber, LiteroticaSearchResults Data)
        {
            this.Searcher = Searcher;
            this.Query = Query;
            this.PageNumber = PageNumber;
            this.Data = Data;

            //  Create page navigation buttons displaying the first, last, and current page number, as well as a few pages +/- the current
            //  EX: If current page number is 35, and last page is 122: 1 33 34 35 36 37 122
            bool IsValidPageNumber(int x) => x >= 1 && x <= MaxPageNumber;
            HashSet<int> NavigationPageNumbers = new() { 1, MaxPageNumber };

            int Offset = 0;
            while (NavigationPageNumbers.Count < 7) // Min, Max, Current, and then 2 pages on each side of current
            {
                int Prev = PageNumber - Offset;
                int Next = PageNumber + Offset;
                if (!IsValidPageNumber(Prev) && !IsValidPageNumber(Next))
                    break;
                else
                {
                    if (IsValidPageNumber(Prev))
                        NavigationPageNumbers.Add(Prev);
                    if (IsValidPageNumber(Next))
                        NavigationPageNumbers.Add(Next);
                }

                Offset++;
            }

            Pages = NavigationPageNumbers.Distinct().Order().Select(x => new SearchResultPage(this, x)).ToList().AsReadOnly();
        }

        public DelegateCommand<object> PreviousPage => new(_ => _ = Searcher.SearchAsync(Query, Math.Max(1, PageNumber - 1)));
        public DelegateCommand<object> NextPage => new(_ => _ = Searcher.SearchAsync(Query, Math.Min(MaxPageNumber, PageNumber + 1)));
    }

    public class SearchResultPage : ViewModelBase
    {
        public SearchResult SearchResult { get; }
        public int PageNumber { get; }
        public bool IsSelected => PageNumber == SearchResult.PageNumber;

        public SearchResultPage(SearchResult SearchResult, int PageNumber)
        {
            this.SearchResult = SearchResult;
            this.PageNumber = PageNumber;
        }

        public DelegateCommand<object> Open => new(_ => _ = SearchResult.Searcher.SearchAsync(SearchResult.Query, PageNumber));
    }
}
