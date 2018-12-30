using System;
using System.Collections.Generic;
using System.Text;

namespace IBcon.Classes.App
{
	/* Ceate instances of services, run them and cincumes generated events */
	public class Controller
	{
		private readonly ApiService _apiService; // IB Gateway connection 
		private readonly WebSocketService _webSocketService; // // Websocket to Laravel/PHP app connection
		private readonly Log _log; // Logger

		// Constructor
		public Controller() {
			_log = new Log();

			_apiService = new ApiService(_log);
			_apiService.onConnection += (object sender, ApiServiceEventArgs args) => _log.Add("Event link from Controller.cs: " + args.Text);
			// onHistoryReceived
			// call WS method and send response to the stream
			_apiService.onHistoryBarsEnd += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.HistoryBarsJsonString);

			_webSocketService = new WebSocketService(_log);
			_webSocketService.onMessage += (object sender, WebSocketServiceEventArgs args) => _apiService.historyBarsLoad(args.Symbol);
			// Call history load method from API SERVICE
			// When history is returned from IB API - call onHistoryReceived


		}

		// Start method. Called from Program.cs
		public void Run() {
			// Test event rise. DELETE IT!
			_apiService.RiseEvent();

			_apiService.Start();
			_webSocketService.Start();
		}
	}
}
