using System;
using IBcon.Classes.App;
using IBcon.Classes;
using System.Threading;
 
namespace IBcon
{
	class Program
	{
		private static MainController controller2;
		private static Controller controller;
		static void Main(string[] mainArgs)
		{
			//controller2 = new MainController();
			//controller.Index(); // Works good.

			var context = new CustomSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(context);

			controller = new Controller();
			controller.Run();

			// Catch .net low level errors and exceptions
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => Console.WriteLine("Program.cs exception. " + args.ExceptionObject.ToString());

			Console.WriteLine("Press any key to exit");
			Console.ReadLine();
		}
	}
}
