using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

namespace CNCMaps.FileFormats.Map {
	public class TunnelLine {
		public int StartX { get; set; }
		public int StartY { get; set; }
		public int Facing { get; set; }
		public int EndX { get; set; }
		public int EndY { get; set; }
		public List<int> Direction { get; set; }

		static Logger logger = LogManager.GetCurrentClassLogger();

        public TunnelLine()
        {
            StartX = -1;
            StartY = -1;
            Facing = -1;
            EndX = -1;
            EndY = -1;
			Direction = new List<int>();
        }

        public TunnelLine(int sx, int sy, int facing, int ex, int ey,  List<int> ds)
        {
            StartX = sx;
            StartY = sy;
            Facing = facing;
            EndX = ex;
            EndY = ey;
			Direction = new List<int>();
			if (ds != null)
				Direction = ds.ToList();
		}
	}
}
