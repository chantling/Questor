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
		private static Dictionary<int,int> buyList = new Dictionary<int, int>();
		private static int minimumDroneAmount = 200;
		private static DateTime nextAction = DateTime.MinValue;
		private static int _jumps;
		private static TravelerDestination travelerDestination;
		private static int orderIterations = 0;
		public static bool error { get; set; }
		
		public static void ProcessState()
		{
			
			if(nextAction > DateTime.UtcNow)
				return;
			
			switch (state) {
					
				case BuyAmmoState.Idle:
					Console.WriteLine(state.ToString());
					state = BuyAmmoState.AmmoCheck;
					break;
					
				case BuyAmmoState.AmmoCheck:
					Console.WriteLine(state.ToString());
					
					if(!Cache.Instance.InStation)
						return;
					
					if(Cache.Instance.ItemHangar == null)
						return;
					
					if(!Combat.Ammo.Any()) {
						return;
					}
					
					buyList = new Dictionary<int, int>();
					
					bool buy = false;
					
					foreach(var ammo in Combat.Ammo) {
						
						var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Quantity);
						int minQty = ammo.Quantity * 4;
						if(totalQuantity < minQty) {
							Logging.Log("BuyAmmo", "Total ammo amount in hangar [" + totalQuantity + "] Minimum amount [" + minQty + "] We're going to buy ammo.", Logging.White);
							buy = true;
							break;
						}
						
					}
					
					if(Drones.UseDrones && Drones.DroneTypeID != 0) {
						var totalQuantityDrones = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == Drones.DroneTypeID).Sum(i => i.Quantity);
						if(totalQuantityDrones < minimumDroneAmount) {
							Logging.Log("BuyAmmo", "Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + minimumDroneAmount + "] We're going to buy drones.", Logging.White);
							buy = true;
						}
					}
					
					if(buy) {
						state = BuyAmmoState.ActivateTransportShip;
					} else {
						state = BuyAmmoState.Done;
					}
					
					
					break;
				case BuyAmmoState.ActivateTransportShip:
					Console.WriteLine(state.ToString());
					
					
					if(!Cache.Instance.InStation)
						return;
					
					if(Cache.Instance.ItemHangar == null)
						return;
					
					if(!Combat.Ammo.Any()) {
						return;
					}
					
					if(Cache.Instance.ShipHangar == null)
						return;
					
					if(Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null && Cache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower()) {
						state = BuyAmmoState.CreateBuyList;
						return;
					}
					
					if(Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName != null && Cache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower()) {
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
					
					if(!Cache.Instance.InStation)
						return;
					
					if(Cache.Instance.ItemHangar == null)
						return;
					
					if(!Combat.Ammo.Any()) {
						return;
					}
					
					if(Cache.Instance.CurrentShipsCargo == null) {
						return;
					}
					
					var invtypes = Cache.Instance.DirectEve.InvTypes;
					
					double freeCargo = (Cache.Instance.CurrentShipsCargo.Capacity-Cache.Instance.CurrentShipsCargo.UsedCapacity);
					
					if(Cache.Instance.CurrentShipsCargo.Capacity == 0) {
						Logging.Log("BuyAmmo", "if(Cache.Instance.CurrentShipsCargo.Capacity == 0)", Logging.White);
						nextAction = DateTime.UtcNow.AddSeconds(5);
						return;
					}
					
					Logging.Log("BuyAmmo", "Current [" + Cache.Instance.ActiveShip.GivenName + "] Cargo [" + Cache.Instance.CurrentShipsCargo.Capacity + "] Used Capacity [" + Cache.Instance.CurrentShipsCargo.UsedCapacity + "] Free Capacity [" +  freeCargo + "]" , Logging.White);
					
					
					if(Drones.UseDrones && Drones.DroneTypeID != 0) {
						var totalQuantityDrones = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == Drones.DroneTypeID).Sum(i => i.Quantity);
						if(totalQuantityDrones < minimumDroneAmount) {
							buyList.Add(Drones.DroneTypeID,Drones.BuyAmmoDroneAmmount);
						}
					}
					
					if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)
					{
						Logging.Log("BuyAmmo", "ERROR: if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)", Logging.White);
						state = BuyAmmoState.Error;
						return;
					}
					
					if(buyList.ContainsKey(Drones.DroneTypeID)) {
						
						
						if(invtypes.Any(d => d.Key == Drones.DroneTypeID)) {
							var droneInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault( d => d.Key == Drones.DroneTypeID).Value;
							var cargoBefore = freeCargo;
							freeCargo = freeCargo - (buyList[Drones.DroneTypeID] * droneInvType.Volume);
							Logging.Log("BuyAmmo","BuyList contains DroneTypeID, reducing freeCargo from [" + cargoBefore  +"] to [" + freeCargo + "]", Logging.White);
						}
						
					}
					
					freeCargo = freeCargo * 0.99; // leave 1% space
					int n = 0;
					bool fiftyPercentAmmoBuySlotAlreadyUsed = false;
					foreach(var ammo in Combat.Ammo) {
						
						if(n > 3)
							break;
						
						try {
							
							var totalQuantity = Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Quantity);
							int minQty = ammo.Quantity * 4;
							var ammoInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault( d => d.Key == ammo.TypeId).Value;
							if(totalQuantity < minQty && !fiftyPercentAmmoBuySlotAlreadyUsed) {
								fiftyPercentAmmoBuySlotAlreadyUsed = true;
								int ammoBuyAmount = (int)((freeCargo * 0.5) / ammoInvType.Volume);
								buyList.Add(ammo.TypeId,ammoBuyAmount);
							} else {
								int ammoBuyAmount = (int)((freeCargo * (0.5/3)) / ammoInvType.Volume);
								buyList.Add(ammo.TypeId,ammoBuyAmount);
							}
							
						} catch (Exception) {
							
							Logging.Log("BuyAmmo", "ERROR: foreach(var ammo in Combat.Ammo)", Logging.White);
							state = BuyAmmoState.Error;
							return;
						}
						n++;
					}
					
					Logging.Log("BuyAmmo", "Builded the AmmoToBuy list. AmmoToBuyList []", Logging.White);
					int z = 0;
					double totalVolumeBuyList = 0;
					foreach(var entry in buyList) {
						var buyInvType = Cache.Instance.DirectEve.InvTypes.FirstOrDefault( d => d.Key == entry.Key).Value;
						double buyTotalVolume = buyInvType.Volume * entry.Value;
						z++;
						
						Logging.Log("BuyAmmo", "[" + z + "] typeID [" + entry.Key + "] amount [" + entry.Value + "] volume [" + buyTotalVolume + "]", Logging.White);
						totalVolumeBuyList += buyTotalVolume;
						
						
					}
					
					double currentShipFreeCargo = (Cache.Instance.CurrentShipsCargo.Capacity-Cache.Instance.CurrentShipsCargo.UsedCapacity);
					Logging.Log("BuyAmmo", "CurrentShipFreeCargo [" + currentShipFreeCargo  + "] BuyListTotalVolume [" + totalVolumeBuyList + "]", Logging.White);
					
					if(currentShipFreeCargo < totalVolumeBuyList) {
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

					_jumps = Cache.Instance.DirectEve.Navigation.GetDestinationPath().Count;

					Traveler.ProcessState();

					if ( _States.CurrentTravelerState == TravelerState.AtDestination)
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
					
					// Is there a market window?
					DirectMarketWindow marketWindow = Cache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
					
					
					if(!buyList.Any()) {
						
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
					
					if(Cache.Instance.ItemHangar == null) {
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
						double basePrice = currentAmmoDirectItem.BasePrice;
						if(avgPrice != 0) {
							maxPrice = avgPrice * 5;
						}
						else {
							maxPrice = basePrice * 10;
						}
						Logging.Log("BuyAmmo", "Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]", Logging.Orange);
					}
					
					// Do we have orders that sell enough ammo for the mission?
					IEnumerable<DirectOrder> orders;
					if(maxPrice == 0) {
						Logging.Log("BuyAmmo", "if(maxPrice == 0)", Logging.Orange);
						orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.TypeId == ammoTypeId).ToList();
					} else {
						Logging.Log("BuyAmmo", "if(maxPrice != 0) max price [" + maxPrice + "]", Logging.Orange);
						orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == ammoTypeId).ToList();
					}
					
					orderIterations++;
					
					if(!orders.Any() && orderIterations < 5) {
						nextAction = DateTime.UtcNow.AddSeconds(5);
						return;
					}
					
					if (!orders.Any() || orders.Sum(o => o.VolumeRemaining) < ammoQuantity)
					{
						Logging.Log("BuyAmmo", "ERROR: Not enough (reasonably priced) ammo available! Moving (maybe) bought ammo and going home.", Logging.Orange);

						// Close the market window
						marketWindow.Close();

						state = BuyAmmoState.MoveItemsToCargo;
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

							Logging.Log("BuyAmmo", "Buying [" + remaining + "] ammo price [" + order.Price + "]", Logging.White);

							// Wait for the order to go through
							nextAction = DateTime.UtcNow.AddSeconds(10);
						}
					}
					
					break;
				case BuyAmmoState.MoveItemsToCargo:
					Console.WriteLine(state.ToString());
					
					
					if(!Cache.Instance.InStation)
						return;
					
					if(Cache.Instance.ItemHangar == null)
						return;
					
					if(!Combat.Ammo.Any()) {
						return;
					}
					
					if(Cache.Instance.CurrentShipsCargo == null) {
						return;
					}
					
					IEnumerable<DirectItem> ammoItems = Cache.Instance.ItemHangar.Items.Where(i => Combat.Ammo.Any(r => r.TypeId == i.TypeId)).ToList();
					if (ammoItems.Any())
					{
						Logging.Log("Arm", "Moving ammo to cargohold", Logging.White);
						Cache.Instance.CurrentShipsCargo.Add(ammoItems);
						return;
					}
					
					Logging.Log("Arm", "Done moving ammo to cargohold", Logging.White);
					
					
					state = BuyAmmoState.Done;
					break;

				case BuyAmmoState.Done:
					Console.WriteLine(state.ToString());
					_States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
					// set the timer here to questorlauncher to determine how often we are allowed to check and buy ammo
					break;
					
				case BuyAmmoState.Error:
					Console.WriteLine(state.ToString());
					state = BuyAmmoState.DisabledForThisSession;
					_States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
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

