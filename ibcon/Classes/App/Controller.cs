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
			// Stocks, futures events
			_webSocketService.onMessage += (object sender, WebSocketServiceEventArgs args) => _apiService.historyBarsLoad(args.clientId, args.symbol, args.currency, args.queryTime, args.duration, args.timeFrame);
			_webSocketService.onSubscribe += (object sender, WebSocketServiceEventArgs args) => _apiService.subscribeToSymbol(args.clientId, args.symbol, args.currency);
			_webSocketService.onPlaceOrder += (object sender, WebSocketServiceEventArgs args) => _apiService.placeOrder(args.symbol, args.currency, args.direction, args.volume);
			// FX events
			_webSocketService.onMessageFx += (object sender, WebSocketServiceEventArgs args) => _apiService.historyBarsLoadFx(args.clientId, args.symbol, args.currency, args.queryTime, args.duration, args.timeFrame);
			_webSocketService.onSubscribeFx += (object sender, WebSocketServiceEventArgs args) => _apiService.subscribeToSymbolFx(args.clientId, args.symbol, args.currency);
			_webSocketService.onPlaceOrderFx += (object sender, WebSocketServiceEventArgs args) => _apiService.placeOrderFx(args.symbol, args.currency, args.direction, args.volume);
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
