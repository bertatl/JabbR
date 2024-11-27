using System.Text.RegularExpressions;

using JabbR.Services;

using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;
using Nancy.Responses;

namespace JabbR.Nancy
{
    public class ErrorPageHandler : IStatusCodeHandler
    {
        private readonly IJabbrRepository _repository;
        private readonly INancyModule _module;
        private readonly IViewRenderer _viewRenderer;

        public ErrorPageHandler(INancyModule module, IJabbrRepository repository, IViewRenderer viewRenderer)
        {
            _module = module;
            _repository = repository;
            _viewRenderer = viewRenderer;
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            // only handle 40x and 50x
            return (int)statusCode >= 400;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            string suggestRoomName = null;
            if (statusCode == HttpStatusCode.NotFound)
            {
                var match = Regex.Match(context.Request.Url.Path, "^/(rooms/)?(?<roomName>[^/]+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var potentialRoomName = match.Groups["roomName"].Value;
                    if (_repository.GetRoomByName(potentialRoomName) != null)
                    {
                        suggestRoomName = potentialRoomName;
                    }
                }
            }

            var viewModel = new
            {
                Error = statusCode,
                ErrorCode = (int)statusCode,
                SuggestRoomName = suggestRoomName
            };

            var response = new HtmlResponse(
                contents: stream =>
                {
                    return _viewRenderer.RenderView("errorPage", viewModel, context);
                },
                statusCode: statusCode
            );

            context.Response = response;
        }
    }
}