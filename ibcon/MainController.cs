using System;
using System.Collections.Generic;
using System.Text;
using Fleck; // Socket server
using System.Linq;
using IBApi; // Namespace from CSharpAPI_9.73.06 linked project
using IBSampleApp;
using System.Net;

// DELETE THIS CLASS!!!
namespace IBcon
{
	public partial class MainController
	{
		// IB API variables
		public IBClient ibClient;
		private EReaderMonitorSignal signal;
		//internal ApiManager apiManager; // Api features like search, place order, getQute etc.

		// Flags
		private bool isConnected = false; // Connection flag. Prevents connect button click when connected
		bool conntectButtonFlag = true; // Turns to false when connect button is clicked

		private List<IWebSocketConnection> allSockets; // The list of all connected clients to the websocket server 

		// Constructor
		public MainController() {
			// Fleck socket server 
			FleckLog.Level = LogLevel.Debug;
			allSockets = new List<IWebSocketConnection>();
			var server = new WebSocketServer("ws://0.0.0.0:8181");
		}

		public void Index() {
			Console.WriteLine("DB host: " + Classes.Settings.dbHost);

			// Fleck socket server 
			FleckLog.Level = LogLevel.Debug;
			allSockets = new List<IWebSocketConnection>();
			var server = new WebSocketServer("ws://0.0.0.0:8181");

			server.SupportedSubProtocols = new[] { "superchat", "chat" };
			server.Start(socket =>
			{
				socket.OnOpen = () =>
				{
					;
					//Log.Insert(DateTime.Now, "Form1.cs", string.Format("Websocket connection open!"), "white");
					allSockets.Add(socket);

					foreach (var socket2 in allSockets.ToList()) // Loop through all connections/connected clients and send each of them a message
					{
						socket2.Send("Putin had a press conferfence today! ");
					}

				};
				socket.OnClose = () =>
				{
					allSockets.Remove(socket);
				};
				socket.OnMessage = message =>
				{
					Console.WriteLine(message);
					// Output message to system log
					//Log.Insert(DateTime.Now, "Form1.cs", string.Format("socket.OnMessage. A message received from a client: {0}", message), "white");
					//allSockets.ToList().ForEach(s => s.Send("Hello from websocket! Form1.cs line 95")); // Send a greeting message to all websocket clients


					// DETERMINE WHAT TYPE OF API REQUEST IS RECEVIED HERE
					// apiManager.Search
					// apiManager.GetQuote

					/*

					var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(message);
					var requestBody = jsonObject["body"];

					
					switch (jsonObject["requestType"].ToString())
					{
						case "symbolSearch":
							apiManager.Search(requestBody["symbol"].ToString());
							break;
						case "getQuote":
							apiManager.GetQuote(requestBody["symbol"].ToString(), (int)requestBody["basketNumber"], requestBody["currency"].ToString());
							break;
						case "getAvailableFunds":
							ibClient.ClientSocket.reqAccountUpdates(true, "U2314623");
							break;
					}
					*/
				};
			});


			Console.WriteLine("Press any key to exit!");
			Console.ReadLine();

		}
	}
}
