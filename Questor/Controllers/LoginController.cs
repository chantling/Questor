/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:51
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
	/// <summary>
	/// Description of LoginController.
	/// </summary>
	public class LoginController : BaseController
	{
		
		private DateTime LastServerStatusCheckWasNotOK { get; set; }
		private bool LoggedIn { get; set; }
		
		public LoginController()
		{
		}
		
		public override void DoWork()
		{
			
			if (IsWorkDone || LocalPulse > DateTime.UtcNow)
			{
				return;
			}
			
			Cache.Instance.WCFClient.GetPipeProxy.SetEveAccountAttributeValue(Cache.Instance.CharName,
				                                                                  "LastQuestorSessionReady", DateTime.UtcNow);
						
			
			if(LoggedIn || Cache.Instance.DirectEve.Session.IsReadyPreLogin) {
					Logging.Log("Successfully logged in.");
					IsWorkDone = true; // once we selected the char the work is done, of if the session is ready ( we already have been loggin in )
					return;
			}
			if(LoggedIn && !Cache.Instance.DirectEve.Session.IsReadyPreLogin) {
				LocalPulse = GetUTCNowDelaySeconds(1, 1);
				Logging.Log("Session not ready yet, waiting.");
				return;
			}
			
			
			
			if (Cache.Instance.DirectEve.Login.AtLogin || Cache.Instance.DirectEve.Login.AtCharacterSelection ||
			    Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)
			{
				
				
				if (Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)
				{
					Logging.Log("if(Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)");
					LocalPulse = GetUTCNowDelaySeconds(2, 4);
					return;
				}

				if (DateTime.UtcNow < LastServerStatusCheckWasNotOK.AddSeconds(Cache.Instance.RandomNumber(4, 7)))
				{
					Logging.Log("lastServerStatusCheckWasNotOK = [" + LastServerStatusCheckWasNotOK.ToShortTimeString() +
					            "] waiting 10 to 20 seconds.");
					return;
				}

				LastServerStatusCheckWasNotOK = DateTime.UtcNow.AddDays(-1);
				//reset this so we never hit this twice in a row w/o another server status check not being OK.

				if (DateTime.UtcNow < LocalPulse)
				{
					if (Logging.DebugOnframe)
						Logging.Log("if (DateTime.UtcNow < _nextPulse)");
					return;
				}

				if (Logging.DebugOnframe) Logging.Log("Pulse...");


				LocalPulse = DateTime.UtcNow.AddMilliseconds(Time.Instance.QuestorBeforeLoginPulseDelay_milliseconds);

				if (DateTime.UtcNow < Cache.QuestorProgramLaunched.AddSeconds(1))
				{
					//
					// do not login for the first 7 seconds, wait...
					//
					return;
				}

				if (Cache._humanInterventionRequired)
				{
					Logging.Log("OnFrame: _humanInterventionRequired is true (this will spam every second or so)");
					LocalPulse = LocalPulse.AddMinutes(2);
					return;
				}

				if (Logging.DebugOnframe)
					Logging.Log("before: if (Cache.Instance.DirectEve.Windows.Count != 0)");

				// We should not get any windows
				if (Cache.Instance.DirectEve.Windows.Count != 0)
				{
					foreach (var window in Cache.Instance.DirectEve.Windows)
					{
						if (string.IsNullOrEmpty(window.Html))
							continue;
						Logging.Log("WindowTitles:" + window.Name + "::" + window.Html);

						//
						// Close these windows and continue
						//
						if (window.Name == "telecom" && !Logging.DebugDoNotCloseTelcomWindows)
						{
							Logging.Log("Closing telecom message...");
							Logging.Log("Content of telecom window (HTML): [" +
							            (window.Html).Replace("\n", "").Replace("\r", "") + "]");
							window.Close();
							continue;
						}

						// Modal windows must be closed
						// But lets only close known modal windows
						if (window.IsModal)
						{
							var close = false;
							var restart = false;
							var needHumanIntervention = false;
							var sayYes = false;
							var sayOk = false;
							var quit = false;

							//bool update = false;

							if (!string.IsNullOrEmpty(window.Html))
							{
								//errors that are repeatable and unavoidable even after a restart of eve/questor
								needHumanIntervention = window.Html.Contains("reason: Account subscription expired");

								//update |= window.Html.Contains("The update has been downloaded");

								// Server going down
								//Logging.Log("[Startup] (1) close is: " + close);
								close |=
									window.Html.ToLower()
									.Contains("please make sure your characters are out of harms way");
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
								close |= window.Html.ToLower()
									.Contains("could not connect to the specified address");
								close |= window.Html.ToLower().Contains("connection timeout");
								close |=
									window.Html.ToLower()
									.Contains("the cluster is not currently accepting connections");
								close |= window.Html.ToLower().Contains("your character is located within");
								close |= window.Html.ToLower().Contains("the transport has not yet been connected");
								close |= window.Html.ToLower().Contains("the user's connection has been usurped");
								close |=
									window.Html.ToLower()
									.Contains("the EVE cluster has reached its maximum user limit");
								close |= window.Html.ToLower().Contains("the connection to the server was closed");
								close |= window.Html.ToLower()
									.Contains("client is already connecting to the server");

								//close |= window.Html.Contains("A client update is available and will now be installed");
								//
								// eventually it would be nice to hit ok on this one and let it update
								//
								close |=
									window.Html.ToLower()
									.Contains("client update is available and will now be installed");
								close |=
									window.Html.ToLower().Contains("change your trial account to a paying account");

								//
								// these windows require a restart of eve all together
								//
								restart |= window.Html.ToLower().Contains("the connection was closed");
								restart |= window.Html.ToLower().Contains("connection to server lost.");
								//INFORMATION
								restart |= window.Html.ToLower().Contains("local cache is corrupt");
								sayOk |= window.Html.ToLower().Contains("local session information is corrupt");
								restart |= window.Html.ToLower().Contains("The client's local session");
								// information is corrupt");
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
								sayOk |=
									window.Html.Contains(
										"The transport has not yet been connected, or authentication was not successful");
							}

							if (sayYes)
							{
								Logging.Log("Found a window that needs 'yes' chosen...");
								Logging.Log("Content of modal window (HTML): [" +
								            (window.Html).Replace("\n", "").Replace("\r", "") + "]");
								window.AnswerModal("Yes");
								continue;
							}

							if (sayOk)
							{
								Logging.Log("Found a window that needs 'ok' chosen...");
								Logging.Log("Content of modal window (HTML): [" +
								            (window.Html).Replace("\n", "").Replace("\r", "") + "]");
								window.AnswerModal("OK");
								if (
									window.Html.Contains(
										"The update has been downloaded. The client will now close and the update process begin"))
								{
									//
									// schedule the closing of launcher.exe via a timedcommand (10 min?) in the uplink...
									//
								}
								continue;
							}

							if (quit)
							{
								Logging.Log("Restarting eve...");
								Logging.Log("Content of modal window (HTML): [" +
								            (window.Html).Replace("\n", "").Replace("\r", "") + "]");
								window.AnswerModal("quit");

								//_directEve.ExecuteCommand(DirectCmd.CmdQuitGame);
							}

							if (restart)
							{
								Logging.Log("Restarting eve...");
								Logging.Log("Content of modal window (HTML): [" +
								            (window.Html).Replace("\n", "").Replace("\r", "") + "]");
								window.AnswerModal("restart");
								continue;
							}

							if (close)
							{
								Logging.Log("Closing modal window...");
								Logging.Log("Content of modal window (HTML): [" +
								            (window.Html).Replace("\n", "").Replace("\r", "") + "]");
								window.Close();
								continue;
							}

							if (needHumanIntervention)
							{
								Logging.Log("ERROR! - Human Intervention is required in this case: halting all login attempts - ERROR!");
								Logging.Log("window.Name is: " + window.Name);
								Logging.Log("window.Html is: " + window.Html);
								Logging.Log("window.Caption is: " + window.Caption);
								Logging.Log("window.Type is: " + window.Type);
								Logging.Log("window.ID is: " + window.Id);
								Logging.Log("window.IsDialog is: " + window.IsDialog);
								Logging.Log("window.IsKillable is: " + window.IsKillable);
								Logging.Log("window.Viewmode is: " + window.ViewMode);
								Logging.Log("ERROR! - Human Intervention is required in this case: halting all login attempts - ERROR!");
								Cache._humanInterventionRequired = true;
								return;
							}
						}

						if (string.IsNullOrEmpty(window.Html))
							continue;

						if (window.Name == "telecom")
							continue;
						Logging.Log("We have an unexpected window, auto login halted.");
						Logging.Log("window.Name is: " + window.Name);
						Logging.Log("window.Html is: " + window.Html);
						Logging.Log("window.Caption is: " + window.Caption);
						Logging.Log("window.Type is: " + window.Type);
						Logging.Log("window.ID is: " + window.Id);
						Logging.Log("window.IsDialog is: " + window.IsDialog);
						Logging.Log("window.IsKillable is: " + window.IsKillable);
						Logging.Log("window.Viewmode is: " + window.ViewMode);
						Logging.Log("We have got an unexpected window, auto login halted.");
						return;
					}
					return;
				}

				if (Cache.Instance.DirectEve.Login.AtLogin &&
				    Cache.Instance.DirectEve.Login.ServerStatus != "Status: OK")
				{
					Logging.Log("Server status =! OK");
					if (Cache.ServerStatusCheck <= 20) // at 10 sec a piece this would be 200+ seconds
					{
						Logging.Log("Server status[" + Cache.Instance.DirectEve.Login.ServerStatus + "] != [OK] try later");
						Cache.ServerStatusCheck++;
						//retry the server status check twice (with 1 sec delay between each) before kicking in a larger delay
						if (Cache.ServerStatusCheck > 2)
						{
							LastServerStatusCheckWasNotOK = DateTime.UtcNow;
						}

						return;
					}

					Cache.ServerStatusCheck = 0;
					Cleanup.ReasonToStopQuestor =
						"Server Status Check shows server still not ready after more than 3 min. Restarting Questor. ServerStatusCheck is [" +
						Cache.ServerStatusCheck + "]";
					Logging.Log(Cleanup.ReasonToStopQuestor);
					Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
					Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor, true);
					return;
				}
				
				Logging.Log("Server status == OK");

				if (Cache.Instance.DirectEve.Login.AtLogin && !Cache.Instance.DirectEve.Login.IsLoading &&
				    !Cache.Instance.DirectEve.Login.IsConnecting)
				{
					
						Logging.Log("Login account [" + Logging.EVELoginUserName + "]");
						Cache.Instance.DirectEve.Login.Login(Logging.EVELoginUserName, Logging.EVELoginPassword);
						LocalPulse = GetUTCNowDelaySeconds(10, 12);
						Logging.Log("Waiting for Character Selection Screen");
						return;
					
				}

				if (Cache.Instance.DirectEve.Login.AtCharacterSelection &&
				    Cache.Instance.DirectEve.Login.IsCharacterSelectionReady &&
				    !Cache.Instance.DirectEve.Login.IsConnecting && !Cache.Instance.DirectEve.Login.IsLoading)
				{
					if (DateTime.UtcNow.Subtract(Cache.EVEAccountLoginStarted).TotalMilliseconds >
					    Cache.Instance.RandomNumber(Time.Instance.CharacterSelectionDelayMinimum_seconds*1000,
					                                Time.Instance.CharacterSelectionDelayMaximum_seconds*1000) &&
					    DateTime.UtcNow > Cache.NextSlotActivate)
					{
						
						
						Logging.Log("SubEnd: " + Cache.Instance.DirectEve.Me.SubTimeEnd.ToString());
						
						Cache.Instance.WCFClient.GetPipeProxy.SetEveAccountAttributeValue(Cache.Instance.CharName,
				                                                                  "SubEnd", Cache.Instance.DirectEve.Me.SubTimeEnd);
						
						
						foreach (var slot in Cache.Instance.DirectEve.Login.CharacterSlots)
						{
							if (slot.CharId.ToString(CultureInfo.InvariantCulture) != Logging.MyCharacterName &&
							    String.Compare(slot.CharName, Logging.MyCharacterName,
							                   StringComparison.OrdinalIgnoreCase) != 0)
							{
								continue;
							}

							Logging.Log("Activating character [" + slot.CharName + "]");
							Cache.NextSlotActivate = DateTime.UtcNow.AddSeconds(5);
							slot.Activate();
							LocalPulse = GetUTCNowDelaySeconds(1,2);
							LoggedIn = true;
							return;
						}

						Logging.Log("Character id/name [" + Logging.MyCharacterName + "] not found, retrying in 10 seconds");
					}
				}

			}
		}
	}
}
