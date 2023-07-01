using Prism.Commands;
using StoryManager.VM.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoryManager.VM.Literotica
{
    public class StoryNavigationButton : ViewModelBase
    {
        public LiteroticaStory StoryVM { get; }
        public SerializableChapter Chapter { get; }
        public SerializablePage Page { get; }

        public int ChapterIndex { get; }
        public int LocalPageIndex { get; }
        public int OverallPageIndex { get; }

        public int ChapterDisplayNumber => ChapterIndex + 1;
        public int LocalPageDisplayNumber => LocalPageIndex + 1;
        public int OverallPageDisplayNumber => OverallPageIndex + 1;

        public StoryNavigationButton(LiteroticaStory StoryVM, SerializableChapter Chapter, SerializablePage Page, int ChapterIndex, int PageIndexWithinChapter, int OverallPageIndex)
        {
            this.StoryVM = StoryVM;
            this.Chapter = Chapter;
            this.Page = Page;
            this.ChapterIndex = ChapterIndex;
            this.LocalPageIndex = PageIndexWithinChapter;
            this.OverallPageIndex = OverallPageIndex;
        }

        public DelegateCommand<object> ScrollTo => new((_) =>
        {
            if (LocalPageIndex == 0)
                _ = StoryVM.TryScrollToChapter(ChapterIndex);
            else
                _ = StoryVM.TryScrollToChapterAndPage(ChapterIndex, LocalPageIndex);
        });
    }
}
