// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias Ut;

namespace Questor.Modules.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using System.Threading;
	using global::Questor.Modules.Actions;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Combat;
	using global::Questor.Modules.Lookup;
	using global::Questor.Modules.States;
	using global::Questor.Modules.Logging;
	using DirectEve;
	using global::Questor.Storylines;
	using Ut::EVE;
	using Ut;
	using Ut::WCF;
	
	public partial class Cache
	{
		private DirectContainer _shipHangar;
		public DirectContainer _itemHangar { get; set; }
		private DirectContainer _lootContainer;
		public DirectContainer _lootHangar;
		public DirectContainer _ammoHangar;
		public DirectContainer HighTierLootContainer { get; set; }

		public DirectContainer ItemHangar
		{
			get
			{
				try
				{
					if (!SafeToUseStationHangars())
					{
						//Logging.Log("ItemHangar", "if (!SafeToUseStationHangars())", Logging.Debug);
						return null;
					}

					if (!Cache.Instance.InSpace && Cache.Instance.InStation)
					{
						if (Cache.Instance._itemHangar == null)
						{
							Cache.Instance._itemHangar = Cache.Instance.DirectEve.GetItemHangar();
						}

						if (Cache.Instance.Windows.All(i => i.Type != "form.StationItems")) // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
						{
							if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(10))
							{
								Logging.Log("Cache.ItemHangar", "Opening ItemHangar", Logging.Debug);
								Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
								Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
								Time.Instance.LastOpenHangar = DateTime.UtcNow;
								return null;
							}

							if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars ) Logging.Log("Cache.ItemHangar", "ItemHangar recently opened, waiting for the window to actually appear", Logging.Debug);
							return null;
						}

						if (Cache.Instance.Windows.Any(i => i.Type == "form.StationItems"))
						{
							if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("Cache.ItemHangar", "if (Cache.Instance.Windows.Any(i => i.Type == form.StationItems))", Logging.Debug);
							return Cache.Instance._itemHangar;
						}

						if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("Cache.ItemHangar", "Not sure how we got here... ", Logging.Debug);
						return null;
					}

					if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("Cache.ItemHangar", "InSpace [" + Cache.Instance.InSpace + "] InStation [" + Cache.Instance.InStation + "] waiting...", Logging.Debug);
					return null;
				}
				catch (Exception ex)
				{
					Logging.Log("ItemHangar", "Exception [" + ex + "]", Logging.Debug);
					return null;
				}
			}

			set { _itemHangar = value; }
		}

		public bool SafeToUseStationHangars()
		{
			if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10)) //yes we are adding 10 more seconds...
			{
				if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("ItemHangar", "if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10))", Logging.Debug);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))
			{
				if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("ItemHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))", Logging.Debug);
				return false;
			}

			return true;
		}

		public bool ReadyItemsHangarSingleInstance(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				DirectContainerWindow lootHangarWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.StationItems") && w.Caption.Contains("Item hangar"));

				// Is the items hangar open?
				if (lootHangarWindow == null)
				{
					// No, command it to open
					Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
					Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
					Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(3, 5));
					Logging.Log(module, "Opening Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					return false;
				}

				Cache.Instance.ItemHangar = Cache.Instance.DirectEve.GetContainer(lootHangarWindow.currInvIdItem);
				return true;
			}

			return false;
		}

		public bool CloseItemsHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "We are in Station", Logging.Teal);
					Cache.Instance.ItemHangar = Cache.Instance.DirectEve.GetItemHangar();

					if (Cache.Instance.ItemHangar == null)
					{
						if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar was null", Logging.Teal);
						return false;
					}

					if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar exists", Logging.Teal);

					// Is the items hangar open?
					if (Cache.Instance.ItemHangar.Window == null)
					{
						Logging.Log(module, "Item Hangar: is closed", Logging.White);
						return true;
					}

					if (!Cache.Instance.ItemHangar.Window.IsReady)
					{
						if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar.window is not yet ready", Logging.Teal);
						return false;
					}

					if (Cache.Instance.ItemHangar.Window.IsReady)
					{
						Cache.Instance.ItemHangar.Window.Close();
						Statistics.LogWindowActionToWindowLog("Itemhangar", "Closing ItemHangar");
						return false;
					}
				}
				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseItemsHangar", "Unable to complete CloseItemsHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool ReadyItemsHangarAsLootHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugItemHangar) Logging.Log("ReadyItemsHangarAsLootHangar", "We are in Station", Logging.Teal);
					Cache.Instance.LootHangar = Cache.Instance.ItemHangar;
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("ReadyItemsHangarAsLootHangar", "Unable to complete ReadyItemsHangarAsLootHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool ReadyItemsHangarAsAmmoHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				if (Logging.DebugHangars) Logging.Log("ReadyItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				if (Logging.DebugHangars) Logging.Log("ReadyItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)", Logging.Teal);
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("ReadyItemsHangarAsAmmoHangar", "We are in Station", Logging.Teal);
					Cache.Instance.AmmoHangar = Cache.Instance.ItemHangar;
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("ReadyItemsHangarAsAmmoHangar", "unable to complete ReadyItemsHangarAsAmmoHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool StackItemsHangarAsLootHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(12) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Logging.DebugItemHangar) Logging.Log("StackItemsHangarAsLootHangar", "public bool StackItemsHangarAsLootHangar(String module)", Logging.Teal);

				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsLootHangar", "if (Cache.Instance.InStation)", Logging.Teal);
					if (Cache.Instance.LootHangar != null)
					{
						try
						{
							if (Cache.Instance.StackHangarAttempts > 0)
							{
								if (!WaitForLockedItems(Time.Instance.LastStackLootHangar)) return false;
								return true;
							}

							if (Cache.Instance.StackHangarAttempts <= 0)
							{
								if (LootHangar.Items.Any() && LootHangar.Items.Count() > RandomNumber(600, 800))
								{
									Logging.Log(module, "Stacking Item Hangar (as LootHangar)", Logging.White);
									Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
									Cache.Instance.LootHangar.StackAll();
									Cache.Instance.StackHangarAttempts++;
									Time.Instance.LastStackLootHangar = DateTime.UtcNow;
									Time.Instance.LastStackItemHangar = DateTime.UtcNow;
									return false;
								}

								return true;
							}

							Logging.Log(module, "Not Stacking LootHangar", Logging.White);
							return true;
						}
						catch (Exception exception)
						{
							Logging.Log(module,"Stacking Item Hangar failed ["  + exception +  "]",Logging.Teal);
							return true;
						}
					}

					if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsLootHangar", "if (!Cache.Instance.ReadyItemsHangarAsLootHangar(Cache.StackItemsHangar)) return false;", Logging.Teal);
					if (!Cache.Instance.ReadyItemsHangarAsLootHangar("Cache.StackItemsHangar")) return false;
					return false;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackItemsHangarAsLootHangar", "Unable to complete StackItemsHangarAsLootHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		private static bool WaitForLockedItems(DateTime __lastAction)
		{
			if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
			{
				if (Math.Abs(DateTime.UtcNow.Subtract(__lastAction).TotalSeconds) > 15)
				{
					Logging.Log(_States.CurrentArmState.ToString(), "Moving Ammo timed out, clearing item locks", Logging.Orange);
					Cache.Instance.DirectEve.UnlockItems();
					return false;
				}

				if (Logging.DebugUnloadLoot) Logging.Log(_States.CurrentArmState.ToString(), "Waiting for Locks to clear. GetLockedItems().Count [" + Cache.Instance.DirectEve.GetLockedItems().Count + "]", Logging.Teal);
				return false;
			}

			Cache.Instance.StackHangarAttempts = 0;
			return true;
		}

		public bool StackItemsHangarAsAmmoHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				Logging.Log("StackItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				Logging.Log("StackItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)", Logging.Teal);
				return false;
			}

			try
			{
				Logging.Log("StackItemsHangarAsAmmoHangar", "public bool StackItemsHangarAsAmmoHangar(String module)", Logging.Teal);

				if (Cache.Instance.InStation)
				{
					Logging.Log("StackItemsHangarAsAmmoHangar", "if (Cache.Instance.InStation)", Logging.Teal);
					if (Cache.Instance.AmmoHangar != null)
					{
						try
						{
							if (Cache.Instance.StackHangarAttempts > 0)
							{
								if (!WaitForLockedItems(Time.Instance.LastStackAmmoHangar)) return false;
								return true;
							}

							if (Cache.Instance.StackHangarAttempts <= 0)
							{
								Logging.Log("StackItemsHangarAsAmmoHangar", "AmmoHangar.Items.Count [" + AmmoHangar.Items.Count() + "]", Logging.White);
								if (AmmoHangar.Items.Any() && AmmoHangar.Items.Count() > RandomNumber(600, 800))
								{
									Logging.Log(module, "Stacking Item Hangar (as AmmoHangar)", Logging.White);
									Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
									Cache.Instance.AmmoHangar.StackAll();
									Cache.Instance.StackHangarAttempts++;
									Time.Instance.LastStackAmmoHangar = DateTime.UtcNow;
									Time.Instance.LastStackItemHangar = DateTime.UtcNow;
									return true;
								}

								return true;
							}

							Logging.Log(module, "Not Stacking AmmoHangar[" + "ItemHangar" + "]", Logging.White);
							return true;
						}
						catch (Exception exception)
						{
							Logging.Log(module, "Stacking Item Hangar failed [" + exception + "]", Logging.Teal);
							return true;
						}
					}

					Logging.Log("StackItemsHangarAsAmmoHangar", "if (!Cache.Instance.ReadyItemsHangarAsAmmoHangar(Cache.StackItemsHangar)) return false;", Logging.Teal);
					if (!Cache.Instance.ReadyItemsHangarAsAmmoHangar("Cache.StackItemsHangar")) return false;
					return false;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackItemsHangarAsAmmoHangar", "Unable to complete StackItemsHangarAsAmmoHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}
		
		public DirectContainer ShipHangar
		{
			get
			{
				try
				{
					if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
					{
						if (Logging.DebugHangars) Logging.Log("OpenShipsHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
						return null;
					}

					if (SafeToUseStationHangars() && !Cache.Instance.InSpace && Cache.Instance.InStation)
					{
						if (Cache.Instance._shipHangar == null)
						{
							Cache.Instance._shipHangar = Cache.Instance.DirectEve.GetShipHangar();
						}

						if (Instance.Windows.All(i => i.Type != "form.StationShips")) // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
						{
							if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(15))
							{
								Statistics.LogWindowActionToWindowLog("ShipHangar", "Opening ShipHangar");
								Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenShipHangar);
								Time.Instance.LastOpenHangar = DateTime.UtcNow;
								return null;
							}

							return null;
						}

						if (Instance.Windows.Any(i => i.Type == "form.StationShips"))
						{
							return Cache.Instance._shipHangar;
						}

						return null;
					}

					return null;
				}
				catch (Exception ex)
				{
					Logging.Log("OpenShipsHangar", "Exception [" + ex + "]", Logging.Debug);
					return null;
				}
			}

			set { _shipHangar = value; }
		}

		public bool StackShipsHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				return false;

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				return false;

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Cache.Instance.ShipHangar != null && Cache.Instance.ShipHangar.IsValid)
					{
						if (Cache.Instance.StackHangarAttempts > 0)
						{
							if (!WaitForLockedItems(Time.Instance.LastStackShipsHangar)) return false;
							return true;
						}

						if (Cache.Instance.StackHangarAttempts <= 0)
						{
							if (Cache.Instance.ShipHangar.Items.Any())
							{
								Logging.Log(module, "Stacking Ship Hangar", Logging.White);
								Time.Instance.LastStackShipsHangar = DateTime.UtcNow;
								Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(3, 5));
								Cache.Instance.ShipHangar.StackAll();
								return false;
							}

							return true;
						}
						
					}
					Logging.Log(module, "Stacking Ship Hangar: not yet ready: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					return false;
				}
				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackShipsHangar", "Unable to complete StackShipsHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool CloseShipsHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				return false;

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				return false;

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("OpenShipsHangar", "We are in Station", Logging.Teal);
					Cache.Instance.ShipHangar = Cache.Instance.DirectEve.GetShipHangar();

					if (Cache.Instance.ShipHangar == null)
					{
						if (Logging.DebugHangars) Logging.Log("OpenShipsHangar", "ShipsHangar was null", Logging.Teal);
						return false;
					}
					if (Logging.DebugHangars) Logging.Log("OpenShipsHangar", "ShipsHangar exists", Logging.Teal);

					// Is the items hangar open?
					if (Cache.Instance.ShipHangar.Window == null)
					{
						Logging.Log(module, "Ship Hangar: is closed", Logging.White);
						return true;
					}

					if (!Cache.Instance.ShipHangar.Window.IsReady)
					{
						if (Logging.DebugHangars) Logging.Log("OpenShipsHangar", "ShipsHangar.window is not yet ready", Logging.Teal);
						return false;
					}

					if (Cache.Instance.ShipHangar.Window.IsReady)
					{
						Cache.Instance.ShipHangar.Window.Close();
						Statistics.LogWindowActionToWindowLog("ShipHangar", "Close ShipHangar");
						return false;
					}
				}
				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseShipsHangar", "Unable to complete CloseShipsHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}
		

		public DirectContainer LootContainer
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_lootContainer == null)
						{
							if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
							{
								//if (Logging.DebugHangars) Logging.Log("LootContainer", "Debug: if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))", Logging.Teal);

								if (Instance.Windows.All(i => i.Type != "form.Inventory"))
									// look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
								{
									if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(10))
									{
										Statistics.LogWindowActionToWindowLog("Inventory", "Opening Inventory");
										Cache.Instance.DirectEve.OpenInventory();
										Time.Instance.LastOpenHangar = DateTime.UtcNow;
										return null;
									}
								}
								
								DirectItem firstLootContainer = Cache.Instance.LootHangar.Items.FirstOrDefault(i => i.GivenName != null && i.IsSingleton && (i.GroupId == (int)Group.FreightContainer || i.GroupId == (int)Group.AuditLogSecureContainer) && i.GivenName.ToLower() == Settings.Instance.LootContainerName.ToLower());
								if (firstLootContainer == null && Cache.Instance.LootHangar.Items.Any(i => i.IsSingleton && (i.GroupId == (int)Group.FreightContainer || i.GroupId == (int)Group.AuditLogSecureContainer)))
								{
									Logging.Log("LootContainer", "Unable to find a container named [" + Settings.Instance.LootContainerName + "], using the available unnamed container", Logging.Teal);
									firstLootContainer = Cache.Instance.LootHangar.Items.FirstOrDefault(i => i.IsSingleton && (i.GroupId == (int)Group.FreightContainer || i.GroupId == (int)Group.AuditLogSecureContainer));
								}

								if (firstLootContainer != null)
								{
									_lootContainer = Cache.Instance.DirectEve.GetContainer(firstLootContainer.ItemId);
									if (_lootContainer != null && _lootContainer.IsValid)
									{
										Logging.Log("LootContainer", "LootContainer is defined", Logging.Debug);
										return _lootContainer;
									}

									Logging.Log("LootContainer", "LootContainer is still null", Logging.Debug);
									return null;
								}

								Logging.Log("LootContainer", "unable to find LootContainer named [ " + Settings.Instance.LootContainerName.ToLower() + " ]", Logging.Orange);
								DirectItem firstOtherContainer = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.GivenName != null && i.IsSingleton && i.GroupId == (int)Group.FreightContainer);

								if (firstOtherContainer != null)
								{
									Logging.Log("LootContainer", "we did however find a container named [ " + firstOtherContainer.GivenName + " ]", Logging.Orange);
									return null;
								}

								return null;
							}

							return null;
						}

						return _lootContainer;
					}

					return null;
				}
				catch (Exception ex)
				{
					Logging.Log("LootContainer", "Exception [" + ex + "]", Logging.Debug);
					return null;
				}
			}
			set
			{
				_lootContainer = value;
			}
		}

		
		public bool OpenAndSelectInvItem(string module, long id)
		{
			try
			{
				if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(10))
				{
					if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
					return false;
				}

				if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				{
					if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (DateTime.UtcNow < NextOpenHangarAction)", Logging.Teal);
					return false;
				}

				if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: about to: if (!Cache.Instance.OpenInventoryWindow", Logging.Teal);

				if (!Cache.Instance.OpenInventoryWindow(module)) return false;

				Cache.Instance.PrimaryInventoryWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Name.Contains("Inventory"));

				if (Cache.Instance.PrimaryInventoryWindow != null && Cache.Instance.PrimaryInventoryWindow.IsReady)
				{
					if (id < 0)
					{
						//
						// this also kicks in if we have no corp hangar at all in station... can we detect that some other way?
						//
						Logging.Log("OpenAndSelectInvItem", "Inventory item ID from tree cannot be less than 0, retrying", Logging.White);
						return false;
					}

					List<long> idsInInvTreeView = Cache.Instance.PrimaryInventoryWindow.GetIdsFromTree(false);
					if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: IDs Found in the Inv Tree [" + idsInInvTreeView.Count() + "]", Logging.Teal);

					foreach (Int64 itemInTree in idsInInvTreeView)
					{
						if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: itemInTree [" + itemInTree + "][looking for: " + id, Logging.Teal);
						if (itemInTree == id)
						{
							if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: Found a match! itemInTree [" + itemInTree + "] = id [" + id + "]", Logging.Teal);
							if (Cache.Instance.PrimaryInventoryWindow.currInvIdItem != id)
							{
								if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: We do not have the right ID selected yet, select it now.", Logging.Teal);
								Cache.Instance.PrimaryInventoryWindow.SelectTreeEntryByID(id);
								Statistics.LogWindowActionToWindowLog("Select Tree Entry", "Selected Entry on Left of Primary Inventory Window");
								Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddMilliseconds(Cache.Instance.RandomNumber(2000, 4400));
								return false;
							}

							if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: We already have the right ID selected.", Logging.Teal);
							return true;
						}

						continue;
					}

					if (!idsInInvTreeView.Contains(id))
					{
						if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: if (!Cache.Instance.InventoryWindow.GetIdsFromTree(false).Contains(ID))", Logging.Teal);

						if (id >= 0 && id <= 6 && Cache.Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
						{
							Logging.Log(module, "ExpandCorpHangar executed", Logging.Teal);
							Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(4);
							return false;
						}

						foreach (Int64 itemInTree in idsInInvTreeView)
						{
							Logging.Log(module, "ID: " + itemInTree, Logging.Red);
						}

						Logging.Log(module, "Was looking for: " + id, Logging.Red);
						return false;
					}

					return false;
				}

				return false;
			}
			catch (Exception ex)
			{
				Logging.Log("OpenAndSelectInvItem", "Exception [" + ex + "]", Logging.Debug);
				return false;
			}
		}

		public bool StackLootContainer(string module)
		{
			try
			{
				if (DateTime.UtcNow.AddMinutes(10) < Time.Instance.LastStackLootContainer)
				{
					return true;
				}

				if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				{
					return false;
				}

				if (DateTime.UtcNow < Time.Instance.NextOpenLootContainerAction)
				{
					return false;
				}

				if (Cache.Instance.InStation)
				{
					if (LootContainer.Window == null)
					{
						DirectItem firstLootContainer = Cache.Instance.LootHangar.Items.FirstOrDefault(i => i.GivenName != null && i.IsSingleton && i.GroupId == (int)Group.FreightContainer && i.GivenName.ToLower() == Settings.Instance.LootContainerName.ToLower());
						if (firstLootContainer != null)
						{
							long lootContainerID = firstLootContainer.ItemId;
							if (!OpenAndSelectInvItem(module, lootContainerID))
								return false;
						}
						else
						{
							return false;
						}
					}

					if (LootContainer.Window == null || !LootContainer.Window.IsReady) return false;

					if (Cache.Instance.StackHangarAttempts > 0)
					{
						if (!WaitForLockedItems(Time.Instance.LastStackLootContainer)) return false;
						return true;
					}

					if (Cache.Instance.StackHangarAttempts <= 0)
					{
						if (Cache.Instance.LootContainer.Items.Any())
						{
							Logging.Log(module, "Loot Container window named: [ " + LootContainer.Window.Name + " ] was found and its contents are being stacked", Logging.White);
							LootContainer.StackAll();
							Time.Instance.LastStackLootContainer = DateTime.UtcNow;
							Time.Instance.LastStackLootHangar = DateTime.UtcNow;
							Time.Instance.NextOpenLootContainerAction = DateTime.UtcNow.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
							return false;
						}

						return true;
					}
				}

				return false;
			}
			catch (Exception ex)
			{
				Logging.Log("StackLootContainer", "Exception [" + ex + "]", Logging.Debug);
				return false;
			}
		}

		public bool CloseLootContainer(string module)
		{
			try
			{
				if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
				{
					if (Logging.DebugHangars) Logging.Log("CloseCorpLootHangar", "Debug: else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))", Logging.Teal);
					DirectContainerWindow lootHangarWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Caption == Settings.Instance.LootContainerName);

					if (lootHangarWindow != null)
					{
						lootHangarWindow.Close();
						Statistics.LogWindowActionToWindowLog("LootHangar", "Closing LootHangar [" + Settings.Instance.LootHangarTabName + "]");
						return false;
					}

					return true;
				}

				return true;
			}
			catch (Exception ex)
			{
				Logging.Log("CloseLootContainer", "Exception [" + ex + "]", Logging.Debug);
				return false;
			}
		}
		
		public DirectContainer LootHangar
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_lootHangar == null && DateTime.UtcNow > Time.Instance.NextOpenHangarAction)
						{

							if (Logging.DebugHangars) Logging.Log("Cache.LootHangar","Using ItemHangar as the LootHangar",Logging.Debug);
							Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
							_lootHangar = Cache.Instance.ItemHangar;
							

							return _lootHangar;
						}

						return _lootHangar;
					}

					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("LootHangar", "Unable to define LootHangar [" + exception + "]", Logging.Teal);
					return null;
				}
			}
			set
			{
				_lootHangar = value;
			}
		}

		public bool CloseLootHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName))
					{
						Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.LootHangarTabName);

						// Is the corp loot Hangar open?
						if (Cache.Instance.LootHangar != null)
						{
							Cache.Instance.corpLootHangarSecondaryWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.InventorySecondary") && w.Caption.Contains(Settings.Instance.LootHangarTabName));
							if (Logging.DebugHangars) Logging.Log("CloseCorpLootHangar", "Debug: if (Cache.Instance.LootHangar != null)", Logging.Teal);

							if (Cache.Instance.corpLootHangarSecondaryWindow != null)
							{
								// if open command it to close
								Cache.Instance.corpLootHangarSecondaryWindow.Close();
								Statistics.LogWindowActionToWindowLog("LootHangar", "Closing LootHangar [" + Settings.Instance.LootHangarTabName + "]");
								Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
								Logging.Log(module, "Closing Corporate Loot Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
								return false;
							}

							return true;
						}

						if (Cache.Instance.LootHangar == null)
						{
							if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName))
							{
								Logging.Log(module, "Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag or setting misconfiguration?", Logging.Orange);
								return true;
							}
							return false;
						}
					}
					else if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
					{
						if (Logging.DebugHangars) Logging.Log("CloseCorpLootHangar", "Debug: else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))", Logging.Teal);
						DirectContainerWindow lootHangarWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.InventorySecondary") && w.Caption.Contains(Settings.Instance.LootContainerName));

						if (lootHangarWindow != null)
						{
							lootHangarWindow.Close();
							Statistics.LogWindowActionToWindowLog("LootHangar", "Closing LootHangar [" + Settings.Instance.LootHangarTabName + "]");
							return false;
						}
						return true;
					}
					else //use local items hangar
					{
						Cache.Instance.LootHangar = Cache.Instance.DirectEve.GetItemHangar();
						if (Cache.Instance.LootHangar == null)
							return false;

						// Is the items hangar open?
						if (Cache.Instance.LootHangar.Window != null)
						{
							// if open command it to close
							Cache.Instance.LootHangar.Window.Close();
							Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Cache.Instance.RandomNumber(1, 4));
							Logging.Log(module, "Closing Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
							return false;
						}

						return true;
					}
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseLootHangar", "Unable to complete CloseLootHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool StackLootHangar(string module)
		{
			if (Math.Abs(DateTime.UtcNow.Subtract(Time.Instance.LastStackLootHangar).TotalMinutes) < 10)
			{
				return true;
			}

			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				if (Logging.DebugHangars) Logging.Log("StackLootHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				if (Logging.DebugHangars) Logging.Log("StackLootHangar", "if (DateTime.UtcNow [" + DateTime.UtcNow + "] < Cache.Instance.NextOpenHangarAction [" + Time.Instance.NextOpenHangarAction + "])", Logging.Teal);
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{

					if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
					{
						if (Logging.DebugHangars) Logging.Log("StackLootHangar", "if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))", Logging.Teal);
						//if (!Cache.Instance.StackLootContainer("Cache.StackLootContainer")) return false;
						Logging.Log("StackLootHangar", "We do not stack containers, you will need to do so manually. StackAll does not seem to work with Primary Inventory windows.", Logging.Teal);
						return true;
					}

					if (Logging.DebugHangars) Logging.Log("StackLootHangar", "!Cache.Instance.StackItemsHangarAsLootHangar(Cache.StackLootHangar))", Logging.Teal);
					if (!Cache.Instance.StackItemsHangarAsLootHangar("Cache.StackItemsHangarAsLootHangar")) return false;
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackLootHangar", "Unable to complete StackLootHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool SortLootHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				if (LootHangar != null && LootHangar.IsValid)
				{
					List<DirectItem> items = Cache.Instance.LootHangar.Items;
					foreach (DirectItem item in items)
					{
						//if (item.FlagId)
						Logging.Log(module, "Items: " + item.TypeName, Logging.White);

						//
						// add items with a high tier or faction to transferlist
						//
					}

					//
					// transfer items in transferlist to HighTierLootContainer
					//
					return true;
				}
			}

			return false;
		}

		public DirectContainer AmmoHangar
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_ammoHangar == null && DateTime.UtcNow > Time.Instance.NextOpenHangarAction)
						{
							if (Settings.Instance.AmmoHangarTabName != string.Empty)
							{
								Cache.Instance.AmmoHangarID = -99;
								Cache.Instance.AmmoHangarID = Cache.Instance.DirectEve.GetCorpHangarId(Settings.Instance.AmmoHangarTabName); //- 1;
								if (Logging.DebugHangars) Logging.Log("AmmoHangar: GetCorpAmmoHangarID", "AmmoHangarID is [" + Cache.Instance.AmmoHangarID + "]", Logging.Teal);

								_ammoHangar = null;
								Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
								_ammoHangar = Cache.Instance.DirectEve.GetCorporationHangar((int)Cache.Instance.AmmoHangarID);
								Statistics.LogWindowActionToWindowLog("AmmoHangar", "AmmoHangar Defined (not opened?)");

								if (_ammoHangar != null && _ammoHangar.IsValid) //do we have a corp hangar tab setup with that name?
								{
									if (Logging.DebugHangars)
									{
										Logging.Log("AmmoHangar", "AmmoHangar is defined (no window needed)", Logging.Debug);
										try
										{
											if (AmmoHangar.Items.Any())
											{
												int AmmoHangarItemCount = AmmoHangar.Items.Count();
												if (Logging.DebugHangars) Logging.Log("AmmoHangar", "AmmoHangar [" + Settings.Instance.AmmoHangarTabName + "] has [" + AmmoHangarItemCount + "] items", Logging.Debug);
											}
										}
										catch (Exception exception)
										{
											Logging.Log("ReadyCorpAmmoHangar", "Exception [" + exception + "]", Logging.Debug);
										}
									}

									return _ammoHangar;
								}

								Logging.Log("AmmoHangar", "Opening Corporate Ammo Hangar: failed! No Corporate Hangar in this station! lag?", Logging.Orange);
								return _ammoHangar;

							}

							if (Settings.Instance.LootHangarTabName == string.Empty && Cache.Instance._lootHangar != null)
							{
								Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
								_ammoHangar = Cache.Instance._lootHangar;
							}
							else
							{
								Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
								_ammoHangar = Cache.Instance.ItemHangar;
							}

							return _ammoHangar;
						}

						return _ammoHangar;
					}

					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("AmmoHangar", "Unable to define AmmoHangar [" + exception + "]", Logging.Teal);
					return null;
				}
			}
			set
			{
				_ammoHangar = value;
			}
		}

		public bool StackAmmoHangar(string module)
		{
			StackAmmohangarAttempts++;
			if (StackAmmohangarAttempts > 15)
			{
				Logging.Log("StackAmmoHangar", "Pausing. Stacking the ammoHangar has failed: attempts [" + StackAmmohangarAttempts + "]", Logging.Teal);
				Cache.Instance.Paused = true;
				return true;
			}
			

			if (Math.Abs(DateTime.UtcNow.Subtract(Time.Instance.LastStackAmmoHangar).TotalMinutes) < 10)
			{
				return true;
			}

			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				if (Logging.DebugHangars) Logging.Log("StackAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				if (Logging.DebugHangars) Logging.Log("StackAmmoHangar", "if (DateTime.UtcNow [" + DateTime.UtcNow + "] < Cache.Instance.NextOpenHangarAction [" + Time.Instance.NextOpenHangarAction + "])", Logging.Teal);
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{

					if (Logging.DebugHangars) Logging.Log("StackAmmoHangar", "Starting [Cache.Instance.StackItemsHangarAsAmmoHangar]", Logging.Teal);
					if (!Cache.Instance.StackItemsHangarAsAmmoHangar(module)) return false;
					if (Logging.DebugHangars) Logging.Log("StackAmmoHangar", "Finished [Cache.Instance.StackItemsHangarAsAmmoHangar]", Logging.Teal);
					StackAmmohangarAttempts = 0;
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackAmmoHangar", "Unable to complete StackAmmoHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool CloseAmmoHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
					{
						if (Logging.DebugHangars) Logging.Log("CloseCorpAmmoHangar", "Debug: if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))", Logging.Teal);

						if (Cache.Instance.AmmoHangar == null)
						{
							Cache.Instance.AmmoHangar = Cache.Instance.DirectEve.GetCorporationHangar(Settings.Instance.AmmoHangarTabName);
						}

						// Is the corp Ammo Hangar open?
						if (Cache.Instance.AmmoHangar != null)
						{
							Cache.Instance.corpAmmoHangarSecondaryWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.InventorySecondary") && w.Caption.Contains(Settings.Instance.AmmoHangarTabName));
							if (Logging.DebugHangars) Logging.Log("CloseCorpAmmoHangar", "Debug: if (Cache.Instance.AmmoHangar != null)", Logging.Teal);

							if (Cache.Instance.corpAmmoHangarSecondaryWindow != null)
							{
								if (Logging.DebugHangars) Logging.Log("CloseCorpAmmoHangar", "Debug: if (ammoHangarWindow != null)", Logging.Teal);

								// if open command it to close
								Cache.Instance.corpAmmoHangarSecondaryWindow.Close();
								Statistics.LogWindowActionToWindowLog("Ammohangar", "Closing AmmoHangar");
								Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
								Logging.Log(module, "Closing Corporate Ammo Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
								return false;
							}

							return true;
						}

						if (Cache.Instance.AmmoHangar == null)
						{
							if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
							{
								Logging.Log(module, "Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag or setting misconfiguration?", Logging.Orange);
							}

							return false;
						}
					}
					else //use local items hangar
					{
						if (Cache.Instance.AmmoHangar == null)
						{
							Cache.Instance.AmmoHangar = Cache.Instance.DirectEve.GetItemHangar();
							return false;
						}

						// Is the items hangar open?
						if (Cache.Instance.AmmoHangar.Window != null)
						{
							// if open command it to close
							if (!Cache.Instance.CloseItemsHangar(module)) return false;
							Logging.Log(module, "Closing AmmoHangar Hangar", Logging.White);
							return true;
						}

						return true;
					}
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseAmmoHangar", "Unable to complete CloseAmmoHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}
		
	}
}
