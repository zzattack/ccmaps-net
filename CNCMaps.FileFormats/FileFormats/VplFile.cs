using System;
using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats.FileFormats {

	public class VplFile : VirtualFile {

		static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

		public VplFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = false)
			: base(baseStream, filename, baseOffset, fileSize, isBuffered) {
		}

		public VplFile(Stream baseStream, string filename = "", bool isBuffered = true)
			: base(baseStream, filename, isBuffered) {
		}

		private uint _firstRemap;
		private uint _lastRemap;
		private uint _numSections;
		private uint _unknown;
		// private Palette _palette; // unused
		private List<byte[]> _lookupSections = new List<byte[]>();

		private bool _parsed = false;
		private void Parse() {
			_firstRemap = ReadUInt32();
			_lastRemap = ReadUInt32();
			_numSections = ReadUInt32();
			_unknown = ReadUInt32();
			var pal = Read(768);
			// _palette = new Palette(pal, "voxels.vpl");
			for (uint i = 0; i < _numSections; i++)
				_lookupSections.Add(Read(256));
			_parsed = true;
		}

		public byte GetPaletteIndex(byte normal, byte maxNormal, byte color) {
			if (!_parsed) Parse();
			int vplSection = (int)(Math.Min(normal, maxNormal - 1) * _numSections / maxNormal);
			return _lookupSections[vplSection][color];
		}

	}
}