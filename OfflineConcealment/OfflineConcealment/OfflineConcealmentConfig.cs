using Torch;

namespace OfflineConcealment
{
    public class OfflineConcealmentConfig : ViewModel
    {

        private int _secondsDelayOnStart = 60;
        private bool _enabled = true;
        private int _updateIntervalSeconds = 60;
        private int _concealRangeMeters = 25000;
        private string _ConcealmentModulesDirectory = "";
        private bool _recheck = true;
        private int _recheckcycles = 60;
        private int _recheckLimit = 10;

        public int RecheckLimit
        {
            get => _recheckLimit;
            set => _recheckLimit = value;
        }

        public bool Recheck
        {
            get => _recheck;
            set => _recheck = value;
        }

        public int Recheckcycles
        {
            get => _recheckcycles;
            set => _recheckcycles = value;
        }

        

        public int SecondsDelayOnStart
        {
            get => _secondsDelayOnStart;
            set => _secondsDelayOnStart = value;
        }

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public int UpdateIntervalSeconds
        {
            get => _updateIntervalSeconds;
            set => _updateIntervalSeconds = value;
        }

        public int ConcealRangeMeters
        {
            get => _concealRangeMeters;
            set => _concealRangeMeters = value;
        }

        public string ConcealmentModulesDirectory
        {
            get => _ConcealmentModulesDirectory;
            set => _ConcealmentModulesDirectory = value;
        }
    }
}
