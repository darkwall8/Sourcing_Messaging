using Microsoft.AspNetCore.Mvc;
using Sourcing.Messaging.BLL.MessageService;

namespace Sourcing.Messaging.API.Controllers
{
    [ApiController]
    [Route("api/inbox")]
    public class InboxController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public InboxController(IMessageService messageService) // ✅ constructeur
        {
            _messageService = messageService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserInbox(string userId)
        {
            var result = await _messageService.GetUserConversationsAsync(userId);
            return StatusCode(result.StatusCode, result);
        }
    }
}
