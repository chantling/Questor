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
    using PySharp;
    using System;

    public class DirectDirectionalScannerWindow : DirectWindow
    {
        private List<DirectDirectionalScanResult> _scanResults;
        public readonly int MAX_SCANNER_RANGE = 2139249551;

        public bool IsReady { get; internal set; }

        internal DirectDirectionalScannerWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            var charId = DirectEve.Session.CharacterId;
            var obj = PyWindow.Attribute("busy");

            IsReady = charId != null && obj.IsValid && (bool)obj == false;
        }




        public int Range
        {
            get { return (int)PyWindow.Attribute("dir_rangeinput").Call("GetValue"); }
            set { PyWindow.Attribute("dir_rangeinput").Call("SetValue", value.ToString()); }
        }


        // __builtin__.uicore.registry.windows[6].angleCont.children._childrenObjects[1].children._childrenObjects[0].children._childrenObjects[0] -- angleSlider
        // slider method header: def SetValue(self, value, updateHandle = False, useIncrements = True, triggerCallback = True):

        public int Angle
        {
            get
            {
                var obj = PyWindow.Attribute("angleCont").Attribute("children").Attribute("_childrenObjects").ToList()[1].Attribute("children").Attribute("_childrenObjects").ToList()[0].Attribute("children").Attribute("_childrenObjects").ToList()[0];
                if (String.Compare((string)obj.Attribute("_name"), "angleSlider") == 0)
                {
                    return (int)obj.Call("GetValue");
                }

                return -1;
            }
            set
            {

                if (value <= 0)
                    return;

                if (value != 15 && value != 30 && value != 60 && value != 90 && value != 180 && value != 360)
                    return;

                if (this.Angle == value)
                    return;

                var obj = PyWindow.Attribute("angleCont").Attribute("children").Attribute("_childrenObjects").ToList()[1].Attribute("children").Attribute("_childrenObjects").ToList()[0].Attribute("children").Attribute("_childrenObjects").ToList()[0];
                if (String.Compare((string)obj.Attribute("_name"), "angleSlider") == 0)
                {

                    obj.Call("SetValue", value, true);
                    _lastDirectSearch = DateTime.UtcNow;
                    PyWindow.Call("EndSetAngleSliderValue", obj);
                }

            }
        }

        private enum DirectionalScannerDegreeValues
        {
            D15 = 15,
            D30 = 30,
            D60 = 60,
            D90 = 90,
            D180 = 180,
            D360 = 360
        }

        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        //self.sr.useoverview.checked
        public bool UserOverViewPreset
        {
            get { return (bool)PyWindow.Attribute("sr").Attribute("useoverview").Call("GetValue"); }
            set { PyWindow.Attribute("sr").Attribute("useoverview").Call("SetValue", value); }
        }

        private DateTime _lastDirectSearch = DateTime.MinValue;
        public void DirectionSearch()
        {
            if (_lastDirectSearch.AddSeconds(4) < DateTime.UtcNow) {
                _lastDirectSearch = DateTime.UtcNow;
                _scanResults = null;
                DirectEve.ThreadedCall(PyWindow.Attribute("DirectionSearch"));
            }
        }

        public List<DirectDirectionalScanResult> DirectionalScanResults
        {
            get
            {
                var charId = DirectEve.Session.CharacterId;
                if (_scanResults == null && charId != null)
                {
                    _scanResults = new List<DirectDirectionalScanResult>();
                    foreach (var result in PyWindow.Attribute("scanresult").ToList())
                    {
                        // scan result is a list of tuples
                        var resultAsList = result.ToList();
                        _scanResults.Add(new DirectDirectionalScanResult(DirectEve, resultAsList[0],
                            resultAsList[1], resultAsList[2]));
                    }
                }

                return _scanResults;
            }
        }

    }
}