using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JabbR.Infrastructure;
using Nancy;
using Nancy.Validation;
using Nancy.ViewEngines.Razor;
using PagedList;
using AntiXSS = Microsoft.Security.Application;

namespace JabbR
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString CheckBox(this IViewEngine viewEngine, string Name, bool value)
        {
            string input = String.Empty;

            var checkBoxBuilder = new StringBuilder();

            checkBoxBuilder.Append(@"<input id=""");
            checkBoxBuilder.Append(AntiXSS.Encoder.HtmlAttributeEncode(Name));
            checkBoxBuilder.Append(@""" data-name=""");
            checkBoxBuilder.Append(AntiXSS.Encoder.HtmlAttributeEncode(Name));
            checkBoxBuilder.Append(@""" type=""checkbox""");
            if (value)
            {
                checkBoxBuilder.Append(@" checked=""checked"" />");
            }
            else
            {
                checkBoxBuilder.Append(" />");
            }

            checkBoxBuilder.Append(@"<input name=""");
            checkBoxBuilder.Append(AntiXSS.Encoder.HtmlAttributeEncode(Name));
            checkBoxBuilder.Append(@""" type=""hidden"" value=""");
            checkBoxBuilder.Append(value.ToString().ToLowerInvariant());
            checkBoxBuilder.Append(@""" />");

            return new HtmlString(checkBoxBuilder.ToString());
        }

        public static IHtmlString ValidationSummary(this IViewEngine viewEngine)
        {
            var validationResult = htmlHelper.RenderContext.Context.ModelValidationResult;
            if (validationResult.IsValid)
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            var summaryBuilder = new StringBuilder();

            summaryBuilder.Append(@"<ul class=""validation-summary-errors"">");
            foreach (var modelValidationError in validationResult.Errors)
            {
                foreach (var memberName in modelValidationError.MemberNames)
                {
                    summaryBuilder.AppendFormat("<li>{0}</li>", modelValidationError.GetMessage(memberName));
                }
            }
            summaryBuilder.Append(@"</ul>");

            return new NonEncodedHtmlString(summaryBuilder.ToString());
        }

        public static IHtmlString ValidationMessage(this IViewEngine viewEngine, string propertyName)
        {
            // Implementation needs to be adjusted based on Nancy 2.0 API
            // This is a placeholder implementation
            return new HtmlString("");
        }

        public static IHtmlString AlertMessages(this IViewEngine viewEngine)
        {
            // Implementation needs to be adjusted based on Nancy 2.0 API
            // This is a placeholder implementation
            return new HtmlString("");
        }

        internal static IEnumerable<ModelValidationError> GetErrorsForProperty(this IViewEngine viewEngine, string propertyName)
        {
            // Implementation needs to be adjusted based on Nancy 2.0 API
            // This is a placeholder implementation
            return Enumerable.Empty<ModelValidationError>();
        }

        public static IHtmlString SimplePager(this IViewEngine viewEngine, IPagedList pagedList, string baseUrl)
        {
            var pagerBuilder = new StringBuilder();

            pagerBuilder.Append(@"<div class=""pager"">");
            pagerBuilder.Append(@"<ul>");

            pagerBuilder.AppendFormat(@"<li class=""previous {0}"">", !pagedList.HasPreviousPage ? "disabled" : "");
            pagerBuilder.AppendFormat(@"<a href=""{0}"">&larr; Prev</a>", pagedList.HasPreviousPage ? String.Format("{0}page={1}", baseUrl, pagedList.PageNumber - 1) : "#");
            pagerBuilder.Append(@"</li>");

            pagerBuilder.AppendFormat(@"<li class=""next {0}"">", !pagedList.HasNextPage ? "disabled" : "");
            pagerBuilder.AppendFormat(@"<a href=""{0}"">Next &rarr;</a>", pagedList.HasNextPage ? String.Format("{0}page={1}", baseUrl, pagedList.PageNumber + 1) : "#");
            pagerBuilder.Append(@"</li>");

            pagerBuilder.Append(@"</ul>");
            pagerBuilder.Append(@"</div>");

            return new HtmlString(pagerBuilder.ToString());
        }

        public static IHtmlString DisplayNoneIf<TModel>(this IViewEngine viewEngine, Expression<Func<TModel, bool>> expression)
        {
            // Implementation needs to be adjusted based on Nancy 2.0 API
            // This is a placeholder implementation
            return new HtmlString(@" style=""display:none;"" ");
        }

        public static string RequestQuery(this IViewEngine viewEngine)
        {
            // Implementation needs to be adjusted based on Nancy 2.0 API
            // This is a placeholder implementation
            return String.Empty;
        }
    }
}