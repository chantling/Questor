using System;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.States;

namespace QuestorManager.Actions
{
    public class BuyLPI
    {
        private static DateTime _lastAction;
        private static DateTime _loyaltyPointTimeout;
        private static long _lastLoyaltyPoints;
        private static DirectLoyaltyPointOffer _offer;

        private QuestorManagerUI _form;
        private int _requiredItemId;
        private int _requiredUnit;

        public BuyLPI(QuestorManagerUI form1)
        {
            _form = form1;
        }

        public int Item { get; set; }

        public int Unit { get; set; }

        public void ProcessState()
        {
            if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 1)
                return;
            _lastAction = DateTime.UtcNow;

            if (Cache.Instance.ItemHangar == null) return;
            var marketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            switch (_States.CurrentBuyLPIState)
            {
                case BuyLPIState.Idle:
                case BuyLPIState.Done:
                    break;

                case BuyLPIState.Begin:

                    /*
                    if(marketWindow != null)
                        marketWindow.Close();

                    if(lpstore != null)
                        lpstore.Close();*/

                    _States.CurrentBuyLPIState = BuyLPIState.ReadyItemhangar;
                    break;

                case BuyLPIState.ReadyItemhangar:

                    if (Cache.Instance.ItemHangar == null) return;
                    if (Cache.Instance.ShipHangar == null) return;

                    _States.CurrentBuyLPIState = BuyLPIState.OpenLpStore;
                    break;

                case BuyLPIState.OpenLpStore:

                    if (Cache.Instance.LPStore == null) return;
                    _States.CurrentBuyLPIState = BuyLPIState.FindOffer;
                    break;

                case BuyLPIState.FindOffer:

                    if (Cache.Instance.LPStore != null)
                    {
                        _offer = Cache.Instance.LPStore.Offers.FirstOrDefault(o => o.TypeId == Item);

                        // Wait for the amount of LP to change
                        if (_lastLoyaltyPoints == Cache.Instance.LPStore.LoyaltyPoints)
                            break;

                        // Do not expect it to be 0 (probably means its reloading)
                        if (Cache.Instance.LPStore.LoyaltyPoints == 0)
                        {
                            if (_loyaltyPointTimeout < DateTime.UtcNow)
                            {
                                Logging.Log("It seems we have no loyalty points left");
                                _States.CurrentBuyLPIState = BuyLPIState.Done;
                                break;
                            }
                            break;
                        }

                        _lastLoyaltyPoints = Cache.Instance.LPStore.LoyaltyPoints;

                        // Find the offer
                        if (_offer == null)
                        {
                            Logging.Log("Can't find offer with type name/id: " + Item + "!");
                            _States.CurrentBuyLPIState = BuyLPIState.Done;
                            break;
                        }
                        _States.CurrentBuyLPIState = BuyLPIState.CheckPetition;
                    }
                    _States.CurrentBuyLPIState = BuyLPIState.OpenLpStore;
                    break;

                case BuyLPIState.CheckPetition:

                    if (Cache.Instance.LPStore != null)
                    {
                        // Check LP
                        if (_lastLoyaltyPoints < _offer.LoyaltyPointCost)
                        {
                            Logging.Log("Not enough loyalty points left");

                            _States.CurrentBuyLPIState = BuyLPIState.Done;
                            break;
                        }

                        // Check ISK
                        if (Cache.Instance.DirectEve.Me.Wealth < _offer.IskCost)
                        {
                            Logging.Log("Not enough ISK left");

                            _States.CurrentBuyLPIState = BuyLPIState.Done;
                            break;
                        }

                        // Check items
                        foreach (var requiredItem in _offer.RequiredItems)
                        {
                            var ship = Cache.Instance.ShipHangar.Items.FirstOrDefault(i => i.TypeId == requiredItem.TypeId);
                            var item = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.TypeId == requiredItem.TypeId);
                            if (item == null || item.Quantity < requiredItem.Quantity)
                            {
                                if (ship == null || ship.Quantity < requiredItem.Quantity)
                                {
                                    Logging.Log("Missing [" + requiredItem.Quantity + "] x [" +
                                                          requiredItem.TypeName + "]");

                                    //if(!_form.chkBuyItems.Checked)
                                    //{
                                    //    Logging.Log("BuyLPI","Done, do not buy item");
                                    //    States.CurrentBuyLPIState = BuyLPIState.Done;
                                    //    break;
                                    //}

                                    Logging.Log("Are buying the item [" + requiredItem.TypeName + "]");
                                    _requiredUnit = Convert.ToInt32(requiredItem.Quantity);
                                    _requiredItemId = requiredItem.TypeId;
                                    _States.CurrentBuyLPIState = BuyLPIState.OpenMarket;
                                    return;
                                }
                            }
                            _States.CurrentBuyLPIState = BuyLPIState.AcceptOffer;
                        }
                        _States.CurrentBuyLPIState = BuyLPIState.OpenLpStore;
                    }
                    break;

                case BuyLPIState.OpenMarket:

                    if (marketWindow == null)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        Statistics.LogWindowActionToWindowLog("MarketWindow", "MarketWindow Opened");
                        break;
                    }

                    if (!marketWindow.IsReady)
                        break;

                    _States.CurrentBuyLPIState = BuyLPIState.BuyItems;
                    break;

                case BuyLPIState.BuyItems:

                    Logging.Log("Opening Market");

                    if (marketWindow != null && marketWindow.DetailTypeId != _requiredItemId)
                    {
                        marketWindow.LoadTypeId(_requiredItemId);
                        break;
                    }

                    if (marketWindow != null)
                    {
                        var orders =
                            marketWindow.SellOrders.Where(o => o.StationId == Cache.Instance.DirectEve.Session.StationId);

                        var order = orders.OrderBy(o => o.Price).FirstOrDefault();

                        if (order == null)
                        {
                            Logging.Log("No orders");
                            _States.CurrentBuyLPIState = BuyLPIState.Done;
                            break;
                        }

                        order.Buy(_requiredUnit, DirectOrderRange.Station);
                    }

                    Logging.Log("Buy Item");

                    _States.CurrentBuyLPIState = BuyLPIState.CheckPetition;

                    break;

                case BuyLPIState.AcceptOffer:

                    if (Cache.Instance.LPStore != null)
                    {
                        var offer2 = Cache.Instance.LPStore.Offers.FirstOrDefault(o => o.TypeId == Item);

                        if (offer2 != null)
                        {
                            Logging.Log("Accepting [" + offer2.TypeName + "]");
                            offer2.AcceptOfferFromWindow();
                        }
                    }
                    _States.CurrentBuyLPIState = BuyLPIState.Quantity;
                    break;

                case BuyLPIState.Quantity:

                    _loyaltyPointTimeout = DateTime.UtcNow.AddSeconds(1);

                    Unit = Unit - 1;
                    if (Unit <= 0)
                    {
                        Logging.Log("Quantity limit reached");

                        _States.CurrentBuyLPIState = BuyLPIState.Done;
                        break;
                    }

                    _States.CurrentBuyLPIState = BuyLPIState.Begin;

                    break;
            }
        }
    }
}