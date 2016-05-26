namespace Questor.Modules.Lookup
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;
    using global::Questor.Modules.Logging;

    public class AgentsList
    {
        private sealed class AgentsDeclineTimers
        {
            private static readonly Lazy<AgentsDeclineTimers> lazy = new Lazy<AgentsDeclineTimers>(() => new AgentsDeclineTimers());
            public static AgentsDeclineTimers Instance { get { return lazy.Value; } }

            private Dictionary<string,DateTime> _timers;
            private string _agentMissionDeclineTimesFilePath;

            private AgentsDeclineTimers()
            {
                _agentMissionDeclineTimesFilePath = Logging.SessionDataCachePath + "agents__mission_decline_times.csv";

                _loadFromCacheFile();
            }

            private void _loadFromCacheFile()
            {
                try
                {
                    _timers = new Dictionary<string, DateTime>();

                }
                catch (Exception exception)
                {
                    Logging.Log("AgentsDeclineTimes", "Exception [" + exception + "]", Logging.Teal);
                }
            }

            private void _writeToCacheFile()
            {
                try
                {
                
                    
                }
                catch (Exception exception)
                {
                    Logging.Log("AgentsDeclineTimes", "Exception [" + exception + "]", Logging.Teal);
                }
            }
                        
            public DateTime getDeclineTimer(string agentName)
            {
                DateTime declineTimer;
                if(!_timers.TryGetValue(agentName, out declineTimer))
                {
                    declineTimer = DateTime.UtcNow;
                }

                return declineTimer;
            }

            public void setDeclineTimer(string agentName, DateTime declineTimer)
            {
                if (_timers.ContainsKey(agentName))
                    _timers[agentName] = declineTimer;
                else
                    _timers.Add(agentName, declineTimer);

                _writeToCacheFile();
            }
        }

        public AgentsList()
        {
        }

        public AgentsList(XElement agentList)
        {
            Name = (string)agentList.Attribute("name") ?? "";
            Priorit = (int)agentList.Attribute("priority");
            long homeStationId = (long)agentList.Attribute("homestationid") > 0 ? (long)agentList.Attribute("homestationid") : 60003760;
            HomeStationId = homeStationId;
        }

        public string Name { get; private set; }

        public int Priorit { get; private set; }
        
        public long HomeStationId { get; private set; }

        public DateTime DeclineTimer
        {
            get
            {
                return AgentsDeclineTimers.Instance.getDeclineTimer(Name);
            }
            set
            {
                AgentsDeclineTimers.Instance.setDeclineTimer(Name, value);
            }
        }
    }
}