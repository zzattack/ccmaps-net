using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CNCMaps.Utility {
	public class CsvConverter : TypeConverter {
		// Overrides the ConvertTo method of TypeConverter.
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			var v = value as IEnumerable<String>;
			if (destinationType == typeof(string)) {
				return string.Join(", ", v.Select(AddQuotes).ToArray());
			}
			return base.ConvertTo(context, culture, value, destinationType);
		}

		private string AddQuotes(string s) {
			if (string.IsNullOrWhiteSpace(s)) return "";

			var sb = new StringBuilder();
			if (!s.StartsWith("\""))
				sb.Append('"');
			sb.Append(s);
			if (!s.EndsWith("\""))
				sb.Append('"');
			return sb.ToString();
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
			if (sourceType == typeof(string)) {
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		public override bool CanConvertTo(ITypeDescriptorContext context, Type sourceType) {
			if (sourceType == typeof(IList<string>)) {
				return true;
			}
			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
			if (value is string) {
				var vs = ((string)value).Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
				return vs.Select(v => v.Trim('"')).ToList();
			}
			return base.ConvertFrom(context, culture, value);
		}

		public override bool IsValid(ITypeDescriptorContext context, object value) {
			return true; // basically anything is valid
		}
	}
}