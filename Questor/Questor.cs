// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using global::Questor.Modules.Activities;

namespace Questor
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using DirectEve;
	using System.Windows.Forms;
	using System.Windows.Threading;
	using global::Questor.Behaviors;
	using global::Questor.Modules.Actions;
	using global::Questor.Modules.Caching;
	using global::Questor.Modules.Combat;
	using global::Questor.Modules.Logging;
	using global::Questor.Modules.Lookup;
	
	using global::Questor.Modules.States;
	using global::Questor.Modules.BackgroundTasks;

	public class Questor : IDisposable
	{
		//private readonly Defense _defense;

		private DateTime _lastQuestorPulse;
		//private static DateTime _nextQuestorAction = DateTime.UtcNow.AddHours(-1);
		private readonly CombatMissionsBehavior _combatMissionsBehavior;
		//private readonly MissionSettings _combatMissionSettings;
		private readonly CombatHelperBehavior _combatHelperBehavior;
		private readonly DedicatedBookmarkSalvagerBehavior _dedicatedBookmarkSalvagerBehavior;
		private readonly DebugHangarsBehavior _debugHangarsBehavior;
		
		//private readonly Statistics _statistics;
		//private readonly BackgroundBehavior _backgroundbehavior;
		//private readonly Cleanup _cleanup;

		private bool _runOnceAfterStartupalreadyProcessed;
		private bool _runOnceInStationAfterStartupalreadyProcessed;
		
		private static DateTime _nextPulse = DateTime.MinValue;
		private static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
		private static DateTime _lastSessionNotReady = DateTime.MinValue;
		private static Random _random = new Random();

		private readonly Stopwatch _watch;

		public Questor()
		{
			
			//Logging.tryToLogToFile = true;
			_lastQuestorPulse = DateTime.UtcNow;

			//_defense = new Defense();
			_combatMissionsBehavior = new CombatMissionsBehavior();
			//_combatMissionSettings = new MissionSettings();
			_combatHelperBehavior = new CombatHelperBehavior();
			_dedicatedBookmarkSalvagerBehavior = new DedicatedBookmarkSalvagerBehavior();
			_debugHangarsBehavior = new DebugHangarsBehavior();
			//_backgroundbehavior = new BackgroundBehavior();
			//_cleanup = new Cleanup();
			_watch = new Stopwatch();
			
			//_statistics = new Statistics();

			Time.Instance.NextStartupAction = DateTime.UtcNow;
			// State fixed on ExecuteMission
			_States.CurrentQuestorState = QuestorState.Idle;

			if (Cache.Instance.DirectEve == null)
			{
				Logging.Log("Startup", "Error on Loading DirectEve, maybe server is down", Logging.Orange);
				Cache.Instance.CloseQuestorCMDLogoff = false;
				Cache.Instance.CloseQuestorCMDExitGame = true;
				Cache.Instance.CloseQuestorEndProcess = true;
				Settings.Instance.AutoStart = true;
				Cleanup.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
				Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
				Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
				return;
			}

			Time.Instance.StartTime = DateTime.UtcNow;
			Time.Instance.QuestorStarted_DateTime = DateTime.UtcNow;

			// get the current process
			Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

			// get the physical mem usage
			Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 + 1 / 1024) / 1024);
			Logging.Log("Questor", "EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB", Logging.White);
			Statistics.SessionIskGenerated = 0;
			Statistics.SessionLootGenerated = 0;
			Statistics.SessionLPGenerated = 0;
			Settings.Instance.CharacterMode = "none";

			try
			{
				Logging.Log("Questor", "Register EVEOnFrame Event", Logging.White);
				//
				// setup the [ Cache.Instance.DirectEve.OnFrame ] Event triggered on every new frame to call EVEOnFrame()
				//
				Cache.Instance.DirectEve.OnFrame += EVEOnFrame;
			}
			catch (Exception ex)
			{
				Logging.Log("Questor", string.Format("DirectEVE.OnFrame: Exception {0}...", ex), Logging.White);
				Cache.Instance.CloseQuestorCMDLogoff = false;
				Cache.Instance.CloseQuestorCMDExitGame = true;
				Cache.Instance.CloseQuestorEndProcess = true;
				Settings.Instance.AutoStart = true;
				Cleanup.ReasonToStopQuestor = "Error on DirectEve.OnFrame, maybe the DirectEVE license server is down";
				Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
				Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
			}

			
			//new SubModules.Debug();
			
			Logging.Log("Questor", "Questor.", Logging.White);
		}

		public void RunOnceAfterStartup()
		{
			if (!_runOnceAfterStartupalreadyProcessed && DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15) && Cache.Instance.DirectEve.Session.CharacterId != null && Cache.Instance.DirectEve.Session.CharacterId > 0)
			{
				if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
				{
					Cache.Instance.DirectEve.Skills.RefreshMySkills();
					_runOnceAfterStartupalreadyProcessed = true;

					Cache.Instance.IterateShipTargetValues("RunOnceAfterStartup");  // populates ship target values from an XML
					//Cache.Instance.IterateInvTypes("RunOnceAfterStartup");          // populates the prices of items (cant we use prices from the game now?!)
					Cache.Instance.IterateUnloadLootTheseItemsAreLootItems("RunOnceAfterStartup");       // populates the list of items we never want in our local cargo (used mainly in unloadloot)

					MissionSettings.UpdateMissionName();
					Logging.MaintainConsoleLogs();

					//var daysLeft = Cache.Instance.DirectEve.Me.DaysLeftOnAccount;
					//Logging.Log("RunOnceAfterStartup", "Cache.Instance.DirectEve.Me.DaysLeftOnAccount [" + daysLeft + "]", Logging.Debug);

				}
				else
				{
					Logging.Log("RunOnceAfterStartup", "Settings.Instance.CharacterName is still null", Logging.Orange);
					Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
					_runOnceAfterStartupalreadyProcessed = false;
					return;
				}
			}
		}

		public void RunOnceInStationAfterStartup()
		{
			if (!_runOnceInStationAfterStartupalreadyProcessed && DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15) && Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10))
			{
				if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
				{
					if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName) || !string.IsNullOrEmpty(Settings.Instance.LootHangarTabName) && Cache.Instance.InStation)
					{
						Logging.Log("RunOnceAfterStartup", "Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);", Logging.Debug);
						Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);
						Statistics.LogWindowActionToWindowLog("CorpHangar", "CorpHangar Opened");
					}



					_runOnceInStationAfterStartupalreadyProcessed = true;
				}
				else
				{
					Logging.Log("RunOnceAfterStartup", "Settings.Instance.CharacterName is still null", Logging.Orange);
					Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
					_runOnceInStationAfterStartupalreadyProcessed = false;
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
				Logging.Log(whatWeAreTiming, " took " + _watch.ElapsedMilliseconds + "ms", Logging.White);
		}

		public static bool SkillQueueCheck()
		{
			if (!Cache.Instance.InSpace && Cache.Instance.InStation)
			{
				if (DateTime.UtcNow < Time.Instance.NextSkillTrainerAction)
					return true;

				if (!Cache.Instance.DirectEve.Skills.AreMySkillsReady)
				{
					if (Logging.DebugSkillTraining) Logging.Log("SkillQueueCheck", "if (!Cache.Instance.DirectEve.Skills.AreMySkillsReady) - this really should not happen (often?)", Logging.Debug);
					return true;
				}

				if (Settings.Instance.ThisToonShouldBeTrainingSkills)
				{
					if (Logging.DebugSkillTraining) Logging.Log("Questor.SkillQueueCheck", "Current Training Queue Length is [" + Cache.Instance.DirectEve.Skills.SkillQueueLength.ToString() + "]", Logging.White);
					if (Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours < 24)
					{
						Logging.Log("Questor.SkillQueueCheck", "Training Queue currently has room. [" + Math.Round(24 - Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours, 2) + " hours free]", Logging.White);
						//QuestorUI.lblCurrentMissionInfo.Text = "Training Queue currently has room. [" + Math.Round(24 - Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours, 2) + " hours free]";

						_States.CurrentQuestorState = QuestorState.SkillTrainer;
						return false;
					}

					Logging.Log("Questor.SkillQueueCheck", "Training Queue is full. [" + Math.Round(Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours, 2) + " is more than 24 hours]", Logging.White);
					Time.Instance.NextSkillTrainerAction = DateTime.UtcNow.AddHours(3);
					return true;
				}

				if (Logging.DebugSkillTraining) Logging.Log("Questor.SkillQueueCheck", "Settings.Instance.ThisToonShouldBeTrainingSkills [" + Settings.Instance.ThisToonShouldBeTrainingSkills + "]", Logging.White);
			}

			return true;
		}
		
		protected static int GetRandom(int minValue, int maxValue)
		{
			return _random.Next(minValue, maxValue);
		}
		protected DateTime GetNowAddDelay(int minDelayInSeconds, int maxDelayInSeconds)
		{
			return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInSeconds * 1000, maxDelayInSeconds * 1000));
		}

		public static bool TimeCheck()
		{
			if (DateTime.UtcNow < Time.Instance.NextTimeCheckAction)
				return false;

			Time.Instance.NextTimeCheckAction = DateTime.UtcNow.AddSeconds(90);

			if (Cache.Instance.ExitWhenIdle)
			{
				Logging.Log("Questor", "ExitWhenIdle set to true.  Quitting game.", Logging.White);
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
			if (_States.CurrentQuestorState == QuestorState.Mining ||
			    _States.CurrentQuestorState == QuestorState.CombatHelperBehavior ||
			    _States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
				//_States.CurrentQuestorState == QuestorState.BackgroundBehavior)
			{
				if (Logging.DebugWalletBalance) Logging.Log("Questor.WalletCheck", "QuestorState is [" + _States.CurrentQuestorState.ToString() + "] which does not use WalletCheck", Logging.White);
				return;
			}

			Time.Instance.LastWalletCheck = DateTime.UtcNow;

			//Logging.Log("[Questor] Wallet Balance Debug Info: LastKnownGoodConnectedTime = " + Settings.Instance.lastKnownGoodConnectedTime);
			//Logging.Log("[Questor] Wallet Balance Debug Info: DateTime.UtcNow - LastKnownGoodConnectedTime = " + DateTime.UtcNow.Subtract(Settings.Instance.LastKnownGoodConnectedTime).TotalSeconds);
			if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 1)
			{
				Logging.Log("Questor.WalletCheck", String.Format("Wallet Balance Has Not Changed in [ {0} ] minutes.", Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)), Logging.White);
			}

			if (Logging.DebugWalletBalance)
			{
				Logging.Log("Questor.WalletCheck", String.Format("DEBUG: Wallet Balance [ {0} ] has been checked.", Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)), Logging.Yellow);

			}

			//Settings.Instance.WalletBalanceChangeLogOffDelay = 2;  //used for debugging purposes
			//Logging.Log("Time.Instance.lastKnownGoodConnectedTime is currently: " + Time.Instance.LastKnownGoodConnectedTime);
			if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) < Settings.Instance.WalletBalanceChangeLogOffDelay)
			{
				try
				{
					if ((long)Cache.Instance.MyWalletBalance != (long)Cache.Instance.DirectEve.Me.Wealth)
					{
						Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
						Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
					}
				}
				catch (Exception exception)
				{
					Logging.Log("Questor.WalletCheck", "Checking my wallet balance caused an exception [" + exception + "]", Logging.White);
				}
			}
			else if (Settings.Instance.WalletBalanceChangeLogOffDelay != 0)
			{
				if ((Cache.Instance.InStation) || (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) > Settings.Instance.WalletBalanceChangeLogOffDelay + 5))
				{
					Logging.Log("Questor", String.Format("Questor: Wallet Balance Has Not Changed in [ {0} ] minutes. Switching to QuestorState.CloseQuestor", Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)), Logging.White);
					Cleanup.ReasonToStopQuestor = "Wallet Balance did not change for over " + Settings.Instance.WalletBalanceChangeLogOffDelay + "min";
					Cache.Instance.CloseQuestorCMDLogoff = false;
					Cache.Instance.CloseQuestorCMDExitGame = true;
					Cleanup.SessionState = "Exiting";
					Cleanup.BeginClosingQuestor();
					return;
				}

				//
				// it is assumed if you got this far that you are in space. If you are 'stuck' in a session change then you'll be stuck another 5 min until the timeout above.
				//
				_States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
				_States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
				_States.CurrentCombatHelperBehaviorState = CombatHelperBehaviorState.GotoBase;
				return;
			}
		}

		public bool OnframeProcessEveryPulse()
		{
			try
			{
				// New frame, invalidate old cache
				Cache.Instance.InvalidateCache();

				if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))
				{
					if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))", Logging.Debug);
					return false;
				}

				if (!Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
				{
					// Update settings (settings only load if character name changed)
					if (!Settings.Instance.DefaultSettingsLoaded)
					{
						Settings.Instance.LoadSettings();
					}
				}

				//if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.OnframeProcessEveryPulse", "return;", Logging.Debug);
				//return false;
				
				if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
				{
					if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (!Cache.Instance.InSpace && !Cache.Instance.InStation)", Logging.Debug);
					return false;
				}

				if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(30))
				{
					Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
				}

				// Start _cleanup.ProcessState
				// Description: Closes Windows, and eventually other things considered 'cleanup' useful to more than just Questor(Missions) but also Anomalies, Mining, etc
				//
				
				// Session is not ready yet, do not continue
				if (!Cache.Instance.DirectEve.Session.IsReady)
				{
					Logging.Log("Questor.ProcessState", "if (!Cache.Instance.DirectEve.Session.IsReady)", Logging.Debug);
					return false;
				}

				if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "Cleanup.ProcessState();", Logging.Debug);
				Cleanup.ProcessState();
				if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "Statistics.ProcessState();", Logging.Debug);
				Statistics.ProcessState();
				

				// Done
				// Cleanup State: ProcessState



				if (Cache.Instance.DirectEve.Session.IsReady)
				{
					Time.Instance.LastSessionIsReady = DateTime.UtcNow;
				}

				if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)
				{
					if (Cache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
					{
						Logging.Log("Panic", "We are in a pod. Don't wait for the session wait timer to expire!", Logging.Red);
						Time.Instance.NextInSpaceorInStation = DateTime.UtcNow;
						return true;
					}

					if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)", Logging.Debug);
					return false;
				}

				// Check 3D rendering
				if (Cache.Instance.DirectEve.Session.IsInSpace && Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
				{
					Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;
				}

				if (DateTime.UtcNow.Subtract(Time.Instance.LastUpdateOfSessionRunningTime).TotalSeconds < Time.Instance.SessionRunningTimeUpdate_seconds)
				{
					Statistics.SessionRunningTime = (int)DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalMinutes;
					Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
				}

				return true;
			}
			catch (Exception ex)
			{
				Logging.Log("Questor.OnframeProcessEveryPulse", "Exception [" + ex + "]", Logging.Debug);
				return false;
			}
		}

		private int pulseDelay = 800;
		private void EVEOnFrame(object sender, EventArgs e)
		{
			try
				
			{
				
				if (_nextPulse > DateTime.UtcNow)
				{
					return;
				}

				
				Time.Instance.LastFrame = DateTime.UtcNow;
				
				if (Cache.Instance.Paused)
				{
					return;
				}

				if (!BeforeLogin.questorUI.tabControlMain.SelectedTab.Text.ToLower().Equals("questor")) {
					_nextPulse = DateTime.UtcNow.AddSeconds(2);
					return;
				}
				
				if(DateTime.UtcNow < Time.Instance.LastDockAction.AddSeconds(9)) { // temorarily fix
					//Logging.Log("LoginOnFrame", "if(DateTime.UtcNow < Time.Instance.LastDockAction.AddSeconds(8)", Logging.White);
					_nextPulse = _nextPulse.AddSeconds(1);
					return;
				}

				if (DateTime.UtcNow.Subtract(_lastQuestorPulse).TotalMilliseconds < pulseDelay)
				{
					return;
				}
				
				if(DateTime.UtcNow < _lastSessionNotReady) {
					_nextPulse = _nextPulse.AddSeconds(1);
					//Logging.Log("Questor.ProcessState", "if(GetNowAddDelay(8,10) > _lastSessionNotReady)", Logging.White);
					return;
				}
				
				
				if(!Cache.Instance.DirectEve.Session.IsReady && !Cache.Instance.DirectEve.Login.AtLogin && !Cache.Instance.DirectEve.Login.AtCharacterSelection) {
					_lastSessionNotReady = GetNowAddDelay(7,8);
				}
				
				
				_lastQuestorPulse = DateTime.UtcNow;
				
				if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
				{
					if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
					{
						_States.CurrentQuestorState = QuestorState.CloseQuestor;
						Cleanup.BeginClosingQuestor();
					}
				}

				
				#region LOGIN

				if (Cache.Instance.DirectEve.Login.AtLogin || Cache.Instance.DirectEve.Login.AtCharacterSelection || Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)
				{
					if(Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading) {
						Logging.Log("LoginOnFrame", "if(Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)", Logging.White);
						_nextPulse = GetNowAddDelay(2,4);
						return;
					}

					//Time.Instance.LastSessionIsReady = DateTime.UtcNow;

					if (DateTime.UtcNow < _lastServerStatusCheckWasNotOK.AddSeconds(Cache.Instance.RandomNumber(4, 7)))
					{
						Logging.Log("LoginOnFrame", "lastServerStatusCheckWasNotOK = [" + _lastServerStatusCheckWasNotOK.ToShortTimeString() + "] waiting 10 to 20 seconds.", Logging.White);
						return;
					}

					_lastServerStatusCheckWasNotOK = DateTime.UtcNow.AddDays(-1); //reset this so we never hit this twice in a row w/o another server status check not being OK.

					if (DateTime.UtcNow < _nextPulse)
					{
						if (Logging.DebugOnframe) Logging.Log("LoginOnFrame", "if (DateTime.UtcNow < _nextPulse)", Logging.White);
						return;
					}

					if (Logging.DebugOnframe) Logging.Log("LoginOnFrame", "Pulse...", Logging.White);


					_nextPulse = DateTime.UtcNow.AddMilliseconds(Time.Instance.QuestorBeforeLoginPulseDelay_milliseconds);

					if (DateTime.UtcNow < Cache.QuestorProgramLaunched.AddSeconds(5))
					{
						//
						// do not login for the first 7 seconds, wait...
						//
						return;
					}

					if (Cache._humanInterventionRequired)
					{
						Logging.Log("Startup", "OnFrame: _humanInterventionRequired is true (this will spam every second or so)", Logging.Orange);
						_nextPulse = _nextPulse.AddMinutes(2);
						return;
					}

					if (Logging.DebugOnframe) Logging.Log("LoginOnFrame", "before: if (Cache.Instance.DirectEve.Windows.Count != 0)", Logging.White);

					// We should not get any windows
					if (Cache.Instance.DirectEve.Windows.Count != 0)
					{
						foreach (DirectWindow window in Cache.Instance.DirectEve.Windows)
						{
							if (string.IsNullOrEmpty(window.Html))
								continue;
							Logging.Log("Startup", "WindowTitles:" + window.Name + "::" + window.Html, Logging.White);

							//
							// Close these windows and continue
							//
							if (window.Name == "telecom" && !Logging.DebugDoNotCloseTelcomWindows)
							{
								Logging.Log("Startup", "Closing telecom message...", Logging.Yellow);
								Logging.Log("Startup", "Content of telecom window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.Yellow);
								window.Close();
								continue;
							}

							// Modal windows must be closed
							// But lets only close known modal windows
							if (window.IsModal)
							{
								bool close = false;
								bool restart = false;
								bool needHumanIntervention = false;
								bool sayYes = false;
								bool sayOk = false;
								bool quit = false;

								//bool update = false;

								if (!string.IsNullOrEmpty(window.Html))
								{
									//errors that are repeatable and unavoidable even after a restart of eve/questor
									needHumanIntervention = window.Html.Contains("reason: Account subscription expired");

									//update |= window.Html.Contains("The update has been downloaded");

									// Server going down
									//Logging.Log("[Startup] (1) close is: " + close);
									close |= window.Html.ToLower().Contains("please make sure your characters are out of harms way");
									close |= window.Html.ToLower().Contains("accepting connections");
									close |= window.Html.ToLower().Contains("could not connect");
									close |= window.Html.ToLower().Contains("the connection to the server was closed");
									close |= window.Html.ToLower().Contains("server was closed");
									close |= window.Html.ToLower().Contains("make sure your characters are out of harm");
									close |= window.Html.ToLower().Contains("connection to server lost");
									close |= window.Html.ToLower().Contains("the socket was closed");
									close |= window.Html.ToLower().Contains("the specified proxy or server node");
									close |= window.Html.ToLower().Contains("starting up");
									close |= window.Html.ToLower().Contains("unable to connect to the selected server");
									close |= window.Html.ToLower().Contains("could not connect to the specified address");
									close |= window.Html.ToLower().Contains("connection timeout");
									close |= window.Html.ToLower().Contains("the cluster is not currently accepting connections");
									close |= window.Html.ToLower().Contains("your character is located within");
									close |= window.Html.ToLower().Contains("the transport has not yet been connected");
									close |= window.Html.ToLower().Contains("the user's connection has been usurped");
									close |= window.Html.ToLower().Contains("the EVE cluster has reached its maximum user limit");
									close |= window.Html.ToLower().Contains("the connection to the server was closed");
									close |= window.Html.ToLower().Contains("client is already connecting to the server");

									//close |= window.Html.Contains("A client update is available and will now be installed");
									//
									// eventually it would be nice to hit ok on this one and let it update
									//
									close |= window.Html.ToLower().Contains("client update is available and will now be installed");
									close |= window.Html.ToLower().Contains("change your trial account to a paying account");

									//
									// these windows require a restart of eve all together
									//
									restart |= window.Html.ToLower().Contains("the connection was closed");
									restart |= window.Html.ToLower().Contains("connection to server lost."); //INFORMATION
									restart |= window.Html.ToLower().Contains("local cache is corrupt");
									sayOk |= window.Html.ToLower().Contains("local session information is corrupt");
									restart |= window.Html.ToLower().Contains("The client's local session"); // information is corrupt");
									restart |= window.Html.ToLower().Contains("restart the client prior to logging in");

									//
									// these windows require a quit of eve all together
									//
									quit |= window.Html.ToLower().Contains("the socket was closed");

									//
									// Modal Dialogs the need "yes" pressed
									//
									//sayYes |= window.Html.Contains("There is a new build available. Would you like to download it now");
									//sayOk |= window.Html.Contains("The update has been downloaded. The client will now close and the update process begin");
									sayOk |= window.Html.Contains("The transport has not yet been connected, or authentication was not successful");

									//Logging.Log("[Startup] (2) close is: " + close);
									//Logging.Log("[Startup] (1) window.Html is: " + window.Html);
								}

								//if (update)
								//{
								//    int secRestart = (400 * 3) + Cache.Instance.RandomNumber(3, 18) * 100 + Cache.Instance.RandomNumber(1, 9) * 10;
								//    LavishScript.ExecuteCommand("uplink exec Echo [${Time}] timedcommand " + secRestart + " OSExecute taskkill /IM launcher.exe");
								//}

								if (sayYes)
								{
									Logging.Log("Startup", "Found a window that needs 'yes' chosen...", Logging.White);
									Logging.Log("Startup", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
									window.AnswerModal("Yes");
									continue;
								}

								if (sayOk)
								{
									Logging.Log("Startup", "Found a window that needs 'ok' chosen...", Logging.White);
									Logging.Log("Startup", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
									window.AnswerModal("OK");
									if (window.Html.Contains("The update has been downloaded. The client will now close and the update process begin"))
									{
										//
										// schedule the closing of launcher.exe via a timedcommand (10 min?) in the uplink...
										//
									}
									continue;
								}

								if (quit)
								{
									Logging.Log("Startup", "Restarting eve...", Logging.Red);
									Logging.Log("Startup", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.Red);
									window.AnswerModal("quit");

									//_directEve.ExecuteCommand(DirectCmd.CmdQuitGame);
								}

								if (restart)
								{
									Logging.Log("Startup", "Restarting eve...", Logging.Red);
									Logging.Log("Startup", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.Red);
									window.AnswerModal("restart");
									continue;
								}

								if (close)
								{
									Logging.Log("Startup", "Closing modal window...", Logging.Yellow);
									Logging.Log("Startup", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.Yellow);
									window.Close();
									continue;
								}

								if (needHumanIntervention)
								{
									Logging.Log("Startup", "ERROR! - Human Intervention is required in this case: halting all login attempts - ERROR!", Logging.Red);
									Logging.Log("Startup", "window.Name is: " + window.Name, Logging.Red);
									Logging.Log("Startup", "window.Html is: " + window.Html, Logging.Red);
									Logging.Log("Startup", "window.Caption is: " + window.Caption, Logging.Red);
									Logging.Log("Startup", "window.Type is: " + window.Type, Logging.Red);
									Logging.Log("Startup", "window.ID is: " + window.Id, Logging.Red);
									Logging.Log("Startup", "window.IsDialog is: " + window.IsDialog, Logging.Red);
									Logging.Log("Startup", "window.IsKillable is: " + window.IsKillable, Logging.Red);
									Logging.Log("Startup", "window.Viewmode is: " + window.ViewMode, Logging.Red);
									Logging.Log("Startup", "ERROR! - Human Intervention is required in this case: halting all login attempts - ERROR!", Logging.Red);
									Cache._humanInterventionRequired = true;
									return;
								}
							}

							if (string.IsNullOrEmpty(window.Html))
								continue;

							if (window.Name == "telecom")
								continue;
							Logging.Log("Startup", "We have an unexpected window, auto login halted.", Logging.Red);
							Logging.Log("Startup", "window.Name is: " + window.Name, Logging.Red);
							Logging.Log("Startup", "window.Html is: " + window.Html, Logging.Red);
							Logging.Log("Startup", "window.Caption is: " + window.Caption, Logging.Red);
							Logging.Log("Startup", "window.Type is: " + window.Type, Logging.Red);
							Logging.Log("Startup", "window.ID is: " + window.Id, Logging.Red);
							Logging.Log("Startup", "window.IsDialog is: " + window.IsDialog, Logging.Red);
							Logging.Log("Startup", "window.IsKillable is: " + window.IsKillable, Logging.Red);
							Logging.Log("Startup", "window.Viewmode is: " + window.ViewMode, Logging.Red);
							Logging.Log("Startup", "We have got an unexpected window, auto login halted.", Logging.Red);
							return;
						}
						return;
					}

					if (Cache.Instance.DirectEve.Login.AtLogin && Cache.Instance.DirectEve.Login.ServerStatus != "Status: OK")
					{
						if (Cache.ServerStatusCheck <= 20) // at 10 sec a piece this would be 200+ seconds
						{
							Logging.Log("Startup", "Server status[" + Cache.Instance.DirectEve.Login.ServerStatus + "] != [OK] try later", Logging.Orange);
							Cache.ServerStatusCheck++;
							//retry the server status check twice (with 1 sec delay between each) before kicking in a larger delay
							if (Cache.ServerStatusCheck > 2)
							{
								_lastServerStatusCheckWasNotOK = DateTime.UtcNow;
							}

							return;
						}

						Cache.ServerStatusCheck = 0;
						Cleanup.ReasonToStopQuestor = "Server Status Check shows server still not ready after more than 3 min. Restarting Questor. ServerStatusCheck is [" + Cache.ServerStatusCheck + "]";
						Logging.Log("Startup", Cleanup.ReasonToStopQuestor, Logging.Red);
						Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
						Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor, true);
						return;
					}

					if (Cache.Instance.DirectEve.Login.AtLogin && !Cache.Instance.DirectEve.Login.IsLoading && !Cache.Instance.DirectEve.Login.IsConnecting)
					{
						if (DateTime.UtcNow.Subtract(Cache.QuestorSchedulerReadyToLogin).TotalMilliseconds > Cache.Instance.RandomNumber(Time.Instance.EVEAccountLoginDelayMinimum_seconds * 1000, Time.Instance.EVEAccountLoginDelayMaximum_seconds * 1000))
						{
							Logging.Log("Startup", "Login account [" + Logging.EVELoginUserName + "]", Logging.White);
							Cache.Instance.DirectEve.Login.Login(Logging.EVELoginUserName, Logging.EVELoginPassword);
							_nextPulse = GetNowAddDelay(10,12);
							Logging.Log("Startup", "Waiting for Character Selection Screen", Logging.White);
							return;
						}
					}

					if (Cache.Instance.DirectEve.Login.AtCharacterSelection && Cache.Instance.DirectEve.Login.IsCharacterSelectionReady && !Cache.Instance.DirectEve.Login.IsConnecting && !Cache.Instance.DirectEve.Login.IsLoading)
					{
						if (DateTime.UtcNow.Subtract(Cache.EVEAccountLoginStarted).TotalMilliseconds > Cache.Instance.RandomNumber(Time.Instance.CharacterSelectionDelayMinimum_seconds * 1000, Time.Instance.CharacterSelectionDelayMaximum_seconds * 1000) && DateTime.UtcNow > Cache.NextSlotActivate)
						{
							foreach (DirectLoginSlot slot in Cache.Instance.DirectEve.Login.CharacterSlots)
							{
								if (slot.CharId.ToString(CultureInfo.InvariantCulture) != Logging.MyCharacterName && System.String.Compare(slot.CharName, Logging.MyCharacterName, System.StringComparison.OrdinalIgnoreCase) != 0)
								{
									continue;
								}

								Logging.Log("Startup", "Activating character [" + slot.CharName + "]", Logging.White);
								Cache.NextSlotActivate = DateTime.UtcNow.AddSeconds(5);
								slot.Activate();
								_nextPulse = GetNowAddDelay(12,14);
								return;
							}

							Logging.Log("Startup", "Character id/name [" + Logging.MyCharacterName + "] not found, retrying in 10 seconds", Logging.White);
						}
					}

					return;
				}

				#endregion
				
				if (Cache.Instance.InSpace) pulseDelay = Time.Instance.QuestorPulseInSpace_milliseconds;
				if (Cache.Instance.InStation) pulseDelay = Time.Instance.QuestorPulseInStation_milliseconds;
				
				Cache.Instance.InvalidateCache();
				
				
				if (!OnframeProcessEveryPulse()) return;
				if (Logging.DebugOnframe) Logging.Log("Questor", "OnFrame: this is Questor.cs [" + DateTime.UtcNow + "] by default the next InSpace pulse will be in [" + Time.Instance.QuestorPulseInSpace_milliseconds + "]milliseconds", Logging.Teal);

				//if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.EVEOnFrame", "return;", Logging.Debug);
				//return;
				RunOnceAfterStartup();
				RunOnceInStationAfterStartup();

				if (!Cache.Instance.Paused)
				{
					if (DateTime.UtcNow.Subtract(Time.Instance.LastWalletCheck).TotalMinutes > Time.Instance.WalletCheck_minutes && !Settings.Instance.DefaultSettingsLoaded)
					{
						WalletCheck();
					}
				}

				// We always check our defense state if we're in space, regardless of questor state
				// We also always check panic
				
				Defense.ProcessState();
				
				
				
				// temporarily fix
				if(!Cache.Instance.Paused && Cache.Instance.InSpace &&  Cache.Instance.Modules.Count(m => !m.IsOnline) > 1) {
					
					foreach(var mod in Cache.Instance.Modules.Where(m => !m.IsOnline)) {
						Logging.Log("Questor.ProcessState", "Offline module: " + mod.TypeName, Logging.Debug);
					}
					
					Logging.Log("Questor.ProcessState", "Offline modules found, closing questor. Modules needs to be activated again manually.", Logging.Debug);
					Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
					Cleanup.SignalToQuitQuestor = true;
//					_States.CurrentQuestorState = QuestorState.Error;
				}

				if (Cache.Instance.Paused) //|| DateTime.UtcNow < _nextQuestorAction)
				{
					if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (Cache.Instance.Paused)", Logging.Debug);
					Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
					Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
					Cache.Instance.GotoBaseNow = false;
					Cleanup.SessionState = string.Empty;
					return;
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
					if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.EVEOnFrame", "if (Cache.Instance.InSpace && Cache.Instance.InWarp)", Logging.Debug);
					return;
				}

				switch (_States.CurrentQuestorState)
				{
					case QuestorState.Idle:
						if (TimeCheck()) return; //Should we close questor due to stoptime or runtime?

						if (!SkillQueueCheck()) return; //if we need to train skills we return here, on the next pass we will be _States.CurrentQuestorState = QuestorSate.SkillTrainer

						if (Cache.Instance.StopBot)
						{
							if (Logging.DebugIdle) Logging.Log("Questor", "Cache.Instance.StopBot = true - this is set by the LocalWatch code so that we stay in station when local is unsafe", Logging.Orange);
							return;
						}

						if (_States.CurrentQuestorState == QuestorState.Idle && Settings.Instance.CharacterMode != "none" && Settings.Instance.CharacterName != null)
						{
							_States.CurrentQuestorState = QuestorState.Start;
							return;
						}

						Logging.Log("Questor", "Settings.Instance.CharacterMode = [" + Settings.Instance.CharacterMode + "]", Logging.Orange);
						_States.CurrentQuestorState = QuestorState.Error;
						break;

					case QuestorState.CombatMissionsBehavior:

						//
						// QuestorState will stay here until changed externally by the behavior we just kicked into starting
						//
						_combatMissionsBehavior.ProcessState();
						break;

					case QuestorState.SkillTrainer:

						//
						// QuestorState will stay here until changed externally by the behavior we just kicked into starting
						//
						SkillTrainerClass.ProcessState();
						break;

					case QuestorState.CombatHelperBehavior:

						//
						// QuestorState will stay here until changed externally by the behavior we just kicked into starting
						//
						_combatHelperBehavior.ProcessState();
						break;

					case QuestorState.DedicatedBookmarkSalvagerBehavior:

						//
						// QuestorState will stay here until changed externally by the behavior we just kicked into starting
						//
						_dedicatedBookmarkSalvagerBehavior.ProcessState();
						break;

					case QuestorState.DebugHangarsBehavior:

						//
						// QuestorState will stay here until changed externally by the behavior we just kicked into starting
						//
						_debugHangarsBehavior.ProcessState();
						break;

					case QuestorState.DebugReloadAll:
						if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distances.OnGridWithMe))) return;
						_States.CurrentQuestorState = QuestorState.Start;
						break;
						
						


					case QuestorState.Start:
						switch (Settings.Instance.CharacterMode.ToLower())
						{
							case "combat missions":
							case "combat_missions":
							case "dps":
								Logging.Log("Questor", "Start Mission Behavior", Logging.White);
								_States.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
								break;

							case "salvage":
								Logging.Log("Questor", "Start Salvaging Behavior", Logging.White);
								_States.CurrentQuestorState = QuestorState.DedicatedBookmarkSalvagerBehavior;
								break;

							case "combat helper":
							case "combat_helper":
							case "combathelper":
								Logging.Log("Questor", "Start CombatHelper Behavior", Logging.White);
								_States.CurrentQuestorState = QuestorState.CombatHelperBehavior;
								break;

							case "custom":
								Logging.Log("Questor", "Start Custom Behavior", Logging.White);
								//_States.CurrentQuestorState = QuestorState.BackgroundBehavior;
								break;

							case "directionalscanner":
								Logging.Log("Questor", "Start DirectionalScanner Behavior", Logging.White);
								_States.CurrentQuestorState = QuestorState.DirectionalScannerBehavior;
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

					case QuestorState.DebugCloseQuestor:

						Logging.Log("Questor", "CloseQuestorCMDUplinkInnerspaceProfile: " + Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile, Logging.White);
						Logging.Log("Questor", "CloseQuestorCMDUplinkISboxerCharacterSet: " + Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet, Logging.White);
						Logging.Log("Questor", "CloseQuestorArbitraryOSCmd" + Settings.Instance.CloseQuestorArbitraryOSCmd, Logging.White);
						Logging.Log("Questor", "CloseQuestorOSCmdContents" + Settings.Instance.CloseQuestorOSCmdContents, Logging.White);
						Logging.Log("Questor", "WalletBalanceChangeLogOffDelay: " + Settings.Instance.WalletBalanceChangeLogOffDelay, Logging.White);
						Logging.Log("Questor", "WalletBalanceChangeLogOffDelayLogoffOrExit: " + Settings.Instance.WalletBalanceChangeLogOffDelayLogoffOrExit, Logging.White);
						Logging.Log("Questor", "EVEProcessMemoryCeiling: " + Settings.Instance.EVEProcessMemoryCeiling, Logging.White);
						Logging.Log("Questor", "Cache.Instance.CloseQuestorCMDExitGame: " + Cache.Instance.CloseQuestorCMDExitGame, Logging.White);
						Logging.Log("Questor", "Cache.Instance.CloseQuestorCMDLogoff: " + Cache.Instance.CloseQuestorCMDLogoff, Logging.White);
						Logging.Log("Questor", "Cache.Instance.CloseQuestorEndProcess: " + Cache.Instance.CloseQuestorEndProcess, Logging.White);
						Logging.Log("Questor", "Time.EnteredCloseQuestor_DateTime: " + Time.EnteredCloseQuestor_DateTime.ToShortTimeString(), Logging.White);
						_States.CurrentQuestorState = QuestorState.Error;
						return;

					case QuestorState.DebugWindows:
						List<DirectWindow> windows = Cache.Instance.Windows;

						if (windows != null && windows.Any())
						{
							foreach (DirectWindow window in windows)
							{
								Logging.Log("Questor", "--------------------------------------------------", Logging.Orange);
								Logging.Log("Questor", "Debug_Window.Name: [" + window.Name + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.Caption: [" + window.Caption + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.Type: [" + window.Type + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.IsModal: [" + window.IsModal + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.IsDialog: [" + window.IsDialog + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.Id: [" + window.Id + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.IsKillable: [" + window.IsKillable + "]", Logging.White);
								Logging.Log("Questor", "Debug_Window.Html: [" + window.Html + "]", Logging.White);
							}
							
						}
						else
						{
							Logging.Log("Questor", "DebugWindows: No Windows Found", Logging.White);
						}
						_States.CurrentQuestorState = QuestorState.Error;
						return;

					case QuestorState.DebugInventoryTree:

						if (Cache.Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
						{
							Logging.Log("DebugInventoryTree", "ExpandCorpHangar executed", Logging.Teal);
						}
						Logging.Log("DebugInventoryTree", "--------------------------------------------------", Logging.Orange);
						Logging.Log("DebugInventoryTree", "InventoryWindow.Name: [" + Cache.Instance.PrimaryInventoryWindow.Name + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.Caption: [" + Cache.Instance.PrimaryInventoryWindow.Caption + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.Type: [" + Cache.Instance.PrimaryInventoryWindow.Type + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.IsModal: [" + Cache.Instance.PrimaryInventoryWindow.IsModal + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.IsDialog: [" + Cache.Instance.PrimaryInventoryWindow.IsDialog + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.Id: [" + Cache.Instance.PrimaryInventoryWindow.Id + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.IsKillable: [" + Cache.Instance.PrimaryInventoryWindow.IsKillable + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.IsReady: [" + Cache.Instance.PrimaryInventoryWindow.IsReady + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.LocationFlag: [" + Cache.Instance.PrimaryInventoryWindow.LocationFlag + "]", Logging.White);
						Logging.Log("DebugInventoryTree", "InventoryWindow.currInvIdName: " + Cache.Instance.PrimaryInventoryWindow.currInvIdName, Logging.Red);
						Logging.Log("DebugInventoryTree", "InventoryWindow.currInvIdName: " + Cache.Instance.PrimaryInventoryWindow.currInvIdItem, Logging.Red);

						foreach (Int64 itemInTree in Cache.Instance.IDsinInventoryTree)
						{
							if (Cache.Instance.PrimaryInventoryWindow.GetIdsFromTree(false).Contains(itemInTree))
							{
								Cache.Instance.PrimaryInventoryWindow.SelectTreeEntryByID(itemInTree);
								Cache.Instance.IDsinInventoryTree.Remove(itemInTree);
								break;
							}
						}
						break;
						
					case QuestorState.DebugMarket:
						
						
						int ammoTypeId = 2456;
						// Is there a market window?
						DirectMarketWindow marketWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();


						var daysLeft = Cache.Instance.DirectEve.Me.DaysLeftOnAccount;
						Logging.Log("RunOnceAfterStartup", "Cache.Instance.DirectEve.Me.DaysLeftOnAccount [" + daysLeft + "]", Logging.Debug);
						
						
						
						
						if (Cache.Instance.ItemHangar == null) return;
						if (Cache.Instance.ItemHangar.Items.Any())
						{
							
							
							foreach (var ammo in Combat.Ammo)
							{

								var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Quantity);
								var totalStacksize = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);
								int minQty = ammo.Quantity * 20;

								Logging.Log("BuyAmmo", "Total ammo amount in hangar type [" + ammo.TypeId + "] stacksize [" + totalStacksize + "] Minimum amount [" + minQty + "] We're going to buy ammo.", Logging.White);
								Logging.Log("BuyAmmo", "Total ammo amount in hangar type [" + ammo.TypeId + "] quantity [" + totalQuantity + "] Minimum amount [" + minQty + "] We're going to buy ammo.", Logging.White);
								
								
								

							}
							
							
							var totalQ = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Quantity);
							var totalS = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Stacksize);
							

							Logging.Log("BuyAmmo", "Total ammo amount in hangar type [" + ammoTypeId + "] stacksize [" + totalS + "] Minimum amount [" + "" + "] We're going to buy ammo.", Logging.White);
							Logging.Log("BuyAmmo", "Total ammo amount in hangar type [" + ammoTypeId + "] quantity [" + totalQ + "] Minimum amount [" + "" + "] We're going to buy ammo.", Logging.White);

							
						}
						

						//                        // We do not have enough ammo, open the market window
						//                        if (marketWindow == null)
//						{
//							_nextPulse = DateTime.UtcNow.AddSeconds(10);
//
//							Logging.Log("BuyAmmo", "Opening market window", Logging.White);
//
//							Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
//							Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
//							return;
//						}
//
//						// Wait for the window to become ready
//						if (!marketWindow.IsReady)
//						{
//							return;
//						}
//
//						// Are we currently viewing the correct ammo orders?
//						if (marketWindow.DetailTypeId != ammoTypeId)
//						{
//							// No, load the ammo orders
//							marketWindow.LoadTypeId(ammoTypeId);
//
//							Logging.Log("BuyAmmo", "Loading market window", Logging.White);
//
//							_nextPulse = DateTime.UtcNow.AddSeconds(10);
//							return;
//						}
//
//						// Get the median sell price
//						DirectInvType type;
//						Cache.Instance.DirectEve.InvTypes.TryGetValue(ammoTypeId, out type);
//
//						var currentAmmoDirectItem = type;
//						double maxPrice = 0;
//
//						if (currentAmmoDirectItem != null)
//						{
//							double avgPrice = currentAmmoDirectItem.GetAverAgePrice;
//							double basePrice = currentAmmoDirectItem.BasePrice/currentAmmoDirectItem.PortionSize;
//
//							Logging.Log("BuyAmmo", "Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]", Logging.Orange);
//						}
//
						break;
						
						
					case QuestorState.DebugDirectionalScanner:
						
						OpenDirectionalScanner();
						
						if(!IsDirectionalScannerReady())
						{
							return;
						}
						
						var w = DirectScannerWindow;
						
						if(DirectScannerWindow != null) {
							
							Logging.Log("DebugBehavior.DebugDirectionalScanner", "if(DirectScannerWindow != null)", Logging.White);
							
							Logging.Log("DebugBehavior.DebugDirectionalScanner", " window.UserOverViewPreset [" + w.UserOverViewPreset + "]", Logging.White);
							Logging.Log("DebugBehavior.DebugDirectionalScanner", " window.Angle [" + w.Angle + "]", Logging.White);
							Logging.Log("DebugBehavior.DebugDirectionalScanner", " window.Range [" + w.Range + "]", Logging.White);

							
						}
						
						SetUseOverviewPresetFalse();
						ScanRangeTest();
						ScanAngleTest();
						
						
						foreach(var ent in w.DirectionalScanResults) {
							Logging.Log("DebugBehavior.Traveler", " ent.typeId [" + ent.TypeId + "]", Logging.White);
						}
						
						break;
						
						
					case QuestorState.DebugModules:
						
						if(!Cache.Instance.InStation) {
							return;
						}
						
						var offlineModules = Cache.Instance.Modules.Where(m => !m.IsOnline);
						
						Console.WriteLine("------------------------------");
						
						if(offlineModules.Count() > 0) {
							foreach(var module in offlineModules) {
								
								Console.WriteLine(module.TypeName);
//								break;
							}
						}
						
						
						_nextPulse = DateTime.UtcNow.AddSeconds(10);
						
						
						break;
						
				}
			}
			catch (Exception ex)
			{
				Logging.Log("Questor.EVEOnFrame", "Exception [" + ex + "]", Logging.Debug);
				return;
			}
		}
		
		
		#region Scanner Functions

		private void OpenDirectionalScanner()
		{
			
			DirectDirectionalScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			if (scanner == null)
			{
				Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDirectionalScanner);
			}
		}

		private bool IsDirectionalScannerReady()
		{
			DirectDirectionalScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			if (scanner != null && scanner.IsReady)
			{
				Logging.Log("IsScannerReady", "scanner: [" + scanner + "]", Logging.Debug);
				return true;
			}
			else
			{
				Logging.Log("IsScannerReady", "scanner is not yet ready", Logging.Debug);
				return false;
			}
		}
		
		private DirectDirectionalScannerWindow DirectScannerWindow {
			get {
				return Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			}
		}
		
		private void SetUseOverviewPresetFalse() {
			
			DirectDirectionalScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			if (scanner != null && scanner.IsReady && scanner.UserOverViewPreset)
			{
				scanner.UserOverViewPreset = false;
			}
		}

		private void ScanRangeTest()
		{
			DirectDirectionalScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			if (scanner != null && scanner.IsReady && scanner.Range != scanner.MAX_SCANNER_RANGE)
			{
				scanner.Range = scanner.MAX_SCANNER_RANGE;
			}
		}
		
		private void ScanAngleTest()
		{
			DirectDirectionalScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			if (scanner != null && scanner.IsReady && scanner.Angle != 360)
			{
				scanner.Angle = 360;
			}
		}


		private void DumpDirectionalScanResults()
		{
			
			var scanner = Cache.Instance.DirectEve.Windows.OfType<DirectDirectionalScannerWindow>().FirstOrDefault();
			if (scanner != null && scanner.IsReady)
			{
				foreach (DirectDirectionalScanResult result in scanner.DirectionalScanResults)
				{
					var entity = result.Entity;
					if (entity != null && entity.IsValid)
					{
						Logging.Log("DumpScanResults", "SR: name [" + result.Name + "] TypeName [" + result.TypeName + "] Distance [" + Math.Round(entity.Distance / 1000, 2) + "k]", Logging.Debug);
					}
					else
					{
						Logging.Log("DumpScanResults", "SR: name [" + result.Name + "] TypeName [" + result.TypeName + "]", Logging.Debug);
					}
				}
			}
		}

		#endregion Old Scanner Functions

		#region IDisposable implementation
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		private bool m_Disposed = false;
		
		protected virtual void Dispose(bool disposing)
		{
			if (!m_Disposed)
			{
				if (disposing)
				{
					//
					// Close any open files here...
					//

				}
				
				// Unmanaged resources are released here.
				
				m_Disposed = true;
			}
		}
		
		~Questor()
		{
			Dispose(false);
		}
		
		#endregion
	}
}