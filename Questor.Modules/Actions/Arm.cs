﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Collections;
using System.IO;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Questor.Modules.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DirectEve;
    using System.Globalization;
    using global::Questor.Modules.BackgroundTasks;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Combat;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;
    using global::Questor.Modules.Logging;
    
    public static class Arm
    {
        static Arm()
        {
            //_ammoTypesToLoad = new List<Ammo>();
            CrystalsToLoad = new List<MiningCrystals>();
        }

        private static List<MiningCrystals> CrystalsToLoad;
        private static bool ItemsAreBeingMoved;
        private static DateTime _lastArmAction;

        private static int _itemsLeftToMoveQuantity;
        
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
        private static DirectInvType DroneInvTypeItem
        {
            get
            {
                try
                {
                    if (_droneInvTypeItem == null)
                    {
                        Cache.Instance.DirectEve.InvTypes.TryGetValue(Drones.DroneTypeID, out _droneInvTypeItem);
                    }

                    return _droneInvTypeItem;
                }
                catch (Exception ex)
                {
                    Logging.Log("LoadSpecificAmmoTypeForNonMissionSituations", "Exception [" + ex + "]", Logging.Debug);
                    return null;
                }
            }
        }
        
        public static bool ArmLoadCapBoosters { get; set; }
        public static bool NeedRepair { get; set; }
        private static IEnumerable<DirectItem> cargoItems;
        private static IEnumerable<DirectItem> ItemHangarItems;
        private static DirectItem ItemHangarItem;
        private static IEnumerable<DirectItem> AmmoHangarItems;
        private static DirectItem AmmoHangarItem;
        private static IEnumerable<DirectItem> LootHangarItems;
        private static DirectItem LootHangarItem;

        public static void LoadSpecificAmmoTypeForNonMissionSituations(DamageType _damageType)
        {
            try
            {
                MissionSettings.AmmoTypesToLoad = new List<Ammo>();
                MissionSettings.AmmoTypesToLoad.AddRange(Combat.Ammo.Where(a => a.DamageType == _damageType).Select(a => a.Clone()));
            }
            catch (Exception ex)
            {
                Logging.Log("LoadSpecificAmmoTypeForNonMissionSituations", "Exception [" + ex + "]", Logging.Debug);
                return;
            }
        }

        private static void LoadSpecificMiningCrystals(IEnumerable<OreType> miningCrystals)
        {
            CrystalsToLoad = new List<MiningCrystals>();
            CrystalsToLoad.AddRange(Combat.MiningCrystals.Where(a => miningCrystals.Contains(a.OreType)).Select(a => a.Clone()));
        }

        public static void RefreshMissionItems(long agentId)
        {
            if (_States.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
            {
                Settings.Instance.UseFittingManager = false;

                //Logging.Log("Cache.RefreshMissionItems", "We are not running missions so we have no mission items to refresh", Logging.Teal);
                return;
            }

            MissionSettings.MissionSpecificShip = null;
            MissionSettings.FactionSpecificShip = null;

            DirectAgentMission missionDetailsForMissionItems = Cache.Instance.GetAgentMission(agentId, false);
            if (missionDetailsForMissionItems == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(MissionSettings.FactionName))
            {
                MissionSettings.FactionName = "Default";
            }

            if (Settings.Instance.UseFittingManager)
            {


                //Set fitting to default
                MissionSettings.DefaultFittingName = MissionSettings.DefaultFitting.FittingName;
                MissionSettings.FittingToLoad = MissionSettings.DefaultFittingName;
                MissionSettings.MissionSpecificShip = null;
                MissionSettings.FactionSpecificShip = null;
                MissionSettings.ChangeMissionShipFittings = false;

                //
                // default to using the faction fitting if defined
                //
                if (!string.IsNullOrEmpty(MissionSettings.FactionFittingForThisMissionsFaction))
                {
                    MissionSettings.FittingToLoad = MissionSettings.FactionFittingForThisMissionsFaction;
                    MissionSettings.MissionDroneTypeID = Drones.FactionDroneTypeID;
                }

                if (MissionSettings.ListOfMissionFittings.Any(m => m.Mission.ToLower() == missionDetailsForMissionItems.Name.ToLower())) //priority goes to mission-specific fittings
                {
                    MissionFitting FittingNameTouseForThisMission;

                    // if we have got multiple copies of the same mission, find the one with the matching faction
                    if (MissionSettings.ListOfMissionFittings.Any(m => m.Faction.ToLower() == MissionSettings.FactionName.ToLower() && (m.Mission.ToLower() == missionDetailsForMissionItems.Name.ToLower())))
                    {
                        FittingNameTouseForThisMission = MissionSettings.ListOfMissionFittings.FirstOrDefault(m => m.Faction.ToLower() == MissionSettings.FactionName.ToLower() && (m.Mission.ToLower() == missionDetailsForMissionItems.Name.ToLower()));
                    }
                    else //otherwise just use the first copy of that mission
                    {
                        FittingNameTouseForThisMission = MissionSettings.ListOfMissionFittings.FirstOrDefault(m => m.Mission.ToLower() == missionDetailsForMissionItems.Name.ToLower());
                    }

                    if (FittingNameTouseForThisMission != null)
                    {
                        //
                        //
                        //
                        // this whole thing should be reworked to use faction fittings, drones, etc and them apply mission specific stuff if avail to overwrite it.
                        //
                        //
                        //
                        MissionSettings.MissionSpecificShip = FittingNameTouseForThisMission.Ship;
                        //
                        // if we have the drone type specified in the mission fitting entry use it, otherwise do not overwrite the default or the drone type specified by the faction
                        //
                        if (FittingNameTouseForThisMission.DroneTypeID != null)
                        {
                            MissionSettings.MissionDroneTypeID = (int)FittingNameTouseForThisMission.DroneTypeID;
                        }

                        if (!(FittingNameTouseForThisMission.Fitting == "" && MissionSettings.MissionSpecificShip != "")) // if we have both specified a mission specific ship and a fitting, then apply that fitting to the ship
                        {
                            MissionSettings.ChangeMissionShipFittings = true;
                            MissionSettings.FittingToLoad = FittingNameTouseForThisMission.Fitting;
                        }
                        else if (!string.IsNullOrEmpty(MissionSettings.FactionFittingForThisMissionsFaction))
                        {
                            MissionSettings.FittingToLoad = MissionSettings.FactionFittingForThisMissionsFaction;
                        }

                        Logging.Log("RefreshMissionItems", "Mission: " + MissionSettings.MissionSpecificShip + " - Faction: " + MissionSettings.FactionName + " - Fitting: " + MissionSettings.FittingToLoad + "]", Logging.White);
                        Logging.Log("RefreshMissionItems", "Ship: " + MissionSettings.MissionSpecificShip + " - ChangeMissionShipFittings: " + MissionSettings.ChangeMissionShipFittings + "Using DroneTypeID [" + Drones.DroneTypeID + "]", Logging.White);
                    }
                }
                else if (!string.IsNullOrEmpty(MissionSettings.FactionFittingForThisMissionsFaction)) // if no mission fittings defined, try to match by faction
                {
                    MissionSettings.FittingToLoad = MissionSettings.FactionFittingForThisMissionsFaction;
                }

                if (MissionSettings.FittingToLoad == "") // otherwise use the default
                {
                    MissionSettings.FittingToLoad = MissionSettings.DefaultFittingName;
                }
            }

            MissionSettings.MissionItems.Clear();
            MissionSettings.BringMissionItem = string.Empty;
            MissionSettings.BringOptionalMissionItem = string.Empty;

            string missionName = Logging.FilterPath(missionDetailsForMissionItems.Name);
            MissionSettings.MissionXmlPath = System.IO.Path.Combine(MissionSettings.MissionsPath, missionName + ".xml");
            if (!File.Exists(MissionSettings.MissionXmlPath))
            {
                return;
            }

            try
            {
                XDocument xdoc = XDocument.Load(MissionSettings.MissionXmlPath);
                IEnumerable<string> items = ((IEnumerable)xdoc.XPathEvaluate("//action[(translate(@name, 'LOT', 'lot')='loot') or (translate(@name, 'LOTIEM', 'lotiem')='lootitem')]/parameter[translate(@name, 'TIEM', 'tiem')='item']/@value")).Cast<XAttribute>().Select(a => ((string)a ?? string.Empty).ToLower());
                MissionSettings.MissionItems.AddRange(items);

                if (xdoc.Root != null)
                {
                    MissionSettings.BringMissionItem = (string)xdoc.Root.Element("bring") ?? string.Empty;
                    MissionSettings.BringMissionItem = MissionSettings.BringMissionItem.ToLower();
                    if (Logging.DebugArm) Logging.Log("RefreshMissionItems", "bring XML [" + xdoc.Root.Element("bring") + "] BringMissionItem [" + MissionSettings.BringMissionItem + "]", Logging.Debug);
                    MissionSettings.BringMissionItemQuantity = (int?)xdoc.Root.Element("bringquantity") ?? 1;
                    if (Logging.DebugArm) Logging.Log("RefreshMissionItems", "bringquantity XML [" + xdoc.Root.Element("bringquantity") + "] BringMissionItemQuantity [" + MissionSettings.BringMissionItemQuantity + "]", Logging.Debug);

                    MissionSettings.BringOptionalMissionItem = (string)xdoc.Root.Element("trytobring") ?? string.Empty;
                    MissionSettings.BringOptionalMissionItem = MissionSettings.BringOptionalMissionItem.ToLower();
                    if (Logging.DebugArm) Logging.Log("RefreshMissionItems", "trytobring XML [" + xdoc.Root.Element("trytobring") + "] BringOptionalMissionItem [" + MissionSettings.BringOptionalMissionItem + "]", Logging.Debug);
                    MissionSettings.BringOptionalMissionItemQuantity = (int?)xdoc.Root.Element("trytobringquantity") ?? 1;
                    if (Logging.DebugArm) Logging.Log("RefreshMissionItems", "trytobringquantity XML [" + xdoc.Root.Element("trytobringquantity") + "] BringOptionalMissionItemQuantity [" + MissionSettings.BringOptionalMissionItemQuantity + "]", Logging.Debug);

                }

                //load fitting setting from the mission file
                //Fitting = (string)xdoc.Root.Element("fitting") ?? "default";
            }
            catch (Exception ex)
            {
                Logging.Log("RefreshMissionItems", "Error loading mission XML file [" + ex.Message + "]", Logging.Orange);
            }
        }

        private static bool LookForItem(string itemToFind, DirectContainer HangarToCheckForItemsdWeAlreadyMoved)
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
                if (HangarToCheckForItemsdWeAlreadyMoved != null && HangarToCheckForItemsdWeAlreadyMoved.Items.Any())
                {
                    cargoItems = HangarToCheckForItemsdWeAlreadyMoved.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind).ToList();
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
                        if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "We have [" + Cache.Instance.ItemHangar.Items.Count() + "] total items in ItemHangar", Logging.Debug);
                        if (Cache.Instance.ItemHangar.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                        {
                            ItemHangarItems = Cache.Instance.ItemHangar.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                            ItemHangarItem = ItemHangarItems.FirstOrDefault();
                            WeHaveThisManyOfThoseItemsInItemHangar = ItemHangarItems.Sum(i => i.Stacksize);
                            if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "We have [" + WeHaveThisManyOfThoseItemsInItemHangar + "] [" + itemToFind + "] in ItemHangar", Logging.Debug);
                        }
                        else if (Cache.Instance.ItemHangar.Items.Any(i => i.TypeId == Drones.DroneTypeID))
                        {
                            IEnumerable<DirectItem> _dronesInItemHangar = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == Drones.DroneTypeID).ToList();
                            Logging.Log(WeAreInThisStateForLogs(), "[" + itemToFind + "] not found by typeName in ItemHangar, but we DID find DroneTypeID [" + Drones.DroneTypeID + "]. We found [" + _dronesInItemHangar.Count() + "] of them in the Itemhangar", Logging.Debug);
                            ItemHangarItem = _dronesInItemHangar.FirstOrDefault();
                            WeHaveThisManyOfThoseItemsInItemHangar = _dronesInItemHangar.Sum(i => i.Stacksize);
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                }

                //
                // check ammohangar for the item
                //
                try
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName))
                    {
                        if (Cache.Instance.AmmoHangar == null) return false;

                        if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "AmmoHangar is defined", Logging.Debug);
                        
                        if (Cache.Instance.AmmoHangar.Items.Any())
                        {
                            if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "We have [" + Cache.Instance.AmmoHangar.Items.Count() + "] total items in AmmoHangar", Logging.Debug);
                            if (Cache.Instance.AmmoHangar.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                            {
                                AmmoHangarItems = Cache.Instance.AmmoHangar.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                                AmmoHangarItem = AmmoHangarItems.FirstOrDefault();
                                WeHaveThisManyOfThoseItemsInAmmoHangar = AmmoHangarItems.Sum(i => i.Stacksize);
                                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "We have [" + WeHaveThisManyOfThoseItemsInAmmoHangar + "] [" + itemToFind + "] in AmmoHangar", Logging.Debug);
                            }
                            else if (Cache.Instance.AmmoHangar.Items.Any(i => i.TypeId == Drones.DroneTypeID))
                            {
                                IEnumerable<DirectItem> _dronesInItemHangar = Cache.Instance.AmmoHangar.Items.Where(i => i.TypeId == Drones.DroneTypeID).ToList();
                                Logging.Log(WeAreInThisStateForLogs(), "[" + itemToFind + "] not found by typeName in ItemHangar, but we DID find DroneTypeID [" + Drones.DroneTypeID + "]. We found [" + _dronesInItemHangar.Count() + "] of them in the AmmoHangar", Logging.Debug);
                                AmmoHangarItem = _dronesInItemHangar.FirstOrDefault();
                                WeHaveThisManyOfThoseItemsInAmmoHangar = _dronesInItemHangar.Sum(i => i.Stacksize);
                            }
                        }

                    }
                }
                catch (Exception ex)
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                }

                //
                // check loothangar for the item
                //
                try
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.LootHangarTabName) && Settings.Instance.LootHangarTabName != Settings.Instance.AmmoHangarTabName)
                    {
                        if (Cache.Instance.LootHangar == null) return false;

                        if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "LootHangar is defined and is different from AmmoHangar", Logging.Debug);
                       
                        if (Cache.Instance.LootHangar.Items.Any())
                        {
                            if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "We have [" + Cache.Instance.LootHangar.Items.Count() + "] total items in LootHangar", Logging.Debug);
                            if (Cache.Instance.LootHangar.Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                            {
                                LootHangarItems = Cache.Instance.LootHangar.Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                                LootHangarItem = LootHangarItems.FirstOrDefault();
                                WeHaveThisManyOfThoseItemsInLootHangar = LootHangarItems.Sum(i => i.Stacksize);
                                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "We have [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + itemToFind + "] in LootHangar", Logging.Debug);
                            }
                            else if (Cache.Instance.LootHangar.Items.Any(i => i.TypeId == Drones.DroneTypeID))
                            {
                                IEnumerable<DirectItem> _dronesInItemHangar = Cache.Instance.LootHangar.Items.Where(i => i.TypeId == Drones.DroneTypeID).ToList();
                                Logging.Log(WeAreInThisStateForLogs(), "[" + itemToFind + "] not found by typeName in LootHangar, but we DID find DroneTypeID [" + Drones.DroneTypeID + "]. We found [" + _dronesInItemHangar.Count() + "] of them in the Loothangar", Logging.Debug);
                                LootHangarItem = _dronesInItemHangar.FirstOrDefault();
                                WeHaveThisManyOfThoseItemsInLootHangar = _dronesInItemHangar.Sum(i => i.Stacksize);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                }
                
                //
                // we searched all hangars, hopefully found some items...
                //
                return true;
            }
            catch (Exception exception)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + exception + "]", Logging.Debug);
                return false;
            }
        }

        private static bool MoveItemsToCargo(string MoveItemTypeName,int totalMoveItemQuantity, ArmState StateToChangeToWhenDoneMoving, ArmState StateWeWereCalledFrom, bool _optional = false)
        {
            try
            {

                if (string.IsNullOrEmpty(MoveItemTypeName))
                {
                    ChangeArmState(StateToChangeToWhenDoneMoving);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    if (!WaitForLockedItems(StateWeWereCalledFrom)) return false;
                    return true;
                }

                if (!LookForItem(MoveItemTypeName, Cache.Instance.CurrentShipsCargo)) return false;

                if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar + WeHaveThisManyOfThoseItemsInAmmoHangar + WeHaveThisManyOfThoseItemsInLootHangar < totalMoveItemQuantity)
                {
                    if (_optional)
                    {
                        ChangeArmState(StateToChangeToWhenDoneMoving);
                        return true;
                    }

                    Logging.Log(WeAreInThisStateForLogs(), "ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] AmmoHangar has: [" + WeHaveThisManyOfThoseItemsInAmmoHangar + "] LootHangar has: [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + MoveItemTypeName + "] we need [" + totalMoveItemQuantity + "] units)", Logging.Red);
                    ItemsAreBeingMoved = false;
                    Cache.Instance.Paused = true;
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return true;
                }

                //
                // check the local cargo for items and subtract the items in the cargo from the quantity we still need to move to our cargohold
                //
                if (cargoItems.Any())
                {
                    _itemsLeftToMoveQuantity = totalMoveItemQuantity;
                    foreach (DirectItem moveItemInCargo in cargoItems)
                    {
                        _itemsLeftToMoveQuantity -= moveItemInCargo.Stacksize;
                        if (_itemsLeftToMoveQuantity <= 0)
                        {
                            ChangeArmState(StateToChangeToWhenDoneMoving);
                            return true;
                        }

                        continue;
                    }
                }

                if (LootHangarItem != null && !string.IsNullOrEmpty(LootHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (LootHangarItem.ItemId <= 0 || LootHangarItem.Volume == 0.00 || LootHangarItem.Quantity == 0)
                    {
                        return false;
                    }

                    int moveItemQuantity = Math.Min(LootHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    Logging.Log(WeAreInThisStateForLogs(), "Moving Item [" + LootHangarItem.TypeName + "] from Loothangar to CargoHold: We have [" + _itemsLeftToMoveQuantity + "] more item(s) to move after this", Logging.White);
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

                    int moveItemQuantity = Math.Min(ItemHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    Logging.Log(WeAreInThisStateForLogs(), "Moving Item [" + ItemHangarItem.TypeName + "] from ItemHangar to CargoHold: We have [" + _itemsLeftToMoveQuantity + "] more item(s) to move after this", Logging.White);
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

                    int moveItemQuantity = Math.Min(AmmoHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    Logging.Log(WeAreInThisStateForLogs(), "Moving Item [" + AmmoHangarItem.TypeName + "] from AmmoHangar to CargoHold: We have [" + _itemsLeftToMoveQuantity + "] more item(s) to move after this", Logging.White);
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
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }

        private static bool MoveDronesToDroneBay(string MoveItemTypeName, ArmState StateToChangeToWhenDoneMoving, ArmState StateWeWereCalledFrom, bool _optional = false)
        {
            try
            {
                //
                // we assume useDrones is true if we got this far already.
                //

                if (string.IsNullOrEmpty(MoveItemTypeName))
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (string.IsNullOrEmpty(MoveItemTypeName))", Logging.Debug);
                    ChangeArmState(StateToChangeToWhenDoneMoving);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (ItemsAreBeingMoved)", Logging.Debug);
                    if (!WaitForLockedItems(StateWeWereCalledFrom)) return false;
                    return true;
                }

                if (Cache.Instance.ItemHangar == null) return false;
                if (Drones.DroneBay == null)
                {
                    return false;
                }
                
                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (Drones.DroneBay != null)", Logging.Debug);    
                
                if (Drones.DroneBay.Capacity == 0 && DroneBayRetries <= 10)
                {
                    DroneBayRetries++;
                    Logging.Log(WeAreInThisStateForLogs(), "Dronebay: not yet ready. Capacity [" + Drones.DroneBay.Capacity + "] UsedCapacity [" + Drones.DroneBay.UsedCapacity + "]", Logging.White);
                    Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(2);
                    return false;
                }

                if (!LookForItem(MoveItemTypeName, Drones.DroneBay))
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (!LookForItem(MoveItemTypeName, Drones.DroneBay))", Logging.Debug);
                    return false;
                }

                Logging.Log(WeAreInThisStateForLogs(), "Dronebay details: Capacity [" + Drones.DroneBay.Capacity + "] UsedCapacity [" + Drones.DroneBay.UsedCapacity + "]", Logging.White);
                if (Drones.DroneBay.Capacity == Drones.DroneBay.UsedCapacity)
                {
                    Logging.Log(WeAreInThisStateForLogs(), "Dronebay is Full. No need to move any more drones.", Logging.White);
                    ChangeArmState(StateToChangeToWhenDoneMoving);
                    return false;
                }

                if (DroneInvTypeItem != null)
                {
                    int neededDrones = (int)Math.Floor((Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity) / DroneInvTypeItem.Volume);
                    Logging.Log(WeAreInThisStateForLogs(), "neededDrones: [" + neededDrones + "]", Logging.White);

                    if ((int)neededDrones == 0)
                    {
                        Logging.Log(WeAreInThisStateForLogs(), "MoveItems", Logging.White);
                        ChangeArmState(ArmState.MoveBringItems);
                        return false;
                    }

                    if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar + WeHaveThisManyOfThoseItemsInAmmoHangar + WeHaveThisManyOfThoseItemsInLootHangar < neededDrones)
                    {
                        Logging.Log(WeAreInThisStateForLogs(), "ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] AmmoHangar has: [" + WeHaveThisManyOfThoseItemsInAmmoHangar + "] LootHangar has: [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + MoveItemTypeName + "] we need [" + neededDrones + "] drones to fill the DroneBay)", Logging.Red);
                        ItemsAreBeingMoved = false;
                        Cache.Instance.Paused = true;
                        ChangeArmState(ArmState.NotEnoughDrones);
                        return true;
                    }

                    //
                    // check the local cargo for items and subtract the items in the cargo from the quantity we still need to move to our cargohold
                    //
                    if (cargoItems.Any())
                    {
                        _itemsLeftToMoveQuantity = neededDrones;
                        foreach (DirectItem moveItemInCargo in cargoItems)
                        {
                            _itemsLeftToMoveQuantity -= moveItemInCargo.Stacksize;
                            if (_itemsLeftToMoveQuantity <= 0)
                            {
                                ChangeArmState(StateToChangeToWhenDoneMoving);
                                return true;
                            }

                            continue;
                        }
                    }

                    if (LootHangarItem != null && !string.IsNullOrEmpty(LootHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                    {
                        if (LootHangarItem.ItemId <= 0 || LootHangarItem.Volume == 0.00 || LootHangarItem.Quantity == 0)
                        {
                            return false;
                        }

                        int moveDroneQuantity = Math.Min(LootHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                        moveDroneQuantity = Math.Max(moveDroneQuantity, 1);
                        _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                        Logging.Log(WeAreInThisStateForLogs(), "Moving Item [" + LootHangarItem.TypeName + "] from LootHangar to DroneBay: We have [" + _itemsLeftToMoveQuantity + "] more item(s) to move after this", Logging.White);
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

                        int moveDroneQuantity = Math.Min(ItemHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                        moveDroneQuantity = Math.Max(moveDroneQuantity, 1);
                        _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                        Logging.Log(WeAreInThisStateForLogs(), "Moving Item [" + ItemHangarItem.TypeName + "] from ItemHangar to DroneBay: We have [" + _itemsLeftToMoveQuantity + "] more item(s) to move after this", Logging.White);
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

                        int moveDroneQuantity = Math.Min(AmmoHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                        moveDroneQuantity = Math.Max(moveDroneQuantity, 1);
                        _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                        Logging.Log(WeAreInThisStateForLogs(), "Moving Item [" + AmmoHangarItem.TypeName + "] from AmmoHangar to DroneBay: We have [" + _itemsLeftToMoveQuantity + "] more item(s) to move after this", Logging.White);
                        Drones.DroneBay.Add(AmmoHangarItem, moveDroneQuantity);
                        ItemsAreBeingMoved = true;
                        _lastArmAction = DateTime.UtcNow;
                        return false;
                    }

                    return true;
                }

                Logging.Log(WeAreInThisStateForLogs(), "droneTypeId is highly likely to be incorrect in your settings xml", Logging.Debug);
                return false;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Red);
                return false;
            }
        }

        private static bool FindDefaultFitting(string module)
        {
            try
            {
                DefaultFittingFound = false;
                if (!DefaultFittingChecked)
                {
                    if (Cache.Instance.FittingManagerWindow == null)
                    {
                        Logging.Log("FindDefaultFitting", "FittingManagerWindow is null", Logging.Debug);
                        return false;
                    }

                    //todo: fix this
                    if (MissionSettings.DefaultFitting == null)
                    {
                        MissionSettings.DefaultFittingName = MissionSettings.DefaultFitting.FittingName;
                        MissionSettings.FittingToLoad = MissionSettings.DefaultFittingName;
                    }

                    if (Logging.DebugFittingMgr) Logging.Log(module, "Character Settings XML says Default Fitting is [" + MissionSettings.DefaultFitting + "]", Logging.White);

                    if (Cache.Instance.FittingManagerWindow.Fittings.Any())
                    {
                        if (Logging.DebugFittingMgr) Logging.Log(module, "if (Cache.Instance.FittingManagerWindow.Fittings.Any())", Logging.Teal);
                        int i = 1;
                        foreach (DirectFitting fitting in Cache.Instance.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            if (Logging.DebugFittingMgr)
                            {
                                Logging.Log(module, "[" + i + "] Found a Fitting Named [" + fitting.Name + "]", Logging.Teal);
                            }

                            if (fitting.Name.ToLower().Equals(MissionSettings.DefaultFittingName.ToLower()))
                            {
                                DefaultFittingChecked = true;
                                DefaultFittingFound = true;
                                Logging.Log(module, "[" + i + "] Found Default Fitting [" + fitting.Name + "]", Logging.White);
                                return true;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Logging.Log("Arm.LoadFitting", "No Fittings found in the Fitting Manager at all!  Disabling fitting manager.", Logging.Orange);
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        return true;
                    }

                    if (!DefaultFittingFound)
                    {
                        Logging.Log("Arm.LoadFitting", "Error! Could not find Default Fitting [" + MissionSettings.DefaultFittingName.ToLower() + "].  Disabling fitting manager.", Logging.Orange);
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        Settings.Instance.UseFittingManager = false;
                        Logging.Log("Arm.LoadFitting", "Closing Fitting Manager", Logging.White);
                        Cache.Instance.FittingManagerWindow.Close();

                        ChangeArmState(ArmState.MoveBringItems);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Red);
                return false;
            }
        }

        public static bool ChangeArmState(ArmState _ArmStateToSet, bool WaitAMomentbeforeNextAction = false, string LogMessage = null)
        {
            try
            {
                //
                // if _ArmStateToSet matches also do this stuff...
                //
                switch (_ArmStateToSet)
                {
                    case ArmState.OpenShipHangar:
                        _States.CurrentCombatState = CombatState.Idle;
                        break;
                        
                    case ArmState.NotEnoughAmmo:
                        if (LogMessage != null) Logging.Log(_States.CurrentArmState.ToString(), LogMessage, Logging.Red);
                        Cache.Instance.Paused = true;
                        _States.CurrentCombatState = CombatState.Idle;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Red);
                return false;
            }

            try
            {
                Arm.ClearDataBetweenStates();
                if (_States.CurrentArmState != _ArmStateToSet)
                {
                    _States.CurrentArmState = _ArmStateToSet;
                    if (WaitAMomentbeforeNextAction) _lastArmAction = DateTime.UtcNow;
                    else Arm.ProcessState();
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Red);
                return false;
            }
        }

        private static string WeAreInThisStateForLogs()
        {
            return _States.CurrentCombatMissionBehaviorState.ToString() + "." + _States.CurrentArmState.ToString();
        }
        private static bool BeginArmState()
        {
            try
            {
                Time.Instance.LastReloadAttemptTimeStamp = new Dictionary<long, DateTime>();
                Time.Instance.LastReloadedTimeStamp = new Dictionary<long, DateTime>();
                //_ammoTypesToLoad.Clear();
                switchingShips = false;
                UseMissionShip = false;          // Were we successful in activating the mission specific ship?
                DefaultFittingChecked = false;   //flag to check for the correct default fitting before using the fitting manager
                DefaultFittingFound = false;      //Did we find the default fitting?
                CustomFittingFound = false;
                ItemsAreBeingMoved = false;
                SwitchShipsOnly = false;
                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Cache.Instance.BringOptionalMissionItemQuantity is [" + MissionSettings.BringOptionalMissionItemQuantity + "]", Logging.Debug);
                ItemHangarRetries = 0;
                DroneBayRetries = 0;
                RefreshMissionItems(AgentInteraction.AgentId);
                _States.CurrentCombatState = CombatState.Idle;

                if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
                {
                    ChangeArmState(ArmState.ActivateSalvageShip);
                    return true;
                }

                ChangeArmState(ArmState.ActivateCombatShip);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }

        private static bool StackAmmoHangarArmState()
        {
            if (!Cache.Instance.StackAmmoHangar(WeAreInThisStateForLogs())) return false;
            ChangeArmState(ArmState.Done);
            return true;
        }

        private static bool WaitForLockedItems(ArmState _armStateToSwitchTo)
        {
            try
            {
                if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
                {
                    if (Math.Abs(DateTime.UtcNow.Subtract(_lastArmAction).TotalSeconds) > 15)
                    {
                        Logging.Log(WeAreInThisStateForLogs(), "Moving Ammo timed out, clearing item locks", Logging.Orange);
                        Cache.Instance.DirectEve.UnlockItems();
                        _lastArmAction = DateTime.UtcNow.AddSeconds(-1);
                        return false;
                    }

                    if (Logging.DebugUnloadLoot) Logging.Log(WeAreInThisStateForLogs(), "Waiting for Locks to clear. GetLockedItems().Count [" + Cache.Instance.DirectEve.GetLockedItems().Count + "]", Logging.Teal);
                    return false;
                }

                _lastArmAction = DateTime.UtcNow.AddSeconds(-1);
                Logging.Log(WeAreInThisStateForLogs(), "Done", Logging.White);
                ItemsAreBeingMoved = false;
                ChangeArmState(_armStateToSwitchTo);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(_States.CurrentArmState.ToString(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }
        private static bool ActivateThisShip(string ShipNameToActivate)
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(Cache.Instance.RandomNumber(2000, 3000))) return false;

                //
                // have we attempted to switch ships already (and are waiting for it to take effect)
                //
                if (switchingShips)
                {
                    if (Cache.Instance.DirectEve.ActiveShip != null && Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == ShipNameToActivate.ToLower())
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
                if (Cache.Instance.DirectEve.ActiveShip != null && Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == ShipNameToActivate.ToLower())
                {
                    switchingShips = false;
                    return true;
                }

                //
                // Check and warn the use if their config is hosed.
                //
                if (string.IsNullOrEmpty(Combat.CombatShipName) || string.IsNullOrEmpty(Settings.Instance.SalvageShipName))
                {
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false, "CombatShipName and SalvageShipName both have to be populated! Fix your characters config.")) return false;
                    return false;
                }

                if (Combat.CombatShipName == Settings.Instance.SalvageShipName)
                {
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false, "CombatShipName and SalvageShipName cannot be the same ship/shipname ffs! Fix your characters config.")) return false;
                    return false;
                }

                //
                // we have the mining shipname configured but it is not the current ship
                // 
                if (!string.IsNullOrEmpty(ShipNameToActivate))
                {
                    if (Cache.Instance.ShipHangar == null) return false;

                    List<DirectItem> shipsInShipHangar = Cache.Instance.ShipHangar.Items;
                    if (shipsInShipHangar.Any(s => s.GivenName != null && s.GivenName.ToLower() == ShipNameToActivate.ToLower()))
                    {
                        if (!Cache.Instance.CloseCargoHold(_States.CurrentArmState.ToString())) return false;
                        DirectItem ship = shipsInShipHangar.FirstOrDefault(s => s.GivenName != null && s.GivenName.ToLower() == ShipNameToActivate.ToLower());
                        if (ship != null)
                        {
                            Logging.Log(WeAreInThisStateForLogs(), "Making [" + ship.GivenName + "] active", Logging.White);
                            ship.ActivateShip();
                            switchingShips = true;
                            _lastArmAction = DateTime.UtcNow;
                            return false;
                        }

                        return false;
                    }

                    if (Cache.Instance.ShipHangar.Items.Any())
                    {
                        Logging.Log(WeAreInThisStateForLogs(), "Found the following ships:", Logging.White);
                        foreach (DirectItem shipInShipHangar in Cache.Instance.ShipHangar.Items)
                        {
                            Logging.Log(WeAreInThisStateForLogs(), "GivenName [" + shipInShipHangar.GivenName.ToLower() + "] TypeName[" + shipInShipHangar.TypeName + "]", Logging.White);
                        }

                        if (!ChangeArmState(ArmState.NotEnoughAmmo, false, "Missing Ship with GivenName [" + ShipNameToActivate.ToLower() + "] in ShipHangar")) return false;
                        return false;
                    }

                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false, "No ships found in ShipHangar!")) return false;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }   
        }

        private static bool ActivateMiningShipArmState()
        {
            if (string.IsNullOrEmpty(Settings.Instance.MiningShipName))
            {
                if (!ChangeArmState(ArmState.NotEnoughAmmo, false, "Could not find miningShipName in settings!")) return false;
                return false;
            }

            if (!ActivateThisShip(Settings.Instance.MiningShipName)) return false;
            return true;
        }

        private static bool ActivateNoobShipArmState()
        {
            if (DateTime.UtcNow < _lastArmAction.AddSeconds(Time.Instance.SwitchShipsDelay_seconds)) return false;
            
            if (Cache.Instance.ActiveShip.GroupId != (int)Group.RookieShip &&
                Cache.Instance.ActiveShip.GroupId != (int)Group.Shuttle)
            {
                if (Cache.Instance.ShipHangar == null) return false;

                List<DirectItem> ships = Cache.Instance.ShipHangar.Items;
                foreach (DirectItem ship in ships.Where(ship => ship.GivenName != null && ship.GroupId == (int)Group.RookieShip || ship.GroupId == (int)Group.Shuttle))
                {
                    if (!Cache.Instance.CloseCargoHold(WeAreInThisStateForLogs())) return false;
                    Logging.Log(WeAreInThisStateForLogs(), "Making [" + ship.GivenName + "] active", Logging.White);
                    ship.ActivateShip();
                    _lastArmAction = DateTime.UtcNow;
                    return true;
                }

                if (!ChangeArmState(ArmState.NotEnoughAmmo, false, "No rookie ships or shuttles to use")) return false;
                return false;
            }

            if (Cache.Instance.ActiveShip.GroupId == (int)Group.RookieShip ||
                Cache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
            {
                if (!ChangeArmState(ArmState.Cleanup, false, "Done")) return false;
                return true;
            }

            return true;
        }

        private static bool ActivateTransportShipArmState()
        {
            if (string.IsNullOrEmpty(Settings.Instance.TransportShipName))
            {
                Logging.Log(WeAreInThisStateForLogs(), "Could not find transportshipName in settings!", Logging.Orange);
                ChangeArmState(ArmState.NotEnoughAmmo);
                return false;
            }

            if (!ActivateThisShip(Settings.Instance.TransportShipName)) return false;
            
            Logging.Log(WeAreInThisStateForLogs(), "Done", Logging.White);
            ChangeArmState(ArmState.Cleanup);
            return true;
        }

        private static bool ActivateSalvageShipArmState()
        {
            try
            {
                if (string.IsNullOrEmpty(Settings.Instance.SalvageShipName))
                {
                    Logging.Log(WeAreInThisStateForLogs(), "Could not find salvageshipName: " + Settings.Instance.SalvageShipName + " in settings!", Logging.Orange);
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                if (!ActivateThisShip(Settings.Instance.SalvageShipName)) return false;

                Logging.Log(WeAreInThisStateForLogs(), "Done", Logging.White);
                ChangeArmState(ArmState.Cleanup);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }

        private static bool ActivateCombatShipArmState()
        {
            try
            {
                if (string.IsNullOrEmpty(Combat.CombatShipName))
                {
                    Logging.Log(WeAreInThisStateForLogs(), "Could not find CombatShipName: " + Combat.CombatShipName + " in settings!", Logging.Orange);
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                if (!ActivateThisShip(Combat.CombatShipName)) return false;

                if (SwitchShipsOnly) ChangeArmState(ArmState.Done, true);
                ChangeArmState(ArmState.RepairShop, true);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(),"Exception [" + ex + "]",Logging.Debug);
                return false;
            }
        }

        private static bool RepairShopArmState()
        {
            try
            {
                if (Cache.Instance.DirectEve.HasSupportInstances() && Panic.UseStationRepair && Arm.NeedRepair)
                {
                    if (!Cache.Instance.RepairItems(WeAreInThisStateForLogs())) return false; //attempt to use repair facilities if avail in station
                }

                if (_States.CurrentSwitchShipState == SwitchShipState.ActivateCombatShip)
                {
                    ChangeArmState(ArmState.Done, true);
                    _States.CurrentSwitchShipState = SwitchShipState.Done;
                    return true;
                }

                ChangeArmState(ArmState.LoadSavedFitting, true);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }

        private static bool LoadSavedFittingArmState()
        {
            try
            {
                if (Settings.Instance.UseFittingManager)
                {
                    //If we are already loading a fitting...
                    if (ItemsAreBeingMoved)
                    {
                        if (!WaitForLockedItems(ArmState.MoveDrones)) return false;
                        return true;
                    }

                    if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior) //|| _States.CurrentQuestorState == QuestorState.BackgroundBehavior)
                    {
                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)", Logging.Teal);

                        if (!FindDefaultFitting(WeAreInThisStateForLogs())) return false;

                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "These are the reasons we would use or not use the fitting manager.(below)", Logging.Teal);
                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "DefaultFittingFound [" + DefaultFittingFound + "]", Logging.Teal);
                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "UseMissionShip [" + UseMissionShip + "]", Logging.Teal);
                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "Cache.Instance.ChangeMissionShipFittings [" + MissionSettings.ChangeMissionShipFittings + "]", Logging.Teal);
                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "if ((!Settings.Instance.UseFittingManager || !DefaultFittingFound) || (UseMissionShip && !Cache.Instance.ChangeMissionShipFittings)) then do not use fitting manager", Logging.Teal);
                        if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "These are the reasons we would use or not use the fitting manager.(above)", Logging.Teal);

                        if ((!DefaultFittingFound) || (UseMissionShip && !MissionSettings.ChangeMissionShipFittings))
                        {
                            if (Logging.DebugFittingMgr) Logging.Log(WeAreInThisStateForLogs(), "if ((!Settings.Instance.UseFittingManager || !DefaultFittingFound) || (UseMissionShip && !Cache.Instance.ChangeMissionShipFittings))", Logging.Teal);
                            ChangeArmState(ArmState.MoveDrones, true);
                            return false;
                        }

                        //let's check first if we need to change fitting at all
                        Logging.Log(WeAreInThisStateForLogs(), "Fitting: " + MissionSettings.FittingToLoad + " - currentFit: " + MissionSettings.CurrentFit, Logging.White);
                        if (MissionSettings.FittingToLoad.Equals(MissionSettings.CurrentFit))
                        {
                            Logging.Log(WeAreInThisStateForLogs(), "Current fit is now correct", Logging.White);
                            ChangeArmState(ArmState.MoveDrones, true);
                            return true;
                        }

                        if (Cache.Instance.FittingManagerWindow == null) return false;

                        Logging.Log(WeAreInThisStateForLogs(), "Looking for saved fitting named: [" + MissionSettings.FittingToLoad + " ]", Logging.White);

                        foreach (DirectFitting fitting in Cache.Instance.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            DirectActiveShip CurrentShip = Cache.Instance.ActiveShip;
                            if (MissionSettings.FittingToLoad.ToLower().Equals(fitting.Name.ToLower()) && fitting.ShipTypeId == CurrentShip.TypeId)
                            {
                                Time.Instance.NextArmAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                                Logging.Log(WeAreInThisStateForLogs(), "Found saved fitting named: [ " + fitting.Name + " ][" + Math.Round(Time.Instance.NextArmAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);

                                //switch to the requested fitting for the current mission
                                fitting.Fit();
                                _lastArmAction = DateTime.UtcNow;
                                ItemsAreBeingMoved = true;
                                MissionSettings.CurrentFit = fitting.Name;
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
                                Logging.Log(WeAreInThisStateForLogs(), "Could not find fitting for this ship typeid.  Using current fitting.", Logging.Orange);
                                ChangeArmState(ArmState.MoveDrones, true);
                                return false;
                            }

                            Logging.Log(WeAreInThisStateForLogs(), "Could not find fitting - switching to default", Logging.Orange);
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
                Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }

        private static bool MoveDrones()
        {
            try
            {
                if (!Drones.UseDrones ||
                    (Cache.Instance.ActiveShip.GroupId == (int)Group.Shuttle ||
                     Cache.Instance.ActiveShip.GroupId == (int)Group.Industrial ||
                     Cache.Instance.ActiveShip.GroupId == (int)Group.TransportShip ||
                     Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName))
                {
                    ChangeArmState(ArmState.MoveBringItems);
                    return false;
                }

                if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior
                    //_States.CurrentQuestorState == QuestorState.BackgroundBehavior 
                   )
                {
                    Logging.Log(WeAreInThisStateForLogs(), "Skipping loading drones for this Questor Behavior", Logging.Orange);
                    ChangeArmState(ArmState.MoveBringItems);
                    return false;
                }

                if (MoveDronesToDroneBay(DroneInvTypeItem.TypeName, ArmState.MoveBringItems, ArmState.MoveDrones, false)) return false;
                
                return false;
            }
            catch (Exception ex)
            {
                Logging.Log(WeAreInThisStateForLogs(),"Exception [" + ex + "]",Logging.Debug);
                return false;
            }
        }

        private static bool MoveBringItems()
        {
            if (!MoveItemsToCargo(MissionSettings.BringMissionItem, MissionSettings.BringMissionItemQuantity, ArmState.MoveOptionalBringItems, ArmState.MoveBringItems, false)) return false;
            return false;
        }

        private static bool MoveOptionalBringItems()
        {
            if (!MoveItemsToCargo(MissionSettings.BringOptionalMissionItem, MissionSettings.BringOptionalMissionItemQuantity, ArmState.MoveCapBoosters, ArmState.MoveOptionalBringItems, true)) return false;
            return false;
        }

        private static bool MoveCapBoosters()
        {
            if (Cache.Instance.ActiveShip.GroupId == (int)Group.Shuttle ||
                 Cache.Instance.ActiveShip.GroupId == (int)Group.Industrial ||
                 Cache.Instance.ActiveShip.GroupId == (int)Group.TransportShip ||
                 Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)
            {
                ChangeArmState(ArmState.MoveAmmo);
                return false;
            }

            if (Combat.WeaponGroupId == 53) //civilian guns of all types
            {
                Logging.Log("Arm.MoveItems", "No ammo needed for civilian guns: done", Logging.White);
                ChangeArmState(ArmState.MoveAmmo);
                return false;
            }

            //
            // load cap boosters
            //
            #region load cap boosters
            DirectInvType _CapBoosterInvTypeItem = null;
            Cache.Instance.DirectEve.InvTypes.TryGetValue(Settings.Instance.CapacitorInjectorScript, out _CapBoosterInvTypeItem);
            if (ArmLoadCapBoosters && _CapBoosterInvTypeItem != null)
            {
                if (!MoveItemsToCargo(_CapBoosterInvTypeItem.TypeName, Settings.Instance.NumberOfCapBoostersToLoad, ArmState.MoveCapBoosters, ArmState.MoveAmmo)) return false;
            }

            ChangeArmState(ArmState.MoveAmmo, true);
            return false;

            #endregion move cap boosters
        }
        private static bool MoveAmmo()
        {
            try
            {

                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(Cache.Instance.RandomNumber(2000, 3000)))
                {
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (DateTime.UtcNow < Cache.Instance.NextArmAction)) return;", Logging.Teal);
                    return false;
                }

                if (Cache.Instance.ActiveShip.GroupId == (int)Group.Shuttle ||
                     Cache.Instance.ActiveShip.GroupId == (int)Group.Industrial ||
                     Cache.Instance.ActiveShip.GroupId == (int)Group.TransportShip ||
                     Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)
                {
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                if (Combat.WeaponGroupId == 53) //civilian guns of all types
                {
                    Logging.Log(WeAreInThisStateForLogs(), "No ammo needed for civilian guns: done", Logging.White);
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                //
                // load ammo
                //
                #region load ammo

                if (ItemsAreBeingMoved)
                {
                    if (!WaitForLockedItems(ArmState.MoveAmmo)) return false;
                    return true;
                }

                //
                // make sure we actually have something in the list of AmmoToLoad before trying to load ammo.
                //

                Ammo CurrentAmmoToLoad = MissionSettings.AmmoTypesToLoad.FirstOrDefault();
                if (CurrentAmmoToLoad == null)
                {
                    if (Logging.DebugArm) Logging.Log("Arm.MoveAmmo", "We have no more ammo types to be loaded. We have to be finished with arm.", Logging.Debug);
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }
                
                try
                {
                    AmmoHangarItems = null;
                    IEnumerable<DirectItem> AmmoItems = null;
                    if (Cache.Instance.AmmoHangar != null && Cache.Instance.AmmoHangar.Items.Any())
                    {
                        AmmoHangarItems = Cache.Instance.AmmoHangar.Items.Where(i => i.TypeId == CurrentAmmoToLoad.TypeId).OrderBy(i => !i.IsSingleton).ThenByDescending(i => i.Quantity).ToList();
                        AmmoItems = AmmoHangarItems.ToList();
                    }

                    if (Logging.DebugArm) Logging.Log("Arm.MoveAmmo", "Ammohangar has [" + AmmoHangarItems.Count() + "] items with the right typeID [" + CurrentAmmoToLoad.TypeId + "] for this ammoType. MoveAmmo will use AmmoHangar", Logging.Debug);
                    if (!AmmoHangarItems.Any())
                    {
                        ItemHangarRetries++;
                        if (ItemHangarRetries < 10)
                        {
                            //just retry... after 10 tries try to use the itemhangar instead of ammohangar
                            return false;
                        }

                        foreach (Ammo ammo in MissionSettings.AmmoTypesToLoad)
                        {
                            Logging.Log("Arm", "Ammohangar was Missing [" + ammo.Quantity + "] units of ammo: [ " + ammo.Description + " ] with TypeId [" + ammo.TypeId + "] trying item hangar next", Logging.Orange);
                        }

                        try
                        {
                            ItemHangarItems = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == CurrentAmmoToLoad.TypeId).OrderBy(i => !i.IsSingleton).ThenByDescending(i => i.Quantity);
                            AmmoItems = ItemHangarItems;
                            if (Logging.DebugArm)
                            {
                                Logging.Log("Arm", "Itemhangar has [" + ItemHangarItems.Count() + "] items with the right typeID [" + CurrentAmmoToLoad.TypeId + "] for this ammoType. MoveAmmo will use ItemHangar", Logging.Debug);
                                //foreach (DirectItem item in AmmoItems)
                                //{
                                //
                                //}
                            }
                            if (!ItemHangarItems.Any())
                            {
                                ItemHangarRetries++;
                                if (ItemHangarRetries < 10)
                                {
                                    //just retry... after 10 tries fail and let the user know we are out of ammo
                                    return false;
                                }

                                foreach (Ammo ammo in MissionSettings.AmmoTypesToLoad)
                                {
                                    Logging.Log("Arm", "Itemhangar was Missing [" + ammo.Quantity + "] units of ammo: [ " + ammo.Description + " ] with TypeId [" + ammo.TypeId + "]", Logging.Orange);
                                }

                                ChangeArmState(ArmState.NotEnoughAmmo);
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            Logging.Log("Arm.MoveItems", "Itemhangar Exception [" + exception + "]", Logging.Debug);
                        }
                    }

                    try
                    {
                        int itemnum = 0;
                        
                        if (AmmoItems != null)
                        {
                            AmmoItems = AmmoItems.ToList();
                            if (AmmoItems.Any())
                            {
                                foreach (DirectItem item in AmmoItems)
                                {
                                    itemnum++;
                                    int moveAmmoQuantity = Math.Min(item.Stacksize, CurrentAmmoToLoad.Quantity);
                                    moveAmmoQuantity = Math.Max(moveAmmoQuantity, 1);
                                    if (Logging.DebugArm) Logging.Log("Arm.MoveAmmo", "In Hangar we have: [" + itemnum + "] TypeName [" + item.TypeName + "] StackSize [" + item.Stacksize + "] - CurrentAmmoToLoad.Quantity [" + CurrentAmmoToLoad.Quantity + "] Actual moveAmmoQuantity [" + moveAmmoQuantity + "]", Logging.White);

                                    if ((moveAmmoQuantity <= item.Stacksize) || moveAmmoQuantity == 1)
                                    {
                                        Logging.Log("Arm.MoveAmmo", "Moving [" + moveAmmoQuantity + "] units of Ammo  [" + item.TypeName + "] from [ AmmoHangar ] to CargoHold", Logging.White);
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
                                        if (CurrentAmmoToLoad.Quantity == 0)
                                        {
                                            //
                                            // if we have moved all the ammo of this type that needs to be moved remove this type of ammo from the list of ammos that need to be moved
                                            // 
                                            MissionSettings.AmmoTypesToLoad.RemoveAll(a => a.TypeId == CurrentAmmoToLoad.TypeId);
                                            return false;
                                        }
                                    }
                                    else
                                    {
                                        Logging.Log("Arm.MoveAmmo", "While calculating what to move we wanted to move [" + moveAmmoQuantity + "] units of Ammo  [" + item.TypeName + "] from [ AmmoHangar ] to CargoHold, but somehow the current Item Stacksize is only [" + item.Stacksize + "]", Logging.White);
                                        continue;
                                    }

                                    return false; //you can only move one set of items per frame.
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Logging.Log("Arm.MoveAmmo", "AmmoItems Exception [" + exception + "]", Logging.Debug);
                    }
                }
                catch (Exception exception)
                {
                    Logging.Log("Arm.MoveAmmo", "Error while processing Itemhangar Items exception was: [" + exception + "]", Logging.Debug);
                }

                if (MissionSettings.AmmoTypesToLoad.Any()) //if we still have any ammo to load here then we must be missing ammo
                {
                    foreach (Ammo ammo in MissionSettings.AmmoTypesToLoad)
                    {
                        Logging.Log("Arm.MoveAmmo", "Missing [" + ammo.Quantity + "] units of ammo: [ " + ammo.Description + " ] with TypeId [" + ammo.TypeId + "]", Logging.Orange);
                    }

                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                _lastArmAction = DateTime.UtcNow;
                ChangeArmState(ArmState.StackAmmoHangar);
                return false;

                #endregion move ammo
            }
            catch (Exception ex)
            {
                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Exception [" + ex + "]", Logging.Teal);
                return false;
            }
        }
        
        private static bool MoveMiningCrystals()
        {
            if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(2000))
            {
                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (DateTime.UtcNow < Cache.Instance.NextArmAction)) return;", Logging.Teal);
                return false;
            }

            if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), " start if (!Cache.Instance.CloseFittingManager(Arm)) return;", Logging.Teal);
            if (!Cache.Instance.CloseFittingManager(WeAreInThisStateForLogs())) return false;
            if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), " finish if (!Cache.Instance.CloseFittingManager(Arm)) return;", Logging.Teal);

            //
            // Check for locked items if we are already moving items
            //
            #region check for item locks

            if (ItemsAreBeingMoved)
            {
                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "if (ItemsAreBeingMoved)", Logging.Teal);

                if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
                {
                    if (DateTime.UtcNow.Subtract(_lastArmAction).TotalSeconds > 45)
                    {
                        Logging.Log(WeAreInThisStateForLogs(), "Moving Items timed out, clearing item locks", Logging.Orange);
                        Cache.Instance.DirectEve.UnlockItems();
                        _lastArmAction = DateTime.UtcNow;
                        ChangeArmState(ArmState.Begin);
                        return false;
                    }

                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Waiting for Locks to clear. GetLockedItems().Count [" + Cache.Instance.DirectEve.GetLockedItems().Count + "]", Logging.Teal);
                    return false;
                }
                ItemsAreBeingMoved = false;
                return false;
            }
            #endregion check for item locks

            //
            // load mining crystals
            //
            #region load mining crystals

            //if (Cache.Instance.Modules.Count(i => i.IsTurret && i.MaxCharges == 0) > 0) //civilian guns of all types
            //{
            //    Logging.Log("Arm.MoveItems", "No ammo needed for civilian guns: done", Logging.White);
            //    _States.CurrentArmState = ArmState.Cleanup;
            //    return false;
            //}

            //
            // make sure we actually have something in the list of AmmoToLoad before trying to load ammo.
            //
            MiningCrystals CurrentMiningCrystalsToLoad = CrystalsToLoad.FirstOrDefault();
            if (CurrentMiningCrystalsToLoad == null)
            {
                //
                // if we have no more ammo types to be loaded we have to be finished with arm.
                //
                Logging.Log(WeAreInThisStateForLogs(), "if (CurrentMiningCrystalsToLoad == null)", Logging.Debug);
                ChangeArmState(ArmState.Cleanup);
                return false;
            }

            try
            {
                AmmoHangarItems = null;
                AmmoHangarItems = Cache.Instance.AmmoHangar.Items.Where(i => i.TypeId == CurrentMiningCrystalsToLoad.TypeId).OrderBy(i => i.IsSingleton).ThenBy(i => i.Quantity);
                        
                if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Ammohangar has [" + AmmoHangarItems.Count() + "] items with the right typeID [" + CurrentMiningCrystalsToLoad.TypeId + "] for this ammoType. MoveAmmo will use AmmoHangar", Logging.Debug);
                if (!AmmoHangarItems.Any())
                {
                    ItemHangarItem = null;
                    ItemHangarItems = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == CurrentMiningCrystalsToLoad.TypeId).OrderBy(i => i.IsSingleton).ThenBy(i => i.Quantity);
                    if (Logging.DebugArm) Logging.Log(WeAreInThisStateForLogs(), "Itemhangar has [" + ItemHangarItems.Count() + "] items with the right typeID [" + CurrentMiningCrystalsToLoad.TypeId + "] for this ammoType. MoveAmmo will use ItemHangar", Logging.Debug);
                    if (!ItemHangarItems.Any())
                    {
                        Logging.Log(WeAreInThisStateForLogs(), "if (!ItemHangarItems.Any())", Logging.Debug);
                        foreach (MiningCrystals _miningCrystal in CrystalsToLoad)
                        {
                            Logging.Log(WeAreInThisStateForLogs(), "Missing [" + _miningCrystal.Quantity + "] units of ammo: [ " + _miningCrystal.Description + " ] with TypeId [" + _miningCrystal.TypeId + "]", Logging.Orange);
                        }

                        ChangeArmState(ArmState.NotEnoughAmmo);
                        return false;
                    }
                }

                foreach (DirectItem item in AmmoHangarItems)
                {
                    int moveMiningCrystalsQuantity = Math.Min(item.Stacksize, CurrentMiningCrystalsToLoad.Quantity);
                    moveMiningCrystalsQuantity = Math.Max(moveMiningCrystalsQuantity, 1);
                    Logging.Log(WeAreInThisStateForLogs(), "Moving [" + moveMiningCrystalsQuantity + "] units of Mining Crystals  [" + item.TypeName + "] from [ AmmoHangar ] to CargoHold", Logging.White);
                    //
                    // move items to cargo
                    //
                    Cache.Instance.CurrentShipsCargo.Add(item, moveMiningCrystalsQuantity);
                    //
                    // subtract the moved items from the items that need to be moved
                    //
                    CurrentMiningCrystalsToLoad.Quantity -= moveMiningCrystalsQuantity;
                    if (CurrentMiningCrystalsToLoad.Quantity == 0)
                    {
                        //
                        // if we have moved all the ammo of this type that needs to be moved remove this type of ammo from the list of ammos that need to be moved
                        // 
                        MissionSettings.AmmoTypesToLoad.RemoveAll(a => a.TypeId == CurrentMiningCrystalsToLoad.TypeId);
                        CrystalsToLoad.RemoveAll(a => a.TypeId == CurrentMiningCrystalsToLoad.TypeId);
                        return false;
                    }

                    return false; //you can only move one set of items per frame.
                }
            }
            catch (Exception exception)
            {
                Logging.Log(WeAreInThisStateForLogs(), "Error while processing Itemhangar Items exception was: [" + exception + "]", Logging.Debug);
            }

            if (CrystalsToLoad.Any()) //if we still have any ammo to load here then we must be missing ammo
            {
                foreach (MiningCrystals _miningCrystal in CrystalsToLoad)
                {
                    Logging.Log(WeAreInThisStateForLogs(), "Missing [" + _miningCrystal.Quantity + "] units of ammo: [ " + _miningCrystal.Description + " ] with TypeId [" + _miningCrystal.TypeId + "]", Logging.Orange);
                }

                ChangeArmState(ArmState.NotEnoughAmmo);
                return false;
            }

            _lastArmAction = DateTime.UtcNow;
            Logging.Log(WeAreInThisStateForLogs(), "Waiting for items", Logging.White);
            //_States.CurrentArmState = ArmState.WaitForItems;
            return false;

            #endregion move ammo
        }

        private static bool CleanupArmState()
        {
            if (Drones.UseDrones && (Cache.Instance.ActiveShip.GroupId != (int)Group.Shuttle && Cache.Instance.ActiveShip.GroupId != (int)Group.Industrial && Cache.Instance.ActiveShip.GroupId != (int)Group.TransportShip))
            {
                // Close the drone bay, its not required in space.
                if (!Drones.CloseDroneBayWindow(WeAreInThisStateForLogs())) return false;
            }

            if (Settings.Instance.UseFittingManager)
            {
                if (!Cache.Instance.CloseFittingManager(WeAreInThisStateForLogs())) return false;
            }

            //if (!Cleanup.CloseInventoryWindows()) return false;
            _States.CurrentArmState = ArmState.Done;
            return false;
        }


        public static void ProcessState()
        {
            try
            {
                if (!Cache.Instance.InStation)
                    return;

                if (Cache.Instance.InSpace)
                    return;

                if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(10)) // we wait 10 seconds after we last thought we were in space before trying to do anything in station
                    return;

                switch (_States.CurrentArmState)
                {
                    case ArmState.Idle:
                        break;

                    case ArmState.Begin:
                        if (!BeginArmState()) break;
                        break;

                    case ArmState.ActivateCombatShip:
                        if (!ActivateCombatShipArmState()) return;
                        break;

                    case ArmState.RepairShop:
                        if (!RepairShopArmState()) return;
                        break;

                    case ArmState.LoadSavedFitting:
                        if (!LoadSavedFittingArmState()) return;
                        break;

                    case ArmState.MoveDrones:
                        if (!MoveDrones()) return;
                        break;

                    case ArmState.MoveBringItems:
                        if (!MoveBringItems()) return;
                        break;

                    case ArmState.MoveOptionalBringItems:
                        if (!MoveOptionalBringItems()) return;
                        break;

                    case ArmState.MoveCapBoosters:
                        if (!MoveCapBoosters()) return;
                        break;

                    case ArmState.MoveAmmo:
                        if (!MoveAmmo()) return;
                        break;

                    case ArmState.MoveMiningCrystals:
                        if (!MoveMiningCrystals()) return;
                        break;

                    case ArmState.StackAmmoHangar:
                        if (!StackAmmoHangarArmState()) return;
                        break;

                    case ArmState.Cleanup:
                        if (!CleanupArmState()) return;
                        break;

                    case ArmState.Done:
                        break;

                    case ArmState.ActivateMiningShip:
                        if (!ActivateMiningShipArmState()) return;
                        break;

                    case ArmState.ActivateNoobShip:
                        if (!ActivateNoobShipArmState()) return;
                        break;

                    case ArmState.ActivateTransportShip:
                        if (!ActivateTransportShipArmState()) return;
                        break;

                    case ArmState.ActivateSalvageShip:
                        if (!ActivateSalvageShipArmState()) return;
                        break;

                    case ArmState.NotEnoughDrones: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;

                    case ArmState.NotEnoughAmmo:   //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;

                }
            }
            catch (Exception ex)
            {
                Logging.Log("Arm.ProcessState","Exception [" + ex + "]",Logging.Debug);
                return;
            }
        }

        ///
        ///   Invalidate the cached items every pulse (called from cache.invalidatecache, which itself is called every frame in questor.cs)
        /// 
        public static void ClearDataBetweenStates()
        {
            try
            {
                //
                // this list of variables is cleared every pulse.
                //
                _itemsLeftToMoveQuantity = 0;                
            }
            catch (Exception exception)
            {
                Logging.Log("Arm.InvalidateCache", "Exception [" + exception + "]", Logging.Debug);
            }
        }

        ///
        ///   Invalidate the cached items every pulse (called from cache.invalidatecache, which itself is called every frame in questor.cs)
        /// 
        public static void InvalidateCache()
        {
            try
            {
                //
                // this list of variables is cleared every pulse.
                //
                cargoItems = null;
                ItemHangarItem = null;
                ItemHangarItems = null;
            }
            catch (Exception exception)
            {
                Logging.Log("Arm.InvalidateCache", "Exception [" + exception + "]", Logging.Debug);
            }
        }

        /*
                private void WhatScriptsShouldILoad()
                {
                    TrackingComputerScriptsToLoad = 0;
                    TrackingDisruptorScriptsToLoad = 0;
                    TrackingLinkScriptsToLoad = 0;
                    SensorBoosterScriptsToLoad = 0;
                    SensorDampenerScriptsToLoad = 0;
                    foreach (ModuleCache module in Cache.Instance.Modules)
                    {
                        if (module.GroupId == (int)Group.TrackingDisruptor ||
                            module.GroupId == (int)Group.TrackingComputer ||
                            module.GroupId == (int)Group.TrackingLink ||
                            module.GroupId == (int)Group.SensorBooster ||
                            module.GroupId == (int)Group.SensorDampener)
                        {
                            if (module.CurrentCharges == 0)
                            {
                                DirectItem scriptToLoad;
                                if (module.GroupId == (int)Group.TrackingDisruptor)
                                {
                                    scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.TrackingDisruptorScript, 1);
                                    if (scriptToLoad !=null)
                                    {
                                        TrackingDisruptorScriptsToLoad++;
                                    }
                                }
                                if (module.GroupId == (int)Group.TrackingComputer)
                                {
                                    scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.TrackingComputerScript, 1);
                                    if (scriptToLoad != null)
                                    {
                                        TrackingComputerScriptsToLoad++;
                                    }
                                }
                                if (module.GroupId == (int)Group.TrackingLink)
                                {
                                    scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.TrackingLinkScript, 1);
                                    if (scriptToLoad != null)
                                    {
                                        TrackingLinkScriptsToLoad++;
                                    }
                                }
                                if (module.GroupId == (int)Group.SensorBooster)
                                {
                                    scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.SensorBoosterScript, 1);
                                    if (scriptToLoad != null)
                                    {
                                        SensorBoosterScriptsToLoad++;
                                    }
                                }
                                if (module.GroupId == (int)Group.SensorDampener)
                                {
                                    scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.SensorDampenerScript, 1);
                                    if (scriptToLoad != null)
                                    {
                                        SensorDampenerScriptsToLoad++;
                                    }
                                }
                            }
                        }
                    }
                }
        */
    }
}