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
			_apiService.onHistoryBarsEnd += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.HistoryBarsJsonString);
			_apiService.onSymbolTick += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.SymbolTickPrice); // real-time quotes
			_apiService.onError += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.ErrorText);
			_apiService.onInfo += (object sender, ApiServiceEventArgs args) => _webSocketService.SendToWsStream(args.InfoText);

			_webSocketService = new WebSocketService(_log);
			_webSocketService.onMessage += (object sender, WebSocketServiceEventArgs args) => _apiService.historyBarsLoad(args.clientId, args.symbol, args.currency, args.queryTime, args.duration, args.timeFrame);
			_webSocketService.onSubscribe += (object sender, WebSocketServiceEventArgs args) => _apiService.subscribeToSymbol(args.clientId, args.symbol, args.currency);
		}

		// Start method. Called from Program.cs
		public void Run() {
			_apiService.Start();
			_webSocketService.Start();
			
			// Test realtime forex quotes
			//_apiService.subscribeToSymbol();
		}
	}
}
