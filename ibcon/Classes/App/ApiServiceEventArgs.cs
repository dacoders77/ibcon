using System;
using System.Collections.Generic;
using System.Text;

namespace IBcon.Classes
{
	/* IB gateway API service message event type */
	public class ApiServiceEventArgs : EventArgs
	{
		public string HistoryBarsJsonString { get; set; }
		public string SymbolTickPrice { get; set; }
	}
}
