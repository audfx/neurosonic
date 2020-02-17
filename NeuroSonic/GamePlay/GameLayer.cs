using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;

using theori;
using theori.Audio;
using theori.Charting;
using theori.Charting.Effects;
using theori.Charting.Playback;
using theori.Charting.Serialization;
using theori.Graphics;
using theori.Gui;
using theori.IO;
using theori.Resources;
using theori.Scripting;

using MoonSharp.Interpreter;

using NeuroSonic.Charting;
using NeuroSonic.GamePlay.Scoring;
using NeuroSonic.Platform;
using theori.Database;
using theori.Configuration;

namespace NeuroSonic.GamePlay
{
    [Flags]
    public enum AutoPlayTargets
    {
        None = 0,

        Buttons = 0x01,
        Lasers = 0x02,

        ButtonsAndLasers = Buttons | Lasers,
    }

    public sealed class GameLayer : NscLayer
    {
        private const float SLAM_VOLUME_MOD = 0.75f;
        private const float SAMPLE_VOLUME_MOD = 0.75f;

        public override int TargetFrameRate => 0;

        public override bool BlocksParentLayer => true;

        private readonly AutoPlayTargets m_autoPlay;

        private bool AutoButtons => (m_autoPlay & AutoPlayTargets.Buttons) != 0;
        private bool AutoLasers => (m_autoPlay & AutoPlayTargets.Lasers) != 0;

        private readonly ClientResourceLocator m_locator;

        private ScriptProgram m_guiScript;
        private Table m_gameTable, m_metaTable, m_scoringTable;

        private HighwayControl m_highwayControl;
        private HighwayView m_highwayView;

        private ScriptableBackground m_background;

        private CriticalLineUi m_critRootUi;
        private CriticalLineWorld m_critRootWorld;
        private ComboDisplay m_comboDisplay;

        private ChartInfo m_chartInfo;
        private Chart m_chart;
        private SlidingChartPlayback m_audioPlayback, m_visualPlayback;
        private MasterJudge m_judge;

        private AudioEffectController m_audioController;
        private AudioTrack m_audio;
        private AudioTrack m_slamSample;
        private readonly Dictionary<string, AudioTrack> m_hitSounds = new Dictionary<string, AudioTrack>();

        private readonly Entity[] m_activeObjects = new Entity[8];
        private readonly bool[] m_streamHasActiveEffects = new bool[8].Fill(true);

        private readonly float[] m_laserInputs = new float[2];

        private readonly bool[] m_cursorsActive = new bool[2];
        private readonly float[] m_cursorAlphas = new float[2];

        private readonly EffectDef[] m_currentEffects = new EffectDef[8];

        private readonly List<EventEntity> m_queuedSlamTiedEvents = new List<EventEntity>();
        private readonly List<time_t> m_queuedSlamSamples = new List<time_t>();

        private time_t CurrentQuarterNodeDuration => m_chart.ControlPoints.MostRecent(m_audioController.Position).QuarterNoteDuration;

        private time_t m_visualOffset = 0;

        private float m_tempHiSpeedMult = 1.0f;

        internal GameLayer(ClientResourceLocator resourceLocator, ChartInfo chartInfo, AutoPlayTargets autoPlay = AutoPlayTargets.None)
            : base(resourceLocator)
        {
            m_locator = resourceLocator;

            m_chartInfo = chartInfo;
            m_autoPlay = autoPlay;

            m_background = new ScriptableBackground(m_locator);
        }

        internal GameLayer(ClientResourceLocator resourceLocator, Chart chart, AudioTrack audio, AutoPlayTargets autoPlay = AutoPlayTargets.None)
            : base(resourceLocator)
        {
            m_locator = resourceLocator;

            m_chartInfo = chart.Info;
            m_chart = chart;
            m_audio = audio;

            m_autoPlay = autoPlay;

            m_background = new ScriptableBackground(m_locator);
        }

        public override void Destroy()
        {
            base.Destroy();

            m_highwayView.Dispose();
            m_background.Dispose();
            m_resources.Dispose();

            m_audioController.Stop();
            m_audioController.Dispose();
        }

        public override bool AsyncLoad()
        {
            string chartsDir = TheoriConfig.ChartsDirectory;
            var setInfo = m_chartInfo.Set;

            if (m_chart == null)
            {
                if (ChartDatabaseService.TryLoadChart(m_chartInfo) is Chart chart)
                {
                    m_chart = chart;

                    string audioFile = Path.Combine(chartsDir, setInfo.FilePath, m_chart.Info.SongFileName);
                    m_audio = AudioTrack.FromFile(audioFile);
                    m_audio.Channel = Mixer.MasterChannel;
                    m_audio.Volume = m_chart.Info.SongVolume / 100.0f;
                }
                else
                {
                    Logger.Log($"Failed to load chart {m_chartInfo.SongTitle}");

                    Pop();
                    return false;
                }
            }

            m_audioPlayback = new SlidingChartPlayback(m_chart, false);
            m_visualPlayback = new SlidingChartPlayback(m_chart, true);

            m_highwayView = new HighwayView(m_locator, m_visualPlayback);
            m_guiScript = new ScriptProgram(m_locator);

            m_gameTable = m_guiScript.NewTable();
            m_guiScript["game"] = m_gameTable;

            m_gameTable["meta"] = m_metaTable = m_guiScript.NewTable();
            m_gameTable["scoring"] = m_scoringTable = m_guiScript.NewTable();

            m_metaTable["SongTitle"] = m_chart.Info.SongTitle;
            m_metaTable["SongArtist"] = m_chart.Info.SongArtist;

            m_metaTable["DifficultyName"] = m_chart.Info.DifficultyName;
            m_metaTable["DifficultyNameShort"] = m_chart.Info.DifficultyNameShort;
            m_metaTable["DifficultyLevel"] = m_chart.Info.DifficultyLevel;
            m_metaTable["DifficultyColor"] = (m_chart.Info.DifficultyColor ?? new Vector3(1, 1, 1)) * 255;

            m_metaTable["PlayKind"] = "N";

            //m_guiScript.LoadFile(m_locator.OpenFileStream("scripts/generic-layer.lua"));
            //m_guiScript.LoadFile(m_locator.OpenFileStream("scripts/game/main.lua"));

            if (!m_guiScript.LuaAsyncLoad())
                return false;

            if (!m_highwayView.AsyncLoad())
                return false;
            if (!m_background.AsyncLoad())
                return false;

            m_slamSample = m_resources.QueueAudioLoad("audio/slam");

            m_hitSounds["clap"] = m_resources.QueueAudioLoad("audio/sample_clap");
            m_hitSounds["clap_punchy"] = m_resources.QueueAudioLoad("audio/sample_clap");
            m_hitSounds["clap_impact"] = m_resources.QueueAudioLoad("audio/sample_kick");
            m_hitSounds["snare"] = m_resources.QueueAudioLoad("audio/sample_snare");
            m_hitSounds["snare_lo"] = m_resources.QueueAudioLoad("audio/sample_snare_lo");

            foreach (var lane in m_chart.Lanes)
            {
                foreach (var entity in lane)
                {
                    switch (entity)
                    {
                        case ButtonEntity button:
                        {
                            if (!button.HasSample) break;

                            if (!m_hitSounds.ContainsKey(button.Sample))
                            {
                                string samplePath = Path.Combine(chartsDir, setInfo.FilePath, button.Sample);
                                if (!File.Exists(samplePath)) break;

                                var sample = AudioTrack.FromFile(samplePath);
                                m_resources.Manage(sample);

                                m_hitSounds[button.Sample] = sample;
                            }
                        } break;
                    }
                }
            }

            ForegroundGui = new Panel()
            {
                Children = new GuiElement[]
                {
                    m_critRootUi = new CriticalLineUi(m_resources),
                    m_critRootWorld = new CriticalLineWorld(m_resources),

                    m_comboDisplay = new ComboDisplay(m_resources)
                    {
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2(0.5f, 0.7f)
                    },
                }
            };

            if (!m_critRootUi.AsyncLoad())
                return false;
            if (!m_critRootWorld.AsyncLoad())
                return false;
            if (!m_comboDisplay.AsyncLoad())
                return false;

            if (!m_resources.LoadAll())
                return false;

            return true;
        }

        public override bool AsyncFinalize()
        {
            if (!m_guiScript.LuaAsyncFinalize())
                return false;
            //m_guiScript.InitSpriteRenderer();

            if (!m_highwayView.AsyncFinalize())
                return false;
            if (!m_background.AsyncFinalize())
                return false;

            if (!m_resources.FinalizeLoad())
                return false;

            m_slamSample.Channel = Mixer.MasterChannel;
            m_slamSample.RemoveFromChannelOnFinish = false;

            foreach (var (name, sample) in m_hitSounds)
            {
                sample.Channel = Mixer.MasterChannel;
                sample.RemoveFromChannelOnFinish = false;
            }

            m_visualOffset = NscConfig.VideoOffset / 1000.0;

            if (!m_critRootUi.AsyncFinalize())
                return false;
            if (!m_critRootWorld.AsyncFinalize())
                return false;
            if (!m_comboDisplay.AsyncFinalize())
                return false;

            return true;
        }

        public override void ClientSizeChanged(int width, int height)
        {
            //m_highwayView.Camera.AspectRatio = Window.Aspect;
        }

        public override void Initialize()
        {
            base.Initialize();

            m_highwayControl = new HighwayControl(HighwayControlConfig.CreateDefaultKsh168());
            m_background.Init();

            SetHiSpeed();

            m_audioPlayback.ObjectHeadCrossPrimary += (dir, entity) =>
            {
                if (dir == PlayDirection.Forward)
                {
                    if (entity is EventEntity evt)
                    {
                        switch (evt)
                        {
                            case SpinImpulseEvent _:
                            case SwingImpulseEvent _:
                            case WobbleImpulseEvent _:
                                m_queuedSlamTiedEvents.Add(evt);
                                break;
                        }
                    }
                }
            };
            m_visualPlayback.ObjectHeadCrossPrimary += (dir, entity) =>
            {
                if (dir == PlayDirection.Forward)
                    m_highwayView.RenderableObjectAppear(entity);
                else m_highwayView.RenderableObjectDisappear(entity);
            };
            m_visualPlayback.ObjectTailCrossSecondary += (dir, obj) =>
            {
                if (dir == PlayDirection.Forward)
                    m_highwayView.RenderableObjectDisappear(obj);
                else m_highwayView.RenderableObjectAppear(obj);
            };

            // TODO(local): Effects wont work with backwards motion, but eventually the
            //  editor (with the only backwards motion support) will pre-render audio instead.
            m_audioPlayback.ObjectHeadCrossCritical += (dir, obj) =>
            {
                if (dir != PlayDirection.Forward) return;

                if (obj is EventEntity evt)
                    PlaybackEventTrigger(evt, dir);
                else PlaybackObjectBegin(obj);
            };
            m_audioPlayback.ObjectTailCrossCritical += (dir, obj) =>
            {
                if (dir == PlayDirection.Backward && obj is EventEntity evt)
                    PlaybackEventTrigger(evt, dir);
                else PlaybackObjectEnd(obj);
            };
            m_visualPlayback.ObjectHeadCrossCritical += (dir, obj) =>
            {
                if (dir != PlayDirection.Forward) return;

                if (obj is EventEntity evt)
                    PlaybackVisualEventTrigger(evt, dir);
            };
            m_visualPlayback.ObjectTailCrossCritical += (dir, obj) =>
            {
                if (dir == PlayDirection.Backward && obj is EventEntity evt)
                    PlaybackVisualEventTrigger(evt, dir);
            };

            m_judge = new MasterJudge(m_chart);
            for (int i = 0; i < 6; i++)
            {
                var judge = (ButtonJudge)m_judge[i];
                judge.JudgementOffset = NscConfig.InputOffset / 1000.0f;
                judge.AutoPlay = AutoButtons;
                judge.OnChipPressed += Judge_OnChipPressed;
                judge.OnTickProcessed += Judge_OnTickProcessed;
                judge.OnHoldPressed += Judge_OnHoldPressed;
                judge.OnHoldReleased += Judge_OnHoldReleased;
                judge.SpawnKeyBeam = CreateKeyBeam;
            }
            for (int i = 0; i < 2; i++)
            {
                int iStack = i;

                var judge = (LaserJudge)m_judge[i + 6];
                judge.JudgementOffset = NscConfig.InputOffset / 1000.0f;
                judge.AutoPlay = AutoLasers;
                judge.OnShowCursor += () => m_cursorsActive[iStack] = true;
                judge.OnHideCursor += () => m_cursorsActive[iStack] = false;
                judge.OnTickProcessed += Judge_OnTickProcessed;
                judge.OnSlamHit += (position, entity) =>
                {
                    if (position < entity.AbsolutePosition)
                        m_queuedSlamSamples.Add(entity.AbsolutePosition);
                    else m_slamSample.Replay();
                };
            }

            m_highwayControl = new HighwayControl(HighwayControlConfig.CreateDefaultKsh168());
            m_highwayView.Reset();

            //m_audio.Volume = 0.8f;
            m_audio.Position = 0.0;
            m_audioController = new AudioEffectController(8, m_audio, true)
            {
                RemoveFromChannelOnFinish = true,
            };
            m_audioController.Finish += () =>
            {
                Logger.Log("track complete");
                ExitGame();
            };

            m_audioController.Position = MathL.Min(0.0, (double)m_chart.TimeStart - 2);

            m_gameTable["Begin"] = (Action)Begin;
            m_guiScript.CallIfExists("Init");

            ClientAs<NscClient>().OpenCurtain();
            Begin();
        }

        private void SetHiSpeed()
        {
            switch (NscConfig.HiSpeedModKind)
            {
                case HiSpeedMod.Default:
                {
                    double hiSpeed = NscConfig.HiSpeed;
                    m_visualPlayback.DefaultViewTime = m_audioPlayback.DefaultViewTime = 8 * 60.0 / (m_chart.ControlPoints.ModeBeatsPerMinute * m_tempHiSpeedMult * hiSpeed);
                } break;
                case HiSpeedMod.MMod:
                {
                    var modSpeed = NscConfig.ModSpeed;
                    double hiSpeed = modSpeed / m_chart.ControlPoints.ModeBeatsPerMinute;
                    m_visualPlayback.DefaultViewTime = m_audioPlayback.DefaultViewTime = 8 * 60.0 / (m_chart.ControlPoints.ModeBeatsPerMinute * m_tempHiSpeedMult * hiSpeed);
                } break;

                case HiSpeedMod.CMod:
                goto case HiSpeedMod.Default; //break;
            }
        }

        private void ExitGame()
        {
            //ClientAs<NscClient>().CloseCurtain(() => Pop());
            CloseCurtain(() =>
            {
                var result = new ScoringResult()
                {
                    Score = m_judge.Score,

                    Gauge = m_judge.Gauge,
                };

                //Push(new ChartResultLayer(m_locator, m_chartInfo, result));
                SetInvalidForResume();
                Pop();
            });
        }

        public void Begin()
        {
            m_audioController.Play();
        }

        public override void Suspended(Layer nextLayer)
        {
            m_audioController.Stop();
        }

        public override void Resumed(Layer previousLayer)
        {
            throw new Exception("Cannot suspend gameplay layer");
        }

        private void PlaybackObjectBegin(Entity entity)
        {
            if (entity is AnalogEntity aobj)
            {
                if (entity.IsInstant)
                {
                    int dir = -MathL.Sign(aobj.FinalValue - aobj.InitialValue);
                    m_highwayControl.ShakeCamera(dir);

#if false
                    if (aobj.InitialValue == (aobj.Lane == 6 ? 0 : 1) && aobj.NextConnected == null)
                        m_highwayControl.ApplyRollImpulse(-dir);
#endif

                    //if (m_judge[(int)entity.Lane].IsBeingPlayed) m_slamSample.Replay();
                }

                if (aobj.PreviousConnected == null)
                {
                    if (!AreLasersActive) m_audioController.SetEffect(6, CurrentQuarterNodeDuration, currentLaserEffectDef, BASE_LASER_MIX);
                    currentActiveLasers[(int)entity.Lane - 6] = true;
                }

                m_activeObjects[(int)entity.Lane] = aobj.Head;
            }
            else if (entity is ButtonEntity bobj)
            {
                //if (bobj.HasEffect) m_audioController.SetEffect(obj.Stream, CurrentQuarterNodeDuration, bobj.Effect);
                //else m_audioController.RemoveEffect(obj.Stream);

                // NOTE(local): can move this out for analog as well, but it doesn't matter RN
                if (!bobj.IsInstant)
                    m_activeObjects[(int)entity.Lane] = entity;
            }
        }

        private void PlaybackObjectEnd(Entity obj)
        {
            if (obj is AnalogEntity aobj)
            {
                if (aobj.NextConnected == null)
                {
                    currentActiveLasers[(int)obj.Lane - 6] = false;
                    if (!AreLasersActive) m_audioController.RemoveEffect(6);

                    if (m_activeObjects[(int)obj.Lane] == aobj.Head)
                        m_activeObjects[(int)obj.Lane] = null;
                }
            }
            if (obj is ButtonEntity bobj)
            {
                //m_audioController.RemoveEffect(obj.Stream);

                // guard in case the Begin function already overwrote us
                if (m_activeObjects[(int)obj.Lane] == obj)
                    m_activeObjects[(int)obj.Lane] = null;
            }
        }

        private void Judge_OnTickProcessed(Entity entity, time_t position, JudgeResult result)
        {
            //Logger.Log($"[{ obj.Stream }] { result.Kind } :: { (int)(result.Difference * 1000) } @ { position }");

            if (result.Kind == JudgeKind.Miss || result.Kind == JudgeKind.Bad)
                m_comboDisplay.Combo = 0;
            else m_comboDisplay.Combo++;

            if ((int)entity.Lane >= 6) return;

            if (entity.IsInstant)
            {
                if (result.Kind != JudgeKind.Miss)
                {
                    if (entity is ButtonEntity button && button.HasSample && m_hitSounds.ContainsKey(button.Sample))
                    {
                        var sample = m_hitSounds[button.Sample];
                        sample.Volume = button.SampleVolume * SAMPLE_VOLUME_MOD;
                        sample.Replay();
                    }
                }
            }
        }

        private void Judge_OnChipPressed(time_t position, Entity obj)
        {
        }

        private void Judge_OnHoldReleased(time_t position, Entity obj)
        {
        }

        private void Judge_OnHoldPressed(time_t position, Entity obj)
        {
            //CreateKeyBeam((int)obj.Lane, JudgeKind.Passive, false);
        }

        private void PlaybackEventTrigger(EventEntity evt, PlayDirection direction)
        {
            if (direction == PlayDirection.Forward)
            {
                switch (evt)
                {
                    case EffectKindEvent effectKind:
                    {
                        var effect = m_currentEffects[effectKind.EffectIndex] = effectKind.Effect;
                        if (effect == null)
                            m_audioController.RemoveEffect(effectKind.EffectIndex);
                        else m_audioController.SetEffect(effectKind.EffectIndex, CurrentQuarterNodeDuration, effect, 1.0f);
                    }
                    break;

                    // TODO(local): left/right lasers separate + allow both independent if needed
                    case LaserFilterGainEvent filterGain: laserGain = filterGain.Gain; break;
                    case LaserFilterKindEvent filterKind:
                    {
                        m_audioController.SetEffect(6, CurrentQuarterNodeDuration, currentLaserEffectDef = filterKind.Effect, m_audioController.GetEffectMix(6));
                    }
                    break;

                    case SlamVolumeEvent pars: m_slamSample.Volume = pars.Volume * SLAM_VOLUME_MOD; break;
                }
            }
        }

        private void PlaybackVisualEventTrigger(EventEntity evt, PlayDirection direction)
        {
            if (direction == PlayDirection.Forward)
            {
                switch (evt)
                {
                    case LaserApplicationEvent app: m_highwayControl.LaserApplication = app.Application; break;

                    case LaserParamsEvent pars:
                    {
                        if (pars.LaserIndex.HasFlag(LaserIndex.Left)) m_highwayControl.LeftLaserParams = pars.Params;
                        if (pars.LaserIndex.HasFlag(LaserIndex.Right)) m_highwayControl.RightLaserParams = pars.Params;
                    }
                    break;
                }
            }
        }

        private float m_lastStartPressTime = 0;
        private bool m_isChangingHiSpeedMult = false;

        public override void ControllerButtonPressed(ControllerButtonInfo info)
        {
            if (info.Button == "start")
            {
                if (Time.Total - m_lastStartPressTime < 0.25f)
                {
                    m_tempHiSpeedMult = 1.0f;
                    SetHiSpeed();

                    m_lastStartPressTime = 0;
                }

                m_lastStartPressTime = Time.Total;
                m_isChangingHiSpeedMult = true;
            }
            else if (info.Button == "back")
                ExitGame();
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    if (info.Button == i)
                    {
                        UserInput_BtPress(i);
                        break;
                    }
                }
            }
        }

        public override void ControllerButtonReleased(ControllerButtonInfo info)
        {
            if (info.Button == "start")
                m_isChangingHiSpeedMult = false;
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    if (info.Button == i)
                    {
                        UserInput_BtRelease(i);
                        break;
                    }
                }
            }
        }

        public override void ControllerAxisChanged(ControllerAxisInfo info)
        {
            if (m_isChangingHiSpeedMult)
            {
                m_tempHiSpeedMult = MathL.Clamp(m_tempHiSpeedMult + info.Delta * 0.1f, 0.1f, 2);
                SetHiSpeed();
            }

                 if (info.Axis == 0) UserInput_VolPulse(0, info.Delta);
            else if (info.Axis == 1) UserInput_VolPulse(1, info.Delta);
        }

        public override void KeyPressed(KeyInfo key)
        {
            switch (key.KeyCode)
            {
                case KeyCode.UP: m_tempPitch += 0.01f; break;
                case KeyCode.DOWN: m_tempPitch -= 0.01f; break;

                case KeyCode.RIGHT: m_tempPitch += 0.1f; break;
                case KeyCode.LEFT: m_tempPitch -= 0.1f; break;

                case KeyCode.PAGEUP:
                {
                    m_audioController.Position += m_chart.ControlPoints.MostRecent(m_audioController.Position).MeasureDuration;
                } break;

                case KeyCode.ESCAPE:
                {
                    ExitGame();
                } break;
            }
        }

        void UserInput_BtPress(int lane)
        {
            if (AutoButtons) return;
            var result = ((ButtonJudge)m_judge[lane]).UserPressed(m_judge.Position);
        }

        void UserInput_BtRelease(int lane)
        {
            if (AutoButtons) return;

            ((ButtonJudge)m_judge[lane]).UserReleased(m_judge.Position);
            m_highwayView.EndKeyBeam(lane);
        }

        void UserInput_VolPulse(int lane, float amount)
        {
            if (AutoLasers) return;
            amount *= 0.5f;

            ((LaserJudge)m_judge[lane + 6]).UserInput(amount, m_judge.Position);
        }

        private void CreateKeyBeam(HybridLabel label, JudgeKind kind, bool isEarly)
        {
            Vector3 color = Vector3.One;

            bool doAnim = true;
            switch (kind)
            {
                case JudgeKind.Passive: doAnim = false; break;
                case JudgeKind.Perfect: color = new Vector3(1, 1, 0); break;
                case JudgeKind.Critical: color = new Vector3(1, 1, 0); break;
                case JudgeKind.Near: color = isEarly ? new Vector3(1.0f, 0, 1.0f) : new Vector3(0.5f, 1, 0.25f); break;
                case JudgeKind.Bad:
                case JudgeKind.Miss: color = new Vector3(1, 0, 0); break;
            }

            m_highwayView.BeginKeyBeam(label, color);
            if (doAnim) m_critRootWorld.TriggerButtonAnimation((int)label, color);
        }

        private void SetLuaDynamicData()
        {
            m_gameTable["Progress"] = MathL.Clamp01((double)(m_audioController.Position / m_chart.LastObjectTime));

            m_scoringTable["CurrentBpm"] = m_chart.ControlPoints.MostRecent(m_audioController.Position).BeatsPerMinute;
            m_scoringTable["CurrentHiSpeed"] = 1.0;

            m_scoringTable["Gauge"] = m_judge.Gauge;
            m_scoringTable["Score"] = m_judge.Score;
        }

        float m_tempPitch = 0;

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            time_t audioPosition = m_audio?.Position ?? 0;
            time_t visualPosition = audioPosition - m_visualOffset;

            m_audioPlayback.Position = audioPosition;
            m_judge.Position = audioPosition;

            m_visualPlayback.Position = visualPosition;
            m_highwayControl.Position = visualPosition;

            //Logger.Log($"{ m_visualPlayback.ViewDistanceForward } / { m_visualPlayback.ViewDistanceForward }");

            float GetPathValueLerped(time_t pos, HybridLabel stream)
            {
                var s = m_audioPlayback.Chart[stream];

                var mrPoint = s.MostRecent<GraphPointEvent>(pos);
                if (mrPoint == null)
                    return ((GraphPointEvent?)s.First)?.Value ?? 0;

                if (mrPoint.HasNext)
                {
                    float alpha = (float)((pos - mrPoint.AbsolutePosition).Seconds / (mrPoint.Next.AbsolutePosition - mrPoint.AbsolutePosition).Seconds);
                    return MathL.Lerp(mrPoint.Value, ((GraphPointEvent)mrPoint.Next).Value, alpha);
                }
                else return mrPoint.Value;
            }

            for (int i = 0; i < m_queuedSlamSamples.Count;)
            {
                time_t slam = m_queuedSlamSamples[i];
                if (slam < audioPosition)
                {
                    m_queuedSlamSamples.RemoveAt(i);
                    m_slamSample.Replay();
                }
                else i++;
            }

            for (int e = 0; e < m_queuedSlamTiedEvents.Count;)
            {
                var evt = m_queuedSlamTiedEvents[e];
                if (evt.AbsolutePosition < visualPosition)
                {
                    switch (evt)
                    {
                        case SpinImpulseEvent spin: m_highwayControl.ApplySpin(spin.Params, visualPosition); break;
                        case SwingImpulseEvent swing: m_highwayControl.ApplySwing(swing.Params, visualPosition); break;
                        case WobbleImpulseEvent wobble: m_highwayControl.ApplyWobble(wobble.Params, visualPosition); break;

                        default: e++; continue;
                    }

                    m_queuedSlamTiedEvents.RemoveAt(e);
                }
                else e++;
            }

            time_t laserAnticipateLookAhead = visualPosition + m_chart.ControlPoints.MostRecent(visualPosition).MeasureDuration * 0.5;
            time_t laserSlamSwingDuration = 0.15;
            for (int i = 0; i < 2; i++)
            {
                m_laserInputs[i] = i; // 0 or 1, default values for each side

                var currentLaser = m_chart[i + 6].MostRecent<AnalogEntity>(visualPosition);
                if (currentLaser == null || visualPosition > currentLaser.AbsoluteEndPosition)
                {
                    float inputValue = m_laserInputs[i];
                    if (currentLaser is { } slam && slam.IsInstant && visualPosition <= slam.AbsoluteEndPosition + laserSlamSwingDuration)
                        inputValue = slam.FinalValue;

                    var checkAnalog = m_chart[i + 6].MostRecent<AnalogEntity>(laserAnticipateLookAhead)?.Head;
                    while (checkAnalog != null && checkAnalog.HasPrevious && checkAnalog.Previous is AnalogEntity pAnalog && (pAnalog = pAnalog.Head).AbsolutePosition > visualPosition)
                        checkAnalog = pAnalog;

                    if (checkAnalog != null && checkAnalog.AbsolutePosition > visualPosition)
                    {
                        if (i == 0)
                            inputValue = MathL.Max(checkAnalog!.InitialValue, inputValue);
                        else inputValue = MathL.Min(checkAnalog!.InitialValue, inputValue);
                    }

                    m_laserInputs[i] = inputValue;
                }
                else m_laserInputs[i] = currentLaser.SampleValue(visualPosition);
            }

            m_highwayControl.MeasureDuration = m_chart.ControlPoints.MostRecent(visualPosition).MeasureDuration;

            m_highwayControl.LeftLaserInput = m_laserInputs[0];
            m_highwayControl.RightLaserInput = 1 - m_laserInputs[1];

            m_highwayControl.Zoom = GetPathValueLerped(visualPosition, NscLane.CameraZoom);
            //m_highwayControl.Zoom = 0;
            m_highwayControl.Pitch = GetPathValueLerped(visualPosition, NscLane.CameraPitch);
            //m_highwayControl.Pitch = m_tempPitch;
            //Console.WriteLine(m_tempPitch);
            m_highwayControl.Offset = GetPathValueLerped(visualPosition, NscLane.CameraOffset);
            m_highwayControl.Roll = GetPathValueLerped(visualPosition, NscLane.CameraTilt);

            m_highwayView.Split0 = GetPathValueLerped(visualPosition, NscLane.Split0);
            m_highwayView.Split1 = GetPathValueLerped(visualPosition, NscLane.Split1);
            m_highwayView.Split2 = GetPathValueLerped(visualPosition, NscLane.Split2);
            m_highwayView.Split3 = GetPathValueLerped(visualPosition, NscLane.Split3);
            m_highwayView.Split4 = GetPathValueLerped(visualPosition, NscLane.Split4);

            for (int i = 0; i < 8; i++)
            {
                var judge = m_judge[i];
                m_streamHasActiveEffects[i] = judge.IsBeingPlayed;
            }

            for (int i = 0; i < 8; i++)
            {
                bool active = m_streamHasActiveEffects[i] && m_activeObjects[i] != null;
                if (i == 6)
                    active |= m_streamHasActiveEffects[i + 1] && m_activeObjects[i + 1] != null;
                m_audioController.SetEffectActive(i, active);
            }

            UpdateEffects();
            m_audioController.EffectsActive = true;

            m_highwayControl.Update(Time.Delta);
            m_highwayControl.ApplyToView(m_highwayView);

            for (int i = 0; i < 8; i++)
            {
                var obj = m_activeObjects[i];

                m_highwayView.SetStreamActive(i, m_streamHasActiveEffects[i]);

                if (obj == null) continue;

                float glow = -0.5f;
                int glowState = 0;

                if (m_streamHasActiveEffects[i])
                {
                    glow = MathL.Cos(10 * MathL.TwoPi * (float)visualPosition) * 0.35f;
                    glowState = 2 + MathL.FloorToInt(visualPosition.Seconds * 20) % 2;
                }

                m_highwayView.SetObjectGlow(obj, glow, glowState);
            }

            const float SCALING = 1.05f;
            if (Window.Width > Window.Height)
                m_highwayView.Viewport = ((int)(Window.Width - Window.Height * SCALING) / 2, (int)(Window.Height * 0.95f - Window.Height * SCALING), (int)(Window.Height * SCALING));
            else m_highwayView.Viewport = (0, (Window.Height - Window.Width) / 2 - Window.Width / 5, Window.Width);
            m_highwayView.Update();

            {
                //var camera = m_highwayView.Camera;

                var defaultTransform = m_highwayView.DefaultTransform;
                var defaultZoomTransform = m_highwayView.DefaultZoomedTransform;
                var totalWorldTransform = m_highwayView.WorldTransform;
                var critLineUiTransform = m_highwayView.CritLineTransform;

                Vector2 comboLeft = m_highwayView.Project(defaultTransform, new Vector3(-0.8f / 6, 0, 0));
                Vector2 comboRight = m_highwayView.Project(defaultTransform, new Vector3(0.8f / 6, 0, 0));

                m_comboDisplay.DigitSize = (comboRight.X - comboLeft.X) / 4;

                Vector2 critRootUiPosition = m_highwayView.Project(critLineUiTransform, Vector3.Zero);
                Vector2 critRootUiPositionWest = m_highwayView.Project(critLineUiTransform, new Vector3(-1, 0, 0));
                Vector2 critRootUiPositionEast = m_highwayView.Project(critLineUiTransform, new Vector3(1, 0, 0));
                Vector2 critRootPositionForward = m_highwayView.Project(critLineUiTransform, new Vector3(0, 0, -1));

                Vector2 critRootWorldPosition = m_highwayView.Project(totalWorldTransform, Vector3.Zero);
                Vector2 critRootWorldPositionWest = m_highwayView.Project(totalWorldTransform, new Vector3(-1, 0, 0));
                Vector2 critRootWorldPositionEast = m_highwayView.Project(totalWorldTransform, new Vector3(1, 0, 0));

                for (int i = 0; i < 2; i++)
                {
                    if (m_cursorsActive[i])
                        m_cursorAlphas[i] = MathL.Min(1, m_cursorAlphas[i] + delta * 3);
                    else m_cursorAlphas[i] = MathL.Max(0, m_cursorAlphas[i] - delta * 5);
                }

                void GetCursorPosition(int lane, out float pos, out float range)
                {
                    var judge = (LaserJudge)m_judge[lane + 6];
                    pos = judge.CursorPosition;
                    range = judge.LaserRange;
                }

                float GetCursorPositionWorld(float xWorld)
                {
                    var critRootCenter = m_highwayView.Project(defaultZoomTransform, Vector3.Zero);
                    var critRootCursor = m_highwayView.Project(defaultZoomTransform, new Vector3(xWorld, 0, 0));
                    return critRootCursor.X - critRootCenter.X;
                }

                GetCursorPosition(0, out float leftLaserPos, out float leftLaserRange);
                GetCursorPosition(1, out float rightLaserPos, out float rightLaserRange);

                float critRootWorldWidth = (m_highwayView.Project(defaultZoomTransform, new Vector3(-0.5f, 0, 0)) - m_highwayView.Project(defaultZoomTransform, new Vector3(0.5f, 0, 0))).Length();
                m_critRootWorld.WorldUnitSize = critRootWorldWidth;

                m_critRootWorld.LeftCursorPosition = 2 * (leftLaserPos - 0.5f) * (5.0f / 6) * leftLaserRange;
                m_critRootWorld.LeftCursorAlpha = m_cursorAlphas[0];
                m_critRootWorld.RightCursorPosition = 2 * (rightLaserPos - 0.5f) * (5.0f / 6) * rightLaserRange;
                m_critRootWorld.RightCursorAlpha = m_cursorAlphas[1];

                Vector2 critUiRotationVector = critRootUiPositionEast - critRootUiPositionWest;
                float critRootUiRotation = MathL.Atan(critUiRotationVector.Y, critUiRotationVector.X);

                Vector2 critWorldRotationVector = critRootWorldPositionEast - critRootWorldPositionWest;
                float critRootWorldRotation = MathL.Atan(critWorldRotationVector.Y, critWorldRotationVector.X);

                m_critRootUi.Position = critRootUiPosition;
                m_critRootWorld.Position = critRootWorldPosition;

                m_critRootUi.Rotation = MathL.ToDegrees(critRootUiRotation) + m_highwayControl.CritLineEffectRoll * 25;
                m_critRootWorld.Rotation = MathL.ToDegrees(critRootWorldRotation);
            }

            m_background.HorizonHeight = m_highwayView.HorizonHeight;
            m_background.CombinedTilt = m_highwayControl.LaserRoll + m_highwayControl.Roll * 360;
            m_background.EffectRotation = m_highwayControl.EffectRoll * 360;
            m_background.SpinTimer = m_highwayControl.SpinTimer;
            m_background.SwingTimer = m_highwayControl.SwingTimer;
            m_background.Update(delta, total);

            SetLuaDynamicData();
            m_guiScript.Update(delta, total);
        }

        private void UpdateEffects()
        {
            UpdateLaserEffects();
        }

        private EffectDef currentLaserEffectDef = BiQuadFilterDef.CreateDefaultPeak();
        private readonly bool[] currentActiveLasers = new bool[2];
        private readonly float[] currentActiveLaserAlphas = new float[2];

        private bool AreLasersActive => currentActiveLasers[0] || currentActiveLasers[1];

        private const float BASE_LASER_MIX = 0.8f;
        private float laserGain = 0.5f;

        private float GetTempRollValue(time_t position, HybridLabel label, out float valueMult, bool oneMinus = false)
        {
            var s = m_audioPlayback.Chart[label];
            valueMult = 1.0f;

            var mrAnalog = s.MostRecent<AnalogEntity>(position);
            if (mrAnalog == null || position > mrAnalog.AbsoluteEndPosition)
                return 0;

            if (mrAnalog.RangeExtended)
                valueMult = 2.0f;
            float result = mrAnalog.SampleValue(position);
            if (oneMinus)
                return 1 - result;
            else return result;
        }

        private void UpdateLaserEffects()
        {
            if (!AreLasersActive)
            {
                // mute both channels always if no lasers are active
                m_audioController.SetEffectMix(6, 0);
                m_audioController.SetEffectMix(7, 0);
            }
            else
            {
                // Update active laser positions for both lasers if either is active
                currentActiveLaserAlphas[0] = currentActiveLasers[0] ? LaserAlpha(0) : 0;
                currentActiveLaserAlphas[1] = currentActiveLasers[1] ? LaserAlpha(1) : 0;

                float alpha;
                if (currentActiveLasers[0] && currentActiveLasers[1])
                    alpha = Math.Max(currentActiveLaserAlphas[0], currentActiveLaserAlphas[1]);
                else if (currentActiveLasers[0])
                    alpha = currentActiveLaserAlphas[0];
                else alpha = currentActiveLaserAlphas[1];

                m_audioController.UpdateEffect(6, CurrentQuarterNodeDuration, alpha);
                m_audioController.SetEffectMix(6, GetEffectMix(currentLaserEffectDef, laserGain, alpha));
            }

            float LaserAlpha(int index) => GetTempRollValue(m_audio.Position, index + 6, out float _, index == 1);
            static float GetEffectMix(EffectDef effect, float mix, float alpha)
            {
                if (effect != null)
                {
                    if (effect is BiQuadFilterDef bqf && bqf.FilterType == FilterType.Peak)
                    {
                        mix *= BASE_LASER_MIX;
                        if (alpha < 0.2f)
                            mix *= alpha / 0.2f;
                        else if (alpha > 0.8f)
                            mix *= 1 - (alpha - 0.8f) / 0.2f;
                    }
                    else
                    {
                        // TODO(local): a lot of these (all?) don't need to have special mixes. idk why these got here but they're needed for some reason? fix
                        switch (effect)
                        {
                            case BitCrusherDef _:
                                mix = effect.Mix.Sample(alpha);
                                break;

                            case GateDef _:
                            case RetriggerDef _:
                            case TapeStopDef _:
                                mix = effect.Mix.Sample(alpha);
                                break;

                            case BiQuadFilterDef _: break;
                        }
                    }
                }

                return mix;
            }
        }

        public override void Render()
        {
            m_background.Render();
            m_highwayView.Render();
        }

        public override void LateRender()
        {
            base.LateRender();
            m_guiScript.Render();
        }
    }
}
