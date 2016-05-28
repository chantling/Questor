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
using Questor.Modules.Caching;

namespace Questor.Modules.Lookup
{
    public class PriorityTarget
    {
        private EntityCache _entity;

        private string _maskedID;

        public long EntityID { get; set; }

        public string MaskedID
        {
            get
            {
                try
                {
                    var numofCharacters = EntityID.ToString().Length;
                    if (numofCharacters >= 5)
                    {
                        _maskedID = EntityID.ToString().Substring(numofCharacters - 4);
                        _maskedID = "[MaskedID]" + _maskedID;
                        return _maskedID;
                    }

                    return "!0!";
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("EntityCache", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return "!0!";
                }
            }
        }

        public string Name { get; set; }

        public PrimaryWeaponPriority PrimaryWeaponPriority { get; set; }

        public DronePriority DronePriority { get; set; }

        public EntityCache Entity
        {
            get { return _entity ?? (_entity = Cache.Instance.EntityById(EntityID)); }
        }

        public void ClearCache()
        {
            _entity = null;
        }
    }
}