﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

using System;
using System.Dynamic;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using System.IO;
using System.Xaml;
using System.Linq;

namespace A2v10.Messaging
{
	public class MessageProcessor : IMessaging
	{
		readonly private IApplicationHost _host;
		readonly private IDbContext _dbContext;
		readonly private IMessageService _emailService;
		readonly private ILogger _logger;

		public MessageProcessor(IApplicationHost host, IDbContext dbContext, IMessageService emailService, ILogger logger)
		{
			_host = host;
			_dbContext = dbContext;
			_emailService = emailService;
			_logger = logger;
		}

		public IQueuedMessage CreateQueuedMessage()
		{
			return new CommonMessage();
		}

		public ExpandoObject PrepareMessage(IQueuedMessage message)
		{
			String json = JsonConvert.SerializeObject(message);
			ExpandoObject msg = JsonConvert.DeserializeObject<ExpandoObject>(json, new ExpandoObjectConverter());
			ConvertToNameValueArray(msg, "Parameters");
			ConvertToNameValueArray(msg, "Environment");
			var dm = new ExpandoObject();
			dm.Set("Message", msg);
			return dm;

		}

		// not Task - simple void
		public async void FireAndForget(Task task)
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				_logger.LogMessagingException(ex);
			}
		}

		public Int64 QueueMessage(IQueuedMessage message)
		{
			ExpandoObject msg = PrepareMessage(message);
			IDataModel md = _dbContext.SaveModel(String.Empty, "a2messaging.[Message.Queue.Update]", msg);
			Int64 msgId = md.Eval<Int64>("Result.Id");
			if (message.Immediately)
			{
				FireAndForget(SendMessageAsync(msgId));
			}
			return msgId;
		}

		public async Task<Int64> QueueMessageAsync(IQueuedMessage message, Boolean immediately)
		{
			ExpandoObject msg = PrepareMessage(message);
			IDataModel md = await _dbContext.SaveModelAsync(String.Empty, "a2messaging.[Message.Queue.Update]", msg);
			Int64 msgId = md.Eval<Int64>("Result.Id");
			if (message.Immediately) { 
				/*
				Task.Run(() => {
					SendMessageAsync(msgId);
				});
				*/
			}
			return msgId;
		}

		void ConvertToNameValueArray(ExpandoObject msg, String name)
		{
			if (!(msg.Get<ExpandoObject>(name) is IDictionary<String, Object> val))
				return;
			var arr = new List<Object>();
			foreach (var v in val)
			{
				var ne = new ExpandoObject();
				ne.Set("Name", v.Key);
				ne.Set("Value", v.Value);
				arr.Add(ne);
			}
			msg.Set(name, arr);
		}

		async Task SendMessageAsync(Int64 msgId)
		{
			var msgModel = await _dbContext.LoadModelAsync(String.Empty, "a2messaging.[Message.Queue.Load]", new { Id = msgId });
			IMessageForSend msg = await ResolveMessageAsync(msgModel);
			await msg.SendAsync(_emailService);
		}

		Task<IMessageForSend> ResolveMessageAsync(IDataModel dm)
		{
			var templateName = dm.Eval<String>("Message.Template");
			var key = dm.Eval<String>("Message.Key");
			var templatePath = _host.MakeFullPath(false, templateName, String.Empty);
			var fullPath = Path.ChangeExtension(templatePath, "xaml");

			var env = dm.Eval<List<ExpandoObject>>("Message.Environment");
			var hostObj = new ExpandoObject();
			hostObj.Set("Name", "Host");
			hostObj.Set("Value", _host.AppHost);
			env.Add(hostObj);

			var tml = XamlServices.Load(fullPath) as Template;

			TemplatedMessage tm = tml.Get(key);
			if (tm == null)
				throw new MessagingException($"Message not found. Key = '{key}'");

			var resolver = new MessageResolver(_host, _dbContext, dm);
			return tm.ResolveAndSendAsync(resolver);
		}
	}
}
