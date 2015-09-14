/*
 * ---------------------------------------
 * User: duketwo
 * Date: 14.09.2015
 * Time: 14:37
 * 
 * ---------------------------------------
 */
using System;
using System.Linq;
using System.Windows.Forms;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;
using Questor.Modules.Logging;
using Questor.Modules.States;

namespace SubModules
{
	/// <summary>
	/// Description of Debug.
	/// </summary>
	public class Debug
	{
		private static DateTime _nextPulse = DateTime.MinValue;
		private static DateTime _lastSessionNotReady = DateTime.MinValue;
		
		public Debug()
		{
			
			Logging.Log("Debug", "Debug started." , Logging.White);
			
			if(Cache.Instance.DirectEve != null) {
				
				Cache.Instance.DirectEve.OnFrame += DebugOnFrame;
			} else {
				Logging.Log("Debug", "if(Cache.Instance.DirectEve == null)" , Logging.White);
			}
		}
		
		private void DebugOnFrame(object sender, EventArgs e)
		{
			if(_nextPulse > DateTime.UtcNow) {
				return;
			}
			
			if(_lastSessionNotReady.AddSeconds(8) > DateTime.UtcNow) {
				return;
			}
			
			Logging.Log("Debug", "Hello" , Logging.White);
			
			_nextPulse = DateTime.UtcNow.AddMilliseconds(400);
			
			if(!Cache.Instance.DirectEve.Session.IsInSpace) {
				return;
			}
			
			if(!Cache.Instance.DirectEve.Session.IsReady) {
				_lastSessionNotReady = DateTime.UtcNow;
				return;
			}
			
			foreach(DirectEntity ent in Cache.Instance.DirectEve.Entities.Where(en => en.CategoryId != (int)CategoryID.Charge)) {
				
				if(Cache.Instance.DirectEve.GetTargets().ContainsKey(ent.Id)) {
					Logging.Log("DebugOnFrame", "Ent [" + ent.Name + "]" +  " Id [" + ent.Id + "]" + " Is still targeted." , Logging.White);
				}
			}
			
			
		}
	}
}