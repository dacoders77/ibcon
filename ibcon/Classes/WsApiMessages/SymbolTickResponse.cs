using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace IBcon.Classes.WsApiMessages
{
	/* Real-time symbol tick object. 
	 * Subscription: reqMktData
	 * @see https://interactivebrokers.github.io/tws-api/classIBApi_1_1EClient.html#a7a19258a3a2087c07c1c57b93f659b63
	 * Sent to open websocket connection and received in Ratchet.php
	 */
	public class SymbolTickResponse
	{
		public double symbolTickPrice; 
		public int clientId; // Request client id. Comes from PHP and returned back.

		// Constructor
		public SymbolTickResponse() // Constructor 
		{
			//ResponseList = new List<BarObject>();
		}

		public string ReturnJson()
		{
			var obj = new ResponseObjectTickPrice // Create object
			{
				clientId = 547841, // How to get this client id? 547841 - is pusher app id from .env
				messageType = "SymbolTickPriceResponse",
				symbolTickPrice = symbolTickPrice,
				symbolTickTime = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds()
			};

			var json = JsonConvert.SerializeObject(obj); // Serialize object (convert to json)
			return json;
		}
	}

	public class ResponseObjectTickPrice
	{
		public int clientId;
		public string messageType;
		public double symbolTickPrice;
		public long symbolTickTime;
	}
}


