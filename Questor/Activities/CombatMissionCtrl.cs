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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DirectEve;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;
using Action = Questor.Modules.Actions.Action;

namespace Questor.Modules.Activities
{
    public class CombatMissionCtrl
    {
        private static int _currentAction;
        private static List<Action> _pocketActions;
        public static bool DeactivateIfNothingTargetedWithinRange;
        private DateTime? _clearPocketTimeout;

        private double _lastX;
        private double _lastY;
        private double _lastZ;
        private DateTime _moveToNextPocket = DateTime.UtcNow.AddHours(10);
        private DateTime _nextCombatMissionCtrlAction = DateTime.UtcNow;
        private bool _waiting;
        private DateTime _waitingSince;
        private int AttemptsToActivateGateTimer;
        private int AttemptsToGetAwayFromGate;
        private bool CargoHoldHasBeenStacked;
        private bool ItemsHaveBeenMoved;

        public CombatMissionCtrl()
        {
            _pocketActions = new List<Action>();
            IgnoreTargets = new HashSet<string>();
        }

        /// <summary>
        ///     List of targets to ignore
        /// </summary>
        public static HashSet<string> IgnoreTargets { get; private set; }

        //public string Mission { get; set; }
        public static int PocketNumber { get; set; }

        private void Nextaction()
        {
            // make sure all approach / orbit / align timers are reset (why cant we wait them out in the next action!?)
            Time.Instance.NextApproachAction = DateTime.UtcNow;
            Time.Instance.NextOrbit = DateTime.UtcNow;
            Time.Instance.NextAlign = DateTime.UtcNow;

            // now that we have completed this action revert OpenWrecks to false

            if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
            {
                if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }

            Salvage.MissionLoot = false;
            Cache.Instance.normalNav = true;
            Cache.Instance.onlyKillAggro = false;
            MissionSettings.MissionActivateRepairModulesAtThisPerc = null;
            MissionSettings.PocketUseDrones = null;
            ItemsHaveBeenMoved = false;
            CargoHoldHasBeenStacked = false;
            _currentAction++;
            return;
        }

        private bool BookmarkPocketForSalvaging()
        {
            if (Logging.Logging.DebugSalvage) Logging.Logging.Log("Entered: BookmarkPocketForSalvaging");
            double RangeToConsiderWrecksDuringLootAll;
            var tractorBeams = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.TractorBeam).ToList();
            if (tractorBeams.Count > 0)
            {
                RangeToConsiderWrecksDuringLootAll = Math.Min(tractorBeams.Min(t => t.OptimalRange), Cache.Instance.ActiveShip.MaxTargetRange);
            }
            else
            {
                RangeToConsiderWrecksDuringLootAll = 1500;
            }

            if ((Salvage.LootEverything || Salvage.LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion) &&
                Cache.Instance.UnlootedContainers.Count(i => i.Distance < RangeToConsiderWrecksDuringLootAll) > Salvage.MinimumWreckCount)
            {
                if (Logging.Logging.DebugSalvage)
                    Logging.Logging.Log("LootEverything [" + Salvage.LootEverything + "] UnLootedContainers [" + Cache.Instance.UnlootedContainers.Count() +
                        "LootedContainers [" + Cache.Instance.LootedContainers.Count() + "] MinimumWreckCount [" + Salvage.MinimumWreckCount +
                        "] We will wait until everything in range is looted.");

                if (Cache.Instance.UnlootedContainers.Count(i => i.Distance < RangeToConsiderWrecksDuringLootAll) > 0)
                {
                    if (Logging.Logging.DebugSalvage)
                        Logging.Logging.Log("if (Cache.Instance.UnlootedContainers.Count [" +
                            Cache.Instance.UnlootedContainers.Count(i => i.Distance < RangeToConsiderWrecksDuringLootAll) +
                            "] (w => w.Distance <= RangeToConsiderWrecksDuringLootAll [" + RangeToConsiderWrecksDuringLootAll + "]) > 0)");
                    return false;
                }

                if (Logging.Logging.DebugSalvage)
                    Logging.Logging.Log("LootEverything [" + Salvage.LootEverything +
                        "] We have LootEverything set to on. We cant have any need for the pocket bookmarks... can we?!");
                return true;
            }

            if (Salvage.CreateSalvageBookmarks)
            {
                if (Logging.Logging.DebugSalvage)
                    Logging.Logging.Log("CreateSalvageBookmarks [" + Salvage.CreateSalvageBookmarks + "]");

                if (MissionSettings.ThisMissionIsNotWorthSalvaging())
                {
                    Logging.Logging.Log("[" + MissionSettings.MissionName + "] is a mission not worth salvaging, skipping salvage bookmark creation");
                    return true;
                }

                // Nothing to loot
                if (Cache.Instance.UnlootedContainers.Count() < Salvage.MinimumWreckCount)
                {
                    if (Logging.Logging.DebugSalvage)
                        Logging.Logging.Log("LootEverything [" + Salvage.LootEverything + "] UnlootedContainers [" + Cache.Instance.UnlootedContainers.Count() +
                            "] MinimumWreckCount [" + Salvage.MinimumWreckCount + "] We will wait until everything in range is looted.");
                    // If Settings.Instance.LootEverything is false we may leave behind a lot of unlooted containers.
                    // This scenario only happens when all wrecks are within tractor range and you have a salvager
                    // ( typically only with a Golem ).  Check to see if there are any cargo containers in space.  Cap
                    // boosters may cause an unneeded salvage trip but that is better than leaving millions in loot behind.
                    if (DateTime.UtcNow > Time.Instance.NextBookmarkPocketAttempt)
                    {
                        Time.Instance.NextBookmarkPocketAttempt = DateTime.UtcNow.AddSeconds(Time.Instance.BookmarkPocketRetryDelay_seconds);
                        if (!Salvage.LootEverything && Cache.Instance.Containers.Count() < Salvage.MinimumWreckCount)
                        {
                            Logging.Logging.Log("No bookmark created because the pocket has [" + Cache.Instance.Containers.Count() + "] wrecks/containers and the minimum is [" +
                                Salvage.MinimumWreckCount + "]");
                            return true;
                        }

                        Logging.Logging.Log("No bookmark created because the pocket has [" + Cache.Instance.UnlootedContainers.Count() +
                            "] wrecks/containers and the minimum is [" + Salvage.MinimumWreckCount + "]");
                        return true;
                    }

                    if (Logging.Logging.DebugSalvage)
                        Logging.Logging.Log("Cache.Instance.NextBookmarkPocketAttempt is in [" + Time.Instance.NextBookmarkPocketAttempt.Subtract(DateTime.UtcNow).TotalSeconds +
                            "sec] waiting");
                    return false;
                }

                // Do we already have a bookmark?
                var bookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                if (bookmarks != null && bookmarks.Any())
                {
                    var bookmark = bookmarks.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int) Distances.OnGridWithMe);
                    if (bookmark != null)
                    {
                        Logging.Logging.Log("salvaging bookmark for this pocket is done [" + bookmark.Title + "]");
                        return true;
                    }

                    //
                    // if we have bookmarks but there is no bookmark on grid we need to continue and create the salvage bookmark.
                    //
                }

                // No, create a bookmark
                var label = string.Format("{0} {1:HHmm}", Settings.Instance.BookmarkPrefix, DateTime.UtcNow);
                Logging.Logging.Log("Bookmarking pocket for salvaging [" + label + "]");
                Cache.Instance.CreateBookmark(label);
                return true;
            }

            return true;
        }

        private void DoneAction()
        {
            // Tell the drones module to retract drones
            Drones.IsMissionPocketDone = true;
            MissionSettings.MissionUseDrones = null;

            if (Drones.ActiveDrones.Any())
            {
                if (Logging.Logging.DebugDoneAction)
                    Logging.Logging.Log("We still have drones out! Wait for them to return.");
                return;
            }

            // Add bookmark (before we're done)
            if (Salvage.CreateSalvageBookmarks)
            {
                if (!BookmarkPocketForSalvaging())
                {
                    if (Logging.Logging.DebugDoneAction)
                        Logging.Logging.Log("Wait for CreateSalvageBookmarks to return true (it just returned false!)");
                    return;
                }
            }

            //
            // we are ready and can set the "done" State.
            //
            Salvage.CurrentlyShouldBeSalvaging = false;
            _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Done;
            if (Logging.Logging.DebugDoneAction)
                Logging.Logging.Log("we are ready and have set [ _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Done ]");
            return;
        }

        private void LogWhatIsOnGridAction(Action action)
        {
            Logging.Logging.Log("Log Entities on Grid.");
            if (!Statistics.EntityStatistics(Cache.Instance.EntitiesOnGrid)) return;
            Nextaction();
            return;
        }

        private void ActivateAction(Action action)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            //we cant move in bastion mode, do not try
            var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
            if (bastionModules.Any(i => i.IsActive))
            {
                Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Activate until bastion deactivates");
                _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(15);
                return;
            }

            bool optional;
            if (!bool.TryParse(action.GetParameterValue("optional"), out optional))
            {
                optional = false;
            }

            var target = action.GetParameterValue("target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
            {
                target = "Acceleration Gate";
            }

            IEnumerable<EntityCache> targets =
                Cache.Instance.EntitiesByName(target, Cache.Instance.EntitiesOnGrid.Where(i => i.Distance < (int) Distances.OnGridWithMe)).ToList();
            if (!targets.Any())
            {
                if (!_waiting)
                {
                    Logging.Logging.Log("Activate: Can't find [" + target + "] to activate! Waiting 30 seconds before giving up");
                    _waitingSince = DateTime.UtcNow;
                    _waiting = true;
                }
                else if (_waiting)
                {
                    if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds > Time.Instance.NoGateFoundRetryDelay_seconds)
                    {
                        Logging.Logging.Log("Activate: After 30 seconds of waiting the gate is still not on grid: CombatMissionCtrlState.Error");
                        if (optional) //if this action has the optional parameter defined as true then we are done if we cant find the gate
                        {
                            DoneAction();
                        }
                        else
                        {
                            _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Error;
                        }
                    }
                }
                return;
            }

            //if (closest.Distance <= (int)Distance.CloseToGateActivationRange) // if your distance is less than the 'close enough' range, default is 7000 meters
            var closest = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (closest != null)
            {
                if (closest.Distance <= (int) Distances.GateActivationRange + 150)
                {
                    if (Logging.Logging.DebugActivateGate)
                        Logging.Logging.Log("if (closest.Distance [" + closest.Distance + "] <= (int)Distances.GateActivationRange [" + (int)Distances.GateActivationRange +
                            "])");

                    // Tell the drones module to retract drones
                    Drones.IsMissionPocketDone = true;

                    // We cant activate if we have drones out
                    if (Drones.ActiveDrones.Any())
                    {
                        if (Logging.Logging.DebugActivateGate)
                            Logging.Logging.Log("if (Cache.Instance.ActiveDrones.Any())");
                        return;
                    }

                    //
                    // this is a bad idea for a speed tank, we ought to somehow cache the object they are orbiting/approaching, etc
                    // this seemingly slowed down the exit from certain missions for me for 2-3min as it had a command to orbit some random object
                    // after the "done" command
                    //
                    if (closest.Distance < -10100)
                    {
                        if (Logging.Logging.DebugActivateGate)
                            Logging.Logging.Log("if (closest.Distance < -10100)");

                        AttemptsToGetAwayFromGate++;
                        if (AttemptsToGetAwayFromGate > 30)
                        {
                            if (closest.Orbit(1000))
                            {
                                Logging.Logging.Log("Activate: We are too close to [" + closest.Name + "] Initiating orbit");
                                return;
                            }

                            return;
                        }
                    }

                    if (Logging.Logging.DebugActivateGate) Logging.Logging.Log("if (closest.Distance >= -10100)");

                    // Add bookmark (before we activate)
                    if (Salvage.CreateSalvageBookmarks)
                    {
                        BookmarkPocketForSalvaging();
                    }

                    if (Logging.Logging.DebugActivateGate)
                        Logging.Logging.Log("Activate: Reload before moving to next pocket");
                    if (!Combat.Combat.ReloadAll(Cache.Instance.MyShipEntity, true)) return;
                    if (Logging.Logging.DebugActivateGate) Logging.Logging.Log("Activate: Done reloading");
                    AttemptsToActivateGateTimer++;

                    if (DateTime.UtcNow > Time.Instance.NextActivateAction || AttemptsToActivateGateTimer > 30)
                    {
                        if (closest.Activate())
                        {
                            Logging.Logging.Log("Activate: [" + closest.Name + "] Move to next pocket after reload command and change state to 'NextPocket'");
                            AttemptsToActivateGateTimer = 0;
                            // Do not change actions, if NextPocket gets a timeout (>2 mins) then it reverts to the last action
                            _moveToNextPocket = DateTime.UtcNow;
                            _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.NextPocket;
                        }
                    }

                    if (Logging.Logging.DebugActivateGate) Logging.Logging.Log("------------------");
                    return;
                }

                AttemptsToActivateGateTimer = 0;
                AttemptsToGetAwayFromGate = 0;

                if (closest.Distance < (int) Distances.WarptoDistance)
                    //else if (closest.Distance < (int)Distances.WarptoDistance) //if we are inside warpto distance then approach
                {
                    if (Logging.Logging.DebugActivateGate)
                        Logging.Logging.Log("if (closest.Distance < (int)Distances.WarptoDistance)");

                    // Move to the target
                    if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                    {
                        if (Cache.Instance.IsOrbiting(closest.Id) || Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closest.Id ||
                            Cache.Instance.MyShipEntity.Velocity < 50)
                        {
                            if (closest.Approach())
                            {
                                Logging.Logging.Log("Approaching target [" + closest.Name + "][" + closest.MaskedId + "][" + Math.Round(closest.Distance / 1000, 0) + "k away]");
                                return;
                            }

                            return;
                        }

                        if (Logging.Logging.DebugActivateGate)
                            Logging.Logging.Log("Cache.Instance.IsOrbiting [" + Cache.Instance.IsOrbiting(closest.Id) + "] Cache.Instance.MyShip.Velocity [" +
                                Math.Round(Cache.Instance.MyShipEntity.Velocity, 0) + "m/s]");
                        if (Logging.Logging.DebugActivateGate)
                            if (Cache.Instance.Approaching != null)
                                Logging.Logging.Log("Cache.Instance.Approaching.Id [" + Cache.Instance.Approaching.Id + "][closest.Id: " + closest.Id + "]");
                        if (Logging.Logging.DebugActivateGate) Logging.Logging.Log("------------------");
                        return;
                    }

                    if (Cache.Instance.IsOrbiting(closest.Id) || Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closest.Id)
                    {
                        Logging.Logging.Log("Activate: Delaying approach for: [" + Math.Round(Time.Instance.NextApproachAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                            "] seconds");
                        return;
                    }

                    if (Logging.Logging.DebugActivateGate) Logging.Logging.Log("------------------");
                    return;
                }

                if (closest.Distance > (int) Distances.WarptoDistance)
                    //we must be outside warpto distance, but we are likely in a DeadSpace so align to the target
                {
                    // We cant warp if we have drones out - but we are aligning not warping so we do not care
                    //if (Cache.Instance.ActiveDrones.Count() > 0)
                    //    return;

                    if (closest.AlignTo())
                    {
                        Logging.Logging.Log("Activate: AlignTo: [" + closest.Name + "] This only happens if we are asked to Activate something that is outside [" +
                            Distances.CloseToGateActivationRange + "]");
                        return;
                    }

                    return;
                }

                Logging.Logging.Log("Activate: Error: [" + closest.Name + "] at [" + closest.Distance +
                    "] is not within jump distance, within warpable distance or outside warpable distance, (!!!), retrying action.");
            }

            return;
        }

        private void ClearAggroAction(Action action)
        {
            if (!Cache.Instance.NormalApproach) Cache.Instance.NormalApproach = true;

            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            if (DistanceToClear != 0 && DistanceToClear != -2147483648 && DistanceToClear != 2147483647)
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            //if (Settings.Instance.TargetSelectionMethod == "isdp")
            //{
            if (Combat.Combat.GetBestPrimaryWeaponTarget(DistanceToClear, false, "combat", Combat.Combat.combatTargets.Where(t => t.IsTargetedBy).ToList()))
                _clearPocketTimeout = null;
            //}
            //else //use new target selection method
            //{
            //    if (Cache.Instance.__GetBestWeaponTargets(DistanceToClear, Combat.combatTargets.Where(t => t.IsTargetedBy)).Any())
            //        _clearPocketTimeout = null;
            //}

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
        }

        private void ClearPocketAction(Action action)
        {
            if (!Cache.Instance.NormalApproach)
            {
                Cache.Instance.NormalApproach = true;
            }

            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
            {
                DistanceToClear = (int) Combat.Combat.MaxRange;
            }

            if (DistanceToClear != 0 && DistanceToClear != -2147483648 && DistanceToClear != 2147483647)
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            //panic handles adding any priority targets and combat will prefer to kill any priority targets

            //If the closest target is out side of our max range, combat cant target, which means GetBest cant return true, so we are going to try and use potentialCombatTargets instead
            if (Combat.Combat.PotentialCombatTargets.Any())
            {
                //we may be too far out of range of the closest target to get combat to kick in, lets move us into range here
                EntityCache ClosestPotentialCombatTarget = null;

                if (Logging.Logging.DebugClearPocket)
                    Logging.Logging.Log("Cache.Instance.__GetBestWeaponTargets(DistanceToClear);");

                // Target
                //if (Settings.Instance.TargetSelectionMethod == "isdp")
                //{
                if (Combat.Combat.GetBestPrimaryWeaponTarget(DistanceToClear, false, "combat"))
                    _clearPocketTimeout = null;

                //}
                //else //use new target selection method
                //{
                //    if (Cache.Instance.__GetBestWeaponTargets(DistanceToClear).Any())
                //        _clearPocketTimeout = null;
                //}

                //
                // grab the preferredPrimaryWeaponsTarget if its defined and exists on grid as our navigation point
                //
                if (Combat.Combat.PreferredPrimaryWeaponTargetID != null && Combat.Combat.PreferredPrimaryWeaponTarget != null)
                {
                    if (Combat.Combat.PreferredPrimaryWeaponTarget.IsOnGridWithMe)
                    {
                        if (Logging.Logging.DebugClearPocket)
                            Logging.Logging.Log("ClosestPotentialCombatTarget = Combat.PreferredPrimaryWeaponTarget [" + Combat.Combat.PreferredPrimaryWeaponTarget.Name + "]");
                        ClosestPotentialCombatTarget = Combat.Combat.PreferredPrimaryWeaponTarget;
                    }
                }

                //
                // retry to use PreferredPrimaryWeaponTarget
                //
                if (ClosestPotentialCombatTarget == null && Combat.Combat.PreferredPrimaryWeaponTargetID != null &&
                    Combat.Combat.PreferredPrimaryWeaponTarget != null)
                {
                    if (Combat.Combat.PreferredPrimaryWeaponTarget.IsOnGridWithMe)
                    {
                        if (Logging.Logging.DebugClearPocket)
                            Logging.Logging.Log("ClosestPotentialCombatTarget = Combat.PreferredPrimaryWeaponTarget [" + Combat.Combat.PreferredPrimaryWeaponTarget.Name + "]");
                        ClosestPotentialCombatTarget = Combat.Combat.PreferredPrimaryWeaponTarget;
                    }
                }

                if (ClosestPotentialCombatTarget == null) //otherwise just grab something close (excluding sentries)
                {
                    if (Combat.Combat.PotentialCombatTargets.Any())
                    {
                        if (Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Nearest5kDistance).FirstOrDefault() != null)
                        {
                            var closestPCT = Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Nearest5kDistance).FirstOrDefault();
                            if (closestPCT != null)
                            {
                                if (Logging.Logging.DebugClearPocket)
                                    Logging.Logging.Log("ClosestPotentialCombatTarget = Combat.PotentialCombatTargets.OrderBy(t => t.Nearest5kDistance).FirstOrDefault(); [" +
                                        closestPCT.Name + "]");
                            }
                        }
                    }

                    ClosestPotentialCombatTarget = Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Nearest5kDistance).FirstOrDefault();
                }

                if (ClosestPotentialCombatTarget != null &&
                    (ClosestPotentialCombatTarget.Distance > Combat.Combat.MaxRange || !ClosestPotentialCombatTarget.IsInOptimalRange))
                {
                    if (!Cache.Instance.IsApproachingOrOrbiting(ClosestPotentialCombatTarget.Id))
                    {
                        NavigateOnGrid.NavigateIntoRange(ClosestPotentialCombatTarget, "combatMissionControl", true);
                    }
                }

                _clearPocketTimeout = null;
            }
            //Cache.Instance.AddPrimaryWeaponPriorityTargets(Combat.PotentialCombatTargets.Where(t => targetNames.Contains(t.Name)).OrderBy(t => t.Distance).ToList(), PrimaryWeaponPriority.PriorityKillTarget, "CombatMissionCtrl.KillClosestByName");

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        private void ClearWithinWeaponsRangeOnlyAction(Action action)
        {
            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
            {
                DistanceToClear = (int) Combat.Combat.MaxRange - 1000;
            }

            if (DistanceToClear == 0 || DistanceToClear == -2147483648 || DistanceToClear == 2147483647)
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            //
            // note this WILL clear sentries within the range given... it does NOT respect the KillSentries setting. 75% of the time this wont matter as sentries will be outside the range
            //

            // Target
            //if (Settings.Instance.TargetSelectionMethod == "isdp")
            //{
            if (Combat.Combat.GetBestPrimaryWeaponTarget(DistanceToClear, false, "combat"))
                _clearPocketTimeout = null;

            //}
            //else //use new target selection method
            //{
            //    if (Cache.Instance.__GetBestWeaponTargets(DistanceToClear).Any())
            //        _clearPocketTimeout = null;
            //}

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue)
            {
                _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);
            }

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value)
            {
                return;
            }

            Logging.Logging.Log("is complete: no more targets in weapons range");
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        private void ClearWithinWeaponsRangeWithAggroOnlyAction(Action action)
        {
            if (Cache.Instance.NormalApproach)
            {
                Cache.Instance.NormalApproach = false;
            }

            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
            {
                DistanceToClear = (int) Combat.Combat.MaxRange;
            }

            if (DistanceToClear != 0 && DistanceToClear != -2147483648 && DistanceToClear != 2147483647)
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            if (Combat.Combat.GetBestPrimaryWeaponTarget(DistanceToClear, false, "combat", Combat.Combat.combatTargets.Where(t => t.IsTargetedBy).ToList()))
                _clearPocketTimeout = null;


            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue)
            {
                _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);
            }

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value)
            {
                return;
            }

            Logging.Logging.Log("is complete: no more targets that are targeting us");
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        private void OrbitEntityAction(Action action)
        {
            if (Cache.Instance.NormalApproach)
            {
                Cache.Instance.NormalApproach = false;
            }

            Cache.Instance.normalNav = false;

            var target = action.GetParameterValue("target");

            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
            {
                notTheClosest = false;
            }

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
            {
                Logging.Logging.Log("No Entity Specified to orbit: skipping OrbitEntity Action");
                Nextaction();
                return;
            }

            IEnumerable<EntityCache> targets = Cache.Instance.EntitiesByPartialName(target).ToList();
            if (!targets.Any())
            {
                // Unlike activate, no target just means next action
                Nextaction();
                return;
            }

            var closest = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (notTheClosest)
            {
                closest = targets.OrderByDescending(t => t.Distance).FirstOrDefault();
            }

            if (closest != null)
            {
                // Move to the target
                if (closest.Orbit(NavigateOnGrid.OrbitDistance))
                {
                    Logging.Logging.Log("Setting [" + closest.Name + "][" + closest.MaskedId + "][" + Math.Round(closest.Distance / 1000, 0) + "k away as the Orbit Target]");
                    Nextaction();
                    return;
                }
            }
            else
            {
                Nextaction();
                return;
            }

            return;
        }

        private void MoveToBackgroundAction(Action action)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

//			if(DateTime.UtcNow < Time.Instance.LastApproachAction.AddSeconds(20)) {
//				Nextaction();
//				return;
//			}
//
            Logging.Logging.Log("MoveToBackground was called.");

            //we cant move in bastion mode, do not try
            var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
            if (bastionModules.Any(i => i.IsActive))
            {
                Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Activate until bastion deactivates");
                _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(15);
                return;
            }

            if (Cache.Instance.NormalApproach)
            {
                Cache.Instance.NormalApproach = false;
            }

            Cache.Instance.normalNav = false;

            int DistanceToApproach;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToApproach))
            {
                DistanceToApproach = (int) Distances.GateActivationRange;
            }

            var target = action.GetParameterValue("target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
            {
                target = "Acceleration Gate";
            }

            IEnumerable<EntityCache> targets = Cache.Instance.EntitiesByName(target, Cache.Instance.EntitiesOnGrid).ToList();
            if (!targets.Any())
            {
                // Unlike activate, no target just means next action
                Nextaction();
                return;
            }

            var closest = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (closest != null)
            {
                // Move to the target
                if (closest.KeepAtRange(DistanceToApproach))
                {
                    Logging.Logging.Log("Approaching target [" + closest.Name + "][" + closest.MaskedId + "][" + Math.Round(closest.Distance / 1000, 0) + "k away]");
                    Nextaction();
                    _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(5);
                    return;
                }

                return;
            }

            return;
        }

        private void MoveToAction(Action action)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            //we cant move in bastion mode, do not try
            var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
            if (bastionModules.Any(i => i.IsActive))
            {
                Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Activate until bastion deactivates");
                _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(15);
                return;
            }

            if (Cache.Instance.NormalApproach)
            {
                Cache.Instance.NormalApproach = false;
            }

            Cache.Instance.normalNav = false;

            var target = action.GetParameterValue("target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
            {
                target = "Acceleration Gate";
            }

            int DistanceToApproach;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToApproach))
            {
                DistanceToApproach = (int) Distances.GateActivationRange;
            }

            bool stopWhenTargeted;
            if (!bool.TryParse(action.GetParameterValue("StopWhenTargeted"), out stopWhenTargeted))
            {
                stopWhenTargeted = false;
            }

            bool stopWhenAggressed;
            if (!bool.TryParse(action.GetParameterValue("StopWhenAggressed"), out stopWhenAggressed))
            {
                stopWhenAggressed = false;
            }

            bool orderDescending;
            if (!bool.TryParse(action.GetParameterValue("OrderDescending"), out orderDescending))
            {
                orderDescending = false;
            }

            var targets = new List<EntityCache>();
            if (Cache.Instance.EntitiesOnGrid != null && Cache.Instance.EntitiesOnGrid.Any())
            {
                //Logging.Log("CombatMissionCtrl[" + PocketNumber + "]." + _pocketActions[_currentAction], "Looking for Target [" + target + "] in List of Entities On Grid. EntitiesOnGrid.Count [" + Cache.Instance.EntitiesOnGrid.Count() + "]", Logging.Debug);
                if (Cache.Instance.EntitiesByName(target, Cache.Instance.EntitiesOnGrid) != null &&
                    Cache.Instance.EntitiesByName(target, Cache.Instance.EntitiesOnGrid).Any())
                {
                    targets = Cache.Instance.EntitiesByName(target, Cache.Instance.EntitiesOnGrid).ToList();
                }
            }

            if (!targets.Any())
            {
                Logging.Logging.Log("no entities found named [" + target + "] proceeding to next action");
                Nextaction();
                return;
            }

            var moveToTarget = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (orderDescending)
            {
                Logging.Logging.Log(" moveTo: orderDescending == true");
                moveToTarget = targets.OrderByDescending(t => t.Distance).FirstOrDefault();
            }

            //if (Settings.Instance.TargetSelectionMethod == "isdp")
            //{
            Combat.Combat.GetBestPrimaryWeaponTarget(Combat.Combat.MaxRange, false, "Combat");
            //}
            //else //use new target selection method
            //{
            //    Cache.Instance.__GetBestWeaponTargets(Combat.MaxRange);
            //}

            if (moveToTarget != null)
            {
                if (stopWhenTargeted)
                {
                    if (Combat.Combat.TargetedBy != null && Combat.Combat.TargetedBy.Any())
                    {
                        if (Cache.Instance.Approaching != null)
                        {
                            if (Cache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                            {
                                NavigateOnGrid.StopMyShip();
                                Logging.Logging.Log("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                    Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                                Nextaction();
                            }
                        }
                    }
                }

                if (stopWhenAggressed)
                {
                    if (Combat.Combat.Aggressed.Any(t => !t.IsSentry))
                    {
                        if (Cache.Instance.Approaching != null)
                        {
                            if (Cache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                            {
                                NavigateOnGrid.StopMyShip();
                                Logging.Logging.Log("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                    Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                                Nextaction();
                            }
                        }
                    }
                }

                if (moveToTarget.Distance < DistanceToApproach) // if we are inside the range that we are supposed to approach assume we are done
                {
                    Logging.Logging.Log("We are [" + Math.Round(moveToTarget.Distance, 0) + "] from a [" + target + "] we do not need to go any further");
                    Nextaction();

                    if (Cache.Instance.Approaching != null)
                    {
                        if (Cache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > Time.Instance.NextApproachAction)
                        {
                            NavigateOnGrid.StopMyShip();
                            Logging.Logging.Log("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                        }
                    }

                    //if (Settings.Instance.SpeedTank)
                    //{
                    //    //this should at least keep speed tanked ships from going poof if a mission XML uses moveto
                    //    closest.Orbit(Cache.Instance.OrbitDistance);
                    //    Logging.Log("CombatMissionCtrl","MoveTo: Initiating orbit after reaching target")
                    //}
                    return;
                }

                if (moveToTarget.Distance < (int) Distances.WarptoDistance) // if we are inside warpto range you need to approach (you cant warp from here)
                {
                    if (Logging.Logging.DebugMoveTo)
                        Logging.Logging.Log("if (closest.Distance < (int)Distances.WarptoDistance)] -  NextApproachAction [" + Time.Instance.NextApproachAction + "]");

                    // Move to the target

                    if (Logging.Logging.DebugMoveTo)
                        if (Cache.Instance.Approaching == null)
                            Logging.Logging.Log("if (Cache.Instance.Approaching == null)");
                    if (Logging.Logging.DebugMoveTo)
                        if (Cache.Instance.Approaching != null)
                            Logging.Logging.Log("Cache.Instance.Approaching.Id [" + Cache.Instance.Approaching.Id + "]");
                    if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != moveToTarget.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                    {
                        if (moveToTarget.Approach())
                        {
                            Logging.Logging.Log("Approaching target [" + moveToTarget.Name + "][" + moveToTarget.MaskedId + "][" + Math.Round(moveToTarget.Distance / 1000, 0) +
                                "k away]");
                            _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(5);
                            return;
                        }

                        return;
                    }
                    if (Logging.Logging.DebugMoveTo)
                        if (Cache.Instance.Approaching != null) Logging.Logging.Log("-----------");
                    return;
                }

                // Probably never happens
                if (moveToTarget.AlignTo())
                {
                    Logging.Logging.Log("Aligning to target [" + moveToTarget.Name + "][" + moveToTarget.MaskedId + "][" + Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                    _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(5);
                    return;
                }

                return;
            }

            return;
        }

        private void WaitUntilTargeted(Action action)
        {
            var targetedBy = Combat.Combat.TargetedBy;
            if (targetedBy != null && targetedBy.Any())
            {
                Logging.Logging.Log("We have been targeted!");

                // We have been locked, go go go ;)
                _waiting = false;
                Nextaction();
                return;
            }

            // Default timeout is 30 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
            {
                timeout = 30;
            }

            if (_waiting)
            {
                if (DateTime.UtcNow < _waitingSince.AddSeconds(timeout))
                {
                    //
                    // Logging.Log("CombatMissionCtrl[" + PocketNumber + "]." + _pocketActions[_currentAction], "Still WaitingUntilTargeted...", Logging.Debug);
                    //
                    return;
                }

                Logging.Logging.Log("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                Nextaction();
                return;
            }

            Logging.Logging.Log("Nothing has us targeted yet: waiting up to [ " + timeout + "sec] starting now.");
            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
            return;
        }

        private void WaitUntilAggressed(Action action)
        {
            // Default timeout is 60 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
            {
                timeout = 60;
            }

            int WaitUntilShieldsAreThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilShieldsAreThisLow"), out WaitUntilShieldsAreThisLow))
            {
                MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilShieldsAreThisLow;
            }

            int WaitUntilArmorIsThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilArmorIsThisLow"), out WaitUntilArmorIsThisLow))
            {
                MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilArmorIsThisLow;
            }

            if (_waiting)
            {
                if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds < timeout)
                {
                    return;
                }

                Logging.Logging.Log("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                Nextaction();
                return;
            }

            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
            return;
        }

        private void ActivateBastionAction(Action action)
        {
            var _done = false;

            if (Cache.Instance.Modules.Any())
            {
                var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                if (!bastionModules.Any() || bastionModules.Any(i => i.IsActive))
                {
                    _done = true;
                }
            }
            else
            {
                Logging.Logging.Log("no bastion modules fitted!");
                _done = true;
            }

            if (_done)
            {
                Logging.Logging.Log("ActivateBastion Action completed.");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                Nextaction();
                return;
            }

            // Default timeout is 60 seconds
            int DeactivateAfterSeconds;
            if (!int.TryParse(action.GetParameterValue("DeactivateAfterSeconds"), out DeactivateAfterSeconds))
            {
                DeactivateAfterSeconds = 5;
            }

            Time.Instance.NextBastionModeDeactivate = DateTime.UtcNow.AddSeconds(DeactivateAfterSeconds);

            DeactivateIfNothingTargetedWithinRange = false;
            if (!bool.TryParse(action.GetParameterValue("DeactivateIfNothingTargetedWithinRange"), out DeactivateIfNothingTargetedWithinRange))
            {
                DeactivateIfNothingTargetedWithinRange = false;
            }

            // Start bastion mode
            if (!Combat.Combat.ActivateBastion(true)) return;
            return;
        }

        private void DebuggingWait(Action action)
        {
            // Default timeout is 1200 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
            {
                timeout = 1200;
            }

            if (_waiting)
            {
                if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds < timeout)
                {
                    return;
                }

                Logging.Logging.Log("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                Nextaction();
                return;
            }

            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
            return;
        }

        private void AggroOnlyAction(Action action)
        {
            if (Cache.Instance.NormalApproach)
            {
                Cache.Instance.NormalApproach = false;
            }

            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            if (DistanceToClear != 0 && DistanceToClear != -2147483648 && DistanceToClear != 2147483647)
            {
                DistanceToClear = (int) Distances.OnGridWithMe;
            }

            //
            // the important bit is here... Adds target to the PrimaryWeapon or Drone Priority Target Lists so that they get killed (we basically wait for combat.cs to do that before proceeding)
            //
            //if (Settings.Instance.TargetSelectionMethod == "isdp")
            //{
            if (Combat.Combat.GetBestPrimaryWeaponTarget(DistanceToClear, false, "combat", Combat.Combat.combatTargets.Where(t => t.IsTargetedBy).ToList()))
                _clearPocketTimeout = null;
            //}
            //else //use new target selection method
            //{
            //    if (Cache.Instance.__GetBestWeaponTargets(DistanceToClear, Combat.combatTargets.Where(t => t.IsTargetedBy).ToList()).Any())
            //        _clearPocketTimeout = null;
            //}

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue)
            {
                _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);
            }

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value)
            {
                return;
            }

            Logging.Logging.Log("is complete: no more targets that are targeting us");
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        private void AddWarpScramblerByNameAction(Action action)
        {
            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
            {
                notTheClosest = false;
            }

            int numberToIgnore;
            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out numberToIgnore))
            {
                numberToIgnore = 0;
            }

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (!targetNames.Any())
            {
                Logging.Logging.Log("No targets defined in AddWarpScramblerByName action!");
                Nextaction();
                return;
            }

            Combat.Combat.AddWarpScramblerByName(targetNames.FirstOrDefault(), numberToIgnore, notTheClosest);

            //
            // this action is passive and only adds things to the WarpScramblers list )before they have a chance to scramble you, so you can target them early
            //
            Nextaction();
            return;
        }

        private void AddEcmNpcByNameAction(Action action)
        {
            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
            {
                notTheClosest = false;
            }

            int numberToIgnore;
            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out numberToIgnore))
            {
                numberToIgnore = 0;
            }

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (!targetNames.Any())
            {
                Logging.Logging.Log("No targets defined in AddWarpScramblerByName action!");
                Nextaction();
                return;
            }

            Combat.Combat.AddWarpScramblerByName(targetNames.FirstOrDefault(), numberToIgnore, notTheClosest);

            //
            // this action is passive and only adds things to the WarpScramblers list )before they have a chance to scramble you, so you can target them early
            //
            Nextaction();
            return;
        }

        private void AddWebifierByNameAction(Action action)
        {
            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
            {
                notTheClosest = false;
            }

            int numberToIgnore;
            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out numberToIgnore))
            {
                numberToIgnore = 0;
            }

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (!targetNames.Any())
            {
                Logging.Logging.Log("No targets defined in AddWebifierByName action!");
                Nextaction();
                return;
            }

            Combat.Combat.AddWebifierByName(targetNames.FirstOrDefault(), numberToIgnore, notTheClosest);

            //
            // this action is passive and only adds things to the WarpScramblers list )before they have a chance to scramble you, so you can target them early
            //
            Nextaction();
            return;
        }

        private void KillAction(Action action)
        {
            if (Cache.Instance.NormalApproach) Cache.Instance.NormalApproach = false;

            bool ignoreAttackers;
            if (!bool.TryParse(action.GetParameterValue("ignoreattackers"), out ignoreAttackers))
            {
                ignoreAttackers = false;
            }

            bool breakOnAttackers;
            if (!bool.TryParse(action.GetParameterValue("breakonattackers"), out breakOnAttackers))
            {
                breakOnAttackers = false;
            }

            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
            {
                notTheClosest = false;
            }

            int numberToIgnore;
            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out numberToIgnore))
            {
                numberToIgnore = 0;
            }

            int attackUntilBelowShieldPercentage;
            if (!int.TryParse(action.GetParameterValue("attackUntilBelowShieldPercentage"), out attackUntilBelowShieldPercentage))
            {
                attackUntilBelowShieldPercentage = 0;
            }

            int attackUntilBelowArmorPercentage;
            if (!int.TryParse(action.GetParameterValue("attackUntilBelowArmorPercentage"), out attackUntilBelowArmorPercentage))
            {
                attackUntilBelowArmorPercentage = 0;
            }

            int attackUntilBelowHullPercentage;
            if (!int.TryParse(action.GetParameterValue("attackUntilBelowHullPercentage"), out attackUntilBelowHullPercentage))
            {
                attackUntilBelowHullPercentage = 0;
            }

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (!targetNames.Any())
            {
                Logging.Logging.Log("No targets defined in kill action!");
                Nextaction();
                return;
            }

            if (Logging.Logging.DebugKillAction)
            {
                var targetNameCount = 0;
                foreach (var targetName in targetNames)
                {
                    targetNameCount++;
                    Logging.Logging.Log("targetNames [" + targetNameCount + "][" + targetName + "]");
                }
            }

            var killTargets = Cache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderBy(t => t.Nearest5kDistance).ToList();

            if (notTheClosest)
                killTargets = Cache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderByDescending(t => t.Nearest5kDistance).ToList();

            if (!killTargets.Any() || killTargets.Count() <= numberToIgnore)
            {
                Logging.Logging.Log("All targets killed " + targetNames.Aggregate((current, next) => current + "[" + next + "] NumToIgnore [" + numberToIgnore + "]"));

                // We killed it/them !?!?!? :)
                IgnoreTargets.RemoveWhere(targetNames.Contains);
                if (ignoreAttackers)
                {
                    //
                    // UNIgnore attackers when kill is done.
                    //
                    foreach (var target in Combat.Combat.PotentialCombatTargets.Where(e => !targetNames.Contains(e.Name)))
                    {
                        if (target.IsTargetedBy && target.IsAttacking)
                        {
                            Logging.Logging.Log("UN-Ignoring [" + target.Name + "][" + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) +
                                "k away] due to ignoreAttackers parameter (and kill action being complete)");
                            IgnoreTargets.Remove(target.Name.Trim());
                        }
                    }
                }
                Nextaction();
                return;
            }

            if (ignoreAttackers)
            {
                foreach (var target in Combat.Combat.PotentialCombatTargets.Where(e => !targetNames.Contains(e.Name)))
                {
                    if (target.IsTargetedBy && target.IsAttacking)
                    {
                        Logging.Logging.Log("Ignoring [" + target.Name + "][" + target.MaskedId + "][" + Math.Round(target.Distance / 1000, 0) +
                            "k away] due to ignoreAttackers parameter");
                        IgnoreTargets.Add(target.Name.Trim());
                    }
                }
            }

            if (breakOnAttackers &&
                Combat.Combat.TargetedBy.Count(
                    t => (!t.IsSentry || (t.IsSentry && Combat.Combat.KillSentries) || (t.IsSentry && t.IsEwarTarget)) && !t.IsIgnored) >
                killTargets.Count(e => e.IsTargetedBy))
            {
                //
                // We are being attacked, break the kill order
                // which involves removing the named targets as PrimaryWeaponPriorityTargets, PreferredPrimaryWeaponTarget, DronePriorityTargets, and PreferredDroneTarget
                //
                Logging.Logging.Log("Breaking off kill order, new spawn has arrived!");
                targetNames.ForEach(t => IgnoreTargets.Add(t));

                if (killTargets.Any())
                {
                    Combat.Combat.RemovePrimaryWeaponPriorityTargets(killTargets.ToList());

                    if (Combat.Combat.PreferredPrimaryWeaponTarget != null && killTargets.Any(i => i.Name == Combat.Combat.PreferredPrimaryWeaponTarget.Name))
                    {
                        var PreferredPrimaryWeaponTargetsToRemove = killTargets.Where(i => i.Name == Combat.Combat.PreferredPrimaryWeaponTarget.Name).ToList();
                        Combat.Combat.RemovePrimaryWeaponPriorityTargets(PreferredPrimaryWeaponTargetsToRemove);
                        if (Drones.UseDrones)
                        {
                            Drones.RemoveDronePriorityTargets(PreferredPrimaryWeaponTargetsToRemove);
                        }
                    }

                    if (Combat.Combat.PreferredPrimaryWeaponTargetID != null)
                    {
                        foreach (var killTarget in killTargets.Where(e => e.Id == Combat.Combat.PreferredPrimaryWeaponTargetID))
                        {
                            if (Combat.Combat.PreferredPrimaryWeaponTargetID == null) continue;
                            Logging.Logging.Log("Breaking Kill Order in: [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" +
                                Combat.Combat.PreferredPrimaryWeaponTarget.MaskedId + "]");
                            Combat.Combat.PreferredPrimaryWeaponTarget = null;
                        }
                    }

                    if (Drones.PreferredDroneTargetID != null)
                    {
                        foreach (var killTarget in killTargets.Where(e => e.Id == Drones.PreferredDroneTargetID))
                        {
                            if (Drones.PreferredDroneTargetID == null) continue;
                            Logging.Logging.Log("Breaking Kill Order in: [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" +
                                Drones.PreferredDroneTarget.MaskedId + "]");
                            Drones.PreferredDroneTarget = null;
                        }
                    }
                }


                foreach (var KillTargetEntity in Cache.Instance.Targets.Where(e => targetNames.Contains(e.Name) && (e.IsTarget || e.IsTargeting)))
                {
                    if (Combat.Combat.PreferredPrimaryWeaponTarget != null)
                    {
                        if (KillTargetEntity.Id == Combat.Combat.PreferredPrimaryWeaponTarget.Id)
                        {
                            continue;
                        }
                    }

                    Logging.Logging.Log("Unlocking [" + KillTargetEntity.Name + "][" + KillTargetEntity.MaskedId + "][" + Math.Round(KillTargetEntity.Distance / 1000, 0) +
                        "k away] due to kill order being put on hold");
                    KillTargetEntity.UnlockTarget("CombatMissionCtrl");
                }
            }
            else //Do not break aggression on attackers (attack normally)
            {
                //
                // check to see if we have priority targets (ECM, warp scramblers, etc, and let combat process those first)
                //
                EntityCache primaryWeaponPriorityTarget = null;
                if (Combat.Combat.PrimaryWeaponPriorityEntities.Any())
                {
                    try
                    {
                        primaryWeaponPriorityTarget = Combat.Combat.PrimaryWeaponPriorityEntities.Where(p => p.Distance < Combat.Combat.MaxRange
                                                                                                             && p.IsReadyToShoot
                                                                                                             && p.IsOnGridWithMe
                                                                                                             &&
                                                                                                             ((!p.IsNPCFrigate && !p.IsFrigate) ||
                                                                                                              (!Drones.UseDrones &&
                                                                                                               !p.IsTooCloseTooFastTooSmallToHit)))
                            .OrderByDescending(pt => pt.IsTargetedBy)
                            .ThenByDescending(pt => pt.IsInOptimalRange)
                            .ThenByDescending(pt => pt.IsEwarTarget)
                            .ThenBy(pt => pt.PrimaryWeaponPriorityLevel)
                            .ThenBy(pt => pt.Distance)
                            .FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Logging.Logging.Log("Exception [" + ex + "]");
                    }
                }

                if (primaryWeaponPriorityTarget != null && primaryWeaponPriorityTarget.IsOnGridWithMe)
                {
                    if (Logging.Logging.DebugKillAction)
                    {
                        if (Combat.Combat.PrimaryWeaponPriorityTargets.Any())
                        {
                            var icount = 0;
                            foreach (var primaryWeaponPriorityEntity in Combat.Combat.PrimaryWeaponPriorityEntities.Where(i => i.IsOnGridWithMe))
                            {
                                icount++;
                                if (Logging.Logging.DebugKillAction)
                                    Logging.Logging.Log("[" + icount + "] PrimaryWeaponPriorityTarget Named [" + primaryWeaponPriorityEntity.Name + "][" +
                                        primaryWeaponPriorityEntity.MaskedId + "][" + Math.Round(primaryWeaponPriorityEntity.Distance / 1000, 0) + "k away]");
                                continue;
                            }
                        }
                    }
                    //
                    // GetBestTarget below will choose to assign PriorityTargets over preferred targets, so we might as well wait... (and not approach the wrong target)
                    //
                }
                else
                {
                    //
                    // then proceed to kill the target
                    //
                    IgnoreTargets.RemoveWhere(targetNames.Contains);

                    if (killTargets.FirstOrDefault() != null) //if it is not null is HAS to be OnGridWithMe as all killTargets are verified OnGridWithMe
                    {
                        if (attackUntilBelowShieldPercentage > 0 && (killTargets.FirstOrDefault().ShieldPct*100) < attackUntilBelowShieldPercentage)
                        {
                            Logging.Logging.Log("Kill target [" + killTargets.FirstOrDefault().Name + "] at [" + Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) +
                                "k] Armor % is [" + killTargets.FirstOrDefault().ShieldPct * 100 + "] which is less then attackUntilBelowShieldPercentage [" +
                                attackUntilBelowShieldPercentage + "] Kill Action Complete, Next Action.");
                            Combat.Combat.RemovePrimaryWeaponPriorityTargets(killTargets);
                            Combat.Combat.PreferredPrimaryWeaponTarget = null;
                            Nextaction();
                            return;
                        }

                        if (attackUntilBelowArmorPercentage > 0 && (killTargets.FirstOrDefault().ArmorPct*100) < attackUntilBelowArmorPercentage)
                        {
                            Logging.Logging.Log("Kill target [" + killTargets.FirstOrDefault().Name + "] at [" + Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) +
                                "k] Armor % is [" + killTargets.FirstOrDefault().ArmorPct * 100 + "] which is less then attackUntilBelowArmorPercentage [" +
                                attackUntilBelowArmorPercentage + "] Kill Action Complete, Next Action.");
                            Combat.Combat.RemovePrimaryWeaponPriorityTargets(killTargets);
                            Combat.Combat.PreferredPrimaryWeaponTarget = null;
                            Nextaction();
                            return;
                        }

                        if (attackUntilBelowHullPercentage > 0 && (killTargets.FirstOrDefault().ArmorPct*100) < attackUntilBelowHullPercentage)
                        {
                            Logging.Logging.Log("Kill target [" + killTargets.FirstOrDefault().Name + "] at [" + Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) +
                                "k] Armor % is [" + killTargets.FirstOrDefault().StructurePct * 100 + "] which is less then attackUntilBelowHullPercentage [" +
                                attackUntilBelowHullPercentage + "] Kill Action Complete, Next Action.");
                            Combat.Combat.RemovePrimaryWeaponPriorityTargets(killTargets);
                            Combat.Combat.PreferredPrimaryWeaponTarget = null;
                            Nextaction();
                            return;
                        }

                        if (Logging.Logging.DebugKillAction)
                            Logging.Logging.Log(" proceeding to kill [" + killTargets.FirstOrDefault().Name + "] at [" +
                                Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) + "k] (this is spammy, but useful debug info)");
                        //if (Combat.PreferredPrimaryWeaponTarget == null || String.IsNullOrEmpty(Cache.Instance.PreferredDroneTarget.Name) || Combat.PreferredPrimaryWeaponTarget.IsOnGridWithMe && Combat.PreferredPrimaryWeaponTarget != currentKillTarget)
                        //{
                        //Logging.Log("CombatMissionCtrl[" + PocketNumber + "]." + _pocketActions[_currentAction], "Adding [" + currentKillTarget.Name + "][" + Math.Round(currentKillTarget.Distance / 1000, 0) + "][" + Cache.Instance.MaskedID(currentKillTarget.Id) + "] groupID [" + currentKillTarget.GroupId + "] TypeID[" + currentKillTarget.TypeId + "] as PreferredPrimaryWeaponTarget", Logging.Teal);
                        Combat.Combat.AddPrimaryWeaponPriorityTarget(killTargets.FirstOrDefault(), PrimaryWeaponPriority.PriorityKillTarget,
                            "CombatMissionCtrl.Kill[" + PocketNumber + "]." + _pocketActions[_currentAction]);
                        Combat.Combat.PreferredPrimaryWeaponTarget = killTargets.FirstOrDefault();
                        //}
                        //else
                        if (Logging.Logging.DebugKillAction)
                        {
                            if (Logging.Logging.DebugKillAction)
                                Logging.Logging.Log("Combat.PreferredPrimaryWeaponTarget =[ " + Combat.Combat.PreferredPrimaryWeaponTarget.Name + " ][" +
                                    Combat.Combat.PreferredPrimaryWeaponTarget.MaskedId + "]");

                            if (Combat.Combat.PrimaryWeaponPriorityTargets.Any())
                            {
                                if (Logging.Logging.DebugKillAction)
                                    Logging.Logging.Log("PrimaryWeaponPriorityTargets Below (if any)");
                                var icount = 0;
                                foreach (var PT in Combat.Combat.PrimaryWeaponPriorityEntities)
                                {
                                    icount++;
                                    if (Logging.Logging.DebugKillAction)
                                        Logging.Logging.Log("PriorityTarget [" + icount + "] [ " + PT.Name + " ][" + PT.MaskedId + "] IsOnGridWithMe [" + PT.IsOnGridWithMe +
                                            "]");
                                }
                                if (Logging.Logging.DebugKillAction)
                                    Logging.Logging.Log("PrimaryWeaponPriorityTargets Above (if any)");
                            }
                        }

                        EntityCache NavigateTowardThisTarget = null;
                        if (Combat.Combat.PreferredPrimaryWeaponTarget != null)
                        {
                            NavigateTowardThisTarget = Combat.Combat.PreferredPrimaryWeaponTarget;
                        }
                        if (Combat.Combat.PreferredPrimaryWeaponTarget != null)
                        {
                            NavigateTowardThisTarget = killTargets.FirstOrDefault();
                        }
                        //we may need to get closer so combat will take over
                        if (NavigateTowardThisTarget.Distance > Combat.Combat.MaxRange || !NavigateTowardThisTarget.IsInOptimalRange)
                        {
                            if (Logging.Logging.DebugKillAction)
                                Logging.Logging.Log("if (Combat.PreferredPrimaryWeaponTarget.Distance > Combat.MaxRange)");
                            //if (!Cache.Instance.IsApproachingOrOrbiting(Combat.PreferredPrimaryWeaponTarget.Id))
                            //{
                            //    if (Logging.DebugKillAction) Logging.Log("CombatMissionCtrl[" + PocketNumber + "]." + _pocketActions[_currentAction], "if (!Cache.Instance.IsApproachingOrOrbiting(Combat.PreferredPrimaryWeaponTarget.Id))", Logging.Debug);
                            NavigateOnGrid.NavigateIntoRange(NavigateTowardThisTarget, "combatMissionControl", true);
                            //}
                        }
                    }
                }

                if (Combat.Combat.PreferredPrimaryWeaponTarget != killTargets.FirstOrDefault())
                {
                    // GetTargets
                    //if (Settings.Instance.TargetSelectionMethod == "isdp")
                    //{
                    Combat.Combat.GetBestPrimaryWeaponTarget(Combat.Combat.MaxRange, false, "Combat");
                    //}
                    //else //use new target selection method
                    //{
                    //    Cache.Instance.__GetBestWeaponTargets(Combat.MaxRange);
                    //}
                }
            }

            // Don't use NextAction here, only if target is killed (checked further up)
            return;
        }

        private void UseDrones(Action action)
        {
            bool usedrones;
            if (!bool.TryParse(action.GetParameterValue("use"), out usedrones))
            {
                usedrones = false;
            }

            if (!usedrones)
            {
                Logging.Logging.Log("Disable launch of drones");
                MissionSettings.PocketUseDrones = false;
            }
            else
            {
                Logging.Logging.Log("Enable launch of drones");
                MissionSettings.PocketUseDrones = true;
            }
            Nextaction();
            return;
        }

        private void KillClosestByNameAction(Action action)
        {
            if (Cache.Instance.NormalApproach) Cache.Instance.NormalApproach = false;

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (targetNames.Count == 0)
            {
                Logging.Logging.Log("No targets defined!");
                Nextaction();
                return;
            }

            //
            // the way this is currently written is will NOT stop after killing the first target as intended, it will clear all targets with the Name given
            //

            Combat.Combat.AddPrimaryWeaponPriorityTarget(
                Combat.Combat.PotentialCombatTargets.Where(t => targetNames.Contains(t.Name)).OrderBy(t => t.Distance).Take(1).FirstOrDefault(),
                PrimaryWeaponPriority.PriorityKillTarget, "CombatMissionCtrl.KillClosestByName");

            //if (Settings.Instance.TargetSelectionMethod == "isdp")
            //{
            if (Combat.Combat.GetBestPrimaryWeaponTarget((double) Distances.OnGridWithMe, false, "combat",
                Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).Take(1).ToList()))
                _clearPocketTimeout = null;
            //}
            //else //use new target selection method
            //{
            //    if (Cache.Instance.__GetBestWeaponTargets((double)Distances.OnGridWithMe, Combat.PotentialCombatTargets.Where(e => !e.IsSentry || (e.IsSentry && Settings.Instance.KillSentries)).OrderBy(t => t.Distance).Take(1).ToList()).Any())
            //        _clearPocketTimeout = null;
            //}

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        private void KillClosestAction(Action action)
        {
            if (Cache.Instance.NormalApproach) Cache.Instance.NormalApproach = false;

            //
            // the way this is currently written is will NOT stop after killing the first target as intended, it will clear all targets with the Name given, in this everything on grid
            //

            //if (Settings.Instance.TargetSelectionMethod == "isdp")
            //{
            if (Combat.Combat.GetBestPrimaryWeaponTarget((double) Distances.OnGridWithMe, false, "combat",
                Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).Take(1).ToList()))
                _clearPocketTimeout = null;
            //}
            //else //use new target selection method
            //{
            //    if (Cache.Instance.__GetBestWeaponTargets((double)Distances.OnGridWithMe, Combat.PotentialCombatTargets.Where(e => !e.IsSentry || (e.IsSentry && Settings.Instance.KillSentries)).OrderBy(t => t.Distance).Take(1).ToList()).Any())
            //        _clearPocketTimeout = null;
            //}

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        private void DropItemAction(Action action)
        {
            try
            {
                //Cache.Instance.DropMode = true;
                var items = action.GetParameterValues("item");
                var targetName = action.GetParameterValue("target");

                int quantity;
                if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
                {
                    quantity = 1;
                }

                if (!CargoHoldHasBeenStacked)
                {
                    Logging.Logging.Log("Stack CargoHold");
                    if (!Cache.Instance.StackCargoHold("DropItem")) return;
                    CargoHoldHasBeenStacked = true;
                    return;
                }

                IEnumerable<EntityCache> targetEntities = Cache.Instance.EntitiesByName(targetName, Cache.Instance.EntitiesOnGrid).ToList();
                if (targetEntities.Any())
                {
                    Logging.Logging.Log("We have [" + targetEntities.Count() + "] entities on grid that match our target by name: [" + targetName.FirstOrDefault() + "]");
                    targetEntities = targetEntities.Where(i => i.IsContainer || i.GroupId == (int) Group.LargeColidableObject);
                        //some missions (like: Onslaught - lvl1) have LCOs that can hold and take cargo, note that same mission has a LCS with the same name!

                    if (!targetEntities.Any())
                    {
                        Logging.Logging.Log("No entity on grid named: [" + targetEntities.FirstOrDefault() + "] that is also a container");

                        // now that we have completed this action revert OpenWrecks to false
                        //Cache.Instance.DropMode = false;
                        Nextaction();
                        return;
                    }

                    var closest = targetEntities.OrderBy(t => t.Distance).FirstOrDefault();

                    if (closest == null)
                    {
                        Logging.Logging.Log("closest: target named [" + targetName.FirstOrDefault() + "] was null" + targetEntities);

                        // now that we have completed this action revert OpenWrecks to false
                        //Cache.Instance.DropMode = false;
                        Nextaction();
                        return;
                    }

                    if (closest.Distance > (int) Distances.SafeScoopRange)
                    {
                        if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                        {
                            if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closest.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                            {
                                if (closest.KeepAtRange(1000))
                                {
                                    Logging.Logging.Log("Approaching target [" + closest.Name + "][" + closest.MaskedId + "] which is at [" +
                                        Math.Round(closest.Distance / 1000, 0) + "k away]");
                                }
                            }
                        }
                    }
                    else if (Cache.Instance.MyShipEntity.Velocity < 50) //nearly stopped
                    {
                        if (DateTime.UtcNow > Time.Instance.NextOpenContainerInSpaceAction)
                        {
                            DirectContainer containerWeWillDropInto = null;

                            containerWeWillDropInto = Cache.Instance.DirectEve.GetContainer(closest.Id);
                            //
                            // the container we are going to drop something into must exist
                            //
                            if (containerWeWillDropInto == null)
                            {
                                Logging.Logging.Log("if (container == null)");
                                return;
                            }

                            //
                            // open the container so we have a window!
                            //
                            if (containerWeWillDropInto.Window == null)
                            {
                                if (closest.OpenCargo())
                                {
                                    Time.Instance.NextOpenContainerInSpaceAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
                                }

                                return;
                            }

                            if (!containerWeWillDropInto.Window.IsReady)
                            {
                                Logging.Logging.Log("LootWrecks: containerWeWillDropInto.Window is not ready, waiting");
                                return;
                            }

                            if (ItemsHaveBeenMoved)
                            {
                                Logging.Logging.Log("We have Dropped the items: ItemsHaveBeenMoved [" + ItemsHaveBeenMoved + "]");
                                // now that we have completed this action revert OpenWrecks to false
                                //Cache.Instance.DropMode = false;
                                Nextaction();
                                return;
                            }

                            //
                            // if we are going to drop something into the can we MUST already have it in our cargohold
                            //
                            if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any())
                            {
                                //int CurrentShipsCargoItemCount = 0;
                                //CurrentShipsCargoItemCount = Cache.Instance.CurrentShipsCargo.Items.Count();

                                //DirectItem itemsToMove = null;
                                //itemsToMove = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeName.ToLower() == items.FirstOrDefault().ToLower());
                                //if (itemsToMove == null)
                                //{
                                //    Logging.Log("MissionController.DropItem", "CurrentShipsCargo has [" + CurrentShipsCargoItemCount + "] items. Item We are supposed to move is: [" + items.FirstOrDefault() + "]", Logging.White);
                                //    return;
                                //}

                                var ItemNumber = 0;
                                foreach (var CurrentShipsCargoItem in Cache.Instance.CurrentShipsCargo.Items)
                                {
                                    ItemNumber++;
                                    Logging.Logging.Log("[" + ItemNumber + "] Found [" + CurrentShipsCargoItem.Quantity + "][" + CurrentShipsCargoItem.TypeName +
                                        "] in Current Ships Cargo: StackSize: [" + CurrentShipsCargoItem.Stacksize + "] We are looking for: [" +
                                        items.FirstOrDefault() + "]");
                                    if (items.Any() && items.FirstOrDefault() != null)
                                    {
                                        var NameOfItemToDropIntoContainer = items.FirstOrDefault();
                                        if (NameOfItemToDropIntoContainer != null)
                                        {
                                            if (CurrentShipsCargoItem.TypeName.ToLower() == NameOfItemToDropIntoContainer.ToLower())
                                            {
                                                Logging.Logging.Log("[" + ItemNumber + "] container.Capacity [" + containerWeWillDropInto.Capacity + "] ItemsHaveBeenMoved [" +
                                                    ItemsHaveBeenMoved + "]");
                                                if (!ItemsHaveBeenMoved)
                                                {
                                                    Logging.Logging.Log("Moving Items: " + items.FirstOrDefault() + " from cargo ship to " + containerWeWillDropInto.TypeName);
                                                    //
                                                    // THIS IS NOT WORKING - EXCEPTION/ERROR IN CLIENT...
                                                    //
                                                    containerWeWillDropInto.Add(CurrentShipsCargoItem, quantity);
                                                    Time.Instance.NextOpenContainerInSpaceAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(4, 6));
                                                    ItemsHaveBeenMoved = true;
                                                    return;
                                                }

                                                return;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Logging.Logging.Log("No Items: Cache.Instance.CurrentShipsCargo.Items.Any()");
                            }
                        }
                    }

                    return;
                }

                Logging.Logging.Log("No entity on grid named: [" + targetEntities.FirstOrDefault() + "]");
                // now that we have completed this action revert OpenWrecks to false
                //Cache.Instance.DropMode = false;
                Nextaction();
                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception: [" + exception + "]");
            }

            return;
        }

        private void LootItemAction(Action action)
        {
            try
            {
                Salvage.CurrentlyShouldBeSalvaging = true;
                Salvage.MissionLoot = true;
                List<string> targetContainerNames = null;
                if (action.GetParameterValues("target") != null)
                {
                    targetContainerNames = action.GetParameterValues("target");
                }

                if ((targetContainerNames == null || !targetContainerNames.Any()) && Salvage.LootItemRequiresTarget)
                {
                    Logging.Logging.Log(" *** No Target Was Specified In the LootItem Action! ***");
                }

                List<string> itemsToLoot = null;
                if (action.GetParameterValues("item") != null)
                {
                    itemsToLoot = action.GetParameterValues("item");
                }

                if (itemsToLoot == null)
                {
                    Logging.Logging.Log(" *** No Item Was Specified In the LootItem Action! ***");
                    Nextaction();
                }

                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                Salvage.OpenWrecks = true;

                int quantity;
                if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
                {
                    quantity = 1;
                }

                if (Cache.Instance.CurrentShipsCargo != null &&
                    Cache.Instance.CurrentShipsCargo.Items.Any(i => itemsToLoot != null && (itemsToLoot.Contains(i.TypeName) && (i.Quantity >= quantity))))
                {
                    Logging.Logging.Log("We are done - we have the item(s)");

                    // now that we have completed this action revert OpenWrecks to false
                    if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                    {
                        if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }


                    Salvage.MissionLoot = false;
                    Salvage.CurrentlyShouldBeSalvaging = false;
                    Nextaction();
                    return;
                }

                //
                // we re-sot by distance on every pulse. The order will be potentially different on each pulse as we move around the field. this is ok and desirable.
                //
                var containers =
                    Cache.Instance.Containers.Where(e => !Cache.Instance.LootedContainers.Contains(e.Id))
                        .OrderByDescending(e => e.GroupId == (int) Group.CargoContainer)
                        .ThenBy(e => e.Distance);

                if (!containers.Any())
                {
                    Logging.Logging.Log("no containers left to loot, next action");


                    if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                    {
                        if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }


                    Salvage.MissionLoot = false;
                    Salvage.CurrentlyShouldBeSalvaging = false;
                    Nextaction();
                    return;
                }

                //
                // add containers that we were told to loot into the ListofContainersToLoot so that they are prioritized by the background salvage routine
                //
                if (targetContainerNames != null && targetContainerNames.Any())
                {
                    foreach (var continerToLoot in containers)
                    {
                        if (targetContainerNames.Any())
                        {
                            foreach (var targetContainerName in targetContainerNames)
                            {
                                if (continerToLoot.Name == targetContainerName)
                                {
                                    if (!Cache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                                    {
                                        Cache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);
                                    }
                                }

                                continue;
                            }
                        }
                        else
                        {
                            foreach (var _unlootedcontainer in Cache.Instance.UnlootedContainers)
                            {
                                if (continerToLoot.Name == _unlootedcontainer.Name)
                                {
                                    if (!Cache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                                    {
                                        Cache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);
                                    }
                                }

                                continue;
                            }
                        }

                        continue;
                    }
                }

                if (itemsToLoot != null && itemsToLoot.Any())
                {
                    foreach (var _itemToLoot in itemsToLoot)
                    {
                        if (!Cache.Instance.ListofMissionCompletionItemsToLoot.Contains(_itemToLoot))
                        {
                            Cache.Instance.ListofMissionCompletionItemsToLoot.Add(_itemToLoot);
                        }
                    }
                }

                EntityCache container;
                if (targetContainerNames != null && targetContainerNames.Any())
                {
                    container = containers.FirstOrDefault(c => targetContainerNames.Contains(c.Name));
                }
                else
                {
                    container = containers.FirstOrDefault();
                }

                if (container != null)
                {
                    if (container.Distance > (int) Distances.SafeScoopRange)
                    {
                        if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != container.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                        {
                            if (container.Approach())
                            {
                                Logging.Logging.Log("Approaching target [" + container.Name + "][" + container.MaskedId + "] which is at [" +
                                    Math.Round(container.Distance / 1000, 0) + "k away]");
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception logged was [" + exception + "]");
                return;
            }
        }

        private void SalvageAction(Action action)
        {
            List<string> itemsToLoot = null;
            if (action.GetParameterValues("item") != null)
            {
                itemsToLoot = action.GetParameterValues("item");
            }

            int quantity;
            if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
            {
                quantity = 1;
            }

            if (Cache.Instance.NormalApproach) Cache.Instance.NormalApproach = false;

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore salvage action
            if (targetNames.Count == 0)
            {
                Logging.Logging.Log("No targets defined!");
                Nextaction();
                return;
            }

            if (itemsToLoot == null)
            {
                Logging.Logging.Log(" *** No Item Was Specified In the Salvage Action! ***");
                Nextaction();
            }
            else if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Window.IsReady)
            {
                if (Cache.Instance.CurrentShipsCargo.Items.Any(i => (itemsToLoot.Contains(i.TypeName) && (i.Quantity >= quantity))))
                {
                    Logging.Logging.Log("We are done - we have the item(s)");

                    // now that we have completed this action revert OpenWrecks to false
                    if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                    {
                        if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }

                    Salvage.MissionLoot = false;
                    Salvage.CurrentlyShouldBeSalvaging = false;
                    Nextaction();
                    return;
                }
            }

            IEnumerable<EntityCache> targets = Cache.Instance.EntitiesByName(targetNames.FirstOrDefault(), Cache.Instance.EntitiesOnGrid.ToList()).ToList();
            if (!targets.Any())
            {
                Logging.Logging.Log("no entities found named [" + targets.FirstOrDefault() + "] proceeding to next action");
                Nextaction();
                return;
            }

            if (Combat.Combat.GetBestPrimaryWeaponTarget((double) Distances.OnGridWithMe, false, "combat",
                Combat.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).Take(1).ToList()))
                _clearPocketTimeout = null;

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            //
            // how do we determine success here? we assume the 'reward' for salvaging will appear in your cargo, we also assume the mission action will know what that item is called!
            //

            var closest = targets.OrderBy(t => t.Distance).FirstOrDefault();
            if (closest != null)
            {
                if (!NavigateOnGrid.NavigateToTarget(targets.FirstOrDefault(), "", true, 500)) return;

                if (Salvage.salvagers == null || !Salvage.salvagers.Any())
                {
                    Logging.Logging.Log("this action REQUIRES at least 1 salvager! - you may need to use Mission specific fittings to accomplish this");
                    Logging.Logging.Log("this action REQUIRES at least 1 salvager! - disabling autostart");
                    Logging.Logging.Log("this action REQUIRES at least 1 salvager! - setting CombatMissionsBehaviorState to GotoBase");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    Settings.Instance.AutoStart = false;
                }
                else if (closest.Distance < Salvage.salvagers.Min(s => s.OptimalRange))
                {
                    if (NavigateOnGrid.SpeedTank) Salvage.OpenWrecks = true;
                    Salvage.CurrentlyShouldBeSalvaging = true;
                    Salvage.TargetWrecks(targets);
                    Salvage.ActivateSalvagers(targets);
                }

                return;
            }

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }


        private void LootAction(Action action)
        {
            try
            {
                var items = action.GetParameterValues("item");
                var targetNames = action.GetParameterValues("target");

                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                Salvage.OpenWrecks = true;
                Salvage.CurrentlyShouldBeSalvaging = true;

                if (!Salvage.LootEverything)
                {
                    if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any(i => items.Contains(i.TypeName)))
                    {
                        Logging.Logging.Log("LootEverything:  We are done looting");

                        // now that we are done with this action revert OpenWrecks to false

                        if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                        {
                            if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                            Salvage.OpenWrecks = false;
                        }

                        Salvage.MissionLoot = false;
                        Salvage.CurrentlyShouldBeSalvaging = false;
                        Nextaction();
                        return;
                    }
                }

                // unlock targets count
                Salvage.MissionLoot = true;

                //
                // sorting by distance is bad if we are moving (we'd change targets unpredictably)... sorting by ID should be better and be nearly the same(?!)
                //
                var containers = Cache.Instance.Containers.Where(e => !Cache.Instance.LootedContainers.Contains(e.Id)).OrderBy(e => e.Distance);

                if (Logging.Logging.DebugLootWrecks)
                {
                    var i = 0;
                    foreach (var _container in containers)
                    {
                        i++;
                        Logging.Logging.Log("[" + i + "] " + _container.Name + "[" + Math.Round(_container.Distance / 1000, 0) + "k] isWreckEmpty [" + _container.IsWreckEmpty +
                            "] IsTarget [" + _container.IsTarget + "]");
                    }
                }

                if (!containers.Any())
                {
                    // lock targets count
                    Logging.Logging.Log("We are done looting");

                    // now that we are done with this action revert OpenWrecks to false

                    if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                    {
                        if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }

                    Salvage.MissionLoot = false;
                    Salvage.CurrentlyShouldBeSalvaging = false;
                    Nextaction();
                    return;
                }

                //
                // add containers that we were told to loot into the ListofContainersToLoot so that they are prioritized by the background salvage routine
                //
                if (targetNames != null && targetNames.Any())
                {
                    foreach (var continerToLoot in containers)
                    {
                        if (continerToLoot.Name == targetNames.FirstOrDefault())
                        {
                            if (!Cache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                            {
                                Cache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);
                            }
                        }
                    }
                }

                var container = containers.FirstOrDefault(c => targetNames != null && targetNames.Contains(c.Name)) ?? containers.FirstOrDefault();
                if (container != null)
                {
                    if (container.Distance > (int) Distances.SafeScoopRange)
                    {
                        if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != container.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                        {
                            if (container.Approach())
                            {
                                Logging.Logging.Log("Approaching target [" + container.Name + "][" + container.MaskedId + "][" + Math.Round(container.Distance / 1000, 0) +
                                    "k away]");
                                return;
                            }

                            return;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception logged was [" + exception + "]");
                return;
            }
        }

        private void IgnoreAction(Action action)
        {
            bool clear;
            if (!bool.TryParse(action.GetParameterValue("clear"), out clear))
                clear = false;

            //List<string> removehighestbty = action.GetParameterValues("RemoveHighestBty");
            //List<string> addhighestbty = action.GetParameterValues("AddHighestBty");

            var add = action.GetParameterValues("add");
            var remove = action.GetParameterValues("remove");

            //string targetNames = action.GetParameterValue("target");

            //int distancetoapp;
            //if (!int.TryParse(action.GetParameterValue("distance"), out distancetoapp))
            //    distancetoapp = 1000;

            //IEnumerable<EntityCache> targets = Cache.Instance.Entities.Where(e => targetNames.Contains(e.Name));
            // EntityCache target = targets.OrderBy(t => t.Distance).FirstOrDefault();

            //IEnumerable<EntityCache> targetsinrange = Cache.Instance.Entities.Where(b => Cache.Instance.DistanceFromEntity(b.X ?? 0, b.Y ?? 0, b.Z ?? 0,target) < distancetoapp);
            //IEnumerable<EntityCache> targetsoutofrange = Cache.Instance.Entities.Where(b => Cache.Instance.DistanceFromEntity(b.X ?? 0, b.Y ?? 0, b.Z ?? 0, target) < distancetoapp);

            if (clear)
            {
                IgnoreTargets.Clear();
            }
            else
            {
                add.ForEach(a => IgnoreTargets.Add(a.Trim()));
                remove.ForEach(a => IgnoreTargets.Remove(a.Trim()));
            }
            Logging.Logging.Log("Updated ignore list");
            if (IgnoreTargets.Any())
            {
                Logging.Logging.Log("Currently ignoring: " + IgnoreTargets.Aggregate((current, next) => "[" + current + "][" + next + "]"));
            }
            else
            {
                Logging.Logging.Log("Your ignore list is empty");
            }

            Nextaction();
            return;
        }

        private void PerformAction(Action action)
        {
            switch (action.State)
            {
                case ActionState.LogWhatIsOnGrid:
                    LogWhatIsOnGridAction(action);
                    break;

                case ActionState.Activate:
                    ActivateAction(action);
                    break;

                case ActionState.ClearPocket:
                    ClearPocketAction(action);
                    break;

                case ActionState.ClearAggro:
                    ClearAggroAction(action);
                    break;

                case ActionState.SalvageBookmark:
                    BookmarkPocketForSalvaging();

                    Nextaction();
                    break;

                case ActionState.Done:
                    DoneAction();
                    break;

                case ActionState.AddEcmNpcByName:
                    AddEcmNpcByNameAction(action);
                    break;

                case ActionState.AddWarpScramblerByName:
                    AddWarpScramblerByNameAction(action);
                    break;

                case ActionState.AddWebifierByName:
                    AddWebifierByNameAction(action);
                    break;

                case ActionState.Kill:
                    KillAction(action);
                    break;

                case ActionState.KillOnce:
                    KillAction(action); // TODO Implement
                    break;

                case ActionState.UseDrones:
                    UseDrones(action);
                    break;

                case ActionState.AggroOnly:
                    AggroOnlyAction(action);
                    break;

                case ActionState.KillClosestByName:
                    KillClosestByNameAction(action);
                    break;

                case ActionState.KillClosest:
                    KillClosestAction(action);
                    break;

                case ActionState.MoveTo:
                    MoveToAction(action);
                    break;

                case ActionState.OrbitEntity:
                    OrbitEntityAction(action);
                    break;

                case ActionState.MoveToBackground:
                    MoveToBackgroundAction(action);
                    break;

                case ActionState.ClearWithinWeaponsRangeOnly:
                    ClearWithinWeaponsRangeOnlyAction(action);
                    break;

                case ActionState.ClearWithinWeaponsRangewAggroOnly:
                    ClearWithinWeaponsRangeWithAggroOnlyAction(action);
                    break;

                case ActionState.Salvage:
                    SalvageAction(action);
                    break;

                //case ActionState.Analyze:
                //    AnalyzeAction(action);
                //    break;

                case ActionState.Loot:
                    LootAction(action);
                    break;

                case ActionState.LootItem:
                    LootItemAction(action);
                    break;

                case ActionState.ActivateBastion:
                    ActivateBastionAction(action);
                    break;

                case ActionState.DropItem:
                    DropItemAction(action);
                    break;

                case ActionState.Ignore:
                    IgnoreAction(action);
                    break;

                case ActionState.WaitUntilTargeted:
                    WaitUntilTargeted(action);
                    break;

                case ActionState.WaitUntilAggressed:
                    WaitUntilAggressed(action);
                    break;

                case ActionState.DebuggingWait:
                    DebuggingWait(action);
                    break;
            }
        }

        public static void ReplaceMissionsActions()
        {
            _pocketActions.Clear();

            //
            // Adds actions specified in the Mission XML
            //
            //
            // Clear the Pocket
            _pocketActions.Add(new Action {State = ActionState.ClearPocket});
            _pocketActions.Add(new Action {State = ActionState.ClearPocket});
            _pocketActions.AddRange(LoadMissionActions(Cache.Instance.Agent.AgentId, PocketNumber, true));

            //we manually add 2 ClearPockets above, then we try to load other mission XMLs for this pocket, if we fail Count will be 2 and we know we need to add an activate and/or a done action.
            if (_pocketActions.Count() == 2)
            {
                // Is there a gate?
                if (Cache.Instance.AccelerationGates != null && Cache.Instance.AccelerationGates.Any())
                {
                    // Activate it (Activate action also moves to the gate)
                    _pocketActions.Add(new Action {State = ActionState.Activate});
                    _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                }
                else // No, were done
                {
                    _pocketActions.Add(new Action {State = ActionState.Done});
                }
            }
        }

        public void ProcessState()
        {
            // There is really no combat in stations (yet)
            if (Cache.Instance.InStation || Logging.Logging.DebugDisableCombatMissionCtrl)
                return;

            // if we are not in space yet, wait...
            if (!Cache.Instance.InSpace)
                return;

            // What? No ship entity?
            if (Cache.Instance.ActiveShip.Entity == null)
                return;

            // There is no combat when cloaked
            if (Cache.Instance.ActiveShip.Entity.IsCloaked)
                return;

            switch (_States.CurrentCombatMissionCtrlState)
            {
                case CombatMissionCtrlState.Idle:
                    break;

                case CombatMissionCtrlState.Done:
                    Statistics.WritePocketStatistics();

                    if (!Cache.Instance.NormalApproach)
                        Cache.Instance.NormalApproach = true;

                    IgnoreTargets.Clear();
                    break;

                case CombatMissionCtrlState.Error:
                    break;

                case CombatMissionCtrlState.Start:
                    PocketNumber = 0;

                    // Update statistic values
                    Cache.Instance.WealthatStartofPocket = Cache.Instance.DirectEve.Me.Wealth;
                    Statistics.StartedPocket = DateTime.UtcNow;

                    // Update UseDrones from settings (this can be overridden with a mission action named UseDrones)
                    MissionSettings.MissionUseDrones = null;
                    MissionSettings.PocketUseDrones = null;

                    // Reset notNormalNav and onlyKillAggro to false
                    Cache.Instance.normalNav = true;
                    Cache.Instance.onlyKillAggro = false;

                    // Update x/y/z so that NextPocket wont think we are there yet because its checking (very) old x/y/z cords
                    _lastX = Cache.Instance.ActiveShip.Entity.X;
                    _lastY = Cache.Instance.ActiveShip.Entity.Y;
                    _lastZ = Cache.Instance.ActiveShip.Entity.Z;

                    _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.LoadPocket;
                    break;

                case CombatMissionCtrlState.LoadPocket:
                    _pocketActions.Clear();
                    _pocketActions.AddRange(LoadMissionActions(Cache.Instance.Agent.AgentId, PocketNumber, true));

                    //
                    // LogStatistics();
                    //
//
//					if(Settings.Instance.DisableAutoBackgroundMoveToGate) {
//						Logging.Log("-", "Settings.Instance.DisableAutoBackgroundMoveToGate ==  true", Logging.White);
//					}

                    if (_pocketActions.Count == 0)
                    {
                        // No Pocket action, load default actions
                        Logging.Logging.Log("No mission actions specified, loading default actions");

                        // Wait for 30 seconds to be targeted
                        _pocketActions.Add(new Action {State = ActionState.WaitUntilTargeted});
                        _pocketActions[0].AddParameter("timeout", "15");

                        // Clear the Pocket
                        _pocketActions.Add(new Action {State = ActionState.ClearPocket});


                        _pocketActions.Add(new Action {State = ActionState.Activate});
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                        _pocketActions[_pocketActions.Count - 1].AddParameter("optional", "true");

                        //Add move to gate background task - we gotta add a switch to the settings config file


                        //var ent = Cache.Instance.AccelerationGates;

                        // Is there a gate?
                        //if (ent != null && ent.Any())
                        //{
                        //Logging.Log("CombatMissionCtrl", "We found an acceleration gate!", Logging.Orange);
                        // Activate it (Activate action also moves to the gate)


                        if (!NavigateOnGrid.SpeedTank)
                        {
                            var backgroundAction = new Action {State = ActionState.MoveToBackground};
                            backgroundAction.AddParameter("target", "Acceleration Gate");
                            backgroundAction.AddParameter("optional", "true");
                            _pocketActions.Insert(0, backgroundAction);
                        }

//						}
                        //else { // No, were done

                        //	Logging.Log("CombatMissionCtrl", "We didn't found an acceleration gate!", Logging.Orange);
                        //		_pocketActions.Add(new Actions.Action { State = ActionState.Done });
                        //}
                    }
                    else
                    {
                        //Add move to gate background task - we gotta add a switch to the settings config file
                        if ((!NavigateOnGrid.SpeedTank) && !_pocketActions.Any(a => a.State == ActionState.MoveToBackground))
                        {
                            var backgroundAction = new Action {State = ActionState.MoveToBackground};
                            backgroundAction.AddParameter("target", "Acceleration Gate");
                            backgroundAction.AddParameter("optional", "true");
                            _pocketActions.Insert(0, backgroundAction);
                        }
                    }

                    Logging.Logging.Log("-----------------------------------------------------------------");
                    Logging.Logging.Log("-----------------------------------------------------------------");
                    Logging.Logging.Log("Mission Timer Currently At: [" + Math.Round(DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes, 0) + "] min");

                    //if (Cache.Instance.OptimalRange != 0)
                    //    Logging.Log("Optimal Range is set to: " + (Cache.Instance.OrbitDistance / 1000).ToString(CultureInfo.InvariantCulture) + "k");
                    Logging.Logging.Log("Max Range is currently: " + (Combat.Combat.MaxRange / 1000).ToString(CultureInfo.InvariantCulture) + "k");
                    Logging.Logging.Log("-----------------------------------------------------------------");
                    Logging.Logging.Log("-----------------------------------------------------------------");
                    Logging.Logging.Log("Pocket [" + PocketNumber + "] loaded, executing the following actions");
                    var pocketActionCount = 1;
                    foreach (var a in _pocketActions)
                    {
                        Logging.Logging.Log("Action [ " + pocketActionCount + " ] " + a);
                        pocketActionCount++;
                    }
                    Logging.Logging.Log("-----------------------------------------------------------------");
                    Logging.Logging.Log("-----------------------------------------------------------------");

                    // Reset pocket information
                    _currentAction = 0;


                    Console.WriteLine("Settings.Instance.LootWhileSpeedTanking: " + Settings.Instance.LootWhileSpeedTanking.ToString());

                    Drones.IsMissionPocketDone = false;


                    if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                    {
                        if (Logging.Logging.DebugTargetWrecks)
                            Logging.Logging.Log("Salvage.OpenWrecks = false;");
                        Salvage.OpenWrecks = false;
                    }
                    else
                    {
                        Salvage.OpenWrecks = true;
                    }

                    IgnoreTargets.Clear();
                    Statistics.PocketObjectStatistics(Cache.Instance.Objects.ToList());
                    _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.ExecutePocketActions;
                    break;

                case CombatMissionCtrlState.ExecutePocketActions:
                    if (_currentAction >= _pocketActions.Count)
                    {
                        // No more actions, but we're not done?!?!?!
                        Logging.Logging.Log("We're out of actions but did not process a 'Done' or 'Activate' action");

                        _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Error;
                        break;
                    }

                    var action = _pocketActions[_currentAction];
                    if (action.ToString() != Cache.Instance.CurrentPocketAction)
                    {
                        Cache.Instance.CurrentPocketAction = action.ToString();
                    }
                    var currentAction = _currentAction;
                    PerformAction(action);

                    if (currentAction != _currentAction)
                    {
                        Logging.Logging.Log("Finished Action." + action);

                        if (_currentAction < _pocketActions.Count)
                        {
                            action = _pocketActions[_currentAction];
                            Logging.Logging.Log("Starting Action." + action);
                        }
                    }

                    break;

                case CombatMissionCtrlState.NextPocket:
                    var distance = Cache.Instance.DistanceFromMe(_lastX, _lastY, _lastZ);
                    if (distance > (int) Distances.NextPocketDistance)
                    {
                        Logging.Logging.Log("We have moved to the next Pocket [" + Math.Round(distance / 1000, 0) + "k away]");

                        // If we moved more then 100km, assume next Pocket
                        PocketNumber++;
                        _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.LoadPocket;
                        Statistics.WritePocketStatistics();
                    }
                    else if (DateTime.UtcNow.Subtract(_moveToNextPocket).TotalMinutes > 2)
                    {
                        Logging.Logging.Log("We have timed out, retry last action");

                        // We have reached a timeout, revert to ExecutePocketActions (e.g. most likely Activate)
                        _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.ExecutePocketActions;
                    }
                    break;
            }

            var newX = Cache.Instance.ActiveShip.Entity.X;
            var newY = Cache.Instance.ActiveShip.Entity.Y;
            var newZ = Cache.Instance.ActiveShip.Entity.Z;

            // For some reason x/y/z returned 0 sometimes
            if (newX != 0 && newY != 0 && newZ != 0)
            {
                // Save X/Y/Z so that NextPocket can check if we actually went to the next Pocket :)
                _lastX = newX;
                _lastY = newY;
                _lastZ = newZ;
            }
        }

        /// <summary>
        ///     Loads mission objectives from XML file
        /// </summary>
        /// <param name="agentId"> </param>
        /// <param name="pocketId"> </param>
        /// <param name="missionMode"> </param>
        /// <returns></returns>
        public static IEnumerable<Action> LoadMissionActions(long agentId, int pocketId, bool missionMode)
        {
            try
            {
                var missiondetails = Cache.Instance.GetAgentMission(agentId, false);
                if (missiondetails == null && missionMode)
                {
                    return new Action[0];
                }

                if (missiondetails != null)
                {
                    MissionSettings.SetmissionXmlPath(Logging.Logging.FilterPath(missiondetails.Name));
                    if (!File.Exists(MissionSettings.MissionXmlPath))
                    {
                        //No mission file but we need to set some cache settings
                        MissionSettings.MissionOrbitDistance = null;
                        MissionSettings.MissionOptimalRange = null;
                        MissionSettings.MissionUseDrones = null;
                        Cache.Instance.AfterMissionSalvaging = Salvage.AfterMissionSalvaging;
                        return new Action[0];
                    }

                    //
                    // this loads the settings from each pocket... but NOT any settings global to the mission
                    //
                    try
                    {
                        var xdoc = XDocument.Load(MissionSettings.MissionXmlPath);
                        if (xdoc.Root != null)
                        {
                            var xElement = xdoc.Root.Element("pockets");
                            if (xElement != null)
                            {
                                var pockets = xElement.Elements("pocket");
                                foreach (var pocket in pockets)
                                {
                                    if ((int) pocket.Attribute("id") != pocketId)
                                    {
                                        continue;
                                    }

                                    if (pocket.Element("orbitentitynamed") != null)
                                    {
                                        Cache.Instance.OrbitEntityNamed = (string) pocket.Element("orbitentitynamed");
                                    }

                                    if (pocket.Element("damagetype") != null)
                                    {
                                        MissionSettings.PocketDamageType =
                                            (DamageType) Enum.Parse(typeof(DamageType), (string) pocket.Element("damagetype"), true);
                                    }

                                    if (pocket.Element("orbitdistance") != null) //Load OrbitDistance from mission.xml, if present
                                    {
                                        MissionSettings.MissionOrbitDistance = (int) pocket.Element("orbitdistance");
                                        Logging.Logging.Log("Using Mission Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");
                                    }
                                    else //Otherwise, use value defined in charname.xml file
                                    {
                                        MissionSettings.MissionOrbitDistance = null;
                                        Logging.Logging.Log("Using Settings Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");
                                    }

                                    if (pocket.Element("optimalrange") != null) //Load OrbitDistance from mission.xml, if present
                                    {
                                        MissionSettings.MissionOptimalRange = (int) pocket.Element("optimalrange");
                                        Logging.Logging.Log("Using Mission OptimalRange [" + NavigateOnGrid.OptimalRange + "]");
                                    }
                                    else //Otherwise, use value defined in charname.xml file
                                    {
                                        MissionSettings.MissionOptimalRange = null;
                                        Logging.Logging.Log("Using Settings OptimalRange [" + NavigateOnGrid.OptimalRange + "]");
                                    }

                                    if (pocket.Element("afterMissionSalvaging") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                    {
                                        Cache.Instance.AfterMissionSalvaging = (bool) pocket.Element("afterMissionSalvaging");
                                    }

                                    if (pocket.Element("dronesKillHighValueTargets") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                    {
                                        MissionSettings.MissionDronesKillHighValueTargets = (bool) pocket.Element("dronesKillHighValueTargets");
                                    }
                                    else //Otherwise, use value defined in charname.xml file
                                    {
                                        MissionSettings.MissionDronesKillHighValueTargets = null;
                                    }

                                    var actions = new List<Action>();
                                    var elements = pocket.Element("actions");
                                    if (elements != null)
                                    {
                                        foreach (var element in elements.Elements("action"))
                                        {
                                            var action = new Action
                                            {
                                                State = (ActionState) Enum.Parse(typeof(ActionState), (string) element.Attribute("name"), true)
                                            };
                                            var xAttribute = element.Attribute("name");
                                            if (xAttribute != null && xAttribute.Value == "ClearPocket")
                                            {
                                                action.AddParameter("", "");
                                            }
                                            else
                                            {
                                                foreach (var parameter in element.Elements("parameter"))
                                                {
                                                    action.AddParameter((string) parameter.Attribute("name"), (string) parameter.Attribute("value"));
                                                }
                                            }
                                            actions.Add(action);
                                        }
                                    }

                                    return actions;
                                }

                                //actions.Add(action);
                            }
                            else
                            {
                                return new Action[0];
                            }
                        }
                        else
                        {
                            {
                                return new Action[0];
                            }
                        }

                        // if we reach this code there is no mission XML file, so we set some things -- Assail

                        MissionSettings.MissionOptimalRange = null;
                        MissionSettings.MissionOrbitDistance = null;
                        Logging.Logging.Log("Using Settings Orbit distance [" + NavigateOnGrid.OrbitDistance + "]");

                        return new Action[0];
                    }
                    catch (Exception ex)
                    {
                        Logging.Logging.Log("Error loading mission XML file [" + ex.Message + "]");
                        return new Action[0];
                    }
                }
                return new Action[0];
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return null;
            }
        }
    }
}