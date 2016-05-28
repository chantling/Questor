using System;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public class Buy
    {
        private DateTime _lastAction;

        private bool _returnBuy;
        public int Item { get; set; }

        public int Unit { get; set; }

        public bool useOrders { get; set; }

        public void ProcessState()
        {
            if (!Cache.Instance.InStation)
                return;

            if (Cache.Instance.InSpace)
                return;

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20))
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return;

            var marketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            switch (_States.CurrentBuyState)
            {
                case BuyState.Idle:
                case BuyState.Done:
                    break;

                case BuyState.Begin:

                    // Close the market window if there is one
                    if (marketWindow != null)
                        marketWindow.Close();
                    _States.CurrentBuyState = BuyState.OpenMarket;
                    break;

                case BuyState.OpenMarket:
                    // Close the market window if there is one
                    //if (marketWindow != null)
                    //    marketWindow.Close();

                    if (marketWindow == null)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        Statistics.LogWindowActionToWindowLog("MarketWindow", "Opening MarketWindow");
                        break;
                    }

                    if (!marketWindow.IsReady)
                        break;

                    Logging.Logging.Log("Buy", "Opening Market", Logging.Logging.White);
                    _States.CurrentBuyState = BuyState.LoadItem;

                    break;

                case BuyState.LoadItem:

                    _lastAction = DateTime.UtcNow;

                    if (marketWindow != null && marketWindow.DetailTypeId != Item)
                    {
                        marketWindow.LoadTypeId(Item);
                        if (useOrders)
                        {
                            _States.CurrentBuyState = BuyState.CreateOrder;
                        }
                        else
                        {
                            _States.CurrentBuyState = BuyState.BuyItem;
                        }

                        break;
                    }

                    break;

                case BuyState.CreateOrder:

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 5)
                        break;

                    _lastAction = DateTime.UtcNow;

                    if (marketWindow != null)
                    {
                        var orders = marketWindow.BuyOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId);

                        var order = orders.OrderByDescending(o => o.Price).FirstOrDefault();
                        if (order != null)
                        {
                            var price = order.Price + 0.01;
                            if (Cache.Instance.DirectEve.Session.StationId != null)
                            {
                                Cache.Instance.DirectEve.Buy((int) Cache.Instance.DirectEve.Session.StationId, Item, price, Unit, DirectOrderRange.Station, 1,
                                    30);
                            }
                        }
                        useOrders = false;
                        _States.CurrentBuyState = BuyState.Done;
                    }

                    break;

                case BuyState.BuyItem:

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 5)
                        break;

                    if (marketWindow != null)
                    {
                        var orders = marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId);

                        var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                        if (order != null)
                        {
                            // Calculate how much we still need
                            if (order.VolumeEntered >= Unit)
                            {
                                order.Buy(Unit, DirectOrderRange.Station);
                                _States.CurrentBuyState = BuyState.WaitForItems;
                            }
                            else
                            {
                                order.Buy(Unit, DirectOrderRange.Station);
                                Unit = Unit - order.VolumeEntered;
                                Logging.Logging.Log("Buy", "Missing " + Convert.ToString(Unit) + " units", Logging.Logging.White);
                                _returnBuy = true;
                                _States.CurrentBuyState = BuyState.WaitForItems;
                            }
                        }
                    }

                    break;

                case BuyState.WaitForItems:
                    // Wait 5 seconds after moving
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 5)
                        break;

                    // Close the market window if there is one
                    if (marketWindow != null)
                        marketWindow.Close();

                    if (_returnBuy)
                    {
                        Logging.Logging.Log("Buy", "Return Buy", Logging.Logging.White);
                        _returnBuy = false;
                        _States.CurrentBuyState = BuyState.OpenMarket;
                        break;
                    }

                    Logging.Logging.Log("Buy", "Done", Logging.Logging.White);
                    _States.CurrentBuyState = BuyState.Done;

                    break;
            }
        }
    }
}