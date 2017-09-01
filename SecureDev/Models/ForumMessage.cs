using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Vladi2.Models
{
    public class ForumMessage
    {
        public string SubjectMessage { get; set; }
        public string TopicMessage { get; set; }
        public string Message { get; set; }
        public string UserName { get; set;}

        public override string ToString()
        {
            return string.Format(
@"Subject {0} 
Topic {1} 
Message {2} 
UserName {3}", SubjectMessage, TopicMessage, Message, UserName);
        }


    }
}