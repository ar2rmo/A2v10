﻿
// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using A2v10.Data.Interfaces;
using A2v10.Infrastructure;

namespace A2v10.Interop
{

	public class XmlCreator
	{
		private readonly IList<String> _schemaPathes;
		private readonly IDataModel _dataModel;
		private readonly String _encoding;
		private readonly XmlSchemaSet _schemaSet;
		private readonly IList<String> _validationErrors;

		public Boolean Validate { get; set; }

		public XmlCreator(IList<String> schemaPathes, IDataModel dataModel, String encoding)
		{
			_schemaPathes = schemaPathes;
			_dataModel = dataModel;
			_encoding = encoding;
			_schemaSet = new XmlSchemaSet();
			_validationErrors = new List<String>();
			Validate = true;
		}

		public Boolean HasErrors => _validationErrors.Count > 0;

		public String ErrorMessage
		{
			get
			{
				if (!HasErrors) return null;
				return String.Join("<br>", _validationErrors);
			}
		}

		public Byte[] CreateXml()
		{
			void eventHandler(Object sender, ValidationEventArgs e)
			{
				if (e.Exception != null)
					_validationErrors.Add(e.Exception.Message);
			}

			foreach (var f in _schemaPathes)
			{
				using (var textReader = XmlReader.Create(f))
				{
					XmlSchema sc = XmlSchema.Read(textReader, eventHandler);
					_schemaSet.Add(sc);
				}
			}

			_schemaSet.Compile();
			return CreateXmlFromSchema();
		}

		public void DoValidate(Stream stream)
		{
			stream.Seek(0, SeekOrigin.Begin);
			XDocument doc = XDocument.Load(stream);
			doc.Validate(_schemaSet, ValidationHandler);
		}

		void ValidationHandler(Object sender, ValidationEventArgs e)
		{
			_validationErrors.Add(e.Message);
		}

		Byte[] CreateXmlFromSchema()
		{
			var sb = new StringBuilder();
			var settings = new XmlWriterSettings()
			{
				Encoding = Encoding.GetEncoding(_encoding),
				Indent = true
			};
			using (var ms = new MemoryStream())
			{
				using (var writer = XmlWriter.Create(ms, settings))
				{
					writer.WriteStartDocument();
					foreach (XmlQualifiedName v in _schemaSet.GlobalElements.Names)
					{
						var elem = _schemaSet.GlobalElements[v] as XmlSchemaElement;
						ProcessElement(writer, elem, 0, _dataModel.Root);
					}
					writer.WriteEndDocument();
				}
				if (Validate)
					DoValidate(ms);
				ms.Seek(0, SeekOrigin.Begin);
				return ms.ToArray();
			}
		}


		public void ProcessArray(XmlWriter writer, XmlSchemaElement elem, Object model, Int32 level)
		{
			Boolean WriteArrayItem(String key, IDictionary<String, Object> dict)
			{
				Boolean written = false;
				foreach (var av in dict)
				{
					if ((key + av.Key == elem.Name) && av.Value != null)
					{
						var typedVal = TypedValue(elem.SchemaTypeName.Name, av.Value, elem.IsNillable);
						if (String.IsNullOrEmpty(typedVal) && elem.IsNillable)
							WriteNil(writer);
						else
							writer.WriteString(typedVal);
						written = true;
						break;
					}
				}
				return written;
			}

			if (model == null)
				return;
			var d = model as IDictionary<String, Object>;
			foreach (var kp in d)
			{
				if (!elem.Name.StartsWith(kp.Key))
					continue;
				Boolean fullElement = kp.Key == elem.Name;
				switch (kp.Value)
				{
					case IList<Object> arr:
						for (var i = 0; i < arr.Count; i++)
						{
							if (fullElement)
							{
								var wrapper = new ExpandoObject();
								wrapper.Set(elem.Name, arr[i]);
								ProcessElement(writer, elem, level + 1, wrapper, simple: false);
							}
							else
							{
								writer.WriteStartElement(elem.Name);
								writer.WriteAttributeString("ROWNUM", (i + 1).ToString());
								Boolean written = WriteArrayItem(kp.Key, arr[i] as IDictionary<String, Object>);
								if (!written)
									writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
								writer.WriteEndElement();
							}
						}
						break;
					case IList<ExpandoObject> arrExp:
						for (var i = 0; i < arrExp.Count; i++)
						{
							writer.WriteStartElement(elem.Name);
							writer.WriteAttributeString("ROWNUM", (i + 1).ToString());
							Boolean written = WriteArrayItem(kp.Key, arrExp[i] as IDictionary<String, Object>);
							if (!written)
								writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
							writer.WriteEndElement();
						}
						break;
					case ExpandoObject eo:
						ProcessElement(writer, elem, level + 1, eo, true);
						break;
				}
			}
		}

		void ProcessElement(XmlWriter writer, XmlSchemaElement elem, Int32 level, ExpandoObject model, Boolean simple = false)
		{
			if (elem.MaxOccurs > 1 && !simple)
			{
				ProcessArray(writer, elem, model, level);
				return;
			}

			var innerModel = model.GetObject(elem.Name);
			if (innerModel == null)
			{
				if (elem.MinOccurs == 0)
					return;
			}

			writer.WriteStartElement(elem.Name);
			if (level == 0 && FirstSchema != null)
				writer.WriteAttributeString("xsi", "noNamespaceSchemaLocation", XmlSchema.InstanceNamespace, FirstSchema);


			switch (elem.ElementSchemaType)
			{
				case XmlSchemaComplexType complexType:
					var pi = complexType.Particle as XmlSchemaSequence;
					if (pi != null)
					{
						if (complexType.AttributeUses != null)
						{
							foreach (var an in complexType.AttributeUses.Names)
							{
								var attr = complexType.AttributeUses[an as XmlQualifiedName] as XmlSchemaAttribute;
								WriteAttribute(writer, attr, model);
							}
						}
						foreach (var p in pi?.Items)
						{
							switch (p)
							{
								case XmlSchemaElement schemaElem:
									if (simple)
									{
										writer.WriteStartElement(schemaElem.Name);
										WriteSimpleElement(writer, schemaElem, model);
										writer.WriteEndElement();
									}
									else
										ProcessElement(writer, schemaElem, level + 1, innerModel as ExpandoObject);
									break;
								case XmlSchemaChoice schemaChoice:
									WriteElementChoice(writer, schemaChoice, innerModel as ExpandoObject);
									break;
							}
						}
					}
					else if (complexType.ContentModel is XmlSchemaSimpleContent)
					{
						WriteSimpleElement(writer, elem, model);
					}
					break;
				case XmlSchemaSimpleType simpleType:
					WriteSimpleElement(writer, elem, model);
					break;
			}
			writer.WriteEndElement();
		}

		void WriteAttribute(XmlWriter writer, XmlSchemaAttribute attr, ExpandoObject model)
		{
			Object val = model.Get<Object>(attr.Name);
			if (val != null)
			{
				var strVal = TypedValue(attr.SchemaTypeName.Name, val, false);
				if (!String.IsNullOrEmpty(strVal))
					writer.WriteAttributeString(attr.Name, strVal);
			}
		}

		void WriteNil(XmlWriter writer)
		{
			writer.WriteAttributeString("xsi", "nil", XmlSchema.InstanceNamespace, "true");
		}

		void WriteSimpleElement(XmlWriter writer, XmlSchemaElement elem, ExpandoObject model)
		{
			if (elem.FixedValue != null)
			{
				writer.WriteString(elem.FixedValue);
				return;
			}
			Object val = model.Get<Object>(elem.Name);
			if (val != null)
			{
				var strVal = TypedValue(elem.SchemaTypeName.Name, val, elem.IsNillable);
				if (String.IsNullOrEmpty(strVal) && elem.IsNillable)
					WriteNil(writer);
				else
					writer.WriteString(strVal);
			}
			else if (elem.IsNillable)
				WriteNil(writer);
			else
			{
				// TODO: check nullability ????
				//throw new XmlCreatorException($"Value for the field '{elem.Name}' is not nullable");
			}
		}

		void WriteElementChoice(XmlWriter writer, XmlSchemaChoice choice, ExpandoObject model)
		{
			foreach (var ch in choice.Items)
			{
				if (!(ch is XmlSchemaElement se))
					continue;
				var val = model.Get<Object>(se.Name);
				if (val != null)
				{
					var typedVal = TypedValue(se.SchemaTypeName.Name, val, se.IsNillable);
					if (typedVal != null)
					{
						writer.WriteStartElement(se.Name);
						writer.WriteString(typedVal);
						writer.WriteEndElement();
						return;
					}
				}
			}
		}

		String TypedValue(String typeName, Object val, Boolean isNillable)
		{
			if (val == null)
				return null;
			switch (typeName)
			{
				case "DGpercentAlloc":
					var dVal1 = Convert.ToDecimal(val);
					return String.Format(CultureInfo.InvariantCulture, "{0:0.00}", dVal1); ;
				case "DGdecimal2":
				case "Decimal2Column":
					var dVal2 = Convert.ToDecimal(val);
					return String.Format(CultureInfo.InvariantCulture, "{0:0.00}", dVal2); ;
				case "DGdecimal2_P":
				case "Decimal2Column_P":
					var dVal2p = Convert.ToDecimal(val);
					return String.Format(CultureInfo.InvariantCulture, "{0:0.00}", dVal2p);
				case "DGdecimal3":
				case "Decimal3Column":
					var dVal3 = Convert.ToDecimal(val);
					return String.Format(CultureInfo.InvariantCulture, "{0:0.000}", dVal3); ;
				case "DGdecimal0":
					var dVal0 = Convert.ToDecimal(val);
					if (isNillable && dVal0 == 0)
						return null;
					return String.Format(CultureInfo.InvariantCulture, "{0:0}", dVal0); ;
				case "DGchk":
				case "ChkColumn":
					var bVal = Convert.ToBoolean(val);
					return bVal ? "1" : null;
				case "DGMonth":
					var intVal = Convert.ToInt32(val);
					if (intVal == 0)
						return null;
					return intVal.ToString();
			}
			return val.ToString();
		}

		String FirstSchema
		{
			get
			{
				if (_schemaPathes.Count == 0)
					return null;
				var ss = _schemaPathes[0];
				return Path.GetFileName(ss); // file name without path
			}
		}
	}
}
