namespace Questor
{
    partial class QuestorUI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
        	this.components = new System.ComponentModel.Container();
        	this.AutoStartCheckBox = new System.Windows.Forms.CheckBox();
        	this.tUpdateUI = new System.Windows.Forms.Timer(this.components);
        	this.DamageTypeComboBox = new System.Windows.Forms.ComboBox();
        	this.lblDamageType = new System.Windows.Forms.Label();
        	this.PauseCheckBox = new System.Windows.Forms.CheckBox();
        	this.Disable3DCheckBox = new System.Windows.Forms.CheckBox();
        	this.lblMissionName = new System.Windows.Forms.Label();
        	this.lblCurrentMissionInfo = new System.Windows.Forms.Label();
        	this.lblPocketAction = new System.Windows.Forms.Label();
        	this.lblCurrentPocketAction = new System.Windows.Forms.Label();
        	this.buttonOpenLogDirectory = new System.Windows.Forms.Button();
        	this.BehaviorComboBox = new System.Windows.Forms.ComboBox();
        	this.label2 = new System.Windows.Forms.Label();
        	this.QuestorStateComboBox = new System.Windows.Forms.ComboBox();
        	this.QuestorStatelbl = new System.Windows.Forms.Label();
        	this.label25 = new System.Windows.Forms.Label();
        	this.label26 = new System.Windows.Forms.Label();
        	this.tabControlMain = new System.Windows.Forms.TabControl();
        	this.tabPage3 = new System.Windows.Forms.TabPage();
        	this.Tabs = new System.Windows.Forms.TabControl();
        	this.tabConsole = new System.Windows.Forms.TabPage();
        	this.logListbox = new System.Windows.Forms.ListBox();
        	this.tabStates = new System.Windows.Forms.TabPage();
        	this.dataStopTimeSpecified = new System.Windows.Forms.Label();
        	this.lblStopTimeSpecified = new System.Windows.Forms.Label();
        	this.MissionsThisSessionData = new System.Windows.Forms.Label();
        	this.MissionsThisSessionlbl = new System.Windows.Forms.Label();
        	this.lastKnownGoodConnectedTimeData = new System.Windows.Forms.Label();
        	this.lastKnownGoodConnectedTimeLabel = new System.Windows.Forms.Label();
        	this.lastInStationData = new System.Windows.Forms.Label();
        	this.lastInSpaceData = new System.Windows.Forms.Label();
        	this.lastInStationLabel = new System.Windows.Forms.Label();
        	this.lastInSpaceLabel = new System.Windows.Forms.Label();
        	this.LastFrameData = new System.Windows.Forms.Label();
        	this.lastSessionisreadyData = new System.Windows.Forms.Label();
        	this.lastSessionIsreadylabel = new System.Windows.Forms.Label();
        	this.lastFrameLabel = new System.Windows.Forms.Label();
        	this.label19 = new System.Windows.Forms.Label();
        	this.panel2 = new System.Windows.Forms.Panel();
        	this.label18 = new System.Windows.Forms.Label();
        	this.label16 = new System.Windows.Forms.Label();
        	this.label15 = new System.Windows.Forms.Label();
        	this.SalvageStateComboBox = new System.Windows.Forms.ComboBox();
        	this.label10 = new System.Windows.Forms.Label();
        	this.LocalWatchStateComboBox = new System.Windows.Forms.ComboBox();
        	this.label9 = new System.Windows.Forms.Label();
        	this.CleanupStateComboBox = new System.Windows.Forms.ComboBox();
        	this.label8 = new System.Windows.Forms.Label();
        	this.DronesStateComboBox = new System.Windows.Forms.ComboBox();
        	this.CombatStateComboBox = new System.Windows.Forms.ComboBox();
        	this.PanicStateComboBox = new System.Windows.Forms.ComboBox();
        	this.panel1 = new System.Windows.Forms.Panel();
        	this.label5 = new System.Windows.Forms.Label();
        	this.AgentInteractionStateComboBox = new System.Windows.Forms.ComboBox();
        	this.TravelerStateComboBox = new System.Windows.Forms.ComboBox();
        	this.label17 = new System.Windows.Forms.Label();
        	this.UnloadStateComboBox = new System.Windows.Forms.ComboBox();
        	this.ArmStateComboBox = new System.Windows.Forms.ComboBox();
        	this.StorylineStateComboBox = new System.Windows.Forms.ComboBox();
        	this.CombatMissionCtrlStateComboBox = new System.Windows.Forms.ComboBox();
        	this.label13 = new System.Windows.Forms.Label();
        	this.label12 = new System.Windows.Forms.Label();
        	this.label7 = new System.Windows.Forms.Label();
        	this.label6 = new System.Windows.Forms.Label();
        	this.label3 = new System.Windows.Forms.Label();
        	this.tabMissions = new System.Windows.Forms.TabPage();
        	this.AgentInteractionPurposeData = new System.Windows.Forms.Label();
        	this.AgentInteractionPurposelbl = new System.Windows.Forms.Label();
        	this.blacklistedmissionsdeclineddata = new System.Windows.Forms.Label();
        	this.blacklistedmissionsdeclinedlbl = new System.Windows.Forms.Label();
        	this.greylistedmissionsdeclineddata = new System.Windows.Forms.Label();
        	this.greylistedmissionsdeclinedlbl = new System.Windows.Forms.Label();
        	this.LastBlacklistedMissionDeclinedData = new System.Windows.Forms.Label();
        	this.LastGreylistedMissionDeclinedData = new System.Windows.Forms.Label();
        	this.LastBlacklistedMissionDeclinedlbl = new System.Windows.Forms.Label();
        	this.LastGreylistedMissionDeclinedlbl = new System.Windows.Forms.Label();
        	this.MinAgentGreyListStandingsData = new System.Windows.Forms.Label();
        	this.MinAgentBlackListStandingsData = new System.Windows.Forms.Label();
        	this.AgentEffectiveStandingsData = new System.Windows.Forms.Label();
        	this.AgentNameData = new System.Windows.Forms.Label();
        	this.AgentInfolbl = new System.Windows.Forms.Label();
        	this.BlacklistStandingslbl = new System.Windows.Forms.Label();
        	this.GreyListStandingslbl = new System.Windows.Forms.Label();
        	this.CurrentEffectiveStandingslbl = new System.Windows.Forms.Label();
        	this.panel3 = new System.Windows.Forms.Panel();
        	this.BlackListedMissionslbl = new System.Windows.Forms.Label();
        	this.GreyListlbl = new System.Windows.Forms.Label();
        	this.BlacklistedMissionstextbox = new System.Windows.Forms.TextBox();
        	this.GreyListedMissionsTextBox = new System.Windows.Forms.TextBox();
        	this.tabTimeStamps = new System.Windows.Forms.TabPage();
        	this.CurrentTimelbl2 = new System.Windows.Forms.Label();
        	this.CurrentTimeData2 = new System.Windows.Forms.Label();
        	this.CurrentTimeData1 = new System.Windows.Forms.Label();
        	this.CurrentTimelbl = new System.Windows.Forms.Label();
        	this.LastSessionChangeData = new System.Windows.Forms.Label();
        	this.LastSessionChangelbl = new System.Windows.Forms.Label();
        	this.NextStartupActionData = new System.Windows.Forms.Label();
        	this.NextStartupActionlbl = new System.Windows.Forms.Label();
        	this.NextDroneRecallData = new System.Windows.Forms.Label();
        	this.NextDroneRecalllbl = new System.Windows.Forms.Label();
        	this.NextDockActionData = new System.Windows.Forms.Label();
        	this.NextDockActionlbl = new System.Windows.Forms.Label();
        	this.NextUndockActionData = new System.Windows.Forms.Label();
        	this.NextUndockActionlbl = new System.Windows.Forms.Label();
        	this.NextAlignData = new System.Windows.Forms.Label();
        	this.NextAlignlbl = new System.Windows.Forms.Label();
        	this.NextActivateActionData = new System.Windows.Forms.Label();
        	this.NextActivateActionlbl = new System.Windows.Forms.Label();
        	this.NextPainterActionData = new System.Windows.Forms.Label();
        	this.NextPainterActionlbl = new System.Windows.Forms.Label();
        	this.NextNosActionData = new System.Windows.Forms.Label();
        	this.NextNosActionlbl = new System.Windows.Forms.Label();
        	this.NextWebActionData = new System.Windows.Forms.Label();
        	this.NextWebActionlbl = new System.Windows.Forms.Label();
        	this.NextWeaponActionData = new System.Windows.Forms.Label();
        	this.NextWeaponActionlbl = new System.Windows.Forms.Label();
        	this.NextReloadData = new System.Windows.Forms.Label();
        	this.NextReloadlbl = new System.Windows.Forms.Label();
        	this.NextTargetActionData = new System.Windows.Forms.Label();
        	this.NextTargetActionlbl = new System.Windows.Forms.Label();
        	this.NextTravelerActionData = new System.Windows.Forms.Label();
        	this.NextTravelerActionlbl = new System.Windows.Forms.Label();
        	this.NextWarpToData = new System.Windows.Forms.Label();
        	this.NextWarpTolbl = new System.Windows.Forms.Label();
        	this.NextOrbitData = new System.Windows.Forms.Label();
        	this.NextOrbitlbl = new System.Windows.Forms.Label();
        	this.NextApproachActionData = new System.Windows.Forms.Label();
        	this.NextApproachActionlbl = new System.Windows.Forms.Label();
        	this.NextActivateSupportModulesData = new System.Windows.Forms.Label();
        	this.NextActivateSupportModuleslbl = new System.Windows.Forms.Label();
        	this.NextRepModuleActionData = new System.Windows.Forms.Label();
        	this.NextRepModuleActionlbl = new System.Windows.Forms.Label();
        	this.NextAfterburnerActionData = new System.Windows.Forms.Label();
        	this.NextAfterburnerActionlbl = new System.Windows.Forms.Label();
        	this.NextDefenceModuleActionData = new System.Windows.Forms.Label();
        	this.NextDefenceModuleActionlbl = new System.Windows.Forms.Label();
        	this.LastJettisonData = new System.Windows.Forms.Label();
        	this.LastJettisonlbl = new System.Windows.Forms.Label();
        	this.NextLootActionData = new System.Windows.Forms.Label();
        	this.NextLootActionlbl = new System.Windows.Forms.Label();
        	this.NextSalvageActionData = new System.Windows.Forms.Label();
        	this.NextSalvageActionlbl = new System.Windows.Forms.Label();
        	this.NextArmActionData = new System.Windows.Forms.Label();
        	this.NextArmActionlbl = new System.Windows.Forms.Label();
        	this.LastActionData = new System.Windows.Forms.Label();
        	this.LastActionlbl = new System.Windows.Forms.Label();
        	this.NextOpenCargoActionData = new System.Windows.Forms.Label();
        	this.NextOpenCargoActionlbl = new System.Windows.Forms.Label();
        	this.NextOpenHangarActionData = new System.Windows.Forms.Label();
        	this.NextOpenHangarActionlbl = new System.Windows.Forms.Label();
        	this.NextDroneBayActionData = new System.Windows.Forms.Label();
        	this.NextDroneBayActionlbl = new System.Windows.Forms.Label();
        	this.NextOpenLootContainerActionData = new System.Windows.Forms.Label();
        	this.NextOpenLootContainerActionlbl = new System.Windows.Forms.Label();
        	this.NextOpenJournalWindowActionData = new System.Windows.Forms.Label();
        	this.NextOpenJournalWindowActionlbl = new System.Windows.Forms.Label();
        	this.NextOpenContainerInSpaceActionData = new System.Windows.Forms.Label();
        	this.NextOpenContainerInSpaceActionlbl = new System.Windows.Forms.Label();
        	this.tabPage1 = new System.Windows.Forms.TabPage();
        	this.button1 = new System.Windows.Forms.Button();
        	this.tabPage2 = new System.Windows.Forms.TabPage();
        	this.tabControlMain.SuspendLayout();
        	this.tabPage3.SuspendLayout();
        	this.Tabs.SuspendLayout();
        	this.tabConsole.SuspendLayout();
        	this.tabStates.SuspendLayout();
        	this.panel2.SuspendLayout();
        	this.panel1.SuspendLayout();
        	this.tabMissions.SuspendLayout();
        	this.panel3.SuspendLayout();
        	this.tabTimeStamps.SuspendLayout();
        	this.tabPage1.SuspendLayout();
        	this.SuspendLayout();
        	// 
        	// AutoStartCheckBox
        	// 
        	this.AutoStartCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
        	this.AutoStartCheckBox.Location = new System.Drawing.Point(240, 40);
        	this.AutoStartCheckBox.Name = "AutoStartCheckBox";
        	this.AutoStartCheckBox.Size = new System.Drawing.Size(65, 23);
        	this.AutoStartCheckBox.TabIndex = 2;
        	this.AutoStartCheckBox.Text = "Autostart";
        	this.AutoStartCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        	this.AutoStartCheckBox.UseVisualStyleBackColor = true;
        	this.AutoStartCheckBox.CheckedChanged += new System.EventHandler(this.AutoStartCheckBoxCheckedChanged);
        	// 
        	// tUpdateUI
        	// 
        	this.tUpdateUI.Enabled = true;
        	this.tUpdateUI.Tick += new System.EventHandler(this.UpdateUiTick);
        	// 
        	// DamageTypeComboBox
        	// 
        	this.DamageTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.DamageTypeComboBox.FormattingEnabled = true;
        	this.DamageTypeComboBox.Location = new System.Drawing.Point(306, 15);
        	this.DamageTypeComboBox.Name = "DamageTypeComboBox";
        	this.DamageTypeComboBox.Size = new System.Drawing.Size(65, 21);
        	this.DamageTypeComboBox.TabIndex = 4;
        	this.DamageTypeComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// lblDamageType
        	// 
        	this.lblDamageType.AutoSize = true;
        	this.lblDamageType.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
        	this.lblDamageType.Location = new System.Drawing.Point(250, 18);
        	this.lblDamageType.Name = "lblDamageType";
        	this.lblDamageType.Size = new System.Drawing.Size(50, 13);
        	this.lblDamageType.TabIndex = 90;
        	this.lblDamageType.Text = "Damage:";
        	// 
        	// PauseCheckBox
        	// 
        	this.PauseCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
        	this.PauseCheckBox.Location = new System.Drawing.Point(306, 40);
        	this.PauseCheckBox.Name = "PauseCheckBox";
        	this.PauseCheckBox.Size = new System.Drawing.Size(65, 23);
        	this.PauseCheckBox.TabIndex = 6;
        	this.PauseCheckBox.Text = "Pause";
        	this.PauseCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        	this.PauseCheckBox.UseVisualStyleBackColor = true;
        	this.PauseCheckBox.CheckedChanged += new System.EventHandler(this.PauseCheckBoxCheckedChanged);
        	// 
        	// Disable3DCheckBox
        	// 
        	this.Disable3DCheckBox.Appearance = System.Windows.Forms.Appearance.Button;
        	this.Disable3DCheckBox.Location = new System.Drawing.Point(385, 13);
        	this.Disable3DCheckBox.Name = "Disable3DCheckBox";
        	this.Disable3DCheckBox.Size = new System.Drawing.Size(154, 23);
        	this.Disable3DCheckBox.TabIndex = 5;
        	this.Disable3DCheckBox.Text = "Disable 3D";
        	this.Disable3DCheckBox.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
        	this.Disable3DCheckBox.UseVisualStyleBackColor = true;
        	this.Disable3DCheckBox.CheckedChanged += new System.EventHandler(this.Disable3DCheckBoxCheckedChanged);
        	// 
        	// lblMissionName
        	// 
        	this.lblMissionName.AutoSize = true;
        	this.lblMissionName.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
        	this.lblMissionName.Location = new System.Drawing.Point(20, 67);
        	this.lblMissionName.Name = "lblMissionName";
        	this.lblMissionName.Size = new System.Drawing.Size(0, 13);
        	this.lblMissionName.TabIndex = 92;
        	// 
        	// lblCurrentMissionInfo
        	// 
        	this.lblCurrentMissionInfo.Location = new System.Drawing.Point(21, 67);
        	this.lblCurrentMissionInfo.MaximumSize = new System.Drawing.Size(250, 13);
        	this.lblCurrentMissionInfo.MinimumSize = new System.Drawing.Size(275, 13);
        	this.lblCurrentMissionInfo.Name = "lblCurrentMissionInfo";
        	this.lblCurrentMissionInfo.Size = new System.Drawing.Size(275, 13);
        	this.lblCurrentMissionInfo.TabIndex = 93;
        	this.lblCurrentMissionInfo.Text = "[ No Mission Selected Yet ]";
        	// 
        	// lblPocketAction
        	// 
        	this.lblPocketAction.AutoSize = true;
        	this.lblPocketAction.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
        	this.lblPocketAction.Location = new System.Drawing.Point(40, 92);
        	this.lblPocketAction.Name = "lblPocketAction";
        	this.lblPocketAction.Size = new System.Drawing.Size(0, 13);
        	this.lblPocketAction.TabIndex = 94;
        	// 
        	// lblCurrentPocketAction
        	// 
        	this.lblCurrentPocketAction.Location = new System.Drawing.Point(41, 92);
        	this.lblCurrentPocketAction.MaximumSize = new System.Drawing.Size(180, 15);
        	this.lblCurrentPocketAction.MinimumSize = new System.Drawing.Size(180, 15);
        	this.lblCurrentPocketAction.Name = "lblCurrentPocketAction";
        	this.lblCurrentPocketAction.Size = new System.Drawing.Size(180, 15);
        	this.lblCurrentPocketAction.TabIndex = 95;
        	// 
        	// buttonOpenLogDirectory
        	// 
        	this.buttonOpenLogDirectory.Location = new System.Drawing.Point(385, 40);
        	this.buttonOpenLogDirectory.Name = "buttonOpenLogDirectory";
        	this.buttonOpenLogDirectory.Size = new System.Drawing.Size(154, 23);
        	this.buttonOpenLogDirectory.TabIndex = 109;
        	this.buttonOpenLogDirectory.Text = "Log Directory";
        	this.buttonOpenLogDirectory.UseVisualStyleBackColor = true;
        	this.buttonOpenLogDirectory.Click += new System.EventHandler(this.ButtonOpenLogDirectoryClick);
        	// 
        	// BehaviorComboBox
        	// 
        	this.BehaviorComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.BehaviorComboBox.FormattingEnabled = true;
        	this.BehaviorComboBox.Location = new System.Drawing.Point(72, 40);
        	this.BehaviorComboBox.Name = "BehaviorComboBox";
        	this.BehaviorComboBox.Size = new System.Drawing.Size(162, 21);
        	this.BehaviorComboBox.TabIndex = 121;
        	this.BehaviorComboBox.SelectedIndexChanged += new System.EventHandler(this.BehaviorComboBoxSelectedIndexChanged);
        	this.BehaviorComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// label2
        	// 
        	this.label2.Location = new System.Drawing.Point(15, 40);
        	this.label2.Name = "label2";
        	this.label2.Size = new System.Drawing.Size(53, 13);
        	this.label2.TabIndex = 120;
        	this.label2.Text = "Behavior";
        	// 
        	// QuestorStateComboBox
        	// 
        	this.QuestorStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.QuestorStateComboBox.FormattingEnabled = true;
        	this.QuestorStateComboBox.Location = new System.Drawing.Point(72, 13);
        	this.QuestorStateComboBox.Name = "QuestorStateComboBox";
        	this.QuestorStateComboBox.Size = new System.Drawing.Size(162, 21);
        	this.QuestorStateComboBox.TabIndex = 119;
        	this.QuestorStateComboBox.SelectedIndexChanged += new System.EventHandler(this.QuestorStateComboBoxSelectedIndexChanged);
        	this.QuestorStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// QuestorStatelbl
        	// 
        	this.QuestorStatelbl.Location = new System.Drawing.Point(13, 13);
        	this.QuestorStatelbl.Name = "QuestorStatelbl";
        	this.QuestorStatelbl.Size = new System.Drawing.Size(50, 18);
        	this.QuestorStatelbl.TabIndex = 119;
        	this.QuestorStatelbl.Text = "Questor";
        	this.QuestorStatelbl.TextAlign = System.Drawing.ContentAlignment.TopRight;
        	// 
        	// label25
        	// 
        	this.label25.Location = new System.Drawing.Point(26, 24);
        	this.label25.Name = "label25";
        	this.label25.Size = new System.Drawing.Size(42, 16);
        	this.label25.TabIndex = 125;
        	this.label25.Text = "State:";
        	this.label25.TextAlign = System.Drawing.ContentAlignment.TopRight;
        	// 
        	// label26
        	// 
        	this.label26.Location = new System.Drawing.Point(15, 53);
        	this.label26.Name = "label26";
        	this.label26.Size = new System.Drawing.Size(53, 14);
        	this.label26.TabIndex = 126;
        	this.label26.Text = "State:";
        	this.label26.TextAlign = System.Drawing.ContentAlignment.TopRight;
        	// 
        	// tabControlMain
        	// 
        	this.tabControlMain.Controls.Add(this.tabPage3);
        	this.tabControlMain.Controls.Add(this.tabPage2);
        	this.tabControlMain.Location = new System.Drawing.Point(2, 3);
        	this.tabControlMain.Name = "tabControlMain";
        	this.tabControlMain.SelectedIndex = 0;
        	this.tabControlMain.Size = new System.Drawing.Size(799, 425);
        	this.tabControlMain.TabIndex = 127;
        	// 
        	// tabPage3
        	// 
        	this.tabPage3.Controls.Add(this.Tabs);
        	this.tabPage3.Controls.Add(this.label26);
        	this.tabPage3.Controls.Add(this.AutoStartCheckBox);
        	this.tabPage3.Controls.Add(this.label25);
        	this.tabPage3.Controls.Add(this.DamageTypeComboBox);
        	this.tabPage3.Controls.Add(this.lblDamageType);
        	this.tabPage3.Controls.Add(this.PauseCheckBox);
        	this.tabPage3.Controls.Add(this.Disable3DCheckBox);
        	this.tabPage3.Controls.Add(this.lblMissionName);
        	this.tabPage3.Controls.Add(this.lblCurrentMissionInfo);
        	this.tabPage3.Controls.Add(this.buttonOpenLogDirectory);
        	this.tabPage3.Controls.Add(this.lblPocketAction);
        	this.tabPage3.Controls.Add(this.QuestorStatelbl);
        	this.tabPage3.Controls.Add(this.BehaviorComboBox);
        	this.tabPage3.Controls.Add(this.QuestorStateComboBox);
        	this.tabPage3.Controls.Add(this.lblCurrentPocketAction);
        	this.tabPage3.Controls.Add(this.label2);
        	this.tabPage3.Location = new System.Drawing.Point(4, 22);
        	this.tabPage3.Name = "tabPage3";
        	this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
        	this.tabPage3.Size = new System.Drawing.Size(791, 399);
        	this.tabPage3.TabIndex = 1;
        	this.tabPage3.Text = "Questor";
        	this.tabPage3.UseVisualStyleBackColor = true;
        	// 
        	// Tabs
        	// 
        	this.Tabs.Controls.Add(this.tabConsole);
        	this.Tabs.Controls.Add(this.tabStates);
        	this.Tabs.Controls.Add(this.tabMissions);
        	this.Tabs.Controls.Add(this.tabTimeStamps);
        	this.Tabs.Controls.Add(this.tabPage1);
        	this.Tabs.Location = new System.Drawing.Point(6, 92);
        	this.Tabs.Name = "Tabs";
        	this.Tabs.SelectedIndex = 0;
        	this.Tabs.Size = new System.Drawing.Size(782, 304);
        	this.Tabs.TabIndex = 117;
        	// 
        	// tabConsole
        	// 
        	this.tabConsole.Controls.Add(this.logListbox);
        	this.tabConsole.Location = new System.Drawing.Point(4, 22);
        	this.tabConsole.Name = "tabConsole";
        	this.tabConsole.Padding = new System.Windows.Forms.Padding(3);
        	this.tabConsole.Size = new System.Drawing.Size(774, 278);
        	this.tabConsole.TabIndex = 0;
        	this.tabConsole.Text = "Console";
        	this.tabConsole.UseVisualStyleBackColor = true;
        	// 
        	// logListbox
        	// 
        	this.logListbox.FormattingEnabled = true;
        	this.logListbox.Location = new System.Drawing.Point(3, 0);
        	this.logListbox.Name = "logListbox";
        	this.logListbox.Size = new System.Drawing.Size(768, 277);
        	this.logListbox.TabIndex = 0;
        	// 
        	// tabStates
        	// 
        	this.tabStates.Controls.Add(this.dataStopTimeSpecified);
        	this.tabStates.Controls.Add(this.lblStopTimeSpecified);
        	this.tabStates.Controls.Add(this.MissionsThisSessionData);
        	this.tabStates.Controls.Add(this.MissionsThisSessionlbl);
        	this.tabStates.Controls.Add(this.lastKnownGoodConnectedTimeData);
        	this.tabStates.Controls.Add(this.lastKnownGoodConnectedTimeLabel);
        	this.tabStates.Controls.Add(this.lastInStationData);
        	this.tabStates.Controls.Add(this.lastInSpaceData);
        	this.tabStates.Controls.Add(this.lastInStationLabel);
        	this.tabStates.Controls.Add(this.lastInSpaceLabel);
        	this.tabStates.Controls.Add(this.LastFrameData);
        	this.tabStates.Controls.Add(this.lastSessionisreadyData);
        	this.tabStates.Controls.Add(this.lastSessionIsreadylabel);
        	this.tabStates.Controls.Add(this.lastFrameLabel);
        	this.tabStates.Controls.Add(this.label19);
        	this.tabStates.Controls.Add(this.panel2);
        	this.tabStates.Controls.Add(this.panel1);
        	this.tabStates.Location = new System.Drawing.Point(4, 22);
        	this.tabStates.Name = "tabStates";
        	this.tabStates.Padding = new System.Windows.Forms.Padding(3);
        	this.tabStates.Size = new System.Drawing.Size(774, 278);
        	this.tabStates.TabIndex = 1;
        	this.tabStates.Text = "States";
        	this.tabStates.UseVisualStyleBackColor = true;
        	// 
        	// dataStopTimeSpecified
        	// 
        	this.dataStopTimeSpecified.AutoSize = true;
        	this.dataStopTimeSpecified.Location = new System.Drawing.Point(691, 195);
        	this.dataStopTimeSpecified.Name = "dataStopTimeSpecified";
        	this.dataStopTimeSpecified.Size = new System.Drawing.Size(27, 13);
        	this.dataStopTimeSpecified.TabIndex = 209;
        	this.dataStopTimeSpecified.Text = "N/A";
        	// 
        	// lblStopTimeSpecified
        	// 
        	this.lblStopTimeSpecified.AutoSize = true;
        	this.lblStopTimeSpecified.Location = new System.Drawing.Point(595, 195);
        	this.lblStopTimeSpecified.Name = "lblStopTimeSpecified";
        	this.lblStopTimeSpecified.Size = new System.Drawing.Size(94, 13);
        	this.lblStopTimeSpecified.TabIndex = 208;
        	this.lblStopTimeSpecified.Text = "stopTimeSpecified";
        	// 
        	// MissionsThisSessionData
        	// 
        	this.MissionsThisSessionData.AutoSize = true;
        	this.MissionsThisSessionData.Location = new System.Drawing.Point(695, 56);
        	this.MissionsThisSessionData.Name = "MissionsThisSessionData";
        	this.MissionsThisSessionData.Size = new System.Drawing.Size(24, 13);
        	this.MissionsThisSessionData.TabIndex = 207;
        	this.MissionsThisSessionData.Text = "n/a";
        	// 
        	// MissionsThisSessionlbl
        	// 
        	this.MissionsThisSessionlbl.AutoSize = true;
        	this.MissionsThisSessionlbl.Location = new System.Drawing.Point(582, 56);
        	this.MissionsThisSessionlbl.Name = "MissionsThisSessionlbl";
        	this.MissionsThisSessionlbl.Size = new System.Drawing.Size(107, 13);
        	this.MissionsThisSessionlbl.TabIndex = 206;
        	this.MissionsThisSessionlbl.Text = "MissionsThisSession:";
        	// 
        	// lastKnownGoodConnectedTimeData
        	// 
        	this.lastKnownGoodConnectedTimeData.AutoSize = true;
        	this.lastKnownGoodConnectedTimeData.Location = new System.Drawing.Point(691, 173);
        	this.lastKnownGoodConnectedTimeData.Name = "lastKnownGoodConnectedTimeData";
        	this.lastKnownGoodConnectedTimeData.Size = new System.Drawing.Size(27, 13);
        	this.lastKnownGoodConnectedTimeData.TabIndex = 205;
        	this.lastKnownGoodConnectedTimeData.Text = "N/A";
        	// 
        	// lastKnownGoodConnectedTimeLabel
        	// 
        	this.lastKnownGoodConnectedTimeLabel.AutoSize = true;
        	this.lastKnownGoodConnectedTimeLabel.Location = new System.Drawing.Point(532, 173);
        	this.lastKnownGoodConnectedTimeLabel.Name = "lastKnownGoodConnectedTimeLabel";
        	this.lastKnownGoodConnectedTimeLabel.Size = new System.Drawing.Size(157, 13);
        	this.lastKnownGoodConnectedTimeLabel.TabIndex = 204;
        	this.lastKnownGoodConnectedTimeLabel.Text = "lastKnownGoodConnectedTime";
        	// 
        	// lastInStationData
        	// 
        	this.lastInStationData.AutoSize = true;
        	this.lastInStationData.Location = new System.Drawing.Point(691, 151);
        	this.lastInStationData.Name = "lastInStationData";
        	this.lastInStationData.Size = new System.Drawing.Size(27, 13);
        	this.lastInStationData.TabIndex = 203;
        	this.lastInStationData.Text = "N/A";
        	// 
        	// lastInSpaceData
        	// 
        	this.lastInSpaceData.AutoSize = true;
        	this.lastInSpaceData.Location = new System.Drawing.Point(691, 125);
        	this.lastInSpaceData.Name = "lastInSpaceData";
        	this.lastInSpaceData.Size = new System.Drawing.Size(27, 13);
        	this.lastInSpaceData.TabIndex = 202;
        	this.lastInSpaceData.Text = "N/A";
        	// 
        	// lastInStationLabel
        	// 
        	this.lastInStationLabel.AutoSize = true;
        	this.lastInStationLabel.Location = new System.Drawing.Point(621, 151);
        	this.lastInStationLabel.Name = "lastInStationLabel";
        	this.lastInStationLabel.Size = new System.Drawing.Size(68, 13);
        	this.lastInStationLabel.TabIndex = 201;
        	this.lastInStationLabel.Text = "lastInStation:";
        	// 
        	// lastInSpaceLabel
        	// 
        	this.lastInSpaceLabel.AutoSize = true;
        	this.lastInSpaceLabel.Location = new System.Drawing.Point(623, 125);
        	this.lastInSpaceLabel.Name = "lastInSpaceLabel";
        	this.lastInSpaceLabel.Size = new System.Drawing.Size(66, 13);
        	this.lastInSpaceLabel.TabIndex = 200;
        	this.lastInSpaceLabel.Text = "lastInSpace:";
        	// 
        	// LastFrameData
        	// 
        	this.LastFrameData.AutoSize = true;
        	this.LastFrameData.Location = new System.Drawing.Point(691, 83);
        	this.LastFrameData.Name = "LastFrameData";
        	this.LastFrameData.Size = new System.Drawing.Size(27, 13);
        	this.LastFrameData.TabIndex = 199;
        	this.LastFrameData.Text = "N/A";
        	// 
        	// lastSessionisreadyData
        	// 
        	this.lastSessionisreadyData.AutoSize = true;
        	this.lastSessionisreadyData.Location = new System.Drawing.Point(691, 104);
        	this.lastSessionisreadyData.Name = "lastSessionisreadyData";
        	this.lastSessionisreadyData.Size = new System.Drawing.Size(27, 13);
        	this.lastSessionisreadyData.TabIndex = 198;
        	this.lastSessionisreadyData.Text = "N/A";
        	// 
        	// lastSessionIsreadylabel
        	// 
        	this.lastSessionIsreadylabel.AutoSize = true;
        	this.lastSessionIsreadylabel.Location = new System.Drawing.Point(587, 104);
        	this.lastSessionIsreadylabel.Name = "lastSessionIsreadylabel";
        	this.lastSessionIsreadylabel.Size = new System.Drawing.Size(102, 13);
        	this.lastSessionIsreadylabel.TabIndex = 197;
        	this.lastSessionIsreadylabel.Text = "lastSessionIsReady:";
        	// 
        	// lastFrameLabel
        	// 
        	this.lastFrameLabel.AutoSize = true;
        	this.lastFrameLabel.Location = new System.Drawing.Point(634, 83);
        	this.lastFrameLabel.Name = "lastFrameLabel";
        	this.lastFrameLabel.Size = new System.Drawing.Size(55, 13);
        	this.lastFrameLabel.TabIndex = 196;
        	this.lastFrameLabel.Text = "lastFrame:";
        	// 
        	// label19
        	// 
        	this.label19.AutoSize = true;
        	this.label19.Location = new System.Drawing.Point(88, 258);
        	this.label19.Name = "label19";
        	this.label19.Size = new System.Drawing.Size(400, 13);
        	this.label19.TabIndex = 168;
        	this.label19.Text = "it is a very bad idea to change these states unless you understand what will happ" +
	"en";
        	// 
        	// panel2
        	// 
        	this.panel2.Controls.Add(this.label18);
        	this.panel2.Controls.Add(this.label16);
        	this.panel2.Controls.Add(this.label15);
        	this.panel2.Controls.Add(this.SalvageStateComboBox);
        	this.panel2.Controls.Add(this.label10);
        	this.panel2.Controls.Add(this.LocalWatchStateComboBox);
        	this.panel2.Controls.Add(this.label9);
        	this.panel2.Controls.Add(this.CleanupStateComboBox);
        	this.panel2.Controls.Add(this.label8);
        	this.panel2.Controls.Add(this.DronesStateComboBox);
        	this.panel2.Controls.Add(this.CombatStateComboBox);
        	this.panel2.Controls.Add(this.PanicStateComboBox);
        	this.panel2.Location = new System.Drawing.Point(6, 6);
        	this.panel2.Name = "panel2";
        	this.panel2.Size = new System.Drawing.Size(257, 236);
        	this.panel2.TabIndex = 154;
        	// 
        	// label18
        	// 
        	this.label18.AutoSize = true;
        	this.label18.Location = new System.Drawing.Point(11, 140);
        	this.label18.Name = "label18";
        	this.label18.Size = new System.Drawing.Size(96, 13);
        	this.label18.TabIndex = 166;
        	this.label18.Text = "LocalWatch State:";
        	// 
        	// label16
        	// 
        	this.label16.AutoSize = true;
        	this.label16.Location = new System.Drawing.Point(30, 170);
        	this.label16.Name = "label16";
        	this.label16.Size = new System.Drawing.Size(77, 13);
        	this.label16.TabIndex = 165;
        	this.label16.Text = "Salvage State:";
        	// 
        	// label15
        	// 
        	this.label15.AutoSize = true;
        	this.label15.Location = new System.Drawing.Point(42, 19);
        	this.label15.Name = "label15";
        	this.label15.Size = new System.Drawing.Size(65, 13);
        	this.label15.TabIndex = 164;
        	this.label15.Text = "Panic State:";
        	// 
        	// SalvageStateComboBox
        	// 
        	this.SalvageStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.SalvageStateComboBox.FormattingEnabled = true;
        	this.SalvageStateComboBox.Location = new System.Drawing.Point(113, 167);
        	this.SalvageStateComboBox.Name = "SalvageStateComboBox";
        	this.SalvageStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.SalvageStateComboBox.TabIndex = 161;
        	this.SalvageStateComboBox.SelectedIndexChanged += new System.EventHandler(this.SalvageStateComboBoxSelectedIndexChanged);
        	this.SalvageStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// label10
        	// 
        	this.label10.AutoSize = true;
        	this.label10.Location = new System.Drawing.Point(35, 79);
        	this.label10.Name = "label10";
        	this.label10.Size = new System.Drawing.Size(72, 13);
        	this.label10.TabIndex = 160;
        	this.label10.Text = "Drones State:";
        	// 
        	// LocalWatchStateComboBox
        	// 
        	this.LocalWatchStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.LocalWatchStateComboBox.FormattingEnabled = true;
        	this.LocalWatchStateComboBox.Location = new System.Drawing.Point(113, 137);
        	this.LocalWatchStateComboBox.Name = "LocalWatchStateComboBox";
        	this.LocalWatchStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.LocalWatchStateComboBox.TabIndex = 159;
        	this.LocalWatchStateComboBox.SelectedIndexChanged += new System.EventHandler(this.LocalWatchStateComboBoxSelectedIndexChanged);
        	this.LocalWatchStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// label9
        	// 
        	this.label9.AutoSize = true;
        	this.label9.Location = new System.Drawing.Point(33, 49);
        	this.label9.Name = "label9";
        	this.label9.Size = new System.Drawing.Size(74, 13);
        	this.label9.TabIndex = 158;
        	this.label9.Text = "Combat State:";
        	// 
        	// CleanupStateComboBox
        	// 
        	this.CleanupStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.CleanupStateComboBox.FormattingEnabled = true;
        	this.CleanupStateComboBox.Location = new System.Drawing.Point(112, 107);
        	this.CleanupStateComboBox.Name = "CleanupStateComboBox";
        	this.CleanupStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.CleanupStateComboBox.TabIndex = 157;
        	this.CleanupStateComboBox.SelectedIndexChanged += new System.EventHandler(this.CleanupStateComboBoxSelectedIndexChanged);
        	this.CleanupStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// label8
        	// 
        	this.label8.AutoSize = true;
        	this.label8.Location = new System.Drawing.Point(30, 110);
        	this.label8.Name = "label8";
        	this.label8.Size = new System.Drawing.Size(77, 13);
        	this.label8.TabIndex = 156;
        	this.label8.Text = "Cleanup State:";
        	// 
        	// DronesStateComboBox
        	// 
        	this.DronesStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.DronesStateComboBox.FormattingEnabled = true;
        	this.DronesStateComboBox.Location = new System.Drawing.Point(113, 76);
        	this.DronesStateComboBox.Name = "DronesStateComboBox";
        	this.DronesStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.DronesStateComboBox.TabIndex = 155;
        	this.DronesStateComboBox.SelectedIndexChanged += new System.EventHandler(this.DronesStateComboBoxSelectedIndexChanged);
        	this.DronesStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// CombatStateComboBox
        	// 
        	this.CombatStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.CombatStateComboBox.FormattingEnabled = true;
        	this.CombatStateComboBox.Location = new System.Drawing.Point(113, 46);
        	this.CombatStateComboBox.Name = "CombatStateComboBox";
        	this.CombatStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.CombatStateComboBox.TabIndex = 154;
        	this.CombatStateComboBox.SelectedIndexChanged += new System.EventHandler(this.CombatStateComboBoxSelectedIndexChanged);
        	this.CombatStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// PanicStateComboBox
        	// 
        	this.PanicStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.PanicStateComboBox.FormattingEnabled = true;
        	this.PanicStateComboBox.Location = new System.Drawing.Point(113, 17);
        	this.PanicStateComboBox.Name = "PanicStateComboBox";
        	this.PanicStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.PanicStateComboBox.TabIndex = 153;
        	this.PanicStateComboBox.SelectedIndexChanged += new System.EventHandler(this.PanicStateComboBoxSelectedIndexChanged);
        	this.PanicStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// panel1
        	// 
        	this.panel1.Controls.Add(this.label5);
        	this.panel1.Controls.Add(this.AgentInteractionStateComboBox);
        	this.panel1.Controls.Add(this.TravelerStateComboBox);
        	this.panel1.Controls.Add(this.label17);
        	this.panel1.Controls.Add(this.UnloadStateComboBox);
        	this.panel1.Controls.Add(this.ArmStateComboBox);
        	this.panel1.Controls.Add(this.StorylineStateComboBox);
        	this.panel1.Controls.Add(this.CombatMissionCtrlStateComboBox);
        	this.panel1.Controls.Add(this.label13);
        	this.panel1.Controls.Add(this.label12);
        	this.panel1.Controls.Add(this.label7);
        	this.panel1.Controls.Add(this.label6);
        	this.panel1.Controls.Add(this.label3);
        	this.panel1.Location = new System.Drawing.Point(263, 6);
        	this.panel1.Name = "panel1";
        	this.panel1.Size = new System.Drawing.Size(263, 236);
        	this.panel1.TabIndex = 153;
        	// 
        	// label5
        	// 
        	this.label5.AutoSize = true;
        	this.label5.Location = new System.Drawing.Point(0, 210);
        	this.label5.Name = "label5";
        	this.label5.Size = new System.Drawing.Size(105, 13);
        	this.label5.TabIndex = 168;
        	this.label5.Text = "CombatHelper State:";
        	// 
        	// AgentInteractionStateComboBox
        	// 
        	this.AgentInteractionStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.AgentInteractionStateComboBox.FormattingEnabled = true;
        	this.AgentInteractionStateComboBox.Location = new System.Drawing.Point(130, 167);
        	this.AgentInteractionStateComboBox.Name = "AgentInteractionStateComboBox";
        	this.AgentInteractionStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.AgentInteractionStateComboBox.TabIndex = 167;
        	this.AgentInteractionStateComboBox.SelectedIndexChanged += new System.EventHandler(this.AgentInteractionStateComboBoxSelectedIndexChanged);
        	this.AgentInteractionStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// TravelerStateComboBox
        	// 
        	this.TravelerStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.TravelerStateComboBox.FormattingEnabled = true;
        	this.TravelerStateComboBox.Location = new System.Drawing.Point(130, 138);
        	this.TravelerStateComboBox.Name = "TravelerStateComboBox";
        	this.TravelerStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.TravelerStateComboBox.TabIndex = 166;
        	this.TravelerStateComboBox.SelectedIndexChanged += new System.EventHandler(this.TravelerStateComboBoxSelectedIndexChanged);
        	this.TravelerStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// label17
        	// 
        	this.label17.AutoSize = true;
        	this.label17.Location = new System.Drawing.Point(48, 141);
        	this.label17.Name = "label17";
        	this.label17.Size = new System.Drawing.Size(77, 13);
        	this.label17.TabIndex = 165;
        	this.label17.Text = "Traveler State:";
        	// 
        	// UnloadStateComboBox
        	// 
        	this.UnloadStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.UnloadStateComboBox.FormattingEnabled = true;
        	this.UnloadStateComboBox.Location = new System.Drawing.Point(130, 107);
        	this.UnloadStateComboBox.Name = "UnloadStateComboBox";
        	this.UnloadStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.UnloadStateComboBox.TabIndex = 164;
        	this.UnloadStateComboBox.SelectedIndexChanged += new System.EventHandler(this.UnloadStateComboBoxSelectedIndexChanged);
        	this.UnloadStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// ArmStateComboBox
        	// 
        	this.ArmStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.ArmStateComboBox.FormattingEnabled = true;
        	this.ArmStateComboBox.Location = new System.Drawing.Point(131, 76);
        	this.ArmStateComboBox.Name = "ArmStateComboBox";
        	this.ArmStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.ArmStateComboBox.TabIndex = 163;
        	this.ArmStateComboBox.SelectedIndexChanged += new System.EventHandler(this.ArmStateComboBoxSelectedIndexChanged);
        	this.ArmStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// StorylineStateComboBox
        	// 
        	this.StorylineStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.StorylineStateComboBox.FormattingEnabled = true;
        	this.StorylineStateComboBox.Location = new System.Drawing.Point(130, 46);
        	this.StorylineStateComboBox.Name = "StorylineStateComboBox";
        	this.StorylineStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.StorylineStateComboBox.TabIndex = 162;
        	this.StorylineStateComboBox.SelectedIndexChanged += new System.EventHandler(this.StorylineStateComboBoxSelectedIndexChanged);
        	this.StorylineStateComboBox.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.DisableMouseWheel);
        	// 
        	// CombatMissionCtrlStateComboBox
        	// 
        	this.CombatMissionCtrlStateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        	this.CombatMissionCtrlStateComboBox.FormattingEnabled = true;
        	this.CombatMissionCtrlStateComboBox.Location = new System.Drawing.Point(130, 16);
        	this.CombatMissionCtrlStateComboBox.Name = "CombatMissionCtrlStateComboBox";
        	this.CombatMissionCtrlStateComboBox.Size = new System.Drawing.Size(130, 21);
        	this.CombatMissionCtrlStateComboBox.TabIndex = 160;
        	// 
        	// label13
        	// 
        	this.label13.AutoSize = true;
        	this.label13.Location = new System.Drawing.Point(1, 19);
        	this.label13.Name = "label13";
        	this.label13.Size = new System.Drawing.Size(124, 13);
        	this.label13.TabIndex = 159;
        	this.label13.Text = "CombatMissionCtrl State:";
        	// 
        	// label12
        	// 
        	this.label12.AutoSize = true;
        	this.label12.Location = new System.Drawing.Point(47, 49);
        	this.label12.Name = "label12";
        	this.label12.Size = new System.Drawing.Size(78, 13);
        	this.label12.TabIndex = 157;
        	this.label12.Text = "Storyline State:";
        	// 
        	// label7
        	// 
        	this.label7.AutoSize = true;
        	this.label7.Location = new System.Drawing.Point(9, 170);
        	this.label7.Name = "label7";
        	this.label7.Size = new System.Drawing.Size(116, 13);
        	this.label7.TabIndex = 156;
        	this.label7.Text = "AgentInteraction State:";
        	// 
        	// label6
        	// 
        	this.label6.AutoSize = true;
        	this.label6.Location = new System.Drawing.Point(52, 110);
        	this.label6.Name = "label6";
        	this.label6.Size = new System.Drawing.Size(72, 13);
        	this.label6.TabIndex = 155;
        	this.label6.Text = "Unload State:";
        	// 
        	// label3
        	// 
        	this.label3.AutoSize = true;
        	this.label3.Location = new System.Drawing.Point(69, 81);
        	this.label3.Name = "label3";
        	this.label3.Size = new System.Drawing.Size(56, 13);
        	this.label3.TabIndex = 154;
        	this.label3.Text = "Arm State:";
        	// 
        	// tabMissions
        	// 
        	this.tabMissions.Controls.Add(this.AgentInteractionPurposeData);
        	this.tabMissions.Controls.Add(this.AgentInteractionPurposelbl);
        	this.tabMissions.Controls.Add(this.blacklistedmissionsdeclineddata);
        	this.tabMissions.Controls.Add(this.blacklistedmissionsdeclinedlbl);
        	this.tabMissions.Controls.Add(this.greylistedmissionsdeclineddata);
        	this.tabMissions.Controls.Add(this.greylistedmissionsdeclinedlbl);
        	this.tabMissions.Controls.Add(this.LastBlacklistedMissionDeclinedData);
        	this.tabMissions.Controls.Add(this.LastGreylistedMissionDeclinedData);
        	this.tabMissions.Controls.Add(this.LastBlacklistedMissionDeclinedlbl);
        	this.tabMissions.Controls.Add(this.LastGreylistedMissionDeclinedlbl);
        	this.tabMissions.Controls.Add(this.MinAgentGreyListStandingsData);
        	this.tabMissions.Controls.Add(this.MinAgentBlackListStandingsData);
        	this.tabMissions.Controls.Add(this.AgentEffectiveStandingsData);
        	this.tabMissions.Controls.Add(this.AgentNameData);
        	this.tabMissions.Controls.Add(this.AgentInfolbl);
        	this.tabMissions.Controls.Add(this.BlacklistStandingslbl);
        	this.tabMissions.Controls.Add(this.GreyListStandingslbl);
        	this.tabMissions.Controls.Add(this.CurrentEffectiveStandingslbl);
        	this.tabMissions.Controls.Add(this.panel3);
        	this.tabMissions.Location = new System.Drawing.Point(4, 22);
        	this.tabMissions.Name = "tabMissions";
        	this.tabMissions.Padding = new System.Windows.Forms.Padding(3);
        	this.tabMissions.Size = new System.Drawing.Size(774, 278);
        	this.tabMissions.TabIndex = 5;
        	this.tabMissions.Text = "Missions";
        	this.tabMissions.UseVisualStyleBackColor = true;
        	// 
        	// AgentInteractionPurposeData
        	// 
        	this.AgentInteractionPurposeData.AutoSize = true;
        	this.AgentInteractionPurposeData.Location = new System.Drawing.Point(582, 246);
        	this.AgentInteractionPurposeData.Name = "AgentInteractionPurposeData";
        	this.AgentInteractionPurposeData.Size = new System.Drawing.Size(24, 13);
        	this.AgentInteractionPurposeData.TabIndex = 18;
        	this.AgentInteractionPurposeData.Text = "n/a";
        	// 
        	// AgentInteractionPurposelbl
        	// 
        	this.AgentInteractionPurposelbl.AutoSize = true;
        	this.AgentInteractionPurposelbl.Location = new System.Drawing.Point(452, 246);
        	this.AgentInteractionPurposelbl.Name = "AgentInteractionPurposelbl";
        	this.AgentInteractionPurposelbl.Size = new System.Drawing.Size(124, 13);
        	this.AgentInteractionPurposelbl.TabIndex = 17;
        	this.AgentInteractionPurposelbl.Text = "AgentInteractionPurpose";
        	// 
        	// blacklistedmissionsdeclineddata
        	// 
        	this.blacklistedmissionsdeclineddata.AutoSize = true;
        	this.blacklistedmissionsdeclineddata.Location = new System.Drawing.Point(582, 171);
        	this.blacklistedmissionsdeclineddata.Name = "blacklistedmissionsdeclineddata";
        	this.blacklistedmissionsdeclineddata.Size = new System.Drawing.Size(24, 13);
        	this.blacklistedmissionsdeclineddata.TabIndex = 16;
        	this.blacklistedmissionsdeclineddata.Text = "n/a";
        	// 
        	// blacklistedmissionsdeclinedlbl
        	// 
        	this.blacklistedmissionsdeclinedlbl.AutoSize = true;
        	this.blacklistedmissionsdeclinedlbl.Location = new System.Drawing.Point(463, 171);
        	this.blacklistedmissionsdeclinedlbl.Name = "blacklistedmissionsdeclinedlbl";
        	this.blacklistedmissionsdeclinedlbl.Size = new System.Drawing.Size(113, 13);
        	this.blacklistedmissionsdeclinedlbl.TabIndex = 15;
        	this.blacklistedmissionsdeclinedlbl.Text = "Blacklisted # Declined";
        	// 
        	// greylistedmissionsdeclineddata
        	// 
        	this.greylistedmissionsdeclineddata.AutoSize = true;
        	this.greylistedmissionsdeclineddata.Location = new System.Drawing.Point(581, 112);
        	this.greylistedmissionsdeclineddata.Name = "greylistedmissionsdeclineddata";
        	this.greylistedmissionsdeclineddata.Size = new System.Drawing.Size(24, 13);
        	this.greylistedmissionsdeclineddata.TabIndex = 14;
        	this.greylistedmissionsdeclineddata.Text = "n/a";
        	// 
        	// greylistedmissionsdeclinedlbl
        	// 
        	this.greylistedmissionsdeclinedlbl.AutoSize = true;
        	this.greylistedmissionsdeclinedlbl.Location = new System.Drawing.Point(467, 112);
        	this.greylistedmissionsdeclinedlbl.Name = "greylistedmissionsdeclinedlbl";
        	this.greylistedmissionsdeclinedlbl.Size = new System.Drawing.Size(112, 13);
        	this.greylistedmissionsdeclinedlbl.TabIndex = 13;
        	this.greylistedmissionsdeclinedlbl.Text = "GreyListed # Declined";
        	// 
        	// LastBlacklistedMissionDeclinedData
        	// 
        	this.LastBlacklistedMissionDeclinedData.AutoSize = true;
        	this.LastBlacklistedMissionDeclinedData.Location = new System.Drawing.Point(582, 184);
        	this.LastBlacklistedMissionDeclinedData.Name = "LastBlacklistedMissionDeclinedData";
        	this.LastBlacklistedMissionDeclinedData.Size = new System.Drawing.Size(24, 13);
        	this.LastBlacklistedMissionDeclinedData.TabIndex = 12;
        	this.LastBlacklistedMissionDeclinedData.Text = "n/a";
        	// 
        	// LastGreylistedMissionDeclinedData
        	// 
        	this.LastGreylistedMissionDeclinedData.AutoSize = true;
        	this.LastGreylistedMissionDeclinedData.Location = new System.Drawing.Point(581, 125);
        	this.LastGreylistedMissionDeclinedData.Name = "LastGreylistedMissionDeclinedData";
        	this.LastGreylistedMissionDeclinedData.Size = new System.Drawing.Size(24, 13);
        	this.LastGreylistedMissionDeclinedData.TabIndex = 11;
        	this.LastGreylistedMissionDeclinedData.Text = "n/a";
        	// 
        	// LastBlacklistedMissionDeclinedlbl
        	// 
        	this.LastBlacklistedMissionDeclinedlbl.AutoSize = true;
        	this.LastBlacklistedMissionDeclinedlbl.Location = new System.Drawing.Point(462, 184);
        	this.LastBlacklistedMissionDeclinedlbl.Name = "LastBlacklistedMissionDeclinedlbl";
        	this.LastBlacklistedMissionDeclinedlbl.Size = new System.Drawing.Size(118, 13);
        	this.LastBlacklistedMissionDeclinedlbl.TabIndex = 10;
        	this.LastBlacklistedMissionDeclinedlbl.Text = "Last BlackList Declined";
        	// 
        	// LastGreylistedMissionDeclinedlbl
        	// 
        	this.LastGreylistedMissionDeclinedlbl.AutoSize = true;
        	this.LastGreylistedMissionDeclinedlbl.Location = new System.Drawing.Point(466, 125);
        	this.LastGreylistedMissionDeclinedlbl.Name = "LastGreylistedMissionDeclinedlbl";
        	this.LastGreylistedMissionDeclinedlbl.Size = new System.Drawing.Size(113, 13);
        	this.LastGreylistedMissionDeclinedlbl.TabIndex = 9;
        	this.LastGreylistedMissionDeclinedlbl.Text = "Last GreyList Declined";
        	// 
        	// MinAgentGreyListStandingsData
        	// 
        	this.MinAgentGreyListStandingsData.AutoSize = true;
        	this.MinAgentGreyListStandingsData.Location = new System.Drawing.Point(582, 99);
        	this.MinAgentGreyListStandingsData.Name = "MinAgentGreyListStandingsData";
        	this.MinAgentGreyListStandingsData.Size = new System.Drawing.Size(24, 13);
        	this.MinAgentGreyListStandingsData.TabIndex = 8;
        	this.MinAgentGreyListStandingsData.Text = "n/a";
        	// 
        	// MinAgentBlackListStandingsData
        	// 
        	this.MinAgentBlackListStandingsData.AutoSize = true;
        	this.MinAgentBlackListStandingsData.Location = new System.Drawing.Point(582, 158);
        	this.MinAgentBlackListStandingsData.Name = "MinAgentBlackListStandingsData";
        	this.MinAgentBlackListStandingsData.Size = new System.Drawing.Size(24, 13);
        	this.MinAgentBlackListStandingsData.TabIndex = 7;
        	this.MinAgentBlackListStandingsData.Text = "n/a";
        	// 
        	// AgentEffectiveStandingsData
        	// 
        	this.AgentEffectiveStandingsData.AutoSize = true;
        	this.AgentEffectiveStandingsData.Location = new System.Drawing.Point(581, 42);
        	this.AgentEffectiveStandingsData.Name = "AgentEffectiveStandingsData";
        	this.AgentEffectiveStandingsData.Size = new System.Drawing.Size(24, 13);
        	this.AgentEffectiveStandingsData.TabIndex = 6;
        	this.AgentEffectiveStandingsData.Text = "n/a";
        	// 
        	// AgentNameData
        	// 
        	this.AgentNameData.AutoSize = true;
        	this.AgentNameData.Location = new System.Drawing.Point(519, 12);
        	this.AgentNameData.Name = "AgentNameData";
        	this.AgentNameData.Size = new System.Drawing.Size(24, 13);
        	this.AgentNameData.TabIndex = 5;
        	this.AgentNameData.Text = "n/a";
        	// 
        	// AgentInfolbl
        	// 
        	this.AgentInfolbl.AutoSize = true;
        	this.AgentInfolbl.Location = new System.Drawing.Point(475, 12);
        	this.AgentInfolbl.Name = "AgentInfolbl";
        	this.AgentInfolbl.Size = new System.Drawing.Size(38, 13);
        	this.AgentInfolbl.TabIndex = 4;
        	this.AgentInfolbl.Text = "Agent:";
        	// 
        	// BlacklistStandingslbl
        	// 
        	this.BlacklistStandingslbl.AutoSize = true;
        	this.BlacklistStandingslbl.Location = new System.Drawing.Point(480, 158);
        	this.BlacklistStandingslbl.Name = "BlacklistStandingslbl";
        	this.BlacklistStandingslbl.Size = new System.Drawing.Size(96, 13);
        	this.BlacklistStandingslbl.TabIndex = 3;
        	this.BlacklistStandingslbl.Text = "Blacklist Standings";
        	// 
        	// GreyListStandingslbl
        	// 
        	this.GreyListStandingslbl.AutoSize = true;
        	this.GreyListStandingslbl.Location = new System.Drawing.Point(481, 99);
        	this.GreyListStandingslbl.Name = "GreyListStandingslbl";
        	this.GreyListStandingslbl.Size = new System.Drawing.Size(95, 13);
        	this.GreyListStandingslbl.TabIndex = 2;
        	this.GreyListStandingslbl.Text = "GreyList Standings";
        	// 
        	// CurrentEffectiveStandingslbl
        	// 
        	this.CurrentEffectiveStandingslbl.AutoSize = true;
        	this.CurrentEffectiveStandingslbl.Location = new System.Drawing.Point(471, 42);
        	this.CurrentEffectiveStandingslbl.Name = "CurrentEffectiveStandingslbl";
        	this.CurrentEffectiveStandingslbl.Size = new System.Drawing.Size(99, 13);
        	this.CurrentEffectiveStandingslbl.TabIndex = 1;
        	this.CurrentEffectiveStandingslbl.Text = "Effective Standings";
        	// 
        	// panel3
        	// 
        	this.panel3.Controls.Add(this.BlackListedMissionslbl);
        	this.panel3.Controls.Add(this.GreyListlbl);
        	this.panel3.Controls.Add(this.BlacklistedMissionstextbox);
        	this.panel3.Controls.Add(this.GreyListedMissionsTextBox);
        	this.panel3.Location = new System.Drawing.Point(6, 12);
        	this.panel3.Name = "panel3";
        	this.panel3.Size = new System.Drawing.Size(449, 258);
        	this.panel3.TabIndex = 0;
        	// 
        	// BlackListedMissionslbl
        	// 
        	this.BlackListedMissionslbl.AutoSize = true;
        	this.BlackListedMissionslbl.Location = new System.Drawing.Point(224, 5);
        	this.BlackListedMissionslbl.Name = "BlackListedMissionslbl";
        	this.BlackListedMissionslbl.Size = new System.Drawing.Size(105, 13);
        	this.BlackListedMissionslbl.TabIndex = 3;
        	this.BlackListedMissionslbl.Text = "BlackListed Missions";
        	// 
        	// GreyListlbl
        	// 
        	this.GreyListlbl.AutoSize = true;
        	this.GreyListlbl.Location = new System.Drawing.Point(12, 5);
        	this.GreyListlbl.Name = "GreyListlbl";
        	this.GreyListlbl.Size = new System.Drawing.Size(100, 13);
        	this.GreyListlbl.TabIndex = 2;
        	this.GreyListlbl.Text = "GreyListed Missions";
        	// 
        	// BlacklistedMissionstextbox
        	// 
        	this.BlacklistedMissionstextbox.Location = new System.Drawing.Point(227, 21);
        	this.BlacklistedMissionstextbox.Multiline = true;
        	this.BlacklistedMissionstextbox.Name = "BlacklistedMissionstextbox";
        	this.BlacklistedMissionstextbox.ReadOnly = true;
        	this.BlacklistedMissionstextbox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        	this.BlacklistedMissionstextbox.Size = new System.Drawing.Size(206, 234);
        	this.BlacklistedMissionstextbox.TabIndex = 1;
        	// 
        	// GreyListedMissionsTextBox
        	// 
        	this.GreyListedMissionsTextBox.Location = new System.Drawing.Point(15, 21);
        	this.GreyListedMissionsTextBox.Multiline = true;
        	this.GreyListedMissionsTextBox.Name = "GreyListedMissionsTextBox";
        	this.GreyListedMissionsTextBox.ReadOnly = true;
        	this.GreyListedMissionsTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        	this.GreyListedMissionsTextBox.Size = new System.Drawing.Size(206, 234);
        	this.GreyListedMissionsTextBox.TabIndex = 0;
        	// 
        	// tabTimeStamps
        	// 
        	this.tabTimeStamps.Controls.Add(this.CurrentTimelbl2);
        	this.tabTimeStamps.Controls.Add(this.CurrentTimeData2);
        	this.tabTimeStamps.Controls.Add(this.CurrentTimeData1);
        	this.tabTimeStamps.Controls.Add(this.CurrentTimelbl);
        	this.tabTimeStamps.Controls.Add(this.LastSessionChangeData);
        	this.tabTimeStamps.Controls.Add(this.LastSessionChangelbl);
        	this.tabTimeStamps.Controls.Add(this.NextStartupActionData);
        	this.tabTimeStamps.Controls.Add(this.NextStartupActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextDroneRecallData);
        	this.tabTimeStamps.Controls.Add(this.NextDroneRecalllbl);
        	this.tabTimeStamps.Controls.Add(this.NextDockActionData);
        	this.tabTimeStamps.Controls.Add(this.NextDockActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextUndockActionData);
        	this.tabTimeStamps.Controls.Add(this.NextUndockActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextAlignData);
        	this.tabTimeStamps.Controls.Add(this.NextAlignlbl);
        	this.tabTimeStamps.Controls.Add(this.NextActivateActionData);
        	this.tabTimeStamps.Controls.Add(this.NextActivateActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextPainterActionData);
        	this.tabTimeStamps.Controls.Add(this.NextPainterActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextNosActionData);
        	this.tabTimeStamps.Controls.Add(this.NextNosActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextWebActionData);
        	this.tabTimeStamps.Controls.Add(this.NextWebActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextWeaponActionData);
        	this.tabTimeStamps.Controls.Add(this.NextWeaponActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextReloadData);
        	this.tabTimeStamps.Controls.Add(this.NextReloadlbl);
        	this.tabTimeStamps.Controls.Add(this.NextTargetActionData);
        	this.tabTimeStamps.Controls.Add(this.NextTargetActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextTravelerActionData);
        	this.tabTimeStamps.Controls.Add(this.NextTravelerActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextWarpToData);
        	this.tabTimeStamps.Controls.Add(this.NextWarpTolbl);
        	this.tabTimeStamps.Controls.Add(this.NextOrbitData);
        	this.tabTimeStamps.Controls.Add(this.NextOrbitlbl);
        	this.tabTimeStamps.Controls.Add(this.NextApproachActionData);
        	this.tabTimeStamps.Controls.Add(this.NextApproachActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextActivateSupportModulesData);
        	this.tabTimeStamps.Controls.Add(this.NextActivateSupportModuleslbl);
        	this.tabTimeStamps.Controls.Add(this.NextRepModuleActionData);
        	this.tabTimeStamps.Controls.Add(this.NextRepModuleActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextAfterburnerActionData);
        	this.tabTimeStamps.Controls.Add(this.NextAfterburnerActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextDefenceModuleActionData);
        	this.tabTimeStamps.Controls.Add(this.NextDefenceModuleActionlbl);
        	this.tabTimeStamps.Controls.Add(this.LastJettisonData);
        	this.tabTimeStamps.Controls.Add(this.LastJettisonlbl);
        	this.tabTimeStamps.Controls.Add(this.NextLootActionData);
        	this.tabTimeStamps.Controls.Add(this.NextLootActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextSalvageActionData);
        	this.tabTimeStamps.Controls.Add(this.NextSalvageActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextArmActionData);
        	this.tabTimeStamps.Controls.Add(this.NextArmActionlbl);
        	this.tabTimeStamps.Controls.Add(this.LastActionData);
        	this.tabTimeStamps.Controls.Add(this.LastActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextOpenCargoActionData);
        	this.tabTimeStamps.Controls.Add(this.NextOpenCargoActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextOpenHangarActionData);
        	this.tabTimeStamps.Controls.Add(this.NextOpenHangarActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextDroneBayActionData);
        	this.tabTimeStamps.Controls.Add(this.NextDroneBayActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextOpenLootContainerActionData);
        	this.tabTimeStamps.Controls.Add(this.NextOpenLootContainerActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextOpenJournalWindowActionData);
        	this.tabTimeStamps.Controls.Add(this.NextOpenJournalWindowActionlbl);
        	this.tabTimeStamps.Controls.Add(this.NextOpenContainerInSpaceActionData);
        	this.tabTimeStamps.Controls.Add(this.NextOpenContainerInSpaceActionlbl);
        	this.tabTimeStamps.Location = new System.Drawing.Point(4, 22);
        	this.tabTimeStamps.Name = "tabTimeStamps";
        	this.tabTimeStamps.Padding = new System.Windows.Forms.Padding(3);
        	this.tabTimeStamps.Size = new System.Drawing.Size(774, 278);
        	this.tabTimeStamps.TabIndex = 6;
        	this.tabTimeStamps.Text = "TimeStamps";
        	this.tabTimeStamps.UseVisualStyleBackColor = true;
        	// 
        	// CurrentTimelbl2
        	// 
        	this.CurrentTimelbl2.AutoSize = true;
        	this.CurrentTimelbl2.Location = new System.Drawing.Point(314, 21);
        	this.CurrentTimelbl2.Name = "CurrentTimelbl2";
        	this.CurrentTimelbl2.Size = new System.Drawing.Size(87, 13);
        	this.CurrentTimelbl2.TabIndex = 284;
        	this.CurrentTimelbl2.Text = "DateTime (Now):";
        	// 
        	// CurrentTimeData2
        	// 
        	this.CurrentTimeData2.AutoSize = true;
        	this.CurrentTimeData2.Location = new System.Drawing.Point(493, 21);
        	this.CurrentTimeData2.Name = "CurrentTimeData2";
        	this.CurrentTimeData2.Size = new System.Drawing.Size(27, 13);
        	this.CurrentTimeData2.TabIndex = 282;
        	this.CurrentTimeData2.Text = "N/A";
        	// 
        	// CurrentTimeData1
        	// 
        	this.CurrentTimeData1.AutoSize = true;
        	this.CurrentTimeData1.Location = new System.Drawing.Point(214, 21);
        	this.CurrentTimeData1.Name = "CurrentTimeData1";
        	this.CurrentTimeData1.Size = new System.Drawing.Size(27, 13);
        	this.CurrentTimeData1.TabIndex = 281;
        	this.CurrentTimeData1.Text = "N/A";
        	// 
        	// CurrentTimelbl
        	// 
        	this.CurrentTimelbl.AutoSize = true;
        	this.CurrentTimelbl.Location = new System.Drawing.Point(35, 21);
        	this.CurrentTimelbl.Name = "CurrentTimelbl";
        	this.CurrentTimelbl.Size = new System.Drawing.Size(87, 13);
        	this.CurrentTimelbl.TabIndex = 280;
        	this.CurrentTimelbl.Text = "DateTime (Now):";
        	// 
        	// LastSessionChangeData
        	// 
        	this.LastSessionChangeData.AutoSize = true;
        	this.LastSessionChangeData.Location = new System.Drawing.Point(493, 232);
        	this.LastSessionChangeData.Name = "LastSessionChangeData";
        	this.LastSessionChangeData.Size = new System.Drawing.Size(27, 13);
        	this.LastSessionChangeData.TabIndex = 279;
        	this.LastSessionChangeData.Text = "N/A";
        	// 
        	// LastSessionChangelbl
        	// 
        	this.LastSessionChangelbl.AutoSize = true;
        	this.LastSessionChangelbl.Location = new System.Drawing.Point(314, 232);
        	this.LastSessionChangelbl.Name = "LastSessionChangelbl";
        	this.LastSessionChangelbl.Size = new System.Drawing.Size(101, 13);
        	this.LastSessionChangelbl.TabIndex = 278;
        	this.LastSessionChangelbl.Text = "LastSessionChange";
        	// 
        	// NextStartupActionData
        	// 
        	this.NextStartupActionData.AutoSize = true;
        	this.NextStartupActionData.Location = new System.Drawing.Point(493, 219);
        	this.NextStartupActionData.Name = "NextStartupActionData";
        	this.NextStartupActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextStartupActionData.TabIndex = 277;
        	this.NextStartupActionData.Text = "N/A";
        	// 
        	// NextStartupActionlbl
        	// 
        	this.NextStartupActionlbl.AutoSize = true;
        	this.NextStartupActionlbl.Location = new System.Drawing.Point(314, 219);
        	this.NextStartupActionlbl.Name = "NextStartupActionlbl";
        	this.NextStartupActionlbl.Size = new System.Drawing.Size(93, 13);
        	this.NextStartupActionlbl.TabIndex = 276;
        	this.NextStartupActionlbl.Text = "NextStartupAction";
        	// 
        	// NextDroneRecallData
        	// 
        	this.NextDroneRecallData.AutoSize = true;
        	this.NextDroneRecallData.Location = new System.Drawing.Point(493, 206);
        	this.NextDroneRecallData.Name = "NextDroneRecallData";
        	this.NextDroneRecallData.Size = new System.Drawing.Size(27, 13);
        	this.NextDroneRecallData.TabIndex = 275;
        	this.NextDroneRecallData.Text = "N/A";
        	// 
        	// NextDroneRecalllbl
        	// 
        	this.NextDroneRecalllbl.AutoSize = true;
        	this.NextDroneRecalllbl.Location = new System.Drawing.Point(314, 206);
        	this.NextDroneRecalllbl.Name = "NextDroneRecalllbl";
        	this.NextDroneRecalllbl.Size = new System.Drawing.Size(88, 13);
        	this.NextDroneRecalllbl.TabIndex = 274;
        	this.NextDroneRecalllbl.Text = "NextDroneRecall";
        	// 
        	// NextDockActionData
        	// 
        	this.NextDockActionData.AutoSize = true;
        	this.NextDockActionData.Location = new System.Drawing.Point(493, 193);
        	this.NextDockActionData.Name = "NextDockActionData";
        	this.NextDockActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextDockActionData.TabIndex = 273;
        	this.NextDockActionData.Text = "N/A";
        	// 
        	// NextDockActionlbl
        	// 
        	this.NextDockActionlbl.AutoSize = true;
        	this.NextDockActionlbl.Location = new System.Drawing.Point(314, 193);
        	this.NextDockActionlbl.Name = "NextDockActionlbl";
        	this.NextDockActionlbl.Size = new System.Drawing.Size(85, 13);
        	this.NextDockActionlbl.TabIndex = 272;
        	this.NextDockActionlbl.Text = "NextDockAction";
        	// 
        	// NextUndockActionData
        	// 
        	this.NextUndockActionData.AutoSize = true;
        	this.NextUndockActionData.Location = new System.Drawing.Point(493, 180);
        	this.NextUndockActionData.Name = "NextUndockActionData";
        	this.NextUndockActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextUndockActionData.TabIndex = 271;
        	this.NextUndockActionData.Text = "N/A";
        	// 
        	// NextUndockActionlbl
        	// 
        	this.NextUndockActionlbl.AutoSize = true;
        	this.NextUndockActionlbl.Location = new System.Drawing.Point(314, 180);
        	this.NextUndockActionlbl.Name = "NextUndockActionlbl";
        	this.NextUndockActionlbl.Size = new System.Drawing.Size(97, 13);
        	this.NextUndockActionlbl.TabIndex = 270;
        	this.NextUndockActionlbl.Text = "NextUndockAction";
        	// 
        	// NextAlignData
        	// 
        	this.NextAlignData.AutoSize = true;
        	this.NextAlignData.Location = new System.Drawing.Point(493, 167);
        	this.NextAlignData.Name = "NextAlignData";
        	this.NextAlignData.Size = new System.Drawing.Size(27, 13);
        	this.NextAlignData.TabIndex = 269;
        	this.NextAlignData.Text = "N/A";
        	// 
        	// NextAlignlbl
        	// 
        	this.NextAlignlbl.AutoSize = true;
        	this.NextAlignlbl.Location = new System.Drawing.Point(314, 167);
        	this.NextAlignlbl.Name = "NextAlignlbl";
        	this.NextAlignlbl.Size = new System.Drawing.Size(52, 13);
        	this.NextAlignlbl.TabIndex = 268;
        	this.NextAlignlbl.Text = "NextAlign";
        	// 
        	// NextActivateActionData
        	// 
        	this.NextActivateActionData.AutoSize = true;
        	this.NextActivateActionData.Location = new System.Drawing.Point(493, 154);
        	this.NextActivateActionData.Name = "NextActivateActionData";
        	this.NextActivateActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextActivateActionData.TabIndex = 267;
        	this.NextActivateActionData.Text = "N/A";
        	// 
        	// NextActivateActionlbl
        	// 
        	this.NextActivateActionlbl.AutoSize = true;
        	this.NextActivateActionlbl.Location = new System.Drawing.Point(314, 152);
        	this.NextActivateActionlbl.Name = "NextActivateActionlbl";
        	this.NextActivateActionlbl.Size = new System.Drawing.Size(98, 13);
        	this.NextActivateActionlbl.TabIndex = 266;
        	this.NextActivateActionlbl.Text = "NextActivateAction";
        	// 
        	// NextPainterActionData
        	// 
        	this.NextPainterActionData.AutoSize = true;
        	this.NextPainterActionData.Location = new System.Drawing.Point(493, 139);
        	this.NextPainterActionData.Name = "NextPainterActionData";
        	this.NextPainterActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextPainterActionData.TabIndex = 265;
        	this.NextPainterActionData.Text = "N/A";
        	// 
        	// NextPainterActionlbl
        	// 
        	this.NextPainterActionlbl.AutoSize = true;
        	this.NextPainterActionlbl.Location = new System.Drawing.Point(314, 139);
        	this.NextPainterActionlbl.Name = "NextPainterActionlbl";
        	this.NextPainterActionlbl.Size = new System.Drawing.Size(92, 13);
        	this.NextPainterActionlbl.TabIndex = 264;
        	this.NextPainterActionlbl.Text = "NextPainterAction";
        	// 
        	// NextNosActionData
        	// 
        	this.NextNosActionData.AutoSize = true;
        	this.NextNosActionData.Location = new System.Drawing.Point(493, 126);
        	this.NextNosActionData.Name = "NextNosActionData";
        	this.NextNosActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextNosActionData.TabIndex = 263;
        	this.NextNosActionData.Text = "N/A";
        	// 
        	// NextNosActionlbl
        	// 
        	this.NextNosActionlbl.AutoSize = true;
        	this.NextNosActionlbl.Location = new System.Drawing.Point(314, 126);
        	this.NextNosActionlbl.Name = "NextNosActionlbl";
        	this.NextNosActionlbl.Size = new System.Drawing.Size(78, 13);
        	this.NextNosActionlbl.TabIndex = 262;
        	this.NextNosActionlbl.Text = "NextNosAction";
        	// 
        	// NextWebActionData
        	// 
        	this.NextWebActionData.AutoSize = true;
        	this.NextWebActionData.Location = new System.Drawing.Point(493, 113);
        	this.NextWebActionData.Name = "NextWebActionData";
        	this.NextWebActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextWebActionData.TabIndex = 261;
        	this.NextWebActionData.Text = "N/A";
        	// 
        	// NextWebActionlbl
        	// 
        	this.NextWebActionlbl.AutoSize = true;
        	this.NextWebActionlbl.Location = new System.Drawing.Point(314, 113);
        	this.NextWebActionlbl.Name = "NextWebActionlbl";
        	this.NextWebActionlbl.Size = new System.Drawing.Size(82, 13);
        	this.NextWebActionlbl.TabIndex = 260;
        	this.NextWebActionlbl.Text = "NextWebAction";
        	// 
        	// NextWeaponActionData
        	// 
        	this.NextWeaponActionData.AutoSize = true;
        	this.NextWeaponActionData.Location = new System.Drawing.Point(493, 100);
        	this.NextWeaponActionData.Name = "NextWeaponActionData";
        	this.NextWeaponActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextWeaponActionData.TabIndex = 259;
        	this.NextWeaponActionData.Text = "N/A";
        	// 
        	// NextWeaponActionlbl
        	// 
        	this.NextWeaponActionlbl.AutoSize = true;
        	this.NextWeaponActionlbl.Location = new System.Drawing.Point(314, 100);
        	this.NextWeaponActionlbl.Name = "NextWeaponActionlbl";
        	this.NextWeaponActionlbl.Size = new System.Drawing.Size(100, 13);
        	this.NextWeaponActionlbl.TabIndex = 258;
        	this.NextWeaponActionlbl.Text = "NextWeaponAction";
        	// 
        	// NextReloadData
        	// 
        	this.NextReloadData.AutoSize = true;
        	this.NextReloadData.Location = new System.Drawing.Point(493, 86);
        	this.NextReloadData.Name = "NextReloadData";
        	this.NextReloadData.Size = new System.Drawing.Size(27, 13);
        	this.NextReloadData.TabIndex = 257;
        	this.NextReloadData.Text = "N/A";
        	// 
        	// NextReloadlbl
        	// 
        	this.NextReloadlbl.AutoSize = true;
        	this.NextReloadlbl.Location = new System.Drawing.Point(314, 86);
        	this.NextReloadlbl.Name = "NextReloadlbl";
        	this.NextReloadlbl.Size = new System.Drawing.Size(63, 13);
        	this.NextReloadlbl.TabIndex = 256;
        	this.NextReloadlbl.Text = "NextReload";
        	// 
        	// NextTargetActionData
        	// 
        	this.NextTargetActionData.AutoSize = true;
        	this.NextTargetActionData.Location = new System.Drawing.Point(493, 73);
        	this.NextTargetActionData.Name = "NextTargetActionData";
        	this.NextTargetActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextTargetActionData.TabIndex = 255;
        	this.NextTargetActionData.Text = "N/A";
        	// 
        	// NextTargetActionlbl
        	// 
        	this.NextTargetActionlbl.AutoSize = true;
        	this.NextTargetActionlbl.Location = new System.Drawing.Point(314, 73);
        	this.NextTargetActionlbl.Name = "NextTargetActionlbl";
        	this.NextTargetActionlbl.Size = new System.Drawing.Size(90, 13);
        	this.NextTargetActionlbl.TabIndex = 254;
        	this.NextTargetActionlbl.Text = "NextTargetAction";
        	// 
        	// NextTravelerActionData
        	// 
        	this.NextTravelerActionData.AutoSize = true;
        	this.NextTravelerActionData.Location = new System.Drawing.Point(493, 60);
        	this.NextTravelerActionData.Name = "NextTravelerActionData";
        	this.NextTravelerActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextTravelerActionData.TabIndex = 253;
        	this.NextTravelerActionData.Text = "N/A";
        	// 
        	// NextTravelerActionlbl
        	// 
        	this.NextTravelerActionlbl.AutoSize = true;
        	this.NextTravelerActionlbl.Location = new System.Drawing.Point(314, 60);
        	this.NextTravelerActionlbl.Name = "NextTravelerActionlbl";
        	this.NextTravelerActionlbl.Size = new System.Drawing.Size(98, 13);
        	this.NextTravelerActionlbl.TabIndex = 252;
        	this.NextTravelerActionlbl.Text = "NextTravelerAction";
        	// 
        	// NextWarpToData
        	// 
        	this.NextWarpToData.AutoSize = true;
        	this.NextWarpToData.Location = new System.Drawing.Point(493, 47);
        	this.NextWarpToData.Name = "NextWarpToData";
        	this.NextWarpToData.Size = new System.Drawing.Size(27, 13);
        	this.NextWarpToData.TabIndex = 251;
        	this.NextWarpToData.Text = "N/A";
        	// 
        	// NextWarpTolbl
        	// 
        	this.NextWarpTolbl.AutoSize = true;
        	this.NextWarpTolbl.Location = new System.Drawing.Point(314, 47);
        	this.NextWarpTolbl.Name = "NextWarpTolbl";
        	this.NextWarpTolbl.Size = new System.Drawing.Size(68, 13);
        	this.NextWarpTolbl.TabIndex = 250;
        	this.NextWarpTolbl.Text = "NextWarpTo";
        	// 
        	// NextOrbitData
        	// 
        	this.NextOrbitData.AutoSize = true;
        	this.NextOrbitData.Location = new System.Drawing.Point(493, 34);
        	this.NextOrbitData.Name = "NextOrbitData";
        	this.NextOrbitData.Size = new System.Drawing.Size(27, 13);
        	this.NextOrbitData.TabIndex = 249;
        	this.NextOrbitData.Text = "N/A";
        	// 
        	// NextOrbitlbl
        	// 
        	this.NextOrbitlbl.AutoSize = true;
        	this.NextOrbitlbl.Location = new System.Drawing.Point(314, 34);
        	this.NextOrbitlbl.Name = "NextOrbitlbl";
        	this.NextOrbitlbl.Size = new System.Drawing.Size(51, 13);
        	this.NextOrbitlbl.TabIndex = 248;
        	this.NextOrbitlbl.Text = "NextOrbit";
        	// 
        	// NextApproachActionData
        	// 
        	this.NextApproachActionData.AutoSize = true;
        	this.NextApproachActionData.Location = new System.Drawing.Point(214, 232);
        	this.NextApproachActionData.Name = "NextApproachActionData";
        	this.NextApproachActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextApproachActionData.TabIndex = 247;
        	this.NextApproachActionData.Text = "N/A";
        	// 
        	// NextApproachActionlbl
        	// 
        	this.NextApproachActionlbl.AutoSize = true;
        	this.NextApproachActionlbl.Location = new System.Drawing.Point(35, 232);
        	this.NextApproachActionlbl.Name = "NextApproachActionlbl";
        	this.NextApproachActionlbl.Size = new System.Drawing.Size(105, 13);
        	this.NextApproachActionlbl.TabIndex = 246;
        	this.NextApproachActionlbl.Text = "NextApproachAction";
        	// 
        	// NextActivateSupportModulesData
        	// 
        	this.NextActivateSupportModulesData.AutoSize = true;
        	this.NextActivateSupportModulesData.Location = new System.Drawing.Point(214, 219);
        	this.NextActivateSupportModulesData.Name = "NextActivateSupportModulesData";
        	this.NextActivateSupportModulesData.Size = new System.Drawing.Size(27, 13);
        	this.NextActivateSupportModulesData.TabIndex = 245;
        	this.NextActivateSupportModulesData.Text = "N/A";
        	// 
        	// NextActivateSupportModuleslbl
        	// 
        	this.NextActivateSupportModuleslbl.AutoSize = true;
        	this.NextActivateSupportModuleslbl.Location = new System.Drawing.Point(35, 219);
        	this.NextActivateSupportModuleslbl.Name = "NextActivateSupportModuleslbl";
        	this.NextActivateSupportModuleslbl.Size = new System.Drawing.Size(145, 13);
        	this.NextActivateSupportModuleslbl.TabIndex = 244;
        	this.NextActivateSupportModuleslbl.Text = "NextActivateSupportModules";
        	// 
        	// NextRepModuleActionData
        	// 
        	this.NextRepModuleActionData.AutoSize = true;
        	this.NextRepModuleActionData.Location = new System.Drawing.Point(214, 206);
        	this.NextRepModuleActionData.Name = "NextRepModuleActionData";
        	this.NextRepModuleActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextRepModuleActionData.TabIndex = 243;
        	this.NextRepModuleActionData.Text = "N/A";
        	// 
        	// NextRepModuleActionlbl
        	// 
        	this.NextRepModuleActionlbl.AutoSize = true;
        	this.NextRepModuleActionlbl.Location = new System.Drawing.Point(35, 206);
        	this.NextRepModuleActionlbl.Name = "NextRepModuleActionlbl";
        	this.NextRepModuleActionlbl.Size = new System.Drawing.Size(114, 13);
        	this.NextRepModuleActionlbl.TabIndex = 242;
        	this.NextRepModuleActionlbl.Text = "NextRepModuleAction";
        	// 
        	// NextAfterburnerActionData
        	// 
        	this.NextAfterburnerActionData.AutoSize = true;
        	this.NextAfterburnerActionData.Location = new System.Drawing.Point(214, 193);
        	this.NextAfterburnerActionData.Name = "NextAfterburnerActionData";
        	this.NextAfterburnerActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextAfterburnerActionData.TabIndex = 241;
        	this.NextAfterburnerActionData.Text = "N/A";
        	// 
        	// NextAfterburnerActionlbl
        	// 
        	this.NextAfterburnerActionlbl.AutoSize = true;
        	this.NextAfterburnerActionlbl.Location = new System.Drawing.Point(35, 193);
        	this.NextAfterburnerActionlbl.Name = "NextAfterburnerActionlbl";
        	this.NextAfterburnerActionlbl.Size = new System.Drawing.Size(111, 13);
        	this.NextAfterburnerActionlbl.TabIndex = 240;
        	this.NextAfterburnerActionlbl.Text = "NextAfterburnerAction";
        	// 
        	// NextDefenceModuleActionData
        	// 
        	this.NextDefenceModuleActionData.AutoSize = true;
        	this.NextDefenceModuleActionData.Location = new System.Drawing.Point(214, 180);
        	this.NextDefenceModuleActionData.Name = "NextDefenceModuleActionData";
        	this.NextDefenceModuleActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextDefenceModuleActionData.TabIndex = 239;
        	this.NextDefenceModuleActionData.Text = "N/A";
        	// 
        	// NextDefenceModuleActionlbl
        	// 
        	this.NextDefenceModuleActionlbl.AutoSize = true;
        	this.NextDefenceModuleActionlbl.Location = new System.Drawing.Point(35, 180);
        	this.NextDefenceModuleActionlbl.Name = "NextDefenceModuleActionlbl";
        	this.NextDefenceModuleActionlbl.Size = new System.Drawing.Size(135, 13);
        	this.NextDefenceModuleActionlbl.TabIndex = 238;
        	this.NextDefenceModuleActionlbl.Text = "NextDefenceModuleAction";
        	// 
        	// LastJettisonData
        	// 
        	this.LastJettisonData.AutoSize = true;
        	this.LastJettisonData.Location = new System.Drawing.Point(214, 167);
        	this.LastJettisonData.Name = "LastJettisonData";
        	this.LastJettisonData.Size = new System.Drawing.Size(27, 13);
        	this.LastJettisonData.TabIndex = 237;
        	this.LastJettisonData.Text = "N/A";
        	// 
        	// LastJettisonlbl
        	// 
        	this.LastJettisonlbl.AutoSize = true;
        	this.LastJettisonlbl.Location = new System.Drawing.Point(35, 167);
        	this.LastJettisonlbl.Name = "LastJettisonlbl";
        	this.LastJettisonlbl.Size = new System.Drawing.Size(63, 13);
        	this.LastJettisonlbl.TabIndex = 236;
        	this.LastJettisonlbl.Text = "LastJettison";
        	// 
        	// NextLootActionData
        	// 
        	this.NextLootActionData.AutoSize = true;
        	this.NextLootActionData.Location = new System.Drawing.Point(214, 154);
        	this.NextLootActionData.Name = "NextLootActionData";
        	this.NextLootActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextLootActionData.TabIndex = 235;
        	this.NextLootActionData.Text = "N/A";
        	// 
        	// NextLootActionlbl
        	// 
        	this.NextLootActionlbl.AutoSize = true;
        	this.NextLootActionlbl.Location = new System.Drawing.Point(35, 152);
        	this.NextLootActionlbl.Name = "NextLootActionlbl";
        	this.NextLootActionlbl.Size = new System.Drawing.Size(80, 13);
        	this.NextLootActionlbl.TabIndex = 234;
        	this.NextLootActionlbl.Text = "NextLootAction";
        	// 
        	// NextSalvageActionData
        	// 
        	this.NextSalvageActionData.AutoSize = true;
        	this.NextSalvageActionData.Location = new System.Drawing.Point(214, 139);
        	this.NextSalvageActionData.Name = "NextSalvageActionData";
        	this.NextSalvageActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextSalvageActionData.TabIndex = 233;
        	this.NextSalvageActionData.Text = "N/A";
        	// 
        	// NextSalvageActionlbl
        	// 
        	this.NextSalvageActionlbl.AutoSize = true;
        	this.NextSalvageActionlbl.Location = new System.Drawing.Point(35, 139);
        	this.NextSalvageActionlbl.Name = "NextSalvageActionlbl";
        	this.NextSalvageActionlbl.Size = new System.Drawing.Size(98, 13);
        	this.NextSalvageActionlbl.TabIndex = 232;
        	this.NextSalvageActionlbl.Text = "NextSalvageAction";
        	// 
        	// NextArmActionData
        	// 
        	this.NextArmActionData.AutoSize = true;
        	this.NextArmActionData.Location = new System.Drawing.Point(214, 126);
        	this.NextArmActionData.Name = "NextArmActionData";
        	this.NextArmActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextArmActionData.TabIndex = 231;
        	this.NextArmActionData.Text = "N/A";
        	// 
        	// NextArmActionlbl
        	// 
        	this.NextArmActionlbl.AutoSize = true;
        	this.NextArmActionlbl.Location = new System.Drawing.Point(35, 126);
        	this.NextArmActionlbl.Name = "NextArmActionlbl";
        	this.NextArmActionlbl.Size = new System.Drawing.Size(77, 13);
        	this.NextArmActionlbl.TabIndex = 230;
        	this.NextArmActionlbl.Text = "NextArmAction";
        	// 
        	// LastActionData
        	// 
        	this.LastActionData.AutoSize = true;
        	this.LastActionData.Location = new System.Drawing.Point(214, 113);
        	this.LastActionData.Name = "LastActionData";
        	this.LastActionData.Size = new System.Drawing.Size(27, 13);
        	this.LastActionData.TabIndex = 229;
        	this.LastActionData.Text = "N/A";
        	// 
        	// LastActionlbl
        	// 
        	this.LastActionlbl.AutoSize = true;
        	this.LastActionlbl.Location = new System.Drawing.Point(35, 113);
        	this.LastActionlbl.Name = "LastActionlbl";
        	this.LastActionlbl.Size = new System.Drawing.Size(57, 13);
        	this.LastActionlbl.TabIndex = 228;
        	this.LastActionlbl.Text = "LastAction";
        	// 
        	// NextOpenCargoActionData
        	// 
        	this.NextOpenCargoActionData.AutoSize = true;
        	this.NextOpenCargoActionData.Location = new System.Drawing.Point(214, 100);
        	this.NextOpenCargoActionData.Name = "NextOpenCargoActionData";
        	this.NextOpenCargoActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextOpenCargoActionData.TabIndex = 227;
        	this.NextOpenCargoActionData.Text = "N/A";
        	// 
        	// NextOpenCargoActionlbl
        	// 
        	this.NextOpenCargoActionlbl.AutoSize = true;
        	this.NextOpenCargoActionlbl.Location = new System.Drawing.Point(35, 100);
        	this.NextOpenCargoActionlbl.Name = "NextOpenCargoActionlbl";
        	this.NextOpenCargoActionlbl.Size = new System.Drawing.Size(113, 13);
        	this.NextOpenCargoActionlbl.TabIndex = 226;
        	this.NextOpenCargoActionlbl.Text = "NextOpenCargoAction";
        	// 
        	// NextOpenHangarActionData
        	// 
        	this.NextOpenHangarActionData.AutoSize = true;
        	this.NextOpenHangarActionData.Location = new System.Drawing.Point(214, 86);
        	this.NextOpenHangarActionData.Name = "NextOpenHangarActionData";
        	this.NextOpenHangarActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextOpenHangarActionData.TabIndex = 225;
        	this.NextOpenHangarActionData.Text = "N/A";
        	// 
        	// NextOpenHangarActionlbl
        	// 
        	this.NextOpenHangarActionlbl.AutoSize = true;
        	this.NextOpenHangarActionlbl.Location = new System.Drawing.Point(35, 86);
        	this.NextOpenHangarActionlbl.Name = "NextOpenHangarActionlbl";
        	this.NextOpenHangarActionlbl.Size = new System.Drawing.Size(120, 13);
        	this.NextOpenHangarActionlbl.TabIndex = 224;
        	this.NextOpenHangarActionlbl.Text = "NextOpenHangarAction";
        	// 
        	// NextDroneBayActionData
        	// 
        	this.NextDroneBayActionData.AutoSize = true;
        	this.NextDroneBayActionData.Location = new System.Drawing.Point(214, 73);
        	this.NextDroneBayActionData.Name = "NextDroneBayActionData";
        	this.NextDroneBayActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextDroneBayActionData.TabIndex = 223;
        	this.NextDroneBayActionData.Text = "N/A";
        	// 
        	// NextDroneBayActionlbl
        	// 
        	this.NextDroneBayActionlbl.AutoSize = true;
        	this.NextDroneBayActionlbl.Location = new System.Drawing.Point(35, 73);
        	this.NextDroneBayActionlbl.Name = "NextDroneBayActionlbl";
        	this.NextDroneBayActionlbl.Size = new System.Drawing.Size(106, 13);
        	this.NextDroneBayActionlbl.TabIndex = 222;
        	this.NextDroneBayActionlbl.Text = "NextDroneBayAction";
        	// 
        	// NextOpenLootContainerActionData
        	// 
        	this.NextOpenLootContainerActionData.AutoSize = true;
        	this.NextOpenLootContainerActionData.Location = new System.Drawing.Point(214, 60);
        	this.NextOpenLootContainerActionData.Name = "NextOpenLootContainerActionData";
        	this.NextOpenLootContainerActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextOpenLootContainerActionData.TabIndex = 221;
        	this.NextOpenLootContainerActionData.Text = "N/A";
        	// 
        	// NextOpenLootContainerActionlbl
        	// 
        	this.NextOpenLootContainerActionlbl.AutoSize = true;
        	this.NextOpenLootContainerActionlbl.Location = new System.Drawing.Point(35, 60);
        	this.NextOpenLootContainerActionlbl.Name = "NextOpenLootContainerActionlbl";
        	this.NextOpenLootContainerActionlbl.Size = new System.Drawing.Size(151, 13);
        	this.NextOpenLootContainerActionlbl.TabIndex = 220;
        	this.NextOpenLootContainerActionlbl.Text = "NextOpenLootContainerAction";
        	// 
        	// NextOpenJournalWindowActionData
        	// 
        	this.NextOpenJournalWindowActionData.AutoSize = true;
        	this.NextOpenJournalWindowActionData.Location = new System.Drawing.Point(214, 47);
        	this.NextOpenJournalWindowActionData.Name = "NextOpenJournalWindowActionData";
        	this.NextOpenJournalWindowActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextOpenJournalWindowActionData.TabIndex = 219;
        	this.NextOpenJournalWindowActionData.Text = "N/A";
        	// 
        	// NextOpenJournalWindowActionlbl
        	// 
        	this.NextOpenJournalWindowActionlbl.AutoSize = true;
        	this.NextOpenJournalWindowActionlbl.Location = new System.Drawing.Point(35, 47);
        	this.NextOpenJournalWindowActionlbl.Name = "NextOpenJournalWindowActionlbl";
        	this.NextOpenJournalWindowActionlbl.Size = new System.Drawing.Size(158, 13);
        	this.NextOpenJournalWindowActionlbl.TabIndex = 218;
        	this.NextOpenJournalWindowActionlbl.Text = "NextOpenJournalWindowAction";
        	// 
        	// NextOpenContainerInSpaceActionData
        	// 
        	this.NextOpenContainerInSpaceActionData.AutoSize = true;
        	this.NextOpenContainerInSpaceActionData.Location = new System.Drawing.Point(214, 34);
        	this.NextOpenContainerInSpaceActionData.Name = "NextOpenContainerInSpaceActionData";
        	this.NextOpenContainerInSpaceActionData.Size = new System.Drawing.Size(27, 13);
        	this.NextOpenContainerInSpaceActionData.TabIndex = 217;
        	this.NextOpenContainerInSpaceActionData.Text = "N/A";
        	// 
        	// NextOpenContainerInSpaceActionlbl
        	// 
        	this.NextOpenContainerInSpaceActionlbl.AutoSize = true;
        	this.NextOpenContainerInSpaceActionlbl.Location = new System.Drawing.Point(35, 34);
        	this.NextOpenContainerInSpaceActionlbl.Name = "NextOpenContainerInSpaceActionlbl";
        	this.NextOpenContainerInSpaceActionlbl.Size = new System.Drawing.Size(173, 13);
        	this.NextOpenContainerInSpaceActionlbl.TabIndex = 216;
        	this.NextOpenContainerInSpaceActionlbl.Text = "NextOpenContainerInSpaceAction:";
        	// 
        	// tabPage1
        	// 
        	this.tabPage1.Controls.Add(this.button1);
        	this.tabPage1.Location = new System.Drawing.Point(4, 22);
        	this.tabPage1.Name = "tabPage1";
        	this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
        	this.tabPage1.Size = new System.Drawing.Size(774, 278);
        	this.tabPage1.TabIndex = 7;
        	this.tabPage1.Text = "Misc";
        	this.tabPage1.UseVisualStyleBackColor = true;
        	// 
        	// button1
        	// 
        	this.button1.Location = new System.Drawing.Point(230, 109);
        	this.button1.Name = "button1";
        	this.button1.Size = new System.Drawing.Size(270, 23);
        	this.button1.TabIndex = 127;
        	this.button1.Text = "Open PyBrowser [TRIAL ONLY]";
        	this.button1.UseVisualStyleBackColor = true;
        	this.button1.Click += new System.EventHandler(this.Button1Click);
        	// 
        	// tabPage2
        	// 
        	this.tabPage2.Location = new System.Drawing.Point(4, 22);
        	this.tabPage2.Name = "tabPage2";
        	this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
        	this.tabPage2.Size = new System.Drawing.Size(791, 399);
        	this.tabPage2.TabIndex = 2;
        	this.tabPage2.Text = "QuestorManager";
        	this.tabPage2.UseVisualStyleBackColor = true;
        	// 
        	// QuestorUI
        	// 
        	this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        	this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        	this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
        	this.ClientSize = new System.Drawing.Size(801, 431);
        	this.Controls.Add(this.tabControlMain);
        	this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        	this.MaximizeBox = false;
        	this.Name = "QuestorUI";
        	this.Text = "Questor";
        	this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.QuestorUIFormClosing);
        	this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.QuestorfrmMainFormClosed);
        	this.Load += new System.EventHandler(this.QuestorUILoad);
        	this.Shown += new System.EventHandler(this.QuestorUIShown);
        	this.tabControlMain.ResumeLayout(false);
        	this.tabPage3.ResumeLayout(false);
        	this.tabPage3.PerformLayout();
        	this.Tabs.ResumeLayout(false);
        	this.tabConsole.ResumeLayout(false);
        	this.tabStates.ResumeLayout(false);
        	this.tabStates.PerformLayout();
        	this.panel2.ResumeLayout(false);
        	this.panel2.PerformLayout();
        	this.panel1.ResumeLayout(false);
        	this.panel1.PerformLayout();
        	this.tabMissions.ResumeLayout(false);
        	this.tabMissions.PerformLayout();
        	this.panel3.ResumeLayout(false);
        	this.panel3.PerformLayout();
        	this.tabTimeStamps.ResumeLayout(false);
        	this.tabTimeStamps.PerformLayout();
        	this.tabPage1.ResumeLayout(false);
        	this.ResumeLayout(false);

        }

        private System.Windows.Forms.CheckBox AutoStartCheckBox;
        private System.Windows.Forms.Timer tUpdateUI;
        private System.Windows.Forms.ComboBox DamageTypeComboBox;
        private System.Windows.Forms.Label lblDamageType;
        private System.Windows.Forms.CheckBox PauseCheckBox;
        private System.Windows.Forms.CheckBox Disable3DCheckBox;
        private System.Windows.Forms.Label lblMissionName;
        private System.Windows.Forms.Label lblPocketAction;
        private System.Windows.Forms.Label lblCurrentPocketAction;
        private System.Windows.Forms.Button buttonOpenLogDirectory;
        private System.Windows.Forms.ComboBox BehaviorComboBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox QuestorStateComboBox;
        private System.Windows.Forms.Label QuestorStatelbl;
        private System.Windows.Forms.Label label25;
        private System.Windows.Forms.Label label26;
        public System.Windows.Forms.Label lblCurrentMissionInfo;
        public System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.TabControl Tabs;
        private System.Windows.Forms.TabPage tabConsole;
        private System.Windows.Forms.TabPage tabStates;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.ComboBox SalvageStateComboBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox LocalWatchStateComboBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox CleanupStateComboBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox DronesStateComboBox;
        private System.Windows.Forms.ComboBox CombatStateComboBox;
        private System.Windows.Forms.ComboBox PanicStateComboBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox AgentInteractionStateComboBox;
        private System.Windows.Forms.ComboBox TravelerStateComboBox;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.ComboBox UnloadStateComboBox;
        private System.Windows.Forms.ComboBox ArmStateComboBox;
        private System.Windows.Forms.ComboBox StorylineStateComboBox;
        private System.Windows.Forms.ComboBox CombatMissionCtrlStateComboBox;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label dataStopTimeSpecified;
        private System.Windows.Forms.Label lblStopTimeSpecified;
        private System.Windows.Forms.Label MissionsThisSessionData;
        private System.Windows.Forms.Label MissionsThisSessionlbl;
        private System.Windows.Forms.Label lastKnownGoodConnectedTimeData;
        private System.Windows.Forms.Label lastKnownGoodConnectedTimeLabel;
        private System.Windows.Forms.Label lastInStationData;
        private System.Windows.Forms.Label lastInSpaceData;
        private System.Windows.Forms.Label lastInStationLabel;
        private System.Windows.Forms.Label lastInSpaceLabel;
        private System.Windows.Forms.Label LastFrameData;
        private System.Windows.Forms.Label lastSessionisreadyData;
        private System.Windows.Forms.Label lastSessionIsreadylabel;
        private System.Windows.Forms.Label lastFrameLabel;
        private System.Windows.Forms.TabPage tabMissions;
        private System.Windows.Forms.Label AgentInteractionPurposeData;
        private System.Windows.Forms.Label AgentInteractionPurposelbl;
        private System.Windows.Forms.Label blacklistedmissionsdeclineddata;
        private System.Windows.Forms.Label blacklistedmissionsdeclinedlbl;
        private System.Windows.Forms.Label greylistedmissionsdeclineddata;
        private System.Windows.Forms.Label greylistedmissionsdeclinedlbl;
        private System.Windows.Forms.Label LastBlacklistedMissionDeclinedData;
        private System.Windows.Forms.Label LastGreylistedMissionDeclinedData;
        private System.Windows.Forms.Label LastBlacklistedMissionDeclinedlbl;
        private System.Windows.Forms.Label LastGreylistedMissionDeclinedlbl;
        private System.Windows.Forms.Label MinAgentGreyListStandingsData;
        private System.Windows.Forms.Label MinAgentBlackListStandingsData;
        private System.Windows.Forms.Label AgentEffectiveStandingsData;
        private System.Windows.Forms.Label AgentNameData;
        private System.Windows.Forms.Label AgentInfolbl;
        private System.Windows.Forms.Label BlacklistStandingslbl;
        private System.Windows.Forms.Label GreyListStandingslbl;
        private System.Windows.Forms.Label CurrentEffectiveStandingslbl;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label BlackListedMissionslbl;
        private System.Windows.Forms.Label GreyListlbl;
        private System.Windows.Forms.TextBox BlacklistedMissionstextbox;
        private System.Windows.Forms.TextBox GreyListedMissionsTextBox;
        private System.Windows.Forms.TabPage tabTimeStamps;
        private System.Windows.Forms.Label CurrentTimelbl2;
        private System.Windows.Forms.Label CurrentTimeData2;
        private System.Windows.Forms.Label CurrentTimeData1;
        private System.Windows.Forms.Label CurrentTimelbl;
        private System.Windows.Forms.Label LastSessionChangeData;
        private System.Windows.Forms.Label LastSessionChangelbl;
        private System.Windows.Forms.Label NextStartupActionData;
        private System.Windows.Forms.Label NextStartupActionlbl;
        private System.Windows.Forms.Label NextDroneRecallData;
        private System.Windows.Forms.Label NextDroneRecalllbl;
        private System.Windows.Forms.Label NextDockActionData;
        private System.Windows.Forms.Label NextDockActionlbl;
        private System.Windows.Forms.Label NextUndockActionData;
        private System.Windows.Forms.Label NextUndockActionlbl;
        private System.Windows.Forms.Label NextAlignData;
        private System.Windows.Forms.Label NextAlignlbl;
        private System.Windows.Forms.Label NextActivateActionData;
        private System.Windows.Forms.Label NextActivateActionlbl;
        private System.Windows.Forms.Label NextPainterActionData;
        private System.Windows.Forms.Label NextPainterActionlbl;
        private System.Windows.Forms.Label NextNosActionData;
        private System.Windows.Forms.Label NextNosActionlbl;
        private System.Windows.Forms.Label NextWebActionData;
        private System.Windows.Forms.Label NextWebActionlbl;
        private System.Windows.Forms.Label NextWeaponActionData;
        private System.Windows.Forms.Label NextWeaponActionlbl;
        private System.Windows.Forms.Label NextReloadData;
        private System.Windows.Forms.Label NextReloadlbl;
        private System.Windows.Forms.Label NextTargetActionData;
        private System.Windows.Forms.Label NextTargetActionlbl;
        private System.Windows.Forms.Label NextTravelerActionData;
        private System.Windows.Forms.Label NextTravelerActionlbl;
        private System.Windows.Forms.Label NextWarpToData;
        private System.Windows.Forms.Label NextWarpTolbl;
        private System.Windows.Forms.Label NextOrbitData;
        private System.Windows.Forms.Label NextOrbitlbl;
        private System.Windows.Forms.Label NextApproachActionData;
        private System.Windows.Forms.Label NextApproachActionlbl;
        private System.Windows.Forms.Label NextActivateSupportModulesData;
        private System.Windows.Forms.Label NextActivateSupportModuleslbl;
        private System.Windows.Forms.Label NextRepModuleActionData;
        private System.Windows.Forms.Label NextRepModuleActionlbl;
        private System.Windows.Forms.Label NextAfterburnerActionData;
        private System.Windows.Forms.Label NextAfterburnerActionlbl;
        private System.Windows.Forms.Label NextDefenceModuleActionData;
        private System.Windows.Forms.Label NextDefenceModuleActionlbl;
        private System.Windows.Forms.Label LastJettisonData;
        private System.Windows.Forms.Label LastJettisonlbl;
        private System.Windows.Forms.Label NextLootActionData;
        private System.Windows.Forms.Label NextLootActionlbl;
        private System.Windows.Forms.Label NextSalvageActionData;
        private System.Windows.Forms.Label NextSalvageActionlbl;
        private System.Windows.Forms.Label NextArmActionData;
        private System.Windows.Forms.Label NextArmActionlbl;
        private System.Windows.Forms.Label LastActionData;
        private System.Windows.Forms.Label LastActionlbl;
        private System.Windows.Forms.Label NextOpenCargoActionData;
        private System.Windows.Forms.Label NextOpenCargoActionlbl;
        private System.Windows.Forms.Label NextOpenHangarActionData;
        private System.Windows.Forms.Label NextOpenHangarActionlbl;
        private System.Windows.Forms.Label NextDroneBayActionData;
        private System.Windows.Forms.Label NextDroneBayActionlbl;
        private System.Windows.Forms.Label NextOpenLootContainerActionData;
        private System.Windows.Forms.Label NextOpenLootContainerActionlbl;
        private System.Windows.Forms.Label NextOpenJournalWindowActionData;
        private System.Windows.Forms.Label NextOpenJournalWindowActionlbl;
        private System.Windows.Forms.Label NextOpenContainerInSpaceActionData;
        private System.Windows.Forms.Label NextOpenContainerInSpaceActionlbl;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ListBox logListbox;
    }
}