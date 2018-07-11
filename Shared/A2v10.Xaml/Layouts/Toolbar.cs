﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using A2v10.Infrastructure;

namespace A2v10.Xaml
{

	public enum ToolbarStyle
	{
		Default,
		Transparent
	}

	public class Toolbar : Container
	{

		public enum ToolbarAlign
		{
			Left,
			Right
		}

		#region Attached Properties
		[ThreadStatic]
		static IDictionary<Object, ToolbarAlign> _attachedPart;

		public static void SetAlign(Object obj, ToolbarAlign aln)
		{
			if (_attachedPart == null)
				_attachedPart = new Dictionary<Object, ToolbarAlign>();
			AttachedHelpers.SetAttached(_attachedPart, obj, aln);
		}

		public static ToolbarAlign GetAlgin(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedPart, obj);
		}

		internal static void ClearAttached()
		{
			_attachedPart = null;
		}

		#endregion

		public ToolbarStyle Style { get; set; }

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			var tb = new TagBuilder("div", "toolbar", IsInGrid);
			onRender?.Invoke(tb);
			if (Style != ToolbarStyle.Default)
				tb.AddCssClass(Style.ToString().ToKebabCase());
			MergeAttributes(tb, context);
			tb.RenderStart(context);
			RenderChildren(context);
			tb.RenderEnd(context);
		}

		internal override void RenderChildren(RenderContext context, Action<TagBuilder> onRenderStatic = null)
		{
			if (_attachedPart == null)
			{
				base.RenderChildren(context);
				return;
			}
			List<UIElementBase> rightList = new List<UIElementBase>();
			// Те, что влево и не установлены
			foreach (var ch in Children)
			{
				if (GetAlgin(ch) == ToolbarAlign.Right)
					rightList.Add(ch);
				else
					ch.RenderElement(context);
			}
			if (rightList.Count == 0)
				return;
			// aligner
			new TagBuilder("div", "aligner").Render(context);

			// Те, что справа
			foreach (var ch in rightList)
				ch.RenderElement(context);
		}
	}
}
