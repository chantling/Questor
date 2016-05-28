//------------------------------------------------------------------------------
//  <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//    Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//    Please look in the accompanying license.htm file for the license that
//    applies to this source code. (a copy can also be found at:
//    http://www.thehackerwithin.com/license.htm)
//  </copyright>
//-------------------------------------------------------------------------------

using System;
using Questor.Modules.Actions;
using Questor.Modules.Logging;
using Questor.Modules.States;

namespace QuestorManager.Actions
{
    public class ValueDump
    {
        private readonly Market _market;
        private readonly QuestorManagerUI _questorManagerForm;
        private DateTime _lastExecute = DateTime.MinValue;
        private bool _valueProcess; //false

        public ValueDump(QuestorManagerUI form1)
        {
            _questorManagerForm = form1;
            _market = new Market();
        }

        public void ProcessState()
        {
            switch (_States.CurrentValueDumpState)
            {
                case ValueDumpState.CheckMineralPrices:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.CheckMineralPrices:");
                    if (!Market.CheckMineralPrices("ValueDump", _questorManagerForm.RefineCheckBox.Checked)) return;
                    _States.CurrentValueDumpState = ValueDumpState.SaveMineralPrices;
                    break;

                case ValueDumpState.SaveMineralPrices:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.SaveMineralPrices:");
                    if (!Market.SaveMineralprices("ValueDump")) return;
                    _States.CurrentValueDumpState = ValueDumpState.Idle;
                    break;

                case ValueDumpState.GetItems:
                    /* fixme: 6/2014
                    if (Logging.DebugValuedump) Logging.Log("ValueDump", "case ValueDumpState.GetItems:", Logging.Debug);
                    if (Cache.Instance.ItemHangar == null) return;
                    Logging.Log("ValueDump", "Loading hangar items", Logging.White);

                    // Clear out the old
                    Market.Items.Clear();
                    List<DirectItem> hangarItems = Cache.Instance.ItemHangar.Items;
                    if (hangarItems != null)
                    {
                        Market.Items.AddRange(hangarItems.Where(i => i.ItemId > 0 && i.MarketGroupId > 0 && i.Quantity > 0).Select(i => new ItemCacheMarket(i, _questorManagerForm.RefineCheckBox.Checked)));
                    }

                    _States.CurrentValueDumpState = ValueDumpState.UpdatePrices;
                     */
                    break;

                case ValueDumpState.UpdatePrices:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.UpdatePrices:");
                    if (
                        !Market.UpdatePrices("ValueDump", _questorManagerForm.cbxSell.Checked, _questorManagerForm.RefineCheckBox.Checked,
                            _questorManagerForm.cbxUndersell.Checked)) return;
                    //
                    // we are out of items
                    //
                    _States.CurrentValueDumpState = ValueDumpState.Idle;
                    break;

                case ValueDumpState.Idle:
                case ValueDumpState.Done:
                    break;

                case ValueDumpState.Begin:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.Begin:");
                    if (_questorManagerForm.RefineCheckBox.Checked && _questorManagerForm.cbxSell.Checked)
                    {
                        _questorManagerForm.cbxSell.Checked = false;
                        _valueProcess = true;
                        _States.CurrentValueDumpState = ValueDumpState.GetItems;
                    }
                    else if (_questorManagerForm.RefineCheckBox.Checked && _valueProcess)
                    {
                        _questorManagerForm.RefineCheckBox.Checked = false;
                        _questorManagerForm.cbxSell.Checked = true;
                        _valueProcess = false;
                        _States.CurrentValueDumpState = ValueDumpState.GetItems;
                    }
                    else
                        _States.CurrentValueDumpState = ValueDumpState.GetItems;
                    break;

                case ValueDumpState.NextItem:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.NextItem:");
                    if (!Market.NextItem("ValueDump")) return;
                    if (_questorManagerForm.cbxSellOrder.Checked)
                        _States.CurrentValueDumpState = ValueDumpState.SellOrder;
                    else
                        _States.CurrentValueDumpState = ValueDumpState.StartQuickSell;
                    break;

                case ValueDumpState.SellOrder:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.SellOrder:");
                    if (!Market.CreateSellOrder("ValueDump", 90, _questorManagerForm.cbxCorpOrder.Checked)) return;
                    _States.CurrentValueDumpState = ValueDumpState.NextItem;
                    break;

                case ValueDumpState.StartQuickSell:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.StartQuickSell:");
                    if (!Market.StartQuickSell("ValueDump", _questorManagerForm.cbxSell.Checked)) return;
                    _States.CurrentValueDumpState = ValueDumpState.InspectOrder;
                    break;

                case ValueDumpState.InspectOrder:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.InspectOrder:");
                    if (
                        !Market.Inspectorder("ValueDump", _questorManagerForm.cbxSell.Checked, _questorManagerForm.RefineCheckBox.Checked,
                            _questorManagerForm.cbxUndersell.Checked, (double) _questorManagerForm.RefineEfficiencyInput.Value)) return;
                    _States.CurrentValueDumpState = ValueDumpState.WaitingToFinishQuickSell;
                    break;

                case ValueDumpState.InspectRefinery:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.InspectRefinery:");
                    if (!Market.InspectRefinery("ValueDump", (double) _questorManagerForm.RefineEfficiencyInput.Value))
                        _States.CurrentValueDumpState = ValueDumpState.NextItem;
                    break;

                case ValueDumpState.WaitingToFinishQuickSell:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.WaitingToFinishQuickSell:");
                    if (!Market.WaitingToFinishQuickSell("ValueDump")) return;
                    _States.CurrentValueDumpState = ValueDumpState.NextItem;
                    break;

                case ValueDumpState.RefineItems:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.RefineItems:");
                    if (Market.RefineItems("ValueDump", _questorManagerForm.RefineCheckBox.Checked)) return;
                    _lastExecute = DateTime.UtcNow;
                    Logging.Log("Waiting 17 seconds for minerals to appear in the item hangar");
                    _States.CurrentValueDumpState = ValueDumpState.WaitingToBack;
                    break;

                case ValueDumpState.WaitingToBack:
                    if (Logging.DebugValuedump) Logging.Log("case ValueDumpState.WaitingToBack:");
                    if (DateTime.UtcNow.Subtract(_lastExecute).TotalSeconds > 17 && _valueProcess)
                    {
                        _States.CurrentValueDumpState = _valueProcess ? ValueDumpState.Begin : ValueDumpState.Done;
                    }
                    break;
            }
        }
    }
}