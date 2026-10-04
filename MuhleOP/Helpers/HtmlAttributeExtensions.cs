using System.Web.Mvc;
using System.Web.Routing;

namespace MuhleOP.Helpers
{
    public static class HtmlAttributeExtensions
    {
        /// <summary>
        /// Builds an HTML attribute dictionary and adds boolean attributes (disabled, readonly)
        /// ONLY when the condition is true.
        ///
        /// Never pass "disabled = null" (or false/"") to an HtmlHelper: the key is still rendered
        /// (disabled="" / disabled="False"), and browsers treat any present boolean attribute as true.
        /// </summary>
        public static RouteValueDictionary Attrs(object attributes, bool disabled = false, bool readOnly = false)
        {
            var dict = HtmlHelper.AnonymousObjectToHtmlAttributes(attributes);
            if (disabled) dict["disabled"] = "disabled";
            if (readOnly) dict["readonly"] = "readonly";
            return dict;
        }
    }
}