using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

namespace IBcon.Classes.WsApiMessages
{
	/*
	 * Create a json object out of history bars IB API response. 
	 * Bars are delivered via api event trigger.
	 * Each bar is dispatched in a separate event. 
	 * When all bars are delvired - api end event is sent. 
	 * 
	 * Serialization class which creates a search response json object
	 * @see https://stackoverflow.com/questions/6201529/how-do-i-turn-a-c-sharp-object-into-a-json-string-in-net
	 */
	public class HistoryBarsLoadResponse
	{
		public List<BarObject> ResponseList;
		public int clientId; // Request client id. Comes from PHP and returned back.

		// Constructor
		public HistoryBarsLoadResponse() // Constructor 
		{
			ResponseList = new List<BarObject>();
		}

		public string ReturnJson()
		{
			var obj = new ResponseObject // Create object
			{
				clientId = clientId,
				messageType = "HistoryBarsLoadResponse",
				barsList = ResponseList.OrderBy(o => o.time_stamp).ToList() // @see https://stackoverflow.com/questions/3309188/how-to-sort-a-listt-by-a-property-in-the-object
			};
			var json = JsonConvert.SerializeObject(obj); // Serialize object (convert to json)
			return json;
		}
	}

	// Object structure
	public class BarObject
	{
		public string date;
		public long time_stamp;
		public double open;
		public double close;
		public double high;
		public double low;
		public double volume; 
	}

	public class ResponseObject
	{
		public int clientId;
		public string messageType;
		public List<BarObject> barsList;
	}
}
