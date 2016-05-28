//// ------------------------------------------------------------------------------
//// <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//// Copyright (c) TheHackerWithin.COM. All Rights Reserved.
////
//// Please look in the accompanying license.htm file for the license that
//// applies to this source code. (a copy can also be found at:
//// http://www.thehackerwithin.com/license.htm)
//// </copyright>
//// -------------------------------------------------------------------------------
//
//namespace BuyLPI
//{
//    using System;
//    using System.Linq;
//    using System.Threading;
//    using DirectEve;
//    using System.Globalization;
//    using global::Questor.Modules.Logging;
//    using global::Questor.Modules.Lookup;
//    using global::Questor.Modules.Caching;
//    using global::Questor.Modules.BackgroundTasks;
//
//    internal class BuyLPI
//    {
//        public static DateTime DateTimeForLogs;
//        private const int WaitMillis = 3500;
//        private static long _lastLoyaltyPoints;
//        private static DateTime _nextAction;
//        private static DateTime _loyaltyPointTimeout;
//        private static string _type;
//        private static int? _quantity;
//        private static int? _totalQuantityOfOrders;
//        private static DateTime _done = DateTime.UtcNow.AddDays(10);
//        private static DateTime _lastPulse;
//        //private static Cleanup _cleanup;
//
//        private static void Main(string[] args)
//        {
//            DateTimeForLogs = DateTime.Now;
//            //Logging.Log("BuyLPI", "BuyLPI: Test", Logging.White);
//            //InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTimeForLogs, "BuyLPI: Test2"));
//            
//            if (args.Length == 0)
//            {
//                //InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTimeForLogs, "BuyLPI: 0 arguments"));
//                Logging.Log("BuyLPI", "Syntax:", Logging.White);
//                Logging.Log("BuyLPI", "DotNet BuyLPI BuyLPI <TypeName or TypeId> [Quantity]", Logging.White);
//                Logging.Log("BuyLPI", "(Quantity is optional)", Logging.White);
//                Logging.Log("BuyLPI", "", Logging.White);
//                Logging.Log("BuyLPI", "Example:", Logging.White);
//                Logging.Log("BuyLPI", "DotNet BuyLPI BuyLPI \"Caldari Navy Mjolnir Torpedo\" 10", Logging.White);
//                Logging.Log("BuyLPI", "*OR*", Logging.White);
//                Logging.Log("BuyLPI", "DotNet BuyLPI BuyLPI 27339 10", Logging.White);
//                return;
//            }
//
//            if (args.Length >= 1)
//            {
//                _type = args[0];
//            }
//
//            if (args.Length >= 2)
//            {
//                int dummy;
//                if (!int.TryParse(args[1], out dummy))
//                {
//                    Logging.Log("BuyLPI", "Quantity must be an integer, 0 - " + int.MaxValue, Logging.White);
//                    return;
//                }
//
//                if (dummy < 0)
//                {
//                    Logging.Log("BuyLPI", "Quantity must be a positive number", Logging.White);
//                    return;
//                }
//
//                _quantity = dummy;
//                _totalQuantityOfOrders = dummy;
//            }
//
//            //InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTimeForLogs, "Starting BuyLPI... - innerspace Echo"));
//            Logging.Log("BuyLPI", "Starting BuyLPI...", Logging.White);
//            //_cleanup = new Cleanup();
//
//
//            #region Load DirectEVE
//            //
//            // Load DirectEVE
//            //
//
//            try
//            {
//                if (Cache.Instance.DirectEve == null)
//                {
//                    //
//                    // DE now has cloaking enabled using EasyHook, If EasyHook DLLs are missing DE should complain. We check for and complain about missing EasyHook stuff before we get this far.
//                    // 
//                    //
//                    //Logging.Log("Startup", "temporarily disabling the loading of DE for debugging purposes, halting", Logging.Debug);
//                    //while (Cache.Instance.DirectEve == null)
//                    //{
//                    //    System.Threading.Thread.Sleep(50); //this pauses forever...
//                    //}
//                    //if (_standaloneInstance)
//                    //{
//                    //    Logging.Log("Startup", "Starting Instance of DirectEVE using StandaloneFramework", Logging.Debug);
//                    //    Cache.Instance.DirectEve = new DirectEve(new StandaloneFramework());
//                    //}
//                    //else
//                    //{
//                        Logging.Log("Startup", "Starting Instance of DirectEVE using Innerspace", Logging.Debug);
//                        Cache.Instance.DirectEve = new DirectEve();
//                    //}
//                }
//            }
//            catch (Exception ex)
//            {
//                Logging.Log("Startup", "Error on Loading DirectEve, maybe server is down", Logging.Orange);
//                Logging.Log("Startup", string.Format("DirectEVE: Exception {0}...", ex), Logging.White);
//                Cache.Instance.CloseQuestorCMDLogoff = false;
//                Cache.Instance.CloseQuestorCMDExitGame = true;
//                Cache.Instance.CloseQuestorEndProcess = true;
//                Settings.Instance.AutoStart = true;
//                Cleanup.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
//                Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
//                Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
//                return;
//            }
//            #endregion Load DirectEVE
//            
//            Cache.Instance.DirectEve.OnFrame += OnFrame;
//
//            // Sleep until we're done
//            while (_done.AddSeconds(5) > DateTime.UtcNow)
//            {
//                Thread.Sleep(50);
//            }
//
//            Cache.Instance.DirectEve.Dispose();
//            Logging.Log("BuyLPI", "BuyLPI finished.", Logging.White);
//            //InnerSpace.Echo(string.Format("{0:HH:mm:ss} {1}", DateTimeForLogs, "BuyLPI: Finished 2"));
//        }
//
//        private static void OnFrame(object sender, EventArgs eventArgs)
//        {
//            // New frame, invalidate old cache
//            Cache.Instance.InvalidateCache();
//
//            Time.Instance.LastFrame = DateTime.UtcNow;
//
//            // Only pulse state changes every 1.5s
//            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 300)
//                return;
//
//            _lastPulse = DateTime.UtcNow;
//
//            // Session is not ready yet, do not continue
//            if (!Cache.Instance.DirectEve.Session.IsReady)
//                return;
//
//            if (Cache.Instance.DirectEve.Session.IsReady)
//                Time.Instance.LastSessionIsReady = DateTime.UtcNow;
//
//            // We are not in space or station, don't do shit yet!
//            if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
//            {
//                Time.Instance.NextInSpaceorInStation = DateTime.UtcNow.AddSeconds(12);
//                Time.Instance.LastSessionChange = DateTime.UtcNow;
//                return;
//            }
//
//            if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)
//                return;
//
//            if (!Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
//            {
//                // Update settings (settings only load if character name changed)
//                if (!Settings.Instance.DefaultSettingsLoaded)
//                {
//                    Settings.Instance.LoadSettings();
//                }
//            }
//
//            // Start _cleanup.ProcessState
//            // Description: Closes Windows, and eventually other things considered 'cleanup' useful to more than just Questor(Missions) but also Anomalies, Mining, etc
//            //
//            Cleanup.ProcessState();
//
//            // Done
//            // Cleanup State: ProcessState
//
//            if (DateTime.UtcNow > _done)
//                return;
//
//            // Wait for the next action
//            if (_nextAction >= DateTime.UtcNow)
//            {
//                return;
//            }
//
//            if (Cache.Instance.ItemHangar == null) return;
//
//            if (Cache.Instance.LPStore == null)
//            {
//                _nextAction = DateTime.UtcNow.AddMilliseconds(WaitMillis);
//                 return;
//            }
//
//            // Wait for the amount of LP to change
//            if (_lastLoyaltyPoints == Cache.Instance.LPStore.LoyaltyPoints)
//                return;
//
//            // Do not expect it to be 0 (probably means its reloading)
//            if (Cache.Instance.LPStore.LoyaltyPoints == 0)
//            {
//                if (_loyaltyPointTimeout < DateTime.UtcNow)
//                {
//                    Logging.Log("BuyLPI", "It seems we have no loyalty points left", Logging.White);
//                    _done = DateTime.UtcNow;
//                    return;
//                }
//                return;
//            }
//
//            _lastLoyaltyPoints = Cache.Instance.LPStore.LoyaltyPoints;
//
//            // Find the offer
//            DirectLoyaltyPointOffer offer = Cache.Instance.LPStore.Offers.FirstOrDefault(o => o.TypeId.ToString(CultureInfo.InvariantCulture) == _type || String.Compare(o.TypeName, _type, StringComparison.OrdinalIgnoreCase) == 0);
//            if (offer == null)
//            {
//                Logging.Log("BuyLPI", " Can't find offer with type name/id: [" + _type + "]", Logging.White);
//                _done = DateTime.UtcNow;
//                return;
//            }
//
//            // Check LP
//            if (_lastLoyaltyPoints < offer.LoyaltyPointCost)
//            {
//                Logging.Log("BuyLPI", "Not enough loyalty points left: you have [" + _lastLoyaltyPoints + "] and you need [" + offer.LoyaltyPointCost + "]", Logging.White);
//                _done = DateTime.UtcNow;
//                return;
//            }
//
//            // Check ISK
//            if (Cache.Instance.DirectEve.Me.Wealth < offer.IskCost)
//            {
//                Logging.Log("BuyLPI", "Not enough ISK left: you have [" + Math.Round(Cache.Instance.DirectEve.Me.Wealth, 0) + "] and you need [" + offer.IskCost + "]", Logging.White);
//                _done = DateTime.UtcNow;
//                return;
//            }
//
//            // Check items
//            foreach (DirectLoyaltyPointOfferRequiredItem requiredItem in offer.RequiredItems)
//            {
//                DirectItem item = Cache.Instance.ItemHangar.Items.FirstOrDefault(i => i.TypeId == requiredItem.TypeId);
//                if (item == null || item.Quantity < requiredItem.Quantity)
//                {
//                    Logging.Log("BuyLPI", "Missing [" + requiredItem.Quantity + "] x [" +
//                                                    requiredItem.TypeName + "]", Logging.White);
//                    _done = DateTime.UtcNow;
//                    return;
//                }
//            }
//
//            // All passed, accept offer
//            if (_quantity != null)
//                if (_totalQuantityOfOrders != null)
//                    Logging.Log("BuyLPI", "Accepting " + offer.TypeName + " [ " + _quantity.Value + " ] of [ " + _totalQuantityOfOrders.Value + " ] orders and will cost another [" + Math.Round(((offer.IskCost * _quantity.Value) / (double)1000000), 2) + "mil isk]", Logging.White);
//            offer.AcceptOfferFromWindow();
//
//            // Set next action + loyalty point timeout
//            _nextAction = DateTime.UtcNow.AddMilliseconds(WaitMillis);
//            _loyaltyPointTimeout = DateTime.UtcNow.AddSeconds(25);
//
//            if (_quantity.HasValue)
//            {
//                _quantity = _quantity.Value - 1;
//                if (_quantity.Value <= 0)
//                {
//                    Logging.Log("BuyLPI", "Quantity limit reached", Logging.White);
//                    _done = DateTime.UtcNow;
//                    return;
//                }
//            }
//        }
//    }
//}

