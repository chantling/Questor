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
		
		public DirectContainerWindow PrimaryInventoryWindow { get; set; }
		public DirectContainerWindow corpAmmoHangarSecondaryWindow { get; set; }
		public DirectContainerWindow corpLootHangarSecondaryWindow { get; set; }
		public DirectLoyaltyPointStoreWindow _lpStore;
		private DirectFittingManagerWindow _fittingManagerWindow;
		public DirectMarketWindow MarketWindow { get; set; }
		
		
		public DirectWindow GetWindowByCaption(string caption)
		{
			return Windows.FirstOrDefault(w => w.Caption.Contains(caption));
		}

		public DirectWindow GetWindowByName(string name)
		{
			DirectWindow WindowToFind = null;
			try
			{
				if (!Cache.Instance.Windows.Any())
				{
					return null;
				}

				// Special cases
				if (name == "Local")
				{
					WindowToFind = Windows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_solarsystemid"));
				}

				if (WindowToFind == null)
				{
					WindowToFind = Windows.FirstOrDefault(w => w.Name == name);
				}

				if (WindowToFind != null)
				{
					return WindowToFind;
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.GetWindowByName", "Exception [" + exception + "]", Logging.Debug);
			}

			return null;
		}
		
		public bool DebugInventoryWindows(string module)
		{
			List<DirectWindow> windows = Cache.Instance.Windows;

			Logging.Log(module, "DebugInventoryWindows: *** Start Listing Inventory Windows ***", Logging.White);
			int windownumber = 0;
			foreach (DirectWindow window in windows)
			{
				if (window.Type.ToLower().Contains("inventory"))
				{
					windownumber++;
					Logging.Log(module, "----------------------------  #[" + windownumber + "]", Logging.White);
					Logging.Log(module, "DebugInventoryWindows.Name:    [" + window.Name + "]", Logging.White);
					Logging.Log(module, "DebugInventoryWindows.Type:    [" + window.Type + "]", Logging.White);
					Logging.Log(module, "DebugInventoryWindows.Caption: [" + window.Caption + "]", Logging.White);
				}
			}
			Logging.Log(module, "DebugInventoryWindows: ***  End Listing Inventory Windows  ***", Logging.White);
			return true;
		}

		public bool StackCargoHold(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				return false;

			if (DateTime.UtcNow < Time.Instance.LastStackCargohold.AddSeconds(90))
				return true;

			try
			{
				Logging.Log(module, "Stacking CargoHold: waiting [" + Math.Round(Time.Instance.NextOpenCargoAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
				if (Cache.Instance.CurrentShipsCargo != null)
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
							if (Cache.Instance.CurrentShipsCargo.Items.Any())
							{
								Time.Instance.LastStackCargohold = DateTime.UtcNow;
								Cache.Instance.CurrentShipsCargo.StackAll();
								Cache.Instance.StackHangarAttempts++;
								return false;
							}

							return true;
						}
					}
					catch (Exception exception)
					{
						Logging.Log(module, "Stacking Item Hangar failed [" + exception + "]", Logging.Teal);
						return true;
					}
				}
				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackCargoHold", "Unable to complete StackCargoHold [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool CloseCargoHold(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				return false;

			try
			{
				if (DateTime.UtcNow < Time.Instance.NextOpenCargoAction)
				{
					if ((DateTime.UtcNow.Subtract(Time.Instance.NextOpenCargoAction).TotalSeconds) > 0)
					{
						Logging.Log("CloseCargoHold", "waiting [" + Math.Round(Time.Instance.NextOpenCargoAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					}

					return false;
				}

				if (Cache.Instance.CurrentShipsCargo == null || Cache.Instance.CurrentShipsCargo.Window == null)
				{
					Cache.Instance._currentShipsCargo = null;
					Logging.Log("CloseCargoHold", "Cargohold was not open, no need to close", Logging.White);
					return true;
				}

				if (Cache.Instance.InStation || Cache.Instance.InSpace) //do we need to special case pods here?
				{
					if (Cache.Instance.CurrentShipsCargo.Window == null)
					{
						Cache.Instance._currentShipsCargo = null;
						Logging.Log("CloseCargoHold", "Cargohold is closed", Logging.White);
						return true;
					}

					if (!Cache.Instance.CurrentShipsCargo.Window.IsReady)
					{
						//Logging.Log(module, "cargo window is not ready", Logging.White);
						return false;
					}

					if (Cache.Instance.CurrentShipsCargo.Window.IsReady)
					{
						Cache.Instance.CurrentShipsCargo.Window.Close();
						Statistics.LogWindowActionToWindowLog("CargoHold", "Closing CargoHold");
						Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(1, 2));
						return false;
					}

					Cache.Instance._currentShipsCargo = null;
					Logging.Log("CloseCargoHold", "Cargohold is probably closed", Logging.White);
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseCargoHold", "Unable to complete CloseCargoHold [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool OpenInventoryWindow(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			Cache.Instance.PrimaryInventoryWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Name.Contains("Inventory"));

			if (Cache.Instance.PrimaryInventoryWindow == null)
			{
				if (Logging.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow is null, opening InventoryWindow", Logging.Teal);

				// No, command it to open
				Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenInventory);
				Statistics.LogWindowActionToWindowLog("Inventory (main)", "Open Inventory");
				Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 3));
				Logging.Log(module, "Opening Inventory Window: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
				return false;
			}

			if (Cache.Instance.PrimaryInventoryWindow != null)
			{
				if (Logging.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow exists", Logging.Teal);
				if (Cache.Instance.PrimaryInventoryWindow.IsReady)
				{
					if (Logging.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow exists and is ready", Logging.Teal);
					return true;
				}

				//
				// if the InventoryWindow "hangs" and is never ready we will hang... it would be better if we set a timer
				// and closed the inventorywindow that is not ready after 10-20seconds. (can we close a window that is in a state if !window.isready?)
				//
				return false;
			}

			return false;
		}

		public DirectLoyaltyPointStoreWindow LPStore
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_lpStore == null)
						{
							if (!Cache.Instance.InStation)
							{
								Logging.Log("LPStore", "Opening LP Store: We are not in station?! There is no LP Store in space, waiting...", Logging.Orange);
								return null;
							}

							if (Cache.Instance.InStation)
							{
								_lpStore = Cache.Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
								
								if (_lpStore == null)
								{
									if (DateTime.UtcNow > Time.Instance.NextLPStoreAction)
									{
										Logging.Log("LPStore", "Opening loyalty point store", Logging.White);
										Time.Instance.NextLPStoreAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(30, 240));
										Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
										Statistics.LogWindowActionToWindowLog("LPStore", "Opening LPStore");
										return null;
									}

									return null;
								}

								return _lpStore;
							}

							return null;
						}

						return _lpStore;
					}

					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("LPStore", "Unable to define LPStore [" + exception + "]", Logging.Teal);
					return null;
				}
			}
			private set
			{
				_lpStore = value;
			}
		}

		public bool CloseLPStore(string module)
		{
			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			if (!Cache.Instance.InStation)
			{
				Logging.Log(module, "Closing LP Store: We are not in station?!", Logging.Orange);
				return false;
			}

			if (Cache.Instance.InStation)
			{
				Cache.Instance.LPStore = Cache.Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
				if (Cache.Instance.LPStore != null)
				{
					Logging.Log(module, "Closing loyalty point store", Logging.White);
					Cache.Instance.LPStore.Close();
					Statistics.LogWindowActionToWindowLog("LPStore", "Closing LPStore");
					return false;
				}

				return true;
			}

			return true; //if we are not in station then the LP Store should have auto closed already.
		}
		
		public DirectFittingManagerWindow FittingManagerWindow
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_fittingManagerWindow == null)
						{
							if (!Cache.Instance.InStation || Cache.Instance.InSpace)
							{
								Logging.Log("FittingManager", "Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...", Logging.Debug);
								return null;
							}

							if (Cache.Instance.InStation)
							{
								if (Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().Any())
								{
									DirectFittingManagerWindow __fittingManagerWindow = Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
									if (__fittingManagerWindow != null && __fittingManagerWindow.IsReady)
									{
										_fittingManagerWindow = __fittingManagerWindow;
										return _fittingManagerWindow;
									}
								}

								if (DateTime.UtcNow > Time.Instance.NextWindowAction)
								{
									Logging.Log("FittingManager", "Opening Fitting Manager Window", Logging.White);
									Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(10, 24));
									Cache.Instance.DirectEve.OpenFitingManager();
									Statistics.LogWindowActionToWindowLog("FittingManager", "Opening FittingManager");
									return null;
								}

								if (Logging.DebugFittingMgr) Logging.Log("FittingManager", "NextWindowAction is still in the future [" + Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds + "] sec", Logging.Debug);
								return null;
							}

							return null;
						}

						return _fittingManagerWindow;
					}

					Logging.Log("FittingManager", "Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...", Logging.Debug);
					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("FittingManager", "Unable to define FittingManagerWindow [" + exception + "]", Logging.Teal);
					return null;
				}
			}
			set
			{
				_fittingManagerWindow = value;
			}
		}

		public bool CloseFittingManager(string module)
		{
			if (Settings.Instance.UseFittingManager)
			{
				if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				{
					return false;
				}

				if (Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault() != null)
				{
					Logging.Log(module, "Closing Fitting Manager Window", Logging.White);
					Cache.Instance.FittingManagerWindow.Close();
					Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
					Cache.Instance.FittingManagerWindow = null;
					Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
					return true;
				}
				
				return true;
			}

			return true;
		}		

		public bool OpenMarket(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextWindowAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				Cache.Instance.MarketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
				
				// Is the Market window open?
				if (Cache.Instance.MarketWindow == null)
				{
					// No, command it to open
					Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
					Statistics.LogWindowActionToWindowLog("MarketWindow", "Opening MarketWindow");
					Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 4));
					Logging.Log(module, "Opening Market Window: waiting [" + Math.Round(Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					return false;
				}

				return true; //if MarketWindow is not null then the window must be open.
			}

			return false;
		}

		public bool CloseMarket(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextWindowAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				Cache.Instance.MarketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

				// Is the Market window open?
				if (Cache.Instance.MarketWindow == null)
				{
					//already closed
					return true;
				}

				//if MarketWindow is not null then the window must be open, so close it.
				Cache.Instance.MarketWindow.Close();
				Statistics.LogWindowActionToWindowLog("MarketWindow", "Closing MarketWindow");
				return true;
			}

			return true;
		}

		public bool OpenContainerInSpace(string module, EntityCache containerToOpen)
		{
			if (DateTime.UtcNow < Time.Instance.NextLootAction)
			{
				return false;
			}

			if (Cache.Instance.InSpace && containerToOpen.Distance <= (int)Distances.ScoopRange)
			{
				Cache.Instance.ContainerInSpace = Cache.Instance.DirectEve.GetContainer(containerToOpen.Id);

				if (Cache.Instance.ContainerInSpace != null)
				{
					if (Cache.Instance.ContainerInSpace.Window == null)
					{
						if (containerToOpen.OpenCargo())
						{
							Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
							Logging.Log(module, "Opening Container: waiting [" + Math.Round(Time.Instance.NextLootAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " sec]", Logging.White);
							return false;
						}

						return false;
					}

					if (!Cache.Instance.ContainerInSpace.Window.IsReady)
					{
						Logging.Log(module, "Container window is not ready", Logging.White);
						return false;
					}

					if (Cache.Instance.ContainerInSpace.Window.IsPrimary())
					{
						Logging.Log(module, "Opening Container window as secondary", Logging.White);
						Cache.Instance.ContainerInSpace.Window.OpenAsSecondary();
						Statistics.LogWindowActionToWindowLog("ContainerInSpace", "Opening ContainerInSpace");
						Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
						return true;
					}
				}

				return true;
			}
			Logging.Log(module, "Not in space or not in scoop range", Logging.Orange);
			return true;
		}

		public bool RepairItems(string module)
		{
			try
			{

				if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(5) && !Cache.Instance.InSpace || DateTime.UtcNow < Time.Instance.NextRepairItemsAction) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				{
					//Logging.Log(module, "Waiting...", Logging.Orange);
					return false;
				}

				if (!Cache.Instance.Windows.Any())
				{
					return false;
				}

				Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));

				if (Cache.Instance.InStation && !Cache.Instance.DirectEve.hasRepairFacility())
				{
					Logging.Log(module, "This station does not have repair facilities to use! aborting attempt to use non-existent repair facility.", Logging.Orange);
					return true;
				}

				if (Cache.Instance.InStation)
				{
					DirectRepairShopWindow repairWindow = Cache.Instance.Windows.OfType<DirectRepairShopWindow>().FirstOrDefault();

					DirectWindow repairQuote = Cache.Instance.GetWindowByName("Set Quantity");

					if (doneUsingRepairWindow)
					{
						doneUsingRepairWindow = false;
						if (repairWindow != null) repairWindow.Close();
						return true;
					}

					foreach (DirectWindow window in Cache.Instance.Windows)
					{
						if (window.Name == "modal")
						{
							if (!string.IsNullOrEmpty(window.Html))
							{
								if (window.Html.Contains("Repairing these items will cost"))
								{
									if (window.Html != null) Logging.Log("RepairItems", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
									Logging.Log(module, "Closing Quote for Repairing All with YES", Logging.White);
									window.AnswerModal("Yes");
									doneUsingRepairWindow = true;
									return false;
								}

								if (window.Html.Contains("How much would you like to repair?"))
								{
									if (window.Html != null) Logging.Log("RepairItems", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
									Logging.Log(module, "Closing Quote for Repairing All with OK", Logging.White);
									window.AnswerModal("OK");
									doneUsingRepairWindow = true;
									return false;
								}
							}
						}
					}

					if (repairQuote != null && repairQuote.IsModal && repairQuote.IsKillable)
					{
						if (repairQuote.Html != null) Logging.Log("RepairItems", "Content of modal window (HTML): [" + (repairQuote.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
						Logging.Log(module, "Closing Quote for Repairing All with OK", Logging.White);
						repairQuote.AnswerModal("OK");
						doneUsingRepairWindow = true;
						return false;
					}

					if (repairWindow == null)
					{
						Logging.Log(module, "Opening repairshop window", Logging.White);
						Cache.Instance.DirectEve.OpenRepairShop();
						Statistics.LogWindowActionToWindowLog("RepairWindow", "Opening RepairWindow");
						Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 3));
						return false;
					}

					if (Cache.Instance.ItemHangar == null) {
						Logging.Log(module, "if (Cache.Instance.ItemHangar == null)", Logging.White);
						return false;
					}
					if (Cache.Instance.ShipHangar == null) {
						Logging.Log(module, "if (Cache.Instance.ShipHangar == null)", Logging.White);
						return false;
					}
					
					if (Drones.UseDrones)
					{
						if (Drones.DroneBay == null) return false;
					}
					
					if(Cache.Instance.ShipHangar.Items == null)
					{
						Logging.Log(module, "Cache.Instance.ShipHangar.Items == null", Logging.White);
						return false;
					}

					//repair ships in ships hangar
					List<DirectItem> repairAllItems = Cache.Instance.ShipHangar.Items;

					//repair items in items hangar and drone bay of active ship also
					repairAllItems.AddRange(Cache.Instance.ItemHangar.Items);
					if (Drones.UseDrones)
					{
						repairAllItems.AddRange(Drones.DroneBay.Items);
					}

					if (repairAllItems.Any())
					{
						if (String.IsNullOrEmpty(repairWindow.AvgDamage()))
						{
							Logging.Log(module, "Add items to repair list", Logging.White);
							repairWindow.RepairItems(repairAllItems);
							Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));
							return false;
						}

						Logging.Log(module, "Repairing Items: repairWindow.AvgDamage: " + repairWindow.AvgDamage(), Logging.White);
						if (repairWindow.AvgDamage() == "Avg: 0,0 % Damaged")
						{
							Logging.Log(module, "Repairing Items: Zero Damage: skipping repair.", Logging.White);
							repairWindow.Close();
							Statistics.LogWindowActionToWindowLog("RepairWindow", "Closing RepairWindow");
							return true;
						}

						repairWindow.RepairAll();
						Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));
						return false;
					}

					Logging.Log(module, "No items available, nothing to repair.", Logging.Orange);
					return true;
				}
				Logging.Log(module, "Not in station.", Logging.Orange);
				return false;
			}
			catch (Exception ex)
			{
				Logging.Log("Cache.RepairItems", "Exception:" + ex, Logging.White);
				return false;
			}
		}
		
		public bool ClosePrimaryInventoryWindow(string module)
		{
			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				return false;

			//
			// go through *every* window
			//
			try
			{
				foreach (DirectWindow window in Cache.Instance.Windows)
				{
					if (window.Type.Contains("form.Inventory"))
					{
						if (Logging.DebugHangars) Logging.Log(module, "ClosePrimaryInventoryWindow: Closing Primary Inventory Window Named [" + window.Name + "]", Logging.White);
						window.Close();
						Statistics.LogWindowActionToWindowLog("Inventory (main)", "Close Inventory");
						Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddMilliseconds(500);
						return false;
					}
				}

				return true;
			}
			catch (Exception exception)
			{
				Logging.Log("ClosePrimaryInventoryWindow", "Unable to complete ClosePrimaryInventoryWindow [" + exception + "]", Logging.Teal);
				return false;
			}
		}
		
		public bool ListInvTree(string module)
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
					List<long> idsInInvTreeView = Cache.Instance.PrimaryInventoryWindow.GetIdsFromTree(false);
					if (Logging.DebugHangars) Logging.Log("OpenAndSelectInvItem", "Debug: IDs Found in the Inv Tree [" + idsInInvTreeView.Count() + "]", Logging.Teal);

					if (Cache.Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
					{
						Statistics.LogWindowActionToWindowLog("Corporate Hangar", "ExpandCorpHangar executed");
						Logging.Log(module, "ExpandCorpHangar executed", Logging.Teal);
						Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(4);
						return false;
					}

					foreach (Int64 itemInTree in idsInInvTreeView)
					{
						Logging.Log(module, "ID: " + itemInTree, Logging.Red);
					}
					return false;
				}

				return false;
			}
			catch (Exception ex)
			{
				Logging.Log("ListInvTree", "Exception [" + ex + "]", Logging.Debug);
				return false;
			}
		}
		
	}
}
