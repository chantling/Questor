﻿using System;
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Storylines
{
    public class GenericCombatStoryline : IStoryline
    {
        //private readonly AgentInteraction _agentInteraction;
        //private readonly Arm _arm;
        private readonly CombatMissionCtrl _combatMissionCtrl;
        private readonly List<Ammo> _neededAmmo;
        private long _agentId;
        //private readonly Combat _combat;
        //private readonly Drones _drones;
        //private readonly Salvage _salvage;
        //private readonly Statistics _statistics;

        private GenericCombatStorylineState _state;

        public GenericCombatStoryline()
        {
            _neededAmmo = new List<Ammo>();
            //_agentInteraction = new AgentInteraction();
            //_arm = new Arm();
            //_combat = new Combat();
            //_drones = new Drones();
            //_salvage = new Salvage();
            //_statistics = new Statistics();
            _combatMissionCtrl = new CombatMissionCtrl();

            //Settings.Instance.SettingsLoaded += ApplySettings;
        }

        public GenericCombatStorylineState State
        {
            get { return _state; }
            set { _state = value; }
        }

        /// <summary>
        ///     We check what ammo we need by starting a conversation with the agent and load the appropriate ammo
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_agentId != Cache.Instance.CurrentStorylineAgentId)
            {
                _neededAmmo.Clear();
                _agentId = Cache.Instance.CurrentStorylineAgentId;

                AgentInteraction.ForceAccept = true; // This makes agent interaction skip the offer-check
                _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                AgentInteraction.Purpose = AgentInteractionPurpose.AmmoCheck;

                _States.CurrentArmState = ArmState.Idle;
                _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Start;
                _States.CurrentDroneState = DroneState.WaitingForTargets;
            }

            try
            {
                if (!Interact())
                    return StorylineState.Arm;

                if (!LoadAmmo())
                    return StorylineState.Arm;

                // We are done, reset agent id
                _agentId = 0;

                return StorylineState.GotoAgent;
            }
            catch (Exception ex)
            {
                // Something went wrong!
                Logging.Log("Something went wrong, blacklist this agent [" + ex.Message + "]");
                return StorylineState.BlacklistAgent;
            }
        }

        /// <summary>
        ///     We have no pre-accept steps
        /// </summary>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            // Not really a step is it? :)
            _state = GenericCombatStorylineState.WarpOutStation;
            return StorylineState.AcceptMission;
        }

        /// <summary>
        ///     Do a mini-questor here (goto mission, execute mission, goto base)
        /// </summary>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            switch (_state)
            {
                case GenericCombatStorylineState.WarpOutStation:

                    DirectBookmark warpOutBookMark = null;
                    try
                    {
                        warpOutBookMark =
                            Cache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "")
                                .OrderByDescending(b => b.CreatedOn)
                                .FirstOrDefault(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);
                    }
                    catch (Exception ex)
                    {
                        Logging.Log("Exception: " + ex);
                    }

                    long solarid = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookMark == null)
                    {
                        Logging.Log("No Bookmark");
                        _state = GenericCombatStorylineState.GotoMission;
                        break;
                    }

                    if (warpOutBookMark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Logging.Log("Warp at " + warpOutBookMark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookMark);
                            Defense.DoNotBreakInvul = true;
                        }

                        Traveler.ProcessState();
                        if (_States.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Logging.Log("Safe!");
                            Defense.DoNotBreakInvul = false;
                            _state = GenericCombatStorylineState.GotoMission;
                            Traveler.Destination = null;
                            break;
                        }

                        break;
                    }

                    Logging.Log("No Bookmark in System");
                    _state = GenericCombatStorylineState.GotoMission;
                    break;

                case GenericCombatStorylineState.GotoMission:
                    var missionDestination = Traveler.Destination as MissionBookmarkDestination;
                    //
                    // if we have no destination yet... OR if missionDestination.AgentId != storyline.CurrentStorylineAgentId
                    //
                    //if (missionDestination != null) Logging.Log("GenericCombatStoryline: missionDestination.AgentId [" + missionDestination.AgentId + "] " + "and storyline.CurrentStorylineAgentId [" + storyline.CurrentStorylineAgentId + "]");
                    //if (missionDestination == null) Logging.Log("GenericCombatStoryline: missionDestination.AgentId [ NULL ] " + "and storyline.CurrentStorylineAgentId [" + storyline.CurrentStorylineAgentId + "]");
                    if (missionDestination == null || missionDestination.AgentId != Cache.Instance.CurrentStorylineAgentId)
                        // We assume that this will always work "correctly" (tm)
                    {
                        var nameOfBookmark = "";
                        if (Settings.Instance.EveServerName == "Tranquility") nameOfBookmark = "Encounter";
                        if (Settings.Instance.EveServerName == "Serenity") nameOfBookmark = "遭遇战";
                        if (nameOfBookmark == "") nameOfBookmark = "Encounter";
                        Logging.Log("Setting Destination to 1st bookmark from AgentID: [" + Cache.Instance.CurrentStorylineAgentId + "] with [" + nameOfBookmark +
                            "] in the title");
                        Traveler.Destination =
                            new MissionBookmarkDestination(MissionSettings.GetMissionBookmark(Cache.Instance.CurrentStorylineAgentId, nameOfBookmark));
                    }

                    if (Combat.PotentialCombatTargets.Any())
                    {
                        Logging.Log("Priority targets found while traveling, engaging!");
                        Combat.ProcessState();
                    }

                    Traveler.ProcessState();
                    if (_States.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        _state = GenericCombatStorylineState.ExecuteMission;

                        //_States.CurrentCombatState = CombatState.CheckTargets;
                        Traveler.Destination = null;
                    }
                    break;

                case GenericCombatStorylineState.ExecuteMission:
                    Combat.ProcessState();
                    Drones.ProcessState();
                    Salvage.ProcessState();
                    _combatMissionCtrl.ProcessState();

                    // If we are out of ammo, return to base, the mission will fail to complete and the bot will reload the ship
                    // and try the mission again
                    if (_States.CurrentCombatState == CombatState.OutOfAmmo)
                    {
                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();

                        Logging.Log("Out of Ammo! - Not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                            Combat.MinimumAmmoCharges + "]");
                        return StorylineState.ReturnToAgent;
                    }

                    if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Done)
                    {
                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();
                        return StorylineState.ReturnToAgent;
                    }

                    // If in error state, just go home and stop the bot
                    if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                    {
                        // Clear looted containers
                        Cache.Instance.LootedContainers.Clear();

                        Logging.Log("Error");
                        return StorylineState.ReturnToAgent;
                    }
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        private void ApplySettings(object sender, EventArgs e)
        {
            //Logging.Log("GenericCombatStoryline.ApplySettings", "called.");
            //Settings.Instance.LoadSettings(true);
        }

        /// <summary>
        ///     Interact with the agent so we know what ammo to bring
        /// </summary>
        /// <returns>True if interact is done</returns>
        private bool Interact()
        {
            // Are we done?
            if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
                return true;

            if (Cache.Instance.Agent == null)
                throw new Exception("Invalid agent");

            // Start the conversation
            if (_States.CurrentAgentInteractionState == AgentInteractionState.Idle)
                _States.CurrentAgentInteractionState = AgentInteractionState.StartConversation;

            // Interact with the agent to find out what ammo we need
            AgentInteraction.ProcessState();

            if (_States.CurrentAgentInteractionState == AgentInteractionState.DeclineMission)
            {
                if (Cache.Instance.Agent.Window != null)
                    Cache.Instance.Agent.Window.Close();
                Logging.Log("Mission offer is in a Low Security System or faction blacklisted.");
                    //do storyline missions in lowsec get blacklisted by: "public StorylineState Arm(Storyline storyline)"?
                throw new Exception("Low security systems");
            }

            return false;
        }

        /// <summary>
        ///     Load the appropriate ammo
        /// </summary>
        /// <returns></returns>
        private bool LoadAmmo()
        {
            if (_States.CurrentArmState == ArmState.Done)
                return true;

            if (_States.CurrentArmState == ArmState.Idle)
                _States.CurrentArmState = ArmState.Begin;

            Modules.Actions.Arm.ProcessState();

            if (_States.CurrentArmState == ArmState.Done)
            {
                _States.CurrentArmState = ArmState.Idle;
                return true;
            }

            return false;
        }
    }
}