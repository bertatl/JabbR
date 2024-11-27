using System.Text.RegularExpressions;

using JabbR.Services;

using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;

namespace JabbR.Nancy
{
    public class ErrorPageHandler : IStatusCodeHandler
    {
        private readonly IViewRenderer _viewRenderer;
        private readonly IJabbrRepository _repository;

        public ErrorPageHandler(IViewFactory factory, IJabbrRepository repository)
        {
            _viewRenderer = new DefaultViewRenderer(factory);
            _repository = repository;
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

            var response = _viewRenderer.RenderView(
                "errorPage",
                new
                {
                    Error = statusCode,
                    ErrorCode = (int)statusCode,
                    SuggestRoomName = suggestRoomName
                },
                new ViewLocationContext { Context = context });

            response.StatusCode = statusCode;
            context.Response = response;
        }
    }
}