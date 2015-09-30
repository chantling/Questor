// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Globalization;

namespace QuestorManager
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows.Forms;
	using System.Xml.Linq;
	using System.IO;
	using DirectEve;
	using global::Questor.Modules.Caching;
	using global::Questor.Modules.Logging;
	using global::Questor.Modules.Lookup;
	using global::Questor.Modules.States;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Actions;
	using global::Questor.Modules.Activities;
	using Actions;

	public partial class QuestorManagerUI : Form
	{
		public QuestormanagerState State { get; set; }

		private bool _changed; //false
		private bool _paused; //false
		private bool _start; //false
		private bool _lpstoreRe; //false
		private bool _requiredCom; //false

		private object _destination;
		private object _extrDestination;
		private int _jumps;
		//private readonly DirectEve _directEve;

		private object _previousDestination;
		private int _previousJumps;
		private List<DirectStation> _stations;
		private List<DirectBookmark> _bookmarks;
		private List<ListItems> List { get; set; }

		public List<ItemCache> Items { get; set; }

		//public List<ItemCache> ItemsToSellUnsorted { get; set; }

		//public List<ItemCache> ItemsToSell { get; set; }

		//public List<ItemCache> ItemsToRefine { get; set; }

		public Dictionary<int, InvType> InvTypesById { get; set; }

		private DateTime _lastPulse;

		private readonly Grab _grab;
		private readonly Drop _drop;
		private readonly Buy _buy;
		private readonly Sell _sell;
		private readonly ValueDump _valuedump;
		private readonly BuyLPI _buylpi;
		//private readonly Defense _defense;
		//private readonly Cleanup _cleanup;
		//private readonly ListItems _item;
		private DateTime _lastAction;
		private Questor.QuestorUI QuestorUI { get; set; }
		private static DateTime _nextPulse = DateTime.MinValue;
		private static DateTime _lastSessionReady = DateTime.MinValue;
		private static DateTime _lastSessionChange = DateTime.MinValue;

		private string _selectHangar = "Local Hangar";

		public QuestorManagerUI(Questor.QuestorUI questorUI)
		{
			QuestorUI = questorUI;
			Application.EnableVisualStyles();
			InitializeComponent();

			_grab = new Grab();
			_drop = new Drop();
			_buy = new Buy();
			_sell = new Sell();
			_valuedump = new ValueDump(this);
			_buylpi = new BuyLPI(this);
			List = new List<ListItems>();
			Items = new List<ItemCache>();
			//ItemsToSell = new List<ItemCache>();
			//ItemsToSellUnsorted = new List<ItemCache>();
			//ItemsToRefine = new List<ItemCache>();

			//
			// InvIgnore.xml
			//
			try
			{
				//MissionSettings.InvIgnore = XDocument.Load(Settings.Instance.Path + "\\InvIgnore.xml"); //items to ignore
			}
			catch (Exception exception)
			{
				Logging.Log("QuestorManager.Valuedump", "Unable to load [" + Settings.Instance.Path + "\\InvIgnore.xml" + "][" + exception + "]", Logging.Teal);
			}

			RefreshAvailableXMLJobs();
			Cache.Instance.DirectEve.OnFrame += OnFrame;
		}

		private void InitializeTraveler()
		{
			if (_stations == null)
			{
				_stations = Cache.Instance.DirectEve.Stations.Values.OrderBy(s => s.Name).ToList();
				_changed = true;
			}

			if (_bookmarks == null || !_bookmarks.Any())
			{
				// Dirty hack to load all category id's (needed because categoryId is lazy-loaded by the bookmarks call)
				Cache.Instance.DirectEve.Bookmarks.All(b => b.CategoryId != 0);
				_bookmarks = Cache.Instance.DirectEve.Bookmarks.OrderBy(b => b.Title).ToList();
				_changed = true;
			}
		}

		public void OnFrame(object sender, EventArgs e)
		{
			try
			{
				
				if (_nextPulse > DateTime.UtcNow)
				{
					return;
				}
				
				if(_paused) {
					return;
				}
				
				
				
				if(!QuestorUI.tabControlMain.SelectedTab.Text.ToLower().Equals("questormanager")) {
					
					_nextPulse = DateTime.UtcNow.AddSeconds(2);
					return;
				}
				
				Cache.Instance.Paused = false;
				
				if(_lastSessionChange.AddSeconds(8) > DateTime.UtcNow) {
					return;
				}
				
				Time.Instance.LastFrame = DateTime.UtcNow;

				// Only pulse state changes every 600ms
				if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < Time.Instance.QuestorPulseInStation_milliseconds)
				{
					return;
				}
				
				_lastPulse = DateTime.UtcNow;

				// Session is not ready yet, do not continue
				if (!Cache.Instance.DirectEve.Session.IsReady) {
					Logging.Log("Qm.OnFrame","Session is not ready yet.",Logging.White);
					return;
				}

				Time.Instance.LastSessionIsReady = DateTime.UtcNow;
				_lastSessionReady = DateTime.UtcNow;

				if(!Cache.Instance.DirectEve.Session.IsInSpace && !Cache.Instance.DirectEve.Session.IsInStation) {
					_lastSessionChange = DateTime.UtcNow;
				}

//				// We are not in space or station, don't do shit yet!
//				if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
//				{
//					Logging.Log("Qm.OnFrame","if (!Cache.Instance.InSpace && !Cache.Instance.InStation)",Logging.White);
//					Time.Instance.NextInSpaceorInStation = DateTime.UtcNow.AddSeconds(12);
//					Time.Instance.LastSessionChange = DateTime.UtcNow;
//					return;
//				}

				// New frame, invalidate old cache
				Cache.Instance.InvalidateCache();
				
				// Update settings (settings only load if character name changed)
				if (!Settings.Instance.DefaultSettingsLoaded)
				{
					Settings.Instance.LoadSettings();
				}
				
				if (Cache.Instance.DirectEve.Me.Name != null) {
					
					Logging.MyCharacterName = Cache.Instance.DirectEve.Me.Name;
				}

				// Check 3D rendering
				if (Cache.Instance.DirectEve.Session.IsInSpace && Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
				{
					Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;
				}

				Defense.ProcessState();
				Cleanup.ProcessState();
				

				if (Cache.Instance.InSpace && Cache.Instance.InWarp) {
					return;
				}

				InitializeTraveler();

				if (_lpstoreRe)
				{
					ResfreshLPI();
				}

				if (_requiredCom)
				{
					Required();
				}

				switch (State)
				{
					case QuestormanagerState.Idle:

						if (_start)
						{
							Logging.Log("QuestorManager", "Start", Logging.White);
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.NextAction:

						if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < 3)
							break;

						if (LstTask.Items.Count <= 0)
						{
							Logging.Log("QuestorManager", "Finish", Logging.White);
							LblStatus.Text = "Finish";
							BttnStart.Text = "Start";
							State = QuestormanagerState.Idle;
							_start = false;
							break;
						}

						if ("QuestorManager" == LstTask.Items[0].Text)
						{
							_destination = LstTask.Items[0].Tag;
							if (_destination == null || _destination.Equals(""))
								_destination = Cache.Instance.BookmarksByLabel(LstTask.Items[0].SubItems[1].Text)[0];
							Logging.Log("manager", "Destination: " + _destination, Logging.White);
							State = QuestormanagerState.Traveler;
							break;
						}

						if ("CmdLine" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.CmdLine;
							break;
						}

						if ("BuyLPI" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.BuyLPI;
							break;
						}

						if ("ValueDump" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.ValueDump;
							break;
						}

						if ("MakeShip" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.MakeShip;
							break;
						}

						if ("Drop" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.Drop;
							break;
						}

						if ("Grab" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.Grab;
							break;
						}

						if ("Buy" == LstTask.Items[0].Text || "BuyOrder" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.Buy;
							break;
						}

						if ("Sell" == LstTask.Items[0].Text || "SellOrder" == LstTask.Items[0].Text)
						{
							LblStatus.Text = LstTask.Items[0].Text + ":-:" + LstTask.Items[0].SubItems[1].Text;
							State = QuestormanagerState.Sell;
							break;
						}

						break;

					case QuestormanagerState.CmdLine:



						Logging.Log("QuestorManager", "CmdLine: " + LstTask.Items[0].SubItems[1].Text, Logging.White);
						Logging.Log("QuestorManager", "CmdLine: Error: command skipped: UseInnerspace is false", Logging.White);


						LstTask.Items.Remove(LstTask.Items[0]);
						_lastAction = DateTime.UtcNow;
						State = QuestormanagerState.NextAction;

						break;

					case QuestormanagerState.BuyLPI:

						if (_States.CurrentBuyLPIState == BuyLPIState.Idle)
						{
							_buylpi.Item = Convert.ToInt32(LstTask.Items[0].Tag);
							_buylpi.Unit = Convert.ToInt32(LstTask.Items[0].SubItems[2].Text);
							Logging.Log("QuestorManager", "BuyLPI: Begin", Logging.White);
							_States.CurrentBuyLPIState = BuyLPIState.Begin;
						}

						_buylpi.ProcessState();

						if (_States.CurrentBuyLPIState == BuyLPIState.Done)
						{
							Logging.Log("QuestorManager", "BuyLPI: Done", Logging.White);
							_States.CurrentBuyLPIState = BuyLPIState.Idle;
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.ValueDump:

						if (chkUpdateMineral.Checked)
						{
							chkUpdateMineral.Checked = false;
							_States.CurrentValueDumpState = ValueDumpState.CheckMineralPrices;
						}

						if (_States.CurrentValueDumpState == ValueDumpState.Idle)
						{
							Logging.Log("QuestorManager", "ValueDump: Begin", Logging.White);
							_States.CurrentValueDumpState = ValueDumpState.Begin;
						}

						_valuedump.ProcessState();

						if (_States.CurrentValueDumpState == ValueDumpState.Done)
						{
							Logging.Log("QuestorManager", "ValueDump: Done", Logging.White);
							_States.CurrentValueDumpState = ValueDumpState.Idle;
							ProcessItems();
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.MakeShip:

						if (Cache.Instance.ShipHangar == null) break;

						//Logging.Log("QuestorManager", "MakeShip: ShipName: [" + Cache.Instance.ActiveShip.GivenName + "]", Logging.White);
						//Logging.Log("QuestorManager", "MakeShip: ShipFind: [" + LstTask.Items[0].SubItems[1].Text + "]", Logging.White);

						if (Cache.Instance.ActiveShip.GivenName == LstTask.Items[0].SubItems[1].Text)
						{
							Logging.Log("QuestorManager", "MakeShip: Ship: [" + LstTask.Items[0].SubItems[1].Text + "] already active", Logging.White);
							LstTask.Items.Remove(LstTask.Items[0]);
							State = QuestormanagerState.NextAction;
							break;
						}

						if (DateTime.UtcNow > _lastAction)
						{
							List<DirectItem> ships = Cache.Instance.ShipHangar.Items;
							foreach (DirectItem ship in ships.Where(ship => ship.GivenName != null && ship.GivenName == LstTask.Items[0].SubItems[1].Text))
							{
								Logging.Log("QuestorManager", "MakeShip: Making [" + ship.GivenName + "] active", Logging.White);

								ship.ActivateShip();
								LstTask.Items.Remove(LstTask.Items[0]);
								_lastAction = DateTime.UtcNow;
								State = QuestormanagerState.NextAction;
								break;
							}
						}

						break;

					case QuestormanagerState.Buy:

						if (_States.CurrentBuyState == BuyState.Idle)
						{
							if (LstTask.Items[0].Text == "BuyOrder")
								_buy.useOrders = true;
							_buy.Item = Convert.ToInt32(LstTask.Items[0].Tag);
							_buy.Unit = Convert.ToInt32(LstTask.Items[0].SubItems[2].Text);
							Logging.Log("QuestorManager", "Buy: Begin", Logging.White);
							_States.CurrentBuyState = BuyState.Begin;
						}

						_buy.ProcessState();

						if (_States.CurrentBuyState == BuyState.Done)
						{
							Logging.Log("QuestorManager", "Buy: Done", Logging.White);
							_States.CurrentBuyState = BuyState.Idle;
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.Sell:

						_sell.Item = Convert.ToInt32(LstTask.Items[0].Tag);
						_sell.Unit = Convert.ToInt32(LstTask.Items[0].SubItems[2].Text);

						if (_States.CurrentSellState == SellState.Idle)
						{
							Logging.Log("QuestorManager", "Sell: Begin", Logging.White);
							_States.CurrentSellState = SellState.Begin;
						}

						_sell.ProcessState();

						if (_States.CurrentSellState == SellState.Done)
						{
							Logging.Log("QuestorManager", "Sell: Done", Logging.White);
							_States.CurrentSellState = SellState.Idle;
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.Drop:

						_drop.Item = Convert.ToInt32(LstTask.Items[0].Tag);
						_drop.Unit = Convert.ToInt32(LstTask.Items[0].SubItems[2].Text);
						_drop.DestinationHangarName = LstTask.Items[0].SubItems[3].Text;

						if (_States.CurrentDropState == DropState.Idle)
						{
							Logging.Log("QuestorManager", "Drop: Begin", Logging.White);
							_States.CurrentDropState = DropState.Begin;
						}

						_drop.ProcessState();

						if (_States.CurrentDropState == DropState.Done)
						{
							Logging.Log("QuestorManager", "Drop: Done", Logging.White);
							_States.CurrentDropState = DropState.Idle;
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.Grab:

						_grab.Item = Convert.ToInt32(LstTask.Items[0].Tag);
						_grab.Unit = Convert.ToInt32(LstTask.Items[0].SubItems[2].Text);
						_grab.Hangar = LstTask.Items[0].SubItems[3].Text;

						if (_States.CurrentGrabState == GrabState.Idle)
						{
							Logging.Log("QuestorManager", "Grab: Begin", Logging.White);
							_States.CurrentGrabState = GrabState.Begin;
						}

						_grab.ProcessState();

						if (_States.CurrentGrabState == GrabState.Done)
						{
							Logging.Log("QuestorManager", "Grab: Done", Logging.White);
							_States.CurrentGrabState = GrabState.Idle;
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						break;

					case QuestormanagerState.Traveler:

						// We are warping
						if (Cache.Instance.DirectEve.Session.IsInSpace && Cache.Instance.ActiveShip.Entity != null && Cache.Instance.ActiveShip.Entity.IsWarping)
							return;

						TravelerDestination travelerDestination = Traveler.Destination;
						if (_destination == null)
						{
							travelerDestination = null;
						}

						if (_destination is DirectBookmark)
						{
							if (!(travelerDestination is BookmarkDestination) || (travelerDestination as BookmarkDestination).BookmarkId != (_destination as DirectBookmark).BookmarkId)
							{
								travelerDestination = new BookmarkDestination(_destination as DirectBookmark);
							}
						}

						if (_destination is DirectSolarSystem)
						{
							if (!(travelerDestination is SolarSystemDestination) || (travelerDestination as SolarSystemDestination).SolarSystemId != (_destination as DirectSolarSystem).Id)
							{
								travelerDestination = new SolarSystemDestination((_destination as DirectSolarSystem).Id);
							}
						}

						if (_destination is DirectStation)
						{
							if (!(travelerDestination is StationDestination) || (travelerDestination as StationDestination).StationId != (_destination as DirectStation).Id)
							{
								travelerDestination = new StationDestination((_destination as DirectStation).Id);
							}
						}

						// Check to see if destination changed, since changing it will set the traveler to Idle
						if (Traveler.Destination != travelerDestination)
						{
							Traveler.Destination = travelerDestination;

						}

						// Record number of jumps
						_jumps = Cache.Instance.DirectEve.Navigation.GetDestinationPath().Count;

						Traveler.ProcessState();



						// Arrived at destination
						if (_destination != null && _States.CurrentTravelerState == TravelerState.AtDestination)
						{
							Logging.Log("QuestorManager", "Arrived at destination", Logging.White);

							Traveler.Destination = null;
							_destination = null;
							LstTask.Items.Remove(LstTask.Items[0]);
							_lastAction = DateTime.UtcNow;
							State = QuestormanagerState.NextAction;
						}

						// An error occurred, reset traveler
						if (_States.CurrentTravelerState == TravelerState.Error)
						{
							if (Traveler.Destination != null)
							{
								Logging.Log("QuestorManager", "Stopped traveling, QuestorManager threw an error...", Logging.White);
							}

							_destination = null;
							Traveler.Destination = null;
							_start = false;
							State = QuestormanagerState.Idle;
						}
						break;
				}
			}
			catch (Exception ex)
			{
				Logging.Log("QuestorManager", "Exception, [" + ex + "]", Logging.White);
				return;
			}
		}

		private ListViewItem[] Filter<T>(IEnumerable<string> search, IEnumerable<T> list, Func<T, string> getTitle, Func<T, string> getType)
		{
			search = search.ToList();
			if (list == null)
			{
				return new ListViewItem[0];
			}

			List<ListViewItem> result = new List<ListViewItem>();
			foreach (T item in list)
			{
				string name = getTitle(item);
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				bool found = search != null && search.All(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) > -1);
				if (!found)
				{
					continue;
				}

				ListViewItem listViewItem = new ListViewItem(name);
				listViewItem.SubItems.Add(getType(item));
				listViewItem.Tag = item;
				result.Add(listViewItem);
			}

			return result.ToArray();
		}

		private void UpdateSearchResultsTick(object sender, EventArgs e)
		{
			if (_previousDestination != _destination || _jumps != _previousJumps)
			{
				_previousDestination = _destination;
				_previousJumps = _jumps;

				string name = string.Empty;
				if (_destination is DirectBookmark)
					name = ((DirectBookmark)_destination).Title;
				if (_destination is DirectRegion)
					name = ((DirectRegion)_destination).Name;
				if (_destination is DirectConstellation)
					name = ((DirectConstellation)_destination).Name;
				if (_destination is DirectSolarSystem)
					name = ((DirectSolarSystem)_destination).Name;
				if (_destination is DirectStation)
					name = ((DirectStation)_destination).Name;

				if (!string.IsNullOrEmpty(name))
				{
					name = @"Traveling to " + name + " (" + _jumps + " jumps)";
				}

				LblStatus.Text = name;
			}

			if (!_changed)
			{
				return;
			}

			_changed = false;

			try
			{
				if ((_bookmarks != null && _bookmarks.Any())
				    || (Cache.Instance.SolarSystems != null && Cache.Instance.SolarSystems.Any())
				    || (_stations != null && _stations.Any()))
				{
					string[] search = SearchTextBox.Text.Split(' ');
					SearchResults.BeginUpdate();
					SearchResults.Items.Clear();

					if (_bookmarks != null && _bookmarks.Any())
					{
						SearchResults.Items.AddRange(Filter(search, _bookmarks, b => b.Title, b => "Bookmark (" + ((CategoryID)b.CategoryId) + ")"));
					}

					if (Cache.Instance.SolarSystems != null && Cache.Instance.SolarSystems.Any())
					{
						SearchResults.Items.AddRange(Filter(search, Cache.Instance.SolarSystems, s => s.Name, b => "Solar System"));
					}

					if (_stations != null && _stations.Any())
					{
						SearchResults.Items.AddRange(Filter(search, _stations, s => s.Name, b => "Station"));
					}

					// Automatically select the only item
					if (SearchResults.Items.Count == 1)
					{
						SearchResults.Items[0].Selected = true;
					}
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.UpdateSearchResultsTick", "Exception [" + exception + "]", Logging.Debug);
			}
			finally
			{
				SearchResults.EndUpdate();
			}
		}

		private void BttnStartClick(object sender, EventArgs e)
		{
			if (BttnStart.Text == "Start")
			{
				BttnStart.Text = "Stop";
				State = QuestormanagerState.Idle;
				_start = true;
			}
			else
			{
				BttnStart.Text = "Start";
				State = QuestormanagerState.Idle;
				_States.CurrentBuyState = BuyState.Idle;
				_States.CurrentDropState = DropState.Idle;
				_States.CurrentGrabState = GrabState.Idle;
				_States.CurrentSellState = SellState.Idle;
				_States.CurrentValueDumpState = ValueDumpState.Idle;
				_States.CurrentBuyLPIState = BuyLPIState.Idle;
				_start = false;
			}
		}

		private void SearchTextBoxTextChanged(object sender, EventArgs e)
		{
			_changed = true;
		}

		private void BttnAddTraveler_Click(object sender, EventArgs e)
		{
			try
			{
				//if (SearchResults != null && SearchResults.Items.Count > 0)
				//{

				//if (SearchResults != null && SearchResults.Items[0] != null)
				//{
				if (SearchResults.SelectedItems[0] != null)
				{
					if (SearchResults.SelectedItems.Count > 0)
					{
						ListViewItem listItem = new ListViewItem("QuestorManager");
						listItem.SubItems.Add(SearchResults.SelectedItems[0].Text);
						listItem.Tag = SearchResults.SelectedItems[0].Tag;
						listItem.SubItems.Add(" ");
						listItem.SubItems.Add(" ");
						LstTask.Items.Add(listItem);
					}
					else
					{
						Logging.Log("QuestorManager", "BttnAddTraveler_Click: SearchResults.SelectedItems is 0", Logging.Debug);
					}
				}
				else
				{
					Logging.Log("QuestorManager", "BttnAddTraveler_Click: SearchResults.SelectedItems[0] is null", Logging.Debug);
				}
				//}
			}
			catch (Exception exception)
			{
				Logging.Log("QuestorManager", "Exception [" + exception + "]", Logging.Debug);
				Logging.Log("QuestorManager", "Is this exception timing based?", Logging.Debug);
			}
		}

		private void BttnTaskForItemClick1(object sender, EventArgs e)
		{
			if (cmbMode.Text == "Select Mode")
			{
				return;
			}

			foreach (ListViewItem item in LstItems.CheckedItems)
			{
				ListViewItem listItem = new ListViewItem(cmbMode.Text);
				listItem.SubItems.Add(item.Text);
				listItem.Tag = item.SubItems[1].Text;
				listItem.SubItems.Add(txtUnit.Text);
				listItem.SubItems.Add(_selectHangar);
				LstTask.Items.Add(listItem);
			}
		}

		private void MoveListViewItem(ref ListView lv, bool moveUp)
		{
			string cache;

			int selIdx = lv.SelectedItems[0].Index;
			if (moveUp)
			{
				// ignore move up of row(0)
				if (selIdx == 0)
				{
					return;
				}

				if (_start)
				{
					if (selIdx == 1)
					{
						return;
					}
				}

				// move the sub items for the previous row
				// to cache to make room for the selected row
				for (int i = 0; i < lv.Items[selIdx].SubItems.Count; i++)
				{
					cache = lv.Items[selIdx - 1].SubItems[i].Text;
					lv.Items[selIdx - 1].SubItems[i].Text = lv.Items[selIdx].SubItems[i].Text;
					lv.Items[selIdx].SubItems[i].Text = cache;
				}
				object cache1 = lv.Items[selIdx - 1].Tag;
				lv.Items[selIdx - 1].Tag = lv.Items[selIdx].Tag;
				lv.Items[selIdx].Tag = cache1;

				lv.Items[selIdx - 1].Selected = true;
				lv.Refresh();
				lv.Focus();
			}
			else
			{
				// ignore move down of last item
				if (selIdx == lv.Items.Count - 1)
				{
					return;
				}

				if (_start)
				{
					if (selIdx == 0)
					{
						return;
					}
				}

				// move the sub items for the next row
				// to cache so we can move the selected row down
				for (int i = 0; i < lv.Items[selIdx].SubItems.Count; i++)
				{
					cache = lv.Items[selIdx + 1].SubItems[i].Text;
					lv.Items[selIdx + 1].SubItems[i].Text = lv.Items[selIdx].SubItems[i].Text;
					lv.Items[selIdx].SubItems[i].Text = cache;
				}
				object cache1 = lv.Items[selIdx + 1].Tag;
				lv.Items[selIdx + 1].Tag = lv.Items[selIdx].Tag;
				lv.Items[selIdx].Tag = cache1;

				lv.Items[selIdx + 1].Selected = true;
				lv.Refresh();
				lv.Focus();
			}
		}

		private void BttnUpClick(object sender, EventArgs e)
		{
			MoveListViewItem(ref LstTask, true);
		}

		private void BttnDownClick(object sender, EventArgs e)
		{
			MoveListViewItem(ref LstTask, false);
		}

		private void BttnDeleteClick(object sender, EventArgs e)
		{
			if (_start)
			{
				if (LstTask.SelectedItems[0].Index == 0)
				{
					return;
				}
			}

			while (LstTask.SelectedItems.Count > 0)
			{
				LstTask.Items.Remove(LstTask.SelectedItems[0]);
			}
		}

		private void TxtSearchItemsTextChanged(object sender, EventArgs e)
		{
			LstItems.Items.Clear();

			if (txtSearchItems.Text.Length > 4)
			{
				string[] search = txtSearchItems.Text.Split(' ');
				foreach (ListItems item in List)
				{
					string name = item.Name;
					if (string.IsNullOrEmpty(name))
					{
						continue;
					}

					bool found = search.All(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) > -1);
					if (!found)
					{
						continue;
					}

					ListViewItem listItem1 = new ListViewItem(item.Name);
					listItem1.SubItems.Add(Convert.ToString(item.Id));
					LstItems.Items.Add(listItem1);
				}
			}
		}

		private void BttnTaskAllItemsClick(object sender, EventArgs e)
		{
			if (cmbAllMode.Text == "Select Mode")
			{
				return;
			}

			ListViewItem listItem = new ListViewItem(cmbAllMode.Text);
			listItem.SubItems.Add("All items");
			listItem.Tag = 00;
			listItem.SubItems.Add("00");
			listItem.SubItems.Add(_selectHangar);
			LstTask.Items.Add(listItem);
		}

		private void BttnTaskMakeShipClick(object sender, EventArgs e)
		{
			if (txtNameShip.Text == "")
			{
				return;
			}

			ListViewItem listItem = new ListViewItem("MakeShip");
			listItem.SubItems.Add(txtNameShip.Text);
			listItem.SubItems.Add(" ");
			listItem.SubItems.Add(" ");
			LstTask.Items.Add(listItem);
		}

		private void ChkPauseCheckedChanged(object sender, EventArgs e)
		{
			if (chkPause.Checked)
			{
				_paused = true;
			}

			if (chkPause.Checked == false)
			{
				_paused = false;
			}
		}

		private void RbttnLocalCheckedChanged(object sender, EventArgs e)
		{
			if (rbttnLocal.Checked)
			{
				_selectHangar = rbttnLocal.Text;
			}
		}

		private void RbttnShipCheckedChanged(object sender, EventArgs e)
		{
			if (rbttnShip.Checked)
			{
				_selectHangar = rbttnShip.Text;
			}
		}

		private void RbttnCorpCheckedChanged(object sender, EventArgs e)
		{
			if (rbttnCorp.Checked)
			{
				txtNameCorp.Enabled = true;
				_selectHangar = txtNameCorp.Text;
			}
			else if (rbttnCorp.Checked == false)
			{
				txtNameCorp.Enabled = false;
			}
		}

		private void TxtNameCorpTextChanged(object sender, EventArgs e)
		{
			_selectHangar = txtNameCorp.Text;
		}

		private void ProcessItems()
		{
			lvItems.Items.Clear();
			foreach (ItemCache item in Items.OrderByDescending(i => i.Value * i.Quantity))
			{
				ListViewItem listItem = new ListViewItem(item.Name);
				listItem.SubItems.Add(string.Format("{0:#,##0}", item.Quantity));
				listItem.SubItems.Add(string.Format("{0:#,##0}", item.QuantitySold));
				listItem.SubItems.Add(string.Format("{0:#,##0}", item.Value));
				listItem.SubItems.Add(string.Format("{0:#,##0}", item.StationBuy));

				if (cbxSell.Checked)
				{
					listItem.SubItems.Add(string.Format("{0:#,##0}", item.StationBuy * item.QuantitySold));
				}
				else
				{
					listItem.SubItems.Add(string.Format("{0:#,##0}", item.Value * item.Quantity));
				}

				lvItems.Items.Add(listItem);
			}

			if (cbxSell.Checked)
			{
				tbTotalMedian.Text = string.Format("{0:#,##0}", Items.Sum(i => i.Value * i.QuantitySold));
				tbTotalSold.Text = string.Format("{0:#,##0}", Items.Sum(i => i.StationBuy * i.QuantitySold));
			}
			else
			{
				tbTotalMedian.Text = string.Format("{0:#,##0}", Items.Sum(i => i.Value * i.Quantity));
				tbTotalSold.Text = "";
			}
		}

		private void FrmMainFormClosed(object sender, FormClosedEventArgs e)
		{
			try
			{
				if (Logging.DebugUI) Logging.Log("QuestorUI", "frmMainFormClosed", Logging.White);
				Cleanup.SignalToQuitQuestor = true;
			}
			catch (Exception ex)
			{
				Logging.Log("QuestorManagerUI", "Exception [" + ex + "]", Logging.Debug);
			}
		}

		private void UpdateMineralPricesButton_Click(object sender, EventArgs e)
		{
			_States.CurrentValueDumpState = ValueDumpState.CheckMineralPrices;
		}

		private void LvItemsColumnClick(object sender, ColumnClickEventArgs e)
		{
			ListViewColumnSort oCompare = new ListViewColumnSort();

			if (lvItems.Sorting == SortOrder.Ascending)
			{
				oCompare.Sorting = SortOrder.Descending;
			}
			else
			{
				oCompare.Sorting = SortOrder.Ascending;
			}

			lvItems.Sorting = oCompare.Sorting;
			oCompare.ColumnIndex = e.Column;

			switch (e.Column)
			{
				case 1:
					oCompare.CompararPor = ListViewColumnSort.TipoCompare.Cadena;
					break;
				case 2:
					oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
					break;
				case 3:
					oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
					break;
				case 4:
					oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
					break;
				case 5:
					oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
					break;
				case 6:
					oCompare.CompararPor = ListViewColumnSort.TipoCompare.Numero;
					break;
			}

			lvItems.ListViewItemSorter = oCompare;
		}

		private void BttnTaskValueDumpClick(object sender, EventArgs e)
		{
			ListViewItem listItem = new ListViewItem("ValueDump");
			listItem.SubItems.Add("All Items");
			listItem.SubItems.Add(" ");
			listItem.SubItems.Add(" ");
			LstTask.Items.Add(listItem);
		}

		private void BttnSaveTaskClick(object sender, EventArgs e)
		{
			if (cmbXML.Text == "Select Jobs")
			{
				MessageBox.Show("Write name to save");
				return;
			}

			string fic = Path.Combine(Settings.Instance.Path, cmbXML.Text + ".jobs");
			string strXml = "<Jobs>";

			for (int o = 0; o < LstTask.Items.Count; o++)
			{
				strXml += "<Job typeJob='" + LstTask.Items[o].SubItems[0].Text + "' Name='" + LstTask.Items[o].SubItems[1].Text + "' Unit='" + LstTask.Items[o].SubItems[2].Text + "' Hangar='" + LstTask.Items[o].SubItems[3].Text + "' Tag='" + LstTask.Items[o].Tag + "' />";
			}

			strXml += "</Jobs>";

			XElement xml = XElement.Parse(strXml);
			XDocument fileXml = new XDocument(xml);
			fileXml.Save(fic);

			RefreshAvailableXMLJobs();
		}

		private void RefreshAvailableXMLJobs()
		{
			cmbXML.Items.Clear();

			DirectoryInfo o = new System.IO.DirectoryInfo(Settings.Instance.Path);

			FileInfo[] myfiles = o.GetFiles("*.jobs");
			for (int y = 0; y <= myfiles.Length - 1; y++)
			{
				string[] file = myfiles[y].Name.Split('.');
				cmbXML.Items.Add(file[0]);
			}
		}

		private void ExtractTraveler(string nameDestination)
		{
			if (_extrDestination == null)
			{
				foreach (DirectStation item in _stations)
				{
					if (nameDestination == item.Name)
					{
						_extrDestination = item;
					}
				}
			}
			else if (_extrDestination == null)
			{
				foreach (DirectSolarSystem item in Cache.Instance.SolarSystems)
				{
					if (nameDestination == item.Name)
					{
						_extrDestination = item;
					}
				}
			}
			else if (_extrDestination == null)
			{
				foreach (DirectBookmark item in _bookmarks)
				{
					if (nameDestination == item.Title)
					{
						_extrDestination = item;
					}
				}
			}
		}

		private void ReadXML(string fic)
		{
			XElement xml = XDocument.Load(fic).Root;

			LstTask.Items.Clear();
			if (xml != null)
			{
				foreach (XElement job in xml.Elements("Job"))
				{
					ListViewItem listItem = new ListViewItem((string)job.Attribute("typeJob"));
					listItem.SubItems.Add((string)job.Attribute("Name"));
					listItem.SubItems.Add((string)job.Attribute("Unit"));
					listItem.SubItems.Add((string)job.Attribute("Hangar"));
					if (((string)job.Attribute("typeJob")) == "QuestorManager")
					{
						ExtractTraveler(((string)job.Attribute("Name")));
						listItem.Tag = _extrDestination;
						_extrDestination = null;
					}
					else
					{
						listItem.Tag = (string)job.Attribute("Tag");
					}

					LstTask.Items.Add(listItem);
				}
			}
		}

		private int LoadSavedTaskList(string[] args)
		{
			if (State == QuestormanagerState.Idle) // if we are not in the idle state then we are already processing a job!
			{
				//Logging.Log("QuestorManager", "LoadSavedTaskList: Args [" + args.Length + "][" + args[0] + "][" + args[1] + "]", Logging.White);
				if (args.Length != 2)
				{
					Logging.Log("QuestorManager", "LoadSavedTaskList [SavedJobFile] - Reads the Saved Task List specified and processes the jobs", Logging.White);
					return -1;
				}

				string savedjobtoload = Path.Combine(Settings.Instance.Path, args[1] + ".jobs");
				if (File.Exists(savedjobtoload))
				{
					try
					{
						ReadXML(savedjobtoload);
					}

					//catch
					//{
					//
					//}
					finally
					{
					}
					return 0;
				}

				Logging.Log("QuestorManager", "LoadSavedTaskList: File Job file [" + savedjobtoload + "] does not exist", Logging.Orange);
				return -1;
			}
			return -1;
		}

		private int StartProcessing(string[] args)
		{
			if (State == QuestormanagerState.Idle) // if we are not in the idle state then we are already processing a job!
			{
				if (args.Length != 1)
				{
					Logging.Log("QuestorManager",
					            "StartProcessing - Starts Processing any already loaded task items",
					            Logging.White);
					return -1;
				}
				try
				{
					_start = true;
					State = QuestormanagerState.Idle;
				}
				//catch
				//{
				//
				//}
				finally
				{
				}
			}

			Logging.Log("QuestorUI", "QuestorState is now: CloseQuestor ", Logging.White);
			return 0;
		}

		private void CmbXMLSelectedIndexChanged(object sender, EventArgs e)
		{
			string fic = Path.Combine(Settings.Instance.Path, cmbXML.Text + ".jobs");
			ReadXML(fic);
		}

		public void ResfreshLPI()
		{
			_lpstoreRe = false;
			DirectLoyaltyPointStoreWindow lpstore = Cache.Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
			if (lpstore == null)
			{
				Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
				Statistics.LogWindowActionToWindowLog("LPStore", "LPStore Opened");
				return;
			}

			lstbuyLPI.Items.Clear();

			string[] search = txtSearchLPI.Text.Split(' ');
			foreach (DirectLoyaltyPointOffer offer in lpstore.Offers)
			{
				string name = offer.TypeName;
				if (string.IsNullOrEmpty(name))
				{
					continue;
				}

				bool found = search.All(t => name.IndexOf(t, StringComparison.OrdinalIgnoreCase) > -1);
				if (!found)
				{
					continue;
				}

				ListViewItem listItem = new ListViewItem(offer.TypeName);
				listItem.SubItems.Add(Convert.ToString(offer.TypeId));
				lstbuyLPI.Items.Add(listItem);
			}
		}

		public void Required()
		{
			DirectLoyaltyPointStoreWindow lpstore = Cache.Instance.Windows.OfType<DirectLoyaltyPointStoreWindow>().FirstOrDefault();
			XDocument invTypes = XDocument.Load(Path.GetDirectoryName(Settings.Instance.Path) + "\\InvTypes.xml");
			if (invTypes.Root != null)
			{
				IEnumerable<XElement> invType = invTypes.Root.Elements("invtype").ToList();

				if (lpstore == null)
				{
					Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenLpstore);
					Statistics.LogWindowActionToWindowLog("LPStore", "LPStore Opened");
					return;
				}

				foreach (DirectLoyaltyPointOffer offer in lpstore.Offers)
				{
					if (offer.TypeName == lstbuyLPI.SelectedItems[0].Text)
					{
						double totalIsk = 0;
						lstItemsRequiered.Items.Clear();

						if (offer.RequiredItems.Count > 0)
						{
							foreach (DirectLoyaltyPointOfferRequiredItem requiredItem in offer.RequiredItems)
							{
								foreach (XElement item in invType)
								{
									if ((string)item.Attribute("name") == requiredItem.TypeName)
									{
										double medianbuy = (double?)item.Attribute("medianbuy") ?? 0;
										ListViewItem listItemRequired = new ListViewItem(requiredItem.TypeName);
										listItemRequired.SubItems.Add(Convert.ToString(requiredItem.Quantity));
										listItemRequired.SubItems.Add(string.Format("{0:#,#0.00}", medianbuy));
										lstItemsRequiered.Items.Add(listItemRequired);
										totalIsk = totalIsk + (Convert.ToDouble(requiredItem.Quantity) * medianbuy);
									}
								}
							}
						}

						lblitemisk.Text = string.Format("{0:#,#0.00}", totalIsk);
						totalIsk = totalIsk + Convert.ToDouble(offer.IskCost);
						lbliskLPI.Text = string.Format("{0:#,#0.00}", offer.IskCost);
						lblTotal.Text = string.Format("{0:#,#0.00}", totalIsk);
						lblLP.Text = string.Format("{0:#,#}", offer.LoyaltyPointCost);
					}
				}
			}
			_requiredCom = false;
		}

		private void BttnRefreshLPIClick(object sender, EventArgs e)
		{
			_lpstoreRe = true;
		}

		private void LstbuyLPISelectedIndexChanged(object sender, EventArgs e)
		{
			_requiredCom = true;
		}

		private void BttnTaskLPIClick(object sender, EventArgs e)
		{
			foreach (ListViewItem item in lstbuyLPI.CheckedItems)
			{
				ListViewItem listItem = new ListViewItem("BuyLPI");
				listItem.SubItems.Add(item.Text);
				listItem.Tag = item.SubItems[1].Text;
				listItem.SubItems.Add(txtUnitLPI.Text);
				listItem.SubItems.Add(" ");
				LstTask.Items.Add(listItem);
			}
		}

		private void BttnTaskLineCmdClick(object sender, EventArgs e)
		{
			ListViewItem listItem = new ListViewItem("CmdLine");
			listItem.SubItems.Add(txtCmdLine.Text);
			listItem.SubItems.Add(" ");
			listItem.SubItems.Add(" ");
			LstTask.Items.Add(listItem);
		}

		private void TxtSearchLPIKeyPress(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)13)
			{
				_lpstoreRe = true;
			}
		}

		private void SearchResults_SelectedIndexChanged(object sender, EventArgs e)
		{
		}

		private void button1_Click(object sender, EventArgs e)
		{

		}

		private void button4_Click(object sender, EventArgs e)
		{
			String content = Clipboard.GetText();
			int count = 0;

			foreach (string line in content.Split('\n'))
			{
				string[] info = line.Split(',');
				if (info.Count() == 4)
				{
					int surplus;
					if (Int32.TryParse(info[3].Replace(".", ""), out surplus))
					{
						if (surplus < 0)
						{
							surplus *= -1;

							foreach (ListItems item in List)
							{
								string name = item.Name;
								if (string.IsNullOrEmpty(name))
								{
									continue;
								}

								if (name.ToLower().Equals(info[0].ToLower()))
								{
									ListViewItem listItem = new ListViewItem("BuyOrder");
									listItem.SubItems.Add(item.Name);
									listItem.Tag = item.Id.ToString(CultureInfo.InvariantCulture);
									listItem.SubItems.Add(surplus.ToString(CultureInfo.InvariantCulture));
									LstTask.Items.Add(listItem);
									count++;
								}
							}
						}
					}
				}
			}
			System.Windows.Forms.MessageBox.Show("Added " + count + " Tasks to your list.");
		}

		private void StartQuestor_Click(object sender, EventArgs e)
		{
			string questorPath = Path.Combine(Settings.Instance.Path, "Questor.exe");
			if (File.Exists(questorPath))
			{
				Logging.Log("QuestorUI", "Launching [ dotnet QuestorManager QuestorManager ] - fix me", Logging.White);
			}
			else
			{
				Logging.Log("QuestorUI", "Unable to launch Questor from [" + questorPath + "] file not found", Logging.Orange);
			}
		}
	}
}