using theori;
using theori.Charting;
using theori.Charting.Effects;

namespace NeuroSonic.Charting
{
    public abstract class HighwayTypedEvent : EventEntity { }
    public abstract class ButtonTypedEvent : EventEntity { }
    public abstract class LaserTypedEvent : EventEntity { }

    [EntityType("LaserApplication")]
    public class LaserApplicationEvent : LaserTypedEvent
    {
        [TheoriProperty("mode")]
        public LaserApplication Application = LaserApplication.Additive;
    }

    [EntityType("LaserParams")]
    public class LaserParamsEvent : LaserTypedEvent
    {
        [TheoriProperty("index")]
        public LaserIndex LaserIndex;
        [TheoriProperty("params")]
        public LaserParams Params;
    }

    [EntityType("GraphPoint")]
    public class GraphPointEvent : EventEntity
    {
        [TheoriProperty("value")]
        public float Value;

        [TheoriProperty("a")]
        [TheoriIgnoreDefault]
        public float ParamA;

        [TheoriProperty("b")]
        [TheoriIgnoreDefault]
        public float ParamB;
    }

    [EntityType("EffectKind")]
    public class EffectKindEvent : ButtonTypedEvent, IHasEffectDef
    {
        [TheoriProperty("index")]
        public int EffectIndex;
        [TheoriProperty("effect")]
        [TheoriIgnoreDefault]
        public EffectDef Effect { get; set; }
    }

    [EntityType("LaserFilterKind")]
    public class LaserFilterKindEvent : LaserTypedEvent, IHasEffectDef
    {
        [TheoriProperty("index")]
        public LaserIndex LaserIndex;
        [TheoriProperty("effect")]
        [TheoriIgnoreDefault]
        public EffectDef Effect { get; set; }
    }

    [EntityType("LaserFilterGain")]
    public class LaserFilterGainEvent : LaserTypedEvent
    {
        [TheoriProperty("index")]
        public LaserIndex LaserIndex;
        [TheoriProperty("gain")]
        public float Gain;
    }

    [EntityType("SlamVolume")]
    public class SlamVolumeEvent : LaserTypedEvent
    {
        [TheoriProperty("volume")]
        public float Volume;
    }

    [EntityType("SpinImpulse")]
    public class SpinImpulseEvent : HighwayTypedEvent
    {
        public SpinParams Params => new SpinParams()
        {
            Direction = Direction,
            Duration = AbsoluteDuration,
        };

        [TheoriProperty("direction")]
        public AngularDirection Direction;
    }

    [EntityType("SwingImpulse")]
    public class SwingImpulseEvent : HighwayTypedEvent
    {
        public SwingParams Params => new SwingParams()
        {
            Direction = Direction,
            Duration = AbsoluteDuration,
            Amplitude = Amplitude,
        };

        [TheoriProperty("direction")]
        public AngularDirection Direction;
        [TheoriProperty("amplitude")]
        public float Amplitude;
    }

    [EntityType("WobbleImpulse")]
    public class WobbleImpulseEvent : HighwayTypedEvent
    {
        public WobbleParams Params => new WobbleParams()
        {
            Direction = Direction,
            Duration = AbsoluteDuration,
            Amplitude = Amplitude,
            Frequency = Frequency,
            Decay = Decay,
        };

        [TheoriProperty("direction")]
        public LinearDirection Direction;
        [TheoriProperty("amplitude")]
        public float Amplitude;
        [TheoriProperty("frequency")]
        public int Frequency;
        [TheoriProperty("decay")]
        public Decay Decay;
    }
}
