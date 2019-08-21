using System;
using System.Collections.Generic;
using System.Numerics;
using NeuroSonic.Properties;
using theori;
using theori.Graphics;
using theori.Gui;
using theori.IO;

namespace NeuroSonic.Startup
{
    public class MenuItem : TextLabel
    {
        public const int SPACING = 35;

        public Action Action;

        public bool Hilited
        {
            set
            {
                if (value)
                    Color = new Vector4(1, 1, 0, 1);
                else Color = Vector4.One;
            }
        }

        public MenuItem(int i, string text, Action action)
            : base(Font.Default, 24, text)
        {
            Action = action;
            Position = new Vector2(0, SPACING * i);
        }
    }

    public abstract class BaseMenuLayer : NscLayer
    {
        protected int ItemIndex { get; private set; }
        private readonly List<MenuItem> m_items = new List<MenuItem>();

        private int m_extraSpacing = 0;

        protected int NextOffset => m_items.Count + m_extraSpacing;

        protected abstract string Title { get; }

        public override void Init()
        {
            base.Init();

            GenerateMenuItems();

            ForegroundGui = new Panel()
            {
                Children = new GuiElement[]
                {
                    new TextLabel(Font.Default, 32, Title)
                    {
                        RelativePositionAxes = Axes.X,
                        Position = new Vector2(0.5f, 20),
                        TextAlignment = Anchor.TopCenter,
                    },

                    new Panel()
                    {
                        Position = new Vector2(40, 100),

                        Children = m_items
                    },

                    new Panel()
                    {
                        RelativeSizeAxes = Axes.X,
                        RelativePositionAxes = Axes.Both,

                        Position = new Vector2(0, 1),
                        Size = new Vector2(1, 0),

                        Children = new GuiElement[]
                        {
                            new TextLabel(Font.Default, 16, Strings.SecretMenu_UpDownNavHint)
                            {
                                TextAlignment = Anchor.BottomLeft,
                                Position = new Vector2(10, -10),
                            },

                            new Panel()
                            {
                                RelativePositionAxes = Axes.X,

                                Position = new Vector2(1, 0),

                                Children = new GuiElement[]
                                {
                                    new TextLabel(Font.Default, 16, Strings.SecretMenu_SelectHint)
                                    {
                                        TextAlignment = Anchor.BottomRight,
                                        Position = new Vector2(-10, -10),
                                    },
                                }
                            },
                        }
                    }
                }
            };

            m_items[ItemIndex].Hilited = true;
        }

        protected abstract void GenerateMenuItems();

        protected void AddMenuItem(MenuItem item) => m_items.Add(item);
        protected void AddSpacing() => m_extraSpacing++;

        protected void NavigateUp()
        {
            m_items[ItemIndex].Hilited = false;
            ItemIndex = (ItemIndex - 1 + m_items.Count) % m_items.Count;
            m_items[ItemIndex].Hilited = true;
        }

        protected void NavigateDown()
        {
            m_items[ItemIndex].Hilited = false;
            ItemIndex = (ItemIndex + 1 + m_items.Count) % m_items.Count;
            m_items[ItemIndex].Hilited = true;
        }

        public override bool KeyPressed(KeyInfo key)
        {
            if (IsSuspended) return false;

            switch (key.KeyCode)
            {
                case KeyCode.ESCAPE: OnExit(); break;

                case KeyCode.UP: NavigateUp(); break;
                case KeyCode.DOWN: NavigateDown(); break;

                case KeyCode.RETURN: m_items[ItemIndex].Action?.Invoke(); break;

                // stick our false thing here, returning true is the default for handled keys
                default:
                    if (Plugin.Config.GetEnum<InputDevice>(NscConfigKey.ButtonInputDevice) != InputDevice.Keyboard) return false;

                    if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_Back))
                    {
                        OnExit();
                        return true;
                    }

                    if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_BT0))
                    {
                        NavigateUp();
                        return true;
                    }

                    if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_BT1))
                    {
                        NavigateDown();
                        return true;
                    }

                    if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_Start))
                    {
                        m_items[ItemIndex].Action?.Invoke();
                        return true;
                    }

                    return false;
            }

            return true;
        }

        public override bool ControllerButtonPressed(ControllerInput input)
        {
            switch (input)
            {
                case ControllerInput.Start: m_items[ItemIndex].Action?.Invoke(); break;
                case ControllerInput.Back: OnExit(); break;

                case ControllerInput.BT0: NavigateUp(); break;
                case ControllerInput.BT1: NavigateDown(); break;

                default: return false;
            }

            return true;
        }

        protected virtual void OnExit() => Host.PopToParent(this);
    }
}
