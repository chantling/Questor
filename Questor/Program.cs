
using Questor.Modules.Lookup;

namespace Questor
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Forms;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Timers;
	using System.Xml.Linq;
	using global::Questor.Modules.Activities;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Caching;
	using global::Questor.Modules.Logging;
	using global::Questor.Modules.Misc;
	using global::Questor.Modules.States;
	using global::Questor.Modules.Lookup;
	using DirectEve;
	

	public static class BeforeLogin
	{
		static BeforeLogin ()
		{
			Logging.UseInnerspace = false; //(defaults to true, will change to false IF passed -i Or -
		}
		
		private static Questor _questor;
		
		private static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
		
		private static void ParseArgs(IEnumerable<string> args)
		{
			if (!string.IsNullOrEmpty(Logging.EVELoginUserName) &&
			    !string.IsNullOrEmpty(Logging.EVELoginPassword) &&
			    !string.IsNullOrEmpty(Logging.MyCharacterName))
			{
				return;
			}

			OptionSet p = new OptionSet
			{
				"Usage: questor [OPTIONS]",
				"Run missions and make uber ISK.",
				"",
				"Options:",
				{"u|user=", "the {USER} we are logging in as.", v => Logging.EVELoginUserName = v},
				{"p|password=", "the user's {PASSWORD}.", v => Logging.EVELoginPassword = v},
				{"c|character=", "the {CHARACTER} to use.", v => Logging.MyCharacterName = v},
				{"n|loginNow", "Login using info in scheduler", v => LoginToEVE._loginNowIgnoreScheduler = v != null},
				{"h|help", "show this message and exit", v => LoginToEVE._showHelp = v != null}
			};

			try
			{
				LoginToEVE._QuestorParamaters = p.Parse(args);
			}
			catch (OptionException ex)
			{
				Logging.Log("Startup", "questor: ", Logging.White);
				Logging.Log("Startup", ex.Message, Logging.White);
				Logging.Log("Startup", "Try `questor --help' for more information.", Logging.White);
				return;
			}

			if (LoginToEVE._showHelp)
			{
				System.IO.StringWriter sw = new System.IO.StringWriter();
				p.WriteOptionDescriptions(sw);
				Logging.Log("Startup", sw.ToString(), Logging.White);
				return;
			}
		}

		
		public static void Main(string[] args)
		{
			ParseArgs(args);

			if (!string.IsNullOrEmpty(Logging.EVELoginUserName) && !string.IsNullOrEmpty(Logging.EVELoginPassword) && !string.IsNullOrEmpty(Logging.MyCharacterName))
			{
				LoginToEVE.ReadyToLoginToEVEAccount = true;
			}

			if (!LoginToEVE.LoadDirectEVEInstance()) return;
			
			Time.Instance.LoginStarted_DateTime = DateTime.UtcNow;			
			
			try
			{
				Logging.Log("Startup", "Launching Questor", Logging.Teal);
				_questor = new Questor();

				Logging.Log("Startup", "Launching QuestorUI", Logging.Teal);
				Application.Run(new QuestorUI());

				while (!Cleanup.SignalToQuitQuestor)
				{
					System.Threading.Thread.Sleep(50); 
				}

				Logging.Log("Startup", "Exiting Questor", Logging.Teal);

			}
			catch (Exception ex)
			{
				Logging.Log("Startup", "Exception [" + ex + "]", Logging.Teal);
			}
			finally
			{
				Cleanup.DirecteveDispose();
				AppDomain.Unload(AppDomain.CurrentDomain);
			}
			
		}
	}
}