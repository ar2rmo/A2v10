﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Dynamic;
using System.Net.Http.Headers;

using Microsoft.AspNet.Identity;

using A2v10.Request;
using A2v10.Web.Mvc.Filters;
using A2v10.Web.Identity;
using A2v10.Infrastructure;
using System.Globalization;
using Newtonsoft.Json;

namespace A2v10.Web.Mvc.Controllers
{
	[Authorize]
	[ExecutingFilter]
	public class AttachmentController : Controller
	{
		A2v10.Request.AttachmentController _baseController = new A2v10.Request.AttachmentController();

		public AttachmentController()
		{
		}

		public Int64 UserId => User.Identity.GetUserId<Int64>();
		public Int32 TenantId => User.Identity.GetUserTenantId();

		[HttpGet]
		public Task Show(String Base, String id)
		{
			return _baseController.Show(Base, id, Response, SetParams);
		}

		[HttpGet]
		public Task Export(String Base, String id)
		{
			return _baseController.Download(Base, id, false, Response, SetParams);
		}

		[HttpPost]
		public Task Raw(String Base, String id)
		{
			return _baseController.Download(Base, id, true, Response, SetParams);
		}

		[HttpPost]
		public Task Signature(String Base, String id)
		{
			return _baseController.Signature(Base, id, Response, SetParams);
		}

		[HttpPost]
		public Task Sign(String Base, String id)
		{
			return _baseController.Sign(Base, id, Request, Response, SetParams);
		}

		void SetParams(ExpandoObject prms)
		{
			prms.Set("UserId", UserId);
			prms.Set("TenantId", TenantId);
		}
	}
}
