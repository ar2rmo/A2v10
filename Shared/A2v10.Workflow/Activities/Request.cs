﻿
using System;
using System.Activities;
using A2v10.Infrastructure;
using System.Activities.Tracking;
using System.Diagnostics;

namespace A2v10.Workflow
{
    public class Request : NativeActivity<RequestResult>
    {
        [RequiredArgument]
        public InArgument<Inbox> Inbox { get; set; }
        public InArgument<TrackRecord> TrackBefore { get; set; }
        public InArgument<TrackRecord> TrackAfter { get; set; }

        public InArgument<MessageInfo> SendBefore { get; set; }
        public InArgument<MessageInfo> SendAfter { get; set; }

        protected override bool CanInduceIdle { get { return true; } }

        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
        }

        protected override void Execute(NativeActivityContext context)
        {
            var process = Process.GetProcessFromContext(context.DataContext);
            Inbox inbox = Inbox.Get<Inbox>(context);
            IDbContext dbContext = ServiceLocator.Current.GetService<IDbContext>();
            inbox.Create(dbContext, process.Id);
            // track before
            DoTrack(TrackBefore.Get<TrackRecord>(context), context);
            // send before
            SendMessage(SendBefore.Get<MessageInfo>(context), inbox, context);
            TrackInfo(context, $"Inbox created (Id: {inbox.Id}).");
            context.CreateBookmark(inbox.Bookmark, new BookmarkCallback(this.ContinueAt));
        }

        void ContinueAt(NativeActivityContext context, Bookmark bookmark, Object obj)
        {
            if (!(obj is RequestResult))
                throw new WorkflowException("Invalid ResponseType. Must be RequestResult");
            IDbContext dbContext = ServiceLocator.Current.GetService<IDbContext>();
            var rr = obj as RequestResult;
            Inbox inbox = Inbox.Get<Inbox>(context);
            inbox.Resumed(dbContext, rr.InboxId, rr.UserId, rr.Answer);
            TrackInfo(context, $"Inbox resumed (Id: {rr.InboxId}, UserId: {rr.UserId}) Result:'{rr.Answer}')");
            // track after
            DoTrack(TrackAfter.Get<TrackRecord>(context), context, rr.UserId);
            // send after
            SendMessage(SendAfter.Get<MessageInfo>(context), inbox, context);
            this.Result.Set(context, rr);
        }

        void DoTrack(TrackRecord record, NativeActivityContext context, Int64? userId = null)
        {
            if (record == null)
                return;
            var process = Process.GetProcessFromContext(context.DataContext);
			record.ProcessId = process.Id;
            TrackInfo(context, "Track recorded successfully");
        }


        void TrackInfo(NativeActivityContext context, String msg)
        {
            var ctr = new CustomTrackingRecord(msg, TraceLevel.Info);
            context.Track(ctr);
        }

        void SendMessage(MessageInfo messageInfo, Inbox inbox, NativeActivityContext context)
        {
            if (messageInfo == null)
                return;
            var process = Process.GetProcessFromContext(context.DataContext);
            IMessaging messaging = ServiceLocator.Current.GetService<IMessaging>();
            IMessage msg = messaging.CreateMessage();

            msg.Template = messageInfo.Template;
            msg.Key = messageInfo.Key;

            msg.Schema = process.Schema;
            msg.Model = process.Model;
            msg.ModelId = process.ModelId;
            msg.Source = $"Inbox:{inbox.Id}";

            msg.Params.Append(messageInfo.Params, replaceExisiting:true);
            msg.Environment.Add("InboxId", inbox.Id);
            msg.Environment.Add("ProcessId", process.Id);

            messaging.QueueMessage(msg);
            TrackInfo(context, $"Message queued successfully (Id: {msg.Id})");
        }
    }
}
