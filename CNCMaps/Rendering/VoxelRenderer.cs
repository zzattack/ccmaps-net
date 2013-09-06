using System;
using CNCMaps.FileFormats;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace CNCMaps.Rendering {
	public class VoxelRenderer : IDisposable {
		static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
		GraphicsContext _ctx;
		GameWindow _gw;

		float[] lightPos = { 5f, 5f, 10f, 0f };
		float[] lightSpec = { 1f, 0.5f, 0f, 0f };
		float[] lightDiffuse = { 0.95f, 0.95f, 0.95f, 1f };
		float[] lightAmb = { 0.6f, 0.6f, 0.6f, 1f };

		DrawingSurface vxl_ds;
		VxlFile _vxlFile;
		HvaFile _hvaFile;
		Palette _palette;

		int frame;
		int pitch;
		double _objectRotation;
		bool _canRender;
		bool _isInit;

		public void Initialize() {
			logger.Info("Initializing voxel renderer");
			_isInit = true;

			vxl_ds = new DrawingSurface(200, 200, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			if (!CreateContext()) {
				logger.Error("No graphics context could not be initialized, voxel rendering will be unavailable");
				return;
			}

			logger.Debug("GL context created");
			try {
				GL.LoadAll();
				logger.Debug("GL functions loaded");

				GL.Enable(EnableCap.DepthTest);
				GL.Enable(EnableCap.Lighting);
				GL.Enable(EnableCap.ColorMaterial);
				GL.Enable(EnableCap.Normalize);
				GL.Disable(EnableCap.RescaleNormal);
				GL.Enable(EnableCap.Blend);
				GL.ShadeModel(ShadingModel.Smooth);

				GL.Light(LightName.Light0, LightParameter.Position, lightPos);
				GL.Light(LightName.Light0, LightParameter.Specular, lightSpec);
				GL.Light(LightName.Light0, LightParameter.Ambient, lightAmb);
				GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
				GL.Enable(EnableCap.Light0);
				GL.ClearColor(0.5f, 0.9f, 0.3f, 0.0f);

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
				_gw = new GameWindow(200, 200, GraphicsMode.Default, "", GameWindowFlags.Default);
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
			var persp = Matrix4.CreatePerspectiveFieldOfView(45f / 180f * (float)Math.PI, vxl_ds.bmd.Width / (float)vxl_ds.bmd.Height, 2, vxl_ds.bmd.Height);
			GL.LoadMatrix(ref persp);

			GL.MatrixMode(MatrixMode.Modelview);
			var lookat = Matrix4.LookAt(0, 0, -10, 0, 0, 0, 0, 1, 0);
			GL.LoadMatrix(ref lookat);

			GL.Translate(0, 0, 10);
			GL.Rotate(60, 1, 0, 0);
			GL.Rotate(180, 0, 1, 0);
			GL.Rotate(_objectRotation, 0, 0, 1);

			GL.Scale(0.075, 0.075, 0.075);
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

			VxlFile.LimbBody.Span.Voxel vx;

			GL.Begin(BeginMode.Quads);
			for (uint x = 0; x != xs; x++) {
				for (uint y = 0; y != ys; y++) {
					for (uint z = 0; z != zs; z++) {
						if (_vxlFile.GetVoxel(x, y, z, out vx)) {
							GL.Color3(_palette.colors[vx.colour]);
							var normal = new float[3];
							_vxlFile.GetXYZNormal(vx.normal, out normal);
							GL.Normal3(normal);

							RenderVoxel(x * sectionScale[0], y * sectionScale[0], z * sectionScale[0], (1.0f - pitch) / 1.0f);
						}
					}
				}
			}
			GL.End();
			GL.PopMatrix();
		}

		public void RenderVoxel(float cx, float cy, float cz, float r) {
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
