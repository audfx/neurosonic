using System;

using theori;
using theori.Graphics;
using theori.IO;
using theori.Platform;
using theori.Resources;
using theori.Scripting;

using NeuroSonic.Graphics;
using NeuroSonic.IO;
using NeuroSonic.Database;

namespace NeuroSonic.Platform
{
    public sealed class NscClient : Client
    {
        static NscClient()
        {
            LuaScript.RegisterType<KeyCode>();
            LuaScript.RegisterType<MouseButton>();
            LuaScript.RegisterType<ControllerInput>();
        }

        public ClientResourceManager StaticResources { get; private set; }

        public ChartDatabaseWorker DatabaseWorker { get; private set; }

        private TransitionCurtain? m_curtain;

        [Pure] public NscClient()
        {
            StaticResources = new ClientResourceManager(ClientSkinService.CurrentlySelectedSkin);
            DatabaseWorker = new ChartDatabaseWorker("local-charts.sqlite");
        }

        //[Const] protected override Layer? CreateInitialLayer() => new SplashScreen();
        [Const] protected override Layer? CreateInitialLayer() => new NscLuaLayer("driver");

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
        public bool CloseCurtain(Action? onClosed = null) => m_curtain!.Close(0.5f, onClosed);
        public bool OpenCurtain(Action? onOpened = null) => m_curtain!.Open(onOpened);

        protected override void EndInputStep()
        {
            base.EndInputStep();

            DatabaseWorker.Update();
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
