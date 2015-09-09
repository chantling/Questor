using Questor.Properties;

namespace Questor
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Forms;
    using System.IO;
    using global::Questor.Modules.Activities;
    using global::Questor.Modules.Actions;
    using global::Questor.Modules.Alerts;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Combat;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;
    using global::Questor.Modules.BackgroundTasks;

    public partial class QuestorUI : Form
    {
        private DateTime _nextUIDataRefresh = DateTime.UtcNow;
        private DateTime _nextScheduleUpdate = DateTime.UtcNow;
        private string _lastLogLine = string.Empty;

        public QuestorUI()
        {
            try
            {
                if (Logging.EnableVisualStyles) Application.EnableVisualStyles();
                if (Logging.DebugUI) Logging.Log("QuestorUI", "QuestorfrmMain", Logging.White);
                if (Logging.DebugUI) Logging.Log("QuestorUI", "InitializeComponent", Logging.White);
                InitializeComponent();
                if (Logging.DebugUI) Logging.Log("QuestorUI", "_questor = new Questor(this);", Logging.White);
                if (Logging.DebugUI) Logging.Log("QuestorUI", "PopulateStateComboBoxes", Logging.White);
                PopulateStateComboBoxes();
                if (Logging.DebugUI) Logging.Log("QuestorUI", "PopulateBehaviorStateComboBox", Logging.White);
                PopulateBehaviorStateComboBox();
                if (Logging.DebugUI) Logging.Log("QuestorUI", "this.Show();", Logging.White);
                Show();
                if (Logging.DebugAttachVSDebugger)
                {
                    if (!System.Diagnostics.Debugger.IsAttached)
                    {
                        Logging.Log("QuestorUI", "VS Debugger is not yet attached: System.Diagnostics.Debugger.Launch()", Logging.Teal);
                        System.Diagnostics.Debugger.Launch();
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
                Cleanup.SignalToQuitQuestor = true;
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }
        }

        private void PopulateStateComboBoxes()
        {
            try
            {
                if (Logging.DebugUI) Logging.Log("QuestorUI", "PopulateStateComboBoxes", Logging.White);
                QuestorStateComboBox.Items.Clear();
                foreach (string text in Enum.GetNames(typeof(QuestorState)))
                    QuestorStateComboBox.Items.Add(text);


                if (Settings.Instance.CharacterMode != null)
                {
                    //if (Logging.DebugUI) Logging.Log("SkillTrainerUI", "PopulateStateComboBoxes", Logging.White);
                    //SkillTrainerStateComboBox.Items.Clear();
                    //foreach (string text in Enum.GetNames(typeof(SkillTrainerState)))
                    //{
                    //    SkillTrainerStateComboBox.Items.Add(text);
                    //}


                    //
                    // populate combo boxes with the various states that are possible
                    //
                    // ComboxBoxes on main windows (at top)
                    //
                    DamageTypeComboBox.Items.Clear();
                    DamageTypeComboBox.Items.Add("Auto");
                    foreach (string text in Enum.GetNames(typeof(DamageType)))
                    {
                        DamageTypeComboBox.Items.Add(text);
                    }

                    //
                    // middle column
                    //
                    PanicStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(PanicState)))
                    {
                        PanicStateComboBox.Items.Add(text);
                    }

                    CombatStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(CombatState)))
                    {
                        CombatStateComboBox.Items.Add(text);
                    }

                    DronesStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(DroneState)))
                    {
                        DronesStateComboBox.Items.Add(text);
                    }

                    CleanupStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(CleanupState)))
                    {
                        CleanupStateComboBox.Items.Add(text);
                    }

                    LocalWatchStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(LocalWatchState)))
                    {
                        LocalWatchStateComboBox.Items.Add(text);
                    }

                    SalvageStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(SalvageState)))
                    {
                        SalvageStateComboBox.Items.Add(text);
                    }

                    //
                    // right column
                    //
                    CombatMissionCtrlStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(CombatMissionCtrlState)))
                    {
                        CombatMissionCtrlStateComboBox.Items.Add(text);
                    }

                    StorylineStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(StorylineState)))
                    {
                        StorylineStateComboBox.Items.Add(text);
                    }

                    ArmStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(ArmState)))
                    {
                        ArmStateComboBox.Items.Add(text);
                    }

                    UnloadStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(UnloadLootState)))
                    {
                        UnloadStateComboBox.Items.Add(text);
                    }

                    TravelerStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(TravelerState)))
                    {
                        TravelerStateComboBox.Items.Add(text);
                    }

                    AgentInteractionStateComboBox.Items.Clear();
                    foreach (string text in Enum.GetNames(typeof(AgentInteractionState)))
                    {
                        AgentInteractionStateComboBox.Items.Add(text);
                    }
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
                //Logging.Log("QuestorUI","populating MissionBlacklisttextbox",Logging.White);
                BlacklistedMissionstextbox.Text = "";
                foreach (string blacklistedmission in MissionSettings.MissionBlacklist)
                {
                    BlacklistedMissionstextbox.AppendText(blacklistedmission + "\r\n");
                }

                //Logging.Log("QuestorUI", "populating MissionBlacklisttextbox", Logging.White);
                GreyListedMissionsTextBox.Text = "";
                foreach (string GreyListedMission in MissionSettings.MissionGreylist)
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
            if (DateTime.UtcNow > _nextUIDataRefresh && DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15))
            {
                _nextUIDataRefresh = DateTime.UtcNow.AddMilliseconds(1000);
                try
                {
                    if (Time.Instance.LastInSpace.AddMilliseconds(1000) > DateTime.UtcNow)
                    {
                        CurrentTimeData1.Text = DateTime.UtcNow.ToLongTimeString();
                        CurrentTimeData2.Text = DateTime.UtcNow.ToLongTimeString();
                        NextOpenContainerInSpaceActionData.Text = Time.Instance.NextOpenContainerInSpaceAction.ToLongTimeString();
                        NextOpenLootContainerActionData.Text = Time.Instance.NextOpenLootContainerAction.ToLongTimeString();
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
                        AutostartData.Text = Settings.Instance.AutoStart.ToString(CultureInfo.InvariantCulture);
                        useDronesData.Text = Drones.UseDrones.ToString(CultureInfo.InvariantCulture);

                        if (Settings.Instance.UserDefinedLavishScriptScript1Description != string.Empty)
                        {
                            lblUserDefinedScript1.Text = Settings.Instance.UserDefinedLavishScriptScript1Description;
                        }

                        if (Settings.Instance.UserDefinedLavishScriptScript2Description != string.Empty)
                        {
                            lblUserDefinedScript2.Text = Settings.Instance.UserDefinedLavishScriptScript2Description;
                        }

                        if (Settings.Instance.UserDefinedLavishScriptScript3Description != string.Empty)
                        {
                            lblUserDefinedScript3.Text = Settings.Instance.UserDefinedLavishScriptScript3Description;
                        }

                        if (Settings.Instance.UserDefinedLavishScriptScript4Description != string.Empty)
                        {
                            lblUserDefinedScript4.Text = Settings.Instance.UserDefinedLavishScriptScript4Description;
                        }

                        //DamageTypeData.Text = MissionSettings.CurrentDamageType.ToString();

                        //OrbitDistanceData.Text = Cache.Instance.OrbitDistance.ToString(CultureInfo.InvariantCulture);
                        //AgentStationIDData.Text = Cache.Instance.AgentStationID.ToString(CultureInfo.InvariantCulture);
                        //AgentIdData.Text = Cache.Instance.AgentId.ToString(CultureInfo.InvariantCulture);
                        //AgentData.Text = Cache.Instance.CurrentAgent.ToString(CultureInfo.InvariantCulture);
                        AgentInteractionPurposeData.Text = AgentInteraction.Purpose.ToString();
                        MissionsThisSessionData.Text = MissionSettings.MissionsThisSession.ToString(CultureInfo.InvariantCulture);

                        //crashes questor when in station?
                        //
                        //if (Combat.MaxRange > 0)
                        //{
                        //    MaxRangeData.Text = Combat.MaxRange.ToString(CultureInfo.InvariantCulture);
                        //causes problems / crashes
                        //}
                        //WeaponRangeData.Text = Cache.Instance.WeaponRange.ToString(CultureInfo.InvariantCulture); //causes problems / crashes
                        //ActiveDronesData.Text = Cache.Instance.ActiveDrones.Count().ToString();                   //causes problems / crashes
                        //if (!Cache.Instance.InWarp && DateTime.UtcNow > _nextWreckUpdate)                            //this was causing exceptions we cant check inWarp from the UI?
                        //{
                        //    _nextWreckUpdate = DateTime.UtcNow.AddSeconds(10);
                        //WrecksData.Text = Cache.Instance.Wrecks.Count().ToString(CultureInfo.InvariantCulture);
                        //UnlootedContainersData.Text = Cache.Instance.UnlootedContainers.Count().ToString(CultureInfo.InvariantCulture); 
                        //ApproachingData.Text = Cache.Instance.IsApproaching.ToString(CultureInfo.InvariantCulture);
                        //}
                        //DamagedDronesData.Text = Cache.Instance.DamagedDrones.Count().ToString(CultureInfo.InvariantCulture);
                        //PriorityTargetsData.Text = Cache.Instance.PriorityTargets.Count().ToString(CultureInfo.InvariantCulture);
                        //if (Cache.Instance.IsMissionPocketDone) IsMissionPocketDoneData.Text = "true";
                        //else if (!Cache.Instance.IsMissionPocketDone) IsMissionPocketDoneData.Text = "false";
                    }

                    if (Time.Instance.LastInStation.AddSeconds(2) > DateTime.UtcNow)
                    {
                        MaxRangeData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        ActiveDronesData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        ApproachingData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        DamagedDronesData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        PriorityTargetsData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        WeaponRangeData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        IsMissionPocketDoneData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        WrecksData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;
                        UnlootedContainersData.Text = Resources.QuestorfrmMain_RefreshInfoDisplayedInUI_n_a;

                        DataAmmoHangarID.Text = Cache.Instance.AmmoHangarID.ToString(CultureInfo.InvariantCulture);
                        DataAmmoHangarName.Text = Settings.Instance.AmmoHangarTabName;
                        DataLootHangarID.Text = Cache.Instance.LootHangarID.ToString(CultureInfo.InvariantCulture);
                        DataLootHangarName.Text = Settings.Instance.LootHangarTabName;
                    }
                }
                catch (Exception ex)
                {
                    if (Logging.DebugUI) Logging.Log("QuestorUI", "RefreshInfoDisplayedInUI: unable to update all UI labels: exception was [" + ex.Message + "]", Logging.Teal);
                }
            }

            try
            {
                if (DateTime.UtcNow > _nextScheduleUpdate)
                {
                    _nextScheduleUpdate = DateTime.UtcNow.AddSeconds(90);
                    if (Time.Instance.StopTimeSpecified)
                    {
                        ScheduledStopTimeData.Text = Time.Instance.StopTime.ToLongTimeString();
                    }

                    //
                    // if control is enabled (checked) then update ManualStopTime so that on next idle questor will check to see if it needs to stop
                    //
                    if (dateTimePicker1.Checked)
                    {
                        Time.Instance.ManualStopTime = dateTimePicker1.Value;
                        if (Time.Instance.ManualStopTime > Time.Instance.StopTime)
                        {
                            Time.Instance.StopTimeSpecified = false;
                        }

                        if (Time.Instance.ManualStopTime < Time.Instance.StopTime)
                        {
                            Time.Instance.StopTimeSpecified = true;
                        }
                    }
                    else
                    {
                        dateTimePicker1.Value = DateTime.Now.AddHours(1);
                        if (!dateTimePicker2.Checked)
                        {
                            Time.Instance.StopTimeSpecified = true;
                        }
                    }
                    //
                    // if control is enabled (checked) then update ManualRestartTime so that on next idle questor will check to see if it needs to stop/restart
                    //
                    if (dateTimePicker2.Checked)
                    {
                        Time.Instance.ManualRestartTime = dateTimePicker2.Value;
                        if (Time.Instance.ManualRestartTime > Time.Instance.StopTime)
                        {
                            Time.Instance.StopTimeSpecified = false;
                        }

                        if (Time.Instance.ManualRestartTime < Time.Instance.StopTime)
                        {
                            Time.Instance.StopTimeSpecified = true;
                        }
                    }
                    else
                    {
                        dateTimePicker1.Value = DateTime.Now.AddHours(1);
                        if (!dateTimePicker1.Checked)
                        {
                            Time.Instance.StopTimeSpecified = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }

        }

        private void PopulateBehaviorStateComboBox()
        {
            try
            {
                if (Logging.DebugUI) Logging.Log("QuestorUI", "PopulateBehaviorStateComboBox", Logging.White);
                if (Settings.Instance.CharacterMode != null)
                {
                    //
                    // populate combo boxes with the various states that are possible
                    //
                    // left column
                    //
                    if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                    {
                        BehaviorComboBox.Items.Clear();
                        foreach (string text in Enum.GetNames(typeof(CombatMissionsBehaviorState)))
                        {
                            BehaviorComboBox.Items.Add(text);
                        }
                    }
                    if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
                    {
                        BehaviorComboBox.Items.Clear();
                        foreach (string text in Enum.GetNames(typeof(DedicatedBookmarkSalvagerBehaviorState)))
                        {
                            BehaviorComboBox.Items.Add(text);
                        }
                    }
                    if (_States.CurrentQuestorState == QuestorState.CombatHelperBehavior)
                    {
                        BehaviorComboBox.Items.Clear();
                        foreach (string text in Enum.GetNames(typeof(CombatHelperBehaviorState)))
                        {
                            BehaviorComboBox.Items.Add(text);
                        }
                    }
                    if (_States.CurrentQuestorState == QuestorState.Mining)
                    {
                        BehaviorComboBox.Items.Clear();
                        foreach (string text in Enum.GetNames(typeof(MiningState)))
                        {
                            BehaviorComboBox.Items.Add(text);
                        }
                    }
                    if (_States.CurrentQuestorState == QuestorState.DirectionalScannerBehavior)
                    {
                        BehaviorComboBox.Items.Clear();
                        foreach (string text in Enum.GetNames(typeof(DirectionalScannerBehaviorState)))
                        {
                            BehaviorComboBox.Items.Add(text);
                        }
                    }
                    if (_States.CurrentQuestorState == QuestorState.DebugHangarsBehavior)
                    {
                        BehaviorComboBox.Items.Clear();
                        foreach (string text in Enum.GetNames(typeof(DebugHangarsBehaviorState)))
                        {
                            BehaviorComboBox.Items.Add(text);
                        }
                    }
                    //if (_States.CurrentQuestorState == QuestorState.BackgroundBehavior)
                    //{
                    //    BehaviorComboBox.Items.Clear();
                    //    foreach (string text in Enum.GetNames(typeof(BackgroundBehaviorState)))
                    //    {
                    //        BehaviorComboBox.Items.Add(text);
                    //    }
                    //}
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
                if (!String.IsNullOrEmpty(Logging.ExtConsole))
                {
                    if (Logging.ExtConsole != _lastLogLine)
                    {
                        if (txtExtConsole.Lines.Count() >= Settings.Instance.MaxLineConsole)
                        {
                            txtExtConsole.Text = "";
                        }

                        txtExtConsole.AppendText(Logging.ExtConsole);
                        _lastLogLine = Logging.ExtConsole;
                        Logging.ExtConsole = null;
                    }
                }

                //if (Logging.DebugUI) Logging.Log("QuestorUI", "UpdateUiTick", Logging.White);
                // The if's in here stop the UI from flickering
                string text = "Questor";
                if (Settings.Instance.CharacterName != string.Empty)
                {
                    text = "Questor [" + Settings.Instance.CharacterName + "]";
                }
                if (Settings.Instance.CharacterName != string.Empty && Cache.Instance.Wealth > 10000000)
                {
                    text = "Questor [" + Settings.Instance.CharacterName + "][" + String.Format("{0:0,0}", Cache.Instance.Wealth / 1000000) + "mil isk]";
                }

                if (Text != text)
                    Text = text;

                lastSessionisreadyData.Text = "[" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady).TotalSeconds, 0) + "] sec ago";
                LastFrameData.Text = "[" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds, 0) + "] sec ago";
                lastInSpaceData.Text = "[" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastInSpace).TotalSeconds, 0) + "] sec ago";
                lastInStationData.Text = "[" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastInStation).TotalSeconds, 0) + "] sec ago";
                lastKnownGoodConnectedTimeData.Text = "[" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0) + "] min ago";
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

                //
                // Left Group
                //
                if ((string)QuestorStateComboBox.SelectedItem != _States.CurrentQuestorState.ToString() && !QuestorStateComboBox.DroppedDown)
                {
                    QuestorStateComboBox.SelectedItem = _States.CurrentQuestorState.ToString();

                    if (_States.CurrentQuestorState == QuestorState.Idle)
                    {
                        //                        _States.LavishEvent_QuestorIdle();
                    }
                }

                if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                {
                    if ((string)BehaviorComboBox.SelectedItem != _States.CurrentCombatMissionBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    {
                        BehaviorComboBox.SelectedItem = _States.CurrentCombatMissionBehaviorState.ToString();
                        //                        _States.LavishEvent_QuestorCombatMissionsBehaviorState();
                    }
                }

                if (_States.CurrentQuestorState == QuestorState.Mining)
                {
                    if ((string)BehaviorComboBox.SelectedItem != _States.CurrentMiningState.ToString() && !BehaviorComboBox.DroppedDown)
                    {
                        BehaviorComboBox.SelectedItem = _States.CurrentMiningState.ToString();
                    }
                }

                if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
                {
                    if ((string)BehaviorComboBox.SelectedItem != _States.CurrentDedicatedBookmarkSalvagerBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    {
                        BehaviorComboBox.SelectedItem = _States.CurrentDedicatedBookmarkSalvagerBehaviorState.ToString();
                        //                        _States.LavishEvent_QuestorDedicatedBookmarkSalvagerBehaviorState();
                    }
                }

                if (_States.CurrentQuestorState == QuestorState.CombatHelperBehavior)
                {
                    if ((string)BehaviorComboBox.SelectedItem != _States.CurrentCombatHelperBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    {
                        BehaviorComboBox.SelectedItem = _States.CurrentCombatHelperBehaviorState.ToString();
                    }
                }

                if (_States.CurrentQuestorState == QuestorState.DirectionalScannerBehavior)
                {
                    if ((string)BehaviorComboBox.SelectedItem != _States.CurrentDirectionalScannerBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                    {
                        BehaviorComboBox.SelectedItem = _States.CurrentDirectionalScannerBehaviorState.ToString();
                    }
                }

                //if (_States.CurrentQuestorState == QuestorState.BackgroundBehavior)
                //{
                //    if ((string)BehaviorComboBox.SelectedItem != _States.CurrentBackgroundBehaviorState.ToString() && !BehaviorComboBox.DroppedDown)
                //    {
                //        BehaviorComboBox.SelectedItem = _States.CurrentBackgroundBehaviorState.ToString();
                //    }
                //}

                //if ((string) DamageTypeComboBox.SelectedItem != "Auto")
                //{
                //    if ((string)DamageTypeComboBox.SelectedItem != MissionSettings.ManualDamageType.ToString() && !DamageTypeComboBox.DroppedDown)
                //    {
                //        DamageTypeComboBox.SelectedItem = MissionSettings.ManualDamageType.ToString();
                //    }    
                //}

                //if ((string) DamageTypeComboBox.SelectedItem == "Auto")
                //{
                //    if (MissionSettings.ManualDamageType != null && !DamageTypeComboBox.DroppedDown)
                //    {
                //        MissionSettings.ManualDamageType = null;
                //    }
                //}


                //
                // Middle group
                //
                if ((string)PanicStateComboBox.SelectedItem != _States.CurrentPanicState.ToString() && !PanicStateComboBox.DroppedDown)
                {
                    PanicStateComboBox.SelectedItem = _States.CurrentPanicState.ToString();
                    //                    _States.LavishEvent_QuestorPanicState();
                }

                if ((string)CombatStateComboBox.SelectedItem != _States.CurrentCombatState.ToString() && !CombatStateComboBox.DroppedDown)
                {
                    CombatStateComboBox.SelectedItem = _States.CurrentCombatState.ToString();
                    //                    _States.LavishEvent_QuestorCombatState();
                }

                if ((string)DronesStateComboBox.SelectedItem != _States.CurrentDroneState.ToString() && !DronesStateComboBox.DroppedDown)
                {
                    DronesStateComboBox.SelectedItem = _States.CurrentDroneState.ToString();
                    //                    _States.LavishEvent_QuestorDronesState();
                }

                if ((string)CleanupStateComboBox.SelectedItem != _States.CurrentCleanupState.ToString() && !CleanupStateComboBox.DroppedDown)
                {
                    CleanupStateComboBox.SelectedItem = _States.CurrentCleanupState.ToString();
                }

                if ((string)LocalWatchStateComboBox.SelectedItem != _States.CurrentLocalWatchState.ToString() && !LocalWatchStateComboBox.DroppedDown)
                {
                    LocalWatchStateComboBox.SelectedItem = _States.CurrentLocalWatchState.ToString();
                }

                if ((string)SalvageStateComboBox.SelectedItem != _States.CurrentSalvageState.ToString() && !SalvageStateComboBox.DroppedDown)
                {
                    SalvageStateComboBox.SelectedItem = _States.CurrentSalvageState.ToString();
                }

                //
                // Right Group
                //
                if ((string)CombatMissionCtrlStateComboBox.SelectedItem != _States.CurrentCombatMissionCtrlState.ToString() && !CombatMissionCtrlStateComboBox.DroppedDown)
                {
                    CombatMissionCtrlStateComboBox.SelectedItem = _States.CurrentCombatMissionCtrlState.ToString();
                }

                if ((string)StorylineStateComboBox.SelectedItem != _States.CurrentStorylineState.ToString() && !StorylineStateComboBox.DroppedDown)
                {
                    StorylineStateComboBox.SelectedItem = _States.CurrentStorylineState.ToString();
                }

                if ((string)ArmStateComboBox.SelectedItem != _States.CurrentArmState.ToString() && !ArmStateComboBox.DroppedDown)
                {
                    ArmStateComboBox.SelectedItem = _States.CurrentArmState.ToString();
                }

                if ((string)UnloadStateComboBox.SelectedItem != _States.CurrentUnloadLootState.ToString() && !UnloadStateComboBox.DroppedDown)
                {
                    UnloadStateComboBox.SelectedItem = _States.CurrentUnloadLootState.ToString();
                }

                if ((string)TravelerStateComboBox.SelectedItem != _States.CurrentTravelerState.ToString() && !TravelerStateComboBox.DroppedDown)
                {
                    TravelerStateComboBox.SelectedItem = _States.CurrentTravelerState.ToString();

                }

                if ((string)AgentInteractionStateComboBox.SelectedItem != _States.CurrentAgentInteractionState.ToString() && !AgentInteractionStateComboBox.DroppedDown)
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

                if (Settings.Instance.WindowXPosition.HasValue)
                {
                    Left = Settings.Instance.WindowXPosition.Value;
                    Settings.Instance.WindowXPosition = null;
                }

                if (Settings.Instance.WindowYPosition.HasValue)
                {
                    Top = Settings.Instance.WindowYPosition.Value;
                    Settings.Instance.WindowYPosition = null;
                }

                if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission && Cache.Instance.CurrentPocketAction != null)
                {
                    string newlblCurrentPocketActiontext = "[ " + Cache.Instance.CurrentPocketAction + " ] Action";
                    if (lblCurrentPocketAction.Text != newlblCurrentPocketActiontext)
                    {
                        lblCurrentPocketAction.Text = newlblCurrentPocketActiontext;
                    }
                }
                else if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Salvage ||
                         _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoSalvageBookmark ||
                         _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.SalvageNextPocket ||
                         _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.BeginAfterMissionSalvaging ||
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

                if (_States.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.Idle && _States.CurrentQuestorState != QuestorState.Idle)
                {
                    if (!String.IsNullOrEmpty(MissionSettings.MissionName))
                    {
                        if (!String.IsNullOrEmpty(MissionSettings.MissionsPath))
                        {
                            if (File.Exists(MissionSettings.MissionXmlPath))
                            {
                                string newlblCurrentMissionInfotext = "[ " + MissionSettings.MissionName + " ][ " + Math.Round(DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes, 0) + " min][ #" + Statistics.MissionsThisSession + " ]";
                                if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                                {
                                    lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                                    buttonOpenMissionXML.Enabled = true;
                                }
                            }
                            else
                            {
                                string newlblCurrentMissionInfotext = "[ " + MissionSettings.MissionName + " ][ " + Math.Round(DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes, 0) + " min][ #" + Statistics.MissionsThisSession + " ]";
                                if (lblCurrentMissionInfo.Text != newlblCurrentMissionInfotext)
                                {
                                    lblCurrentMissionInfo.Text = newlblCurrentMissionInfotext;
                                    buttonOpenMissionXML.Enabled = false;
                                }
                            }
                        }
                    }
                    else if (String.IsNullOrEmpty(MissionSettings.MissionName))
                    {
                        lblCurrentMissionInfo.Text = Resources.QuestorfrmMain_UpdateUiTick_No_Mission_Selected_Yet;
                        buttonOpenMissionXML.Enabled = false;
                    }
                    else
                    {
                        //lblCurrentMissionInfo.Text = "No Mission XML exists for this mission";
                        buttonOpenMissionXML.Enabled = false;
                    }
                }
                else
                {
                    buttonOpenMissionXML.Enabled = false;
                }


                if (Settings.Instance.DefaultSettingsLoaded)
                {
                    buttonOpenCharacterXML.Enabled = false;
                    buttonOpenSchedulesXML.Enabled = false;
                    buttonQuestormanager.Enabled = false;
                    buttonOpenCommonSettingsXML.Enabled = false;
                    buttonQuestorStatistics.Enabled = false;
                }
                else
                {
                    if (Settings.Instance.CharacterXMLExists)
                    {
                        buttonOpenCharacterXML.Enabled = true;
                        Settings.Instance.CharacterXMLExists = true;
                    }
                    else
                    {
                        buttonOpenCharacterXML.Enabled = false;
                        Settings.Instance.CharacterXMLExists = false;
                    }

                    //
                    // Does Schedules.xml exist in the directory where we started questor?
                    //
                    if (Settings.Instance.SchedulesXMLExists)
                    {
                        buttonOpenCharacterXML.Enabled = true;
                        Settings.Instance.SchedulesXMLExists = true;
                    }
                    else
                    {
                        buttonOpenSchedulesXML.Enabled = false;
                        Settings.Instance.SchedulesXMLExists = false;
                    }

                    //
                    // Does QuestorStatistics.exe exist in the directory where we started questor?
                    //
                    if (Settings.Instance.QuestorStatisticsExists)
                    {
                        buttonQuestorStatistics.Enabled = true;
                        Settings.Instance.QuestorStatisticsExists = true;
                    }
                    else
                    {
                        buttonQuestorStatistics.Enabled = false;
                        Settings.Instance.QuestorStatisticsExists = false;
                    }

                    //
                    // Does QuestorSettings.exe exist in the directory where we started questor?
                    //
                    if (Settings.Instance.QuestorSettingsExists)
                    {
                        buttonOpenCommonSettingsXML.Enabled = true;
                        Settings.Instance.QuestorSettingsExists = true;
                    }
                    else
                    {
                        buttonOpenCommonSettingsXML.Enabled = false;
                        Settings.Instance.QuestorSettingsExists = false;
                    }

                    //
                    // Does Questormanager.exe exist in the directory where we started questor?
                    //
                    if (Settings.Instance.QuestorManagerExists)
                    {
                        buttonQuestormanager.Enabled = true;
                        Settings.Instance.QuestorManagerExists = true;
                    }
                    else
                    {
                        buttonQuestormanager.Enabled = false;
                        Settings.Instance.QuestorManagerExists = false;
                    }
                }

                int extraWaitSeconds = 0;
                if (!System.Diagnostics.Debugger.IsAttached) //do not restart due to no frames or Session.Isready aging if a debugger is attached until it reaches absurdity...
                {
                    extraWaitSeconds = 60;
                }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds > (Time.Instance.NoFramesRestart_seconds + extraWaitSeconds) && DateTime.UtcNow.Subtract(LoginToEVE.EVEAccountLoginStarted).TotalSeconds > 300)
                {
                    if (DateTime.UtcNow.Subtract(Time.Instance.LastLogMessage).TotalSeconds > 30)
                    {
                        Logging.Log("QuestorUI", "The Last UI Frame Drawn by EVE was [" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds, 0) + "] seconds ago! This is bad. - Exiting EVE", Logging.Red);
                        Cleanup.ReasonToStopQuestor = "The Last UI Frame Drawn by EVE was [" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastFrame).TotalSeconds, 0) + "] seconds ago! This is bad. - Exiting EVE";
                        //
                        // closing eve would be a very good idea here
                        //
                        Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                        Application.Exit();
                    }
                }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady).TotalSeconds > (Time.Instance.NoSessionIsReadyRestart_seconds + extraWaitSeconds) &&
                        DateTime.UtcNow.Subtract(LoginToEVE.EVEAccountLoginStarted).TotalSeconds > 210)
                {
                    if (DateTime.UtcNow.Subtract(Time.Instance.LastLogMessage).TotalSeconds > 60)
                    {
                        Logging.Log("QuestorUI", "The Last Session.IsReady = true was [" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady).TotalSeconds, 0) + "] seconds ago! This is bad. - Exiting EVE", Logging.Red);
                        Cleanup.ReasonToStopQuestor = "The Last Session.IsReady = true was [" + Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastSessionIsReady).TotalSeconds, 0) + "] seconds ago! This is bad. - Exiting EVE";
                        Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                        Application.Exit();
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Log("QuestorUI", "Exception [" + ex + "]", Logging.Debug);
            }

        }

        private void DamageTypeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            return;
        }

        private void PauseCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Cache.Instance.Paused = PauseCheckBox.Checked;
        }

        private void Disable3DCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.Disable3D = Disable3DCheckBox.Checked;
        }

        private void TxtComandKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
               
            }
        }

        private void ChkShowConsoleCheckedChanged(object sender, EventArgs e)
        {
            //Form frmMain = new Form();
            //Size = chkShowDetails.Checked ? new System.Drawing.Size(707, 434) : new System.Drawing.Size(362, 124);
        }

        private void FrmMainLoad(object sender, EventArgs e)
        {
        }

        private void DisableMouseWheel(object sender, MouseEventArgs e)
        {
            ((HandledMouseEventArgs)e).Handled = true;
        }

        private void ButtonQuestorStatisticsClick(object sender, EventArgs e)
        {
            string path = Logging.PathToCurrentDirectory;
            Process[] processes = System.Diagnostics.Process.GetProcessesByName("QuestorStatistics");

            if (processes.Length == 0)
            {
                // QuestorStatistics
                try
                {
                    System.Diagnostics.Process.Start(path + "\\QuestorStatistics.exe");
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    Logging.Log("QuestorUI", "QuestorStatistics could not be launched the error was: " + ex.Message, Logging.Orange);
                }
            }
        }

        private void ButtonOpenLogDirectoryClick(object sender, EventArgs e)
        {
            //string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            System.Diagnostics.Process.Start(Logging.Logpath);
        }

        private void ButtonOpenMissionXmlClick(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Launching [" + MissionSettings.MissionXmlPath + "]", Logging.White);
            System.Diagnostics.Process.Start(MissionSettings.MissionXmlPath);
        }

        private void QuestorStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentQuestorState = (QuestorState)Enum.Parse(typeof(QuestorState), QuestorStateComboBox.Text);
            PopulateBehaviorStateComboBox();
            PopulateMissionLists();

            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
        }

        private void BehaviorComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            //Logging.Log("QuestorUI","BehaviorComboBoxChanged: Current QuestorState is: [" + _States.CurrentQuestorState + "]",Logging.White);
            if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
            {
                _States.CurrentCombatMissionBehaviorState = (CombatMissionsBehaviorState)Enum.Parse(typeof(CombatMissionsBehaviorState), BehaviorComboBox.Text);
                //                _States.LavishEvent_QuestorCombatMissionsBehaviorState();
            }

            if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
            {
                _States.CurrentDedicatedBookmarkSalvagerBehaviorState = (DedicatedBookmarkSalvagerBehaviorState)Enum.Parse(typeof(DedicatedBookmarkSalvagerBehaviorState), BehaviorComboBox.Text);
                //                _States.LavishEvent_QuestorDedicatedBookmarkSalvagerBehaviorState();
            }

            if (_States.CurrentQuestorState == QuestorState.CombatHelperBehavior)
            {
                _States.CurrentCombatHelperBehaviorState = (CombatHelperBehaviorState)Enum.Parse(typeof(CombatHelperBehaviorState), BehaviorComboBox.Text);
            }

            if (_States.CurrentQuestorState == QuestorState.DirectionalScannerBehavior)
            {
                _States.CurrentDirectionalScannerBehaviorState = (DirectionalScannerBehaviorState)Enum.Parse(typeof(DirectionalScannerBehaviorState), BehaviorComboBox.Text);
            }

            //if (_States.CurrentQuestorState == QuestorState.DebugInventoryTree)
            //{
            //    _States.CurrentDe =
            //        (DebugHangarsState)
            //        Enum.Parse(typeof(DebugHangarsState), BehaviorComboBox.Text);
            //}

            if (_States.CurrentQuestorState == QuestorState.DebugHangarsBehavior)
            {
                _States.CurrentDebugHangarBehaviorState = (DebugHangarsBehaviorState)Enum.Parse(typeof(DebugHangarsBehaviorState), BehaviorComboBox.Text);
            }

            if (_States.CurrentQuestorState == QuestorState.Mining)
            {
                _States.CurrentMiningState = (MiningState)Enum.Parse(typeof(MiningState), BehaviorComboBox.Text);
            }
            //if (_States.CurrentQuestorState == QuestorState.BackgroundBehavior)
            //{
            //    _States.CurrentBackgroundBehaviorState = (BackgroundBehaviorState)Enum.Parse(typeof(BackgroundBehaviorState), BehaviorComboBox.Text);
            //    //_States.LavishEvent_QuestorCombatMissionsBehaviorState();
            //}

            try
            {
                AgentNameData.Text = Cache.Instance.CurrentAgent;
                AgentEffectiveStandingsData.Text = Cache.Instance.AgentEffectiveStandingtoMeText;

                //DeclinedTimeData.Text = Cache.Instance.CurrentAgent.DeclineTimer;
                //
                // greylist info
                //
                MinAgentGreyListStandingsData.Text = Math.Round(MissionSettings.MinAgentGreyListStandings, 2).ToString(CultureInfo.InvariantCulture);
                LastGreylistedMissionDeclinedData.Text = MissionSettings.LastGreylistMissionDeclined;
                greylistedmissionsdeclineddata.Text = MissionSettings.GreyListedMissionsDeclined.ToString(CultureInfo.InvariantCulture);

                //
                // blacklist info
                //
                MinAgentBlackListStandingsData.Text = Math.Round(MissionSettings.MinAgentBlackListStandings, 2).ToString(CultureInfo.InvariantCulture);
                LastBlacklistedMissionDeclinedData.Text = MissionSettings.LastBlacklistMissionDeclined;
                blacklistedmissionsdeclineddata.Text = MissionSettings.BlackListedMissionsDeclined.ToString(CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                //if we get an exception here ignore it as it should not effect anything, the GUI is only displaying data collected and processed elsewhere
                if (Logging.DebugExceptions || (Logging.DebugUI)) Logging.Log("QuestorUI", "Exception was [" + ex.Message + "]", Logging.Teal);
            }
        }

        private void PanicStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentPanicState = (PanicState)Enum.Parse(typeof(PanicState), PanicStateComboBox.Text);

            // If you are at the controls enough to change states... assume that panic needs to do nothing
            //_questor.panicstatereset = true; //this cannot be reset when the index changes, as that happens during natural state changes, this needs to be a mouse event
            //            _States.LavishEvent_QuestorPanicState();
        }

        private void CombatStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCombatState = (CombatState)Enum.Parse(typeof(CombatState), CombatStateComboBox.Text);
            //            _States.LavishEvent_QuestorCombatState();
        }

        private void DronesStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentDroneState = (DroneState)Enum.Parse(typeof(DroneState), DronesStateComboBox.Text);
            //            _States.LavishEvent_QuestorDronesState();
        }

        private void CleanupStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentCleanupState = (CleanupState)Enum.Parse(typeof(CleanupState), CleanupStateComboBox.Text);
        }

        private void LocalWatchStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentLocalWatchState = (LocalWatchState)Enum.Parse(typeof(LocalWatchState), LocalWatchStateComboBox.Text);
        }

        private void SalvageStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentSalvageState = (SalvageState)Enum.Parse(typeof(SalvageState), SalvageStateComboBox.Text);
        }

        private void StorylineStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentStorylineState = (StorylineState)Enum.Parse(typeof(StorylineState), StorylineStateComboBox.Text);
        }

        private void ArmStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentArmState = (ArmState)Enum.Parse(typeof(ArmState), ArmStateComboBox.Text);
        }

        private void UnloadStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentUnloadLootState = (UnloadLootState)Enum.Parse(typeof(UnloadLootState), UnloadStateComboBox.Text);
        }

        private void TravelerStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentTravelerState = (TravelerState)Enum.Parse(typeof(TravelerState), TravelerStateComboBox.Text);
        }

        private void AgentInteractionStateComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            _States.CurrentAgentInteractionState = (AgentInteractionState)Enum.Parse(typeof(AgentInteractionState), AgentInteractionStateComboBox.Text);
        }

        private void TxtExtConsoleTextChanged(object sender, EventArgs e)
        {
        }

        private void AutoStartCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Settings.Instance.AutoStart = AutoStartCheckBox.Checked;

        }

        private void ButtonOpenCharacterXMLClick(object sender, EventArgs e)
        {
            if (File.Exists(Logging.CharacterSettingsPath))
            {
                Logging.Log("QuestorUI", "Launching [" + Logging.CharacterSettingsPath + "]", Logging.White);
                System.Diagnostics.Process.Start(Logging.CharacterSettingsPath);
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to open [" + Logging.CharacterSettingsPath + "] file not found", Logging.Orange);
            }
        }

        private void ButtonOpenSchedulesXMLClick(object sender, EventArgs e)
        {
            string schedulesXmlPath = Path.Combine(Settings.Instance.Path, "Schedules.xml");
            if (File.Exists(schedulesXmlPath))
            {
                Logging.Log("QuestorUI", "Launching [" + schedulesXmlPath + "]", Logging.White);
                System.Diagnostics.Process.Start(schedulesXmlPath);
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to open [" + schedulesXmlPath + "] file not found", Logging.Orange);
            }
        }

        private void ButtonQuestormanagerClick(object sender, EventArgs e)
        {
            string questorManagerPath = Path.Combine(Settings.Instance.Path, "QuestorManager.exe");
            if (File.Exists(questorManagerPath))
            {
                    Logging.Log("QuestorUI", "Launching [ dotnet QuestorManager QuestorManager ] - fix me",
                                Logging.White);
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to launch QuestorManager from [" + questorManagerPath + "] file not found", Logging.Orange);
            }
        }

        private void ButtonOpenCommonSettingsXMLClick(object sender, EventArgs e)
        {
            if (File.Exists(Settings.Instance.CommonSettingsPath))
            {
                Logging.Log("QuestorUI", "Launching [" + Settings.Instance.CommonSettingsPath + "]", Logging.White);
                System.Diagnostics.Process.Start(Settings.Instance.CommonSettingsPath);
            }
            else
            {
                Logging.Log("QuestorUI", "Unable to open [" + Settings.Instance.CommonSettingsPath + "] file not found", Logging.Orange);
            }
        }

        private void ExitWhenIdleCheckBoxCheckedChanged(object sender, EventArgs e)
        {
            Cache.Instance.ExitWhenIdle = ExitWhenIdleCheckBox.Checked;
            AutoStartCheckBox.Checked = false;
            Settings.Instance.AutoStart = false;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            MissionSettings.StopSessionAfterMissionNumber = (int)numericUpDown1.Value;
        }

        private void ReloadAllClick(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "ReloadAll button was pressed: changing QuestorState to ReloadAll- when done reloading it should return to the configured behavior", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugReloadAll;
        }

        private void OutOfAmmoClick(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "OutOfAmmo button was pressed: changing CombatState to OutOfAmmo", Logging.Teal);
            _States.CurrentCombatState = CombatState.OutOfAmmo;
        }

        private void BttnCloseAllInventoryWindowsClick(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close ALL Inventory Windows button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseAllInventoryWindows;
        }

        private void btnReadyItemsHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open ItemsHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.ReadyItemsHangar;
        }

        private void btnStackItemsHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack ItemsHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackItemsHangar;
        }

        private void bttnCloseItemsHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close ItemsHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseItemsHangar;
        }

        private void btnOpenShipsHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open ShipsHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenShipsHangar;
        }

        private void btnStackShipsHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack ShipsHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackShipsHangar;
        }

        private void bttnCloseShipsHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close ShipsHangar button was pressed - Closing All inventoryWindows", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseShipsHangar;
        }

        private void btnOpenFreightContainer_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open LootContainer button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenLootContainer;
        }

        private void btnStackFreightContainer_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack LootContainer button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackLootContainer;
        }

        private void bttnCloseLootContainer_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close LootContainer button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseLootContainer;
        }

        private void btnOpenCorpAmmoHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open CorpAmmoHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenCorpAmmoHangar;
        }

        private void btnStackCorpAmmoHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack CorpAmmoHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackCorpAmmoHangar;
        }

        private void bttnCloseCorpAmmoHangar_Click_1(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close CorpAmmoHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseCorpAmmoHangar;
        }

        private void btnOpenCorpLootHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open CorpLootHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenCorpLootHangar;
        }

        private void btnStackCorpLootHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack CorpLootHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackCorpLootHangar;
        }

        private void bttnCloseCorpLootHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close CorpLootHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseCorpLootHangar;
        }

        private void btnOpenAmmoHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open AmmoHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenAmmoHangar;
        }

        private void btnStackAmmoHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack AmmoHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackAmmoHangar;
        }

        private void bttnCloseAmmoHangar_Click_1(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close LootHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseLootHangar;
        }

        private void btnOpenLootHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open LootHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenLootHangar;
        }

        private void btnStackLootHangar_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack LootHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackLootHangar;
        }

        private void bttnCloseLootHangar_Click_1(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close AmmoHangar button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseAmmoHangar;
        }

        private void bttnQueryAmmoHangarID_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Get AmmoHangarID button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.GetAmmoHangarID;
        }

        private void bttnQueryLootHangarID_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Get LootHangarID button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.GetLootHangarID;
        }

        private void bttnOpenCargoHold_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Open Cargo Hold button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenCargoHold;
        }

        private void bttnStackCargoHold_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Stack Cargo Hold button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.StackCargoHold;
        }

        private void bttnCloseCargoHold_Click(object sender, EventArgs e)
        {
            Cache.Instance.Paused = false;
            Logging.Log("QuestorUI", "Close Cargo Hold button was pressed", Logging.Teal);
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.CloseCargoHold;
        }

        private void bttnMaintainConsoleLogs_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Starting Logging.MaintainConsoleLogs() Routine.", Logging.Debug);
            Logging.MaintainConsoleLogs();
        }

        private void btnSendTestEmail_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "This Button Sends a test email to the configured email address", Logging.Debug);
            Email.SendEmail("Someone Pushed the Test Email Button", "This is Just a Test Email from Questor");
        }

        private void bttnResizeEVEWindow_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Running Innerspace command: timedcommand 150 WindowCharacteristics -size " + 1280 + "x" + 960, Logging.White);
            //            if (Logging.UseInnerspace) LavishScript.ExecuteCommand("timedcommand 150 WindowCharacteristics -size " + 1280 + "x" + 960);
            Logging.Log("QuestorUI", "Running Innerspace command: timedcommand 200 WindowCharacteristics -pos " + 0 + "," + 0, Logging.White);
            //            if (Logging.UseInnerspace) LavishScript.ExecuteCommand("timedcommand 200 WindowCharacteristics -pos " + 0 + "," + 0);
        }

        private void bttnLogEntitiesOnGrid_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Starting Statistics.EntityStatistics(Cache.Instance.Entities); Routine.", Logging.Debug);
            Statistics.EntityStatistics(Cache.Instance.EntitiesOnGrid);
        }

        private void bttnLogContainersOnGrid_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Statistics.EntityStatistics(Cache.Instance.Containers); Routine.", Logging.Debug);
            Statistics.EntityStatistics(Cache.Instance.Containers);
        }

        private void bttnOpenOreHold_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Open OreHold button was pressed", Logging.Debug);
            if (Cache.Instance.OreHold == null) return;
        }

        private void bttnOpenInventory_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Open Inventory button was pressed", Logging.Debug);
            //Cache.Instance.OpenInventoryWindow("OpenInventory");

            Cache.Instance.Paused = false;
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.OpenInventory;
        }

        private void bttnListInvTree_Click(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "List InvTree button was pressed", Logging.Debug);
            //Cache.Instance.ListInvTree("ListInvTree");

            Cache.Instance.Paused = false;
            _States.CurrentQuestorState = QuestorState.DebugHangarsBehavior;
            _States.CurrentDebugHangarBehaviorState = DebugHangarsBehaviorState.ListInvTree;
        }

        private void brnSetStopTimetoNow_Click_1(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Setting: Cache.Instance.StopTime to [" + DateTime.Now + "] - the next Questor.Idle state should see and process this.", Logging.Debug);
            Time.Instance.StopTimeSpecified = true;
            Time.Instance.NextTimeCheckAction = DateTime.UtcNow;
            Time.Instance.StopTime = DateTime.Now; //(use local time not UTC time because this is Arm schedule related timer)
        }

        private void btnSetQuestorQuittingFlag_Click_1(object sender, EventArgs e)
        {
            Logging.Log("QuestorUI", "Setting: Cleanup.SessionState to [Quitting] - the next Questor.Idle state should see and process this.", Logging.Debug);
            Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
        }

        private void bttnUserDefinedScript1_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.UserDefinedLavishScriptScript1 != string.Empty)
            {
                Logging.Log("QuestorUI", "Running User Defined LavishScript Script #1 [" + Settings.Instance.UserDefinedLavishScriptScript1 + "]", Logging.Debug);
                //                if (Logging.UseInnerspace) LavishScript.ExecuteCommand("runscript " + Settings.Instance.UserDefinedLavishScriptScript1);    
            }
            else
            {
                Logging.Log("QuestorUI", "User Defined LavishScript Script #1 is not defined in your characters settings xml: doing nothing", Logging.Debug);
            }
        }

        private void bttnUserDefinedScript2_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.UserDefinedLavishScriptScript2 != string.Empty)
            {
                Logging.Log("QuestorUI", "Running User Defined LavishScript Script #1 [" + Settings.Instance.UserDefinedLavishScriptScript2 + "]", Logging.Debug);
                //                if (Logging.UseInnerspace) LavishScript.ExecuteCommand("runscript " + Settings.Instance.UserDefinedLavishScriptScript2);
            }
            else
            {
                Logging.Log("QuestorUI", "User Defined LavishScript Script #2 is not defined in your characters settings xml: doing nothing", Logging.Debug);
            }
        }

        private void bttnUserDefinedScript3_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.UserDefinedLavishScriptScript3 != string.Empty)
            {
                Logging.Log("QuestorUI", "Running User Defined LavishScript Script #1 [" + Settings.Instance.UserDefinedLavishScriptScript3 + "]", Logging.Debug);
                //                if (Logging.UseInnerspace) LavishScript.ExecuteCommand("runscript " + Settings.Instance.UserDefinedLavishScriptScript3);
            }
            else
            {
                Logging.Log("QuestorUI", "User Defined LavishScript Script #3 is not defined in your characters settings xml: doing nothing", Logging.Debug);
            }
        }

        private void bttnUserDefinedScript4_Click(object sender, EventArgs e)
        {
            if (Settings.Instance.UserDefinedLavishScriptScript4 != string.Empty)
            {
                Logging.Log("QuestorUI", "Running User Defined LavishScript Script #1 [" + Settings.Instance.UserDefinedLavishScriptScript4 + "]", Logging.Debug);
                //                if (Logging.UseInnerspace) LavishScript.ExecuteCommand("runscript " + Settings.Instance.UserDefinedLavishScriptScript4);
            }
            else
            {
                Logging.Log("QuestorUI", "User Defined LavishScript Script #4 is not defined in your characters settings xml: doing nothing", Logging.Debug);
            }
        }
    }
}