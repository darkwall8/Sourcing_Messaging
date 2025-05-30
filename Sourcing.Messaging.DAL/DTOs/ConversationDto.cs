using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sourcing.Messaging.DAL.DTOs
{
    public class ConversationDto
    {
        public string OtherUserId { get; set; } = string.Empty;
        public string LastMessageContent { get; set; } = string.Empty;
        public DateTime LastMessageTime { get; set; }
        public bool HasUnreadMessages { get; set; }
    }
}
