﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Configuration;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace A2v10.Web.Mvc.Start
{
	public class WebApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);

			Debug.Assert(ViewEngines.Engines[0] is WebFormViewEngine);
			ViewEngines.Engines.RemoveAt(0); // WebForm is not used
			var siteMode = ConfigurationManager.AppSettings["siteMode"];
			if (siteMode == "site") {
				var razor = ViewEngines.Engines[0] as RazorViewEngine;
				razor.PartialViewLocationFormats = new String[]
				{
				"~/App_Apps/VueSite/{1}/{0}.cshtml",
				"~/App_Apps/VueSite/Shared/{0}.cshtml",
				};
			}
		}
	}
}
