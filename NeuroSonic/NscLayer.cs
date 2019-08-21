using theori;

using NeuroSonic.IO;

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
        public override void Init()
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
    }

    public abstract class NscOverlay : Overlay, IControllerInputLayer
    {
        public override void Init()
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
    }
}
