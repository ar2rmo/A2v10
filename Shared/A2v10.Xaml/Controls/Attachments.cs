﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;

namespace A2v10.Xaml
{
	public class Attachments : UIElementBase
	{
		public String Url { get; set; }
		public String Accept { get; set; }

		public String Delegate { get; set; }
		public String ErrorDelegate { get; set; }

		public Object Argument { get; set; }

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			if (SkipRender(context))
				return;
			var tag = new TagBuilder("a2-attachments", null, IsInGrid);
			onRender?.Invoke(tag);
			MergeAttributes(tag, context);
			tag.MergeAttribute(":source", "$data");

			MergeBindingAttributeString(tag, context, "url", nameof(Url), Url);

			if (String.IsNullOrEmpty(Delegate))
				throw new XamlException("Delegate is required for Attachments element");
			tag.MergeAttribute(":delegate", $"$delegate('{Delegate}')");
			if (ErrorDelegate != null)
				tag.MergeAttribute(":error-delegate", $"$delegate('{ErrorDelegate}')");

			MergeBindingAttributeString(tag, context, "accept", nameof(Accept), Accept);
			var argBind = GetBinding(nameof(Argument));
			if (argBind != null)
				tag.MergeAttribute(":argument", argBind.GetPath(context));
			tag.Render(context);
		}
	}
}
