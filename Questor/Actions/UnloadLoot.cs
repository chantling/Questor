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
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public static class UnloadLoot
    {
        //private static DateTime _nextUnloadAction = DateTime.UtcNow;
        private static DateTime _lastUnloadAction = DateTime.MinValue;

        //private static DateTime _lastPulse;

        private static bool AmmoIsBeingMoved;
        private static bool LootIsBeingMoved;
        private static IEnumerable<DirectItem> ammoToMove;
        private static IEnumerable<DirectItem> scriptsToMove;
        private static IEnumerable<DirectItem> commonMissionCompletionItemsToMove;
        private static IEnumerable<DirectItem> missionGateKeysToMove;

        private static bool WaitForLockedItems(string CallingRoutineForLogs, UnloadLootState _UnloadLootStateToSwitchTo)
        {
            if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
            {
                if (Math.Abs(DateTime.UtcNow.Subtract(_lastUnloadAction).TotalSeconds) > 15)
                {
                    Logging.Logging.Log("Moving Ammo timed out, clearing item locks");
                    Cache.Instance.DirectEve.UnlockItems();
                    _lastUnloadAction = DateTime.UtcNow.AddSeconds(-1);
                    return false;
                }

                if (Logging.Logging.DebugUnloadLoot)
                    Logging.Logging.Log("Waiting for Locks to clear. GetLockedItems().Count [" + Cache.Instance.DirectEve.GetLockedItems().Count + "]");
                return false;
            }

            _lastUnloadAction = DateTime.UtcNow.AddSeconds(-1);
            Logging.Logging.Log("Done");
            AmmoIsBeingMoved = false;
            _States.CurrentUnloadLootState = _UnloadLootStateToSwitchTo;
            return true;
        }


        public static long CurrentLootValueInCurrentShipInventory()
        {
            long lootValue = 0;
            foreach (var item in LootItemsInCurrentShipInventory())
            {
                lootValue += (long) item.AveragePrice()*Math.Max(item.Quantity, 1);
            }
            return lootValue;
        }


        public static long CurrentLootValueInItemHangar()
        {
            long lootValue = 0;
            foreach (var item in LootItemsInItemHangar())
            {
                lootValue += (long) item.AveragePrice()*Math.Max(item.Quantity, 1);
            }
            return lootValue;
        }

        public static List<DirectItem> LootItemsInCurrentShipInventory()
        {
            return Cache.Instance.CurrentShipsCargo.Items.Where(i => Combat.Combat.Ammo.All(a => a.TypeId != i.TypeId)
                                                                     && Settings.Instance.CapacitorInjectorScript != i.TypeId
                                                                     && i.TypeId != (int) TypeID.AngelDiamondTag
                                                                     && i.TypeId != (int) TypeID.GuristasDiamondTag
                                                                     && i.TypeId != (int) TypeID.ImperialNavyGatePermit
                                                                     && i.GroupId != (int) Group.AccelerationGateKeys
                                                                     && i.GroupId != (int) Group.Livestock
                                                                     && i.GroupId != (int) Group.MiscSpecialMissionItems
                                                                     && i.GroupId != (int) Group.Kernite
                                                                     && i.GroupId != (int) Group.Omber
                                                                     && i.GroupId != (int) Group.Commodities
                                                                     && i.TypeId != (int) TypeID.MetalScraps
                                                                     && i.TypeId != (int) TypeID.ReinforcedMetalScraps
                                                                     && i.TypeId != (int) TypeID.AncillaryShieldBoosterScript
                                                                     && i.TypeId != (int) TypeID.CapacitorInjectorScript
                                                                     && i.TypeId != (int) TypeID.FocusedWarpDisruptionScript
                                                                     && i.TypeId != (int) TypeID.OptimalRangeDisruptionScript
                                                                     && i.TypeId != (int) TypeID.OptimalRangeScript
                                                                     && i.TypeId != (int) TypeID.ScanResolutionDampeningScript
                                                                     && i.TypeId != (int) TypeID.ScanResolutionScript
                                                                     && i.TypeId != (int) TypeID.TargetingRangeDampeningScript
                                                                     && i.TypeId != (int) TypeID.TargetingRangeScript
                                                                     && i.TypeId != (int) TypeID.TrackingSpeedDisruptionScript
                                                                     && i.TypeId != (int) TypeID.TrackingSpeedScript
                                                                     && i.GroupId != (int) Group.CapacitorGroupCharge
                                                                     || Cache.Instance.UnloadLootTheseItemsAreLootById.ContainsKey(i.TypeId)
                ).ToList();
        }


        public static List<DirectItem> LootItemsInItemHangar()
        {
            return Cache.Instance.ItemHangar.Items.Where(i => Combat.Combat.Ammo.All(a => a.TypeId != i.TypeId)
                                                              && Settings.Instance.CapacitorInjectorScript != i.TypeId
                                                              && i.TypeId != (int) TypeID.AngelDiamondTag
                                                              && i.TypeId != (int) TypeID.GuristasDiamondTag
                                                              && i.TypeId != (int) TypeID.ImperialNavyGatePermit
                                                              && i.GroupId != (int) Group.AccelerationGateKeys
                                                              && i.GroupId != (int) Group.Livestock
                                                              && i.GroupId != (int) Group.MiscSpecialMissionItems
                                                              && i.GroupId != (int) Group.Kernite
                                                              && i.GroupId != (int) Group.Omber
                                                              && i.GroupId != (int) Group.Commodities
                                                              && i.TypeId != (int) TypeID.MetalScraps
                                                              && i.TypeId != (int) TypeID.ReinforcedMetalScraps
                                                              && i.TypeId != (int) TypeID.AncillaryShieldBoosterScript
                                                              && i.TypeId != (int) TypeID.CapacitorInjectorScript
                                                              && i.TypeId != (int) TypeID.FocusedWarpDisruptionScript
                                                              && i.TypeId != (int) TypeID.OptimalRangeDisruptionScript
                                                              && i.TypeId != (int) TypeID.OptimalRangeScript
                                                              && i.TypeId != (int) TypeID.ScanResolutionDampeningScript
                                                              && i.TypeId != (int) TypeID.ScanResolutionScript
                                                              && i.TypeId != (int) TypeID.TargetingRangeDampeningScript
                                                              && i.TypeId != (int) TypeID.TargetingRangeScript
                                                              && i.TypeId != (int) TypeID.TrackingSpeedDisruptionScript
                                                              && i.TypeId != (int) TypeID.TrackingSpeedScript
                                                              && i.GroupId != (int) Group.CapacitorGroupCharge
                                                              || Cache.Instance.UnloadLootTheseItemsAreLootById.ContainsKey(i.TypeId)
                ).ToList();
        }

        private static bool MoveAmmo()
        {
            try
            {
                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(1000))
                {
                    return false;
                }

                if (AmmoIsBeingMoved)
                {
                    if (!WaitForLockedItems(WeAreInThisStateForLogs(), UnloadLootState.MoveMissionGateKeys)) return false;
                    Logging.Logging.Log("Done");
                    AmmoIsBeingMoved = false;
                    return false;
                }

                try
                {
                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                        return false;
                    }

                    if (Cache.Instance.CurrentShipsCargo.Window.Type == "form.ActiveShipCargo")
                    {
                        if (Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo.Window.Type == \"form.ActiveShipCargo\")");
                        //
                        // Add Ammo to the list of things to move
                        //
                        try
                        {
                            ammoToMove =
                                Cache.Instance.CurrentShipsCargo.Items.Where(
                                    i => Combat.Combat.Ammo.Any(a => a.TypeId == i.TypeId) || Settings.Instance.CapacitorInjectorScript == i.TypeId).ToList();
                        }
                        catch (Exception exception)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No Ammo Found in CargoHold: moving on. [" + exception + "]");
                        }

                        if (ammoToMove != null)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("if (ammoToMove != null)");
                            if (ammoToMove.Any())
                            {
                                if (Logging.Logging.DebugUnloadLoot)
                                    Logging.Logging.Log("if (ammoToMove.Any())");
                                Logging.Logging.Log("Moving [" + ammoToMove.Count() + "] Ammo Stacks [" + ammoToMove.Where(i => !i.IsSingleton).Sum(i => i.Stacksize) +
                                    "] Total Units to AmmoHangar");
                                AmmoIsBeingMoved = true;
                                Cache.Instance.AmmoHangar.Add(ammoToMove);
                                _lastUnloadAction = DateTime.UtcNow;
                                return false;
                            }
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No Ammo Found in CargoHold: moving on.");
                        }

                        if (Logging.Logging.DebugUnloadLoot) Logging.Logging.Log("No Ammo in cargo to move");
                        _States.CurrentUnloadLootState = UnloadLootState.MoveMissionGateKeys;
                        return true;
                    }

                    if (Logging.Logging.DebugUnloadLoot)
                        Logging.Logging.Log("Cache.Instance.CargoHold is Not yet valid");
                    return false;
                }
                catch (NullReferenceException)
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        private static string WeAreInThisStateForLogs()
        {
            return _States.CurrentCombatMissionBehaviorState.ToString() + "." + _States.CurrentUnloadLootState.ToString();
        }

        private static bool MoveMissionGateKeys()
        {
            try
            {
                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(Cache.Instance.RandomNumber(2000, 3000)))
                {
                    return false;
                }

                if (AmmoIsBeingMoved)
                {
                    if (!WaitForLockedItems(WeAreInThisStateForLogs(), UnloadLootState.MoveCommonMissionCompletionItems)) return false;
                    return false;
                }

                try
                {
                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                        return false;
                    }

                    if (Cache.Instance.CurrentShipsCargo.Window.Type == "form.ActiveShipCargo")
                    {
                        if (Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo.Window.Type == \"form.ActiveShipCargo\")");
                        //
                        // Add gate keys to the list of things to move to the AmmoHangar, they are not mission completion items but are used during missions so should be avail
                        // to all pilots (thus the use of the ammo hangar)
                        //
                        try
                        {
                            missionGateKeysToMove = Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == (int) TypeID.AngelDiamondTag
                                                                                                      || i.TypeId == (int) TypeID.GuristasDiamondTag
                                                                                                      || i.TypeId == (int) TypeID.ImperialNavyGatePermit
                                                                                                      || i.GroupId == (int) Group.AccelerationGateKeys).ToList();
                        }
                        catch (Exception exception)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No Mission GateKeys Found in CargoHold: moving on. [" + exception + "]");
                        }

                        if (missionGateKeysToMove != null)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("if (missionGateKeysToMove != null)");
                            if (missionGateKeysToMove.Any())
                            {
                                if (Logging.Logging.DebugUnloadLoot)
                                    Logging.Logging.Log("if (missionGateKeysToMove.Any())");
                                Logging.Logging.Log("Moving [" + missionGateKeysToMove.Count() + "] Mission GateKeys to AmmoHangar");
                                AmmoIsBeingMoved = true;
                                Cache.Instance.AmmoHangar.Add(missionGateKeysToMove);
                                _lastUnloadAction = DateTime.UtcNow;
                                return false;
                            }

                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No Mission GateKeys Found in CargoHold: moving on.");
                        }

                        if (Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("No Mission Gate Keys in cargo to move");
                        _States.CurrentUnloadLootState = UnloadLootState.MoveCommonMissionCompletionItems;
                        return true;
                    }

                    if (Logging.Logging.DebugUnloadLoot)
                        Logging.Logging.Log("Cache.Instance.CargoHold is Not yet valid");
                    return false;
                }
                catch (NullReferenceException)
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        private static bool MoveCommonMissionCompletionItems()
        {
            try
            {
                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(Cache.Instance.RandomNumber(2000, 3000)))
                {
                    return false;
                }

                if (AmmoIsBeingMoved)
                {
                    if (!WaitForLockedItems(WeAreInThisStateForLogs(), UnloadLootState.MoveScripts)) return false;
                    Logging.Logging.Log("Done");
                    AmmoIsBeingMoved = false;
                    return false;
                }

                try
                {
                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                        return false;
                    }

                    if (Cache.Instance.CurrentShipsCargo.Window.Type == "form.ActiveShipCargo")
                    {
                        if (Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo.Window.Type == \"form.ActiveShipCargo\")");

                        //
                        // Add mission item  to the list of things to move to the itemhangar as they will be needed to complete the mission
                        //
                        try
                        {
                            //Cache.Instance.InvTypesById.ContainsKey(i.TypeId)
                            commonMissionCompletionItemsToMove = Cache.Instance.CurrentShipsCargo.Items.Where(i => i.GroupId == (int) Group.Livestock
                                                                                                                   ||
                                                                                                                   i.GroupId ==
                                                                                                                   (int) Group.MiscSpecialMissionItems
                                                                                                                   || i.GroupId == (int) Group.Kernite
                                                                                                                   || i.GroupId == (int) Group.Omber
                                                                                                                   ||
                                                                                                                   (i.GroupId == (int) Group.Commodities &&
                                                                                                                    i.TypeId != (int) TypeID.MetalScraps &&
                                                                                                                    i.TypeId !=
                                                                                                                    (int) TypeID.ReinforcedMetalScraps)
                                                                                                                   &&
                                                                                                                   !Cache.Instance
                                                                                                                       .UnloadLootTheseItemsAreLootById
                                                                                                                       .ContainsKey(i.TypeId)
                                ).ToList();
                        }
                        catch (Exception exception)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No Mission CompletionItems Found in CargoHold: moving on. [" + exception + "]");
                        }


                        if (commonMissionCompletionItemsToMove != null)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("if (commonMissionCompletionItemsToMove != null)");
                            if (commonMissionCompletionItemsToMove.Any())
                            {
                                if (Logging.Logging.DebugUnloadLoot)
                                    Logging.Logging.Log("if (commonMissionCompletionItemsToMove.Any())");
                                if (Cache.Instance.ItemHangar == null) return false;
                                Logging.Logging.Log("Moving [" + commonMissionCompletionItemsToMove.Count() + "] Mission Completion items to ItemHangar");
                                Cache.Instance.ItemHangar.Add(commonMissionCompletionItemsToMove);
                                AmmoIsBeingMoved = true;
                                _lastUnloadAction = DateTime.UtcNow;
                                return false;
                            }

                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No Mission CompletionItems Found in CargoHold: moving on.");
                        }

                        if (Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("No commonMissionCompletionItems in cargo to move");
                        _States.CurrentUnloadLootState = UnloadLootState.MoveScripts;
                        return true;
                    }

                    if (Logging.Logging.DebugUnloadLoot)
                        Logging.Logging.Log("Cache.Instance.CargoHold is Not yet valid");
                    return false;
                }
                catch (NullReferenceException)
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        private static bool MoveScripts()
        {
            try
            {
                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(Cache.Instance.RandomNumber(2000, 3000)))
                {
                    return false;
                }

                if (AmmoIsBeingMoved)
                {
                    if (!WaitForLockedItems(WeAreInThisStateForLogs(), UnloadLootState.MoveLoot)) return false;
                    Logging.Logging.Log("Done");
                    AmmoIsBeingMoved = false;
                    return false;
                }

                try
                {
                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                        return false;
                    }

                    if (Cache.Instance.CurrentShipsCargo.Window.Type == "form.ActiveShipCargo")
                    {
                        if (Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo.Window.Type == \"form.ActiveShipCargo\")");
                        //
                        // Add Scripts (by groupID) to the list of things to move
                        //

                        try
                        {
                            //
                            // items to move has to be cleared here before assigning but is currently not being cleared here
                            //
                            scriptsToMove = Cache.Instance.CurrentShipsCargo.Items.Where(i =>
                                i.TypeId == (int) TypeID.AncillaryShieldBoosterScript ||
                                i.TypeId == (int) TypeID.CapacitorInjectorScript ||
                                i.TypeId == (int) TypeID.FocusedWarpDisruptionScript ||
                                i.TypeId == (int) TypeID.OptimalRangeDisruptionScript ||
                                i.TypeId == (int) TypeID.OptimalRangeScript ||
                                i.TypeId == (int) TypeID.ScanResolutionDampeningScript ||
                                i.TypeId == (int) TypeID.ScanResolutionScript ||
                                i.TypeId == (int) TypeID.TargetingRangeDampeningScript ||
                                i.TypeId == (int) TypeID.TargetingRangeScript ||
                                i.TypeId == (int) TypeID.TrackingSpeedDisruptionScript ||
                                i.TypeId == (int) TypeID.TrackingSpeedScript ||
                                i.GroupId == (int) Group.CapacitorGroupCharge).ToList();
                        }
                        catch (Exception exception)
                        {
                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("MoveAmmo: No Scripts Found in CargoHold: moving on. [" + exception + "]");
                        }

                        try
                        {
                            if (scriptsToMove != null)
                            {
                                if (Logging.Logging.DebugUnloadLoot)
                                    Logging.Logging.Log("if (scriptsToMove != null)");
                                if (scriptsToMove.Any())
                                {
                                    if (Logging.Logging.DebugUnloadLoot)
                                        Logging.Logging.Log("if (scriptsToMove.Any())");
                                    if (Cache.Instance.ItemHangar == null) return false;
                                    Logging.Logging.Log("Moving [" + scriptsToMove.Count() + "] Scripts to ItemHangar");
                                    AmmoIsBeingMoved = true;
                                    Cache.Instance.ItemHangar.Add(scriptsToMove);
                                    _lastUnloadAction = DateTime.UtcNow;
                                    return false;
                                }
                                if (Logging.Logging.DebugUnloadLoot)
                                    Logging.Logging.Log("No Scripts Found in CargoHold: moving on.");
                            }

                            if (Logging.Logging.DebugUnloadLoot)
                                Logging.Logging.Log("No scripts in cargo to move");
                            _States.CurrentUnloadLootState = UnloadLootState.MoveLoot;
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Logging.Logging.Log("Exception [" + ex + "]");
                        }
                    }

                    if (Logging.Logging.DebugUnloadLoot)
                        Logging.Logging.Log("Cache.Instance.CargoHold is Not yet valid");
                    return false;
                }
                catch (NullReferenceException)
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        private static bool StackAmmoHangar()
        {
            try
            {
                //disable stacking for now
                _States.CurrentUnloadLootState = UnloadLootState.MoveLoot;
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        private static bool MoveLoot()
        {
            if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(1000))
            {
                return false;
            }

            if (Cache.Instance.CurrentShipsCargo == null)
            {
                Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            if (Logging.Logging.DebugUnloadLoot)
                Logging.Logging.Log("Cache.Instance.CurrentShipsCargo.Items.Any() [" + Cache.Instance.CurrentShipsCargo.Items.Any() + "]");


            if (LootIsBeingMoved || !Cache.Instance.CurrentShipsCargo.Items.Any())
            {
                if (!WaitForLockedItems(WeAreInThisStateForLogs(), UnloadLootState.StackLootHangar)) return false;
                //
                // why do we *ever* have to close the loothangar?
                //
                //if (!Cache.Instance.CloseLootHangar("UnloadLootState.MoveLoot")) return false;
                Logging.Logging.Log("Loot was worth an estimated [" + Statistics.LootValue.ToString("#,##0") + "] isk in buy-orders");
                LootIsBeingMoved = false;
                _States.CurrentUnloadLootState = UnloadLootState.StackLootHangar;
                return true;
            }

            IEnumerable<DirectItem> lootToMove = Cache.Instance.CurrentShipsCargo.Items.ToList();


            //IEnumerable<DirectItem> somelootToMove = lootToMove;
            if (Logging.Logging.DebugUnloadLoot)
                Logging.Logging.Log("foreach (DirectItem item in lootToMove) (start)");

            var y = lootToMove.Count();
            var x = 1;

            foreach (var item in lootToMove)
            {
                if (item.Volume != 0)
                {
                    if (Logging.Logging.DebugLootValue)
                        Logging.Logging.Log("[" + x + "of" + y + "] ItemName [" + item.TypeName + "] ItemTypeID [" + item.TypeId + "] AveragePrice[" + (int)item.AveragePrice() +
                            "]");
                    Statistics.LootValue += (int) item.AveragePrice()*Math.Max(item.Quantity, 1);
                }
                x++;
            }

            if (Logging.Logging.DebugUnloadLoot)
                Logging.Logging.Log("foreach (DirectItem item in lootToMove) (done)");
            if (lootToMove.Any())
            {
                if (Logging.Logging.DebugUnloadLoot)
                    Logging.Logging.Log("if (lootToMove.Any() && !LootIsBeingMoved))");

                if (string.IsNullOrEmpty(Settings.Instance.LootHangarTabName)) // if we do NOT have the loot hangar configured.
                {
                    /*
                    if (Logging.DebugUnloadLoot) Logging.Log("UnloadLootState.Moveloot", "LootHangar setting is not configured, assuming lothangar is local items hangar (and its 999 item limit)", Logging.White);

                    // Move loot to the loot hangar
                    int roominHangar = (999 - Cache.Instance.LootHangar.Items.Count);
                    if (roominHangar > lootToMove.Count())
                    {
                        if (Logging.DebugUnloadLoot) Logging.Log("UnloadLootState.Moveloot", "LootHangar has plenty of room to move loot all in one go", Logging.White);
                        Cache.Instance.LootHangar.Add(lootToMove);
                        AllLootWillFit = true;
                        _lootToMoveWillStillNotFitCount = 0;
                        return;
                    }

                    AllLootWillFit = false;
                    Logging.Log("Unloadloot", "LootHangar is almost full and contains [" + Cache.Instance.LootHangar.Items.Count + "] of 999 total possible stacks", Logging.Orange);
                    if (roominHangar > 50)
                    {
                        if (Logging.DebugUnloadLoot) Logging.Log("UnloadLoot", "LootHangar has more than 50 item slots left", Logging.White);
                        somelootToMove = lootToMove.Where(i => Settings.Instance.Ammo.All(a => a.TypeId != i.TypeId)).ToList().GetRange(0, 49).ToList();
                    }
                    else if (roominHangar > 20)
                    {
                        if (Logging.DebugUnloadLoot) Logging.Log("UnloadLoot", "LootHangar has more than 20 item slots left", Logging.White);
                        somelootToMove = lootToMove.Where(i => Settings.Instance.Ammo.All(a => a.TypeId != i.TypeId)).ToList().GetRange(0, 19).ToList();
                    }

                    if (somelootToMove.Any())
                    {
                        Logging.Log("UnloadLoot", "Moving [" + somelootToMove.Count() + "]  of [" + lootToMove.Count() + "] items into the LootHangar", Logging.White);
                        Cache.Instance.LootHangar.Add(somelootToMove);
                        return;
                    }

                    if (_lootToMoveWillStillNotFitCount < 7)
                    {
                        _lootToMoveWillStillNotFitCount++;
                        if (!Cache.Instance.StackLootHangar("Unloadloot")) return;
                        return;
                    }

                    Logging.Log("Unloadloot", "We tried to stack the loothangar 7 times and we still could not fit all the LootToMove into the LootHangar [" + Cache.Instance.LootHangar.Items.Count + " items ]", Logging.Red);
                    _States.CurrentQuestorState = QuestorState.Error;
                    return;
					 */
                }

                //
                // if we are using the corp hangar then just grab all the loot in one go.
                //
                if (lootToMove.Any() && !LootIsBeingMoved)
                {
                    if (Cache.Instance.LootHangar == null)
                    {
                        if (Logging.Logging.DebugHangars || Logging.Logging.DebugUnloadLoot)
                            Logging.Logging.Log("if (Cache.Instance.LootHangar == null)");
                        return false;
                    }

                    //Logging.Log("UnloadLoot", "Moving [" + lootToMove.Count() + "] items from CargoHold to LootHangar which has [" + Cache.Instance.LootHangar.Items.Count() + "] items in it now.", Logging.White);
                    Logging.Logging.Log("Moving [" + lootToMove.Count() + "] items from CargoHold to Loothangar");
                    LootIsBeingMoved = true;
                    Cache.Instance.LootHangar.Add(lootToMove);
                    _lastUnloadAction = DateTime.UtcNow;
                    return false;
                }
                if (Logging.Logging.DebugUnloadLoot) Logging.Logging.Log("1) if (lootToMove.Any()) is false");
                return false;
            }

            if (Logging.Logging.DebugUnloadLoot) Logging.Logging.Log("2) if (lootToMove.Any()) is false");
            return false;
        }

        private static bool StackLootHangar()
        {
            try
            {
                //disable stacking for now
//				_States.CurrentUnloadLootState = UnloadLootState.Done;
//				return true;

                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(Cache.Instance.RandomNumber(2000, 3000)))
                {
                    return false;
                }

                try
                {
                    //
                    // Stack AmmoHangar
                    //
                    if (Logging.Logging.DebugUnloadLoot)
                        Logging.Logging.Log("if (!Cache.Instance.StackAmmoHangar(UnloadLoot.MoveAmmo)) return;");
                    if (!Cache.Instance.StackLootHangar("UnloadLoot.StackLootHangar")) return false;
                    _States.CurrentUnloadLootState = UnloadLootState.Done;
                    return true;
                }
                catch (NullReferenceException)
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        private static bool EveryUnloadLootPulse()
        {
            try
            {
                if (!Cache.Instance.InStation)
                    return false;

                if (Cache.Instance.InSpace)
                    return false;

                if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20))
                    // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                    return false;

                return true;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }


        public static void ProcessState()
        {
            if (!EveryUnloadLootPulse()) return;

            switch (_States.CurrentUnloadLootState)
            {
                case UnloadLootState.Idle:
                    break;

                case UnloadLootState.Done:
                    break;

                case UnloadLootState.Begin:
                    AmmoIsBeingMoved = false;
                    LootIsBeingMoved = false;
                    _lastUnloadAction = DateTime.UtcNow.AddMinutes(-1);
                    //if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any())
                    //{
                    _States.CurrentUnloadLootState = UnloadLootState.MoveAmmo;
                    //}
                    //
                    //_States.CurrentUnloadLootState = UnloadLootState.Done;
                    break;

                case UnloadLootState.MoveAmmo:
                    if (!MoveAmmo()) return;
                    break;

                case UnloadLootState.MoveMissionGateKeys:
                    if (!MoveMissionGateKeys()) return;
                    break;

                case UnloadLootState.MoveCommonMissionCompletionItems:
                    if (!MoveCommonMissionCompletionItems()) return;
                    break;

                case UnloadLootState.MoveScripts:
                    if (!MoveScripts()) return;
                    break;

                case UnloadLootState.StackAmmoHangar:
                    if (!StackAmmoHangar()) return;
                    break;

                case UnloadLootState.MoveLoot:
                    if (!MoveLoot()) return;
                    break;

                case UnloadLootState.StackLootHangar:
                    if (!StackLootHangar()) return;
                    break;
            }
        }
    }
}