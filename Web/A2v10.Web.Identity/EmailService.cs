﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNet.Identity;

using Newtonsoft.Json;

using A2v10.Infrastructure;

namespace A2v10.Web.Identity
{
	public class EmailService : IIdentityMessageService, IMessageService
	{
		private readonly ILogger _logger;
		public EmailService(ILogger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		String GetJsonResult(String phase, String destination, String result = null, String message = null)
		{
			var r = new
			{
				sendmail = new
				{
					phase,
					destination,
					result,
					message
				}
			};

			return JsonConvert.SerializeObject(r, new JsonSerializerSettings()
			{
				NullValueHandling = NullValueHandling.Ignore
			});
		}

		public Task SendAsync(IdentityMessage message)
		{
			Send(message.Destination, message.Subject, message.Body);
			return Task.FromResult(0);
		}

		void HackSubjectEncoding(MailMessage mm, String subject)
		{
			var msgPI = mm.GetType().GetField("message", BindingFlags.Instance | BindingFlags.NonPublic);
			if (msgPI == null) return;
			Object msg = msgPI.GetValue(mm);
			if (msg == null) return;
			var subjPI = msg.GetType().GetField("subject", BindingFlags.Instance | BindingFlags.NonPublic);
			if (subjPI == null) return;
			// without line breaks!
			String encodedSubject = Convert.ToBase64String(Encoding.UTF8.GetBytes(subject), Base64FormattingOptions.None);
			String subjString = $"=?UTF-8?B?{encodedSubject}?=";
			subjPI.SetValue(msg, subjString);
		}

		public void Send(String to, String subject, String body)
		{
			try
			{
				using (var client = new SmtpClient())
				{
					client.DeliveryFormat = SmtpDeliveryFormat.International;
					using (var mm = new MailMessage())
					{
						mm.To.Add(new MailAddress(to));
						mm.BodyTransferEncoding = TransferEncoding.Base64;
						mm.SubjectEncoding = Encoding.Unicode;
						mm.HeadersEncoding = Encoding.UTF8;
						mm.BodyEncoding = Encoding.UTF8;

						mm.Subject =  subject;
						HackSubjectEncoding(mm, subject);

						mm.Body = body;

						mm.IsBodyHtml = true;

						//????
						//var av = AlternateView.CreateAlternateViewFromString(body, Encoding.UTF8, "text/html");
						//mm.AlternateViews.Add(av);

						// sync variant. avoid exception loss
						_logger.LogMessaging(GetJsonResult("send", to));
						client.Send(mm);
						_logger.LogMessaging(GetJsonResult("result", to, "success"));
					}
				}
			}
			catch (Exception ex)
			{
				LogException(to, ex);
				throw; // rethrow
			}
		}

		void LogException(String to, Exception ex)
		{
			String msg = ex.Message;
			if (ex.InnerException != null)
				msg = ex.InnerException.Message;
			_logger.LogMessaging(new LogEntry(LogSeverity.Error, GetJsonResult("result", to, "exception", msg)));
		}
	}
}
