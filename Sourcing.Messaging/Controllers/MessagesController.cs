using Microsoft.AspNetCore.Mvc;
using Sourcing.Messaging.BLL.MessageService;
using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.API.Controllers
{
    [ApiController]
    [Route("api/messages")]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto dto)
        {
            var result = await _messageService.SendMessageAsync(dto);
            return StatusCode(result.StatusCode, result);
        }



        [HttpGet("{userA}/{userB}")]
        public async Task<IActionResult> GetConversation(string userA, string userB)
        {
            var result = await _messageService.GetConversationAsync(userA, userB);
            return StatusCode(result.StatusCode, result);
        }


    }
}
