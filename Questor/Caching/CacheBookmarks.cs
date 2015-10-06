// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias Ut;

namespace Questor.Modules.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using System.Threading;
	using global::Questor.Modules.Actions;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Combat;
	using global::Questor.Modules.Lookup;
	using global::Questor.Modules.States;
	using global::Questor.Modules.Logging;
	using DirectEve;
	using global::Questor.Storylines;
	using Ut::EVE;
	using Ut;
	using Ut::WCF;
	
	public partial class Cache
	{
		
		
	
		public List<DirectBookmark> _allBookmarks;

		public List<DirectBookmark> AllBookmarks
		{
			get
			{
				try
				{
					if (Cache.Instance._allBookmarks == null || !Cache.Instance._allBookmarks.Any())
					{
						if (DateTime.UtcNow > Time.Instance.NextBookmarkAction)
						{
							Time.Instance.NextBookmarkAction = DateTime.UtcNow.AddMilliseconds(200);
							if (DirectEve.Bookmarks.Any())
							{
								_allBookmarks = Cache.Instance.DirectEve.Bookmarks;
								return _allBookmarks;
							}

							return null; //there are no bookmarks to list...
						}

						return null; //new List<DirectBookmark>(); //there are no bookmarks to list...
					}

					return Cache.Instance._allBookmarks;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.allBookmarks", "Exception [" + exception + "]", Logging.Debug);
					return new List<DirectBookmark>();;
				}
			}
			set
			{
				_allBookmarks = value;
			}
		}

		/// <summary>
		///   Return a bookmark by id
		/// </summary>
		/// <param name = "bookmarkId"></param>
		/// <returns></returns>
		public DirectBookmark BookmarkById(long bookmarkId)
		{
			try
			{
				if (Cache.Instance.AllBookmarks != null && Cache.Instance.AllBookmarks.Any())
				{
					return Cache.Instance.AllBookmarks.FirstOrDefault(b => b.BookmarkId == bookmarkId);
				}

				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.BookmarkById", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}
		
		public List<DirectBookmark> BookmarksByLabel(string label)
		{
			try
			{
				// Does not seems to refresh the Corporate Bookmark list so it's having troubles to find Corporate Bookmarks
				if (Cache.Instance.AllBookmarks != null && Cache.Instance.AllBookmarks.Any())
				{
					return Cache.Instance.AllBookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().StartsWith(label.ToLower())).OrderBy(f => f.LocationId).ThenBy(i => Cache.Instance.DistanceFromMe(i.X ?? 0, i.Y ?? 0, i.Z ?? 0)).ToList();
				}

				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.BookmarkById", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		
		public List<DirectBookmark> BookmarksThatContain(string label)
		{
			try
			{
				if (Cache.Instance.AllBookmarks != null && Cache.Instance.AllBookmarks.Any())
				{
					return Cache.Instance.AllBookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().Contains(label.ToLower())).OrderBy(f => f.LocationId).ToList();
				}

				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.BookmarksThatContain", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}
		
		public void CreateBookmark(string label)
		{
			try
			{
				if (Cache.Instance.AfterMissionSalvageBookmarks.Count() < 100)
				{
					if (Salvage.CreateSalvageBookmarksIn.ToLower() == "corp".ToLower())
					{
						DirectBookmarkFolder folder = Cache.Instance.DirectEve.BookmarkFolders.FirstOrDefault(i => i.Name == Settings.Instance.BookmarkFolder);
						if (folder != null)
						{
							Cache.Instance.DirectEve.CorpBookmarkCurrentLocation(label, "", folder.Id);
						}
						else
						{
							Cache.Instance.DirectEve.CorpBookmarkCurrentLocation(label, "", null);
						}
					}
					else
					{
						DirectBookmarkFolder folder = Cache.Instance.DirectEve.BookmarkFolders.FirstOrDefault(i => i.Name == Settings.Instance.BookmarkFolder);
						if (folder != null)
						{
							Cache.Instance.DirectEve.BookmarkCurrentLocation(label, "", folder.Id);
						}
						else
						{
							Cache.Instance.DirectEve.BookmarkCurrentLocation(label, "", null);
						}
					}
				}
				else
				{
					Logging.Log("CreateBookmark", "We already have over 100 AfterMissionSalvage bookmarks: their must be a issue processing or deleting bookmarks. No additional bookmarks will be created until the number of salvage bookmarks drops below 100.", Logging.Orange);
				}

				return;
			}
			catch (Exception ex)
			{
				Logging.Log("CreateBookmark", "Exception [" + ex + "]", Logging.Debug);
				return;
			}
		}
		
		private IEnumerable<DirectBookmark> ListOfUndockBookmarks;

		internal static DirectBookmark _undockBookmarkInLocal;
		public DirectBookmark UndockBookmark
		{
			get
			{
				try
				{
					if (_undockBookmarkInLocal == null)
					{
						if (ListOfUndockBookmarks == null)
						{
							if (Settings.Instance.UndockBookmarkPrefix != "")
							{
								ListOfUndockBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix);
							}
						}
						if (ListOfUndockBookmarks != null && ListOfUndockBookmarks.Any())
						{
							ListOfUndockBookmarks = ListOfUndockBookmarks.Where(i => i.LocationId == Cache.Instance.DirectEve.Session.LocationId).ToList();
							_undockBookmarkInLocal = ListOfUndockBookmarks.OrderBy(i => Cache.Instance.DistanceFromMe(i.X ?? 0, i.Y ?? 0, i.Z ?? 0)).FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distances.NextPocketDistance);
							if (_undockBookmarkInLocal != null)
							{
								return _undockBookmarkInLocal;
							}

							return null;
						}

						return null;
					}

					return _undockBookmarkInLocal;
				}
				catch (Exception exception)
				{
					Logging.Log("UndockBookmark", "[" + exception + "]", Logging.Teal);
					return null;
				}
			}
			internal set
			{
				_undockBookmarkInLocal = value;
			}

		}
		
		public IEnumerable<DirectBookmark> SafeSpotBookmarks
		{
			get
			{
				try
				{

					if (_safeSpotBookmarks == null)
					{
						_safeSpotBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.SafeSpotBookmarkPrefix).ToList();
					}

					if (_safeSpotBookmarks != null && _safeSpotBookmarks.Any())
					{
						return _safeSpotBookmarks;
					}

					return new List<DirectBookmark>();
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.SafeSpotBookmarks", "Exception [" + exception + "]", Logging.Debug);
				}

				return new List<DirectBookmark>();
			}
		}

		public IEnumerable<DirectBookmark> AfterMissionSalvageBookmarks
		{
			get
			{
				try
				{
					string _bookmarkprefix = Settings.Instance.BookmarkPrefix;

					if (_States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
					{
						return Cache.Instance.BookmarksByLabel(_bookmarkprefix + " ").Where(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(AgedDate) < 0).ToList();
					}

					if (Cache.Instance.BookmarksByLabel(_bookmarkprefix + " ") != null)
					{
						return Cache.Instance.BookmarksByLabel(_bookmarkprefix + " ").ToList();
					}

					return new List<DirectBookmark>();
				}
				catch (Exception ex)
				{
					Logging.Log("AfterMissionSalvageBookmarks", "Exception [" + ex + "]", Logging.Debug);
					return new List<DirectBookmark>();
				}
			}
		}

		//Represents date when bookmarks are eligible for salvage. This should not be confused with when the bookmarks are too old to salvage.
		public DateTime AgedDate
		{
			get
			{
				try
				{
					return DateTime.UtcNow.AddMinutes(-Salvage.AgeofBookmarksForSalvageBehavior);
				}
				catch (Exception ex)
				{
					Logging.Log("AgedDate", "Exception [" + ex + "]", Logging.Debug);
					return DateTime.UtcNow.AddMinutes(-45);
				}
			}
		}

		public DirectBookmark GetSalvagingBookmark
		{
			get
			{
				try
				{
					if (Cache.Instance.AllBookmarks != null && Cache.Instance.AllBookmarks.Any())
					{
						List<DirectBookmark> _SalvagingBookmarks;
						DirectBookmark _SalvagingBookmark;
						if (Salvage.FirstSalvageBookmarksInSystem)
						{
							Logging.Log("CombatMissionsBehavior.BeginAftermissionSalvaging", "Salvaging at first bookmark from system", Logging.White);
							_SalvagingBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
							if (_SalvagingBookmarks != null && _SalvagingBookmarks.Any())
							{
								_SalvagingBookmark = _SalvagingBookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);
								return _SalvagingBookmark;
							}

							return null;
						}

						Logging.Log("CombatMissionsBehavior.BeginAftermissionSalvaging", "Salvaging at first oldest bookmarks", Logging.White);
						_SalvagingBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
						if (_SalvagingBookmarks != null && _SalvagingBookmarks.Any())
						{
							_SalvagingBookmark = _SalvagingBookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault();
							return _SalvagingBookmark;
						}

						return null;
					}

					return null;
				}
				catch (Exception ex)
				{
					Logging.Log("GetSalvagingBookmark", "Exception [" + ex + "]", Logging.Debug);
					return null;
				}
			}
		}

		public DirectBookmark GetTravelBookmark
		{
			get
			{
				try
				{
					DirectBookmark bm = Cache.Instance.BookmarksByLabel(Settings.Instance.TravelToBookmarkPrefix).OrderByDescending(b => b.CreatedOn).FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId) ??
						Cache.Instance.BookmarksByLabel(Settings.Instance.TravelToBookmarkPrefix).OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
						Cache.Instance.BookmarksByLabel("Jita").OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
						Cache.Instance.BookmarksByLabel("Rens").OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
						Cache.Instance.BookmarksByLabel("Amarr").OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
						Cache.Instance.BookmarksByLabel("Dodixie").OrderByDescending(b => b.CreatedOn).FirstOrDefault();

					if (bm != null)
					{
						Logging.Log("CombatMissionsBehavior.BeginAftermissionSalvaging", "GetTravelBookmark [" + bm.Title + "][" + bm.LocationId + "]", Logging.White);
					}
					return bm;
				}
				catch (Exception ex)
				{
					Logging.Log("GetTravelBookmark", "Exception [" + ex + "]", Logging.Debug);
					return null;
				}
			}
		}

		private int _bookmarkDeletionAttempt;
		public DateTime NextBookmarkDeletionAttempt = DateTime.UtcNow;

		public bool DeleteBookmarksOnGrid(string module)
		{
			try
			{
				if (DateTime.UtcNow < NextBookmarkDeletionAttempt)
				{
					return false;
				}

				NextBookmarkDeletionAttempt = DateTime.UtcNow.AddSeconds(5 + Settings.Instance.RandomNumber(1, 5));

				//
				// remove all salvage bookmarks over 48hrs old - they have long since been rendered useless
				//
				DeleteUselessSalvageBookmarks(module);

				List<DirectBookmark> bookmarksInLocal = new List<DirectBookmark>(AfterMissionSalvageBookmarks.Where(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId).OrderBy(b => b.CreatedOn));
				DirectBookmark onGridBookmark = bookmarksInLocal.FirstOrDefault(b => Cache.Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distances.OnGridWithMe);
				if (onGridBookmark != null)
				{
					_bookmarkDeletionAttempt++;
					if (_bookmarkDeletionAttempt <= bookmarksInLocal.Count() + 60)
					{
						Logging.Log(module, "removing salvage bookmark:" + onGridBookmark.Title, Logging.White);
						onGridBookmark.Delete();
						Logging.Log(module, "after: removing salvage bookmark:" + onGridBookmark.Title, Logging.White);
						NextBookmarkDeletionAttempt = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 6));
						return false;
					}

					if (_bookmarkDeletionAttempt > bookmarksInLocal.Count() + 60)
					{
						Logging.Log(module, "error removing bookmark!" + onGridBookmark.Title, Logging.White);
						_States.CurrentQuestorState = QuestorState.Error;
						return false;
					}

					return false;
				}

				_bookmarkDeletionAttempt = 0;
				Time.Instance.NextSalvageTrip = DateTime.UtcNow;
				Statistics.FinishedSalvaging = DateTime.UtcNow;
				return true;
			}
			catch (Exception ex)
			{
				Logging.Log("DeleteBookmarksOnGrid", "Exception [" + ex + "]", Logging.Debug);
				return true;
			}
		}

		public bool DeleteUselessSalvageBookmarks(string module)
		{
			if (DateTime.UtcNow < NextBookmarkDeletionAttempt)
			{
				if (Logging.DebugSalvage) Logging.Log("DeleteUselessSalvageBookmarks", "NextBookmarkDeletionAttempt is still [" + NextBookmarkDeletionAttempt.Subtract(DateTime.UtcNow).TotalSeconds + "] sec in the future... waiting", Logging.Debug);
				return false;
			}

			try
			{
				//Delete bookmarks older than 2 hours.
				DateTime bmExpirationDate = DateTime.UtcNow.AddMinutes(-Salvage.AgeofSalvageBookmarksToExpire);
				List<DirectBookmark> uselessSalvageBookmarks = new List<DirectBookmark>(AfterMissionSalvageBookmarks.Where(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0).ToList());

				DirectBookmark uselessSalvageBookmark = uselessSalvageBookmarks.FirstOrDefault();
				if (uselessSalvageBookmark != null)
				{
					_bookmarkDeletionAttempt++;
					if (_bookmarkDeletionAttempt <= uselessSalvageBookmarks.Count(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0) + 60)
					{
						Logging.Log(module, "removing a salvage bookmark that aged more than [" + Salvage.AgeofSalvageBookmarksToExpire + "]" + uselessSalvageBookmark.Title, Logging.White);
						NextBookmarkDeletionAttempt = DateTime.UtcNow.AddSeconds(5 + Settings.Instance.RandomNumber(1, 5));
						uselessSalvageBookmark.Delete();
						return false;
					}

					if (_bookmarkDeletionAttempt > uselessSalvageBookmarks.Count(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0) + 60)
					{
						Logging.Log(module, "error removing bookmark!" + uselessSalvageBookmark.Title, Logging.White);
						_States.CurrentQuestorState = QuestorState.Error;
						return false;
					}

					return false;
				}
			}
			catch (Exception ex)
			{
				Logging.Log("Cache.DeleteUselessSalvageBookmarks", "Exception:" + ex.Message, Logging.White);
			}

			return true;
		}
		
	}
}
