using System;

using theori;
using theori.Charting;

using NeuroSonic.Charting.KShootMania;
using System.Collections.Generic;

namespace NeuroSonic.Charting.Conversions
{
    public static class ChartExt_Ksh2Voltex
    {
        class TempButtonState
        {
            public tick_t StartPosition;
            public string Sample;

            public TempButtonState(tick_t pos)
            {
                StartPosition = pos;
            }
        }

        class TempLaserState
        {
            public ControlPoint ControlPoint;

            public tick_t StartPosition;
            public float StartAlpha;

            public int HiResTickCount;
            /// <summary>
            /// For fast sequences of slams (which are very rare).
            /// Normally you'd see straight segments in the file connecting slams,
            ///  but when many are stacked on top of eachother they APPEAR to be simple
            ///  linear sections instead.
            /// If two sections which appear to the linear are back to back, this is used
            ///  to insert the extra spacing.
            /// </summary>
            public tick_t PreviousSlamDuration;

            public CurveShape Shape = CurveShape.Linear;
            public float CurveA, CurveB;
            public int CurveResolution;
            
            public TempLaserState(tick_t pos, ControlPoint cp)
            {
                StartPosition = pos;
                ControlPoint = cp;
            }
        }

        public static Chart ToVoltex(this KshChart ksh, ChartInfo? info = null)
        {
            Logger.Log($"ksh.convert start");

            bool hasActiveEffects = !(ksh.Metadata.MusicFile != null && ksh.Metadata.MusicFileNoFx != null);
            Logger.Log($"ksh.convert effects disabled");

            var chart = NeuroSonicChartFactory.Instance.CreateNew();
            chart.Offset = (ksh.Metadata.OffsetMillis) / 1_000.0;

            // if info is non-null, set information exists as well.
            chart.Info = info ?? new ChartInfo()
            {
                SongTitle = ksh.Metadata.Title,
                SongArtist = ksh.Metadata.Artist,
                SongFileName = ksh.Metadata.MusicFile ?? ksh.Metadata.MusicFileNoFx ?? "??",
                SongVolume = ksh.Metadata.MusicVolume,
                ChartOffset = chart.Offset,
                Charter = ksh.Metadata.EffectedBy,
                JacketFileName = ksh.Metadata.JacketPath,
                JacketArtist = ksh.Metadata.Illustrator,
                BackgroundFileName = ksh.Metadata.Background,
                BackgroundArtist = "Unknown",
                DifficultyLevel = ksh.Metadata.Level,
                DifficultyIndex = ksh.Metadata.Difficulty.ToDifficultyIndex(ksh.FileName),
                DifficultyName = ksh.Metadata.Difficulty.ToDifficultyString(ksh.FileName),
                DifficultyNameShort = ksh.Metadata.Difficulty.ToShortString(ksh.FileName),
                DifficultyColor = ksh.Metadata.Difficulty.GetColor(ksh.FileName),
            };

            {
                if (double.TryParse(ksh.Metadata.BeatsPerMinute, out double bpm))
                    chart.ControlPoints.Root.BeatsPerMinute = bpm;

                var laserParams = chart[NscLane.LaserEvent].Add<LaserParamsEvent>(0);
                laserParams.LaserIndex = LaserIndex.Both;

                var laserGain = chart[NscLane.LaserEvent].Add<LaserFilterGainEvent>(0);
                laserGain.LaserIndex = LaserIndex.Both;
                if (!hasActiveEffects)
                    laserGain.Gain = 0.0f;
                else laserGain.Gain = ksh.Metadata.PFilterGain / 100.0f;
                
                var laserFilter = chart[NscLane.LaserEvent].Add<LaserFilterKindEvent>(0);
                laserFilter.LaserIndex = LaserIndex.Both;
                laserFilter.Effect = new KshEffectRef(ksh.Metadata.FilterType, null).CreateEffectDef(ksh.FilterDefines);

                var slamVolume = chart[NscLane.LaserEvent].Add<SlamVolumeEvent>(0);
                slamVolume.Volume = ksh.Metadata.SlamVolume / 100.0f;
            }

            double modeBpm;
            if (ksh.Metadata.HiSpeedBpm != null)
                modeBpm = ksh.Metadata.HiSpeedBpm.Value;
            else
            {
                var bpms = new List<(tick_t, double)>();
                tick_t lastTick = 0;

                foreach (var tickRef in ksh)
                {
                    var tick = tickRef.Tick;
                    
                    int blockOffset = tickRef.Block;
                    tick_t chartPos = blockOffset + (double)tickRef.Index / tickRef.MaxIndex;

                    foreach (var setting in tick.Settings)
                    {
                        if (setting.Key == "t")
                        {
                            double bpm = double.Parse(setting.Value.ToString());
                            if (bpms.Count > 0 && bpm == bpms[bpms.Count - 1].Item2)
                                continue;
                            bpms.Add((chartPos, bpm));
                        }
                    }

                    lastTick = chartPos;
                }

                var bpmDurs = new Dictionary<double, tick_t>();
                tick_t longest = -1;

                double result = 120.0;
                for (int i = 0; i < bpms.Count; i++)
                {
                    bool last = i == bpms.Count - 1;
                    var (when, bpm) = bpms[i];

                    tick_t duration;
                    if (last)
                        duration = lastTick - when;
                    else duration = bpms[i].Item1 - when;

                    if (bpmDurs.TryGetValue(bpm, out tick_t accum))
                        bpmDurs[bpm] = accum + duration;
                    else bpmDurs[bpm] = duration;

                    if (bpmDurs[bpm] > longest)
                    {
                        longest = bpmDurs[bpm];
                        result = bpm;
                    }
                }

                modeBpm = result;
            }

            var lastCp = chart.ControlPoints.Root;

            var buttonStates = new TempButtonState[6];
            var laserStates = new TempLaserState[2];
            bool[] laserIsExtended = new bool[2] { false, false };
            GraphPointEvent lastTiltEvent = null;

            foreach (var tickRef in ksh)
            {
                var tick = tickRef.Tick;
                
                int blockOffset = tickRef.Block;
                tick_t chartPos = blockOffset + (double)tickRef.Index / tickRef.MaxIndex;

                string[] chipHitSounds = new string[6];
                float[] chipHitSoundsVolume = new float[6];

                foreach (var setting in tick.Settings)
                {
                    string key = setting.Key;
                    switch (key)
                    {
                        case "beat":
                        {
                            if (!setting.Value.ToString().TrySplit('/', out string n, out string d))
                            {
                                n = d = "4";
                                Logger.Log($"ksh.convert({ chartPos }) error: { setting.Value } is not a valid time signature. Defaulting to 4/4.");
                            }

                            tick_t pos = MathL.Ceil((double)chartPos);
                            ControlPoint cp = chart.ControlPoints.GetOrCreate(pos, true);
                            cp.BeatCount = int.Parse(n);
                            cp.BeatKind = int.Parse(d);
                            lastCp = cp;

                            Logger.Log($"ksh.convert({ chartPos }) time signature { cp.BeatCount }/{ cp.BeatKind }");
                        } break;

                        case "t":
                        {
                            ControlPoint cp = chart.ControlPoints.GetOrCreate(chartPos, true);
                            cp.BeatsPerMinute = double.Parse(setting.Value.ToString());
                            cp.SpeedMultiplier = cp.BeatsPerMinute / modeBpm;
                            lastCp = cp;
                            Logger.Log($"ksh.convert({ chartPos }) bpm { cp.BeatsPerMinute }");
                        } break;

                        case "fx-l":
                        case "fx-r":
                        {
                            if (hasActiveEffects)
                            {
                                var effectEvent = chart[NscLane.ButtonEvent].Add<EffectKindEvent>(chartPos);
                                effectEvent.EffectIndex = key == "fx-l" ? 4 : 5;
                                effectEvent.Effect = (setting.Value.Value as KshEffectRef)?.CreateEffectDef(ksh.FxDefines);
                                Logger.Log($"ksh.convert({ chartPos }) set { key } { effectEvent.Effect?.GetType().Name ?? "nothing" }");
                            }
                            else Logger.Log($"ksh.convert({ chartPos }) effects disabled for { key }");
                        } break;

                        case "fx-l_se":
                        case "fx-r_se":
                        {
                            string chipFx = (string)setting.Value.Value;
                            float volume = 1.0f;

                            if (chipFx.TrySplit(';', out string fxName, out string volStr))
                            {
                                chipFx = fxName;
                                if (volStr.Contains(';'))
                                    volStr = volStr.Substring(0, volStr.IndexOf(';'));
                                volume = int.Parse(volStr) / 100.0f;
                            }

                            int i = key == "fx-l_se" ? 4 : 5;
                            chipHitSoundsVolume[i] = volume;
                            chipHitSounds[i] = chipFx;
                        } break;

                        case "fx-l_param1":
                        {
                            Logger.Log($"ksh.convert({ chartPos }) skipping fx-l_param1.");
                        } break;

                        case "fx-r_param1":
                        {
                            Logger.Log($"ksh.convert({ chartPos }) skipping fx-r_param1.");
                        } break;

                        case "pfiltergain":
                        {
                            if (hasActiveEffects)
                            {
                                var laserGain = chart[NscLane.LaserEvent].Add<LaserFilterGainEvent>(chartPos);
                                laserGain.LaserIndex = LaserIndex.Both;
                                laserGain.Gain = setting.Value.ToInt() / 100.0f;
                                Logger.Log($"ksh.convert({ chartPos }) set { key } { setting.Value }");
                            }
                            else Logger.Log($"ksh.convert({ chartPos }) effects disabled for { key }");
                        } break;

                        case "filtertype":
                        {
                            if (hasActiveEffects)
                            {
                                var laserFilter = chart[NscLane.LaserEvent].Add<LaserFilterKindEvent>(chartPos);
                                laserFilter.LaserIndex = LaserIndex.Both;
                                laserFilter.Effect = (setting.Value.Value as KshEffectRef)?.CreateEffectDef(ksh.FilterDefines);
                                Logger.Log($"ksh.convert({ chartPos }) set { key } { laserFilter.Effect?.GetType().Name ?? "nothing" }");
                            }
                            else Logger.Log($"ksh.convert({ chartPos }) effects disabled for { key }");
                        }
                        break;

                        case "filter-l": // NOTE(local): This is an extension, not originally supported in KSH. Used primarily for development purposes, but may also be exported to KSH should someone want to export back to that format.
                        case "filter-r": // NOTE(local): This is an extension, not originally supported in KSH. Used primarily for development purposes, but may also be exported to KSH should someone want to export back to that format.
                        {
                            Logger.Log($"ksh.convert({ chartPos }) skipping { key }.");
                        }
                        break;

                        case "filter-l_gain": // NOTE(local): This is an extension, not originally supported in KSH. Used primarily for development purposes, but may also be exported to KSH should someone want to export back to that format.
                        case "filter-r_gain": // NOTE(local): This is an extension, not originally supported in KSH. Used primarily for development purposes, but may also be exported to KSH should someone want to export back to that format.
                        {
                            Logger.Log($"ksh.convert({ chartPos }) skipping { key }.");
                        }
                        break;

                        case "chokkakuvol":
                        {
                            var slamVoume = chart[NscLane.LaserEvent].Add<SlamVolumeEvent>(chartPos);
                            slamVoume.Volume = setting.Value.ToInt() / 100.0f;
                            Logger.Log($"ksh.convert({ chartPos }) set { key } { setting.Value }");
                        } break;

                        case "laserrange_l": { laserIsExtended[0] = true; } break;
                        case "laserrange_r": { laserIsExtended[1] = true; } break;
                        
                        case "zoom_bottom":
                        {
                            var point = chart[NscLane.CameraZoom].Add<GraphPointEvent>(chartPos);
                            point.Value = setting.Value.ToInt() / 100.0f;
                            Logger.Log($"ksh.convert({ chartPos }) zoom { setting.Value }");
                        } break;
                        
                        case "zoom_top":
                        {
                            var point = chart[NscLane.CameraPitch].Add<GraphPointEvent>(chartPos);
                            point.Value = setting.Value.ToInt() / 100.0f;
                            Logger.Log($"ksh.convert({ chartPos }) pitch { setting.Value }");
                        } break;
                        
                        case "zoom_side":
                        {
                            var point = chart[NscLane.CameraOffset].Add<GraphPointEvent>(chartPos);
                            point.Value = setting.Value.ToInt() / 100.0f;
                            Logger.Log($"ksh.convert({ chartPos }) offset { setting.Value }");
                        } break;

                        case "roll": // NOTE(local): This is an extension, not originally supported in KSH. Used primarily for development purposes, but may also be exported to KSH should someone want to export back to that format.
                        {
                            var point = chart[NscLane.CameraTilt].Add<GraphPointEvent>(chartPos);
                            point.Value = setting.Value.ToInt() / 360.0f;
                            Logger.Log($"ksh.convert({ chartPos }) custom manual tilt { setting.Value }");
                        } break;

                        case "tilt":
                        {
                            var laserApps = chart[NscLane.LaserEvent].Add<LaserApplicationEvent>(chartPos);

                            string v = setting.Value.ToString();
                            if (v.StartsWith("keep_"))
                            {
                                laserApps.Application = LaserApplication.Additive | LaserApplication.KeepMax;
                                v = v.Substring(5);
                            }
                            
                            var laserParams = chart[NscLane.LaserEvent].Add<LaserParamsEvent>(chartPos);
                            laserParams.LaserIndex = LaserIndex.Both;

                            bool disableTilt = true;
                            switch (v)
                            {
                                default:
                                {
                                    if (int.TryParse(v, out int manualValue))
                                    {
                                        disableTilt = false;

                                        if (lastTiltEvent == null)
                                        {
                                            var startPoint = chart[NscLane.CameraTilt].Add<GraphPointEvent>(chartPos);
                                            startPoint.Value = 0;
                                        }

                                        var point = chart[NscLane.CameraTilt].Add<GraphPointEvent>(chartPos);
                                        point.Value = -manualValue * 14 / 360.0f;

                                        lastTiltEvent = point;
                                    }
                                } goto case "zero";

                                case "zero": laserParams.Params.Function = LaserFunction.Zero; break;
                                case "normal": laserParams.Params.Scale = LaserScale.Normal; break;
                                case "bigger": laserParams.Params.Scale = LaserScale.Bigger; break;
                                case "biggest": laserParams.Params.Scale = LaserScale.Biggest; break;
                            }

                            if (disableTilt && lastTiltEvent != null)
                            {
                            }
                        } break;

                        case "fx_sample":
                        {
                            Logger.Log($"ksh.convert({ chartPos }) skipping fx_sample.");
                        } break;

                        case "stop":
                        {
                            ControlPoint cp = chart.ControlPoints.GetOrCreate(chartPos, true);
                            // TODO(local): this breaks when there's another control point between here and there
                            chart.ControlPoints.GetOrCreate(chartPos + int.Parse(setting.Value.ToString()) / 192.0, true);

                            cp.StopChart = true;
                            lastCp = cp;

                            Logger.Log($"ksh.convert({ chartPos }) stop { int.Parse(setting.Value.ToString()) / 192.0 }");
                        } break;

                        case "lane_toggle":
                        {
                            Logger.Log($"ksh.convert({ chartPos }) skipping lane_toggle.");
                        } break;
                    }
                }

                for (int b = 0; b < 6; b++)
                {
                    bool isFx = b >= 4;
                    
                    var data = isFx ? tick.Fx[b - 4] : tick.Bt[b];

                    void CreateHold(tick_t endPos)
                    {
                        var state = buttonStates[b];

                        var startPos = state.StartPosition;
                        var button = chart[(HybridLabel)b].Add<ButtonEntity>(startPos, endPos - startPos);
                    }

                    switch (data.State)
                    {
                        case KshButtonState.Off:
                        {
                            if (buttonStates[b] != null)
                                CreateHold(chartPos);
                            buttonStates[b] = null;
                        } break;

                        case KshButtonState.Chip:
                        case KshButtonState.ChipSample:
                        {
                            var chip = chart[(HybridLabel)b].Add<ButtonEntity>(chartPos);
                            chip.Sample = chipHitSounds[b];
                            chip.SampleVolume = chipHitSoundsVolume[b];
                        } break;
                        
                        case KshButtonState.Hold:
                        {
                            if (buttonStates[b] == null)
                                buttonStates[b] = new TempButtonState(chartPos);
                        } break;
                    }
                }

                for (int l = 0; l < 2; l++)
                {
                    var data = tick.Laser[l];
                    var state = data.State;

                    tick_t CreateSegment(tick_t endPos, float endAlpha)
                    {
                        var startPos = laserStates[l].StartPosition;
                        float startAlpha = laserStates[l].StartAlpha;

                        var duration = endPos - startPos;
                        //if (duration <= tick_t.FromFraction(1, 32 * lastCp.BeatCount / lastCp.BeatKind))
                        if (laserStates[l].HiResTickCount <= 6 && startAlpha != endAlpha)
                        {
                            duration = 0;

                            if (laserStates[l].PreviousSlamDuration != 0)
                            {
                                var cDuration = laserStates[l].PreviousSlamDuration;

                                var connector = chart[(HybridLabel)(l + 6)].Add<AnalogEntity>(startPos, cDuration);
                                connector.InitialValue = startAlpha;
                                connector.FinalValue = startAlpha;
                                connector.RangeExtended = laserIsExtended[l];

                                startPos += cDuration;
                            }
                        }

                        var analog = chart[(HybridLabel)(l + 6)].Add<AnalogEntity>(startPos, duration);
                        analog.InitialValue = startAlpha;
                        analog.FinalValue = endAlpha;
                        analog.RangeExtended = laserIsExtended[l];
                        analog.Shape = laserStates[l].Shape;
                        analog.CurveA = laserStates[l].CurveA;
                        analog.CurveB = laserStates[l].CurveB;

                        return startPos + duration;
                    }

                    switch (state)
                    {
                        case KshLaserState.Inactive:
                        {
                            if (laserStates[l] != null)
                            {
                                laserStates[l] = null;
                                laserIsExtended[l] = false;
                            }
                        } break;

                        case KshLaserState.Lerp:
                        {
                            laserStates[l].HiResTickCount += (192 * lastCp.BeatCount / lastCp.BeatKind) / tickRef.MaxIndex;
                        } break;
                        
                        case KshLaserState.Position:
                        {
                            var alpha = data.Position;
                            var startPos = chartPos;

                            tick_t prevSlamDuration = 0;
                            if (laserStates[l] != null)
                            {
                                startPos = CreateSegment(chartPos, alpha.Alpha);
                                if (startPos != chartPos)
                                    prevSlamDuration = chartPos - startPos;
                            }

                            var ls = laserStates[l] = new TempLaserState(startPos, lastCp)
                            {
                                StartAlpha = alpha.Alpha,
                                HiResTickCount = (192 * lastCp.BeatCount / lastCp.BeatKind) / tickRef.MaxIndex,
                                PreviousSlamDuration = prevSlamDuration,
                                CurveResolution = 0,
                            };

                            for (int i = tick.Comments.Count - 1; i >= 0; i--)
                            {
                                string c = tick.Comments[i];
                                if (!c.StartsWith("LaserShape "))
                                    continue;
                                c = c.Substring("LaserShape ".Length).Trim();

                                if (c.StartsWith("ThreePoint"))
                                {
                                    float a = 0.5f, b = 0.5f;
                                    if (c != "ThreePoint" && c.TrySplit(' ', out string tp, out string sa, out string sb))
                                    {
                                        float.TryParse(sa, out a);
                                        float.TryParse(sb, out b);
                                    }

                                    ls.Shape = CurveShape.ThreePoint;
                                    ls.CurveA = a;
                                    ls.CurveB = b;
                                }
                                else if (c.StartsWith("Cosine"))
                                {
                                    float a = 0.0f;
                                    if (c != "Cosine" && c.TrySplit(' ', out string tp, out string sa))
                                    {
                                        float.TryParse(sa, out a);
                                    }

                                    ls.Shape = CurveShape.Cosine;
                                    ls.CurveA = a;
                                }
                                else continue;
                                break;
                            }
                        } break;
                    }
                }

                switch (tick.Add.Kind)
                {
                    case KshAddKind.None: break;

                    case KshAddKind.Spin:
                    {
                        tick_t duration = tick_t.FromFraction(tick.Add.Duration * 2, 192);
                        var spin = chart[NscLane.HighwayEvent].Add<SpinImpulseEvent>(chartPos, duration);
                        spin.Direction = (AngularDirection)tick.Add.Direction;
                    } break;

                    case KshAddKind.Swing:
                    {
                        tick_t duration = tick_t.FromFraction(tick.Add.Duration * 2, 192);
                        var swing = chart[NscLane.HighwayEvent].Add<SwingImpulseEvent>(chartPos, duration);
                        swing.Direction = (AngularDirection)tick.Add.Direction;
                        swing.Amplitude = tick.Add.Amplitude * 70 / 100.0f;
                    } break;

                    case KshAddKind.Wobble:
                    {
                        tick_t duration = tick_t.FromFraction(tick.Add.Duration, 192);
                        var wobble = chart[NscLane.HighwayEvent].Add<WobbleImpulseEvent>(chartPos, duration);
                        wobble.Direction = (LinearDirection)tick.Add.Direction;
                        wobble.Amplitude = tick.Add.Amplitude / 250.0f;
                        wobble.Decay = (Decay)tick.Add.Decay;
                        wobble.Frequency = tick.Add.Frequency;
                    } break;
                }
            }

            Logger.Log($"ksh.convert end");
            return chart;
        }
    }
}
