using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace IBcon.Classes
{
	/*
	 * Ib client requires sync context (IBClient.cs line 144) which only fires in winform application type.
	 * Now we need to trigger it manually. 
	 * sc = SynchronizationContext.Current;
	 * @see https://veganhunter.net/2016/12/02/synchronizationcontext-in-console-applications/
	 */
	public class CustomSynchronizationContext : SynchronizationContext
	{
		public override void Post(SendOrPostCallback action, object state)
		{
			SendOrPostCallback actionWrap = (object state2) =>
			{
				SynchronizationContext.SetSynchronizationContext(new CustomSynchronizationContext());
				action.Invoke(state2);
			};
			var callback = new WaitCallback(actionWrap.Invoke);
			ThreadPool.QueueUserWorkItem(callback, state);
		}
		public override SynchronizationContext CreateCopy()
		{
			return new CustomSynchronizationContext();
		}
		public override void Send(SendOrPostCallback d, object state)
		{
			base.Send(d, state);
		}
		public override void OperationStarted()
		{
			base.OperationStarted();
		}
		public override void OperationCompleted()
		{
			base.OperationCompleted();
		}
	}
}
