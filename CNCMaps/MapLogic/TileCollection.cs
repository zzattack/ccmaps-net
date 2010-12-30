using System;
using System.Collections.Generic;
using CNCMaps.FileFormats;
using CNCMaps.VirtualFileSystem;

namespace CNCMaps.MapLogic {

	class TileCollection {
		TheaterType theaterType;
		IniFile theaterIni;
		string tileExtension;

		List<int> TileNumToSet = new List<int>();
		List<int> SetNumToFirstTile = new List<int>();
		List<RandomizedTileSet> TileSets = new List<RandomizedTileSet>();

		class TileSet {

			public string FileName { get; private set; }

			public string SetName { get; private set; }

			public int TilesInSet { get; private set; }

			public TileSet(string fileName, string setName, int tilesInSet) {
				this.FileName = fileName;
				this.SetName = setName;
				this.TilesInSet = tilesInSet;
			}
		}

		class RandomizedTileSet {
			static Random randomTileChooser = new Random();
			public List<TmpFile> tmpFiles = new List<TmpFile>();

			internal void AddTile(TmpFile tmpFile) {
				tmpFiles.Add(tmpFile);
			}

			TmpFile GetTile() {
				return tmpFiles[randomTileChooser.Next(tmpFiles.Count)];
			}
		}

		int ACliffMMPieces; int ACliffPieces; int BlackTile;
		int BridgeBottomLeft1; int BridgeBottomLeft2; int BridgeBottomRight1;
		int BridgeBottomRight2; int BridgeMiddle1; int BridgeMiddle2;
		int BridgeSet; int BridgeTopLeft1; int BridgeTopLeft2;
		int BridgeTopRight1; int BridgeTopRight2; int ClearTile;
		int ClearToGreenLat; int ClearToPaveLat; int ClearToRoughLat;
		int ClearToSandLat; int CliffRamps; int CliffSet;
		int DestroyableCliffs; int DirtRoadCurve; int DirtRoadJunction;
		int DirtRoadSlopes; int DirtRoadStraight; int DirtTrackTunnels;
		int DirtTunnels; int GreenTile; int HeightBase;
		int Ice1Set; int Ice2Set; int Ice3Set;
		int IceShoreSet; int MMRampBase; int MMWaterCliffAPieces;
		int Medians; int MiscPaveTile; int MonorailSlopes;
		int PaveTile; int PavedRoadEnds; int PavedRoadSlopes;
		int PavedRoads; int RampBase; int RampSmooth;
		int Rocks; int RoughGround; int RoughTile;
		int SandTile; int ShorePieces; int SlopeSetPieces;
		int SlopeSetPieces2; int TrackTunnels; int TrainBridgeSet;
		int Tunnels; int WaterBridge; int WaterCaves;
		int WaterCliffAPieces; int WaterCliffs; int WaterSet;
		int WaterfallEast; int WaterfallNorth; int WaterfallSouth;
		int WaterfallWest; int WoodBridgeSet;

		public TileCollection(TheaterType theaterType) {
			this.theaterType = theaterType;
			this.tileExtension = TheaterDefaults.GetTileExtension(theaterType);
			this.theaterIni = VFS.Open(TheaterDefaults.GetTheaterIni(theaterType)) as IniFile;

			#region Set numbers

			IniFile.IniSection General = theaterIni.GetSection("General");
			ACliffMMPieces = General.ReadInt("ACliffMMPieces", -1);
			ACliffPieces = General.ReadInt("ACliffPieces", -1);
			BlackTile = General.ReadInt("BlackTile", -1);
			BridgeBottomLeft1 = General.ReadInt("BridgeBottomLeft1", -1);
			BridgeBottomLeft2 = General.ReadInt("BridgeBottomLeft2", -1);
			BridgeBottomRight1 = General.ReadInt("BridgeBottomRight1", -1);
			BridgeBottomRight2 = General.ReadInt("BridgeBottomRight2", -1);
			BridgeMiddle1 = General.ReadInt("BridgeMiddle1", -1);
			BridgeMiddle2 = General.ReadInt("BridgeMiddle2", -1);
			BridgeSet = General.ReadInt("BridgeSet", -1);
			BridgeTopLeft1 = General.ReadInt("BridgeTopLeft1", -1);
			BridgeTopLeft2 = General.ReadInt("BridgeTopLeft2", -1);
			BridgeTopRight1 = General.ReadInt("BridgeTopRight1", -1);
			BridgeTopRight2 = General.ReadInt("BridgeTopRight2", -1);
			ClearTile = General.ReadInt("ClearTile", -1);
			ClearToGreenLat = General.ReadInt("ClearToGreenLat", -1);
			ClearToPaveLat = General.ReadInt("ClearToPaveLat", -1);
			ClearToRoughLat = General.ReadInt("ClearToRoughLat", -1);
			ClearToSandLat = General.ReadInt("ClearToSandLat", -1);
			CliffRamps = General.ReadInt("CliffRamps", -1);
			CliffSet = General.ReadInt("CliffSet", -1);
			DestroyableCliffs = General.ReadInt("DestroyableCliffs", -1);
			DirtRoadCurve = General.ReadInt("DirtRoadCurve", -1);
			DirtRoadJunction = General.ReadInt("DirtRoadJunction", -1);
			DirtRoadSlopes = General.ReadInt("DirtRoadSlopes", -1);
			DirtRoadStraight = General.ReadInt("DirtRoadStraight", -1);
			DirtTrackTunnels = General.ReadInt("DirtTrackTunnels", -1);
			DirtTunnels = General.ReadInt("DirtTunnels", -1);
			GreenTile = General.ReadInt("GreenTile", -1);
			HeightBase = General.ReadInt("HeightBase", -1);
			Ice1Set = General.ReadInt("Ice1Set", -1);
			Ice2Set = General.ReadInt("Ice2Set", -1);
			Ice3Set = General.ReadInt("Ice3Set", -1);
			IceShoreSet = General.ReadInt("IceShoreSet", -1);
			MMRampBase = General.ReadInt("MMRampBase", -1);
			MMWaterCliffAPieces = General.ReadInt("MMWaterCliffAPieces", -1);
			Medians = General.ReadInt("Medians", -1);
			MiscPaveTile = General.ReadInt("MiscPaveTile", -1);
			MonorailSlopes = General.ReadInt("MonorailSlopes", -1);
			PaveTile = General.ReadInt("PaveTile", -1);
			PavedRoadEnds = General.ReadInt("PavedRoadEnds", -1);
			PavedRoadSlopes = General.ReadInt("PavedRoadSlopes", -1);
			PavedRoads = General.ReadInt("PavedRoads", -1);
			RampBase = General.ReadInt("RampBase", -1);
			RampSmooth = General.ReadInt("RampSmooth", -1);
			Rocks = General.ReadInt("Rocks", -1);
			RoughGround = General.ReadInt("RoughGround", -1);
			RoughTile = General.ReadInt("RoughTile", -1);
			SandTile = General.ReadInt("SandTile", -1);
			ShorePieces = General.ReadInt("ShorePieces", -1);
			SlopeSetPieces = General.ReadInt("SlopeSetPieces", -1);
			SlopeSetPieces2 = General.ReadInt("SlopeSetPieces2", -1);
			TrackTunnels = General.ReadInt("TrackTunnels", -1);
			TrainBridgeSet = General.ReadInt("TrainBridgeSet", -1);
			Tunnels = General.ReadInt("Tunnels", -1);
			WaterBridge = General.ReadInt("WaterBridge", -1);
			WaterCaves = General.ReadInt("WaterCaves", -1);
			WaterCliffAPieces = General.ReadInt("WaterCliffAPieces", -1);
			WaterCliffs = General.ReadInt("WaterCliffs", -1);
			WaterSet = General.ReadInt("WaterSet", -1);
			WaterfallEast = General.ReadInt("WaterfallEast", -1);
			WaterfallNorth = General.ReadInt("WaterfallNorth", -1);
			WaterfallSouth = General.ReadInt("WaterfallSouth", -1);
			WaterfallWest = General.ReadInt("WaterfallWest", -1);
			WoodBridgeSet = General.ReadInt("WoodBridgeSet", -1);

			#endregion

			int i = 0;
			// we initialize a theater-specific vfs containing only
			// the mixes containing the stuff we need to prevent
			// searching through all archives for every tile
			VFS tilesVFS = new VFS();
			foreach (string s in TheaterDefaults.GetTheaterMixes(theaterType))
				tilesVFS.AddMix(VFS.Open(s) as MixFile);

			int setNum = 0;
			while (true) {
				string sectionName = "TileSet" + i++.ToString("0000");
				var sect = theaterIni.GetSection(sectionName);

				if (sect == null)
					break;

				TileSet ts = new TileSet(sect.ReadString("FileName"), sect.ReadString("SetName"), sect.ReadInt("TilesInSet"));
				SetNumToFirstTile.Add(TileSets.Count);

				for (int j = 1; j <= ts.TilesInSet; j++) {
					TileNumToSet.Add(setNum);
					RandomizedTileSet rs = new RandomizedTileSet();

					for (char r = (char)('a' - 1); r <= 'z'; r++) {
						if ((r >= 'a') && ts.SetName == "Bridges") continue;

						// filename = set filename + dd + .tmp/.urb/.des etc
						string filename = ts.FileName + i.ToString("00");
						if (r >= 'a') filename += r;
						filename += tileExtension;
						var tmpFile = tilesVFS.OpenFile(filename, FileFormat.Tmp) as TmpFile;
						if (tmpFile != null) rs.AddTile(tmpFile);
						else break;
					}

					TileSets.Add(rs);
				}

				setNum++;
			}
		}

		public bool ConnectTiles(int tileNum1, int tileNum2) {
			int setNum1 = GetSetNum(tileNum1);
			int setNum2 = GetSetNum(tileNum2);

			if (setNum1 == setNum2) return false;

			// grass doesn't connect with shores
			else if (setNum1 == GreenTile && setNum2 == ShorePieces ||
				(setNum2 == GreenTile && setNum1 == ShorePieces)) return false;

			// grass doesn't connect with waterbridges
			else if (setNum1 == GreenTile && setNum2 == WaterBridge ||
				(setNum2 == GreenTile && setNum1 == WaterBridge)) return false;

			// pave's don't connect with paved roads
			else if (setNum1 == PaveTile && setNum2 == PavedRoads ||
				(setNum2 == PaveTile && setNum1 == PavedRoads)) return false;

			// pave's don't connect with medians
			else if (setNum1 == PaveTile && setNum2 == Medians ||
				(setNum2 == PaveTile && setNum1 == Medians)) return false;

			// all other transitions are connected with a CLAT set
			return true;
		}

		public bool IsLAT(int tileNum) {
			int setNum = GetSetNum(tileNum);
			return
				setNum == RoughTile ||
				setNum == SandTile ||
				setNum == GreenTile ||
				setNum == PaveTile;
		}

		public bool IsCLAT(int tileNum) {
			int setNum = GetSetNum(tileNum);
			return
				setNum == ClearToRoughLat ||
				setNum == ClearToSandLat ||
				setNum == ClearToGreenLat ||
				setNum == ClearToPaveLat;
		}

		public int GetLAT(int tileNum) {
			int setNum = GetSetNum(tileNum);
			if (setNum == ClearToRoughLat)
				return RoughTile;
			else if (setNum == ClearToSandLat)
				return SandTile;
			else if (setNum == ClearToGreenLat)
				return GreenTile;
			else if (setNum == ClearToPaveLat)
				return PaveTile;
			else
				return -1;
		}

		public int GetCLATSet(int tileNum) {
			int setNum = GetSetNum(tileNum);
			if (setNum == RoughTile)
				return ClearToRoughLat;
			else if (setNum == SandTile)
				return ClearToSandLat;
			else if (setNum == GreenTile)
				return ClearToGreenLat;
			else if (setNum == PaveTile)
				return ClearToPaveLat;
			else
				return -1;
		}

		public int GetSetNum(int tileNum) {
			if (tileNum < 0) return 0;
			return TileNumToSet[tileNum];
		}

		public int GetTileNumFromSet(int setNum, byte tileNumWithinSet = 0) {
			return SetNumToFirstTile[setNum] + tileNumWithinSet;
		}

		/// <summary>Recalculates tile system. </summary>
		public void RecalculateTileSystem(MapTile[,] tiles) {
			for (int x = 0; x < tiles.GetLength(0); x++) {
				for (int y = 0; y < tiles.GetLength(1); y++) {
					MapTile t = tiles[x, y];
					// If this tile comes from a CLAT (connecting lat) set,
					// then replace it's set and tilenr by corresponding LAT sets'
					if (IsCLAT(t.TileNum)) {
						int latSetNum = GetLAT(t.TileNum);
						t.TileNum = (short)GetTileNumFromSet(latSetNum);
					}
				}
			}

			// Recalculate LAT system (tile connecting)
			for (int x = 0; x < tiles.GetLength(0); x++) {
				for (int y = 0; y < tiles.GetLength(1); y++) {
					MapTile t = tiles[x, y];
					// If this tile is a LAT tile, we might have to connect it
					if (IsLAT(t.TileNum)) {
						// Find Tileset that contains the connecting pieces
						int clatSet = GetCLATSet(t.TileNum);
						// Which tile to use from that tileset
						byte transitionTile = 0;

						// Find out setnums of adjacent cells
						int top_right = t.TileNum, top_left = t.TileNum, bottom_right = t.TileNum, bottom_left = t.TileNum;

						if (t.Dx < tiles.GetLength(0) - 1)
							top_right = tiles[x + 1, y].TileNum;
						if (x > 1)
							top_left = tiles[x - 1, y].TileNum;
						if (y < tiles.GetLength(1) - 1)
							bottom_right = tiles[x, y + 1].TileNum;
						if (y > 1)
							bottom_left = tiles[x, y - 1].TileNum;

						if (ConnectTiles(t.TileNum, top_right))
							transitionTile += 1;

						if (ConnectTiles(t.TileNum, bottom_right))
							transitionTile += 2;

						if (ConnectTiles(t.TileNum, bottom_left))
							transitionTile += 4;

						if (ConnectTiles(t.TileNum, top_left))
							transitionTile += 8;

						if (transitionTile > 0) {
							// Do not change this setnum, as then we could recognize it as
							// a different tileset for later tiles around this one.
							// (T->SetNum = clatSet;)
							t.TileNum = (short)GetTileNumFromSet(clatSet, transitionTile);
						}
					}
				}
			}
		}
	}
}