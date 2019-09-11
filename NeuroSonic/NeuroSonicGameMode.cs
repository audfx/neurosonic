using theori.Charting;
using theori.GameModes;

using NeuroSonic.Charting;

namespace NeuroSonic
{
    public sealed class NeuroSonicGameMode : GameMode
    {
        public static GameMode Instance { get; } = new NeuroSonicGameMode();

        public override bool SupportsStandaloneUsage => true;
        public override bool SupportsSharedUsage => true;

        public NeuroSonicGameMode()
            : base("NeuroSonic")
        {
        }

#if false
        public override void InvokeStandalone(string[] args) => Plugin.NSC_Main(args);
        public override Layer CreateSharedGameLayer() => new GameLayer(null, null, null, AutoPlay.None);
#endif

        public override ChartFactory CreateChartFactory() => new NeuroSonicChartFactory();
    }
}
