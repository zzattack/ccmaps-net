using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using CNCMaps.FileFormats;
using CNCMaps.Game;
using CNCMaps.Map;
using CNCMaps.VirtualFileSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CNCMaps.Rendering {
	public class VoxelRenderer : IDisposable {
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
		GraphicsContext _ctx;
		GameWindow _gw;
		bool _canRender;
		bool _isInit;

		// color contributors; the standard voxels.vpl already adds a lot of ambient,
		// that's why these seem high
		private static readonly Vector3 _diffuse = new Vector3(1.2f);
		private static readonly Vector3 _ambient = new Vector3(0.8f);

		DrawingSurface _surface;
		VplFile _vplFile;

		public void Initialize() {
			logger.Info("Initializing voxel renderer");
			_isInit = true;

			_surface = new DrawingSurface(400, 400, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			if (!CreateContext()) {
				logger.Error("No graphics context could not be initialized, voxel rendering will be unavailable");
				return;
			}

			logger.Debug("GL context created");
			try {
				GL.LoadAll();
				logger.Debug("GL functions loaded");

				GL.Enable(EnableCap.DepthTest);
				GL.Enable(EnableCap.ColorMaterial);

				//_vplFile = VFS.Open<VplFile>("voxels.vpl"); 
				_canRender = SetupFramebuffer();
			}

			catch (Exception exc) {
				logger.Error("Voxel rendering will not be available because an exception occurred while initializing OpenGL: {0}", exc.ToString());
			}
		}

		private bool CreateContext() {
			logger.Debug("Creating graphics context, trying {0} first", Program.Settings.PreferOSMesa ? "OSMesa" : "Window Manager");

			if (Program.Settings.PreferOSMesa)
				return CreateMesaContext() || CreateGameWindow();
			else
				return CreateGameWindow() || CreateMesaContext();
		}

		private bool CreateGameWindow() {
			try {
				_gw = new GameWindow(_surface.Width, _surface.Height, GraphicsMode.Default, "", GameWindowFlags.Default);
				return true;
			}
			catch {
				logger.Warn("GameWindow could not be created.");
				return false;
			}
		}

		private bool CreateMesaContext() {
			try {
				_ctx = GraphicsContext.CreateMesaContext();
				long ctxPtr = long.Parse(_ctx.ToString()); // cannot access private .Context
				if (ctxPtr != 0) {
					_ctx.MakeCurrent(new OpenTK.Platform.Mesa.BitmapWindowInfo(_surface.bmd));
					if (!_ctx.IsCurrent) {
						logger.Warn("Could not make context current");
						throw new InvalidOperationException("Mesa context could not be made current");
					}
				}
				logger.Info("Successfully acquired Mesa context");
				return true;
			}
			catch {
				logger.Warn("Mesa context could not be created");
				return false;
			}
		}

		public void Dispose() {
			_surface.Dispose();
			if (_ctx != null) _ctx.Dispose();
			if (_gw != null) _gw.Dispose();
		}

		public DrawingSurface Render(VxlFile vxl, HvaFile hva, GameObject obj, DrawProperties props) {
			if (!_isInit) Initialize();
			if (!_canRender) {
				logger.Warn("Not rendering {0} because no OpenGL context could be obtained", vxl.FileName);
				return null;
			}

			logger.Debug("Rendering voxel {0}", vxl.FileName);
			vxl.Initialize();
			hva.Initialize();

			GL.Viewport(0, 0, _surface.bmd.Width, _surface.bmd.Height);
			GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

			// RA2 uses dimetric projection with camera elevated 30° off the ground
			GL.MatrixMode(MatrixMode.Projection);
			var persp = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(30), _surface.bmd.Width / (float)_surface.bmd.Height, 1, _surface.bmd.Height);
			GL.LoadMatrix(ref persp);

			GL.MatrixMode(MatrixMode.Modelview);
			var lookat = Matrix4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);
			GL.LoadMatrix(ref lookat);
			GL.Scale(0.0145, 0.0145, 0.0145); // seems to work well enough for all voxels
			GL.Translate(0, 0, 10);

			float direction = (obj is OwnableObject) ? (obj as OwnableObject).Direction : 0;
			float objectRotation = 45f - direction / 256f * 360f; // convert game rotation to world degrees
			var world = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(objectRotation)); // object facing
			world *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(180)); // invert y-axis
			world *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(60)); // this is how the game places voxels flat on the world

			// art.ini TurretOffset value positions some voxel parts over our x-axis
			world = Matrix4.CreateTranslation(0.18f * props.TurretVoxelOffset, 0, 0) * world;
			GL.MultMatrix(ref world);

			// direction of light vector given by pitch & yaw
			float pitch = MathHelper.DegreesToRadians(210);
			float yaw = MathHelper.DegreesToRadians(120);

			/* helps to find good pitch/yaw
			var colors = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Orange, Color.Black, Color.Purple, Color.SlateBlue, Color.DimGray, Color.White, Color.Teal, Color.Tan };
			for (int i = 0; i < 360; i += 30) {
				for (int j = 0; j < 360; j += 30) {
					GL.Color3(colors[i / 30]);
					var shadowTransform2 =
						Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(i))
						* Matrix4.CreateRotationY(MathHelper.DegreesToRadians(j));
					GL.LineWidth(2);
					GL.Begin(BeginMode.Lines);
					GL.Vertex3(0, 0, 0);
					GL.Vertex3(Vector3.Multiply(ExtractRotationVector(ToOpenGL(Matrix4.Invert(world * shadowTransform2))), 100f));
					GL.End();
				}
			}*/

			var shadowTransform = Matrix4.CreateRotationZ(pitch) * Matrix4.CreateRotationY(yaw);

			foreach (var section in vxl.Sections) {
				GL.PushMatrix();

				var frameRot = hva.LoadGLMatrix(section.Index);
				frameRot.M41 *= section.HVAMultiplier * section.ScaleX;
				frameRot.M42 *= section.HVAMultiplier * section.ScaleY;
				frameRot.M43 *= section.HVAMultiplier * section.ScaleZ;

				var frameScale = Matrix4.Scale(section.HVAMultiplier); // seems I don't need this?
				var frameTransl = Matrix4.CreateTranslation(section.MinBounds);
				var frame = frameRot * frameTransl;
				GL.MultMatrix(ref frame);

				// undo world transformations on light direction
				var lightDirection = ExtractRotationVector(ToOpenGL(Matrix4.Invert(world * frame * shadowTransform)));

				/* draw line in direction light comes from
				GL.LineWidth(2);
				GL.Begin(BeginMode.Lines);
				GL.Vertex3(0, 0, 0);
				GL.Vertex3(Vector3.Multiply(lightDirection, 100f));
				GL.End();
				*/

				GL.Begin(BeginMode.Quads);
				for (uint x = 0; x != section.SizeX; x++) {
					for (uint y = 0; y != section.SizeY; y++) {
						foreach (VxlFile.Voxel vx in section.Spans[x, y].Voxels) {
							Color color = obj.Palette.Colors[vx.ColorIndex];
							Vector3 normal = section.GetNormals()[vx.NormalIndex];
							// shader function taken from https://github.com/OpenRA/OpenRA/blob/bleed/cg/vxl.fx
							// thanks to pchote for a LOT of help getting it right
							Vector3 colorMult = Vector3.Add(_ambient, _diffuse * Math.Max(Vector3.Dot(normal, lightDirection), 0f));
							GL.Color3(
								(byte)Math.Min(255, color.R * colorMult.X),
								(byte)Math.Min(255, color.G * colorMult.Y),
								(byte)Math.Min(255, color.B * colorMult.Z));

							Vector3 vxlPos = Vector3.Multiply(new Vector3(x, y, vx.Z), section.Scale);
							RenderVoxel(vxlPos);

							/* draw line in normal direction
							if (r.Next(100) == 4) {
								float m = Math.Max(Vector3.Dot(normal, lightDirection), 0f);
								GL.Color3(m, m, m);
								GL.LineWidth(1);
								GL.Begin(BeginMode.Lines);
								GL.Vertex3(new Vector3(x, y, vx.Z));
								GL.Vertex3(new Vector3(x, y, vx.Z) + Vector3.Multiply(normal, 100f));
								GL.End();
							}*/

						}
					}
				}
				GL.End();
				GL.PopMatrix();
			}

			// read pixels back to surface
			GL.ReadPixels(0, 0, _surface.bmd.Width, _surface.bmd.Height, PixelFormat.Bgra, PixelType.UnsignedByte, _surface.bmd.Scan0);
			return _surface;
		}

		bool SetupFramebuffer() {
			int fbo;
			try {
				GL.Ext.GenFramebuffers(1, out fbo);
				GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
				GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
				GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
			}
			catch {
				logger.Error("Failed to initialize framebuffers. Voxels will not be rendered.");
				return false;
			}
			int depthbuffer;
			GL.Ext.GenRenderbuffers(1, out depthbuffer);
			GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, depthbuffer);
			GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.DepthComponent32, _surface.bmd.Width, _surface.bmd.Height);
			GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachmentExt, RenderbufferTarget.RenderbufferExt, depthbuffer);

			int rgb_rb;
			GL.Ext.GenRenderbuffers(1, out rgb_rb);
			GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, rgb_rb);
			GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.Rgba8, _surface.bmd.Width, _surface.bmd.Height);
			GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, RenderbufferTarget.RenderbufferExt, rgb_rb);

			return GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt) == FramebufferErrorCode.FramebufferCompleteExt;
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

		public void RenderVoxel(Vector3 v) {
			float r = 0.5f;
			float left = v.X - r;
			float right = v.X + r;
			float fbase = v.Y - r;
			float top = v.Y + r;
			float front = v.Z - r;
			float back = v.Z + r;

			// Base
			GL.Vertex3(left, fbase, front);
			GL.Vertex3(right, fbase, front);
			GL.Vertex3(right, fbase, back);
			GL.Vertex3(left, fbase, back);

			// Back
			GL.Vertex3(left, fbase, back);
			GL.Vertex3(right, fbase, back);
			GL.Vertex3(right, top, back);
			GL.Vertex3(left, top, back);

			// Top
			GL.Vertex3(left, top, front);
			GL.Vertex3(right, top, front);
			GL.Vertex3(right, top, back);
			GL.Vertex3(left, top, back);

			// Right
			GL.Vertex3(right, fbase, front);
			GL.Vertex3(right, fbase, back);
			GL.Vertex3(right, top, back);
			GL.Vertex3(right, top, front);

			// Front
			GL.Vertex3(left, fbase, front);
			GL.Vertex3(right, fbase, front);
			GL.Vertex3(right, top, front);
			GL.Vertex3(left, top, front);

			// Left
			GL.Vertex3(left, fbase, front);
			GL.Vertex3(left, fbase, back);
			GL.Vertex3(left, top, back);
			GL.Vertex3(left, top, front);
		}

	}
}
