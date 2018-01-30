﻿
using System;
using System.Globalization;
using System.IO;
using System.Dynamic;

using ChakraHost.Hosting;

using A2v10.Infrastructure;
using A2v10.Runtime.Properties;
using A2v10.Script;
using A2v10.Request;
using A2v10.Runtime;
using A2v10.Data;
using A2v10.Xaml;
using A2v10.Workflow;
using System.Web;

namespace A2v10RuntimeNet
{
    public static class Desktop
    {
		static IScriptContext _scriptContext;

		public static bool HasError { get; set; }
		public static String LastErrorMessage { get; set; }

		public static void Start()
		{
			try
			{
				Resources.Culture = CultureInfo.InvariantCulture;
				ScriptContext.Start();
			}
			catch (Exception ex)
			{
				SetLastError(ex);
			}
		}

		public static void Stop()
		{
			if (_scriptContext == null)
				return;
			(_scriptContext as IDisposable).Dispose();
		}

		public static void LoadRuntimeLibrary()
		{
			String[] app =
			{
				Resources.Application,
				Resources.Form_form,
                Resources.Solution
			};
			ParseLibraryElements(app);
		}

        public static void LoadModuleContext()
        {
            String[] app =
            {
                Resources.App_context,
            };
            ParseLibraryElements(app);
        }

        static IScriptContext ScriptContext
		{
			get
			{
				if (_scriptContext == null)
					_scriptContext = new ScriptContext();
				return _scriptContext;
			}
		}

		static void SetLastError(Exception ex)
		{
			HasError = true;
			if (ex is JavaScriptScriptException)
			{
				var jsex = (ex as JavaScriptScriptException);
				LastErrorMessage = jsex.Error.GetProperty(JavaScriptPropertyId.FromString("message")).ConvertToString().ToString();
			}
			else
			{
				LastErrorMessage = ex.Message;
			}
		}

		static void ParseLibraryElements(String[] elems)
		{
			foreach (var elem in elems)
			{
				var lib = JavaScriptContext.ParseScriptLibrary(elem);
				lib.CallFunction(JavaScriptValue.Undefined);
			}
		}

        public static void OpenSolution(String fileName)
        {
            try
            {
                throw new Exception($"opens file {fileName} (from C#)");
            }
            catch (Exception ex)
            {
                SetLastError(ex);
            }
        }

        public static void StartDesktopServices()
        {
            IServiceLocator locator = ServiceLocator.Current;
            IProfiler profiler = new DesktopProfiler();
            IApplicationHost host = new DesktopApplicationHost(profiler);
            ILocalizer localizer = new DesktopLocalizer();
            IDbContext dbContext = new SqlDbContext(host, localizer);
            IRenderer renderer = new XamlRenderer(profiler);
            IWorkflowEngine wfEngine = new WorkflowEngine(host, dbContext);
            locator.RegisterService<IProfiler>(profiler);
            locator.RegisterService<IApplicationHost>(host);
            locator.RegisterService<IDbContext>(dbContext);
            locator.RegisterService<IRenderer>(renderer);
            locator.RegisterService<IWorkflowEngine>(wfEngine);

        }

        static void Render(BaseController ctrl, RequestUrlKind kind, String path, String search, TextWriter writer)
        {
            ExpandoObject loadPrms = new ExpandoObject();
            loadPrms.Append(HttpUtility.ParseQueryString(search), toPascalCase: true);
            // TODO: current user ID;
            loadPrms.Set("UserId", 100);
            ctrl.RenderElementKind(kind, path, loadPrms, writer).Wait();
        }


        public static String ProcessRequest(String url, String search, String postData)
        {
            var controller = new BaseController();
            if (url.StartsWith("admin/"))
            {
                url = url.Substring(6);
                controller.Admin = true;
            }
            Int64 userId = 100; // TODO: userId
            Int32 tenantId = 0; // TODO: tenantId
            try
            {
                using (var writer = new StringWriter()) {
                    if (url.StartsWith("_page/"))
                        Render(controller, RequestUrlKind.Page, url.Substring(6), search, writer);
                    else if (url.StartsWith("_dialog/"))
                        Render(controller, RequestUrlKind.Dialog, url.Substring(8), search, writer);
                    else if (url.StartsWith("_popup/"))
                        Render(controller, RequestUrlKind.Popup, url.Substring(7), search, writer);
                    else if (url.StartsWith("_data/"))
                    {
                        var command = url.Substring(6);
                        controller.Data(command, tenantId, userId, postData, writer).Wait();
                    }
                    else if (url.StartsWith("_image/"))
                    {
                        controller.Image("/" + url, userId).Wait(); // with _image prefix
                    }
                    else
                    {
                        // TODO: exception
                        writer.Write($"<div>page '{url}' not found.</div>");
                    }
                    return writer.ToString();
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                    ex = ex.InnerException;
                // TODO:: /exception
                return $"<div>{ex.Message}</div>";
            }
        }
	}
}
