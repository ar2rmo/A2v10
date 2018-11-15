﻿// Copyright © 2012-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Activities;
using System.Activities.Tracking;
using System.Diagnostics;
using System.Dynamic;
using A2v10.Data.Interfaces;
using A2v10.Infrastructure;

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
		public InArgument<ModelStateInfo> StateBefore { get; set; }
		public InArgument<ModelStateInfo> StateAfter { get; set; }

		public OutArgument<Int64> InboxId { get; set; }

		protected override bool CanInduceIdle { get { return true; } }

		protected override void CacheMetadata(NativeActivityMetadata metadata)
		{
			base.CacheMetadata(metadata);
		}

		protected override void Execute(NativeActivityContext context)
		{
			var process = Process.GetProcessFromContext(context.DataContext);
			var wfResult = context.GetExtension<WorkflowResult>();
			Inbox inbox = Inbox.Get<Inbox>(context);
			IDbContext dbContext = context.GetExtension<IDbContext>();
			inbox.Create(dbContext, process.Id);
			InboxId.Set(context, inbox.Id);
			wfResult.InboxIds.Add(inbox.Id);
			// track before
			DoTrack(dbContext, TrackBefore.Get<TrackRecord>(context), context);
			// send before
			SendMessage(SendBefore.Get<MessageInfo>(context), inbox, context);
			// state before
			DoModelState(dbContext, StateBefore.Get<ModelStateInfo>(context), context, inbox.Id);
			TrackRecord(context, $"Inbox created {{Id: {inbox.Id}}}.");
			context.CreateBookmark(inbox.Bookmark, new BookmarkCallback(this.ContinueAt));
		}

		void ContinueAt(NativeActivityContext context, Bookmark bookmark, Object obj)
		{
			if (!(obj is RequestResult))
				throw new WorkflowException("Invalid ResponseType. Must be RequestResult");
			IDbContext dbContext = context.GetExtension<IDbContext>();
			Process process = Process.GetProcessFromContext(context.DataContext);
			process.DbContext = dbContext;
			var rr = obj as RequestResult;
			Inbox inbox = Inbox.Get<Inbox>(context);
			inbox.Resumed(dbContext, rr.InboxId, rr.UserId, rr.Answer);
			TrackRecord(context, $"Inbox resumed {{Id: {rr.InboxId}, UserId: {rr.UserId}) Result:'{rr.Answer}'}}");
			// track after
			DoTrack(dbContext, TrackAfter.Get<TrackRecord>(context), context, rr.UserId);
			// send after
			SendMessage(SendAfter.Get<MessageInfo>(context), inbox, context);
			// state after
			DoModelState(dbContext, StateAfter.Get<ModelStateInfo>(context), context, rr.InboxId, rr.UserId);
			this.Result.Set(context, rr);
		}

		void DoTrack(IDbContext dbContext, TrackRecord record, NativeActivityContext context, Int64? userId = null)
		{
			if (record == null)
				return;
			var process = Process.GetProcessFromContext(context.DataContext);
			record.ProcessId = process.Id;
			if (record.UserId == 0 && userId.HasValue)
				record.UserId = userId.Value;
			record.Update(dbContext);
			TrackRecord(context, $"TrackRecord written successfully {{Id:{record.Id}}}");
		}

		void TrackRecord(NativeActivityContext context, String msg)
		{
			var ctr = new CustomTrackingRecord(msg, TraceLevel.Info);
			context.Track(ctr);
		}

		void SendMessage(MessageInfo messageInfo, Inbox inbox, NativeActivityContext context)
		{
			if (messageInfo == null)
				return;
			var process = Process.GetProcessFromContext(context.DataContext);
			IMessaging messaging = context.GetExtension<IMessaging>();
			IMessage msg = messaging.CreateMessage();

			msg.Template = messageInfo.Template;
			msg.Key = messageInfo.Key;

			msg.DataSource = process.DataSource;
			msg.Schema = process.Schema;
			msg.Model = process.ModelName;
			msg.ModelId = process.ModelId;
			msg.Source = $"Inbox:{inbox.Id}";

			msg.Params.Append(messageInfo.Params, replaceExisiting: true);
			msg.Environment.Add("InboxId", inbox.Id);
			msg.Environment.Add("ProcessId", process.Id);

			messaging.QueueMessage(msg);
			TrackRecord(context, $"Message queued successfully {{Id: {msg.Id}}}");
		}

		void DoModelState(IDbContext dbContext, ModelStateInfo state, NativeActivityContext context, Int64? inboxId = null, Int64? userId = null)
		{
			if (state == null)
				return;
			var prms = new ExpandoObject();
			var process = Process.GetProcessFromContext(context.DataContext);
			prms.Set("Id", process.ModelId);
			prms.Set("State", state.State);
			prms.Set("Process", process.Id);
			if (inboxId != null)
				prms.Set("Inbox", inboxId.Value);
			if (userId != null)
				prms.Set("UserId", userId.Value);
			dbContext.LoadModel(state.DataSource, state.Procedure, prms);
		}
	}
}
