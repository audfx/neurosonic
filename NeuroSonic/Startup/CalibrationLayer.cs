using System;
using System.Numerics;
using NeuroSonic.Properties;
using theori;
using theori.Audio;
using theori.Graphics;
using theori.Gui;
using theori.IO;

namespace NeuroSonic.Startup
{
#if false
    public class CalibrationLayer : NscLayer
    {
        class ClickTrack : AudioSource
        {
            private const double CLICK_DURATION_SECONDS = 0.075;

            public override bool CanSeek => false;

            private readonly int m_sampleRate;
            public override int SampleRate => m_sampleRate;

            private readonly int m_channels;
            public override int Channels => m_channels;

            private time_t m_position;
            public override time_t Position { get => m_position; set => throw new NotImplementedException(); }

            public override time_t Length => throw new NotImplementedException();

            public time_t BeatDuration { get; }

            private readonly double m_frequency;
            private readonly double m_samplesPerBeat, m_clickDurationSamples;
            private long m_singleChannelSampleIndex = 0;

            public bool Silenced = false;

            public ClickTrack(int sampleRate, int channels, double frequency, double beatsPerMinute)
            {
                m_sampleRate = sampleRate;
                m_channels = channels;
                m_frequency = frequency;

                BeatDuration = 60 / beatsPerMinute;

                m_samplesPerBeat = 60 * sampleRate / beatsPerMinute;
                m_clickDurationSamples = sampleRate * CLICK_DURATION_SECONDS;
            }

            public override int Read(Span<float> buffer)
            {
                int sampleRate = m_sampleRate, channels = m_channels;
                m_position = time_t.FromSeconds(m_singleChannelSampleIndex / (double)sampleRate);

                double vol = Volume;
                double attack = 0.0005 * sampleRate;

                int numSamples = buffer.Length / channels;
                if (Silenced)
                {
                    for (int i = 0; i < numSamples; i++)
                        buffer[i * channels] = buffer[i * channels + 1] = 0;
                    goto end;
                }

                // do this kinda naive first
                for (int i = 0; i < numSamples; i++)
                {
                    long sampleIndex = m_singleChannelSampleIndex + i;
                    double relativeSample = sampleIndex % m_samplesPerBeat;

                    double amp;
                    if (relativeSample < attack)
                        amp = vol * (relativeSample / attack);
                    else amp = vol * Math.Max(0, 1 - ((relativeSample - attack) / (m_clickDurationSamples - attack)));

                    double value = 0;
                    if (amp > 0)
                        value = MathL.Sin(sampleIndex * m_frequency * MathL.TwoPi / m_sampleRate);

                    buffer[i * channels + 0] = (float)(value * amp);
                    buffer[i * channels + 1] = (float)(value * amp);
                }

            end:
                m_singleChannelSampleIndex += numSamples;
                return buffer.Length;
            }

            public override void Seek(time_t position) => throw new NotImplementedException();
        }

        private string Title => "Calibration";

        private ClickTrack m_click;
        private BasicSpriteRenderer m_renderer;

        private TextLabel m_inputLabel, m_videoLabel;
        private TextLabel m_inputValueLabel, m_videoValueLabel;

        private bool m_calcInputOffset = true;

        private time_t m_totalInputInacc, m_totalVideoInacc;
        private int m_inputInaccCount, m_videoInaccCount;

        private int InputOffset => m_inputInaccCount == 0 ? 0 : MathL.RoundToInt(m_totalInputInacc.Seconds * 1000 / m_inputInaccCount);
        private int VideoOffset => m_videoInaccCount == 0 ? 0 : MathL.RoundToInt(m_totalVideoInacc.Seconds * 1000 / m_videoInaccCount);

        public override void Destroy()
        {
            base.Destroy();

            m_click.Channel = null;
            m_click = null;

            m_renderer.Dispose();
            m_renderer = null;
        }

        public override void Initialize()
        {
            base.Initialize();

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

                    m_inputLabel = new TextLabel(Font.Default, 24, "Input Offset")
                    {
                        RelativePositionAxes = Axes.X,
                        Position = new Vector2(0.25f, 20),
                        TextAlignment = Anchor.TopCenter,
                    },

                    m_videoLabel = new TextLabel(Font.Default, 24, "Video Offset")
                    {
                        RelativePositionAxes = Axes.X,
                        Position = new Vector2(0.75f, 20),
                        TextAlignment = Anchor.TopCenter,
                    },

                    m_inputValueLabel = new TextLabel(Font.Default, 24, "0")
                    {
                        RelativePositionAxes = Axes.X,
                        Position = new Vector2(0.25f, 50),
                        TextAlignment = Anchor.TopCenter,
                    },

                    m_videoValueLabel = new TextLabel(Font.Default, 24, "0")
                    {
                        RelativePositionAxes = Axes.X,
                        Position = new Vector2(0.75f, 50),
                        TextAlignment = Anchor.TopCenter,
                    },

                    new Panel()
                    {
                        RelativeSizeAxes = Axes.X,
                        RelativePositionAxes = Axes.Both,

                        Position = new Vector2(0, 1),
                        Size = new Vector2(1, 0),

                        Children = new GuiElement[]
                        {
                            new TextLabel(Font.Default, 16, "[Left / FX-L] Select Input Offset")
                            {
                                TextAlignment = Anchor.BottomLeft,
                                Position = new Vector2(10, -10),
                            },

                            new TextLabel(Font.Default, 16, "Press the Spacebar or any BT to either clicks or the scrolling bars")
                            {
                                RelativePositionAxes = Axes.X,
                                TextAlignment = Anchor.BottomCenter,
                                Position = new Vector2(0.5f, -70),
                            },

                            new TextLabel(Font.Default, 16, "Press the Enter or START to save the values to your config")
                            {
                                RelativePositionAxes = Axes.X,
                                TextAlignment = Anchor.BottomCenter,
                                Position = new Vector2(0.5f, -40),
                            },

                            new Panel()
                            {
                                RelativePositionAxes = Axes.X,

                                Position = new Vector2(1, 0),

                                Children = new GuiElement[]
                                {
                                    new TextLabel(Font.Default, 16, "Select Video Offset [Right / FX-R]")
                                    {
                                        TextAlignment = Anchor.BottomRight,
                                        Position = new Vector2(-10, -10),
                                    },
                                }
                            },
                        }
                    },
                }
            };

            var master = Mixer.MasterChannel;
            int sampleRate = master.SampleRate;
            int channels = master.Channels;
            double frequency = 432 * 2;
            double bpm = 150;

            m_click = new ClickTrack(sampleRate, channels, frequency, bpm);
            m_click.Channel = master;

            m_renderer = new BasicSpriteRenderer();
        }

        public override bool KeyPressed(KeyInfo key)
        {
            switch (key.KeyCode)
            {
                case KeyCode.ESCAPE: Pop(); break;

                case KeyCode.RETURN:
                {
                    Plugin.Config.Set(NscConfigKey.InputOffset, InputOffset);
                    Plugin.Config.Set(NscConfigKey.VideoOffset, VideoOffset);

                    Pop();
                } break;

                case KeyCode.SPACE:
                {
                    time_t pos = m_click.Position;
                    time_t beatDur = m_click.BeatDuration;

                    time_t inacc = (pos + beatDur / 2) % beatDur - beatDur / 2;
                    if (m_calcInputOffset)
                    {
                        m_totalInputInacc += inacc;
                        m_inputInaccCount++;
                    }
                    else
                    {
                        // Video is calculated as the opposite for reasons, dw
                        m_totalVideoInacc -= inacc;
                        m_videoInaccCount++;
                    }
                } break;

                case KeyCode.LEFT:  m_calcInputOffset = true;  break;
                case KeyCode.RIGHT: m_calcInputOffset = false; break;

                default: return false;
            }

            return true;
        }

        public override bool ControllerButtonPressed(ControllerInput input)
        {
            switch (input)
            {
                case ControllerInput.Back: Pop(); break;

                case ControllerInput.Start:
                {
                    Plugin.Config.Set(NscConfigKey.InputOffset, InputOffset);
                    Plugin.Config.Set(NscConfigKey.VideoOffset, VideoOffset);

                    Pop();
                }
                break;

                case ControllerInput.BT0:
                case ControllerInput.BT1:
                case ControllerInput.BT2:
                case ControllerInput.BT3:
                {
                    time_t pos = m_click.Position;
                    time_t beatDur = m_click.BeatDuration;

                    time_t inacc = (pos + beatDur / 2) % beatDur - beatDur / 2;
                    if (m_calcInputOffset)
                    {
                        m_totalInputInacc += inacc;
                        m_inputInaccCount++;
                    }
                    else
                    {
                        // Video is calculated as the opposite for reasons, dw
                        m_totalVideoInacc -= inacc;
                        m_videoInaccCount++;
                    }
                }
                break;

                case ControllerInput.FX0: m_calcInputOffset = true; break;
                case ControllerInput.FX1: m_calcInputOffset = false; break;

                default: return false;
            }

            return true;
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            m_click.Silenced = !m_calcInputOffset;

            m_inputLabel.Color = m_inputValueLabel.Color =  m_calcInputOffset ? Vector4.One : new Vector4(0.5f, 0.5f, 0.5f, 1);
            m_videoLabel.Color = m_videoValueLabel.Color = !m_calcInputOffset ? Vector4.One : new Vector4(0.5f, 0.5f, 0.5f, 1);

            m_inputValueLabel.Text = $"{ InputOffset }";
            m_videoValueLabel.Text = $"{ VideoOffset }";
        }

        public override void Render()
        {
            base.Render();

            float w = Window.Width, h = Window.Height;

            m_renderer.BeginFrame();
            {
                m_renderer.SetColor(127, 127, 127);
                m_renderer.FillRect(w / 2 - 1, 150, 2, h - 300);

                if (!m_calcInputOffset)
                {
                    time_t pos = m_click.Position;
                    time_t beatDur = m_click.BeatDuration;

                    time_t clickProgress = pos % beatDur;

                    float alpha = (float)(clickProgress / beatDur).Seconds;

                    int range = 1;
                    for (int i = -range; i <= range; i++)
                        DrawAt(alpha + i);

                    void DrawAt(float diff)
                    {
                        float a = 0.0f;
                        if (diff >= 0 && diff <= 0.4f)
                            a = 1 - diff / 0.4f;

                        m_renderer.SetColor(MathL.Lerp(127, 255, a), MathL.Lerp(127, 255, a), MathL.Lerp(255, 0, a));
                        float s = MathL.Lerp(2, 20, a);

                        m_renderer.FillRect((w - s) / 2 - (w / (2 * range)) * diff, 150, s, h - 300);
                    }
                }
            }
            m_renderer.EndFrame();
        }
    }
#endif
}
