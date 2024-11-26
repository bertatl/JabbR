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
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Html;

namespace JabbR
{
    public static class HtmlHelperExtensions
    {
        public static string CheckBox(this IHtmlHelper helper, string Name, bool value)
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

            return checkBoxBuilder.ToString();
        }

        public static IHtmlContent ValidationSummary(this IHtmlHelper htmlHelper, ModelStateDictionary modelState)
        {
            if (modelState.IsValid)
            {
                return HtmlString.Empty;
            }

            var summaryBuilder = new StringBuilder();

            summaryBuilder.Append(@"<ul class=""validation-summary-errors"">");
            foreach (var modelStateEntry in modelState)
            {
                foreach (var error in modelStateEntry.Value.Errors)
                {
                    summaryBuilder.AppendFormat("<li>{0}</li>", error.ErrorMessage);
                }
            }
            summaryBuilder.Append(@"</ul>");

            return new HtmlString(summaryBuilder.ToString());
        }

        public static string ValidationMessage(this IHtmlHelper htmlHelper, string propertyName)
        {
            var errorsForField = htmlHelper.GetErrorsForProperty(propertyName).ToList();

            if (!errorsForField.Any())
            {
                return String.Empty;
            }

            return errorsForField.First().ErrorMessage;
        }

        public static string AlertMessages(this IHtmlHelper htmlHelper)
        {
            const string message = @"<div class=""alert alert-{0}"">{1}</div>";
            var alertsDynamicValue = htmlHelper.ViewContext.ViewData["Alerts"];
            var alerts = alertsDynamicValue as AlertMessageStore;

            if (alerts == null || !alerts.Messages.Any())
            {
                return String.Empty;
            }

            var builder = new StringBuilder();

            foreach (var messageDetail in alerts.Messages)
            {
                builder.AppendFormat(message, messageDetail.Key, messageDetail.Value);
            }

            return builder.ToString();
        }

        internal static IEnumerable<ModelError> GetErrorsForProperty(this IHtmlHelper htmlHelper,
                                                                         string propertyName)
        {
            if (htmlHelper.ViewContext.ModelState.IsValid)
            {
                return Enumerable.Empty<ModelError>();
            }

            if (htmlHelper.ViewContext.ModelState.TryGetValue(propertyName, out ModelStateEntry entry))
            {
                return entry.Errors;
            }

            return Enumerable.Empty<ModelError>();
        }

        public static string SimplePager(this IHtmlHelper htmlHelper, IPagedList pagedList, string baseUrl)
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

            return pagerBuilder.ToString();
        }

        public static string DisplayNoneIf(this IHtmlHelper htmlHelper, Expression<Func<object, bool>> expression)
        {
            if (expression.Compile()(htmlHelper.Model))
            {
                return @" style=""display:none;"" ";
            }

            return String.Empty;
        }

        public static string RequestQuery(this IHtmlHelper htmlHelper)
        {
            if (htmlHelper.RenderContext.Context.Request.Url != null && !String.IsNullOrEmpty(htmlHelper.RenderContext.Context.Request.Url.Query))
            {
                return "?" + htmlHelper.RenderContext.Context.Request.Url.Query;
            }

            return String.Empty;
        }
    }
}