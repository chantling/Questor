/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 17:38
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Questor.Controllers;
using Questor;
using Questor.Modules.Caching;
using Questor.Modules.Logging;

namespace Questor
{
	/// <summary>
	/// Description of QuestorControllerManager.
	/// </summary>
	public class QuestorControllerManager
	{
		
		DateTime Pulse { get; set; }
		Random Rnd { get; set; }
		int PulseDelayMilliseconds { get; set; }
		public HashSet<IController> ControllerList;
		
		
		public QuestorControllerManager(int pulseDelayMilliseconds = 800)
		{
			
			this.PulseDelayMilliseconds = pulseDelayMilliseconds;
			this.Pulse = DateTime.MinValue;
			Rnd = new Random();
			ControllerList = new HashSet<IController>();
			
			if (!Cache.LoadDirectEVEInstance(Cache.D3DVersion)) return;
			Cache.Instance.DirectEve.OnFrame += EVEOnFrame;
			
		}
		
		void EVEOnFrame(object sender, EventArgs e)
		{
			if (DateTime.UtcNow < NextPulse)
				return;
			
			foreach(IController controller in ControllerList.Where(a => !a.IsWorkDone).ToList()){
				
				var deps = controller.GetControllerDependencies();
				
				if(deps.Any()) {
					// there are denpendencies, lets check if every single one is met
					if(ControllerList.All( c => deps.Any( d =>  d.Key.Equals(c.GetType()) && d.Value == c.IsWorkDone))) {
						controller.DoWork();
					}
					
				} else {
					// no dependencies
					controller.DoWork();
				}
			}
		}
		
		public void AddController(IController controller)
		{
			if(ControllerList.All(c => !c.GetType().Equals(controller.GetType())))
				ControllerList.Add(controller);
		}
		
		public void RemoveAllControllers() {
			ControllerList.RemoveWhere(s => s != null);
		}
		
		public void RemovedFinishedControllers() {
			ControllerList.RemoveWhere(s => s.IsWorkDone);
		}
		
		public void RemoveControllerOfType(Type t) {
			ControllerList.RemoveWhere( c => c.GetType().Equals(t)); //RemoveControllerType(typeof(LoginController));
		}
		
		
		public DateTime NextPulse
		{
			set { Pulse = value; }
			get
			{
				DateTime ret = Pulse;
				if (DateTime.UtcNow >= Pulse)
				{
					Pulse = DateTime.UtcNow.AddMilliseconds(this.PulseDelayMilliseconds);
				}
				return ret;
			}
		}
	}
}
