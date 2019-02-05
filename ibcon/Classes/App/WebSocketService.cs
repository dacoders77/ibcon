using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fleck; // Socket server

namespace IBcon.Classes.App
{
	/* Provides websocket connection functionality for C# - Laravel App bridge. 
	 * Listens to requests from PHP and transfer them to IB gatway which is loaced in a docker container on linux host.
	 * Responses are parsed, converted to json and sent back to Laravel PHP app.
	 */

	// Delegate
	public delegate void WebSocketMessageEventHandler(object sender, WebSocketServiceEventArgs args);

	class WebSocketService
	{
		private List<IWebSocketConnection> _allSockets; // The list of all connected clients to the websocket server
		private WebSocketServer _server;
		private Log _log;
		public event WebSocketMessageEventHandler onMessage;
		public event WebSocketMessageEventHandler onSubscribe;

		// Constructor
		public WebSocketService(Log log) {
			FleckLog.Level = LogLevel.Error; // LogLevel.Debug
			_allSockets = new List<IWebSocketConnection>();
			_server = new WebSocketServer("ws://0.0.0.0:8181");
			_log = log;
		}

		public void Start() {
			_server.SupportedSubProtocols = new[] { "superchat", "chat" };
			_server.Start(socket =>
			{
				socket.OnOpen = () => onWebSocketOpen(socket);
				socket.OnClose = () => _allSockets.Remove(socket);
				socket.OnMessage = message => onWebSocketMessage(message);
			});
		}

		public void SendToWsStream(string jsonString) {
			_log.Add("**************SSDS Send to open WS stream: " + jsonString);
			foreach (var socket in _allSockets.ToList()) socket.Send(jsonString); // Loop through all connections/connected clients and send each of them a message
		}

		private void onWebSocketOpen(IWebSocketConnection socket) {
			_log.Add("Websocket connection open!");
			_allSockets.Add(socket);
			foreach (var socket2 in _allSockets.ToList()) socket2.Send("Hello from C# websocket! WebSocketService.cs");
		}

		private void onWebSocketMessage(string message) {
			_log.Add("A message received from a client: " + message);
			_allSockets.ToList().ForEach(s => s.Send("Hello from C# websocket! WebSocketService.cs"));

			var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(message);
			var requestBody = jsonObject["body"];

			switch (jsonObject["requestType"].ToString())
			{
				case "historyLoad":
					onMessage(this, new WebSocketServiceEventArgs
					{
						clientId = (int)jsonObject["clientId"],
						symbol = requestBody["symbol"].ToString(),
						currency = requestBody["currency"].ToString(),
						queryTime = requestBody["queryTime"].ToString(),
						duration = requestBody["duration"].ToString(),
						timeFrame = requestBody["timeFrame"].ToString()
					});
					break;
				case "subscribeToSymbol":
					//_log.Add("----------------SUBSCRIBE EVENT GGHHFF");
					// call onSubscribe
					// Unsubscribe from previous request using the same request id 12345
					// call subscription.
					// IF: price = -1 -> send error message
					onSubscribe(this, new WebSocketServiceEventArgs
					{
						clientId = (int)jsonObject["clientId"],
						symbol = requestBody["symbol"].ToString(),
						currency = requestBody["currency"].ToString(),
						//queryTime = requestBody["queryTime"].ToString(),
						//duration = requestBody["duration"].ToString(),
						//timeFrame = requestBody["timeFrame"].ToString()
					});
					break;

				//case "GetHistoryBars":
					//apiManager.GetQuote(requestBody["symbol"].ToString(), (int)requestBody["basketNumber"], requestBody["currency"].ToString());
					// Fire get history bars event
					//break;
			}
		}
	}
}
