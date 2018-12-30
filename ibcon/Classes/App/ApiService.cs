using System;
using System.Collections.Generic;
using System.Text;
using IBSampleApp;
using IBApi;
using System.Threading;
using IBcon.Classes.WsApiMessages;
using System.Globalization;

namespace IBcon.Classes.App
{
	/* IB Gateway API connection and events (price tick, history bars etc.) handling.  
	 * Connect to the IB Gatway located in a docker container. 
	 */

	// Delegate
	public delegate void ApiMessageEventHandler(object sender, ApiServiceEventArgs args);

	public class ApiService
	{
		// IB API variables
		public IBClient ibClient;
		private EReaderMonitorSignal signal;
		// Other
		private int initialNextValidOrderID;
		// Flags
		private bool isConnected = false; // Connection flag. Prevents connect button click when connected
		// Next order ID
		public int nxtOrderID
		{
			get
			{
				initialNextValidOrderID++;
				return initialNextValidOrderID;
			}
		}

		// Logger
		private readonly Log _log;

		// Events
		public event ApiMessageEventHandler onConnection;
		public event ApiMessageEventHandler onHistoryBarsEnd;

		// API response objects
		private HistoryBarsLoadResponse historyBarsLoadResponse;
		

		// Constructor
		public ApiService(Log log) {
			// Create new instance of IBClient
			signal = new EReaderMonitorSignal();
			ibClient = new IBClient(signal);

			// Logger
			_log = log;

			// Api response messages
			historyBarsLoadResponse = new HistoryBarsLoadResponse();
		}

		// Test rise event method. Delete it
		public void RiseEvent() {
			onConnection(this, new ApiServiceEventArgs { Text = "Manual force event rise from ApiService.cs"}); // Trigger event
		}

		// Start method
		public void Start() {
			// Event links
			ibClient.CurrentTime += IbClient_CurrentTime; // Get exchnage current time 
			// ibClient.MarketDataType += IbClient_MarketDataType;
			ibClient.Error += IbClient_Error; // Errors handling
			ibClient.TickPrice += IbClient_TickPrice; // reqMarketData. EWrapper Interface
			ibClient.OrderStatus += IbClient_OrderStatus; // Status of a placed order
			ibClient.NextValidId += IbClient_NextValidId; // Fires when api is connected. Connection status received here
			
			ibClient.HistoricalData += IbClient_HistoricalData; // History bars
			ibClient.HistoricalDataEnd += IbClient_HistoricalDataEnd; // End transmission confirmation

			// Get connected
			IBGatewayConnect();
		}

		private void IBGatewayConnect() 
		{
			try
			{
				ibClient.ClientId = 2; // Client id. Multiple clients can be connected to the same gateway with the same login/password
				ibClient.ClientSocket.eConnect(Settings.dbHost, Settings.ibGateWayPort, ibClient.ClientId);

				// Create a reader to consume messages from the TWS. The EReader will consume the incoming messages and put them in a queue
				var reader = new EReader(ibClient.ClientSocket, signal);
				reader.Start();

				// Once the messages are in the queue, an additional thread can be created to fetch them
				new Thread(() =>
				{ while (ibClient.ClientSocket.IsConnected()) { signal.waitForSignal(); reader.processMsgs(); } })
				{ IsBackground = true }.Start(); // https://interactivebrokers.github.io/tws-api/connection.html#gsc.tab=0
			}
			catch (Exception exception)
			{
				//ListViewLog.AddRecord(this.GetType().Name, "Connection failed. Check your connection credentials. Exception: " + exception);
				_log.Add("Connection failed. Check your connection credentials. Exception: " + exception);
			}

			// Request exchnage current time
			try
			{
				ibClient.ClientSocket.reqCurrentTime();
			}
			catch (Exception exception)
			{
				_log.Add("req time. Exception: " + exception);
			}
		}

		private void IbClient_CurrentTime(long obj) // Get exchnage current time event
		{
			//ListViewLog.AddRecord("ApiService.cs", "Exchange current time:" + UnixTimeStampToDateTime(obj).ToString());
			_log.Add("Exchange current time:" + UnixTimeStampToDateTime(obj).ToString());
		}

		private void IbClient_Error(int arg1, int arg2, string arg3, Exception arg4) // Errors handling event
		{
			if (arg4 != null) // Show exception if it is not null. There are errors with no exceptions
				Console.WriteLine(
					"ApiService.cs IbClient_Error" +
					"link: " + arg4.HelpLink + "\r" +
					"result" + arg4.HResult + "\r" +
					"inner exception: " + arg4.InnerException + "\r" +
					"message: " + arg4.Message + "\r" +
					"source: " + arg4.Source + "\r" +
					"stack trace: " + arg4.StackTrace + "\r" +
					"target site: " + arg4.TargetSite + "\r"
					);

			// Must be carefull with these ticks! While debugging - disable this filter. Otherwise you can miss important information 
			// https://interactivebrokers.github.io/tws-api/message_codes.html
			// 2104 - A market data farm is connected.
			// 2108 - A market data farm connection has become inactive but should be available upon demand.
			// 2106 - A historical data farm is connected. 
			// 10167 - Requested market data is not subscribed. Displaying delayed market data
			// .. Not all codes are listed

			if (arg2 != 2104 && arg2 != 2119 && arg2 != 2108 && arg2 != 2106 && arg2 != 10167)
			//if (true)
			{
				// arg1 - requestId
				//ListViewLog.AddRecord("ApiService.cs", "IbClient_Error: args: " + arg1 + " " + arg2 + " " + arg3 + "exception: " + arg4);
				_log.Add("IbClient_Error: args: " + arg1 + " " + arg2 + " " + arg3 + "exception: " + arg4);
				// id, code, text
				// A error can triggerd by any request: fx, quote or place order. Use place order for now
				//basket.UpdateInfoJson(string.Format("Place order error! Error text: {2} . Error code:{1}  RequestID: {0}. ibClient.NextOrderId: {3}", arg1, arg2, arg3, ibClient.NextOrderId), "placeOrder", "error", arg1, "placeorder_request_id"); // Update json info feild in DB
			}
		}

		private void IbClient_TickPrice(IBSampleApp.messages.TickPriceMessage obj) // ReqMktData. Get quote. Tick types https://interactivebrokers.github.io/tws-api/rtd_simple_syntax.html 
		{
			char requestCode = obj.RequestId.ToString()[obj.RequestId.ToString().Length - 1]; // First char is the code. C# requests: 5 - fx, 6 - stock. PHP: 7 - stock
																							  // Tick types: Close - for FX quotes. DelayedCLose - for stock quotes
																							  //ListViewLog.AddRecord("ApiService.cs line 215", "IbClient_TickPrice. Price " + obj.Price + " requestId: " + obj.RequestId + " tick type: " + TickType.getField(obj.Field));
			_log.Add("IbClient_TickPrice. Price " + obj.Price + " requestId: " + obj.RequestId + " tick type: " + TickType.getField(obj.Field));

			// FX quote. C# while executing a basket
			// When a fx quote is received, ExecuteBasketThread() checks it and requests a stock quote
			if (TickType.getField(obj.Field) == "close") // bidPrice = -1. This value returned when market is closed. https://interactivebrokers.github.io/tws-api/md_receive.html
			{
				/*
				basket.assetForexQuote = obj.Price;
				ListViewLog.AddRecord(this, "brokerListBox", "ApiService.cs line 210", "IbClient_TickPrice. FX Quote: " + obj.Price + " " + obj.RequestId, "yellow");

				basket.UpdateInfoJson(string.Format("FX quote successfully recevied. FX quote: {0}. RequestID: {1}", obj.Price, obj.RequestId), "fxQuoteRequest", "ok", obj.RequestId, "fx_request_id"); // Update json info feild in DB
				basket.addForexQuoteToDB(obj.RequestId, obj.Price); // Update fx quote in the BD
				*/
			}

			// For stock. A request from PHP. While adding an asset to a basket
			// In this case we do not record this price to the DB. It is recorded from PHP
			if (TickType.getField(obj.Field) == "delayedLast" && requestCode.ToString() == "7") // PHP. Stock quote request
			{

				//ListViewLog.AddRecord("ApiService.cs line 221", "IbClient_TickPrice. PHP req. price: " + obj.Price + " reqId: " + obj.RequestId);
				_log.Add("IbClient_TickPrice. PHP req. price: " + obj.Price + " reqId: " + obj.RequestId);

				/*
				quoteResponse.symbolPrice = obj.Price;
				quoteResponse.symbolName = apiManager.symbolPass; // We have to store symbol name and basket number as apiManager fields. Symbol name is not returned with IbClient_TickPrice response as well as basket number. Then basket number will be returnet to php and passed as the parameter to Quote.php class where price field will be updated. Symbol name and basket number are the key
				quoteResponse.basketNum = apiManager.basketNumber; // Pass basket number to api manager. First basket number was assigned to a class field basketNumber of apiManager class 

				foreach (var socket in allSockets.ToList()) // Loop through all connections/connected clients and send each of them a message
				{
					socket.Send(quoteResponse.ReturnJson());
				}
				*/
			}

			// C#. ApiManager stock quote request
			if (TickType.getField(obj.Field) == "delayedLast" && requestCode.ToString() == "6")
			{
				// Updte quote value in DB
				//basket.UpdateStockQuoteValue(obj.RequestId, obj.Price, this);
				//ListViewLog.AddRecord("ApiService.cs", "IbClient_TickPrice. C# 6 req. price: " + obj.Price + " " + obj.RequestId);
				_log.Add("IbClient_TickPrice. C# 6 req. price: " + obj.Price + " " + obj.RequestId);

				//basket.UpdateInfoJson(string.Format("Stock quote successfully recevied. Stock quote: {0}. RequestID: {1}", obj.Price, obj.RequestId), "stockQuoteRequest", "ok", obj.RequestId, "quote_request_id"); // Update json info feild in DB
			}
		}

		private void IbClient_OrderStatus(IBSampleApp.messages.OrderStatusMessage obj)
		{
			//ListViewLog.AddRecord("ApiService.cs", "IbClient_OrderStatus. line 153. avgFillprice, filled, orderID, orderStatus: " + obj.AvgFillPrice + " | " + obj.Filled + " | " + obj.OrderId + " | " + obj.Status);
			_log.Add("IbClient_OrderStatus. line 153. avgFillprice, filled, orderID, orderStatus: " + obj.AvgFillPrice + " | " + obj.Filled + " | " + obj.OrderId + " | " + obj.Status);
			//basket.UpdateInfoJsonExecuteOrder(string.Format("Order executed! Info text: {0}", obj.Status), "executeOrder", "ok", obj.OrderId, obj.AvgFillPrice, obj.Filled); // Update json info feild in DB
		}

		private void IbClient_NextValidId(IBSampleApp.messages.ConnectionStatusMessage obj) // Api connection established
		{
			// TEST EVENT RISE
			// Subscription is performed in Controller.cs
			onConnection(this, new ApiServiceEventArgs { Text = "CONNECTED TO API! from ApiService.cs" }); // Trigger event


			initialNextValidOrderID = ibClient.NextOrderId; // Get initial value once. Then this value vill be encreased
			//ListViewLog.AddRecord("ApiService.cs", "API connected: " + obj.IsConnected + " Next valid req id: " + ibClient.NextOrderId + " ");
			_log.Add("API connected: " + obj.IsConnected + " Next valid req id: " + ibClient.NextOrderId);
			// 1 - Realtime, 2 - Frozen, 3 - Delayed data, 4 - Delayed frozen
			ibClient.ClientSocket.reqMarketDataType(3); // https://interactivebrokers.github.io/tws-api/classIBApi_1_1EClient.html#ae03b31bb2702ba519ed63c46455872b6 
			
			/*
			isConnected = true;
			if (obj.IsConnected)
			{
				status_CT.Text = "Connected";
				button13.Text = "Disconnect";
			}
			*/

			// 1 - Realtime, 2 - Frozen, 3 - Delayed data, 4 - Delayed frozen
			ibClient.ClientSocket.reqMarketDataType(3); // https://interactivebrokers.github.io/tws-api/classIBApi_1_1EClient.html#ae03b31bb2702ba519ed63c46455872b6 
		}

		private static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
		{
			System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
			dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
			return dtDateTime;
		}

		// Fetch historical bars 
		// Bars are returned to IbClient_HistoricalData one by one
		public void historyBarsLoad(string symbol) {
			// HISTORY BARS REQUEST TEST
			// @see https://interactivebrokers.github.io/tws-api/historical_bars.html 
			// EurGbpFx

			Contract contract = new Contract();
			contract.Symbol = symbol; // "EUR"
			contract.SecType = "CASH";
			contract.Currency = "USD";
			contract.Exchange = "IDEALPRO";

			try
			{
				string queryTime = DateTime.Now.AddHours(-1).ToString("yyyyMMdd HH:mm:ss");
				ibClient.ClientSocket.reqHistoricalData(4001, contract, queryTime, "1 D", "30 mins", "MIDPOINT", 1, 1, false, null);

				//ibClient.ClientSocket.reqHistoricalData(4002, ContractSamples.EuropeanStock(), queryTime, "10 D", "1 min", "TRADES", 1, 1, false, null);
			}
			catch (Exception exception)
			{
				//ListViewLog.AddRecord("ApiService.cs", "reqHistoricalData. Exception: " + exception);
				_log.Add("reqHistoricalData. Exception: " + exception);
			}
		}

		// Fires up on each bar transmission
		// @see https://stackoverflow.com/questions/17632584/how-to-get-the-unix-timestamp-in-c-sharp/35425123
		private void IbClient_HistoricalData(IBSampleApp.messages.HistoricalDataMessage bar)
		{
			//Console.WriteLine("HistoricalData. RequestId: " + bar.RequestId + " Time: " + bar.Date + ", Open: " + bar.Open + ", High: " + bar.High + ", Low: " + bar.Low + ", Close: " + bar.Close + ", Volume: " + bar.Volume + ", Count: " + bar.Count + ", WAP: " + bar.Wap);
			Console.Write(bar.Open);
			historyBarsLoadResponse.ResponseList.Add(new BarObject {
				date = DateTime.ParseExact(bar.Date, "yyyyMMdd  HH:mm:ss", null).ToString("yyyy-MM-dd HH:mm:ss"),
				time_stamp = ((DateTimeOffset)DateTime.ParseExact(bar.Date, "yyyyMMdd  HH:mm:ss", null)).ToUnixTimeSeconds() * 1000,
				open = bar.Open,
				close = bar.Close,
				high = bar.High,
				low = bar.Low,
				volume = bar.Volume
			});
		}

		// Fires once when transmission is over
		private void IbClient_HistoricalDataEnd(IBSampleApp.messages.HistoricalDataEndMessage obj)
		{
			_log.Add("**** BARS END!" + obj);
			//_log.Add("**** JSON: " + historyBarsLoadResponse.ReturnJson());
			// Fire event and send obj to WS
			onHistoryBarsEnd(this, new ApiServiceEventArgs { HistoryBarsJsonString = historyBarsLoadResponse.ReturnJson() });
			historyBarsLoadResponse.ResponseList.Clear(); // Empty list 
		}
	}
}
