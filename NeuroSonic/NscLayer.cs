using System.Numerics;

using theori;
using theori.Graphics;
using theori.Gui;

using NeuroSonic.IO;
using NeuroSonic.Platform;
using System;

namespace NeuroSonic
{
    public interface IControllerInputLayer
    {
        bool ControllerButtonPressed(ControllerInput input);
        bool ControllerButtonReleased(ControllerInput input);
        bool ControllerAxisChanged(ControllerInput input, float delta);
    }

    public abstract class NscLayer : Layer, IControllerInputLayer
    {
        protected Panel? ForegroundGui;

        protected void CloseCurtain(float holdTime, Action? onClosed = null) => ClientAs<NscClient>().CloseCurtain(holdTime, onClosed);
        protected void CloseCurtain(Action? onClosed = null) => ClientAs<NscClient>().CloseCurtain(onClosed);
        protected void OpenCurtain() => ClientAs<NscClient>().OpenCurtain();

        public override void Initialize()
        {
            Input.Register(this);
        }

        public override void Destroy()
        {
            Input.UnRegister(this);
        }

        public virtual bool ControllerButtonPressed(ControllerInput input) => false;
        public virtual bool ControllerButtonReleased(ControllerInput input) => false;
        public virtual bool ControllerAxisChanged(ControllerInput input, float delta) => false;

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
