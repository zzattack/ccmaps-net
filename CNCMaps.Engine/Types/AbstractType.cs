using System;
using System.Collections.Generic;
using System.Linq;
using CNCMaps.FileFormats;

namespace CNCMaps.Engine.Types {
	public class AbstractType {
		public string ID;
		public string Name;
		public string UIName;

		public AbstractType(string ID) {
			this.ID = ID;
		}

		public virtual void LoadRules(IniFile.IniSection rules) {
			Name = rules.ReadString("Name", ID);
			UIName = rules.ReadString("Name");
		}
		public virtual void LoadArt(IniFile.IniSection art) {
		}


		protected List<Abilities> ReadFlags<T1>(List<string> list) {
			throw new NotImplementedException();
		}

		protected static List<T> ReadEnumList<T>(List<string> loose) {
			return loose.Select(l => (T)Enum.Parse(typeof(T), l)).ToList();
		}

		protected static List<T> GetList<T>(List<string> loose) where T : AbstractType {
			return loose.Select(TypesRepository.Get<T>).Where(t => t != null).ToList();
		}

		protected static T Get<T>(string obj) where T : AbstractType {
			return TypesRepository.Get<T>(obj);
		}

	}
}
