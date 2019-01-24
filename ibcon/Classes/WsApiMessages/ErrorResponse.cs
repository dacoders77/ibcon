using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IBcon.Classes.WsApiMessages
{
	class ErrorResponse
	{
		public string errorText;

		// Constructor
		public ErrorResponse() // Constructor 
		{
			//
		}

		public string ReturnJson()
		{
			var obj = new ResponseObjectTickPriceError // Create object
			{
				clientId = 547841, // How to get this client id? 547841 - is pusher app id from .env
				messageType = "Error",
				errorText = errorText
			};

			var json = JsonConvert.SerializeObject(obj); // Serialize object (convert to json)
			return json;
		}
	}

	public class ResponseObjectTickPriceError{

		public int clientId;
		public string messageType;
		public string errorText;
	}

}
