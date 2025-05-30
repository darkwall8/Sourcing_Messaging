using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sourcing.Messaging.BLL.Models;
using Sourcing.Messaging.DAL.DTOs;

namespace Sourcing.Messaging.BLL.MessageService
{
    public interface IMessageService
    {
        Task<ServiceResult<IEnumerable<MessageDto>>> GetConversationAsync(string userA, string userB);

        Task<ServiceResult<MessageDto>> SendMessageAsync(MessageDto dto);
        Task<ServiceResult<IEnumerable<ConversationDto>>> GetUserConversationsAsync(string userId);

    }
}
