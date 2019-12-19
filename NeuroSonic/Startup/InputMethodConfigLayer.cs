using System;

using theori.IO;

using NeuroSonic.Properties;

namespace NeuroSonic.Startup
{
#if false
    public sealed class InputMethodConfigLayer : BaseMenuLayer
    {
        protected override string Title => Strings.SecretMenu_InputMethodTitle;

        protected override void GenerateMenuItems()
        {
            AddMenuItem(new MenuItem(NextOffset, Strings.SecretMenu_InputKeyboardOnly, () => SelectKeyboard(false)));
            AddMenuItem(new MenuItem(NextOffset, Strings.SecretMenu_InputKeyboardMouse, () => SelectKeyboard(true)));

            foreach (var gamepad in UserInputService.ConnectedGamepads)
            {
                AddMenuItem(new MenuItem(NextOffset, gamepad.Name ?? "Unknown Gamepad", () => SelectGamepad(gamepad.DeviceIndex)));
            }
        }

        private void SelectKeyboard(bool andMouse)
        {
            Plugin.Config.Set(NscConfigKey.ButtonInputDevice, InputDevice.Keyboard);
            Plugin.Config.Set(NscConfigKey.LaserInputDevice, andMouse ? InputDevice.Mouse : InputDevice.Keyboard);
            Plugin.SaveNscConfig();

            Pop();
        }

        private void SelectGamepad(int deviceId)
        {
            Plugin.Config.Set(NscConfigKey.ButtonInputDevice, InputDevice.Controller);
            Plugin.Config.Set(NscConfigKey.LaserInputDevice, InputDevice.Controller);
            Plugin.SwitchActiveGamepad(deviceId); // Saves the config anyway

            Pop();
        }
    }
#endif
}
