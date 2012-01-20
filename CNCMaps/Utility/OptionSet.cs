using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CNCMaps.Utility {
	public class OptionSet : KeyedCollection<string, Option> {

		public OptionSet()
			: this(delegate(string f) { return f; }) {
		}

		public OptionSet(Converter<string, string> localizer) {
			this.localizer = localizer;
		}

		Converter<string, string> localizer;

		public Converter<string, string> MessageLocalizer {
			get { return localizer; }
		}

		protected override string GetKeyForItem(Option item) {
			if (item == null)
				throw new ArgumentNullException("option");
			if (item.Names != null && item.Names.Length > 0)
				return item.Names[0];
			// This should never happen, as it's invalid for Option to be
			// constructed w/o any names.
			throw new InvalidOperationException("Option has no names!");
		}

		[Obsolete("Use KeyedCollection.this[string]")]
		protected Option GetOptionForName(string option) {
			if (option == null)
				throw new ArgumentNullException("option");
			try {
				return base[option];
			}
			catch (KeyNotFoundException) {
				return null;
			}
		}

		protected override void InsertItem(int index, Option item) {
			base.InsertItem(index, item);
			AddImpl(item);
		}

		protected override void RemoveItem(int index) {
			base.RemoveItem(index);
			Option p = Items[index];
			// KeyedCollection.RemoveItem() handles the 0th item
			for (int i = 1; i < p.Names.Length; ++i) {
				Dictionary.Remove(p.Names[i]);
			}
		}

		protected override void SetItem(int index, Option item) {
			base.SetItem(index, item);
			RemoveItem(index);
			AddImpl(item);
		}

		private void AddImpl(Option option) {
			if (option == null)
				throw new ArgumentNullException("option");
			var added = new List<string>(option.Names.Length);
			try {
				// KeyedCollection.InsertItem/SetItem handle the 0th name.
				for (int i = 1; i < option.Names.Length; ++i) {
					Dictionary.Add(option.Names[i], option);
					added.Add(option.Names[i]);
				}
			}
			catch (Exception) {
				foreach (string name in added)
					Dictionary.Remove(name);
				throw;
			}
		}

		public new OptionSet Add(Option option) {
			base.Add(option);
			return this;
		}

		sealed class ActionOption : Option {
			Action<OptionValueCollection> action;

			public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action)
				: base(prototype, description, count) {
				if (action == null)
					throw new ArgumentNullException("action");
				this.action = action;
			}

			protected override void OnParseComplete(OptionContext c) {
				action(c.OptionValues);
			}
		}

		public OptionSet Add(string prototype, Action<string> action) {
			return Add(prototype, null, action);
		}

		public OptionSet Add(string prototype, string description, Action<string> action) {
			if (action == null)
				throw new ArgumentNullException("action");
			Option p = new ActionOption(prototype, description, 1,
			                            delegate(OptionValueCollection v) { action(v[0]); });
			base.Add(p);
			return this;
		}

		public OptionSet Add(string prototype, OptionAction<string, string> action) {
			return Add(prototype, null, action);
		}

		public OptionSet Add(string prototype, string description, OptionAction<string, string> action) {
			if (action == null)
				throw new ArgumentNullException("action");
			Option p = new ActionOption(prototype, description, 2,
			                            delegate(OptionValueCollection v) { action(v[0], v[1]); });
			base.Add(p);
			return this;
		}

		sealed class ActionOption<T> : Option {
			Action<T> action;

			public ActionOption(string prototype, string description, Action<T> action)
				: base(prototype, description, 1) {
				if (action == null)
					throw new ArgumentNullException("action");
				this.action = action;
			}

			protected override void OnParseComplete(OptionContext c) {
				action(Parse<T>(c.OptionValues[0], c));
			}
		}

		sealed class ActionOption<TKey, TValue> : Option {
			OptionAction<TKey, TValue> action;

			public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action)
				: base(prototype, description, 2) {
				if (action == null)
					throw new ArgumentNullException("action");
				this.action = action;
			}

			protected override void OnParseComplete(OptionContext c) {
				action(
					Parse<TKey>(c.OptionValues[0], c),
					Parse<TValue>(c.OptionValues[1], c));
			}
		}

		public OptionSet Add<T>(string prototype, Action<T> action) {
			return Add(prototype, null, action);
		}

		public OptionSet Add<T>(string prototype, string description, Action<T> action) {
			return Add(new ActionOption<T>(prototype, description, action));
		}

		public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action) {
			return Add(prototype, null, action);
		}

		public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action) {
			return Add(new ActionOption<TKey, TValue>(prototype, description, action));
		}

		protected virtual OptionContext CreateOptionContext() {
			return new OptionContext(this);
		}

#if LINQ
		public List<string> Parse (IEnumerable<string> arguments)
		{
			bool process = true;
			OptionContext c = CreateOptionContext ();
			c.OptionIndex = -1;
			var def = GetOptionForName ("<>");
			var unprocessed =
				from argument in arguments
				where ++c.OptionIndex >= 0 && (process || def != null)
					? process
						? argument == "--"
							? (process = false)
							: !Parse (argument, c)
								? def != null
									? Unprocessed (null, def, c, argument)
									: true
								: false
						: def != null
							? Unprocessed (null, def, c, argument)
							: true
					: true
				select argument;
			List<string> r = unprocessed.ToList ();
			if (c.Option != null)
				c.Option.Invoke (c);
			return r;
		}
#else

		public List<string> Parse(IEnumerable<string> arguments) {
			OptionContext c = CreateOptionContext();
			c.OptionIndex = -1;
			bool process = true;
			var unprocessed = new List<string>();
			Option def = Contains("<>") ? this["<>"] : null;
			foreach (string argument in arguments) {
				++c.OptionIndex;
				if (argument == "--") {
					process = false;
					continue;
				}
				if (!process) {
					Unprocessed(unprocessed, def, c, argument);
					continue;
				}
				if (!Parse(argument, c))
					Unprocessed(unprocessed, def, c, argument);
			}
			if (c.Option != null)
				c.Option.Invoke(c);
			return unprocessed;
		}

#endif

		private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument) {
			if (def == null) {
				extra.Add(argument);
				return false;
			}
			c.OptionValues.Add(argument);
			c.Option = def;
			c.Option.Invoke(c);
			return false;
		}

		private readonly Regex ValueOption = new Regex(
			@"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

		protected bool GetOptionParts(string argument, out string flag, out string name, out string sep, out string value) {
			if (argument == null)
				throw new ArgumentNullException("argument");

			flag = name = sep = value = null;
			Match m = ValueOption.Match(argument);
			if (!m.Success) {
				return false;
			}
			flag = m.Groups["flag"].Value;
			name = m.Groups["name"].Value;
			if (m.Groups["sep"].Success && m.Groups["value"].Success) {
				sep = m.Groups["sep"].Value;
				value = m.Groups["value"].Value;
			}
			return true;
		}

		protected virtual bool Parse(string argument, OptionContext c) {
			if (c.Option != null) {
				ParseValue(argument, c);
				return true;
			}

			string f, n, s, v;
			if (!GetOptionParts(argument, out f, out n, out s, out v))
				return false;

			Option p;
			if (Contains(n)) {
				p = this[n];
				c.OptionName = f + n;
				c.Option = p;
				switch (p.OptionValueType) {
					case OptionValueType.None:
						c.OptionValues.Add(n);
						c.Option.Invoke(c);
						break;
					case OptionValueType.Optional:
					case OptionValueType.Required:
						ParseValue(v, c);
						break;
				}
				return true;
			}
			// no match; is it a bool option?
			if (ParseBool(argument, n, c))
				return true;
			// is it a bundled option?
			if (ParseBundledValue(f, string.Concat(n + s + v), c))
				return true;

			return false;
		}

		private void ParseValue(string option, OptionContext c) {
			if (option != null)
				foreach (string o in c.Option.ValueSeparators != null
				                     	? option.Split(c.Option.ValueSeparators, StringSplitOptions.None)
				                     	: new[] { option }) {
					c.OptionValues.Add(o);
				}
			if (c.OptionValues.Count == c.Option.MaxValueCount ||
			    c.Option.OptionValueType == OptionValueType.Optional)
				c.Option.Invoke(c);
			else if (c.OptionValues.Count > c.Option.MaxValueCount) {
				throw new OptionException(localizer(string.Format(
					"Error: Found {0} option values when expecting {1}.",
					c.OptionValues.Count, c.Option.MaxValueCount)),
				                          c.OptionName);
			}
		}

		private bool ParseBool(string option, string n, OptionContext c) {
			Option p;
			string rn;
			if (n.Length >= 1 && (n[n.Length - 1] == '+' || n[n.Length - 1] == '-') &&
			    Contains((rn = n.Substring(0, n.Length - 1)))) {
				p = this[rn];
				string v = n[n.Length - 1] == '+' ? option : null;
				c.OptionName = option;
				c.Option = p;
				c.OptionValues.Add(v);
				p.Invoke(c);
				return true;
			}
			return false;
		}

		private bool ParseBundledValue(string f, string n, OptionContext c) {
			if (f != "-")
				return false;
			for (int i = 0; i < n.Length; ++i) {
				Option p;
				string opt = f + n[i].ToString();
				string rn = n[i].ToString();
				if (!Contains(rn)) {
					if (i == 0)
						return false;
					throw new OptionException(string.Format(localizer(
						"Cannot bundle unregistered option '{0}'."), opt), opt);
				}
				p = this[rn];
				switch (p.OptionValueType) {
					case OptionValueType.None:
						Invoke(c, opt, n, p);
						break;
					case OptionValueType.Optional:
					case OptionValueType.Required: {
						string v = n.Substring(i + 1);
						c.Option = p;
						c.OptionName = opt;
						ParseValue(v.Length != 0 ? v : null, c);
						return true;
					}
					default:
						throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
				}
			}
			return true;
		}

		private static void Invoke(OptionContext c, string name, string value, Option option) {
			c.OptionName = name;
			c.Option = option;
			c.OptionValues.Add(value);
			option.Invoke(c);
		}

		private const int OptionWidth = 29;

		public void WriteOptionDescriptions(TextWriter o) {
			foreach (Option p in this) {
				int written = 0;
				if (!WriteOptionPrototype(o, p, ref written))
					continue;

				if (written < OptionWidth)
					o.Write(new string(' ', OptionWidth - written));
				else {
					o.WriteLine();
					o.Write(new string(' ', OptionWidth));
				}

				List<string> lines = GetLines(localizer(GetDescription(p.Description)));
				o.WriteLine(lines[0]);
				var prefix = new string(' ', OptionWidth + 2);
				for (int i = 1; i < lines.Count; ++i) {
					o.Write(prefix);
					o.WriteLine(lines[i]);
				}
			}
		}

		bool WriteOptionPrototype(TextWriter o, Option p, ref int written) {
			string[] names = p.Names;

			int i = GetNextOptionIndex(names, 0);
			if (i == names.Length)
				return false;

			if (names[i].Length == 1) {
				Write(o, ref written, "  -");
				Write(o, ref written, names[0]);
			}
			else {
				Write(o, ref written, "      --");
				Write(o, ref written, names[0]);
			}

			for (i = GetNextOptionIndex(names, i + 1);
			     i < names.Length; i = GetNextOptionIndex(names, i + 1)) {
				Write(o, ref written, ", ");
				Write(o, ref written, names[i].Length == 1 ? "-" : "--");
				Write(o, ref written, names[i]);
			}

			if (p.OptionValueType == OptionValueType.Optional ||
			    p.OptionValueType == OptionValueType.Required) {
				if (p.OptionValueType == OptionValueType.Optional) {
					Write(o, ref written, localizer("["));
				}
				Write(o, ref written, localizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
				string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0
				             	? p.ValueSeparators[0]
				             	: " ";
				for (int c = 1; c < p.MaxValueCount; ++c) {
					Write(o, ref written, localizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
				}
				if (p.OptionValueType == OptionValueType.Optional) {
					Write(o, ref written, localizer("]"));
				}
			}
			return true;
		}

		static int GetNextOptionIndex(string[] names, int i) {
			while (i < names.Length && names[i] == "<>") {
				++i;
			}
			return i;
		}

		static void Write(TextWriter o, ref int n, string s) {
			n += s.Length;
			o.Write(s);
		}

		private static string GetArgumentName(int index, int maxIndex, string description) {
			if (description == null)
				return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
			string[] nameStart;
			if (maxIndex == 1)
				nameStart = new[] { "{0:", "{" };
			else
				nameStart = new[] { "{" + index + ":" };
			for (int i = 0; i < nameStart.Length; ++i) {
				int start, j = 0;
				do {
					start = description.IndexOf(nameStart[i], j);
				} while (start >= 0 && j != 0 ? description[j++ - 1] == '{' : false);
				if (start == -1)
					continue;
				int end = description.IndexOf("}", start);
				if (end == -1)
					continue;
				return description.Substring(start + nameStart[i].Length, end - start - nameStart[i].Length);
			}
			return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
		}

		private static string GetDescription(string description) {
			if (description == null)
				return string.Empty;
			var sb = new StringBuilder(description.Length);
			int start = -1;
			for (int i = 0; i < description.Length; ++i) {
				switch (description[i]) {
					case '{':
						if (i == start) {
							sb.Append('{');
							start = -1;
						}
						else if (start < 0)
							start = i + 1;
						break;
					case '}':
						if (start < 0) {
							if ((i + 1) == description.Length || description[i + 1] != '}')
								throw new InvalidOperationException("Invalid option description: " + description);
							++i;
							sb.Append("}");
						}
						else {
							sb.Append(description.Substring(start, i - start));
							start = -1;
						}
						break;
					case ':':
						if (start < 0)
							goto default;
						start = i + 1;
						break;
					default:
						if (start < 0)
							sb.Append(description[i]);
						break;
				}
			}
			return sb.ToString();
		}

		private static List<string> GetLines(string description) {
			var lines = new List<string>();
			if (string.IsNullOrEmpty(description)) {
				lines.Add(string.Empty);
				return lines;
			}
			int length = 80 - OptionWidth - 2;
			int start = 0, end;
			do {
				end = GetLineEnd(start, length, description);
				bool cont = false;
				if (end < description.Length) {
					char c = description[end];
					if (c == '-' || (char.IsWhiteSpace(c) && c != '\n'))
						++end;
					else if (c != '\n') {
						cont = true;
						--end;
					}
				}
				lines.Add(description.Substring(start, end - start));
				if (cont) {
					lines[lines.Count - 1] += "-";
				}
				start = end;
				if (start < description.Length && description[start] == '\n')
					++start;
			} while (end < description.Length);
			return lines;
		}

		private static int GetLineEnd(int start, int length, string description) {
			int end = Math.Min(start + length, description.Length);
			int sep = -1;
			for (int i = start; i < end; ++i) {
				switch (description[i]) {
					case ' ':
					case '\t':
					case '\v':
					case '-':
					case ',':
					case '.':
					case ';':
						sep = i;
						break;
					case '\n':
						return i;
				}
			}
			if (sep == -1 || end == description.Length)
				return end;
			return sep;
		}
	}
}