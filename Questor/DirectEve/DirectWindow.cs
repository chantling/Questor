﻿// ------------------------------------------------------------------------------
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using PySharp;

    public class DirectWindow : DirectObject
    {
        private static WindowType[] _windowTypes = new[]
        {
            new WindowType("name", "marketsellaction", (directEve, pyWindow) => new DirectMarketActionWindow(directEve, pyWindow)),
            new WindowType("name", "marketbuyaction", (directEve, pyWindow) => new DirectMarketActionWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.AgentDialogueWindow", (directEve, pyWindow) => new DirectAgentWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.VirtualInvWindow", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.PVPOfferView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.PVPTrade", (directEve, pyWindow) => new DirectTradeWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.SpyHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.StationItems", (directEve, pyWindow) => new DirectOwnContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Inventory", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InventoryPrimary", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InventorySecondary", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationItems", (directEve, pyWindow) => new DirectOwnContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationCorpHangars", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.CorpHangarArray", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.CorpMemberHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.CorpMarketHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ShipCargoView", (directEve, pyWindow) => new DirectOwnContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ActiveShipCargo", (directEve, pyWindow) => new DirectOwnContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DockedCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InflightCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LootCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DroneBay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RegionalMarket", (directEve, pyWindow) => new DirectMarketWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LSCChannel", (directEve, pyWindow) => new DirectChatWindow(directEve, pyWindow)),
            new WindowType("name", "telecom", (directEve, pyWindow) => new DirectTelecomWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.FittingMgmt", (directEve, pyWindow) => new DirectFittingManagerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Scanner", (directEve, pyWindow) => new DirectScannerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ReprocessingDialog", (directEve, pyWindow) => new DirectReprocessingWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LPStore", (directEve, pyWindow) => new DirectLoyaltyPointStoreWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RepairShopWindow", (directEve, pyWindow) => new DirectRepairShopWindow(directEve, pyWindow)),
            new WindowType("windowID", "directionalScanner", (directEve, pyWindow) => new DirectDirectionalScannerWindow(directEve, pyWindow))
        };

        internal PyObject PyWindow;

        internal DirectWindow(DirectEve directEve, PyObject pyWindow) : base(directEve)
        {
            PyWindow = pyWindow;

            Id = (int?)pyWindow.Attribute("windowID");
            Type = (string)pyWindow.Attribute("__guid__");
            Name = (string)pyWindow.Attribute("name");
            IsKillable = (bool)pyWindow.Attribute("_killable");
            IsDialog = (bool)pyWindow.Attribute("isDialog");
            IsModal = (bool)pyWindow.Attribute("isModal");
            Caption = (string)pyWindow.Call("GetCaption");

            var paragraphs = pyWindow.Attribute("edit").Attribute("sr").Attribute("paragraphs").ToList();

            //var html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
            //if (String.IsNullOrEmpty(html))
            //    html = (string)pyWindow.Attribute("edit").Attribute("sr").Attribute("currentTXT");
            //if (String.IsNullOrEmpty(html))
            //    html = (string)pyWindow.Attribute("sr").Attribute("messageArea").Attribute("sr").Attribute("currentTXT");

            //Html = html;

            ViewMode = (string)pyWindow.Attribute("viewMode");
        }

        public int? Id { get; internal set; }
        public string Type { get; internal set; }
        public string Name { get; internal set; }
        public string Caption { get; internal set; }

        string html;
        public string Html
        {
            get
            {
                if (!String.IsNullOrEmpty(html))
                    return html;

                var paragraphs = PyWindow.Attribute("edit").Attribute("sr").Attribute("paragraphs").ToList();
                html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
                if (String.IsNullOrEmpty(html))
                    html = (string)PyWindow.Attribute("edit").Attribute("sr").Attribute("currentTXT");
                if (String.IsNullOrEmpty(html))
                {
                    paragraphs = PyWindow.Attribute("sr").Attribute("messageArea").Attribute("sr").Attribute("paragraphs").ToList();
                    html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
                }

                return html;
            }
        }

        public string ViewMode { get; internal set; }

        public bool IsKillable { get; internal set; }
        public bool IsDialog { get; internal set; }
        public bool IsModal { get; internal set; }

        internal static List<DirectWindow> GetWindows(DirectEve directEve)
        {
            var windows = new List<DirectWindow>();

            var pySharp = directEve.PySharp;
            var builtin = pySharp.Import("__builtin__");
            var pyWindows = builtin.Attribute("uicore").Attribute("registry").Call("GetWindows").ToList();
            foreach (var pyWindow in pyWindows)
            {
                // Ignore destroyed windows
                if ((bool)pyWindow.Attribute("destroyed"))
                    continue;

                DirectWindow window = null;
                foreach (var windowType in _windowTypes)
                {
                    if ((string)pyWindow.Attribute(windowType.Attribute) != windowType.Value)
                        continue;

                    window = windowType.Creator(directEve, pyWindow);
                }

                if (window == null)
                    window = new DirectWindow(directEve, pyWindow);

                windows.Add(window);
            }

            return windows;
        }

        public virtual bool Close()
        {
            if (!IsKillable)
                return false;

            return DirectEve.ThreadedCall(PyWindow.Attribute("CloseByUser"));
        }

        /// <summary>
        ///     Find a child object (usually button)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static PyObject FindChild(PyObject container, string name)
        {
            var childs = container.Attribute("children").Attribute("_childrenObjects").ToList();
            return childs.Find(c => String.Compare((string)c.Attribute("_name"), name) == 0) ?? global::DirectEve.PySharp.PySharp.PyZero;
        }

        /// <summary>
        ///     Find a child object (using the supplied path)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static PyObject FindChildWithPath(PyObject container, IEnumerable<string> path)
        {
            return path.Aggregate(container, FindChild);
        }

        // FROM CONST: probably better to read the values from the game
        //ID_NONE = 0
        //ID_OK = 1
        //ID_CANCEL = 2
        //ID_YES = 6
        //ID_NO = 7
        //ID_CLOSE = 8
        //ID_HELP = 9

        public enum ModalResultType
        {
            NONE,
            OK,
            CANCEL,
            YES,
            NO,
            CLOSE,
            HELP
        }

        public int GetModalResult(ModalResultType mr)
        {
            int modalResult = 0;
            switch (mr)
            {
                case ModalResultType.NONE:
                    modalResult = 0;
                    break;
                case ModalResultType.OK:
                    modalResult = 1;
                    break;
                case ModalResultType.CANCEL:
                    modalResult = 2;
                    break;
                case ModalResultType.YES:
                    modalResult = 6;
                    break;
                case ModalResultType.NO:
                    modalResult = 7;
                    break;
                case ModalResultType.CLOSE:
                    modalResult = 8;
                    break;
                case ModalResultType.HELP:
                    modalResult = 9;
                    break;
            }

            return modalResult;
        }

        public bool SetModalResult(ModalResultType mr)
        {
            int modalResult = GetModalResult(mr);
            if (this.IsModal)
                return DirectEve.ThreadedCall(PyWindow.Attribute("SetModalResult"), new object[] { modalResult });
            return false;
        }

        /// <summary>
        ///     Answers a modal window
        /// </summary>
        /// <param name="button">a string indicating which button to press. Possible values are: Yes, No, Ok, Cancel, Suppress</param>
        /// <returns>true if successfull</returns>
        public bool AnswerModal(string button)
        {
            //string[] buttonPath = { "__maincontainer", "bottom", "btnsmainparent", "btns", "Yes_Btn" };

            if (!this.IsModal)
                return false;

            ModalResultType mr = ModalResultType.YES;

            switch (button)
            {
                case "Yes":
                    break;
                case "No":
                    //buttonPath[4] = "No_Btn";
                    mr = ModalResultType.NO;
                    break;
                case "OK":
                case "Ok":
                    if (Name == "Set Quantity")
                    {
                        if (PyWindow != null)
                        {
                            PyWindow.Call("Confirm", 12345);
                            return true;
                        }
                        return false;
                    }
                    mr = ModalResultType.OK;
                    //buttonPath[4] = "OK_Btn";
                    break;
                case "Cancel":
                    mr = ModalResultType.CANCEL;
                    //buttonPath[4] = "Cancel_Btn";
                    break;
                //case "Suppress":
                //string[] suppress = { "__maincontainer", "main", "suppressContainer", "suppress" };
                //buttonPath = suppress;
                //break;
                default:
                    return false;
            }

            //var btn = FindChildWithPath(PyWindow, buttonPath);
            //if (btn != null)
            //    return DirectEve.ThreadedCall(btn.Attribute("OnClick"));
            return SetModalResult(mr);

            //return false;
        }

        #region Nested type: WindowType

        private class WindowType
        {
            public WindowType(string attribute, string value, Func<DirectEve, PyObject, DirectWindow> creator)
            {
                Attribute = attribute;
                Value = value;
                Creator = creator;
            }

            public string Attribute { get; set; }
            public string Value { get; set; }
            public Func<DirectEve, PyObject, DirectWindow> Creator { get; set; }
        }

        #endregion
    }
}