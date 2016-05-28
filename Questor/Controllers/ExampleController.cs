/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 17:31
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using Questor.Modules.Logging;

namespace Questor.Controllers
{
	/// <summary>
	/// Description of ExampleController.
	/// </summary>
	public class ExampleController : BaseController
	{
		enum ExampleControllerStates { Start, Print, End }

		
		ExampleControllerStates State { get; set; }

		public ExampleController()
		{
			Logging.Log("Starting a new DefaultController");
		}

		public override void DoWork()
		{
			if (IsWorkDone || LocalPulse > DateTime.UtcNow )
			{
				return;
			}
			
			switch (State)
			{
				case ExampleControllerStates.Start:
					Logging.Log("Start DefaultController");
					State = ExampleControllerStates.Print;
					break;
				case ExampleControllerStates.Print:
					
					State = ExampleControllerStates.End;
					break;
				case ExampleControllerStates.End:
					Logging.Log("End DefaultController");
					IsWorkDone = true;
					break;
			}
		}
	}
}
