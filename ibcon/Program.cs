using System;
 
namespace ibcon
{
	class Program
	{
		private static MainController controller;
		static void Main(string[] args)
		{
			controller = new MainController();
			controller.Index();
		}
	}
}
