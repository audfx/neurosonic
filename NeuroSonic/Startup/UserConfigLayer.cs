using System;
using System.Numerics;

using NeuroSonic.IO;
using NeuroSonic.Properties;

using theori;
using theori.Graphics;
using theori.Gui;
using theori.IO;

namespace NeuroSonic.Startup
{
    public class UserConfigLayer : BaseMenuLayer
    {
        class Nav
        {
            public Direction2D Dir;
            public bool Modified;

            public bool Left => ((int)Dir & 0x03) == (int)Direction2D.Left;
            public bool Right => ((int)Dir & 0x03) == (int)Direction2D.Right;
            public bool Up => ((int)Dir & 0x30) == (int)Direction2D.Up;
            public bool Down => ((int)Dir & 0x30) == (int)Direction2D.Down;

            public Nav(KeyInfo key)
            {
                Dir = Direction2D.None;
                switch (key.KeyCode)
                {
                    case KeyCode.LEFT: Dir |= Direction2D.Left; break;
                    case KeyCode.RIGHT: Dir |= Direction2D.Right; break;
                    case KeyCode.UP: Dir |= Direction2D.Up; break;
                    case KeyCode.DOWN: Dir |= Direction2D.Down; break;
                }

                Modified = key.Mods.HasFlag(KeyMod.LCTRL) || key.Mods.HasFlag(KeyMod.RCTRL) || key.Mods.HasFlag(KeyMod.CTRL);
            }

            public Nav(ControllerInput input, bool mod)
            {
                Dir = Direction2D.None;
                switch (input)
                {
                    case ControllerInput.BT0: Dir |= Direction2D.Up; break;
                    case ControllerInput.BT1: Dir |= Direction2D.Down; break;
                    case ControllerInput.BT2: Dir |= Direction2D.Left; break;
                    case ControllerInput.BT3: Dir |= Direction2D.Right; break;
                }

                Modified = mod;
            }
        }

        protected override string Title => Strings.SecretMenu_UserConfigTitle;

        private int m_hsKindIndex, m_hsValueIndex;
        private int m_voIndex, m_ioIndex;
        private int m_llHueIndex, m_rlHueIndex;

        private Action<Nav>[] m_configActions;

        private HiSpeedMod m_hiSpeedKind;
        private TextLabel[] m_hsKinds;

        private float m_hiSpeed, m_modSpeed;
        private TextLabel m_hs;

        private int m_voffValue;
        private TextLabel m_voff;

        private int m_ioffValue;
        private TextLabel m_ioff;

        private int m_llHueValue;
        private Sprite m_llHueSprite;
        private TextLabel m_llHue;

        private int m_rlHueValue;
        private Sprite m_rlHueSprite;
        private TextLabel m_rlHue;

        private int m_maxConfigIndex;
        private int m_activeIndex = -1;

        protected override void GenerateMenuItems()
        {
            AddMenuItem(new MenuItem(m_hsKindIndex = NextOffset, Strings.Term_HiSpeedKind, null));
            AddMenuItem(new MenuItem(m_hsValueIndex = NextOffset, Strings.Term_HiSpeedValue, null));
            AddMenuItem(new MenuItem(m_voIndex = NextOffset, Strings.Term_VideoOffset, null));
            AddMenuItem(new MenuItem(m_ioIndex = NextOffset, Strings.Term_InputOffset, null));
            AddMenuItem(new MenuItem(m_llHueIndex = NextOffset, Strings.Term_Gameplay_LeftLaserHue, null));
            AddMenuItem(new MenuItem(m_rlHueIndex = NextOffset, Strings.Term_Gameplay_RightLaserHue, null));
            m_maxConfigIndex = NextOffset;
            AddSpacing();
            AddMenuItem(new MenuItem(NextOffset, "Timing Calibration", OpenCalibration));
        }

        public override void Destroy()
        {
            base.Destroy();

            Plugin.SaveNscConfig();
        }

        private void OpenCalibration()
        {
            Host.PushLayer(new CalibrationLayer());
        }

        public override void Resumed()
        {
            base.Resumed();

            m_voffValue = Plugin.Config.GetInt(NscConfigKey.VideoOffset);
            m_ioffValue = Plugin.Config.GetInt(NscConfigKey.InputOffset);
        }

        public override void Init()
        {
            base.Init();

            const int ALIGN = 300;
            const int SPACING = MenuItem.SPACING;

            m_hiSpeedKind = Plugin.Config.GetEnum<HiSpeedMod>(NscConfigKey.HiSpeedModKind);
            m_hiSpeed = Plugin.Config.GetFloat(NscConfigKey.HiSpeed);
            m_modSpeed = Plugin.Config.GetFloat(NscConfigKey.ModSpeed);
            m_voffValue = Plugin.Config.GetInt(NscConfigKey.VideoOffset);
            m_ioffValue = Plugin.Config.GetInt(NscConfigKey.InputOffset);
            m_llHueValue = Plugin.Config.GetInt(NscConfigKey.Laser0Color);
            m_rlHueValue = Plugin.Config.GetInt(NscConfigKey.Laser1Color);

            ForegroundGui.AddChild(new Panel()
            {
                Position = new Vector2(ALIGN, 100),

                Children = new GuiElement[]
                {
                    new Panel()
                    {
                        Position = new Vector2(0, SPACING * m_hsKindIndex),
                        Children = m_hsKinds = new TextLabel[]
                        {
                            new TextLabel(Font.Default, 24, Strings.Term_HiSpeedKind_Multiplier)   { Position = new Vector2(0, 0) },
                            new TextLabel(Font.Default, 24, Strings.Term_HiSpeedKind_ModeMod)     { Position = new Vector2(150, 0) },
                            new TextLabel(Font.Default, 24, Strings.Term_HiSpeedKind_ConstantMod) { Position = new Vector2(300, 0) },
                        }
                    },

                    new Panel()
                    {
                        Position = new Vector2(0, SPACING * m_hsValueIndex),
                        Children = new TextLabel[]
                        {
                            m_hs = new TextLabel(Font.Default, 24, "0")   { Position = new Vector2(0, 0) },
                        }
                    },

                    new Panel()
                    {
                        Position = new Vector2(0, SPACING * m_voIndex),
                        Children = new TextLabel[]
                        {
                            m_voff = new TextLabel(Font.Default, 24, "0")   { Position = new Vector2(0, 0) },
                        }
                    },

                    new Panel()
                    {
                        Position = new Vector2(0, SPACING * m_ioIndex),
                        Children = new TextLabel[]
                        {
                            m_ioff = new TextLabel(Font.Default, 24, "0")   { Position = new Vector2(0, 0) },
                        }
                    },

                    new Panel()
                    {
                        Position = new Vector2(0, SPACING * m_llHueIndex),
                        Children = new GuiElement[]
                        {
                            m_llHueSprite = new Sprite(null) { Size = new Vector2(SPACING - 10) },
                            m_llHue = new TextLabel(Font.Default, 24, "0")   { Position = new Vector2(SPACING, 0) },
                        }
                    },

                    new Panel()
                    {
                        Position = new Vector2(0, SPACING * m_rlHueIndex),
                        Children = new GuiElement[]
                        {
                            m_rlHueSprite = new Sprite(null) { Size = new Vector2(SPACING - 10) },
                            m_rlHue = new TextLabel(Font.Default, 24, "0")   { Position = new Vector2(SPACING, 0) },
                        }
                    },
                }
            });

            m_configActions = new Action<Nav>[]
            {
                KeyPressed_HiSpeedKind,
                KeyPressed_HiSpeed,
                KeyPressed_VideoOffset,
                KeyPressed_InputOffset,
                KeyPressed_LeftLaserHue,
                KeyPressed_RightLaserHue,
            };
        }

        private void KeyPressed_HiSpeedKind(Nav key)
        {
            if (!key.Left && !key.Right) return;
            int dir = key.Left ? -1 : 1;

            m_hiSpeedKind = (HiSpeedMod)((int)(m_hiSpeedKind + dir + 3) % 3);
            Plugin.Config.Set(NscConfigKey.HiSpeedModKind, m_hiSpeedKind);
        }

        private void KeyPressed_HiSpeed(Nav key)
        {
            if (!key.Left && !key.Right) return;
            int dir = key.Left ? -1 : 1;

            bool ctrl = key.Modified;

            switch (m_hiSpeedKind)
            {
                case HiSpeedMod.Default:
                {
                    float amt = ctrl ? 1 : 5;
                    m_hiSpeed = MathL.Round(((m_hiSpeed * 10) + dir * amt) / amt) * amt / 10;
                    Plugin.Config.Set(NscConfigKey.HiSpeed, m_hiSpeed);
                } break;

                case HiSpeedMod.MMod:
                case HiSpeedMod.CMod:
                {
                    float amt = ctrl ? 5 : 25;
                    m_modSpeed = MathL.Round((m_modSpeed + dir * amt) / amt) * amt;
                    Plugin.Config.Set(NscConfigKey.ModSpeed, m_modSpeed);
                } break;
            }
        }

        private void KeyPressed_VideoOffset(Nav key)
        {
            if (!key.Left && !key.Right) return;
            int dir = key.Left ? -1 : 1;

            bool ctrl = key.Modified;
            int amt = ctrl ? 1 : 5;

            m_voffValue += dir * amt;
            Plugin.Config.Set(NscConfigKey.VideoOffset, m_voffValue);
        }

        private void KeyPressed_InputOffset(Nav key)
        {
            if (!key.Left && !key.Right) return;
            int dir = key.Left ? -1 : 1;

            bool ctrl = key.Modified;
            int amt = ctrl ? 1 : 5;

            m_ioffValue += dir * amt;
            Plugin.Config.Set(NscConfigKey.InputOffset, m_ioffValue);
        }

        private void KeyPressed_LeftLaserHue(Nav key)
        {
            if (!key.Left && !key.Right) return;
            int dir = key.Left ? -1 : 1;

            bool ctrl = key.Modified;
            int amt = ctrl ? 5 : 30;

            m_llHueValue = (m_llHueValue + dir * amt + 360) % 360;
            Plugin.Config.Set(NscConfigKey.Laser0Color, m_llHueValue);
        }

        private void KeyPressed_RightLaserHue(Nav key)
        {
            if (!key.Left && !key.Right) return;
            int dir = key.Left ? -1 : 1;

            bool ctrl = key.Modified;
            int amt = ctrl ? 5 : 30;

            m_rlHueValue = (m_rlHueValue + dir * amt + 360) % 360;
            Plugin.Config.Set(NscConfigKey.Laser1Color, m_rlHueValue);
        }

        private void UpdateHiSpeedKinds()
        {
            bool active = m_activeIndex == m_hsKindIndex;

            Vector4 nCol = active ? new Vector4(0.5f, 0.5f, 0.5f, 1) : new Vector4(0.25f, 0.25f, 0.25f, 1.0f);
            Vector4 aCol = active ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1.0f);

            for (int i = 0; i < m_hsKinds.Length; i++)
            {
                if (i == (int)m_hiSpeedKind)
                    m_hsKinds[i].Color = aCol;
                else m_hsKinds[i].Color = nCol;
            }
        }

        private void UpdateHiSpeed()
        {
            bool active = m_activeIndex == m_hsValueIndex;
            m_hs.Color = active ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1);

            switch (m_hiSpeedKind)
            {
                case HiSpeedMod.Default: m_hs.Text = $"{m_hiSpeed:F1}"; break;

                case HiSpeedMod.MMod:
                case HiSpeedMod.CMod: m_hs.Text = $"{(int)m_modSpeed}"; break;
            }
        }

        private void UpdateVideoOffset()
        {
            bool active = m_activeIndex == m_voIndex;
            m_voff.Color = active ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1);
            m_voff.Text = $"{m_voffValue}";
        }

        private void UpdateInputOffset()
        {
            bool active = m_activeIndex == m_ioIndex;
            m_ioff.Color = active ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1);
            m_ioff.Text = $"{m_ioffValue}";
        }

        private void UpdateLeftLaserHue()
        {
            bool active = m_activeIndex == m_llHueIndex;
            var color = new Vector4(Color.HSVtoRGB(new Vector3(m_llHueValue / 360.0f, 1, active ? 1 : 0.5f)), 1);
            m_llHueSprite.Color = color;
            m_llHue.Color = active ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1);
            m_llHue.Text = $"{m_llHueValue}";
        }

        private void UpdateRightLaserHue()
        {
            bool active = m_activeIndex == m_rlHueIndex;
            var color = new Vector4(Color.HSVtoRGB(new Vector3(m_rlHueValue / 360.0f, 1, active ? 1 : 0.5f)), 1);
            m_rlHueSprite.Color = color;
            m_rlHue.Color = active ? new Vector4(1, 1, 1, 1) : new Vector4(0.5f, 0.5f, 0.5f, 1);
            m_rlHue.Text = $"{m_rlHueValue}";
        }

        public override bool KeyPressed(KeyInfo key)
        {
            if (m_activeIndex >= 0)
            {
                switch (key.KeyCode)
                {
                    case KeyCode.RETURN:
                    case KeyCode.ESCAPE: m_activeIndex = -1; break;

                    default: m_configActions[m_activeIndex](new Nav(key)); break;
                }

                return true;
            }
            else if (ItemIndex < m_maxConfigIndex)
            {
                if (key.KeyCode == KeyCode.RETURN)
                {
                    m_activeIndex = ItemIndex;
                    return true;
                }
            }

            return base.KeyPressed(key);
        }

        protected internal override bool ControllerButtonPressed(ControllerInput input)
        {
            if (m_activeIndex >= 0)
            {
                switch (input)
                {
                    case ControllerInput.Start:
                    case ControllerInput.Back: m_activeIndex = -1; break;

                    default: m_configActions[m_activeIndex](new Nav(input, Input.IsButtonDown(ControllerInput.FX0))); break;
                }

                return true;
            }
            else if (ItemIndex < m_maxConfigIndex)
            {
                if (input == ControllerInput.Start)
                {
                    m_activeIndex = ItemIndex;
                    return true;
                }
            }

            return base.ControllerButtonPressed(input);
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            UpdateHiSpeedKinds();
            UpdateHiSpeed();
            UpdateVideoOffset();
            UpdateInputOffset();
            UpdateLeftLaserHue();
            UpdateRightLaserHue();
        }
    }
}
