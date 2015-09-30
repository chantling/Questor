// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------


namespace Questor.Modules.Caching
{
    using System;
    //using System.Collections.Generic;
    using DirectEve;
    using global::Questor.Modules.Logging;

    public class EachWeaponsVolleyCache
    {
        public DateTime ThisVolleyCacheCreated = DateTime.UtcNow;
        public string targetName; // Name of target entity
        public long targetTypeID; // TypeID of target entity
        public long targetGroupID; // GroupID of target entity
        public long targetItemID; // ItemID of target entity - tells us which entity specifically (globally unique)
        public double targetVelocity; // Velocity of target entity
        public double targetDistance; // Distance of target entity from me
        public double targetShieldPercentage; // Shield percentage of target entity
        public double targetArmorPercentage; // Armor percentage of target entity
        public double targetHullPercentage; // Hull percentage of target entity
        public double targetShieldHitPoints; // Shield HitPoints of target entity
        public double targetArmorHitPoints; // Shield HitPoints of target entity
        public double targetHullHitPoints; // Shield HitPoints of target entity
        public double targetAngularVelocity;
        public double targetTransversalVelocity;
        public double targetXCoordinate;
        public double targetYCoordinate;
        public double targetZCoordinate;

        public string myShipName; // Name of self
        public long myShipTypeID; // TypeID of my ship 
        public long myShipGroupID; // GroupID of my ship
        public double myShipVelocity; // Velocity of my ship
        public double myShipShieldPercentage; // Shield percentage of my ship
        public double myShipArmorPercentage; // Armor percentage of my ship
        public double myShipHullPercentage; // Hull percentage of my ship
        public double myShipShieldHitPoints; // Shield HitPoints of my ship
        public double myShipArmorHitPoints; // Shield HitPoints of my ship
        public double myShipHullHitPoints; // Shield HitPoints of my ship
        public double myShipCapacitorPercentage;
        public double myShipXCoordinate;
        public double myShipYCoordinate;
        public double myShipZCoordinate;

        public string moduleName;
        public long moduleTypeID;
        public long moduleGroupID;
        public long moduleItemID;
        public double? moduleFalloff;
        public double? moduleOptimal;
        public double moduleCurrentCharges;
        public double? moduleTargetID;
        public string moduleAmmoTypeName;
        public double moduleAmmoTypeID;

        public double thisWasVolleyNumber;

        public string missionName;
        public int pocketNumber;
        public string systemName;

        //
        // ItemID (of module), DateTime of activation, TargetId (what are we shooting at), Target.TypeID, Targets Distance, Targets Velocity, Targets Shield %, Targets Armor %, Targets Hull %, Targets Shield HitPoints, Targets Armor HitPoints, Targets Hull HitPoints
        //
        public EachWeaponsVolleyCache(DirectModule module, EntityCache target)
        {
            //
            // reminder: this class and all the info within it is created (and destroyed!) each frame for each module!
            //
            //_module = module;
            //_target = target;
            //_self = self;
            ThisVolleyCacheCreated = DateTime.UtcNow;
            targetName = target.Name; // Name of target entity
            targetTypeID = target.TypeId; // TypeID of target entity
            targetGroupID = target.GroupId; // GroupID of target entity
            targetItemID = target.Id; // ItemID of target entity - tells us which entity specifically (globally unique)
            targetVelocity = target.Velocity; // Velocity of target entity
            targetDistance = target.Distance; // Distance of target entity from me
            targetShieldPercentage = target.ShieldPct; // Shield percentage of target entity
            targetArmorPercentage = target.ArmorPct; // Armor percentage of target entity
            targetHullPercentage = target.StructurePct; // Hull percentage of target entity
            targetShieldHitPoints = target.ShieldHitPoints; // Shield HitPoints of target entity
            targetArmorHitPoints = target.ArmorHitPoints; // Shield HitPoints of target entity
            targetHullHitPoints = target.StructureHitPoints; // Shield HitPoints of target entity
            targetAngularVelocity = target.AngularVelocity;
            targetTransversalVelocity = target.TransversalVelocity;
            targetXCoordinate = target.XCoordinate;
            targetYCoordinate = target.YCoordinate;
            targetZCoordinate = target.ZCoordinate;

            myShipName = Cache.Instance.ActiveShip.TypeName; // Name of target entity
            myShipTypeID = Cache.Instance.ActiveShip.TypeId; // TypeID of target entity
            myShipGroupID = Cache.Instance.ActiveShip.GroupId; // GroupID of target entity
            myShipVelocity = Cache.Instance.MyShipEntity.Velocity; // Velocity of target entity
            myShipShieldPercentage = Cache.Instance.ActiveShip.ArmorPercentage; // Shield percentage of target entity
            myShipArmorPercentage = Cache.Instance.ActiveShip.ArmorPercentage; // Armor percentage of target entity
            myShipHullPercentage = Cache.Instance.ActiveShip.StructurePercentage; // Hull percentage of target entity
            myShipShieldHitPoints = Cache.Instance.ActiveShip.Shield; // Shield HitPoints of target entity
            myShipArmorHitPoints = Cache.Instance.ActiveShip.Armor; // Shield HitPoints of target entity
            myShipHullHitPoints = Cache.Instance.ActiveShip.Structure; // Shield HitPoints of target entity
            myShipCapacitorPercentage = Cache.Instance.ActiveShip.CapacitorPercentage;
            myShipXCoordinate = Cache.Instance.MyShipEntity.XCoordinate;
            myShipYCoordinate = Cache.Instance.MyShipEntity.YCoordinate;
            myShipZCoordinate = Cache.Instance.MyShipEntity.ZCoordinate;

            moduleName = module.TypeName;
            moduleItemID = module.ItemId;
            moduleTypeID = module.TypeId;
            moduleGroupID = module.GroupId;
            moduleFalloff = module.FallOff;
            moduleOptimal = module.OptimalRange;
            moduleTargetID = module.TargetId;
            moduleCurrentCharges = module.CurrentCharges;
            if (moduleGroupID != 53 && module.Charge != null)
            {
                if (Logging.DebugEachWeaponsVolleyCache) Logging.Log("DebugEachWeaponsVolleyCache", "[" + thisWasVolleyNumber + "] ModuleItemID [" + moduleItemID + "] ModuleTypeID [" + moduleTypeID + "] ModuleGroupID [" + moduleGroupID + "] ModuleCurrentCharges [" + moduleCurrentCharges + "]", Logging.Debug);
                moduleAmmoTypeName = module.Charge.TypeName;
                moduleAmmoTypeID = module.Charge.TypeId;    
            }
            
            thisWasVolleyNumber = Cache.Instance.VolleyCount;
            Cache.Instance.VolleyCount++;

            ThisVolleyCacheCreated = DateTime.UtcNow;
        }       
    }
}