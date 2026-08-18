using System;
using System.Drawing;
using CNCMaps.Engine.Drawables;
using CNCMaps.Engine.Game;
using CNCMaps.Engine.Map;
using CNCMaps.FileFormats;
using NLog;
using OpenTK.Mathematics;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

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

		// color contributors; the standard voxels.vpl already adds a lot of ambient,
		// that's why these seem high
		private static readonly Vector3 Diffuse = new Vector3(1.3f);
		private static readonly Vector3 Ambient = new Vector3(0.8f);

		DrawingSurface _surface;
		float[] _zBuffer; // window-space depth in [-1,1] (ndc z), depth-test "less"

		public void Initialize() {
			Logger.Info("Initializing voxel renderer");
			_isInit = true;
			_surface = new DrawingSurface(400, 400, PixelFormat.Format32bppArgb);
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
			var persp = Matrix4.CreateOrthographic(orthoDiameter, orthoDiameter * _surface.Height / _surface.Width, 1, _surface.Height);

			var lookat = Matrix4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);
			var trans = Matrix4.CreateTranslation(0, 0, 10);

			// align and zoom
			var world = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(60));
			world = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(180)) * world;
			world = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(-45)) * world;
			world = Matrix4.CreateScale(0.028f, 0.028f, 0.028f) * world;

			// determine tilt vectors
			Matrix4 tilt = Matrix4.Identity;
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
				tilt *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(tiltPitch));
				tilt *= Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(tiltYaw));
			}

			// object rotation around Z
			float direction = (obj is OwnableObject) ? (obj as OwnableObject).Direction : 0;
			float objectRotation = 90 - direction / 256f * 360f - tiltYaw; // convert game rotation to world degrees
			Matrix4 @object = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objectRotation)) * tilt; // object facing
																										   // art.ini TurretOffset value positions some voxel parts over our x-axis
			@object = Matrix4.CreateTranslation(0.18f * props.TurretVoxelOffset, 0, 0) * @object;

			float pitch = MathHelper.DegreesToRadians(210);
			float yaw = MathHelper.DegreesToRadians(120);
			var shadowTransform = Matrix4.CreateRotationZ(pitch) * Matrix4.CreateRotationY(yaw);
			// clear shadowbuf
			var shadBuf = _surface.GetShadows();
			Array.Clear(shadBuf, 0, shadBuf.Length);

			foreach (var section in vxl.Sections) {
				var frameRot = hva.LoadGLMatrix(section.Index);
				frameRot.M41 *= section.HVAMultiplier * section.ScaleX;
				frameRot.M42 *= section.HVAMultiplier * section.ScaleY;
				frameRot.M43 *= section.HVAMultiplier * section.ScaleZ;

				var frameTransl = Matrix4.CreateTranslation(section.MinBounds);
				var frame = frameTransl * frameRot;

				// full modelview-projection for this section, mirroring the former GL
				// matrix stack (row-vector convention: leftmost matrix applies first)
				var mvp = frame * @object * world * trans * lookat * persp;

				// shadow: flatten the model onto the ground plane (z=0 in upright world
				// space, i.e. after the model/facing/tilt transforms but before the
				// camera transforms), then project to screen like regular geometry.
				// This projects the actual voxel volume straight down, like the game.
				var flatten = Matrix4.Identity;
				flatten.M33 = 0f;
				var shadowMvp = frame * @object * flatten * world * trans * lookat * persp;

				// undo world transformations on light direction
				var v = @object * world * frame * shadowTransform;

				var lightDirection = (v.Determinant != 0.0) ? ExtractRotationVector(ToOpenGL(Matrix4.Invert(v))) : Vector3.Zero;

				for (uint x = 0; x != section.SizeX; x++) {
					for (uint y = 0; y != section.SizeY; y++) {
						foreach (VxlFile.Voxel vx in section.Spans[x, y].Voxels) {
							if (vx.ColorIndex == 0) continue;
							Color color = obj.Palette.Colors[vx.ColorIndex];
							Vector3 normal = section.GetNormal(vx.NormalIndex);
							// shader function taken from https://github.com/OpenRA/OpenRA/blob/bleed/cg/vxl.fx
							// thanks to pchote for a LOT of help getting it right
							Vector3 colorMult = Vector3.Add(Ambient, Diffuse * Math.Max(Vector3.Dot(normal, lightDirection), 0f));
							byte cr = (byte)Math.Min(255, color.R * colorMult.X);
							byte cg = (byte)Math.Min(255, color.G * colorMult.Y);
							byte cb = (byte)Math.Min(255, color.B * colorMult.Z);

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

			var world = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(60));
			world = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objectRotation)) * world; // object facing
			world = Matrix4.CreateScale(0.25f, 0.25f, 0.25f) * world;

			// art.ini TurretOffset value positions some voxel parts over our x-axis
			world = Matrix4.CreateTranslation(0.18f * props.TurretVoxelOffset, 0, 0) * world;
			var camera = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(30), 1f, 1, 100);
			world = world * camera;

			Rectangle ret = Rectangle.Empty;
			foreach (var section in vxl.Sections) {
				var frameRot = hva.LoadGLMatrix(section.Index);
				frameRot.M41 *= section.HVAMultiplier * section.ScaleX;
				frameRot.M42 *= section.HVAMultiplier * section.ScaleY;
				frameRot.M43 *= section.HVAMultiplier * section.ScaleZ;

				var minbounds = new Vector3(section.MinBounds);
				if (props.HasShadow)
					minbounds.Z = -100;

				var frameTransl = Matrix4.CreateTranslation(minbounds);
				var frame = frameTransl * frameRot * world;

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
				floorTopLeft = Vector3.TransformVector(floorTopLeft, frame);
				floorTopRight = Vector3.TransformVector(floorTopRight, frame);
				floorBottomRight = Vector3.TransformVector(floorBottomRight, frame);
				floorBottomLeft = Vector3.TransformVector(floorBottomLeft, frame);

				ceilTopLeft = Vector3.TransformVector(ceilTopLeft, frame);
				ceilTopRight = Vector3.TransformVector(ceilTopRight, frame);
				ceilBottomRight = Vector3.TransformVector(ceilBottomRight, frame);
				ceilBottomLeft = Vector3.TransformVector(ceilBottomLeft, frame);

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

		static float[] ToOpenGL(Matrix4 source) {
			var destination = new float[16];
			destination[00] = source.Column0.X;
			destination[01] = source.Column1.X;
			destination[02] = source.Column2.X;
			destination[03] = source.Column3.X;
			destination[04] = source.Column0.Y;
			destination[05] = source.Column1.Y;
			destination[06] = source.Column2.Y;
			destination[07] = source.Column3.Y;
			destination[08] = source.Column0.Z;
			destination[09] = source.Column1.Z;
			destination[10] = source.Column2.Z;
			destination[11] = source.Column3.Z;
			destination[12] = source.Column0.W;
			destination[13] = source.Column1.W;
			destination[14] = source.Column2.W;
			destination[15] = source.Column3.W;
			return destination;
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

		void RenderVoxel(Vector3 v, ref Matrix4 mvp, byte r, byte g, byte b) {
			const float rad = 0.5f;
			// transform the 8 cube corners to window coordinates
			bool valid = true;
			for (int i = 0; i < 8; i++) {
				var corner = new Vector4(
					v.X + (((i & 1) != 0) ? rad : -rad),
					v.Y + (((i & 2) != 0) ? rad : -rad),
					v.Z + (((i & 4) != 0) ? rad : -rad), 1f);
				var clip = Vector4.TransformRow(corner, mvp);
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

		void RenderVoxelShadow(Vector3 v, ref Matrix4 shadowMvp, bool[] shadBuf) {
			const float rad = 0.5f;
			for (int i = 0; i < 8; i++) {
				var corner = new Vector4(
					v.X + (((i & 1) != 0) ? rad : -rad),
					v.Y + (((i & 2) != 0) ? rad : -rad),
					v.Z + (((i & 4) != 0) ? rad : -rad), 1f);
				var clip = Vector4.TransformRow(corner, shadowMvp);
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
