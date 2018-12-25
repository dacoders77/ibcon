using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Schema;
using System.IO;

namespace IBcon.Classes
{
	public static class Settings
	{
		public static string dbHost { get; }
		public static string userId { get; }
		public static string password { get; }
		public static string database { get; }
		public static int ibGateWayPort { get; }
		public static string smtpLogin { get; }
		public static string smtpPassword { get; }
		public static string emailFromName { get; }
		public static string emailFromEmail { get; }
		public static string emailTo { get; }
		public static string emailToName { get; }
		public static string emailCopy { get; }
		public static string emailCopyName { get; }

		/**
		 * Store settings in json file.
		 */
		static Settings()
		{
			try
			{
				string jsonFromFile = File.ReadAllText("ibcon_settings.json"); // File must be located in Debug folder or next whre .exe is placed
				JSchema jsonParsed = JSchema.Parse(jsonFromFile);
				var x = Newtonsoft.Json.Linq.JObject.Parse(jsonFromFile);

				ibGateWayPort = (int)x["ibGateWayPort"]; // Type cast 
				dbHost = (string)x["dbHost"];
				smtpLogin = (string)x["smtpLogin"];
				smtpPassword = (string)x["smtpPassword"];
				emailFromName = (string)x["emailFromName"];
				emailFromEmail = (string)x["emailFromEmail"];
				emailTo = (string)x["emailTo"];
				emailToName = (string)x["emailToName"];
				emailCopy = (string)x["emailCopy"];
				emailCopyName = (string)x["emailCopyName"];
			}
			catch (Exception exception)
			{
				Console.WriteLine("Settings.cs. Error opening ibcon_settings.json: " + exception.Message);
			}
		}
	}
}
