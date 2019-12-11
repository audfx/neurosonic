using System;

using theori;
using theori.Configuration;
using theori.Gui;
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
                }
                else
                {
                    goto error;
                }
            }
            else if (btInputMode == InputDevice.Keyboard)
            {
                float keySens = Plugin.Config.GetFloat(NscConfigKey.Key_Sensitivity);
                float mouseSens = Plugin.Config.GetFloat(NscConfigKey.Mouse_Sensitivity);
                int smooth = Plugin.Config.GetInt(NscConfigKey.LaserInputSmoothing);

                Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_BT0);

                void SetButton(HybridLabel name, NscConfigKey key)
                {
                    m_controller!.SetButtonToKey(name, Plugin.Config.GetEnum<KeyCode>(key));
                }

                void SetAxisKeys(HybridLabel name, NscConfigKey keyNeg, NscConfigKey keyPos)
                {
                    m_controller!.SetAxisToKeysLinear(name, Plugin.Config.GetEnum<KeyCode>(keyPos), Plugin.Config.GetEnum<KeyCode>(keyNeg));
                }

                // TODO(local): Get config of the gui axes and onto the standard axis
                void SetAxisMouse(HybridLabel name, NscConfigKey axis)
                {
                    var axesAxis = Plugin.Config.GetEnum<Axes>(axis);
                    m_controller!.SetAxisToMouseAxis(name, axesAxis == Axes.X ? Axis.X : Axis.Y, mouseSens);
                }

                SetButton("start", NscConfigKey.Key_Start);
                SetButton("back", NscConfigKey.Key_Back);

                SetButton(0, NscConfigKey.Key_BT0);
                SetButton(1, NscConfigKey.Key_BT1);
                SetButton(2, NscConfigKey.Key_BT2);
                SetButton(3, NscConfigKey.Key_BT3);

                SetButton(4, NscConfigKey.Key_FX0);
                SetButton(5, NscConfigKey.Key_FX1);

                if (volInputMode == InputDevice.Keyboard)
                {
                    SetAxisKeys(0, NscConfigKey.Key_Laser0Neg, NscConfigKey.Key_Laser0Pos);
                    SetAxisKeys(1, NscConfigKey.Key_Laser1Neg, NscConfigKey.Key_Laser1Pos);
                }
                else if (volInputMode == InputDevice.Mouse)
                {
                    SetAxisMouse(0, NscConfigKey.Mouse_Laser0Axis);
                    SetAxisMouse(1, NscConfigKey.Mouse_Laser1Axis);
                }
            }

            return;

        error:
            throw new InvalidOperationException($"No controller implementation supports Buttons using { btInputMode } and Lasers using { volInputMode }");
        }

        public static bool IsButtonDown(HybridLabel label) => false;
    }
}
