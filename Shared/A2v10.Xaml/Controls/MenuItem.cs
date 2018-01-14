﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;

namespace A2v10.Xaml
{
    public class MenuItem: CommandControl
    {
        public Icon Icon { get; set; }

        internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
        {
            var mi = new TagBuilder("a", "dropdown-item");
            if (HasIcon)
            {
                MergeAttributes(mi, context, MergeAttrMode.NoContent);
                mi.RenderStart(context);
                RenderIcon(context, Icon);
                var span = new TagBuilder("span");
                MergeAttributesBase(span, context, MergeAttrMode.Content); // skip command!
                span.RenderStart(context);
                RenderContent(context);
                span.RenderEnd(context);
                mi.RenderEnd(context);
            }
            else {
                MergeAttributes(mi, context, MergeAttrMode.All);
                mi.RenderStart(context);
                RenderContent(context);
                mi.RenderEnd(context);
            }
        }

        Boolean HasIcon => GetBinding(nameof(Icon)) != null || Icon != Icon.NoIcon;
    }
}
