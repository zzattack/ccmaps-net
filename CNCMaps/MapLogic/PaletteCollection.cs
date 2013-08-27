using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {
	// Starkku: Changed from struct to class to allow inclusion of methods for custom palette handling.
	class PaletteCollection : IEnumerable<Palette> {
		private TheaterType _theaterType; // needed for custom palettes
		public List<Palette> CustomPalettes = new List<Palette>();
		public Palette IsoPalette, OvlPalette, UnitPalette, AnimPalette;

		internal Palette GetPalette(PaletteSettings paletteType) {
			switch (paletteType) {
				case PaletteSettings.Anim: return AnimPalette;
				case PaletteSettings.Overlay: return OvlPalette;
				case PaletteSettings.Unit: return UnitPalette;
				case PaletteSettings.Custom:
					throw new ArgumentException("GetPalette only works on built-in default palettes");
				case PaletteSettings.Iso:
				default:
					return IsoPalette;
			}
		}

		public IEnumerator<Palette> GetEnumerator() {
			var p = new List<Palette>();
			p.Add(IsoPalette);
			p.Add(OvlPalette);
			p.Add(UnitPalette);
			p.Add(AnimPalette);
			p.AddRange(CustomPalettes);
			return p.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		/// <param name="theaterType">Type of the theatre that this palette collection belongs to.</param>
		public PaletteCollection(TheaterType theaterType) {
			this._theaterType = theaterType;
		}

		/// <summary>
		/// Gets a custom palette from collection. If custom palette is not found, creates one, adds it to the collection and returns it.
		/// Search is done by comparing names of the palettes.
		/// </summary>
		/// <param name="PaletteName">Name of the palette to find, without theater or .pal extension.</param>
		/// <param name="IsTheaterSpecific">Whether or not this palette is theater specific.</param>
		/// <returns>The correct custom palette.</returns>
		public Palette GetCustomPalette(string paletteName, bool isTheaterSpecific) {
			string paletteFileName = paletteName.ToLower() + (isTheaterSpecific ? TheaterDefaults.GetExtension(_theaterType) : ".pal");
			var pal = CustomPalettes.FirstOrDefault(p => p.Name == paletteFileName);

			if (pal == null) {
				// palette hasn't been loaded yet
				pal = new Palette(VFS.Open<PalFile>(paletteFileName), paletteName);
				CustomPalettes.Add(pal);
			}
			return pal;
		}

	}
}