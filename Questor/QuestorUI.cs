using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using PythonBrowser;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;
using Questor.Properties;
using QuestorManager;
using Settings = Questor.Modules.Lookup.Settings;

namespace Questor
{
    extern alias Ut;


    public partial class QuestorUI : Form
    {
        static object Lock = new object();
        private string _lastLogLine = string.Empty;
        private DateTime _nextScheduleUpdate = DateTime.UtcNow;
        private DateTime _nextUIDataRefresh = DateTime.UtcNow;

        public QuestorUI()
        {
            try
            {
                if (Logging.EnableVisualStyles) Application.EnableVisualStyles();
                InitializeComponent();
                PopulateStateComboBoxes();
                PopulateBehaviorStateComboBox();

//				if (Logging.DebugAttachVSDebugger)
//				{
//					if (!System.Diagnostics.Debugger.IsAttached)
//					{
//						Logging.Log("QuestorUI", "VS Debugger is not yet attached: System.Diagnostics.Debugger.Launch()", Logging.Teal);
//						System.Diagnostics.Debugger.Launch();
//					}
//				}

                var form = new QuestorManagerUI(this);
                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Dock = DockStyle.Fill;


                foreach (TabPage tab in tabControlMain.TabPages)
                {
                    if (tab.Text.ToLower().Equals("questormanager"))
                    {
                        tab.Controls.Add(form);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }
        }

        private void QuestorfrmMainFormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                if (Logging.DebugUI) Logging.Log("QuestorUI", "QuestorfrmMainFormClosed", Logging.White);
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }

            Logging.OnMessage -= AddLog;
        }

        private void PopulateStateComboBoxes()
        {
            try
            {
                if (Logging.DebugUI) Logging.Log("QuestorUI", "PopulateStateComboBoxes", Logging.White);
                QuestorStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(QuestorState)))
                    QuestorStateComboBox.Items.Add(text);

                // ComboxBoxes on main windows (at top)
                DamageTypeComboBox.Items.Clear();
                DamageTypeComboBox.Items.Add("Auto");
                foreach (var text in Enum.GetNames(typeof(DamageType)))
                {
                    DamageTypeComboBox.Items.Add(text);
                }

                // middle column
                PanicStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(PanicState)))
                {
                    PanicStateComboBox.Items.Add(text);
                }

                CombatStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(CombatState)))
                {
                    CombatStateComboBox.Items.Add(text);
                }

                DronesStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(DroneState)))
                {
                    DronesStateComboBox.Items.Add(text);
                }

                CleanupStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(CleanupState)))
                {
                    CleanupStateComboBox.Items.Add(text);
                }

                LocalWatchStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(LocalWatchState)))
                {
                    LocalWatchStateComboBox.Items.Add(text);
                }

                SalvageStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(SalvageState)))
                {
                    SalvageStateComboBox.Items.Add(text);
                }

                // right column
                CombatMissionCtrlStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(CombatMissionCtrlState)))
                {
                    CombatMissionCtrlStateComboBox.Items.Add(text);
                }

                StorylineStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(StorylineState)))
                {
                    StorylineStateComboBox.Items.Add(text);
                }

                ArmStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(ArmState)))
                {
                    ArmStateComboBox.Items.Add(text);
                }

                UnloadStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(UnloadLootState)))
                {
                    UnloadStateComboBox.Items.Add(text);
                }

                TravelerStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(TravelerState)))
                {
                    TravelerStateComboBox.Items.Add(text);
                }

                AgentInteractionStateComboBox.Items.Clear();
                foreach (var text in Enum.GetNames(typeof(AgentInteractionState)))
                {
                    AgentInteractionStateComboBox.Items.Add(text);
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }
        }

        private void PopulateMissionLists()
        {
            try
            {
                BlacklistedMissionstextbox.Text = "";
                foreach (var blacklistedmission in MissionSettings.MissionBlacklist)
                {
                    BlacklistedMissionstextbox.AppendText(blacklistedmission + "\r\n");
                }

                GreyListedMissionsTextBox.Text = "";
                foreach (var GreyListedMission in MissionSettings.MissionGreylist)
                {
                    GreyListedMissionsTextBox.AppendText(GreyListedMission + "\r\n");
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }
        }

        private void RefreshInfoDisplayedInUI()
        {
            if (DateTime.UtcNow > _nextUIDataRefresh &&
                DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15))
            {
                _nextUIDataRefresh = DateTime.UtcNow.AddMilliseconds(1000);
                try
                {
                    if (Time.Instance.LastInSpace.AddMilliseconds(1000) > DateTime.UtcNow)
                    {
                        CurrentTimeData1.Text = DateTime.UtcNow.ToLongTimeString();
                        CurrentTimeData2.Text = DateTime.UtcNow.ToLongTimeString();
                        NextOpenContainerInSpaceActionData.Text =
                            Time.Instance.NextOpenContainerInSpaceAction.ToLongTimeString();
                        NextOpenLootContainerActionData.Text =
                            Time.Instance.NextOpenLootContainerAction.ToLongTimeString();
                        NextDroneBayActionData.Text = Time.Instance.NextDroneBayAction.ToLongTimeString();
                        NextOpenHangarActionData.Text = Time.Instance.NextOpenHangarAction.ToLongTimeString();
                        NextOpenCargoActionData.Text = Time.Instance.NextOpenCargoAction.ToLongTimeString();
                        LastActionData.Text = "";
                        NextArmActionData.Text = Time.Instance.NextArmAction.ToLongTimeString();
                        NextSalvageActionData.Text = Time.Instance.NextSalvageAction.ToLongTimeString();
                        NextLootActionData.Text = Time.Instance.NextLootAction.ToLongTimeString();
                        LastJettisonData.Text = Time.Instance.LastJettison.ToLongTimeString();
                        NextActivateSupportModulesData.Text = Time.Instance.NextActivateModules.ToLongTimeString();
                        NextApproachActionData.Text = Time.Instance.NextApproachAction.ToLongTimeString();
                        NextOrbitData.Text = Time.Instance.NextOrbit.ToLongTimeString();
                        NextWarpToData.Text = Time.Instance.NextWarpAction.ToLongTimeString();
                        NextTravelerActionData.Text = Time.Instance.NextTravelerAction.ToLongTimeString();
                        NextTargetActionData.Text = Time.Instance.NextTargetAction.ToLongTimeString();
                        NextActivateActionData.Text = Time.Instance.NextActivateAction.ToLongTimeString();
                        NextAlignData.Text = Time.Instance.NextAlign.ToLongTimeString();
                        NextUndockActionData.Text = Time.Instance.NextUndockAction.ToLongTimeString();
                        NextDockActionData.Text = Time.Instance.NextDockAction.ToLongTimeString();
                        NextDroneRecallData.Text = Time.Instance.NextDroneRecall.ToLongTimeString();
                        NextStartupActionData.Text = Time.Instance.NextStartupAction.ToLongTimeString();
                        LastSessionChangeData.Text = Time.Instance.LastSessionChange.ToLongTimeString();
                        MissionsThisSessionData.Text =
                            MissionSettings.MissionsThisSession.ToString(CultureInfo.InvariantCulture);
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.DebugUI)
                        Logging.Log("QuestorUI",
                            "RefreshInfoDisplayedInUI: unable to update all UI labels: exception was [" + ex.Message +
                            "]", Logging.Teal);
                }
            }
        }

        private void PopulateBehaviorStateComboBox()
        {
            try
            {
                if (Logging.DebugUI) Logging.Log("QuestorUI", "PopulateBehaviorStateComboBox", Logging.White);

                if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                {
                    BehaviorComboBox.Items.Clear();
                    foreach (var text in Enum.GetNames(typeof(CombatMissionsBehaviorState)))
                    {
                        BehaviorComboBox.Items.Add(text);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }
        }

        private void UpdateUiTick(object sender, EventArgs e)
        {
            try
            {
                // The if's in here stop the UI from flickering
                var text = "Questor";
                if (Settings.Instance.CharacterName != string.Empty)
                {
                    text = "Questor [" + Settings.Instance.CharacterName + "]";
                }
                if (Settings.Instance.CharacterName != string.Empty && Cache.Instance.Wealth > 10000000)
                {
                    text = "Questor [" + Settings.Instance.CharacterName + "][" +
                           String.Format("{0:0,0}", Cache.Instance.Wealth/1000000) + "mil isk]";
                }

                if (Text != text)
                    Text = text;

                lastSessionisreadyData.Text = "[" +
                                              Math.Round(
                                                  DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady)
                                                      .TotalSeconds, 0) + "] sec ago";
                LastFrameData.Text = "[" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds, 0) +
                                     "] sec ago";
                lastInSpaceData.Text = "[" +
                                       Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastInSpace).TotalSeconds, 0) +
                                       "] sec ago";
                lastInStationData.Text = "[" +
                                         Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastInStation).TotalSeconds,
                                             0) + "] sec ago";
                lastKnownGoodConnectedTimeData.Text = "[" +
                                                      Math.Round(
                                                          DateTime.UtcNow.Subtract(
                                                              Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0) +
                                                      "] min ago";
                dataStopTimeSpecified.Text = Time.Instance.StopTimeSpecified.ToString();

                if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
                {
                    if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
                    {
                        _States.CurrentQuestorState = QuestorState.CloseQuestor;
                        Cleanup.CloseQuestor("Quitting");
                    }
                }

                RefreshInfoDisplayedInUI();

                // Left Group
                if ((string) QuestorStateComboBox.SelectedItem != _States.CurrentQuestorState.ToString() &&
                    !QuestorStateComboBox.DroppedDown)
                {
                    QuestorStateComboBox.SelectedItem = _States.CurrentQuestorState.ToString();
                }

                if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                {
                    if ((string) BehaviorComboBox.SelectedItem != _States.CurrentCombatMissionBehaviorState.ToString() &&
                        !BehaviorComboBox.DroppedDown)
                    {
                        BehaviorComboBox.SelectedItem = _States.CurrentCombatMissionBehaviorState.ToString();
                    }
                }

                // Middle group
                if ((string) PanicStateComboBox.SelectedItem != _States.CurrentPanicState.ToString() &&
                    !PanicStateComboBox.DroppedDown)
                {
                    PanicStateComboBox.SelectedItem = _States.CurrentPanicState.ToString();
                }

                if ((string) CombatStateComboBox.SelectedItem != _States.CurrentCombatState.ToString() &&
                    !CombatStateComboBox.DroppedDown)
                {
                    CombatStateComboBox.SelectedItem = _States.CurrentCombatState.ToString();
                }

                if ((string) DronesStateComboBox.SelectedItem != _States.CurrentDroneState.ToString() &&
                    !DronesStateComboBox.DroppedDown)
                {
                    DronesStateComboBox.SelectedItem = _States.CurrentDroneState.ToString();
                }

                if ((string) CleanupStateComboBox.SelectedItem != _States.CurrentCleanupState.ToString() &&
                    !CleanupStateComboBox.DroppedDown)
                {
                    CleanupStateComboBox.SelectedItem = _States.CurrentCleanupState.ToString();
                }

                if ((string) LocalWatchStateComboBox.SelectedItem != _States.CurrentLocalWatchState.ToString() &&
                    !LocalWatchStateComboBox.DroppedDown)
                {
                    LocalWatchStateComboBox.SelectedItem = _States.CurrentLocalWatchState.ToString();
                }

                if ((string) SalvageStateComboBox.SelectedItem != _States.CurrentSalvageState.ToString() &&
                    !SalvageStateComboBox.DroppedDown)
                {
                    SalvageStateComboBox.SelectedItem = _States.CurrentSalvageState.ToString();
                }

                // Right Group
                if ((string) CombatMissionCtrlStateComboBox.SelectedItem !=
                    _States.CurrentCombatMissionCtrlState.ToString() && !CombatMissionCtrlStateComboBox.DroppedDown)
                {
                    CombatMissionCtrlStateComboBox.SelectedItem = _States.CurrentCombatMissionCtrlState.ToString();
                }

                if ((string) StorylineStateComboBox.SelectedItem != _States.CurrentStorylineState.ToString() &&
                    !StorylineStateComboBox.DroppedDown)
                {
                    StorylineStateComboBox.SelectedItem = _States.CurrentStorylineState.ToString();
                }

                if ((string) ArmStateComboBox.SelectedItem != _States.CurrentArmState.ToString() &&
                    !ArmStateComboBox.DroppedDown)
                {
                    ArmStateComboBox.SelectedItem = _States.CurrentArmState.ToString();
                }

                if ((string) UnloadStateComboBox.SelectedItem != _States.CurrentUnloadLootState.ToString() &&
                    !UnloadStateComboBox.DroppedDown)
                {
                    UnloadStateComboBox.SelectedItem = _States.CurrentUnloadLootState.ToString();
                }

                if ((string) TravelerStateComboBox.SelectedItem != _States.CurrentTravelerState.ToString() &&
                    !TravelerStateComboBox.DroppedDown)
                {
                    TravelerStateComboBox.SelectedItem = _States.CurrentTravelerState.ToString();
                }

                if ((string) AgentInteractionStateComboBox.SelectedItem !=
                    _States.CurrentAgentInteractionState.ToString() && !AgentInteractionStateComboBox.DroppedDown)
                {
                    AgentInteractionStateComboBox.SelectedItem = _States.CurrentAgentInteractionState.ToString();
                }

                if (AutoStartCheckBox.Checked != Settings.Instance.AutoStart)
                {
                    AutoStartCheckBox.Checked = Settings.Instance.AutoStart;
                }

                if (PauseCheckBox.Checked != Cache.Instance.Paused)
                {
                    PauseCheckBox.Checked = Cache.Instance.Paused;
                }

                if (Disable3DCheckBox.Checked != Settings.Instance.Disable3D)
                {
                    Disable3DCheckBox.Checked = Settings.Instance.Disable3D;
                }

                if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission &&
                    Cache.Instance.CurrentPocketAction != null)
                {
                    var newlblCurrentPocketActiontext = "[ " + Cache.Instance.CurrentPocketAction + " ] Action";
                    if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    {
                        lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
                    }
                }
                else if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Salvage ||
                         _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoSalvageBookmark ||
                         _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.SalvageNextPocket ||
                         _States.CurrentCombatMissionBehaviorState ==
                         CombatMissionsBehaviorState.BeginAfterMissionSalvaging ||
                         _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.SalvageUseGate)
                {
                    const string newlblCurrentPocketActiontext = "[ " + "After Mission Salvaging" + " ] ";
                    if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    {
                        lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
                    }
                }
                else
                {
                    const string newlblCurrentPocketActiontext = "[ ]";
                    if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    {
                        lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
                    }
                }

                if (_States.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.Idle &&
                    _States.CurrentQuestorState != QuestorState.Idle)
                {
                    if (!String.IsNullOrEmpty(MissionSettings.MissionName))
                    {
                        if (!String.IsNullOrEmpty(MissionSettings.MissionsPath))
                        {
                            if (File.Exists(MissionSettings.MissionXmlPath))
                            {
                                var newlblCurrentMissionInfotext = "[ " + MissionSettings.MissionName + " ][ " +
                                                                   Math.Round(
                                                                       DateTime.UtcNow.Subtract(
                                                                           Statistics.StartedMission).TotalMinutes, 0) +
                                                                   " min][ #" + Statistics.MissionsThisSession + " ]";
                                if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                                {
                                    lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                                }
                            }
                            else
                            {
                                var newlblCurrentMissionInfotext = "[ " + MissionSettings.MissionName + " ][ " +
                                                                   Math.Round(
                                                                       DateTime.UtcNow.Subtract(
                                                                           Statistics.StartedMission).TotalMinutes, 0) +
                                                                   " min][ #" + Statistics.MissionsThisSession + " ]";
                                if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                                {
                                    lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                                }
                            }
                        }
                    }
                    else if (String.IsNullOrEmpty(MissionSettings.MissionName))
                    {
                        lblCurrentMissionInfo.Text = Resources.QuestorfrmMain_UpdateUiTick_No_Mission_Selected_Yet;
                    }
                }

                var extraWaitSeconds = 0;
                if (!Debugger.IsAttached)
                    //do not restart due to no frames or Session.Isready aging if a debugger is attached until it reaches absurdity...
                {
                    extraWaitSeconds = 60;
                }


                if (!Cache.Instance.Paused)
                {
                    if (DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds >
                        (Time.Instance.NoFramesRestart_seconds + extraWaitSeconds) &&
                        DateTime.UtcNow.Subtract(Cache.EVEAccountLoginStarted).TotalSeconds > 300)
                    {
                        if (DateTime.UtcNow.Subtract(Time.Instance.LastLogMessage).TotalSeconds > 30)
                        {
                            Logging.Log("QuestorUI",
                                "The Last UI Frame Drawn by EVE was [" +
                                Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds, 0) +
                                "] seconds ago! This is bad. - Exiting EVE", Logging.Red);
                            Cleanup.ReasonToStopQuestor = "The Last UI Frame Drawn by EVE was [" +
                                                          Math.Round(
                                                              DateTime.UtcNow.Subtract(Time.Instance.LastFrame)
                                                                  .TotalSeconds, 0) +
                                                          "] seconds ago! This is bad. - Exiting EVE";
                            Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                            Application.Exit();
                        }
                    }

                    if (DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady).TotalSeconds >
                        (Time.Instance.NoSessionIsReadyRestart_seconds + extraWaitSeconds) &&
                        DateTime.UtcNow.Subtract(Cache.EVEAccountLoginStarted).TotalSeconds > 210)
                    {
                        if (DateTime.UtcNow.Subtract(Time.Instance.LastLogMessage).TotalSeconds > 60)
                        {
                            Logging.Log("QuestorUI",
                                "The Last Session.IsReady = true was [" +
                                Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady).TotalSeconds, 0) +
                                "] seconds ago! This is bad. - Exiting EVE", Logging.Red);
                            Cleanup.ReasonToStopQuestor = "The Last Session.IsReady = true was [" +
                                                          Math.Round(
                                                              DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady)
                                                                  .TotalSeconds, 0) +
                                                          "] seconds ago! This is bad. - Exiting EVE";
                            Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                            Application.Exit();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }
        }

        private void PauseCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Cache.Instance.Paused = PauseCheckBox.Checked;
        }

        private void Disable3DCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.Disable3D = Disable3DCheckBox.Checked;
        }

        private void DisableMouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs) e).Handled = true;
        }

        private void ButtonOpenLogDirectoryClick(object sender, EventArgs e)
        {
            Process.Start(Logging.Logpath);
        }

        private void ButtonOpenMissionXmlClick(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Launching [" + MissionSettings.MissionXmlPath + "]", Logging.White);
            Process.Start(MissionSettings.MissionXmlPath);
        }

        private void QuestorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentQuestorState = (QuestorState) Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
            PopulateBehaviorStateComboBox();
            PopulateMissionLists();
        }

        private void BehaviorComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
            {
                _States.CurrentCombatMissionBehaviorState =
                    (CombatMissionsBehaviorState) Enum.Parse(typeof(CombatMissionsBehaviorState), BehaviorComboBox.Text);
            }
            try
            {
                AgentNameData.Text = Cache.Instance.CurrentAgent;
                AgentEffectiveStandingsData.Text = Cache.Instance.AgentEffectiveStandingtoMeText;

                // greylist info
                MinAgentGreyListStandingsData.Text =
                    Math.Round(MissionSettings.MinAgentGreyListStandings, 2).ToString(CultureInfo.InvariantCulture);
                LastGreylistedMissionDeclinedData.Text = MissionSettings.LastGreylistMissionDeclined;
                greylistedmissionsdeclineddata.Text =
                    MissionSettings.GreyListedMissionsDeclined.ToString(CultureInfo.InvariantCulture);

                // blacklist info
                MinAgentBlackListStandingsData.Text =
                    Math.Round(MissionSettings.MinAgentBlackListStandings, 2).ToString(CultureInfo.InvariantCulture);
                LastBlacklistedMissionDeclinedData.Text = MissionSettings.LastBlacklistMissionDeclined;
                blacklistedmissionsdeclineddata.Text =
                    MissionSettings.BlackListedMissionsDeclined.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                if (Logging.DebugExceptions || (Logging.DebugUI))
                    Logging.Log("QuestorUI", "Exception was [" + ex.Message + "]", Logging.Teal);
            }
        }

        private void PanicStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentPanicState = (PanicState) Enum.Parse(typeof(PanicState), PanicStateComboBox.Text);
        }

        private void CombatStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCombatState = (CombatState) Enum.Parse(typeof(CombatState), CombatStateComboBox.Text);
        }

        private void DronesStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentDroneState = (DroneState) Enum.Parse(typeof(DroneState), DronesStateComboBox.Text);
        }

        private void CleanupStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCleanupState = (CleanupState) Enum.Parse(typeof(CleanupState), CleanupStateComboBox.Text);
        }

        private void LocalWatchStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentLocalWatchState =
                (LocalWatchState) Enum.Parse(typeof(LocalWatchState), LocalWatchStateComboBox.Text);
        }

        private void SalvageStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentSalvageState = (SalvageState) Enum.Parse(typeof(SalvageState), SalvageStateComboBox.Text);
        }

        private void StorylineStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentStorylineState =
                (StorylineState) Enum.Parse(typeof(StorylineState), StorylineStateComboBox.Text);
        }

        private void ArmStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentArmState = (ArmState) Enum.Parse(typeof(ArmState), ArmStateComboBox.Text);
        }

        private void UnloadStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentUnloadLootState =
                (UnloadLootState) Enum.Parse(typeof(UnloadLootState), UnloadStateComboBox.Text);
        }

        private void TravelerStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentTravelerState = (TravelerState) Enum.Parse(typeof(TravelerState), TravelerStateComboBox.Text);
        }

        private void AgentInteractionStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentAgentInteractionState =
                (AgentInteractionState) Enum.Parse(typeof(AgentInteractionState), AgentInteractionStateComboBox.Text);
        }

        private void AutoStartCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.AutoStart = AutoStartCheckBox.Checked;
        }

        void QuestorUIShown(object sender, EventArgs e)
        {
            if ((Cache.Instance.WCFClient.GetPipeProxy.IsMainFormMinimized() &&
                 Cache.Instance.WCFClient.GetPipeProxy.GetEVESettings().ToggleHideShowOnMinimize) ||
                Cache.Instance.WCFClient.GetPipeProxy.GetEveAccount(Cache.Instance.EveAccount.CharacterName).Hidden)
            {
                Logging.Log("QuestorUIShown", "Hiding form.");
                BeginInvoke(new MethodInvoker(delegate { Hide(); }));
//				Cache.Instance.WCFClient.GetPipeProxy.CallHideEveWindows(Cache.Instance.EveAccount.CharacterName);
            }
        }

        void Button1Click(object sender, EventArgs e)
        {
            var frm = new PythonBrowserFrm();
            frm.Show();
        }

        void AddLog(string msg)
        {
            try
            {
                lock (Lock)
                {
                    if (logListbox.Items.Count >= 1000)
                    {
                        logListbox.Items.Clear();
                    }

                    logListbox.Items.Add(msg);

                    if (logListbox.Items.Count > 1)
                        logListbox.TopIndex = logListbox.Items.Count - 1;
                }
            }
            catch (Exception)
            {
            }
        }

        void AddLogInvoker(string msg)
        {
            try
            {
                this.Invoke((MethodInvoker) delegate { AddLog(msg); });
            }
            catch (Exception)
            {
            }
        }

        void QuestorUILoad(object sender, EventArgs e)
        {
            Logging.OnMessage += AddLogInvoker;
        }

        void QuestorUIFormClosing(object sender, FormClosingEventArgs e)
        {
            Logging.OnMessage -= AddLogInvoker;
            Cache.Instance.Paused = true;
            Cache.Instance.DirectEve.OnFrame -= Questor.questor.EVEOnFrame;
        }
    }
}