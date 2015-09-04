// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Collections.Generic;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;

namespace QuestorManager
{
    using System;
    using System.Windows.Forms;
    using Questor.Modules.Logging;
    using global::Questor.Modules.Activities;
    using global::Questor.Modules.Misc;

    internal static class BeforeLogin
    {
        private static QuestorManagerUI _questorManaagerUI;

        private static void ParseArgs(IEnumerable<string> args)
        {
            if (!string.IsNullOrEmpty(Logging.EVELoginUserName) &&
                !string.IsNullOrEmpty(Logging.EVELoginPassword) &&
                !string.IsNullOrEmpty(Logging.MyCharacterName))
            {
                return;
            }

            OptionSet p = new OptionSet {
        		"Usage: QuestorManager [OPTIONS]",
        		"Scriptable: Traveler, Transport, Simple Buy/Sell Market Actions, etc",
        		"",
        		"Options:",
        		{"u|user=", "the {USER} we are logging in as.", v => Logging.EVELoginUserName = v},
                {"p|password=", "the user's {PASSWORD}.", v => Logging.EVELoginPassword = v},
                {"c|character=", "the {CHARACTER} to use.", v => Logging.MyCharacterName = v},
                {"l|loginOnly", "login only and exit.", v => LoginToEVE._loginOnly = v != null},
                {"x|chantling|scheduler", "use scheduler (thank you chantling!)", v => LoginToEVE._chantlingScheduler = v != null},
                {"n|loginNow", "Login using info in scheduler", v => LoginToEVE._loginNowIgnoreScheduler = v != null},
                {"i|standalone", "Standalone instance, hook D3D w/o Innerspace!", v => LoginToEVE._standaloneInstance = v != null},
                {"h|help", "show this message and exit", v => LoginToEVE._showHelp = v != null}
        	};


            try
            {
                LoginToEVE._QuestorParamaters = p.Parse(args);
            }
            catch (OptionException ex)
            {
                Logging.Log("Startup", "QuestorManager: ", Logging.White);
                Logging.Log("Startup", ex.Message, Logging.White);
                Logging.Log("Startup", "Try `QuestorManager --help' for more information.", Logging.White);
                return;
            }

            if (LoginToEVE._showHelp)
            {
                System.IO.StringWriter sw = new System.IO.StringWriter();
                p.WriteOptionDescriptions(sw);
                Logging.Log("Startup", sw.ToString(), Logging.White);
                return;
            }
        }

    	/// <summary>
        ///   The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            //if (Logging.DebugPreLogin)
            //{
            //    int i = 0;
            //    foreach (string arg in args)
            //    {
            //        Logging.Log("Startup", " *** Questor Parameters we have parsed [" + i + "] - [" + arg + "]", Logging.Debug);
            //        i++;
            //    }
            //}

            ParseArgs(args);
            LoginToEVE.OptionallyLoadPreLoginSettingsFromINI(args);

            //
            // Wait to login based on schedule info from schedules.xml
            //
            if (LoginToEVE._chantlingScheduler && !string.IsNullOrEmpty(Logging.MyCharacterName))
            {
                LoginToEVE.WaitToLoginUntilSchedulerSaysWeShould();
            }

            //
            // direct login, no schedules.xml
            //
            if (!string.IsNullOrEmpty(Logging.EVELoginUserName) && !string.IsNullOrEmpty(Logging.EVELoginPassword) && !string.IsNullOrEmpty(Logging.MyCharacterName))
            {
                LoginToEVE.ReadyToLoginToEVEAccount = true;
            }

            if (!LoginToEVE.LoadDirectEVEInstance()) return;

            //if (!LoginToEVE.VerifyDirectEVESupportInstancesAvailable()) return;

            if (!Logging.DebugDisableAutoLogin)
            {
                try
                {
                    //Cache.Instance.DirectEve.OnFrame += LoginToEVE.LoginOnFrame;
                }
                catch (Exception ex)
                {
                    Logging.Log("Startup", string.Format("DirectEVE.OnFrame: Exception {0}...", ex), Logging.White);
                }

                // Sleep until we're loggedInAndreadyToStartQuestorUI
                while (!LoginToEVE.loggedInAndreadyToStartQuestorUI)
                {
                    System.Threading.Thread.Sleep(50); //this runs while we wait to login
                }

                if (LoginToEVE.loggedInAndreadyToStartQuestorUI)
                {
                    try
                    {
                        //Cache.Instance.DirectEve.OnFrame -= LoginToEVE.LoginOnFrame;
                    }
                    catch (Exception ex)
                    {
                        Logging.Log("Startup", "DirectEVE.Dispose: Exception [" + ex + "]", Logging.White);
                    }

                    // If the last parameter is false, then we only auto-login
                    if (LoginToEVE._loginOnly)
                    {
                        Logging.Log("Startup", "_loginOnly: done and exiting", Logging.Teal);
                        return;
                    }


                    LoginToEVE.StartTime = DateTime.Now;

                    //
                    // We should only get this far if run if we are already logged in...
                    // launch questor
                    //
                    try
                    {

                        Logging.Log("Startup", "We are logged in.", Logging.Teal);
                        Logging.Log("Startup", "Launching Questor", Logging.Teal);
                        _questorManaagerUI = new QuestorManagerUI();

                        int intdelayQuestorManagerUI = 0;
                        while (intdelayQuestorManagerUI < 50) //2.5sec = 50ms x 50
                        {
                            intdelayQuestorManagerUI++;
                            System.Threading.Thread.Sleep(50);
                        }

                        Logging.Log("Startup", "Launching QuestorManagerUI", Logging.Teal);
                        Application.Run(new QuestorManagerUI());

                        while (!Cleanup.SignalToQuitQuestor)
                        {
                            System.Threading.Thread.Sleep(50); //this runs while questor is running.
                        }

                        Logging.Log("Startup", "Exiting Questor", Logging.Teal);

                    }
                    catch (Exception ex)
                    {
                        Logging.Log("Startup", "Exception [" + ex + "]", Logging.Teal);
                    }
                    finally
                    {
                        Cleanup.DirecteveDispose();
                        AppDomain.Unload(AppDomain.CurrentDomain);
                    }
                }
            }
            else
            {
                Logging.Log("Startup", "DebugDisableAutoLogin is true (check characters prelogin settings ini), closing before doing anything useful!", Logging.Debug);
                Cache.Instance.DirectEve.Dispose();
            }
        }
    }
}