using System.Text.RegularExpressions;

using JabbR.Services;

using Nancy;
using Nancy.ErrorHandling;
using Nancy.Responses.Negotiation;

namespace JabbR.Nancy
{
    public class ErrorPageHandler : IStatusCodeHandler
    {
        private readonly IJabbrRepository _repository;
        private readonly IResponseNegotiator _responseNegotiator;

        public ErrorPageHandler(IJabbrRepository repository, IResponseNegotiator responseNegotiator)
        {
            _repository = repository;
            _responseNegotiator = responseNegotiator;
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

            var negotiationContext = new NegotiationContext
            {
                Context = context,
                ViewName = "errorPage"
            };

            var model = new
            {
                Error = statusCode,
                ErrorCode = (int)statusCode,
                SuggestRoomName = suggestRoomName
            };

            var response = _responseNegotiator.NegotiateResponse(model, negotiationContext);

            response.StatusCode = statusCode;
            context.Response = response;
        }
    }
}