using CNCMaps.Shared;
using Xunit;

namespace CNCMaps.Tests {
	public class RenderSettingsTests {
		private static RenderSettings Parse(params string[] args) {
			var rs = new RenderSettings();
			rs.ConfigureFromArgs(args);
			return rs;
		}

		[Fact]
		public void ParsesTypicalGuiGeneratedCommandLine() {
			var rs = Parse("-i", @"C:\maps\test.map", "-o", "render", "-d", @"C:\out", "-p", "-c", "7",
				"-j", "-q", "80", "-Y", "-m", @"C:\mix", "--mark-start-pos", "-S", "--start-pos-size", "5.5");
			Assert.Equal(@"C:\maps\test.map", rs.InputFile);
			Assert.Equal("render", rs.OutputFile);
			Assert.Equal(@"C:\out", rs.OutputDir);
			Assert.True(rs.SavePNG);
			Assert.Equal(7, rs.PNGQuality);
			Assert.True(rs.SaveJPEG);
			Assert.Equal(80, rs.JPEGCompression);
			Assert.Equal(EngineType.YurisRevenge, rs.Engine);
			Assert.Equal(@"C:\mix", rs.MixFilesDirectory);
			Assert.True(rs.MarkStartPos);
			Assert.Equal(StartPositionMarking.Squared, rs.StartPositionMarking);
			Assert.Equal(5.5, rs.MarkerStartSize);
		}

		[Fact]
		public void DefaultsSurviveEmptyArgs() {
			var rs = Parse();
			Assert.Equal(4, rs.PNGQuality);
			Assert.Equal(95, rs.JPEGCompression);
			Assert.Equal(EngineType.AutoDetect, rs.Engine);
			Assert.Equal(SizeMode.Auto, rs.SizeMode);
			Assert.True(rs.Backup);
			Assert.False(rs.SavePNG);
			Assert.Null(rs.MarkerStartSize);
		}

		[Fact]
		public void LongAndShortFormsAreEquivalent() {
			var byShort = Parse("-i", "a.map", "-p", "-F", "-g", "-n");
			var byLong = Parse("--infile", "a.map", "--output-png", "--force-fullmap", "--icegrowth", "--ignore-lighting");
			Assert.Equal(byShort.InputFile, byLong.InputFile);
			Assert.Equal(byShort.SavePNG, byLong.SavePNG);
			Assert.Equal(byShort.SizeMode, byLong.SizeMode);
			Assert.Equal(byShort.MarkIceGrowth, byLong.MarkIceGrowth);
			Assert.Equal(byShort.IgnoreLighting, byLong.IgnoreLighting);
		}

		[Fact]
		public void EqualsSignValueFormWorks() {
			var rs = Parse("--infile=b.map", "-c=9", "--jpeg-quality=50");
			Assert.Equal("b.map", rs.InputFile);
			Assert.Equal(9, rs.PNGQuality);
			Assert.Equal(50, rs.JPEGCompression);
		}

		[Fact]
		public void PreviewMarkerOptionsSetPackAndType() {
			var rs = Parse("-K");
			Assert.True(rs.GeneratePreviewPack);
			Assert.Equal(PreviewMarkersType.SelectedAsAbove, rs.PreviewMarkers);

			rs = Parse("-k");
			Assert.True(rs.GeneratePreviewPack);
			Assert.Equal(PreviewMarkersType.None, rs.PreviewMarkers);
		}

		[Fact]
		public void HelpFlagOnlySetsShowHelp() {
			var rs = Parse("-h");
			Assert.True(rs.ShowHelp);
			Assert.Contains("--infile", rs.GetHelpText());
		}

		[Fact]
		public void UnknownOptionsAreIgnoredWithoutAffectingOthers() {
			var rs = Parse("--does-not-exist", "-p", "-i", "c.map");
			Assert.True(rs.SavePNG);
			Assert.Equal("c.map", rs.InputFile);
		}

		[Fact]
		public void CaseSensitiveShortOptionsAreDistinct() {
			Assert.Equal(EngineType.RedAlert2, Parse("-y").Engine);
			Assert.Equal(EngineType.YurisRevenge, Parse("-Y").Engine);
			Assert.Equal(EngineType.TiberianSun, Parse("-t").Engine);
			Assert.Equal(EngineType.Firestorm, Parse("-T").Engine);
			Assert.Equal(SizeMode.Full, Parse("-F").SizeMode);
			Assert.Equal(SizeMode.Local, Parse("-f").SizeMode);
		}

		[Fact]
		public void NoPreviewFixupDisablesDimensionFix() {
			Assert.True(Parse().FixPreviewDimensions);
			Assert.False(Parse("-x").FixPreviewDimensions);
			Assert.False(Parse("--no-preview-fixup").FixPreviewDimensions);
		}

		[Fact]
		public void TunnelOptionsMapToTheirOwnProperties() {
			var rs = Parse("--tunnels", "--tunnelpos");
			Assert.True(rs.TunnelPaths);
			Assert.True(rs.TunnelPosition);

			rs = Parse("--tunnels");
			Assert.True(rs.TunnelPaths);
			Assert.False(rs.TunnelPosition);
		}

		[Fact]
		public void ThumbnailConfigAcceptsAspectPrefix() {
			var rs = Parse("-z", "+(200,100)");
			Assert.Equal("+(200,100)", rs.ThumbnailConfig);
		}
	}
}
