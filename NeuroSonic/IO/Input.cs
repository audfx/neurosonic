using System;
using System.Collections.Generic;
using System.IO;
using theori.Configuration;
using theori.IO;

namespace NeuroSonic.IO
{
    public static class Input
    {
        private static Controller? m_controller;
        public static Controller Controller => m_controller ?? throw new InvalidOperationException("Controller has not been initialized yet!");

        private static readonly List<Func<Controller?>> m_defaultFactories = new List<Func<Controller?>>()
        {
            CreateDefaultController,
            //CreateDefaultController,
            //CreateYuanConController,
        };

        private static readonly Dictionary<string, Controller> m_controllers = new Dictionary<string, Controller>();

        private static Controller? CreateDefaultController()
        {
            var con = new Controller("NeuroSonic User");

            con.SetButtonToKey(0, KeyCode.D);
            con.SetButtonToKey(1, KeyCode.F);
            con.SetButtonToKey(2, KeyCode.J);
            con.SetButtonToKey(3, KeyCode.K);
            con.SetButtonToKey(4, KeyCode.V);
            con.SetButtonToKey(5, KeyCode.N);
            con.SetButtonToKey("start", KeyCode.D1);
            con.SetButtonToKey("back", KeyCode.BACKSPACE);

            con.SetAxisToKeysLinear(0, KeyCode.W, KeyCode.Q);
            con.SetAxisToKeysLinear(1, KeyCode.P, KeyCode.O);

            return con;
        }

        private static Controller? CreateKeyboardController()
        {
            var con = new Controller("NeuroSonic Keyboard");

            con.SetButtonToKey(0, KeyCode.A);
            con.SetButtonToKey(0, KeyCode.H);

            con.SetButtonToKey(1, KeyCode.S);
            con.SetButtonToKey(1, KeyCode.J);

            con.SetButtonToKey(2, KeyCode.D);
            con.SetButtonToKey(2, KeyCode.K);

            con.SetButtonToKey(3, KeyCode.F);
            con.SetButtonToKey(3, KeyCode.L);

            con.SetButtonToKey(4, KeyCode.Z);
            con.SetButtonToKey(4, KeyCode.X);

            con.SetButtonToKey(5, KeyCode.COMMA);
            con.SetButtonToKey(5, KeyCode.PERIOD);

            con.SetButtonToKey("start", KeyCode.SPACE);
            con.SetButtonToKey("start", KeyCode.RETURN);

            con.SetButtonToKey("back", KeyCode.ESCAPE);

            con.SetAxisToKeysLinear(0, KeyCode.W, KeyCode.Q);
            con.SetAxisToKeysLinear(1, KeyCode.O, KeyCode.I);

            return con;
        }

        private static Controller? CreateYuanConController()
        {
            if (!(UserInputService.TryGetGamepadFromName("YuanCon") is Gamepad gamepad)) return null;

            var con = new Controller("NeuroSonic YuanCon");

            con.SetButtonToGamepadButton(0, gamepad, 1);
            con.SetButtonToGamepadButton(1, gamepad, 2);
            con.SetButtonToGamepadButton(2, gamepad, 3);
            con.SetButtonToGamepadButton(3, gamepad, 7);
            con.SetButtonToGamepadButton(4, gamepad, 5);
            con.SetButtonToGamepadButton(5, gamepad, 6);

            con.SetButtonToGamepadButton("start", gamepad, 0);
            con.SetButtonToGamepadButton("back", gamepad, 9);

            con.SetAxisToGamepadAxis(0, gamepad, 0, ControllerAxisStyle.Radial);
            con.SetAxisToGamepadAxis(1, gamepad, 1, ControllerAxisStyle.Radial);

            return con;
        }

        public static void Initialize()
        {
            foreach (var factory in m_defaultFactories)
            {
                if (!(factory() is Controller con)) continue;

                string filePath = Path.Combine("controllers", con.Name + ".json");
                if (File.Exists(filePath)) continue;

                m_controllers[con.Name] = con;
                con.SaveToFile();
            }

            if (TheoriConfig.SelectedController != null)
                SelectController(TheoriConfig.SelectedController);
            else SelectController("NeuroSonic User.json");
        }

        public static void DeselectController()
        {
            if (m_controller == null) return;

            UserInputService.RemoveController(m_controller!);
            m_controller = null;
        }

        public static void SelectController(string controllerFileName)
        {
            Controller? nextController = null;
            if (m_controllers.TryGetValue(controllerFileName, out var cached))
                nextController = cached;
            if (Controller.TryCreateFromFile(Path.Combine("controllers", controllerFileName)) is Controller controller)
            {
                m_controllers[controllerFileName] = controller;
                nextController = controller;
            }
            else DeselectController();

            if (nextController != null)
            {
                if (m_controller != null)
                    UserInputService.RemoveController(m_controller);
                m_controller = nextController;
                UserInputService.RegisterController(m_controller!);
            }
        }
    }
}
