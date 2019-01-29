﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.Windows.Markup;

namespace A2v10.Xaml
{
	public class Case : Container
	{
		public String Value { get; set; }

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			RenderChildren(context);
		}
	}

	public class Else : Case
	{
	}

	public class CaseCollection : List<Case>
	{
		public CaseCollection()
		{
		}
	}

	[ContentProperty("Cases")]
	public class Switch : UIElementBase
	{
		public Object Expression { get; set; }

		public CaseCollection Cases { get; set; } = new CaseCollection();

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			var expr = GetBinding(nameof(Expression));
			if (expr == null)
				new XamlException("Binding 'Expression' must be a Bind");
			for (var i=0; i<Cases.Count; i++)
			{
				var itm = Cases[i];
				var t = new TagBuilder("template");
				var ifKey = (i == 0) ? "v-if " : "v-else-if";
				if (itm is Else)
					ifKey = "v-else";
				t.MergeAttribute("v-if", $"({expr.GetPathFormat(context)}) === '{itm.Value}'");
				t.RenderStart(context);
				itm.RenderElement(context);
				t.RenderEnd(context);
			}
		}
	}
}
