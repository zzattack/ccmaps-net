using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CNCMaps.MapLogic {
	interface OwnableObject {
		string Owner { get; set; }
		short Health { get; set; }
		short Direction { get; set; }
	}
}
