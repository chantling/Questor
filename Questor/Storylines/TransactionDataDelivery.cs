
namespace Questor.Storylines
{
    using System;
    using System.Linq;
    using DirectEve;
    using global::Questor.Modules.Actions;
    using global::Questor.Modules.Activities;
    using global::Questor.Modules.States;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;

    public class TransactionDataDelivery : IStoryline
    {
        private DateTime _nextAction;
        private TransactionDataDeliveryState _state;

        /// <summary>
        ///   Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.Arm;

            // Are we in a shuttle?  Yes, go to the agent
            if (Cache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
                return StorylineState.GotoAgent;

            // Open the ship hangar
            if (Cache.Instance.ShipHangar == null) return StorylineState.Arm;

            //  Look for a shuttle
            DirectItem item = Cache.Instance.ShipHangar.Items.FirstOrDefault(i => i.Quantity == -1 && i.GroupId == (int)Group.Shuttle);
            if (item != null)
            {
                Logging.Log("TransactionDataDelivery", "Switching to shuttle", Logging.White);

                _nextAction = DateTime.UtcNow.AddSeconds(10);

                item.ActivateShip();
                return StorylineState.Arm;
            }

            Logging.Log("TransactionDataDelivery", "No shuttle found, going in active ship", Logging.Orange);
            return StorylineState.GotoAgent;
        }

        /// <summary>
        ///   There are no actions before you accept the mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            _state = TransactionDataDeliveryState.GotoPickupLocation;

            _States.CurrentTravelerState = TravelerState.Idle;
            Traveler.Destination = null;

            return StorylineState.AcceptMission;
        }

        private bool GotoMissionBookmark(long agentId, string title)
        {
            MissionBookmarkDestination destination = Traveler.Destination as MissionBookmarkDestination;
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
            // Open the item hangar (should still be open)
            if (Cache.Instance.ItemHangar == null) return false;

            if (Cache.Instance.CurrentShipsCargo == null) return false;

            // 314 == Transaction And Salary Logs (all different versions)
            const int groupId = 314;
            DirectContainer from = pickup ? Cache.Instance.ItemHangar : Cache.Instance.CurrentShipsCargo;
            DirectContainer to = pickup ? Cache.Instance.CurrentShipsCargo : Cache.Instance.ItemHangar;

            // We moved the item
            if (to.Items.Any(i => i.GroupId == groupId))
                return true;

            if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
                return false;

            // Move items
            foreach (DirectItem item in from.Items.Where(i => i.GroupId == groupId))
            {
                Logging.Log("TransactionDataDelivery", "Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"), Logging.White);
                to.Add(item);
            }
            _nextAction = DateTime.UtcNow.AddSeconds(10);
            return false;
        }

        /// <summary>
        ///   Goto the pickup location
        ///   Pickup the item
        ///   Goto drop off location
        ///   Drop the item
        ///   Goto Agent
        ///   Complete mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.ExecuteMission;

            switch (_state)
            {
                case TransactionDataDeliveryState.GotoPickupLocation:
                    if (GotoMissionBookmark(Cache.Instance.CurrentStorylineAgentId, "Objective (Pick Up)"))
                        _state = TransactionDataDeliveryState.PickupItem;
                    break;

                case TransactionDataDeliveryState.PickupItem:
                    if (MoveItem(true))
                        _state = TransactionDataDeliveryState.GotoDropOffLocation;
                    break;

                case TransactionDataDeliveryState.GotoDropOffLocation:
                    if (GotoMissionBookmark(Cache.Instance.CurrentStorylineAgentId, "Objective (Drop Off)"))
                        _state = TransactionDataDeliveryState.DropOffItem;
                    break;

                case TransactionDataDeliveryState.DropOffItem:
                    if (MoveItem(false))
                        return StorylineState.ReturnToAgent;
                    break;
            }

            return StorylineState.ExecuteMission;
        }
    }
}