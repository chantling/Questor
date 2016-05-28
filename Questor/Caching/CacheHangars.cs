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
                                Logging.Logging.Log("Opening ItemHangar");
                                Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
                                Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
                                Time.Instance.LastOpenHangar = DateTime.UtcNow;
                                return null;
                            }

                            if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                                Logging.Logging.Log("ItemHangar recently opened, waiting for the window to actually appear");
                            return null;
                        }

                        if (Instance.Windows.Any(i => i.Type == "form.StationItems"))
                        {
                            if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                                Logging.Logging.Log("if (Cache.Instance.Windows.Any(i => i.Type == form.StationItems))");
                            return Instance._itemHangar;
                        }

                        if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                            Logging.Logging.Log("Not sure how we got here... ");
                        return null;
                    }

                    if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                        Logging.Logging.Log("InSpace [" + Instance.InSpace + "] InStation [" + Instance.InStation + "] waiting...");
                    return null;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
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
                            Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)");
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
                    Logging.Logging.Log("Exception [" + ex + "]");
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
                                    Logging.Logging.Log("Unable to find a container named [" + Settings.Instance.LootContainerName + "], using the available unnamed container");
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
                                        Logging.Logging.Log("LootContainer is defined");
                                        return _lootContainer;
                                    }

                                    Logging.Logging.Log("LootContainer is still null");
                                    return null;
                                }

                                Logging.Logging.Log("unable to find LootContainer named [ " + Settings.Instance.LootContainerName.ToLower() + " ]");
                                var firstOtherContainer =
                                    Instance.ItemHangar.Items.FirstOrDefault(
                                        i => i.GivenName != null && i.IsSingleton && i.GroupId == (int) Group.FreightContainer);

                                if (firstOtherContainer != null)
                                {
                                    Logging.Logging.Log("we did however find a container named [ " + firstOtherContainer.GivenName + " ]");
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
                    Logging.Logging.Log("Exception [" + ex + "]");
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
                                Logging.Logging.Log("Using ItemHangar as the LootHangar");
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
                    Logging.Logging.Log("Unable to define LootHangar [" + exception + "]");
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
                                    Logging.Logging.Log("AmmoHangarID is [" + Instance.AmmoHangarID + "]");

                                _ammoHangar = null;
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
                                _ammoHangar = Instance.DirectEve.GetCorporationHangar((int) Instance.AmmoHangarID);
                                Statistics.LogWindowActionToWindowLog("AmmoHangar", "AmmoHangar Defined (not opened?)");

                                if (_ammoHangar != null && _ammoHangar.IsValid) //do we have a corp hangar tab setup with that name?
                                {
                                    if (Logging.Logging.DebugHangars)
                                    {
                                        Logging.Logging.Log("AmmoHangar is defined (no window needed)");
                                        try
                                        {
                                            if (AmmoHangar.Items.Any())
                                            {
                                                var AmmoHangarItemCount = AmmoHangar.Items.Count();
                                                if (Logging.Logging.DebugHangars)
                                                    Logging.Logging.Log("AmmoHangar [" + Settings.Instance.AmmoHangarTabName + "] has [" + AmmoHangarItemCount + "] items");
                                            }
                                        }
                                        catch (Exception exception)
                                        {
                                            Logging.Logging.Log("Exception [" + exception + "]");
                                        }
                                    }

                                    return _ammoHangar;
                                }

                                Logging.Logging.Log("Opening Corporate Ammo Hangar: failed! No Corporate Hangar in this station! lag?");
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
                    Logging.Logging.Log("Unable to define AmmoHangar [" + exception + "]");
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
                    Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10))");
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))
            {
                if (Logging.Logging.DebugArm || Logging.Logging.DebugUnloadLoot || Logging.Logging.DebugHangars)
                    Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))");
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
                    Logging.Logging.Log("Opening Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
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
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("We are in Station");
                    Instance.ItemHangar = Instance.DirectEve.GetItemHangar();

                    if (Instance.ItemHangar == null)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("ItemsHangar was null");
                        return false;
                    }

                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("ItemsHangar exists");

                    // Is the items hangar open?
                    if (Instance.ItemHangar.Window == null)
                    {
                        Logging.Logging.Log("Item Hangar: is closed");
                        return true;
                    }

                    if (!Instance.ItemHangar.Window.IsReady)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("ItemsHangar.window is not yet ready");
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
                Logging.Logging.Log("Unable to complete CloseItemsHangar [" + exception + "]");
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
                    if (Logging.Logging.DebugItemHangar) Logging.Logging.Log("We are in Station");
                    Instance.LootHangar = Instance.ItemHangar;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete ReadyItemsHangarAsLootHangar [" + exception + "]");
                return false;
            }
        }

        public bool ReadyItemsHangarAsAmmoHangar(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)");
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)");
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("We are in Station");
                    Instance.AmmoHangar = Instance.ItemHangar;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("unable to complete ReadyItemsHangarAsAmmoHangar [" + exception + "]");
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
                    Logging.Logging.Log("public bool StackItemsHangarAsLootHangar(String module)");

                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("if (Cache.Instance.InStation)");
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
                                    Logging.Logging.Log("Stacking Item Hangar (as LootHangar)");
                                    Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
                                    Instance.LootHangar.StackAll();
                                    Instance.StackHangarAttempts++;
                                    Time.Instance.LastStackLootHangar = DateTime.UtcNow;
                                    Time.Instance.LastStackItemHangar = DateTime.UtcNow;
                                    return false;
                                }

                                return true;
                            }

                            Logging.Logging.Log("Not Stacking LootHangar");
                            return true;
                        }
                        catch (Exception exception)
                        {
                            Logging.Logging.Log("Stacking Item Hangar failed [" + exception + "]");
                            return true;
                        }
                    }

                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("if (!Cache.Instance.ReadyItemsHangarAsLootHangar(Cache.StackItemsHangar)) return false;");
                    if (!Instance.ReadyItemsHangarAsLootHangar("Cache.StackItemsHangar")) return false;
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete StackItemsHangarAsLootHangar [" + exception + "]");
                return true;
            }
        }

        private static bool WaitForLockedItems(DateTime __lastAction)
        {
            if (Instance.DirectEve.GetLockedItems().Count != 0)
            {
                if (Math.Abs(DateTime.UtcNow.Subtract(__lastAction).TotalSeconds) > 15)
                {
                    Logging.Logging.Log("Moving Ammo timed out, clearing item locks");
                    Instance.DirectEve.UnlockItems();
                    return false;
                }

                if (Logging.Logging.DebugUnloadLoot)
                    Logging.Logging.Log("Waiting for Locks to clear. GetLockedItems().Count [" + Instance.DirectEve.GetLockedItems().Count + "]");
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
                Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)");
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                Logging.Logging.Log("if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)");
                return false;
            }

            try
            {
                Logging.Logging.Log("public bool StackItemsHangarAsAmmoHangar(String module)");

                if (Instance.InStation)
                {
                    Logging.Logging.Log("if (Cache.Instance.InStation)");
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
                                Logging.Logging.Log("AmmoHangar.Items.Count [" + AmmoHangar.Items.Count() + "]");
                                if (AmmoHangar.Items.Any() && AmmoHangar.Items.Count() > RandomNumber(600, 800))
                                {
                                    Logging.Logging.Log("Stacking Item Hangar (as AmmoHangar)");
                                    Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
                                    Instance.AmmoHangar.StackAll();
                                    Instance.StackHangarAttempts++;
                                    Time.Instance.LastStackAmmoHangar = DateTime.UtcNow;
                                    Time.Instance.LastStackItemHangar = DateTime.UtcNow;
                                    return true;
                                }

                                return true;
                            }

                            Logging.Logging.Log("Not Stacking AmmoHangar[" + "ItemHangar" + "]");
                            return true;
                        }
                        catch (Exception exception)
                        {
                            Logging.Logging.Log("Stacking Item Hangar failed [" + exception + "]");
                            return true;
                        }
                    }

                    Logging.Logging.Log("if (!Cache.Instance.ReadyItemsHangarAsAmmoHangar(Cache.StackItemsHangar)) return false;");
                    if (!Instance.ReadyItemsHangarAsAmmoHangar("Cache.StackItemsHangar")) return false;
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete StackItemsHangarAsAmmoHangar [" + exception + "]");
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
                                Logging.Logging.Log("Stacking Ship Hangar");
                                Time.Instance.LastStackShipsHangar = DateTime.UtcNow;
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(3, 5));
                                Instance.ShipHangar.StackAll();
                                return false;
                            }

                            return true;
                        }
                    }
                    Logging.Logging.Log("Stacking Ship Hangar: not yet ready: waiting [" +
                        Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    return false;
                }
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete StackShipsHangar [" + exception + "]");
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
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("We are in Station");
                    Instance.ShipHangar = Instance.DirectEve.GetShipHangar();

                    if (Instance.ShipHangar == null)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("ShipsHangar was null");
                        return false;
                    }
                    if (Logging.Logging.DebugHangars) Logging.Logging.Log("ShipsHangar exists");

                    // Is the items hangar open?
                    if (Instance.ShipHangar.Window == null)
                    {
                        Logging.Logging.Log("Ship Hangar: is closed");
                        return true;
                    }

                    if (!Instance.ShipHangar.Window.IsReady)
                    {
                        if (Logging.Logging.DebugHangars) Logging.Logging.Log("ShipsHangar.window is not yet ready");
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
                Logging.Logging.Log("Unable to complete CloseShipsHangar [" + exception + "]");
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
                    if (id < 0)
                    {
                        //
                        // this also kicks in if we have no corp hangar at all in station... can we detect that some other way?
                        //
                        Logging.Logging.Log("Inventory item ID from tree cannot be less than 0, retrying");
                        return false;
                    }

                    var idsInInvTreeView = Instance.PrimaryInventoryWindow.GetIdsFromTree(false);
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Debug: IDs Found in the Inv Tree [" + idsInInvTreeView.Count() + "]");

                    foreach (var itemInTree in idsInInvTreeView)
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("Debug: itemInTree [" + itemInTree + "][looking for: " + id);
                        if (itemInTree == id)
                        {
                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("Debug: Found a match! itemInTree [" + itemInTree + "] = id [" + id + "]");
                            if (Instance.PrimaryInventoryWindow.currInvIdItem != id)
                            {
                                if (Logging.Logging.DebugHangars)
                                    Logging.Logging.Log("Debug: We do not have the right ID selected yet, select it now.");
                                Instance.PrimaryInventoryWindow.SelectTreeEntryByID(id);
                                Statistics.LogWindowActionToWindowLog("Select Tree Entry", "Selected Entry on Left of Primary Inventory Window");
                                Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddMilliseconds(Instance.RandomNumber(2000, 4400));
                                return false;
                            }

                            if (Logging.Logging.DebugHangars)
                                Logging.Logging.Log("Debug: We already have the right ID selected.");
                            return true;
                        }

                        continue;
                    }

                    if (!idsInInvTreeView.Contains(id))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("Debug: if (!Cache.Instance.InventoryWindow.GetIdsFromTree(false).Contains(ID))");

                        if (id >= 0 && id <= 6 && Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
                        {
                            Logging.Logging.Log("ExpandCorpHangar executed");
                            Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(4);
                            return false;
                        }

                        foreach (var itemInTree in idsInInvTreeView)
                        {
                            Logging.Logging.Log("ID: " + itemInTree);
                        }

                        Logging.Logging.Log("Was looking for: " + id);
                        return false;
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
                            Logging.Logging.Log("Loot Container window named: [ " + LootContainer.Window.Name + " ] was found and its contents are being stacked");
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
                Logging.Logging.Log("Exception [" + ex + "]");
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
                        Logging.Logging.Log("Debug: else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))");
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
                Logging.Logging.Log("Exception [" + ex + "]");
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
                                Logging.Logging.Log("Debug: if (Cache.Instance.LootHangar != null)");

                            if (Instance.corpLootHangarSecondaryWindow != null)
                            {
                                // if open command it to close
                                Instance.corpLootHangarSecondaryWindow.Close();
                                Statistics.LogWindowActionToWindowLog("LootHangar", "Closing LootHangar [" + Settings.Instance.LootHangarTabName + "]");
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Instance.RandomNumber(1, 3));
                                Logging.Logging.Log("Closing Corporate Loot Hangar: waiting [" +
                                    Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                                return false;
                            }

                            return true;
                        }

                        if (Instance.LootHangar == null)
                        {
                            if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName))
                            {
                                Logging.Logging.Log("Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag or setting misconfiguration?");
                                return true;
                            }
                            return false;
                        }
                    }
                    else if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("Debug: else if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))");
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
                            Logging.Logging.Log("Closing Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                "sec]");
                            return false;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete CloseLootHangar [" + exception + "]");
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
                    Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)");
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("if (DateTime.UtcNow [" + DateTime.UtcNow + "] < Cache.Instance.NextOpenHangarAction [" + Time.Instance.NextOpenHangarAction + "])");
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                    {
                        if (Logging.Logging.DebugHangars)
                            Logging.Logging.Log("if (!string.IsNullOrEmpty(Settings.Instance.LootContainer))");
                        //if (!Cache.Instance.StackLootContainer("Cache.StackLootContainer")) return false;
                        Logging.Logging.Log("We do not stack containers, you will need to do so manually. StackAll does not seem to work with Primary Inventory windows.");
                        return true;
                    }

                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("!Cache.Instance.StackItemsHangarAsLootHangar(Cache.StackLootHangar))");
                    if (!Instance.StackItemsHangarAsLootHangar("Cache.StackItemsHangarAsLootHangar")) return false;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete StackLootHangar [" + exception + "]");
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
                        Logging.Logging.Log("Items: " + item.TypeName);

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
                Logging.Logging.Log("Pausing. Stacking the ammoHangar has failed: attempts [" + StackAmmohangarAttempts + "]");
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
                    Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)");
                return false;
            }

            if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
            {
                if (Logging.Logging.DebugHangars)
                    Logging.Logging.Log("if (DateTime.UtcNow [" + DateTime.UtcNow + "] < Cache.Instance.NextOpenHangarAction [" + Time.Instance.NextOpenHangarAction + "])");
                return false;
            }

            try
            {
                if (Instance.InStation)
                {
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Starting [Cache.Instance.StackItemsHangarAsAmmoHangar]");
                    if (!Instance.StackItemsHangarAsAmmoHangar(module)) return false;
                    if (Logging.Logging.DebugHangars)
                        Logging.Logging.Log("Finished [Cache.Instance.StackItemsHangarAsAmmoHangar]");
                    StackAmmohangarAttempts = 0;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete StackAmmoHangar [" + exception + "]");
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
                            Logging.Logging.Log("Debug: if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangar))");

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
                                Logging.Logging.Log("Debug: if (Cache.Instance.AmmoHangar != null)");

                            if (Instance.corpAmmoHangarSecondaryWindow != null)
                            {
                                if (Logging.Logging.DebugHangars)
                                    Logging.Logging.Log("Debug: if (ammoHangarWindow != null)");

                                // if open command it to close
                                Instance.corpAmmoHangarSecondaryWindow.Close();
                                Statistics.LogWindowActionToWindowLog("Ammohangar", "Closing AmmoHangar");
                                Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2 + Instance.RandomNumber(1, 3));
                                Logging.Logging.Log("Closing Corporate Ammo Hangar: waiting [" +
                                    Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                                return false;
                            }

                            return true;
                        }

                        if (Instance.AmmoHangar == null)
                        {
                            if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
                            {
                                Logging.Logging.Log("Closing Corporate Hangar: failed! No Corporate Hangar in this station! lag or setting misconfiguration?");
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
                            Logging.Logging.Log("Closing AmmoHangar Hangar");
                            return true;
                        }

                        return true;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete CloseAmmoHangar [" + exception + "]");
                return false;
            }
        }
    }
}