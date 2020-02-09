using System;
using System.Linq;

using theori;
using theori.Database;
using theori.Graphics;
using theori.Platform;
using theori.Resources;

using NeuroSonic.Graphics;
using NeuroSonic.GamePlay;
using theori.Configuration;
using NeuroSonic.IO;
using theori.Scripting;

namespace NeuroSonic.Platform
{
    public sealed class NscClient : Client
    {
        private readonly CustomChartTypeScanner m_scanner;

        static NscClient()
        {
            ScriptService.RegisterType<AutoPlayTargets>();
        }

        [Pure] public NscClient()
        {
            ChartDatabaseService.Initialize();

            m_scanner = new CustomChartTypeScanner();
        }

        [Const] protected override Layer? CreateInitialLayer() => new NscLayer(ClientSkinService.CurrentlySelectedSkin, "driver");
        [Const] protected override TransitionCurtain CreateTransitionCurtain() => new NscTransitionCurtain();

        protected override UnhandledExceptionAction OnUnhandledException()
        {
            return UnhandledExceptionAction.GiveUpRethrow;
        }

        public override void SetHost(ClientHost host)
        {
            base.SetHost(host);

            theori.Host.StaticResources.AquireTexture("textures/theori-logo-large");
            theori.Host.StaticResources.AquireTexture("textures/audfx-text-large");

            host.Exited += OnExited;
            Input.Initialize();

            m_scanner.BeginSearching();
        }

        private void OnExited()
        {
        }

        protected override void Update(float varyingDelta, float totalTime)
        {
            base.Update(varyingDelta, totalTime);

            Curtain?.Update(varyingDelta, totalTime);
        }

        protected override void EndRenderStep()
        {
            Curtain?.Render();
            base.EndRenderStep();
        }
    }
}
