// ------------------------------------------------------------------------------
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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using DirectEve;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Actions
{
    public static class AgentInteraction
    {
        public const string RequestMission = "Request Mission";
        public const string ViewMission = "View Mission";
        public const string CompleteMission = "Complete Mission";
        public const string LocateCharacter = "Locate Character";
        public const string Accept = "Accept";
        public const string Decline = "Decline";
        public const string Close = "Close";
        public const string Delay = "Delay";
        public const string Quit = "Quit";
        public const string NoJobsAvailable = "Sorry, I have no jobs available for you.";

        //public static string MissionName;

        private static bool _agentStandingsCheckFlag; //false;
        private static bool _waitingOnAgentResponse;
        private static bool _waitingOnMission;

        private static DateTime _agentWindowTimeStamp = DateTime.MinValue;
        private static DateTime _agentStandingsCheckTimeOut = DateTime.UtcNow.AddDays(1);
        //private static DateTime _nextAgentAction { get; set; }
        private static DateTime _lastAgentAction;
        private static DateTime _waitingOnAgentResponseTimer = DateTime.UtcNow;
        private static DateTime _waitingOnMissionTimer = DateTime.UtcNow;
        private static DateTime _agentWindowLastReady = DateTime.UtcNow.AddDays(-1);
        private static DateTime _lastAgentInteractionPulse = DateTime.UtcNow.AddDays(-1);
        private static DateTime _lastAgentActionStateChange = DateTime.UtcNow.AddDays(-1);

        private static int LoyaltyPointCounter;


        static AgentInteraction()
        {
        }

        public static float StandingsNeededToAccessLevel1Agent { get; set; }
        public static float StandingsNeededToAccessLevel2Agent { get; set; }
        public static float StandingsNeededToAccessLevel3Agent { get; set; }
        public static float StandingsNeededToAccessLevel4Agent { get; set; }
        public static float StandingsNeededToAccessLevel5Agent { get; set; }

        public static bool UseStorylineAgentAsActiveAgent { get; set; }

        public static bool ForceAccept { get; set; }

        public static AgentInteractionPurpose Purpose { get; set; }

        public static DirectWindow JournalWindow { get; set; }

        private static void StartConversation(string module)
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                Cache.Instance.AgentEffectiveStandingtoMe = Cache.Instance.DirectEve.Standings.EffectiveStanding(AgentId,
                    Cache.Instance.DirectEve.Session.CharacterId ?? -1);
                Cache.Instance.AgentCorpEffectiveStandingtoMe = Cache.Instance.DirectEve.Standings.EffectiveStanding(Agent.CorpId,
                    Cache.Instance.DirectEve.Session.CharacterId ?? -1);
                Cache.Instance.AgentFactionEffectiveStandingtoMe = Cache.Instance.DirectEve.Standings.EffectiveStanding(Agent.FactionId,
                    Cache.Instance.DirectEve.Session.CharacterId ?? -1);

                Cache.Instance.StandingUsedToAccessAgent = Math.Max(Cache.Instance.AgentEffectiveStandingtoMe,
                    Math.Max(Cache.Instance.AgentCorpEffectiveStandingtoMe, Cache.Instance.AgentFactionEffectiveStandingtoMe));
                //AgentsList currentAgent = MissionSettings.ListOfAgents.FirstOrDefault(i => i.Name == Cache.Instance.CurrentAgent);

                Cache.Instance.AgentEffectiveStandingtoMeText = Cache.Instance.StandingUsedToAccessAgent.ToString("0.00");

                //
                // Standings Check: if this is a totally new agent this check will timeout after 20 seconds
                //
                if (DateTime.UtcNow < _agentStandingsCheckTimeOut)
                {
                    if (((float) Cache.Instance.StandingUsedToAccessAgent == (float) 0.00))
                    {
                        if (!_agentStandingsCheckFlag)
                        {
                            _agentStandingsCheckTimeOut = DateTime.UtcNow.AddSeconds(15);
                            _agentStandingsCheckFlag = true;
                        }

                        _lastAgentAction = DateTime.UtcNow;
                        Logging.Logging.Log(" Agent [" + Cache.Instance.DirectEve.GetAgentById(AgentId).Name + "] Standings show as [" +
                            Cache.Instance.StandingUsedToAccessAgent + " and must not yet be available. retrying for [" +
                            Math.Round((double)_agentStandingsCheckTimeOut.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " sec]");
                        return;
                    }
                }

                switch (Agent.Level)
                {
                    //
                    // what do tutorial mission agents show as?
                    //
                    case 1: //lvl1 agent
                        if (Cache.Instance.StandingUsedToAccessAgent < StandingsNeededToAccessLevel1Agent)
                        {
                            Logging.Logging.Log("Our Standings to [" + Agent.Name + "] are [" + Cache.Instance.StandingUsedToAccessAgent + "] < [" +
                                StandingsNeededToAccessLevel1Agent + "]");
                            return;
                        }
                        break;

                    case 2: //lvl2 agent
                        if (Cache.Instance.StandingUsedToAccessAgent < StandingsNeededToAccessLevel2Agent)
                        {
                            Logging.Logging.Log("Our Standings to [" + Agent.Name + "] are [" + Cache.Instance.StandingUsedToAccessAgent + "] < [" +
                                StandingsNeededToAccessLevel2Agent + "]");
                            return;
                        }
                        break;

                    case 3: //lvl3 agent
                        if (Cache.Instance.StandingUsedToAccessAgent < StandingsNeededToAccessLevel3Agent)
                        {
                            Logging.Logging.Log("Our Standings to [" + Agent.Name + "] are [" + Cache.Instance.StandingUsedToAccessAgent + "] < [" +
                                StandingsNeededToAccessLevel3Agent + "]");
                            return;
                        }
                        break;

                    case 4: //lvl4 agent
                        if (Cache.Instance.StandingUsedToAccessAgent < StandingsNeededToAccessLevel4Agent)
                        {
                            Logging.Logging.Log("Our Standings to [" + Agent.Name + "] are [" + Cache.Instance.StandingUsedToAccessAgent + "] < [" +
                                StandingsNeededToAccessLevel4Agent + "]");
                            return;
                        }
                        break;

                    case 5: //lvl5 agent
                        if (Cache.Instance.StandingUsedToAccessAgent < StandingsNeededToAccessLevel5Agent)
                        {
                            Logging.Logging.Log("Our Standings to [" + Agent.Name + "] are [" + Cache.Instance.StandingUsedToAccessAgent + "] < [" +
                                StandingsNeededToAccessLevel5Agent + "]");
                            return;
                        }
                        break;
                }

                if (!OpenAgentWindow(module)) return;

                if (DateTime.UtcNow < _lastAgentActionStateChange.AddMilliseconds(Cache.Instance.RandomNumber(800, 1200)))
                    return; //enforce a 4 sec wait after each state change

                if (Purpose == AgentInteractionPurpose.AmmoCheck)
                {
                    Logging.Logging.Log("Checking ammo type");
                    _States.CurrentAgentInteractionState = AgentInteractionState.WaitForMission;
                }
                else
                {
                    Logging.Logging.Log("Replying to agent");
                    _States.CurrentAgentInteractionState = AgentInteractionState.ReplyToAgent;
                }

                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        private static void ReplyToAgent(string module)
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;


                if (!OpenAgentWindow(module)) return;

                if (DateTime.UtcNow < _lastAgentActionStateChange.AddMilliseconds(Cache.Instance.RandomNumber(800, 1200)))
                    return; //enforce a 4 sec wait after each state change

                if (Agent.Window.AgentResponses == null || !Agent.Window.AgentResponses.Any())
                {
                    if (_waitingOnAgentResponse == false)
                    {
                        if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                            Logging.Logging.Log("Debug: if (_waitingOnAgentResponse == false)");
                        _waitingOnAgentResponseTimer = DateTime.UtcNow;
                        _waitingOnAgentResponse = true;
                    }

                    if (DateTime.UtcNow.Subtract(_waitingOnAgentResponseTimer).TotalSeconds > 15)
                    {
                        Logging.Logging.Log("Debug: agentWindowAgentresponses == null : trying to close the agent window");
                        Agent.Window.Close();
                    }

                    if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                        Logging.Logging.Log("Debug: if (Agent.Window.AgentResponses == null || !Agent.Window.AgentResponses.Any())");
                    return;
                }

                if (Agent.Window.AgentResponses.Any())
                {
                    if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                        Logging.Logging.Log("Debug: we have Agent.Window.AgentResponces");
                }

                _waitingOnAgentResponse = false;

                var request = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(RequestMission));
                var complete = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(CompleteMission));
                var view = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(ViewMission));
                var accept = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(Accept));
                var decline = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(Decline));
                var delay = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(Delay));
                var quit = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(Quit));
                var close = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(Close));
                var NoMoreMissionsAvailable = Agent.Window.AgentResponses.FirstOrDefault(r => r.Text.Contains(NoJobsAvailable));
                var html = Agent.Window.Objective;

                //
                // Read the possibly responses and make sure we are 'doing the right thing' - set AgentInteractionPurpose to fit the state of the agent window
                //
                if (NoMoreMissionsAvailable != null)
                {
                    if (MissionSettings.ListOfAgents != null && MissionSettings.ListOfAgents.Count() > 1)
                    {
                        //
                        //Change Agents
                        //
                        var _currentAgent = MissionSettings.ListOfAgents.FirstOrDefault(i => i.Name == Cache.Instance.CurrentAgent);
                        if (_currentAgent != null)
                        {
                            Logging.Logging.Log("Our current agent [" + Cache.Instance.CurrentAgent + "] does not have any more missions for us. Attempting to change agents" +
                                Cache.Instance.CurrentAgent);
                            _currentAgent.DeclineTimer = DateTime.UtcNow.AddDays(5);
                        }
                        CloseConversation();

                        return;
                    }

                    Logging.Logging.Log("Our current agent [" + Cache.Instance.CurrentAgent +
                        "] does not have any more missions for us. Define more / different agents in the character XML. Pausing." + Cache.Instance.CurrentAgent);
                    Cache.Instance.Paused = true;
                }

                if (Purpose != AgentInteractionPurpose.AmmoCheck) //do not change the AgentInteractionPurpose if we are checking which ammo type to use.
                {
                    if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                        Logging.Logging.Log("if (Purpose != AgentInteractionPurpose.AmmoCheck) //do not change the AgentInteractionPurpose if we are checking which ammo type to use.");
                    if (accept != null && decline != null && delay != null)
                    {
                        if (Purpose != AgentInteractionPurpose.StartMission)
                        {
                            Logging.Logging.Log("ReplyToAgent: Found accept button, Changing Purpose to StartMission");
                            _agentWindowTimeStamp = DateTime.UtcNow;
                            Purpose = AgentInteractionPurpose.StartMission;
                        }
                    }

                    if (complete != null && quit != null && close != null && Statistics.MissionCompletionErrors == 0)
                    {
                        //
                        // this should run for ANY courier and likely needs to be changed when we implement generic courier support
                        //
                        if (Purpose != AgentInteractionPurpose.CompleteMission)
                        {
                            Logging.Logging.Log("ReplyToAgent: Found complete button, Changing Purpose to CompleteMission");

                            //we have a mission in progress here, attempt to complete it
                            if (DateTime.UtcNow > _agentWindowTimeStamp.AddSeconds(30))
                            {
                                Purpose = AgentInteractionPurpose.CompleteMission;
                            }
                        }
                    }

                    if (request != null && close != null)
                    {
                        if (Purpose != AgentInteractionPurpose.StartMission)
                        {
                            Logging.Logging.Log("ReplyToAgent: Found request button, Changing Purpose to StartMission");

                            //we do not have a mission yet, request one?
                            if (DateTime.UtcNow > _agentWindowTimeStamp.AddSeconds(15))
                            {
                                Purpose = AgentInteractionPurpose.StartMission;
                            }
                        }
                    }
                }

                if (complete != null)
                {
                    if (Purpose == AgentInteractionPurpose.CompleteMission)
                    {
                        // let's try to get the isk and lp value here again

                        var lpCurrentMission = 0;
                        var iskFinishedMission = 0;
                        Statistics.ISKMissionReward = 0;
                        Statistics.LoyaltyPointsForCurrentMission = 0;
                        var iskRegex = new Regex(@"([0-9]+)((\.([0-9]+))*) ISK", RegexOptions.Compiled);
                        foreach (Match itemMatch in iskRegex.Matches(html))
                        {
                            var val = 0;
                            int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out val);
                            iskFinishedMission += val;
                        }

                        var lpRegex = new Regex(@"([0-9]+) Loyalty Points", RegexOptions.Compiled);
                        foreach (Match itemMatch in lpRegex.Matches(html))
                        {
                            var val = 0;
                            int.TryParse(Regex.Match(itemMatch.Value, @"\d+").Value, out val);
                            lpCurrentMission += val;
                        }

                        Statistics.LoyaltyPointsForCurrentMission = lpCurrentMission;
                        Statistics.ISKMissionReward = iskFinishedMission;

                        // Complete the mission, close conversation
                        Logging.Logging.Log("Saying [Complete Mission] ISKMissionReward [" + Statistics.ISKMissionReward + "] LoyaltyPointsForCurrentMission [" +
                            Statistics.LoyaltyPointsForCurrentMission + "]");
                        complete.Say();
                        MissionSettings.FactionName = string.Empty;
                        ChangeAgentInteractionState(AgentInteractionState.CloseConversation, true, "Closing conversation");
                    }
                    else
                    {
                        // Apparently someone clicked "accept" already
                        ChangeAgentInteractionState(AgentInteractionState.WaitForMission, true, "Waiting for mission");
                    }
                }
                else if (request != null)
                {
                    if (Purpose == AgentInteractionPurpose.StartMission)
                    {
                        // Request a mission and wait for it
                        Logging.Logging.Log("Saying [Request Mission]");
                        request.Say();
                        ChangeAgentInteractionState(AgentInteractionState.WaitForMission, true, "Waiting for mission");
                    }
                    else
                    {
                        Logging.Logging.Log("Unexpected dialog options: requesting mission since we have that button available");
                        request.Say();
                        ChangeAgentInteractionState(AgentInteractionState.UnexpectedDialogOptions, true);
                    }
                }
                else if (view != null)
                {
                    if (DateTime.UtcNow < _lastAgentAction.AddMilliseconds(Cache.Instance.RandomNumber(1500, 2000))) // was 2000,4000
                    {
                        return;
                    }

                    // View current mission
                    Logging.Logging.Log("Saying [View Mission]");
                    view.Say();
                    _lastAgentAction = DateTime.UtcNow;
                    // No state change
                }
                else if (accept != null || decline != null)
                {
                    if (Purpose == AgentInteractionPurpose.StartMission)
                    {
                        ChangeAgentInteractionState(AgentInteractionState.WaitForMission, true, "Waiting for mission");
                            // Do not say anything, wait for the mission
                    }
                    else
                    {
                        ChangeAgentInteractionState(AgentInteractionState.UnexpectedDialogOptions, true, "Unexpected dialog options");
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        private static void WaitForMission(string module)
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                if (!OpenAgentWindow(module)) return;

                if (!OpenJournalWindow(module)) return;

                MissionSettings.Mission = Cache.Instance.GetAgentMission(AgentId, true);
                if (MissionSettings.Mission == null)
                {
                    if (_waitingOnMission == false)
                    {
                        _waitingOnMissionTimer = DateTime.UtcNow;
                        _waitingOnMission = true;
                    }
                    if (DateTime.UtcNow.Subtract(_waitingOnMissionTimer).TotalSeconds > 30)
                    {
                        Logging.Logging.Log("WaitForMission: Unable to find mission from that agent (yet?) : AgentInteraction.AgentId [" + AgentId + "]");
                        JournalWindow.Close();
                        if (DateTime.UtcNow.Subtract(_waitingOnMissionTimer).TotalSeconds > 120)
                        {
                            Cache.Instance.CloseQuestorCMDLogoff = false;
                            Cache.Instance.CloseQuestorCMDExitGame = true;
                            Cleanup.ReasonToStopQuestor =
                                "AgentInteraction: WaitforMission: Journal would not open/refresh - mission was null: restarting EVE Session";
                            Logging.Logging.Log(Cleanup.ReasonToStopQuestor);
                            Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                        }
                    }

                    return;
                }

                _waitingOnMission = false;
                ChangeAgentInteractionState(AgentInteractionState.PrepareForOfferedMission);
                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        private static void PrepareForOfferedMission()
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                if (MissionSettings.Mission != null)
                {
                    MissionSettings.MissionName = Logging.Logging.FilterPath(MissionSettings.Mission.Name);

                    Logging.Logging.Log("[" + Agent.Name + "] standing toward me is [" + Cache.Instance.AgentEffectiveStandingtoMeText + "], minAgentGreyListStandings: [" +
                        MissionSettings.MinAgentGreyListStandings + "]");
                    var html = Agent.Window.Objective;
                    if (Logging.Logging.DebugAllMissionsOnBlackList || CheckFaction() ||
                        MissionSettings.MissionBlacklist.Any(m => m.ToLower() == MissionSettings.MissionName.ToLower()))
                    {
                        if (Purpose != AgentInteractionPurpose.AmmoCheck)
                        {
                            Logging.Logging.Log("Attempting to Decline mission blacklisted mission [" + MissionSettings.Mission.Name + "] Expires [" +
                                MissionSettings.Mission.ExpiresOn + "]");
                        }

                        if (CheckFaction())
                        {
                            Logging.Logging.Log("Attempting to Decline faction blacklisted mission [" + MissionSettings.Mission.Name + "] Expires [" +
                                MissionSettings.Mission.ExpiresOn + "]");
                        }

                        //
                        // this is tracking declined missions before they are actually declined (bad?)
                        // to fix this wed have to move this tracking stuff to the decline state and pass a reason we are
                        // declining the mission to that process too... not knowing why we are declining is downright silly
                        //
                        MissionSettings.LastBlacklistMissionDeclined = MissionSettings.MissionName;
                        MissionSettings.BlackListedMissionsDeclined++;
                        ChangeAgentInteractionState(AgentInteractionState.DeclineMission);
                        return;
                    }

                    if (Logging.Logging.DebugDecline)
                        Logging.Logging.Log("[" + MissionSettings.MissionName + "] is not on the blacklist and might be on the GreyList we have not checked yet");

                    if (Logging.Logging.DebugAllMissionsOnGreyList ||
                        MissionSettings.MissionGreylist.Any(m => m.ToLower() == MissionSettings.MissionName.ToLower())) //-1.7
                    {
                        if (Cache.Instance.StandingUsedToAccessAgent > MissionSettings.MinAgentGreyListStandings)
                        {
                            MissionSettings.LastGreylistMissionDeclined = MissionSettings.MissionName;
                            MissionSettings.GreyListedMissionsDeclined++;
                            ChangeAgentInteractionState(AgentInteractionState.DeclineMission, false,
                                "Declining GreyListed mission [" + MissionSettings.MissionName + "]");
                            return;
                        }

                        Logging.Logging.Log("Unable to decline GreyListed mission: AgentEffectiveStandings [" + Cache.Instance.StandingUsedToAccessAgent +
                            "] >  MinGreyListStandings [" + MissionSettings.MinAgentGreyListStandings + "]");
                    }
                    else
                    {
                        if (Logging.Logging.DebugDecline)
                            Logging.Logging.Log("[" + MissionSettings.MissionName +
                                "] is not on the GreyList and will likely be run if it is not in lowsec, we have not checked for that yet");
                    }

                    //public bool RouteIsAllHighSec(long solarSystemId, List<long> currentDestination)
                    //Cache.Instance.RouteIsAllHighSec(Cache.Instance.DirectEve.Session.SolarSystemId, );

                    //
                    // at this point we have not yet accepted the mission, thus we do not have the bookmark in people and places
                    // we cannot and should not accept the mission without checking the route first, declining after accepting incurs a much larger penalty to standings
                    //
                    DirectBookmark missionBookmark = MissionSettings.Mission.Bookmarks.FirstOrDefault();
                    if (missionBookmark != null)
                    {
                        Logging.Logging.Log("mission bookmark: System: [" + missionBookmark.LocationId.ToString() + "]");
                    }
                    else
                    {
                        Logging.Logging.Log("There are No Bookmarks Associated with " + MissionSettings.Mission.Name + " yet");
                    }

                    if (html.Contains("The route generated by current autopilot settings contains low security systems!"))
                    {
                        var decline = !Cache.Instance.CourierMission ||
                                      (Cache.Instance.CourierMission && !MissionSettings.AllowNonStorylineCourierMissionsInLowSec);

                        if (decline)
                        {
                            if (Purpose != AgentInteractionPurpose.AmmoCheck)
                            {
                                Logging.Logging.Log("Declining [" + MissionSettings.MissionName + "] because it was taking us through low-sec");
                            }

                            ChangeAgentInteractionState(AgentInteractionState.DeclineMission);
                            return;
                        }
                    }
                    else
                    {
                        if (Logging.Logging.DebugDecline)
                            Logging.Logging.Log("[" + MissionSettings.MissionName + "] is not in lowsec so we will do the mission");
                    }

                    //
                    // if MissionName is a Courier Mission set Cache.Instance.CourierMission = true;
                    //
                    switch (MissionSettings.MissionName)
                    {
                        case "Enemies Abound (2 of 5)": //lvl4 courier
                        case "In the Midst of Deadspace (2 of 5)": //lvl4 courier
                        case "Pot and Kettle - Delivery (3 of 5)": //lvl4 courier
                        case "Technological Secrets (2 of 3)": //lvl4 courier
                        case "New Frontiers - Toward a Solution (3 of 7)": //lvl3 courier
                        case "New Frontiers - Nanite Express (6 of 7)": //lvl3 courier
                        case "Portal to War (3 of 5)": //lvl3 courier
                        case "Guristas Strike - The Interrogation (2 of 10)": //lvl3 courier
                        case "Guristas Strike - Possible Leads (4 of 10)": //lvl3 courier
                        case "Guristas Strike - The Flu Outbreak (6 of 10)": //lvl3 courier
                        case "Angel Strike - The Interrogation (2 of 10)": //lvl3 courier
                        case "Angel Strike - Possible Leads (4 of 10)": //lvl3 courier
                        case "Angel Strike - The Flu Outbreak (6 of 10)": //lvl3 courier
                        case "Interstellar Railroad (2 of 4)": //lvl1 courier
                            Cache.Instance.CourierMission = true;
                            break;

                        default:
                            Cache.Instance.CourierMission = false;
                            break;
                    }

                    if (!ForceAccept)
                    {
                        // Is the mission offered?
                        if (MissionSettings.Mission.State == (int) MissionState.Offered &&
                            (MissionSettings.Mission.Type == "Mining" || MissionSettings.Mission.Type == "Trade" ||
                             (MissionSettings.Mission.Type == "Courier" && Cache.Instance.CourierMission)))
                        {
                            if (!MissionSettings.Mission.Important) //do not decline courier/mining/trade storylines!
                            {
                                ChangeAgentInteractionState(AgentInteractionState.DeclineMission, false, "Declining courier/mining/trade");
                                return;
                            }
                        }
                    }


                    // stats

                    // regex to read current mission lp & isk value
                    // print read values
                    // ([0-9]+)\.([0-9]+)\.([0-9]+) ISK
                    // ([0-9]+) Loyalty Points

                    var lpCurrentMission = 0;
                    var iskFinishedMission = 0;
                    Statistics.ISKMissionReward = 0;
                    Statistics.LoyaltyPointsForCurrentMission = 0;
                    var iskRegex = new Regex(@"([0-9]+)((\.([0-9]+))*) ISK", RegexOptions.Compiled);
                    foreach (Match itemMatch in iskRegex.Matches(html))
                    {
                        var val = 0;
                        int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out val);
                        iskFinishedMission += val;
                    }

                    var lpRegex = new Regex(@"([0-9]+) Loyalty Points", RegexOptions.Compiled);
                    foreach (Match itemMatch in lpRegex.Matches(html))
                    {
                        var val = 0;
                        int.TryParse(Regex.Match(itemMatch.Value, @"\d+").Value, out val);
                        lpCurrentMission += val;
                    }

                    Statistics.LoyaltyPointsTotal = Cache.Instance.Agent.LoyaltyPoints;
                    Statistics.LoyaltyPointsForCurrentMission = lpCurrentMission;
                    Statistics.ISKMissionReward = iskFinishedMission;
                    Cache.Instance.Wealth = Cache.Instance.DirectEve.Me.Wealth;
                    Logging.Logging.Log("ISK finished mission [" + iskFinishedMission + "] LoyalityPoints [" + lpCurrentMission + "]");

                    if (!Cache.Instance.CourierMission)
                    {
                        MissionSettings.loadedAmmo = false;
                        MissionSettings.GetFactionName(html);
                        //MissionSettings.GetDungeonId(html);
                        MissionSettings.SetmissionXmlPath(Logging.Logging.FilterPath(MissionSettings.MissionName));

                        MissionSettings.AmmoTypesToLoad = new Dictionary<Ammo, DateTime>();

                        MissionSettings.ClearMissionSpecificSettings();
                            // we want to clear this every time, not only if the xml exists. else we run into troubles with faction damagetype selection

                        if (File.Exists(MissionSettings.MissionXmlPath))
                        {
                            MissionSettings.LoadMissionXmlData();
                        }
                        else
                        {
                            Logging.Logging.Log("Missing mission xml [" + MissionSettings.MissionName + "] from [" + MissionSettings.MissionXmlPath + "] !!!");
                            MissionSettings.MissionXMLIsAvailable = false;
                            if (MissionSettings.RequireMissionXML)
                            {
                                Logging.Logging.Log("Stopping Questor because RequireMissionXML is true in your character XML settings");
                                Logging.Logging.Log("You will need to create a mission XML for [" + MissionSettings.MissionName + "]");
                                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                                Cache.Instance.Paused = true;
                                return;
                            }
                        }

                        if (!MissionSettings.loadedAmmo)
                        {
                            Logging.Logging.Log("List of Ammo not yet populated. Attempting to choose ammo based on faction");
                            MissionSettings.FactionDamageType = MissionSettings.GetFactionDamageType(html);
                            MissionSettings.LoadCorrectFactionOrMissionAmmo();
                        }

                        if (Purpose == AgentInteractionPurpose.AmmoCheck)
                        {
                            ChangeAgentInteractionState(AgentInteractionState.CloseConversation, false, "Closing conversation");
                            return;
                        }
                    }

                    if (MissionSettings.Mission.State == (int) MissionState.Offered)
                    {
                        ChangeAgentInteractionState(AgentInteractionState.AcceptMission, false, "Accepting mission [" + MissionSettings.MissionName + "]");
                        return;
                    }

                    // If we already accepted the mission, close the conversation
                    ChangeAgentInteractionState(AgentInteractionState.CloseConversation, false,
                        "Mission [" + MissionSettings.MissionName + "] already accepted, Closing conversation");
                    return;
                }

                ChangeAgentInteractionState(AgentInteractionState.Idle, true,
                    "We have no mission yet, how did we get into GetReadyForMission when we do not yet have one to get ready for?");
                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        private static void AcceptMission(string module)
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                if (!OpenAgentWindow(module)) return;

                if (DateTime.UtcNow < _lastAgentActionStateChange.AddMilliseconds(Cache.Instance.RandomNumber(800, 1200))) return; // was 4000

                var responses = Agent.Window.AgentResponses;
                var html = Agent.Window.Briefing;

                if (responses == null || responses.Count == 0)
                    return;

                var accept = responses.FirstOrDefault(r => r.Text.Contains(Accept));
                if (accept == null)
                    return;

                if (DateTime.UtcNow < _lastAgentAction.AddMilliseconds(Cache.Instance.RandomNumber(500, 700))) return; // was 1000

                if (Agent.LoyaltyPoints == -1 && Agent.Level > 1)
                {
                    if (LoyaltyPointCounter < 3)
                    {
                        //Logging.Log("AgentInteraction", "Loyalty Points still -1; retrying", Logging.Red);
                        _lastAgentAction = DateTime.UtcNow;
                        LoyaltyPointCounter++;
                        return;
                    }
                }


                LoyaltyPointCounter = 0;


                Logging.Logging.Log("Saying [Accept]");
                accept.Say();
                ChangeAgentInteractionState(AgentInteractionState.CloseConversation, true, "Closing conversation");
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        private static void DeclineMission(string module)
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                if (!OpenAgentWindow(module)) return;

                if (DateTime.UtcNow < _lastAgentActionStateChange.AddMilliseconds(Cache.Instance.RandomNumber(800, 1200)))
                    return; //enforce a 4 sec wait after each state change

                var responses = Agent.Window.AgentResponses;
                if (responses == null || responses.Count == 0)
                {
                    if (Logging.Logging.DebugDecline)
                        Logging.Logging.Log("if (responses == null || responses.Count == 0) return");
                    return;
                }

                var decline = responses.FirstOrDefault(r => r.Text.Contains(Decline));
                if (decline == null)
                {
                    if (Logging.Logging.DebugDecline)
                        Logging.Logging.Log("if (decline == null) return");
                    return;
                }

                // Check for agent decline timer

                var html = Agent.Window.Briefing;
                if (html.Contains("Declining a mission from this agent within the next"))
                {
                    //this need to divide by 10 was a remnant of the html scrape method we were using before. this can likely be removed now.
                    if (Cache.Instance.StandingUsedToAccessAgent != 0)
                    {
                        if (Cache.Instance.StandingUsedToAccessAgent > 10)
                        {
                            Logging.Logging.Log("if (Cache.Instance.StandingUsedToAccessAgent > 10)");
                            Cache.Instance.StandingUsedToAccessAgent = Cache.Instance.StandingUsedToAccessAgent/10;
                        }

                        if (MissionSettings.MinAgentBlackListStandings > 10)
                        {
                            Logging.Logging.Log("if (Cache.Instance.StandingUsedToAccessAgent > 10)");
                            MissionSettings.MinAgentBlackListStandings = MissionSettings.MinAgentBlackListStandings/10;
                        }

                        Logging.Logging.Log("Agent decline timer detected. Current standings: " + Math.Round(Cache.Instance.StandingUsedToAccessAgent, 2) +
                            ". Minimum standings: " + Math.Round(MissionSettings.MinAgentBlackListStandings, 2));
                    }

                    var hourRegex = new Regex("\\s(?<hour>\\d+)\\shour");
                    var minuteRegex = new Regex("\\s(?<minute>\\d+)\\sminute");
                    var hourMatch = hourRegex.Match(html);
                    var minuteMatch = minuteRegex.Match(html);
                    var hours = 0;
                    var minutes = 0;
                    if (hourMatch.Success)
                    {
                        var hourValue = hourMatch.Groups["hour"].Value;
                        hours = Convert.ToInt32(hourValue);
                    }
                    if (minuteMatch.Success)
                    {
                        var minuteValue = minuteMatch.Groups["minute"].Value;
                        minutes = Convert.ToInt32(minuteValue);
                    }

                    var secondsToWait = ((hours*3600) + (minutes*60) + 60);


                    //
                    // standings are below the blacklist minimum
                    // (any lower and we might lose access to this agent)
                    // and no other agents are NOT available (or are also in cool-down)
                    //
                    if ((MissionSettings.WaitDecline && Cache.Instance.AllAgentsStillInDeclineCoolDown))
                    {
                        //
                        // if true we ALWAYS wait (or switch agents?!?)
                        //
                        Logging.Logging.Log("Waiting " + (secondsToWait / 60) + " minutes to try decline again because waitDecline setting is set to true");
                        CloseConversation();
                        ChangeAgentInteractionState(AgentInteractionState.StartConversation, true);
                        return;
                    }

                    //
                    // if WaitDecline is false we only wait if standings are below our configured minimums
                    //
                    if (Cache.Instance.StandingUsedToAccessAgent < MissionSettings.MinAgentBlackListStandings)
                    {
                        if (Logging.Logging.DebugDecline)
                            Logging.Logging.Log("if (Cache.Instance.StandingUsedToAccessAgent <= Settings.Instance.MinAgentBlackListStandings)");

                        //TODO - We should probably check if there are other agents who's effective standing is above the minAgentBlackListStanding.
                        if (Cache.Instance.AllAgentsStillInDeclineCoolDown)
                        {
                            //
                            // wait.
                            //
                            Logging.Logging.Log("Current standings [" + Math.Round(Cache.Instance.StandingUsedToAccessAgent, 2) + "] are below configured minimum of [" +
                                MissionSettings.MinAgentBlackListStandings + "].  Waiting " + (secondsToWait / 60) +
                                " minutes to try decline again because no other agents were avail for use.");
                            CloseConversation();
                            ChangeAgentInteractionState(AgentInteractionState.StartConversation, true);
                            return;
                        }
                    }

                    Logging.Logging.Log("Current standings [" + Math.Round(Cache.Instance.StandingUsedToAccessAgent, 2) + "] is above our configured minimum [" +
                        MissionSettings.MinAgentBlackListStandings + "].  Declining [" + MissionSettings.Mission.Name + "] note: WaitDecline is false");
                }

                //
                // this closes the conversation, blacklists the agent for this session and goes back to base.
                //
                if (_States.CurrentStorylineState == StorylineState.DeclineMission || _States.CurrentStorylineState == StorylineState.AcceptMission)
                {
                    MissionSettings.Mission.RemoveOffer();
                    if (Settings.Instance.DeclineStorylinesInsteadofBlacklistingfortheSession)
                    {
                        Logging.Logging.Log("Saying [Decline]");
                        decline.Say();
                        _lastAgentAction = DateTime.UtcNow;
                    }
                    else
                    {
                        Logging.Logging.Log("Storyline: Storylines are not set to be declined, thus we will add this agent to the blacklist for this session.");
                        Cache.Instance.AgentBlacklist.Add(Cache.Instance.CurrentStorylineAgentId);
                        Statistics.MissionCompletionErrors = 0;
                    }

                    _States.CurrentStorylineState = StorylineState.Idle;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ChangeAgentInteractionState(AgentInteractionState.Idle, true);
                    return;
                }

                // Decline and request a new mission
                Logging.Logging.Log("Saying [Decline]");
                decline.Say();
                Statistics.MissionCompletionErrors = 0;
                ChangeAgentInteractionState(AgentInteractionState.StartConversation, true, "Replying to agent");
                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        public static bool CheckFaction()
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return false;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                MissionSettings.ClearFactionSpecificSettings();
                var agentWindow = Agent.Window;
                var html = agentWindow.Objective;
                var logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");
                var logoMatch = logoRegex.Match(html);
                if (logoMatch.Success)
                {
                    var logo = logoMatch.Groups["factionlogo"].Value;

                    // Load faction xml
                    var xml = XDocument.Load(Path.Combine(Settings.Instance.Path, "Factions.xml"));
                    if (xml.Root != null)
                    {
                        var faction = xml.Root.Elements("faction").FirstOrDefault(f => (string) f.Attribute("logo") == logo);

                        MissionSettings.FactionName = "Default";
                        if (faction != null)
                        {
                            var factionName = ((string) faction.Attribute("name"));
                            MissionSettings.FactionName = factionName;
                            Logging.Logging.Log("Mission enemy faction: " + factionName);
                            if (MissionSettings.FactionBlacklist.Any(m => m.ToLower() == factionName.ToLower()))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            Logging.Logging.Log("Faction fitting: Missing Factions.xml :aborting faction fittings");
                        }
                    }
                }
                else
                {
                    MissionSettings.FactionName = "Default";
                }
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }

        public static void CloseConversation()
        {
            try
            {
                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                    return;
                }

                var Agent = Cache.Instance.Agent;
                var AgentId = Agent.AgentId;

                if (Agent != null)
                {
                    var agentWindow = Agent.Window;
                    if (agentWindow == null)
                    {
                        Logging.Logging.Log("Done");
                        ChangeAgentInteractionState(AgentInteractionState.Done);
                    }

                    if (agentWindow != null && agentWindow.IsReady)
                    {
                        if (DateTime.UtcNow < _lastAgentAction.AddMilliseconds(Cache.Instance.RandomNumber(1000, 1500))) // was 1500,2000
                        {
                            //Logging.Log("AgentInteraction.CloseConversation", "will continue in [" + Math.Round(_nextAgentAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "]sec", Logging.Yellow);
                            return;
                        }

                        Logging.Logging.Log("Attempting to close Agent Window");
                        _lastAgentAction = DateTime.UtcNow;
                        agentWindow.Close();
                    }

                    MissionSettings.Mission = Cache.Instance.GetAgentMission(AgentId, true);
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        public static bool OpenAgentWindow(string module)
        {
            if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
            {
                Logging.Logging.Log("if(Cache.Instance.Agent == null || Cache.Instance.Agent.IsValid)");
                return false;
            }

            var Agent = Cache.Instance.Agent;
            var AgentId = Agent.AgentId;

            var _delayInSeconds = 0;
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(10) && !Cache.Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (Agent.Window == null)
            {
                if (DateTime.UtcNow < _lastAgentAction.AddMilliseconds(Cache.GetRandom(1500, 1700))) // was 3000 ms
                {
                    if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                        Logging.Logging.Log("if (DateTime.UtcNow < _lastAgentAction.AddSeconds(3))");
                    return false;
                }

                if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                    Logging.Logging.Log("Attempting to Interact with the agent named [" + Agent.Name + "] in [" + Cache.Instance.DirectEve.GetLocationName(Agent.SolarSystemId) +
                        "]");
                Agent.InteractWith();
                _delayInSeconds = 4;
                _lastAgentAction = DateTime.UtcNow;
                Statistics.LogWindowActionToWindowLog("AgentWindow", "Opening AgentWindow");
                return false;
            }

            if (!Agent.Window.IsReady)
            {
                return false;
            }

            if (Agent.Window.IsReady && AgentId == Agent.AgentId && DateTime.UtcNow > _agentWindowLastReady.AddSeconds(_delayInSeconds + 2))
            {
                _agentWindowLastReady = DateTime.UtcNow;
                if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                    Logging.Logging.Log("AgentWindow is ready: set _agentWindowLastReady = DateTime.UtcNow;");
            }

            if (DateTime.UtcNow < _agentWindowLastReady.AddSeconds(10) && DateTime.UtcNow > _agentWindowLastReady.AddSeconds(_delayInSeconds))
            {
                if (Logging.Logging.DebugAgentInteractionReplyToAgent)
                    Logging.Logging.Log("AgentWindow is ready: it has been more than 2 seco0nds since the agent window was ready. continue.");
                return true;
            }

            return false;
        }

        public static bool OpenJournalWindow(string module)
        {
            if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(20) && !Cache.Instance.InSpace)
                // we wait 20 seconds after we last thought we were in space before trying to do anything in station
            {
                return false;
            }

            if (Cache.Instance.InStation)
            {
                JournalWindow = Cache.Instance.GetWindowByName("journal");

                // Is the journal window open?
                if (JournalWindow == null)
                {
                    if (DateTime.UtcNow < Time.Instance.NextWindowAction)
                    {
                        return false;
                    }

                    // No, command it to open
                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                    Statistics.LogWindowActionToWindowLog("JournalWindow", "Opening JournalWindow");
                    Time.Instance.NextWindowAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 4));
                    Logging.Logging.Log("Opening Journal Window: waiting [" + Math.Round(Time.Instance.NextWindowAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    return false;
                }

                return true; //if JournalWindow is not null then the window must be open.
            }

            return false;
        }

        public static bool ChangeAgentInteractionState(AgentInteractionState _AgentInteractionState, bool WaitAMomentbeforeNextAction = false,
            string LogMessage = null)
        {
            try
            {
                //
                // if _ArmStateToSet matches also do this stuff...
                //

                Logging.Logging.Log("New state [" + _AgentInteractionState.ToString() + "]");

                switch (_AgentInteractionState)
                {
                    case AgentInteractionState.Done:
                        break;

                    case AgentInteractionState.Idle:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }

            try
            {
                _lastAgentActionStateChange = DateTime.UtcNow;
                if (_States.CurrentAgentInteractionState != _AgentInteractionState)
                {
                    _States.CurrentAgentInteractionState = _AgentInteractionState;
                    if (WaitAMomentbeforeNextAction)
                    {
                        _lastAgentAction = DateTime.UtcNow;
                    }

                    // else AgentInteraction.ProcessState(); // why the fuck do we call this again :/
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }

        public static void ProcessState()
        {
            try
            {
                if (DateTime.UtcNow.Subtract(_lastAgentInteractionPulse).TotalMilliseconds < 1000)
                {
                    return;
                }

                _lastAgentInteractionPulse = DateTime.UtcNow;

                if (!Cache.Instance.InStation)
                {
                    return;
                }

                if (Cache.Instance.InSpace)
                {
                    return;
                }

                if (!Cache.Instance.Windows.Any())
                {
                    return;
                }

                foreach (var window in Cache.Instance.Windows)
                {
                    if (window.IsModal)
                    {
                        var needHumanIntervention = false;
                        var sayyes = false;

                        if (!string.IsNullOrEmpty(window.Html))
                        {
                            //errors that are repeatable and unavoidable even after a restart of eve/questor
                            needHumanIntervention |= window.Html.Contains("One or more mission objectives have not been completed");
                            needHumanIntervention |= window.Html.Contains("Please check your mission journal for further information");
                            needHumanIntervention |= window.Html.Contains("You have to be at the drop off location to deliver the items in person");

                            sayyes |= window.Html.Contains("objectives requiring a total capacity");
                            sayyes |= window.Html.Contains("your ship only has space for");
                        }

                        if (sayyes)
                        {
                            Logging.Logging.Log("Found a window that needs 'yes' chosen...");
                            Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                            window.AnswerModal("Yes");
                            continue;
                        }

                        if (needHumanIntervention)
                        {
                            Statistics.MissionCompletionErrors++;
                            Statistics.LastMissionCompletionError = DateTime.UtcNow;

                            Logging.Logging.Log("This window indicates an error completing a mission: [" + Statistics.MissionCompletionErrors +
                                "] errors already we will stop questor and halt restarting when we reach 4");
                            window.Close();

                            if (Statistics.MissionCompletionErrors > 4 && Cache.Instance.InStation)
                            {
                                if (MissionSettings.MissionXMLIsAvailable)
                                {
                                    Logging.Logging.Log("ERROR: Mission XML is available for [" + MissionSettings.MissionName +
                                        "] but we still did not complete the mission after 3 tries! - ERROR!");
                                    Settings.Instance.AutoStart = false;
                                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                                }
                                else
                                {
                                    Logging.Logging.Log("ERROR: Mission XML is missing for [" + MissionSettings.MissionName +
                                        "] and we we unable to complete the mission after 3 tries! - ERROR!");
                                    Settings.Instance.AutoStart = false;
                                        //we purposely disable autostart so that when we quit eve and questor here it stays closed until manually restarted as this error is fatal (and repeating)
                                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                                }
                            }

                            continue;
                        }
                    }

                    continue;
                }

                switch (_States.CurrentAgentInteractionState)
                {
                    case AgentInteractionState.Idle:
                        break;

                    case AgentInteractionState.Done:
                        break;

                    case AgentInteractionState.ChangeAgent:
                        Logging.Logging.Log("Change Agent");
                        break;

                    case AgentInteractionState.StartConversation:
                        StartConversation("AgentInteraction.StartConversation");
                        break;

                    case AgentInteractionState.ReplyToAgent:
                        ReplyToAgent("AgentInteraction.ReplyToAgent");
                        break;

                    case AgentInteractionState.WaitForMission:
                        WaitForMission("AgentInteraction.WaitForMission");
                        break;

                    case AgentInteractionState.PrepareForOfferedMission:
                        PrepareForOfferedMission();
                        break;

                    case AgentInteractionState.AcceptMission:
                        AcceptMission("AgentInteraction.AcceptMission");
                        break;

                    case AgentInteractionState.DeclineMission:
                        DeclineMission("AgentInteraction.DeclineMission");
                        break;

                    case AgentInteractionState.CloseConversation:
                        CloseConversation();
                        break;
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }
    }
}