namespace CNCMaps {
	internal struct RenderSettings {

		public string InputFile { get; set; }

		public string OutputFile { get; set; }

		public string OutputDir { get; set; }

		public bool SavePNG { get; set; }

		public bool SaveJPEG { get; set; }

		public int PNGQuality { get; set; }

		public int JPEGCompression { get; set; }

		public string MixFilesDirectory { get; set; }

		public bool ShowHelp { get; set; }

		public bool MarkOreFields { get; set; }

		public bool IgnoreLocalSize { get; set; }

		public EngineType Engine { get; set; }

		public StartPositionMarking StartPositionMarking;

		public bool GeneratePreviewPack { get; set; }
		public bool OmitPreviewPackMarkers { get; set; }

		public bool PreferOSMesa { get; set; }

		internal static RenderSettings CreateDefaults() {
			return new RenderSettings {
				PNGQuality = 6,
				SavePNG = false,
				JPEGCompression = 95,
				SaveJPEG = false,
				ShowHelp = false,
				MarkOreFields = false,
				Engine = EngineType.AutoDetect,
				StartPositionMarking = StartPositionMarking.None,
				InputFile = "",
				OutputDir = "",
				OutputFile = "",
				MixFilesDirectory = ""
			};
		}
	}

}