using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JabbR.Infrastructure;
using Nancy.Validation;
using Nancy.ViewEngines.Razor;
using PagedList;
using AntiXSS = Microsoft.Security.Application;

namespace JabbR
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlString CheckBox<T>(this IHtmlHelper<T> helper, string Name, bool value)
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

            return new NonEncodedHtmlString(checkBoxBuilder.ToString());
        }

        public static IHtmlString ValidationSummary<TModel>(this IHtmlHelper<TModel> htmlHelper)
        {
            var validationResult = htmlHelper.ViewContext.ModelState;
            if (validationResult.IsValid)
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            var summaryBuilder = new StringBuilder();

            summaryBuilder.Append(@"<ul class=""validation-summary-errors"">");
            foreach (var modelState in validationResult)
            {
                foreach (var error in modelState.Value.Errors)
                {
                    summaryBuilder.AppendFormat("<li>{0}</li>", error.ErrorMessage);
                }
            }
            summaryBuilder.Append(@"</ul>");

            return new NonEncodedHtmlString(summaryBuilder.ToString());
        }

        public static IHtmlString ValidationMessage<TModel>(this IHtmlHelper<TModel> htmlHelper, string propertyName)
        {
            var errorsForField = htmlHelper.ViewContext.ModelState[propertyName]?.Errors;

            if (errorsForField == null || !errorsForField.Any())
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            return new NonEncodedHtmlString(errorsForField.First().ErrorMessage);
        }

        public static IHtmlString AlertMessages<TModel>(this IHtmlHelper<TModel> htmlHelper)
        {
            const string message = @"<div class=""alert alert-{0}"">{1}</div>";
            var alertsDynamicValue = htmlHelper.ViewBag.Alerts;
            var alerts = alertsDynamicValue as AlertMessageStore;

            if (alerts == null || !alerts.Messages.Any())
            {
                return new NonEncodedHtmlString(String.Empty);
            }

            var builder = new StringBuilder();

            foreach (var messageDetail in alerts.Messages)
            {
                builder.AppendFormat(message, messageDetail.Key, messageDetail.Value);
            }

            return new NonEncodedHtmlString(builder.ToString());
        }

        internal static IEnumerable<ModelError> GetErrorsForProperty<TModel>(this IHtmlHelper<TModel> htmlHelper,
                                                                         string propertyName)
        {
            var validationResult = htmlHelper.ViewContext.ModelState;
            if (validationResult.IsValid)
            {
                return Enumerable.Empty<ModelError>();
            }

            if (validationResult.TryGetValue(propertyName, out var modelStateEntry))
            {
                return modelStateEntry.Errors;
            }

            return Enumerable.Empty<ModelError>();
        }

        public static IHtmlString SimplePager<TModel>(this IHtmlHelper<TModel> htmlHelper, IPagedList pagedList, string baseUrl)
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

            return new NonEncodedHtmlString(pagerBuilder.ToString());
        }

        public static IHtmlString DisplayNoneIf<TModel>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool>> expression)
        {
            if (expression.Compile()(htmlHelper.ViewData.Model))
            {
                return new NonEncodedHtmlString(@" style=""display:none;"" ");
            }

            return NonEncodedHtmlString.Empty;
        }

        public static string RequestQuery<TModel>(this IHtmlHelper<TModel> htmlHelper)
        {
            if (htmlHelper.ViewContext.HttpContext.Request.QueryString.HasValue)
            {
                return htmlHelper.ViewContext.HttpContext.Request.QueryString.Value;
            }

            return String.Empty;
        }
    }
}