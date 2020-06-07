using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CNCMaps.Engine.Rendering;
using CNCMaps.FileFormats;
using CNCMaps.FileFormats.VirtualFileSystem;
using CNCMaps.Shared;

namespace CNCMaps.Engine.Game {
	public class PaletteCollection : IEnumerable<Palette> {
		public List<Palette> CustomPalettes = new List<Palette>();
		public Palette IsoPalette, OvlPalette, UnitPalette, AnimPalette;
		private readonly VFS _vfs;

		public PaletteCollection(VFS vfs) {
			_vfs = vfs;
		}

		internal Palette GetPalette(PaletteType paletteType) {
			switch (paletteType) {
				case PaletteType.Anim: return AnimPalette;
				case PaletteType.Overlay: return OvlPalette;
				case PaletteType.Unit: return UnitPalette;
				case PaletteType.Custom:
					throw new ArgumentException("GetPalette only works on built-in default palettes");
				case PaletteType.Iso:
				default:
					return IsoPalette;
			}
		}

		public IEnumerator<Palette> GetEnumerator() {
			var p = new List<Palette> {
				IsoPalette, 
				OvlPalette, 
				UnitPalette, 
				AnimPalette
			};
			p.AddRange(CustomPalettes);
			return p.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		
		/// <summary>
		/// Gets a custom palette from collection. If custom palette is not found, creates one, adds it to the collection and returns it.
		/// Search is done by comparing names of the palettes.
		/// </summary>
		/// <param name="paletteName">Name of the palette to find, without theater or .pal extension.</param>
		/// <returns>The correct custom palette.</returns>
		public Palette GetCustomPalette(string paletteName) {
			string fileName;
            // Starkku: Necessary to distinguish between object and theater/animation palettes when recalculating values.
            bool objectPalette = false;
            if (paletteName.ToLower().EndsWith(".pal")) // full name already given
                fileName = paletteName;
            else
            { 
                // filename = <paletteName><theaterExtension>.pal (e.g. lib<tem/sno/urb>.pal)
                fileName = paletteName + ModConfig.ActiveTheater.Extension.Substring(1) + ".pal";
                objectPalette = true;
            }

			var pal = CustomPalettes.FirstOrDefault(p => p.Name == paletteName);
			if (pal == null) {
				// palette hasn't been loaded yet
                // Starkku: If the original does not exist, it means the file it should use does not exist. It now returns a null in this case, which is
                // handled appropriately wherever this method is called to fall back to the default palette for that type of object.
                PalFile orig = _vfs.Open<PalFile>(fileName);
                if (orig == null) return null;
                pal = new Palette(_vfs.Open<PalFile>(fileName), paletteName, objectPalette);
				CustomPalettes.Add(pal);
			}
			return pal;
		}

	}
}