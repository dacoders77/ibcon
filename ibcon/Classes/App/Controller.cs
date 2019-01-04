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
			_apiService.onConnection += (object sender, ApiServiceEventArgs args) => _log.Add("onConnection event rised");
			// onHistoryReceived
			// call WS method and send response to the stream
			_apiService.onHistoryBarsEnd += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.HistoryBarsJsonString);

			_webSocketService = new WebSocketService(_log);
			_webSocketService.onMessage += (object sender, WebSocketServiceEventArgs args) => _apiService.historyBarsLoad(args.clientId, args.symbol, args.currency, args.queryTime, args.duration, args.timeFrame);
			// Call history load method from API SERVICE
			// When history is returned from IB API - call onHistoryReceived

			// real-time quotes
			_apiService.onSymbolTick += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.SymbolTickPrice);
		}

		// Start method. Called from Program.cs
		public void Run() {
			_apiService.Start();
			_webSocketService.Start();
			
			// Test realtime forex quotes
			_apiService.subscribeToSymbol();
		}
	}
}
