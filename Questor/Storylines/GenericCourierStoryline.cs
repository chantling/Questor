﻿using System;
using System.Linq;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Storylines
{
    public class GenericCourier : IStoryline
    {
        private DateTime _nextGenericCourierStorylineAction;
        private GenericCourierStorylineState _state;

        public StorylineState Arm(Storyline storyline)
        {
            if (_nextGenericCourierStorylineAction > DateTime.UtcNow) return StorylineState.Arm;

            if (Cache.Instance.ShipHangar == null) return StorylineState.Arm;

            // Are we in an industrial?  Yes, goto the agent
            //var directEve = Cache.Instance.DirectEve;
            //if (Cache.Instance.ActiveShip.TypeId == 648 || Cache.Instance.ActiveShip.TypeId == 649 || Cache.Instance.ActiveShip.TypeId == 650 || Cache.Instance.ActiveShip.TypeId == 651 || Cache.Instance.ActiveShip.TypeId == 652 || Cache.Instance.ActiveShip.TypeId == 653 || Cache.Instance.ActiveShip.TypeId == 654 || Cache.Instance.ActiveShip.TypeId == 655 || Cache.Instance.ActiveShip.TypeId == 656 || Cache.Instance.ActiveShip.TypeId == 657 || Cache.Instance.ActiveShip.TypeId == 1944 || Cache.Instance.ActiveShip.TypeId == 19744)
            //    return StorylineState.GotoAgent;

            // Open the ship hangar
            if (Cache.Instance.ShipHangar == null)
            {
                _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(5);
                return StorylineState.Arm;
            }

            ////  Look for an industrial
            //var item = Cache.Instance.ShipHangar.Items.FirstOrDefault(i => i.Quantity == -1 && (i.TypeId == 648 || i.TypeId == 649 || i.TypeId == 650 || i.TypeId == 651 || i.TypeId == 652 || i.TypeId == 653 || i.TypeId == 654 || i.TypeId == 655 || i.TypeId == 656 || i.TypeId == 657 || i.TypeId == 1944 || i.TypeId == 19744));
            //if (item != null)
            //{
            //    Logging.Log("GenericCourier", "Switching to an industrial", Logging.White);

            //    _nextAction = DateTime.UtcNow.AddSeconds(10);

            //    item.ActivateShip();
            //    return StorylineState.Arm;
            //}
            //else
            //{
            //    Logging.Log("GenericCourier", "No industrial found, going in active ship", Logging.White);
            //    return StorylineState.GotoAgent;
            //}

            if (string.IsNullOrEmpty(Settings.Instance.TransportShipName.ToLower()))
            {
                _States.CurrentArmState = ArmState.NotEnoughAmmo;
                Logging.Log("Could not find transportshipName in settings!");
                return StorylineState.BlacklistAgent;
            }

            try
            {
                if (Logging.DebugArm) Logging.Log("try");
                if (Cache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower())
                {
                    if (Logging.DebugArm)
                        Logging.Log("if (Cache.Instance.ActiveShip.GivenName.ToLower() != transportshipName.ToLower())");
                    if (!Cache.Instance.ShipHangar.Items.Any()) return StorylineState.Arm; //no ships?!?

                    if (Logging.DebugArm)
                        Logging.Log("if (!Cache.Instance.ShipHangar.Items.Any()) return StorylineState.Arm; done");

                    var ships = Cache.Instance.ShipHangar.Items;
                    if (Logging.DebugArm) Logging.Log("List<DirectItem> ships = Cache.Instance.ShipHangar.Items;");

                    foreach (
                        var ship in ships.Where(ship => ship.GivenName != null && ship.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower()))
                    {
                        Logging.Log("Making [" + ship.GivenName + "] active");
                        ship.ActivateShip();
                        _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                        return StorylineState.Arm;
                    }

                    return StorylineState.Arm;
                }
            }
            catch (Exception exception)
            {
                Logging.Log("Exception thrown while attempting to switch to transport ship: [" + exception + "]");
                Logging.Log("blacklisting this storyline agent for this session because we could not switch to the configured TransportShip named [" +
                    Settings.Instance.TransportShipName + "]");
                return StorylineState.BlacklistAgent;
            }

            if (DateTime.UtcNow > Time.Instance.NextArmAction) //default 7 seconds
            {
                if (Cache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower())
                {
                    Logging.Log("Done");
                    _States.CurrentArmState = ArmState.Done;
                    return StorylineState.GotoAgent;
                }
            }

            return StorylineState.Arm;
        }

        /// <summary>
        ///     There are no pre-accept actions
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            _state = GenericCourierStorylineState.GotoPickupLocation;

            _States.CurrentTravelerState = TravelerState.Idle;
            Traveler.Destination = null;

            return StorylineState.AcceptMission;
        }

        /// <summary>
        ///     Goto the pickup location
        ///     Pickup the item
        ///     Goto drop off location
        ///     Drop the item
        ///     Complete mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            if (_nextGenericCourierStorylineAction > DateTime.UtcNow)
                return StorylineState.ExecuteMission;

            switch (_state)
            {
                case GenericCourierStorylineState.GotoPickupLocation:
                    if (GotoMissionBookmark(Cache.Instance.CurrentStorylineAgentId, "Objective (Pick Up)"))
                        _state = GenericCourierStorylineState.PickupItem;
                    break;

                case GenericCourierStorylineState.PickupItem:
                    if (MoveItem(true))
                        _state = GenericCourierStorylineState.GotoDropOffLocation;
                    break;

                case GenericCourierStorylineState.GotoDropOffLocation:
                    if (GotoMissionBookmark(Cache.Instance.CurrentStorylineAgentId, "Objective (Drop Off)"))
                        _state = GenericCourierStorylineState.DropOffItem;
                    break;

                case GenericCourierStorylineState.DropOffItem:
                    if (MoveItem(false))
                    {
                        return StorylineState.CompleteMission;
                    }
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        private bool GotoMissionBookmark(long agentId, string title)
        {
            var destination = Traveler.Destination as MissionBookmarkDestination;
            if (destination == null || destination.AgentId != agentId || !destination.Title.ToLower().StartsWith(title.ToLower()))
                Traveler.Destination = new MissionBookmarkDestination(MissionSettings.GetMissionBookmark(agentId, title));

            Traveler.ProcessState();

            if (_States.CurrentTravelerState == TravelerState.AtDestination)
            {
                Traveler.Destination = null;
                return true;
            }

            return false;
        }

        private bool MoveItem(bool pickup)
        {
            var directEve = Cache.Instance.DirectEve;

            // Open the item hangar (should still be open)
            if (Cache.Instance.ItemHangar == null) return false;

            if (Cache.Instance.CurrentShipsCargo == null) return false;

            var from = pickup ? Cache.Instance.ItemHangar : Cache.Instance.CurrentShipsCargo;
            var to = pickup ? Cache.Instance.CurrentShipsCargo : Cache.Instance.ItemHangar;

            // We moved the item

            if (to.Items.Any(i => i.GroupId == (int) Group.MiscSpecialMissionItems || i.GroupId == (int) Group.Livestock))
                return true;

            if (directEve.GetLockedItems().Count != 0)
                return false;

            // Move items
            foreach (var item in from.Items.Where(i => i.GroupId == (int) Group.MiscSpecialMissionItems || i.GroupId == (int) Group.Livestock))
            {
                Logging.Log("Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"));
                to.Add(item, item.Stacksize);
            }
            _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(10);
            return false;
        }
    }
}