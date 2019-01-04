using System;
using System.Collections.Generic;
using System.Text;

namespace IBcon.Classes.App
{
	/* Websocket service message event type.
	 * When a request is sent from PHP, it contains a clientId which is equal to DB name because it is unuque. 
	 * Then this clentId is returned back to PHP.
	 * Several instances of PHP bot can operate at the same time in send requests to C# server. 
	 * @see https://interactivebrokers.github.io/tws-api/historical_bars.html
	 */
	public class WebSocketServiceEventArgs : EventArgs
	{
		public int clientId { get; set; }
		public string symbol { get; set; }
		public string currency { get; set; }
		public string queryTime { get; set; } // The request's end date and time (the empty string indicates current present moment).
		public string duration { get; set; } // The amount of time (or Valid Duration String units) to go back from the request's given end date and time.
		public string timeFrame { get; set; } // Bar size
	}
}
