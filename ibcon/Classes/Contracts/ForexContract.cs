using System;
using System.Collections.Generic;
using System.Text;
using IBApi;

namespace IBcon.Classes.Contracts
{
	/*
	 * https://interactivebrokers.github.io/tws-api/basic_contracts.html 
	 */
	public class ForexContract : Contract
	{
		// Constructor
		public ForexContract() {
			SecType = "CASH";
			Exchange = "IDEALPRO";
		}
	}
}
