using System;
using System.Drawing;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using CNCMaps.Shared;
using NLog;
using System.Numerics;

namespace CNCMaps.Engine.Rendering {
	/// <summary>
	/// Renders voxel models to an offscreen surface the way gamemd's voxel library does: every
	/// voxel projects to a single pixel, stepped in 8.8 fixed point from the far corner of the
	/// section's bounding box and drawn in painter's order. No GPU or OpenGL driver is needed and
	/// output is identical on every machine.
	/// </summary>
	public class VxlRenderer : IDisposable {
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		bool _isInit;

		// color contributors for the fallback lighting used when no voxels.vpl is available;
		// the standard voxels.vpl already adds a lot of ambient, that's why these seem high
		private static readonly Vector3 Diffuse = new Vector3(1.3f);
		private static readonly Vector3 Ambient = new Vector3(0.8f);

		// game light directions for the vpl page selection (from WorldAlteringEditor)
		private static readonly Vector3 TSLight = -Vector3.UnitX;
		private static readonly Vector3 YRLight = MatrixMath.TransformNormal(-Vector3.UnitX, Matrix4x4.CreateRotationZ(float.DegreesToRadians(45f)));

		VplFile _vpl;
		EngineType _engine = EngineType.YurisRevenge;

		/// <summary>Sets the voxels.vpl lookup used for game-accurate lighting; without
		/// it a Lambert approximation is used.</summary>
		public void Configure(VplFile vpl, EngineType engine) {
			_vpl = vpl;
			_engine = engine;
		}

		DrawingSurface _surface;

		/// <summary>Canvas row (top-down, as the blit reads it) of the lowest point the rendered
		/// model's volume projects to. gamemd keeps a cached voxel in the region its volume occupies
		/// and anchors the standing z gradient at that region's bottom row, which for a turret whose
		/// box reaches under its visible geometry lies below the last drawn pixel.</summary>
		public int VolumeBottomRow { get; private set; }

		public void Initialize() {
			Logger.Info("Initializing voxel renderer");
			_isInit = true;
			_surface = new DrawingSurface(400, 400, SurfaceFormat.Bgra32);
		}

		public void Dispose() {
			_surface?.Dispose();
		}

		/// <summary>Screen shift of a voxel shadow, gamemd Set_Voxel_Light_Angle: -6 * light.X with the
		/// light (-sqrt(1/2), -sqrt(1/2), 0) turned 45 degrees about Y.</summary>
		const float ShadowShiftX = 3f;

		// bounding box corners in the order gamemd's Prep_For_Object scans them; bit 0 = max x,
		// bit 1 = max y, bit 2 = max z
		static readonly int[] CornerScanOrder = { 3, 1, 0, 2, 7, 5, 4, 6 };

		public DrawingSurface Render(VxlFile vxl, HvaFile hva, GameObject obj, DrawProperties props, int shadowSection = 0) {
			if (!_isInit) Initialize();

			Logger.Debug("Rendering voxel {0}", vxl.FileName);
			vxl.Initialize();
			hva.Initialize();

			Clear();

			// RA2 projects orthographically with the camera elevated 30 degrees off the ground
			// (gamemd IsometricViewMatrix = RotX(-60) * RotZ(-45), no scaling): one voxel unit
			// is one screen pixel.
			var persp = MatrixMath.CreateOrthographicGL(_surface.Width, _surface.Height, 1, _surface.Height);

			var lookat = Matrix4x4.CreateLookAt(new Vector3(0, 0, -10), Vector3.Zero, Vector3.UnitY);
			var trans = Matrix4x4.CreateTranslation(0, 0, 10);

			var world = Matrix4x4.CreateRotationX(float.DegreesToRadians(60));
			world = MatrixMath.Mul(Matrix4x4.CreateRotationY(float.DegreesToRadians(180)), world);
			world = MatrixMath.Mul(Matrix4x4.CreateRotationZ(float.DegreesToRadians(-45)), world);

			// determine tilt vectors
			Matrix4x4 tilt = Matrix4x4.Identity;
			int tiltPitch = 0, tiltYaw = 0;
			if (obj.Tile.Drawable != null) {
				var img = (obj.Tile.Drawable as TileDrawable).GetTileImage(obj.Tile);
				int ramp = img?.RampType ?? 0;
				if (ramp == 0 || ramp >= 17) {
					tiltPitch = tiltYaw = 0;
				}
				else if (ramp <= 4) {
					// screen-diagonal facings (perpendicular to axes)
					tiltPitch = 25;
					tiltYaw = -90 * ramp;
				}
				else {
					// world-diagonal facings (perpendicular to screen)
					tiltPitch = 25;
					tiltYaw = 225 - 90 * ((ramp - 1) % 4);
				}
				tilt = MatrixMath.Mul(tilt, Matrix4x4.CreateRotationX(float.DegreesToRadians(tiltPitch)));
				tilt = MatrixMath.Mul(tilt, Matrix4x4.CreateRotationZ(float.DegreesToRadians(tiltYaw)));
			}

			// object rotation around Z
			float direction = (obj is OwnableObject) ? (obj as OwnableObject).Direction : 0;
			float objectRotation = 90 - direction / 256f * 360f - tiltYaw; // convert game rotation to world degrees
			Matrix4x4 @object = MatrixMath.Mul(Matrix4x4.CreateRotationZ(float.DegreesToRadians(objectRotation)), tilt); // object facing
																										   // art.ini TurretOffset value positions some voxel parts over our x-axis
			@object = MatrixMath.Mul(Matrix4x4.CreateTranslation(0.18f * props.TurretVoxelOffset, 0, 0), @object);

			float pitch = float.DegreesToRadians(210);
			float yaw = float.DegreesToRadians(120);
			var shadowTransform = MatrixMath.Mul(Matrix4x4.CreateRotationZ(pitch), Matrix4x4.CreateRotationY(yaw));
			float volumeMinY = float.MaxValue;

			// project every section's bounding box first: gamemd centres the whole model in its
			// draw buffer on the union of the boxes, and that centre decides the pixel rounding
			int sectionCount = vxl.Sections.Count;
			var lightDirs = new Vector3[sectionCount];
			var corners = new Vector3[sectionCount][];
			float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
			for (int s = 0; s < sectionCount; s++) {
				var section = vxl.Sections[s];
				var frameRot = hva.LoadGLMatrix(section.Index);
				frameRot.M41 *= section.HVAMultiplier * section.ScaleX;
				frameRot.M42 *= section.HVAMultiplier * section.ScaleY;
				frameRot.M43 *= section.HVAMultiplier * section.ScaleZ;

				var frameTransl = Matrix4x4.CreateTranslation(section.MinBounds);
				var frame = MatrixMath.Mul(frameTransl, frameRot);

				// full modelview-projection for this section, mirroring the former GL
				// matrix stack (row-vector convention: leftmost matrix applies first)
				var mvp = MatrixMath.Mul(frame, @object, world, trans, lookat, persp);
				for (int i = 0; i < 8; i++) {
					var corner = new Vector4(
						(((i & 1) != 0) ? section.SizeX - 0.5f : -0.5f) * section.Scale.X,
						(((i & 2) != 0) ? section.SizeY - 0.5f : -0.5f) * section.Scale.Y,
						(((i & 4) != 0) ? section.SizeZ - 0.5f : -0.5f) * section.Scale.Z, 1f);
					var clip = MatrixMath.TransformRow(corner, mvp);
					if (clip.W > 1e-6f)
						volumeMinY = MathF.Min(volumeMinY, (clip.Y / clip.W + 1f) * _surface.Height / 2f);
				}

				corners[s] = new Vector3[8];
				for (int i = 0; i < 8; i++) {
					var corner = new Vector4(((i & 1) != 0) ? section.SpanX : 0f, ((i & 2) != 0) ? section.SpanY : 0f, ((i & 4) != 0) ? section.SpanZ : 0f, 1f);
					var p = Project(corner, mvp);
					corners[s][i] = p;
					minX = MathF.Min(minX, p.X);
					maxX = MathF.Max(maxX, p.X);
					minY = MathF.Min(minY, p.Y);
					maxY = MathF.Max(maxY, p.Y);
				}

				// undo world transformations on light direction
				var v = MatrixMath.Mul(@object, world, frame, shadowTransform);
				lightDirs[s] = Matrix4x4.Invert(v, out var vInv) ? ExtractRotationVector(ToOpenGL(vInv)) : Vector3.Zero;
			}
			float centerX = (minX + maxX) * 0.5f, centerY = (minY + maxY) * 0.5f;

			for (int s = 0; s < sectionCount; s++)
				DrawSection(vxl.Sections[s], corners[s], centerX, centerY, obj, direction, lightDirs[s]);

			if (sectionCount > 0) {
				int shadow = Math.Min(shadowSection, sectionCount - 1);
				DrawShadow(vxl.Sections[shadow], corners[shadow]);
			}

			VolumeBottomRow = _surface.Height - 1 - Math.Clamp((int)MathF.Floor(volumeMinY), 0, _surface.Height - 1);
			return _surface;
		}

		/// <summary>Window position of a model point relative to the draw point: x right, y down,
		/// z growing away from the viewer.</summary>
		Vector3 Project(in Vector4 p, in Matrix4x4 mvp) {
			var clip = MatrixMath.TransformRow(p, mvp);
			float invW = 1f / clip.W;
			return new Vector3(clip.X * invW * _surface.Width / 2f, -clip.Y * invW * _surface.Height / 2f, clip.Z * invW);
		}

		/// <summary>
		/// Walks the section's voxels like gamemd's Draw_Voxel_Regular_Lighting_Normals_ASM: every
		/// voxel is one pixel, placed by stepping 8.8 fixed-point screen deltas from the projected
		/// bounding box corner farthest from the viewer, and later voxels overwrite earlier ones
		/// (painter's order, no depth test). The 2 px wide z-buffered drawer that UseBuffer=yes selects
		/// (SHAD, HIND, SCHP, SCHD) is not modelled.
		/// </summary>
		void DrawSection(VxlFile.Section section, Vector3[] corners, float centerX, float centerY, GameObject obj, float direction, Vector3 lightDirection) {
			int anchor = CornerScanOrder[0];
			foreach (int c in CornerScanOrder)
				if (corners[c].Z > corners[anchor].Z)
					anchor = c;
			bool maxX = (anchor & 1) != 0, maxY = (anchor & 2) != 0, maxZ = (anchor & 4) != 0;
			Vector3 c0 = corners[anchor], cx = corners[anchor ^ 1], cy = corners[anchor ^ 2], cz = corners[anchor ^ 4];

			// VoxelLibrary::Render_Object: the anchor lands at buffer (128,128) minus the model
			// centre, deltas are truncated to 1/256 px per voxel; positions live in unsigned
			// 16-bit 8.8 registers, hence the masks
			int baseX = (int)((float)((double)c0.X + 128 - (double)centerX) * 256) & 0xFFFF;
			int baseY = (int)((float)((double)c0.Y + 128 - (double)centerY) * 256) & 0xFFFF;
			int stepXx = (short)((double)(cx.X - c0.X) / section.SizeX * 256), stepXy = (short)((double)(cx.Y - c0.Y) / section.SizeX * 256);
			int stepYx = (short)((double)(cy.X - c0.X) / section.SizeY * 256), stepYy = (short)((double)(cy.Y - c0.Y) / section.SizeY * 256);
			int stepZx = (short)((double)(cz.X - c0.X) / section.SizeZ * 256), stepZy = (short)((double)(cz.Y - c0.Y) / section.SizeZ * 256);
			int originX = _surface.Width / 2 - 128 + (int)centerX, originY = _surface.Height / 2 - 128 + (int)centerY;

			// game-accurate lighting: precompute which vpl page every normal maps to
			byte[] vplPages = _vpl != null ? PreCalculateVplLighting(section.GetNormals(), direction) : null;

			for (int wy = 0; wy < section.SizeY; wy++) {
				int dy = maxY ? section.SizeY - 1 - wy : wy;
				for (int wx = 0; wx < section.SizeX; wx++) {
					int dx = maxX ? section.SizeX - 1 - wx : wx;
					var voxels = section.Spans[dx, dy].Voxels;
					if (voxels.Count == 0) continue;
					int colX = baseX + wx * stepXx + wy * stepYx;
					int colY = baseY + wx * stepXy + wy * stepYy;
					for (int i = 0; i < voxels.Count; i++) {
						var vx = voxels[maxZ ? voxels.Count - 1 - i : i];
						if (vx.ColorIndex == 0) continue;
						int wz = maxZ ? section.SizeZ - 1 - vx.Z : vx.Z;
						int px = (((colX + wz * stepZx) & 0xFFFF) >> 8) + originX;
						int py = (((colY + wz * stepZy) & 0xFFFF) >> 8) + originY;
						if ((uint)px >= (uint)_surface.Width || (uint)py >= (uint)_surface.Height) continue;

						byte cr, cg, cb;
						if (vplPages != null) {
							// like the game: remap the palette index through voxels.vpl
							// for the lighting page this voxel's normal maps to
							byte remapped = _vpl.GetPaletteIndex(vplPages[vx.NormalIndex], vx.ColorIndex);
							Color color = obj.Palette.Colors[remapped];
							cr = color.R;
							cg = color.G;
							cb = color.B;
						}
						else {
							Color color = obj.Palette.Colors[vx.ColorIndex];
							Vector3 normal = section.GetNormal(vx.NormalIndex);
							// shader function taken from https://github.com/OpenRA/OpenRA/blob/bleed/cg/vxl.fx
							// thanks to pchote for a LOT of help getting it right
							Vector3 colorMult = Vector3.Add(Ambient, Diffuse * Math.Max(Vector3.Dot(normal, lightDirection), 0f));
							cr = (byte)Math.Min(255, color.R * colorMult.X);
							cg = (byte)Math.Min(255, color.G * colorMult.Y);
							cb = (byte)Math.Min(255, color.B * colorMult.Z);
						}
						SetPixel(px, py, cr, cg, cb);
					}
				}
			}
		}

		/// <summary>
		/// VoxelLibrary::Render_Shadow: every column holding a voxel stamps a 2 px wide dot on the
		/// section's bottom bounding-box face, which the same view projects to the screen; the
		/// silhouette is shifted by the light vector and centred on that face's own box.
		/// </summary>
		void DrawShadow(VxlFile.Section section, Vector3[] corners) {
			Vector3 c0 = corners[0], cx = corners[1], cy = corners[2];
			float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
			for (int i = 0; i < 4; i++) {
				minX = MathF.Min(minX, corners[i].X + ShadowShiftX);
				maxX = MathF.Max(maxX, corners[i].X + ShadowShiftX);
				minY = MathF.Min(minY, corners[i].Y);
				maxY = MathF.Max(maxY, corners[i].Y);
			}
			float centerX = (minX + maxX) * 0.5f, centerY = (minY + maxY) * 0.5f;
			int baseX = (int)((c0.X + ShadowShiftX + 128 - centerX) * 256) & 0xFFFF;
			int baseY = (int)((c0.Y + 128 - centerY) * 256) & 0xFFFF;
			int stepXx = (short)((cx.X - c0.X) / section.SizeX * 256), stepXy = (short)((cx.Y - c0.Y) / section.SizeX * 256);
			int stepYx = (short)((cy.X - c0.X) / section.SizeY * 256), stepYy = (short)((cy.Y - c0.Y) / section.SizeY * 256);
			int originX = _surface.Width / 2 - 128 + (int)centerX, originY = _surface.Height / 2 - 128 + (int)centerY;

			var shadBuf = _surface.GetShadows();
			for (int y = 0; y < section.SizeY; y++) {
				for (int x = 0; x < section.SizeX; x++) {
					if (section.Spans[x, y].Voxels.Count == 0) continue;
					int px = (((baseX + x * stepXx + y * stepYx) & 0xFFFF) >> 8) + originX;
					int py = (((baseY + x * stepXy + y * stepYy) & 0xFFFF) >> 8) + originY;
					if ((uint)py >= (uint)_surface.Height) continue;
					if ((uint)px < (uint)_surface.Width) shadBuf[py * _surface.Width + px] = true;
					if ((uint)(px + 1) < (uint)_surface.Width) shadBuf[py * _surface.Width + px + 1] = true;
				}
			}
		}

		unsafe void SetPixel(int x, int y, byte r, byte g, byte b) {
			byte* pix = (byte*)_surface.BitmapData.Scan0 + y * _surface.BitmapData.Stride + x * 4;
			pix[0] = b;
			pix[1] = g;
			pix[2] = r;
			pix[3] = 255;
		}

		public static Rectangle GetBounds(GameObject obj, VxlFile vxl, HvaFile hva, DrawProperties props) {
			vxl.Initialize();
			hva.Initialize();

			float direction = (obj is OwnableObject) ? (obj as OwnableObject).Direction : 0;
			float objectRotation = 45f - direction / 256f * 360f; // convert game rotation to world degrees

			var world = Matrix4x4.CreateRotationX(float.DegreesToRadians(60));
			world = MatrixMath.Mul(Matrix4x4.CreateRotationZ(float.DegreesToRadians(objectRotation)), world); // object facing
			world = MatrixMath.Mul(Matrix4x4.CreateScale(0.25f, 0.25f, 0.25f), world);

			// art.ini TurretOffset value positions some voxel parts over our x-axis
			world = MatrixMath.Mul(Matrix4x4.CreateTranslation(0.18f * props.TurretVoxelOffset, 0, 0), world);
			var camera = MatrixMath.CreatePerspectiveFieldOfViewGL(float.DegreesToRadians(30), 1f, 1, 100);
			world = MatrixMath.Mul(world, camera);

			Rectangle ret = Rectangle.Empty;
			foreach (var section in vxl.Sections) {
				var frameRot = hva.LoadGLMatrix(section.Index);
				frameRot.M41 *= section.HVAMultiplier * section.ScaleX;
				frameRot.M42 *= section.HVAMultiplier * section.ScaleY;
				frameRot.M43 *= section.HVAMultiplier * section.ScaleZ;

				var minbounds = section.MinBounds;
				if (props.HasShadow)
					minbounds.Z = -100;

				var frameTransl = Matrix4x4.CreateTranslation(minbounds);
				var frame = MatrixMath.Mul(frameTransl, frameRot, world);

				// floor rect of the bounding box
				Vector3 floorTopLeft = new Vector3(0, 0, 0);
				Vector3 floorTopRight = new Vector3(section.SpanX, 0, 0);
				Vector3 floorBottomRight = new Vector3(section.SpanX, section.SpanY, 0);
				Vector3 floorBottomLeft = new Vector3(0, section.SpanY, 0);

				// ceil rect of the bounding box
				Vector3 ceilTopLeft = new Vector3(0, 0, section.SpanZ);
				Vector3 ceilTopRight = new Vector3(section.SpanX, 0, section.SpanZ);
				Vector3 ceilBottomRight = new Vector3(section.SpanX, section.SpanY, section.SpanZ);
				Vector3 ceilBottomLeft = new Vector3(0, section.SpanY, section.SpanZ);

				// apply transformations
				floorTopLeft = MatrixMath.TransformNormal(floorTopLeft, frame);
				floorTopRight = MatrixMath.TransformNormal(floorTopRight, frame);
				floorBottomRight = MatrixMath.TransformNormal(floorBottomRight, frame);
				floorBottomLeft = MatrixMath.TransformNormal(floorBottomLeft, frame);

				ceilTopLeft = MatrixMath.TransformNormal(ceilTopLeft, frame);
				ceilTopRight = MatrixMath.TransformNormal(ceilTopRight, frame);
				ceilBottomRight = MatrixMath.TransformNormal(ceilBottomRight, frame);
				ceilBottomLeft = MatrixMath.TransformNormal(ceilBottomLeft, frame);

				int FminX = (int)Math.Floor(Math.Min(Math.Min(Math.Min(floorTopLeft.X, floorTopRight.X), floorBottomRight.X), floorBottomLeft.X));
				int FmaxX = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(floorTopLeft.X, floorTopRight.X), floorBottomRight.X), floorBottomLeft.X));
				int FminY = (int)Math.Floor(Math.Min(Math.Min(Math.Min(floorTopLeft.Y, floorTopRight.Y), floorBottomRight.Y), floorBottomLeft.Y));
				int FmaxY = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(floorTopLeft.Y, floorTopRight.Y), floorBottomRight.Y), floorBottomLeft.Y));

				int TminX = (int)Math.Floor(Math.Min(Math.Min(Math.Min(ceilTopLeft.X, ceilTopRight.X), ceilBottomRight.X), ceilBottomLeft.X));
				int TmaxX = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(ceilTopLeft.X, ceilTopRight.X), ceilBottomRight.X), ceilBottomLeft.X));
				int TminY = (int)Math.Floor(Math.Min(Math.Min(Math.Min(ceilTopLeft.Y, ceilTopRight.Y), ceilBottomRight.Y), ceilBottomLeft.Y));
				int TmaxY = (int)Math.Ceiling(Math.Max(Math.Max(Math.Max(ceilTopLeft.Y, ceilTopRight.Y), ceilBottomRight.Y), ceilBottomLeft.Y));

				int minX = Math.Min(FminX, TminX);
				int maxX = Math.Max(FmaxX, TmaxX);
				int minY = Math.Min(FminY, TminY);
				int maxY = Math.Max(FmaxY, TmaxY);

				ret = Rectangle.Union(ret, Rectangle.FromLTRB(minX, minY, maxX, maxY));
			}

			return ret;
		}

		/// <summary>
		/// Maps every voxel normal to the voxels.vpl lighting page the game would use,
		/// for a given object facing. Blinn-Phong reflection model as reverse-engineered
		/// by the WorldAlteringEditor project.
		/// </summary>
		byte[] PreCalculateVplLighting(Vector3[] normalsTable, float direction) {
			float rotationFromFacing = MathF.Tau * direction / 256f;
			Vector3 baseLight = _engine >= EngineType.RedAlert2 ? YRLight : TSLight;
			Vector3 light = MatrixMath.TransformNormal(baseLight, Matrix4x4.CreateRotationZ(rotationFromFacing - float.DegreesToRadians(45f)));

			// halfway vector between light direction and view direction (Blinn-Phong)
			Vector3 viewer = Vector3.UnitZ;
			Vector3 halfway = Vector3.Normalize(light + viewer);

			const float specularStrength = 3.0f; // constant used in YR

			var pages = new byte[256];
			for (int i = 0; i < normalsTable.Length; i++) {
				float diffuse = Math.Max(Vector3.Dot(normalsTable[i], light), 0f);
				float halfwayDot = Vector3.Dot(normalsTable[i], halfway);
				float specular = halfwayDot / (specularStrength - halfwayDot * specularStrength + halfwayDot);
				specular = Math.Max(specular, 0f);

				pages[i] = (byte)Math.Clamp((diffuse + specular) * 16.0f, 0f, 255f);
			}

			// special normal indices are neutrally lit
			pages[253] = 16;
			pages[254] = 16;
			pages[255] = 16;

			return pages;
		}

		static readonly float[] zeroVector = { 0, 0, 0, 1 };
		static readonly float[] zVector = { 0, 0, 1, 1 };
		static Vector3 ExtractRotationVector(float[] mtx) {
			var tVec = MatrixVectorMultiply(mtx, zVector);
			var tOrigin = MatrixVectorMultiply(mtx, zeroVector);
			tVec[0] -= tOrigin[0] * tVec[3] / tOrigin[3];
			tVec[1] -= tOrigin[1] * tVec[3] / tOrigin[3];
			tVec[2] -= tOrigin[2] * tVec[3] / tOrigin[3];

			// Renormalize
			var w = (float)Math.Sqrt(tVec[0] * tVec[0] + tVec[1] * tVec[1] + tVec[2] * tVec[2]);
			tVec[0] /= w;
			tVec[1] /= w;
			tVec[2] /= w;
			tVec[3] = 1f;

			return new Vector3(tVec[0], tVec[1], tVec[2]);
		}

		static float[] ToOpenGL(Matrix4x4 source) {
			return new[] {
				source.M11, source.M12, source.M13, source.M14,
				source.M21, source.M22, source.M23, source.M24,
				source.M31, source.M32, source.M33, source.M34,
				source.M41, source.M42, source.M43, source.M44,
			};
		}

		static float[] MatrixVectorMultiply(float[] mtx, float[] vec) {
			var ret = new float[4];
			for (var j = 0; j < 4; j++) {
				ret[j] = 0;
				for (var k = 0; k < 4; k++)
					ret[j] += mtx[4 * k + j] * vec[k];
			}

			return ret;
		}

		unsafe void Clear() {
			byte* p = (byte*)_surface.BitmapData.Scan0;
			for (int y = 0; y < _surface.Height; y++)
				new Span<byte>(p + y * _surface.BitmapData.Stride, _surface.Width * 4).Clear();
			var shadBuf = _surface.GetShadows();
			Array.Clear(shadBuf, 0, shadBuf.Length);
		}
	}
}
