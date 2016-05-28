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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using DirectEve;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public static class Arm
    {
        private static bool ItemsAreBeingMoved;
        private static DateTime _lastArmAction;
        private static DateTime _lastFitAction = DateTime.UtcNow;


        private static int _itemsLeftToMoveQuantity;

        // Chant - 05/03/2016 - globals for moving scripts to cargo
        private static int TrackingDisruptorScripts = 0;
        private static int TrackingComputerScripts = 0;
        private static int TrackingLinkScripts = 0;
        private static int SensorBoosterScripts = 0;
        private static int SensorDampenerScripts = 0;
        private static int CapacitorInjectorScripts = 0;
        private static int AncillaryShieldBoosterScripts = 0;
        private static bool bWaitingonScripts = false;

        private static bool DefaultFittingChecked; //false; //flag to check for the correct default fitting before using the fitting manager
        private static bool DefaultFittingFound; //Did we find the default fitting?
        private static bool UseMissionShip; //false; // Were we successful in activating the mission specific ship?
        private static bool CustomFittingFound;
        private static bool switchingShips;
        public static bool SwitchShipsOnly;

        private static int ItemHangarRetries = 0;
        private static int DroneBayRetries = 0;
        private static int WeHaveThisManyOfThoseItemsInCargo;
        private static int WeHaveThisManyOfThoseItemsInItemHangar;
        private static int WeHaveThisManyOfThoseItemsInAmmoHangar;
        private static int WeHaveThisManyOfThoseItemsInLootHangar;
        private static DirectInvType _droneInvTypeItem;
        private static IEnumerable<DirectItem> cargoItems;
        private static IEnumerable<DirectItem> ItemHangarItems;
        private static DirectItem ItemHangarItem;
        private static IEnumerable<DirectItem> AmmoHangarItems;
        private static DirectItem AmmoHangarItem;
        private static IEnumerable<DirectItem> LootHangarItems;
        private static DirectItem LootHangarItem;

        private static DirectInvType DroneInvTypeItem
        {
            get
            {
                try
                {
                    if (_droneInvTypeItem == null)
                    {
                        if (Logging.Logging.DebugArm)
                            Logging.Logging.Log("DroneInvTypeItem", " Drones.DroneTypeID: " + Drones.DroneTypeID, Logging.Logging.Debug);
                        Cache.Instance.DirectEve.InvTypes.TryGetValue(Drones.DroneTypeID, out _droneInvTypeItem);
                    }

                    return _droneInvTypeItem;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("DroneInvTypeItem", "Exception [" + ex + "]", Logging.Logging.Debug);
                    return null;
                }
            }
        }

        public static bool ArmLoadCapBoosters { get; set; }
        public static bool NeedRepair { get; set; }
        private static DateTime LastRepairDateTime { get; set; }

        public static void ClearDataBetweenStates()
        {
            _itemsLeftToMoveQuantity = 0;
        } // check

        public static void InvalidateCache()
        {
            cargoItems = null;
            ItemHangarItem = null;
            ItemHangarItems = null;
        } // check

        private static string WeAreInThisStateForLogs()
        {
            return _States.CurrentCombatMissionBehaviorState.ToString() + "." + _States.CurrentArmState.ToString();
        } // check

        public static void ProcessState()
        {
            try
            {
                if (!Cache.Instance.InStation)
                    return;

                if (Cache.Instance.InSpace)
                    return;

                if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(10))
                    // we wait 10 seconds after we last thought we were in space before trying to do anything in station
                    return;

                if (Time.Instance.NextArmAction > DateTime.UtcNow)
                {
                    return;
                }

                switch (_States.CurrentArmState)
                {
                    case ArmState.Idle:
                        break;

                    case ArmState.Begin:
                        if (!BeginArm()) break;
                        break;

                    case ArmState.ActivateCombatShip:
                        if (!ActivateCombatShip()) return;
                        break;

                    case ArmState.RepairShop:
                        if (!RepairShop()) return;
                        break;

                    case ArmState.StripFitting:
                        if (!StripFitting()) return;
                        break;

                    case ArmState.LoadSavedFitting:
                        if (!LoadSavedFitting()) return;
                        break;

                    case ArmState.MoveDrones:
                        if (!MoveDrones()) return;
                        break;

                    case ArmState.MoveMissionItems:
                        if (!MoveMissionItems()) return;
                        break;

                    case ArmState.MoveOptionalItems:
                        if (!MoveOptionalItems()) return;
                        break;

                    case ArmState.MoveScripts:
                        if (!MoveScripts()) return;
                        break;

                    case ArmState.MoveCapBoosters:
                        if (!MoveCapBoosters()) return;
                        break;

                    case ArmState.MoveAmmo:
                        if (!MoveAmmo()) return;
                        break;

                    case ArmState.StackAmmoHangar:
                        if (!StackAmmoHangar()) return;
                        break;

                    case ArmState.Cleanup:
                        if (!Cleanup()) return;
                        break;

                    case ArmState.Done:
                        break;

                    case ArmState.ActivateTransportShip:
                        if (!ActivateTransportShip()) return;
                        break;

                    case ArmState.ActivateSalvageShip:
                        if (!ActivateSalvageShip()) return;
                        break;

                    case ArmState.NotEnoughDrones: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;

                    case ArmState.NotEnoughAmmo: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Arm.ProcessState", "Exception [" + ex + "]", Logging.Logging.Debug);
                return;
            }
        } // check

        public static void RefreshMissionItems(long agentId)
        {
            if (_States.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
            {
                Settings.Instance.UseFittingManager = false;
                return;
            }

            MissionSettings.MissionSpecificShip = null;
            MissionSettings.FactionSpecificShip = null;

            var missionDetailsForMissionItems = Cache.Instance.GetAgentMission(agentId, false);
            if (missionDetailsForMissionItems == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(MissionSettings.FactionName))
            {
                MissionSettings.FactionName = "Default";
            }

            MissionSettings.MissionItems.Clear();
            MissionSettings.MoveMissionItems = string.Empty;
            MissionSettings.MoveOptionalMissionItems = string.Empty;

            var missionName = Logging.Logging.FilterPath(missionDetailsForMissionItems.Name);
            MissionSettings.MissionXmlPath = Path.Combine(MissionSettings.MissionsPath, missionName + ".xml");
            if (!File.Exists(MissionSettings.MissionXmlPath))
            {
                return;
            }

            try
            {
                var xdoc = XDocument.Load(MissionSettings.MissionXmlPath);
                var items =
                    ((IEnumerable)
                        xdoc.XPathEvaluate(
                            "//action[(translate(@name, 'LOT', 'lot')='loot') or (translate(@name, 'LOTIEM', 'lotiem')='lootitem')]/parameter[translate(@name, 'TIEM', 'tiem')='item']/@value"))
                        .Cast<XAttribute>().Select(a => ((string) a ?? string.Empty).ToLower());
                MissionSettings.MissionItems.AddRange(items);

                if (xdoc.Root != null)
                {
                    MissionSettings.MoveMissionItems = (string) xdoc.Root.Element("bring") ?? string.Empty;
                    MissionSettings.MoveMissionItems = MissionSettings.MoveMissionItems.ToLower();
                    if (Logging.Logging.DebugArm)
                        Logging.Logging.Log("RefreshMissionItems",
                            "bring XML [" + xdoc.Root.Element("bring") + "] BringMissionItem [" + MissionSettings.MoveMissionItems + "]", Logging.Logging.Debug);
                    MissionSettings.MoveMissionItemsQuantity = (int?) xdoc.Root.Element("bringquantity") ?? 1;
                    if (Logging.Logging.DebugArm)
                        Logging.Logging.Log("RefreshMissionItems",
                            "bringquantity XML [" + xdoc.Root.Element("bringquantity") + "] BringMissionItemQuantity [" +
                            MissionSettings.MoveMissionItemsQuantity + "]", Logging.Logging.Debug);

                    MissionSettings.MoveOptionalMissionItems = (string) xdoc.Root.Element("trytobring") ?? string.Empty;
                    MissionSettings.MoveOptionalMissionItems = MissionSettings.MoveOptionalMissionItems.ToLower();
                    if (Logging.Logging.DebugArm)
                        Logging.Logging.Log("RefreshMissionItems",
                            "trytobring XML [" + xdoc.Root.Element("trytobring") + "] BringOptionalMissionItem [" + MissionSettings.MoveOptionalMissionItems +
                            "]", Logging.Logging.Debug);
                    MissionSettings.MoveOptionalMissionItemQuantity = (int?) xdoc.Root.Element("trytobringquantity") ?? 1;
                    if (Logging.Logging.DebugArm)
                        Logging.Logging.Log("RefreshMissionItems",
                            "trytobringquantity XML [" + xdoc.Root.Element("trytobringquantity") + "] BringOptionalMissionItemQuantity [" +
                            MissionSettings.MoveOptionalMissionItemQuantity + "]", Logging.Logging.Debug);
                }
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("RefreshMissionItems", "Error loading mission XML file [" + ex.Message + "]", Logging.Logging.Orange);
            }
        } // check

        private static bool LookForItem(string itemToFind, DirectContainer hangarToCheckForItemsdWeAlreadyMoved)
        {
            try
            {
                WeHaveThisManyOfThoseItemsInCargo = 0;
                WeHaveThisManyOfThoseItemsInItemHangar = 0;
                WeHaveThisManyOfThoseItemsInAmmoHangar = 0;
                WeHaveThisManyOfThoseItemsInLootHangar = 0;
                cargoItems = new List<DirectItem>();

                ItemHangarItems = new List<DirectItem>();
                ItemHangarItem = null;

                AmmoHangarItems = new List<DirectItem>();
                AmmoHangarItem = null;

                LootHangarItems = new List<DirectItem>();
                LootHangarItem = null;
                //
                // check the local cargo for items and subtract the items in the cargo from the quantity we still need to move to our cargohold
                //
                if (hangarToCheckForItemsdWeAlreadyMoved != null && hangarToCheckForItemsdWeAlreadyMoved.Items.Any())
                {
                    cargoItems =
                        hangarToCheckForItemsdWeAlreadyMoved.Items.Where(i => (i.TypeName ?? string.Empty).ToLower().Equals(itemToFind.ToLower())).ToList();
                    WeHaveThisManyOfThoseItemsInCargo = cargoItems.Sum(i => i.Stacksize);
                    //do not return here
                }

                //
                // check itemhangar for the item
                //
                try
                {
                    if (Cache.Instance.ItemHangar == null) return false;
                    if (Cache.Instance.ItemHangar.Items.Any())
                    {
                        //if (Logging.DebugArm) Logging.Log("Arm.LookForItem", "We have [" + Cache.Instance.ItemHangar.Items.Count() + "] total items in ItemHangar", Logging.Debug);
                        if (Cache.Instance.ItemHangar.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                        {
                            ItemHangarItems =
                                Cache.Instance.ItemHangar.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                            ItemHangarItem = ItemHangarItems.OrderBy(s => s.Stacksize).FirstOrDefault();
                            WeHaveThisManyOfThoseItemsInItemHangar = ItemHangarItems.Sum(i => i.Stacksize);
                            if (Logging.Logging.DebugArm)
                                Logging.Logging.Log("Arm.LookForItem",
                                    "We have [" + WeHaveThisManyOfThoseItemsInItemHangar + "] [" + itemToFind + "] in ItemHangar", Logging.Logging.Debug);
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugArm) Logging.Logging.Log("Arm.LookForItem", "Exception [" + ex + "]", Logging.Logging.Debug);
                }

                //
                // check ammohangar for the item
                //
                try
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
                    {
                        if (Cache.Instance.AmmoHangar == null) return false;

                        if (Logging.Logging.DebugArm) Logging.Logging.Log("Arm.LookForItem", "AmmoHangar is defined", Logging.Logging.Debug);

                        if (Cache.Instance.AmmoHangar.Items.Any())
                        {
                            if (Logging.Logging.DebugArm)
                                Logging.Logging.Log("Arm.LookForItem", "We have [" + Cache.Instance.AmmoHangar.Items.Count() + "] total items in AmmoHangar",
                                    Logging.Logging.Debug);
                            if (Cache.Instance.AmmoHangar.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                            {
                                AmmoHangarItems =
                                    Cache.Instance.AmmoHangar.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                                AmmoHangarItem = AmmoHangarItems.OrderBy(s => s.Stacksize).FirstOrDefault();
                                WeHaveThisManyOfThoseItemsInAmmoHangar = AmmoHangarItems.Sum(i => i.Stacksize);
                                if (Logging.Logging.DebugArm)
                                    Logging.Logging.Log("Arm.LookForItem",
                                        "We have [" + WeHaveThisManyOfThoseItemsInAmmoHangar + "] [" + itemToFind + "] in AmmoHangar", Logging.Logging.Debug);
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugArm) Logging.Logging.Log("Arm.LookForItem", "Exception [" + ex + "]", Logging.Logging.Debug);
                }

                //
                // check loothangar for the item
                //
                try
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName) && Settings.Instance.LootHangarTabName != Settings.Instance.AmmoHangarTabName)
                    {
                        if (Cache.Instance.LootHangar == null) return false;

                        if (Logging.Logging.DebugArm)
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "LootHangar is defined and is different from AmmoHangar", Logging.Logging.Debug);

                        if (Cache.Instance.LootHangar.Items.Any())
                        {
                            if (Logging.Logging.DebugArm)
                                Logging.Logging.Log(WeAreInThisStateForLogs(),
                                    "We have [" + Cache.Instance.LootHangar.Items.Count() + "] total items in LootHangar", Logging.Logging.Debug);
                            if (Cache.Instance.LootHangar.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                            {
                                LootHangarItems =
                                    Cache.Instance.LootHangar.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                                LootHangarItem = LootHangarItems.OrderBy(s => s.Stacksize).FirstOrDefault();
                                WeHaveThisManyOfThoseItemsInLootHangar = LootHangarItems.Sum(i => i.Stacksize);
                                if (Logging.Logging.DebugArm)
                                    Logging.Logging.Log(WeAreInThisStateForLogs(),
                                        "We have [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + itemToFind + "] in LootHangar", Logging.Logging.Debug);
                                return true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugArm) Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                }

                //
                // we searched all hangars, hopefully found some items...
                //
                return true;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + exception + "]", Logging.Logging.Debug);
                return false;
            }
        } // check

        private static bool ActivateTransportShip() // check
        {
            if (string.IsNullOrEmpty(Settings.Instance.TransportShipName))
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Could not find transportshipName in settings!", Logging.Logging.Orange);
                ChangeArmState(ArmState.NotEnoughAmmo);
                return false;
            }

            if (!ActivateShip(Settings.Instance.TransportShipName)) return false;

            Logging.Logging.Log(WeAreInThisStateForLogs(), "Done", Logging.Logging.White);
            ChangeArmState(ArmState.Cleanup);
            return true;
        } // check

        private static bool ActivateSalvageShip()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.SalvageShipName))
                {
                    Logging.Logging.Log(WeAreInThisStateForLogs(), "Could not find salvageshipName: " + Settings.Instance.SalvageShipName + " in settings!",
                        Logging.Logging.Orange);
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                if (!ActivateShip(Settings.Instance.SalvageShipName)) return false;

                Logging.Logging.Log(WeAreInThisStateForLogs(), "Done", Logging.Logging.White);
                ChangeArmState(ArmState.Cleanup);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // check

        private static bool WaitForLockedItems(ArmState _armStateToSwitchTo)
        {
            try
            {
                if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
                {
                    if (Math.Abs(DateTime.UtcNow.Subtract(_lastArmAction).TotalSeconds) > 15)
                    {
                        Logging.Logging.Log(WeAreInThisStateForLogs(), "Moving Ammo timed out, clearing item locks", Logging.Logging.Orange);
                        Cache.Instance.DirectEve.UnlockItems();
                        _lastArmAction = DateTime.UtcNow.AddSeconds(-1);
                        return false;
                    }

                    if (Logging.Logging.DebugUnloadLoot)
                        Logging.Logging.Log(WeAreInThisStateForLogs(),
                            "Waiting for Locks to clear. GetLockedItems().Count [" + Cache.Instance.DirectEve.GetLockedItems().Count + "]", Logging.Logging.Teal);
                    return false;
                }

                _lastArmAction = DateTime.UtcNow.AddSeconds(-1);
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Done", Logging.Logging.White);
                ItemsAreBeingMoved = false;
                ChangeArmState(_armStateToSwitchTo);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // check

        private static bool ActivateShip(string shipName)
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(Cache.Instance.RandomNumber(4000, 5000))) return false;

                //
                // have we attempted to switch ships already (and are waiting for it to take effect)
                //
                if (switchingShips)
                {
                    if (Cache.Instance.DirectEve.ActiveShip != null && Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == shipName.ToLower())
                    {
                        switchingShips = false;
                        return true;
                    }

                    _lastArmAction = DateTime.UtcNow;
                    return false;
                }

                //
                // is the ShipName is already the current ship? (we may have started in the right ship!)
                //
                if (Cache.Instance.DirectEve.ActiveShip != null && Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == shipName.ToLower())
                {
                    switchingShips = false;
                    return true;
                }

                //
                // Check and warn the use if their config is hosed.
                //
                if (string.IsNullOrEmpty(Combat.Combat.CombatShipName) || string.IsNullOrEmpty(Settings.Instance.SalvageShipName))
                {
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                    return false;
                }

                if (Combat.Combat.CombatShipName == Settings.Instance.SalvageShipName)
                {
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                    return false;
                }

                //
                // we have the mining shipname configured but it is not the current ship
                // 
                if (!string.IsNullOrEmpty(shipName))
                {
                    if (Cache.Instance.ShipHangar == null) return false;

                    var shipsInShipHangar = Cache.Instance.ShipHangar.Items;
                    if (shipsInShipHangar.Any(s => s.GivenName != null && s.GivenName.ToLower() == shipName.ToLower()))
                    {
                        if (!Cache.Instance.CloseCargoHold(_States.CurrentArmState.ToString())) return false;
                        var ship = shipsInShipHangar.FirstOrDefault(s => s.GivenName != null && s.GivenName.ToLower() == shipName.ToLower());
                        if (ship != null)
                        {
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "Making [" + ship.GivenName + "] active", Logging.Logging.White);
                            ship.ActivateShip();
                            switchingShips = true;
                            _lastArmAction = DateTime.UtcNow;
                            return false;
                        }

                        return false;
                    }

                    if (Cache.Instance.ShipHangar.Items.Any())
                    {
                        Logging.Logging.Log(WeAreInThisStateForLogs(), "Found the following ships:", Logging.Logging.White);
                        foreach (var shipInShipHangar in Cache.Instance.ShipHangar.Items)
                        {
                            Logging.Logging.Log(WeAreInThisStateForLogs(),
                                "GivenName [" + shipInShipHangar.GivenName.ToLower() + "] TypeName[" + shipInShipHangar.TypeName + "]", Logging.Logging.White);
                        }

                        if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GroupId == (int) Group.Capsule)
                        {
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "Capsule detected... this shouldn't happen, disabling this instance.");

                            Cache.Instance.WCFClient.GetPipeProxy.SetEveAccountAttributeValue(Cache.Instance.CharName, "IsActive", false);
                        }

                        if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                        return false;
                    }

                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // check

        private static bool DoesDefaultFittingExist(string module)
        {
            try
            {
                DefaultFittingFound = false;
                if (!DefaultFittingChecked)
                {
                    if (Logging.Logging.DebugFittingMgr)
                        Logging.Logging.Log(module, "Character Settings XML says Default Fitting is [" + MissionSettings.DefaultFittingName + "]",
                            Logging.Logging.White);

                    if (Cache.Instance.FittingManagerWindow == null)
                    {
                        Logging.Logging.Log("Arm.FindDefaultFitting", "FittingManagerWindow is null", Logging.Logging.Debug);
                        return false;
                    }

                    if (Logging.Logging.DebugFittingMgr)
                        Logging.Logging.Log(module, "Character Settings XML says Default Fitting is [" + MissionSettings.DefaultFittingName + "]",
                            Logging.Logging.White);

                    if (Cache.Instance.FittingManagerWindow.Fittings.Any())
                    {
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(module, "if (Cache.Instance.FittingManagerWindow.Fittings.Any())", Logging.Logging.Teal);
                        var i = 1;
                        foreach (var fitting in Cache.Instance.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            if (Logging.Logging.DebugFittingMgr)
                            {
                                Logging.Logging.Log(module, "[" + i + "] Found a Fitting Named: [" + fitting.Name + "]", Logging.Logging.Teal);
                            }

                            if (fitting.Name.ToLower().Equals(MissionSettings.DefaultFittingName.ToLower()))
                            {
                                DefaultFittingChecked = true;
                                DefaultFittingFound = true;
                                Logging.Logging.Log(module, "[" + i + "] Found Default Fitting [" + fitting.Name + "]", Logging.Logging.White);
                                return true;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Logging.Logging.Log("Arm.LoadFitting", "No Fittings found in the Fitting Manager at all!  Disabling fitting manager.",
                            Logging.Logging.Orange);
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        return true;
                    }

                    if (!DefaultFittingFound)
                    {
                        Logging.Logging.Log("Arm.LoadFitting",
                            "Error! Could not find Default Fitting [" + MissionSettings.DefaultFittingName.ToLower() + "].  Disabling fitting manager.",
                            Logging.Logging.Orange);
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        Settings.Instance.UseFittingManager = false;
                        Logging.Logging.Log("Arm.LoadFitting", "Closing Fitting Manager", Logging.Logging.White);
                        Cache.Instance.FittingManagerWindow.Close();

                        ChangeArmState(ArmState.MoveMissionItems);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Logging.Red);
                return false;
            }
        } // check

        public static bool ChangeArmState(ArmState state, bool wait = false)
        {
            try
            {
                switch (state)
                {
                    case ArmState.OpenShipHangar:
                        _States.CurrentCombatState = CombatState.Idle;
                        break;
                    case ArmState.NotEnoughAmmo:
                        Cache.Instance.Paused = true;
                        _States.CurrentCombatState = CombatState.Idle;
                        break;
                }


                if (_States.CurrentArmState != state)
                {
                    ClearDataBetweenStates();
                    _States.CurrentArmState = state;
                    if (wait)
                    {
                        _lastArmAction = DateTime.UtcNow;
                    }
//					else
//					{
//					  Arm.ProcessState(); // why are we calling this here again? ://
//					}
                }

                return true;
            }

            catch (Exception ex)
            {
                Logging.Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Logging.Red);
                return false;
            }
        } // let's assume this work

        private static bool MoveItemsToCargo(string itemName, int quantity, ArmState nextState, ArmState fromState,
            bool moveToNextStateIfQuantityIsBelowAsk = false)
        {
            try
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    ChangeArmState(nextState);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    if (!WaitForLockedItems(fromState)) return false;
                    return false;
                }

                if (!LookForItem(itemName, Cache.Instance.CurrentShipsCargo)) return false;


                if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar + WeHaveThisManyOfThoseItemsInAmmoHangar +
                    WeHaveThisManyOfThoseItemsInLootHangar < quantity)
                {
                    // Chant 05/03/2016 - if moveToNextStateIfQuantityIsBelowAsk = true and we don't have the requested amount, at least bring what we've got
                    if (!moveToNextStateIfQuantityIsBelowAsk)
                    {
                        Logging.Logging.Log(WeAreInThisStateForLogs(),
                            "ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] AmmoHangar has: [" + WeHaveThisManyOfThoseItemsInAmmoHangar +
                            "] LootHangar has: [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + itemName + "] we need [" + quantity + "] units)",
                            Logging.Logging.Red);
                        ItemsAreBeingMoved = false;
                        Cache.Instance.Paused = true;
                        ChangeArmState(ArmState.NotEnoughAmmo);
                        return true;
                    }
                }

                _itemsLeftToMoveQuantity = quantity - WeHaveThisManyOfThoseItemsInCargo > 0 ? quantity - WeHaveThisManyOfThoseItemsInCargo : 0;

                //  here we check if we have enough free m3 in our ship hangar

                if (Cache.Instance.CurrentShipsCargo != null && (ItemHangarItem != null || LootHangarItem != null || AmmoHangarItem != null))
                {
                    var amount = 0;
                    var freeCapacity = Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity;
                    var freeCapacityReduced = freeCapacity*0.7; // keep some free space for ammo
                    if (ItemHangarItem != null)
                        amount = Convert.ToInt32(freeCapacityReduced/ItemHangarItem.Volume);
                    else if (LootHangarItem != null)
                        amount = Convert.ToInt32(freeCapacityReduced/LootHangarItem.Volume);
                    else if (AmmoHangarItem != null)
                        amount = Convert.ToInt32(freeCapacityReduced/AmmoHangarItem.Volume);

                    _itemsLeftToMoveQuantity = Math.Min(amount, _itemsLeftToMoveQuantity);

                    Logging.Logging.Log("Arm.MoveItemsToCargo",
                        "freeCapacity [" + freeCapacity + "] freeCapacityReduced [" + freeCapacityReduced + "] amount [" + amount +
                        "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]", Logging.Logging.White);
                }
                else // we've got none of the item in our hangars, return true to move on
                {
                    Logging.Logging.Log("Arm.MoveItemsToCargo", "Cache.Instance.CurrentShipsCargo == null || ItemHangarItem != null", Logging.Logging.White);
                    ChangeArmState(nextState);
                    ItemsAreBeingMoved = false;
                    return true;
                }

                if (_itemsLeftToMoveQuantity <= 0)
                {
                    Logging.Logging.Log("Arm.MoveItemsToCargo", "if (_itemsLeftToMoveQuantity <= 0)", Logging.Logging.White);
                    ChangeArmState(nextState);
                    return false;
                }

                Logging.Logging.Log("Arm.MoveItemsToCargo", "_itemsLeftToMoveQuantity: " + _itemsLeftToMoveQuantity, Logging.Logging.White);

                if (LootHangarItem != null && !string.IsNullOrEmpty(LootHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (LootHangarItem.ItemId <= 0 || LootHangarItem.Volume == 0.00 || LootHangarItem.Quantity == 0)
                    {
                        return false;
                    }

                    var moveItemQuantity = Math.Min(LootHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    Logging.Logging.Log(WeAreInThisStateForLogs(),
                        "Moving(1) Item [" + LootHangarItem.TypeName + "] from Loothangar to CargoHold: We have [" + _itemsLeftToMoveQuantity +
                        "] more item(s) to move after this", Logging.Logging.White);


                    Cache.Instance.CurrentShipsCargo.Add(LootHangarItem, moveItemQuantity);

                    ItemsAreBeingMoved = true;
                    _lastArmAction = DateTime.UtcNow;
                    return false;
                }

                if (ItemHangarItem != null && !string.IsNullOrEmpty(ItemHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (ItemHangarItem.ItemId <= 0 || ItemHangarItem.Volume == 0.00 || ItemHangarItem.Quantity == 0)
                    {
                        return false;
                    }

                    var moveItemQuantity = Math.Min(ItemHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    Logging.Logging.Log(WeAreInThisStateForLogs(),
                        "Moving(2) Item [" + ItemHangarItem.TypeName + "] from ItemHangar to CargoHold: We have [" + _itemsLeftToMoveQuantity +
                        "] more item(s) to move after this", Logging.Logging.White);
                    Cache.Instance.CurrentShipsCargo.Add(ItemHangarItem, moveItemQuantity);
                    ItemsAreBeingMoved = true;
                    _lastArmAction = DateTime.UtcNow;
                    return false;
                }

                if (AmmoHangarItem != null && !string.IsNullOrEmpty(AmmoHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (AmmoHangarItem.ItemId <= 0 || AmmoHangarItem.Volume == 0.00 || AmmoHangarItem.Quantity == 0)
                    {
                        return false;
                    }

                    var moveItemQuantity = Math.Min(AmmoHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    Logging.Logging.Log(WeAreInThisStateForLogs(),
                        "Moving(3) Item [" + AmmoHangarItem.TypeName + "] from AmmoHangar to CargoHold: We have [" + _itemsLeftToMoveQuantity +
                        "] more item(s) to move after this", Logging.Logging.White);
                    Cache.Instance.CurrentShipsCargo.Add(AmmoHangarItem, moveItemQuantity);
                    ItemsAreBeingMoved = true;
                    _lastArmAction = DateTime.UtcNow;
                    return false;
                }

                ItemsAreBeingMoved = false;
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        }

        private static bool MoveDronesToDroneBay(string itemName, ArmState nextState, ArmState fromState)
        {
            try
            {
                if (Logging.Logging.DebugArm) Logging.Logging.Log("Arm.MoveDronesToDroneBay", "(re)Entering MoveDronesToDroneBay", Logging.Logging.Debug);

                if (string.IsNullOrEmpty(itemName))
                {
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if (string.IsNullOrEmpty(MoveItemTypeName))", Logging.Logging.Debug);
                    ChangeArmState(nextState);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if (ItemsAreBeingMoved)", Logging.Logging.Debug);
                    if (!WaitForLockedItems(fromState)) return false;
                    return false;
                }

                if (Cache.Instance.ItemHangar == null)
                {
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if (Cache.Instance.ItemHangar == null)", Logging.Logging.Debug);
                    return false;
                }

                if (Drones.DroneBay == null)
                {
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if (Drones.DroneBay == null)", Logging.Logging.Debug);
                    return false;
                }

                if (Drones.DroneBay.Capacity == 0 && DroneBayRetries <= 10)
                {
                    DroneBayRetries++;
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                        "Dronebay: not yet ready. Capacity [" + Drones.DroneBay.Capacity + "] UsedCapacity [" + Drones.DroneBay.UsedCapacity + "]",
                        Logging.Logging.White);
                    Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(2);
                    return false;
                }

                if (!LookForItem(itemName, Drones.DroneBay))
                {
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if (!LookForItem(MoveItemTypeName, Drones.DroneBay))", Logging.Logging.Debug);
                    return false;
                }

                if (Drones.DroneBay != null && DroneInvTypeItem != null && Drones.DroneBay.Items != null && Cache.Instance.AmmoHangar != null &&
                    Cache.Instance.AmmoHangar.Items != null)
                {
                    if (Drones.DroneBay.Items.Any(d => d.TypeId != DroneInvTypeItem.TypeId))
                    {
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay", "We have other drones in the bay, moving them to the ammo hangar.", Logging.Logging.Red);
                        var droneBayItem = Drones.DroneBay.Items.FirstOrDefault();
                        Cache.Instance.AmmoHangar.Add(droneBayItem);
                        Time.Instance.NextArmAction = DateTime.UtcNow.AddMilliseconds(300);
                    }
                    return false;
                }

                Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                    "Dronebay details: Capacity [" + Drones.DroneBay.Capacity + "] UsedCapacity [" + Drones.DroneBay.UsedCapacity + "]", Logging.Logging.White);

                if ((int) Drones.DroneBay.Capacity == (int) Drones.DroneBay.UsedCapacity)
                {
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if ((int)Drones.DroneBay.Capacity == (int)Drones.DroneBay.UsedCapacity)",
                        Logging.Logging.White);
                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "Dronebay is Full. No need to move any more drones.", Logging.Logging.White);
                    ChangeArmState(nextState);
                    return false;
                }

                if (Drones.DroneBay != null && DroneInvTypeItem != null && DroneInvTypeItem.Volume != 0)
                {
                    var neededDrones = (int) Math.Floor((Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity)/DroneInvTypeItem.Volume);
                    _itemsLeftToMoveQuantity = neededDrones;

                    Logging.Logging.Log("Arm.MoveDronesToDroneBay", "neededDrones: [" + neededDrones + "]", Logging.Logging.White);

                    if ((int) neededDrones == 0)
                    {
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay", "MoveItems", Logging.Logging.White);
                        ChangeArmState(ArmState.MoveMissionItems);
                        return false;
                    }

                    if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar + WeHaveThisManyOfThoseItemsInAmmoHangar +
                        WeHaveThisManyOfThoseItemsInLootHangar < neededDrones)
                    {
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                            "ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] AmmoHangar has: [" + WeHaveThisManyOfThoseItemsInAmmoHangar +
                            "] LootHangar has: [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + itemName + "] we need [" + neededDrones +
                            "] drones to fill the DroneBay)", Logging.Logging.Red);
                        ItemsAreBeingMoved = false;
                        Cache.Instance.Paused = true;
                        ChangeArmState(ArmState.NotEnoughDrones);
                        return true;
                    }


                    //  here we check if we have enough free m3 in our drone hangar

                    if (Drones.DroneBay != null && DroneInvTypeItem != null && DroneInvTypeItem.Volume != 0)
                    {
                        var freeCapacity = Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity;

                        Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                            "freeCapacity [" + freeCapacity + "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]" + " DroneInvTypeItem.Volume [" +
                            DroneInvTypeItem.Volume + "]", Logging.Logging.White);

                        var amount = Convert.ToInt32(freeCapacity/DroneInvTypeItem.Volume);
                        _itemsLeftToMoveQuantity = Math.Min(amount, _itemsLeftToMoveQuantity);

                        Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                            "freeCapacity [" + freeCapacity + "] amount [" + amount + "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]",
                            Logging.Logging.White);
                    }
                    else
                    {
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay", "Drones.DroneBay || ItemHangarItem != null", Logging.Logging.White);
                        ChangeArmState(nextState);
                        return false;
                    }

//					if (cargoItems.Any())  // check the local cargo for items and subtract the items in the cargo from the quantity we still need to move to our cargohold
//					{
//
//						foreach (DirectItem moveItemInCargo in cargoItems)
//						{
//							_itemsLeftToMoveQuantity -= moveItemInCargo.Stacksize;
//							if (_itemsLeftToMoveQuantity <= 0)
//							{
//								ChangeArmState(nextState);
//								return true;
//							}
//
//							continue;
//						}
//					}

                    if (_itemsLeftToMoveQuantity <= 0)
                    {
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay", "if (_itemsLeftToMoveQuantity <= 0)", Logging.Logging.White);
                        ChangeArmState(nextState);
                        return false;
                    }

                    if (LootHangarItem != null && !string.IsNullOrEmpty(LootHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                    {
                        if (LootHangarItem.ItemId <= 0 || LootHangarItem.Volume == 0.00 || LootHangarItem.Quantity == 0)
                        {
                            return false;
                        }

                        var moveDroneQuantity = Math.Min(LootHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                        moveDroneQuantity = Math.Max(moveDroneQuantity, 1);
                        _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                            "Moving(4) Item [" + LootHangarItem.TypeName + "] from LootHangar to DroneBay: We have [" + _itemsLeftToMoveQuantity +
                            "] more item(s) to move after this", Logging.Logging.White);
                        Drones.DroneBay.Add(LootHangarItem, moveDroneQuantity);
                        ItemsAreBeingMoved = true;
                        _lastArmAction = DateTime.UtcNow;
                        return false;
                    }

                    if (ItemHangarItem != null && !string.IsNullOrEmpty(ItemHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                    {
                        if (ItemHangarItem.ItemId <= 0 || ItemHangarItem.Volume == 0.00 || ItemHangarItem.Quantity == 0)
                        {
                            return false;
                        }

                        var moveDroneQuantity = Math.Min(ItemHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                        moveDroneQuantity = Math.Max(moveDroneQuantity, 1);
                        _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                            "Moving Item(5) [" + ItemHangarItem.TypeName + "] from ItemHangar to DroneBay: We have [" + _itemsLeftToMoveQuantity +
                            "] more item(s) to move after this", Logging.Logging.White);
                        Drones.DroneBay.Add(ItemHangarItem, moveDroneQuantity);
                        ItemsAreBeingMoved = true;
                        _lastArmAction = DateTime.UtcNow;
                        return false;
                    }

                    if (AmmoHangarItem != null && !string.IsNullOrEmpty(AmmoHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                    {
                        if (AmmoHangarItem.ItemId <= 0 || AmmoHangarItem.Volume == 0.00 || AmmoHangarItem.Quantity == 0)
                        {
                            return false;
                        }

                        var moveDroneQuantity = Math.Min(AmmoHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                        moveDroneQuantity = Math.Max(moveDroneQuantity, 1);
                        _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                        Logging.Logging.Log("Arm.MoveDronesToDroneBay",
                            "Moving(6) Item [" + AmmoHangarItem.TypeName + "] from AmmoHangar to DroneBay: We have [" + _itemsLeftToMoveQuantity +
                            "] more item(s) to move after this", Logging.Logging.White);
                        Drones.DroneBay.Add(AmmoHangarItem, moveDroneQuantity);
                        ItemsAreBeingMoved = true;
                        _lastArmAction = DateTime.UtcNow;
                        return false;
                    }

                    return true;
                }

                Logging.Logging.Log("Arm.MoveDronesToDroneBay", "droneTypeId is highly likely to be incorrect in your settings xml", Logging.Logging.Debug);
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Arm.MoveDronesToDroneBay", "Exception [" + ex + "]", Logging.Logging.Red);
                return false;
            }
        }

        /*************** <-- BELOW USED STATES --> ***************/

        private static bool BeginArm() // --> ArmState.ActivateCombatShip
        {
            try
            {
                Time.Instance.LastReloadAttemptTimeStamp = new Dictionary<long, DateTime>();
                Time.Instance.LastReloadedTimeStamp = new Dictionary<long, DateTime>();
                //_ammoTypesToLoad.Clear();
                switchingShips = false;
                UseMissionShip = false; // Were we successful in activating the mission specific ship?
                DefaultFittingChecked = false; //flag to check for the correct default fitting before using the fitting manager
                DefaultFittingFound = false; //Did we find the default fitting?
                CustomFittingFound = false;
                ItemsAreBeingMoved = false;
                SwitchShipsOnly = false;
                if (Logging.Logging.DebugArm)
                    Logging.Logging.Log(WeAreInThisStateForLogs(),
                        "Cache.Instance.BringOptionalMissionItemQuantity is [" + MissionSettings.MoveOptionalMissionItemQuantity + "]", Logging.Logging.Debug);
                ItemHangarRetries = 0;
                DroneBayRetries = 0;
                RefreshMissionItems(Cache.Instance.Agent.AgentId);
                _States.CurrentCombatState = CombatState.Idle;

                ChangeArmState(ArmState.ActivateCombatShip);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // --> ArmState.ActivateCombatShip

        private static bool ActivateCombatShip() // -> ArmState.RepairShop
        {
            try
            {
                if (string.IsNullOrEmpty(Combat.Combat.CombatShipName))
                {
                    Logging.Logging.Log(WeAreInThisStateForLogs(), "Could not find CombatShipName: " + Combat.Combat.CombatShipName + " in settings!",
                        Logging.Logging.Orange);
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                if (!ActivateShip(Combat.Combat.CombatShipName))
                {
                    return false;
                }

                if (SwitchShipsOnly)
                {
                    ChangeArmState(ArmState.Done, true);
                    SwitchShipsOnly = false;
                    return true;
                }

                ChangeArmState(ArmState.RepairShop, true);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // --> ArmState.RepairShop

        private static bool RepairShop() // --> ArmState.LoadSavedFitting
        {
            try
            {
//				Arm.NeedRepair = true;  // enable repair by default

                if (Panic.UseStationRepair && NeedRepair)
                {
                    if (!Cache.Instance.RepairItems(WeAreInThisStateForLogs())) return false; //attempt to use repair facilities if avail in station
                }

                NeedRepair = false;

                LastRepairDateTime = DateTime.UtcNow;
                ChangeArmState(ArmState.StripFitting, true);

                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // --> ArmState.LoadEmptyFitting


        private static bool StripFitting()
        {
            if (!Settings.Instance.UseFittingManager)
            {
                ChangeArmState(ArmState.MoveDrones, true);
                return true;
            }

            if (Cache.Instance.FittingManagerWindow == null) return false;

            // if there are no offline modules we dont need to load the empty fitting
            if (!MissionSettings.OfflineModulesFound)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Not loading empty fitting as there are no offline modules.", Logging.Logging.White);
                ChangeArmState(ArmState.LoadSavedFitting, true);
                return true;
            }

            MissionSettings.CurrentFit = String.Empty; // force to acutally select the correct mission fitting
            var currentShip = Cache.Instance.ActiveShip;
            currentShip.StripFitting();

            ChangeArmState(ArmState.LoadSavedFitting, true);
            return true;
        } // --> ArmState.LoadSavedFtting


        private static bool LoadSavedFitting() // --> ArmState.MoveDrones
        {
            try
            {
//				if(LastRepairDateTime.AddSeconds(20) < DateTime.UtcNow) {
//					Logging.Log(WeAreInThisStateForLogs(), "FAILED selecting Fitting. Moving next state.", Logging.White);
//					ChangeArmState(ArmState.MoveDrones, true);
//					return true;
//				}
//				

                var agent = Cache.Instance.Agent;

                if (agent == null)
                {
                    ChangeArmState(ArmState.MoveDrones, true);
                    return true;
                }


                try
                {
                    if (Cache.Instance.GetAgentMission(agent.AgentId, false).State != (int) MissionState.Accepted)
                    {
                        ChangeArmState(ArmState.MoveDrones, true);
                        return true;
                    }
                }
                catch (Exception)
                {
                    ChangeArmState(ArmState.MoveDrones, true);
                    return true;
                }


                if (Settings.Instance.UseFittingManager && MissionSettings.Mission != null)
                {
                    //If we are already loading a fitting...
                    if (ItemsAreBeingMoved)
                    {
                        if (!WaitForLockedItems(ArmState.MoveDrones)) return false;
                        return true;
                    }

                    if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior) //|| _States.CurrentQuestorState == QuestorState.BackgroundBehavior)
                    {
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)",
                                Logging.Logging.Teal);

                        if (!DoesDefaultFittingExist(WeAreInThisStateForLogs())) return false;

                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "These are the reasons we would use or not use the fitting manager.(below)",
                                Logging.Logging.Teal);
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "DefaultFittingFound [" + DefaultFittingFound + "]", Logging.Logging.Teal);
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "UseMissionShip [" + UseMissionShip + "]", Logging.Logging.Teal);
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(),
                                "Cache.Instance.ChangeMissionShipFittings [" + MissionSettings.ChangeMissionShipFittings + "]", Logging.Logging.Teal);
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(),
                                "if ((!Settings.Instance.UseFittingManager || !DefaultFittingFound) || (UseMissionShip && !Cache.Instance.ChangeMissionShipFittings)) then do not use fitting manager",
                                Logging.Logging.Teal);
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "These are the reasons we would use or not use the fitting manager.(above)",
                                Logging.Logging.Teal);

                        if ((!DefaultFittingFound) || (UseMissionShip && !MissionSettings.ChangeMissionShipFittings))
                        {
                            if (Logging.Logging.DebugFittingMgr)
                                Logging.Logging.Log(WeAreInThisStateForLogs(),
                                    "if ((!Settings.Instance.UseFittingManager || !DefaultFittingFound) || (UseMissionShip && !Cache.Instance.ChangeMissionShipFittings))",
                                    Logging.Logging.Teal);
                            ChangeArmState(ArmState.MoveDrones, true);
                            return false;
                        }

                        //let's check first if we need to change fitting at all
                        Logging.Logging.Log(WeAreInThisStateForLogs(),
                            "Fitting: " + MissionSettings.FittingToLoad + " - currentFit: " + MissionSettings.CurrentFit, Logging.Logging.White);
                        if (MissionSettings.FittingToLoad.Equals(MissionSettings.CurrentFit))
                        {
                            Logging.Logging.Log(WeAreInThisStateForLogs(), "Current fit is now correct", Logging.Logging.White);
                            ChangeArmState(ArmState.MoveDrones, true);
                            return true;
                        }

                        if (Cache.Instance.FittingManagerWindow == null) return false;

                        Logging.Logging.Log(WeAreInThisStateForLogs(), "Looking for saved fitting named: [" + MissionSettings.FittingToLoad + " ]",
                            Logging.Logging.White);

                        foreach (var fitting in Cache.Instance.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            var currentShip = Cache.Instance.ActiveShip;
                            if (MissionSettings.FittingToLoad.ToLower().Equals(fitting.Name.ToLower()) && fitting.ShipTypeId == currentShip.TypeId)
                            {
                                Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                                Logging.Logging.Log(WeAreInThisStateForLogs(),
                                    "Found saved fitting named: [ " + fitting.Name + " ][" +
                                    Math.Round(Time.Instance.NextArmAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.Logging.White);

                                //switch to the requested fitting for the current mission
                                fitting.Fit();
                                _lastArmAction = DateTime.UtcNow;
                                _lastFitAction = DateTime.UtcNow;
                                ItemsAreBeingMoved = true;
                                MissionSettings.CurrentFit = fitting.Name;
                                MissionSettings.OfflineModulesFound = false;
                                CustomFittingFound = true;
                                return false;
                            }

                            continue;
                        }

                        //if we did not find it, we'll set currentfit to default
                        //this should provide backwards compatibility without trying to fit always
                        if (!CustomFittingFound)
                        {
                            if (UseMissionShip)
                            {
                                Logging.Logging.Log(WeAreInThisStateForLogs(), "Could not find fitting for this ship typeid.  Using current fitting.",
                                    Logging.Logging.Orange);
                                ChangeArmState(ArmState.MoveDrones, true);
                                return false;
                            }

                            Logging.Logging.Log(WeAreInThisStateForLogs(), "Could not find fitting - switching to default", Logging.Logging.Orange);
                            MissionSettings.FittingToLoad = MissionSettings.DefaultFittingName;
                            ChangeArmState(ArmState.MoveDrones, true);
                            return false;
                        }
                    }
                }

                ChangeArmState(ArmState.MoveDrones, true);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // --> ArmState.MoveDrones

        private static bool MoveDrones() // --> ArmState.MoveMissionItems
        {
            try
            {
                if (DateTime.UtcNow < _lastFitAction.AddMilliseconds(Cache.Instance.RandomNumber(400, 600)))
                {
                    //if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (DateTime.UtcNow < Cache.Instance.NextArmAction)) return;", Logging.Teal);
                    return false;
                }

                if (!Drones.UseDrones)
                {
                    //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", "UseDrones is [" + Drones.UseDrones + "] Changing ArmState to MoveBringItems",Logging.Debug);
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }

                if (Cache.Instance.ActiveShip.GroupId == (int) Group.Shuttle ||
                    Cache.Instance.ActiveShip.GroupId == (int) Group.Industrial ||
                    Cache.Instance.ActiveShip.GroupId == (int) Group.TransportShip)
                {
                    //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", "ActiveShip GroupID is [" + Cache.Instance.ActiveShip.GroupId + "] Which we assume is a Shuttle, Industrial, TransportShip: Changing ArmState to MoveBringItems", Logging.Debug);
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }

                if (Cache.Instance.ActiveShip.GivenName != Combat.Combat.CombatShipName)
                {
                    //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", "ActiveShip Name is [" + Cache.Instance.ActiveShip.GivenName + "] Which is not the CombatShipname [" + Combat.CombatShipName + "]: Changing ArmState to MoveBringItems", Logging.Debug);
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }


                if (DroneInvTypeItem == null)
                {
                    Logging.Logging.Log("Arm.MoveDrones", "(DroneInvTypeItem == null)", Logging.Logging.Orange);
                    return false;
                }

                //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", " DroneInvTypeItem.TypeName: " + DroneInvTypeItem.TypeName, Logging.Orange);

                if (!MoveDronesToDroneBay(DroneInvTypeItem.TypeName, ArmState.MoveMissionItems, ArmState.MoveDrones))
                {
                    return false;
                }

                //Logging.Log("Arm.MoveDrones", "MoveDronesToDroneBay returned true! CurrentArmState is [" + _States.CurrentArmState + "]: this should NOT still be MoveDrones!", Logging.Orange);
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Debug);
                return false;
            }
        } // --> ArmState.MoveMissionItems

        private static bool MoveMissionItems() // --> MoveOptionalItems
        {
            if (
                !MoveItemsToCargo(MissionSettings.MoveMissionItems, MissionSettings.MoveMissionItemsQuantity, ArmState.MoveOptionalItems,
                    ArmState.MoveMissionItems, false)) return false;
            return false;
        } // --> MoveOptionalItems

        private static bool MoveOptionalItems() // --> ArmState.MoveScripts
        {
            if (
                !MoveItemsToCargo(MissionSettings.MoveOptionalMissionItems, MissionSettings.MoveOptionalMissionItemQuantity, ArmState.MoveScripts,
                    ArmState.MoveOptionalItems, true)) return false;
            return false;
        } // --> ArmState.MoveScripts

        // Chant - 05/02/2016 - need to load sensor manipulation scripts if specified
        private static bool MoveScripts() // --> ArmState.MoveCapBoosters
        {
            if (Cache.Instance.ActiveShip.GivenName != Combat.Combat.CombatShipName)
            {
                Logging.Logging.Log("Arm.MoveScripts", "if (Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)", Logging.Logging.White);
                ChangeArmState(ArmState.MoveCapBoosters);
                return false;
            }

            var TrackingDisruptorScriptsLeft = 0;
            var TrackingComputerScriptsLeft = 0;
            var TrackingLinkScriptsLeft = 0;
            var SensorBoosterScriptsLeft = 0;
            var SensorDampenerScriptsLeft = 0;
            var CapacitorInjectorScriptsLeft = 0;
            var AncillaryShieldBoosterScriptsLeft = 0;

            if (!bWaitingonScripts)
            {
                TrackingDisruptorScriptsLeft =
                    TrackingDisruptorScripts =
                        Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.TrackingDisruptor).Sum(i => i.Quantity));
                TrackingComputerScriptsLeft =
                    TrackingComputerScripts =
                        Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.TrackingComputer).Sum(i => i.Quantity));
                TrackingLinkScriptsLeft =
                    TrackingLinkScripts = Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.TrackingLink).Sum(i => i.Quantity));
                SensorBoosterScriptsLeft =
                    SensorBoosterScripts = Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.SensorBooster).Sum(i => i.Quantity));
                SensorDampenerScriptsLeft =
                    SensorDampenerScripts =
                        Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.SensorDampener).Sum(i => i.Quantity));
                CapacitorInjectorScriptsLeft =
                    CapacitorInjectorScripts =
                        Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.CapacitorInjector).Sum(i => i.Quantity));
                AncillaryShieldBoosterScriptsLeft =
                    AncillaryShieldBoosterScripts =
                        Math.Abs(Cache.Instance.FittedModules.Items.Where(i => i.GroupId == (int) Group.AncillaryShieldBooster).Sum(i => i.Quantity));

                bWaitingonScripts = true;
            }
            else
            {
                TrackingDisruptorScriptsLeft = Math.Max(0,
                    TrackingDisruptorScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.TrackingDisruptorScript).Sum(i => i.Quantity)));
                TrackingComputerScriptsLeft = Math.Max(0,
                    TrackingComputerScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.TrackingComputerScript).Sum(i => i.Quantity)));
                TrackingLinkScriptsLeft = Math.Max(0,
                    TrackingLinkScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.TrackingLinkScript).Sum(i => i.Quantity)));
                SensorBoosterScriptsLeft = Math.Max(0,
                    SensorBoosterScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.SensorBoosterScript).Sum(i => i.Quantity)));
                SensorDampenerScriptsLeft = Math.Max(0,
                    SensorDampenerScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.SensorDampenerScript).Sum(i => i.Quantity)));
                CapacitorInjectorScriptsLeft = Math.Max(0,
                    CapacitorInjectorScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.CapacitorInjectorScript).Sum(i => i.Quantity)));
                AncillaryShieldBoosterScriptsLeft = Math.Max(0,
                    AncillaryShieldBoosterScripts -
                    Math.Abs(Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == Settings.Instance.AncillaryShieldBoosterScript).Sum(i => i.Quantity)));
            }

            DirectInvType _ScriptInvTypeItem = null;
            if ((TrackingDisruptorScriptsLeft >= 1) &&
                Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.TrackingDisruptorScript, out _ScriptInvTypeItem))
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, TrackingDisruptorScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Tracking Disruptor scripts in hangar", Logging.Logging.Orange);
                    TrackingDisruptorScriptsLeft = 0;
                    TrackingDisruptorScripts = 0;
                }
                return false;
            }
            if ((TrackingComputerScriptsLeft >= 1) &&
                Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.TrackingComputerScript, out _ScriptInvTypeItem))
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, TrackingComputerScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Tracking Computer scripts in hangar", Logging.Logging.Orange);
                    TrackingComputerScriptsLeft = 0;
                    TrackingComputerScripts = 0;
                }
                return false;
            }
            if ((TrackingLinkScriptsLeft >= 1) && Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.TrackingLinkScript, out _ScriptInvTypeItem))
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, TrackingLinkScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Tracking Link scripts in hangar", Logging.Logging.Orange);
                    TrackingLinkScriptsLeft = 0;
                    TrackingLinkScripts = 0;
                }
                return false;
            }
            if ((SensorBoosterScriptsLeft >= 1) && Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.SensorBoosterScript, out _ScriptInvTypeItem))
            {
                Logging.Logging.Log("Arm.MoveScripts", "[" + SensorBoosterScriptsLeft + "] SensorBoosterScriptsLeft", Logging.Logging.Orange);
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, SensorBoosterScripts, ArmState.MoveScripts, ArmState.MoveScripts, true) && !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Sensor Booster scripts in hangar", Logging.Logging.Orange);
                    SensorBoosterScriptsLeft = 0;
                    SensorBoosterScripts = 0;
                }
                return false;
            }
            if ((SensorDampenerScriptsLeft >= 1) &&
                Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.SensorDampenerScript, out _ScriptInvTypeItem))
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, SensorDampenerScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Sensor Dampener scripts in hangar", Logging.Logging.Orange);
                    SensorDampenerScriptsLeft = 0;
                    SensorDampenerScripts = 0;
                }
                return false;
            }
            if ((CapacitorInjectorScriptsLeft >= 1) &&
                Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.CapacitorInjectorScript, out _ScriptInvTypeItem))
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, CapacitorInjectorScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Capacitor Injector scripts in hangar", Logging.Logging.Orange);
                    CapacitorInjectorScriptsLeft = 0;
                    CapacitorInjectorScripts = 0;
                }
                return false;
            }
            if ((AncillaryShieldBoosterScriptsLeft >= 1) &&
                Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.AncillaryShieldBoosterScript, out _ScriptInvTypeItem))
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, AncillaryShieldBoosterScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Logging.Logging.Log("Arm.MoveScripts", "Not enough Ancillary Shield Booster scripts in hangar", Logging.Logging.Orange);
                    AncillaryShieldBoosterScriptsLeft = 0;
                    AncillaryShieldBoosterScripts = 0;
                }
                return false;
            }

            Logging.Logging.Log("Arm.MoveScripts", "Finished moving scripts", Logging.Logging.Orange);
            bWaitingonScripts = false;
            ChangeArmState(ArmState.MoveCapBoosters, true);
            //return successfullyMovedScripts;
            return false;
        } // --> ArmState.MoveCapBoosters

        private static bool MoveCapBoosters() // --> ArmState.MoveAmmo
        {
            if (Cache.Instance.ActiveShip.GivenName != Combat.Combat.CombatShipName)
            {
                Logging.Logging.Log("Arm.MoveCapBoosters", "if (Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)", Logging.Logging.White);
                ChangeArmState(ArmState.MoveAmmo);
                return false;
            }

            DirectInvType _CapBoosterInvTypeItem = null;
            Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.CapacitorInjectorScript, out _CapBoosterInvTypeItem);


            if (ArmLoadCapBoosters && _CapBoosterInvTypeItem != null)
            {
                Logging.Logging.Log("Arm.MoveCapBoosters", "Calling MoveItemsToCargo", Logging.Logging.White);
                if (!MoveItemsToCargo(_CapBoosterInvTypeItem.TypeName, Settings.Instance.NumberOfCapBoostersToLoad, ArmState.MoveAmmo, ArmState.MoveCapBoosters))
                {
                    return false;
                }
            }

            ChangeArmState(ArmState.MoveAmmo, true);
            return false;
        } // --> ArmState.MoveAmmo

        private static bool MoveAmmo() // --> ArmState.StackAmmoHangar
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(Cache.Instance.RandomNumber(1500, 2000)))
                {
                    //if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (DateTime.UtcNow < Cache.Instance.NextArmAction)) return;", Logging.Teal);
                    return false;
                }

                if (Cache.Instance.ActiveShip.GroupId == (int) Group.Shuttle ||
                    Cache.Instance.ActiveShip.GroupId == (int) Group.Industrial ||
                    Cache.Instance.ActiveShip.GroupId == (int) Group.TransportShip ||
                    Cache.Instance.ActiveShip.GivenName != Combat.Combat.CombatShipName)
                {
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                if (Cache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                    || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                    || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                    || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
                {
                    Logging.Logging.Log(WeAreInThisStateForLogs(), "No ammo needed for civilian guns: done", Logging.Logging.White);
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    if (!WaitForLockedItems(ArmState.MoveAmmo)) return false;
                    return true; // this might make trouble
                }

                var CurrentAmmoToLoad = MissionSettings.AmmoTypesToLoad.FirstOrDefault().Key;
                    // make sure we actually have something in the list of AmmoToLoad before trying to load ammo.
                if (CurrentAmmoToLoad == null)
                {
                    Logging.Logging.Log("Arm.MoveAmmo", "We have no more ammo types to be loaded. We have to be finished with arm.", Logging.Logging.White);
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                try
                {
                    AmmoHangarItems = null;
                    IEnumerable<DirectItem> AmmoItems = null;
                    if (Cache.Instance.AmmoHangar != null && Cache.Instance.AmmoHangar.Items != null)
                    {
                        Logging.Logging.Log("Arm.MoveAmmo", "if (Cache.Instance.AmmoHangar != null && Cache.Instance.AmmoHangar.Items != null)",
                            Logging.Logging.White);
                        AmmoHangarItems =
                            Cache.Instance.AmmoHangar.Items.Where(i => i.TypeId == CurrentAmmoToLoad.TypeId)
                                .OrderBy(i => !i.IsSingleton)
                                .ThenByDescending(i => i.Quantity)
                                .ToList();
                        AmmoItems = AmmoHangarItems.ToList();
                    }

                    if (AmmoHangarItems == null)
                    {
                        _lastArmAction = DateTime.UtcNow;
                        Logging.Logging.Log("Arm.MoveAmmo", "if(AmmoHangarItems == null)", Logging.Logging.White);
                        return false;
                    }

                    if (Logging.Logging.DebugArm)
                        Logging.Logging.Log("Arm.MoveAmmo",
                            "Ammohangar has [" + AmmoHangarItems.Count() + "] items with the right typeID [" + CurrentAmmoToLoad.TypeId +
                            "] for this ammoType. MoveAmmo will use AmmoHangar", Logging.Logging.Debug);
                    if (!AmmoHangarItems.Any())
                    {
                        if (Cache.storyline != null && Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items != null &&
                            Cache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == CurrentAmmoToLoad.TypeId))
                        {
                            Logging.Logging.Log("Arm.MoveAmmo", "We don't have enough ammo, but since we are on a storyline we ignore that.",
                                Logging.Logging.White);

                            MissionSettings.AmmoTypesToLoad.Remove(CurrentAmmoToLoad);
                            return false;
                        }

                        ItemHangarRetries++;
                        if (ItemHangarRetries < 10) //just retry... after 10 tries try to use the itemhangar instead of ammohangar
                        {
                            return false;
                        }


                        foreach (var ammo in MissionSettings.AmmoTypesToLoad)
                        {
                            Logging.Logging.Log("Arm",
                                "Ammohangar was Missing [" + ammo.Key.Quantity + "] units of ammo: [ " + ammo.Key.Description + " ] with TypeId [" +
                                ammo.Key.TypeId + "] trying item hangar next", Logging.Logging.Orange);
                        }

                        try
                        {
                            ItemHangarItems =
                                Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == CurrentAmmoToLoad.TypeId)
                                    .OrderBy(i => !i.IsSingleton)
                                    .ThenByDescending(i => i.Quantity);
                            AmmoItems = ItemHangarItems;
                            if (Logging.Logging.DebugArm)
                            {
                                Logging.Logging.Log("Arm",
                                    "Itemhangar has [" + ItemHangarItems.Count() + "] items with the right typeID [" + CurrentAmmoToLoad.TypeId +
                                    "] for this ammoType. MoveAmmo will use ItemHangar", Logging.Logging.Debug);
                            }
                            if (!ItemHangarItems.Any())
                            {
                                ItemHangarRetries++;
                                if (ItemHangarRetries < 10) //just retry... after 10 tries fail and let the user know we are out of ammo
                                {
                                    return false;
                                }

                                foreach (var ammo in MissionSettings.AmmoTypesToLoad)
                                {
                                    Logging.Logging.Log("Arm",
                                        "Itemhangar was Missing [" + ammo.Key.Quantity + "] units of ammo: [ " + ammo.Key.Description + " ] with TypeId [" +
                                        ammo.Key.TypeId + "]", Logging.Logging.Orange);
                                }

                                ChangeArmState(ArmState.NotEnoughAmmo);
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            Logging.Logging.Log("Arm.MoveItems", "Itemhangar Exception [" + exception + "]", Logging.Logging.Debug);
                        }
                    }

                    try
                    {
                        var itemnum = 0;

                        if (AmmoItems != null)
                        {
                            AmmoItems = AmmoItems.ToList();
                            if (AmmoItems.Any())
                            {
                                foreach (var item in AmmoItems)
                                {
                                    itemnum++;
                                    var moveAmmoQuantity = Math.Min(item.Stacksize, CurrentAmmoToLoad.Quantity); // this shoulda work

                                    moveAmmoQuantity = Math.Max(moveAmmoQuantity, 1); // this should work also

                                    if (Logging.Logging.DebugArm)
                                        Logging.Logging.Log("Arm.MoveAmmo",
                                            "In Hangar we have: [" + itemnum + "] TypeName [" + item.TypeName + "] StackSize [" + item.Stacksize +
                                            "] - CurrentAmmoToLoad.Quantity [" + CurrentAmmoToLoad.Quantity + "] Actual moveAmmoQuantity [" + moveAmmoQuantity +
                                            "]", Logging.Logging.White);

                                    if ((moveAmmoQuantity <= item.Stacksize) && moveAmmoQuantity >= 1)
                                    {
                                        Logging.Logging.Log("Arm.MoveAmmo",
                                            "Moving [" + moveAmmoQuantity + "] units of Ammo  [" + item.TypeName + "] from [ AmmoHangar ] to CargoHold",
                                            Logging.Logging.White);
                                        //
                                        // move items to cargo
                                        //
                                        Cache.Instance.CurrentShipsCargo.Add(item, moveAmmoQuantity);
                                        ItemsAreBeingMoved = true;
                                        _lastArmAction = DateTime.UtcNow;

                                        //
                                        // subtract the moved items from the items that need to be moved
                                        //

                                        CurrentAmmoToLoad.Quantity -= moveAmmoQuantity;

                                        MissionSettings.AmmoTypesToLoad.Remove(CurrentAmmoToLoad);
                                        MissionSettings.AmmoTypesToLoad.Add(CurrentAmmoToLoad, DateTime.UtcNow);


                                        if (CurrentAmmoToLoad.Quantity <= 0)
                                        {
                                            //
                                            // if we have moved all the ammo of this type that needs to be moved remove this type of ammo from the list of ammos that need to be moved
                                            // 
                                            MissionSettings.AmmoTypesToLoad.Remove(CurrentAmmoToLoad);
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        Logging.Logging.Log("Arm.MoveAmmo",
                                            "While calculating what to move we wanted to move [" + moveAmmoQuantity + "] units of Ammo  [" + item.TypeName +
                                            "] from [ AmmoHangar ] to CargoHold, but somehow the current Item Stacksize is only [" + item.Stacksize + "]",
                                            Logging.Logging.White);
                                        continue;
                                    }

                                    return false; //you can only move one set of items per frame.
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Logging.Logging.Log("Arm.MoveAmmo", "AmmoItems Exception [" + exception + "]", Logging.Logging.Debug);
                    }
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Arm.MoveAmmo", "Error while processing Itemhangar Items exception was: [" + exception + "]", Logging.Logging.Debug);
                }

                if (MissionSettings.AmmoTypesToLoad.Any()) //if we still have any ammo to load here then we must be missing ammo
                {
                    foreach (var ammo in MissionSettings.AmmoTypesToLoad)
                    {
                        Logging.Logging.Log("Arm.MoveAmmo",
                            "Missing [" + ammo.Key.Quantity + "] units of ammo: [ " + ammo.Key.Description + " ] with TypeId [" + ammo.Key.TypeId + "]",
                            Logging.Logging.Orange);
                    }

                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                _lastArmAction = DateTime.UtcNow;
                ChangeArmState(ArmState.StackAmmoHangar);
                return false;
            }
            catch (Exception ex)
            {
                if (Logging.Logging.DebugArm) Logging.Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Logging.Teal);
                return false;
            }
        } // --> ArmState.StackAmmoHangar

        private static bool StackAmmoHangar() // --> ArmState.Done
        {
            if (!Cache.Instance.StackAmmoHangar(WeAreInThisStateForLogs())) return false;
            Cleanup();
            ChangeArmState(ArmState.Done);
            return true;
        } // --> ArmState.Done

        private static bool Cleanup() // not used atm
        {
//			if (Drones.UseDrones && (Cache.Instance.ActiveShip.GroupId != (int)Group.Shuttle && Cache.Instance.ActiveShip.GroupId != (int)Group.Industrial && Cache.Instance.ActiveShip.GroupId != (int)Group.TransportShip))
//			{
//				// Close the drone bay, its not required in space.
//				if (!Drones.CloseDroneBayWindow(WeAreInThisStateForLogs())) return false;
//			}

//			if (Settings.Instance.UseFittingManager)
//			{
//				if (!Cache.Instance.CloseFittingManager(WeAreInThisStateForLogs())) return false;
//			}

            if (Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault() != null)
            {
                Cache.Instance.FittingManagerWindow.Close();
                Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
                Cache.Instance.FittingManagerWindow = null;
                return true;
            }


            //if (!Cleanup.CloseInventoryWindows()) return false;
            _States.CurrentArmState = ArmState.Done;
            return false;
        } // not used atm
    }
}