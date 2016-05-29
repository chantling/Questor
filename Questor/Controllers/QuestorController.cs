/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using DirectEve;
using Questor.Behaviors;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;
using System.Collections.Generic;

namespace Questor.Controllers
{
	public class QuestorController : BaseController, IDisposable
	{
		private static DateTime _nextPulse = DateTime.MinValue;
		private static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
		private static DateTime _lastSessionNotReady = DateTime.MinValue;
		private static Random _random = new Random();

		private readonly CombatMissionsBehavior _combatMissionsBehavior;
		private readonly Stopwatch _watch;

		private DateTime _lastQuestorPulse;

		private bool _runOnceAfterStartupalreadyProcessed;

		private int pulseDelay = 800;

		public QuestorController()
		{
			_lastQuestorPulse = DateTime.UtcNow;
			_combatMissionsBehavior = new CombatMissionsBehavior();
			_watch = new Stopwatch();
			Time.Instance.NextStartupAction = DateTime.UtcNow;
			_States.CurrentQuestorState = QuestorState.Idle;
			Time.Instance.StartTime = DateTime.UtcNow;
			Time.Instance.QuestorStarted_DateTime = DateTime.UtcNow;
			Settings.Instance.CharacterMode = "none";
		}

		public void RunOnceAfterStartup()
		{
			if (!_runOnceAfterStartupalreadyProcessed &&
			    DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15) &&
			    Cache.Instance.DirectEve.Session.CharacterId != null && Cache.Instance.DirectEve.Session.CharacterId > 0)
			{
				if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
				{
					_runOnceAfterStartupalreadyProcessed = true;

					Cache.Instance.IterateShipTargetValues("RunOnceAfterStartup");
					// populates ship target values from an XML
					Cache.Instance.IterateUnloadLootTheseItemsAreLootItems("RunOnceAfterStartup");
					// populates the list of items we never want in our local cargo (used mainly in unloadloot)

					MissionSettings.UpdateMissionName();
					Logging.MaintainConsoleLogs();
				}
				else
				{
					Logging.Log("Settings.Instance.CharacterName is still null");
					Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
					_runOnceAfterStartupalreadyProcessed = false;
					return;
				}
			}
		}

		public void DebugPerformanceClearandStartTimer()
		{
			_watch.Reset();
			_watch.Start();
		}

		public void DebugPerformanceStopandDisplayTimer(string whatWeAreTiming)
		{
			_watch.Stop();
			if (Logging.DebugPerformance)
				Logging.Log(" took " + _watch.ElapsedMilliseconds + "ms");
		}

		protected DateTime UTCNowAddDelay(int minDelayInSeconds, int maxDelayInSeconds)
		{
			return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInSeconds*1000, maxDelayInSeconds*1000));
		}

		public static bool TimeCheck()
		{
			if (DateTime.UtcNow < Time.Instance.NextTimeCheckAction)
				return false;

			Time.Instance.NextTimeCheckAction = DateTime.UtcNow.AddSeconds(90);

			if (Cache.Instance.ExitWhenIdle)
			{
				Logging.Log("ExitWhenIdle set to true.  Quitting game.");
				Cleanup.ReasonToStopQuestor = "ExitWhenIdle set to true";
				Settings.Instance.AutoStart = false;
				Cache.Instance.CloseQuestorCMDLogoff = false;
				Cache.Instance.CloseQuestorCMDExitGame = true;
				Cleanup.SessionState = "Exiting";
				Cleanup.BeginClosingQuestor();
				return true;
			}

			return false;
		}

		public static void WalletCheck()
		{
			Time.Instance.LastWalletCheck = DateTime.UtcNow;

			//Logging.Log("[Questor] Wallet Balance Debug Info: LastKnownGoodConnectedTime = " + Settings.Instance.lastKnownGoodConnectedTime);
			//Logging.Log("[Questor] Wallet Balance Debug Info: DateTime.UtcNow - LastKnownGoodConnectedTime = " + DateTime.UtcNow.Subtract(Settings.Instance.LastKnownGoodConnectedTime).TotalSeconds);
			if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 1)
			{
				Logging.Log(String.Format("Wallet Balance Has Not Changed in [ {0} ] minutes.",
				                          Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
			}

			if (Logging.DebugWalletBalance)
			{
				Logging.Log(String.Format("DEBUG: Wallet Balance [ {0} ] has been checked.",
				                          Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
			}

			//Settings.Instance.WalletBalanceChangeLogOffDelay = 2;  //used for debugging purposes
			//Logging.Log("Time.Instance.lastKnownGoodConnectedTime is currently: " + Time.Instance.LastKnownGoodConnectedTime);
			if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) <
			    Settings.Instance.WalletBalanceChangeLogOffDelay)
			{
				try
				{
					if ((long) Cache.Instance.MyWalletBalance != (long) Cache.Instance.DirectEve.Me.Wealth)
					{
						Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
						Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
					}
				}
				catch (Exception exception)
				{
					Logging.Log("Checking my wallet balance caused an exception [" + exception + "]");
				}
			}
			else if (Settings.Instance.WalletBalanceChangeLogOffDelay != 0)
			{
				if ((Cache.Instance.InStation) ||
				    (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) >
				     Settings.Instance.WalletBalanceChangeLogOffDelay + 5))
				{
					Logging.Log(String.Format(
						"Questor: Wallet Balance Has Not Changed in [ {0} ] minutes. Switching to QuestorState.CloseQuestor",
						Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes,
						           0)));
					Cleanup.ReasonToStopQuestor = "Wallet Balance did not change for over " +
						Settings.Instance.WalletBalanceChangeLogOffDelay + "min";
					Cache.Instance.CloseQuestorCMDLogoff = false;
					Cache.Instance.CloseQuestorCMDExitGame = true;
					Cleanup.SessionState = "Exiting";
					Cleanup.BeginClosingQuestor();
					return;
				}

				//
				// it is assumed if you got this far that you are in space. If you are 'stuck' in a session change then you'll be stuck another 5 min until the timeout above.
				//
				_States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

				return;
			}
		}

		public bool OnframeProcessEveryPulse()
		{
			try
			{
				// New frame, invalidate old cache
				Cache.Instance.InvalidateCache();

				if (DateTime.UtcNow <
				    Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))
				{
					if (Logging.DebugQuestorEVEOnFrame)
						Logging.Log("if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))");
					return false;
				}

				if (!Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
				{
					// Update settings (settings only load if character name changed)
					if (!Settings.Instance.DefaultSettingsLoaded)
					{
						Settings.Instance.LoadSettings();
						return false;
					}
				}

				if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
				{
					Logging.Log("if (!Cache.Instance.InSpace && !Cache.Instance.InStation)");
					return false;
				}

				if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(30))
				{
					Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
				}

				// Session is not ready yet, do not continue
				if (!Cache.Instance.DirectEve.Session.IsReady)
				{
					Logging.Log("if (!Cache.Instance.DirectEve.Session.IsReady)");
					return false;
				}

				Cleanup.ProcessState();
				Statistics.ProcessState();

				if (Cache.Instance.DirectEve.Session.IsReady)
				{
					Time.Instance.LastSessionIsReady = DateTime.UtcNow;
				}

				if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)
				{
					if (Cache.Instance.ActiveShip.GroupId == (int) Group.Capsule)
					{
						Logging.Log("We are in a pod. Don't wait for the session wait timer to expire!");
						Time.Instance.NextInSpaceorInStation = DateTime.UtcNow;
						return true;
					}

					if (Logging.DebugQuestorEVEOnFrame)
						Logging.Log("if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)");
					return false;
				}

				// Check 3D rendering
				if (Cache.Instance.DirectEve.Session.IsInSpace &&
				    Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
				{
					Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;
				}
				
				if(Cache.Instance.ActiveShip == null ||  Cache.Instance.ActiveShip.GivenName == null || (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null && Cache.Instance.ActiveShip.GivenName.Length < 1)) {
					return false;
				}

				if (DateTime.UtcNow.Subtract(Time.Instance.LastUpdateOfSessionRunningTime).TotalSeconds <
				    Time.Instance.SessionRunningTimeUpdate_seconds)
				{
					Statistics.SessionRunningTime =
						(int) DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalMinutes;
					Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
				}
				
				if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
				{
					if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
					{
						_States.CurrentQuestorState = QuestorState.CloseQuestor;
						Cleanup.BeginClosingQuestor();
					}
				}

				// When in warp there's nothing we can do, so ignore everything
				if (Cache.Instance.InSpace && Cache.Instance.InWarp)
				{
					if (Logging.DebugQuestorEVEOnFrame)
						Logging.Log("if (Cache.Instance.InSpace && Cache.Instance.InWarp)");
					return false;
				}


				return true;
			}
			catch (Exception ex)
			{
				Logging.Log("Exception [" + ex + "]");
				return false;
			}
		}
		
		public override Dictionary<Type,Boolean> GetControllerDependencies() {
			
			if(ControllerDependencies == null) {
				ControllerDependencies = new Dictionary<Type, bool>();
				ControllerDependencies.Add(typeof(LoginController),true);
			}
			return ControllerDependencies;
		}
		
		public override void DoWork()
		{
			try
			{
				if (_nextPulse > DateTime.UtcNow)
				{
					return;
				}

				Time.Instance.LastFrame = DateTime.UtcNow;

				Cache.Instance.CanSafelyCloseQuestorWindow = false;

				if (!Program.QuestorUIInstance.tabControlMain.SelectedTab.Text.ToLower().Equals("questor"))
				{
					_nextPulse = DateTime.UtcNow.AddSeconds(2);
					return;
				}

				if (DateTime.UtcNow < Time.Instance.LastDockAction.AddSeconds(5))
				{
					// temorarily fix
					//Logging.Log("LoginOnFrame", "if(DateTime.UtcNow < Time.Instance.LastDockAction.AddSeconds(8)", Logging.White);
					_nextPulse = UTCNowAddDelay(1, 1);
					return;
				}

				if (DateTime.UtcNow.Subtract(_lastQuestorPulse).TotalMilliseconds < pulseDelay)
				{
					return;
				}
				_lastQuestorPulse = DateTime.UtcNow;

				if (DateTime.UtcNow < _lastSessionNotReady)
				{
					_nextPulse = UTCNowAddDelay(1, 2);
					//Logging.Log("Questor.ProcessState", "if(GetNowAddDelay(8,10) > _lastSessionNotReady)", Logging.White);
					return;
				}

				if (!Cache.Instance.DirectEve.Session.IsReady)
				{
					_lastSessionNotReady = UTCNowAddDelay(7, 8);
					return;
				}

				Cache.Instance.WCFClient.GetPipeProxy.SetEveAccountAttributeValue(Cache.Instance.CharName,
				                                                                  "LastQuestorSessionReady", DateTime.UtcNow);

				if (Cache.Instance.InSpace) pulseDelay = Time.Instance.QuestorPulseInSpace_milliseconds;
				if (Cache.Instance.InStation) pulseDelay = Time.Instance.QuestorPulseInStation_milliseconds;

				if (!OnframeProcessEveryPulse()) return;

				RunOnceAfterStartup();

				Defense.ProcessState();

				switch (_States.CurrentQuestorState)
				{
					case QuestorState.Idle:
						if (TimeCheck()) return; //Should we close questor due to stoptime or runtime?

						if (Cache.Instance.StopBot)
						{
							if (Logging.DebugIdle)
								Logging.Log("Cache.Instance.StopBot = true - this is set by the LocalWatch code so that we stay in station when local is unsafe");
							return;
						}

						if (_States.CurrentQuestorState == QuestorState.Idle &&
						    Settings.Instance.CharacterMode != "none" && Settings.Instance.CharacterName != null)
						{
							_States.CurrentQuestorState = QuestorState.Start;
							return;
						}

						Logging.Log("Settings.Instance.CharacterMode = [" + Settings.Instance.CharacterMode + "]");
						_States.CurrentQuestorState = QuestorState.Error;
						break;

					case QuestorState.CombatMissionsBehavior:

						_combatMissionsBehavior.ProcessState();
						break;


					case QuestorState.Start:
						switch (Settings.Instance.CharacterMode.ToLower())
						{
							case "combat missions":
							case "combat_missions":
							case "dps":
								Logging.Log("Start Mission Behavior");
								_States.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
								break;
						}
						break;

					case QuestorState.CloseQuestor:
						if (Cleanup.ReasonToStopQuestor == string.Empty)
						{
							Cleanup.ReasonToStopQuestor = "case QuestorState.CloseQuestor:";
						}

						Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
						return;
				}
			}
			catch (Exception ex)
			{
				Logging.Log("Exception [" + ex + "]");
				return;
			}
		}
		
	}
}