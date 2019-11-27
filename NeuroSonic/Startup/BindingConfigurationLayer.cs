using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using NeuroSonic.Properties;
using theori;
using theori.Configuration;
using theori.Graphics;
using theori.Gui;
using theori.IO;

namespace NeuroSonic.Startup
{
#if false
    public sealed class ControllerConfigurationLayer : BaseMenuLayer//, IGamepadListener
    {
        class Bindable : Panel
        {
            public readonly ControllerInput Which;

            private readonly Sprite m_fill;
            private readonly TextLabel m_primaryLabel, m_secondaryLabel;

            public Vector4 FillColor { set => m_fill.Color = value; }
            public string PrimaryText { set => m_primaryLabel.Text = value; }
            public string SecondaryText { set { if (m_secondaryLabel != null) m_secondaryLabel.Text = value; } }

            public Bindable(string name, ControllerInput which, bool withSecondary)
            {
                Which = which;

                Children = new GuiElement[]
                {
                    m_fill = new Sprite(null)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = Vector2.One,
                    },

                    new TextLabel(Font.Default, 24, name)
                    {
                        RelativePositionAxes = Axes.Both,

                        Color = new Vector4(0, 0, 0, 1),
                        Position = new Vector2(0.5f, 0.25f),
                        TextAlignment = Anchor.MiddleCenter,
                    },

                    m_primaryLabel = new TextLabel(Font.Default, 16, "<?>")
                    {
                        RelativePositionAxes = Axes.Both,

                        Color = new Vector4(0, 0, 0, 1),
                        Position = new Vector2(0.5f, 0.5f),
                        TextAlignment = Anchor.MiddleCenter,
                    },
                };

                if (withSecondary)
                {
                    AddChild(m_secondaryLabel = new TextLabel(Font.Default, 16, "<?>")
                    {
                        RelativePositionAxes = Axes.Both,

                        Color = new Vector4(0, 0, 0, 1),
                        Position = new Vector2(0.5f, 0.75f),
                        TextAlignment = Anchor.MiddleCenter,
                    });
                }
            }
        }

        protected override string Title => Strings.SecretMenu_InputBindingConfigTitle;

        private bool m_isEditing = false;
        private Panel m_graphicPanel;

        private int m_bindableIndex = 0;
        private readonly List<Bindable> m_bindables = new List<Bindable>();
        // primary or secondary bindings
        private int m_codeIndex = -1;

        private readonly Dictionary<ControllerInput, (NscConfigKey, NscConfigKey?)> m_bindingIndices = new Dictionary<ControllerInput, (NscConfigKey, NscConfigKey?)>();

        private readonly InputDevice m_buttonInputDevice;
        private readonly InputDevice m_laserInputDevice;

        private readonly Vector4 m_selectedColor = new Vector4(1, 1, 0, 1);

        public ControllerConfigurationLayer()
        {
            m_buttonInputDevice = Plugin.Config.GetEnum<InputDevice>(NscConfigKey.ButtonInputDevice);
            m_laserInputDevice = Plugin.Config.GetEnum<InputDevice>(NscConfigKey.LaserInputDevice);

            if (m_buttonInputDevice == InputDevice.Keyboard)
            {
                m_bindingIndices[ControllerInput.Start] = (NscConfigKey.Key_Start, NscConfigKey.Key_StartAlt);
                m_bindingIndices[ControllerInput.Back] = (NscConfigKey.Key_Back, NscConfigKey.Key_BackAlt);

                m_bindingIndices[ControllerInput.BT0] = (NscConfigKey.Key_BT0, NscConfigKey.Key_BT0Alt);
                m_bindingIndices[ControllerInput.BT1] = (NscConfigKey.Key_BT1, NscConfigKey.Key_BT1Alt);
                m_bindingIndices[ControllerInput.BT2] = (NscConfigKey.Key_BT2, NscConfigKey.Key_BT2Alt);
                m_bindingIndices[ControllerInput.BT3] = (NscConfigKey.Key_BT3, NscConfigKey.Key_BT3Alt);

                m_bindingIndices[ControllerInput.FX0] = (NscConfigKey.Key_FX0, NscConfigKey.Key_FX0Alt);
                m_bindingIndices[ControllerInput.FX1] = (NscConfigKey.Key_FX1, NscConfigKey.Key_FX1Alt);
            }
            else // Controller
            {
                m_bindingIndices[ControllerInput.Start] = (NscConfigKey.Controller_Start, null);
                m_bindingIndices[ControllerInput.Back] = (NscConfigKey.Controller_Back, null);

                m_bindingIndices[ControllerInput.BT0] = (NscConfigKey.Controller_BT0, null);
                m_bindingIndices[ControllerInput.BT1] = (NscConfigKey.Controller_BT1, null);
                m_bindingIndices[ControllerInput.BT2] = (NscConfigKey.Controller_BT2, null);
                m_bindingIndices[ControllerInput.BT3] = (NscConfigKey.Controller_BT3, null);

                m_bindingIndices[ControllerInput.FX0] = (NscConfigKey.Controller_FX0, null);
                m_bindingIndices[ControllerInput.FX1] = (NscConfigKey.Controller_FX1, null);
            }

            if (m_laserInputDevice == InputDevice.Keyboard)
            {
                m_bindingIndices[ControllerInput.Laser0Negative] = (NscConfigKey.Key_Laser0Neg, NscConfigKey.Key_Laser0NegAlt);
                m_bindingIndices[ControllerInput.Laser0Positive] = (NscConfigKey.Key_Laser0Pos, NscConfigKey.Key_Laser0PosAlt);
                m_bindingIndices[ControllerInput.Laser1Negative] = (NscConfigKey.Key_Laser1Neg, NscConfigKey.Key_Laser1NegAlt);
                m_bindingIndices[ControllerInput.Laser1Positive] = (NscConfigKey.Key_Laser1Pos, NscConfigKey.Key_Laser1PosAlt);
            }
            else if (m_laserInputDevice == InputDevice.Mouse)
            {
                m_bindingIndices[ControllerInput.Laser0Axis] = (NscConfigKey.Mouse_Laser0Axis, null);
                m_bindingIndices[ControllerInput.Laser1Axis] = (NscConfigKey.Mouse_Laser1Axis, null);
            }
            else if (m_laserInputDevice == InputDevice.Controller)
            {
                m_bindingIndices[ControllerInput.Laser0Axis] = (NscConfigKey.Controller_Laser0Axis, null);
                m_bindingIndices[ControllerInput.Laser1Axis] = (NscConfigKey.Controller_Laser1Axis, null);
            }
        }

        protected override void GenerateMenuItems()
        {
            AddMenuItem(new MenuItem(NextOffset, Strings.SecretMenu_ConfigureControllerBindings, () =>
            {
                m_isEditing = true;
                m_bindables[m_bindableIndex = 0].FillColor = m_selectedColor;
            }));
            //AddMenuItem(new MenuItem(NextOffset, "Other Misc. Bindings", () => { }));
        }

        public override void Destroy()
        {
            base.Destroy();

            UserInputService.GamepadButtonPressed -= GamepadButtonPressed;
            UserInputService.GamepadAxisChanged -= GamepadAxisChanged;
        }

        public override void Initialize()
        {
            base.Initialize();

            UserInputService.GamepadButtonPressed += GamepadButtonPressed;
            UserInputService.GamepadAxisChanged += GamepadAxisChanged;

            Bindable start, back, bt0, bt1, bt2, bt3, fx0, fx1;

            Panel container;
            ForegroundGui!.AddChild(m_graphicPanel = new Panel()
            {
                Children = new GuiElement[]
                {
                    container = new Panel()
                    {
                        RelativeSizeAxes = Axes.Both,
                        RelativePositionAxes = Axes.Both,

                        Size = new Vector2(1.0f, 1 / 0.75f),
                        Position = new Vector2(0.0f, -0.25f),

                        Children = new GuiElement[]
                        {
                            start = new Bindable("Start", ControllerInput.Start, m_bindingIndices[ControllerInput.Start].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.45f, 0.2f),
                                Size = new Vector2(0.1f, 0.1f),
                            },

                            back = new Bindable("Back", ControllerInput.Back, m_bindingIndices[ControllerInput.Back].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.45f, 0.7f),
                                Size = new Vector2(0.1f, 0.1f),
                            },

                            bt0 = new Bindable("BT-A", ControllerInput.BT0, m_bindingIndices[ControllerInput.BT0].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.025f, 0.4f),
                                Size = new Vector2(0.2f, 0.2f),
                            },

                            bt1 = new Bindable("BT-B", ControllerInput.BT1, m_bindingIndices[ControllerInput.BT1].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.275f, 0.4f),
                                Size = new Vector2(0.2f, 0.2f),
                            },

                            bt2 = new Bindable("BT-C", ControllerInput.BT2, m_bindingIndices[ControllerInput.BT2].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.525f, 0.4f),
                                Size = new Vector2(0.2f, 0.2f),
                            },

                            bt3 = new Bindable("BT-D", ControllerInput.BT3, m_bindingIndices[ControllerInput.BT3].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.775f, 0.4f),
                                Size = new Vector2(0.2f, 0.2f),
                            },

                            fx0 = new Bindable("FX-L", ControllerInput.FX0, m_bindingIndices[ControllerInput.FX0].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.175f, 0.65f),
                                Size = new Vector2(0.175f, 0.1f),
                            },

                            fx1 = new Bindable("FX-R", ControllerInput.FX1, m_bindingIndices[ControllerInput.FX1].Item2 != null)
                            {
                                RelativeSizeAxes = Axes.Both,
                                RelativePositionAxes = Axes.Both,

                                Position = new Vector2(0.675f, 0.65f),
                                Size = new Vector2(0.175f, 0.1f),
                            },
                        }
                    },
                }
            });

            m_bindables.Add(start);

            if (m_laserInputDevice == InputDevice.Keyboard)
            {
                Bindable laser0neg = new Bindable("VOL-L(-)", ControllerInput.Laser0Negative, m_bindingIndices[ControllerInput.FX1].Item2 != null)
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,

                    Position = new Vector2(0.05f, 0.25f),
                    Size = new Vector2(0.1f),
                }, laser0pos = new Bindable("VOL-L(+)", ControllerInput.Laser0Positive, m_bindingIndices[ControllerInput.FX1].Item2 != null)
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,

                    Position = new Vector2(0.2f, 0.25f),
                    Size = new Vector2(0.1f),
                };

                container.AddChild(laser0neg);
                container.AddChild(laser0pos);

                Bindable laser1neg = new Bindable("VOL-R(-)", ControllerInput.Laser1Negative, m_bindingIndices[ControllerInput.Laser1Negative].Item2 != null)
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,

                    Position = new Vector2(0.7f, 0.25f),
                    Size = new Vector2(0.1f),
                }, laser1pos = new Bindable("VOL-R(+)", ControllerInput.Laser1Positive, m_bindingIndices[ControllerInput.Laser1Positive].Item2 != null)
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,

                    Position = new Vector2(0.85f, 0.25f),
                    Size = new Vector2(0.1f),
                };

                container.AddChild(laser1neg);
                container.AddChild(laser1pos);

                m_bindables.Add(laser0neg);
                m_bindables.Add(laser0pos);

                m_bindables.Add(laser1neg);
                m_bindables.Add(laser1pos);
            }
            else
            {
                Bindable laser0 = new Bindable("VOL-L", ControllerInput.Laser0Axis, m_bindingIndices[ControllerInput.Laser0Axis].Item2 != null)
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,

                    Position = new Vector2(0.2f, 0.25f),
                    Size = new Vector2(0.1f),
                }, laser1 = new Bindable("VOL-R", ControllerInput.Laser1Axis, m_bindingIndices[ControllerInput.Laser1Axis].Item2 != null)
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.Both,

                    Position = new Vector2(0.7f, 0.25f),
                    Size = new Vector2(0.1f),
                };

                container.AddChild(laser0);
                container.AddChild(laser1);

                m_bindables.Add(laser0);
                m_bindables.Add(laser1);
            }

            m_bindables.Add(bt0);
            m_bindables.Add(bt1);
            m_bindables.Add(bt2);
            m_bindables.Add(bt3);

            m_bindables.Add(fx0);
            m_bindables.Add(fx1);

            m_bindables.Add(back);

            foreach (var binding in m_bindables)
                UpdateBindableText(binding, m_bindingIndices[binding.Which]);

            ForegroundGui.AddChild(new Panel()
            {
                RelativeSizeAxes = Axes.X,
                RelativePositionAxes = Axes.Both,

                Position = new Vector2(0, 1),
                Size = new Vector2(1, 0),

                Children = new GuiElement[]
                {
                    new TextLabel(Font.Default, 16, Strings.SecretMenu_BindingChangePrimary)
                    {
                        TextAlignment = Anchor.BottomLeft,
                        Position = new Vector2(10, -40),
                    },

                    new Panel()
                    {
                        RelativePositionAxes = Axes.X,

                        Position = new Vector2(1, 0),

                        Children = new GuiElement[]
                        {
                            new TextLabel(Font.Default, 16, Strings.SecretMenu_BindingChangeSecondary)
                            {
                                TextAlignment = Anchor.BottomRight,
                                Position = new Vector2(-10, -40),
                            },
                        }
                    },
                }
            });
        }

        public override void KeyPressed(KeyInfo key)
        {
            // when NOT editing a config, do the base menu layer inputs
            if (!m_isEditing)
                base.KeyPressed(key);
            else
            {
                if (m_codeIndex == -1)
                {
                    switch (key.KeyCode)
                    {
                        case KeyCode.ESCAPE:
                            m_isEditing = false;
                            m_bindables[m_bindableIndex].FillColor = Vector4.One;
                            break;

                        case KeyCode.RETURN:
                            if ((key.Mods & KeyMod.CTRL) != 0)
                                m_codeIndex = 1;
                            else m_codeIndex = 0;
                            break;

                        case KeyCode.UP:
                        case KeyCode.LEFT:
                            m_bindables[m_bindableIndex].FillColor = Vector4.One;
                            m_bindableIndex = MathL.Max(0, m_bindableIndex - 1);
                            m_bindables[m_bindableIndex].FillColor = m_selectedColor;
                            break;

                        case KeyCode.DOWN:
                        case KeyCode.RIGHT:
                            m_bindables[m_bindableIndex].FillColor = Vector4.One;
                            m_bindableIndex = MathL.Min(m_bindables.Count - 1, m_bindableIndex + 1);
                            m_bindables[m_bindableIndex].FillColor = m_selectedColor;
                            break;
                    }
                }
                else
                {
                    switch (key.KeyCode)
                    {
                        case KeyCode.ESCAPE:
                        {
                            var binding = m_bindables[m_bindableIndex];
                            bool buttonBinding = (int)binding.Which < (int)ControllerInput.Laser0Axis;

                            var configKeys = m_bindingIndices[binding.Which];
                            var configKey = (NscConfigKey)(m_codeIndex == 0 ? configKeys.Item1 : configKeys.Item2);
                            m_codeIndex = -1;

                            if (buttonBinding)
                            {
                                if (m_buttonInputDevice == InputDevice.Keyboard)
                                    Plugin.Config.Set(configKey, KeyCode.UNKNOWN);
                                else // if (m_buttonInputDevice == InputDevice.Controller)
                                    Plugin.Config.Set(configKey, -1);
                                Plugin.SaveConfig();

                                Logger.Log($"Unbound { configKey } (from { binding.Which })");
                                UpdateBindableText(binding, configKeys);
                            }
                            else
                            {
                                if (m_laserInputDevice == InputDevice.Keyboard)
                                    Plugin.Config.Set(configKey, KeyCode.UNKNOWN);
                                else if (m_laserInputDevice == InputDevice.Mouse)
                                    Plugin.Config.Set(configKey, Axes.None);
                                else // if (m_buttonInputDevice == InputDevice.Controller)
                                    Plugin.Config.Set(configKey, -1);
                                Plugin.SaveConfig();

                                Logger.Log($"Unbound { configKey } (from { binding.Which })");
                                UpdateBindableText(binding, configKeys);
                            }
                        }
                        break;

                        default:
                        {
                            // check if the key is INVALID for use as a binding for keyboards, and ignore keys otherwise if setting for gamepad
                            var binding = m_bindables[m_bindableIndex];
                            bool buttonBinding = (int)binding.Which < (int)ControllerInput.Laser0Axis;

                            if (buttonBinding)
                            {
                                // consume buttons regardless
                                if (m_buttonInputDevice == InputDevice.Keyboard)
                                    SetKeyboardBinding();
                            }
                            else
                            {
                                if (m_laserInputDevice == InputDevice.Keyboard)
                                {
                                    Debug.Assert(binding.Which != ControllerInput.Laser0Axis && binding.Which != ControllerInput.Laser1Axis, "Axes not supported for keys bro");
                                    SetKeyboardBinding();
                                }
                                else if (m_laserInputDevice == InputDevice.Mouse)
                                {
                                    switch (key.KeyCode)
                                    {
                                        case KeyCode.X:
                                        case KeyCode.D0:
                                        case KeyCode.KP_0:
                                        case KeyCode.H:
                                            SetMouseAxisBinding(Axes.X);
                                            break;

                                        case KeyCode.Y:
                                        case KeyCode.D1:
                                        case KeyCode.KP_1:
                                        case KeyCode.V:
                                            SetMouseAxisBinding(Axes.Y);
                                            break;

                                        default: break;
                                    }
                                }
                            }

                            // both will do the same thing, but require different checks. this is fine for now.
                            void SetKeyboardBinding()
                            {
                                var configKeys = m_bindingIndices[binding.Which];
                                var configKey = (NscConfigKey)(m_codeIndex == 0 ? configKeys.Item1 : configKeys.Item2);

                                Plugin.Config.Set(configKey, key.KeyCode);
                                Plugin.SaveConfig();

                                Logger.Log($"Set Key for { configKey } (from { binding.Which }) to { key.KeyCode }");
                                UpdateBindableText(binding, configKeys);

                                m_codeIndex = -1; // when we set the binding, exit binding config
                            }

                            void SetMouseAxisBinding(Axes axis)
                            {
                                var configKeys = m_bindingIndices[binding.Which];
                                var configKey = (NscConfigKey)(m_codeIndex == 0 ? configKeys.Item1 : configKeys.Item2);

                                Plugin.Config.Set(configKey, axis);
                                Plugin.SaveConfig();

                                Logger.Log($"Set Mouse Axis for { configKey } (from { binding.Which }) to Axis { axis }");
                                UpdateBindableText(binding, configKeys);

                                m_codeIndex = -1; // when we set the binding, exit binding config
                            }
                        }
                        break; // consume anyway
                    }
                }
            }
        }

        public void GamepadButtonPressed(GamepadButtonInfo info)
        {
            // TODO(local): relic?
            if (info.Gamepad.DeviceIndex != Plugin.Gamepad.DeviceIndex) return;
            if (!m_isEditing) return;

            if (m_codeIndex == -1)
            {
            }
            else
            {
                var binding = m_bindables[m_bindableIndex];
                bool buttonBinding = (int)binding.Which < (int)ControllerInput.Laser0Axis;

                // buttons on controllers currently don't work for axes sorry,
                //  if needed will add but uhhh I've only seen one controller use
                //  buttons for lasers and IDEK if that was gamepad mode or keyboard mode sooo.
                if (!buttonBinding) return;
                // also duh we have to be in controller mode
                if (m_buttonInputDevice != InputDevice.Controller) return;

                var configKeys = m_bindingIndices[binding.Which];
                var configKey = (NscConfigKey)(m_codeIndex == 0 ? configKeys.Item1 : configKeys.Item2);

                Plugin.Config.Set(configKey, (int)info.Button);
                Plugin.SaveConfig();

                Logger.Log($"Set Button for { configKey } (from { binding.Which }) to { info.Button }");
                UpdateBindableText(binding, configKeys);

                m_codeIndex = -1; // when we set the binding, exit binding config
            }
        }

        public void GamepadAxisChanged(GamepadAxisInfo info)
        {
            if (!m_isEditing) return;

            if (m_codeIndex == -1)
            {
            }
            else
            {
                var binding = m_bindables[m_bindableIndex];
                bool buttonBinding = (int)binding.Which < (int)ControllerInput.Laser0Axis;

                // kill for analog devices on button settings duh
                if (buttonBinding) return;
                // and second duh, controller mode
                if (m_laserInputDevice != InputDevice.Controller) return;

                var configKeys = m_bindingIndices[binding.Which];
                var configKey = (NscConfigKey)(m_codeIndex == 0 ? configKeys.Item1 : configKeys.Item2);

                Plugin.Config.Set(configKey, (int)info.Axis);
                Plugin.SaveConfig();

                Logger.Log($"Set Button for { configKey } (from { binding.Which }) to { info.Axis }");
                UpdateBindableText(binding, configKeys);

                m_codeIndex = -1; // when we set the binding, exit binding config
            }

            return;
        }

        private void UpdateBindableText(Bindable b, (NscConfigKey Primary, NscConfigKey? Secondary) keys)
        {
            if (keys.Secondary is NscConfigKey secondary)
                b.SecondaryText = Plugin.Config.GetAsStringImage(secondary);
            b.PrimaryText = Plugin.Config.GetAsStringImage(keys.Primary);
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            int size = (int)MathL.Max(Window.Width * 0.6f, Window.Height * 0.6f);
            m_graphicPanel.Size = new Vector2(size, size * 0.75f);
            m_graphicPanel.Position = new Vector2((Window.Width - size) / 2, Window.Height - size * 0.75f);
        }
    }
#endif
}
