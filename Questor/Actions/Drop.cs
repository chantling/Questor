using System;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public class Drop
    {
        private DateTime _lastAction;
        public DirectContainer dropHangar = null;
        public int Item { get; set; }

        public int Unit { get; set; }

        public string DestinationHangarName { get; set; }

        public void ProcessState()
        {
            if (!Cache.Instance.InStation)
                return;

            if (Cache.Instance.InSpace)
                return;

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(10))
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return;

            switch (_States.CurrentDropState)
            {
                case DropState.Idle:
                case DropState.Done:
                    break;

                case DropState.Begin:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: Begin");

                    _States.CurrentDropState = DropState.ReadyItemhangar;
                    break;

                case DropState.ReadyItemhangar:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: ReadyItemhangar");
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2) return;
                    dropHangar = Cache.Instance.ItemHangar;

                    if (DestinationHangarName == "Local Hangar")
                    {
                        if (Cache.Instance.ItemHangar == null) return;
                        dropHangar = Cache.Instance.ItemHangar;
                    }
                    else if (DestinationHangarName == "Ship Hangar")
                    {
                        if (Cache.Instance.ShipHangar == null) return;
                        dropHangar = Cache.Instance.ShipHangar;
                    }
                    else
                    {
                        if (dropHangar != null && dropHangar.Window == null)
                        {
                            // No, command it to open
                            //Cache.Instance.DirectEve.OpenCorporationHangar();
                            break;
                        }

                        if (dropHangar != null && !dropHangar.Window.IsReady) return;
                    }

                    Logging.Logging.Log("Opening Hangar");
                    _States.CurrentDropState = DropState.OpenCargo;
                    break;

                case DropState.OpenCargo:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: OpenCargo");

                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                        return;
                    }

                    Logging.Logging.Log("Opening Cargo Hold");
                    _States.CurrentDropState = Item == 00 ? DropState.AllItems : DropState.MoveItems;
                    break;

                case DropState.MoveItems:

                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: MoveItems");
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2) return;
                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                        return;
                    }

                    DirectItem dropItem;

                    if (Unit == 00)
                    {
                        try
                        {
                            if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Item TypeID is [" + Item + "]");

                            var iCount = 1;
                            foreach (var ItemTest in Cache.Instance.CurrentShipsCargo.Items)
                            {
                                iCount++;
                                if (Logging.Logging.DebugQuestorManager)
                                    Logging.Logging.Log("[" + iCount + "] ItemName: [" + ItemTest.TypeName + "]");
                            }

                            if (Cache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == Item))
                            {
                                dropItem = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == Item);
                                if (dropItem != null)
                                {
                                    if (Logging.Logging.DebugQuestorManager)
                                        Logging.Logging.Log("dropItem = [" + dropItem.TypeName + "]");
                                    if (dropHangar != null) dropHangar.Add(dropItem, dropItem.Quantity);
                                    Logging.Logging.Log("Moving all the items");
                                    _lastAction = DateTime.UtcNow;
                                    _States.CurrentDropState = DropState.WaitForMove;
                                    return;
                                }
                            }
                            else
                            {
                                if (Logging.Logging.DebugQuestorManager)
                                    Logging.Logging.Log("missing item with typeID of [" + Item + "]");
                            }
                        }
                        catch (Exception exception)
                        {
                            Logging.Logging.Log("MoveItems (all): Exception [" + exception + "]");
                        }
                        return;
                    }

                    try
                    {
                        if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Item = [" + Item + "]");
                        dropItem = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => (i.TypeId == Item));
                        if (dropItem != null)
                        {
                            if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Unit = [" + Unit + "]");

                            if (dropHangar != null) dropHangar.Add(dropItem, Unit);
                            Logging.Logging.Log("Moving item");
                            _lastAction = DateTime.UtcNow;
                            _States.CurrentDropState = DropState.WaitForMove;
                            return;
                        }
                    }
                    catch (Exception exception)
                    {
                        Logging.Logging.Log("MoveItems: Exception [" + exception + "]");
                    }

                    break;

                case DropState.AllItems:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: AllItems");
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2) return;

                    var allItem = Cache.Instance.CurrentShipsCargo.Items;
                    if (allItem != null)
                    {
                        if (dropHangar != null) dropHangar.Add(allItem);
                        Logging.Logging.Log("Moving item");
                        _lastAction = DateTime.UtcNow;
                        _States.CurrentDropState = DropState.WaitForMove;
                        return;
                    }

                    break;

                case DropState.WaitForMove:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: WaitForMove");

                    // Wait 2 seconds after moving
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2) return;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        _States.CurrentDropState = DropState.StackItemsHangar;
                        return;
                    }

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds > 60)
                    {
                        Logging.Logging.Log("Moving items timed out, clearing item locks");
                        Cache.Instance.DirectEve.UnlockItems();

                        _States.CurrentDropState = DropState.StackItemsHangar;
                        return;
                    }
                    break;

                case DropState.StackItemsHangar:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: StackItemsHangar");
                    // Do not stack until 5 seconds after the cargo has cleared
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 5) return;

                    // Stack everything
                    if (dropHangar != null)
                    {
                        Logging.Logging.Log("Stacking items");
                        dropHangar.StackAll();
                        _lastAction = DateTime.UtcNow;
                        _States.CurrentDropState = DropState.WaitForStacking;
                        return;
                    }
                    break;

                case DropState.WaitForStacking:
                    if (Logging.Logging.DebugQuestorManager) Logging.Logging.Log("Entered: WaitForStacking");
                    // Wait 5 seconds after stacking
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 5) return;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        Logging.Logging.Log("Done");
                        _States.CurrentDropState = DropState.Done;
                        return;
                    }

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds > 120)
                    {
                        Logging.Logging.Log("Stacking items timed out, clearing item locks");
                        Cache.Instance.DirectEve.UnlockItems();

                        Logging.Logging.Log("Done");
                        _States.CurrentDropState = DropState.Done;
                        return;
                    }
                    break;
            }
        }
    }
}