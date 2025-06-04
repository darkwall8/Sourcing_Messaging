using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Sourcing.Messaging.DAL.DTOs
{
    

    public class MessageDto
    {
        [BsonId] // indique que c'est la clé primaire MongoDB
        [BsonRepresentation(BsonType.ObjectId)] // convertit la chaîne en ObjectId automatiquement
        public string? Id { get; set; }  // nommé Id (pas _id) pour respecter conventions C#

        [BsonElement("senderId")]
        public string SenderId { get; set; }

        [BsonElement("receiverId")]
        public string ReceiverId { get; set; }

        [BsonElement("content")]
        public string Content { get; set; }

        [BsonElement("sentAt")]
        public DateTime SentAt { get; set; }

        [BsonElement("isRead")]
        public bool IsRead { get; set; }
    }

}
