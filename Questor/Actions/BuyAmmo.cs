﻿/*
 * ---------------------------------------
 * User: duketwo
 * Date: 09.10.2015
 * Time: 11:08
 * 
 * ---------------------------------------
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Actions
{
    /// <summary>
    ///     Description of BuyAmmo.
    /// </summary>
    public static class BuyAmmo
    {
        private static Dictionary<int, int> buyList = new Dictionary<int, int>();
        private static Dictionary<int, int> moveToCargoList = new Dictionary<int, int>();
        private static int minimumDroneAmount = 200;
        private static DateTime nextAction = DateTime.MinValue;
        private static int jumps;
        private static TravelerDestination travelerDestination;
        private static int orderIterations = 0;

        private static Dictionary<BuyAmmoState, int> stateIterations = new Dictionary<BuyAmmoState, int>();
        private static int maxStateIterations = 500;

        private static int hoursBetweenAmmoBuy = 10;
        private static int minAmmoMultiplier = 20;
        private static int maxAmmoMultiplier = 100;
        private static int maxAvgPriceMultiplier = 3;
        private static int maxBasePriceMultiplier = 10;

        public static BuyAmmoState state { get; set; } // idle == default
        public static bool error { get; set; }

        private static bool StateCheckEveryPulse
        {
            get
            {
                if (stateIterations.ContainsKey(state))
                    stateIterations[state] = stateIterations[state] + 1;
                else
                    stateIterations.AddOrUpdate(state, 1);

                if (stateIterations[state] >= maxStateIterations)
                {
                    Logging.Log("ERROR:  if (stateIterations[state] >= maxStateIterations)");
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
                        Logging.Log("We were buying ammo already in the past [" + hoursBetweenAmmoBuy + "] hours.");
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
                    moveToCargoList = new Dictionary<int, int>();

                    var buy = false;

                    foreach (var ammo in Combat.Ammo)
                    {
                        var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);
                        var minQty = ammo.Quantity*minAmmoMultiplier;
                        if (totalQuantity < minQty)
                        {
                            Logging.Log("Total ammo amount in hangar type [" + ammo.TypeId + "] [" + totalQuantity + "] Minimum amount [" + minQty +
                                "] We're going to buy ammo.");
                            buy = true;
                            break;
                        }
                    }

                    if (Drones.UseDrones)
                    {
                        var droneTypeIds = new List<int>();
                        droneTypeIds.Add(Drones.DroneTypeID);

                        foreach (var factionFtting in MissionSettings.ListofFactionFittings)
                        {
                            if (factionFtting.DroneTypeID == null)
                                continue;
                            if (factionFtting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) factionFtting.DroneTypeID))
                                droneTypeIds.Add((int) factionFtting.DroneTypeID);
                        }

                        foreach (var missionFitting in MissionSettings.ListOfMissionFittings)
                        {
                            if (missionFitting.DroneTypeID == null)
                                continue;
                            if (missionFitting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) missionFitting.DroneTypeID))
                                droneTypeIds.Add((int) missionFitting.DroneTypeID);
                        }

                        foreach (var droneTypeId in droneTypeIds)
                        {
                            var totalQuantityDrones = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Stacksize);
                            if (totalQuantityDrones < minimumDroneAmount)
                            {
                                Logging.Log("Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + minimumDroneAmount +
                                    "] We're going to buy drones of type [" + droneTypeId + "]");
                                buy = true;
                            }
                        }
                    }


                    Logging.Log("LastAmmoBuy was on [" + Cache.Instance.EveAccount.LastAmmoBuy + "]");


                    if (buy)
                    {
                        state = BuyAmmoState.ActivateTransportShip;
                    }
                    else
                    {
                        Logging.Log("There is still enough ammo avaiable in the itemhangar. Changing state to done.");
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

                    if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null &&
                        Cache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower())
                    {
                        state = BuyAmmoState.CreateBuyList;
                        return;
                    }

                    if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null &&
                        Cache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower())
                    {
                        var ships = Cache.Instance.ShipHangar.Items;
                        foreach (
                            var ship in ships.Where(ship => ship.GivenName != null && ship.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower())
                            )
                        {
                            Logging.Log("Making [" + ship.GivenName + "] active");
                            ship.ActivateShip();
                            nextAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
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

                    var freeCargo = (Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity);

                    if (Cache.Instance.CurrentShipsCargo.Capacity == 0)
                    {
                        Logging.Log("if(Cache.Instance.CurrentShipsCargo.Capacity == 0)");
                        nextAction = DateTime.UtcNow.AddSeconds(5);
                        return;
                    }

                    Logging.Log("Current [" + Cache.Instance.ActiveShip.GivenName + "] Cargo [" + Cache.Instance.CurrentShipsCargo.Capacity + "] Used Capacity [" +
                        Cache.Instance.CurrentShipsCargo.UsedCapacity + "] Free Capacity [" + freeCargo + "]");


                    if (Drones.UseDrones)
                    {
                        var droneTypeIds = new List<int>();
                        droneTypeIds.Add(Drones.DroneTypeID);

                        foreach (var factionFtting in MissionSettings.ListofFactionFittings)
                        {
                            if (factionFtting.DroneTypeID == null)
                                continue;
                            if (factionFtting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) factionFtting.DroneTypeID))
                                droneTypeIds.Add((int) factionFtting.DroneTypeID);
                        }

                        foreach (var missionFitting in MissionSettings.ListOfMissionFittings)
                        {
                            if (missionFitting.DroneTypeID == null)
                                continue;
                            if (missionFitting.DroneTypeID <= 0)
                                continue;
                            if (!droneTypeIds.Contains((int) missionFitting.DroneTypeID))
                                droneTypeIds.Add((int) missionFitting.DroneTypeID);
                        }

                        foreach (var droneTypeId in droneTypeIds.Distinct())
                        {
                            var totalQuantityDrones = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Stacksize);

                            if (totalQuantityDrones < minimumDroneAmount)
                            {
                                Logging.Log("Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + minimumDroneAmount + "]");
                                buyList.AddOrUpdate(droneTypeId, Drones.BuyAmmoDroneAmmount);
                            }
                        }
                    }

                    // here we could also run through our mission xml folder and seach for the bring, trybring items and add them here ( if we dont have them in our hangar )
                    if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)
                    {
                        Logging.Log("ERROR: if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)");
                        state = BuyAmmoState.Error;
                        return;
                    }

//					if (Combat.Ammo.Any(a => Combat.Ammo.Count(b => b.TypeId == a.TypeId) > 1))
//					{
//						Logging.Log("BuyAmmo", "ERROR: One or more ammo types have the same type id. Fix that.", Logging.White);
//						state = BuyAmmoState.Error;
//						return;
//					}


                    foreach (var buyListKeyValuePair in buyList.ToList())
                    {
                        // create a copy to allow removing elements

                        if (!Cache.Instance.DirectEve.InvTypes.ContainsKey(buyListKeyValuePair.Key))
                        {
                            Logging.Log("TypeId [" + buyListKeyValuePair.Key + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                            buyList.Remove(buyListKeyValuePair.Key);
                            continue;
                        }

                        var droneInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault(d => d.Key == buyListKeyValuePair.Key).Value;
                        var cargoBefore = freeCargo;
                        freeCargo = freeCargo - (buyListKeyValuePair.Value*droneInvType.Volume);
                        Logging.Log("Drones, Reducing freeCargo from [" + cargoBefore + "] to [" + freeCargo + "]");
                    }

                    freeCargo = freeCargo*0.995; // leave 0.5% free space
                    var majorBuySlotUsed = false;
                    foreach (var ammo in Combat.Ammo)
                    {
                        try
                        {
                            var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);
                            var minQty = ammo.Quantity*minAmmoMultiplier;
                            var maxQty = ammo.Quantity*maxAmmoMultiplier;


                            if (!Cache.Instance.DirectEve.InvTypes.ContainsKey(ammo.TypeId))
                            {
                                Logging.Log("TypeId [" + ammo.TypeId + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                                continue;
                            }

                            var ammoInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault(d => d.Key == ammo.TypeId).Value;
                            if (totalQuantity < minQty && !majorBuySlotUsed)
                            {
                                majorBuySlotUsed = true;
                                var ammoBuyAmount = (int) ((freeCargo*0.4)/ammoInvType.Volume); // 40% of the volume for the first missing ammo
                                buyList.AddOrUpdate(ammo.TypeId, ammoBuyAmount);
                            }
                            else
                            {
                                if (totalQuantity <= maxQty)
                                {
                                    var ammoBuyAmount = (int) ((freeCargo*(0.6/(Combat.Ammo.Count - 1)))/ammoInvType.Volume); // 60% for the rest
                                    buyList.AddOrUpdate(ammo.TypeId, ammoBuyAmount);
                                        // yes we're not using the whole cargo here if one or more types are above max qty
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Logging.Log("ERROR: foreach(var ammo in Combat.Ammo)");
                            Logging.Log("Stacktrace [" + e.StackTrace + "]");
                            state = BuyAmmoState.Error;
                            return;
                        }
                    }

                    Logging.Log("Done building the ammoToBuy list:");
                    var z = 0;
                    double totalVolumeBuyList = 0;
                    foreach (var entry in buyList)
                    {
                        var buyInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault(d => d.Key == entry.Key).Value;
                        var buyTotalVolume = buyInvType.Volume*entry.Value;
                        z++;

                        Logging.Log("[" + z + "] typeID [" + entry.Key + "] amount [" + entry.Value + "] volume [" + buyTotalVolume + "]");
                        totalVolumeBuyList += buyTotalVolume;
                    }

                    var currentShipFreeCargo = (Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity);
                    Logging.Log("CurrentShipFreeCargo [" + currentShipFreeCargo + "] BuyListTotalVolume [" + totalVolumeBuyList + "]");

                    if (currentShipFreeCargo < totalVolumeBuyList)
                    {
                        Logging.Log("if(currentShipFreeCargo < totalVolumeBuyList)");
                        state = BuyAmmoState.Error;
                        return;
                    }

                    state = BuyAmmoState.TravelToDestinationStation;

                    foreach (var entry in buyList)
                    {
                        moveToCargoList.Add(entry.Key, entry.Value);
                    }

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
                        Logging.Log("Arrived at destination");
                        state = BuyAmmoState.BuyAmmo;
                        orderIterations = 0;
                        Traveler.Destination = null;

                        return;
                    }

                    if (_States.CurrentTravelerState == TravelerState.Error)
                    {
                        if (Traveler.Destination != null)
                        {
                            Logging.Log("Stopped traveling, traveller threw an error...");
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
                    var marketWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();


                    if (!buyList.Any())
                    {
                        // Close the market window if there is one
                        if (marketWindow != null)
                        {
                            marketWindow.Close();
                        }

                        Logging.Log("Finished buying changing state to MoveItemsToCargo");
                        state = BuyAmmoState.MoveItemsToCargo;
                        return;
                    }

                    Console.WriteLine(state.ToString());

                    var currentBuyListItem = buyList.FirstOrDefault();

                    var ammoTypeId = currentBuyListItem.Key;
                    var ammoQuantity = currentBuyListItem.Value;

                    if (Cache.Instance.ItemHangar == null)
                    {
                        return;
                    }

                    // Do we have the ammo we need in the Item Hangar?

                    if (Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Stacksize) >= ammoQuantity)
                    {
                        var ammoItemInHangar = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.TypeId == ammoTypeId);
                        if (ammoItemInHangar != null)
                        {
                            Logging.Log("We have [" +
                                Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Stacksize).ToString(CultureInfo.InvariantCulture) +
                                "] " + ammoItemInHangar.TypeName + " in the item hangar.");
                        }

                        buyList.Remove(ammoTypeId);
                        return;
                    }

                    // We do not have enough ammo, open the market window
                    if (marketWindow == null)
                    {
                        nextAction = DateTime.UtcNow.AddSeconds(10);

                        Logging.Log("Opening market window");

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

                        Logging.Log("Loading market window");

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
                        var avgPrice = currentAmmoDirectItem.GetAverAgePrice;
                        var basePrice = currentAmmoDirectItem.BasePrice/currentAmmoDirectItem.PortionSize;

                        Logging.Log("Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "] groupID [" +
                            currentAmmoDirectItem.GroupId + "] groupName [" + currentAmmoDirectItem.GroupId + "]");


                        if (avgPrice != 0)
                        {
                            maxPrice = avgPrice*maxAvgPriceMultiplier; // 3 times the avg price
                        }
                        else
                        {
                            if (basePrice != 0)
                            {
                                maxPrice = basePrice*maxBasePriceMultiplier; // 6 times the base price
                            }
                            else
                            {
                                maxPrice = 1000;
                                    // if everything else falls through we limit the price to 1000 isk to not get fooled by market manipulations, this also passively disables buying drones
                            }
                        }
                        Logging.Log("Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]");
                    }

                    // Are there any orders with an reasonable price?
                    IEnumerable<DirectOrder> orders;
                    if (maxPrice == 0)
                    {
                        Logging.Log("if(maxPrice == 0)");
                        orders =
                            marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.TypeId == ammoTypeId).ToList();
                    }
                    else
                    {
                        Logging.Log("if(maxPrice != 0) max price [" + maxPrice + "]");
                        orders =
                            marketWindow.SellOrders.Where(
                                o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == ammoTypeId).ToList();
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
                        Logging.Log("No reasonably priced ammo available! Removing this item from the buyList");
                        buyList.Remove(ammoTypeId);
                        nextAction = DateTime.UtcNow.AddSeconds(3);
                        return;
                    }

                    // How much ammo do we still need?
                    var neededQuantity = ammoQuantity - Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Stacksize);
                    if (neededQuantity > 0)
                    {
                        // Get the first order
                        var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                        if (order != null)
                        {
                            // Calculate how much ammo we still need
                            var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                            var orderPrice = (long) (remaining*order.Price);

                            if (orderPrice < Cache.Instance.MyWalletBalance)
                            {
                                Logging.Log("Buying [" + remaining + "] ammo price [" + order.Price + "]");
                                order.Buy(remaining, DirectOrderRange.Station);

                                // Wait for the order to go through
                                nextAction = DateTime.UtcNow.AddSeconds(10);
                            }
                            else
                            {
                                Logging.Log("ERROR: We don't have enough ISK on our wallet to finish that transaction.");
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

                    IEnumerable<DirectItem> ammoItems = Cache.Instance.ItemHangar.Items.Where(i => moveToCargoList.ContainsKey(i.TypeId)).ToList();
                    if (ammoItems.Any())
                    {
                        var ammoItem = ammoItems.FirstOrDefault();

                        var maxVolumeToMove = Math.Min(ammoItem.Stacksize, moveToCargoList[ammoItem.TypeId]);
                        maxVolumeToMove = Math.Max(1, maxVolumeToMove);

                        Logging.Log("Moving ammo to cargohold");
                        Cache.Instance.CurrentShipsCargo.Add(ammoItem, maxVolumeToMove);
                        return;
                    }

                    Logging.Log("Done moving ammo to cargohold");
                    state = BuyAmmoState.Done;
                    break;

                case BuyAmmoState.Done:
//					Console.WriteLine(state.ToString());

                    if (Cache.Instance.DirectEve.Session.StationId != null && Cache.Instance.DirectEve.Session.StationId > 0 &&
                        Cache.Instance.DirectEve.Session.StationId == Settings.Instance.BuyAmmoStationID)
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                        _States.CurrentArmState = ArmState.Idle;
                    }

//					Logging.Log("BuyAmmo", "State iterations statistics: []", Logging.White);
//					foreach(var kV in stateIterations)
//					{
//						Logging.Log("BuyAmmo", "State [BuyAmmoState." + kV.Key.ToString() + "] iterations [" + kV.Value + "]" , Logging.White);
//					}

                    break;

                case BuyAmmoState.Error:
                    Console.WriteLine(state.ToString());
                    state = BuyAmmoState.DisabledForThisSession;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                    _States.CurrentArmState = ArmState.Idle;
                    Logging.Log("ERROR. BuyAmmo should stay disabled while this session is still active.");
                    break;

                case BuyAmmoState.DisabledForThisSession:
                    break;
                default:
                    throw new Exception("Invalid value for BuyAmmoState");
            }
        }
    }
}