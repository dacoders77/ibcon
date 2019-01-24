using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IBcon.Classes.WsApiMessages
{
	class InfoResponse
	{
		public string infoText;

		// Constructor
		public InfoResponse() // Constructor 
		{
			//
		}

		public string ReturnJson()
		{
			var obj = new ResponseObjectInfo // Create object
			{
				clientId = 547841, // How to get this client id? 547841 - is pusher app id from .env
				messageType = "Info",
				infoText = infoText
			};

			var json = JsonConvert.SerializeObject(obj); // Serialize object (convert to json)
			return json;
		}
	}

	public class ResponseObjectInfo
	{

		public int clientId;
		public string messageType;
		public string infoText;
	}
}
