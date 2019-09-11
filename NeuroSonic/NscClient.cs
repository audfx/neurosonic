using System;

using theori;
using theori.Graphics;
using theori.Platform;

using NeuroSonic.Graphics;
using NeuroSonic.Startup;
using NeuroSonic.IO;

namespace NeuroSonic.Platform
{
    public sealed class NscClient : Client
    {
        private TransitionCurtain? m_curtain;

        [Pure] public NscClient()
        {
        }

        [Const] protected override Layer? CreateInitialLayer() => new SplashScreen();

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
            Plugin.Initialize(host);

            m_curtain = new NscTransitionCurtain();
        }

        private void OnExited()
        {
            Plugin.SaveNscConfig();

            Input.DestroyController();
            Plugin.Gamepad?.Close();
        }

        public bool CloseCurtain(float holdTime, Action? onClosed = null) => m_curtain!.Close(holdTime, onClosed);
        public bool CloseCurtain(Action? onClosed = null) => m_curtain!.Close(1.5f, onClosed);
        public bool OpenCurtain(Action? onOpened = null) => m_curtain!.Open(onOpened);

        protected override void EndInputStep()
        {
            base.EndInputStep();

            Input.Update();
        }

        protected override void Update(float varyingDelta, float totalTime)
        {
            base.Update(varyingDelta, totalTime);

            m_curtain?.Update(varyingDelta, totalTime);
        }

        protected override void FixedUpdate(float fixedDelta, float totalTime)
        {
            base.FixedUpdate(fixedDelta, totalTime);
        }

        protected override void EndRenderStep()
        {
            m_curtain?.Render();
            base.EndRenderStep();
        }
    }
}
