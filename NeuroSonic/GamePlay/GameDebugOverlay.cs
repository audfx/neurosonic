using System.Collections.Generic;
using System.Numerics;

using theori;
using theori.Gui;
using theori.Resources;

using NeuroSonic.GamePlay.Scoring;
using NeuroSonic.IO;

namespace NeuroSonic.GamePlay
{
    public sealed class GameDebugOverlay : Overlay
    {
        private ControllerVisualizer m_visualizer;
        private TimingBar m_timingBar;

        private readonly List<Sprite> m_streamActiveIndicators = new List<Sprite>();

        internal GameDebugOverlay(ClientResourceManager skin)
        {
            ForegroundGui = new Panel()
            {
                Children = new GuiElement[]
                {
                    m_visualizer = new ControllerVisualizer(skin),
                    m_timingBar = new TimingBar()
                    {
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.Both,

                        Position = new Vector2(0.3f, 0.95f),
                        Size = new Vector2(0.4f, 0.05f),
                    },
                }
            };

            var spriteContainer = new Panel()
            {
                RelativePositionAxes = Axes.X,
                Position = new Vector2(0.5f, 0),
            };
            ForegroundGui.AddChild(spriteContainer);

            for (int i = 0; i < 8; i++)
            {
                var sprite = new Sprite(null)
                {
                    Position = new Vector2(-4 * 50 - 5 + i * 50, 0),
                    Size = new Vector2(40),
                };
                spriteContainer.AddChild(sprite);
                m_streamActiveIndicators.Add(sprite);
            }
        }

        public void AddTimingInfo(time_t timingDelta, JudgeKind kind)
        {
            m_timingBar.AddTimingInfo(timingDelta, kind);
        }

        public void SetStreamActive(int stream, bool active)
        {
            m_streamActiveIndicators[stream].Color = active ? new Vector4(0, 1, 1, 1) : Vector4.One;
        }
    }

    public class ControllerVisualizer : Panel
    {
        class ButtonSprite : Panel
        {
            private Sprite m_inactive, m_active;

            public bool Active
            {
                set
                {
                    m_inactive.Color = value ? Vector4.Zero : Vector4.One;
                    m_active.Color = value ? Vector4.One : Vector4.Zero;
                }
            }

            public ButtonSprite(ClientResourceManager skin, string buttonName)
            {
                var itex = skin.AquireTexture($"textures/debug_{buttonName}");
                var atex = skin.AquireTexture($"textures/debug_{buttonName}_active");

                Children = new GuiElement[]
                {
                    m_inactive = new Sprite(itex)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = Vector2.One,
                        Color = Vector4.One,
                    },

                    m_active = new Sprite(atex)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = Vector2.One,
                        Color = new Vector4(1, 1, 0, 1),
                    },
                };

                Active = false;
            }
        }

        class KnobSprite : Panel
        {
            private Sprite m_sprite;

            public KnobSprite(ClientResourceManager skin)
            {
                var tex = skin.AquireTexture("textures/debug_vol");
                Children = new GuiElement[]
                {
                    m_sprite = new Sprite(tex)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = Vector2.One,
                        Color = Vector4.One,
                    }
                };
            }

            public override void Update()
            {
                base.Update();
                Origin = Size / 2;
            }
        }

        private readonly Dictionary<ControllerInput, ButtonSprite> m_bts = new Dictionary<ControllerInput, ButtonSprite>();
        private readonly Dictionary<ControllerInput, KnobSprite> m_knobs = new Dictionary<ControllerInput, KnobSprite>();

        public ControllerVisualizer(ClientResourceManager skin)
        {
            Size = new Vector2(230, 120);
            Children = new GuiElement[]
            {
                new Sprite(null)
                {
                    Color = Vector4.One * 0.5f,
                    RelativeSizeAxes = Axes.Both,
                    Size = Vector2.One,
                },

                m_bts[ControllerInput.Start] = new ButtonSprite(skin, "bt")
                {
                    Position = new Vector2(107, 10),
                    Size = new Vector2(16),
                },

                m_bts[ControllerInput.Back] = new ButtonSprite(skin, "bt")
                {
                    Position = new Vector2(107, 94),
                    Size = new Vector2(16),
                },

                m_bts[ControllerInput.BT0] = new ButtonSprite(skin, "bt")
                {
                    Position = new Vector2(40, 50),
                    Size = new Vector2(30),
                },

                m_bts[ControllerInput.BT1] = new ButtonSprite(skin, "bt")
                {
                    Position = new Vector2(80, 50),
                    Size = new Vector2(30),
                },

                m_bts[ControllerInput.BT2] = new ButtonSprite(skin, "bt")
                {
                    Position = new Vector2(120, 50),
                    Size = new Vector2(30),
                },

                m_bts[ControllerInput.BT3] = new ButtonSprite(skin, "bt")
                {
                    Position = new Vector2(160, 50),
                    Size = new Vector2(30),
                },

                m_bts[ControllerInput.FX0] = new ButtonSprite(skin, "fx")
                {
                    Position = new Vector2(55, 90),
                    Size = new Vector2(30, 15),
                },

                m_bts[ControllerInput.FX1] = new ButtonSprite(skin, "fx")
                {
                    Position = new Vector2(140, 90),
                    Size = new Vector2(30, 15),
                },

                m_knobs[ControllerInput.Laser0Axis] = new KnobSprite(skin)
                {
                    Position = new Vector2(20, 30),
                    Size = new Vector2(25),
                },

                m_knobs[ControllerInput.Laser1Axis] = new KnobSprite(skin)
                {
                    Position = new Vector2(210, 30),
                    Size = new Vector2(25),
                },
            };
        }

        public override void Update()
        {
            base.Update();

            foreach (var pair in m_bts)
                pair.Value.Active = Input.IsButtonDown(pair.Key);

            foreach (var pair in m_knobs)
                pair.Value.Rotation = 360 * (1 + Input.RawAxisValue(pair.Key)) * 0.5f;
        }
    }
    
    public class TimingBar : Panel
    {
        private readonly time_t m_inaccuracyWindow = 150.0 / 1000;

        private int m_numInputs;
        private time_t m_totalInaccuracy;

        private Sprite m_cursor;

        public TimingBar()
        {
            Children = new GuiElement[]
            {
                new Sprite(null)
                {
                    RelativePositionAxes = Axes.Both,
                    RelativeSizeAxes = Axes.Both,
                    Position = new Vector2(0, 0.25f),
                    Size = new Vector2(1.0f, 0.5f),
                    Color = Vector4.One,
                },

                new Sprite(null)
                {
                    RelativePositionAxes = Axes.Both,
                    RelativeSizeAxes = Axes.Y,
                    Origin = new Vector2(2, 0),
                    Position = new Vector2(0.5f, 0),
                    Size = new Vector2(4, 1.0f),
                    Color = new Vector4(0, 0, 0, 1),
                },

                m_cursor = new Sprite(null)
                {
                    RelativePositionAxes = Axes.Both,
                    RelativeSizeAxes = Axes.Y,
                    Origin = new Vector2(5, 0),
                    Size = new Vector2(10, 1.0f),
                    Color = new Vector4(0, 1, 0, 1),
                }
            };
        }

        public void AddTimingInfo(time_t timingDelta, JudgeKind kind)
        {
            m_numInputs++;
            m_totalInaccuracy += timingDelta;
        }

        public override void Update()
        {
            base.Update();

            if (m_numInputs == 0)
                m_cursor.Position = new Vector2(0.5f, 0);
            else
            {
                time_t inacc = m_totalInaccuracy / m_numInputs;
                float alpha = (float)(inacc / m_inaccuracyWindow);

                m_cursor.Position = new Vector2((1 + alpha) * 0.5f, 0);
            }
        }
    }
}
