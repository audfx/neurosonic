using System;
using System.Collections.Generic;
using System.Numerics;
using NeuroSonic.Platform;
using NeuroSonic.Properties;
using theori;
using theori.Graphics;
using theori.Gui;
using theori.IO;

namespace NeuroSonic.Startup
{
#if false
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

        public override void Initialize()
        {
            base.Initialize();

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

            ClientAs<NscClient>().OpenCurtain();
        }

        public override void Resumed(Layer previousLayer)
        {
            base.Resumed(previousLayer);

            ClientAs<NscClient>().OpenCurtain();
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

        public override void KeyPressed(KeyInfo key)
        {
            if (IsSuspended) return;

            switch (key.KeyCode)
            {
                case KeyCode.ESCAPE: OnExit(); break;

                case KeyCode.UP: NavigateUp(); break;
                case KeyCode.DOWN: NavigateDown(); break;

                case KeyCode.RETURN: m_items[ItemIndex].Action?.Invoke(); break;

                // stick our false thing here, returning true is the default for handled keys
                default:
                    if (NscConfig.ButtonInputDevice != InputDevice.Keyboard) return;

                    if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_Back))
                        OnExit();
                    else if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_BT0))
                        NavigateUp();
                    else if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_BT1))
                        NavigateDown();
                    else if (key.KeyCode == Plugin.Config.GetEnum<KeyCode>(NscConfigKey.Key_Start))
                        m_items[ItemIndex].Action?.Invoke();

                    break;
            }
        }

        public override void ControllerButtonPressed(ControllerButtonInfo info)
        {
                 if (info.Button == "Start") m_items[ItemIndex].Action?.Invoke();
            else if (info.Button == "Back") OnExit();
            else if (info.Button == 0) NavigateUp();
            else if (info.Button == 1) NavigateDown();
        }

        protected virtual void OnExit() => Pop();
    }
#endif
}
