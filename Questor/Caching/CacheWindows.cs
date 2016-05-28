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
using System;
using System.Linq;
using DirectEve;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;

namespace Questor.Modules.Caching
{
    public partial class Cache
    {
        private DirectFittingManagerWindow _fittingManagerWindow;
        public DirectLoyaltyPointStoreWindow _lpStore;

        public DirectContainerWindow PrimaryInventoryWindow { get; set; }
        public DirectContainerWindow corpAmmoHangarSecondaryWindow { get; set; }
        public DirectContainerWindow corpLootHangarSecondaryWindow { get; set; }
        public DirectMarketWindow MarketWindow { get; set; }

        public DirectLoyaltyPointStoreWindow LPStore
        {
            get
            {
                try
                {
                    if (Instance.InStation)
                    {
                        if (_lpStore == null)
                        {
                            if (!Instance.InStation)
                            {
                                Logging.Logging.Log("Opening LP Store: We are not in station?! There is no LP Store in space, waiting...");
                                return null;
                            }

                            if (Instance.InStation)
                            {
                                _lpStore = Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();

                                if (_lpStore == null)
                                {
                                    if (DateTime.UtcNow > Time.Instance.NextLPStoreAction)
                                    {
                                        Logging.Logging.Log("Opening loyalty point store");
                                        Time.Instance.NextLPStoreAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(30, 240));
                                        Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
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
                    Logging.Logging.Log("Unable to define LPStore [" + exception + "]");
                    return null;
                }
            }
            private set { _lpStore = value; }
        }

        public DirectFittingManagerWindow FittingManagerWindow
        {
            get
            {
                try
                {
                    if (Instance.InStation)
                    {
                        if (_fittingManagerWindow == null)
                        {
                            if (!Instance.InStation || Instance.InSpace)
                            {
                                Logging.Logging.Log("Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...");
                                return null;
                            }

                            if (Instance.InStation)
                            {
                                if (Instance.Windows.OfType<DirectFittingManagerWindow>().Any())
                                {
                                    var __fittingManagerWindow = Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
                                    if (__fittingManagerWindow != null && __fittingManagerWindow.IsReady)
                                    {
                                        _fittingManagerWindow = __fittingManagerWindow;
                                        return _fittingManagerWindow;
                                    }
                                }

                                if (DateTime.UtcNow > Time.Instance.NextWindowAction)
                                {
                                    Logging.Logging.Log("Opening Fitting Manager Window");
                                    Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(10, 24));
                                    Instance.DirectEve.OpenFitingManager();
                                    Statistics.LogWindowActionToWindowLog("FittingManager", "Opening FittingManager");
                                    return null;
                                }

                                if (Logging.Logging.DebugFittingMgr)
                                    Logging.Logging.Log("NextWindowAction is still in the future [" + Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds +
                                        "] sec");
                                return null;
                            }

                            return null;
                        }

                        return _fittingManagerWindow;
                    }

                    Logging.Logging.Log("Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...");
                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Unable to define FittingManagerWindow [" + exception + "]");
                    return null;
                }
            }
            set { _fittingManagerWindow = value; }
        }


        public DirectWindow GetWindowByCaption(string caption)
        {
            return Windows.FirstOrDefault(w => w.Caption.Contains(caption));
        }

        public DirectWindow GetWindowByName(string name)
        {
            DirectWindow WindowToFind = null;
            try
            {
                if (!Instance.Windows.Any())
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
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            return null;
        }

        public bool DebugInventoryWindows(string module)
        {
            var windows = Instance.Windows;

            Logging.Logging.Log("DebugInventoryWindows: *** Start Listing Inventory Windows ***");
            var windownumber = 0;
            foreach (var window in windows)
            {
                if (window.Type.ToLower().Contains("inventory"))
                {
                    windownumber++;
                    Logging.Logging.Log("----------------------------  #[" + windownumber + "]");
                    Logging.Logging.Log("DebugInventoryWindows.Name:    [" + window.Name + "]");
                    Logging.Logging.Log("DebugInventoryWindows.Type:    [" + window.Type + "]");
                    Logging.Logging.Log("DebugInventoryWindows.Caption: [" + window.Caption + "]");
                }
            }
            Logging.Logging.Log("DebugInventoryWindows: ***  End Listing Inventory Windows  ***");
            return true;
        }

        public bool StackCargoHold(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.UtcNow < Time.Instance.LastStackCargohold.AddSeconds(90))
                return true;

            try
            {
                Logging.Logging.Log("Stacking CargoHold: waiting [" + Math.Round(Time.Instance.NextOpenCargoAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                if (Instance.CurrentShipsCargo != null)
                {
                    try
                    {
                        if (Instance.StackHangarAttempts > 0)
                        {
                            if (!WaitForLockedItems(Time.Instance.LastStackAmmoHangar)) return false;
                            return true;
                        }

                        if (Instance.StackHangarAttempts <= 0)
                        {
                            if (Instance.CurrentShipsCargo.Items.Any())
                            {
                                Time.Instance.LastStackCargohold = DateTime.UtcNow;
                                Instance.CurrentShipsCargo.StackAll();
                                Instance.StackHangarAttempts++;
                                return false;
                            }

                            return true;
                        }
                    }
                    catch (Exception exception)
                    {
                        Logging.Logging.Log("Stacking Item Hangar failed [" + exception + "]");
                        return true;
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete StackCargoHold [" + exception + "]");
                return true;
            }
        }

        public bool CloseCargoHold(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            try
            {
                if (DateTime.UtcNow < Time.Instance.NextOpenCargoAction)
                {
                    if ((DateTime.UtcNow.Subtract(Time.Instance.NextOpenCargoAction).TotalSeconds) > 0)
                    {
                        Logging.Logging.Log("waiting [" + Math.Round(Time.Instance.NextOpenCargoAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    }

                    return false;
                }

                if (Instance.CurrentShipsCargo == null || Instance.CurrentShipsCargo.Window == null)
                {
                    Instance._currentShipsCargo = null;
                    Logging.Logging.Log("Cargohold was not open, no need to close");
                    return true;
                }

                if (Instance.InStation || Instance.InSpace) //do we need to special case pods here?
                {
                    if (Instance.CurrentShipsCargo.Window == null)
                    {
                        Instance._currentShipsCargo = null;
                        Logging.Logging.Log("Cargohold is closed");
                        return true;
                    }

                    if (!Instance.CurrentShipsCargo.Window.IsReady)
                    {
                        //Logging.Log(module, "cargo window is not ready", Logging.White);
                        return false;
                    }

                    if (Instance.CurrentShipsCargo.Window.IsReady)
                    {
                        Instance.CurrentShipsCargo.Window.Close();
                        Statistics.LogWindowActionToWindowLog("CargoHold", "Closing CargoHold");
                        Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(1, 2));
                        return false;
                    }

                    Instance._currentShipsCargo = null;
                    Logging.Logging.Log("Cargohold is probably closed");
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete CloseCargoHold [" + exception + "]");
                return true;
            }
        }

        public bool OpenInventoryWindow(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            Instance.PrimaryInventoryWindow =
                (DirectContainerWindow) Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Name.Contains("Inventory"));

            if (Instance.PrimaryInventoryWindow == null)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("Cache.Instance.InventoryWindow is null, opening InventoryWindow");

                // No, command it to open
                Instance.DirectEve.ExecuteCommand(DirectCmd.OpenInventory);
                Statistics.LogWindowActionToWindowLog("Inventory (main)", "Open Inventory");
                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(2, 3));
                Logging.Logging.Log("Opening Inventory Window: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                return false;
            }

            if (Instance.PrimaryInventoryWindow != null)
            {
                if (Logging.Logging.DebugHangars) Logging.Logging.Log("Cache.Instance.InventoryWindow exists");
                if (Instance.PrimaryInventoryWindow.IsReady)
                {
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("Cache.Instance.InventoryWindow exists and is ready");
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

        public bool CloseLPStore(string module)
        {
            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                return false;
            }

            if (!Instance.InStation)
            {
                Logging.Logging.Log("Closing LP Store: We are not in station?!");
                return false;
            }

            if (Instance.InStation)
            {
                Instance.LPStore = Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
                if (Instance.LPStore != null)
                {
                    Logging.Logging.Log("Closing loyalty point store");
                    Instance.LPStore.Close();
                    Statistics.LogWindowActionToWindowLog("LPStore", "Closing LPStore");
                    return false;
                }

                return true;
            }

            return true; //if we are not in station then the LP Store should have auto closed already.
        }

        public bool CloseFittingManager(string module)
        {
            if (Settings.Instance.UseFittingManager)
            {
                if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                {
                    return false;
                }

                if (Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault() != null)
                {
                    Logging.Logging.Log("Closing Fitting Manager Window");
                    Instance.FittingManagerWindow.Close();
                    Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
                    Instance.FittingManagerWindow = null;
                    Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                    return true;
                }

                return true;
            }

            return true;
        }

        public bool OpenMarket(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextWindowAction)
            {
                return false;
            }

            if (Instance.InStation)
            {
                Instance.MarketWindow = Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                // Is the Market window open?
                if (Instance.MarketWindow == null)
                {
                    // No, command it to open
                    Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                    Statistics.LogWindowActionToWindowLog("MarketWindow", "Opening MarketWindow");
                    Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(2, 4));
                    Logging.Logging.Log("Opening Market Window: waiting [" + Math.Round(Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    return false;
                }

                return true; //if MarketWindow is not null then the window must be open.
            }

            return false;
        }

        public bool CloseMarket(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextWindowAction)
            {
                return false;
            }

            if (Instance.InStation)
            {
                Instance.MarketWindow = Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                // Is the Market window open?
                if (Instance.MarketWindow == null)
                {
                    //already closed
                    return true;
                }

                //if MarketWindow is not null then the window must be open, so close it.
                Instance.MarketWindow.Close();
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

            if (Instance.InSpace && containerToOpen.Distance <= (int) Distances.ScoopRange)
            {
                Instance.ContainerInSpace = Instance.DirectEve.GetContainer(containerToOpen.Id);

                if (Instance.ContainerInSpace != null)
                {
                    if (Instance.ContainerInSpace.Window == null)
                    {
                        if (containerToOpen.OpenCargo())
                        {
                            Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                            Logging.Logging.Log("Opening Container: waiting [" + Math.Round(Time.Instance.NextLootAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " sec]");
                            return false;
                        }

                        return false;
                    }

                    if (!Instance.ContainerInSpace.Window.IsReady)
                    {
                        Logging.Logging.Log("Container window is not ready");
                        return false;
                    }

                    if (Instance.ContainerInSpace.Window.IsPrimary())
                    {
                        Logging.Logging.Log("Opening Container window as secondary");
                        Instance.ContainerInSpace.Window.OpenAsSecondary();
                        Statistics.LogWindowActionToWindowLog("ContainerInSpace", "Opening ContainerInSpace");
                        Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                        return true;
                    }
                }

                return true;
            }
            Logging.Logging.Log("Not in space or not in scoop range");
            return true;
        }

        public bool RepairItems(string module)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(5) && !Instance.InSpace || DateTime.UtcNow < Time.Instance.NextRepairItemsAction)
                    // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                {
                    //Logging.Log(module, "Waiting...", Logging.Orange);
                    return false;
                }

                if (!Instance.Windows.Any())
                {
                    return false;
                }

                Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));

                if (Instance.InStation && !Instance.DirectEve.hasRepairFacility())
                {
                    Logging.Logging.Log("This station does not have repair facilities to use! aborting attempt to use non-existent repair facility.");
                    return true;
                }

                if (Instance.InStation)
                {
                    var repairWindow = Instance.Windows.OfType<DirectRepairShopWindow>().FirstOrDefault();

                    var repairQuote = Instance.GetWindowByName("Set Quantity");

                    if (doneUsingRepairWindow)
                    {
                        doneUsingRepairWindow = false;
                        if (repairWindow != null) repairWindow.Close();
                        return true;
                    }

                    foreach (var window in Instance.Windows)
                    {
                        if (window.Name == "modal")
                        {
                            if (!string.IsNullOrEmpty(window.Html))
                            {
                                if (window.Html.Contains("Repairing these items will cost"))
                                {
                                    if (window.Html != null)
                                        Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    Logging.Logging.Log("Closing Quote for Repairing All with YES");
                                    window.AnswerModal("Yes");
                                    doneUsingRepairWindow = true;
                                    return false;
                                }

                                if (window.Html.Contains("How much would you like to repair?"))
                                {
                                    if (window.Html != null)
                                        Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    Logging.Logging.Log("Closing Quote for Repairing All with OK");
                                    window.AnswerModal("OK");
                                    doneUsingRepairWindow = true;
                                    return false;
                                }
                            }
                        }
                    }

                    if (repairQuote != null && repairQuote.IsModal && repairQuote.IsKillable)
                    {
                        if (repairQuote.Html != null)
                            Logging.Logging.Log("Content of modal window (HTML): [" + (repairQuote.Html).Replace("\n", "").Replace("\r", "") + "]");
                        Logging.Logging.Log("Closing Quote for Repairing All with OK");
                        repairQuote.AnswerModal("OK");
                        doneUsingRepairWindow = true;
                        return false;
                    }

                    if (repairWindow == null)
                    {
                        Logging.Logging.Log("Opening repairshop window");
                        Instance.DirectEve.OpenRepairShop();
                        Statistics.LogWindowActionToWindowLog("RepairWindow", "Opening RepairWindow");
                        Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 3));
                        return false;
                    }

                    if (Instance.ItemHangar == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.ItemHangar == null)");
                        return false;
                    }
                    if (Instance.ShipHangar == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.ShipHangar == null)");
                        return false;
                    }

                    if (Drones.UseDrones)
                    {
                        if (Drones.DroneBay == null) return false;
                    }

                    if (Instance.ShipHangar.Items == null)
                    {
                        Logging.Logging.Log("Cache.Instance.ShipHangar.Items == null");
                        return false;
                    }

                    //repair ships in ships hangar
                    var repairAllItems = Instance.ShipHangar.Items;

                    //repair items in items hangar and drone bay of active ship also
                    repairAllItems.AddRange(Instance.ItemHangar.Items);
                    if (Drones.UseDrones)
                    {
                        repairAllItems.AddRange(Drones.DroneBay.Items);
                    }

                    if (repairAllItems.Any())
                    {
                        if (String.IsNullOrEmpty(repairWindow.AvgDamage()))
                        {
                            Logging.Logging.Log("Add items to repair list");
                            repairWindow.RepairItems(repairAllItems);
                            Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));
                            return false;
                        }

                        Logging.Logging.Log("Repairing Items: repairWindow.AvgDamage: " + repairWindow.AvgDamage());
                        if (repairWindow.AvgDamage() == "Avg: 0,0 % Damaged")
                        {
                            Logging.Logging.Log("Repairing Items: Zero Damage: skipping repair.");
                            repairWindow.Close();
                            Statistics.LogWindowActionToWindowLog("RepairWindow", "Closing RepairWindow");
                            return true;
                        }

                        repairWindow.RepairAll();
                        Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));
                        return false;
                    }

                    Logging.Logging.Log("No items available, nothing to repair.");
                    return true;
                }
                Logging.Logging.Log("Not in station.");
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception:" + ex);
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
                foreach (var window in Instance.Windows)
                {
                    if (window.Type.Contains("form.Inventory"))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("ClosePrimaryInventoryWindow: Closing Primary Inventory Window Named [" + window.Name + "]");
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
                Logging.Logging.Log("Unable to complete ClosePrimaryInventoryWindow [" + exception + "]");
                return false;
            }
        }

        public bool ListInvTree(string module)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(10))
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Debug: if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Debug: if (DateTime.UtcNow < NextOpenHangarAction)");
                    return false;
                }

                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("Debug: about to: if (!Cache.Instance.OpenInventoryWindow");

                if (!Instance.OpenInventoryWindow(module)) return false;

                Instance.PrimaryInventoryWindow =
                    (DirectContainerWindow) Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Name.Contains("Inventory"));

                if (Instance.PrimaryInventoryWindow != null && Instance.PrimaryInventoryWindow.IsReady)
                {
                    var idsInInvTreeView = Instance.PrimaryInventoryWindow.GetIdsFromTree(false);
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Debug: IDs Found in the Inv Tree [" + idsInInvTreeView.Count() + "]");

                    if (Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
                    {
                        Statistics.LogWindowActionToWindowLog("Corporate Hangar", "ExpandCorpHangar executed");
                        Logging.Logging.Log("ExpandCorpHangar executed");
                        Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(4);
                        return false;
                    }

                    foreach (var itemInTree in idsInInvTreeView)
                    {
                        Logging.Logging.Log("ID: " + itemInTree);
                    }
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }
    }
}