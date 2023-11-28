namespace OfflineConcealment
{
    /// <summary>
    /// Stores all the data about a single grid
    /// </summary>
    public class ConcealmentData
    {
        public bool Concealed;
        public int CyclesSinceLastRefresh;

        public ConcealmentData(bool concealed, int cyclesSinceLastRefresh)
        {
            Concealed = concealed;
            CyclesSinceLastRefresh = cyclesSinceLastRefresh;
        }

        public ConcealmentData()
        {
        }
    }
}