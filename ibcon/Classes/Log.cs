using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace IBcon.Classes
{
	/* Simple logger class. Shows time of the message, source file, line number and caller method */
	public class Log
	{
		public void Add(string message, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, [System.Runtime.CompilerServices.CallerFilePath] string path = null)
		{
			Console.WriteLine(DateTime.Now + " " + message + " Class: " + path + " Line number: " + lineNumber + " Caller: (" + caller + ") ");
		}
	}
}
