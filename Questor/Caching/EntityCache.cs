﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DirectEve;
using Questor.Modules.Activities;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;

namespace Questor.Modules.Caching
{
    public class EntityCache
    {
        private const int DictionaryCountThreshhold = 250;
        private readonly DirectEntity _directEntity;
        //public static int EntityCacheInstances = 0;
        private readonly DateTime ThisEntityCacheCreated = DateTime.UtcNow;

        private double? _angularVelocity;

        private double? _armorHitPoints;

        private double? _armorPct;


        private double? _armorResistanceEM;

        private double? _armorResistanceExplosive;

        private double? _armorResistanceKinetic;

        private double? _armorResistanceThermal;

        private List<string> _attacks;

        private int? _categoryId;

        private double? _distance;

        public double? _effectiveHitpointsViaEM;

        public double? _effectiveHitpointsViaExplosive;

        public double? _effectiveHitpointsViaKinetic;

        public double? _effectiveHitpointsViaThermal;

        private long? _followId;

        private string _givenName;

        private int? _groupID;

        private long? _id;

        private bool? _isActiveTarget;

        private bool? _isAttacking;

        private bool? _IsBadIdea;

        private bool? _isBattleCruiser;


        private bool? _isBattleship;

        private bool? _isContainer;

        private bool? _isCorrectSizeForMyWeapons;

        private bool? _isCruiser;


        private bool? _isCurrentTarget;

        private bool? _IsDroneKillPriority;

        private bool? _isEntityIShouldKeepShooting;

        private bool? _isEntityIShouldKeepShootingWithDrones;

        private bool? _isEntityIShouldLeaveAlone;

        private bool? _isEwarTarget;

        private bool? _isFrigate;

        private bool? _isHigherPriorityPresent;

        private bool? _isHighValueTarget;

        private bool? _isIgnored;

        private bool? _isInOptimalRange;

        private bool? _isLargeCollidable;

        private bool? _isLastTargetDronesWereShooting;

        private bool? _isLastTargetPrimaryWeaponsWereShooting;

        private bool? _isLootTarget;

        private bool? _isLowValueTarget;

        private bool? _isMiscJunk;

        private bool? _isNpc;

        private bool? _isNPCBattleCruiser;

        private bool? _isNPCBattleship;

        private bool? _isNpcByGroupID;

        private bool? _isNPCCruiser;

        private bool? _isNPCFrigate;

        private bool? _isOnGridWithMe;

        private bool? _isPlayer;

        private bool? _isPreferredDroneTarget;

        private bool? _isPreferredPrimaryWeaponTarget;

        private bool? _isPrimaryWeaponKillPriority;

        private bool? _IsReadyToShoot;

        private bool? _IsReadyToTarget;

        private bool? _isSentry;

        private bool? _isTarget;

        private bool? _isTargetedBy;

        private bool? _isTargeting;

        private bool? _IsTooCloseTooFastTooSmallToHit;

        private bool? _isValid;

        private int? _mode;
        private string _name;

        private double? _nearest5kDistance;

        private PrimaryWeaponPriority? _primaryWeaponPriorityLevel;

        private double? _shieldHitPoints;

        private double? _shieldPct;

        private double? _shieldResistanceEM;

        private double? _shieldResistanceExplosive;

        private double? _shieldResistanceKinetic;

        private double? _shieldResistanceThermal;

        private double? _structureHitPoints;

        private double? _structurePct;

        private int? _targetValue;

        private double? _transversalVelocity;

        private int? _TypeId;

        private string _typeName;

        private double? _velocity;

        private double? _xCoordinate;

        private double? _yCoordinate;

        private double? _zCoordinate;

        public EntityCache(DirectEntity entity)
        {
            //
            // reminder: this class and all the info within it is created (and destroyed!) each frame for each entity!
            //
            _directEntity = entity;
            //Interlocked.Increment(ref EntityCacheInstances);
            ThisEntityCacheCreated = DateTime.UtcNow;
        }

        //~EntityCache()
        //{
        //    Interlocked.Decrement(ref EntityCacheInstances);
        //}


        public double GetBounty
        {
            get { return _directEntity != null ? _directEntity.GetBounty() : 0; }
        }

        public int GroupId
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_groupID == null)
                        {
                            if (Cache.Instance.EntityGroupID.Any() && Cache.Instance.EntityGroupID.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityGroupID.Count() + "] Entities in Cache.Instance.EntityGroupID");
                            }

                            if (Cache.Instance.EntityGroupID.Any())
                            {
                                int value;
                                if (Cache.Instance.EntityGroupID.TryGetValue(Id, out value))
                                {
                                    _groupID = value;
                                    return (int) _groupID;
                                }
                            }

                            _groupID = _directEntity.GroupId;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityGroupID as [" + _groupID + "]");

                            Cache.Instance.EntityGroupID.Add(Id, (int) _groupID);
                            return (int) _groupID;
                        }

                        return (int) _groupID;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public int CategoryId
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_categoryId == null)
                        {
                            _categoryId = _directEntity.CategoryId;
                        }

                        return (int) _categoryId;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public long Id
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_id == null)
                        {
                            _id = _directEntity.Id;
                        }

                        return (long) _id;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public string MaskedId
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var numofCharacters = _directEntity.Id.ToString(CultureInfo.InvariantCulture).Length;
                        if (numofCharacters >= 5)
                        {
                            var maskedID = _directEntity.Id.ToString(CultureInfo.InvariantCulture).Substring(numofCharacters - 4);
                            maskedID = "[MaskedID]" + maskedID;
                            return maskedID;
                        }

                        return "!0!";
                    }

                    return "!0!";
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return "!0!";
                }
            }
        }

        public int TypeId
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_TypeId == null)
                        {
                            if (Cache.Instance.EntityTypeID.Any() && Cache.Instance.EntityTypeID.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityTypeID.Count() + "] Entities in Cache.Instance.EntityTypeID");
                            }

                            if (Cache.Instance.EntityTypeID.Any())
                            {
                                int value;
                                if (Cache.Instance.EntityTypeID.TryGetValue(Id, out value))
                                {
                                    _TypeId = value;
                                    return (int) _TypeId;
                                }
                            }

                            _TypeId = _directEntity.TypeId;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityTypeId as [" + _TypeId + "]");
                            Cache.Instance.EntityTypeID.Add(Id, (int) _TypeId);
                            return (int) _TypeId;
                        }

                        return (int) _TypeId;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public long FollowId
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_followId == null)
                        {
                            _followId = _directEntity.FollowId;
                        }

                        return (long) _followId;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public List<string> Attacks
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_attacks == null)
                        {
                            _attacks = _directEntity.Attacks;
                        }

                        return _attacks;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public int Mode
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_mode == null)
                        {
                            _mode = _directEntity.Mode;
                            //
                            // 1 = Approaching or entityCombat
                            // 2 =
                            // 3 = Warping
                            // 4 = Orbiting
                            // 5 =
                            // 6 = entityPursuit
                            // 7 =
                            // 8 =
                            // 9 =
                            // 10 = entityEngage
                            //
                        }

                        return (int) _mode;
                    }

                    _mode = null;
                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public string Name
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        if (_name == null)
                        {
                            if (Cache.Instance.EntityNames.Any() && Cache.Instance.EntityNames.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityNames.Count() + "] Entities in Cache.Instance.EntityNames");
                            }

                            if (Cache.Instance.EntityNames.Any())
                            {
                                string value;
                                if (Cache.Instance.EntityNames.TryGetValue(Id, out value))
                                {
                                    _name = value;
                                    return _name;
                                }
                            }

                            _name = _directEntity.Name;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + MaskedId + "] to EntityName as [" + _name + "]");
                            Cache.Instance.EntityNames.Add(Id, _name);
                            return _name ?? string.Empty;
                        }

                        return _name;
                    }

                    return string.Empty;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return string.Empty;
                }
            }
        }

        public string TypeName
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        _typeName = _directEntity.TypeName;
                        return _typeName ?? "";
                    }

                    return "";
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return "";
                }
            }
        }

        public string GivenName
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }
                        if (String.IsNullOrEmpty(_givenName))
                        {
                            _givenName = _directEntity.GivenName;
                        }

                        return _givenName ?? "";
                    }

                    return "";
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return "";
                }
            }
        }

        public double Distance
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }
                        if (_distance == null)
                        {
                            _distance = _directEntity.Distance;
                        }

                        return (double) _distance;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double Nearest5kDistance
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_nearest5kDistance == null)
                        {
                            if (Distance > 0 && Distance < 900000000)
                            {
                                //_nearest5kDistance = Math.Round((Distance / 1000) * 2, MidpointRounding.AwayFromZero) / 2;
                                _nearest5kDistance = Math.Ceiling(Math.Round((Distance/1000))/5.0)*5;
                            }
                        }

                        return _nearest5kDistance ?? Distance;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ShieldPct
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_shieldPct == null)
                        {
                            _shieldPct = _directEntity.ShieldPct;
                            return (double) _shieldPct;
                        }

                        return (double) _shieldPct;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ShieldHitPoints
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_shieldHitPoints == null)
                        {
                            _shieldHitPoints = _directEntity.Shield;
                            return _shieldHitPoints ?? 0;
                        }

                        return (double) _shieldHitPoints;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ShieldResistanceEM
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_shieldResistanceEM == null)
                        {
                            _shieldResistanceEM = _directEntity.ShieldResistanceEM;
                            return _shieldResistanceEM ?? 0;
                        }

                        return (double) _shieldResistanceEM;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ShieldResistanceExplosive
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_shieldResistanceExplosive == null)
                        {
                            if (_directEntity.ShieldResistanceExplosion != null)
                            {
                                _shieldResistanceExplosive = _directEntity.ShieldResistanceExplosion;
                                return (double) _shieldResistanceExplosive;
                            }

                            _shieldResistanceExplosive = 0;
                            return (double) _shieldResistanceExplosive;
                        }

                        return (double) _shieldResistanceExplosive;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ShieldResistanceKinetic
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_shieldResistanceKinetic == null)
                        {
                            if (_directEntity.ShieldResistanceKinetic != null)
                            {
                                _shieldResistanceKinetic = _directEntity.ShieldResistanceKinetic;
                                return (double) _shieldResistanceKinetic;
                            }

                            _shieldResistanceKinetic = 0;
                            return (double) _shieldResistanceKinetic;
                        }

                        return (double) _shieldResistanceKinetic;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ShieldResistanceThermal
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_shieldResistanceThermal == null)
                        {
                            if (_directEntity.ShieldResistanceThermal != null)
                            {
                                _shieldResistanceThermal = _directEntity.ShieldResistanceThermal;
                                return (double) _shieldResistanceThermal;
                            }

                            _shieldResistanceThermal = 0;
                            return (double) _shieldResistanceThermal;
                        }

                        return (double) _shieldResistanceThermal;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ArmorPct
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_armorPct == null)
                        {
                            _armorPct = _directEntity.ArmorPct;
                            return (double) _armorPct;
                        }

                        return (double) _armorPct;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ArmorHitPoints
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_armorHitPoints == null)
                        {
                            _armorHitPoints = _directEntity.Armor;
                            return _armorHitPoints ?? 0;
                        }

                        return (double) _armorHitPoints;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ArmorResistanceEM
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_armorResistanceEM == null)
                        {
                            _armorResistanceEM = _directEntity.ArmorResistanceEM;
                            return _armorResistanceEM ?? 0;
                        }

                        return (double) _armorResistanceEM;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ArmorResistanceExplosive
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_armorResistanceExplosive == null)
                        {
                            _armorResistanceExplosive = _directEntity.ArmorResistanceExplosion;
                            return _armorResistanceExplosive ?? 0;
                        }

                        return (double) _armorResistanceExplosive;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ArmorResistanceKinetic
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_armorResistanceKinetic == null)
                        {
                            _armorResistanceKinetic = _directEntity.ArmorResistanceKinetic;
                            return _armorResistanceKinetic ?? 0;
                        }

                        return (double) _armorResistanceKinetic;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ArmorResistanceThermal
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_armorResistanceThermal == null)
                        {
                            if (_directEntity.ArmorResistanceThermal != null)
                            {
                                _armorResistanceThermal = _directEntity.ArmorResistanceThermal;
                                return (double) _armorResistanceThermal;
                            }

                            _armorResistanceThermal = 0;
                            return (double) _armorResistanceThermal;
                        }

                        return (double) _armorResistanceThermal;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double StructurePct
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_structurePct == null)
                        {
                            _structurePct = _directEntity.StructurePct;
                            return (double) _structurePct;
                        }

                        return (double) _structurePct;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double StructureHitPoints
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_structureHitPoints == null)
                        {
                            _structureHitPoints = _directEntity.Structure;
                            return _structureHitPoints ?? 0;
                        }

                        return (double) _structureHitPoints;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double EffectiveHitpointsViaEM
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_effectiveHitpointsViaEM == null)
                        {
                            //
                            // this does not take into account hull, but most things have so very little hull (LCOs might be a problem!)
                            //
                            _effectiveHitpointsViaEM = ((ShieldHitPoints*ShieldResistanceEM) + (ArmorHitPoints*ArmorResistanceEM));
                            return (double) _effectiveHitpointsViaEM;
                        }

                        return (double) _effectiveHitpointsViaEM;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double EffectiveHitpointsViaExplosive
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_effectiveHitpointsViaExplosive == null)
                        {
                            //
                            // this does not take into account hull, but most things have so very little hull (LCOs might be a problem!)
                            //
                            _effectiveHitpointsViaExplosive = ((ShieldHitPoints*ShieldResistanceExplosive) + (ArmorHitPoints*ArmorResistanceExplosive));
                            return (double) _effectiveHitpointsViaExplosive;
                        }

                        return (double) _effectiveHitpointsViaExplosive;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double EffectiveHitpointsViaKinetic
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_effectiveHitpointsViaKinetic == null)
                        {
                            //
                            // this does not take into account hull, but most things have so very little hull (LCOs might be a problem!)
                            //
                            _effectiveHitpointsViaKinetic = ((ShieldHitPoints*ShieldResistanceKinetic) + (ArmorHitPoints*ArmorResistanceKinetic));
                            return (double) _effectiveHitpointsViaKinetic;
                        }

                        return (double) _effectiveHitpointsViaKinetic;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double EffectiveHitpointsViaThermal
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_effectiveHitpointsViaThermal == null)
                        {
                            //
                            // this does not take into account hull, but most things have so very little hull (LCOs might be a problem!)
                            //
                            _effectiveHitpointsViaThermal = ((ShieldHitPoints*ShieldResistanceThermal) + (ArmorHitPoints*ArmorResistanceThermal));
                            return (double) _effectiveHitpointsViaThermal;
                        }

                        return (double) _effectiveHitpointsViaThermal;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public bool IsNpc
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isNpc == null)
                        {
                            _isNpc = _directEntity.IsNpc;
                            return (bool) _isNpc;
                        }

                        return (bool) _isNpc;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public double Velocity
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_velocity == null)
                        {
                            _velocity = _directEntity.Velocity;
                            return (double) _velocity;
                        }

                        return (double) _velocity;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double TransversalVelocity
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_transversalVelocity == null)
                        {
                            _transversalVelocity = _directEntity.TransversalVelocity;
                            return (double) _transversalVelocity;
                        }

                        return (double) _transversalVelocity;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double AngularVelocity
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_angularVelocity == null)
                        {
                            _angularVelocity = _directEntity.AngularVelocity;
                            return (double) _angularVelocity;
                        }

                        return (double) _angularVelocity;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double XCoordinate
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_xCoordinate == null)
                        {
                            _xCoordinate = _directEntity.X;
                            return (double) _xCoordinate;
                        }

                        return (double) _xCoordinate;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double YCoordinate
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_yCoordinate == null)
                        {
                            _yCoordinate = _directEntity.Y;
                            return (double) _yCoordinate;
                        }

                        return (double) _yCoordinate;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public double ZCoordinate
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_zCoordinate == null)
                        {
                            _zCoordinate = _directEntity.Z;
                            return (double) _zCoordinate;
                        }

                        return (double) _zCoordinate;
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public bool IsTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isTarget == null)
                        {
                            _isTarget = _directEntity.IsTarget;
                            return (bool) _isTarget;
                        }

                        return (bool) _isTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsCorrectSizeForMyWeapons
        {
            get
            {
                try
                {
                    if (_isCorrectSizeForMyWeapons == null)
                    {
                        if (Cache.Instance.MyShipEntity.IsFrigate)
                        {
                            if (IsFrigate)
                            {
                                _isCorrectSizeForMyWeapons = true;
                                return (bool) _isCorrectSizeForMyWeapons;
                            }
                        }

                        if (Cache.Instance.MyShipEntity.IsCruiser)
                        {
                            if (IsCruiser)
                            {
                                _isCorrectSizeForMyWeapons = true;
                                return (bool) _isCorrectSizeForMyWeapons;
                            }
                        }

                        if (Cache.Instance.MyShipEntity.IsBattlecruiser || Cache.Instance.MyShipEntity.IsBattleship)
                        {
                            if (IsBattleship || IsBattlecruiser)
                            {
                                _isCorrectSizeForMyWeapons = true;
                                return (bool) _isCorrectSizeForMyWeapons;
                            }
                        }

                        return false;
                    }

                    return (bool) _isCorrectSizeForMyWeapons;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                }

                return false;
            }
        }

        public bool isPreferredPrimaryWeaponTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isPreferredPrimaryWeaponTarget == null)
                        {
                            if (Combat.Combat.PreferredPrimaryWeaponTarget != null && Combat.Combat.PreferredPrimaryWeaponTarget.Id == Id)
                            {
                                _isPreferredPrimaryWeaponTarget = true;
                                return (bool) _isPreferredPrimaryWeaponTarget;
                            }

                            _isPreferredPrimaryWeaponTarget = false;
                            return (bool) _isPreferredPrimaryWeaponTarget;
                        }

                        return (bool) _isPreferredPrimaryWeaponTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPrimaryWeaponKillPriority
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isPrimaryWeaponKillPriority == null)
                        {
                            if (Combat.Combat.PrimaryWeaponPriorityTargets.Any(e => e.Entity.Id == Id))
                            {
                                _isPrimaryWeaponKillPriority = true;
                                return (bool) _isPrimaryWeaponKillPriority;
                            }

                            _isPrimaryWeaponKillPriority = false;
                            return (bool) _isPrimaryWeaponKillPriority;
                        }

                        return (bool) _isPrimaryWeaponKillPriority;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool isPreferredDroneTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isPreferredDroneTarget == null)
                        {
                            if (Drones.PreferredDroneTarget != null && Drones.PreferredDroneTarget.Id == _directEntity.Id)
                            {
                                _isPreferredDroneTarget = true;
                                return (bool) _isPreferredDroneTarget;
                            }

                            _isPreferredDroneTarget = false;
                            return (bool) _isPreferredDroneTarget;
                        }

                        return (bool) _isPreferredDroneTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsDroneKillPriority
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_IsDroneKillPriority == null)
                        {
                            if (Drones.DronePriorityTargets.Any(e => e.Entity.Id == _directEntity.Id))
                            {
                                _IsDroneKillPriority = true;
                                return (bool) _IsDroneKillPriority;
                            }

                            _IsDroneKillPriority = false;
                            return (bool) _IsDroneKillPriority;
                        }

                        return (bool) _IsDroneKillPriority;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTooCloseTooFastTooSmallToHit
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_IsTooCloseTooFastTooSmallToHit == null)
                        {
                            if (IsNPCFrigate || IsFrigate)
                            {
                                if (Combat.Combat.DoWeCurrentlyHaveTurretsMounted() && Drones.UseDrones)
                                {
                                    if (Distance < Combat.Combat.DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons
                                        && Velocity > Combat.Combat.SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons)
                                    {
                                        _IsTooCloseTooFastTooSmallToHit = true;
                                        return (bool) _IsTooCloseTooFastTooSmallToHit;
                                    }

                                    _IsTooCloseTooFastTooSmallToHit = false;
                                    return (bool) _IsTooCloseTooFastTooSmallToHit;
                                }

                                _IsTooCloseTooFastTooSmallToHit = false;
                                return (bool) _IsTooCloseTooFastTooSmallToHit;
                            }
                        }

                        return _IsTooCloseTooFastTooSmallToHit ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsReadyToShoot
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_IsReadyToShoot == null)
                        {
                            if (!HasExploded && IsTarget && !IsIgnored && Distance < Combat.Combat.MaxRange)
                            {
                                _IsReadyToShoot = true;
                                return (bool) _IsReadyToShoot;
                            }
                        }

                        return _IsReadyToShoot ?? false;
                    }

                    if (Logging.Logging.DebugIsReadyToShoot) Logging.Logging.Log("_directEntity is null or invalid");
                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsReadyToTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_IsReadyToTarget == null)
                        {
                            if (!HasExploded && !IsTarget && !IsTargeting && Distance < Combat.Combat.MaxTargetRange)
                            {
                                _IsReadyToTarget = true;
                                return (bool) _IsReadyToTarget;
                            }

                            _IsReadyToTarget = false;
                            return (bool) _IsReadyToTarget;
                        }

                        return (bool) _IsReadyToTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsHigherPriorityPresent
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isHigherPriorityPresent == null)
                        {
                            if (Combat.Combat.PrimaryWeaponPriorityTargets.Any() || Drones.DronePriorityTargets.Any())
                            {
                                if (Combat.Combat.PrimaryWeaponPriorityTargets.Any())
                                {
                                    if (Combat.Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id))
                                    {
                                        var _currentPrimaryWeaponPriority =
                                            Combat.Combat.PrimaryWeaponPriorityEntities.Where(t => t.Id == _directEntity.Id)
                                                .Select(pt => pt.PrimaryWeaponPriorityLevel)
                                                .FirstOrDefault();

                                        if (
                                            !Combat.Combat.PrimaryWeaponPriorityEntities.All(
                                                pt => pt.PrimaryWeaponPriorityLevel < _currentPrimaryWeaponPriority && pt.Distance < Combat.Combat.MaxRange))
                                        {
                                            _isHigherPriorityPresent = true;
                                            return (bool) _isHigherPriorityPresent;
                                        }

                                        _isHigherPriorityPresent = false;
                                        return (bool) _isHigherPriorityPresent;
                                    }

                                    if (Combat.Combat.PrimaryWeaponPriorityEntities.Any(e => e.Distance < Combat.Combat.MaxRange))
                                    {
                                        _isHigherPriorityPresent = true;
                                        return (bool) _isHigherPriorityPresent;
                                    }

                                    _isHigherPriorityPresent = false;
                                    return (bool) _isHigherPriorityPresent;
                                }

                                if (Drones.DronePriorityTargets.Any())
                                {
                                    if (Drones.DronePriorityTargets.Any(pt => pt.EntityID == _directEntity.Id))
                                    {
                                        var _currentEntityDronePriority =
                                            Drones.DronePriorityEntities.Where(t => t.Id == _directEntity.Id)
                                                .Select(pt => pt.DronePriorityLevel)
                                                .FirstOrDefault();

                                        if (
                                            !Drones.DronePriorityEntities.All(
                                                pt => pt.DronePriorityLevel < _currentEntityDronePriority && pt.Distance < Drones.MaxDroneRange))
                                        {
                                            return true;
                                        }

                                        return false;
                                    }

                                    if (Drones.DronePriorityEntities.Any(e => e.Distance < Drones.MaxDroneRange))
                                    {
                                        _isHigherPriorityPresent = true;
                                        return (bool) _isHigherPriorityPresent;
                                    }

                                    _isHigherPriorityPresent = false;
                                    return (bool) _isHigherPriorityPresent;
                                }

                                _isHigherPriorityPresent = false;
                                return (bool) _isHigherPriorityPresent;
                            }
                        }

                        return _isHigherPriorityPresent ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsActiveTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isActiveTarget == null)
                        {
                            if (_directEntity.IsActiveTarget)
                            {
                                _isActiveTarget = true;
                                return (bool) _isActiveTarget;
                            }

                            _isActiveTarget = false;
                            return (bool) _isActiveTarget;
                        }

                        return (bool) _isActiveTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsLootTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Settings.Instance.FleetSupportSlave)
                        {
                            return false;
                        }

                        if (_isLootTarget == null)
                        {
                            if (Cache.Instance.ListofContainersToLoot.Contains(Id))
                            {
                                return true;
                            }

                            _isLootTarget = false;
                            return (bool) _isLootTarget;
                        }

                        return (bool) _isLootTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsCurrentTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isCurrentTarget == null)
                        {
                            if (Combat.Combat.CurrentWeaponTarget() != null)
                            {
                                _isCurrentTarget = true;
                                return (bool) _isCurrentTarget;
                            }

                            _isCurrentTarget = false;
                            return (bool) _isCurrentTarget;
                        }

                        return (bool) _isCurrentTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsLastTargetPrimaryWeaponsWereShooting
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isLastTargetPrimaryWeaponsWereShooting == null)
                        {
                            if (Combat.Combat.LastTargetPrimaryWeaponsWereShooting != null && Id == Combat.Combat.LastTargetPrimaryWeaponsWereShooting.Id)
                            {
                                _isLastTargetPrimaryWeaponsWereShooting = true;
                                return (bool) _isLastTargetPrimaryWeaponsWereShooting;
                            }

                            _isLastTargetPrimaryWeaponsWereShooting = false;
                            return (bool) _isLastTargetPrimaryWeaponsWereShooting;
                        }

                        return (bool) _isLastTargetPrimaryWeaponsWereShooting;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsLastTargetDronesWereShooting
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isLastTargetDronesWereShooting == null)
                        {
                            if (Drones.LastTargetIDDronesEngaged != null && Id == Drones.LastTargetIDDronesEngaged)
                            {
                                _isLastTargetDronesWereShooting = true;
                                return (bool) _isLastTargetDronesWereShooting;
                            }

                            _isLastTargetDronesWereShooting = false;
                            return (bool) _isLastTargetDronesWereShooting;
                        }

                        return (bool) _isLastTargetDronesWereShooting;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInOptimalRange
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isInOptimalRange == null)
                        {
                            if (NavigateOnGrid.SpeedTank && NavigateOnGrid.OrbitDistance != 0)
                            {
                                if (NavigateOnGrid.OptimalRange == 0)
                                {
                                    MissionSettings.MissionOptimalRange = NavigateOnGrid.OrbitDistance + 5000;
                                }
                            }

                            if (MissionSettings.MissionOptimalRange != 0 || NavigateOnGrid.OptimalRange != 0)
                            {
                                double optimal = 0;

                                if (MissionSettings.MissionOptimalRange != null && MissionSettings.MissionOptimalRange != 0)
                                {
                                    optimal = (double) MissionSettings.MissionOptimalRange;
                                }
                                else if (NavigateOnGrid.OptimalRange != 0)
                                    //do we really need this condition? we cant even get in here if one of them is not != 0, that is the idea, if its 0 we sure as hell do not want to use it as the optimal
                                {
                                    optimal = NavigateOnGrid.OptimalRange;
                                }

                                if (optimal > Cache.Instance.ActiveShip.MaxTargetRange)
                                {
                                    optimal = Cache.Instance.ActiveShip.MaxTargetRange - 500;
                                }

                                if (Combat.Combat.DoWeCurrentlyProjectilesMounted())
                                {
                                    if (Distance > Combat.Combat.InsideThisRangeIsHardToTrack)
                                    {
                                        if (Distance < (optimal*10) && Distance < Cache.Instance.ActiveShip.MaxTargetRange)
                                        {
                                            _isInOptimalRange = true;
                                            return (bool) _isInOptimalRange;
                                        }
                                    }
                                }

                                else if (Combat.Combat.DoWeCurrentlyHaveTurretsMounted()) //Lasers, Projectile, and Hybrids
                                {
                                    if (Distance > Combat.Combat.InsideThisRangeIsHardToTrack)
                                    {
                                        if (Distance < (optimal*1.5) && Distance < Cache.Instance.ActiveShip.MaxTargetRange)
                                        {
                                            _isInOptimalRange = true;
                                            return (bool) _isInOptimalRange;
                                        }
                                    }
                                }
                                else //missile boats - use max range
                                {
                                    optimal = Combat.Combat.MaxRange;
                                    if (Distance < optimal)
                                    {
                                        _isInOptimalRange = true;
                                        return (bool) _isInOptimalRange;
                                    }

                                    _isInOptimalRange = false;
                                    return (bool) _isInOptimalRange;
                                }

                                _isInOptimalRange = false;
                                return (bool) _isInOptimalRange;
                            }

                            // If you have no optimal you have to assume the entity is within Optimal... (like missiles)
                            _isInOptimalRange = true;
                            return (bool) _isInOptimalRange;
                        }

                        return (bool) _isInOptimalRange;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInOptimalRangeOrNothingElseAvail
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        //if it is in optimal, return true, we want to shoot things that are in optimal!
                        if (IsInOptimalRange)
                        {
                            return true;
                        }

                        //Any targets which are not the current target and is not a wreck or container
                        if (!Cache.Instance.Targets.Any(i => i.Id != Id && !i.IsContainer))
                        {
                            return true;
                        }

                        //something else must be available to shoot, and this entity is not in optimal, return false;
                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsInDroneRange
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Drones.MaxDroneRange > 0) //&& Cache.Instance.UseDrones)
                        {
                            if (Distance < Drones.MaxDroneRange)
                            {
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsDronePriorityTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Drones.DronePriorityTargets.All(i => i.EntityID != Id))
                        {
                            return false;
                        }

                        return true;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPriorityWarpScrambler
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Combat.Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id))
                        {
                            if (PrimaryWeaponPriorityLevel == PrimaryWeaponPriority.WarpScrambler)
                            {
                                return true;
                            }

                            //return false; //check for drone priority targets too!
                        }

                        if (Drones.DronePriorityTargets.Any(pt => pt.EntityID == Id))
                        {
                            if (DronePriorityLevel == DronePriority.WarpScrambler)
                            {
                                return true;
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPrimaryWeaponPriorityTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Combat.Combat.PrimaryWeaponPriorityTargets.Any(i => i.EntityID == Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public PrimaryWeaponPriority PrimaryWeaponPriorityLevel
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_primaryWeaponPriorityLevel == null)
                        {
                            if (Combat.Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id))
                            {
                                _primaryWeaponPriorityLevel = Combat.Combat.PrimaryWeaponPriorityTargets.Where(t => t.Entity.IsTarget && t.EntityID == Id)
                                    .Select(pt => pt.PrimaryWeaponPriority)
                                    .FirstOrDefault();
                                return (PrimaryWeaponPriority) _primaryWeaponPriorityLevel;
                            }

                            return PrimaryWeaponPriority.NotUsed;
                        }

                        return (PrimaryWeaponPriority) _primaryWeaponPriorityLevel;
                    }

                    return PrimaryWeaponPriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return PrimaryWeaponPriority.NotUsed;
                }
            }
        }

        public DronePriority DronePriorityLevel
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Drones.DronePriorityTargets.Any(pt => pt.EntityID == _directEntity.Id))
                        {
                            var currentTargetPriority = Drones.DronePriorityTargets.Where(t => t.Entity.IsTarget
                                                                                               && t.EntityID == Id)
                                .Select(pt => pt.DronePriority)
                                .FirstOrDefault();

                            return currentTargetPriority;
                        }

                        return DronePriority.NotUsed;
                    }

                    return DronePriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return DronePriority.NotUsed;
                }
            }
        }

        public bool IsTargeting
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isTargeting == null)
                        {
                            _isTargeting = _directEntity.IsTargeting;
                            return (bool) _isTargeting;
                        }

                        return (bool) _isTargeting;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTargetedBy
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isTargetedBy == null)
                        {
                            _isTargetedBy = _directEntity.IsTargetedBy;
                            return (bool) _isTargetedBy;
                        }

                        return (bool) _isTargetedBy;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAttacking
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isAttacking == null)
                        {
                            _isAttacking = _directEntity.IsAttacking;
                            return (bool) _isAttacking;
                        }

                        return (bool) _isAttacking;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsWreckEmpty
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (GroupId == (int) Group.Wreck)
                        {
                            return _directEntity.IsEmpty;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool HasReleased
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        return _directEntity.HasReleased;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool HasExploded
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        return _directEntity.HasExploded;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsEwarTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isEwarTarget == null)
                        {
                            var result = false;
                            result |= IsWarpScramblingMe;
                            result |= IsWebbingMe;
                            result |= IsNeutralizingMe;
                            result |= IsJammingMe;
                            result |= IsSensorDampeningMe;
                            result |= IsTargetPaintingMe;
                            result |= IsTrackingDisruptingMe;
                            _isEwarTarget = result;
                            return (bool) _isEwarTarget;
                        }

                        return (bool) _isEwarTarget;
                    }

                    return _isEwarTarget ?? false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public DronePriority IsActiveDroneEwarType
        {
            get
            {
                try
                {
                    if (IsWarpScramblingMe)
                    {
                        return DronePriority.WarpScrambler;
                    }

                    if (IsWebbingMe)
                    {
                        return DronePriority.Webbing;
                    }

                    if (IsNeutralizingMe)
                    {
                        return DronePriority.PriorityKillTarget;
                    }

                    if (IsJammingMe)
                    {
                        return DronePriority.PriorityKillTarget;
                    }

                    if (IsSensorDampeningMe)
                    {
                        return DronePriority.PriorityKillTarget;
                    }

                    if (IsTargetPaintingMe)
                    {
                        return DronePriority.PriorityKillTarget;
                    }

                    if (IsTrackingDisruptingMe)
                    {
                        return DronePriority.PriorityKillTarget;
                    }

                    return DronePriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return DronePriority.NotUsed;
                }
            }
        }

        public PrimaryWeaponPriority IsActivePrimaryWeaponEwarType
        {
            get
            {
                try
                {
                    if (IsWarpScramblingMe)
                    {
                        return PrimaryWeaponPriority.WarpScrambler;
                    }

                    if (IsWebbingMe)
                    {
                        return PrimaryWeaponPriority.Webbing;
                    }

                    if (IsNeutralizingMe)
                    {
                        return PrimaryWeaponPriority.Neutralizing;
                    }

                    if (IsJammingMe)
                    {
                        return PrimaryWeaponPriority.Jamming;
                    }

                    if (IsSensorDampeningMe)
                    {
                        return PrimaryWeaponPriority.Dampening;
                    }

                    if (IsTargetPaintingMe)
                    {
                        return PrimaryWeaponPriority.TargetPainting;
                    }

                    if (IsTrackingDisruptingMe)
                    {
                        return PrimaryWeaponPriority.TrackingDisrupting;
                    }

                    return PrimaryWeaponPriority.NotUsed;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return PrimaryWeaponPriority.NotUsed;
                }
            }
        }

        public bool IsWarpScramblingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (Cache.Instance.ListOfWarpScramblingEntities.Contains(Id))
                        {
                            return true;
                        }

                        if (_directEntity.Attacks.Contains("effects.WarpScramble"))
                        {
                            if (!Cache.Instance.ListOfWarpScramblingEntities.Contains(Id))
                            {
                                Cache.Instance.ListOfWarpScramblingEntities.Add(Id);
                            }

                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsWebbingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_directEntity.Attacks.Contains("effects.ModifyTargetSpeed"))
                        {
                            if (!Cache.Instance.ListofWebbingEntities.Contains(Id)) Cache.Instance.ListofWebbingEntities.Add(Id);
                            return true;
                        }

                        if (Cache.Instance.ListofWebbingEntities.Contains(Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsNeutralizingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewEnergyNeut"))
                        {
                            if (!Cache.Instance.ListNeutralizingEntities.Contains(Id)) Cache.Instance.ListNeutralizingEntities.Add(Id);
                            return true;
                        }

                        if (Cache.Instance.ListNeutralizingEntities.Contains(Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsJammingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("electronic"))
                        {
                            if (!Cache.Instance.ListOfJammingEntities.Contains(Id)) Cache.Instance.ListOfJammingEntities.Add(Id);
                            return true;
                        }

                        if (Cache.Instance.ListOfJammingEntities.Contains(Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsSensorDampeningMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewRemoteSensorDamp"))
                        {
                            if (!Cache.Instance.ListOfDampenuingEntities.Contains(Id)) Cache.Instance.ListOfDampenuingEntities.Add(Id);
                            return true;
                        }

                        if (Cache.Instance.ListOfDampenuingEntities.Contains(Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTargetPaintingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewTargetPaint"))
                        {
                            if (!Cache.Instance.ListOfTargetPaintingEntities.Contains(Id)) Cache.Instance.ListOfTargetPaintingEntities.Add(Id);
                            return true;
                        }

                        if (Cache.Instance.ListOfTargetPaintingEntities.Contains(Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTrackingDisruptingMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_directEntity.ElectronicWarfare.Contains("ewTrackingDisrupt"))
                        {
                            if (!Cache.Instance.ListOfTrackingDisruptingEntities.Contains(Id)) Cache.Instance.ListOfTrackingDisruptingEntities.Add(Id);
                            return true;
                        }

                        if (Cache.Instance.ListOfTrackingDisruptingEntities.Contains(Id))
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public int Health
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        return (int) ((ShieldPct + ArmorPct + StructurePct)*100);
                    }

                    return 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return 0;
                }
            }
        }

        public bool IsEntityIShouldKeepShooting
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isEntityIShouldKeepShooting == null)
                        {
                            //
                            // Is our current target already in armor? keep shooting the same target if so...
                            //
                            if (IsReadyToShoot
                                && IsInOptimalRange && !IsLargeCollidable
                                && (((!IsFrigate && !IsNPCFrigate) || !IsTooCloseTooFastTooSmallToHit))
                                && ArmorPct*100 < Combat.Combat.DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage)
                            {
                                if (Logging.Logging.DebugGetBestTarget)
                                    Logging.Logging.Log("[" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + " GroupID [" + GroupId +
                                        "]] has less than 60% armor, keep killing this target");
                                _isEntityIShouldKeepShooting = true;
                                return (bool) _isEntityIShouldKeepShooting;
                            }

                            _isEntityIShouldKeepShooting = false;
                            return (bool) _isEntityIShouldKeepShooting;
                        }

                        return (bool) _isEntityIShouldKeepShooting;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception: [" + ex + "]");
                }

                return false;
            }
        }

        public bool IsEntityIShouldKeepShootingWithDrones
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isEntityIShouldKeepShootingWithDrones == null)
                        {
                            //
                            // Is our current target already in armor? keep shooting the same target if so...
                            //
                            if (IsReadyToShoot
                                && IsInDroneRange
                                && !IsLargeCollidable
                                && ((IsFrigate || IsNPCFrigate) || Drones.DronesKillHighValueTargets)
                                && ShieldPct*100 < 80)
                            {
                                if (Logging.Logging.DebugGetBestTarget)
                                    Logging.Logging.Log("[" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + " GroupID [" + GroupId +
                                        "]] has less than 60% armor, keep killing this target");
                                _isEntityIShouldKeepShootingWithDrones = true;
                                return (bool) _isEntityIShouldKeepShootingWithDrones;
                            }

                            _isEntityIShouldKeepShootingWithDrones = false;
                            return (bool) _isEntityIShouldKeepShootingWithDrones;
                        }

                        return (bool) _isEntityIShouldKeepShootingWithDrones;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception: [" + ex + "]");
                }

                return false;
            }
        }

        public bool IsSentry
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isSentry == null)
                        {
                            if (Cache.Instance.EntityIsSentry.Any() && Cache.Instance.EntityIsSentry.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsSentry.Count() + "] Entities in Cache.Instance.EntityIsSentry");
                            }

                            if (Cache.Instance.EntityIsSentry.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsSentry.TryGetValue(Id, out value))
                                {
                                    _isSentry = value;
                                    return (bool) _isSentry;
                                }
                            }

                            var result = false;
                            //if (GroupId == (int)Group.SentryGun) return true;
                            result |= (GroupId == (int) Group.ProtectiveSentryGun);
                            result |= (GroupId == (int) Group.MobileSentryGun);
                            result |= (GroupId == (int) Group.DestructibleSentryGun);
                            result |= (GroupId == (int) Group.MobileMissileSentry);
                            result |= (GroupId == (int) Group.MobileProjectileSentry);
                            result |= (GroupId == (int) Group.MobileLaserSentry);
                            result |= (GroupId == (int) Group.MobileHybridSentry);
                            result |= (GroupId == (int) Group.DeadspaceOverseersSentry);
                            result |= (GroupId == (int) Group.StasisWebificationBattery);
                            result |= (GroupId == (int) Group.EnergyNeutralizingBattery);
                            _isSentry = result;
                            Cache.Instance.EntityIsSentry.Add(Id, result);
                            return (bool) _isSentry;
                        }

                        return (bool) _isSentry;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsIgnored
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isIgnored == null)
                        {
                            //IsIgnoredRefreshes++;
                            //if (Cache.Instance.Entities.All(t => t.Id != _directEntity.Id))
                            //{
                            //    IsIgnoredRefreshes = IsIgnoredRefreshes + 1000;
                            //    _isIgnored = true;
                            //    return _isIgnored ?? true;
                            //}
                            if (CombatMissionCtrl.IgnoreTargets != null && CombatMissionCtrl.IgnoreTargets.Any())
                            {
                                _isIgnored = CombatMissionCtrl.IgnoreTargets.Contains(Name.Trim());
                                if ((bool) _isIgnored)
                                {
                                    if (Combat.Combat.PreferredPrimaryWeaponTarget != null && Combat.Combat.PreferredPrimaryWeaponTarget.Id != Id)
                                    {
                                        Combat.Combat.PreferredPrimaryWeaponTarget = null;
                                    }

                                    if (Cache.Instance.EntityIsLowValueTarget.ContainsKey(Id))
                                    {
                                        Cache.Instance.EntityIsLowValueTarget.Remove(Id);
                                    }

                                    if (Cache.Instance.EntityIsHighValueTarget.ContainsKey(Id))
                                    {
                                        Cache.Instance.EntityIsHighValueTarget.Remove(Id);
                                    }

                                    if (Logging.Logging.DebugEntityCache)
                                        Logging.Logging.Log("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored + "]");
                                    return (bool) _isIgnored;
                                }

                                _isIgnored = false;
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored + "]");
                                return (bool) _isIgnored;
                            }

                            _isIgnored = false;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored + "]");
                            return (bool) _isIgnored;
                        }

                        if (Logging.Logging.DebugEntityCache)
                            Logging.Logging.Log("[" + Name + "][" + Math.Round(Distance / 1000, 0) + "k][" + MaskedId + "] isIgnored [" + _isIgnored + "]");
                        return (bool) _isIgnored;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool HaveLootRights
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (GroupId == (int) Group.SpawnContainer)
                        {
                            return true;
                        }

                        var result = false;
                        if (Cache.Instance.ActiveShip.Entity != null)
                        {
                            result |= _directEntity.CorpId == Cache.Instance.ActiveShip.Entity.CorpId;
                            result |= _directEntity.OwnerId == Cache.Instance.ActiveShip.Entity.CharId;
                            //
                            // It would be nice if this were eventually extended to detect and include 'abandoned' wrecks (blue ones).
                            // I do not yet know what attributed actually change when that happens. We should collect some data.
                            //
                            return result;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public int? TargetValue
        {
            get
            {
                try
                {
                    var result = -1;

                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_targetValue == null)
                        {
                            ShipTargetValue value = null;

                            try
                            {
                                value = Cache.Instance.ShipTargetValues.FirstOrDefault(v => v.GroupId == GroupId);
                            }
                            catch (Exception exception)
                            {
                                if (Logging.Logging.DebugShipTargetValues)
                                    Logging.Logging.Log("exception [" + exception + "]");
                            }

                            if (value == null)
                            {
                                if (IsNPCBattleship)
                                {
                                    _targetValue = 4;
                                }
                                else if (IsNPCBattlecruiser)
                                {
                                    _targetValue = 3;
                                }
                                else if (IsNPCCruiser)
                                {
                                    _targetValue = 2;
                                }
                                else if (IsNPCFrigate)
                                {
                                    _targetValue = 0;
                                }

                                return _targetValue ?? -1;
                            }

                            _targetValue = value.TargetValue;
                            return _targetValue;
                        }

                        return _targetValue;
                    }

                    return result;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return -1;
                }
            }
        }

        public bool IsHighValueTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isHighValueTarget == null)
                        {
                            if (Cache.Instance.EntityIsHighValueTarget.Any() && Cache.Instance.EntityIsHighValueTarget.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsHighValueTarget.Count() + "] Entities in Cache.Instance.EntityIsHighValueTarget");
                            }

                            if (Cache.Instance.EntityIsHighValueTarget.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsHighValueTarget.TryGetValue(Id, out value))
                                {
                                    _isHighValueTarget = value;
                                    return (bool) _isHighValueTarget;
                                }
                            }

                            if (TargetValue != null)
                            {
                                if (!IsIgnored && !IsContainer && !IsBadIdea && !IsCustomsOffice && !IsFactionWarfareNPC && !IsPlayer)
                                {
                                    if (TargetValue >= Combat.Combat.MinimumTargetValueToConsiderTargetAHighValueTarget)
                                    {
                                        if (IsSentry && !Combat.Combat.KillSentries && !IsEwarTarget)
                                        {
                                            _isHighValueTarget = false;
                                            if (Logging.Logging.DebugEntityCache)
                                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsHighValueTarget as [" + _isHighValueTarget + "]");
                                            Cache.Instance.EntityIsHighValueTarget.Add(Id, (bool) _isHighValueTarget);
                                            return (bool) _isHighValueTarget;
                                        }

                                        _isHighValueTarget = true;
                                        if (Logging.Logging.DebugEntityCache)
                                            Logging.Logging.Log("Adding [" + Name + "] to EntityIsHighValueTarget as [" + _isHighValueTarget + "]");
                                        Cache.Instance.EntityIsHighValueTarget.Add(Id, (bool) _isHighValueTarget);
                                        return (bool) _isHighValueTarget;
                                    }

                                    //if (IsLargeCollidable)
                                    //{
                                    //    return true;
                                    //}
                                }

                                _isHighValueTarget = false;
                                //do not cache things that may be ignored temporarily...
                                return (bool) _isHighValueTarget;
                            }

                            _isHighValueTarget = false;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsHighValueTarget as [" + _isHighValueTarget + "]");
                            Cache.Instance.EntityIsHighValueTarget.Add(Id, (bool) _isHighValueTarget);
                            return (bool) _isHighValueTarget;
                        }

                        return (bool) _isHighValueTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsLowValueTarget
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isLowValueTarget == null)
                        {
                            if (Cache.Instance.EntityIsLowValueTarget.Any() && Cache.Instance.EntityIsLowValueTarget.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsLowValueTarget.Count() + "] Entities in Cache.Instance.EntityIsLowValueTarget");
                            }

                            if (Cache.Instance.EntityIsLowValueTarget.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsLowValueTarget.TryGetValue(Id, out value))
                                {
                                    _isLowValueTarget = value;
                                    return (bool) _isLowValueTarget;
                                }
                            }

                            if (!IsIgnored && !IsContainer && !IsBadIdea && !IsCustomsOffice && !IsFactionWarfareNPC && !IsPlayer)
                            {
                                if (TargetValue != null && TargetValue <= Combat.Combat.MaximumTargetValueToConsiderTargetALowValueTarget)
                                {
                                    if (IsSentry && !Combat.Combat.KillSentries && !IsEwarTarget)
                                    {
                                        _isLowValueTarget = false;
                                        if (Logging.Logging.DebugEntityCache)
                                            Logging.Logging.Log("Adding [" + Name + "] to EntityIsLowValueTarget as [" + _isLowValueTarget + "]");
                                        Cache.Instance.EntityIsLowValueTarget.Add(Id, (bool) _isLowValueTarget);
                                        return (bool) _isLowValueTarget;
                                    }

                                    if (TargetValue < 0 && Velocity == 0)
                                    {
                                        _isLowValueTarget = false;
                                        if (Logging.Logging.DebugEntityCache)
                                            Logging.Logging.Log("Adding [" + Name + "] to EntityIsLowValueTarget as [" + _isLowValueTarget + "]");
                                        Cache.Instance.EntityIsLowValueTarget.Add(Id, (bool) _isLowValueTarget);
                                        return (bool) _isLowValueTarget;
                                    }

                                    _isLowValueTarget = true;
                                    if (Logging.Logging.DebugEntityCache)
                                        Logging.Logging.Log("Adding [" + Name + "] to EntityIsLowValueTarget as [" + _isLowValueTarget + "]");
                                    Cache.Instance.EntityIsLowValueTarget.Add(Id, (bool) _isLowValueTarget);
                                    return (bool) _isLowValueTarget;
                                }

                                _isLowValueTarget = false;
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("Adding [" + Name + "] to EntityIsLowValueTarget as [" + _isLowValueTarget + "]");
                                Cache.Instance.EntityIsLowValueTarget.Add(Id, (bool) _isLowValueTarget);
                                return (bool) _isLowValueTarget;
                            }

                            _isLowValueTarget = false;
                            //do not cache things that may be ignored temporarily
                            return (bool) _isLowValueTarget;
                        }

                        return (bool) _isLowValueTarget;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public DirectContainerWindow CargoWindow
        {
            get
            {
                try
                {
                    if (!Cache.Instance.Windows.Any())
                    {
                        return null;
                    }

                    return Cache.Instance.Windows.OfType<DirectContainerWindow>().FirstOrDefault(w => w.ItemId == Id);
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public bool IsValid
        {
            get
            {
                try
                {
                    if (_directEntity != null)
                    {
                        if (_isValid == null)
                        {
                            _isValid = _directEntity.IsValid;
                            return (bool) _isValid;
                        }

                        return (bool) _isValid;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsContainer
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isContainer == null)
                        {
                            var result = false;
                            result |= (GroupId == (int) Group.Wreck);
                            result |= (GroupId == (int) Group.CargoContainer);
                            result |= (GroupId == (int) Group.SpawnContainer);
                            result |= (GroupId == (int) Group.MissionContainer);
                            result |= (GroupId == (int) Group.DeadSpaceOverseersBelongings);
                            _isContainer = result;
                            return (bool) _isContainer;
                        }

                        return (bool) _isContainer;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPlayer
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isPlayer == null)
                        {
                            _isPlayer = _directEntity.IsPc;
                            return (bool) _isPlayer;
                        }

                        return (bool) _isPlayer;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTargetingMeAndNotYetTargeted
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= (((IsNpc || IsNpcByGroupID) || IsAttacking)
                                   && CategoryId == (int) CategoryID.Entity
                                   && Distance < Combat.Combat.MaxTargetRange
                                   && !IsLargeCollidable
                                   && (!IsTargeting && !IsTarget && IsTargetedBy)
                                   && !IsContainer
                                   && !IsIgnored
                                   && (!IsBadIdea || IsAttacking)
                                   && !IsEntityIShouldLeaveAlone
                                   && !IsFactionWarfareNPC
                                   && !IsStation);

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsNotYetTargetingMeAndNotYetTargeted
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= (((IsNpc || IsNpcByGroupID))
                                   && (!IsTargeting && !IsTarget)
                                   && !IsContainer
                                   && CategoryId == (int) CategoryID.Entity
                                   && Distance < Combat.Combat.MaxTargetRange
                                   && !IsIgnored
                                   && !IsBadIdea
                                   && !IsTargetedBy
                                   && !IsEntityIShouldLeaveAlone
                                   && !IsFactionWarfareNPC
                                   && !IsLargeCollidable
                                   && !IsStation);

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsTargetWeCanShootButHaveNotYetTargeted
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= (CategoryId == (int) CategoryID.Entity
                                   && !IsTarget
                                   && !IsTargeting
                                   && Distance < Combat.Combat.MaxTargetRange
                                   && !IsIgnored
                                   && !IsStation);

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Frigate includes all elite-variants - this does NOT need to be limited to players, as we check for players
        ///     specifically everywhere this is used
        /// </summary>
        public bool IsFrigate
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isFrigate == null)
                        {
                            if (Cache.Instance.EntityIsFrigate.Any() && Cache.Instance.EntityIsFrigate.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsFrigate.Count() + "] Entities in Cache.Instance.EntityIsFrigate");
                            }

                            if (Cache.Instance.EntityIsFrigate.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsFrigate.TryGetValue(Id, out value))
                                {
                                    _isFrigate = value;
                                    return (bool) _isFrigate;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Frigate;
                            result |= GroupId == (int) Group.AssaultShip;
                            result |= GroupId == (int) Group.StealthBomber;
                            result |= GroupId == (int) Group.ElectronicAttackShip;
                            result |= GroupId == (int) Group.PrototypeExplorationShip;

                            // Technically not frigs, but for our purposes they are
                            result |= GroupId == (int) Group.Destroyer;
                            result |= GroupId == (int) Group.Interdictor;
                            result |= GroupId == (int) Group.Interceptor;

                            _isFrigate = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsFrigate as [" + _isFrigate + "]");
                            Cache.Instance.EntityIsFrigate.Add(Id, (bool) _isFrigate);
                            return (bool) _isFrigate;
                        }

                        return (bool) _isFrigate;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Frigate includes all elite-variants - this does NOT need to be limited to players, as we check for players
        ///     specifically everywhere this is used
        /// </summary>
        public bool IsNPCFrigate
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isNPCFrigate == null)
                        {
                            if (Cache.Instance.EntityIsNPCFrigate.Any() && Cache.Instance.EntityIsNPCFrigate.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsNPCFrigate.Count() + "] Entities in Cache.Instance.EntityIsNPCFrigate");
                            }

                            if (Cache.Instance.EntityIsNPCFrigate.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsNPCFrigate.TryGetValue(Id, out value))
                                {
                                    _isNPCFrigate = value;
                                    return (bool) _isNPCFrigate;
                                }
                            }

                            var result = false;
                            if (IsPlayer)
                            {
                                //
                                // if it is a player it is by definition not an NPC
                                //
                                return false;
                            }
                            result |= GroupId == (int) Group.Frigate;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Guristas_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_Destroyer;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Destroyer;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Destroyer;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Destroyer;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Destroyer;
                            result |= GroupId == (int) Group.Mission_Khanid_Destroyer;
                            result |= GroupId == (int) Group.Mission_CONCORD_Destroyer;
                            result |= GroupId == (int) Group.Mission_Mordu_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_Destroyer;
                            result |= GroupId == (int) Group.Mission_Thukker_Destroyer;
                            result |= GroupId == (int) Group.Mission_Generic_Destroyers;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_Destroyer;
                            result |= GroupId == (int) Group.asteroid_angel_cartel_frigate;
                            result |= GroupId == (int) Group.asteroid_blood_raiders_frigate;
                            result |= GroupId == (int) Group.asteroid_guristas_frigate;
                            result |= GroupId == (int) Group.asteroid_sanshas_nation_frigate;
                            result |= GroupId == (int) Group.asteroid_serpentis_frigate;
                            result |= GroupId == (int) Group.deadspace_angel_cartel_frigate;
                            result |= GroupId == (int) Group.deadspace_blood_raiders_frigate;
                            result |= GroupId == (int) Group.deadspace_guristas_frigate;
                            result |= GroupId == (int) Group.deadspace_sanshas_nation_frigate;
                            result |= GroupId == (int) Group.deadspace_serpentis_frigate;
                            result |= GroupId == (int) Group.mission_amarr_empire_frigate;
                            result |= GroupId == (int) Group.mission_caldari_state_frigate;
                            result |= GroupId == (int) Group.mission_gallente_federation_frigate;
                            result |= GroupId == (int) Group.mission_minmatar_republic_frigate;
                            result |= GroupId == (int) Group.mission_khanid_frigate;
                            result |= GroupId == (int) Group.mission_concord_frigate;
                            result |= GroupId == (int) Group.mission_mordu_frigate;
                            result |= GroupId == (int) Group.asteroid_rouge_drone_frigate;
                            result |= GroupId == (int) Group.deadspace_rogue_drone_frigate;
                            result |= GroupId == (int) Group.asteroid_angel_cartel_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_blood_raiders_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_guristas_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_sanshas_nation_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_serpentis_commander_frigate;
                            result |= GroupId == (int) Group.mission_generic_frigates;
                            result |= GroupId == (int) Group.mission_thukker_frigate;
                            result |= GroupId == (int) Group.asteroid_rouge_drone_commander_frigate;
                            result |= GroupId == (int) Group.TutorialDrone;
                            //result |= Name.Contains("Spider Drone"); //we *really* need to find out the GroupID of this one.
                            _isNPCFrigate = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsNPCFrigate as [" + _isNPCFrigate + "]");
                            Cache.Instance.EntityIsNPCFrigate.Add(Id, (bool) _isNPCFrigate);
                            return (bool) _isNPCFrigate;
                        }

                        return (bool) _isNPCFrigate;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Cruiser includes all elite-variants
        /// </summary>
        public bool IsCruiser
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isCruiser == null)
                        {
                            if (Cache.Instance.EntityIsCruiser.Any() && Cache.Instance.EntityIsCruiser.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsCruiser.Count() + "] Entities in Cache.Instance.EntityIsCruiser");
                            }

                            if (Cache.Instance.EntityIsCruiser.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsCruiser.TryGetValue(Id, out value))
                                {
                                    _isCruiser = value;
                                    return (bool) _isCruiser;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Cruiser;
                            result |= GroupId == (int) Group.HeavyAssaultShip;
                            result |= GroupId == (int) Group.Logistics;
                            result |= GroupId == (int) Group.ForceReconShip;
                            result |= GroupId == (int) Group.CombatReconShip;
                            result |= GroupId == (int) Group.HeavyInterdictor;

                            _isCruiser = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsCruiser as [" + _isCruiser + "]");
                            Cache.Instance.EntityIsCruiser.Add(Id, (bool) _isCruiser);
                            return (bool) _isCruiser;
                        }

                        return (bool) _isCruiser;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Cruiser includes all elite-variants
        /// </summary>
        public bool IsNPCCruiser
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isNPCCruiser == null)
                        {
                            if (Cache.Instance.EntityIsNPCCruiser.Any() && Cache.Instance.EntityIsNPCCruiser.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsNPCCruiser.Count() + "] Entities in Cache.Instance.EntityIsNPCCruiser");
                            }

                            if (Cache.Instance.EntityIsNPCCruiser.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsNPCCruiser.TryGetValue(Id, out value))
                                {
                                    _isNPCCruiser = value;
                                    return (bool) _isNPCCruiser;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Storyline_Cruiser;
                            result |= GroupId == (int) Group.Storyline_Mission_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Guristas_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_Cruiser;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Cruiser;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Cruiser;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Cruiser;
                            result |= GroupId == (int) Group.Mission_Khanid_Cruiser;
                            result |= GroupId == (int) Group.Mission_CONCORD_Cruiser;
                            result |= GroupId == (int) Group.Mission_Mordu_Cruiser;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_Cruiser;
                            result |= GroupId == (int) Group.Mission_Generic_Cruisers;
                            result |= GroupId == (int) Group.Deadspace_Overseer_Cruiser;
                            result |= GroupId == (int) Group.Mission_Thukker_Cruiser;
                            result |= GroupId == (int) Group.Mission_Generic_Battle_Cruisers;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_Cruiser;
                            result |= GroupId == (int) Group.Mission_Faction_Cruiser;
                            result |= GroupId == (int) Group.Mission_Faction_Industrials;
                            _isNPCCruiser = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsNPCCruiser as [" + _isNPCCruiser + "]");
                            Cache.Instance.EntityIsNPCCruiser.Add(Id, (bool) _isNPCCruiser);
                            return (bool) _isNPCCruiser;
                        }

                        return (bool) _isNPCCruiser;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     BattleCruiser includes all elite-variants
        /// </summary>
        public bool IsBattlecruiser
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isBattleCruiser == null)
                        {
                            if (Cache.Instance.EntityIsBattleCruiser.Any() && Cache.Instance.EntityIsBattleCruiser.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsBattleCruiser.Count() + "] Entities in Cache.Instance.EntityIsBattleCruiser");
                            }

                            if (Cache.Instance.EntityIsBattleCruiser.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsBattleCruiser.TryGetValue(Id, out value))
                                {
                                    _isBattleCruiser = value;
                                    return (bool) _isBattleCruiser;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Battlecruiser;
                            result |= GroupId == (int) Group.CommandShip;
                            result |= GroupId == (int) Group.StrategicCruiser; // Technically a cruiser, but hits hard enough to be a BC :)
                            _isBattleCruiser = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsBattleCruiser as [" + _isBattleCruiser + "]");
                            Cache.Instance.EntityIsBattleCruiser.Add(Id, (bool) _isBattleCruiser);
                            return (bool) _isBattleCruiser;
                        }

                        return (bool) _isBattleCruiser;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     BattleCruiser includes all elite-variants
        /// </summary>
        public bool IsNPCBattlecruiser
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isNPCBattleCruiser == null)
                        {
                            if (Cache.Instance.EntityIsNPCBattleCruiser.Any() && Cache.Instance.EntityIsNPCBattleCruiser.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsNPCBattleCruiser.Count() + "] Entities in Cache.Instance.EntityIsNPCBattleCruiser");
                            }

                            if (Cache.Instance.EntityIsNPCBattleCruiser.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsNPCBattleCruiser.TryGetValue(Id, out value))
                                {
                                    _isNPCBattleCruiser = value;
                                    return (bool) _isNPCBattleCruiser;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Guristas_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_BattleCruiser;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Khanid_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_CONCORD_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Mordu_Battlecruiser;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Mission_Thukker_Battlecruiser;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_BattleCruiser;
                            _isNPCBattleCruiser = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsNPCBattleCruiser as [" + _isNPCBattleCruiser + "]");
                            Cache.Instance.EntityIsNPCBattleCruiser.Add(Id, (bool) _isNPCBattleCruiser);
                            return (bool) _isNPCBattleCruiser;
                        }

                        return (bool) _isNPCBattleCruiser;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Battleship includes all elite-variants
        /// </summary>
        public bool IsBattleship
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isBattleship == null)
                        {
                            if (Cache.Instance.EntityIsBattleShip.Any() && Cache.Instance.EntityIsBattleShip.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsBattleShip.Count() + "] Entities in Cache.Instance.EntityIsBattleShip");
                            }

                            if (Cache.Instance.EntityIsBattleShip.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsBattleShip.TryGetValue(Id, out value))
                                {
                                    _isBattleship = value;
                                    return (bool) _isBattleship;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Battleship;
                            result |= GroupId == (int) Group.EliteBattleship;
                            result |= GroupId == (int) Group.BlackOps;
                            result |= GroupId == (int) Group.Marauder;
                            _isBattleship = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsBattleShip as [" + _isBattleship + "]");
                            Cache.Instance.EntityIsBattleShip.Add(Id, (bool) _isBattleship);
                            return (bool) _isBattleship;
                        }

                        return (bool) _isBattleship;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        /// <summary>
        ///     Battleship includes all elite-variants
        /// </summary>
        public bool IsNPCBattleship
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isNPCBattleship == null)
                        {
                            if (Cache.Instance.EntityIsNPCBattleShip.Any() && Cache.Instance.EntityIsNPCBattleShip.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsNPCBattleShip.Count() + "] Entities in Cache.Instance.EntityIsNPCBattleShip");
                            }

                            if (Cache.Instance.EntityIsNPCBattleShip.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsNPCBattleShip.TryGetValue(Id, out value))
                                {
                                    _isNPCBattleship = value;
                                    return (bool) _isNPCBattleship;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Storyline_Battleship;
                            result |= GroupId == (int) Group.Storyline_Mission_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Guristas_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_Battleship;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Battleship;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Battleship;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Battleship;
                            result |= GroupId == (int) Group.Mission_Khanid_Battleship;
                            result |= GroupId == (int) Group.Mission_CONCORD_Battleship;
                            result |= GroupId == (int) Group.Mission_Mordu_Battleship;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Battleship;
                            result |= GroupId == (int) Group.Mission_Generic_Battleships;
                            result |= GroupId == (int) Group.Deadspace_Overseer_Battleship;
                            result |= GroupId == (int) Group.Mission_Thukker_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_Battleship;
                            result |= GroupId == (int) Group.Mission_Faction_Battleship;
                            _isNPCBattleship = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsNPCBattleShip as [" + _isNPCBattleship + "]");
                            Cache.Instance.EntityIsNPCBattleShip.Add(Id, (bool) _isNPCBattleship);
                            return (bool) _isNPCBattleship;
                        }

                        return (bool) _isNPCBattleship;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsLargeCollidable
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isLargeCollidable == null)
                        {
                            if (Cache.Instance.EntityIsLargeCollidable.Any() && Cache.Instance.EntityIsLargeCollidable.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsLargeCollidable.Count() + "] Entities in Cache.Instance.EntityIsLargeCollidable");
                            }

                            if (Cache.Instance.EntityIsLargeCollidable.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsLargeCollidable.TryGetValue(Id, out value))
                                {
                                    _isLargeCollidable = value;
                                    return (bool) _isLargeCollidable;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.LargeColidableObject;
                            result |= GroupId == (int) Group.LargeColidableShip;
                            result |= GroupId == (int) Group.LargeColidableStructure;
                            result |= GroupId == (int) Group.DeadSpaceOverseersStructure;
                            result |= GroupId == (int) Group.DeadSpaceOverseersBelongings;
                            _isLargeCollidable = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsLargeCollidableObject as [" + _isLargeCollidable + "]");
                            Cache.Instance.EntityIsLargeCollidable.Add(Id, (bool) _isLargeCollidable);
                            return (bool) _isLargeCollidable;
                        }

                        return (bool) _isLargeCollidable;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsMiscJunk
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isMiscJunk == null)
                        {
                            if (Cache.Instance.EntityIsMiscJunk.Any() && Cache.Instance.EntityIsMiscJunk.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsMiscJunk.Count() + "] Entities in Cache.Instance.EntityIsMiscJunk");
                            }

                            if (Cache.Instance.EntityIsMiscJunk.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsMiscJunk.TryGetValue(Id, out value))
                                {
                                    _isMiscJunk = value;
                                    return (bool) _isMiscJunk;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.PlayerDrone;
                            result |= GroupId == (int) Group.Wreck;
                            result |= GroupId == (int) Group.AccelerationGate;
                            result |= GroupId == (int) Group.GasCloud;
                            _isMiscJunk = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsMiscJunk as [" + _isMiscJunk + "]");
                            Cache.Instance.EntityIsMiscJunk.Add(Id, (bool) _isMiscJunk);
                            return (bool) _isMiscJunk;
                        }

                        return (bool) _isMiscJunk;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsBadIdea
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_IsBadIdea == null)
                        {
                            if (Cache.Instance.EntityIsBadIdea.Any() && Cache.Instance.EntityIsBadIdea.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsBadIdea.Count() + "] Entities in Cache.Instance.EntityIsBadIdea");
                            }

                            if (Cache.Instance.EntityIsBadIdea.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsBadIdea.TryGetValue(Id, out value))
                                {
                                    _IsBadIdea = value;
                                    return (bool) _IsBadIdea;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.ConcordDrone;
                            result |= GroupId == (int) Group.PoliceDrone;
                            result |= GroupId == (int) Group.CustomsOfficial;
                            result |= GroupId == (int) Group.Billboard;
                            result |= GroupId == (int) Group.Stargate;
                            result |= GroupId == (int) Group.Station;
                            result |= GroupId == (int) Group.SentryGun;
                            result |= GroupId == (int) Group.Capsule;
                            result |= GroupId == (int) Group.MissionContainer;
                            result |= GroupId == (int) Group.CustomsOffice;
                            result |= GroupId == (int) Group.GasCloud;
                            result |= GroupId == (int) Group.ConcordBillboard;
                            result |= IsFrigate;
                            result |= IsCruiser;
                            result |= IsBattlecruiser;
                            result |= IsBattleship;
                            result |= IsPlayer;
                            _IsBadIdea = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsBadIdea as [" + _IsBadIdea + "]");
                            Cache.Instance.EntityIsBadIdea.Add(Id, (bool) _IsBadIdea);
                            return (bool) _IsBadIdea;
                        }

                        return (bool) _IsBadIdea;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsFactionWarfareNPC
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.FactionWarfareNPC;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsNpcByGroupID
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isNpcByGroupID == null)
                        {
                            if (Cache.Instance.EntityIsNPCByGroupID.Any() && Cache.Instance.EntityIsNPCByGroupID.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsNPCByGroupID.Count() + "] Entities in Cache.Instance.EntityIsNPCByGroupID");
                            }

                            if (Cache.Instance.EntityIsNPCByGroupID.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsNPCByGroupID.TryGetValue(Id, out value))
                                {
                                    _isNpcByGroupID = value;
                                    return (bool) _isNpcByGroupID;
                                }
                            }

                            var result = false;
                            result |= IsLargeCollidable;
                            result |= IsSentry;
                            result |= GroupId == (int) Group.DeadSpaceOverseersStructure;
                            //result |= GroupId == (int)Group.DeadSpaceOverseersBelongings;
                            result |= GroupId == (int) Group.Storyline_Battleship;
                            result |= GroupId == (int) Group.Storyline_Mission_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Guristas_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_Battleship;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Battleship;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Battleship;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Battleship;
                            result |= GroupId == (int) Group.Mission_Khanid_Battleship;
                            result |= GroupId == (int) Group.Mission_CONCORD_Battleship;
                            result |= GroupId == (int) Group.Mission_Mordu_Battleship;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Battleship;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Battleship;
                            result |= GroupId == (int) Group.Mission_Generic_Battleships;
                            result |= GroupId == (int) Group.Deadspace_Overseer_Battleship;
                            result |= GroupId == (int) Group.Mission_Thukker_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_Battleship;
                            result |= GroupId == (int) Group.Mission_Faction_Battleship;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Guristas_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_BattleCruiser;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Khanid_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_CONCORD_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Mordu_Battlecruiser;
                            result |= GroupId == (int) Group.Mission_Faction_Industrials;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Mission_Thukker_Battlecruiser;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_BattleCruiser;
                            result |= GroupId == (int) Group.Storyline_Cruiser;
                            result |= GroupId == (int) Group.Storyline_Mission_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Guristas_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_Cruiser;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Cruiser;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Cruiser;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Cruiser;
                            result |= GroupId == (int) Group.Mission_Khanid_Cruiser;
                            result |= GroupId == (int) Group.Mission_CONCORD_Cruiser;
                            result |= GroupId == (int) Group.Mission_Mordu_Cruiser;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_Cruiser;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_Cruiser;
                            result |= GroupId == (int) Group.Mission_Generic_Cruisers;
                            result |= GroupId == (int) Group.Deadspace_Overseer_Cruiser;
                            result |= GroupId == (int) Group.Mission_Thukker_Cruiser;
                            result |= GroupId == (int) Group.Mission_Generic_Battle_Cruisers;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_Cruiser;
                            result |= GroupId == (int) Group.Mission_Faction_Cruiser;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Angel_Cartel_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Blood_Raiders_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Guristas_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Sanshas_Nation_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Serpentis_Destroyer;
                            result |= GroupId == (int) Group.Mission_Amarr_Empire_Destroyer;
                            result |= GroupId == (int) Group.Mission_Caldari_State_Destroyer;
                            result |= GroupId == (int) Group.Mission_Gallente_Federation_Destroyer;
                            result |= GroupId == (int) Group.Mission_Minmatar_Republic_Destroyer;
                            result |= GroupId == (int) Group.Mission_Khanid_Destroyer;
                            result |= GroupId == (int) Group.Mission_CONCORD_Destroyer;
                            result |= GroupId == (int) Group.Mission_Mordu_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Angel_Cartel_Commander_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Blood_Raiders_Commander_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Guristas_Commander_Destroyer;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Sanshas_Nation_Commander_Destroyer;
                            result |= GroupId == (int) Group.Asteroid_Serpentis_Commander_Destroyer;
                            result |= GroupId == (int) Group.Mission_Thukker_Destroyer;
                            result |= GroupId == (int) Group.Mission_Generic_Destroyers;
                            result |= GroupId == (int) Group.Asteroid_Rogue_Drone_Commander_Destroyer;
                            result |= GroupId == (int) Group.TutorialDrone;
                            result |= GroupId == (int) Group.asteroid_angel_cartel_frigate;
                            result |= GroupId == (int) Group.asteroid_blood_raiders_frigate;
                            result |= GroupId == (int) Group.asteroid_guristas_frigate;
                            result |= GroupId == (int) Group.asteroid_sanshas_nation_frigate;
                            result |= GroupId == (int) Group.asteroid_serpentis_frigate;
                            result |= GroupId == (int) Group.deadspace_angel_cartel_frigate;
                            result |= GroupId == (int) Group.deadspace_blood_raiders_frigate;
                            result |= GroupId == (int) Group.deadspace_guristas_frigate;
                            result |= GroupId == (int) Group.Deadspace_Overseer_Frigate;
                            result |= GroupId == (int) Group.Deadspace_Rogue_Drone_Swarm;
                            result |= GroupId == (int) Group.deadspace_sanshas_nation_frigate;
                            result |= GroupId == (int) Group.deadspace_serpentis_frigate;
                            result |= GroupId == (int) Group.mission_amarr_empire_frigate;
                            result |= GroupId == (int) Group.mission_caldari_state_frigate;
                            result |= GroupId == (int) Group.mission_gallente_federation_frigate;
                            result |= GroupId == (int) Group.mission_minmatar_republic_frigate;
                            result |= GroupId == (int) Group.mission_khanid_frigate;
                            result |= GroupId == (int) Group.mission_concord_frigate;
                            result |= GroupId == (int) Group.mission_mordu_frigate;
                            result |= GroupId == (int) Group.asteroid_rouge_drone_frigate;
                            result |= GroupId == (int) Group.deadspace_rogue_drone_frigate;
                            result |= GroupId == (int) Group.asteroid_angel_cartel_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_blood_raiders_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_guristas_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_sanshas_nation_commander_frigate;
                            result |= GroupId == (int) Group.asteroid_serpentis_commander_frigate;
                            result |= GroupId == (int) Group.mission_generic_frigates;
                            result |= GroupId == (int) Group.mission_thukker_frigate;
                            result |= GroupId == (int) Group.asteroid_rouge_drone_commander_frigate;
                            _isNpcByGroupID = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsNPCByGroupID as [" + _isNpcByGroupID + "]");
                            Cache.Instance.EntityIsNPCByGroupID.Add(Id, (bool) _isNpcByGroupID);
                            return (bool) _isNpcByGroupID;
                        }

                        return (bool) _isNpcByGroupID;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsEntityIShouldLeaveAlone
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isEntityIShouldLeaveAlone == null)
                        {
                            if (Cache.Instance.EntityIsEntutyIShouldLeaveAlone.Any() &&
                                Cache.Instance.EntityIsEntutyIShouldLeaveAlone.Count() > DictionaryCountThreshhold)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We have [" + Cache.Instance.EntityIsEntutyIShouldLeaveAlone.Count() +
                                        "] Entities in Cache.Instance.EntityIsEntutyIShouldLeaveAlone");
                            }

                            if (Cache.Instance.EntityIsEntutyIShouldLeaveAlone.Any())
                            {
                                bool value;
                                if (Cache.Instance.EntityIsEntutyIShouldLeaveAlone.TryGetValue(Id, out value))
                                {
                                    _isEntityIShouldLeaveAlone = value;
                                    return (bool) _isEntityIShouldLeaveAlone;
                                }
                            }

                            var result = false;
                            result |= GroupId == (int) Group.Merchant; // Merchant, Convoy?
                            result |= GroupId == (int) Group.Mission_Merchant; // Merchant, Convoy? - Dread Pirate Scarlet
                            result |= GroupId == (int) Group.FactionWarfareNPC;
                            result |= IsOreOrIce;
                            _isEntityIShouldLeaveAlone = result;
                            if (Logging.Logging.DebugEntityCache)
                                Logging.Logging.Log("Adding [" + Name + "] to EntityIsEntutyIShouldLeaveAlone as [" + _isEntityIShouldLeaveAlone + "]");
                            Cache.Instance.EntityIsEntutyIShouldLeaveAlone.Add(Id, (bool) _isEntityIShouldLeaveAlone);
                            return (bool) _isEntityIShouldLeaveAlone;
                        }

                        return (bool) _isEntityIShouldLeaveAlone;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsOnGridWithMe
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (_isOnGridWithMe == null)
                        {
                            if (Distance < (double) Distances.OnGridWithMe)
                            {
                                _isOnGridWithMe = true;
                                return (bool) _isOnGridWithMe;
                            }

                            _isOnGridWithMe = false;
                            return (bool) _isOnGridWithMe;
                        }

                        return (bool) _isOnGridWithMe;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsStation
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.Station;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsCustomsOffice
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.CustomsOffice;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsCelestial
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= CategoryId == (int) CategoryID.Celestial;
                        result |= CategoryId == (int) CategoryID.Station;
                        result |= GroupId == (int) Group.Moon;
                        result |= GroupId == (int) Group.AsteroidBelt;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAsteroidBelt
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.AsteroidBelt;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsPlanet
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.Planet;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsMoon
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.Moon;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsAsteroid
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= CategoryId == (int) CategoryID.Asteroid;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsShipWithOreHold
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= TypeId == (int) TypeID.Venture;
                        result |= GroupId == (int) Group.MiningBarge;
                        result |= GroupId == (int) Group.Exhumer;
                        result |= GroupId == (int) Group.IndustrialCommandShip; // Orca
                        result |= GroupId == (int) Group.CapitalIndustrialShip; // Rorqual
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsShipWithNoDroneBay
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= TypeId == (int) TypeID.Tengu;
                        result |= GroupId == (int) Group.Shuttle;
                        if (Cache.Instance.InSpace && Cache.Instance.InMission)
                        {
                            if (Drones.DroneBay != null && Drones.DroneBay.IsReady)
                            {
                                //
                                // can or should we just check for drone bandwidth?
                                //
                                if (Drones.DroneBay.Volume == 0)
                                {
                                    if (Logging.Logging.DebugDrones) Logging.Logging.Log("Dronebay Volume = 0");
                                    //result = true; // no drone bay available
                                }
                            }
                        }

                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsShipWithNoCargoBay
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.Capsule;
                        //result |= GroupId == (int)Group.Shuttle;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool SalvagersAvailable
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= Cache.Instance.Modules.Any(m => m.GroupId == (int) Group.Salvager && m.IsOnline);
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsOreOrIce
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= GroupId == (int) Group.Plagioclase;
                        result |= GroupId == (int) Group.Spodumain;
                        result |= GroupId == (int) Group.Kernite;
                        result |= GroupId == (int) Group.Hedbergite;
                        result |= GroupId == (int) Group.Arkonor;
                        result |= GroupId == (int) Group.Bistot;
                        result |= GroupId == (int) Group.Pyroxeres;
                        result |= GroupId == (int) Group.Crokite;
                        result |= GroupId == (int) Group.Jaspet;
                        result |= GroupId == (int) Group.Omber;
                        result |= GroupId == (int) Group.Scordite;
                        result |= GroupId == (int) Group.Gneiss;
                        result |= GroupId == (int) Group.Veldspar;
                        result |= GroupId == (int) Group.Hemorphite;
                        result |= GroupId == (int) Group.DarkOchre;
                        result |= GroupId == (int) Group.Ice;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsEwarImmune
        {
            get
            {
                try
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        var result = false;
                        result |= TypeId == (int) TypeID.Zor;
                        return result;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public double? DistanceFromEntity(EntityCache OtherEntityToMeasureFrom)
        {
            try
            {
                if (OtherEntityToMeasureFrom == null)
                {
                    return null;
                }

                var deltaX = XCoordinate - OtherEntityToMeasureFrom.XCoordinate;
                var deltaY = YCoordinate - OtherEntityToMeasureFrom.YCoordinate;
                var deltaZ = ZCoordinate - OtherEntityToMeasureFrom.ZCoordinate;

                return Math.Sqrt((deltaX*deltaX) + (deltaY*deltaY) + (deltaZ*deltaZ));
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return 0;
            }
        }

        public bool BookmarkThis(string NameOfBookmark = "bookmark", string Comment = "")
        {
            try
            {
                if (Cache.Instance.BookmarksByLabel(NameOfBookmark).Any(i => i.LocationId == Cache.Instance.DirectEve.Session.LocationId))
                {
                    var PreExistingBookmarks = Cache.Instance.BookmarksByLabel(NameOfBookmark);
                    if (PreExistingBookmarks.Any())
                    {
                        foreach (var _PreExistingBookmark in PreExistingBookmarks)
                        {
                            if (_PreExistingBookmark.X == _directEntity.X
                                && _PreExistingBookmark.Y == _directEntity.Y
                                && _PreExistingBookmark.Z == _directEntity.Z)
                            {
                                if (Logging.Logging.DebugEntityCache)
                                    Logging.Logging.Log("We already have a bookmark for [" + Name + "] and do not need another.");
                                return true;
                            }
                            continue;
                        }
                    }
                }

                if (IsLargeCollidable || IsStation || IsAsteroid || IsAsteroidBelt)
                {
                    Cache.Instance.DirectEve.BookmarkEntity(_directEntity, NameOfBookmark, Comment, 0);
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            return false;
        }

        public bool LockTarget(string module)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.NextTargetAction)
                {
                    return false;
                }

                if (_directEntity != null && _directEntity.IsValid)
                {
                    if (!IsTarget)
                    {
                        if (!HasExploded)
                        {
                            if (Distance < Combat.Combat.MaxTargetRange)
                            {
                                if (Cache.Instance.Targets.Count() < Cache.Instance.MaxLockedTargets)
                                {
                                    if (!IsTargeting)
                                    {
                                        if (Cache.Instance.EntitiesOnGrid.Any(i => i.Id == Id))
                                        {
                                            // If the bad idea is attacking, attack back
                                            if (IsBadIdea && !IsAttacking)
                                            {
                                                Logging.Logging.Log("[" + module + "] Attempted to target a player or concord entity! [" + Name + "] - aborting");
                                                return false;
                                            }

                                            if (Distance >= 250001 || Distance > Combat.Combat.MaxTargetRange) //250k is the MAX targeting range in eve.
                                            {
                                                Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "] which is [" + Math.Round(Distance / 1000, 2) +
                                                    "k] away. Do not try to lock things that you cant possibly target");
                                                return false;
                                            }

                                            // Remove the target info (its been targeted)
                                            foreach (
                                                var target in
                                                    Cache.Instance.EntitiesOnGrid.Where(e => e.IsTarget && Cache.Instance.TargetingIDs.ContainsKey(e.Id)))
                                            {
                                                Cache.Instance.TargetingIDs.Remove(target.Id);
                                            }

                                            if (Cache.Instance.TargetingIDs.ContainsKey(Id))
                                            {
                                                var lastTargeted = Cache.Instance.TargetingIDs[Id];

                                                // Ignore targeting request
                                                var seconds = DateTime.UtcNow.Subtract(lastTargeted).TotalSeconds;
                                                if (seconds < 20)
                                                {
                                                    Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId +
                                                        "][" + Cache.Instance.Targets.Count() + "] targets already, can reTarget in [" +
                                                        Math.Round(20 - seconds, 0) + "]");
                                                    return false;
                                                }
                                            }
                                            // Only add targeting id's when its actually being targeted

                                            var entId = _directEntity.Id;
                                            if (!Statistics.BountyValues.ContainsKey(entId))
                                            {
                                                var bounty = _directEntity.GetBounty();
                                                Logging.Logging.Log("Added bounty [" + bounty + "] ent.id [" + entId + "]");
                                                Statistics.BountyValues.AddOrUpdate(entId, bounty);
                                            }

                                            if (_directEntity.LockTarget())
                                            {
                                                //Cache.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                                                Cache.Instance.TargetingIDs[Id] = DateTime.UtcNow;
                                                return true;
                                            }

                                            Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                                                Cache.Instance.Targets.Count() + "] targets already, LockTarget failed (unknown reason)");
                                            return false;
                                        }

                                        Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                                            Cache.Instance.Targets.Count() + "] targets already, LockTarget failed: target was not in Entities List");
                                        return false;
                                    }

                                    Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                                        Cache.Instance.Targets.Count() + "] targets already, LockTarget aborted: target is already being targeted");
                                    return false;
                                }

                                Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                                    Cache.Instance.Targets.Count() + "] targets already, we only have [" + Cache.Instance.MaxLockedTargets + "] slots!");
                                return false;
                            }

                            Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                                Cache.Instance.Targets.Count() + "] targets already, my targeting range is only [" + Combat.Combat.MaxTargetRange + "]!");
                            return false;
                        }

                        Logging.Logging.Log("[" + module + "] tried to lock [" + Name + "][" + Cache.Instance.Targets.Count() + "] targets already, target is already dead!");
                        return false;
                    }

                    Logging.Logging.Log("[" + module + "] LockTarget request has been ignored for [" + Name + "][" + Math.Round(Distance / 1000, 2) + "k][" + MaskedId + "][" +
                        Cache.Instance.Targets.Count() + "] targets already, target is already locked!");
                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool UnlockTarget(string module)
        {
            try
            {
                if (_directEntity != null && _directEntity.IsValid)
                {
                    //if (Distance > 250001)
                    //{
                    //    return false;
                    //}

                    Cache.Instance.TargetingIDs.Remove(Id);

                    if (IsTarget)
                    {
                        _directEntity.UnlockTarget();
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Jump()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextJumpAction)
                {
                    if (Time.Instance.LastInSpace.AddSeconds(2) > DateTime.UtcNow && Cache.Instance.InSpace)
                    {
                        if (_directEntity != null && _directEntity.IsValid)
                        {
                            if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                            {
                                Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) +
                                    "k][" + MaskedId + "] was created more than 5 seconds ago (ugh!)");
                            }

                            if (Distance < 2500)
                            {
                                _directEntity.Jump();
                                Cache.Instance.ClearPerPocketCache("Jump()");
                                Time.Instance.LastSessionChange = DateTime.UtcNow;
                                Time.Instance.NextInSpaceorInStation = DateTime.UtcNow;
                                Time.Instance.WehaveMoved = DateTime.UtcNow.AddDays(-7);
                                Time.Instance.NextJumpAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(8, 12));
                                Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerJumpedGateNextCommandDelay_seconds);
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerJumpedGateNextCommandDelay_seconds);
                                return true;
                            }

                            Logging.Logging.Log("we tried to jump through [" + Name + "] but it is [" + Math.Round(Distance / 1000, 2) + "k away][" + MaskedId + "]");
                            return false;
                        }

                        Logging.Logging.Log("[" + Name + "] DirecEntity is null or is not valid");
                        return false;
                    }

                    Logging.Logging.Log("We have not yet been in space for 2 seconds, waiting");
                    return false;
                }

                Logging.Logging.Log("We still have [" + DateTime.UtcNow.Subtract(Time.Instance.NextJumpAction) + "] seconds until we should jump again.");
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Activate()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextActivateAction)
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        //we cant move in bastion mode, do not try
                        var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                        if (bastionModules.Any(i => i.IsActive))
                        {
                            Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Activate Gate");
                            return false;
                        }

                        _directEntity.Activate();
                        Cache.Instance.ClearPerPocketCache("Activate");
                        Time.Instance.LastInWarp = DateTime.UtcNow;
                        Time.Instance.NextActivateAction = DateTime.UtcNow.AddSeconds(15);
                        return true;
                    }

                    Logging.Logging.Log("[" + Name + "] DirecEntity is null or is not valid");
                    return false;
                }

                Logging.Logging.Log("You have another [" + Time.Instance.NextActivateAction.Subtract(DateTime.UtcNow).TotalSeconds +
                    "] sec before we should attempt to activate [" + Name + "], waiting.");
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Approach()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                {
                    if (_directEntity != null && _directEntity.IsValid && DateTime.UtcNow > Time.Instance.NextApproachAction)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        //we cant move in bastion mode, do not try
                        var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                        if (bastionModules.Any(i => i.IsActive))
                        {
                            Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Approach");
                            return false;
                        }

                        _directEntity.Approach();
                        Time.Instance.LastApproachAction = DateTime.UtcNow;
                        Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds);
                        Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds);
                        Cache.Instance.Approaching = this;
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                Cache.Instance.Approaching = null;
                return false;
            }
        }

        public bool KeepAtRange(int range)
        {
            try
            {
                if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        //we cant move in bastion mode, do not try
                        var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                        if (bastionModules.Any(i => i.IsActive))
                        {
                            Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Approach");
                            return false;
                        }

                        Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds);
                        _directEntity.KeepAtRange(range);
                        Cache.Instance.Approaching = this;
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                Cache.Instance.Approaching = null;
                return false;
            }
        }

        public bool Orbit(int _orbitRange)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextOrbit)
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        //we cant move in bastion mode, do not try
                        var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                        if (bastionModules.Any(i => i.IsActive))
                        {
                            Logging.Logging.Log("BastionMode is active, we cannot move, aborting attempt to Orbit");
                            return false;
                        }

                        _directEntity.Orbit(_orbitRange);
                        Logging.Logging.Log("Initiating Orbit [" + Name + "][at " + Math.Round(((double)_orbitRange / 1000), 2) + "k][" + MaskedId + "]");
                        Time.Instance.NextOrbit = DateTime.UtcNow.AddSeconds(10 + Cache.Instance.RandomNumber(1, 15));
                        Cache.Instance.Approaching = this;
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                Cache.Instance.Approaching = null;
                return false;
            }
        }

        public bool WarpTo()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextWarpAction)
                {
                    if (Time.Instance.LastInSpace.AddSeconds(2) > DateTime.UtcNow && Cache.Instance.InSpace)
                    {
                        if (_directEntity != null && _directEntity.IsValid)
                        {
                            if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                            {
                                Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) +
                                    "k][" + MaskedId + "] was created more than 5 seconds ago (ugh!)");
                            }

                            //
                            // If the position we are trying to warp to is more than 1/2 a light year away it MUST be in a different solar system (31500+ AU)
                            //
                            if (Distance < (long) Distances.HalfOfALightYearInAU)
                            {
                                if (Distance > (int) Distances.WarptoDistance)
                                {
                                    //we cant move in bastion mode, do not try
                                    var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                                    if (bastionModules.Any(i => i.IsActive))
                                    {
                                        Logging.Logging.Log("BastionMode is active, we cannot warp, aborting attempt to warp");
                                        return false;
                                    }

                                    _directEntity.WarpTo();
                                    Cache.Instance.ClearPerPocketCache("WarpTo");
                                    Time.Instance.WehaveMoved = DateTime.UtcNow;
                                    Time.Instance.LastInWarp = DateTime.UtcNow;
                                    Time.Instance.NextWarpAction = DateTime.UtcNow.AddSeconds(Time.Instance.WarptoDelay_seconds);
                                    return true;
                                }

                                Logging.Logging.Log("[" + Name + "] Distance [" + Math.Round(Distance / 1000, 0) + "k] is not greater then 150k away, WarpTo aborted!");
                                return false;
                            }

                            Logging.Logging.Log("[" + Name + "] Distance [" + Math.Round(Distance / 1000, 0) +
                                "k] was greater than 5000AU away, we assume this an error!, WarpTo aborted!");
                            return false;
                        }

                        Logging.Logging.Log("[" + Name + "] DirecEntity is null or is not valid");
                        return false;
                    }

                    Logging.Logging.Log("We have not yet been in space at least 2 seconds, waiting");
                    return false;
                }

                //Logging.Log("EntityCache.WarpTo", "Waiting [" + Math.Round(Time.Instance.NextWarpAction.Subtract(DateTime.UtcNow).TotalSeconds,0) + "sec] before next attempted warp.", Logging.Debug);
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool AlignTo()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextAlign)
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        if (DateTime.UtcNow.AddSeconds(-5) > ThisEntityCacheCreated)
                        {
                            Logging.Logging.Log("The EntityCache instance that represents [" + _directEntity.Name + "][" + Math.Round(_directEntity.Distance / 1000, 0) + "k][" +
                                MaskedId + "] was created more than 5 seconds ago (ugh!)");
                        }

                        _directEntity.AlignTo();
                        Time.Instance.WehaveMoved = DateTime.UtcNow;
                        Time.Instance.NextAlign = DateTime.UtcNow.AddMinutes(Time.Instance.AlignDelay_minutes);
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool Dock()
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.LastInWarp.AddSeconds(5))
                {
                    return false;
                }

                if (DateTime.UtcNow > Time.Instance.NextDockAction)
                {
                    if (Time.Instance.LastInSpace.AddSeconds(2) > DateTime.UtcNow && Cache.Instance.InSpace &&
                        DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(20))
                    {
                        if (_directEntity != null && _directEntity.IsValid)
                        {
                            //if (Distance < (int) Distances.DockingRange)
                            //{

                            if (Cache.Instance.Modules.Any(m => m.IsOnline && m.IsActive && !m.IsDeactivating))
                            {
                                Time.Instance.NextDockAction = DateTime.UtcNow.AddMilliseconds(Cache.Instance.RandomNumber(500, 800));
                                return false;
                            }

                            _directEntity.Dock();
                            Time.Instance.WehaveMoved = DateTime.UtcNow;
                            Time.Instance.NextDockAction = DateTime.UtcNow.AddSeconds(Time.Instance.DockingDelay_seconds);
                            Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.DockingDelay_seconds);
                            Time.Instance.LastSessionChange = DateTime.UtcNow;
                            Time.Instance.LastDockAction = DateTime.UtcNow;
                            Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerJumpedGateNextCommandDelay_seconds);
                            //}

                            //Logging.Log("Dock", "[" + Name + "][" + Distance +"] is not in docking range, aborting docking request", Logging.Debug);
                            //return false;
                        }

                        //Logging.Log("Dock", "[" + Name + "]: directEntity is null or is not valid", Logging.Debug);
                        return false;
                    }

                    Logging.Logging.Log("We were last detected in space [" + DateTime.UtcNow.Subtract(Time.Instance.LastInSpace).TotalSeconds +
                        "] seconds ago. We have been unDocked for [ " + DateTime.UtcNow.Subtract(Time.Instance.LastInStation).TotalSeconds +
                        " ] seconds. we should not dock yet, waiting");
                    return false;
                }

                //Logging.Log("Dock", "Dock command will not be allowed again until after another [" + Math.Round(Time.Instance.NextDockAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.Red);
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public bool OpenCargo()
        {
            try
            {
                if (DateTime.UtcNow > Time.Instance.NextOpenCargoAction)
                {
                    if (_directEntity != null && _directEntity.IsValid)
                    {
                        _directEntity.OpenCargo();
                        Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public void MakeActiveTarget()
        {
            try
            {
                if (_directEntity != null && _directEntity.IsValid)
                {
                    if (IsTarget)
                    {
                        _directEntity.MakeActiveTarget();
                        Time.Instance.NextMakeActiveTargetAction = DateTime.UtcNow.AddSeconds(1 + Cache.Instance.RandomNumber(2, 3));
                    }

                    return;
                }

                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }
    }
}