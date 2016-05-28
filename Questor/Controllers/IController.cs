/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 17:08
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Questor.Controllers
{
	/// <summary>
	/// Description of IController.
	/// </summary>
  public interface IController
    {
        bool IsWorkDone { get; set; }
        void DoWork();
    }
}
