using System;

namespace Pattern
{
	/*
	 * Design patern of the developing project.
	 * The listed below code does nothing itself. It just a pattern mockup. 
	 * Pattern type: View - Model - View. 
	 * The application consumes api messages (realtime stick quotes) from IB Gateway (View).
	 * Parses it (Model).
	 * Parse it to Laravel php application located at the same host (View).
	 * Architeture diagram: 
	 * 
	 * App features except api messages parsing:
	 * 1. Historical data request. 
	 * 2. DB storage
	 * 3. Quote streams subscription/unsubscription
	 * 4. 
	 * 
	 * Todo/questions:
	 * 1. Where to store api response messages classes? 
	 * 2. Which method is called in static void Main()? It is empty now. 
	 * 3. Where to store Fleck/websocket handle class? 
	 */
	class Program
	{
		// Main static method
		static void Main(string[] args)
		{
			// New classes instances 

			// Call method. Which one? 
			// App.Run
		}
	}

	// View class
	public class View
	{
		private readonly Controller _controller;

		public ViewResponseDto1 Request1(ViewRequestDto1 @params)
		{
			var result = _controller.Method1(@params.Param1, @params.Param2);
			return new ViewResponseDto1 { Success = result };
		}
	}

	// Request class 1
	public class ViewRequestDto1
	{
		public string Param1 { get; set; }
		public int Param2 { get; set; }
	}

	// Request class 2
	public class ViewResponseDto1
	{
		public bool Success { get; set; }
	}

	// Controller class
	public class Controller
	{
		private readonly ApiService _apiService;
		private readonly DbService _dbService;

		// Constructor
		// Takes two services as parameters. There can be more than two.
		public Controller(ApiService apiService, DbService dbService)
		{
			_apiService = apiService;
			_dbService = dbService;
		}

		// Method call
		// IB API gateway connect here?
		public bool Method1(string param1, int param2)
		{
			try
			{
				_apiService.Method1(param1, param2);
				_dbService.Method1(param1, DateTimeOffset.UtcNow);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}
	}

	/*
	 * External API  handling.
	 * IB gateway connection and message consumption (subscription).
	 * Resources uesd:
	 * 1. CSharpAPI_9.73.06. Referenced as a project. 
	 * 2. IBSampleApp. 
	 * Download: http://interactivebrokers.github.io/# 
	 * Github reference: http://interactivebrokers.github.io/tws-api/  
	 */
	public class ApiService
	{
		// IB events go here
		public void Method1(string param1, int param2)
		{

		}
	}

	/* 
	 * DB handling.
	 * Historical data storage. 
	 * 
	 */
	public class DbService
	{
		public void Method1(string param1, DateTimeOffset timestamp)
		{

		}
	}
}
