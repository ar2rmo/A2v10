﻿using A2v10.Infrastructure;
using A2v10.Runtime.Properties;
using A2v10.Script;
using ChakraHost.Hosting;
using System;
using System.Globalization;

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
				Resources.Form_form
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
	}
}
