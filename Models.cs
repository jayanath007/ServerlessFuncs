using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace GroupChatFuncs
{

    enum ContentType
    {
        Text = 0,
        Data = 1,
    }
    public class Message
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("n");
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public int Type { get; set; }
        public string Content { get; set; }
    }

    public class MessageCreateModel
    {
        public int Type { get; set; }
        public string Content { get; set; }
    }

    public class MessageUpdateModel
    {
        public string Content { get; set; }
    }

    public class MessageTableEntity : TableEntity
    {
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public int Type { get; set; }
        public string Content { get; set; }
    }



    public static class Mappings
    {
        public static MessageTableEntity ToTableEntity(this Message message)
        {
            return new MessageTableEntity()
            {
                PartitionKey = "MESSAGE",
                RowKey = message.Id,
                CreatedTime = message.CreatedTime,
                Type = message.Type,
                Content = message.Content,
            };
        }

        public static Message ToMessage(this MessageTableEntity message)
        {
            return new Message()
            {
                Id = message.RowKey,
                CreatedTime = message.CreatedTime,
                Type = message.Type,
                Content = message.Content
            };
        }

    }
}
