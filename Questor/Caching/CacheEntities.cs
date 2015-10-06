// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias Ut;

namespace Questor.Modules.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using System.Threading;
	using global::Questor.Modules.Actions;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Combat;
	using global::Questor.Modules.Lookup;
	using global::Questor.Modules.States;
	using global::Questor.Modules.Logging;
	using DirectEve;
	using global::Questor.Storylines;
	using Ut::EVE;
	using Ut;
	using Ut::WCF;
	
	public partial class Cache
	{
		
		
				public IEnumerable<EntityCache> EntitiesByName(string nameToSearchFor, IEnumerable<EntityCache> EntitiesToLookThrough)
		{
			return EntitiesToLookThrough.Where(e => e.Name.ToLower() == nameToSearchFor.ToLower()).ToList();
		}

		public EntityCache EntityByName(string name)
		{
			return Cache.Instance.Entities.FirstOrDefault(e => System.String.Compare(e.Name, name, System.StringComparison.OrdinalIgnoreCase) == 0);
		}

		public IEnumerable<EntityCache> EntitiesByPartialName(string nameToSearchFor)
		{
			try
			{
				if (Cache.Instance.Entities != null && Cache.Instance.Entities.Any())
				{
					IEnumerable<EntityCache> _entitiesByPartialName = Cache.Instance.Entities.Where(e => e.Name.Contains(nameToSearchFor)).ToList();
					if (!_entitiesByPartialName.Any())
					{
						_entitiesByPartialName = Cache.Instance.Entities.Where(e => e.Name == nameToSearchFor).ToList();
					}
					
					//if we have no entities by that name return null;
					if (!_entitiesByPartialName.Any())
					{
						_entitiesByPartialName = null;
					}

					return _entitiesByPartialName;
				}

				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.allBookmarks", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		public IEnumerable<EntityCache> EntitiesThatContainTheName(string label)
		{
			try
			{
				return Cache.Instance.Entities.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.ToLower().Contains(label.ToLower())).ToList();
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.EntitiesThatContainTheName", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		public EntityCache EntityById(long id)
		{
			try
			{
				if (_entitiesById.ContainsKey(id))
				{
					return _entitiesById[id];
				}

				EntityCache entity = Cache.Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == id);
				_entitiesById[id] = entity;
				return entity;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.EntityById", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		public double DistanceFromMe(double x, double y, double z)
		{
			try
			{

				if (Cache.Instance.ActiveShip.Entity == null)
				{
					return double.MaxValue;
				}

				double curX = Cache.Instance.ActiveShip.Entity.X;
				double curY = Cache.Instance.ActiveShip.Entity.Y;
				double curZ = Cache.Instance.ActiveShip.Entity.Z;

				return Math.Round(Math.Sqrt((curX - x) * (curX - x) + (curY - y) * (curY - y) + (curZ - z) * (curZ - z)), 2);
			}
			catch (Exception ex)
			{
				Logging.Log("DistanceFromMe", "Exception [" + ex + "]", Logging.Debug);
				return 0;
			}
		}
		
		public Func<EntityCache, int> OrderByLowestHealth()
		{
			try
			{
				return t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct);
			}
			catch (Exception ex)
			{
				Logging.Log("OrderByLowestHealth", "Exception [" + ex + "]", Logging.Debug);
				return null;
			}
		}
		
		public int MaxLockedTargets
		{
			get
			{
				try
				{
					if (_maxLockedTargets == null)
					{
						_maxLockedTargets = Math.Min(Cache.Instance.DirectEve.Me.MaxLockedTargets, Cache.Instance.ActiveShip.MaxLockedTargets);
						return (int)_maxLockedTargets;
					}

					return (int)_maxLockedTargets;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.MaxLockedTargets", "Exception [" + exception + "]", Logging.Debug);
					return -1;
				}
			}
		}

		private List<EntityCache> _myAmmoInSpace;
		public IEnumerable<EntityCache> myAmmoInSpace
		{
			get
			{
				if (_myAmmoInSpace == null)
				{
					if (myCurrentAmmoInWeapon != null)
					{
						_myAmmoInSpace = Cache.Instance.Entities.Where(e => e.Distance > 3000 && e.IsOnGridWithMe && e.TypeId == myCurrentAmmoInWeapon.TypeId && e.Velocity > 50).ToList();
						if (_myAmmoInSpace.Any())
						{
							return _myAmmoInSpace;
						}

						return null;
					}

					return null;
				}

				return _myAmmoInSpace;
			}
		}

		public IEnumerable<EntityCache> Containers
		{
			get
			{
				try
				{
					return _containers ?? (_containers = Cache.Instance.EntitiesOnGrid.Where(e =>
					                                                                         e.IsContainer &&
					                                                                         e.HaveLootRights &&
					                                                                         //(e.GroupId == (int)Group.Wreck && !e.IsWreckEmpty) &&
					                                                                         (e.Name != "Abandoned Container")).ToList());
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Containers", "Exception [" + exception + "]", Logging.Debug);
					return new List<EntityCache>();
				}
			}
		}

		public IEnumerable<EntityCache> ContainersIgnoringLootRights
		{
			get
			{
				return _containers ?? (_containers = Cache.Instance.EntitiesOnGrid.Where(e =>
				                                                                         e.IsContainer &&
				                                                                         //(e.GroupId == (int)Group.Wreck && !e.IsWreckEmpty) &&
				                                                                         (e.Name != "Abandoned Container")).ToList());
			}
		}

		private IEnumerable<EntityCache> _wrecks;
		
		public IEnumerable<EntityCache> Wrecks
		{
			get { return _wrecks ?? (_wrecks = Cache.Instance.EntitiesOnGrid.Where(e => (e.GroupId == (int)Group.Wreck)).ToList()); }
		}

		public IEnumerable<EntityCache> UnlootedContainers
		{
			get
			{
				return _unlootedContainers ?? (_unlootedContainers = Cache.Instance.EntitiesOnGrid.Where(e =>
				                                                                                         e.IsContainer &&
				                                                                                         e.HaveLootRights &&
				                                                                                         (!LootedContainers.Contains(e.Id))).OrderBy(
				                               	e => e.Distance).
				                               ToList());
			}
		}

		
		public IEnumerable<EntityCache> UnlootedWrecksAndSecureCans
		{
			get
			{
				return _unlootedWrecksAndSecureCans ?? (_unlootedWrecksAndSecureCans = Cache.Instance.EntitiesOnGrid.Where(e =>
				                                                                                                           (e.GroupId == (int)Group.Wreck || e.GroupId == (int)Group.SecureContainer ||
				                                                                                                            e.GroupId == (int)Group.AuditLogSecureContainer ||
				                                                                                                            e.GroupId == (int)Group.FreightContainer)).OrderBy(e => e.Distance).
				                                        ToList());
			}
		}

		public IEnumerable<EntityCache> _TotalTargetsandTargeting;

		public IEnumerable<EntityCache> TotalTargetsandTargeting
		{
			get
			{
				if (_TotalTargetsandTargeting == null)
				{
					_TotalTargetsandTargeting = Cache.Instance.Targets.Concat(Cache.Instance.Targeting.Where(i => !i.IsTarget));
					return _TotalTargetsandTargeting;
				}

				return _TotalTargetsandTargeting;
			}
		}

		public int TotalTargetsandTargetingCount
		{
			get
			{
				if (!TotalTargetsandTargeting.Any())
				{
					return 0;
				}

				return TotalTargetsandTargeting.Count();
			}
		}

		public int TargetingSlotsNotBeingUsedBySalvager
		{
			get
			{
				if (Salvage.MaximumWreckTargets > 0 && Cache.Instance.MaxLockedTargets >= 5)
				{
					return Cache.Instance.MaxLockedTargets - Salvage.MaximumWreckTargets;
				}

				return Cache.Instance.MaxLockedTargets;
			}
		}
		
		public IEnumerable<EntityCache> Targets
		{
			get
			{
				if (_targets == null)
				{
					_targets = Cache.Instance.EntitiesOnGrid.Where(e => e.IsTarget).ToList();
				}
				
				// Remove the target info from the TargetingIDs Queue (its been targeted)
				foreach (EntityCache target in _targets.Where(t => TargetingIDs.ContainsKey(t.Id)))
				{
					TargetingIDs.Remove(target.Id);
				}

				return _targets;
			}
		}

		public IEnumerable<EntityCache> Targeting
		{
			get
			{
				if (_targeting == null)
				{
					_targeting = Cache.Instance.EntitiesOnGrid.Where(e => e.IsTargeting || Cache.Instance.TargetingIDs.ContainsKey(e.Id)).ToList();
				}

				if (_targeting.Any())
				{
					return _targeting;
				}

				return new List<EntityCache>();
			}
		}

		public List<long> IDsinInventoryTree
		{
			get
			{
				Logging.Log("Cache.IDsinInventoryTree", "Refreshing IDs from inventory tree, it has been longer than 30 seconds since the last refresh", Logging.Teal);
				return _IDsinInventoryTree ?? (_IDsinInventoryTree = Cache.Instance.PrimaryInventoryWindow.GetIdsFromTree(false));
			}
		}


		private List<EntityCache> _entitiesOnGrid;

		public IEnumerable<EntityCache> EntitiesOnGrid
		{
			get
			{
				try
				{
					if (_entitiesOnGrid == null)
					{
						_entitiesOnGrid = Cache.Instance.Entities.Where(e => e.IsOnGridWithMe).ToList();
						return _entitiesOnGrid;
					}

					return _entitiesOnGrid;
				}
				catch (NullReferenceException) { }  // this can happen during session changes

				return new List<EntityCache>();
			}
		}

		
		private List<EntityCache> _entities;

		public IEnumerable<EntityCache> Entities
		{
			get
			{
				try
				{
					if (_entities == null)
					{
						_entities = Cache.Instance.DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId != (int)CategoryID.Charge).Select(i => new EntityCache(i)).ToList();
						return _entities;
					}

					return _entities;
				}
				catch (NullReferenceException) { }  // this can happen during session changes

				return new List<EntityCache>();
			}
		}


		private List<EntityCache> _chargeEntities;

		public IEnumerable<EntityCache> ChargeEntities
		{
			get
			{
				try
				{
					if (_chargeEntities == null)
					{
						_chargeEntities = Cache.Instance.DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId == (int)CategoryID.Charge).Select(i => new EntityCache(i)).ToList();
						return _chargeEntities;
					}

					return _chargeEntities;
				}
				catch (NullReferenceException) { }  // this can happen during session changes

				return new List<EntityCache>();
			}
		}

		public Dictionary<long, string> EntityNames = new Dictionary<long, string>();
		public Dictionary<long, int> EntityTypeID = new Dictionary<long, int>();
		public Dictionary<long, int> EntityGroupID = new Dictionary<long, int>();
		public Dictionary<long, long> EntityBounty = new Dictionary<long, long>();
		public Dictionary<long, bool> EntityIsFrigate = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsNPCFrigate = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsCruiser = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsNPCCruiser = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsBattleCruiser = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsNPCBattleCruiser = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsBattleShip = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsNPCBattleShip = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsHighValueTarget = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsLowValueTarget = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsLargeCollidable = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsMiscJunk = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsBadIdea = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsFactionWarfareNPC = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsNPCByGroupID = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsEntutyIShouldLeaveAlone = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsSentry = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityHaveLootRights = new Dictionary<long, bool>();
		public Dictionary<long, bool> EntityIsStargate = new Dictionary<long, bool>();

		private IEnumerable<EntityCache> _entitiesActivelyBeingLocked;
		public IEnumerable<EntityCache> EntitiesActivelyBeingLocked
		{
			get
			{
				if (!InSpace)
				{
					return new List<EntityCache>();
				}

				if (Cache.Instance.EntitiesOnGrid.Any())
				{
					if (_entitiesActivelyBeingLocked == null)
					{
						_entitiesActivelyBeingLocked = Cache.Instance.EntitiesOnGrid.Where(i => i.IsTargeting).ToList();
						if (_entitiesActivelyBeingLocked.Any())
						{
							return _entitiesActivelyBeingLocked;
						}

						return new List<EntityCache>();
					}

					return _entitiesActivelyBeingLocked;
				}
				
				return new List<EntityCache>();
			}
		}

		private List<EntityCache> _entitiesNotSelf;

		public IEnumerable<EntityCache> EntitiesNotSelf
		{
			get
			{
				if (_entitiesNotSelf == null)
				{
					_entitiesNotSelf = Cache.Instance.EntitiesOnGrid.Where(i => i.CategoryId != (int)CategoryID.Asteroid && i.Id != Cache.Instance.ActiveShip.ItemId).ToList();
					if (_entitiesNotSelf.Any())
					{
						return _entitiesNotSelf;
					}

					return new List<EntityCache>();
				}

				return _entitiesNotSelf;
			}
		}

		private EntityCache _myShipEntity;
		public EntityCache MyShipEntity
		{
			get
			{
				if (_myShipEntity == null)
				{
					if (!Cache.Instance.InSpace)
					{
						return null;
					}

					_myShipEntity = Cache.Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == Cache.Instance.ActiveShip.ItemId);
					return _myShipEntity;
				}

				return _myShipEntity;
			}
		}

		public bool InSpace
		{
			get
			{
				try
				{
					if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(7))
					{
						return false;
					}

					if (DateTime.UtcNow < Time.Instance.LastInSpace.AddMilliseconds(800))
					{
						//if We already set the LastInStation timestamp this iteration we do not need to check if we are in station
						return true;
					}
					
					if (DirectEve.Session.IsInSpace)
					{
						if (!Cache.Instance.InStation)
						{
							if (Cache.Instance.DirectEve.ActiveShip.Entity != null)
							{
								if (DirectEve.Session.IsReady)
								{
									if (Cache.Instance.Entities.Any())
									{
										Time.Instance.LastInSpace = DateTime.UtcNow;
										return true;
									}
								}
								
								if (Logging.DebugInSpace) Logging.Log("InSpace", "Session is Not Ready", Logging.Debug);
								return false;
							}
							
							if (Logging.DebugInSpace) Logging.Log("InSpace", "Cache.Instance.DirectEve.ActiveShip.Entity is null", Logging.Debug);
							return false;
						}

						if (Logging.DebugInSpace) Logging.Log("InSpace", "NOT InStation is False", Logging.Debug);
						return false;
					}

					if (Logging.DebugInSpace) Logging.Log("InSpace", "InSpace is False", Logging.Debug);
					return false;
				}
				catch (Exception ex)
				{
					if (Logging.DebugExceptions) Logging.Log("Cache.InSpace", "if (DirectEve.Session.IsInSpace && !DirectEve.Session.IsInStation && DirectEve.Session.IsReady && Cache.Instance.ActiveShip.Entity != null) <---must have failed exception was [" + ex.Message + "]", Logging.Teal);
					return false;
				}
			}
		}

		public bool InStation
		{
			get
			{
				try
				{
					if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(7))
					{
						return false;
					}

					if (DateTime.UtcNow < Time.Instance.LastInStation.AddMilliseconds(800))
					{
						//if We already set the LastInStation timestamp this iteration we do not need to check if we are in station
						return true;
					}

					if (DirectEve.Session.IsInStation && !DirectEve.Session.IsInSpace && DirectEve.Session.IsReady)
					{
						if (!Cache.Instance.Entities.Any())
						{
							Time.Instance.LastInStation = DateTime.UtcNow;
							return true;
						}
					}

					return false;
				}
				catch (Exception ex)
				{
					if (Logging.DebugExceptions) Logging.Log("Cache.InStation", "if (DirectEve.Session.IsInStation && !DirectEve.Session.IsInSpace && DirectEve.Session.IsReady) <---must have failed exception was [" + ex.Message + "]", Logging.Teal);
					return false;
				}
			}
		}

		public bool InWarp
		{
			get
			{
				try
				{
					if (Cache.Instance.InSpace && !Cache.Instance.InStation)
					{
						if (Cache.Instance.ActiveShip != null)
						{
							if (Cache.Instance.ActiveShip.Entity != null)
							{
								if (Cache.Instance.ActiveShip.Entity.Mode == 3)
								{
									if (Cache.Instance.Modules != null && Cache.Instance.Modules.Any())
									{
										Combat.ReloadAll(Cache.Instance.MyShipEntity, true);
									}
									
									Time.Instance.LastInWarp = DateTime.UtcNow;
									return true;
								}
								
								if (Logging.DebugInWarp && !Cache.Instance.Paused) Logging.Log("Cache.InWarp", "We are not in warp.Cache.Instance.ActiveShip.Entity.Mode  is [" + (int)Cache.Instance.MyShipEntity.Mode + "]", Logging.Teal);
								return false;
							}
							
							if (Logging.DebugInWarp && !Cache.Instance.Paused) Logging.Log("Cache.InWarp", "Why are we checking for InWarp if Cache.Instance.ActiveShip.Entity is Null? (session change?)", Logging.Teal);
							return false;
						}
						
						if (Logging.DebugInWarp && !Cache.Instance.Paused) Logging.Log("Cache.InWarp", "Why are we checking for InWarp if Cache.Instance.ActiveShip is Null? (session change?)", Logging.Teal);
						return false;
					}
					
					if (Logging.DebugInWarp && !Cache.Instance.Paused) Logging.Log("Cache.InWarp", "Why are we checking for InWarp while docked or between session changes?", Logging.Teal);
					return false;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.InWarp", "InWarp check failed, exception [" + exception + "]", Logging.Teal);
				}

				return false;
			}
		}

		public bool IsOrbiting(long EntityWeWantToBeOrbiting = 0)
		{
			try
			{
				if (Cache.Instance.Approaching != null)
				{
					bool _followIDIsOnGrid = false;

					if (EntityWeWantToBeOrbiting != 0)
					{
						_followIDIsOnGrid = (EntityWeWantToBeOrbiting == Cache.Instance.ActiveShip.Entity.FollowId);
					}
					else
					{
						_followIDIsOnGrid = Cache.Instance.EntitiesOnGrid.Any(i => i.Id == Cache.Instance.ActiveShip.Entity.FollowId);
					}

					if (Cache.Instance.ActiveShip.Entity != null && Cache.Instance.ActiveShip.Entity.Mode == 4 && _followIDIsOnGrid)
					{
						return true;
					}

					return false;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.IsApproaching", "Exception [" + exception + "]", Logging.Debug);
				return false;
			}
		}

		public bool IsApproaching(long EntityWeWantToBeApproaching = 0)
		{
			try
			{
				if (Cache.Instance.Approaching != null)
				{
					bool _followIDIsOnGrid = false;

					if (EntityWeWantToBeApproaching != 0)
					{
						_followIDIsOnGrid = (EntityWeWantToBeApproaching == Cache.Instance.ActiveShip.Entity.FollowId);
					}
					else
					{
						_followIDIsOnGrid = Cache.Instance.EntitiesOnGrid.Any(i => i.Id == Cache.Instance.ActiveShip.Entity.FollowId);
					}

					if (Cache.Instance.ActiveShip.Entity != null && Cache.Instance.ActiveShip.Entity.Mode == 1 && _followIDIsOnGrid)
					{
						return true;
					}

					return false;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.IsApproaching", "Exception [" + exception + "]", Logging.Debug);
				return false;
			}
		}

		public bool IsApproachingOrOrbiting(long EntityWeWantToBeApproachingOrOrbiting = 0)
		{
			try
			{
				if (IsApproaching(EntityWeWantToBeApproachingOrOrbiting))
				{
					return true;
				}

				if (IsOrbiting(EntityWeWantToBeApproachingOrOrbiting))
				{
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.IsApproachingOrOrbiting", "Exception [" + exception + "]", Logging.Debug);
				return false;
			}
		}

		public List<EntityCache> Stations
		{
			get
			{
				try
				{
					if (_stations == null)
					{
						if (Cache.Instance.Entities.Any())
						{
							_stations = Cache.Instance.Entities.Where(e => e.CategoryId == (int)CategoryID.Station).OrderBy(i => i.Distance).ToList();
							if (_stations.Any())
							{
								return _stations;
							}

							return new List<EntityCache>();
						}

						return null;
					}

					return _stations;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.SolarSystems", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}

		public EntityCache ClosestStation
		{
			get
			{
				try
				{
					if (Stations != null && Stations.Any())
					{
						return Stations.OrderBy(s => s.Distance).FirstOrDefault() ?? Cache.Instance.Entities.OrderByDescending(s => s.Distance).FirstOrDefault();
					}

					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.IsApproaching", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}

		public EntityCache StationByName(string stationName)
		{
			EntityCache station = Stations.First(x => x.Name.ToLower() == stationName.ToLower());
			return station;
		}


		public IEnumerable<DirectSolarSystem> _solarSystems;
		public IEnumerable<DirectSolarSystem> SolarSystems
		{
			get
			{
				try
				{
					//High sec: 1090
					//Low sec: 817
					//0.0: 3524 (of which 230 are not connected)
					//W-space: 2499

					//High sec + Low sec = Empire: 1907
					//Empire + 0.0 = K-space: 5431
					//K-space + W-space = Total: 7930
					if (Time.Instance.LastSessionChange.AddSeconds(30) > DateTime.UtcNow && (Cache.Instance.InSpace || Cache.Instance.InStation))
					{
						if (_solarSystems == null || !_solarSystems.Any() || _solarSystems.Count() < 5400)
						{
							if (Cache.Instance.DirectEve.SolarSystems.Any())
							{
								if (Cache.Instance.DirectEve.SolarSystems.Values.Any())
								{
									_solarSystems = Cache.Instance.DirectEve.SolarSystems.Values.OrderBy(s => s.Name).ToList();
								}

								return null;
							}
							
							return null;
						}

						return _solarSystems;
					}

					return null;
				}
				catch (NullReferenceException) // Not sure why this happens, but seems to be no problem
				{
					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.SolarSystems", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}

		public IEnumerable<EntityCache> JumpBridges
		{
			get { return _jumpBridges ?? (_jumpBridges = Cache.Instance.Entities.Where(e => e.GroupId == (int)Group.JumpBridge).ToList()); }
		}

		public List<EntityCache> Stargates
		{
			get
			{
				try
				{
					if (_stargates == null)
					{
						if (Cache.Instance.Entities != null && Cache.Instance.Entities.Any())
						{
							//if (Cache.Instance.EntityIsStargate.Any())
							//{
							//    if (_stargates != null && _stargates.Any()) _stargates.Clear();
							//    if (_stargates == null) _stargates = new List<EntityCache>();
							//    foreach (KeyValuePair<long, bool> __stargate in Cache.Instance.EntityIsStargate)
							//    {
							//        _stargates.Add(Cache.Instance.Entities.FirstOrDefault(i => i.Id == __stargate.Key));
							//    }
							//
							//    if (_stargates.Any()) return _stargates;
							//}

							_stargates = Cache.Instance.Entities.Where(e => e.GroupId == (int)Group.Stargate).ToList();
							//foreach (EntityCache __stargate in _stargates)
							//{
							//    if (Cache.Instance.EntityIsStargate.Any())
							//    {
							//        if (!Cache.Instance.EntityIsStargate.ContainsKey(__stargate.Id))
							//        {
							//            Cache.Instance.EntityIsStargate.Add(__stargate.Id, true);
							//            continue;
							//        }
							//
							//        continue;
							//    }
							//
							//    Cache.Instance.EntityIsStargate.Add(__stargate.Id, true);
							//    continue;
							//}

							return _stargates;
						}

						return null;
					}

					return _stargates;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Stargates", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}

		public EntityCache ClosestStargate
		{
			get
			{
				try
				{
					if (Cache.Instance.InSpace)
					{
						if (Cache.Instance.Entities != null && Cache.Instance.Entities.Any())
						{
							if (Cache.Instance.Stargates != null && Cache.Instance.Stargates.Any())
							{
								return Cache.Instance.Stargates.OrderBy(s => s.Distance).FirstOrDefault() ?? null;
							}

							return null;
						}

						return null;
					}

					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.ClosestStargate", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}

		public EntityCache StargateByName(string locationName)
		{
			{
				return _stargate ?? (_stargate = Cache.Instance.EntitiesByName(locationName, Cache.Instance.Entities.Where(i => i.GroupId == (int)Group.Stargate)).FirstOrDefault(e => e.GroupId == (int)Group.Stargate));
			}
		}

		public IEnumerable<EntityCache> BigObjects
		{
			get
			{
				try
				{
					return _bigObjects ?? (_bigObjects = Cache.Instance.EntitiesOnGrid.Where(e =>
					                                                                         e.Distance < (double)Distances.OnGridWithMe &&
					                                                                         (e.IsLargeCollidable || e.CategoryId == (int)CategoryID.Asteroid || e.GroupId == (int)Group.SpawnContainer)
					                                                                        ).OrderBy(t => t.Distance).ToList());
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.BigObjects", "Exception [" + exception + "]", Logging.Debug);
					return new List<EntityCache>();
				}
			}
		}

		public IEnumerable<EntityCache> AccelerationGates
		{
			get
			{
				return _gates ?? (_gates = Cache.Instance.EntitiesOnGrid.Where(e =>
				                                                               e.Distance < (double)Distances.OnGridWithMe &&
				                                                               e.GroupId == (int)Group.AccelerationGate &&
				                                                               e.Distance < (double)Distances.OnGridWithMe).OrderBy(t => t.Distance).ToList());
			}
		}

		public IEnumerable<EntityCache> BigObjectsandGates
		{
			get
			{
				return _bigObjectsAndGates ?? (_bigObjectsAndGates = Cache.Instance.EntitiesOnGrid.Where(e =>
				                                                                                         (e.IsLargeCollidable || e.CategoryId == (int)CategoryID.Asteroid || e.GroupId == (int)Group.AccelerationGate || e.GroupId == (int)Group.SpawnContainer)
				                                                                                         && e.Distance < (double)Distances.DirectionalScannerCloseRange).OrderBy(t => t.Distance).ToList());
			}
		}

		public IEnumerable<EntityCache> Objects
		{
			get
			{
				return _objects ?? (_objects = Cache.Instance.EntitiesOnGrid.Where(e =>
				                                                                   !e.IsPlayer &&
				                                                                   e.GroupId != (int)Group.SpawnContainer &&
				                                                                   e.GroupId != (int)Group.Wreck &&
				                                                                   e.Distance < 200000).OrderBy(t => t.Distance).ToList());
			}
		}

		public EntityCache Star
		{
			get { return _star ?? (_star = Entities.FirstOrDefault(e => e.CategoryId == (int)CategoryID.Celestial && e.GroupId == (int)Group.Star)); }
		}
		
		public EntityCache Approaching
		{
			get
			{
				try
				{
					if (_approaching == null)
					{
						DirectEntity ship = Cache.Instance.ActiveShip.Entity;
						if (ship != null && ship.IsValid && !ship.HasExploded && !ship.HasReleased)
						{
							if (ship.FollowId != 0)
							{
								_approaching = EntityById(ship.FollowId);
								if (_approaching != null && _approaching.IsValid)
								{
									return _approaching;
								}

								return null;
							}

							return null;
						}

						return null;
					}
					
					return _approaching;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Approaching", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
			set
			{
				_approaching = value;
			}
		}
		
		
		public bool GateInGrid()
		{
			try
			{
				if (Cache.Instance.AccelerationGates.FirstOrDefault() == null || !Cache.Instance.AccelerationGates.Any())
				{
					return false;
				}

				Time.Instance.LastAccelerationGateDetected = DateTime.UtcNow;
				return true;
			}
			catch (Exception ex)
			{
				Logging.Log("GateInGrid", "Exception [" + ex + "]", Logging.Debug);
				return true;
			}
		}
	}
}
