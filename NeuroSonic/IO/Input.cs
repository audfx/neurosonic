using System;

using theori;
using theori.Configuration;
using theori.IO;

namespace NeuroSonic.IO
{
    public static class Input
    {
        private static Controller? m_controller;
        public static Controller Controller => m_controller ?? throw new InvalidOperationException("Controller has not been initialized yet!");

        public static void CreateController()
        {
            var btInputMode = Plugin.Config.GetEnum<InputDevice>(NscConfigKey.ButtonInputDevice);
            var volInputMode = Plugin.Config.GetEnum<InputDevice>(NscConfigKey.LaserInputDevice);

            if (m_controller != null)
            {
                UserInputService.RemoveController(m_controller);
                m_controller = null;
            }

            m_controller = new Controller("NeuroSonic Controller");
            UserInputService.RegisterController(m_controller);

            if (btInputMode == InputDevice.Controller && volInputMode == InputDevice.Controller)
            {
                int deviceId = Plugin.Host.Config.GetInt(TheoriConfigKey.Controller_DeviceID);
                if (UserInputService.TryGetGamepadFromDeviceIndex(deviceId) is { } gamepad)
                {
                    float sens = Plugin.Config.GetFloat(NscConfigKey.Controller_Sensitivity);
                    int smooth = Plugin.Config.GetInt(NscConfigKey.LaserInputSmoothing);

                    void SetButton(HybridLabel name, NscConfigKey key)
                    {
                        m_controller!.SetButtonToGamepadButton(name, gamepad, (uint)Plugin.Config.GetInt(key));
                    }

                    void SetAxis(HybridLabel name, NscConfigKey key)
                    {
                        m_controller!.SetAxisToGamepadAxis(name, gamepad, (uint)Plugin.Config.GetInt(key), ControllerAxisStyle.Radial, sens, smooth);
                    }

                    SetButton("start", NscConfigKey.Controller_Start);
                    SetButton("back", NscConfigKey.Controller_Back);

                    SetButton(0, NscConfigKey.Controller_BT0);
                    SetButton(1, NscConfigKey.Controller_BT1);
                    SetButton(2, NscConfigKey.Controller_BT2);
                    SetButton(3, NscConfigKey.Controller_BT3);

                    SetButton(4, NscConfigKey.Controller_FX0);
                    SetButton(5, NscConfigKey.Controller_FX1);

                    SetAxis(0, NscConfigKey.Controller_Laser0Axis);
                    SetAxis(1, NscConfigKey.Controller_Laser1Axis);

                    return;
                }
                else
                {
                }
            }
            else if (btInputMode == InputDevice.Keyboard)
            {
                if (volInputMode == InputDevice.Keyboard)
                {
                }
                else if (volInputMode == InputDevice.Mouse)
                {
                }
            }

            throw new InvalidOperationException($"No controller implementation supports Buttons using { btInputMode } and Lasers using { volInputMode }");
        }

        public static bool IsButtonDown(HybridLabel label) => false;
    }
}
