using System;
using System.Drawing.Drawing2D;
using CNCMaps.FileFormats;
using CNCMaps.VirtualFileSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CNCMaps.Rendering {
	public class VoxelRenderer : IDisposable {
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
		GraphicsContext _ctx;
		GameWindow _gw;

		readonly Vector3 _diffuse = new Vector3(1.0f, 1.0f, 1.0f);
		readonly Vector3 _ambient = new Vector3(0.8f, 0.8f, 0.8f);

		DrawingSurface vxl_ds;
		VxlFile _vxlFile;
		HvaFile _hvaFile;
		Palette _palette;
		VplFile _vplFile;

		int frame;
		double _objectRotation;
		bool _canRender;
		bool _isInit;

		int _vertexShader;
		int _fragmentShader;
		int _program;

		public void Initialize() {
			logger.Info("Initializing voxel renderer");
			_isInit = true;

			vxl_ds = new DrawingSurface(400, 400, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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
				_gw = new GameWindow(vxl_ds.Width, vxl_ds.Height, GraphicsMode.Default, "", GameWindowFlags.Default);
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
					_ctx.MakeCurrent(new OpenTK.Platform.Mesa.BitmapWindowInfo(vxl_ds.bmd));
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
			vxl_ds.Dispose();
			if (_ctx != null) _ctx.Dispose();
			if (_gw != null) _gw.Dispose();
		}

		public DrawingSurface Render(VxlFile vxlFile, HvaFile hvaFile, double objectRotation, Palette palette) {
			if (!_isInit) Initialize();
			if (!_canRender) {
				logger.Warn("Not rendering {0} because no OpenGL context could be obtained", vxlFile.FileName);
				return null;
			}
			this._vxlFile = vxlFile;
			this._hvaFile = hvaFile;
			this._palette = palette;
			this._objectRotation = objectRotation;

			logger.Debug("Rendering voxel {0}", vxlFile.FileName);

			vxlFile.Initialize();
			hvaFile.Initialize();
			SetupFrameRender();
			GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);

			// determine size
			for (int i = 0; i != vxlFile.NumSections(); i++) {
				vxlFile.SetSection(i);
				hvaFile.SetSection(i);
				RenderSection();
			}

			GL.ReadPixels(0, 0, vxl_ds.bmd.Width, vxl_ds.bmd.Height, PixelFormat.Bgra, PixelType.UnsignedByte, vxl_ds.bmd.Scan0);
			return vxl_ds;
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
			GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.DepthComponent32, vxl_ds.bmd.Width, vxl_ds.bmd.Height);
			GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.DepthAttachmentExt, RenderbufferTarget.RenderbufferExt, depthbuffer);

			int rgb_rb;
			GL.Ext.GenRenderbuffers(1, out rgb_rb);
			GL.Ext.BindRenderbuffer(RenderbufferTarget.RenderbufferExt, rgb_rb);
			GL.Ext.RenderbufferStorage(RenderbufferTarget.RenderbufferExt, RenderbufferStorage.Rgba8, vxl_ds.bmd.Width, vxl_ds.bmd.Height);
			GL.Ext.FramebufferRenderbuffer(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext, RenderbufferTarget.RenderbufferExt, rgb_rb);

			return GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt) == FramebufferErrorCode.FramebufferCompleteExt;
		}

		private void SetupFrameRender() {
			GL.Viewport(0, 0, vxl_ds.bmd.Width, vxl_ds.bmd.Height);
			GL.MatrixMode(MatrixMode.Projection);
			var persp = Matrix4.CreatePerspectiveFieldOfView(30f / 180f * (float)Math.PI, vxl_ds.bmd.Width / (float)vxl_ds.bmd.Height, 2, vxl_ds.bmd.Height);
			GL.LoadMatrix(ref persp);

			GL.MatrixMode(MatrixMode.Modelview);
			var lookat = Matrix4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);
			GL.LoadMatrix(ref lookat);

			GL.Translate(0, 0, 10);
			GL.Rotate(60, 1, 0, 0);
			GL.Rotate(180, 0, 1, 0);
			GL.Rotate(_objectRotation, 0, 0, 1);

			GL.Scale(0.028, 0.028, 0.028);
		}

		void RenderSection() {
			GL.PushMatrix();

			byte xs, ys, zs;
			_vxlFile.GetSize(out xs, out ys, out zs);

			float[] min, max;
			_vxlFile.GetBounds(out min, out max);

			/* Calculate the screen units / voxel ratio for scaling */
			max[0] -= min[0];
			max[1] -= min[1];
			max[2] -= min[2];
			var sectionScale = new float[3];
			sectionScale[0] = max[0] / xs;
			sectionScale[1] = max[1] / ys;
			sectionScale[2] = max[2] / zs;

			// Load transformation matrix
			float[] transform;
			_hvaFile.loadGLMatrix(frame, out transform);
			// The HVA transformation matrices have to be scaled
			transform[12] *= _vxlFile.GetScale() * sectionScale[0];
			transform[13] *= _vxlFile.GetScale() * sectionScale[1];
			transform[14] *= _vxlFile.GetScale() * sectionScale[2];

			// Apply the transform for this frame
			GL.MultMatrix(transform);

			// Translate to the bottom left of the section's bounding box
			GL.Translate(min[0], min[1], min[2]);

			float pitch = 50.0f / 180.0f * (float)Math.PI;
			float yaw = 240.0f / 180.0f * (float)Math.PI;
			Matrix4 lightDir = Matrix4.Mult(Matrix4.CreateRotationX(pitch), Matrix4.CreateRotationY(yaw));
			Matrix4 modelView;
			GL.GetFloat(GetPName.ModelviewMatrix, out modelView);

			var lightTransform = Matrix4.Mult(Matrix4.Invert(modelView), lightDir);
			var lightDirVec = ExtractRotationVector(ToOpenGL(Matrix4.Mult(Matrix4.Invert(modelView), lightTransform)));

			GL.Begin(BeginMode.Quads);
			for (uint x = 0; x != xs; x++) {
				for (uint y = 0; y != ys; y++) {
					for (uint z = 0; z != zs; z++) {
						VxlFile.LimbBody.Span.Voxel vx;
						if (_vxlFile.GetVoxel(x, y, z, out vx)) {
							float[] normalf = new float[3];
							_vxlFile.GetXYZNormal(vx.normal, out normalf);

							var color = _palette.Colors[vx.colour];
							Vector3 normal = new Vector3(normalf[0], normalf[1], normalf[2]);

							Vector3 colorMult = Vector3.Add(_ambient, _diffuse * Math.Max(Vector3.Dot(normal, lightDirVec), 0f));
							GL.Color3(
								(byte)Math.Min(255, color.R * colorMult.X), 
								(byte)Math.Min(255, color.G * colorMult.Y),
								(byte)Math.Min(255, color.B * colorMult.Z));

							RenderVoxel(x * sectionScale[0], y * sectionScale[0], z * sectionScale[0]);
						}
					}
				}
			}
			GL.End();
			GL.PopMatrix();
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

		public void RenderVoxel(float cx, float cy, float cz) {
			float r = 0.33f;
			float left = cx - r;
			float right = cx + r;
			float fbase = cy - r;
			float top = cy + r;
			float front = cz - r;
			float back = cz + r;

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
