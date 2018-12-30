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

		// Constructor
		public WebSocketService(Log log) {
			FleckLog.Level = LogLevel.Debug;
			_allSockets = new List<IWebSocketConnection>();
			_server = new WebSocketServer("ws://0.0.0.0:8181");
			_log = log;
		}

		public void Start() {
			Console.WriteLine("host: " + Classes.Settings.dbHost);

			_server.SupportedSubProtocols = new[] { "superchat", "chat" };
			_server.Start(socket =>
			{
				socket.OnOpen = () =>
				{
					_log.Add("Websocket connection open!");
					_allSockets.Add(socket);

					// Loop through all connections/connected clients and send each of them a message
					foreach (var socket2 in _allSockets.ToList()) 
					{
						socket2.Send("Putin had a press conferfence today! ");
					}

				};
				socket.OnClose = () =>
				{
					_allSockets.Remove(socket);
				};
				socket.OnMessage = message =>
				{
					_log.Add("A message received from a client: " + message);
					_allSockets.ToList().ForEach(s => s.Send("Hello from websocket! Form1.cs line 95"));

					// DETERMINE WHAT TYPE OF API REQUEST IS RECEVIED HERE
					// apiManager.Search
					// apiManager.GetQuote
					
					// Subscribe
					// Get history data

					
					var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(message);
					var requestBody = jsonObject["body"];

					// clientId can be pulled from settings
					// DO CLIENT ID TEST
					
					switch (jsonObject["requestType"].ToString())
					{
						case "historyLoad":
							//apiManager.Search(requestBody["symbol"].ToString());
							// Fire history load event
							//_log.Add("----------------AAAAAAAAAAA");
							onMessage(this, new WebSocketServiceEventArgs { Symbol = requestBody["symbol"].ToString() });
							break;

						case "GetHistoryBars":
							//apiManager.GetQuote(requestBody["symbol"].ToString(), (int)requestBody["basketNumber"], requestBody["currency"].ToString());
							// Fire get history bars event
							break;
					}
					
				};
			});
		}

		public void SendToWsStream(string historyBarsJsonString) {
			//_log.Add(historyBarsJsonString);

			foreach (var socket in _allSockets.ToList()) // Loop through all connections/connected clients and send each of them a message
			{
				socket.Send(historyBarsJsonString);
			}
		}
	}
}
