using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using theori.Audio.Effects;
using theori.Charting.Effects;

namespace NeuroSonic.Charting.KShootMania
{
    public class KshBlock
    {
        public readonly List<KshTick> Ticks = new List<KshTick>();

        public int TickCount => Ticks.Count;
        public KshTick this[int index] => Ticks[index];
    }

    public class KshTick
    {
        public readonly List<string> Comments = new List<string>();
        public readonly List<KshTickSetting> Settings = new List<KshTickSetting>();

        public readonly KshButtonData[] Bt = new KshButtonData[4];
        public readonly KshButtonData[] Fx = new KshButtonData[2];
        public readonly KshLaserData[] Laser = new KshLaserData[2];

        public KshAddData Add;
    }

    public struct KshTickSetting
    {
        public string Key;
        public Variant Value;

        public KshTickSetting(string key, Variant value)
        {
            Key = key;
            Value = value;
        }
    }

    public enum KshButtonState
    {
        Off, Chip, Hold, ChipSample,
    }
    
    public enum KshLaserState
    {
        Inactive, Lerp, Position,
    }

    public struct KshButtonData
    {
        public KshButtonState State;
    }

    public enum KshOldFxHoldKind
    {
        None = 0,

        BitCrusher = 'B',
        
        Gate_4  = 'G',
        Gate_8  = 'H',
        Gate_16 = 'I',
        Gate_32 = 'J',
        Gate_12 = 'K',
        Gate_24 = 'L',
        
        Retrigger_8  = 'S',
        Retrigger_16 = 'T',
        Retrigger_32 = 'U',
        Retrigger_12 = 'V',
        Retrigger_24 = 'W',

        Phaser = 'Q',
        Flanger = 'F',
        Wobble = 'X',
        SideChain = 'D',
        TapeStop = 'A',
    }

    static class KshOldFxHoldKind_Ext
    {
        public static void GetEffectInfo(this KshOldFxHoldKind kind, out string name, out string param)
        {
            string str = kind.ToString();
            if (!str.TrySplit('_', out name, out param))
            {
                name = str;
                param = null;
            }
        }
    }

    public struct KshLaserData
    {
        public KshLaserState State;
        public KshLaserPosition Position;
    }

    public struct KshLaserPosition
    {
        public const int Resolution = 51;

        class Chars : Dictionary<char, int>
        {
            public int NumChars;

            public Chars()
            {
                void AddRange(char start, char end)
                {
                    for (char c = start; c <= end; c++)
                        this[c] = NumChars++;
                }

			    AddRange('0', '9');
			    AddRange('A', 'Z');
			    AddRange('a', 'o');

                Debug.Assert(NumChars == Resolution);
            }
        }

        static Chars chars = new Chars();

        public float Alpha
        {
            get => value / (float)(Resolution - 1);
            set => Value = (int)Math.Round(value * (Resolution - 1));
        }

        private int value;
        public int Value
        {
            get => value;
            set => this.value = MathL.Clamp(value, 0, chars.NumChars - 1);
        }

        public char Image
        {
            get
            {
                int v = Value;
                return chars.Where(kvp => kvp.Value == v).Single().Key;
            }

            set => chars.TryGetValue(value, out this.value);
        }

        public KshLaserPosition(int value)
        {
            this.value = MathL.Clamp(value, 0, chars.NumChars - 1);
        }

        public KshLaserPosition(char image)
        {
            chars.TryGetValue(image, out value);
        }
    }

    public enum KshAddKind
    {
        None, Spin, Swing, Wobble
    }

    public struct KshAddData
    {
        public KshAddKind Kind;
        public int Direction;
        public int Duration;
        public int Amplitude;
        public int Frequency;
        public int Decay;
    }

    public struct KshTickRef
    {
        public int Block, Index, MaxIndex;
        public KshTick Tick;
    }

    public enum KshEffectKind
    {
        None = 0,

        Retrigger,
        Gate,
        Flanger,
        PitchShift,
        BitCrusher,
        Phaser,
        Wobble,
        TapeStop,
        Echo,
        SideChain,
        Peak,
        LowPass,
        HighPass
    }

    public class KshEffectRef
    {
        public readonly string Name;
        public readonly string Param;

        public KshEffectRef(string name, string param)
        {
            Name = name;
            Param = param;
        }

        public EffectDef CreateEffectDef(Dictionary<string, KshEffectDef> context)
        {
            var effectDef = context[Name];

            var effectKind = effectDef.EffectKind;
            var pars = effectDef.EffectParams;

            T GetEffectParam<T>(string parsKey, T parsDef) where T : IEffectParam
            {
                if (pars.TryGetValue(parsKey, out var parsValue) && parsValue is T valueT)
                    return valueT;
                return parsDef;
            }

            switch (effectKind)
            {
                case KshEffectKind.Retrigger:
                {
                    var step = Param != null ? 1.0f / int.Parse(Param) : GetEffectParam<EffectParamF>("waveLength", 0.25f);
                    return new RetriggerDef(
                        GetEffectParam<EffectParamF>("mix", 1.0f),
                        GetEffectParam<EffectParamF>("rate", 0.7f),
                        GetEffectParam<EffectParamF>("waveLength", step),
                        GetEffectParam<EffectParamF>("updatePeriod", 0.5f)
                        );
                }

                case KshEffectKind.Gate:
                {
                    var step = Param != null ? 1.0f / int.Parse(Param) : GetEffectParam<EffectParamF>("waveLength", 0.125f);
                    return new GateDef(
                        GetEffectParam<EffectParamF>("mix", 1.0f),
                        GetEffectParam<EffectParamF>("rate", 0.7f),
                        GetEffectParam<EffectParamF>("waveLength", step)
                        );
                }

                case KshEffectKind.Flanger: return new FlangerDef(GetEffectParam<EffectParamF>("mix", 1.0f));

                case KshEffectKind.BitCrusher:
                {
                    var reduction = Param != null ? int.Parse(Param) : GetEffectParam<EffectParamI>("reduction", 4);
                    return new BitCrusherDef(GetEffectParam<EffectParamF>("mix", 1.0f), reduction);
                }

                case KshEffectKind.Phaser: return new PhaserDef(GetEffectParam<EffectParamF>("mix", 0.5f));

                case KshEffectKind.Wobble:
                {
                    var step = Param != null ? 1.0f / int.Parse(Param) : GetEffectParam<EffectParamF>("waveLength", 1.0f / 12);
                    return new WobbleDef(GetEffectParam<EffectParamF>("mix", 1.0f), GetEffectParam<EffectParamF>("waveLength", step));
                }

                case KshEffectKind.TapeStop:
                {
                    var speed = Param != null ? 16.0f / MathL.Max(int.Parse(Param), 1) : GetEffectParam<EffectParamF>("speed", 50.0f);
                    return new TapeStopDef(GetEffectParam<EffectParamF>("mix", 1.0f), speed);
                }

                case KshEffectKind.SideChain:
                {
                    var step = Param != null ? 1.0f / int.Parse(Param) : GetEffectParam<EffectParamF>("waveLength", 0.25f);
                    return new SideChainDef(GetEffectParam<EffectParamF>("mix", 1.0f), 1.0f, step);
                }

                case KshEffectKind.Peak: return BiQuadFilterDef.CreateDefaultPeak();
                case KshEffectKind.LowPass: return BiQuadFilterDef.CreateDefaultLowPass();
                case KshEffectKind.HighPass: return BiQuadFilterDef.CreateDefaultHighPass();

                default: return null;
            }
        }
    }

    public class KshEffectDef
    {
        public readonly KshEffectKind EffectKind;
        public readonly Dictionary<string, IEffectParam> EffectParams;

        public KshEffectDef(KshEffectKind effectKind, Dictionary<string, IEffectParam> pars)
        {
            EffectKind = effectKind;
            EffectParams = pars ?? new Dictionary<string, IEffectParam>();
        }
    }

    /// <summary>
    /// Contains all relevant data for a single chart.
    /// </summary>
    public sealed class KshChart : IEnumerable<KshTickRef>
    {
        internal const string SEP = "--";

        public static KshChart CreateFromFile(string fileName)
        {
            using (var reader = File.OpenText(fileName))
                return Create(fileName, reader);
        }

        public static KshChart Create(string fileName, StreamReader reader)
        {
            var chart = new KshChart
            {
                FileName = fileName,
                Metadata = KshChartMetadata.Create(reader)
            };

            chart.FxDefines["Retrigger"] = new KshEffectDef(KshEffectKind.Retrigger, null);
            chart.FxDefines["Gate"] = new KshEffectDef(KshEffectKind.Gate, null);
            chart.FxDefines["Flanger"] = new KshEffectDef(KshEffectKind.Flanger, null);
            chart.FxDefines["PitchShift"] = new KshEffectDef(KshEffectKind.PitchShift, null);
            chart.FxDefines["BitCrusher"] = new KshEffectDef(KshEffectKind.BitCrusher, null);
            chart.FxDefines["Wobble"] = new KshEffectDef(KshEffectKind.Wobble, null);
            chart.FxDefines["Phaser"] = new KshEffectDef(KshEffectKind.Phaser, null);
            chart.FxDefines["TapeStop"] = new KshEffectDef(KshEffectKind.TapeStop, null);
            chart.FxDefines["Echo"] = new KshEffectDef(KshEffectKind.Echo, null);
            chart.FxDefines["SideChain"] = new KshEffectDef(KshEffectKind.SideChain, null);

            chart.FilterDefines["peak"] = new KshEffectDef(KshEffectKind.Peak, null);
            chart.FilterDefines["lpf1"] = new KshEffectDef(KshEffectKind.LowPass, null);
            chart.FilterDefines["hpf1"] = new KshEffectDef(KshEffectKind.HighPass, null);
            chart.FilterDefines["bitc"] = new KshEffectDef(KshEffectKind.BitCrusher, new Dictionary<string, IEffectParam>()
            {
                ["reduction"] = new EffectParamI(4, 45, Ease.InExpo),
            });
            chart.FilterDefines["fx;bitc"] = chart.FilterDefines["bitc"];

            var block = new KshBlock();
            var tick = new KshTick();

            string line, lastFx = "00";
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                line = line.Trim();

                if (line[0] == '#')
                {
                    Dictionary<string, IEffectParam> GetParameterList(string args, out KshEffectKind effectKind)
                    {
                        var result = new Dictionary<string, IEffectParam>();
                        effectKind = KshEffectKind.None;

                        foreach (string a in args.Split(';'))
                        {
                            if (!a.TrySplit('=', out string k, out string v))
                                continue;

                            k = k.Trim();
                            v = v.Trim();

                            if (k == "type")
                            {
                                Logger.Log($"ksh fx type: { v }");
                                switch (v.ToLower())
                                {
                                    case "retrigger": effectKind = KshEffectKind.Retrigger; break;
                                    case "gate": effectKind = KshEffectKind.Gate; break;
                                    case "flanger": effectKind = KshEffectKind.Flanger; break;
                                    case "pitchshift": effectKind = KshEffectKind.PitchShift; break;
                                    case "bitcrusher": effectKind = KshEffectKind.BitCrusher; break;
                                    case "wobble": effectKind = KshEffectKind.Wobble; break;
                                    case "phaser": effectKind = KshEffectKind.Phaser; break;
                                    case "tapestop": effectKind = KshEffectKind.TapeStop; break;
                                    case "echo": effectKind = KshEffectKind.Echo; break;
                                    case "sidechain": effectKind = KshEffectKind.SideChain; break;
                                }
                            }
                            else
                            {
                                // NOTE(local): We aren't worried about on/off state for now, if ever
                                if (v.Contains('>')) v = v.Substring(v.IndexOf('>') + 1).Trim();
                                bool isRange = v.TrySplit('-', out string v0, out string v1);

                                // TODO(local): this will ONLY allow ranges of the same type, so 0.5-1/8 is illegal (but are these really ever used?)
                                // (kinda makes sense for Hz-kHz but uh shh)
                                IEffectParam pv;
                                if (v.Contains("on") || v.Contains("off"))
                                {
                                    if (isRange)
                                        pv = new EffectParamI(v0.Contains("on") ? 1 : 0,
                                            v1.Contains("on") ? 1 : 0, Ease.Linear);
                                    else pv = new EffectParamI(v.Contains("on") ? 1 : 0);
                                }
                                else if (v.Contains('/'))
                                {
                                    if (isRange)
                                    {
                                        pv = new EffectParamX(
                                            int.Parse(v0.Substring(v0.IndexOf('/') + 1)),
                                            int.Parse(v1.Substring(v1.IndexOf('/') + 1)));
                                    }
                                    else pv = new EffectParamX(int.Parse(v.Substring(v.IndexOf('/') + 1)));
                                }
                                else if (v.Contains('%'))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(int.Parse(v0.Substring(0, v0.IndexOf('%'))) / 100.0f,
                                            int.Parse(v1.Substring(0, v1.IndexOf('%'))) / 100.0f, Ease.Linear);
                                    else pv = new EffectParamF(int.Parse(v.Substring(0, v.IndexOf('%'))) / 100.0f);
                                }
                                else if (v.Contains("samples"))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(int.Parse(v0.Substring(0, v0.IndexOf("samples"))),
                                            int.Parse(v1.Substring(0, v1.IndexOf("samples"))), Ease.Linear);
                                    else pv = new EffectParamF(int.Parse(v.Substring(0, v.IndexOf("samples"))));
                                }
                                else if (v.Contains("ms"))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(int.Parse(v0.Substring(0, v0.IndexOf("ms"))) / 1000.0f,
                                            int.Parse(v1.Substring(0, v1.IndexOf("ms"))) / 1000.0f, Ease.Linear);
                                    else pv = new EffectParamF(int.Parse(v.Substring(0, v.IndexOf("ms"))) / 1000.0f);
                                }
                                else if (v.Contains("s"))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(int.Parse(v0.Substring(0, v0.IndexOf("s"))) / 1000.0f,
                                            int.Parse(v1.Substring(0, v1.IndexOf("s"))) / 1000.0f, Ease.Linear);
                                    else pv = new EffectParamF(int.Parse(v.Substring(0, v.IndexOf("s"))) / 1000.0f);
                                }
                                else if (v.Contains("kHz"))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(float.Parse(v0.Substring(0, v0.IndexOf("kHz"))) * 1000.0f,
                                            float.Parse(v1.Substring(0, v1.IndexOf("kHz"))) * 1000.0f, Ease.Linear);
                                    else pv = new EffectParamF(float.Parse(v.Substring(0, v.IndexOf("kHz"))) * 1000.0f);
                                }
                                else if (v.Contains("Hz"))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(float.Parse(v0.Substring(0, v0.IndexOf("Hz"))),
                                            float.Parse(v1.Substring(0, v1.IndexOf("Hz"))), Ease.Linear);
                                    else pv = new EffectParamF(float.Parse(v.Substring(0, v.IndexOf("Hz"))));
                                }
                                else if (v.Contains("dB"))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(float.Parse(v0.Substring(0, v0.IndexOf("dB"))),
                                            float.Parse(v1.Substring(0, v1.IndexOf("dB"))), Ease.Linear);
                                    else pv = new EffectParamF(float.Parse(v.Substring(0, v.IndexOf("dB"))));
                                }
                                else if (float.TryParse(isRange ? v0 : v, out float floatValue))
                                {
                                    if (isRange)
                                        pv = new EffectParamF(floatValue, float.Parse(v1), Ease.Linear);
                                    else pv = new EffectParamF(floatValue);
                                }
                                else pv = new EffectParamS(v);

                                Logger.Log($"  ksh fx param: { k } = { pv }");
                                result[k] = pv;
                            }
                        }
                        return result;
                    }

                    if (!line.TrySplit(' ', out string defKind, out string defKey, out string argList))
                        continue;

                    Logger.Log($">> ksh { defKind } \"{ defKey }\"");

                    var pars = GetParameterList(argList, out KshEffectKind effectType);
                    KshEffectDef def = new KshEffectDef(effectType, pars);
                    
                    if (defKind == "#define_fx")
                        chart.FxDefines[defKey] = def;
                    else if (defKind == "#define_filter")
                        chart.FilterDefines[defKey] = def;
                }
                if (line == SEP)
                {
                    chart.m_blocks.Add(block);
                    block = new KshBlock();
                }
                else if (line.StartsWith("//"))
                {
                    tick.Comments.Add(line.Substring(2).Trim());
                }
                if (line.TrySplit('=', out string key, out string value))
                {
                    // defined fx should probably be named different than the defaults,
                    //  so it's like slightly safe to assume that failing to create
                    //  a built-in definition from this for either means its a defined effect?
                    if (key == "fx-l" || key == "fx-r" || key == "filtertype")
                    {
                        if (string.IsNullOrWhiteSpace(value))
                            tick.Settings.Add(new KshTickSetting(key, Variant.Null));
                        else
                        {
                            string effectName = value, effectParam = null;
                            if (value != "fx;bitc" && value.TrySplit(';', out string name, out string param))
                            {
                                effectName = name;
                                effectParam = param;
                            }

                            var effectRef = new Variant(new KshEffectRef(effectName, effectParam));
                            tick.Settings.Add(new KshTickSetting(key, effectRef));
                        }
                    }
                    else tick.Settings.Add(new KshTickSetting(key, value));
                }
                else
                {
                    if (!line.TrySplit('|', out string bt, out string fx, out string vol))
                        continue;

                    if (vol.Length > 2)
                    {
                        string add = vol.Substring(2);
                        vol = vol.Substring(0, 2);

                        if (add.Length >= 2)
                        {
                            string[] args = add.Substring(2).Split(';');

                            char c = add[0];
                            switch (c)
                            {
                                case '@':
                                {
                                    char d = add[1];
                                    switch (d)
                                    {
                                        case '(': case ')': tick.Add.Kind = KshAddKind.Spin; break;
                                        case '<': case '>': tick.Add.Kind = KshAddKind.Swing; break;
                                    }
                                    switch (d)
                                    {
                                        case '(': case '<': tick.Add.Direction = -1; break;
                                        case ')': case '>': tick.Add.Direction =  1; break;
                                    }
                                    ParseArg(0, out tick.Add.Duration);
                                    tick.Add.Amplitude = 100;
                                } break;
                                
                                case 'S':
                                {
                                    char d = add[1];
                                    tick.Add.Kind = KshAddKind.Wobble;
                                    tick.Add.Direction = d == '<' ? -1 : (d == '>' ? 1 : 0);
                                    ParseArg(0, out tick.Add.Duration);
                                    ParseArg(1, out tick.Add.Amplitude);
                                    ParseArg(2, out tick.Add.Frequency);
                                    ParseArg(3, out tick.Add.Decay);
                                } break;
                            }

                            void ParseArg(int i, out int v)
                            {
                                if (args.Length > i) int.TryParse(args[i], out v);
                                else v = 0;
                            }
                        }
                    }

                    for (int i = 0; i < MathL.Min(4, bt.Length); i++)
                    {
                        char c = bt[i];
                        switch (c)
                        {
                            case '0': tick.Bt[i].State = KshButtonState.Off; break;
                            case '1': tick.Bt[i].State = KshButtonState.Chip; break;
                            case '2': tick.Bt[i].State = KshButtonState.Hold; break;
                        }
                    }

                    for (int i = 0; i < MathL.Min(2, fx.Length); i++)
                    {
                        char c = fx[i];
                        switch (c)
                        {
                            case '0': tick.Fx[i].State = KshButtonState.Off; break;
                            case '1': tick.Fx[i].State = KshButtonState.Hold; break;
                            case '2': tick.Fx[i].State = KshButtonState.Chip; break;
                            case '3': tick.Fx[i].State = KshButtonState.ChipSample; break;
                                
                            default:
                            {
                                var kind = (KshOldFxHoldKind)c;
                                if (Enum.IsDefined(typeof(KshOldFxHoldKind), kind) && kind != KshOldFxHoldKind.None)
                                {
                                    tick.Fx[i].State = KshButtonState.Hold;
                                    if (lastFx[i] != c)
                                    {
                                        kind.GetEffectInfo(out string effectName, out string effectParam);
                                        var effectRef = new Variant(new KshEffectRef(effectName, effectParam));

                                        tick.Settings.Add(new KshTickSetting(i == 0 ? "fx-l" : "fx-r", effectRef));
                                    }
                                }
                            } break;
                        }
                    }
                    lastFx = fx;

                    for (int i = 0; i < MathL.Min(2, vol.Length); i++)
                    {
                        char c = vol[i];
                        switch (c)
                        {
                            case '-': tick.Laser[i].State = KshLaserState.Inactive; break;
                            case ':': tick.Laser[i].State = KshLaserState.Lerp; break;
                            default:
                            {
                                tick.Laser[i].State = KshLaserState.Position;
                                tick.Laser[i].Position.Image = c;
                            } break;
                        }
                    }

                    block.Ticks.Add(tick);
                    tick = new KshTick();
                }
            }

            return chart;
        }

        public string FileName;
        public KshChartMetadata Metadata;

        private List<KshBlock> m_blocks = new List<KshBlock>();
        public KshTick this[int block, int tick] => m_blocks[block][tick];

        public readonly Dictionary<string, KshEffectDef> FxDefines = new Dictionary<string, KshEffectDef>();
        public readonly Dictionary<string, KshEffectDef> FilterDefines = new Dictionary<string, KshEffectDef>();
        
        public int BlockCount => m_blocks.Count;

        IEnumerator<KshTickRef> IEnumerable<KshTickRef>.GetEnumerator() => new TickEnumerator(this);
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<KshTickRef>)this).GetEnumerator();

        class TickEnumerator : IEnumerator<KshTickRef>
        {
            private KshChart m_chart;
            private int m_block, m_tick = -1;
            
            object IEnumerator.Current => Current;
            public KshTickRef Current => new KshTickRef()
            {
                Block = m_block,
                Index = m_tick,
                MaxIndex = m_chart.m_blocks[m_block].TickCount,
                Tick = m_chart[m_block, m_tick],
            };

            public TickEnumerator(KshChart c)
            {
                m_chart = c;
            }

            public void Dispose() => m_chart = null;

            public bool MoveNext()
            {
                if (m_tick == m_chart.m_blocks[m_block].TickCount - 1)
                {
                    m_block++;
                    m_tick = 0;

                    return m_block < m_chart.m_blocks.Count;
                }
                else m_tick++;

                return true;
            }

            public void Reset()
            {
                m_block = 0;
                m_tick = 0;
            }
        }
    }
}
