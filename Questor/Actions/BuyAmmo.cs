/*
 * ---------------------------------------
 * User: duketwo
 * Date: 09.10.2015
 * Time: 11:08
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using DirectEve;
using global::Questor.Modules.Caching;
using global::Questor.Modules.States;
using global::Questor.Modules.Logging;
using global::Questor.Modules.Lookup;
using global::Questor.Modules.Activities;
using global::Questor.Modules.Combat;
using global::Questor.Modules.Actions;
using global::Questor.Modules.BackgroundTasks;


namespace Questor.Actions
{
    /// <summary>
    /// Description of BuyAmmo.
    /// </summary>
    public static class BuyAmmo
    {

        public static BuyAmmoState state { get; set; } // idle == default
        private static Dictionary<int, int> buyList = new Dictionary<int, int>();
        private static int minimumDroneAmount = 200;
        private static DateTime nextAction = DateTime.MinValue;
        private static int jumps;
        private static TravelerDestination travelerDestination;
        private static int orderIterations = 0;

        private static Dictionary<BuyAmmoState, int> stateIterations = new Dictionary<BuyAmmoState, int>();
        private static int maxStateIterations = 500;

        private static int hoursBetweenAmmoBuy = 10;
        public static bool error { get; set; }
        private static int minAmmoMultiplier = 20;
        private static int maxAmmoMultiplier = 100;
        private static int maxAvgPriceMultiplier = 3;
        private static int maxBasePriceMultiplier = 6;

        private static bool StateCheckEveryPulse
        {
            get
            {
                if (stateIterations.ContainsKey(state))
                    stateIterations[state] = stateIterations[state] + 1;
                else
                    stateIterations.Add(state, 1);

                if (stateIterations[state] >= maxStateIterations)
                {
                    Logging.Log("BuyAmmo", "ERROR:  if (stateIterations[state] >= maxStateIterations)", Logging.White);
                    state = BuyAmmoState.Error;
                    return false;
                }

                return true;
            }
        }

        public static void ProcessState()
        {

            if (nextAction > DateTime.UtcNow)
                return;

            if (!StateCheckEveryPulse)
                return;

            switch (state)
            {

                case BuyAmmoState.Idle:
                    Console.WriteLine(state.ToString());
                    stateIterations = new Dictionary<BuyAmmoState, int>();
                    state = BuyAmmoState.AmmoCheck;
                    break;

                case BuyAmmoState.AmmoCheck:
                    Console.WriteLine(state.ToString());


                    if (!Cache.Instance.InStation)
                        return;

                    if (Cache.Instance.EveAccount.LastAmmoBuy.AddHours(hoursBetweenAmmoBuy) > DateTime.UtcNow) // temporarily disabled
                    {
                        Logging.Log("BuyAmmo", "We were buying ammo already in the past [" + hoursBetweenAmmoBuy + "] hours.", Logging.White);
                        state = BuyAmmoState.Done;
                        return;
                    }

                    if (Cache.Instance.ItemHangar == null)
                        return;

                    if (!Combat.Ammo.Any())
                    {
                        return;
                    }

                    buyList = new Dictionary<int, int>();

                    bool buy = false;

                    foreach (var ammo in Combat.Ammo)
                    {

                        var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Quantity);
                        int minQty = ammo.Quantity * minAmmoMultiplier;
                        if (totalQuantity < minQty)
                        {
                            Logging.Log("BuyAmmo", "Total ammo amount in hangar type [" + ammo.TypeId + "] [" + totalQuantity + "] Minimum amount [" + minQty + "] We're going to buy ammo.", Logging.White);
                            buy = true;
                            break;
                        }

                    }

                    if (Drones.UseDrones)
                    {

                        List<int> droneTypeIds = new List<int>();
                        droneTypeIds.Add(Drones.DroneTypeID);

                        foreach (var factionFtting in MissionSettings.ListofFactionFittings)
                        {
                            if (factionFtting.DroneTypeID == null)
                                continue;
                            if (factionFtting.DroneTypeID == 0)
                                continue;
                            if (!droneTypeIds.Contains((int)factionFtting.DroneTypeID))
                                droneTypeIds.Add((int)factionFtting.DroneTypeID);
                        }

                        foreach (var missionFitting in MissionSettings.ListOfMissionFittings)
                        {
                            if (missionFitting.DroneTypeID == null)
                                continue;
                            if (missionFitting.DroneTypeID == 0)
                                continue;
                            if (!droneTypeIds.Contains((int)missionFitting.DroneTypeID))
                                droneTypeIds.Add((int)missionFitting.DroneTypeID);
                        }

                        foreach (int droneTypeId in droneTypeIds)
                        {
                            var totalQuantityDrones = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Quantity);
                            if (totalQuantityDrones < minimumDroneAmount)
                            {
                                Logging.Log("BuyAmmo", "Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + minimumDroneAmount + "] We're going to buy drones of type [" + droneTypeId + "]", Logging.White);
                                buy = true;
                            }
                        }
                    }


                    Logging.Log("BuyAmmo", "LastAmmoBuy was on [" + Cache.Instance.EveAccount.LastAmmoBuy + "]", Logging.White);



                    if (buy)
                    {
                        state = BuyAmmoState.ActivateTransportShip;
                    }
                    else
                    {
                        Logging.Log("BuyAmmo", "There is still enough ammo avaiable in the itemhangar. Changing state to done.", Logging.White);
                        state = BuyAmmoState.Done;
                    }


                    break;
                case BuyAmmoState.ActivateTransportShip:
                    Console.WriteLine(state.ToString());

                    Cache.Instance.WCFClient.GetPipeProxy.SetEveAccountAttributeValue(Cache.Instance.CharName, "LastAmmoBuy", DateTime.UtcNow);

                    if (!Cache.Instance.InStation)
                        return;

                    if (Cache.Instance.ItemHangar == null)
                        return;

                    if (!Combat.Ammo.Any())
                    {
                        return;
                    }

                    if (Cache.Instance.ShipHangar == null)
                        return;

                    if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null && Cache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower())
                    {
                        state = BuyAmmoState.CreateBuyList;
                        return;
                    }

                    if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null && Cache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower())
                    {
                        List<DirectItem> ships = Cache.Instance.ShipHangar.Items;
                        foreach (DirectItem ship in ships.Where(ship => ship.GivenName != null && ship.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower()))
                        {
                            Logging.Log("BuyAmmo", "Making [" + ship.GivenName + "] active", Logging.White);
                            ship.ActivateShip();
                            nextAction = DateTime.UtcNow.AddSeconds(Modules.Lookup.Time.Instance.SwitchShipsDelay_seconds);
                        }

                        state = BuyAmmoState.CreateBuyList;
                    }
                    break;

                case BuyAmmoState.CreateBuyList:
                    Console.WriteLine(state.ToString());

                    if (!Cache.Instance.InStation)
                        return;

                    if (Cache.Instance.ItemHangar == null)
                        return;

                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        return;
                    }

                    if (!Combat.Ammo.Any())
                    {
                        return;
                    }

                    var invtypes = Cache.Instance.DirectEve.InvTypes;

                    double freeCargo = (Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity);

                    if (Cache.Instance.CurrentShipsCargo.Capacity == 0)
                    {
                        Logging.Log("BuyAmmo", "if(Cache.Instance.CurrentShipsCargo.Capacity == 0)", Logging.White);
                        nextAction = DateTime.UtcNow.AddSeconds(5);
                        return;
                    }

                    Logging.Log("BuyAmmo", "Current [" + Cache.Instance.ActiveShip.GivenName + "] Cargo [" + Cache.Instance.CurrentShipsCargo.Capacity + "] Used Capacity [" + Cache.Instance.CurrentShipsCargo.UsedCapacity + "] Free Capacity [" + freeCargo + "]", Logging.White);


                    if (Drones.UseDrones)
                    {

                        List<int> droneTypeIds = new List<int>();
                        droneTypeIds.Add(Drones.DroneTypeID);

                        foreach (var factionFtting in MissionSettings.ListofFactionFittings)
                        {
                            if (factionFtting.DroneTypeID == null)
                                continue;
                            if (factionFtting.DroneTypeID == 0)
                                continue;
                            if (!droneTypeIds.Contains((int)factionFtting.DroneTypeID))
                                droneTypeIds.Add((int)factionFtting.DroneTypeID);
                        }

                        foreach (var missionFitting in MissionSettings.ListOfMissionFittings)
                        {
                            if (missionFitting.DroneTypeID == null)
                                continue;
                            if (missionFitting.DroneTypeID == 0)
                                continue;
                            if (!droneTypeIds.Contains((int)missionFitting.DroneTypeID))
                                droneTypeIds.Add((int)missionFitting.DroneTypeID);
                        }

                        foreach (int droneTypeId in droneTypeIds)
                        {
                            var totalQuantityDrones = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Quantity);

                            if (totalQuantityDrones < minimumDroneAmount)
                            {
                                Logging.Log("BuyAmmo", "Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + minimumDroneAmount + "]", Logging.White);
                                buyList.Add(Drones.DroneTypeID, Drones.BuyAmmoDroneAmmount);
                            }
                        }
                    }

                    // here we could also run through our mission xml folder and seach for the bring, trybring items and add them here?


                    if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)
                    {
                        Logging.Log("BuyAmmo", "ERROR: if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)", Logging.White);
                        state = BuyAmmoState.Error;
                        return;
                    }

                    if (Combat.Ammo.Any(a => Combat.Ammo.Count(b => b.TypeId == a.TypeId) > 1))
                    {
                        Logging.Log("BuyAmmo", "ERROR: One or more ammo types have the same type id. Fix that.", Logging.White);
                        state = BuyAmmoState.Error;
                        return;
                    }


                    foreach (var buyListKeyValuePair in buyList.ToList())
                    { // create a copy to allow removing elements

                        if (!Cache.Instance.DirectEve.InvTypes.ContainsKey(buyListKeyValuePair.Key))
                        {
                            Logging.Log("BuyAmmo", "TypeId [" + buyListKeyValuePair.Key + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.", Logging.White);
                            buyList.Remove(buyListKeyValuePair.Key);
                            continue;
                        }

                        var droneInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault(d => d.Key == buyListKeyValuePair.Key).Value;
                        var cargoBefore = freeCargo;
                        freeCargo = freeCargo - (buyList[Drones.DroneTypeID] * droneInvType.Volume);
                        Logging.Log("BuyAmmo", "Drones, Reducing freeCargo from [" + cargoBefore + "] to [" + freeCargo + "]", Logging.White);

                    }

                    freeCargo = freeCargo * 0.995; // leave 0.5% free space
                    bool majorBuySlotUsed = false;
                    foreach (var ammo in Combat.Ammo)
                    {

                        try
                        {

                            var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Quantity);
                            int minQty = ammo.Quantity * minAmmoMultiplier;
                            int maxQty = ammo.Quantity * maxAmmoMultiplier;


                            if (!Cache.Instance.DirectEve.InvTypes.ContainsKey(ammo.TypeId))
                            {
                                Logging.Log("BuyAmmo", "TypeId [" + ammo.TypeId + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.", Logging.White);
                                continue;
                            }

                            var ammoInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault(d => d.Key == ammo.TypeId).Value;
                            if (totalQuantity < minQty && !majorBuySlotUsed)
                            {
                                majorBuySlotUsed = true;
                                int ammoBuyAmount = (int)((freeCargo * 0.4) / ammoInvType.Volume); // 40% of the volume for the first missing ammo
                                buyList.Add(ammo.TypeId, ammoBuyAmount);
                            }
                            else
                            {
                                if (totalQuantity <= maxQty)
                                {
                                    int ammoBuyAmount = (int)((freeCargo * (0.6 / (Combat.Ammo.Count - 1))) / ammoInvType.Volume); // 60% for the rest
                                    buyList.Add(ammo.TypeId, ammoBuyAmount); // yes we're not using the whole cargo here if one or more types are above max qty
                                }
                            }

                        }
                        catch (Exception)
                        {

                            Logging.Log("BuyAmmo", "ERROR: foreach(var ammo in Combat.Ammo)", Logging.White);
                            state = BuyAmmoState.Error;
                            return;
                        }

                    }

                    Logging.Log("BuyAmmo", "Done building the ammoToBuy list:", Logging.White);
                    int z = 0;
                    double totalVolumeBuyList = 0;
                    foreach (var entry in buyList)
                    {
                        var buyInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault(d => d.Key == entry.Key).Value;
                        double buyTotalVolume = buyInvType.Volume * entry.Value;
                        z++;

                        Logging.Log("BuyAmmo", "[" + z + "] typeID [" + entry.Key + "] amount [" + entry.Value + "] volume [" + buyTotalVolume + "]", Logging.White);
                        totalVolumeBuyList += buyTotalVolume;


                    }

                    double currentShipFreeCargo = (Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity);
                    Logging.Log("BuyAmmo", "CurrentShipFreeCargo [" + currentShipFreeCargo + "] BuyListTotalVolume [" + totalVolumeBuyList + "]", Logging.White);

                    if (currentShipFreeCargo < totalVolumeBuyList)
                    {
                        Logging.Log("BuyAmmo", "if(currentShipFreeCargo < totalVolumeBuyList)", Logging.White);
                        state = BuyAmmoState.Error;
                        return;
                    }

                    state = BuyAmmoState.TravelToDestinationStation;

                    travelerDestination = new StationDestination(Settings.Instance.BuyAmmoStationID);

                    break;

                case BuyAmmoState.TravelToDestinationStation:
                    Console.WriteLine(state.ToString());

                    if (Cache.Instance.DirectEve.Session.IsInSpace && Cache.Instance.ActiveShip.Entity != null && Cache.Instance.ActiveShip.Entity.IsWarping)
                        return;

                    if (Traveler.Destination != travelerDestination)
                    {
                        Traveler.Destination = travelerDestination;
                    }

                    jumps = Cache.Instance.DirectEve.Navigation.GetDestinationPath().Count;

                    Traveler.ProcessState();

                    if (_States.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Logging.Log("BuyAmmo", "Arrived at destination", Logging.White);
                        state = BuyAmmoState.BuyAmmo;
                        orderIterations = 0;
                        Traveler.Destination = null;

                        return;

                    }

                    if (_States.CurrentTravelerState == TravelerState.Error)
                    {
                        if (Traveler.Destination != null)
                        {
                            Logging.Log("BuyAmmo", "Stopped traveling, traveller threw an error...", Logging.White);
                        }

                        Traveler.Destination = null;
                        state = BuyAmmoState.Error;
                        return;
                    }


                    break;
                case BuyAmmoState.BuyAmmo:

                    if (!Cache.Instance.InStation)
                        return;

                    if (Cache.Instance.ItemHangar == null)
                        return;

                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        return;
                    }

                    // Is there a market window?
                    DirectMarketWindow marketWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();


                    if (!buyList.Any())
                    {

                        // Close the market window if there is one
                        if (marketWindow != null)
                        {
                            marketWindow.Close();
                        }

                        Logging.Log("BuyAmmo", "Finished buying changing state to MoveItemsToCargo", Logging.White);
                        state = BuyAmmoState.MoveItemsToCargo;
                        return;
                    }

                    Console.WriteLine(state.ToString());

                    var currentBuyListItem = buyList.FirstOrDefault();

                    int ammoTypeId = currentBuyListItem.Key;
                    int ammoQuantity = currentBuyListItem.Value;

                    if (Cache.Instance.ItemHangar == null)
                    {
                        return;
                    }

                    // Do we have the ammo we need in the Item Hangar?

                    if (Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Quantity) >= ammoQuantity)
                    {
                        DirectItem ammoItemInHangar = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.TypeId == ammoTypeId);
                        if (ammoItemInHangar != null)
                        {
                            Logging.Log("BuyAmmo", "We have [" + Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Quantity).ToString(CultureInfo.InvariantCulture) + "] " + ammoItemInHangar.TypeName + " in the item hangar.", Logging.White);
                        }

                        buyList.Remove(ammoTypeId);
                        return;
                    }

                    // We do not have enough ammo, open the market window
                    if (marketWindow == null)
                    {
                        nextAction = DateTime.UtcNow.AddSeconds(10);

                        Logging.Log("BuyAmmo", "Opening market window", Logging.White);

                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                        return;
                    }

                    // Wait for the window to become ready
                    if (!marketWindow.IsReady)
                    {
                        return;
                    }

                    // Are we currently viewing the correct ammo orders?
                    if (marketWindow.DetailTypeId != ammoTypeId)
                    {
                        // No, load the ammo orders
                        marketWindow.LoadTypeId(ammoTypeId);

                        Logging.Log("BuyAmmo", "Loading market window", Logging.White);

                        nextAction = DateTime.UtcNow.AddSeconds(10);
                        return;
                    }

                    // Get the median sell price
                    DirectInvType type;
                    Cache.Instance.DirectEve.InvTypes.TryGetValue(ammoTypeId, out type);

                    var currentAmmoDirectItem = type;
                    double maxPrice = 0;

                    if (currentAmmoDirectItem != null)
                    {
                        double avgPrice = currentAmmoDirectItem.GetAverAgePrice;
                        double basePrice = currentAmmoDirectItem.BasePrice / currentAmmoDirectItem.PortionSize;

                        if (avgPrice != 0)
                        {
                            maxPrice = avgPrice * maxAvgPriceMultiplier; // 3 times the avg price
                        }
                        else
                        {
                            if (basePrice != 0)
                            {
                                maxPrice = basePrice * maxBasePriceMultiplier; // 6 times the base price
                            }
                            else
                            {

                                maxPrice = 1000; // if everything else falls through we limit the price to 1000 isk to not get fooled by market manipulations, this also passively disables buying drones
                            }
                        }
                        Logging.Log("BuyAmmo", "Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]", Logging.Orange);
                    }

                    // Are there any orders with an reasonable price?
                    IEnumerable<DirectOrder> orders;
                    if (maxPrice == 0)
                    {
                        Logging.Log("BuyAmmo", "if(maxPrice == 0)", Logging.Orange);
                        orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.TypeId == ammoTypeId).ToList();
                    }
                    else
                    {
                        Logging.Log("BuyAmmo", "if(maxPrice != 0) max price [" + maxPrice + "]", Logging.Orange);
                        orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == ammoTypeId).ToList();
                    }

                    orderIterations++;

                    if (!orders.Any() && orderIterations < 5)
                    {
                        nextAction = DateTime.UtcNow.AddSeconds(5);
                        return;
                    }


                    // Is there any order left?
                    if (!orders.Any())
                    {
                        Logging.Log("BuyAmmo", "No reasonably priced ammo available! Removing this item from the buyList", Logging.Orange);
                        buyList.Remove(ammoTypeId);
                        nextAction = DateTime.UtcNow.AddSeconds(3);
                        return;
                    }

                    // How much ammo do we still need?
                    int neededQuantity = ammoQuantity - Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Quantity);
                    if (neededQuantity > 0)
                    {
                        // Get the first order
                        DirectOrder order = orders.OrderBy(o => o.Price).FirstOrDefault();
                        if (order != null)
                        {

                            // Calculate how much ammo we still need
                            int remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                            order.Buy(remaining, DirectOrderRange.Station);
                            long orderPrice = (long)(remaining * order.Price);

                            if (orderPrice < Cache.Instance.MyWalletBalance)
                            {

                                Logging.Log("BuyAmmo", "Buying [" + remaining + "] ammo price [" + order.Price + "]", Logging.White);

                                // Wait for the order to go through
                                nextAction = DateTime.UtcNow.AddSeconds(10);
                            }
                            else
                            {

                                Logging.Log("BuyAmmo", "ERROR: We don't have enough ISK on our wallet to finish that transaction.", Logging.White);
                                state = BuyAmmoState.Error;
                                return;
                            }
                        }
                    }

                    break;
                case BuyAmmoState.MoveItemsToCargo:
                    Console.WriteLine(state.ToString());


                    if (!Cache.Instance.InStation)
                        return;

                    if (Cache.Instance.ItemHangar == null)
                        return;

                    if (!Combat.Ammo.Any())
                    {
                        return;
                    }

                    if (Cache.Instance.CurrentShipsCargo == null)
                    {
                        return;
                    }

                    IEnumerable<DirectItem> ammoItems = Cache.Instance.ItemHangar.Items.Where(i => Combat.Ammo.Any(r => r.TypeId == i.TypeId)).ToList();
                    if (ammoItems.Any())
                    {
                        Logging.Log("BuyAmmo", "Moving ammo to cargohold", Logging.White);
                        Cache.Instance.CurrentShipsCargo.Add(ammoItems);
                        return;
                    }

                    Logging.Log("BuyAmmo", "Done moving ammo to cargohold", Logging.White);
                    state = BuyAmmoState.Done;
                    break;

                case BuyAmmoState.Done:
                    Console.WriteLine(state.ToString());

                    if (Cache.Instance.DirectEve.Session.StationId != null && Cache.Instance.DirectEve.Session.StationId > 0 && Cache.Instance.DirectEve.Session.StationId == Settings.Instance.BuyAmmoStationID)
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                        _States.CurrentArmState = ArmState.Idle;
                    }

                    Logging.Log("BuyAmmo", "State iterations statistics: []", Logging.White);
                    foreach(var kV in stateIterations)
                    {
                        Logging.Log("BuyAmmo", "State [BuyAmmoState." + kV.Key.ToString() + "] iterations [" + kV.Value + "]" , Logging.White);
                    }

                    break;

                case BuyAmmoState.Error:
                    Console.WriteLine(state.ToString());
                    state = BuyAmmoState.DisabledForThisSession;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                    _States.CurrentArmState = ArmState.Idle;
                    Logging.Log("BuyAmmo", "ERROR. BuyAmmo should stay disabled while this session is still active.", Logging.White);
                    break;

                case BuyAmmoState.DisabledForThisSession:
                    break;
                default:
                    throw new Exception("Invalid value for BuyAmmoState");
            }
        }
    }
}

