// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace DirectEve
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PySharp;

    
    public class DirectModule : DirectItem
    {

        private class ModuleTargetInfo
        {
            public long LastTargetId { get; set; }
            public DateTime LastSeenOnline { get; set; }

        }

        private static Dictionary<long, DateTime> LastSeenActivatedModuleDict = new Dictionary<long, DateTime>();

        //private List<DirectItem> _matchingAmmo;
        private PyObject _pyModule;

        internal DirectModule(DirectEve directEve, PyObject pyModule) : base(directEve)
        {
            _pyModule = pyModule;
        }

        public bool IsOnline { get; internal set; }
        public bool IsGoingOnline { get; internal set; }

        public bool IsActivatable { get; internal set; }
        public bool IsActive { get; internal set; }
        public bool IsDeactivating { get; internal set; }

        public bool IsReloadingAmmo { get; internal set; }
        public bool IsChangingAmmo { get; internal set; }

        public bool IsOverloaded { get; internal set; }
        public bool IsPendingOverloading { get; internal set; }
        public bool IsPendingStopOverloading { get; internal set; }
        public bool IsBeingRepaired { get; internal set; }
        public bool AutoReload { get; internal set; }

        public DirectItem Charge { get; internal set; }

        public double Damage { get; internal set; }
        public double Hp { get; internal set; }

        private DateTime lastActivation = DateTime.MinValue; ////Does this even work? We dispose everything after this frame ~ Ferox // nope this doesn't work ~ duketwo
        private long lastTarget = 0;

        public int CurrentCharges
        {
            get { return Charge != null ? Charge.Stacksize : 0; }
        }

        public int MaxCharges
        {
            get
            {
                if (Capacity == 0)
                    return 0;

                if (Charge != null && Charge.Volume > 0)
                    return (int)(Capacity / Charge.Volume);

                /*if (MatchingAmmo.Count > 0)
                    return (int) (Capacity/MatchingAmmo[0].Volume);*/

                return 0;
            }
        }

        public long? TargetId { get; internal set; }

        public double? OptimalRange
        {
            get { return Attributes.TryGet<double>("maxRange"); }
        }

        public double? FallOff
        {
            get { return Attributes.TryGet<double>("falloff"); }
        }

        public double? Duration
        {
            get { return Attributes.TryGet<double>("duration"); }
        }

        public double? CapacitorNeed
        {
            get { return Attributes.TryGet<double>("capacitorNeed"); }
        }

        /*public List<DirectItem> MatchingAmmo
        {
            get
            {
                if (_matchingAmmo == null)
                {
                    _matchingAmmo = new List<DirectItem>();

                    var pyCharges = _pyModule.Call("GetMatchingAmmo", TypeId).ToList();
                    foreach (var pyCharge in pyCharges)
                    {
                        var charge = new DirectItem(DirectEve);
                        charge.PyItem = pyCharge;
                        _matchingAmmo.Add(charge);
                    }
                }

                return _matchingAmmo;
            }
        }*/

        internal static List<DirectModule> GetModules(DirectEve directEve)
        {
            var modules = new List<DirectModule>();

            var pySharp = directEve.PySharp;
            var builtin = pySharp.Import("__builtin__");

            var pyModules = builtin.Attribute("uicore").Attribute("layer").Attribute("shipui").Attribute("slotsContainer").Attribute("modulesByID").ToDictionary<long>();
            foreach (var pyModule in pyModules)
            {
                var module = new DirectModule(directEve, pyModule.Value);
                module.PyItem = pyModule.Value.Attribute("moduleinfo");
                module.ItemId = pyModule.Key;
                module.IsOnline = (bool)pyModule.Value.Attribute("online");
//                module.IsGoingOnline = (bool)pyModule.Value.Attribute("goingOnline");
                module.IsReloadingAmmo = (bool)pyModule.Value.Attribute("reloadingAmmo");
                module.IsChangingAmmo = (bool)pyModule.Value.Attribute("changingAmmo");

                module.Damage = (double)pyModule.Value.Attribute("sr").Attribute("damage");
                module.Hp = (double)pyModule.Value.Attribute("sr").Attribute("hp");
                //module.IsOverloaded = (int)directEve.GetLocalSvc("godma").Call("GetOverloadState", module.ItemId) == 1;
                //module.IsPendingOverloading = (int)directEve.GetLocalSvc("godma").Call("GetOverloadState", module.ItemId) == 2;
                //module.IsPendingStopOverloading = (int)directEve.GetLocalSvc("godma").Call("GetOverloadState", module.ItemId) == 3;
                module.IsBeingRepaired = (bool)pyModule.Value.Attribute("isBeingRepaired");
                module.AutoReload = (bool)pyModule.Value.Attribute("autoreload");

                var effect = pyModule.Value.Attribute("def_effect");
                module.IsActivatable = effect.IsValid;
                module.IsActive = (bool)effect.Attribute("isActive");
                module.IsDeactivating = (bool)effect.Attribute("isDeactivating");
                module.TargetId = (long?)effect.Attribute("targetID");

                var pyCharge = pyModule.Value.Attribute("charge");
                if (pyCharge.IsValid)
                {
                    module.Charge = new DirectItem(directEve);
                    module.Charge.PyItem = pyCharge;
                }

                modules.Add(module);
            }

            return modules;
        }

        internal static void UpdateActiveModules(DirectEve directEve)
        {
            var pySharp = directEve.PySharp;
            var builtin = pySharp.Import("__builtin__");

            var pyModules = builtin.Attribute("uicore").Attribute("layer").Attribute("shipui").Attribute("sr").Attribute("modules").ToDictionary<long>();
            foreach (var pyModule in pyModules)
            {
            	
                var itemId = pyModule.Key;
                var effect = pyModule.Value.Attribute("def_effect");

                bool isActive = (bool)effect.Attribute("isActive");

                if (isActive)
                {
                    LastSeenActivatedModuleDict[itemId] = DateTime.UtcNow;
                }
            }

        }


        public void OnlineModule()
        {
            DirectEve.ThreadedCall(_pyModule.Attribute("ChangeOnline"),1);
            //_pyModule.Call("ChangeOnline", 1);
                //return _pyModule.Call("ChangeOnline", new object[] { 1 });   
        }

        public void OfflineModule()
        {
            DirectEve.ThreadedCall(_pyModule.Attribute("ChangeOnline"),0);
            //_pyModule.Call("ChangeOnline", 0);   
        }

        public bool Click()
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("Click"));
        }

        /// <summary>
        ///     Toggles overload of the DirectModule. If it's not allowed it will fail silently.
        /// </summary>
        /// <returns></returns>
        public bool ToggleOverload()
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("ToggleOverload"));
        }

        /// <summary>
        ///     Repairs a DirectModule in space with nanite paste
        /// </summary>
        /// <returns></returns>
        public bool Repair()
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("RepairModule"));
        }

        /// <summary>
        ///     Cancels the repairing of DirectModule in space
        /// </summary>
        /// <returns></returns>
        public bool CancelRepair()
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("CancelRepair"));
        }

        public void UnloadToCargo()
        {

            if (this.IsReloadingAmmo)
            {
                Console.WriteLine(" if (this.IsReloadingAmmo)");
                return;
            }

            if (this.Charge == null)
            {
                Console.WriteLine("if (this.Charge == null)");
                return;
            }

            if (this.Charge.ItemId <= 0)
            {
                Console.WriteLine("  if (this.Charge.ItemId <= 0)");
                return;
            }
             
            if (this.Charge.TypeId <= 0)
            {
                Console.WriteLine("if (this.Charge.TypeId <= 0)");
                return;
            }

            Console.WriteLine("Calling UnloadToCargo.");
            DirectEve.ThreadedCall(_pyModule.Attribute("UnloadToCargo"), this.Charge.ItemId);
            //_pyModule.Call("UnloadToCargo", this.Charge.ItemId);

        }

        public bool ChangeAmmo(DirectItem charge)
        {
            if (charge.ItemId <= 0)
                return false;

            if (charge.TypeId <= 0)
                return false;

            if (charge.Stacksize <= 0)
                return false;

            //if(this.Charge != null && this.Charge.Quantity > 0)
            //{
            //    UnloadToCargo();
            //}

            var realoadInfo = _pyModule.Call("GetChargeReloadInfo").ToList();
            if (charge.TypeId == (int)realoadInfo[0])
                return ReloadAmmo(charge);
            else
            {
                _pyModule.Attribute("stateManager").Call("ChangeAmmoTypeForModule", ItemId, charge.TypeId);
                return ReloadAmmo(charge);
            }
        }

        public bool ReloadAmmo(DirectItem charge)
        {
            if (charge.ItemId <= 0)
                return false;

            return DirectEve.ThreadedCall(_pyModule.Attribute("ReloadAmmo"), charge.ItemId, 1, charge.IsSingleton);
        }

        public bool Activate()
        {
            if (DateTime.Now.Subtract(lastActivation).TotalSeconds < 5)
                return false;
            lastActivation = DateTime.Now;
            
            try
            {
                if (LastSeenActivatedModuleDict.ContainsKey(this.ItemId))
                {
                    if (LastSeenActivatedModuleDict[this.ItemId].AddMilliseconds(500) > DateTime.UtcNow)
                    {
                        //Console.WriteLine("Can't reactivate module so fast again.");
                        DirectEve._lastModuleBlocked = DateTime.UtcNow;
                        return false;
                    }
                }

                LastSeenActivatedModuleDict[this.ItemId] = DateTime.UtcNow; // this is necessary so it's really impossible to activate it again on the same target within a pulse/frame
                return DirectEve.ThreadedCall(_pyModule.Attribute("ActivateEffect"), _pyModule.Attribute("def_effect"));

            }
            catch (Exception ex)
            {

                Console.WriteLine("DirectEve.Activate Exception [" + ex + "]");
                return false;
            }


        }

        public bool Activate(long targetId)
        {
            int delay;
            if (lastTarget == targetId)
                delay = 10;
            else delay = 5;
            if (DateTime.Now.Subtract(lastActivation).TotalSeconds < delay)
                return false;

            lastActivation = DateTime.Now;

            try
            {
                if (DirectEve.EntitiesById.ContainsKey(targetId) && !DirectEve.IsTargetBeingRemoved(targetId) && DirectEve.IsTargetStillValid(targetId))
                {

                    if (LastSeenActivatedModuleDict.ContainsKey(this.ItemId))
                    {
                        if (LastSeenActivatedModuleDict[this.ItemId].AddMilliseconds(500) > DateTime.UtcNow)
                        {
                            //Console.WriteLine("Can't reactivate module so fast again.");
                            DirectEve._lastModuleBlocked = DateTime.UtcNow;
                            return false;
                        }
                    }

                    LastSeenActivatedModuleDict[this.ItemId] = DateTime.UtcNow; // this is necessary so it's really impossible to activate it again on the same target within a pulse/frame
                    return DirectEve.ThreadedCall(_pyModule.Attribute("ActivateEffect"), _pyModule.Attribute("def_effect"), targetId);
                }

                DirectEve._lastModuleBlocked = DateTime.UtcNow;
                return false;

            }
            catch (Exception ex)
            {

                Console.WriteLine("DirectEve.Activate Exception [" + ex + "]");
                return false;
            }

        }

        public bool Deactivate()
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("DeactivateEffect"), _pyModule.Attribute("def_effect"));
        }

        public bool SetAutoReload(bool on)
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("SetAutoReload"), on);
        }
    }
}