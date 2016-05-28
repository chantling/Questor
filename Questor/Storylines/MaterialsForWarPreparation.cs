using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Storylines
{
    public class MaterialsForWarPreparation : IStoryline
    {
        //private bool OreLoaded = false;
        private DateTime _nextAction;

        /// <summary>
        ///     Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
            {
                return StorylineState.Arm;
            }

            if (Cache.Instance.ActiveShip == null || Cache.Instance.ActiveShip.GivenName == null)
            {
                if (Logging.DebugArm) Logging.Log("if (Cache.Instance.ActiveShip == null)");
                _nextAction = DateTime.UtcNow.AddSeconds(3);
                return StorylineState.Arm;
            }

            if (Cache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower())
            {
                // Open the ship hangar
                if (Cache.Instance.ShipHangar == null) return StorylineState.Arm;

                var ships = Cache.Instance.ShipHangar.Items.Where(i => i.IsSingleton).ToList();


                if (ships.Any(s => s.GroupId == (int) Group.Shuttle && s.IsSingleton && s.GivenName != null))
                {
                    ships.FirstOrDefault(s => s.GivenName != null && s.GroupId == (int) Group.Shuttle && s.IsSingleton).ActivateShip();
                    Logging.Log("Found a shuttle - Making Shuttle active");
                    _nextAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                    return StorylineState.GotoAgent;
                }

                foreach (var ship in ships.Where(ship => ship.GivenName != null && ship.GivenName.ToLower() == Settings.Instance.TransportShipName.ToLower()))
                {
                    Logging.Log("Making [" + ship.GivenName + "] active");
                    ship.ActivateShip();
                    _nextAction = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                    return StorylineState.Arm;
                }

                if (Cache.Instance.ActiveShip.GivenName.ToLower() != Settings.Instance.TransportShipName.ToLower())
                {
                    Logging.Log("Missing TransportShip named [" + Settings.Instance.TransportShipName + "]");
                    return StorylineState.GotoAgent;
                }
            }

            if (Cache.Instance.ItemHangar == null) return StorylineState.Arm;

            IEnumerable<DirectItem> items = Cache.Instance.ItemHangar.Items.Where(k => k.TypeId == MissionSettings.MaterialsForWarOreID).ToList();
            if (!items.Any())
            {
                if (Logging.DebugArm)
                    Logging.Log("Ore for MaterialsForWar: typeID [" + MissionSettings.MaterialsForWarOreID + "] not found in ItemHangar");
                items = Cache.Instance.AmmoHangar.Items.Where(k => k.TypeId == MissionSettings.MaterialsForWarOreID).ToList();
                if (!items.Any())
                {
                    if (Logging.DebugArm)
                        Logging.Log("Ore for MaterialsForWar: typeID [" + MissionSettings.MaterialsForWarOreID + "] not found in AmmoHangar");
                    //
                    // if we do not have the ore... either we can blacklist it right here, or continue normally
                    //
                    return StorylineState.GotoAgent;
                    //return StorylineState.BlacklistAgent;
                }
            }

            var oreIncargo = 0;
            foreach (var cargoItem in Cache.Instance.CurrentShipsCargo.Items.ToList())
            {
                if (cargoItem.TypeId != MissionSettings.MaterialsForWarOreID)
                    continue;

                oreIncargo += cargoItem.Quantity;
                continue;
            }

            var oreToLoad = MissionSettings.MaterialsForWarOreQty - oreIncargo;
            if (oreToLoad <= 0)
            {
                //OreLoaded = true;
                return StorylineState.GotoAgent;
            }

            var item = items.FirstOrDefault();
            if (item != null)
            {
                var moveOreQuantity = Math.Min(item.Stacksize, oreToLoad);
                Cache.Instance.CurrentShipsCargo.Add(item, moveOreQuantity);
                Logging.Log("Moving [" + moveOreQuantity + "] units of Ore [" + item.TypeName + "] Stack size: [" + item.Stacksize + "] from hangar to CargoHold");
                _nextAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(3, 6));
                return StorylineState.Arm; // you can only move one set of items per frame
            }

            Logging.Log("defined TransportShip found, going in active ship");
            return StorylineState.GotoAgent;
        }

        /// <summary>
        ///     Check if we have kernite in station
        /// </summary>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            var directEve = Cache.Instance.DirectEve;
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.PreAcceptMission;

            // the ore and ore quantity can be stored in the characters settings xml this is to facility mission levels other than 4.
            //The defaults are for level 4 so it will not break for those people that do not include these in their settings file
            //  Level 1         <MaterialsForWarOreID>1230</MaterialsForWarOreID>
            //                  <MaterialsForWarOreQty>999</MaterialsForWarOreQty>
            //  Level 4         <MaterialsForWarOreID>20</MaterialsForWarOreID>
            //                  <MaterialsForWarOreQty>8000</MaterialsForWarOreQty>

            var oreid = MissionSettings.MaterialsForWarOreID; //1230;
            var orequantity = MissionSettings.MaterialsForWarOreQty; //999

            // Open the item hangar
            if (Cache.Instance.ItemHangar == null) return StorylineState.PreAcceptMission;

            //if (Cache.Instance.ItemHangar.Window == null)
            //{
            //    Logging.Log("MaterialsForWar", "PreAcceptMission: ItemHangar is null", Logging.Orange);
            //    if (!Cache.Instance.ReadyItemsHangar("MaterialsForWarPreparation")) return StorylineState.PreAcceptMission;
            //    return StorylineState.PreAcceptMission;
            //}

            // Is there a market window?
            var marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            // Do we have the ore we need in the Item Hangar?.

            if (Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity) >= orequantity)
            {
                var thisOreInhangar = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.TypeId == oreid);
                if (thisOreInhangar != null)
                {
                    Logging.Log("We have [" + Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity).ToString(CultureInfo.InvariantCulture) +
                        "] " + thisOreInhangar.TypeName + " in the item hangar accepting mission");
                }

                // Close the market window if there is one
                if (marketWindow != null)
                {
                    marketWindow.Close();
                }

                return StorylineState.AcceptMission;
            }

            if (Cache.Instance.CurrentShipsCargo == null) return StorylineState.PreAcceptMission;

            if (Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity) >= orequantity)
            {
                var thisOreInhangar = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == oreid);
                if (thisOreInhangar != null)
                {
                    Logging.Log("We have [" +
                        Cache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity).ToString(CultureInfo.InvariantCulture) + "] " +
                        thisOreInhangar.TypeName + " in the CargoHold accepting mission");
                }

                // Close the market window if there is one
                if (marketWindow != null)
                {
                    marketWindow.Close();
                }

                return StorylineState.AcceptMission;
            }

            if (true)
            {
                // We do not have enough ore, open the market window
                if (marketWindow == null)
                {
                    _nextAction = DateTime.UtcNow.AddSeconds(10);

                    Logging.Log("Opening market window");

                    directEve.ExecuteCommand(DirectCmd.OpenMarket);
                    Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                    return StorylineState.PreAcceptMission;
                }

                // Wait for the window to become ready (this includes loading the ore info)
                if (!marketWindow.IsReady)
                {
                    return StorylineState.PreAcceptMission;
                }

                // Are we currently viewing ore orders?
                if (marketWindow.DetailTypeId != oreid)
                {
                    // No, load the ore orders
                    marketWindow.LoadTypeId(oreid);

                    Logging.Log("Loading market window");

                    _nextAction = DateTime.UtcNow.AddSeconds(5);
                    return StorylineState.PreAcceptMission;
                }

                // Get the median sell price
                DirectInvType type;
                Cache.Instance.DirectEve.InvTypes.TryGetValue(oreid, out type);

                var OreTypeNeededForThisMission = type;
                double maxPrice = 0;

                if (OreTypeNeededForThisMission != null)
                {
                    maxPrice = OreTypeNeededForThisMission.BasePrice/OreTypeNeededForThisMission.PortionSize;
                    maxPrice = maxPrice*10;
                }

                IEnumerable<DirectOrder> orders;

                if (maxPrice != 0)
                {
                    orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId && o.Price < maxPrice).ToList();
                }
                else
                {
                    orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();
                }

                // Do we have orders that sell enough ore for the mission?

                orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();
                if (!orders.Any() || orders.Sum(o => o.VolumeRemaining) < orequantity)
                {
                    Logging.Log("Not enough (reasonably priced) ore available! Blacklisting agent for this Questor session! maxPrice [" + maxPrice + "]");

                    // Close the market window
                    marketWindow.Close();

                    // No, black list the agent in this Questor session (note we will never decline storylines!)
                    return StorylineState.BlacklistAgent;
                }

                // How much ore do we still need?
                var neededQuantity = orequantity - Cache.Instance.ItemHangar.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity);
                if (neededQuantity > 0)
                {
                    // Get the first order
                    var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                    if (order != null)
                    {
                        // Calculate how much ore we still need
                        var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                        order.Buy(remaining, DirectOrderRange.Station);

                        Logging.Log("Buying [" + remaining + "] ore");

                        // Wait for the order to go through
                        _nextAction = DateTime.UtcNow.AddSeconds(10);
                    }
                }
                return StorylineState.PreAcceptMission;
            }
        }

        /// <summary>
        ///     We have no execute mission code
        /// </summary>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            return StorylineState.CompleteMission;
        }

        /// <summary>
        ///     We have no combat/delivery part in this mission, just accept it
        /// </summary>
        /// <returns></returns>
        public StorylineState PostAcceptMission(Storyline storyline)
        {
            // Close the market window (if its open)
            return StorylineState.CompleteMission;
        }
    }
}