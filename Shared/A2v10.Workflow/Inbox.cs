﻿using A2v10.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A2v10.Workflow
{
    public class Inbox
    {
        public Int64 Id { get; set; }
        public String Bookmark { get; set; }
        public String For { get; set; }
        public Int64 ForId { get; set; }
        public Int64 ForId2 { get; set; }
        public String Text { get; set; }
        public String View { get; set; }
        internal Int64 ProcessId { get; set; }
        internal void Create(IDbContext dbContext, Int64 processId)
        {
            this.ProcessId = processId;
            dbContext.Execute(String.Empty, "a2workflow.[CreateInbox]", this);
            Id = this.Id;
        }

        internal void Resumed(IDbContext dbContext, Int64 Id, Int64 UserId, String Answer)
        {
            var arg = new { Id = Id, UserId = UserId, Answer = Answer };
            dbContext.Execute(String.Empty, "a2workflow.[ResumeInbox]", arg);
        }
    }
}
