/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 17:07
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Questor.Controllers
{
	/// <summary>
	/// Description of BaseController.
	/// </summary>
	public abstract class BaseController : IController
	{
		protected Random Rnd { get; set; }
		protected int RandomFactor { get; set; }
		public bool IsWorkDone { get; set; }
		protected DateTime LocalPulse { get; set; }
		
		protected BaseController()
		{
			Rnd = new Random();
			RandomFactor = 1;
		}
		
		protected bool CanWork { get { return DateTime.UtcNow > LocalPulse; } }

		public abstract void DoWork();

		protected int GetRandom(int minValue, int maxValue)
		{
			return Rnd.Next(minValue, maxValue) * RandomFactor;
		}
		protected DateTime GetUTCNowDelaySeconds(int minDelayInSeconds, int maxDelayInSeconds)
		{
			return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInSeconds * 1000, maxDelayInSeconds * 1000));
		}
		
		protected DateTime GetUTCNowDelayMilliseconds(int minDelayInMilliseconds, int maxDelayInMilliseconds)
		{
			return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInMilliseconds, maxDelayInMilliseconds));
		}
		
		internal int VeryLowRandom {
			get { return GetRandom(500,1000)*RandomFactor; }
		}
		
		internal int LowRandom {
			get { return GetRandom(1000,2000)*RandomFactor; }
		}
		
		internal int MediumRandom {
			get {  return GetRandom(3500,5000)*RandomFactor; }
		}
		
		internal int HighRandom {
			get { return GetRandom(5000,10000)*RandomFactor; }
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
				}
				m_Disposed = true;
			}
		}

		~BaseController()
		{
			Dispose(false);
		}

		#endregion
	}
}
