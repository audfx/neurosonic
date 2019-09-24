using System.Collections.Generic;
using theori;

namespace NeuroSonic.IO
{
    public static class Input
    {
        private static Controller Controller { get; set; }

        private static readonly List<IControllerInputLayer> layers = new List<IControllerInputLayer>();

        public static void CreateController()
        {
            DestroyController();
            Controller = Controller.Create();

            if (Controller != null)
            {
                Controller.ButtonPressed = Controller_ButtonPressed;
                Controller.ButtonReleased = Controller_ButtonReleased;
                Controller.AxisChanged = Controller_AxisChanged;
            }
        }

        public static void DestroyController()
        {
            if (Controller != null)
            {
                Controller.ButtonPressed = null;
                Controller.ButtonReleased = null;
                Controller.AxisChanged = null;

                Controller.Dispose();
            }
        }

        public static void Controller_ButtonPressed(ControllerInput input)
        {
            bool allow3BtStartBack = Plugin.Config.GetBool(NscConfigKey.Allow3BtStart);
            if (allow3BtStartBack && input == ControllerInput.Start)
            {
                bool a = IsButtonDown(ControllerInput.BT0);
                bool b = IsButtonDown(ControllerInput.BT1);
                bool c = IsButtonDown(ControllerInput.BT2);
                bool d = IsButtonDown(ControllerInput.BT3);

                if ((a && b && c) || (a && b && d) || (a && c && d) || (b && c && d))
                    input = ControllerInput.Back;
            }

            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].ControllerButtonPressed(input))
                    break;
            }
        }

        public static void Controller_ButtonReleased(ControllerInput input)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].ControllerButtonReleased(input))
                    break;
            }
        }

        public static void Controller_AxisChanged(ControllerInput input, float delta)
        {
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i].ControllerAxisChanged(input, delta))
                    break;
            }
        }

        public static void Register(IControllerInputLayer layer)
        {
            if (layers.Contains(layer)) return;
            layers.Add(layer);
        }

        public static void UnRegister(IControllerInputLayer layer) => layers.Remove(layer);
        public static void Update() => Controller?.Update();

        public static bool IsButtonDown(ControllerInput input) => Controller?.IsButtonDown(input) ?? false;
        public static float AxisDelta(ControllerInput input) => Controller?.AxisDelta(input) ?? 0.0f;
        public static float RawAxisValue(ControllerInput input) => Controller?.RawAxisValue(input) ?? 0.0f;
    }
}
