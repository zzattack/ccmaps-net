using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CNCMaps.Engine.Rendering {
	// A simple canvas with intuitive zooming and panning operations.
	// Implemented using a virtual size mechanism with helper methods
	// to aid lightning-fast updated by client code hooking into 'VirtualDraw' event.
	public class ZoomableCanvas : Control, IMessageFilter {
		#region privates
		private const double _zoomMultiplier = 1.2; // zooming adds/removes 20% per step
		private int _zoomStep = 0; // zoom factor == pow(zoomMultiplier,zoomStep)
		private const int MAX_ZOOM_STEPS = 16;

		private PointF _panningPos; // non-scaled offset between top-left corner of client rectangle and top-left pixel of image
		private bool _panning = false; // whether we are currently in panning mode (initiated by right-mouse down)
		private Point _panningStartMouseLocation; // mouse coordinate at start of drag
		private PointF _panningStartDragLocation; // _panningPos at start of drag

		private bool _zoomSizing = false; // whether we are currently selecting a zoom region (initiated by left-mouse down)
		private Point _zoomSizingStartLocation; // mouse pos where zoom operation was started
		private Point _zoomSizingMousePos; // mouse pos at last movement while still zooming

		private Size _virtualSize;
		private TextureBrush _texture; // background texture

		#endregion

		#region properties

		private bool _virtualMode;
		public bool VirtualMode {
			get => _virtualMode;
			set {
				_virtualMode = value;
				Invalidate();
			}
		}
		public Size VirtualSize {
			get => _virtualSize;
			set {
				_virtualSize = value;
				_panningPos = PointF.Empty;
				Invalidate();
			}
		}
		public Size ImageSize => VirtualMode ? VirtualSize : Image?.Size ?? Size.Empty;

		private Image _image;
		public Image Image {
			get => _image;
			set {
				_image = value;
				if (!_virtualMode) {
					Invalidate();
				}
			}
		}

		public float ZoomFactor => (float)Math.Pow(_zoomMultiplier, _zoomStep);
		#endregion

		#region events
		public event PaintEventHandler VirtualDraw;
		#endregion

		public ZoomableCanvas() {
			SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
			SetStyle(ControlStyles.StandardDoubleClick, false);

			CreateGridTexture();
		}

		protected override void Dispose(bool disposing) {
			_texture.Dispose();
			base.Dispose(disposing);
		}

		protected override void OnPaintBackground(PaintEventArgs e) {
			e.Graphics.FillRectangle(_texture, e.ClipRectangle);
		}

		protected override void OnPaint(PaintEventArgs e) {
			base.OnPaint(e);

			if (VirtualMode) {
				OnVirtualDraw(e);
			}
			else if (Image != null) {
				e.Graphics.DrawImage(Image, ClientRectangle,
					Rectangle.Round(ScaleRectangle(ClientRectangle)), GraphicsUnit.Pixel);
			}

			if (_zoomSizing) {
				var r = Rectangle.FromLTRB(
					Math.Min(_zoomSizingStartLocation.X, _zoomSizingMousePos.X),
					 Math.Min(_zoomSizingStartLocation.Y, _zoomSizingMousePos.Y),
				   Math.Max(_zoomSizingStartLocation.X, _zoomSizingMousePos.X),
				   Math.Max(_zoomSizingStartLocation.Y, _zoomSizingMousePos.Y));
				using (var brush = new SolidBrush(Color.FromArgb(80, Color.DodgerBlue))) {
					e.Graphics.FillRectangle(brush, r);
				}
				using (var pen = new Pen(Color.DodgerBlue)) {
					e.Graphics.DrawRectangle(pen, r);
				}
			}
		}

		protected virtual void OnVirtualDraw(PaintEventArgs e) {
			VirtualDraw?.Invoke(this, e);
		}

		protected override void OnMouseDown(MouseEventArgs e) {
			base.OnMouseDown(e);
			if (e.Button == MouseButtons.Right) {
				_panning = true;
				_panningStartMouseLocation = e.Location;
				_panningStartDragLocation = _panningPos;
			}
			else if (e.Button == MouseButtons.Left) {
				_zoomSizing = true;
				_zoomSizingStartLocation = e.Location;
			}
		}

		protected override void OnMouseUp(MouseEventArgs e) {
			base.OnMouseUp(e);

			if (e.Button == MouseButtons.Right) {
				_panning = false;
			}

			else if (_zoomSizing && e.Button == MouseButtons.Left) {
				_zoomSizing = false;
				var p1 = _zoomSizingStartLocation;
				var p2 = e.Location;
				var r = RectangleF.FromLTRB(
					Math.Min(p1.X, p2.X),
					Math.Min(p1.Y, p2.Y),
					Math.Max(p1.X, p2.X),
					Math.Max(p1.Y, p2.Y));

				// Since use on touch screens can often lead to unintended pinch/zoom actions,
				// ensure the minimum region is not too small
				if (r.Width >= 16 && r.Height >= 16) {
					ZoomToRegion(Rectangle.Round(ScaleRectangle(r)));
				}
				Invalidate();
			}
		}

		protected override void OnMouseMove(MouseEventArgs e) {
			if (_panning) {
				_panningPos = new PointF(
					_panningStartDragLocation.X + (e.Location.X - _panningStartMouseLocation.X),
					_panningStartDragLocation.Y + (e.Location.Y - _panningStartMouseLocation.Y));
				Invalidate();
			}
			else if (_zoomSizing) {
				_zoomSizingMousePos = e.Location;
				Invalidate();
			}
			else {
				base.OnMouseMove(e);
			}
		}

		protected override void OnMouseWheel(MouseEventArgs e) {
			base.OnMouseWheel(e);
			ZoomStep(e.Delta / 120, e.Location);

			if (_panning) {
				// reset panning under new zoomlevel conditions
				_panningStartDragLocation = _panningPos;
				_panningStartMouseLocation = e.Location;
			}

			Invalidate();
		}

		protected override void OnHandleCreated(EventArgs e) {
			base.OnHandleCreated(e);
			Application.AddMessageFilter(this);
		}

		protected override void OnHandleDestroyed(EventArgs e) {
			base.OnHandleDestroyed(e);
			Application.RemoveMessageFilter(this);
		}

		private void ZoomStep(int stepDelta, PointF focus) {
			double zoomPre = ZoomFactor;
			_zoomStep += stepDelta;
			_zoomStep = Math.Max(-MAX_ZOOM_STEPS, Math.Min(+MAX_ZOOM_STEPS, _zoomStep));
			double zoomPost = ZoomFactor;

			// It is desirable to have the cursor pre-zoom point at the same
			// image pixel location both before and after the zoom is applied.
			// This is achieved by moving the panning position.
			// formula follows p' = m-((f-p)/z)*z' where (p=pan pre, p'=pan after, f=focus pixel, z=zoom pre, z'=zoom after)
			_panningPos.X = (float)(focus.X - (focus.X - _panningPos.X) / zoomPre * zoomPost);
			_panningPos.Y = (float)(focus.Y - (focus.Y - _panningPos.Y) / zoomPre * zoomPost);
		}

		public void ZoomToFit() {
			ZoomToRegion(new Rectangle(Point.Empty, ImageSize));
		}

		private void ZoomToRegion(Rectangle region) {
			double exactZoom = Math.Min(Math.Log(ClientRectangle.Width / (double)region.Width, _zoomMultiplier),
										Math.Log(ClientRectangle.Height / (double)region.Height, _zoomMultiplier));
			// by using floor (as opposed to rounding), we ensure selected area fits entirely
			_zoomStep = Math.Max(-MAX_ZOOM_STEPS, Math.Min(+MAX_ZOOM_STEPS, (int)Math.Floor(exactZoom)));
			_panningPos = new PointF(-region.Location.X * ZoomFactor, -region.Location.Y * ZoomFactor);

			// since zoom step is integer and we maintain aspect ratio, resulting zoom area
			// may cover more than the region to which we zoom, so we should center within that
			_panningPos.X -= (region.Width * ZoomFactor - ClientSize.Width) / 2f;
			_panningPos.Y -= (region.Height * ZoomFactor - ClientSize.Height) / 2f;

			Invalidate();
		}

		public PointF PointToImagePixel(PointF point) {
			return new PointF((point.X - _panningPos.X) / ZoomFactor, (point.Y - _panningPos.Y) / ZoomFactor);
		}

		public PointF ImagePixelToPoint(PointF pixel) {
			return new PointF(pixel.X * ZoomFactor + _panningPos.X, pixel.Y * ZoomFactor + _panningPos.Y);
		}

		public RectangleF ScaleRectangle(RectangleF viewArea) {
			// scale rectangle in pixel coordinate to virtual image coordinates
			RectangleF r = new RectangleF(
				(viewArea.Left - _panningPos.X) / ZoomFactor,
				(viewArea.Top - _panningPos.Y) / ZoomFactor,
				viewArea.Width / ZoomFactor,
				viewArea.Height / ZoomFactor);
			return r;
		}

		public Rectangle GetOffsetRectangle(RectangleF imageArea) {
			// Inverse of ScaleRectangle
			return new Rectangle(
				(int)Math.Round(imageArea.Left * ZoomFactor + _panningPos.X),
				(int)Math.Round(imageArea.Top * ZoomFactor + _panningPos.Y),
				(int)Math.Round(imageArea.Width * ZoomFactor),
				(int)Math.Round(imageArea.Height * ZoomFactor));
		}

		public void CreateGridTexture() {
			const int cellSize = 8;
			Color cellColor = Color.White;
			Color altColor = Color.Gainsboro;

			using (var bm = new Bitmap(cellSize * 2, cellSize * 2)) {
				using (Graphics g = Graphics.FromImage(bm)) {
					using (Brush brush = new SolidBrush(cellColor)) {
						g.FillRectangle(brush, new Rectangle(cellSize, 0, cellSize, cellSize));
						g.FillRectangle(brush, new Rectangle(0, cellSize, cellSize, cellSize));
					}

					using (Brush brush = new SolidBrush(altColor)) {
						g.FillRectangle(brush, new Rectangle(0, 0, cellSize, cellSize));
						g.FillRectangle(brush, new Rectangle(cellSize, cellSize, cellSize, cellSize));
					}
				}
				_texture = new TextureBrush(bm);
			}
		}

		public bool PreFilterMessage(ref Message m) {
			switch (m.Msg) {
				case 0x020A: // WM_MOUSEWHEEL
				case 0x020E: //.WM_MOUSEHWHEEL
					IntPtr hControlUnderMouse;

					hControlUnderMouse = WindowFromPoint(new Point((int)m.LParam));
					if (hControlUnderMouse != m.HWnd) {
						if (FromHandle(hControlUnderMouse) is ZoomableCanvas zc) {
							// redirect the message to the control under the mouse
							SendMessage(hControlUnderMouse, m.Msg, m.WParam, m.LParam);

							// swallow message, otherwise two controls will scroll simultaneously
							return true;
						}
					}
					break;
			}

			return false;
		}

		[DllImport("user32.dll")]
		static extern IntPtr WindowFromPoint(Point p);

		[DllImport("user32.dll")]
		public static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
	}
}
