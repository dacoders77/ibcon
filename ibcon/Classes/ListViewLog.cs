using System;
using System.Collections.Generic;
using System.Text;

namespace IBcon.Classes
{
	public static class ListViewLog
	{
		public static void AddRecord(object var1, string outputTo, string source, string payload, string color) {
			Console.WriteLine(source + " " + payload);
		}
	}
}
