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
using System.Xml.Linq;

namespace Questor.Modules.Lookup
{
    public class ShipTargetValue
    {
        public ShipTargetValue(XElement element)
        {
            try
            {
                Name = (string) element.Attribute("name");
                GroupId = (int) element.Attribute("groupid");
                TargetValue = (int) element.Attribute("targetvalue");
            }
            catch (Exception)
            {
                //add logging here
            }
        }

        public string Name { get; private set; }

        public int GroupId { get; private set; }

        public int TargetValue { get; private set; }
    }
}