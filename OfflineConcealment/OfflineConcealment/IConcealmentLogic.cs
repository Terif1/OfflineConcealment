using Sandbox.Game.Entities;

namespace OfflineConcealment
{
    public interface IConcealmentLogic
    {
        void OnReveal(MyCubeGrid grid);

        void OnConceal(MyCubeGrid grid);
    }
}