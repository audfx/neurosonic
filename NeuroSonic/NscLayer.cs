using theori;

using NeuroSonic.IO;

namespace NeuroSonic
{
    public abstract class NscLayer : Layer
    {
        public override void Init()
        {
            Input.Register(this);
        }

        public override void Destroy()
        {
            Input.UnRegister(this);
        }

        public override void Update(float delta, float total)
        {
            Input.Update();
        }

        protected internal virtual bool ControllerButtonPressed(ControllerInput input) => false;
        protected internal virtual bool ControllerButtonReleased(ControllerInput input) => false;
        protected internal virtual bool ControllerAxisChanged(ControllerInput input, float delta) => false;
    }
}
