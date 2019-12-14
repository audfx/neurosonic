using System;
using System.Numerics;

using theori;
using theori.Graphics;
using theori.Gui;
using theori.Resources;
using theori.Scripting;

using NeuroSonic.GamePlay;

using MoonSharp.Interpreter;
using theori.Database;
using theori.Platform;
using NeuroSonic.Platform;

namespace NeuroSonic
{
    public class NscLayer : Layer
    {
        protected Panel? ForegroundGui;

        public readonly Table tblNsc;
        
        public readonly Table tblNscCharts;

        public new NscClient Client => ClientAs<NscClient>();

        public NscLayer(ClientResourceLocator? resourceLocator = null, string? layerPath = null, params DynValue[] args)
            : base(resourceLocator ?? ClientSkinService.CurrentlySelectedSkin, layerPath, args)
        {
            m_script["nsc"] = tblNsc = m_script.NewTable();

            tblNsc["charts"] = tblNscCharts = m_script.NewTable();

            tblNsc["pushGameplay"] = (Action<ChartInfoHandle>)(chartInfo => Push(new GameLayer(ResourceLocator, chartInfo, AutoPlayTargets.None)));

            tblNscCharts["scanForKshCharts"] = (Action)(() =>
            {
                Client.DatabaseWorker.SetToPopulate();
            });
        }

        protected override Layer CreateNewLuaLayer(string layerPath, DynValue[] args) => new NscLayer(ResourceLocator, layerPath, args);

        public override void LateUpdate()
        {
            base.LateUpdate();
            ForegroundGui?.Update();
        }

        public override void LateRender()
        {
            static void RenderGui(Panel? panel)
            {
                if (panel == null) return;

                var dimensions = new Vector2(Window.Width, Window.Height);

                panel.Size = dimensions;
                panel.Position = Vector2.Zero;

                using var rq = new GuiRenderQueue(dimensions);
                panel.Render(rq);
            }

            RenderGui(ForegroundGui);
        }
    }
}
