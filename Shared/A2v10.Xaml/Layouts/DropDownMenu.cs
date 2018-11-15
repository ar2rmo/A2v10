﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using A2v10.Infrastructure;

namespace A2v10.Xaml
{
	public class DropDownMenu : Container
	{

		public DropDownDirection Direction { get; set; }


		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			if (SkipRender(context))
				return;
			var menu = new TagBuilder("div", "dropdown-menu menu");
			menu.MergeAttribute("role", "menu");
			MergeAttributes(menu, context);
			if (Direction != DropDownDirection.DownLeft)
				menu.AddCssClass(Direction.ToString().ToKebabCase());
			menu.RenderStart(context);
			RenderChildren(context);
			menu.RenderEnd(context);
		}
	}
}
