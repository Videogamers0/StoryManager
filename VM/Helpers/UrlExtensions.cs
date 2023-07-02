using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace StoryManager.VM.Helpers
{
    #region https://stackoverflow.com/a/12410826
    public static class UrlExtensions
    {
        public static string SetUrlParameter(this string url, string paramName, string value)
        {
            return new Uri(url).SetParameter(paramName, value).ToString();
        }

        public static Uri SetParameter(this Uri url, string paramName, string value)
        {
            var queryParts = HttpUtility.ParseQueryString(url.Query);
            queryParts[paramName] = value;
            return new Uri(url.AbsoluteUriExcludingQuery() + '?' + queryParts.ToString());
        }

        public static string AbsoluteUriExcludingQuery(this Uri url)
        {
            return url.AbsoluteUri.Split('?').FirstOrDefault() ?? String.Empty;
        }
    }
    #endregion
}
