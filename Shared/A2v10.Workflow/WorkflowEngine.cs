﻿// Copyright © 2012-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Threading.Tasks;

using A2v10.Infrastructure;
using A2v10.Data.Interfaces;

namespace A2v10.Workflow
{
	public class WorkflowEngine : IWorkflowEngine
	{
		readonly IDbContext _dbContext;
		readonly IApplicationHost _host;
		private readonly IMessaging _messaging;

		public WorkflowEngine(IApplicationHost host, IDbContext context, IMessaging messaging)
		{
			_host = host;
			_dbContext = context;
			_messaging = messaging;
		}

		public async Task<WorkflowResult> StartWorkflow(StartWorkflowInfo info)
		{
			return await AppWorkflow.StartWorkflow(_host, _dbContext, _messaging, info);
		}

		public async Task<WorkflowResult> ResumeWorkflow(ResumeWorkflowInfo info)
		{
			return await AppWorkflow.ResumeWorkflow(_host, _dbContext, _messaging, info);
		}

		public void ProcessPendingWorkflows()
		{
		}
	}
}
