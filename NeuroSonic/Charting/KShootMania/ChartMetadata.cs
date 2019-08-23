#define SDVX_SPECIAL_DIFFS_SEPARATE

using System;
using System.IO;
using System.Numerics;

namespace NeuroSonic.Charting.KShootMania
{
    public enum Difficulty
    {
        Light,
        Challenge,
        Extended,
        Infinite,
    }

    public static class DifficultyExt
    {
        public static int ToDifficultyIndex(this Difficulty d, string fileNameContext = null)
        {
            if (fileNameContext != null)
            {
                fileNameContext = fileNameContext.ToLower();
                switch (d)
                {
                    case Difficulty.Infinite:
                        if (fileNameContext.Contains("inf") || fileNameContext.Contains("grv")
                         || fileNameContext.Contains("hvn") || fileNameContext.Contains("vvd"))
#if SDVX_SPECIAL_DIFFS_SEPARATE
                            return 4;
#else
                            return 3;
#endif
                        return 3;
                }
            }

            switch (d)
            {
                case Difficulty.Light: return 0;
                case Difficulty.Challenge: return 1;
                case Difficulty.Extended: return 2;
            }

            return 0;
        }

        public static string ToDifficultyString(this Difficulty d, string fileNameContext = null)
        {
            if (fileNameContext != null)
            {
                fileNameContext = fileNameContext.ToLower();
                switch (d)
                {
                    case Difficulty.Light:
                        if (fileNameContext.Contains("nov"))
                            return "Novice";
                        break;
                    case Difficulty.Challenge:
                        if (fileNameContext.Contains("adv"))
                            return "Advanced";
                        break;
                    case Difficulty.Extended:
                        if (fileNameContext.Contains("exh"))
                            return "Exhaust";
                        break;
                    case Difficulty.Infinite:
                        if (fileNameContext.Contains("inf"))
                            return "Infinite";
                        else if (fileNameContext.Contains("grv"))
                            return "Gravity";
                        else if (fileNameContext.Contains("hvn"))
                            return "Heavenly";
                        else if (fileNameContext.Contains("vvd"))
                            return "Vivid";
                        else if (fileNameContext.Contains("mxm"))
                            return "Maximum";
                        break;
                }
            }

            switch (d)
            {
                case Difficulty.Light: return "Light";
                case Difficulty.Challenge: return "Challenge";
                case Difficulty.Extended: return "Extended";
                case Difficulty.Infinite: return "Infinite";
            }

            return "XX";
        }

        public static string ToShortString(this Difficulty d, string fileNameContext = null)
        {
            if (fileNameContext != null)
            {
                fileNameContext = fileNameContext.ToLower();
                switch (d)
                {
                    case Difficulty.Light:
                        if (fileNameContext.Contains("nov"))
                            return "NOV";
                        break;
                    case Difficulty.Challenge:
                        if (fileNameContext.Contains("adv"))
                            return "ADV";
                        break;
                    case Difficulty.Extended:
                        if (fileNameContext.Contains("exh"))
                            return "EXH";
                        break;
                    case Difficulty.Infinite:
                        if (fileNameContext.Contains("inf"))
                            return "INF";
                        else if (fileNameContext.Contains("grv"))
                            return "GRV";
                        else if (fileNameContext.Contains("hvn"))
                            return "HVN";
                        else if (fileNameContext.Contains("vvd"))
                            return "VVD";
                        else if (fileNameContext.Contains("mxm"))
                            return "MXM";
                        break;
                }
            }

            switch (d)
            {
                case Difficulty.Light: return "LT";
                case Difficulty.Challenge: return "CH";
                case Difficulty.Extended: return "EX";
                case Difficulty.Infinite: return "IN";
            }

            return "XX";
        }

        public static Vector3 GetColor(this Difficulty d, string fileNameContext = null)
        {
            if (fileNameContext != null)
            {
                fileNameContext = fileNameContext.ToLower();
                switch (d)
                {
                    case Difficulty.Light:
                        if (fileNameContext.Contains("nov"))
                            return new Vector3(0.8f, 0.4f, 1);
                        break;
                    case Difficulty.Challenge:
                        if (fileNameContext.Contains("adv"))
                            return new Vector3(1, 1, 0.25f);
                        break;
                    case Difficulty.Extended:
                        if (fileNameContext.Contains("exh"))
                            return new Vector3(1, 0.2f, 0.4f);
                        break;
                    case Difficulty.Infinite:
                        if (fileNameContext.Contains("inf"))
                            return new Vector3(1, 0.5f, 1);
                        else if (fileNameContext.Contains("grv"))
                            return new Vector3(1, 0.4f, 0);
                        else if (fileNameContext.Contains("hvn"))
                            return new Vector3(0, 0.55f, 1);
                        else if (fileNameContext.Contains("vvd"))
                            return new Vector3(1, 0.1f, 0.55f);
                        else if (fileNameContext.Contains("mxm"))
                            return new Vector3(0.95f);
                        break;
                }
            }

            switch (d)
            {
                case Difficulty.Light: return new Vector3(0, 1, 0);
                case Difficulty.Challenge: return new Vector3(1, 1, 0);
                case Difficulty.Extended: return new Vector3(1, 0, 0);
                case Difficulty.Infinite: return new Vector3(0.5f, 1, 1);
            }

            return Vector3.One;
        }
    }

    public sealed class KshChartMetadata
    {
        public static KshChartMetadata Create(StreamReader reader)
        {
            var meta = new KshChartMetadata();

            string line;
            while ((line = reader.ReadLine()) != KshChart.SEP && line != null)
            {
                if (string.IsNullOrEmpty(line))
                    continue;
                line = line.Trim();

                if (line.TrySplit('=', out string key, out string value))
                    meta.Set(key.Trim(), value.Trim());
            }

            return meta;
        }

        public string Title = "";
        public string Artist = "";
        public string EffectedBy = "";

        public string JacketPath;
        public string Illustrator = "";

        public Difficulty Difficulty = Difficulty.Light;
        public int Level = 1;

        public string BeatsPerMinute = "";
        public int Numerator = 4;
        public int Denominator = 4;

        public string MusicFile;
        public string MusicFileNoFx;
        public int MusicVolume = 100;

        public int OffsetMillis;

        public string Background = "";
        // Layer

        public int PreviewOffsetMillis;
        public int PreviewLengthMillis;

        public int PFilterGain = 100;
        public string FilterType = "";
            
        public int SlamAutoVolume = 100;
        public int SlamVolume = 100;

        public string Tags = "";

        public double? HiSpeedBpm;

        public void Set(string name, string value)
        {
            switch (name)
            {
                case "title": Title = value; return;
                case "artist": Artist = value; return;
                case "effect": EffectedBy = value; return;

                case "jacket": JacketPath = value; return;
                case "illustrator": Illustrator = value; return;

                case "difficulty":
                {
                    var dif = Difficulty.Light;
                    if (value == "challenge")
                        dif = Difficulty.Challenge;
                    else if (value == "extended")
                        dif = Difficulty.Extended;
                    else if (value == "infinite")
                        dif = Difficulty.Infinite;
                    Difficulty = dif;
                } return;
                case "level": Level = int.Parse(value); return;
                        
                case "t": BeatsPerMinute = value; return;
                case "beat":
                    if (value.TrySplit('/', out string n, out string d))
                    {
                        Numerator = int.Parse(n);
                        Denominator = int.Parse(d);
                    }
                    return;

                case "m":
                {
                    if (value.TrySplit(';', out string nofx, out string fx))
                    {
                        MusicFileNoFx = nofx;
                        MusicFile = fx;

                        if (fx.TrySplit(';', out fx, out string _))
                        { // do something with the last file
                        }
                    }
                    else MusicFileNoFx = value;
                } return;

                case "mvol": MusicVolume = int.Parse(value); return;
                        
                case "o": OffsetMillis = int.Parse(value); return;

                case "po": PreviewOffsetMillis = int.Parse(value); return;
                case "plength": PreviewLengthMillis = int.Parse(value); return;

                case "pfiltergain": PFilterGain = int.Parse(value); return;
                case "filtertype": FilterType = value; return;

                case "chokkakuvol": SlamVolume = int.Parse(value); return;

                case "tags": Tags = value; return;

                case "to": HiSpeedBpm = double.Parse(value); return;
            }
        }
    }
}
