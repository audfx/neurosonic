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
using System.Numerics;
using theori.IO;

namespace NeuroSonic.Platform
{
    public sealed class NscClient : Client
    {
        private readonly CustomChartTypeScanner m_scanner;

        static NscClient()
        {
            ScriptService.RegisterType<AutoPlayTargets>();
        }

        private readonly RenderBatch2D m_renderer;

        [Pure] public NscClient()
        {
            ChartDatabaseService.Initialize();

            m_scanner = new CustomChartTypeScanner();

            m_renderer = new RenderBatch2D(new ClientResourceManager(ClientSkinService.CurrentlySelectedSkin));

            UserConfigManager.SetFromKey("NeuroSonic.__processSpecialHotkeys", true);
            UserInputService.RawKeyPressed += UserInputService_RawKeyPressed;
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

        private void UserInputService_RawKeyPressed(KeyInfo key)
        {
            if (!(UserConfigManager.GetFromKey("NeuroSonic.__processSpecialHotkeys") is bool b) || !b) return;

            if (key.KeyCode == NscConfig.ControllerToggle)
            {
                var inputModes = UserInputService.InputModes;
                if (inputModes.HasFlag(UserInputService.Modes.Controller))
                {
                    if (inputModes.HasFlag(UserInputService.Modes.Desktop))
                        UserInputService.InputModes = UserInputService.Modes.Controller;
                    else UserInputService.InputModes = UserInputService.Modes.Desktop | UserInputService.Modes.Gamepad;
                }
                else UserInputService.InputModes = UserInputService.Modes.Any;
            }
        }

        protected override void Update(float varyingDelta, float totalTime)
        {
            base.Update(varyingDelta, totalTime);

            Curtain?.Update(varyingDelta, totalTime);
        }

        protected override void EndRenderStep()
        {
            Curtain?.Render();

            using var batch = m_renderer.Use();
            batch.SetFont(null);
            batch.SetFontSize(12);
            batch.SetFillColor(Vector4.One);
            batch.SetTextAlign(Anchor.BottomLeft);
            batch.FillString($"v{typeof(NscClient).Assembly.GetName().Version.ToString()} - {string.Join(", ", UserInputService.InputModes.Explode()).ToLower()}", 5, Window.Height - 5);

            base.EndRenderStep();
        }
    }
}
