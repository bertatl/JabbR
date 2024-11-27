using System.Text.RegularExpressions;

using JabbR.Services;

using Nancy;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;

namespace JabbR.Nancy
{
    public class ErrorPageHandler : IStatusCodeHandler
    {
        private readonly IJabbrRepository _repository;
        private readonly IViewFactory _viewFactory;

        public ErrorPageHandler(IViewFactory viewFactory, IJabbrRepository repository)
        {
            _viewFactory = viewFactory;
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

            var view = _viewFactory.RenderView(
                "errorPage",
                new
                {
                    Error = statusCode,
                    ErrorCode = (int)statusCode,
                    SuggestRoomName = suggestRoomName
                },
                context);

            context.Response = view;
            context.Response.StatusCode = statusCode;
        }
    }
}