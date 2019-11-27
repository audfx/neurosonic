using System;
using System.Numerics;

using MoonSharp.Interpreter;

using theori.Charting;
using theori.Resources;
using theori.Scripting;

using NeuroSonic.GamePlay.Scoring;

namespace NeuroSonic.GamePlay
{
#if false
    public sealed class ChartResultLayer : NscLayer
    {
        private readonly ClientResourceLocator m_locator;

        private readonly ChartInfo m_chartInfo;
        private readonly ScoringResult m_result;

        private readonly Table m_layerTable, m_chartInfoTable, m_resultTable;

        internal ChartResultLayer(ClientResourceLocator resourceLocator, ChartInfo chartInfo, ScoringResult result)
            : base(resourceLocator)
        {
            m_locator = resourceLocator;

            m_chartInfo = chartInfo;
            m_result = result;

            m_script["layer"] = m_layerTable = m_script.NewTable();
            m_layerTable["chartInfo"] = m_chartInfoTable = m_script.NewTable();
            m_layerTable["result"] = m_resultTable = m_script.NewTable();

            m_script.InitNeuroSonicEnvironment();
        }

        public override void Destroy()
        {
            base.Destroy();

            m_script.DestroyNeuroSonicEnvironment();
        }

        public override bool AsyncLoad()
        {
            m_chartInfoTable["SongTitle"] = m_chartInfo.SongTitle;
            m_chartInfoTable["SongArtist"] = m_chartInfo.SongArtist;

            m_chartInfoTable["DifficultyName"] = m_chartInfo.DifficultyName;
            m_chartInfoTable["DifficultyNameShort"] = m_chartInfo.DifficultyNameShort;
            m_chartInfoTable["DifficultyLevel"] = m_chartInfo.DifficultyLevel;
            m_chartInfoTable["DifficultyColor"] = (m_chartInfo.DifficultyColor ?? new Vector3(1, 1, 1)) * 255;

            m_chartInfoTable["PlayKind"] = "N";

            m_resultTable["Score"] = m_result.Score;
            m_resultTable["Gauge"] = m_result.Gauge;

            m_script.LoadFile(m_locator.OpenFileStream("scripts/generic-layer.lua"));
            m_script.LoadFile(m_locator.OpenFileStream("scripts/game/chart-result.lua"));
            if (!m_script.LuaAsyncLoad())
                return false;

            if (!m_resources.LoadAll())
                return false;

            return true;
        }

        public override bool AsyncFinalize()
        {
            m_script.InitSpriteRenderer();
            if (!m_script.LuaAsyncFinalize())
                return false;

            if (!m_resources.FinalizeLoad())
                return false;

            return true;
        }

        public override void Initialize()
        {
            base.Initialize();

            m_layerTable["Continue"] = (Action)Continue;
            m_script.CallIfExists("Init");

            OpenCurtain();
        }

        // TODO(local): Have this also be able to transition immediately to the next chart in a course.
        private void Continue()
        {
            CloseCurtain(() => Pop());
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            m_script.Update(delta, total);
        }

        public override void Render()
        {
            base.Render();

            m_script.Draw();
        }
    }
#endif
}
