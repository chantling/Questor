//using System;
//using System.Windows.Forms;
//using Questor.Modules.Logging;
//using Questor.Modules.Misc;
//
//namespace ValueDump
//{
//    internal static class ValueDump
//    {
//    	private static bool _standaloneInstance;
//    	private static bool _showHelp;
//    	
//        /// <summary>
//        /// The main entry point for the application.
//        /// </summary>
//        [STAThread]
//        private static void Main(string[] args)
//        {
//        	
//        	OptionSet p = new OptionSet {
//        		"Usage: QuestorManager [OPTIONS]",
//        		"",
//        		"",
//        		"Options:",
//        		{"i|standalone instance", "Standalone instance, hook D3D w/o Innerspace!", v => _standaloneInstance = v != null},
//        		{"h|help", "show this message and exit", v => _showHelp = v != null}
//        	};
//        	
//        	
//        	try
//        	{
//        		p.Parse(args);
//        	}
//        	catch (OptionException ex)
//        	{
//        		Logging.Log("Startup", "ValueDump: ", Logging.White);
//        		Logging.Log("Startup", ex.Message, Logging.White);
//        		Logging.Log("Startup", "Try `ValueDump --help' for more information.", Logging.White);
//        		return;
//        	}
//        	
//        	if (_showHelp)
//        	{
//        		System.IO.StringWriter sw = new System.IO.StringWriter();
//        		p.WriteOptionDescriptions(sw);
//        		Logging.Log("Startup", sw.ToString(), Logging.White);
//        		return;
//        	}
//        	
//            try
//            {	
//                Application.Run(new ValueDumpUI(_standaloneInstance));
//            }
//            catch (Exception) { }
//        }
//    }
//}