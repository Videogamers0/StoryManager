using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace StoryManager.VM.Helpers
{
    public static class CollectionViewHelpers
    {
        /// <summary>Creates a <see cref="ICollectionView"/> with live sorting and filtering on the given parameters.</summary>
        /// <param name="items">The source items for the collection view</param>
        /// <param name="sortingPropertyPaths">The paths to the properties that will be sorted on. All <see cref="SortDescription"/>s will use <see cref="ListSortDirection.Ascending"/> order.</param>
        /// <param name="ComputeIsVisible">A predicate that returns true if the item should be visible. Can be null if no filtering is needed.<para/>
        /// EX: "x => return x.Foo &amp;&amp; x.Bar" / "x => x.IsVisible"</param>
        /// <param name="filterPropertyPath">The path to the property that should be used for live filtering. Can be null if filtering does not need to be dynamically updated.<para/>
        /// If the filter predicate requires multiple live filtering properties, use <see cref="GetSortedFilteredCollectionView{T}(ICollection{T}, IEnumerable{SortDescription}, Predicate{T}, IEnumerable{string})"/> instead.</param>
        public static ICollectionView GetSortedFilteredCollectionView<T>(ICollection<T> items, Predicate<T> ComputeIsVisible, string filterPropertyPath, params string[] sortingPropertyPaths)
        {
            IEnumerable<SortDescription> pSortDescriptions = sortingPropertyPaths.Select(x => new SortDescription(x, ListSortDirection.Ascending));
            return GetSortedFilteredCollectionView(items, ComputeIsVisible, new List<string>() { filterPropertyPath }, pSortDescriptions.ToArray());
        }

        public static ICollectionView GetSortedFilteredCollectionView<T>(ICollection<T> items, Predicate<T> ComputeIsVisible, string filterPropertyPath, params SortDescription[] sortDescriptions)
            => GetSortedFilteredCollectionView(items, ComputeIsVisible, new List<string>() { filterPropertyPath }, sortDescriptions);

        //https://stackoverflow.com/a/48543927/11689514
        //Store references to the generated CollectionViewSource objects so they don't get garbage collected
        private static readonly List<CollectionViewSource> CVSList = new();

        /// <summary>Creates a <see cref="ICollectionView"/> with live sorting and filtering on the given parameters.</summary>
        /// <param name="items">The source items for the collection view</param>
        /// <param name="ComputeIsVisible">A predicate that returns true if the item should be visible. Can be null if no filtering is needed.<para/>
        /// EX: "x => return x.Foo &amp;&amp; x.Bar" / "x => x.IsVisible"</param>
        /// <param name="filterPropertyPaths">The paths to the properties that should be used for live filtering. Can be null if filtering does not need to be dynamically updated.</param>
        public static ICollectionView GetSortedFilteredCollectionView<T>(ICollection<T> items, Predicate<T> ComputeIsVisible, IEnumerable<string> filterPropertyPaths, params SortDescription[] sortDescriptions)
        {
            if (items == null)
                return null;

            CollectionViewSource CSV = new CollectionViewSource() { Source = items };
            CVSList.Add(CSV);
            ICollectionView CV = CSV.View;
            //ICollectionView CV = new ListCollectionView(pItems);

            using (CV.DeferRefresh())
            {
                //  Apply sorting
                if (CV.CanSort && sortDescriptions?.Any() == true)
                {
                    foreach (SortDescription pSortDesc in sortDescriptions)
                        CV.SortDescriptions.Add(pSortDesc);

                    //  Apply live-sorting
                    if (CV is ICollectionViewLiveShaping LiveCV)
                    {
                        foreach (SortDescription pSortDesc in sortDescriptions)
                            LiveCV.LiveSortingProperties.Add(pSortDesc.PropertyName);

                        LiveCV.IsLiveSorting = true;
                    }
                }

                //  Apply filtering
                if (CV.CanFilter && ComputeIsVisible != null)
                {
                    CV.Filter += (sender) =>
                    {
                        return sender is T pTypedItem && ComputeIsVisible(pTypedItem);
                    };

                    //  Apply live-filtering
                    if (CV is ICollectionViewLiveShaping LiveCV && filterPropertyPaths?.Any() == true)
                    {
                        foreach (string szFilterProperty in filterPropertyPaths)
                            LiveCV.LiveFilteringProperties.Add(szFilterProperty);

                        LiveCV.IsLiveFiltering = true;
                    }
                }
            }

            return CV;
        }
    }
}