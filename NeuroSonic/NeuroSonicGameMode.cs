using theori.Charting;
using theori.GameModes;

using NeuroSonic.Charting;
using theori.Charting.Serialization;
using NeuroSonic.Charting.KShootMania;

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

        public override ChartFactory GetChartFactory() => new NeuroSonicChartFactory();
        public override IChartSerializer? CreateChartSerializer(string chartsDirectory, string? fileFormat) => fileFormat switch
        {
            ".ksh" => new KshChartSerializer(chartsDirectory),
            _ => null,
        };
    }
}
