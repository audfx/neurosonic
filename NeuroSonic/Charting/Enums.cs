using theori;
using theori.Charting;

namespace NeuroSonic.Charting
{
    public static class NscLane
    {
        public static readonly HybridLabel HighwayEvent = nameof(HighwayEvent);
        public static readonly HybridLabel ButtonEvent = nameof(ButtonEvent);
        public static readonly HybridLabel LaserEvent = nameof(LaserEvent);

        public static readonly HybridLabel CameraZoom = nameof(CameraZoom);
        public static readonly HybridLabel CameraPitch = nameof(CameraPitch);
        public static readonly HybridLabel CameraOffset = nameof(CameraOffset);
        public static readonly HybridLabel CameraTilt = nameof(CameraTilt);
    }

    public enum Damping : byte
    {
        /// <summary>
        /// Immediately applies the target value.
        /// </summary>
        Off = 0,
        /// <summary>
        /// Slowly interpolates towards the target value,
        /// </summary>
        Slow,
        /// <summary>
        /// Quickly interpolates towards the target value.
        /// </summary>
        Fast,
    }

    public enum Decay : byte
    {
        /// <summary>
        /// The function will not decay over time.
        /// </summary>
        Off = 0,
        /// <summary>
        /// The function will decay to half its original
        ///  amplitude by the end of its duration.
        /// </summary>
        OnSlow = 1,
        /// <summary>
        /// The function will decay to 0 amplitude by
        ///  the end of its duration.
        /// </summary>
        On = 2,
    }

    [System.Flags]
    public enum LaserApplication : ushort
    {
        /// <summary>
        /// Ignore the laser inputs.
        /// </summary>
        Zero = 0,

        /// <summary>
        /// Add both processed laser inputs together.
        /// </summary>
        Additive = 0x0001,

        /// <summary>
        /// Take input values from the first non-zero laser input only.
        /// 
        /// For Example:
        /// If the left laser sends non-zero input first, then
        ///  only its values are accepted.
        /// Should the left laser then return to the zero position,
        ///  the right laser could take control instead.
        /// </summary>
        Initial = 0x0002,

        /// <summary>
        /// Selected only the left laser input.
        /// </summary>
        Left = 0x0003,
        
        /// <summary>
        /// Selected only the right laser input.
        /// </summary>
        Right = 0x0004,

        /// <summary>
        /// Keeps the maximum value (read: farthest from zero) of the
        ///  laser output only for the target direction.
        /// If the roll value is negative, then only lesser negative
        ///  values are applied; positive roll values continue with
        ///  greater positive values similarly.
        /// </summary>
        KeepMax = 0x1000,

        /// <summary>
        /// Keeps the minimum value (read: nearest to zero) of the
        ///  laser output only for the target direction.
        /// If the roll value is negative, then only greater negative
        ///  values are applied; positive roll values continue with
        ///  lesser positive values similarly.
        /// </summary>
        KeepMin = 0x2000,
        
        ApplicationMask = 0x0FFF,
        FlagMask = 0xF000,
    }

    public enum LaserFunction : byte
    {
        /// <summary>
        /// Keep the input value as-is.
        /// </summary>
        Source,
        /// <summary>
        /// The input value is discarded entirely.
        /// </summary>
        Zero,
        /// <summary>
        /// Negate the input value.
        /// </summary>
        NegativeSource,
        /// <summary>
        /// Subtract the input value from 1.
        /// </summary>
        OneMinusSource,
    }

    public enum LaserScale : byte
    {
        /// <summary>
        /// Multiply the result by the "normal" laser amplitude.
        /// </summary>
        Normal,
        /// <summary>
        /// Multiply the result value by half of the "normal" laser amplitude.
        /// </summary>
        Smaller,
        /// <summary>
        /// Multiply the result value by 1.5 of the "normal" laser amplitude.
        /// </summary>
        Bigger,
        /// <summary>
        /// Multiply the result value by twice the "normal" laser amplitude.
        /// </summary>
        Biggest,
    }
    
    [System.Flags]
    public enum LaserIndex
    {
        Neither = 0x00,

        Left = 0x01,
        Right = 0x02,

        Both = Left | Right,
    }
}
