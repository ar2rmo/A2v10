﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Markup;

using A2v10.Infrastructure;

namespace A2v10.Xaml
{
    public enum RowStyle
    {
        Default,
        Title,
        Header,
        Footer,
        Total
    }

    [ContentProperty("Cells")]
    public class SheetRow : UIElement
    {
        public SheetCells Cells { get; } = new SheetCells();

        public RowStyle Style { get; set; }

        internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
        {
            var tr = new TagBuilder("tr");
            if (onRender != null)
                onRender(tr);
            MergeAttributes(tr, context);
            if (Style != RowStyle.Default)
                tr.AddCssClass("row-" + Style.ToString().ToKebabCase());
            tr.RenderStart(context);
            foreach (var c in Cells)
                c.RenderElement(context);
            tr.RenderEnd(context);
        }

        protected override void OnEndInit()
        {
            base.OnEndInit();
            foreach (var c in Cells)
                c.SetParent(this);
        }
    }

    [TypeConverter(typeof(SheetRowsConverter))]
    public class SheetRows : List<SheetRow>
    {
    }

    public class SheetRowsConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(String))
                return true;
            return false;
        }

        public override Object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
        {
            if (value == null)
                return null;
            if (value is String)
            {
                SheetRows rows = new SheetRows();
                SheetRow row = new SheetRow();
                rows.Add(row);
                foreach (var s in value.ToString().Split(','))
                {
                    row.Cells.Add(new SheetCell() { Content = s.Trim() });
                }
                return rows;
            }
            throw new XamlException($"Invalid SheetRows value '{value}'");
        }
    }
}
