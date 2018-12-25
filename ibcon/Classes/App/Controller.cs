using System;
using System.Collections.Generic;
using System.Text;

namespace IBcon.Classes.App
{
	public class Controller
	{
		// IB Gateway connection 
		private readonly ApiService _apiService;

		// Constructor
		public Controller() {
			_apiService = new ApiService();
		}

		// Method
		public void Run() {
			_apiService.Start();
		}

	}
}
