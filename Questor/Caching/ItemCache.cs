//------------------------------------------------------------------------------
//  <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//    Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//    Please look in the accompanying license.htm file for the license that
//    applies to this source code. (a copy can also be found at:
//    http://www.thehackerwithin.com/license.htm)
//  </copyright>
//-------------------------------------------------------------------------------

namespace Questor.Modules.Caching
{
    using System;
    using System.Collections.Generic;
    using DirectEve;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.States;

    public class ItemCache
    {
        public ItemCache(DirectItem item, bool cacheRefineOutput)
        {
            try
            {
                //Id = item.ItemId;
                //Name = item.TypeName;
                //TypeId = item.TypeId;
                //Volume = item.Volume;
                //Quantity = item.Quantity;
                NameForSorting = item.TypeName.Replace("'", "");
                GroupId = item.GroupId;
                CategoryId = item.CategoryId;
                BasePrice = item.BasePrice;
                Capacity = item.Capacity;
                MarketGroupId = item.MarketGroupId;
                PortionSize = item.PortionSize;

                QuantitySold = 0;

                RefineOutput = new List<ItemCache>();
                if (cacheRefineOutput)
                {
                    foreach (DirectItem i in item.Materials)
                        RefineOutput.Add(new ItemCache(i, false));
                }

                maxVelocity = item.Attributes.TryGet<int>("maxVelocity");
                
                emDamage = item.Attributes.TryGet<int>("emDamage");
                explosiveDamage = item.Attributes.TryGet<int>("explosiveDamage");
                kineticDamage = item.Attributes.TryGet<int>("explosiveDamage");
                thermalDamage = item.Attributes.TryGet<int>("thermalDamage");
                metaLevel = item.Attributes.TryGet<int>("metaLevel");
                hp = item.Attributes.TryGet<int>("hp");
                techLevel = item.Attributes.TryGet<int>("techLevel");
                radius = item.Attributes.TryGet<int>("radius");

                //
                // only useful for missiles should we not pull this info for items that wont have these attributes?!
                //
                aoeDamageReductionFactor = item.Attributes.TryGet<int>("aoeDamageReductionFactor");
                detonationRange = item.Attributes.TryGet<int>("detonationRange");
                aoeCloudSize = item.Attributes.TryGet<int>("aoeCloudSize");
                aoeVelocity = item.Attributes.TryGet<int>("aoeVelocity");
                agility = item.Attributes.TryGet<int>("agility");
                explosionDelay = item.Attributes.TryGet<int>("explosionDelay");
                maxVelocityBonus = item.Attributes.TryGet<int>("maxVelocityBonus");
                
                //
                // only useful for AutoCannon / Artillery and Blaster/RailGun ammo should we not pull this info for items that wont have these attributes?!
                //
                fallofMultiplier = item.Attributes.TryGet<int>("fallofMultiplier");
                weaponRangeMultiplier = item.Attributes.TryGet<int>("weaponRangeMultiplier");
                trackingSpeedMultiplier = item.Attributes.TryGet<int>("trackingSpeedMultiplier");
                powerNeedMultiplier = item.Attributes.TryGet<int>("powerNeedMultiplier");

            }
            catch (Exception exception)
            {
                Logging.Log("ItemCache", "Exception [" + exception + "]", Logging.Debug);
            }
        }

        public int maxVelocity { get; set; } //(int)ammo.Attribute("maxVelocity");
        public int emDamage { get; set; } //(int)ammo.Attribute("emDamage");
        public int explosiveDamage { get; set; } //(int)ammo.Attribute("explosiveDamage");
        public int kineticDamage { get; set; } //(int)ammo.Attribute("kineticDamage");
        public int thermalDamage { get; set; } //(int)ammo.Attribute("thermalDamage");
        public int metaLevel { get; set; } //(int)ammo.Attribute("metaLevel");
        public int hp { get; set; } //(int)ammo.Attribute("hp");
        public int techLevel { get; set; } //(int)ammo.Attribute("techLevel");
        public int radius { get; set; } //(int)ammo.Attribute("radius");

        //
        // only useful for missiles
        //
        public int aoeDamageReductionFactor { get; set; } //(int)ammo.Attribute("aoeDamageReductionFactor");
        public int detonationRange { get; set; } //(int)ammo.Attribute("detonationRange");
        public int aoeCloudSize { get; set; } //(int)ammo.Attribute("aoeCloudSize");
        public int aoeVelocity { get; set; } //(int)ammo.Attribute("aoeVelocity");
        public int agility { get; set; } //(int)ammo.Attribute("agility");
        public int explosionDelay { get; set; } //(int)ammo.Attribute("explosionDelay");
        public int maxVelocityBonus { get; set; } //(int)ammo.Attribute("maxVelocityBonus");
        
        //
        // only useful for AutoCannons and RailGuns
        //
        public int fallofMultiplier { get; set; } //(int)ammo.Attribute("fallofMultiplier");
        public int weaponRangeMultiplier { get; set; } //(int)ammo.Attribute("weaponRangeMultiplier");
        public int trackingSpeedMultiplier { get; set; } //(int)ammo.Attribute("trackingSpeedMultiplier");
        public int powerNeedMultiplier { get; set; } //(int)ammo.Attribute("powerNeedMultiplier");
        
        public string NameForSorting { get; private set; }

        public int GroupId { get; private set; }

        public int CategoryId { get; private set; }

        public double BasePrice { get; private set; }

        public double Capacity { get; private set; }

        public int MarketGroupId { get; private set; }

        public int PortionSize { get; private set; }

        public int QuantitySold { get; set; }

        public double? StationBuy { get; set; }

        public List<ItemCache> RefineOutput { get; private set; }

        private readonly DirectItem _directItem;

        public ItemCache(DirectItem item)
        {
            _directItem = item;
        }

        public DirectItem DirectItem
        {
            get { return _directItem; }
        }

        public long Id
        {
            get { return _directItem.ItemId; }
        }

        public int TypeId
        {
            get { return _directItem.TypeId; }
            //private set { return value};
        }

        public int GroupID
        {
            get { return _directItem.GroupId; }
        }

        public int Quantity
        {
            get { return _directItem.Quantity; }
            //private set (return value);
        }

        public bool IsContraband
        {
            get
            {
                bool result = false;
                result |= (GroupID == (int)Group.Drugs);
                result |= (GroupID == (int)Group.ToxicWaste);
                result |= (TypeId == (int)TypeID.Slaves);
                result |= (TypeId == (int)TypeID.Small_Arms);
                result |= (TypeId == (int)TypeID.Ectoplasm);
                //result |= (TypeId == (int)TypeID.AIMEDs);
                return result;
            }
        }

        public bool IsAliveandWontFitInContainers
        {
            get
            {
                if (TypeId == 41) return true;      // Garbage
                if (TypeId == 42) return true;      // Spiced Wine
                if (TypeId == 42) return true;      // Antibiotics
                if (TypeId == 44) return true;      // Enriched Uranium
                if (TypeId == 45) return true;      // Frozen Plant Seeds
                if (TypeId == 3673) return true;    // Wheat
                if (TypeId == 3699) return true;    // Quafe
                if (TypeId == 3715) return true;    // Frozen Food
                if (TypeId == 3717) return true;    // Dairy Products
                if (TypeId == 3721) return true;    // Slaves
                if (TypeId == 3723) return true;    // Slaver Hound
                if (TypeId == 3725) return true;    // Livestock
                if (TypeId == 3727) return true;    // Plutonium
                if (TypeId == 3729) return true;    // Toxic Waste
                if (TypeId == 3771) return true;    // Ectoplasm
                if (TypeId == 3773) return true;    // Hydrochloric Acid
                if (TypeId == 3775) return true;    // Viral Agent
                if (TypeId == 3777) return true;    // Long-limb Roes
                if (TypeId == 3779) return true;    // Biomass
                if (TypeId == 3804) return true;    // VIPs
                if (TypeId == 3806) return true;    // Refugees
                if (TypeId == 3808) return true;    // Prisoners
                //if (TypeId == 3810) return true;  // Marines **Common Mission Completion Item
                if (TypeId == 12865) return true;   // Quafe Ultra
                if (TypeId == 13267) return true;   // Janitor
                if (TypeId == 17765) return true;   // Exotic Dancers
                if (TypeId == 22208) return true;   // Prostitute
                if (TypeId == 22209) return true;   // Refugee
                if (TypeId == 22210) return true;   // Cloned SOE officer
                //if (TypeId == 25373) return true;   // Militants **Common Mission Completion Item
                // people (all the different kinds - ugh?)
                return false;
            }
        }

        public bool IsTypicalMissionCompletionItem
        {
            get
            {
                if (TypeId == 25373) return true;   // Militants
                if (TypeId == 3810) return true;    // Marines
                if (TypeId == 2076) return true;    // Gate Key
                if (TypeId == 24576) return true;    // Imperial Navy Gate Permit
                if (TypeId == 28260) return true;   // Zbikoki's Hacker Card
                if (TypeId == 3814) return true;    // Reports
                if (TypeId == 24766) return true;    // Ship's Crew

                return false;
            }
        }

        public bool IsOre
        {   // GroupIDs listed in this order: Plagioclase	Spodumain	Kernite	Hedbergite	Arkonor	Bistot	Pyroxeres	Crokite	Jaspet	Omber	Scordite	Gneiss	Veldspar	Hemorphite	Dark Ochre Ice
            get { return GroupID == 458 || GroupID == 461 || GroupID == 457 || GroupID == 454 || GroupID == 450 || GroupID == 451 || GroupID == 459 || GroupID == 452 || GroupID == 456 || GroupID == 469 || GroupID == 460 || GroupID == 467 || GroupID == 462 || GroupID == 455 || GroupID == 453 || GroupID == 465; }
        }

        public bool IsLowEndMineral
        {   // Tritanium, pyerite, mexalon
            get { return TypeId == 34 || TypeId == 35 || TypeId == 36; }
        }

        public bool IsHighEndMineral
        {   // isogen, nocxium, zydrine, megacyte
            get { return TypeId == 37 || TypeId == 38 || TypeId == 39 || TypeId == 40; }
        }

        public bool IsScrapMetal
        {
            get { return TypeId == 30497 || TypeId == 15331; }
        }

        public bool IsCommonMissionItem
        {   //Zbikoki's Hacker Card 28260, Reports 3814, Gate Key 2076, Militants 25373, Marines 3810
            get { return TypeId == 28260 || TypeId == 3814 || TypeId == 2076 || TypeId == 25373 || TypeId == 3810; }
        }

        public bool InjectSkillBook
        {   //Zbikoki's Hacker Card 28260, Reports 3814, Gate Key 2076, Militants 25373, Marines 3810
            get
            {
                if (_directItem.CategoryId == (int)CategoryID.Skill)
                {
                    _directItem.InjectSkill();    
                }
                
                return false;
            }
        }

        public bool DoesNotRequireAmmo
        {
            get
            {
                if (TypeId == (int)TypeID.CivilianGatlingPulseLaser) return true;
                if (TypeId == (int)TypeID.CivilianGatlingAutocannon) return true;
                if (TypeId == (int)TypeID.CivilianGatlingRailgun) return true;
                if (TypeId == (int)TypeID.CivilianLightElectronBlaster) return true;
                return false;
            }
        }

        public bool IsTurret
        {
            get
            {
                if (GroupId == (int)Group.EnergyWeapon) return true;
                if (GroupId == (int)Group.ProjectileWeapon) return true;
                if (GroupId == (int)Group.HybridWeapon) return true;
                return false;
            }
        }

        public bool IsMissionItem
        {
            get
            {
                try
                {
                    if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                    {
                        if (MissionSettings.MissionItems.Contains((Name ?? string.Empty).ToLower()))
                        {
                            return true;
                        }

                        if (Cache.Instance.ListofMissionCompletionItemsToLoot.Contains((Name ?? string.Empty).ToLower()))
                        {
                            return true;
                        }

                        if (IsTypicalMissionCompletionItem)
                        {
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Logging.Log("ItemCache.IsMissionItem", "Exception [" + ex + "]", Logging.Debug);
                    return false;
                }
            }
        }

        public bool IsLootForShipFitting
        {   //Named 100mn Afterburner, Named Target Painter (PWNAGE),
            // this needs attention // fix me
            get { return TypeId == 1 || TypeId == 1; }
        }

        public bool IsBookmark
        {
            get { return TypeId == 51; }
        }

        //public string Name { get; private set; }

        public string Name
        {
            get { return _directItem.TypeName; }
            //private set {return value;};
        }

        public double Volume
        {
            get { return _directItem.Volume; }
            //private set { return value; };
        }

        public double TotalVolume
        {
            get { return _directItem.Volume * Quantity; }
        }

        public double? IskPerM3
        {
            get
            {
                try
                {
                    if (_directItem != null)
                    {
                        if (_directItem.AveragePrice() > 0)
                        {
                            return _directItem.AveragePrice() / _directItem.Volume;
                        }

                        return 0.001;
                    }

                    return 0.001;
                }
                catch (Exception ex)
                {
                    Logging.Log("ItemCache.IskPerM3", "Exception [" + ex + "]", Logging.Debug);
                    return null;
                }
            }
        }

        public double? Value
        {
            get
            {
                try
                {
                    if (_directItem != null)
                    {
                        if (_directItem.AveragePrice() > 0)
                        {
                            return _directItem.AveragePrice();
                        }

                        return 0.001;
                    }

                    return 0.001;
                }
                catch (Exception ex)
                {
                    Logging.Log("ItemCache.Value","Exception [" + ex + "]",Logging.Debug);
                    return null;
                }
            }
        }
    }
}