﻿using System;
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
		private Order order;
		// Other
		private int initialNextValidOrderID;
		// Flags
		private bool isConnected = false; // Connection flag. Prevents connect button click when connected
		// Next order ID
		private int nxtOrderID
		{
			get
			{
				initialNextValidOrderID++;
				return initialNextValidOrderID;
			}
		}
		// Current subscribtion orderId
		int currOrderId = 0;

		// Logger
		private readonly Log _log;

		// Events
		public event ApiMessageEventHandler onHistoryBarsEnd;
		public event ApiMessageEventHandler onSymbolTick;
		public event ApiMessageEventHandler onError;
		public event ApiMessageEventHandler onInfo;

		// API response objects
		private HistoryBarsLoadResponse _historyBarsLoadResponse;
		private SymbolTickResponse _symbolTickResponse;
		private ErrorResponse _errorResponse;
		private InfoResponse _infoResponse;

		// Constructor
		public ApiService(Log log) {
			// Create new instance of IBClient
			signal = new EReaderMonitorSignal();
			ibClient = new IBClient(signal);

			// Logger
			_log = log;

			// Api Response objects instances
			_historyBarsLoadResponse = new HistoryBarsLoadResponse();
			_symbolTickResponse = new SymbolTickResponse();
			_errorResponse = new ErrorResponse();
			_infoResponse = new InfoResponse();
			
		}

		// Start method
		public void Start() {
			ibClient.CurrentTime += IbClient_CurrentTime; // Get exchnage current time 
			ibClient.MarketDataType += IbClient_MarketDataType; ;
			ibClient.Error += IbClient_Error; // Errors handling
			
			ibClient.OrderStatus += IbClient_OrderStatus; // Status of a placed order
			ibClient.NextValidId += IbClient_NextValidId; // Fires when api is connected. Connection status received here
			
			ibClient.HistoricalData += IbClient_HistoricalData; // History bars
			ibClient.HistoricalDataEnd += IbClient_HistoricalDataEnd; // End transmission confirmation

			ibClient.TickPrice += IbClient_TickPrice; // reqMarketData. EWrapper Interface. Real-time ticks
			ibClient.TickString += IbClient_TickString; // Tick epoch time. Tick #45. http://interactivebrokers.github.io/tws-api/tick_types.html

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
			_log.Add("Exchange current time:" + UnixTimeStampToDateTime(obj).ToString());
		}

		private void IbClient_Error(int arg1, int arg2, string arg3, Exception arg4) // Errors handling event
		{
			//_infoResponse.infoText = "Info test message"; // Works good
			// onInfo(this, new ApiServiceEventArgs { InfoText = _infoResponse.ReturnJson() });

			// Show exception if it is not null. There are errors with no exceptions
			if (arg4 != null) {
				string errorString = "ApiService.cs IbClient_Error" +
						"link: " + arg4.HelpLink + "\r" +
						"result" + arg4.HResult + "\r" +
						"inner exception: " + arg4.InnerException + "\r" +
						"message: " + arg4.Message + "\r" +
						"source: " + arg4.Source + "\r" +
						"stack trace: " + arg4.StackTrace + "\r" +
						"target site: " + arg4.TargetSite + "\r";
				_errorResponse.errorText = errorString;
				onError(this, new ApiServiceEventArgs { ErrorText = _errorResponse.ReturnJson() });
				_log.Add(errorString);
			}
			
			// Must be carefull with these ticks! While debugging - disable this filter. Otherwise you can miss important information 
			// https://interactivebrokers.github.io/tws-api/message_codes.html
			// 2104 - A market data farm is connected.
			// 2108 - A market data farm connection has become inactive but should be available upon demand.
			// 2106 - A historical data farm is connected. 
			// 10167 - Requested market data is not subscribed. Displaying delayed market data
			// .. Not all codes are listed
			if (arg2 != 2104 && arg2 != 2119 && arg2 != 2108 && arg2 != 2106 && arg2 != 10167)
			{
				string errorString = "IbClient_Error: args: " + arg1 + " " + arg2 + " " + arg3 + " exception: " + arg4;
				_errorResponse.errorText = errorString;
				onError(this, new ApiServiceEventArgs { ErrorText = _errorResponse.ReturnJson() });
				_log.Add(errorString);
				
				// id, code, text
				// A error can triggerd by any request: fx, quote or place order. Use place order for now
				//basket.UpdateInfoJson(string.Format("Place order error! Error text: {2} . Error code:{1}  RequestID: {0}. ibClient.NextOrderId: {3}", arg1, arg2, arg3, ibClient.NextOrderId), "placeOrder", "error", arg1, "placeorder_request_id"); // Update json info feild in DB
			}
		}

		private void IbClient_OrderStatus(IBSampleApp.messages.OrderStatusMessage obj)
		{
			_log.Add("IbClient_OrderStatus. line 153. avgFillprice, filled, orderID, orderStatus: " + obj.AvgFillPrice + " | " + obj.Filled + " | " + obj.OrderId + " | " + obj.Status);
			//basket.UpdateInfoJsonExecuteOrder(string.Format("Order executed! Info text: {0}", obj.Status), "executeOrder", "ok", obj.OrderId, obj.AvgFillPrice, obj.Filled); // Update json info feild in DB
		}

		private void IbClient_NextValidId(IBSampleApp.messages.ConnectionStatusMessage obj) // Api connection established
		{
			initialNextValidOrderID = ibClient.NextOrderId; // Get initial value once. Then this value vill be encreased. Used for each new request
			//List("API connected: " + obj.IsConnected + " Next valid req id: " + ibClient.NextOrderId + " ");

			_log.Add("API connected: " + obj.IsConnected + " Next valid req id: " + ibClient.NextOrderId);
			// 1 - Realtime, 2 - Frozen, 3 - Delayed data, 4 - Delayed frozen
			ibClient.ClientSocket.reqMarketDataType(3); // https://interactivebrokers.github.io/tws-api/classIBApi_1_1EClient.html#ae03b31bb2702ba519ed63c46455872b6 
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
		// @see https://interactivebrokers.github.io/tws-api/historical_bars.html 
		public void historyBarsLoad(int clientId, string symbol, string currency, string queryTime, string duration, string timeFrame) {

			//Contracts.ForexContract contract = new Contracts.ForexContract();
			Contract contract;
			contract = new Contract();
			contract.SecType = "STK";
			contract.Currency = "USD";
			contract.Exchange = "SMART";
			contract.Symbol = symbol; // EUR
			contract.Currency = currency; // USD

			try
			{
				//string queryTime = DateTime.Now.AddHours(-1).ToString("yyyyMMdd HH:mm:ss");
				//ibClient.ClientSocket.reqHistoricalData(4001, contract, queryTime, "1 D", "15 mins", "MIDPOINT", 1, 1, false, null); // "1 min"
				//ibClient.ClientSocket.reqHistoricalData(clientId, contract, queryTime, duration, timeFrame, "MIDPOINT", 1, 1, false, null); 
				ibClient.ClientSocket.reqHistoricalData(nxtOrderID, contract, queryTime, duration, timeFrame, "MIDPOINT", 1, 1, false, null);

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
			Console.Write(bar.Open + "|");
			_historyBarsLoadResponse.ResponseList.Add(new BarObject {
				date = DateTime.ParseExact(bar.Date, "yyyyMMdd  HH:mm:ss", null).ToString("yyyy-MM-dd HH:mm:ss"),
				time_stamp = ((DateTimeOffset)DateTime.ParseExact(bar.Date, "yyyyMMdd  HH:mm:ss", null)).ToUnixTimeSeconds() * 1000,
				open = bar.Open,
				close = bar.Close,
				high = bar.High,
				low = bar.Low,
				volume = bar.Volume
			});
		}

		// Fires once when historical bars transmission is over
		private void IbClient_HistoricalDataEnd(IBSampleApp.messages.HistoricalDataEndMessage obj)
		{
			_log.Add("**** BARS END!" + obj.RequestId);
			_historyBarsLoadResponse.clientId = obj.RequestId; // Set clientId 
			onHistoryBarsEnd(this, new ApiServiceEventArgs { HistoryBarsJsonString = _historyBarsLoadResponse.ReturnJson() });
			_historyBarsLoadResponse.ResponseList.Clear(); // Empty list 

			_infoResponse.infoText = "Historical bars transmission finished";
			onInfo(this, new ApiServiceEventArgs { InfoText = _infoResponse.ReturnJson() });
		}

		// Sybscribe to trades 
		// @see https://interactivebrokers.github.io/tws-api/classIBApi_1_1EClient.html#a7a19258a3a2087c07c1c57b93f659b63 
		public void subscribeToSymbol(int clientId, string symbol, string currency) {
			if (currOrderId != 0)
				ibClient.ClientSocket.cancelMktData(currOrderId); // Unsubscribe first

			//Contracts.ForexContract contract = new Contracts.ForexContract();
			//Contracts.StockContract contract = new Contracts.StockContract();

			Contract contract;
			contract = new Contract();
			contract.SecType = "STK";
			contract.Currency = "USD";
			contract.Exchange = "SMART";
			contract.Symbol = symbol; // EUR, AAPL
			contract.Currency = currency; // USD

			try
			{
				//ibClient.ClientSocket.reqMktData(clientId, contract, string.Empty, false, false, new List<TagValue>());
				int orderIdTemp = nxtOrderID;
				currOrderId = orderIdTemp;
				ibClient.ClientSocket.reqMktData(orderIdTemp, contract, string.Empty, false, false, new List<TagValue>());

			}
			catch (Exception exception)
			{
				_log.Add("reqHistoricalData. Exception: " + exception);
			}
		}

		// Market data event (real-time trades)
		// @see price = -1 error: https://interactivebrokers.github.io/tws-api/md_receive.html 
		private void IbClient_TickPrice(IBSampleApp.messages.TickPriceMessage obj)
		{
			if (obj.Price != -1)
			{
				//_symbolTickResponse.clientId = 12345; // No need to set it here
				_symbolTickResponse.symbolTickPrice = obj.Price;
				onSymbolTick(this, new ApiServiceEventArgs { SymbolTickPrice = _symbolTickResponse.ReturnJson() });
			}
			else {
				_errorResponse.errorText = "SymbolTickPriceError. Exchange returned Price = -1. this indicates that there is no data currently available. Most commonly this occurs when requesting data from markets that are closed. ApiService.cs. https://interactivebrokers.github.io/tws-api/md_receive.html";
				onError(this, new ApiServiceEventArgs { ErrorText = _errorResponse.ReturnJson() });
			}
		}

		// Epoch time
		private void IbClient_TickString(int arg1, int arg2, string arg3)
		{
			//_log.Add("OOPNN: " + arg1 + " " + arg2 + " " + arg3);
		}

		// @see https://interactivebrokers.github.io/tws-api/market_data_type.html 
		private void IbClient_MarketDataType(IBSampleApp.messages.MarketDataTypeMessage obj)
		{
			switch (obj.MarketDataType)
			{
				case 1:
					_log.Add("Subscribed to LIVE market data");
					break;
				case 2:
					_log.Add("Subscribed to FROZEN market data");
					break;
				case 3:
					_log.Add("Subscribed to DELAYED market data");
					break;
				case 4:
					_log.Add("Subscribed to DELAYED-FROZEN market data");
					break;
			}
		}

		public void placeOrder(string symbol, string currency, string direction, double volume) {
			Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; // Unix time in milleseconds is used as an order id
			Contract contract;
			contract = new Contract();
			contract.SecType = "STK";
			contract.Exchange = "SMART";
			contract.Symbol = symbol; // EUR, AAPL
			contract.Currency = currency; // USD

			// Place order goes from here
			order = new Order();
			order.OrderId = unixTimestamp;
			order.Action = direction;
			order.OrderType = "MKT";
			order.TotalQuantity = volume;
			order.Tif = "DAY";

			// Console.WriteLine("PlaceOrder. ApiManager.cs line 132. " + DateTime.Now.ToString("yyyy.MM.dd. HH:mm:ss:fff" + " ibClient.NextOrderId: " + ibClient.NextOrderId) + " " + contract.Symbol + " | " + contract.Currency);
			// form.basket.UpdateInfoJson(string.Format("Order placed. RequestID: {0}", requestId), "placeOrder", "ok", requestId, "placeorder_request_id"); // Update json info feild in DB
			ibClient.ClientSocket.placeOrder(unixTimestamp, contract, order);
		}

		// ******* FX METHODS *******

		// Fetch FX historical bars 
		// Bars are returned to IbClient_HistoricalData one by one
		// @see https://interactivebrokers.github.io/tws-api/historical_bars.html 
		public void historyBarsLoadFx(int clientId, string symbol, string currency, string queryTime, string duration, string timeFrame)
		{
			Contract contract;
			contract = new Contract();
			contract.SecType = "CASH";
			contract.Exchange = "IDEALPRO";
			contract.Symbol = symbol; // EUR
			contract.Currency = currency; // USD

			try
			{
				ibClient.ClientSocket.reqHistoricalData(nxtOrderID, contract, queryTime, duration, timeFrame, "MIDPOINT", 1, 1, false, null);
			}
			catch (Exception exception)
			{
				_log.Add("reqHistoricalData FX. Exception: " + exception);
			}
		}

		// Sybscribe to FX trades 
		// @see https://interactivebrokers.github.io/tws-api/classIBApi_1_1EClient.html#a7a19258a3a2087c07c1c57b93f659b63 
		public void subscribeToSymbolFx(int clientId, string symbol, string currency)
		{
			if (currOrderId != 0)
				ibClient.ClientSocket.cancelMktData(currOrderId); // Unsubscribe first

			Contract contract;
			contract = new Contract();
			contract.SecType = "CASH";
			contract.Exchange = "IDEALPRO";
			contract.Symbol = symbol; // EUR
			contract.Currency = currency; // USD

			try
			{
				int orderIdTemp = nxtOrderID;
				currOrderId = orderIdTemp;
				ibClient.ClientSocket.reqMktData(orderIdTemp, contract, string.Empty, false, false, new List<TagValue>());

			}
			catch (Exception exception)
			{
				_log.Add("subscribeToSymbolFx. Exception: " + exception);
			}
		}

		public void placeOrderFx(string symbol, string currency, string direction, double volume)
		{
			Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; // Unix time in milleseconds is used as an order id
			Contract contract;
			contract = new Contract();
			contract.SecType = "CASH";
			contract.Exchange = "IDEALPRO";
			contract.Symbol = symbol; // EUR
			contract.Currency = currency; // USD

			// Place order goes from here
			order = new Order();
			order.OrderId = unixTimestamp;
			order.Action = direction;
			order.OrderType = "MKT";
			order.TotalQuantity = volume;
			order.Tif = "DAY";
		
			ibClient.ClientSocket.placeOrder(unixTimestamp, contract, order);
		}
	}
}
