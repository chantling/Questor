using System;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public class Sell
    {
        private DateTime _lastAction;
        public int Item { get; set; }

        public int Unit { get; set; }

        public void ProcessState()
        {
            if (!Cache.Instance.InStation)
                return;

            if (Cache.Instance.InSpace)
                return;

            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20))
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
                return;

            //DirectMarketWindow marketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
            var sellWindow = Cache.Instance.Windows.OfType<DirectMarketActionWindow>().FirstOrDefault(w => w.IsSellAction);

            switch (_States.CurrentSellState)
            {
                case SellState.Idle:
                case SellState.Done:
                    break;

                case SellState.Begin:
                    _States.CurrentSellState = SellState.StartQuickSell;
                    break;

                case SellState.StartQuickSell:

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 1)
                        break;
                    _lastAction = DateTime.UtcNow;

                    if (Cache.Instance.ItemHangar == null) break;

                    var directItem = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => (i.TypeId == Item));
                    if (directItem == null)
                    {
                        Logging.Logging.Log("Item " + Item + " no longer exists in the hanger");
                        break;
                    }

                    // Update Quantity
                    if (Unit == 00)
                        Unit = directItem.Quantity;

                    Logging.Logging.Log("Starting QuickSell for " + Item);
                    if (!directItem.QuickSell())
                    {
                        _lastAction = DateTime.UtcNow.AddSeconds(-5);

                        Logging.Logging.Log("QuickSell failed for " + Item + ", retrying in 5 seconds");
                        break;
                    }

                    _States.CurrentSellState = SellState.WaitForSellWindow;
                    break;

                case SellState.WaitForSellWindow:

                    //if (sellWindow == null || !sellWindow.IsReady || sellWindow.Item.ItemId != Item)
                    //    break;

                    // Mark as new execution
                    _lastAction = DateTime.UtcNow;

                    Logging.Logging.Log("Inspecting sell order for " + Item);
                    _States.CurrentSellState = SellState.InspectOrder;
                    break;

                case SellState.InspectOrder:
                    // Let the order window stay open for 2 seconds
                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 2)
                        break;
                    if (sellWindow != null)
                    {
                        if ((!sellWindow.OrderId.HasValue || !sellWindow.Price.HasValue || !sellWindow.RemainingVolume.HasValue))
                        {
                            Logging.Logging.Log("No order available for " + Item);

                            sellWindow.Cancel();
                            _States.CurrentSellState = SellState.WaitingToFinishQuickSell;
                            break;
                        }

                        var price = sellWindow.Price.Value;

                        Logging.Logging.Log("Selling " + Unit + " of " + Item + " [Sell price: " + (price * Unit).ToString("#,##0.00") + "]");
                        sellWindow.Accept();
                        _States.CurrentSellState = SellState.WaitingToFinishQuickSell;
                    }
                    _lastAction = DateTime.UtcNow;
                    break;

                case SellState.WaitingToFinishQuickSell:
                    if (sellWindow == null || !sellWindow.IsReady || sellWindow.Item.ItemId != Item)
                    {
                        var modal = Cache.Instance.Windows.FirstOrDefault(w => w.IsModal);
                        if (modal != null)
                            modal.Close();

                        _States.CurrentSellState = SellState.Done;
                        break;
                    }
                    break;
            }
        }
    }
}