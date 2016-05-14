// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using global::Questor.Modules.Caching;
using global::Questor.Modules.Logging;
using global::Questor.Modules.Lookup;
using global::Questor.Modules.Activities;
using global::Questor.Modules.States;
using global::Questor.Modules.Actions;
using global::Questor.Modules.BackgroundTasks;

namespace Questor.Behaviors
{
	public class DebugHangarsBehavior
	{
		public bool PanicStateReset; //false;

		public DebugHangarsBehavior()
		{
			//
			// this is combat mission specific and needs to be generalized
			//
			_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Idle;
			_States.CurrentArmState = ArmState.Idle;
			_States.CurrentUnloadLootState = UnloadLootState.Idle;
			_States.CurrentTravelerState = TravelerState.Idle;
		}

		private void BeginClosingQuestor()
		{
			Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
			_States.CurrentQuestorState = QuestorState.CloseQuestor;
		}

		public void ProcessState()
		{
			//Logging.Log("DebugHangarsBehavior","ProcessState - every tick",Logging.Teal);
			if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
			{
				if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
				{
					_States.CurrentQuestorState = QuestorState.CloseQuestor;
					BeginClosingQuestor();
				}
			}

			if (Cache.Instance.GotoBaseNow)
			{
				_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.GotoBase;
			}
			if ((DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalSeconds > 10) && (DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalSeconds < 60))
			{
				if (Cache.Instance.QuestorJustStarted)
				{
					Cache.Instance.QuestorJustStarted = false;
					Cleanup.SessionState = "Starting Up";

					
				}
			}

			//
			// Panic always runs, not just in space
			//
			Panic.ProcessState();
			if (_States.CurrentPanicState == PanicState.Panic || _States.CurrentPanicState == PanicState.Panicking)
			{
				// If Panic is in panic state, questor is in panic States.CurrentDebugHangarBehaviorState :)
				_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Panic;

				if (PanicStateReset)
				{
					_States.CurrentPanicState = PanicState.Normal;
					PanicStateReset = false;
				}
			}
			else if (_States.CurrentPanicState == PanicState.Resume)
			{
				// Reset panic state
				_States.CurrentPanicState = PanicState.Normal;

				// Sit Idle and wait for orders.
				_States.CurrentTravelerState = TravelerState.Idle;
				_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Idle;
			}
			
			switch (_States.CurrentDebugHangarBehaviorState)
			{
				case DebugHangarsBehaviorState.Traveler:
					Salvage.OpenWrecks = false;
					List<int> destination = Cache.Instance.DirectEve.Navigation.GetDestinationPath();
					if (destination == null || destination.Count == 0)
					{
						// happens if autopilot is not set and this QuestorState is chosen manually
						// this also happens when we get to destination (!?)
						Logging.Log("DebugHangarsBehavior.Traveler", "No destination?", Logging.White);
						_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
						return;
					}
					else if (destination.Count == 1 && destination.FirstOrDefault() == 0)
						destination[0] = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;

					if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.Last())
					{
						IEnumerable<DirectBookmark> bookmarks = Cache.Instance.AllBookmarks.Where(b => b.LocationId == destination.Last()).ToList();
						if (bookmarks != null && bookmarks.Any())
							Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
						else
						{
							Logging.Log("DebugHangarsBehavior.Traveler", "Destination: [" + Cache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]", Logging.White);
							Traveler.Destination = new SolarSystemDestination(destination.Last());
						}
					}
					else
					{
						
						//we also assume you are connected during a manual set of questor into travel mode (safe assumption considering someone is at the kb)
						Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
						Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
						
						Traveler.ProcessState();



						if (_States.CurrentTravelerState == TravelerState.AtDestination)
						{
							if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
							{
								Logging.Log("DebugHangarsBehavior.Traveler", "an error has occurred", Logging.White);
								_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
								return;
							}

							if (Cache.Instance.InSpace)
							{
								Logging.Log("DebugHangarsBehavior.Traveler", "Arrived at destination (in space, Questor stopped)", Logging.White);
								_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
								return;
							}

							Logging.Log("DebugHangarsBehavior.Traveler", "Arrived at destination", Logging.White);
							_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Idle;
							return;
						}
					}
					break;

				case DebugHangarsBehaviorState.GotoNearestStation:
					if (!Cache.Instance.InSpace || Cache.Instance.InWarp) return;
					EntityCache station = null;
					if (Cache.Instance.Stations != null && Cache.Instance.Stations.Any())
					{
						station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
					}

					if (station != null)
					{
						if (station.Distance > (int)Distances.WarptoDistance)
						{
							if (station.WarpTo())
							{
								Logging.Log("DebugHangarsBehavior.GotoNearestStation", "[" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]", Logging.White);
								_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Idle;
								break;
							}

							break;
						}

						if (station.Distance < 1900)
						{
							if (station.Dock())
							{
								Logging.Log("DebugBehavior.GotoNearestStation", "[" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]", Logging.White);
								return;
								
							}
							return;
						}
						else
						{
							if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != station.Id)
							{
								if (station.Approach())
								{
									Logging.Log("DebugBehavior.GotoNearestStation", "Approaching [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]", Logging.White);
								}
							}
						}
					}
					else
					{
						_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error; //should we goto idle here?
					}
					break;

				case DebugHangarsBehaviorState.ReadyItemsHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.ReadyItemsHangar:", Logging.White);
					if (Cache.Instance.ItemHangar == null) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackItemsHangar:
					Time.Instance.LastStackItemHangar = DateTime.MinValue;
					Logging.Log("DebugHangars", "DebugHangarsState.StackItemsHangar:", Logging.White);
					if (!Cache.Instance.StackItemsHangarAsAmmoHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
					//_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseItemsHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseItemsHangar:", Logging.White);
					if (!Cache.Instance.CloseItemsHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenShipsHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenShipsHangar:", Logging.White);
					if (Cache.Instance.ShipHangar == null) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackShipsHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.StackShipsHangar:", Logging.White);
					if (!Cache.Instance.StackShipsHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseShipsHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseShipsHangar:", Logging.White);
					if (!Cache.Instance.CloseShipsHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenLootContainer:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenLootContainer:", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackLootContainer:
					Logging.Log("DebugHangars", "DebugHangarsState.StackLootContainer:", Logging.White);
					if (!Cache.Instance.StackLootContainer("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseLootContainer:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseLootContainer:", Logging.White);
					if (!Cache.Instance.CloseLootContainer("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenCorpAmmoHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenCorpAmmoHangar:", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
					Logging.Log("OpenCorpAmmoHangar", "AmmoHangar Contains [" + Cache.Instance.AmmoHangar.Items.Count() + "] Items", Logging.Debug);
					
					try
					{
						int icount = 0;
						foreach (DirectItem itemfound in Cache.Instance.AmmoHangar.Items)
						{
							icount++;
							Logging.Log("Arm.MoveItems", "Found: Name [" + itemfound.TypeName + "] Quantity [" + itemfound.Quantity + "] in the AmmoHangar", Logging.Red);
							if (icount > 20)
							{
								Logging.Log("Arm.MoveItems", "max items to log reached (over 20). there are probably more items but we only log 20 of em.", Logging.Red);
								break;
							}

							continue;
						}
					}
					catch (Exception exception)
					{
						Logging.Log("OpenCorpLootHangar", "Exception was: [" + exception + "]", Logging.Debug);
					}

					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackCorpAmmoHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.StackCorpAmmoHangar:", Logging.White);
					if (!Cache.Instance.StackCorpAmmoHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseCorpAmmoHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseCorpAmmoHangar:", Logging.White);
					if (!Cache.Instance.CloseCorpHangar("DebugHangars", "AMMO")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenCorpLootHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenCorpLootHangar:", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
					Logging.Log("OpenCorpLootHangar", "LootHangar Contains [" + Cache.Instance.LootHangar.Items.Count() + "] Items", Logging.Debug);

					try
					{
						int icount2 = 0;
						foreach (DirectItem itemfound in Cache.Instance.LootHangar.Items)
						{
							icount2++;
							Logging.Log("Arm.MoveItems", "Found: Name [" + itemfound.TypeName + "] Quantity [" + itemfound.Quantity + "] in the LootHangar", Logging.Red);
							if (icount2 > 20)
							{
								Logging.Log("Arm.MoveItems", "max items to log reached (over 20). there are probably more items but we only log 20 of em.", Logging.Red);
								break;
							}

							continue;
						}
					}
					catch (Exception exception)
					{
						Logging.Log("OpenCorpLootHangar", "Exception was: [" +  exception+ "]", Logging.Debug);
					}
					
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackCorpLootHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.StackCorpLootHangar:", Logging.White);
					if (!Cache.Instance.StackCorpLootHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseCorpLootHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseCorpLootHangar:", Logging.White);
					if (!Cache.Instance.CloseCorpHangar("DebugHangars", "LOOT")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenAmmoHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenAmmoHangar:", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackAmmoHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.StackAmmoHangar:", Logging.White);
					if (!Cache.Instance.StackAmmoHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseAmmoHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseAmmoHangar:", Logging.White);
					if (!Cache.Instance.CloseAmmoHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenLootHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenLootHangar:", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackLootHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.StackLootHangar:", Logging.White);
					if (!Cache.Instance.StackLootHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseLootHangar:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseLootHangar:", Logging.White);
					if (!Cache.Instance.CloseLootHangar("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseAllInventoryWindows:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseAllInventoryWindows:", Logging.White);
					if (!Cleanup.CloseInventoryWindows()) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenCargoHold:
					Logging.Log("DebugHangars", "DebugHangarsState.StackLootHangar:", Logging.White);
					if (Cache.Instance.CurrentShipsCargo == null) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.StackCargoHold:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseLootHangar:", Logging.White);
					if (!Cache.Instance.StackCargoHold("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.CloseCargoHold:
					Logging.Log("DebugHangars", "DebugHangarsState.CloseAllInventoryWindows:", Logging.White);
					if (!Cache.Instance.CloseCargoHold("DebugHangars")) return;
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.GetAmmoHangarID:
					Logging.Log("DebugHangars", "DebugHangarsState.GetAmmoHangarID:", Logging.White);
					if (!Cache.Instance.GetCorpAmmoHangarID()) return;
					Logging.Log("DebugHangars", "AmmoHangarId [" + Cache.Instance.AmmoHangarID + "]", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.GetLootHangarID:
					Logging.Log("DebugHangars", "DebugHangarsState.GetLootHangarID:", Logging.White);
					if (!Cache.Instance.GetCorpLootHangarID()) return;
					Logging.Log("DebugHangars", "LootHangarId [" + Cache.Instance.LootHangarID + "]", Logging.White);
					Cache.Instance.DebugInventoryWindows("DebugHangars");
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.OpenInventory:
					Logging.Log("DebugHangars", "DebugHangarsState.OpenInventory:", Logging.White);
					if (!Cache.Instance.OpenInventoryWindow("DebugHangarsState.OpenInventoryWindow")) return;
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.ListInvTree:
					Logging.Log("DebugHangars", "DebugHangarsState.ListInvTree:", Logging.White);
					if (!Cache.Instance.ListInvTree("DebugHangarsState.ListInvTree")) return;
//					_States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.Error;
					Cache.Instance.Paused = true;
					break;

				case DebugHangarsBehaviorState.Default:
					break;
			}
		}
	}
}