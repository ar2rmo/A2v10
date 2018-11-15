﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace A2v10.Xaml
{

	public class Splitter : Container
	{
		public Orientation Orientation { get; set; }
		public Length Height { get; set; }
		public Length MinWidth { get; set; }

		#region Attached Properties
		[ThreadStatic]
		static IDictionary<Object, GridLength> _attachedWidths;
		[ThreadStatic]
		static IDictionary<Object, Length> _attachedMinWidths;

		public static void SetWidth(Object obj, GridLength width)
		{
			if (_attachedWidths == null)
				_attachedWidths = new Dictionary<Object, GridLength>();
			AttachedHelpers.SetAttached(_attachedWidths, obj, width);
		}

		public static GridLength GetWidth(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedWidths, obj);
		}

		public static void SetMinWidth(Object obj, Length width)
		{
			if (_attachedMinWidths == null)
				_attachedMinWidths = new Dictionary<Object, Length>();
			AttachedHelpers.SetAttached(_attachedMinWidths, obj, width);
		}

		public static Length GetMinWidth(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedMinWidths, obj);
		}

		internal static void ClearAttached()
		{
			_attachedWidths = null;
			_attachedMinWidths = null;
		}

		#endregion

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			/* TODO: 
             * 1. Horizontal splitter
            */
			if (SkipRender(context))
				return;
			var spl = new TagBuilder("div", "splitter");
			onRender?.Invoke(spl);
			MergeAttributes(spl, context);
			if (Height != null)
				spl.MergeStyle("height", Height.Value);
			if (MinWidth != null)
				spl.MergeStyle("min-width", MinWidth.Value);
			spl.AddCssClass(Orientation.ToString().ToLowerInvariant());
			// width
			GridLength p1w = GetWidth(Children[0]) ?? GridLength.Fr1();
			GridLength p2w = GetWidth(Children[1]) ?? GridLength.Fr1();

			String rowsCols = Orientation == Orientation.Vertical ? "grid-template-columns" : "grid-template-rows";
			spl.MergeStyle(rowsCols, $"{p1w} 5px {p2w}");

			spl.RenderStart(context);

			// first part
			var p1 = new TagBuilder("div", "spl-part spl-first");
			p1.RenderStart(context);
			Children[0].RenderElement(context);
			p1.RenderEnd(context);

			new TagBuilder("div", "spl-handle")
				.MergeAttribute(Orientation == Orientation.Vertical ? "v-resize" : "h-resize", String.Empty)
				.MergeAttribute("first-pane-width", p1w?.Value.ToString())
				.MergeAttribute("data-min-width", GetMinWidth(Children[0])?.Value.ToString())
				.MergeAttribute("second-min-width", GetMinWidth(Children[1])?.Value.ToString())
				.Render(context);

			// second part
			var p2 = new TagBuilder("div", "spl-part spl-second");
			p2.RenderStart(context);
			Children[1].RenderElement(context);
			p2.RenderEnd(context);

			// drag-handle
			new TagBuilder("div", "drag-handle")
				.Render(context);

			spl.RenderEnd(context);
		}

		protected override void OnEndInit()
		{
			base.OnEndInit();
			if (Children.Count != 2)
				throw new XamlException("The splitter must have two panels");
			if (Orientation == Orientation.Horizontal)
				throw new XamlException("The horizontal splitter is not yet supported");
		}
	}
}
