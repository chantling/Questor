// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace DirectEve
{
    using System.Collections.Generic;
    using System.Linq;
    using PySharp;

    public class DirectRepairShopWindow : DirectWindow
    {
        internal DirectRepairShopWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        public bool RepairItems(List<DirectItem> items)
        {
            PyObject util = PySharp.Import("util");
            var PyItems = items.Select(i => i.PyItem);
            PyItems = PyItems.Where(i => (util.Call("IsItemOfRepairableType", i)).ToBool());
            if (PyItems.Any())
                return DirectEve.ThreadedCall(PyWindow.Attribute("DisplayRepairQuote"), PyItems);
            return false;
        }

        // OpenWindow -> SelectAll() -> GetSelected() > 0 -> QuoteItems() -> GetAll() > 0 -> RepairAll()

        public bool SelectAll()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("sr").Attribute("scroll").Attribute("SelectAll"));
        }

        public List<PyObject> GetAll()
        {
            return PyWindow.Call("GetAll").ToList<PyObject>();
        }

        public List<PyObject> GetSelected()
        {
            return PyWindow.Call("GetSelected").ToList<PyObject>();
        }

        public bool QuoteItems()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("QuoteItems"));
        }

        public bool RepairAll()
        {

            return DirectEve.ThreadedCall(PyWindow.Attribute("RepairAll"));
        }

        public string AvgDamage()
        {

            try
            {
                return (string)PyWindow.Attribute("sr").Attribute("avgDamage").Attribute("text");
            }
            catch
            {
                return "";
            }
        }
    }
}