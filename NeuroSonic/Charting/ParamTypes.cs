using theori;
using theori.Charting;

namespace NeuroSonic.Charting
{
    public struct LaserParams
    {
        /// <summary>
        /// What function is applied to this laser's output.
        /// </summary>
        [TheoriProperty("function")]
        public LaserFunction Function;
        /// <summary>
        /// How to scale the laser's output.
        /// </summary>
        [TheoriProperty("scale")]
        public LaserScale Scale;
    }

    public struct SpinParams
    {
        /// <summary>
        /// The direction of this spin.
        /// </summary>
        public AngularDirection Direction;

        /// <summary>
        /// The duration of this spin.
        /// </summary>
        public time_t Duration;
    }

    public struct SwingParams
    {
        /// <summary>
        /// The direction of this swing.
        /// </summary>
        public AngularDirection Direction;

        /// <summary>
        /// The duration of this swing.
        /// </summary>
        public time_t Duration;

        /// <summary>
        /// The amplitude of the swing in degrees.
        /// </summary>
        public float Amplitude;
    }

    public struct WobbleParams
    {
        /// <summary>
        /// The direction of this wobble.
        /// </summary>
        public LinearDirection Direction;

        /// <summary>
        /// The duration of this wobble.
        /// </summary>
        public time_t Duration;

        /// <summary>
        /// The amplitude of the wobble in units equal to
        ///  half the width of the highway.
        /// </summary>
        public float Amplitude;

        /// <summary>
        /// The number of half-oscillations to complete.
        /// Due to the shape of a wobble curve, this frequency
        ///  is half that of its respective sine wave.
        /// </summary>
        public int Frequency;

        /// <summary>
        /// Specifies how the wobble decays over time.
        /// See the individual docs for each <see cref="OpenRM.Voltex.Decay"/> value separately.
        /// </summary>
        public Decay Decay;
    }
}
