﻿// Copyright © 2012-2017 Alex Kukhtin. All rights reserved.

using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using A2v10.Infrastructure;
using A2v10.Tests.Config;
using System.Threading.Tasks;
using System.Dynamic;
using A2v10.Data.Interfaces;
using A2v10.Data.Tests;

namespace A2v10.Tests
{

	[TestClass]
	[TestCategory("Workflow")]
	public class WorkflowTest
	{
		IWorkflowEngine _workflow;
		IDbContext _dbContext;

		public WorkflowTest()
		{
			TestConfig.Start();
			_dbContext = ServiceLocator.Current.GetService<IDbContext>();
			_workflow = ServiceLocator.Current.GetService<IWorkflowEngine>();
		}

		[TestMethod]
		public async Task StartWorkflowCold()
		{
			await StartWorkflowInt();
		}

		[TestMethod]
		public async Task StartWorkflowHot()
		{
			await StartWorkflowInt();
		}

		async Task StartWorkflowInt()
		{
			var info = new StartWorkflowInfo()
			{
				Source = "file:Workflows/SimpleWorkflow_v1",
				UserId = 50, // predefined user id
			};
			WorkflowResult result = await _workflow.StartWorkflow(info);
			Assert.AreNotEqual(0, result.ProcessId);
		}

		[TestMethod]
		public async Task SimpleRequest()
		{
			Int64 modelId = 123; // predefined
			Int64 userId = 50; // predefined
			String bookmark = "Bookmark1";
			var info = new StartWorkflowInfo()
			{
				Source = "file:Workflows/SimpleRequest_v1",
				UserId = userId,
				Schema = "a2test",
				Model = "SimpleModel",
				ActionBase = "simple/model",
				ModelId = modelId
			};
			WorkflowResult result = await _workflow.StartWorkflow(info);
			Assert.AreNotEqual(0, result.ProcessId);

			var pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Process.Load]",
				new { UserId = userId, Id = result.ProcessId }
			);

			var dt = new DataTester(pm, "Process");
			dt.AreValueEqual(result.ProcessId, "Id");
			dt.AreValueEqual("a2test", "Schema");
			dt.AreValueEqual("SimpleModel", "Model");
			dt.AreValueEqual(modelId, "ModelId");
			dt.AreValueEqual(userId, "Owner");
			dt.AreValueEqual("simple/model", "ActionBase");

			dt = new DataTester(pm, "Process.Inboxes");
			dt.IsArray(1);
			dt.AreArrayValueEqual(bookmark, 0, "Bookmark");
			dt.AreArrayValueEqual("User", 0, "For");
			dt.AreArrayValueEqual(userId, 0, "ForId");
			dt.AreArrayValueEqual("inbox/action", 0, "Action");
			Int64 inboxId = dt.GetArrayValue<Int64>(0, "Id");

			Assert.AreEqual(inboxId, result.InboxIds[0]);

			dt = new DataTester(pm, "Process.Workflow");
			dt.AreValueEqual("Idle", "ExecutionStatus");

			var rInfo = new ResumeWorkflowInfo()
			{
				Id = inboxId,
				Answer = "OK",
				UserId = userId
			};
			var resumeResult = await _workflow.ResumeWorkflow(rInfo);
			Assert.AreEqual(resumeResult.ProcessId, result.ProcessId);
			Assert.AreEqual(resumeResult.InboxIds.Count, 0);

			pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Process.Load]",
				new { UserId = userId, Id = result.ProcessId }
			);
			dt = new DataTester(pm, "Process.Workflow");
			dt.AreValueEqual("Closed", "ExecutionStatus");

			pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Inbox.Debug.Load]",
				new { UserId = userId, Id = inboxId }
			);
			dt = new DataTester(pm, "Inbox");
			dt.AreValueEqual(inboxId, "Id");
			dt.AreValueEqual(bookmark, "Bookmark");
			dt.AreValueEqual(userId, "UserRemoved");
			dt.AreValueEqual("OK", "Answer");
			dt.AreValueEqual(true, "Void");
		}

		[TestMethod]
		public async Task RequestResultParams()
		{
			Int64 userId = 50; // predefined
			String bookmark1 = "Bookmark1";
			var info = new StartWorkflowInfo()
			{
				Source = "file:Workflows/RequestResultParams_v1",
				UserId = userId
			};
			WorkflowResult result = await _workflow.StartWorkflow(info);
			Assert.AreNotEqual(0, result.ProcessId);

			var pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Process.Load]",
				new { UserId = userId, Id = result.ProcessId }
			);
			var dt = new DataTester(pm, "Process");
			dt.AreValueEqual(result.ProcessId, "Id");
			dt.AreValueEqual(userId, "Owner");

			dt = new DataTester(pm, "Process.Inboxes");
			dt.IsArray(1);
			dt.AreArrayValueEqual(bookmark1, 0, "Bookmark");
			dt.AreArrayValueEqual("User", 0, "For");
			dt.AreArrayValueEqual(userId, 0, "ForId");
			Int64 inboxId = dt.GetArrayValue<Int64>(0, "Id");

			Assert.AreEqual(inboxId, result.InboxIds[0]);
			dt = new DataTester(pm, "Process.Workflow");
			dt.AreValueEqual("Idle", "ExecutionStatus");

			ExpandoObject resumeParams = new ExpandoObject();
			resumeParams.Set("Id", 500L);
			resumeParams.Set("Text", "ParamText");
			var rInfo = new ResumeWorkflowInfo()
			{
				Id = inboxId,
				Answer = "OK",
				UserId = userId,
				Params = resumeParams
			};
			var resumeResult = await _workflow.ResumeWorkflow(rInfo);
			Assert.AreEqual(resumeResult.ProcessId, result.ProcessId);
			Assert.AreEqual(resumeResult.InboxIds.Count, 1);
			pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Process.Load]",
				new { UserId = userId, Id = result.ProcessId }
			);
			dt = new DataTester(pm, "Process.Inboxes");
			dt.IsArray(1);
			inboxId = dt.GetArrayValue<Int64>(0, "Id");

			rInfo = new ResumeWorkflowInfo()
			{
				Id = inboxId,
				UserId = userId
			};

			resumeResult = await _workflow.ResumeWorkflow(rInfo);
			Assert.AreEqual(resumeResult.ProcessId, result.ProcessId);
			Assert.AreEqual(resumeResult.InboxIds.Count, 0);
			pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Process.Load]",
				new { UserId = userId, Id = result.ProcessId }
			);
			dt = new DataTester(pm, "Process.TrackRecords");
			dt.IsArray(1);
			dt.AreArrayValueEqual("Id:500 Text:ParamText", 0, "Message");
			dt.AreArrayValueEqual(userId, 0, "UserId");
		}

		[TestMethod]
		public async Task LoadProcessModel()
		{
			Int64 modelId = 123; // predefined
			Int64 userId = 50; // predefined
			String bookmark = "Bookmark1";
			var info = new StartWorkflowInfo()
			{
				Source = "file:Workflows/LoadModel_v1",
				UserId = userId,
				Schema = "a2test",
				Model = "SimpleModel",
				ActionBase = "simple/model",
				ModelId = modelId
			};
			WorkflowResult result = await _workflow.StartWorkflow(info);
			Assert.AreNotEqual(0, result.ProcessId);

			var pm = await _dbContext.LoadModelAsync(String.Empty, "a2workflow.[Process.Load]",
				new { UserId = userId, Id = result.ProcessId }
			);

			var dt = new DataTester(pm, "Process.Inboxes");
			dt.IsArray(1);
			dt.AreArrayValueEqual(bookmark, 0, "Bookmark");
			dt.AreArrayValueEqual("User", 0, "For");
			dt.AreArrayValueEqual("ObjectName", 0, "Text");
			dt.AreArrayValueEqual(userId, 0, "ForId");
			Int64 inboxId = dt.GetArrayValue<Int64>(0, "Id");

			var rInfo = new ResumeWorkflowInfo()
			{
				Id = inboxId,
				UserId = userId
			};

			var resumeResult = await _workflow.ResumeWorkflow(rInfo);
			Assert.AreEqual(resumeResult.ProcessId, result.ProcessId);
		}
	}
}