﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;

using A2v10.Infrastructure;

namespace A2v10.Xaml
{
	public class Grid : Container
	{

		#region Attached Properties
		[ThreadStatic]
		static IDictionary<Object, Int32> _attachedColumn;
		[ThreadStatic]
		static IDictionary<Object, Int32> _attachedRow;
		[ThreadStatic]
		static IDictionary<Object, Int32> _attachedColSpan;
		[ThreadStatic]
		static IDictionary<Object, Int32> _attachedRowSpan;
		[ThreadStatic]
		static IDictionary<Object, VerticalAlign> _attachedVAlign;


		public static void SetCol(Object obj, Int32 col)
		{
			if (_attachedColumn == null)
				_attachedColumn = new Dictionary<Object, Int32>();
			AttachedHelpers.SetAttached(_attachedColumn, obj, col);
		}

		public static Int32? GetCol(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedColumn, obj);
		}

		public static void SetRow(Object obj, Int32 row)
		{
			if (_attachedRow == null)
				_attachedRow = new Dictionary<Object, Int32>();
			AttachedHelpers.SetAttached(_attachedRow, obj, row);
		}

		public static Int32? GetRow(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedRow, obj);
		}

		public static void SetColSpan(Object obj, Int32 span)
		{
			if (_attachedColSpan == null)
				_attachedColSpan = new Dictionary<Object, Int32>();
			AttachedHelpers.SetAttached(_attachedColSpan, obj, span);
		}

		public static Int32? GetColSpan(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedColSpan, obj);
		}

		public static void SetRowSpan(Object obj, Int32 span)
		{
			if (_attachedRowSpan == null)
				_attachedRowSpan = new Dictionary<Object, Int32>();
			AttachedHelpers.SetAttached(_attachedRowSpan, obj, span);
		}

		public static Int32? GetRowSpan(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedRowSpan, obj);
		}

		public static void SetVAlign(Object obj, VerticalAlign vAlign)
		{
			if (_attachedVAlign == null)
				_attachedVAlign = new Dictionary<Object, VerticalAlign>();
			AttachedHelpers.SetAttached(_attachedVAlign, obj, vAlign);
		}

		public static VerticalAlign? GetVAlign(Object obj)
		{
			return AttachedHelpers.GetAttached(_attachedVAlign, obj);
		}

		public static void ClearAttached()
		{
			_attachedRow = null;
			_attachedColumn = null;
			_attachedRowSpan = null;
			_attachedColSpan = null;
			_attachedVAlign = null;
		}
		#endregion


		public Length Height { get; set; }

		public BackgroundStyle Background { get; set; }

		public ShadowStyle DropShadow { get; set; }


		RowDefinitions _rows;
		ColumnDefinitions _columns;

		public RowDefinitions Rows
		{
			get
			{
				if (_rows == null)
					_rows = new RowDefinitions();
				return _rows;
			}
			set
			{
				_rows = value;
			}
		}

		public ColumnDefinitions Columns
		{
			get
			{
				if (_columns == null)
					_columns = new ColumnDefinitions();
				return _columns;
			}
			set
			{
				_columns = value;
			}
		}

		internal override void RenderElement(RenderContext context, Action<TagBuilder> onRender = null)
		{
			if (SkipRender(context))
				return;
			var grid = new TagBuilder("div", "grid", IsInGrid);
			onRender?.Invoke(grid);
			MergeAttributes(grid, context);
			if (Height != null)
				grid.MergeStyle("height", Height.Value);
			if (_rows != null)
				grid.MergeStyle("grid-template-rows", _rows.ToAttribute());
			if (_columns != null)
				grid.MergeStyle("grid-template-columns", _columns.ToAttribute());
			if (Background != BackgroundStyle.None)
				grid.AddCssClass("background-" + Background.ToString().ToKebabCase());
			if (DropShadow != ShadowStyle.None)
			{
				grid.AddCssClass("drop-shadow");
				grid.AddCssClass(DropShadow.ToString().ToLowerInvariant());
			}

			grid.RenderStart(context);
			RenderChildren(context);
			grid.RenderEnd(context);
		}

		internal override void RenderChildren(RenderContext context, Action<TagBuilder> onRenderStatic = null)
		{
			foreach (var ch in Children)
			{
				ch.IsInGrid = true;
				if (ch is GridGroup gg)
				{
					var grpTemplate = new TagBuilder("template");
					gg.MergeAttributes(grpTemplate, context, MergeAttrMode.Visibility);
					grpTemplate.RenderStart(context);
					foreach (var gc in gg.Children)
					{
						gc.IsInGrid = true;
						using (context.GridContext(GetRow(gc), GetCol(gc), GetRowSpan(gc), GetColSpan(gc), GetVAlign(gc)))
						{
							gc.RenderElement(context);
						}
					}
					grpTemplate.RenderEnd(context);
				}
				else
				{
					using (context.GridContext(GetRow(ch), GetCol(ch), GetRowSpan(ch), GetColSpan(ch), GetVAlign(ch)))
					{
						ch.RenderElement(context);
					}
				}
			}
		}
	}

	public class RowDefinition
	{
		public GridLength Height { get; set; }
	}

	[TypeConverter(typeof(RowDefinitionsConverter))]
	public class RowDefinitions : List<RowDefinition>
	{
		public static RowDefinitions FromString(String val)
		{
			var coll = new RowDefinitions();
			foreach (var row in val.Split(','))
			{
				var rd = new RowDefinition
				{
					Height = GridLength.FromString(row.Trim())
				};
				coll.Add(rd);
			}
			return coll;
		}
		public String ToAttribute()
		{
			var sb = new StringBuilder();
			foreach (var w in this)
			{
				sb.Append(w.Height.Value).Append(" ");
			}
			return sb.ToString();
		}
	}

	public class ColumnDefinition
	{
		public GridLength Width { get; set; }
	}

	[TypeConverter(typeof(ColumnDefinitionsConverter))]
	public class ColumnDefinitions : List<ColumnDefinition>
	{
		public static ColumnDefinitions FromString(String val)
		{
			var coll = new ColumnDefinitions();
			foreach (var row in val.Split(','))
			{
				var cd = new ColumnDefinition
				{
					Width = GridLength.FromString(row.Trim())
				};
				coll.Add(cd);
			}
			return coll;
		}

		public String ToAttribute()
		{
			var sb = new StringBuilder();
			foreach (var w in this)
			{
				sb.Append(w.Width.Value).Append(" ");
			}
			return sb.ToString();
		}
	}

	public class RowDefinitionsConverter : TypeConverter
	{
		public override Boolean CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(String))
				return true;
			else if (sourceType == typeof(RowDefinitions))
				return true;
			return false;
		}

		public override Object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
		{
			if (value == null)
				return null;
			if (value is RowDefinitions)
				return value;
			else if (value is String)
			{
				return RowDefinitions.FromString(value.ToString());
			}
			return base.ConvertFrom(context, culture, value);
		}
	}

	public class ColumnDefinitionsConverter : TypeConverter
	{
		public override Boolean CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof(String))
				return true;
			else if (sourceType == typeof(ColumnDefinitions))
				return true;
			return false;
		}

		public override Object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, Object value)
		{
			if (value == null)
				return null;
			if (value is ColumnDefinitions)
				return value;
			else if (value is String)
			{
				return ColumnDefinitions.FromString(value.ToString());
			}
			return base.ConvertFrom(context, culture, value);
		}
	}
}
