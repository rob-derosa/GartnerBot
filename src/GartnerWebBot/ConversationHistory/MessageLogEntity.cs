using Microsoft.WindowsAzure.Storage.Table;
using System;

namespace GartnerBot.ConversationHistory
{
    public class MessageLogEntity : TableEntity
    {
        public string Body
        {
            get;
            set;
        }
    }
}
