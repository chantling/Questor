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
using System;
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Caching
{
    public partial class Cache
    {
        internal static DirectBookmark _undockBookmarkInLocal;
        public List<DirectBookmark> _allBookmarks;
        private int _bookmarkDeletionAttempt;
        private IEnumerable<DirectBookmark> ListOfUndockBookmarks;
        public DateTime NextBookmarkDeletionAttempt = DateTime.UtcNow;

        public List<DirectBookmark> AllBookmarks
        {
            get
            {
                try
                {
                    if (Instance._allBookmarks == null || !Instance._allBookmarks.Any())
                    {
                        if (DateTime.UtcNow > Time.Instance.NextBookmarkAction)
                        {
                            Time.Instance.NextBookmarkAction = DateTime.UtcNow.AddMilliseconds(200);
                            if (DirectEve.Bookmarks.Any())
                            {
                                _allBookmarks = Instance.DirectEve.Bookmarks;
                                return _allBookmarks;
                            }

                            return null; //there are no bookmarks to list...
                        }

                        return null; //new List<DirectBookmark>(); //there are no bookmarks to list...
                    }

                    return Instance._allBookmarks;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return new List<DirectBookmark>();
                    ;
                }
            }
            set { _allBookmarks = value; }
        }

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
                                ListOfUndockBookmarks = Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix);
                            }
                        }
                        if (ListOfUndockBookmarks != null && ListOfUndockBookmarks.Any())
                        {
                            ListOfUndockBookmarks = ListOfUndockBookmarks.Where(i => i.LocationId == Instance.DirectEve.Session.LocationId).ToList();
                            _undockBookmarkInLocal =
                                ListOfUndockBookmarks.OrderBy(i => Instance.DistanceFromMe(i.X ?? 0, i.Y ?? 0, i.Z ?? 0))
                                    .FirstOrDefault(b => Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int) Distances.NextPocketDistance);
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
                    Logging.Logging.Log("[" + exception + "]");
                    return null;
                }
            }
            internal set { _undockBookmarkInLocal = value; }
        }

        public IEnumerable<DirectBookmark> SafeSpotBookmarks
        {
            get
            {
                try
                {
                    if (_safeSpotBookmarks == null)
                    {
                        _safeSpotBookmarks = Instance.BookmarksByLabel(Settings.Instance.SafeSpotBookmarkPrefix).ToList();
                    }

                    if (_safeSpotBookmarks != null && _safeSpotBookmarks.Any())
                    {
                        return _safeSpotBookmarks;
                    }

                    return new List<DirectBookmark>();
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
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
                    var _bookmarkprefix = Settings.Instance.BookmarkPrefix;

                    if (Instance.BookmarksByLabel(_bookmarkprefix + " ") != null)
                    {
                        return Instance.BookmarksByLabel(_bookmarkprefix + " ").ToList();
                    }

                    return new List<DirectBookmark>();
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                    return new List<DirectBookmark>();
                }
            }
        }

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
                    Logging.Logging.Log("Exception [" + ex + "]");
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
                    if (Instance.AllBookmarks != null && Instance.AllBookmarks.Any())
                    {
                        List<DirectBookmark> _SalvagingBookmarks;
                        DirectBookmark _SalvagingBookmark;
                        if (Salvage.FirstSalvageBookmarksInSystem)
                        {
                            Logging.Logging.Log("Salvaging at first bookmark from system");
                            _SalvagingBookmarks = Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                            if (_SalvagingBookmarks != null && _SalvagingBookmarks.Any())
                            {
                                _SalvagingBookmark =
                                    _SalvagingBookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault(c => c.LocationId == Instance.DirectEve.Session.SolarSystemId);
                                return _SalvagingBookmark;
                            }

                            return null;
                        }

                        Logging.Logging.Log("Salvaging at first oldest bookmarks");
                        _SalvagingBookmarks = Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
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
                    Logging.Logging.Log("Exception [" + ex + "]");
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
                    var bm =
                        Instance.BookmarksByLabel(Settings.Instance.TravelToBookmarkPrefix)
                            .OrderByDescending(b => b.CreatedOn)
                            .FirstOrDefault(c => c.LocationId == Instance.DirectEve.Session.SolarSystemId) ??
                        Instance.BookmarksByLabel(Settings.Instance.TravelToBookmarkPrefix).OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
                        Instance.BookmarksByLabel("Jita").OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
                        Instance.BookmarksByLabel("Rens").OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
                        Instance.BookmarksByLabel("Amarr").OrderByDescending(b => b.CreatedOn).FirstOrDefault() ??
                        Instance.BookmarksByLabel("Dodixie").OrderByDescending(b => b.CreatedOn).FirstOrDefault();

                    if (bm != null)
                    {
                        Logging.Logging.Log("GetTravelBookmark [" + bm.Title + "][" + bm.LocationId + "]");
                    }
                    return bm;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public DirectBookmark BookmarkById(long bookmarkId)
        {
            try
            {
                if (Instance.AllBookmarks != null && Instance.AllBookmarks.Any())
                {
                    return Instance.AllBookmarks.FirstOrDefault(b => b.BookmarkId == bookmarkId);
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return null;
            }
        }

        public List<DirectBookmark> BookmarksByLabel(string label)
        {
            try
            {
                // Does not seems to refresh the Corporate Bookmark list so it's having troubles to find Corporate Bookmarks
                if (Instance.AllBookmarks != null && Instance.AllBookmarks.Any())
                {
                    return
                        Instance.AllBookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().StartsWith(label.ToLower()))
                            .OrderBy(f => f.LocationId)
                            .ThenBy(i => Instance.DistanceFromMe(i.X ?? 0, i.Y ?? 0, i.Z ?? 0))
                            .ToList();
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return null;
            }
        }

        public List<DirectBookmark> BookmarksThatContain(string label)
        {
            try
            {
                if (Instance.AllBookmarks != null && Instance.AllBookmarks.Any())
                {
                    return
                        Instance.AllBookmarks.Where(b => !string.IsNullOrEmpty(b.Title) && b.Title.ToLower().Contains(label.ToLower()))
                            .OrderBy(f => f.LocationId)
                            .ToList();
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return null;
            }
        }

        public void CreateBookmark(string label)
        {
            try
            {
                if (Instance.AfterMissionSalvageBookmarks.Count() < 100)
                {
                    if (Salvage.CreateSalvageBookmarksIn.ToLower() == "corp".ToLower())
                    {
                        var folder = Instance.DirectEve.BookmarkFolders.FirstOrDefault(i => i.Name == Settings.Instance.BookmarkFolder);
                        if (folder != null)
                        {
                            Instance.DirectEve.CorpBookmarkCurrentLocation(label, "", folder.Id);
                        }
                        else
                        {
                            Instance.DirectEve.CorpBookmarkCurrentLocation(label, "", null);
                        }
                    }
                    else
                    {
                        var folder = Instance.DirectEve.BookmarkFolders.FirstOrDefault(i => i.Name == Settings.Instance.BookmarkFolder);
                        if (folder != null)
                        {
                            Instance.DirectEve.BookmarkCurrentLocation(label, "", folder.Id);
                        }
                        else
                        {
                            Instance.DirectEve.BookmarkCurrentLocation(label, "", null);
                        }
                    }
                }
                else
                {
                    Logging.Logging.Log("We already have over 100 AfterMissionSalvage bookmarks: their must be a issue processing or deleting bookmarks. No additional bookmarks will be created until the number of salvage bookmarks drops below 100.");
                }

                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return;
            }
        }

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

                var bookmarksInLocal =
                    new List<DirectBookmark>(
                        AfterMissionSalvageBookmarks.Where(b => b.LocationId == Instance.DirectEve.Session.SolarSystemId).OrderBy(b => b.CreatedOn));
                var onGridBookmark = bookmarksInLocal.FirstOrDefault(b => Instance.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int) Distances.OnGridWithMe);
                if (onGridBookmark != null)
                {
                    _bookmarkDeletionAttempt++;
                    if (_bookmarkDeletionAttempt <= bookmarksInLocal.Count() + 60)
                    {
                        Logging.Logging.Log("removing salvage bookmark:" + onGridBookmark.Title);
                        onGridBookmark.Delete();
                        Logging.Logging.Log("after: removing salvage bookmark:" + onGridBookmark.Title);
                        NextBookmarkDeletionAttempt = DateTime.UtcNow.AddSeconds(Instance.RandomNumber(2, 6));
                        return false;
                    }

                    if (_bookmarkDeletionAttempt > bookmarksInLocal.Count() + 60)
                    {
                        Logging.Logging.Log("error removing bookmark!" + onGridBookmark.Title);
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
                Logging.Logging.Log("Exception [" + ex + "]");
                return true;
            }
        }

        public bool DeleteUselessSalvageBookmarks(string module)
        {
            if (DateTime.UtcNow < NextBookmarkDeletionAttempt)
            {
                if (Logging.Logging.DebugSalvage)
                    Logging.Logging.Log("NextBookmarkDeletionAttempt is still [" + NextBookmarkDeletionAttempt.Subtract(DateTime.UtcNow).TotalSeconds +
                        "] sec in the future... waiting");
                return false;
            }

            try
            {
                //Delete bookmarks older than 2 hours.
                var bmExpirationDate = DateTime.UtcNow.AddMinutes(-Salvage.AgeofSalvageBookmarksToExpire);
                var uselessSalvageBookmarks =
                    new List<DirectBookmark>(
                        AfterMissionSalvageBookmarks.Where(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0).ToList());

                var uselessSalvageBookmark = uselessSalvageBookmarks.FirstOrDefault();
                if (uselessSalvageBookmark != null)
                {
                    _bookmarkDeletionAttempt++;
                    if (_bookmarkDeletionAttempt <=
                        uselessSalvageBookmarks.Count(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0) + 60)
                    {
                        Logging.Logging.Log("removing a salvage bookmark that aged more than [" + Salvage.AgeofSalvageBookmarksToExpire + "]" + uselessSalvageBookmark.Title);
                        NextBookmarkDeletionAttempt = DateTime.UtcNow.AddSeconds(5 + Settings.Instance.RandomNumber(1, 5));
                        uselessSalvageBookmark.Delete();
                        return false;
                    }

                    if (_bookmarkDeletionAttempt >
                        uselessSalvageBookmarks.Count(e => e.CreatedOn != null && e.CreatedOn.Value.CompareTo(bmExpirationDate) < 0) + 60)
                    {
                        Logging.Logging.Log("error removing bookmark!" + uselessSalvageBookmark.Title);
                        _States.CurrentQuestorState = QuestorState.Error;
                        return false;
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception:" + ex.Message);
            }

            return true;
        }
    }
}