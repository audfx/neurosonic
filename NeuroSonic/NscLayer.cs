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
using NeuroSonic.IO;
using theori.IO;

namespace NeuroSonic
{
    public class NscLayer : Layer
    {
        static NscLayer()
        {
            ScriptService.RegisterType<HiSpeedMod>();
        }

        protected Panel? ForegroundGui;

        public readonly Table tblNsc;
        
        public readonly Table tblNscCharts;
        public readonly Table tblNscInput;

        public new NscClient Client => ClientAs<NscClient>();

        public NscLayer(ClientResourceLocator? resourceLocator = null, string? layerPath = null, params DynValue[] args)
            : base(resourceLocator ?? ClientSkinService.CurrentlySelectedSkin, layerPath, args)
        {
            m_script["AutoPlayTargets"] = typeof(AutoPlayTargets);
            m_script["HiSpeedMod"] = typeof(HiSpeedMod);

            m_script["nsc"] = tblNsc = m_script.NewTable();

            tblNsc["charts"] = tblNscCharts = m_script.NewTable();
            tblNsc["input"] = tblNscInput = m_script.NewTable();

            //tblNsc["pushGameplay"] = (Action<ChartInfoHandle>)(chartInfo => Push(new GameLayer(ResourceLocator, chartInfo, AutoPlayTargets.None)));
            tblNsc["pushGameplay"] = DynValue.NewCallback((context, args) =>
            {
                if (args.Count == 0)
                    throw new ScriptRuntimeException("Chart info expected for pushGameplay.");
                args = args.SkipMethodCall();

                ChartInfoHandle chartInfo = args.AsUserData<ChartInfoHandle>(0, "pushGameplay"); ;
                var autoPlay = AutoPlayTargets.None;
                if (args.Count >= 2)
                    autoPlay = args.AsUserData<AutoPlayTargets>(1, "pushGameplay");

                var layer = new GameLayer(ResourceLocator, chartInfo, autoPlay);
                Push(layer);

                return DynValue.Void;
            });

            tblNscCharts["scanForKshCharts"] = (Action)(() =>
            {
                Client.DatabaseWorker.SetToPopulate();
            });

            tblNscInput["getController"] = (Func<Controller>)(() => Input.Controller);
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
