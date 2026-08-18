#region copyright notice
// Code obtained from http://www.codeproject.com/Articles/23242/Property-Grid-Dynamic-List-ComboBox-Validation-and
// Written by Dave Elliott
// Licensed under The Code Project Open License (CPOL) - http://www.codeproject.com/info/cpol10.aspx
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace CNCMaps.Shared {
	public enum PropertyFlags {
		[StandardValue("None", "None of the flags should be applied to this property.")]
		None = 0,
		[StandardValue("Display name", "Display name should be retrieved from resource if possible for this property.")]
		LocalizeDisplayName = 1,
		[StandardValue("Category name", "Category name should be retrieved from resource if possible for this property.")]
		LocalizeCategoryName = 2,
		[StandardValue("Description", "Description string should be retrieved from resource if possible for this property.")]
		LocalizeDescription = 4,
		[StandardValue("Enumeration", "Enumerations' display strings should be retrieved from resource if possible  for this property if it is an enumeration type.")]
		LocalizeEnumerations = 8,
		[StandardValue("Exclusive", "Values can only be selected from a list and user are not allowed to type in the value for this property.")]
		ExclusiveStandardValues = 16,

		[StandardValue("Use resource for all string", "Use resource for all string for this property.")]
		LocalizeAllString = LocalizeDisplayName | LocalizeDescription |
			  LocalizeCategoryName | LocalizeEnumerations,

		[StandardValue("Expandable", "Make property expandlabe if property type is IEnemerable")]
		ExpandIEnumerable = 32,

		[StandardValue("Supports standard values", "Property supports standard values.")]
		SupportStandardValues = 64,

		[StandardValue("All flags", "All of the flags should be applied to this property.")]
		All = LocalizeAllString | ExclusiveStandardValues | ExpandIEnumerable | SupportStandardValues,

		Default = LocalizeAllString | SupportStandardValues,
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
	public class PropertyStateFlagsAttribute : Attribute {
		public PropertyStateFlagsAttribute()
			: base() {

		}
		public PropertyStateFlagsAttribute(PropertyFlags flags)
			: base() {
			m_Flags = flags;
		}

		private PropertyFlags m_Flags = PropertyFlags.All & ~PropertyFlags.ExclusiveStandardValues;

		public PropertyFlags Flags {
			get {
				return m_Flags;
			}
			set {
				m_Flags = value;
			}
		}



	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
	public class IdAttribute : Attribute {
		public IdAttribute()
			: base() {
		}

		public IdAttribute(int propertyId, int categoryId)
			: base() {
			PropertyId = propertyId;
			CategoryId = categoryId;
		}
		private int m_PropertyId = 0;

		public int PropertyId {
			get {
				return m_PropertyId;
			}
			set {
				m_PropertyId = value;
			}
		}
		private int m_CategoryId = 0;

		public int CategoryId {
			get {
				return m_CategoryId;
			}
			set {
				m_CategoryId = value;
			}
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
	public class StandardValueAttribute : Attribute {
		//public StandardValueAttribute()
		//{

		//}

		public StandardValueAttribute(object value) {
			m_Value = value;
		}
		public StandardValueAttribute(object value, string displayName) {
			m_DisplayName = displayName;
			m_Value = value;
		}
		public StandardValueAttribute(string displayName, string description) {
			m_DisplayName = displayName;
			m_Description = description;
		}
		private string m_DisplayName = String.Empty;
		public string DisplayName {
			get {
				if (String.IsNullOrEmpty(m_DisplayName)) {
					if (Value != null) {
						return Value.ToString();
					}
				}
				return m_DisplayName;
			}
			set {
				m_DisplayName = value;
			}
		}

		private bool m_Visible = true;
		public bool Visible {
			get {
				return m_Visible;
			}
			set {
				m_Visible = value;
			}
		}

		private bool m_Enabled = true;
		public bool Enabled {
			get {
				return m_Enabled;
			}
			set {
				m_Enabled = value;
			}
		}

		private string m_Description = String.Empty;
		public string Description {
			get {
				return m_Description;
			}
			set {
				m_Description = value;
			}
		}

		internal object m_Value = null;

		public object Value {
			get {
				return m_Value;
			}
		}
		public override string ToString() {
			return DisplayName;
		}
		public static StandardValueAttribute[] GetEnumItems(Type enumType) {
			if (enumType == null) {
				throw new ArgumentNullException("'enumInstance' is null.");
			}

			if (!enumType.IsEnum) {
				throw new ArgumentException("'enumInstance' is not Enum type.");
			}

			ArrayList arrAttr = new ArrayList();
			FieldInfo[] fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static);
			foreach (FieldInfo fi in fields) {
				StandardValueAttribute[] attr = fi.GetCustomAttributes(typeof(StandardValueAttribute), false).Cast<StandardValueAttribute>().ToArray();

				if (attr != null && attr.Length > 0) {
					attr[0].m_Value = fi.GetValue(null);
					arrAttr.Add(attr[0]);
				}
				else {
					StandardValueAttribute atr = new StandardValueAttribute(fi.GetValue(null));
					arrAttr.Add(atr);
				}
			}
			StandardValueAttribute[] retAttr = arrAttr.ToArray(typeof(StandardValueAttribute)) as StandardValueAttribute[];
			return retAttr;
		}



	}
}
