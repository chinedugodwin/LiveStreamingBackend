﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Glocomx
{
    public class ChatMessage
    {
        public string SenderName { get; set; }

        public string Text { get; set; }
        public string UserId { get; set; }

        public DateTimeOffset SentAt { get; set; }
    }
}
