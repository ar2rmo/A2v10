﻿// Copyright © 2012-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Linq;
using System.Activities;
using System.Activities.Expressions;
using System.Activities.XamlIntegration;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace A2v10.Workflow
{
	internal static class RuntimeActivity
	{
		static IDictionary<String, Type> _cache = new ConcurrentDictionary<String, Type>();

		static void CacheType(String key, Type type)
		{
			if (_cache.ContainsKey(key))
				return;
			_cache.Add(key, type);
		}

		static Type GetCachedType(String key)
		{
			if (_cache.TryGetValue(key, out Type type))
				return type;
			return null;
		}

		static Type CompileExpressions(DynamicActivity dynamicActivity)
		{
			// activityName is the Namespace.Type of the activity that contains the  
			// C# expressions. For Dynamic Activities this can be retrieved using the  
			// name property , which must be in the form Namespace.Type.  
			String activityName = dynamicActivity.Name;
			// Split activityName into Namespace and Type.Append _CompiledExpressionRoot to the type name  
			// to represent the new type that represents the compiled expressions.  
			// Take everything after the last . for the type name.  
			String activityType = activityName.Split('.').Last() + "_CompiledExpressionRoot";
			// Take everything before the last . for the namespace.  
			String activityNamespace = String.Join(".", activityName.Split('.').Reverse().Skip(1).Reverse());

			// Create a TextExpressionCompilerSettings.  
			TextExpressionCompilerSettings settings = new TextExpressionCompilerSettings
			{
				Activity = dynamicActivity,
				Language = "C#",
				ActivityName = activityType,
				ActivityNamespace = activityNamespace,
				RootNamespace = null,
				GenerateAsPartialClass = false,
				AlwaysGenerateSource = true,
				ForImplementation = true
			};

			// Compile the C# expression.  
			TextExpressionCompilerResults results =
				new TextExpressionCompiler(settings).Compile();

			// Any compilation errors are contained in the CompilerMessages.  
			if (results.HasErrors)
			{
				throw new Exception("Compilation failed.");
			}

			// Create an instance of the new compiled expression type.  
			ICompiledExpressionRoot compiledExpressionRoot =
				Activator.CreateInstance(results.ResultType,
					new Object[] { dynamicActivity }) as ICompiledExpressionRoot;

			// Attach it to the activity.  
			CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(
				dynamicActivity, compiledExpressionRoot);
			return results.ResultType;
		}

		static void CreateCompiledActivity(DynamicActivity dynamicActivity, Type resultType)
		{
			ICompiledExpressionRoot compiledExpressionRoot =
				Activator.CreateInstance(resultType,
					new Object[] { dynamicActivity }) as ICompiledExpressionRoot;

			// Attach it to the activity.  
			CompiledExpressionInvoker.SetCompiledExpressionRootForImplementation(
				dynamicActivity, compiledExpressionRoot);
		}

		public static Boolean IsTypeCached(String name)
		{
			Type cachedType = RuntimeActivity.GetCachedType(name);
			return cachedType != null;
		}

		public static Boolean Compile(String name, Activity root)
		{
			Type cachedType = RuntimeActivity.GetCachedType(name);
			Boolean bCached = false;
			if (cachedType != null)
			{
				RuntimeActivity.CreateCompiledActivity(root as DynamicActivity, cachedType);
				bCached = true;
			}
			else
			{
				cachedType = RuntimeActivity.CompileExpressions(root as DynamicActivity);
				RuntimeActivity.CacheType(name, cachedType);
			}
			return bCached;
		}
	}
}
