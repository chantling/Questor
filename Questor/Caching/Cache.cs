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
		
		private static Cache _instance = new Cache();
		
		public static Storyline storyline { get; set; }

		public static Cache Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new Cache();
				}
				return _instance;
			}
		}
		
		private DateTime LastEveAccountPoll = DateTime.MinValue;
		private EveAccount _EveAccount = null;
		public EveAccount EveAccount
		{
			get  {
				
				if(_EveAccount == null || LastEveAccountPoll.AddSeconds(1) < DateTime.UtcNow) {
					LastEveAccountPoll = DateTime.UtcNow;
					_EveAccount = WCFClient.Instance.GetPipeProxy.GetEveAccount(this.CharName);
					
				}
				
				return _EveAccount;

			}
		}
		
		public string CharName { get; set; }
		public string PipeName { get; set; }
		
		public WCFClient WCFClient {
			get {
				return WCFClient.Instance;
			}
		}
		
		public Cache()
		{
			LastModuleTargetIDs = new Dictionary<long, long>();
			TargetingIDs = new Dictionary<long, DateTime>();
			_entitiesById = new Dictionary<long, EntityCache>();
			
			LootedContainers = new HashSet<long>();
			Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
			
			Interlocked.Increment(ref CacheInstances);
		}

		~Cache()
		{
			Interlocked.Decrement(ref CacheInstances);
		}
		
		public void InvalidateCache()
		{
			try
			{
				Logging.InvalidateCache();
				Arm.InvalidateCache();
				Drones.InvalidateCache();
				Combat.InvalidateCache();
				Salvage.InvalidateCache();

				_ammoHangar = null;
				_lootHangar = null;
				_lootContainer = null;
				_fittedModules = null;
				

				//
				// this list of variables is cleared every pulse.
				//
				_agent = null;
				_allBookmarks = null;
				_approaching = null;
				_bigObjects = null;
				_bigObjectsAndGates = null;
				_chargeEntities = null;
				_currentShipsCargo = null;
				_containerInSpace = null;
				_containers = null;
				_entities = null;
				_entitiesNotSelf = null;
				_entitiesOnGrid = null;
				_entitiesById.Clear();
				_fittingManagerWindow = null;
				_gates = null;
				_IDsinInventoryTree = null;
				_itemHangar = null;
				_jumpBridges = null;
				_lpStore = null;
				_maxLockedTargets = null;
				_modules = null;
				_myAmmoInSpace = null;
				_myCurrentAmmoInWeapon = null;
				_myShipEntity = null;
				_objects = null;
				_safeSpotBookmarks = null;
				_shipHangar = null;
				_star = null;
				_stations = null;
				_stargate = null;
				_stargates = null;
				_targets = null;
				_targeting = null;
				_TotalTargetsandTargeting = null;
				_unlootedContainers = null;
				_unlootedWrecksAndSecureCans = null;
				_weapons = null;
				_windows = null;
				_wrecks = null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.InvalidateCache", "Exception [" + exception + "]", Logging.Debug);
			}
		}
		

		private DirectAgent _agent;
		public long CurrentStorylineAgentId { get; set; }
		public List<long> AgentBlacklist;
		private EntityCache _approaching;
		private List<EntityCache> _gates;
		private List<EntityCache> _bigObjectsAndGates;
		private List<EntityCache> _objects;
		private List<DirectBookmark> _safeSpotBookmarks;
		private readonly Dictionary<long, EntityCache> _entitiesById;
		private List<ModuleCache> _modules;
		public string OrbitEntityNamed;
		public DirectLocation MissionSolarSystem;
		public string DungeonId;
		private EntityCache _star;
		private List<EntityCache> _stations;
		private List<EntityCache> _stargates;
		private List<EntityCache> _containers;
		private List<EntityCache> _bigObjects;
		private EntityCache _stargate;
		private IEnumerable<EntityCache> _jumpBridges;
		private List<EntityCache> _targeting;
		private List<EntityCache> _targets;
		public List<long> _IDsinInventoryTree;
		private List<EntityCache> _unlootedContainers;
		private List<EntityCache> _unlootedWrecksAndSecureCans;
		private List<DirectWindow> _windows;
		private int? _maxLockedTargets;

		public HashSet<long> ListOfWarpScramblingEntities = new HashSet<long>();
		public HashSet<long> ListOfJammingEntities = new HashSet<long>();
		public HashSet<long> ListOfTrackingDisruptingEntities = new HashSet<long>();
		public HashSet<long> ListNeutralizingEntities = new HashSet<long>();
		public HashSet<long> ListOfTargetPaintingEntities = new HashSet<long>();
		public HashSet<long> ListOfDampenuingEntities = new HashSet<long>();
		public HashSet<long> ListofWebbingEntities = new HashSet<long>();
		public HashSet<long> ListofContainersToLoot = new HashSet<long>();
		public HashSet<string> ListofMissionCompletionItemsToLoot = new HashSet<string>();
		public long VolleyCount;
		public void IterateShipTargetValues(string module)
		{
			string path = Logging.PathToCurrentDirectory;

			if (path != null)
			{
				string ShipTargetValuesXmlFile = System.IO.Path.Combine(path, "ShipTargetValues.xml");
				ShipTargetValues = new List<ShipTargetValue>();

				if (!File.Exists(ShipTargetValuesXmlFile))
				{
					Logging.Log(module, "IterateShipTargetValues - unable to find [" + ShipTargetValuesXmlFile + "]", Logging.White);
					return;
				}

				try
				{
					Logging.Log(module, "IterateShipTargetValues - Loading [" + ShipTargetValuesXmlFile + "]", Logging.White);
					XDocument values = XDocument.Load(ShipTargetValuesXmlFile);
					if (values.Root != null)
					{
						foreach (XElement value in values.Root.Elements("ship"))
						{
							ShipTargetValues.Add(new ShipTargetValue(value));
						}
					}
				}
				catch (Exception exception)
				{
					Logging.Log(module, "IterateShipTargetValues - Exception: [" + exception + "]", Logging.Red);
				}
			}
		}
		public void IterateUnloadLootTheseItemsAreLootItems(string module)
		{
			string path = Logging.PathToCurrentDirectory;

			if (path != null)
			{
				string UnloadLootTheseItemsAreLootItemsXmlFile = System.IO.Path.Combine(path, "UnloadLootTheseItemsAreLootItems.xml");
				UnloadLootTheseItemsAreLootById = new Dictionary<int, string>();

				if (!File.Exists(UnloadLootTheseItemsAreLootItemsXmlFile))
				{
					Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - unable to find [" + UnloadLootTheseItemsAreLootItemsXmlFile + "]", Logging.White);
					return;
				}

				try
				{
					Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - Loading [" + UnloadLootTheseItemsAreLootItemsXmlFile + "]", Logging.White);
					MissionSettings.UnloadLootTheseItemsAreLootItems = XDocument.Load(UnloadLootTheseItemsAreLootItemsXmlFile);

					if (MissionSettings.UnloadLootTheseItemsAreLootItems.Root != null)
					{
						foreach (XElement element in MissionSettings.UnloadLootTheseItemsAreLootItems.Root.Elements("invtype"))
						{
							UnloadLootTheseItemsAreLootById.Add((int)element.Attribute("id"), (string)element.Attribute("name"));
						}
					}
				}
				catch (Exception exception)
				{
					Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]", Logging.Red);
				}
			}
			else
			{
				Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - unable to find [" + Logging.PathToCurrentDirectory + "]", Logging.White);
			}
		}
		public static int CacheInstances;
		public HashSet<long> LootedContainers { get; private set; }
		public bool ExitWhenIdle;
		public bool StopBot;
		public static bool LootAlreadyUnloaded;
		public bool RouteIsAllHighSecBool;
		public double Wealth { get; set; }
		public double WealthatStartofPocket { get; set; }
		public int StackHangarAttempts { get; set; }
		public bool NormalApproach = true;
		public bool CourierMission;
		public bool doneUsingRepairWindow;
		public long AmmoHangarID = -99;
		public long LootHangarID = -99;
		public static D3DDetour.D3DVersion D3DVersion { get; set; }
		public static Random _random = new Random();
		public static int GetRandom(int minValue, int maxValue)
		{
			return _random.Next(minValue, maxValue);
		}
		public DirectAgentMission GetAgentMission(long agentId, bool ForceUpdate)
		{
			if (DateTime.UtcNow < Time.Instance.NextGetAgentMissionAction)
			{
				if (MissionSettings.FirstAgentMission != null)
				{
					return MissionSettings.FirstAgentMission;
				}

				return null;
			}

			try
			{
				if (ForceUpdate || MissionSettings.myAgentMissionList == null || !MissionSettings.myAgentMissionList.Any())
				{
					MissionSettings.myAgentMissionList = DirectEve.AgentMissions.Where(m => m.AgentId == agentId).ToList();
					Time.Instance.NextGetAgentMissionAction = DateTime.UtcNow.AddSeconds(5);
				}

				if (MissionSettings.myAgentMissionList.Any())
				{
					MissionSettings.FirstAgentMission = MissionSettings.myAgentMissionList.FirstOrDefault();
					return MissionSettings.FirstAgentMission;
				}

				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.Instance.GetAgentMission", "DirectEve.AgentMissions failed: [" + exception + "]", Logging.Teal);
				return null;
			}
		}
		public bool InMission { get; set; }
		public bool normalNav = true;  
		public bool onlyKillAggro { get; set; }
		public int StackLoothangarAttempts { get; set; }
		public int StackAmmohangarAttempts { get; set; }
		public int StackItemhangarAttempts { get; set; }
		
		public string Path;

		public bool _isCorpInWar = false;
		
		public bool IsCorpInWar
		{
			get
			{
				if (DateTime.UtcNow > Time.Instance.NextCheckCorpisAtWar)
				{
					bool war = DirectEve.Me.IsAtWar;
					Cache.Instance._isCorpInWar = war;

					Time.Instance.NextCheckCorpisAtWar = DateTime.UtcNow.AddMinutes(15);
					if (!_isCorpInWar)
					{
						if (Logging.DebugWatchForActiveWars) Logging.Log("IsCorpInWar", "Your corp is not involved in any wars (yet)", Logging.Green);
					}
					else
					{
						if (Logging.DebugWatchForActiveWars) Logging.Log("IsCorpInWar", "Your corp is involved in a war, be careful", Logging.Orange);
					}

					return _isCorpInWar;
				}
				
				return _isCorpInWar;
			}
		}

		public bool LocalSafe(int maxBad, double stand)
		{
			int number = 0;
			DirectChatWindow local = (DirectChatWindow)GetWindowByName("Local");

			try
			{
				foreach (DirectCharacter localMember in local.Members)
				{
					float[] alliance = { DirectEve.Standings.GetPersonalRelationship(localMember.AllianceId), DirectEve.Standings.GetCorporationRelationship(localMember.AllianceId), DirectEve.Standings.GetAllianceRelationship(localMember.AllianceId) };
					float[] corporation = { DirectEve.Standings.GetPersonalRelationship(localMember.CorporationId), DirectEve.Standings.GetCorporationRelationship(localMember.CorporationId), DirectEve.Standings.GetAllianceRelationship(localMember.CorporationId) };
					float[] personal = { DirectEve.Standings.GetPersonalRelationship(localMember.CharacterId), DirectEve.Standings.GetCorporationRelationship(localMember.CharacterId), DirectEve.Standings.GetAllianceRelationship(localMember.CharacterId) };

					if (alliance.Min() <= stand || corporation.Min() <= stand || personal.Min() <= stand)
					{
						Logging.Log("Cache.LocalSafe", "Bad Standing Pilot Detected: [ " + localMember.Name + "] " + " [ " + number + " ] so far... of [ " + maxBad + " ] allowed", Logging.Orange);
						number++;
					}

					if (number > maxBad)
					{
						Logging.Log("Cache.LocalSafe", "[" + number + "] Bad Standing pilots in local, We should stay in station", Logging.Orange);
						return false;
					}
				}
			}
			catch (Exception exception)
			{
				Logging.Log("LocalSafe", "Exception [" + exception + "]", Logging.Debug);
			}
			
			return true;
		}

		public DirectEve DirectEve { get; set; }


		public Dictionary<int, String> UnloadLootTheseItemsAreLootById { get; private set; }

		public List<ShipTargetValue> ShipTargetValues { get; private set; }


		public DamageType FrigateDamageType { get; set; }


		public DamageType CruiserDamageType { get; set; }

		public DamageType BattleCruiserDamageType { get; set; }


		public DamageType BattleShipDamageType { get; set; }

		public DamageType LargeColidableDamageType { get; set; }

		public bool AfterMissionSalvaging { get; set; }

		private DirectContainer _currentShipsCargo;

		public DirectContainer CurrentShipsCargo
		{
			get
			{
				try
				{
					if ((Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10)) || (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10)))
					{
						if (_currentShipsCargo == null)
						{
							_currentShipsCargo = Cache.Instance.DirectEve.GetShipsCargo();
							if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "_currentShipsCargo is null", Logging.Debug);
						}

						if (Cache.Instance.Windows.All(i => i.Type != "form.ActiveShipCargo")) // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
						{
							if (DateTime.UtcNow > Time.Instance.NextOpenCurrentShipsCargoWindowAction)
							{
								Statistics.LogWindowActionToWindowLog("CargoHold", "Opening CargoHold");
								if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);", Logging.Debug);
								Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
								Time.Instance.NextOpenCurrentShipsCargoWindowAction = DateTime.UtcNow.AddMilliseconds(1000 + Cache.Instance.RandomNumber(0, 2000));
							}

							if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "Waiting on NextOpenCurrentShipsCargoWindowAction [" + DateTime.UtcNow.Subtract(Time.Instance.NextOpenCurrentShipsCargoWindowAction).TotalSeconds + "sec]", Logging.Debug);
						}
						
						return _currentShipsCargo;
					}

					int EntityCount = 0;
					if (Cache.Instance.Entities.Any())
					{
						EntityCount = Cache.Instance.Entities.Count();
					}

					if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "Cache.Instance.MyShipEntity is null: We have a total of [" + EntityCount + "] entities available at the moment.", Logging.Debug);
					return null;
					
				}
				catch (Exception exception)
				{
					Logging.Log("CurrentShipsCargo", "Unable to complete ReadyCargoHold [" + exception + "]", Logging.Teal);
					return null;
				}
			}
		}

		public DirectContainer _containerInSpace { get; set; }

		public DirectContainer ContainerInSpace
		{
			get
			{
				if (_containerInSpace == null)
				{
					return null;
				}

				return _containerInSpace;
			}

			set { _containerInSpace = value; }
		}

		public DirectActiveShip ActiveShip
		{
			get
			{
				return Cache.Instance.DirectEve.ActiveShip;
			}
		}

		public int WeaponRange
		{
			get
			{
				// Get ammo based on current damage type
				IEnumerable<Ammo> ammo = Combat.Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList();

				try
				{
					// Is our ship's cargo available?
					if (Cache.Instance.CurrentShipsCargo != null)
					{
						ammo = ammo.Where(a => Cache.Instance.CurrentShipsCargo.Items.Any(i => a.TypeId == i.TypeId && i.Quantity >= Combat.MinimumAmmoCharges));
					}
					else
					{
						return System.Convert.ToInt32(Combat.MaxTargetRange);
					}

					// Return ship range if there's no ammo left
					if (!ammo.Any())
					{
						return System.Convert.ToInt32(Combat.MaxTargetRange);
					}

					return ammo.Max(a => a.Range);
				}
				catch (Exception ex)
				{
					if (Logging.DebugExceptions) Logging.Log("Cache.WeaponRange", "exception was:" + ex.Message, Logging.Teal);

					// Return max range
					if (Cache.Instance.ActiveShip != null)
					{
						return System.Convert.ToInt32(Combat.MaxTargetRange);
					}

					return 0;
				}
			}
		}

		private DirectItem _myCurrentAmmoInWeapon;
		public DirectItem myCurrentAmmoInWeapon
		{
			get
			{
				try
				{
					if (_myCurrentAmmoInWeapon == null)
					{
						if (Cache.Instance.Weapons != null && Cache.Instance.Weapons.Any())
						{
							ModuleCache WeaponToCheckForAmmo = Cache.Instance.Weapons.FirstOrDefault();
							if (WeaponToCheckForAmmo != null)
							{
								_myCurrentAmmoInWeapon = WeaponToCheckForAmmo.Charge;
								return _myCurrentAmmoInWeapon;
							}

							return null;
						}

						return null;
					}

					return _myCurrentAmmoInWeapon;
				}
				catch (Exception ex)
				{
					if (Logging.DebugExceptions) Logging.Log("Cache.myCurrentAmmoInWeapon", "exception was:" + ex.Message, Logging.Teal);
					return null;
				}
			}
		}


		public Dictionary<long, long> LastModuleTargetIDs { get; private set; }

		public Dictionary<long, DateTime> TargetingIDs { get; private set; }

		public bool AllAgentsStillInDeclineCoolDown { get; set; }

		private string _currentAgent { get; set; }

		public bool Paused { get; set; }

		public long TotalMegaBytesOfMemoryUsed = 0;
		public double MyWalletBalance { get; set; }

		public bool UpdateMyWalletBalance()
		{
			//we know we are connected here
			Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
			Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
			return true;
		}

		public string CurrentPocketAction { get; set; }
		public float AgentEffectiveStandingtoMe;
		public string AgentEffectiveStandingtoMeText;
		public float AgentCorpEffectiveStandingtoMe;
		public float AgentFactionEffectiveStandingtoMe;
		public float StandingUsedToAccessAgent;
		public bool MissionBookmarkTimerSet;
		public long AgentStationID { get; set; }
		public string AgentStationName;
		public long AgentSolarSystemID;
		public string CurrentAgent
		{
			get
			{
				try
				{
					if (Settings.Instance.CharacterXMLExists)
					{
						if (string.IsNullOrEmpty(_currentAgent))
						{
							try
							{
								

								// TODO: to use this, Unload & Arm needs to be modified to NOT arm/unload/load on the storyline agent station
								//if(storyline != null && storyline.HasStoryline()) {
								//	var agentId = storyline.StorylineMission.AgentId;
								
								//	if(agentId > 0) {
								//		var agent = Cache.Instance.DirectEve.GetAgentById(agentId);
								//		if(agent != null) {
								//			Logging.Log("CurrentAgent","We got an open storyline mission, going to that agent.", Logging.White);
								//			_currentAgent = agent.Name;
								//			return agent.Name;
								//		}
								//	}
								
								//}
								
								if (MissionSettings.ListOfAgents != null && MissionSettings.ListOfAgents.Count() >= 1 && (!string.IsNullOrEmpty(SwitchAgent())))
								{
									_currentAgent = SwitchAgent();
									
								} else
								{
									
									if(MissionSettings.ListOfAgents != null && MissionSettings.ListOfAgents.Count() >= 1) {
										
										_currentAgent = SelectFirstAgent(true);
										
									} else
									{
										Logging.Log("Cache.CurrentAgent", "MissionSettings.ListOfAgents == null || MissionSettings.ListOfAgents.Count() < 1", Logging.White);
									}
								}
								
								Logging.Log("Cache.CurrentAgent", "[ " + _currentAgent + " ] AgentID [ " + Agent.AgentId + " ]", Logging.White);
								
							}
							catch (Exception ex)
							{
								Logging.Log("Cache.AgentId", "Exception [" + ex + "]", Logging.Debug);
								return string.Empty;
							}
						}

						return _currentAgent;
					}

					return string.Empty;
				}
				catch (Exception ex)
				{
					Logging.Log("SelectNearestAgent", "Exception [" + ex + "]", Logging.Debug);
					return "";
				}
			}
			set
			{
				try
				{
					_currentAgent = value;
				}
				catch (Exception ex)
				{
					Logging.Log("SelectNearestAgent", "Exception [" + ex + "]", Logging.Debug);
				}
			}
		}
		private static readonly Func<DirectAgent, DirectSession, bool> AgentInThisSolarSystemSelector = (a, s) => a.SolarSystemId == s.SolarSystemId;
		private static readonly Func<DirectAgent, DirectSession, bool> AgentInThisStationSelector = (a, s) => a.StationId == s.StationId;

		private string SelectNearestAgent(bool requireValidDeclineTimer )
		{
			string agentName = null;

			try
			{
				DirectAgentMission mission = null;

				if (!MissionSettings.ListOfAgents.Any()) return string.Empty;
				
				// first we try to find if we accepted a mission (not important) given by an agent in settings agents list
				foreach (AgentsList potentialAgent in MissionSettings.ListOfAgents)
				{
					if (Cache.Instance.DirectEve.AgentMissions.Any(m => m.State == (int)MissionState.Accepted && !m.Important && DirectEve.GetAgentById(m.AgentId).Name == potentialAgent.Name))
					{
						mission = Cache.Instance.DirectEve.AgentMissions.FirstOrDefault(m => m.State == (int)MissionState.Accepted && !m.Important && DirectEve.GetAgentById(m.AgentId).Name == potentialAgent.Name);

						// break on first accepted (not important) mission found
						break;
					}
				}

				if (mission != null)
				{
					agentName = DirectEve.GetAgentById(mission.AgentId).Name;
				}
				// no accepted (not important) mission found, so we need to find the nearest agent in our settings agents list
				else if (Cache.Instance.DirectEve.Session.IsReady)
				{
					try
					{
						Func<DirectAgent, DirectSession, bool> selector = DirectEve.Session.IsInSpace ? AgentInThisSolarSystemSelector : AgentInThisStationSelector;
						var nearestAgent = MissionSettings.ListOfAgents
							.Where(x => !requireValidDeclineTimer || DateTime.UtcNow >= x.DeclineTimer)
							.OrderBy(x => x.Priorit)
							.Select(x => new { Agent = x, DirectAgent = DirectEve.GetAgentByName(x.Name) })
							.FirstOrDefault(x => selector(x.DirectAgent, DirectEve.Session));

						if (nearestAgent != null)
						{
							agentName = nearestAgent.Agent.Name;
						}
						else if (MissionSettings.ListOfAgents.OrderBy(j => j.Priorit).Any())
						{
							AgentsList __HighestPriorityAgentInList = MissionSettings.ListOfAgents
								.Where(x => !requireValidDeclineTimer || DateTime.UtcNow >= x.DeclineTimer)
								.OrderBy(x => x.Priorit)
								.FirstOrDefault();
							if (__HighestPriorityAgentInList != null)
							{
								agentName = __HighestPriorityAgentInList.Name;
							}
						}
					}
					catch (NullReferenceException) {}
				}
			}
			catch (Exception ex)
			{
				Logging.Log("SelectNearestAgent", "Exception [" + ex + "]", Logging.Debug);
			}

			return agentName ?? null;
		}

		public string SelectFirstAgent(bool returnFirstOneIfNoneFound = false)
		{
			try
			{
				
				AgentsList FirstAgent = MissionSettings.ListOfAgents.OrderBy(j => j.Priorit).FirstOrDefault();

				if (FirstAgent != null)
				{
					return FirstAgent.Name;
				}

				Logging.Log("SelectFirstAgent", "Unable to find the first agent, are your agents configured?", Logging.Debug);
				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.SelectFirstAgent", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		public string SwitchAgent()
		{
			try
			{
				string agentNameToSwitchTo = null;

//				if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.PrepareStorylineSwitchAgents)
//				{
//					//TODO: must be a better way to achieve this
//
//					var storyLineMissions = Cache.Instance.DirectEve.AgentMissions.Where(m => m.Important);
//					if(storyLineMissions.Any()) {
//
//					}
//
//					return string.Empty;
//				}
				
				if (string.IsNullOrEmpty(_currentAgent))
				{
					// it means that this is first switch for Questor, so we'll check missions, then station or system for agents.
					AllAgentsStillInDeclineCoolDown = false;
					if (!string.IsNullOrEmpty(SelectNearestAgent(true)))
					{
						agentNameToSwitchTo = SelectNearestAgent(true);
						return agentNameToSwitchTo;
					}

					if (!string.IsNullOrEmpty(SelectNearestAgent(false)))
					{
						agentNameToSwitchTo = SelectNearestAgent(false);
						return agentNameToSwitchTo;
					}

					return string.Empty;
				}
				
				// find agent by priority and with ok declineTimer
				AgentsList agentToUseByPriority = MissionSettings.ListOfAgents.OrderBy(j => j.Priorit).FirstOrDefault(i => DateTime.UtcNow >= i.DeclineTimer);

				if (agentToUseByPriority != null)
				{
					AllAgentsStillInDeclineCoolDown = false; //this literally means we DO have agents available (at least one agents decline timer has expired and is clear to use)
					return agentToUseByPriority.Name;
				}
				
				// Why try to find an agent at this point ?
				/*
                try
                {
                    agent = Settings.Instance.ListOfAgents.OrderBy(j => j.Priorit).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    Logging.Log("Cache.SwitchAgent", "Unable to process agent section of [" + Settings.Instance.CharacterSettingsPath + "] make sure you have a valid agent listed! Pausing so you can fix it. [" + ex.Message + "]", Logging.Debug);
                    Cache.Instance.Paused = true;
                }
				 */
				AllAgentsStillInDeclineCoolDown = true; //this literally means we have no agents available at the moment (decline timer likely)
				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.SwitchAgent", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		public DirectAgent Agent
		{
			get
			{
				try
				{
					if (Settings.Instance.CharacterXMLExists)
					{
						try
						{
							if (_agent == null)
							{
								Logging.Log("Cache.Agent","Trying to GetAgentByName", Logging.White);
								_agent = Cache.Instance.DirectEve.GetAgentByName(CurrentAgent);
							}

							if (_agent != null)
							{
//								Logging.Log("Cache: CurrentAgent", "Processing Agent Info...", Logging.White);
								Cache.Instance.AgentStationName = Cache.Instance.DirectEve.GetLocationName(Cache.Instance._agent.StationId);
								Cache.Instance.AgentStationID = Cache.Instance._agent.StationId;

								Cache.Instance.AgentSolarSystemID = Cache.Instance._agent.SolarSystemId;
//								Logging.Log("Cache: CurrentAgent", "AgentStationName [" + Cache.Instance.AgentStationName + "]", Logging.White);
//								Logging.Log("Cache: CurrentAgent", "AgentStationID [" + Cache.Instance.AgentStationID + "]", Logging.White);
//								Logging.Log("Cache: CurrentAgent", "AgentSolarSystemID [" + Cache.Instance.AgentSolarSystemID + "]", Logging.White);
								
								return _agent;
							} else {
								Logging.Log("Cache.Agent","_agent == null", Logging.White);
							}
						}
						catch (Exception ex)
						{
							Logging.Log("Cache.Agent", "Unable to process agent section of [" + Logging.CharacterSettingsPath + "] make sure you have a valid agent listed! Pausing so you can fix it. [" + ex.Message + "]", Logging.Debug);
							Cache.Instance.Paused = true;
						}
					}

					Logging.Log("Cache.Agent", "if (!Settings.Instance.CharacterXMLExists)", Logging.Debug);
					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Agent", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}
		
		public IEnumerable<ModuleCache> Modules
		{
			get
			{
				try
				{
					if (_modules == null || !_modules.Any())
					{
						_modules = Cache.Instance.DirectEve.Modules.Select(m => new ModuleCache(m)).ToList();
					}

					return _modules;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Modules", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}

		public DirectContainer _fittedModules;
		public DirectContainer FittedModules
		{
			get
			{
				try
				{
					if (_fittedModules == null)
					{
						_fittedModules = Cache.Instance.DirectEve.GetShipsModules();
					}

					return _fittedModules;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Modules", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
		}


		private IEnumerable<ModuleCache> _weapons;
		public IEnumerable<ModuleCache> Weapons
		{
			get
			{
				if (_weapons == null)
				{
					_weapons = Modules.Where(m => m.GroupId == Combat.WeaponGroupId).ToList(); // ||
					//m.GroupId == (int)Group.ProjectileWeapon ||
					//m.GroupId == (int)Group.EnergyWeapon ||
					//m.GroupId == (int)Group.HybridWeapon ||
					//m.GroupId == (int)Group.CruiseMissileLaunchers ||
					//m.GroupId == (int)Group.RocketLaunchers ||
					//m.GroupId == (int)Group.StandardMissileLaunchers ||
					//m.GroupId == (int)Group.TorpedoLaunchers ||
					//m.GroupId == (int)Group.AssaultMissilelaunchers ||
					//m.GroupId == (int)Group.HeavyMissilelaunchers ||
					//m.GroupId == (int)Group.DefenderMissilelaunchers);
					if (MissionSettings.MissionWeaponGroupId != 0)
					{
						_weapons = Modules.Where(m => m.GroupId == MissionSettings.MissionWeaponGroupId).ToList();
					}

					if (Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10))
					{
						if (!_weapons.Any())
						{
							int moduleNumber = 0;
							//Logging.Log("Cache.Weapons", "WeaponGroupID is defined as [" + Combat.WeaponGroupId + "] in your characters settings XML", Logging.Debug);
							foreach (ModuleCache _module in Cache.Instance.Modules)
							{
								moduleNumber++;
								//Logging.Log("Cache.Weapons", "[" + moduleNumber + "][" + _module.TypeName + "] typeID [" + _module.TypeId + "] groupID [" + _module.GroupId + "]", Logging.White);
							}
						}
						else
						{
							if (DateTime.UtcNow > Time.Instance.NextModuleDisableAutoReload)
							{
								//int weaponNumber = 0;
								foreach (ModuleCache _weapon in Cache.Instance.Weapons)
								{
									//weaponNumber++;
									if (_weapon.AutoReload)
									{
										bool returnValueHereNotUsed = _weapon.DisableAutoReload;
										Time.Instance.NextModuleDisableAutoReload = DateTime.UtcNow.AddSeconds(2);
									}
									//Logging.Log("Cache.Weapons", "[" + weaponNumber + "][" + _module.TypeName + "] typeID [" + _module.TypeId + "] groupID [" + _module.GroupId + "]", Logging.White);
								}
							}
						}
					}
					
				}

				return _weapons;
			}
		}

		public List<DirectWindow> Windows
		{
			get
			{
				try
				{
					if (Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(20) || (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20)))
					{
						return _windows ?? (_windows = DirectEve.Windows);
					}

					return new List<DirectWindow>();
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Windows", "Exception [" + exception + "]", Logging.Debug);
				}

				return null;
			}
		}

		public bool CloseQuestorCMDLogoff;

		public bool CloseQuestorCMDExitGame = true;

		public bool CloseQuestorEndProcess;

		public bool GotoBaseNow;

		public bool QuestorJustStarted = true;


		public DirectWindow GetWindowByCaption(string caption)
		{
			return Windows.FirstOrDefault(w => w.Caption.Contains(caption));
		}

		public DirectWindow GetWindowByName(string name)
		{
			DirectWindow WindowToFind = null;
			try
			{
				if (!Cache.Instance.Windows.Any())
				{
					return null;
				}

				// Special cases
				if (name == "Local")
				{
					WindowToFind = Windows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_solarsystemid"));
				}

				if (WindowToFind == null)
				{
					WindowToFind = Windows.FirstOrDefault(w => w.Name == name);
				}

				if (WindowToFind != null)
				{
					return WindowToFind;
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.GetWindowByName", "Exception [" + exception + "]", Logging.Debug);
			}

			return null;
		}


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

		public DirectItem CheckCargoForItem(int typeIdToFind, int quantityToFind)
		{
			try
			{
				if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any())
				{
					DirectItem item = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == typeIdToFind && i.Quantity >= quantityToFind);
					return item;
				}

				return null; // no items found
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.CheckCargoForItem", "Exception [" + exception + "]", Logging.Debug);
			}

			return null;
		}

		public bool CheckifRouteIsAllHighSec()
		{
			Cache.Instance.RouteIsAllHighSecBool = false;

			try
			{
				// Find the first waypoint
				if (DirectEve.Navigation.GetDestinationPath() != null && DirectEve.Navigation.GetDestinationPath().Count > 0)
				{
					List<int> currentPath = DirectEve.Navigation.GetDestinationPath();
					if (currentPath == null || !currentPath.Any()) return false;
					if (currentPath[0] == 0) return false; //No destination set - prevents exception if somehow we have got an invalid destination

					foreach (int _system in currentPath)
					{
						if (_system < 60000000) // not a station
						{
							DirectSolarSystem solarSystemInRoute = Cache.Instance.DirectEve.SolarSystems[_system];
							if (solarSystemInRoute != null)
							{
								if (solarSystemInRoute.Security < 0.45)
								{
									//Bad bad bad
									Instance.RouteIsAllHighSecBool = false;
									return true;
								}

								continue;
							}

							Logging.Log("CheckifRouteIsAllHighSec", "Jump number [" + _system + "of" + currentPath.Count() + "] in the route came back as null, we could not get the system name or sec level", Logging.Debug);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.CheckifRouteIsAllHighSec", "Exception [" + exception +"]", Logging.Debug);
			}
			

			//
			// if DirectEve.Navigation.GetDestinationPath() is null or 0 jumps then it must be safe (can we assume we are not in lowsec or 0.0 already?!)
			//
			Cache.Instance.RouteIsAllHighSecBool = true;
			return true;
		}

		public void ClearPerPocketCache(string callingroutine)
		{
			try
			{
				if (DateTime.Now > Time.NextClearPocketCache)
				{
					MissionSettings.ClearPocketSpecificSettings();
					Combat._doWeCurrentlyHaveTurretsMounted = null;
					Combat.LastTargetPrimaryWeaponsWereShooting = null;
					Drones.LastTargetIDDronesEngaged = null;

					_ammoHangar = null;
					_lootHangar = null;
					_lootContainer = null;

					ListOfWarpScramblingEntities.Clear();
					ListOfJammingEntities.Clear();
					ListOfTrackingDisruptingEntities.Clear();
					ListNeutralizingEntities.Clear();
					ListOfTargetPaintingEntities.Clear();
					ListOfDampenuingEntities.Clear();
					ListofWebbingEntities.Clear();
					ListofContainersToLoot.Clear();
					ListofMissionCompletionItemsToLoot.Clear();
					
					ListOfUndockBookmarks = null;

					//MyMissileProjectionSkillLevel = SkillPlan.MissileProjectionSkillLevel();

					EntityNames.Clear();
					EntityTypeID.Clear();
					EntityGroupID.Clear();
					EntityBounty.Clear();
					EntityIsFrigate.Clear();
					EntityIsNPCFrigate.Clear();
					EntityIsCruiser.Clear();
					EntityIsNPCCruiser.Clear();
					EntityIsBattleCruiser.Clear();
					EntityIsNPCBattleCruiser.Clear();
					EntityIsBattleShip.Clear();
					EntityIsNPCBattleShip.Clear();
					EntityIsHighValueTarget.Clear();
					EntityIsLowValueTarget.Clear();
					EntityIsLargeCollidable.Clear();
					EntityIsSentry.Clear();
					EntityIsMiscJunk.Clear();
					EntityIsBadIdea.Clear();
					EntityIsFactionWarfareNPC.Clear();
					EntityIsNPCByGroupID.Clear();
					EntityIsEntutyIShouldLeaveAlone.Clear();
					EntityHaveLootRights.Clear();
					EntityIsStargate.Clear();

					Cache.Instance.LootedContainers.Clear();
					return;
				}

				//Logging.Log("ClearPerPocketCache", "[ " + callingroutine + " ] Attempted to ClearPocketCache within 5 seconds of a previous ClearPocketCache, aborting attempt", Logging.Debug);
			}
			catch (Exception ex)
			{
				Logging.Log("ClearPerPocketCache", "Exception [" + ex + "]", Logging.Debug);
				return;
			}
			finally
			{
				Time.NextClearPocketCache = DateTime.UtcNow.AddSeconds(5);
			}
		}
		
		public int RandomNumber(int min, int max)
		{
			Random random = new Random();
			return random.Next(min, max);
		}

		public bool DebugInventoryWindows(string module)
		{
			List<DirectWindow> windows = Cache.Instance.Windows;

			Logging.Log(module, "DebugInventoryWindows: *** Start Listing Inventory Windows ***", Logging.White);
			int windownumber = 0;
			foreach (DirectWindow window in windows)
			{
				if (window.Type.ToLower().Contains("inventory"))
				{
					windownumber++;
					Logging.Log(module, "----------------------------  #[" + windownumber + "]", Logging.White);
					Logging.Log(module, "DebugInventoryWindows.Name:    [" + window.Name + "]", Logging.White);
					Logging.Log(module, "DebugInventoryWindows.Type:    [" + window.Type + "]", Logging.White);
					Logging.Log(module, "DebugInventoryWindows.Caption: [" + window.Caption + "]", Logging.White);
				}
			}
			Logging.Log(module, "DebugInventoryWindows: ***  End Listing Inventory Windows  ***", Logging.White);
			return true;
		}

		public DirectContainer _itemHangar { get; set; }

		public DirectContainer ItemHangar
		{
			get
			{
				try
				{
					if (!SafeToUseStationHangars())
					{
						//Logging.Log("ItemHangar", "if (!SafeToUseStationHangars())", Logging.Debug);
						return null;
					}

					if (!Cache.Instance.InSpace && Cache.Instance.InStation)
					{
						if (Cache.Instance._itemHangar == null)
						{
							Cache.Instance._itemHangar = Cache.Instance.DirectEve.GetItemHangar();
						}

						if (Cache.Instance.Windows.All(i => i.Type != "form.StationItems")) // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
						{
							if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(10))
							{
								Logging.Log("Cache.ItemHangar", "Opening ItemHangar", Logging.Debug);
								Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
								Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
								Time.Instance.LastOpenHangar = DateTime.UtcNow;
								return null;
							}

							if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars ) Logging.Log("Cache.ItemHangar", "ItemHangar recently opened, waiting for the window to actually appear", Logging.Debug);
							return null;
						}

						if (Cache.Instance.Windows.Any(i => i.Type == "form.StationItems"))
						{
							if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("Cache.ItemHangar", "if (Cache.Instance.Windows.Any(i => i.Type == form.StationItems))", Logging.Debug);
							return Cache.Instance._itemHangar;
						}

						if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("Cache.ItemHangar", "Not sure how we got here... ", Logging.Debug);
						return null;
					}

					if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("Cache.ItemHangar", "InSpace [" + Cache.Instance.InSpace + "] InStation [" + Cache.Instance.InStation + "] waiting...", Logging.Debug);
					return null;
				}
				catch (Exception ex)
				{
					Logging.Log("ItemHangar", "Exception [" + ex + "]", Logging.Debug);
					return null;
				}
			}

			set { _itemHangar = value; }
		}

		public bool SafeToUseStationHangars()
		{
			if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10)) //yes we are adding 10 more seconds...
			{
				if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("ItemHangar", "if (DateTime.UtcNow < Time.Instance.NextDockAction.AddSeconds(10))", Logging.Debug);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))
			{
				if (Logging.DebugArm || Logging.DebugUnloadLoot || Logging.DebugHangars) Logging.Log("ItemHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(15))", Logging.Debug);
				return false;
			}

			return true;
		}

		public bool ReadyItemsHangarSingleInstance(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				DirectContainerWindow lootHangarWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.StationItems") && w.Caption.Contains("Item hangar"));

				// Is the items hangar open?
				if (lootHangarWindow == null)
				{
					// No, command it to open
					Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenHangarFloor);
					Statistics.LogWindowActionToWindowLog("Itemhangar", "Opening ItemHangar");
					Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(3, 5));
					Logging.Log(module, "Opening Item Hangar: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					return false;
				}

				Cache.Instance.ItemHangar = Cache.Instance.DirectEve.GetContainer(lootHangarWindow.currInvIdItem);
				return true;
			}

			return false;
		}

		public bool CloseItemsHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "We are in Station", Logging.Teal);
					Cache.Instance.ItemHangar = Cache.Instance.DirectEve.GetItemHangar();

					if (Cache.Instance.ItemHangar == null)
					{
						if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar was null", Logging.Teal);
						return false;
					}

					if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar exists", Logging.Teal);

					// Is the items hangar open?
					if (Cache.Instance.ItemHangar.Window == null)
					{
						Logging.Log(module, "Item Hangar: is closed", Logging.White);
						return true;
					}

					if (!Cache.Instance.ItemHangar.Window.IsReady)
					{
						if (Logging.DebugHangars) Logging.Log("OpenItemsHangar", "ItemsHangar.window is not yet ready", Logging.Teal);
						return false;
					}

					if (Cache.Instance.ItemHangar.Window.IsReady)
					{
						Cache.Instance.ItemHangar.Window.Close();
						Statistics.LogWindowActionToWindowLog("Itemhangar", "Closing ItemHangar");
						return false;
					}
				}
				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseItemsHangar", "Unable to complete CloseItemsHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool ReadyItemsHangarAsLootHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugItemHangar) Logging.Log("ReadyItemsHangarAsLootHangar", "We are in Station", Logging.Teal);
					Cache.Instance.LootHangar = Cache.Instance.ItemHangar;
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("ReadyItemsHangarAsLootHangar", "Unable to complete ReadyItemsHangarAsLootHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool ReadyItemsHangarAsAmmoHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				if (Logging.DebugHangars) Logging.Log("ReadyItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				if (Logging.DebugHangars) Logging.Log("ReadyItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)", Logging.Teal);
				return false;
			}

			try
			{
				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("ReadyItemsHangarAsAmmoHangar", "We are in Station", Logging.Teal);
					Cache.Instance.AmmoHangar = Cache.Instance.ItemHangar;
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("ReadyItemsHangarAsAmmoHangar", "unable to complete ReadyItemsHangarAsAmmoHangar [" + exception + "]", Logging.Teal);
				return false;
			}
		}

		public bool StackItemsHangarAsLootHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			try
			{
				if (Logging.DebugItemHangar) Logging.Log("StackItemsHangarAsLootHangar", "public bool StackItemsHangarAsLootHangar(String module)", Logging.Teal);

				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsLootHangar", "if (Cache.Instance.InStation)", Logging.Teal);
					if (Cache.Instance.LootHangar != null)
					{
						try
						{
							if (Cache.Instance.StackHangarAttempts > 0)
							{
								if (!WaitForLockedItems(Time.Instance.LastStackLootHangar)) return false;
								return true;
							}

							if (Cache.Instance.StackHangarAttempts <= 0)
							{
								if (LootHangar.Items.Any() && LootHangar.Items.Count() > RandomNumber(600, 800))
								{
									Logging.Log(module, "Stacking Item Hangar (as LootHangar)", Logging.White);
									Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
									Cache.Instance.LootHangar.StackAll();
									Cache.Instance.StackHangarAttempts++;
									Time.Instance.LastStackLootHangar = DateTime.UtcNow;
									Time.Instance.LastStackItemHangar = DateTime.UtcNow;
									return false;
								}

								return true;
							}

							Logging.Log(module, "Not Stacking LootHangar", Logging.White);
							return true;
						}
						catch (Exception exception)
						{
							Logging.Log(module,"Stacking Item Hangar failed ["  + exception +  "]",Logging.Teal);
							return true;
						}
					}

					if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsLootHangar", "if (!Cache.Instance.ReadyItemsHangarAsLootHangar(Cache.StackItemsHangar)) return false;", Logging.Teal);
					if (!Cache.Instance.ReadyItemsHangarAsLootHangar("Cache.StackItemsHangar")) return false;
					return false;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackItemsHangarAsLootHangar", "Unable to complete StackItemsHangarAsLootHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		private static bool WaitForLockedItems(DateTime __lastAction)
		{
			if (Cache.Instance.DirectEve.GetLockedItems().Count != 0)
			{
				if (Math.Abs(DateTime.UtcNow.Subtract(__lastAction).TotalSeconds) > 15)
				{
					Logging.Log(_States.CurrentArmState.ToString(), "Moving Ammo timed out, clearing item locks", Logging.Orange);
					Cache.Instance.DirectEve.UnlockItems();
					return false;
				}

				if (Logging.DebugUnloadLoot) Logging.Log(_States.CurrentArmState.ToString(), "Waiting for Locks to clear. GetLockedItems().Count [" + Cache.Instance.DirectEve.GetLockedItems().Count + "]", Logging.Teal);
				return false;
			}

			Cache.Instance.StackHangarAttempts = 0;
			return true;
		}

		public bool StackItemsHangarAsAmmoHangar(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)", Logging.Teal);
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsAmmoHangar", "if (DateTime.UtcNow < Cache.Instance.NextOpenHangarAction)", Logging.Teal);
				return false;
			}

			try
			{
				if (Logging.DebugItemHangar) Logging.Log("StackItemsHangarAsAmmoHangar", "public bool StackItemsHangarAsAmmoHangar(String module)", Logging.Teal);

				if (Cache.Instance.InStation)
				{
					if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsAmmoHangar", "if (Cache.Instance.InStation)", Logging.Teal);
					if (Cache.Instance.AmmoHangar != null)
					{
						try
						{
							if (Cache.Instance.StackHangarAttempts > 0)
							{
								if (!WaitForLockedItems(Time.Instance.LastStackAmmoHangar)) return false;
								return true;
							}

							if (Cache.Instance.StackHangarAttempts <= 0)
							{
								if (AmmoHangar.Items.Any() && AmmoHangar.Items.Count() > RandomNumber(600, 800))
								{
									Logging.Log(module, "Stacking Item Hangar (as AmmoHangar)", Logging.White);
									Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(5);
									Cache.Instance.AmmoHangar.StackAll();
									Cache.Instance.StackHangarAttempts++;
									Time.Instance.LastStackAmmoHangar = DateTime.UtcNow;
									Time.Instance.LastStackItemHangar = DateTime.UtcNow;
									return true;
								}

								return true;
							}

							Logging.Log(module, "Not Stacking AmmoHangar[" + "ItemHangar" + "]", Logging.White);
							return true;
						}
						catch (Exception exception)
						{
							Logging.Log(module, "Stacking Item Hangar failed [" + exception + "]", Logging.Teal);
							return true;
						}
					}

					if (Logging.DebugHangars) Logging.Log("StackItemsHangarAsAmmoHangar", "if (!Cache.Instance.ReadyItemsHangarAsAmmoHangar(Cache.StackItemsHangar)) return false;", Logging.Teal);
					if (!Cache.Instance.ReadyItemsHangarAsAmmoHangar("Cache.StackItemsHangar")) return false;
					return false;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackItemsHangarAsAmmoHangar", "Unable to complete StackItemsHangarAsAmmoHangar [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool StackCargoHold(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				return false;

			if (DateTime.UtcNow < Time.Instance.LastStackCargohold.AddSeconds(90))
				return true;

			try
			{
				Logging.Log(module, "Stacking CargoHold: waiting [" + Math.Round(Time.Instance.NextOpenCargoAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
				if (Cache.Instance.CurrentShipsCargo != null)
				{
					try
					{
						if (Cache.Instance.StackHangarAttempts > 0)
						{
							if (!WaitForLockedItems(Time.Instance.LastStackAmmoHangar)) return false;
							return true;
						}

						if (Cache.Instance.StackHangarAttempts <= 0)
						{
							if (Cache.Instance.CurrentShipsCargo.Items.Any())
							{
								Time.Instance.LastStackCargohold = DateTime.UtcNow;
								Cache.Instance.CurrentShipsCargo.StackAll();
								Cache.Instance.StackHangarAttempts++;
								return false;
							}

							return true;
						}
					}
					catch (Exception exception)
					{
						Logging.Log(module, "Stacking Item Hangar failed [" + exception + "]", Logging.Teal);
						return true;
					}
				}
				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("StackCargoHold", "Unable to complete StackCargoHold [" + exception + "]", Logging.Teal);
				return true;
			}
		}

		public bool CloseCargoHold(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				return false;

			try
			{
				if (DateTime.UtcNow < Time.Instance.NextOpenCargoAction)
				{
					if ((DateTime.UtcNow.Subtract(Time.Instance.NextOpenCargoAction).TotalSeconds) > 0)
					{
						Logging.Log("CloseCargoHold", "waiting [" + Math.Round(Time.Instance.NextOpenCargoAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					}

					return false;
				}

				if (Cache.Instance.CurrentShipsCargo == null || Cache.Instance.CurrentShipsCargo.Window == null)
				{
					Cache.Instance._currentShipsCargo = null;
					Logging.Log("CloseCargoHold", "Cargohold was not open, no need to close", Logging.White);
					return true;
				}

				if (Cache.Instance.InStation || Cache.Instance.InSpace) //do we need to special case pods here?
				{
					if (Cache.Instance.CurrentShipsCargo.Window == null)
					{
						Cache.Instance._currentShipsCargo = null;
						Logging.Log("CloseCargoHold", "Cargohold is closed", Logging.White);
						return true;
					}

					if (!Cache.Instance.CurrentShipsCargo.Window.IsReady)
					{
						//Logging.Log(module, "cargo window is not ready", Logging.White);
						return false;
					}

					if (Cache.Instance.CurrentShipsCargo.Window.IsReady)
					{
						Cache.Instance.CurrentShipsCargo.Window.Close();
						Statistics.LogWindowActionToWindowLog("CargoHold", "Closing CargoHold");
						Time.Instance.NextOpenCargoAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(1, 2));
						return false;
					}

					Cache.Instance._currentShipsCargo = null;
					Logging.Log("CloseCargoHold", "Cargohold is probably closed", Logging.White);
					return true;
				}

				return false;
			}
			catch (Exception exception)
			{
				Logging.Log("CloseCargoHold", "Unable to complete CloseCargoHold [" + exception + "]", Logging.Teal);
				return true;
			}
		}
		
		public DirectContainerWindow PrimaryInventoryWindow { get; set; }

		public DirectContainerWindow corpAmmoHangarSecondaryWindow { get; set; }

		public DirectContainerWindow corpLootHangarSecondaryWindow { get; set; }

		public bool OpenInventoryWindow(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			Cache.Instance.PrimaryInventoryWindow = (DirectContainerWindow)Cache.Instance.Windows.FirstOrDefault(w => w.Type.Contains("form.Inventory") && w.Name.Contains("Inventory"));

			if (Cache.Instance.PrimaryInventoryWindow == null)
			{
				if (Logging.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow is null, opening InventoryWindow", Logging.Teal);

				// No, command it to open
				Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenInventory);
				Statistics.LogWindowActionToWindowLog("Inventory (main)", "Open Inventory");
				Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 3));
				Logging.Log(module, "Opening Inventory Window: waiting [" + Math.Round(Time.Instance.NextOpenHangarAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
				return false;
			}

			if (Cache.Instance.PrimaryInventoryWindow != null)
			{
				if (Logging.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow exists", Logging.Teal);
				if (Cache.Instance.PrimaryInventoryWindow.IsReady)
				{
					if (Logging.DebugHangars) Logging.Log("debug", "Cache.Instance.InventoryWindow exists and is ready", Logging.Teal);
					return true;
				}

				//
				// if the InventoryWindow "hangs" and is never ready we will hang... it would be better if we set a timer
				// and closed the inventorywindow that is not ready after 10-20seconds. (can we close a window that is in a state if !window.isready?)
				//
				return false;
			}

			return false;
		}

		public DirectLoyaltyPointStoreWindow _lpStore;
		public DirectLoyaltyPointStoreWindow LPStore
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_lpStore == null)
						{
							if (!Cache.Instance.InStation)
							{
								Logging.Log("LPStore", "Opening LP Store: We are not in station?! There is no LP Store in space, waiting...", Logging.Orange);
								return null;
							}

							if (Cache.Instance.InStation)
							{
								_lpStore = Cache.Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
								
								if (_lpStore == null)
								{
									if (DateTime.UtcNow > Time.Instance.NextLPStoreAction)
									{
										Logging.Log("LPStore", "Opening loyalty point store", Logging.White);
										Time.Instance.NextLPStoreAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(30, 240));
										Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
										Statistics.LogWindowActionToWindowLog("LPStore", "Opening LPStore");
										return null;
									}

									return null;
								}

								return _lpStore;
							}

							return null;
						}

						return _lpStore;
					}

					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("LPStore", "Unable to define LPStore [" + exception + "]", Logging.Teal);
					return null;
				}
			}
			private set
			{
				_lpStore = value;
			}
		}

		public bool CloseLPStore(string module)
		{
			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
			{
				return false;
			}

			if (!Cache.Instance.InStation)
			{
				Logging.Log(module, "Closing LP Store: We are not in station?!", Logging.Orange);
				return false;
			}

			if (Cache.Instance.InStation)
			{
				Cache.Instance.LPStore = Cache.Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
				if (Cache.Instance.LPStore != null)
				{
					Logging.Log(module, "Closing loyalty point store", Logging.White);
					Cache.Instance.LPStore.Close();
					Statistics.LogWindowActionToWindowLog("LPStore", "Closing LPStore");
					return false;
				}

				return true;
			}

			return true; //if we are not in station then the LP Store should have auto closed already.
		}

		private DirectFittingManagerWindow _fittingManagerWindow; 
		public DirectFittingManagerWindow FittingManagerWindow
		{
			get
			{
				try
				{
					if (Cache.Instance.InStation)
					{
						if (_fittingManagerWindow == null)
						{
							if (!Cache.Instance.InStation || Cache.Instance.InSpace)
							{
								Logging.Log("FittingManager", "Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...", Logging.Debug);
								return null;
							}

							if (Cache.Instance.InStation)
							{
								if (Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().Any())
								{
									DirectFittingManagerWindow __fittingManagerWindow = Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault();
									if (__fittingManagerWindow != null && __fittingManagerWindow.IsReady)
									{
										_fittingManagerWindow = __fittingManagerWindow;
										return _fittingManagerWindow;
									}
								}

								if (DateTime.UtcNow > Time.Instance.NextWindowAction)
								{
									Logging.Log("FittingManager", "Opening Fitting Manager Window", Logging.White);
									Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(10, 24));
									Cache.Instance.DirectEve.OpenFitingManager();
									Statistics.LogWindowActionToWindowLog("FittingManager", "Opening FittingManager");
									return null;
								}

								if (Logging.DebugFittingMgr) Logging.Log("FittingManager", "NextWindowAction is still in the future [" + Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds + "] sec", Logging.Debug);
								return null;
							}

							return null;
						}

						return _fittingManagerWindow;
					}

					Logging.Log("FittingManager", "Opening Fitting Manager: We are not in station?! There is no Fitting Manager in space, waiting...", Logging.Debug);
					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("FittingManager", "Unable to define FittingManagerWindow [" + exception + "]", Logging.Teal);
					return null;
				}
			}
			set
			{
				_fittingManagerWindow = value;
			}
		}

		public bool CloseFittingManager(string module)
		{
			if (Settings.Instance.UseFittingManager)
			{
				if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				{
					return false;
				}

				if (Cache.Instance.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault() != null)
				{
					Logging.Log(module, "Closing Fitting Manager Window", Logging.White);
					Cache.Instance.FittingManagerWindow.Close();
					Statistics.LogWindowActionToWindowLog("FittingManager", "Closing FittingManager");
					Cache.Instance.FittingManagerWindow = null;
					Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddSeconds(2);
					return true;
				}
				
				return true;
			}

			return true;
		}
		
		public DirectMarketWindow MarketWindow { get; set; }

		public bool OpenMarket(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextWindowAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				Cache.Instance.MarketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
				
				// Is the Market window open?
				if (Cache.Instance.MarketWindow == null)
				{
					// No, command it to open
					Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
					Statistics.LogWindowActionToWindowLog("MarketWindow", "Opening MarketWindow");
					Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 4));
					Logging.Log(module, "Opening Market Window: waiting [" + Math.Round(Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]", Logging.White);
					return false;
				}

				return true; //if MarketWindow is not null then the window must be open.
			}

			return false;
		}

		public bool CloseMarket(string module)
		{
			if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
			{
				return false;
			}

			if (DateTime.UtcNow < Time.Instance.NextWindowAction)
			{
				return false;
			}

			if (Cache.Instance.InStation)
			{
				Cache.Instance.MarketWindow = Cache.Instance.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

				// Is the Market window open?
				if (Cache.Instance.MarketWindow == null)
				{
					//already closed
					return true;
				}

				//if MarketWindow is not null then the window must be open, so close it.
				Cache.Instance.MarketWindow.Close();
				Statistics.LogWindowActionToWindowLog("MarketWindow", "Closing MarketWindow");
				return true;
			}

			return true;
		}

		public bool OpenContainerInSpace(string module, EntityCache containerToOpen)
		{
			if (DateTime.UtcNow < Time.Instance.NextLootAction)
			{
				return false;
			}

			if (Cache.Instance.InSpace && containerToOpen.Distance <= (int)Distances.ScoopRange)
			{
				Cache.Instance.ContainerInSpace = Cache.Instance.DirectEve.GetContainer(containerToOpen.Id);

				if (Cache.Instance.ContainerInSpace != null)
				{
					if (Cache.Instance.ContainerInSpace.Window == null)
					{
						if (containerToOpen.OpenCargo())
						{
							Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
							Logging.Log(module, "Opening Container: waiting [" + Math.Round(Time.Instance.NextLootAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " sec]", Logging.White);
							return false;
						}

						return false;
					}

					if (!Cache.Instance.ContainerInSpace.Window.IsReady)
					{
						Logging.Log(module, "Container window is not ready", Logging.White);
						return false;
					}

					if (Cache.Instance.ContainerInSpace.Window.IsPrimary())
					{
						Logging.Log(module, "Opening Container window as secondary", Logging.White);
						Cache.Instance.ContainerInSpace.Window.OpenAsSecondary();
						Statistics.LogWindowActionToWindowLog("ContainerInSpace", "Opening ContainerInSpace");
						Time.Instance.NextLootAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.LootingDelay_milliseconds);
						return true;
					}
				}

				return true;
			}
			Logging.Log(module, "Not in space or not in scoop range", Logging.Orange);
			return true;
		}

		public bool RepairItems(string module)
		{
			try
			{

				if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(5) && !Cache.Instance.InSpace || DateTime.UtcNow < Time.Instance.NextRepairItemsAction) // we wait 20 seconds after we last thought we were in space before trying to do anything in station
				{
					//Logging.Log(module, "Waiting...", Logging.Orange);
					return false;
				}

				if (!Cache.Instance.Windows.Any())
				{
					return false;
				}

				Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));

				if (Cache.Instance.InStation && !Cache.Instance.DirectEve.hasRepairFacility())
				{
					Logging.Log(module, "This station does not have repair facilities to use! aborting attempt to use non-existent repair facility.", Logging.Orange);
					return true;
				}

				if (Cache.Instance.InStation)
				{
					DirectRepairShopWindow repairWindow = Cache.Instance.Windows.OfType<DirectRepairShopWindow>().FirstOrDefault();

					DirectWindow repairQuote = Cache.Instance.GetWindowByName("Set Quantity");

					if (doneUsingRepairWindow)
					{
						doneUsingRepairWindow = false;
						if (repairWindow != null) repairWindow.Close();
						return true;
					}

					foreach (DirectWindow window in Cache.Instance.Windows)
					{
						if (window.Name == "modal")
						{
							if (!string.IsNullOrEmpty(window.Html))
							{
								if (window.Html.Contains("Repairing these items will cost"))
								{
									if (window.Html != null) Logging.Log("RepairItems", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
									Logging.Log(module, "Closing Quote for Repairing All with YES", Logging.White);
									window.AnswerModal("Yes");
									doneUsingRepairWindow = true;
									return false;
								}

								if (window.Html.Contains("How much would you like to repair?"))
								{
									if (window.Html != null) Logging.Log("RepairItems", "Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
									Logging.Log(module, "Closing Quote for Repairing All with OK", Logging.White);
									window.AnswerModal("OK");
									doneUsingRepairWindow = true;
									return false;
								}
							}
						}
					}

					if (repairQuote != null && repairQuote.IsModal && repairQuote.IsKillable)
					{
						if (repairQuote.Html != null) Logging.Log("RepairItems", "Content of modal window (HTML): [" + (repairQuote.Html).Replace("\n", "").Replace("\r", "") + "]", Logging.White);
						Logging.Log(module, "Closing Quote for Repairing All with OK", Logging.White);
						repairQuote.AnswerModal("OK");
						doneUsingRepairWindow = true;
						return false;
					}

					if (repairWindow == null)
					{
						Logging.Log(module, "Opening repairshop window", Logging.White);
						Cache.Instance.DirectEve.OpenRepairShop();
						Statistics.LogWindowActionToWindowLog("RepairWindow", "Opening RepairWindow");
						Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(1, 3));
						return false;
					}

					if (Cache.Instance.ItemHangar == null) {
						Logging.Log(module, "if (Cache.Instance.ItemHangar == null)", Logging.White);
						return false;
					}
					if (Cache.Instance.ShipHangar == null) {
						Logging.Log(module, "if (Cache.Instance.ShipHangar == null)", Logging.White);
						return false;
					}
					
					if (Drones.UseDrones)
					{
						if (Drones.DroneBay == null) return false;
					}
					
					if(Cache.Instance.ShipHangar.Items == null)
					{
						Logging.Log(module, "Cache.Instance.ShipHangar.Items == null", Logging.White);
						return false;
					}

					//repair ships in ships hangar
					List<DirectItem> repairAllItems = Cache.Instance.ShipHangar.Items;

					//repair items in items hangar and drone bay of active ship also
					repairAllItems.AddRange(Cache.Instance.ItemHangar.Items);
					if (Drones.UseDrones)
					{
						repairAllItems.AddRange(Drones.DroneBay.Items);
					}

					if (repairAllItems.Any())
					{
						if (String.IsNullOrEmpty(repairWindow.AvgDamage()))
						{
							Logging.Log(module, "Add items to repair list", Logging.White);
							repairWindow.RepairItems(repairAllItems);
							Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));
							return false;
						}

						Logging.Log(module, "Repairing Items: repairWindow.AvgDamage: " + repairWindow.AvgDamage(), Logging.White);
						if (repairWindow.AvgDamage() == "Avg: 0,0 % Damaged")
						{
							Logging.Log(module, "Repairing Items: Zero Damage: skipping repair.", Logging.White);
							repairWindow.Close();
							Statistics.LogWindowActionToWindowLog("RepairWindow", "Closing RepairWindow");
							return true;
						}

						repairWindow.RepairAll();
						Time.Instance.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(Settings.Instance.RandomNumber(2, 4));
						return false;
					}

					Logging.Log(module, "No items available, nothing to repair.", Logging.Orange);
					return true;
				}
				Logging.Log(module, "Not in station.", Logging.Orange);
				return false;
			}
			catch (Exception ex)
			{
				Logging.Log("Cache.RepairItems", "Exception:" + ex, Logging.White);
				return false;
			}
		}
		
		public bool ClosePrimaryInventoryWindow(string module)
		{
			if (DateTime.UtcNow < Time.Instance.NextOpenHangarAction)
				return false;

			//
			// go through *every* window
			//
			try
			{
				foreach (DirectWindow window in Cache.Instance.Windows)
				{
					if (window.Type.Contains("form.Inventory"))
					{
						if (Logging.DebugHangars) Logging.Log(module, "ClosePrimaryInventoryWindow: Closing Primary Inventory Window Named [" + window.Name + "]", Logging.White);
						window.Close();
						Statistics.LogWindowActionToWindowLog("Inventory (main)", "Close Inventory");
						Time.Instance.NextOpenHangarAction = DateTime.UtcNow.AddMilliseconds(500);
						return false;
					}
				}

				return true;
			}
			catch (Exception exception)
			{
				Logging.Log("ClosePrimaryInventoryWindow", "Unable to complete ClosePrimaryInventoryWindow [" + exception + "]", Logging.Teal);
				return false;
			}
		}

	}
}
