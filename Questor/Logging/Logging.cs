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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Questor.Modules.Lookup;
using System.Runtime.CompilerServices;

namespace Questor.Modules.Logging
{
    public static class Logging
    {
        public delegate void Message(string msg);

        //list of colors
        public const string Green = ""; //traveler mission control
        public const string Yellow = "";
        public const string Blue = ""; //DO NOT USE - blends into default lavish GUIs background.
        public const string Red = ""; //error panic
        public const string Orange = ""; //error can fix
        public const string Purple = ""; //combat
        public const string Magenta = ""; //drones
        public const string Teal = ""; //log debug
        public const string White = ""; //questor
        public const string Debug = Teal; //log debug

        public static string PathToCurrentDirectory;

        public static DateTime DateTimeForLogs;

        public static string EVELoginUserName;
        public static string EVELoginPassword;
        public static string MyCharacterName;

        public static string CharacterSettingsPath;


        private static string colorLogLine;
        private static string plainLogLine;
        public static bool ConsoleLogOpened = false;

        private static string _characterNameForLogs;

        static Logging()
        {
            PathToCurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }


        //
        // Debug Variables
        //
        public static bool DebugActivateGate { get; set; }
        public static bool DebugActivateWeapons { get; set; }
        public static bool DebugActivateBastion { get; set; }
        public static bool DebugAdaptEVE { get; set; }
        public static bool DebugAdaptEVEDLL { get; set; }
        public static bool DebugAddDronePriorityTarget { get; set; }
        public static bool DebugAddPrimaryWeaponPriorityTarget { get; set; }
        public static bool DebugAgentInteractionReplyToAgent { get; set; }
        public static bool DebugAllMissionsOnBlackList { get; set; }
        public static bool DebugAllMissionsOnGreyList { get; set; }
        public static bool DebugAmmo { get; set; }
        public static bool DebugAppDomains { get; set; }
        public static bool DebugArm { get; set; }
        public static bool DebugAttachVSDebugger { get; set; }
        public static bool DebugAutoStart { get; set; }
        public static bool DebugBeforeLogin { get; set; }
        public static bool DebugBlackList { get; set; }
        public static bool DebugCargoHold { get; set; }
        public static bool DebugChat { get; set; }
        public static bool DebugCleanup { get; set; }
        public static bool DebugClearPocket { get; set; }
        public static bool DebugCombat { get; set; }
        public static bool DebugCombatMissionBehavior { get; set; }
        public static bool DebugCourierMissions { get; set; }
        public static bool DebugDecline { get; set; }
        public static bool DebugDefense { get; set; }
        public static bool DebugDisableCleanup { get; set; }
        public static bool DebugDisableCombatMissionsBehavior { get; set; }
        public static bool DebugDisableCombatMissionCtrl { get; set; }
        public static bool DebugDisableCombat { get; set; }
        public static bool DebugDisableDrones { get; set; }
        public static bool DebugDisablePanic { get; set; }
        public static bool DebugDisableSalvage { get; set; }
        public static bool DebugDisableTargetCombatants { get; set; }
        public static bool DebugDisableGetBestTarget { get; set; }
        public static bool DebugDisableGetBestDroneTarget { get; set; }
        public static bool DebugDisableNavigateIntoRange { get; set; }
        public static bool DebugDoneAction { get; set; }
        public static bool DebugDoNotCloseTelcomWindows { get; set; }
        public static bool DebugDrones { get; set; }
        public static bool DebugDroneHealth { get; set; }
        public static bool DebugEachWeaponsVolleyCache { get; set; }
        public static bool DebugEntityCache { get; set; }
        public static bool DebugExecuteMission { get; set; }
        public static bool DebugExceptions { get; set; }
        public static bool DebugFittingMgr { get; set; }
        public static bool DebugFleetSupportSlave { get; set; }
        public static bool DebugFleetSupportMaster { get; set; }
        public static bool DebugGetBestTarget { get; set; }
        public static bool DebugGetBestDroneTarget { get; set; }
        public static bool DebugGotobase { get; set; }
        public static bool DebugGreyList { get; set; }
        public static bool DebugHangars { get; set; }
        public static bool DebugIdle { get; set; }
        public static bool DebugInSpace { get; set; }
        public static bool DebugInStation { get; set; }
        public static bool DebugInWarp { get; set; }
        public static bool DebugIsReadyToShoot { get; set; }
        public static bool DebugItemHangar { get; set; }
        public static bool DebugKillTargets { get; set; }
        public static bool DebugKillAction { get; set; }
        public static bool DebugLoadScripts { get; set; }
        public static bool DebugLogging { get; set; }
        public static bool DebugLootWrecks { get; set; }
        public static bool DebugLootValue { get; set; }
        public static bool DebugNavigateOnGrid { get; set; }
        public static bool DebugMiningBehavior { get; set; }
        public static bool DebugMissionFittings { get; set; }
        public static bool DebugMoveTo { get; set; }
        public static bool DebugOnframe { get; set; }
        public static bool DebugOverLoadWeapons { get; set; }
        public static bool DebugPanic { get; set; }
        public static bool DebugPerformance { get; set; }
        public static bool DebugPotentialCombatTargets { get; set; }
        public static bool DebugPreferredPrimaryWeaponTarget { get; set; }
        public static bool DebugPreLogin { get; set; }
        public static bool DebugQuestorLoader { get; set; }
        public static bool DebugQuestorManager { get; set; }
        public static bool DebugQuestorEVEOnFrame { get; set; }
        public static bool DebugReloadAll { get; set; }
        public static bool DebugReloadorChangeAmmo { get; set; }
        public static bool DebugRemoteRepair { get; set; }
        public static bool DebugSalvage { get; set; }
        public static bool DebugScheduler { get; set; }
        public static bool DebugSettings { get; set; }
        public static bool DebugShipTargetValues { get; set; }
        public static bool DebugSkillTraining { get; set; }
        public static bool DebugSpeedMod { get; set; }
        public static bool DebugStatistics { get; set; }
        public static bool DebugStorylineMissions { get; set; }
        public static bool DebugTargetCombatants { get; set; }
        public static bool DebugTargetWrecks { get; set; }
        public static bool DebugTractorBeams { get; set; }
        public static bool DebugTraveler { get; set; }
        public static bool DebugUI { get; set; }
        public static bool DebugUndockBookmarks { get; set; }
        public static bool DebugUnloadLoot { get; set; }
        public static bool DebugValuedump { get; set; }
        public static bool DebugWalletBalance { get; set; }
        public static bool DebugWeShouldBeInSpaceORInStationAndOutOfSessionChange { get; set; }
        public static bool DebugWatchForActiveWars { get; set; }
        public static bool DebugMaintainConsoleLogs { get; set; }
        public static string ExtConsole { get; set; }

        public static string SessionDataCachePath { get; set; }
        public static string Logpath { get; set; }

        public static bool EnableVisualStyles { get; set; }
        public static bool DebugDisableAutoLogin { get; set; }

        public static string ConsoleLogPath { get; set; }
        public static string ConsoleLogFile { get; set; }
        public static bool SaveLogRedacted { get; set; }

        public static string redactedPlainLogLine { get; set; }
        public static string redactedColorLogLine { get; set; }
        public static string ConsoleLogPathRedacted { get; set; }
        public static string ConsoleLogFileRedacted { get; set; }

        // number of days of console logs to keep (anything older will be deleted on startup)

        public static int ConsoleLogDaysOfLogsToKeep { get; set; }

        public static string characterNameForLogs
        {
            get
            {
                if (String.IsNullOrEmpty(_characterNameForLogs))
                {
                    if (String.IsNullOrEmpty(MyCharacterName))
                    {
                        if (String.IsNullOrEmpty(Settings.Instance.CharacterName))
                        {
                            return "_PreLogin-UnknownCharacterName_";
                        }

                        return FilterPath(Settings.Instance.CharacterName);
                    }

                    return FilterPath(MyCharacterName);
                }

                return _characterNameForLogs;
            }
            set { _characterNameForLogs = value; }
        }

        public static event Message OnMessage;

        public static void Log(string line, bool verbose = false, [CallerMemberName]string DescriptionOfWhere = "")
        {
            try
            {
                SessionDataCachePath = PathToCurrentDirectory + "\\SessionDataCache\\" + characterNameForLogs + "\\";
                Logpath = PathToCurrentDirectory + "\\log\\" + characterNameForLogs + "\\";

                ConsoleLogPath = Path.Combine(Logpath, "Console\\");
                ConsoleLogFile = Path.Combine(ConsoleLogPath,
                    string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + characterNameForLogs + "-" + "console" + ".log");
                ConsoleLogPathRedacted = Path.Combine(Logpath, "Console\\");
                ConsoleLogFileRedacted = Path.Combine(ConsoleLogPath,
                    string.Format("{0:MM-dd-yyyy}", DateTime.Today) + "-" + "redacted" + "-" + "console" + ".log");

                DateTimeForLogs = DateTime.Now;

                if (verbose) //tons of info
                {
                    var sf = new StackFrame(1, true);
                    DescriptionOfWhere += "-[line" + sf.GetFileLineNumber().ToString() + "]in[" + Path.GetFileName(sf.GetFileName()) + "][" +
                                          sf.GetMethod().Name + "]";
                }

                colorLogLine = line;
                plainLogLine = line;
                
                
                var plainLogLineWithTime = String.Format("{0:HH:mm:ss} {1}", DateTimeForLogs,
                    "[" + DescriptionOfWhere + "] " + (plainLogLine));
                
                redactedPlainLogLine = String.Format("{0:HH:mm:ss} {1}", DateTimeForLogs,
                    "[" + DescriptionOfWhere + "] " + FilterSensitiveInfo(plainLogLine) + "\r\n"); //In memory Console Log with sensitive info redacted

                if (OnMessage != null)
                {
                    OnMessage(plainLogLineWithTime);
                }
                // eventlistener here
                //Console.Write(redactedPlainLogLine);

                if (!ConsoleLogOpened)
                {
                    PrepareConsoleLog();
                }

                if (ConsoleLogOpened)
                {
                    WriteToConsoleLog();
                }
            }
            catch (Exception exception)
            {
                BasicLog(DescriptionOfWhere, exception.Message);
            }
        }

        private static bool PrepareConsoleLog()
        {
            if (ConsoleLogPath != null && ConsoleLogFile != null)
            {
                if (!string.IsNullOrEmpty(ConsoleLogFile))
                {
                    Directory.CreateDirectory(ConsoleLogPath);
                    if (Directory.Exists(ConsoleLogPath))
                    {
                        ConsoleLogOpened = true;
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        private static void WriteToConsoleLog()
        {
            if (ConsoleLogFile != null) //normal
            {
                File.AppendAllText(ConsoleLogFile, redactedPlainLogLine); //Write In Memory Console log entry to File
            }

            if (ConsoleLogFileRedacted != null)
            {
                File.AppendAllText(ConsoleLogFileRedacted, redactedPlainLogLine); //Write In Memory Console log entry to File
            }

            return;
        }

        public static void BasicLog(string module, string logmessage)
        {
            try
            {
                Console.WriteLine("{0:HH:mm:ss} {1}", DateTime.UtcNow, "[" + module + "] " + logmessage);
                if (SaveLogRedacted && ConsoleLogFileRedacted != null)
                {
                    if (Directory.Exists(Path.GetDirectoryName(ConsoleLogFileRedacted)))
                    {
                        File.AppendAllText(ConsoleLogFileRedacted, string.Format("{0:HH:mm:ss} {1}", DateTime.UtcNow, "[" + module + "] " + logmessage));
                    }
                }

                if (SaveLogRedacted && ConsoleLogFile != null)
                {
                    if (Directory.Exists(Path.GetDirectoryName(ConsoleLogFile)))
                    {
                        File.AppendAllText(ConsoleLogFile, string.Format("{0:HH:mm:ss} {1}", DateTime.UtcNow, "[" + module + "] " + logmessage));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.ToString());
            }
        }

        public static void InvalidateCache()
        {
            _characterNameForLogs = string.Empty;
            return;
        }

        public static string FilterSensitiveInfo(string line)
        {
            try
            {
                if (line == null)
                    return string.Empty;
                if (!string.IsNullOrEmpty(MyCharacterName))
                {
                    line = line.Replace(MyCharacterName, MyCharacterName.Substring(0, 2) + "_MyEVECharacterNameRedacted_");
                    line = line.Replace("/" + MyCharacterName, "/" + MyCharacterName.Substring(0, 2) + "_MyEVECharacterNameRedacted_");
                    line = line.Replace("\\" + MyCharacterName, "\\" + MyCharacterName.Substring(0, 2) + "_MyEVECharacterNameRedacted_");
                    line = line.Replace("[" + MyCharacterName + "]", "[" + MyCharacterName.Substring(0, 2) + "_MyEVECharacterNameRedacted_]");
                    line = line.Replace(MyCharacterName + ".xml", MyCharacterName.Substring(0, 2) + "_MyEVECharacterNameRedacted_.xml");
                }

                if (!string.IsNullOrEmpty(EVELoginUserName) && !string.IsNullOrEmpty(EVELoginUserName))
                {
                    line = line.Replace(EVELoginUserName, EVELoginUserName.Substring(0, 2) + "_MyEVELoginNameRedacted_");
                }

                if (!string.IsNullOrEmpty(EVELoginPassword) && !string.IsNullOrWhiteSpace(EVELoginPassword))
                {
                    line = line.Replace(EVELoginPassword, EVELoginPassword.Substring(0, 2) + "_MyEVELoginPasswordRedacted_");
                }

                if (!string.IsNullOrEmpty(CharacterSettingsPath))
                {
                    line = line.Replace(CharacterSettingsPath, CharacterSettingsPath.Substring(0, 2) + "_MySettingsFileNameRedacted_.xml");
                }

                if (!String.IsNullOrEmpty(EVELoginUserName))
                {
                    line = line.Replace(EVELoginUserName, EVELoginUserName.Substring(0, 2) + "_HiddenEVELoginName_");
                }
                if (!String.IsNullOrEmpty(EVELoginPassword))
                {
                    line = line.Replace(EVELoginPassword, "_HiddenPassword_");
                }
                if (!string.IsNullOrEmpty(Environment.UserName))
                {
                    line = line.Replace("\\" + Environment.UserName + "\\", "\\_MyWindowsLoginNameRedacted_\\");
                    line = line.Replace("/" + Environment.UserName + "/", "/_MyWindowsLoginNameRedacted_/");
                }
                if (!string.IsNullOrEmpty(Environment.UserDomainName))
                {
                    line = line.Replace(Environment.UserDomainName, "_MyWindowsDomainNameRedacted_");
                }

                return line;
            }
            catch (Exception exception)
            {
                BasicLog("FilterSensitiveInfo", exception.Message);
                return line;
            }
        }

        public static string ReplaceUnderscoresWithSpaces(string line)
        {
            try
            {
                if (line == null)
                    return string.Empty;
                if (!string.IsNullOrEmpty(line))
                {
                    line = line.Replace("_", " ");
                }

                return line;
            }
            catch (Exception exception)
            {
                BasicLog("ReplaceUnderscoresWithSpaces", exception.Message);
                return line;
            }
        }

        public static string FilterPath(string path)
        {
            try
            {
                if (path == null)
                {
                    return string.Empty;
                }

                path = path.Replace("\"", "");
                path = path.Replace("?", "");
                path = path.Replace("\\", "");
                path = path.Replace("/", "");
                path = path.Replace("'", "");
                path = path.Replace("*", "");
                path = path.Replace(":", "");
                path = path.Replace(">", "");
                path = path.Replace("<", "");
                path = path.Replace(".", "");
                path = path.Replace(",", "");
                path = path.Replace("'", "");
                while (path.IndexOf("  ", StringComparison.Ordinal) >= 0)
                    path = path.Replace("  ", " ");
                return path.Trim();
            }
            catch (Exception exception)
            {
                Log("Exception [" + exception + "]");
                return null;
            }
        }

        public static void MaintainConsoleLogs()
        {
            const string searchpattern = ".log";
            var keepdate = DateTime.UtcNow.AddDays(-ConsoleLogDaysOfLogsToKeep);

            try
            {
                if (DebugMaintainConsoleLogs) Log("ConsoleLogPath is [" + ConsoleLogPath + "]");
                var fileListing = new DirectoryInfo(ConsoleLogPath);

                if (fileListing.Exists)
                {
                    if (DebugMaintainConsoleLogs) Log("if (fileListing.Exists)");
                    foreach (var log in fileListing.GetFiles(searchpattern))
                    {
                        if (DebugMaintainConsoleLogs)
                            Log("foreach (FileInfo log in fileListing.GetFiles(searchpattern))");
                        if (log.LastWriteTime <= keepdate)
                        {
                            if (DebugMaintainConsoleLogs) Log("if (log.LastWriteTime <= keepdate)");
                            try
                            {
                                Log("Removing old console log named [" + log.Name + "] Dated [" + log.LastWriteTime + "]");
                                log.Delete();
                            }
                            catch (Exception ex)
                            {
                                Log("Unable to delete log [" + ex.Message + "]");
                            }
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                BasicLog("FilterSensitiveInfo", exception.Message);
            }
        }

        public static class RichTextBoxExtensions
        {
            public static void AppendText(RichTextBox box, string text, Color color)
            {
                box.SelectionStart = box.TextLength;
                box.SelectionLength = 0;

                box.SelectionColor = color;
                box.AppendText(text);
                box.SelectionColor = box.ForeColor;
            }
        }
    }
}