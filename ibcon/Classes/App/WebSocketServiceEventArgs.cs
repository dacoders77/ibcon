using System;
using System.Collections.Generic;
using System.Text;

namespace IBcon.Classes.App
{
	/* Websocket service message event type */
	public class WebSocketServiceEventArgs : EventArgs
	{
		public string Symbol { get; set; }
	}
}
