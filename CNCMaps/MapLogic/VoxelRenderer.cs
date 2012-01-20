using System;
using System.Diagnostics;
using CNCMaps.FileFormats;
using CNCMaps.Utility;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace CNCMaps.MapLogic {
	public class VoxelRenderer : GameWindow {

		float[] lightPos = { 5f, 5f, 10f, 0f };
		float[] lightSpec = { 1f, 0.5f, 0f, 0f };
		float[] lightDiffuse = { 0.95f, 0.95f, 0.95f, 1f };
		float[] lightAmb = { 0.6f, 0.6f, 0.6f, 1f };

		public VoxelRenderer() : base(200, 200) {
			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Lighting);
			GL.Enable(EnableCap.ColorMaterial);
			GL.Enable(EnableCap.Normalize);
			GL.Enable(EnableCap.Blend);
			GL.ShadeModel(ShadingModel.Smooth);

			GL.Light(LightName.Light0, LightParameter.Position, lightPos);
			GL.Light(LightName.Light0, LightParameter.Specular, lightSpec);
			GL.Light(LightName.Light0, LightParameter.Ambient, lightAmb);
			GL.Light(LightName.Light0, LightParameter.Diffuse, lightDiffuse);
			GL.Enable(EnableCap.Light0);
			GL.ClearColor(0.5f, 0.9f, 0.3f, 0.0f);
		}

		DrawingSurface vxl_ds = new DrawingSurface(200, 200, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

		VxlFile vxlFile;
		HvaFile hvaFile;
		Palette palette;

		int frame;
		int pitch;

		double objectRotation;
		public DrawingSurface Render(VxlFile vxlFile, HvaFile hvaFile, double objectRotation, Palette palette) {
			this.vxlFile = vxlFile;
			this.hvaFile = hvaFile;
			this.palette = palette;
			this.objectRotation = objectRotation;

			vxlFile.Initialize();
			hvaFile.Initialize();
			SetupFrameRender();
			GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
			
			// determine size
			for (int i = 0; i != vxlFile.NumSections(); i++) {
				vxlFile.SetSection(i);
				hvaFile.SetSection(i);
				renderSection();
			}

			GL.ReadPixels(0, 0, vxl_ds.bmd.Width, vxl_ds.bmd.Height, PixelFormat.Bgra, PixelType.UnsignedByte, vxl_ds.bmd.Scan0);
			return vxl_ds;
		}

		private void SetupFrameRender() {
			int fbo;
			GL.Ext.GenFramebuffers(1, out fbo);
			GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
			GL.Ext.FramebufferDrawBuffer(fbo, DrawBufferMode.ColorAttachment0);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			GL.ReadBuffer(ReadBufferMode.ColorAttachment0);
					
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

			Debug.Assert(GL.CheckFramebufferStatus(FramebufferTarget.FramebufferExt) == FramebufferErrorCode.FramebufferCompleteExt);
			
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
			GL.Rotate(objectRotation, 0, 0, 1);

			GL.Scale(0.075, 0.075, 0.075);
		}

		 void renderSection() {
			GL.PushMatrix();

			byte xs, ys, zs;
			vxlFile.getSize(out xs, out ys, out zs);

			float[] min, max;
			vxlFile.getBounds(out min, out max);

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
			hvaFile.loadGLMatrix(frame, out transform);
			// The HVA transformation matrices have to be scaled
			transform[12] *= vxlFile.getScale() * sectionScale[0];
			transform[13] *= vxlFile.getScale() * sectionScale[1];
			transform[14] *= vxlFile.getScale() * sectionScale[2];

			// Apply the transform for this frame
			GL.MultMatrix(transform);

			/* Translate to the bottom left of the section's bounding box */
			GL.Translate(min[0], min[1], min[2]);
	 
			VxlFile.LimbBody.Span.Voxel vx;
			
			GL.Begin(BeginMode.Quads);
			for(uint x = 0; x != xs; x++) {
				for(uint y = 0; y != ys; y++) {
					for(uint z = 0; z != zs; z++) {
						if (vxlFile.getVoxel(x, y, z, out vx)) {
							GL.Color3(palette.colors[vx.colour]);
				
							var normal = new float[3];
							vxlFile.getXYZNormal(vx.normal, out normal);
							GL.Normal3(normal);
							renderVoxel(x * sectionScale[0], y * sectionScale[1], z * sectionScale[2], (1.0f - pitch) / 2.0f);
							GL.PopMatrix();
						}
					}
				}
			}
			GL.End();
			
			GL.PopMatrix();
		}
		void renderVoxel(float cx, float cy, float cz, float r) {
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
