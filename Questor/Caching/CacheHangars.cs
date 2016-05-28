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
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Caching
{
    public partial class Cache
    {
        public DirectContainer _ammoHangar;
        private DirectContainer _lootContainer;
        public DirectContainer _lootHangar;
        private DirectContainer _shipHangar;
        public DirectContainer _itemHangar { get; set; }
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

                    if (!Instance.InSpace && Instance.InStation)
                    {
                        if (Instance._itemHangar == null)
                        {
                            Instance._itemHangar = Instance.DirectEve.GetItemHangar();
                        }

                        if (Instance.Windows.All(i => i.Type != "form.StationItems"))
                            // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
                        {
                            if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(10))
                            {
                                Logging.Logging.Log("Cache.ItemHangar", "Opening ItemHangar", Logging.Logging.Debug);
                                Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
                                Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                                Time.Instance.LastOpenHangar = DateTime.UtcNow;
                                return null;
                            }

                            if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                                Logging.Logging.Log("Cache.ItemHangar", "ItemHangar recently opened, waiting for the window to actually appear",
                                    Logging.Logging.Debug);
                            return null;
                        }

                        if (Instance.Windows.Any(i => i.Type == "form.StationItems"))
                        {
                            if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                                Logging.Logging.Log("Cache.ItemHangar", "if (Cache.Instance.Windows.Any(i => i.Type == form.StationItems))",
                                    Logging.Logging.Debug);
                            return Instance._itemHangar;
                        }

                        if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                            Logging.Logging.Log("Cache.ItemHangar", "Not sure how we got here... ", Logging.Logging.Debug);
                        return null;
                    }

                    if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Cache.ItemHangar", "InSpace [" + Instance.InSpace + "] InStation [" + Instance.InStation + "] waiting...",
                            Logging.Logging.Debug);
                    return null;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("ItemHangar", "Exception [" + ex + "]", Logging.Logging.Debug);
                    return null;
                }
            }

            set { _itemHangar = value; }
        }

        public DirectContainer ShipHangar
        {
            get
            {
                try
                {
                    if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                        // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("OpenShipsHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)",
                                Logging.Logging.Teal);
                        return null;
                    }

                    if (SafeToUseStationHangars() && !Instance.InSpace && Instance.InStation)
                    {
                        if (Instance._shipHangar == null)
                        {
                            Instance._shipHangar = Instance.DirectEve.GetShipHangar();
                        }

                        if (Instance.Windows.All(i => i.Type != "form.StationShips"))
                            // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
                        {
                            if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(15))
                            {
                                Statistics.LogWindowActionToWindowLog("ShipHangar", "Opening ShipHangar");
                                Instance.DirectEve.ExecuteCommand(DirectCmd.OpenShipHangar);
                                Time.Instance.LastOpenHangar = DateTime.UtcNow;
                                return null;
                            }

                            return null;
                        }

                        if (Instance.Windows.Any(i => i.Type == "form.StationShips"))
                        {
                            return Instance._shipHangar;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("OpenShipsHangar", "Exception [" + ex + "]", Logging.Logging.Debug);
                    return null;
                }
            }

            set { _shipHangar = value; }
        }


        public DirectContainer LootContainer
        {
            get
            {
                try
                {
                    if (Instance.InStation)
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
                                        Instance.DirectEve.OpenInventory();
                                        Time.Instance.LastOpenHangar = DateTime.UtcNow;
                                        return null;
                                    }
                                }

                                var firstLootContainer =
                                    Instance.LootHangar.Items.FirstOrDefault(
                                        i =>
                                            i.GivenName != null && i.IsSingleton &&
                                            (i.GroupId == (int) Group.FreightContainer || i.GroupId == (int) Group.AuditLogSecureContainer) &&
                                            i.GivenName.ToLower() == Settings.Instance.LootContainerName.ToLower());
                                if (firstLootContainer == null &&
                                    Instance.LootHangar.Items.Any(
                                        i => i.IsSingleton && (i.GroupId == (int) Group.FreightContainer || i.GroupId == (int) Group.AuditLogSecureContainer)))
                                {
                                    Logging.Logging.Log("LootContainer",
                                        "Unable to find a container named [" + Settings.Instance.LootContainerName + "], using the available unnamed container",
                                        Logging.Logging.Teal);
                                    firstLootContainer =
                                        Instance.LootHangar.Items.FirstOrDefault(
                                            i =>
                                                i.IsSingleton && (i.GroupId == (int) Group.FreightContainer || i.GroupId == (int) Group.AuditLogSecureContainer));
                                }

                                if (firstLootContainer != null)
                                {
                                    _lootContainer = Instance.DirectEve.GetContainer(firstLootContainer.ItemId);
                                    if (_lootContainer != null && _lootContainer.IsValid)
                                    {
                                        Logging.Logging.Log("LootContainer", "LootContainer is defined", Logging.Logging.Debug);
                                        return _lootContainer;
                                    }

                                    Logging.Logging.Log("LootContainer", "LootContainer is still null", Logging.Logging.Debug);
                                    return null;
                                }

                                Logging.Logging.Log("LootContainer",
                                    "unable to find LootContainer named [ " + Settings.Instance.LootContainerName.ToLower() + " ]", Logging.Logging.Orange);
                                var firstOtherContainer =
                                    Instance.ItemHangar.Items.FirstOrDefault(
                                        i => i.GivenName != null && i.IsSingleton && i.GroupId == (int) Group.FreightContainer);

                                if (firstOtherContainer != null)
                                {
                                    Logging.Logging.Log("LootContainer", "we did however find a container named [ " + firstOtherContainer.GivenName + " ]",
                                        Logging.Logging.Orange);
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
                    Logging.Logging.Log("LootContainer", "Exception [" + ex + "]", Logging.Logging.Debug);
                    return null;
                }
            }
            set { _lootContainer = value; }
        }

        public DirectContainer LootHangar
        {
            get
            {
                try
                {
                    if (Instance.InStation)
                    {
                        if (_lootHangar == null && DateTime.UtcNow > Time.Instance.NextOpenHangarAction)
                        {
                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("Cache.LootHangar", "Using ItemHangar as the LootHangar", Logging.Logging.Debug);
                            Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                            _lootHangar = Instance.ItemHangar;


                            return _lootHangar;
                        }

                        return _lootHangar;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("LootHangar", "Unable to define LootHangar [" + exception + "]", Logging.Logging.Teal);
                    return null;
                }
            }
            set { _lootHangar = value; }
        }

        public DirectContainer AmmoHangar
        {
            get
            {
                try
                {
                    if (Instance.InStation)
                    {
                        if (_ammoHangar == null && DateTime.UtcNow > Time.Instance.NextOpenHangarAction)
                        {
                            if (Settings.Instance.AmmoHangarTabName != string.Empty)
                            {
                                Instance.AmmoHangarID = -99;
                                Instance.AmmoHangarID = Instance.DirectEve.GetCorpHangarId(Settings.Instance.AmmoHangarTabName); //- 1;
                                if (Logging.Logging.DebugHangars)
                                    Logging.Logging.Log("AmmoHangar: GetCorpAmmoHangarID", "AmmoHangarID is [" + Instance.AmmoHangarID + "]",
                                        Logging.Logging.Teal);

                                _ammoHangar = null;
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                                _ammoHangar = Instance.DirectEve.GetCorporationHangar((int) Instance.AmmoHangarID);
                                Statistics.LogWindowActionToWindowLog("AmmoHangar", "AmmoHangar Defined (not opened?)");

                                if (_ammoHangar != null && _ammoHangar.IsValid) //do we have a corp hangar tab setup with that name?
                                {
                                    if (Logging.Logging.DebugHangars)
                                    {
                                        Logging.Logging.Log("AmmoHangar", "AmmoHangar is defined (no window needed)", Logging.Logging.Debug);
                                        try
                                        {
                                            if (AmmoHangar.Items.Any())
                                            {
                                                var AmmoHangarItemCount = AmmoHangar.Items.Count();
                                                if (Logging.Logging.DebugHangars)
                                                    Logging.Logging.Log("AmmoHangar",
                                                        "AmmoHangar [" + Settings.Instance.AmmoHangarTabName + "] has [" + AmmoHangarItemCount + "] items",
                                                        Logging.Logging.Debug);
                                            }
                                        }
                                        catch (Exception exception)
                                        {
                                            Logging.Logging.Log("ReadyCorpAmmoHangar", "Exception [" + exception + "]", Logging.Logging.Debug);
                                        }
                                    }

                                    return _ammoHangar;
                                }

                                Logging.Logging.Log("AmmoHangar", "Opening Corporate Ammo Hangar: failed! No Corporate Hangar in this station! lag?",
                                    Logging.Logging.Orange);
                                return _ammoHangar;
                            }

                            if (Settings.Instance.LootHangarTabName == string.Empty && Instance._lootHangar != null)
                            {
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                                _ammoHangar = Instance._lootHangar;
                            }
                            else
                            {
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                                _ammoHangar = Instance.ItemHangar;
                            }

                            return _ammoHangar;
                        }

                        return _ammoHangar;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("AmmoHangar", "Unable to define AmmoHangar [" + exception + "]", Logging.Logging.Teal);
                    return null;
                }
            }
            set { _ammoHangar = value; }
        }

        public bool SafeToUseStationHangars()
        {
            if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10)) //yes we are adding 10 more seconds...
            {
                if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                    Logging.Logging.Log("ItemHangar", "if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10))", Logging.Logging.Debug);
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))
            {
                if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                    Logging.Logging.Log("ItemHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))", Logging.Logging.Debug);
                return false;
            }

            return true;
        }

        public bool ReadyItemsHangarSingleInstance(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                return false;
            }

            if (Instance.InStation)
            {
                var lootHangarWindow =
                    (DirectContainerWindow) Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.StationItems") && w.Caption.Contains("Item hangar"));

                // Is the items hangar open?
                if (lootHangarWindow == null)
                {
                    // No, command it to open
                    Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                    Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
                    Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(3, 5));
                    Logging.Logging.Log(module,
                        "Opening Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]",
                        Logging.Logging.White);
                    return false;
                }

                Instance.ItemHangar = Instance.DirectEve.GetContainer(lootHangarWindow.currInvIdItem);
                return true;
            }

            return false;
        }

        public bool CloseItemsHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenItemsHangar", "We are in Station", Logging.Logging.Teal);
                    Instance.ItemHangar = Instance.DirectEve.GetItemHangar();

                    if (Instance.ItemHangar == null)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenItemsHangar", "ItemsHangar was null", Logging.Logging.Teal);
                        return false;
                    }

                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenItemsHangar", "ItemsHangar exists", Logging.Logging.Teal);

                    // Is the items hangar open?
                    if (Instance.ItemHangar.Window == null)
                    {
                        Logging.Logging.Log(module, "Item Hangar: is closed", Logging.Logging.White);
                        return true;
                    }

                    if (!Instance.ItemHangar.Window.IsReady)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenItemsHangar", "ItemsHangar.window is not yet ready", Logging.Logging.Teal);
                        return false;
                    }

                    if (Instance.ItemHangar.Window.IsReady)
                    {
                        Instance.ItemHangar.Window.Close();
                        Statistics.LogWindowActionToWindowLog("Itemhangar", "Closing ItemHangar");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("CloseItemsHangar", "Unable to complete CloseItemsHangar [" + exception + "]", Logging.Logging.Teal);
                return false;
            }
        }

        public bool ReadyItemsHangarAsLootHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugItemHangar) Logging.Logging.Log("ReadyItemsHangarAsLootHangar", "We are in Station", Logging.Logging.Teal);
                    Instance.LootHangar = Instance.ItemHangar;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("ReadyItemsHangarAsLootHangar", "Unable to complete ReadyItemsHangarAsLootHangar [" + exception + "]", Logging.Logging.Teal);
                return false;
            }
        }

        public bool ReadyItemsHangarAsAmmoHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("ReadyItemsHangarAsAmmoHangar",
                        "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Logging.Teal);
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("ReadyItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)", Logging.Logging.Teal);
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("ReadyItemsHangarAsAmmoHangar", "We are in Station", Logging.Logging.Teal);
                    Instance.AmmoHangar = Instance.ItemHangar;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("ReadyItemsHangarAsAmmoHangar", "unable to complete ReadyItemsHangarAsAmmoHangar [" + exception + "]", Logging.Logging.Teal);
                return false;
            }
        }

        public bool StackItemsHangarAsLootHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(12) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                return false;
            }

            try
            {
                if (Logging.Logging.DebugItemHangar)
                    Logging.Logging.Log("StackItemsHangarAsLootHangar", "public bool StackItemsHangarAsLootHangar(String module)", Logging.Logging.Teal);

                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("StackItemsHangarAsLootHangar", "if (Cache.Instance.InStation)", Logging.Logging.Teal);
                    if (Instance.LootHangar != null)
                    {
                        try
                        {
                            if (Instance.StackHangarAttempts > 0)
                            {
                                if (!WaitForLockedItems(Time.Instance.LastStackLootHangar)) return false;
                                return true;
                            }

                            if (Instance.StackHangarAttempts <= 0)
                            {
                                if (LootHangar.Items.Any() && LootHangar.Items.Count() > RandomNumber(600, 800))
                                {
                                    Logging.Logging.Log(module, "Stacking Item Hangar (as LootHangar)", Logging.Logging.White);
                                    Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
                                    Instance.LootHangar.StackAll();
                                    Instance.StackHangarAttempts++;
                                    Time.Instance.LastStackLootHangar = DateTime.UtcNow;
                                    Time.Instance.LastStackItemHangar = DateTime.UtcNow;
                                    return false;
                                }

                                return true;
                            }

                            Logging.Logging.Log(module, "Not Stacking LootHangar", Logging.Logging.White);
                            return true;
                        }
                        catch (Exception exception)
                        {
                            Logging.Logging.Log(module, "Stacking Item Hangar failed [" + exception + "]", Logging.Logging.Teal);
                            return true;
                        }
                    }

                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("StackItemsHangarAsLootHangar",
                            "if (!Cache.Instance.ReadyItemsHangarAsLootHangar(Cache.StackItemsHangar)) return false;", Logging.Logging.Teal);
                    if (!Instance.ReadyItemsHangarAsLootHangar("Cache.StackItemsHangar")) return false;
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("StackItemsHangarAsLootHangar", "Unable to complete StackItemsHangarAsLootHangar [" + exception + "]", Logging.Logging.Teal);
                return true;
            }
        }

        private static bool WaitForLockedItems(DateTime __lastAction)
        {
            if (Instance.DirectEve.GetLockedItems().Count != 0)
            {
                if (Math.Abs(DateTime.UtcNow.Subtract(__lastAction).TotalSeconds) > 15)
                {
                    Logging.Logging.Log(_States.CurrentArmState.ToString(), "Moving Ammo timed out, clearing item locks", Logging.Logging.Orange);
                    Instance.DirectEve.UnlockItems();
                    return false;
                }

                if (Logging.Logging.DebugUnloadLoot)
                    Logging.Logging.Log(_States.CurrentArmState.ToString(),
                        "Waiting for Locks to clear. GetLockedItems().Count [" + Instance.DirectEve.GetLockedItems().Count + "]", Logging.Logging.Teal);
                return false;
            }

            Instance.StackHangarAttempts = 0;
            return true;
        }

        public bool StackItemsHangarAsAmmoHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                Logging.Logging.Log("StackItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)",
                    Logging.Logging.Teal);
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                Logging.Logging.Log("StackItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)", Logging.Logging.Teal);
                return false;
            }

            try
            {
                Logging.Logging.Log("StackItemsHangarAsAmmoHangar", "public bool StackItemsHangarAsAmmoHangar(String module)", Logging.Logging.Teal);

                if (Instance.InStation)
                {
                    Logging.Logging.Log("StackItemsHangarAsAmmoHangar", "if (Cache.Instance.InStation)", Logging.Logging.Teal);
                    if (Instance.AmmoHangar != null)
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
                                Logging.Logging.Log("StackItemsHangarAsAmmoHangar", "AmmoHangar.Items.Count [" + AmmoHangar.Items.Count() + "]",
                                    Logging.Logging.White);
                                if (AmmoHangar.Items.Any() && AmmoHangar.Items.Count() > RandomNumber(600, 800))
                                {
                                    Logging.Logging.Log(module, "Stacking Item Hangar (as AmmoHangar)", Logging.Logging.White);
                                    Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
                                    Instance.AmmoHangar.StackAll();
                                    Instance.StackHangarAttempts++;
                                    Time.Instance.LastStackAmmoHangar = DateTime.UtcNow;
                                    Time.Instance.LastStackItemHangar = DateTime.UtcNow;
                                    return true;
                                }

                                return true;
                            }

                            Logging.Logging.Log(module, "Not Stacking AmmoHangar[" + "ItemHangar" + "]", Logging.Logging.White);
                            return true;
                        }
                        catch (Exception exception)
                        {
                            Logging.Logging.Log(module, "Stacking Item Hangar failed [" + exception + "]", Logging.Logging.Teal);
                            return true;
                        }
                    }

                    Logging.Logging.Log("StackItemsHangarAsAmmoHangar",
                        "if (!Cache.Instance.ReadyItemsHangarAsAmmoHangar(Cache.StackItemsHangar)) return false;", Logging.Logging.Teal);
                    if (!Instance.ReadyItemsHangarAsAmmoHangar("Cache.StackItemsHangar")) return false;
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("StackItemsHangarAsAmmoHangar", "Unable to complete StackItemsHangarAsAmmoHangar [" + exception + "]", Logging.Logging.Teal);
                return true;
            }
        }

        public bool StackShipsHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                return false;

            try
            {
                if (Instance.InStation)
                {
                    if (Instance.ShipHangar != null && Instance.ShipHangar.IsValid)
                    {
                        if (Instance.StackHangarAttempts > 0)
                        {
                            if (!WaitForLockedItems(Time.Instance.LastStackShipsHangar)) return false;
                            return true;
                        }

                        if (Instance.StackHangarAttempts <= 0)
                        {
                            if (Instance.ShipHangar.Items.Any())
                            {
                                Logging.Logging.Log(module, "Stacking Ship Hangar", Logging.Logging.White);
                                Time.Instance.LastStackShipsHangar = DateTime.UtcNow;
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(3, 5));
                                Instance.ShipHangar.StackAll();
                                return false;
                            }

                            return true;
                        }
                    }
                    Logging.Logging.Log(module,
                        "Stacking Ship Hangar: not yet ready: waiting [" +
                        Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.Logging.White);
                    return false;
                }
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("StackShipsHangar", "Unable to complete StackShipsHangar [" + exception + "]", Logging.Logging.Teal);
                return true;
            }
        }

        public bool CloseShipsHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return false;

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                return false;

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenShipsHangar", "We are in Station", Logging.Logging.Teal);
                    Instance.ShipHangar = Instance.DirectEve.GetShipHangar();

                    if (Instance.ShipHangar == null)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenShipsHangar", "ShipsHangar was null", Logging.Logging.Teal);
                        return false;
                    }
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenShipsHangar", "ShipsHangar exists", Logging.Logging.Teal);

                    // Is the items hangar open?
                    if (Instance.ShipHangar.Window == null)
                    {
                        Logging.Logging.Log(module, "Ship Hangar: is closed", Logging.Logging.White);
                        return true;
                    }

                    if (!Instance.ShipHangar.Window.IsReady)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("OpenShipsHangar", "ShipsHangar.window is not yet ready", Logging.Logging.Teal);
                        return false;
                    }

                    if (Instance.ShipHangar.Window.IsReady)
                    {
                        Instance.ShipHangar.Window.Close();
                        Statistics.LogWindowActionToWindowLog("ShipHangar", "Close ShipHangar");
                        return false;
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("CloseShipsHangar", "Unable to complete CloseShipsHangar [" + exception + "]", Logging.Logging.Teal);
                return false;
            }
        }


        public bool OpenAndSelectInvItem(string module, long id)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(10))
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("OpenAndSelectInvItem",
                            "Debug: if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Logging.Teal);
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("OpenAndSelectInvItem", "Debug: if (DateTime.UtcNow < NextOpenHangarAction)", Logging.Logging.Teal);
                    return false;
                }

                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("OpenAndSelectInvItem", "Debug: about to: if (!Cache.Instance.OpenInventoryWindow", Logging.Logging.Teal);

                if (!Instance.OpenInventoryWindow(module)) return false;

                Instance.PrimaryInventoryWindow =
                    (DirectContainerWindow) Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Name.Contains("Inventory"));

                if (Instance.PrimaryInventoryWindow != null && Instance.PrimaryInventoryWindow.IsReady)
                {
                    if (id < 0)
                    {
                        //
                        // this also kicks in if we have no corp hangar at all in station... can we detect that some other way?
                        //
                        Logging.Logging.Log("OpenAndSelectInvItem", "Inventory item ID from tree cannot be less than 0, retrying", Logging.Logging.White);
                        return false;
                    }

                    var idsInInvTreeView = Instance.PrimaryInventoryWindow.GetIdsFromTree(false);
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("OpenAndSelectInvItem", "Debug: IDs Found in the Inv Tree [" + idsInInvTreeView.Count() + "]", Logging.Logging.Teal);

                    foreach (var itemInTree in idsInInvTreeView)
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("OpenAndSelectInvItem", "Debug: itemInTree [" + itemInTree + "][looking for: " + id, Logging.Logging.Teal);
                        if (itemInTree == id)
                        {
                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("OpenAndSelectInvItem", "Debug: Found a match! itemInTree [" + itemInTree + "] = id [" + id + "]",
                                    Logging.Logging.Teal);
                            if (Instance.PrimaryInventoryWindow.currInvIdItem != id)
                            {
                                if (Logging.Logging.DebugHangars)
                                    Logging.Logging.Log("OpenAndSelectInvItem", "Debug: We do not have the right ID selected yet, select it now.",
                                        Logging.Logging.Teal);
                                Instance.PrimaryInventoryWindow.SelectTreeEntryByID(id);
                                Statistics.LogWindowActionToWindowLog("Select Tree Entry", "Selected Entry on Left of Primary Inventory Window");
                                Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddMilliseconds(Instance.RandomNumber(2000, 4400));
                                return false;
                            }

                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("OpenAndSelectInvItem", "Debug: We already have the right ID selected.", Logging.Logging.Teal);
                            return true;
                        }

                        continue;
                    }

                    if (!idsInInvTreeView.Contains(id))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("OpenAndSelectInvItem", "Debug: if (!Cache.Instance.InventoryWindow.GetIdsFromTree(false).Contains(ID))",
                                Logging.Logging.Teal);

                        if (id >= 0 && id <= 6 && Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
                        {
                            Logging.Logging.Log(module, "ExpandCorpHangar executed", Logging.Logging.Teal);
                            Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(4);
                            return false;
                        }

                        foreach (var itemInTree in idsInInvTreeView)
                        {
                            Logging.Logging.Log(module, "ID: " + itemInTree, Logging.Logging.Red);
                        }

                        Logging.Logging.Log(module, "Was looking for: " + id, Logging.Logging.Red);
                        return false;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("OpenAndSelectInvItem", "Exception [" + ex + "]", Logging.Logging.Debug);
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

                if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                    // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                {
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextOpenLootContainerAction)
                {
                    return false;
                }

                if (Instance.InStation)
                {
                    if (LootContainer.Window == null)
                    {
                        var firstLootContainer =
                            Instance.LootHangar.Items.FirstOrDefault(
                                i =>
                                    i.GivenName != null && i.IsSingleton && i.GroupId == (int) Group.FreightContainer &&
                                    i.GivenName.ToLower() == Settings.Instance.LootContainerName.ToLower());
                        if (firstLootContainer != null)
                        {
                            var lootContainerID = firstLootContainer.ItemId;
                            if (!OpenAndSelectInvItem(module, lootContainerID))
                                return false;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    if (LootContainer.Window == null || !LootContainer.Window.IsReady) return false;

                    if (Instance.StackHangarAttempts > 0)
                    {
                        if (!WaitForLockedItems(Time.Instance.LastStackLootContainer)) return false;
                        return true;
                    }

                    if (Instance.StackHangarAttempts <= 0)
                    {
                        if (Instance.LootContainer.Items.Any())
                        {
                            Logging.Logging.Log(module,
                                "Loot Container window named: [ " + LootContainer.Window.Name + " ] was found and its contents are being stacked",
                                Logging.Logging.White);
                            LootContainer.StackAll();
                            Time.Instance.LastStackLootContainer = DateTime.UtcNow;
                            Time.Instance.LastStackLootHangar = DateTime.UtcNow;
                            Time.Instance.NextOpenLootContainerAction = DateTime.UtcNow.AddSeconds(2 + Instance.RandomNumber(1, 3));
                            return false;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("StackLootContainer", "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        }

        public bool CloseLootContainer(string module)
        {
            try
            {
                if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("CloseCorpLootHangar", "Debug: else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))",
                            Logging.Logging.Teal);
                    var lootHangarWindow =
                        (DirectContainerWindow)
                            Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Caption == Settings.Instance.LootContainerName);

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
                Logging.Logging.Log("CloseLootContainer", "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
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
                if (Instance.InStation)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName))
                    {
                        Instance.LootHangar = Instance.DirectEve.GetCorporationHangar(Settings.Instance.LootHangarTabName);

                        // Is the corp loot Hangar open?
                        if (Instance.LootHangar != null)
                        {
                            Instance.corpLootHangarSecondaryWindow =
                                (DirectContainerWindow)
                                    Instance.Windows.FirstOrDefault(
                                        w => w.Type.Contains("form.InventorySecondary") && w.Caption.Contains(Settings.Instance.LootHangarTabName));
                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("CloseCorpLootHangar", "Debug: if (Cache.Instance.LootHangar != null)", Logging.Logging.Teal);

                            if (Instance.corpLootHangarSecondaryWindow != null)
                            {
                                // if open command it to close
                                Instance.corpLootHangarSecondaryWindow.Close();
                                Statistics.LogWindowActionToWindowLog("LootHangar", "Closing LootHangar [" + Settings.Instance.LootHangarTabName + "]");
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Instance.RandomNumber(1, 3));
                                Logging.Logging.Log(module,
                                    "Closing Corporate Loot Hangar: waiting [" +
                                    Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.Logging.White);
                                return false;
                            }

                            return true;
                        }

                        if (Instance.LootHangar == null)
                        {
                            if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName))
                            {
                                Logging.Logging.Log(module,
                                    "Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag or setting misconfiguration?",
                                    Logging.Logging.Orange);
                                return true;
                            }
                            return false;
                        }
                    }
                    else if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("CloseCorpLootHangar", "Debug: else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))",
                                Logging.Logging.Teal);
                        var lootHangarWindow =
                            (DirectContainerWindow)
                                Instance.Windows.FirstOrDefault(
                                    w => w.Type.Contains("form.InventorySecondary") && w.Caption.Contains(Settings.Instance.LootContainerName));

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
                        Instance.LootHangar = Instance.DirectEve.GetItemHangar();
                        if (Instance.LootHangar == null)
                            return false;

                        // Is the items hangar open?
                        if (Instance.LootHangar.Window != null)
                        {
                            // if open command it to close
                            Instance.LootHangar.Window.Close();
                            Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Instance.RandomNumber(1, 4));
                            Logging.Logging.Log(module,
                                "Closing Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                "sec]", Logging.Logging.White);
                            return false;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("CloseLootHangar", "Unable to complete CloseLootHangar [" + exception + "]", Logging.Logging.Teal);
                return false;
            }
        }

        public bool StackLootHangar(string module)
        {
            if (Math.Abs(DateTime.UtcNow.Subtract(Time.Instance.LastStackLootHangar).TotalMinutes) < 10)
            {
                return true;
            }

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("StackLootHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)",
                        Logging.Logging.Teal);
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("StackLootHangar",
                        "if (DateTime.UtcNow [" + DateTime.UtcNow + "] < Cache.Instance.NextOpenHangarAction [" + Time.Instance.NextOpenHangarAction + "])",
                        Logging.Logging.Teal);
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("StackLootHangar", "if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))", Logging.Logging.Teal);
                        //if (!Cache.Instance.StackLootContainer("Cache.StackLootContainer")) return false;
                        Logging.Logging.Log("StackLootHangar",
                            "We do not stack containers, you will need to do so manually. StackAll does not seem to work with Primary Inventory windows.",
                            Logging.Logging.Teal);
                        return true;
                    }

                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("StackLootHangar", "!Cache.Instance.StackItemsHangarAsLootHangar(Cache.StackLootHangar))", Logging.Logging.Teal);
                    if (!Instance.StackItemsHangarAsLootHangar("Cache.StackItemsHangarAsLootHangar")) return false;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("StackLootHangar", "Unable to complete StackLootHangar [" + exception + "]", Logging.Logging.Teal);
                return true;
            }
        }

        public bool SortLootHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                return false;
            }

            if (Instance.InStation)
            {
                if (LootHangar != null && LootHangar.IsValid)
                {
                    var items = Instance.LootHangar.Items;
                    foreach (var item in items)
                    {
                        //if (item.FlagId)
                        Logging.Logging.Log(module, "Items: " + item.TypeName, Logging.Logging.White);

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

        public bool StackAmmoHangar(string module)
        {
            StackAmmohangarAttempts++;
            if (StackAmmohangarAttempts > 15)
            {
                Logging.Logging.Log("StackAmmoHangar", "Pausing. Stacking the ammoHangar has failed: attempts [" + StackAmmohangarAttempts + "]",
                    Logging.Logging.Teal);
                Instance.Paused = true;
                return true;
            }


            if (Math.Abs(DateTime.UtcNow.Subtract(Time.Instance.LastStackAmmoHangar).TotalMinutes) < 10)
            {
                return true;
            }

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("StackAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)",
                        Logging.Logging.Teal);
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("StackAmmoHangar",
                        "if (DateTime.UtcNow [" + DateTime.UtcNow + "] < Cache.Instance.NextOpenHangarAction [" + Time.Instance.NextOpenHangarAction + "])",
                        Logging.Logging.Teal);
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("StackAmmoHangar", "Starting [Cache.Instance.StackItemsHangarAsAmmoHangar]", Logging.Logging.Teal);
                    if (!Instance.StackItemsHangarAsAmmoHangar(module)) return false;
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("StackAmmoHangar", "Finished [Cache.Instance.StackItemsHangarAsAmmoHangar]", Logging.Logging.Teal);
                    StackAmmohangarAttempts = 0;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("StackAmmoHangar", "Unable to complete StackAmmoHangar [" + exception + "]", Logging.Logging.Teal);
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
                if (Instance.InStation)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("CloseCorpAmmoHangar", "Debug: if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))", Logging.Logging.Teal);

                        if (Instance.AmmoHangar == null)
                        {
                            Instance.AmmoHangar = Instance.DirectEve.GetCorporationHangar(Settings.Instance.AmmoHangarTabName);
                        }

                        // Is the corp Ammo Hangar open?
                        if (Instance.AmmoHangar != null)
                        {
                            Instance.corpAmmoHangarSecondaryWindow =
                                (DirectContainerWindow)
                                    Instance.Windows.FirstOrDefault(
                                        w => w.Type.Contains("form.InventorySecondary") && w.Caption.Contains(Settings.Instance.AmmoHangarTabName));
                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("CloseCorpAmmoHangar", "Debug: if (Cache.Instance.AmmoHangar != null)", Logging.Logging.Teal);

                            if (Instance.corpAmmoHangarSecondaryWindow != null)
                            {
                                if (Logging.Logging.DebugHangars)
                                    Logging.Logging.Log("CloseCorpAmmoHangar", "Debug: if (ammoHangarWindow != null)", Logging.Logging.Teal);

                                // if open command it to close
                                Instance.corpAmmoHangarSecondaryWindow.Close();
                                Statistics.LogWindowActionToWindowLog("Ammohangar", "Closing AmmoHangar");
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Instance.RandomNumber(1, 3));
                                Logging.Logging.Log(module,
                                    "Closing Corporate Ammo Hangar: waiting [" +
                                    Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.Logging.White);
                                return false;
                            }

                            return true;
                        }

                        if (Instance.AmmoHangar == null)
                        {
                            if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
                            {
                                Logging.Logging.Log(module,
                                    "Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag or setting misconfiguration?",
                                    Logging.Logging.Orange);
                            }

                            return false;
                        }
                    }
                    else //use local items hangar
                    {
                        if (Instance.AmmoHangar == null)
                        {
                            Instance.AmmoHangar = Instance.DirectEve.GetItemHangar();
                            return false;
                        }

                        // Is the items hangar open?
                        if (Instance.AmmoHangar.Window != null)
                        {
                            // if open command it to close
                            if (!Instance.CloseItemsHangar(module)) return false;
                            Logging.Logging.Log(module, "Closing AmmoHangar Hangar", Logging.Logging.White);
                            return true;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("CloseAmmoHangar", "Unable to complete CloseAmmoHangar [" + exception + "]", Logging.Logging.Teal);
                return false;
            }
        }
    }
}