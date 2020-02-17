using System;
using System.Numerics;

using theori;
using theori.Charting;

using NeuroSonic.Charting;

namespace NeuroSonic.GamePlay
{
    public delegate float FloatFunction(float input);

    public struct HighwayControlConfig
    {
        public static HighwayControlConfig CreateDefaultKsh168()
        {
            return new HighwayControlConfig()
            {
                LaserTiltUnitDegrees = 14,

                // smaller doesn't exist in KSH, but give it a default value anyway.
                LaserTiltSmallerMult = 0.5f,
                LaserTiltBiggerMult = 1.5f,
                LaserTiltBiggestMult = 2.0f,

                LaserTiltToBackgroundRotationMult = 0.45f,

                ZoomMinBound = -16.0f, // Not going to let it be unbound but will totally stop short
                ZoomMaxBound = 3.0f,
                ZoomFunction = (input) =>
                {
                    float result;
                    if (input > 3) result = 0.2f; // disallows super zooms
                    else if (input > 0) // 0 - 3
                        result = MathL.Square((input - 3) / 3.0f) * 0.8f + 0.2f;
                    else if (input > -3) // -3 - 0
                        result = -0.8f * input + 1;
                    else result = -input + 0.4f;
                    return result - 1;
                },

                PitchUnitDegrees = 15, // for reference, won't actually be used since PitchFunction is defined
                PitchFunction = (input) =>
                {
                    // TODO(local): It's pretty necessary to have the top zoom line up with
                    //  key points from KSM's view.
                    // The two I've found most important are -400 in KSM is flat with the camera zoom,
                    //  and +559 is flat with the screen like a 2D view.
                    // It MIGHT be worth also checking for the opposite of -400 for pointing straight at
                    //  the camera, and +1200 for being a 180 degree rotation as that won't be accurate
                    //  if this is implemented as I normally would.
                    // The biggest problem, though, is that, currently, figuring out what angles will
                    //  actually line up based on the view angle is going to be difficult and manual.
                    // The highway view will need to be more "configurable" as well, so that the viewing
                    //  angle the distance from the camera, etc. can all be pre-determined and also
                    //  imported and used for these calculations without giving access to the 
                    //  highway view to this part of the program.

                    const float KSM_FLAT_AWAY = -4.0f;
                    const float KSM_VERT = 5.59f;
                    const float KSM_FLAT_TOWARDS = 7.99f;
                    const float KSM_FLAT_TOWARDS_NEG = -24 + KSM_FLAT_TOWARDS;

                    const float NSC_FLAT_AWAY = -3.925f;
                    const float NSC_VERT = 4.79f;
                    const float NSC_FLAT_TOWARDS = 8.065f;
                    const float NSC_FLAT_TOWARDS_NEG = -24 + NSC_FLAT_TOWARDS;


                    float addAmt = 0;
                    while (input < -(24 + KSM_FLAT_TOWARDS))
                    {
                        input += 24;
                        addAmt--;
                    }
                    while (input > KSM_FLAT_TOWARDS)
                    {
                        input -= 24;
                        addAmt++;
                    }

                    float result;
                    if (input > 0 && input <= KSM_VERT)
                        result = (input / KSM_VERT) * NSC_VERT;
                    else if (input > KSM_VERT && input <= KSM_FLAT_TOWARDS)
                        result = NSC_VERT + (input - KSM_VERT) / (KSM_FLAT_TOWARDS - KSM_VERT) * (NSC_FLAT_TOWARDS - NSC_VERT);
                    else if (input <= 0 && input > KSM_FLAT_AWAY)
                        result = (input / KSM_FLAT_AWAY) * NSC_FLAT_AWAY;
                    else // <= KSM_FLAT_AWAY, > KSM_FLAT_TOWARDS_NEG
                        result = NSC_FLAT_AWAY + (input - KSM_FLAT_AWAY) / (KSM_FLAT_TOWARDS_NEG - KSM_FLAT_AWAY) * (NSC_FLAT_TOWARDS_NEG - NSC_FLAT_AWAY);

                    return result * 15 + addAmt * 360;
                    //return input * 15;
                },

                OffsetUnitWorld = 5.0f / (12 * 1.16f),
            };
        }

        /// <summary>
        /// How many degrees a laser at max output will tilt the highway
        ///  at normal tilt amount.
        /// </summary>
        public float LaserTiltUnitDegrees;

        public float LaserTiltSmallerMult;
        public float LaserTiltBiggerMult;
        public float LaserTiltBiggestMult;

        /// <summary>
        /// How much the laser tilt (NOT manual tilt control) affects the background rotation.
        /// In KSH this seems to be slightly less than 0.5 times.
        /// </summary>
        public float LaserTiltToBackgroundRotationMult;

        // TODO(local): It might be nice to support options for controlling the default
        //  orientation of the highway; either KSM or SDVX based (named something else, of course)
        //  or otherwise controlling, within reason, the default look before applying zooms etc.
        // If that becomes a feature, a limited range of things should likely be put
        //  here for giving to the highway view as well.
        // Things like default zoom amount (which would ofc change how hard zooming is applied,)
        //  or default viewing angle (so more of the highway is on the screen by default, similar to KSM)
        // This is PROBABLY not a great idea, but given that everything necessary will likely be
        //  in this configuration struct, it's not a TERRIBLE idea..?

        public float ZoomMinBound;
        public float ZoomMaxBound;

        // TODO(local): figure out how best to work with zooms!
        // My first thought is that this should return in the scale of
        //  the base distance from the camera, so 1 is twice as far
        //  from the camera as 0.
        // This is kinda nonsensical for zooming OUT if you think too hard
        //  about it, but zooming IN should really have a well defined lower-bound.
        // -1 puts the camera right on the crit line, which is bad, so anything
        //  > 0 is desired as an output; keeping the scale linear from there
        //  makes sense to me otherwise, so we'll try it.
        // Document it properly if that becomes the case.
        // (Note that this replaces any ZoomUnitDistance or ZoomAmount config parameters)
        public FloatFunction ZoomFunction;

        /// <summary>
        /// For every unit of input pitch, the unit of output pitch.
        /// For example, KSH 1.68 uses 2400 (24 units with the way we parse it)
        ///  as a full rotation, 360 degrees.
        /// That makes every output unit 15 degrees (360 / 24).
        /// Other input formats will need to set this to something else.
        /// </summary>
        public float PitchUnitDegrees;

        /// <summary>
        /// If provided, 
        /// </summary>
        public FloatFunction PitchFunction;

        /// <summary>
        /// For ever unit of offset, the world-space unit of translation.
        /// What I'd consider sensible is either 5/12 or 1/2; moving the origin to
        ///  align with the center of a laser lane or to the edge of the highway respectively.
        /// KSH uses 1.16 units = 5/12 world-space unit translation, for example, so
        ///  the configuration value here would be (5 / (12 * 1.16f)) output units.
        /// </summary>
        public float OffsetUnitWorld;
    }

    public sealed class HighwayControl
    {
        //private const float LASER_BASE_STRENGTH = 14;

        public static LaserParams DefaultLaserParams { get; } = new LaserParams()
        {
            Function = LaserFunction.Source,
        };

        class Timed<T>
            where T : struct
        {
            public readonly time_t StartTime;
            public readonly T Params;

            public Timed(time_t startTime, T p)
            {
                StartTime = startTime;
                Params = p;
            }
        }

        class CameraShake
        {
            public readonly time_t StartTime, Duration;
            public readonly Vector3 Strength;

            public CameraShake(time_t startTime, time_t duration, float sx, float sy, float sz)
                : this(startTime, duration, new Vector3(sx, sy, sz))
            {
            }

            public CameraShake(time_t startTime, time_t duration, Vector3 strength)
            {
                StartTime = startTime;
                Duration = duration;
                Strength = strength;
            }

            public Vector3 Sample(time_t pos)
            {
                float alpha = (float)(pos.Seconds / Duration.Seconds);
                return Strength * DampedSin(alpha, 1, 1, 0);
            }
        }

        #region Private Data
        
        private time_t m_position;
        private time_t m_measureDuration = 1;

        private LaserParams m_leftLaserParams = DefaultLaserParams;
        private LaserParams m_rightLaserParams = DefaultLaserParams;
        
        private float m_leftLaserInput, m_rightLaserInput;
        private float m_combinedLaserOutput, m_targetCombinedLaserOutput;
        
        private float m_zoom, m_pitch, m_offset, m_roll;
        private float m_effectRoll, m_critLineEffectRoll, m_effectOffset;

        private CameraShake? m_shake;

        private LinearDirection m_selectedLaser = LinearDirection.None;
        private LaserApplication m_laserApplication = LaserApplication.Additive;
        private Damping m_laserDamping = Damping.Slow;
        
        private Timed<SpinParams>? m_spin;
        private Timed<SwingParams>? m_swing;
        private Timed<WobbleParams>? m_wobble;

        #endregion

        public HighwayControlConfig Config { get; }

        #region Programmable Control Interface
        
        public time_t Position { set => m_position = value; }
        public time_t MeasureDuration { set => m_measureDuration = value; }

        public LaserParams LeftLaserParams  { set => m_leftLaserParams  = value; }
        public LaserParams RightLaserParams { set => m_rightLaserParams = value; }
        
        public float LeftLaserInput  { set { m_leftLaserInput = value; } }
        public float RightLaserInput { set { m_rightLaserInput = value; } }

        public float LaserRoll { get { return m_combinedLaserOutput; } }

        public float Zoom { set { m_zoom = value; } }
        public float Pitch { set { m_pitch = value; } }
        public float Offset { set { m_offset = value; } }
        public float Roll { get => m_roll; set => m_roll = value; }

        public float EffectOffset { get { return m_effectOffset; } }
        public float EffectRoll { get { return m_effectRoll; } }
        public float CritLineEffectRoll { get { return m_critLineEffectRoll; } }

        public LaserApplication LaserApplication { set => m_laserApplication = value; }
        public Damping LaserDamping { set => m_laserDamping = value; }

        public float SpinTimer => m_spin == null ? 0 : (m_spin.Params.Direction == AngularDirection.Clockwise ? -1 : 1) * (float)((m_position - m_spin.StartTime) / m_spin.Params.Duration);
        public float SwingTimer => m_swing == null ? 0 : (m_swing.Params.Direction == AngularDirection.Clockwise ? -1 : 1) * (float)((m_position - m_swing.StartTime) / m_swing.Params.Duration);

        public void ShakeCamera(float dir)
        {
            var s = new Vector3(0.05f, 0.02f, 0) * dir;
            m_shake = new CameraShake(m_position, 0.1, s);
        }

        /// <summary>
        /// Applies a full spin (360 rotation with recovery animation)
        ///  to this highway using the given associated parameters.
        /// </summary>
        public void ApplySpin(SpinParams p, time_t? time = null)
        {
            m_spin = new Timed<SpinParams>(time ?? m_position, p);
        }

        /// <summary>
        /// Applies a back-and-forth swing to this highway
        ///  using the given associated parameters.
        /// </summary>
        public void ApplySwing(SwingParams p, time_t? time = null)
        {
            m_swing = new Timed<SwingParams>(time ?? m_position, p);
        }
        
        /// <summary>
        /// Applies a horizontal "wobble" to this highway
        ///  using the given associated parameters.
        /// </summary>
        public void ApplyWobble(WobbleParams p, time_t? time = null)
        {
            m_wobble = new Timed<WobbleParams>(time ?? m_position, p);
        }

        #endregion

        public HighwayControl(HighwayControlConfig config)
        {
            Config = config;
        }

        public void ApplyToView(HighwayView view)
        {
            view.TargetLaserRoll = m_combinedLaserOutput;
            view.TargetZoom = Config.ZoomFunction(MathL.Clamp(m_zoom, Config.ZoomMinBound, Config.ZoomMaxBound));
            if (Config.PitchFunction != null)
                view.TargetPitch = Config.PitchFunction(m_pitch);
            else view.TargetPitch = m_pitch * Config.PitchUnitDegrees;
            view.TargetOffset = (m_offset * 5) / (12 * 1.16f);
            view.TargetEffectOffset = m_effectOffset;
            view.TargetBaseRoll = m_roll;
            view.TargetEffectRoll = m_effectRoll;
            if (m_shake != null)
                view.CameraOffset = m_shake.Sample(m_position - m_shake.StartTime);
            else view.CameraOffset = Vector3.Zero;
        }

        private static float DampedSin(float t, float amplitude, float frequency, float decayTo)
        {
            //float decay = MathL.Lerp(1, decayTo, 1 - (1 - t) * (1 - t));
            float decay = MathL.Lerp(1, decayTo, t);
            return amplitude * decay * MathL.Sin(frequency * 2 * t * MathL.Pi);
        }

        public void Update(float delta)
        {
            if (MathL.Abs(m_combinedLaserOutput) < 0.001f) m_combinedLaserOutput = 0;

            if (m_shake != null && m_shake.StartTime + m_shake.Duration < m_position)
                m_shake = null;

            float leftLaser = -ProcessLaserInput(ref m_leftLaserInput, m_leftLaserParams);
            float rightLaser = ProcessLaserInput(ref m_rightLaserInput, m_rightLaserParams);
            
            var appFlag = m_laserApplication & LaserApplication.FlagMask;
            var appValue = m_laserApplication & LaserApplication.ApplicationMask;

            float laserOutput = 0;

            switch (appValue)
            {
                case LaserApplication.Zero: m_selectedLaser = LinearDirection.None; break;
                case LaserApplication.Additive: laserOutput = leftLaser + rightLaser; m_selectedLaser = LinearDirection.None; break;

                case LaserApplication.Left: laserOutput = leftLaser; m_selectedLaser = LinearDirection.Left; break;
                case LaserApplication.Right: laserOutput = rightLaser; m_selectedLaser = LinearDirection.Right; break;

                case LaserApplication.Initial:
                {
                    if (m_selectedLaser == LinearDirection.None)
                    {
                        if (leftLaser == 0)
                        {
                            if (rightLaser != 0)
                                m_selectedLaser = LinearDirection.Right;
                        }
                        else if (rightLaser == 0)
                            m_selectedLaser = LinearDirection.Left;
                    }
                    
                    // apply the selected laser, if one has been selected
                    if (m_selectedLaser == LinearDirection.Left)
                        laserOutput = leftLaser;
                    else if (m_selectedLaser == LinearDirection.Right)
                        laserOutput = rightLaser;
                } break;
            }

            switch (appFlag)
            {
                case LaserApplication.KeepMax:
                {
                    if (m_targetCombinedLaserOutput < 0)
                        laserOutput = MathL.Min(laserOutput, m_targetCombinedLaserOutput);
                    else if (m_targetCombinedLaserOutput > 0)
                        laserOutput = MathL.Max(laserOutput, m_targetCombinedLaserOutput);
                } break;

                case LaserApplication.KeepMin:
                {
                    if (m_targetCombinedLaserOutput < 0)
                        laserOutput = MathL.Max(laserOutput, m_targetCombinedLaserOutput);
                    else if (m_targetCombinedLaserOutput > 0)
                        laserOutput = MathL.Min(laserOutput, m_targetCombinedLaserOutput);
                } break;
            }

            const float SPEED_FAST = 70, ACCEL_FAST = 30;
            const float SPEED_SLOW = 40, ACCEL_SLOW = 15;

            m_targetCombinedLaserOutput = laserOutput;
            if (m_targetCombinedLaserOutput == 0)
            {
                LerpTo(ref m_combinedLaserOutput, m_targetCombinedLaserOutput, 0, (float)m_measureDuration.Seconds * 4);
            }
            else
            {
                switch (m_laserDamping)
                {
                    case Damping.Fast: LerpTo(ref m_combinedLaserOutput, m_targetCombinedLaserOutput, SPEED_FAST, ACCEL_FAST); break;
                    case Damping.Slow: LerpTo(ref m_combinedLaserOutput, m_targetCombinedLaserOutput, SPEED_SLOW, ACCEL_SLOW); break;
                    case Damping.Off:
                    {
                        const int SPEED = 60;
                        if (m_targetCombinedLaserOutput < m_combinedLaserOutput)
                            m_combinedLaserOutput = Math.Max(m_targetCombinedLaserOutput, m_combinedLaserOutput - delta * SPEED);
                        else m_combinedLaserOutput = Math.Min(m_targetCombinedLaserOutput, m_combinedLaserOutput + delta * SPEED);
                    } break;
                }
            }
            
            float spinRoll = 0;
            float swingRoll = 0;
            float wobbleOffset = 0;
            float critLineRotation = 0;

            if (m_spin != null)
            {
                if (m_spin.StartTime > m_position || m_spin.StartTime + m_spin.Params.Duration < m_position)
                    m_spin = null;
                else
                {

                    float time = (float)((m_position - m_spin.StartTime) / m_spin.Params.Duration);
                    //Trace.WriteLine($"SPIN CONTROL: from { m_spin.StartTime } for { m_spin.Params.Duration }, { time }");
                    float dir = (int)m_spin.Params.Direction;

	                const float TSPIN = 0.75f / 2.0f;
	                const float TRECOV = 0.75f / 2.0f;

                    if (time < TSPIN + TRECOV)
                        critLineRotation += -DampedSin(time / (TSPIN + TRECOV), 1, 1, 0) * dir;

	                //float bgAngle = MathL.Clamp(time * 4.0f, 0.0f, 2.0f) * dir;
	                if (time <= TSPIN)
                        spinRoll = -dir * (TSPIN - time) / TSPIN;
	                else
	                {
		                if (time < TSPIN + TRECOV)
                            spinRoll = DampedSin((time - TSPIN) / TRECOV, 30f / 360, 0.5f, 0) * dir;
                        else spinRoll = 0.0f;
                    }
                }
            }

            if (m_swing != null)
            {
                if (m_swing.StartTime > m_position || m_swing.StartTime + m_swing.Params.Duration < m_position)
                    m_swing = null;
                else
                {
                    float time = (float)((m_position - m_swing.StartTime) / m_swing.Params.Duration);
                    float dir = (int)m_swing.Params.Direction;

                    critLineRotation += -DampedSin(time, 1, 1, 0) * dir;

                    #if false
                    // dividing the amplitude by 0.5625 makes the first crest of the sin
                    //  wave reach exactly that amplitude, as its damped quadradically.
                    // A frequency of 1 leaves 1 crest and 1 trough of the wave,
                    //  so at time 0.25 the first crest is reached.
                    // The damping equation is applied quadratically in terms of
                    //  1 - time, which is 0.75.
                    // 0.75 ^ 2 = 0.5625.
                    // At time 0.25 the amplitude of the wave is exactly what
                    //  the setting wants it to be.
			        swingRoll = DampedSin(time, (m_swing.Params.Amplitude / 0.5625f) / 360, 1, 0) * dir;
                    #else
			        swingRoll = DampedSin(time, (m_swing.Params.Amplitude / 0.75f) / 360, 1, 0) * dir;
                    #endif
                }
            }

            if (m_wobble != null)
            {
                if (m_wobble.StartTime > m_position || m_wobble.StartTime + m_wobble.Params.Duration < m_position)
                    m_wobble = null;
                else
                {
                    float time = (float)((m_position - m_wobble.StartTime) / m_wobble.Params.Duration);
                    float dir = (int)m_wobble.Params.Direction;

                    float decay = 0;
                    switch (m_wobble.Params.Decay)
                    {
                        case Decay.Off: decay = 1; break;
                        case Decay.OnSlow: decay = 0.5f; break;
                        case Decay.On: decay = 0; break;
                    }

			        wobbleOffset = DampedSin(time, m_wobble.Params.Amplitude * 0.5f,
				                   m_wobble.Params.Frequency / 2.0f, decay) * dir;
                }
            }

            m_effectRoll = spinRoll + swingRoll;
            m_critLineEffectRoll = critLineRotation;
            m_effectOffset = wobbleOffset;

            float ProcessLaserInput(ref float value, LaserParams p)
            {
                float output = value;

                switch (p.Function)
                {
                    case LaserFunction.Zero: return 0;
                    case LaserFunction.Source: break;
                    case LaserFunction.NegativeSource: output = -output; break;
                    case LaserFunction.OneMinusSource: output = 1 - output; break;
                }
            
                switch (p.Scale)
                {
                    case LaserScale.Normal: break;
                    case LaserScale.Smaller: output *= Config.LaserTiltSmallerMult; break;
                    case LaserScale.Bigger: output *= Config.LaserTiltBiggerMult; break;
                    case LaserScale.Biggest: output *= Config.LaserTiltBiggestMult; break;
                }

                return output * Config.LaserTiltUnitDegrees;
            }

            void LerpTo(ref float value, float target, float max, float speed)
            {
                float diff = MathL.Abs(target - value);
                float change = diff * delta * speed;

                if (max != 0) change = MathL.Min(max * delta, change);

                if (target < value)
                    value = MathL.Max(value - change, target);
                else value = MathL.Min(value + change, target);
            }
        }
    }
}
