using System;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public class Grab
    {
        private double _freeCargoCapacity;

        private DateTime _lastAction;
        public int Item { get; set; }

        public int Unit { get; set; }

        public string Hangar { get; set; }

        public void ProcessState()
        {
            if (!Cache.Instance.InStation)
                return;

            if (Cache.Instance.InSpace)
                return;

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20))
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return;

            DirectContainer hangar = null;

            if (_States.CurrentGrabState != GrabState.WaitForItems)
            {
                if (Cache.Instance.ItemHangar == null) return;
                if (Cache.Instance.ShipHangar == null) return;

                if ("Local Hangar" == Hangar)
                    hangar = Cache.Instance.ItemHangar;
                else if ("Ship Hangar" == Hangar)
                    hangar = Cache.Instance.ShipHangar;
                else
                    hangar = Cache.Instance.DirectEve.GetCorporationHangar(Hangar);
            }

            switch (_States.CurrentGrabState)
            {
                case GrabState.Idle:
                case GrabState.Done:
                    break;

                case GrabState.Begin:
                    _States.CurrentGrabState = GrabState.ReadyItemhangar;
                    break;

                case GrabState.ReadyItemhangar:
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2)
                        break;

                    if ("Local Hangar" == Hangar)
                    {
                        if (Cache.Instance.ItemHangar == null) return;
                    }
                    else if ("Ship Hangar" == Hangar)
                    {
                        if (Cache.Instance.ShipHangar == null) return;

                        if (hangar != null && (hangar.Window == null || !hangar.Window.IsReady))
                            break;
                    }
                    else if (Hangar != null)
                    {
                        if (hangar == null || !hangar.IsValid)
                        {
                            Logging.Logging.Log("Grab", "No Valid Corp Hangar found. Hangar: " + Hangar, Logging.Logging.White);
                            _States.CurrentGrabState = GrabState.Idle;
                            return;
                        }
                    }

                    Logging.Logging.Log("Grab", "Opening Hangar", Logging.Logging.White);
                    _States.CurrentGrabState = GrabState.OpenCargo;
                    break;

                case GrabState.OpenCargo:

                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        Logging.Logging.Log("MoveItems", "if (Cache.Instance.CurrentShipsCargo == null)", Logging.Logging.Teal);
                        return;
                    }

                    Logging.Logging.Log("Grab", "Opening Cargo Hold", Logging.Logging.White);
                    _freeCargoCapacity = Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity;
                    _States.CurrentGrabState = Item == 00 ? GrabState.AllItems : GrabState.MoveItems;

                    break;

                case GrabState.MoveItems:

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2)
                        break;
                    if (Unit == 00)
                    {
                        if (hangar != null)
                        {
                            var grabItems = hangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                            if (grabItems != null)
                            {
                                var totalVolum = grabItems.Quantity*grabItems.Volume;
                                if (_freeCargoCapacity >= totalVolum)
                                {
                                    //foreach (DirectItem item in items)
                                    //{
                                    //    Logging.Log("Grab", "Items: " + item.TypeName, Logging.White);
                                    //}
                                    Cache.Instance.CurrentShipsCargo.Add(grabItems, grabItems.Quantity);
                                    _freeCargoCapacity -= totalVolum;
                                    Logging.Logging.Log("Grab.MoveItems", "Moving all the items", Logging.Logging.White);
                                    _lastAction = DateTime.UtcNow;
                                    _States.CurrentGrabState = GrabState.WaitForItems;
                                }
                                else
                                {
                                    _States.CurrentGrabState = GrabState.Done;
                                    Logging.Logging.Log("Grab.MoveItems", "No load capacity", Logging.Logging.White);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (hangar != null)
                        {
                            var grabItem = hangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                            if (grabItem != null)
                            {
                                var totalVolum = Unit*grabItem.Volume;
                                if (_freeCargoCapacity >= totalVolum)
                                {
                                    Cache.Instance.CurrentShipsCargo.Add(grabItem, Unit);
                                    _freeCargoCapacity -= totalVolum;
                                    Logging.Logging.Log("Grab.MoveItems", "Moving item", Logging.Logging.White);
                                    _lastAction = DateTime.UtcNow;
                                    _States.CurrentGrabState = GrabState.WaitForItems;
                                }
                                else
                                {
                                    _States.CurrentGrabState = GrabState.Done;
                                    Logging.Logging.Log("Grab.MoveItems", "No load capacity", Logging.Logging.White);
                                }
                            }
                        }
                    }

                    break;

                case GrabState.AllItems:

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2)
                        break;

                    if (hangar != null)
                    {
                        var allItem = hangar.Items;
                        if (allItem != null)
                        {
                            foreach (var item in allItem)
                            {
                                if (Cache.Instance.ActiveShip.ItemId == item.ItemId)
                                {
                                    allItem.Remove(item);
                                    continue;
                                }

                                var totalVolum = item.Quantity*item.Volume;

                                if (_freeCargoCapacity >= totalVolum)
                                {
                                    Cache.Instance.CurrentShipsCargo.Add(item);
                                    _freeCargoCapacity -= totalVolum;
                                }
                                else
                                {
                                    // we are out of room, should we do a partial item move?
                                    var quantityWeCanFit = _freeCargoCapacity/item.Volume;
                                    Cache.Instance.CurrentShipsCargo.Add(item, (int) quantityWeCanFit);

                                    //we are now "full" and should go "home" or "market" (how do we decide where to go ffs?)
                                }
                            }
                            Logging.Logging.Log("Grab.AllItems", "Moving items", Logging.Logging.White);
                            _lastAction = DateTime.UtcNow;
                            _States.CurrentGrabState = GrabState.WaitForItems;
                        }
                    }

                    break;

                case GrabState.WaitForItems:
                    // Wait 5 seconds after moving
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 5)
                        break;

                    if (Cache.Instance.DirectEve.GetLockedItems().Count == 0)
                    {
                        Logging.Logging.Log("Grab", "Done", Logging.Logging.White);
                        _States.CurrentGrabState = GrabState.Done;
                        break;
                    }

                    break;
            }
        }
    }
}