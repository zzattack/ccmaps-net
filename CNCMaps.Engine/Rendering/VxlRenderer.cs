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
	/// Renders voxel models to an offscreen surface using a small software rasterizer.
	/// This replaces the former OpenGL implementation with equivalent semantics
	/// (fixed-function pipeline, flat-shaded quads, depth-test less), so no GPU or
	/// OpenGL driver is required and output is identical on every machine.
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
		float[] _zBuffer; // window-space depth in [-1,1] (ndc z), depth-test "less"

		public void Initialize() {
			Logger.Info("Initializing voxel renderer");
			_isInit = true;
			_surface = new DrawingSurface(400, 400, SurfaceFormat.Bgra32);
			_zBuffer = new float[_surface.Width * _surface.Height];
		}

		public void Dispose() {
			_surface?.Dispose();
		}

		public DrawingSurface Render(VxlFile vxl, HvaFile hva, GameObject obj, DrawProperties props) {
			if (!_isInit) Initialize();

			Logger.Debug("Rendering voxel {0}", vxl.FileName);
			vxl.Initialize();
			hva.Initialize();

			Clear();

			// RA2 uses dimetric projection with camera elevated 30° off the ground.
			// The game projects orthographically (the world is axonometric); the ortho
			// volume is sized to the scale the historical perspective camera (fov 30°,
			// eye distance 20) had at the model's depth: 2*tan(15°)*20 world units.
			const float orthoDiameter = 10.7157f;
			var persp = MatrixMath.CreateOrthographicGL(orthoDiameter, orthoDiameter * _surface.Height / _surface.Width, 1, _surface.Height);

			var lookat = Matrix4x4.CreateLookAt(new Vector3(0, 0, -10), Vector3.Zero, Vector3.UnitY);
			var trans = Matrix4x4.CreateTranslation(0, 0, 10);

			// align and zoom
			var world = Matrix4x4.CreateRotationX(float.DegreesToRadians(60));
			world = MatrixMath.Mul(Matrix4x4.CreateRotationY(float.DegreesToRadians(180)), world);
			world = MatrixMath.Mul(Matrix4x4.CreateRotationZ(float.DegreesToRadians(-45)), world);
			world = MatrixMath.Mul(Matrix4x4.CreateScale(0.028f, 0.028f, 0.028f), world);

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
			// clear shadowbuf
			var shadBuf = _surface.GetShadows();
			Array.Clear(shadBuf, 0, shadBuf.Length);

			foreach (var section in vxl.Sections) {
				var frameRot = hva.LoadGLMatrix(section.Index);
				frameRot.M41 *= section.HVAMultiplier * section.ScaleX;
				frameRot.M42 *= section.HVAMultiplier * section.ScaleY;
				frameRot.M43 *= section.HVAMultiplier * section.ScaleZ;

				var frameTransl = Matrix4x4.CreateTranslation(section.MinBounds);
				var frame = MatrixMath.Mul(frameTransl, frameRot);

				// full modelview-projection for this section, mirroring the former GL
				// matrix stack (row-vector convention: leftmost matrix applies first)
				var mvp = MatrixMath.Mul(frame, @object, world, trans, lookat, persp);

				// shadow: flatten the model onto the ground plane (z=0 in upright world
				// space, i.e. after the model/facing/tilt transforms but before the
				// camera transforms), then project to screen like regular geometry.
				// This projects the actual voxel volume straight down, like the game.
				var flatten = Matrix4x4.Identity;
				flatten.M33 = 0f;
				var shadowMvp = MatrixMath.Mul(frame, @object, flatten, world, trans, lookat, persp);

				// undo world transformations on light direction
				var v = MatrixMath.Mul(@object, world, frame, shadowTransform);

				var lightDirection = Matrix4x4.Invert(v, out var vInv) ? ExtractRotationVector(ToOpenGL(vInv)) : Vector3.Zero;

				// game-accurate lighting: precompute which vpl page every normal maps to
				byte[] vplPages = _vpl != null ? PreCalculateVplLighting(section.GetNormals(), direction) : null;

				for (uint x = 0; x != section.SizeX; x++) {
					for (uint y = 0; y != section.SizeY; y++) {
						foreach (VxlFile.Voxel vx in section.Spans[x, y].Voxels) {
							if (vx.ColorIndex == 0) continue;
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

							Vector3 vxlPos = Vector3.Multiply(new Vector3(x, y, vx.Z), section.Scale);
							RenderVoxel(vxlPos, ref mvp, cr, cg, cb);
							RenderVoxelShadow(vxlPos, ref shadowMvp, shadBuf);
						}
					}
				}
			}

			return _surface;
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

		#region software rasterizer

		struct ScreenVertex {
			public float X, Y, Z; // window coordinates (GL convention, y up) + ndc z
		}

		unsafe void Clear() {
			// clear color to transparent black, depth to far plane
			byte* p = (byte*)_surface.BitmapData.Scan0;
			for (int y = 0; y < _surface.Height; y++)
				new Span<byte>(p + y * _surface.BitmapData.Stride, _surface.Width * 4).Clear();
			for (int i = 0; i < _zBuffer.Length; i++)
				_zBuffer[i] = float.MaxValue;
		}

		// cube corner offsets, index = x + y*2 + z*4 (x: left/right, y: base/top, z: front/back)
		static readonly int[][] CubeFaces = {
			new[] { 0, 1, 5, 4 }, // base   (y = base)
			new[] { 4, 5, 7, 6 }, // back   (z = back)
			new[] { 2, 3, 7, 6 }, // top    (y = top)
			new[] { 1, 5, 7, 3 }, // right  (x = right)
			new[] { 0, 1, 3, 2 }, // front  (z = front)
			new[] { 0, 4, 6, 2 }, // left   (x = left)
		};

		readonly ScreenVertex[] _corners = new ScreenVertex[8];

		void RenderVoxel(Vector3 v, ref Matrix4x4 mvp, byte r, byte g, byte b) {
			const float rad = 0.5f;
			// transform the 8 cube corners to window coordinates
			bool valid = true;
			for (int i = 0; i < 8; i++) {
				var corner = new Vector4(
					v.X + (((i & 1) != 0) ? rad : -rad),
					v.Y + (((i & 2) != 0) ? rad : -rad),
					v.Z + (((i & 4) != 0) ? rad : -rad), 1f);
				var clip = MatrixMath.TransformRow(corner, mvp);
				if (clip.W <= 1e-6f) {
					valid = false; // behind the camera; the fixed camera setup never hits this
					break;
				}
				float invW = 1f / clip.W;
				_corners[i].X = (clip.X * invW + 1f) * _surface.Width / 2f;
				_corners[i].Y = (clip.Y * invW + 1f) * _surface.Height / 2f;
				_corners[i].Z = clip.Z * invW;
			}
			if (!valid)
				return;

			foreach (var f in CubeFaces) {
				RasterizeTriangle(_corners[f[0]], _corners[f[1]], _corners[f[2]], r, g, b);
				RasterizeTriangle(_corners[f[0]], _corners[f[2]], _corners[f[3]], r, g, b);
			}
		}

		readonly ScreenVertex[] _shadowCorners = new ScreenVertex[8];

		void RenderVoxelShadow(Vector3 v, ref Matrix4x4 shadowMvp, bool[] shadBuf) {
			const float rad = 0.5f;
			for (int i = 0; i < 8; i++) {
				var corner = new Vector4(
					v.X + (((i & 1) != 0) ? rad : -rad),
					v.Y + (((i & 2) != 0) ? rad : -rad),
					v.Z + (((i & 4) != 0) ? rad : -rad), 1f);
				var clip = MatrixMath.TransformRow(corner, shadowMvp);
				if (clip.W <= 1e-6f)
					return;
				float invW = 1f / clip.W;
				_shadowCorners[i].X = (clip.X * invW + 1f) * _surface.Width / 2f;
				_shadowCorners[i].Y = (clip.Y * invW + 1f) * _surface.Height / 2f;
			}

			// the flattened cube's faces together cover its ground silhouette
			foreach (var f in CubeFaces) {
				RasterizeShadowTriangle(_shadowCorners[f[0]], _shadowCorners[f[1]], _shadowCorners[f[2]], shadBuf);
				RasterizeShadowTriangle(_shadowCorners[f[0]], _shadowCorners[f[2]], _shadowCorners[f[3]], shadBuf);
			}
		}

		void RasterizeShadowTriangle(ScreenVertex v0, ScreenVertex v1, ScreenVertex v2, bool[] shadBuf) {
			int minX = Math.Max(0, (int)MathF.Floor(MathF.Min(v0.X, MathF.Min(v1.X, v2.X)) - 0.5f));
			int maxX = Math.Min(_surface.Width - 1, (int)MathF.Ceiling(MathF.Max(v0.X, MathF.Max(v1.X, v2.X)) - 0.5f));
			int minY = Math.Max(0, (int)MathF.Floor(MathF.Min(v0.Y, MathF.Min(v1.Y, v2.Y)) - 0.5f));
			int maxY = Math.Min(_surface.Height - 1, (int)MathF.Ceiling(MathF.Max(v0.Y, MathF.Max(v1.Y, v2.Y)) - 0.5f));
			if (minX > maxX || minY > maxY)
				return;

			float area = (v1.X - v0.X) * (v2.Y - v0.Y) - (v1.Y - v0.Y) * (v2.X - v0.X);
			if (area == 0f)
				return;
			float invArea = 1f / area;

			int height = _surface.Height, width = _surface.Width;
			for (int py = minY; py <= maxY; py++) {
				float sy = py + 0.5f;
				// the shadow buffer is indexed top-down (see BlitVoxelToSurface)
				int row = (height - 1 - py) * width;
				for (int px = minX; px <= maxX; px++) {
					float sx = px + 0.5f;
					float w0 = ((v1.X - v0.X) * (sy - v0.Y) - (v1.Y - v0.Y) * (sx - v0.X)) * invArea;
					float w1 = ((v2.X - v1.X) * (sy - v1.Y) - (v2.Y - v1.Y) * (sx - v1.X)) * invArea;
					float w2 = 1f - w0 - w1;
					if (w0 < 0f || w1 < 0f || w2 < 0f)
						continue;
					shadBuf[row + px] = true;
				}
			}
		}

		unsafe void RasterizeTriangle(ScreenVertex v0, ScreenVertex v1, ScreenVertex v2, byte r, byte g, byte b) {
			// bounding box, clipped to viewport; samples at pixel centers (x+0.5, y+0.5)
			int minX = Math.Max(0, (int)MathF.Floor(MathF.Min(v0.X, MathF.Min(v1.X, v2.X)) - 0.5f));
			int maxX = Math.Min(_surface.Width - 1, (int)MathF.Ceiling(MathF.Max(v0.X, MathF.Max(v1.X, v2.X)) - 0.5f));
			int minY = Math.Max(0, (int)MathF.Floor(MathF.Min(v0.Y, MathF.Min(v1.Y, v2.Y)) - 0.5f));
			int maxY = Math.Min(_surface.Height - 1, (int)MathF.Ceiling(MathF.Max(v0.Y, MathF.Max(v1.Y, v2.Y)) - 0.5f));
			if (minX > maxX || minY > maxY)
				return;

			float area = (v1.X - v0.X) * (v2.Y - v0.Y) - (v1.Y - v0.Y) * (v2.X - v0.X);
			if (area == 0f)
				return;
			float invArea = 1f / area;

			byte* scan0 = (byte*)_surface.BitmapData.Scan0;
			int stride = _surface.BitmapData.Stride;

			for (int py = minY; py <= maxY; py++) {
				float sy = py + 0.5f;
				// store rows bottom-up like GL.ReadPixels used to; BlitVoxelToSurface
				// compensates for that when copying to the map surface
				byte* row = scan0 + py * stride;
				int zRow = py * _surface.Width;
				for (int px = minX; px <= maxX; px++) {
					float sx = px + 0.5f;
					// barycentric coordinates (signed areas); accept both windings since
					// the former GL pipeline did not cull faces
					float w0 = ((v1.X - v0.X) * (sy - v0.Y) - (v1.Y - v0.Y) * (sx - v0.X)) * invArea;
					float w1 = ((v2.X - v1.X) * (sy - v1.Y) - (v2.Y - v1.Y) * (sx - v1.X)) * invArea;
					float w2 = 1f - w0 - w1;
					if (w0 < 0f || w1 < 0f || w2 < 0f)
						continue;

					// window-space z interpolates linearly in screen space
					// (w1 weighs v0, w2 weighs v1, w0 weighs v2, from the opposing edges)
					float z = w1 * v0.Z + w2 * v1.Z + w0 * v2.Z;
					int zIdx = zRow + px;
					if (z >= _zBuffer[zIdx])
						continue; // depth-test "less", like the GL default
					_zBuffer[zIdx] = z;

					byte* pix = row + px * 4;
					pix[0] = b;
					pix[1] = g;
					pix[2] = r;
					pix[3] = 255;
				}
			}
		}

		#endregion
	}
}
