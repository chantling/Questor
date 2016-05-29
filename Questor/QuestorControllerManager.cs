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
using Questor.Modules.Lookup;
using Questor.Modules.BackgroundTasks;

namespace Questor
{
	/// <summary>
	/// Description of QuestorControllerManager.
	/// </summary>
	public class QuestorControllerManager : IDisposable
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
			
			#region header for every controller
			
			
			if (Cache.Instance.Paused)
			{
				// Chant - 05/02/2016 - Reset our timeouts so we don't exit every time we're paused for more than a few seconds
				Time.Instance.LastSessionIsReady = DateTime.UtcNow;
				Time.Instance.LastFrame = DateTime.UtcNow;
				Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
				NavigateOnGrid.AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
				Cache.Instance.CanSafelyCloseQuestorWindow = true;
				return;
			}

			
			#endregion end header for every controller
			
			foreach(IController controller in ControllerList.Where(a => !a.IsWorkDone).ToList()){
				
				var deps = controller.GetControllerDependencies();
				
				if(deps.Any()) {
					
					// there are denpendencies, lets check if every single one is met
					if(deps.All( d => ControllerList.Any( c => c.GetType().Equals(d.Key)  && c.IsWorkDone == d.Value))) {
						
						controller.DoWork();
					}
					
				} else {
					// no dependencies
					controller.DoWork();
				}
			}
		}
		
		public void AddControllers(IEnumerable<IController> controllers) {
			foreach(var controller in controllers) {
				this.AddController(controller);
			}
		}
		
		public void AddController(IController controller)
		{
			if(ControllerList.All(c => !c.GetType().Equals(controller.GetType())))
				ControllerList.Add(controller);
		}
		
		public bool IsControllerOfTypeExistingAlready(Type t) {
			return ControllerList.Any(c => c.GetType().Equals(t));
		}
		
		public void RemoveAllControllers() {
			ControllerList.RemoveWhere(s => s != null);
		}
		
		public void RemovedFinishedControllers() {
			ControllerList.RemoveWhere(s => s.IsWorkDone);
		}
		
		public void RemoveControllers(IEnumerable<Type> controllers) {
			foreach(var t in controllers) {
				this.RemoveController(t);
			}
		}
		
		public void RemoveController(Type t) {
			ControllerList.RemoveWhere( c => c.GetType().Equals(t)); //RemoveControllerType(typeof(LoginController));
		}
		
		public void RemoveController(IController controller) {
			ControllerList.RemoveWhere(c => c == controller);
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
		
		
		#region IDisposable implementation

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		
		private bool m_Disposed = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!m_Disposed)
			{
				if (disposing)
				{
					if(Cache.Instance.DirectEve != null) {
						Cache.Instance.DirectEve.OnFrame -= EVEOnFrame;
						Cache.Instance.DirectEve.Dispose();
					}
					
				}
				m_Disposed = true;
			}
		}

		~QuestorControllerManager()
		{
			Dispose(false);
		}

		#endregion
	}
}
