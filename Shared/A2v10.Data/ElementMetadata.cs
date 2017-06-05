﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A2v10.Data
{
	public class ElementMetadata
	{
		IDictionary<String, FieldMetadata> _fields = new Dictionary<String, FieldMetadata>();

		public String Id { get; private set; }
		public String Name { get; private set; }

		public void AddField(FieldInfo field, FieldType type)
		{
			if (!field.IsVisible)
				return;
			if (IsFieldExists(field.PropertyName, type))
				return;
			FieldMetadata fm = new FieldMetadata(field, type);
			_fields.Add(field.PropertyName, fm);
			switch (field.SpecType)
			{
				case SpecType.Id:
					Id = field.PropertyName;
					break;
				case SpecType.Name:
					Name = field.PropertyName;
					break;
			}
		}

		public Int32 FieldCount { get { return _fields.Count; } }

		public Boolean ContainsField(String field)
		{
			return _fields.ContainsKey(field);
		}

		bool IsFieldExists(String name, FieldType fieldType)
		{
			FieldMetadata fm;
			if (_fields.TryGetValue(name, out fm))
			{
				if (fm.Type != fieldType)
					throw new DataLoaderException($"Invalid property '{name}'. Type mismatch.");
				return true;
			}
			return false;
		}
	}
}
