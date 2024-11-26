using System;
using System.Globalization;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using JabbR.Infrastructure;
using JabbR.Models;
using JabbR.Services;
using JabbR.WebApi.Model;

namespace JabbR.WebApi
{
    [ApiController]
    [Route("[controller]")]
    public class MessagesController : ControllerBase
    {
        const string FilenameDateFormat = "yyyy-MM-dd.HHmmsszz";
        private readonly IJabbrRepository _repository;

        public MessagesController(IJabbrRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult GetAllMessages(string room, string range = "last-hour")
        {
            var end = DateTime.Now;
            DateTime start;

            switch (range)
            {
                case "last-hour":
                    start = end.AddHours(-1);
                    break;
                case "last-day":
                    start = end.AddDays(-1);
                    break;
                case "last-week":
                    start = end.AddDays(-7);
                    break;
                case "last-month":
                    start = end.AddDays(-30);
                    break;
                case "all":
                    start = DateTime.MinValue;
                    break;
                default:
                    return BadRequest("Range value not recognized");
            }

            var filenamePrefix = room + ".";

            if (start != DateTime.MinValue)
            {
                filenamePrefix += start.ToString(FilenameDateFormat, CultureInfo.InvariantCulture) + ".";
            }

            filenamePrefix += end.ToString(FilenameDateFormat, CultureInfo.InvariantCulture);

            ChatRoom chatRoom;

            try
            {
                chatRoom = _repository.VerifyRoom(room, mustBeOpen: false);
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message, filenamePrefix });
            }

            if (chatRoom.Private)
            {
// TODO: Allow viewing messages using auth token
                return NotFound(new { error = String.Format(LanguageResources.RoomNotFound, chatRoom.Name), filenamePrefix });
            }

            var messages = _repository.GetMessagesByRoom(chatRoom)
                .Where(msg => msg.When <= end && msg.When >= start)
                .OrderBy(msg => msg.When)
                .Select(msg => new MessageApiModel
                {
                    Content = msg.Content,
                    Username = msg.User.Name,
                    When = msg.When,
                    HtmlEncoded = msg.HtmlEncoded,
                });

            return Ok(new { messages, filenamePrefix });
        }
    }
}