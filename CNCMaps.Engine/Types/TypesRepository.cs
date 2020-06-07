using System;

namespace CNCMaps.Engine.Types {
	class TypesRepository {
		public static T Get<T>(string s) where T : AbstractType {
			throw new NotImplementedException();
		}

		internal static Animation GetAnimType(string p) {
			throw new NotImplementedException();
		}

		internal static WeaponType GetWeaponType(string p) {
			throw new NotImplementedException();
		}

		internal static AircraftType GetAircraftType(string p) {
			throw new NotImplementedException();
		}

		internal static InfantryType GetInfantryType(string p) {
			throw new NotImplementedException();
		}

		internal static Sound GetSound(string p) {
			throw new NotImplementedException();
		}

	}
}
