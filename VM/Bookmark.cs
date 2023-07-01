using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StoryManager.VM
{
    [DataContract(Name = "Bookmark", Namespace = "")]
    public class Bookmark
    {
        [DataMember(Name = "ViewerHeight")]
        public double ViewerWidth { get; set; }
        [DataMember(Name = "ViewerWidth")]
        public double ViewerHeight { get; set; }

        [DataMember(Name = "ScrollX")]
        public double ScrollXOffset { get; set; }
        [DataMember(Name = "ScrollY")]
        public double ScrollYOffset { get; set; }

        private Bookmark() { }

        public Bookmark(double ViewerWidth, double ViewerHeight, double ScrollXOffset, double ScrollYOffset)
        {
            this.ViewerWidth = ViewerWidth;
            this.ViewerHeight = ViewerHeight;
            this.ScrollXOffset = ScrollXOffset;
            this.ScrollYOffset = ScrollYOffset;
        }

        public static async Task<Bookmark> CreateAsync(WebView2 Viewer)
        {
            string xOffset = await Viewer.ExecuteScriptAsync(@"window.pageXOffset;");
            string yOffset = await Viewer.ExecuteScriptAsync(@"window.pageYOffset;");
            return new Bookmark(Viewer.ActualWidth, Viewer.ActualHeight, double.Parse(xOffset), double.Parse(yOffset));
        }

        /// <summary>Loads this <see cref="Bookmark"/> if the given <paramref name="Viewer"/>'s size is roughly equivalent to the dimensions saved by this <see cref="Bookmark"/>.</summary>
        public async Task<bool> TryLoadAsync(WebView2 Viewer, bool IgnoreViewerHeight = true)
        {
            //  Bookmarks are intended to scroll to a particular piece of content.
            //  If the dimensions of the browser have changed, the scroll offsets won't map to the same content anymore.
            const double Tolerance = 10.0;
            if (Math.Abs(Viewer.ActualWidth - ViewerWidth) <= Tolerance && (IgnoreViewerHeight || Math.Abs(Viewer.ActualHeight - ViewerHeight) <= Tolerance))
            {
                await LoadAsync(Viewer);
                return true;
            }
            else
                return false;
        }

        public async Task LoadAsync(WebView2 Viewer)
        {
            await Viewer.EnsureCoreWebView2Async();
            string Script = $@"window.scrollTo({ScrollXOffset}, {ScrollYOffset});";
            await Viewer.ExecuteScriptAsync(Script);
        }
    }
}
