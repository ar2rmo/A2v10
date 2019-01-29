﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

using System;
using System.Windows.Markup;

namespace A2v10.Xaml
{
	[ContentProperty("Content")]
	public class Hyperlink : Inline
	{
		public Object Content { get; set; }
		public ControlSize Size { get; set; }
		public Icon Icon { get; set; }

		public Command Command { get; set; }

		public UIElementBase DropDown { get; set; }

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			if (SkipRender(context))
				return;
			Boolean bHasDropDown = DropDown != null;
			if (bHasDropDown)
			{
				DropDownDirection? dir = (DropDown as DropDownMenu)?.Direction;
				Boolean bDropUp = (dir == DropDownDirection.UpLeft) || (dir == DropDownDirection.UpRight);
				var wrap = new TagBuilder("div", "dropdown hlink-dd-wrapper", IsInGrid)
					.AddCssClass(bDropUp ? "dir-up" : "dir-down")
					.MergeAttribute("v-dropdown", String.Empty);
				onRender?.Invoke(wrap);
				if (!Block)
					wrap.AddCssClass("a2-inline");
				MergeAttributes(wrap, context, MergeAttrMode.Visibility);
				wrap.RenderStart(context);
				var hasAddOn = wrap.HasClass("add-on");
				RenderHyperlink(context, false, null, inside:true, addOn:hasAddOn);
				DropDown.RenderElement(context);
				wrap.RenderEnd(context);
			}
			else
			{
				RenderHyperlink(context, IsInGrid, onRender, false);
			}
		}

		void RenderHyperlink(RenderContext context, Boolean inGrid, Action<TagBuilder> onRender = null, Boolean inside = false, Boolean addOn = false)
		{
			Boolean bHasDropDown = DropDown != null;

			var tag = new TagBuilder("a", "a2-hyperlink", inGrid);
			onRender?.Invoke(tag);
			var attrMode = MergeAttrMode.All;
			if (inside)
				attrMode &= ~MergeAttrMode.Visibility;
			MergeAttributes(tag, context, attrMode);
			MergeCommandAttribute(tag, context);
			tag.AddCssClassBool(Block, "block");
			if (!Block)
				tag.AddCssClass("a2-inline");

			if (Size != ControlSize.Default)
			{
				switch (Size)
				{
					case ControlSize.Small:
						tag.AddCssClass("small");
						break;
					case ControlSize.Large:
						tag.AddCssClass("large");
						break;
					case ControlSize.Normal:
						tag.AddCssClass("normal");
						break;
					default:
						throw new XamlException("Only ControlSize.Small, ControlSize.Normal or ControlSize.Large is supported for the Hyperlink");
				}
			}

			if (bHasDropDown)
				tag.MergeAttribute("toggle", String.Empty);

			tag.RenderStart(context);

			RenderIcon(context, Icon);
			var cbind = GetBinding(nameof(Content));
			if (cbind != null)
			{
				new TagBuilder("span")
					.MergeAttribute("v-text", cbind.GetPathFormat(context))
					.Render(context);
			}
			else if (Content is UIElementBase)
			{
				(Content as UIElementBase).RenderElement(context);
			}
			else if (Content != null)
			{
				context.Writer.Write(context.Localize(Content.ToString()));
			}

			if (bHasDropDown && !addOn)
			{
				var bDropUp = (DropDown as DropDownMenu)?.IsDropUp;
				new TagBuilder("span", "caret")
					.AddCssClassBool(bDropUp, "up")
					.Render(context);
			}
			tag.RenderEnd(context);
		}

		protected override void OnEndInit()
		{
			base.OnEndInit();
			if (DropDown != null && GetBindingCommand(nameof(Command)) != null)
				throw new XamlException("Hyperlink. The DropDown and Command can't be specified at the same time");
		}
	}
}
