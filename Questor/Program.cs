
using Questor.Modules.Lookup;

namespace Questor
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using global::Questor.Modules.Activities;
    using global::Questor.Modules.BackgroundTasks;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Misc;
    

    public static class BeforeLogin
    {
        static BeforeLogin ()
        {
            Logging.UseInnerspace = true; //(defaults to true, will change to false IF passed -i Or -
        }
        
        private static Questor _questor;
        
        private static void ParseArgs(IEnumerable<string> args)
        {
            if (!string.IsNullOrEmpty(Logging.EVELoginUserName) &&
                !string.IsNullOrEmpty(Logging.EVELoginPassword) &&
                !string.IsNullOrEmpty(Logging.MyCharacterName))
            {
                return;
            }

            OptionSet p = new OptionSet
            {
                "Usage: questor [OPTIONS]",
                "Run missions and make uber ISK.",
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
                //Logging.Log(string.Format("questor: extra = {0}", string.Join(" ", extra.ToArray())));
            }
            catch (OptionException ex)
            {
                Logging.Log("Startup", "questor: ", Logging.White);
                Logging.Log("Startup", ex.Message, Logging.White);
                Logging.Log("Startup", "Try `questor --help' for more information.", Logging.White);
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

        
        public static void Main(string[] args)
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
            if (LoginToEVE.OptionallyLoadPreLoginSettingsFromINI(args))
            {
                if (Logging.DebugBeforeLogin) Logging.Log("Startup:", "OptionallyLoadPreLoginSettingsFromINI was true", Logging.Debug);    
            }
            else
            {
                Logging.Log("Startup:", "OptionallyLoadPreLoginSettingsFromINI was false!", Logging.Debug);
            }
            
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
            
            Time.Instance.LoginStarted_DateTime = DateTime.UtcNow;

            if (!Logging.DebugDisableAutoLogin)
            {
                try
                {
                    Cache.Instance.DirectEve.OnFrame += LoginToEVE.LoginOnFrame;
                    LoginToEVE.useLoginOnFrameEvent = true;
                }
                catch (Exception ex)
                {
                    Logging.Log("Startup", string.Format("DirectEVE.OnFrame: Exception {0}...", ex), Logging.White);
                }

                // Sleep until we're loggedInAndreadyToStartQuestorUI
                while (LoginToEVE.useLoginOnFrameEvent)
                {
                    System.Threading.Thread.Sleep(50); //this runs while we wait to login
                }
                
                try
                {
                    Logging.Log("Startup", "We are logged into EVE: unRegistering LoginOnFrame Event, wait 3 sec", Logging.Teal);
                    Cache.Instance.DirectEve.OnFrame -= LoginToEVE.LoginOnFrame;
                    LoginToEVE.StartTime = DateTime.Now;
                    LoginToEVE.DoneLoggingInToEVETimeStamp = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    Logging.Log("Startup", "DirectEVE.Dispose: Exception [" + ex + "]", Logging.White);
                }

                while (DateTime.UtcNow < LoginToEVE.DoneLoggingInToEVETimeStamp.AddSeconds(5)) //wait a few seconds
                {
                    Logging.Log("Startup", "Waiting a few seconds before launching Questor", Logging.White);
                    System.Threading.Thread.Sleep(1000); //this runs while we wait for the LoginOnframe Event to unregister
                }

                // If the last parameter is false, then we only auto-login
                if (LoginToEVE._loginOnly)
                {
                    Logging.Log("Startup", "_loginOnly: done and exiting", Logging.Teal);
                    return;
                }

                //
                // We should only get this far if run if we are already logged in...
                // launch questor
                //
                try
                {
                    Logging.Log("Startup", "Launching Questor", Logging.Teal);
                    _questor = new Questor();

                    int intdelayQuestorUI = 0;
                    while (intdelayQuestorUI < 200) //10sec = 200ms x 50
                    {
                        intdelayQuestorUI++;
                        System.Threading.Thread.Sleep(50);
                    }

                    Logging.Log("Startup", "Launching QuestorUI", Logging.Teal);
                    Application.Run(new QuestorUI());

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
            else
            {
                Logging.Log("Startup", "DebugDisableAutoLogin is true (check characters prelogin settings ini), closing before doing anything useful!", Logging.Debug);
                Cache.Instance.DirectEve.Dispose();
            }
        }
    }
}