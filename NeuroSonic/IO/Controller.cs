using System;
using System.Collections.Generic;

using theori;
using theori.Gui;
using theori.IO;

namespace NeuroSonic.IO
{
    public abstract class Controller : Disposable
    {
        public static Controller Create()
        {
            var btInputMode = Plugin.Config.GetEnum<InputDevice>(NscConfigKey.ButtonInputDevice);
            var volInputMode = Plugin.Config.GetEnum<InputDevice>(NscConfigKey.LaserInputDevice);

            // TODO(local): This makes lots of assumptions about the mouse
            Mouse.Relative = false;

            if (btInputMode == InputDevice.Controller && volInputMode == InputDevice.Controller)
                return new GamepadController(Plugin.Gamepad);
            else if (btInputMode == InputDevice.Keyboard)
            {
                if (volInputMode == InputDevice.Keyboard)
                    return new KeyboardController();
                else if (volInputMode == InputDevice.Mouse)
                {
                    Mouse.Relative = true;
                    return new KeyboardMouseController();
                }
            }

            throw new InvalidOperationException($"No controller implementation supports Buttons using { btInputMode } and Lasers using { volInputMode }");
        }

        public Action<ControllerInput> ButtonPressed;
        public Action<ControllerInput> ButtonReleased;
        public Action<ControllerInput, float> AxisChanged;

        public abstract bool IsButtonDown(ControllerInput input);
        public abstract float AxisDelta(ControllerInput input);
        public abstract float RawAxisValue(ControllerInput input);

        public abstract void Update();
    }

    public sealed class GamepadController : Controller
    {
        private Gamepad m_gamepad;

        private readonly Dictionary<int, ControllerInput> m_buttonToControllerInput = new Dictionary<int, ControllerInput>();
        private readonly Dictionary<int, ControllerInput> m_axisToControllerInput = new Dictionary<int, ControllerInput>();

        private readonly Dictionary<ControllerInput, List<uint>> m_buttons = new Dictionary<ControllerInput, List<uint>>();

        private readonly float[] m_axisPrevious = new float[2];
        private readonly float[] m_axisAverageDelta = new float[2];

        private readonly float[] m_currentDelta = new float[2], m_nextDelta = new float[2];

        public GamepadController(Gamepad gamepad)
        {
            m_gamepad = gamepad;

            if (gamepad != null)
            {
                m_gamepad.ButtonPressed += OnButtonPressed;
                m_gamepad.ButtonReleased += OnButtonReleased;
                m_gamepad.AxisChanged += OnAxisChanged;
            }

            SetButtonCode(ControllerInput.Start, NscConfigKey.Controller_Start);
            SetButtonCode(ControllerInput.Back, NscConfigKey.Controller_Back);

            SetButtonCode(ControllerInput.BT0, NscConfigKey.Controller_BT0);
            SetButtonCode(ControllerInput.BT1, NscConfigKey.Controller_BT1);
            SetButtonCode(ControllerInput.BT2, NscConfigKey.Controller_BT2);
            SetButtonCode(ControllerInput.BT3, NscConfigKey.Controller_BT3);

            SetButtonCode(ControllerInput.FX0, NscConfigKey.Controller_FX0);
            SetButtonCode(ControllerInput.FX1, NscConfigKey.Controller_FX1);

            m_axisToControllerInput[Plugin.Config.GetInt(NscConfigKey.Controller_Laser0Axis)] = ControllerInput.Laser0Axis;
            m_axisToControllerInput[Plugin.Config.GetInt(NscConfigKey.Controller_Laser1Axis)] = ControllerInput.Laser1Axis;

            void SetButtonCode(ControllerInput input, NscConfigKey primaryKey, NscConfigKey? secondaryKey = null)
            {
                m_buttons[input] = new List<uint>();

                m_buttonToControllerInput[Plugin.Config.GetInt(primaryKey)] = input;
                if (secondaryKey is NscConfigKey s)
                    m_buttonToControllerInput[Plugin.Config.GetInt(s)] = input;
            }
        }

        public override void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                m_currentDelta[i] = m_nextDelta[i];
                m_nextDelta[i] = 0.0f;
            }
        }

        public override bool IsButtonDown(ControllerInput input) => m_buttons[input].Count > 0;
        public override float AxisDelta(ControllerInput input) => m_currentDelta[input - ControllerInput.Laser0Axis];
        public override float RawAxisValue(ControllerInput input) => m_axisPrevious[input == ControllerInput.Laser0Axis ? 0 : 1];

        protected override void DisposeManaged()
        {
            if (m_gamepad == null) return;

            m_gamepad.ButtonPressed -= OnButtonPressed;
            m_gamepad.ButtonReleased -= OnButtonReleased;
            m_gamepad.AxisChanged -= OnAxisChanged;

            m_gamepad = null;
        }

        private void OnButtonPressed(uint index)
        {
            if (m_buttonToControllerInput.TryGetValue((int)index, out ControllerInput input))
            {
                var list = m_buttons[input];
                bool wasDown = list.Count > 0;
                if (!list.Contains(index))
                    list.Add(index);

                if (!wasDown && list.Count > 0)
                    ButtonPressed?.Invoke(input);
            }
        }

        private void OnButtonReleased(uint index)
        {
            if (m_buttonToControllerInput.TryGetValue((int)index, out ControllerInput input))
            {
                var list = m_buttons[input];
                bool wasDown = list.Count > 0;

                list.Remove(index);
                if (wasDown && list.Count == 0)
                    ButtonReleased?.Invoke(input);
            }
        }

        private void OnAxisChanged(uint index, float value)
        {
            if (!m_axisToControllerInput.TryGetValue((int)index, out ControllerInput input))
                return;

            float p = m_axisPrevious[index];
            float c = value;

            float delta = c - p;
            if (p > 0.9f && c < -0.9f)
                delta = m_axisAverageDelta[index];
            else if (p < -0.9f && c > 0.9f)
                delta = -m_axisAverageDelta[index];
            else if (delta > 0)
                m_axisAverageDelta[index] = (m_axisAverageDelta[index] + delta) * 0.5f;

            m_axisPrevious[index] = value;
            m_nextDelta[input - ControllerInput.Laser0Axis] = delta;

            AxisChanged?.Invoke(input, delta);
        }
    }

    // TODO(local): handle alt buttons as well bc that's important
    public abstract class KeyboardControllerButtonOnly : Controller
    {
        private readonly Dictionary<KeyCode, ControllerInput> m_keyToControllerInput = new Dictionary<KeyCode, ControllerInput>();

        private readonly Dictionary<ControllerInput, List<KeyCode>> m_buttons = new Dictionary<ControllerInput, List<KeyCode>>();

        protected KeyboardControllerButtonOnly()
        {
            Keyboard.KeyPress += Keyboard_KeyPress;
            Keyboard.KeyRelease += Keyboard_KeyRelease;

            SetKeyCode(ControllerInput.Start, NscConfigKey.Key_Start, NscConfigKey.Key_StartAlt);
            SetKeyCode(ControllerInput.Back, NscConfigKey.Key_Back, NscConfigKey.Key_BackAlt);

            SetKeyCode(ControllerInput.BT0, NscConfigKey.Key_BT0, NscConfigKey.Key_BT0Alt);
            SetKeyCode(ControllerInput.BT1, NscConfigKey.Key_BT1, NscConfigKey.Key_BT1Alt);
            SetKeyCode(ControllerInput.BT2, NscConfigKey.Key_BT2, NscConfigKey.Key_BT2Alt);
            SetKeyCode(ControllerInput.BT3, NscConfigKey.Key_BT3, NscConfigKey.Key_BT3Alt);

            SetKeyCode(ControllerInput.FX0, NscConfigKey.Key_FX0, NscConfigKey.Key_FX0Alt);
            SetKeyCode(ControllerInput.FX1, NscConfigKey.Key_FX1, NscConfigKey.Key_FX1Alt);

            void SetKeyCode(ControllerInput input, NscConfigKey primaryKey, NscConfigKey? secondaryKey = null)
            {
                m_buttons[input] = new List<KeyCode>();

                m_keyToControllerInput[Plugin.Config.GetEnum<KeyCode>(primaryKey)] = input;
                if (secondaryKey is NscConfigKey s)
                    m_keyToControllerInput[Plugin.Config.GetEnum<KeyCode>(s)] = input;
            }
        }

        public override bool IsButtonDown(ControllerInput input) => m_buttons[input].Count > 0;

        protected override void DisposeManaged()
        {
            Keyboard.KeyPress -= Keyboard_KeyPress;
            Keyboard.KeyRelease -= Keyboard_KeyRelease;
        }

        private void Keyboard_KeyPress(KeyInfo info) => OnKeyPressed(info.KeyCode);

        private void Keyboard_KeyRelease(KeyInfo info) => OnKeyReleased(info.KeyCode);

        protected virtual void OnKeyPressed(KeyCode key)
        {
            if (m_keyToControllerInput.TryGetValue(key, out ControllerInput input))
            {
                var list = m_buttons[input];
                bool wasDown = list.Count > 0;
                if (!list.Contains(key))
                    list.Add(key);

                if (!wasDown && list.Count > 0)
                    ButtonPressed?.Invoke(input);
            }
        }

        protected virtual void OnKeyReleased(KeyCode key)
        {
            if (m_keyToControllerInput.TryGetValue(key, out ControllerInput input))
            {
                var list = m_buttons[input];
                bool wasDown = list.Count > 0;

                list.Remove(key);
                if (wasDown && list.Count == 0)
                    ButtonReleased?.Invoke(input);
            }
        }
    }

    public sealed class KeyboardController : KeyboardControllerButtonOnly
    {
        private readonly Dictionary<KeyCode, ControllerInput> m_keyToControllerInput = new Dictionary<KeyCode, ControllerInput>();
        private readonly Dictionary<ControllerInput, List<KeyCode>> m_directions = new Dictionary<ControllerInput, List<KeyCode>>();

        private readonly float m_sensitivity;
        private readonly float[] m_rawValues = new float[2];

        public KeyboardController()
        {
            m_sensitivity = Plugin.Config.GetFloat(NscConfigKey.Key_Sensitivity) * 4;

            SetKeyCode(ControllerInput.Laser0Negative, NscConfigKey.Key_Laser0Neg, NscConfigKey.Key_Laser0NegAlt);
            SetKeyCode(ControllerInput.Laser0Positive, NscConfigKey.Key_Laser0Pos, NscConfigKey.Key_Laser0PosAlt);

            SetKeyCode(ControllerInput.Laser1Negative, NscConfigKey.Key_Laser1Neg, NscConfigKey.Key_Laser1NegAlt);
            SetKeyCode(ControllerInput.Laser1Positive, NscConfigKey.Key_Laser1Pos, NscConfigKey.Key_Laser1PosAlt);

            void SetKeyCode(ControllerInput input, NscConfigKey primaryKey, NscConfigKey? secondaryKey = null)
            {
                m_directions[input] = new List<KeyCode>();

                m_keyToControllerInput[Plugin.Config.GetEnum<KeyCode>(primaryKey)] = input;
                if (secondaryKey is NscConfigKey s)
                    m_keyToControllerInput[Plugin.Config.GetEnum<KeyCode>(s)] = input;
            }
        }

        public override bool IsButtonDown(ControllerInput input)
        {
            return base.IsButtonDown(input);
        }
        public override float AxisDelta(ControllerInput input) => GetDelta(input - ControllerInput.Laser0Axis);
        public override float RawAxisValue(ControllerInput input) => m_rawValues[input == ControllerInput.Laser0Axis ? 0 : 1];

        private float GetDelta(int axis)
        {
            int dir = m_directions[ControllerInput.Laser0Positive + 2 * axis].Count
                    - m_directions[ControllerInput.Laser0Negative + 2 * axis].Count;
            return dir * m_sensitivity * Time.Delta;
        }

        public override void Update()
        {
            float dir0 = (m_directions[ControllerInput.Laser0Positive].Count
                     - m_directions[ControllerInput.Laser0Negative].Count)
                     * Time.Delta * m_sensitivity;
            float dir1 = (m_directions[ControllerInput.Laser1Positive].Count
                     - m_directions[ControllerInput.Laser1Negative].Count)
                     * Time.Delta * m_sensitivity;

            m_rawValues[0] += dir0;
            m_rawValues[1] += dir1;

            if (dir0 != 0) AxisChanged?.Invoke(ControllerInput.Laser0Axis, dir0);
            if (dir1 != 0) AxisChanged?.Invoke(ControllerInput.Laser1Axis, dir1);
        }

        protected override void OnKeyPressed(KeyCode key)
        {
            if (m_keyToControllerInput.TryGetValue(key, out ControllerInput input))
            {
                if (!m_directions[input].Contains(key))
                    m_directions[input].Add(key);
            }
            else base.OnKeyPressed(key);
        }

        protected override void OnKeyReleased(KeyCode key)
        {
            if (m_keyToControllerInput.TryGetValue(key, out ControllerInput input))
                m_directions[input].Remove(key);
            else base.OnKeyReleased(key);
        }
    }

    public sealed class KeyboardMouseController : KeyboardControllerButtonOnly
    {
        private readonly Dictionary<Axes, ControllerInput> m_mouseToControllerInput = new Dictionary<Axes, ControllerInput>();

        private readonly float m_sensitivity;
        private readonly float[] m_rawValues = new float[2];

        private readonly float[] m_currentDelta = new float[2], m_nextDelta = new float[2];

        public override float AxisDelta(ControllerInput input) => m_currentDelta[input - ControllerInput.Laser0Axis];
        public override float RawAxisValue(ControllerInput input) => m_rawValues[input == ControllerInput.Laser0Axis ? 0 : 1];

        public KeyboardMouseController()
        {
            Mouse.Move += Mouse_Move;

            m_sensitivity = Plugin.Config.GetFloat(NscConfigKey.Mouse_Sensitivity);

            m_mouseToControllerInput[Plugin.Config.GetEnum<Axes>(NscConfigKey.Mouse_Laser0Axis)] = ControllerInput.Laser0Axis;
            m_mouseToControllerInput[Plugin.Config.GetEnum<Axes>(NscConfigKey.Mouse_Laser1Axis)] = ControllerInput.Laser1Axis;
        }

        public override void Update()
        {
            for (int i = 0; i < 2; i++)
            {
                m_currentDelta[i] = m_nextDelta[i];
                m_nextDelta[i] = 0.0f;
            }
        }

        protected override void DisposeManaged()
        {
            base.DisposeManaged();
            Mouse.Move -= Mouse_Move;
        }

        private void Mouse_Move(int xDelta, int yDelta)
        {
            float amt = m_sensitivity * Time.Delta;
            if (xDelta != 0)
            {
                var inputKind = m_mouseToControllerInput[Axes.X];
                m_nextDelta[inputKind - ControllerInput.Laser0Axis] = xDelta * amt;
                m_rawValues[inputKind - ControllerInput.Laser0Axis] += xDelta * amt;
                AxisChanged?.Invoke(inputKind, xDelta * amt);
            }
            if (yDelta != 0)
            {
                var inputKind = m_mouseToControllerInput[Axes.Y];
                m_nextDelta[inputKind - ControllerInput.Laser0Axis] = yDelta * amt;
                m_rawValues[inputKind - ControllerInput.Laser0Axis] += yDelta * amt;
                AxisChanged?.Invoke(inputKind, yDelta * amt);
            }
        }
    }
}
