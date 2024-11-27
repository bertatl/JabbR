using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JabbR.Infrastructure;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using PagedList;
using AntiXSS = Microsoft.Security.Application;

namespace JabbR
{
    public static class HtmlHelperExtensions
    {
        public static IHtmlContent CheckBox<T>(this IHtmlHelper<T> helper, string Name, bool value)
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

        public static IHtmlContent ValidationSummary<TModel>(this IHtmlHelper<TModel> htmlHelper)
        {
            var validationResult = htmlHelper.ViewContext.ModelState;
            if (validationResult.IsValid)
            {
                return HtmlString.Empty;
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

        public static IHtmlContent ValidationMessage<TModel>(this IHtmlHelper<TModel> htmlHelper, string propertyName)
        {
            var errorsForField = htmlHelper.ViewContext.ModelState[propertyName]?.Errors;

            if (errorsForField == null || !errorsForField.Any())
            {
                return HtmlString.Empty;
            }

            return new HtmlString(errorsForField.First().ErrorMessage);
        }

        public static IHtmlContent AlertMessages<TModel>(this IHtmlHelper<TModel> htmlHelper)
        {
            const string message = @"<div class=""alert alert-{0}"">{1}</div>";
            var alerts = htmlHelper.ViewBag.Alerts as AlertMessageStore;

            if (alerts == null || !alerts.Messages.Any())
            {
                return HtmlString.Empty;
            }

            var builder = new StringBuilder();

            foreach (var messageDetail in alerts.Messages)
            {
                builder.AppendFormat(message, messageDetail.Key, messageDetail.Value);
            }

            return new HtmlString(builder.ToString());
        }

        internal static IEnumerable<string> GetErrorsForProperty<TModel>(this IHtmlHelper<TModel> htmlHelper,
                                                                         string propertyName)
        {
            var modelState = htmlHelper.ViewContext.ModelState;
            if (modelState.IsValid)
            {
                return Enumerable.Empty<string>();
            }

            var errorsForField = modelState[propertyName]?.Errors.Select(e => e.ErrorMessage) ?? Enumerable.Empty<string>();

            return errorsForField;
        }

        public static IHtmlContent SimplePager<TModel>(this IHtmlHelper<TModel> htmlHelper, IPagedList pagedList, string baseUrl)
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

        public static IHtmlContent DisplayNoneIf<TModel>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, bool>> expression)
        {
            if (expression.Compile()(htmlHelper.ViewData.Model))
            {
                return new HtmlString(@" style=""display:none;"" ");
            }

            return HtmlString.Empty;
        }

        public static string RequestQuery<TModel>(this IHtmlHelper<TModel> htmlHelper)
        {
            var query = htmlHelper.ViewContext.HttpContext.Request.QueryString.Value;
            if (!string.IsNullOrEmpty(query))
            {
                return query;
            }

            return string.Empty;
        }
    }
}