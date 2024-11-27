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
        private readonly INancyModule _module;

        public ErrorPageHandler(INancyModule module, IJabbrRepository repository)
        {
            _module = module;
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

            var viewModel = new
            {
                Error = statusCode,
                ErrorCode = (int)statusCode,
                SuggestRoomName = suggestRoomName
            };

            var response = _module.Negotiate
                .WithModel(viewModel)
                .WithView("errorPage");

            context.Response = response;
            context.Response.StatusCode = statusCode;
        }
    }
}