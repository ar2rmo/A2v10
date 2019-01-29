﻿
using System;
using System.Configuration;
using System.Globalization;
using System.Threading;

using A2v10.BackgroundTasks;
using A2v10.Data;
using A2v10.Data.Interfaces;
using A2v10.Infrastructure;
using A2v10.Messaging;
using A2v10.Workflow;

namespace BackgroundProcessor
{
	public class Program
	{
		static void Main(String[] args)
		{
			BackgroundTasksManager _manager = null;
			try
			{
				var defaultCulture = ConfigurationManager.AppSettings["defaultCulture"];
				if (defaultCulture != null)
				{
					var cultureInfo = new CultureInfo(defaultCulture);
					CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
					CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
				}

				StartServices();
				var loc = ServiceLocator.Current;
				ILogger logger = loc.GetService<ILogger>();
				IDbContext dbContext = loc.GetService<IDbContext>();
				IApplicationHost host = loc.GetService<IApplicationHost>();
				IMessaging messaging = loc.GetService<IMessaging>();
				Console.WriteLine("Service started");
				_manager = new BackgroundTasksManager(host, dbContext, logger, messaging);
				logger.LogBackground($"CurrentCulutre: {Thread.CurrentThread.CurrentCulture}");
				_manager.Start();
				_manager.StartTasksFromConfig();
				Console.WriteLine("Press any key to stop service...");
				Console.Read();
				_manager.Stop();
			}
			catch (Exception ex)
			{
				if (ex.InnerException != null)
					ex = ex.InnerException;
				Console.WriteLine(ex.Message);
				_manager?.Stop();
			}
		}

		private static IServiceLocator _currentService;

		static void StartServices()
		{
			ServiceLocator.Start = (IServiceLocator loc) =>
			{
				var profiler = new NullProfiler();
				var localizer = new NullLocalizer();
				var host = new BackgroundApplicationHost(profiler);
				var dbContext = new SqlDbContext(profiler, host, localizer);
				var logger = new BackgroundLogger(dbContext);
				var workflow = new WorkflowEngine(host, dbContext, null);
				var emailService = new EmailService(logger);
				var messaging = new MessageProcessor(host, dbContext, emailService, logger);

				loc.RegisterService<IProfiler>(profiler);
				loc.RegisterService<ILocalizer>(localizer);
				loc.RegisterService<IDbContext>(dbContext);
				loc.RegisterService<ILogger>(logger);
				loc.RegisterService<IApplicationHost>(host);
				loc.RegisterService<IWorkflowEngine>(workflow);
				loc.RegisterService<IMessaging>(messaging);
			};

			ServiceLocator.GetCurrentLocator = () =>
			{
				if (_currentService == null)
					_currentService = new ServiceLocator();
				return _currentService;
			};
		}
	}
}
