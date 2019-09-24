using System;
using System.Collections.Generic;
using System.Diagnostics;

using theori;
using theori.Charting;

using NeuroSonic.Charting;

namespace NeuroSonic.GamePlay.Scoring
{
    public class LaserJudge : StreamJudge
    {
        enum JudgeState
        {
            Idle,

            ActiveOn,
            ActiveOff,

            CursorReset,

            LaserBegin,
            LaserEnd,

            SwitchDirection,
            SameDirectionSlam,
        }

        enum TickKind
        {
            Slam,
            Segment
        }

        class StateTick
        {
            public readonly AnalogEntity RootEntity;
            public readonly AnalogEntity SegmentEntity;

            public readonly time_t Position;
            public readonly JudgeState State;

            public bool IsSlam => SegmentEntity.IsInstant;

            public StateTick(AnalogEntity root, AnalogEntity segment, time_t pos, JudgeState state)
            {
                RootEntity = root;
                SegmentEntity = segment;
                Position = pos;
                State = state;
            }
        }

        class ScoreTick
        {
            public readonly AnalogEntity Entity;

            public readonly time_t Position;
            public readonly TickKind Kind;

            public ScoreTick(AnalogEntity entity, time_t pos, TickKind kind)
            {
                Entity = entity;
                Position = pos;
                Kind = kind;
            }
        }

        private readonly time_t m_slamActivateRadius = 150 / 1000.0;
        private readonly time_t m_directionChangeRadius = 150 / 1000.0;
        private readonly time_t m_cursorResetDistance = 1000 / 1000.0;
        private readonly time_t m_lockDuration = 150 / 1000.0;
        /// <summary>
        /// The amount of distance the cursor can be from the laser to still be considered active.
        /// </summary>
        private readonly float m_cursorActiveRange = 0.1f;

        private time_t m_lastUpdatePosition = double.MinValue;

        private readonly List<StateTick> m_stateTicks = new List<StateTick>();
        private readonly List<ScoreTick> m_scoreTicks = new List<ScoreTick>();

        private JudgeState m_state = JudgeState.Idle;
        private StateTick m_currentStateTick;

        private int m_stateIndex = 0, m_scoreIndex = 0;

        private float m_desiredCursorPosition = 0;
        private int m_direction = 0;

        private time_t m_lockTimer = 0.0;
        private double m_lockTimerSpeed = 1.0;
        private bool IsLocked => m_lockTimer > 0;

        private bool m_canControlCursorMovement = false;

        private bool HasStateTicks => m_stateIndex < m_stateTicks.Count;
        private StateTick NextStateTick => m_stateTicks[m_stateIndex];

        private bool HasScoreTicks => m_scoreIndex < m_scoreTicks.Count;
        private ScoreTick NextScoreTick => m_scoreTicks[m_scoreIndex];

        public float CursorPosition { get; private set; } = 0;
        public float LaserRange { get; private set; } = 1;

        public event Action<time_t, Entity> OnSlamHit;

        public event Action<time_t, Entity> OnLaserActivated;
        public event Action<time_t, Entity> OnLaserDeactivated;

        public event Action<Entity, time_t, JudgeResult> OnTickProcessed;

        public event Action OnShowCursor;
        public event Action OnHideCursor;

        public LaserJudge(Chart chart, LaneLabel label)
            : base(chart, label)
        {
            tick_t tickStep = (Chart.MaxBpm >= 255 ? 2.0 : 1.0) / (4 * 4);

            // score ticks first
            foreach (var entity in chart[label])
            {
                var root = (AnalogEntity)entity;
                if (root.PreviousConnected != null) continue;

                // now we're working with the root
                var start = root;
                while (start != null)
                {
                    var next = start;
                    while (next.NextConnected is AnalogEntity a && !a.IsInstant)
                        next = a;

                    Debug.Assert(next.Position >= start.Position);
                    if (next.Position > start.Position)
                        Debug.Assert(!next.IsInstant);

                    tick_t startPos = start.Position;
                    bool endsWithSlam = next.NextConnected is AnalogEntity && next.IsInstant;

                    int numTicks = MathL.Max(1, MathL.FloorToInt((double)(next.EndPosition - startPos) / (double)tickStep));
                    if (endsWithSlam && next.EndPosition - (startPos + tickStep * numTicks) < tickStep)
                        numTicks--;

                    for (int i = 0; i < numTicks; i++)
                    {
                        tick_t pos = startPos + i * tickStep;

                        var kind = (i == 0 && start.IsInstant) ? TickKind.Slam : TickKind.Segment;
                        m_scoreTicks.Add(new ScoreTick(root, chart.CalcTimeFromTick(pos), kind));
                    }

                    start = next.NextConnected as AnalogEntity;
                }
            }

            // state ticks seconds
            foreach (var entity in chart[label])
            {
                var root = (AnalogEntity)entity;
                if (root.PreviousConnected != null) continue;

                time_t cursorResetTime = root.AbsolutePosition - m_cursorResetDistance;
                time_t laserBeginTime = root.AbsolutePosition - m_directionChangeRadius;

                if (root.Previous is AnalogEntity p)
                {
                    cursorResetTime = MathL.Max((double)p.AbsoluteEndPosition, (double)cursorResetTime);
                    laserBeginTime = MathL.Max((double)p.AbsoluteEndPosition, (double)laserBeginTime);
                }

                m_stateTicks.Add(new StateTick(root, root, cursorResetTime, JudgeState.CursorReset));
                m_stateTicks.Add(new StateTick(root, root, laserBeginTime, JudgeState.LaserBegin));

                if (root.DirectionSign != 0)
                    m_stateTicks.Add(new StateTick(root, root, root.AbsolutePosition, JudgeState.SwitchDirection));

                if (root.NextConnected is AnalogEntity segment)
                {
                    while (segment != null)
                    {
                        if (segment.DirectionSign != ((AnalogEntity)segment.Previous).DirectionSign)
                            m_stateTicks.Add(new StateTick(root, segment, segment.AbsolutePosition, JudgeState.SwitchDirection));
                        else if (segment.IsInstant)
                            m_stateTicks.Add(new StateTick(root, segment, segment.AbsolutePosition, JudgeState.SameDirectionSlam));

                        if (segment.NextConnected == null)
                            m_stateTicks.Add(new StateTick(root, segment, segment.AbsoluteEndPosition, JudgeState.LaserEnd));
                        segment = segment.NextConnected as AnalogEntity;
                    }
                }
                else m_stateTicks.Add(new StateTick(root, root, root.AbsoluteEndPosition, JudgeState.LaserEnd));
            }
        }

        public override int CalculateNumScorableTicks() => m_scoreTicks.Count;
        public override int[] GetCategorizedTicks() => new int[] { CalculateNumScorableTicks() };

        protected override void AdvancePosition(time_t position)
        {
            time_t timeDelta = position - m_lastUpdatePosition;
            m_lastUpdatePosition = position;

            if (!HasStateTicks && !HasScoreTicks) return;

            switch (m_state)
            {
                case JudgeState.Idle:
                {
                    var nextStateTick = NextStateTick;
                    while (position - (nextStateTick.Position + JudgementOffset) >= 0)
                    {
                        if (nextStateTick.State == JudgeState.CursorReset)
                        {
                            AdvanceStateTick();

                            OnShowCursor?.Invoke();

                            CursorPosition = m_desiredCursorPosition = nextStateTick.RootEntity.InitialValue;
                            LaserRange = nextStateTick.RootEntity.RangeExtended ? 2 : 1;

                            m_direction = 0;
                        }
                        else if (nextStateTick.State == JudgeState.LaserBegin)
                        {
                            AdvanceStateTick();

                            //LastLockTime = position;
                            m_direction = 0;
                            m_state = nextStateTick.RootEntity.IsInstant ?
                                (IsBeingPlayed ? JudgeState.ActiveOn : JudgeState.ActiveOff) :
                                JudgeState.ActiveOn;
                            m_currentStateTick = nextStateTick;

                            // The first score tick happens at the same time as the laser start event,
                            //  hop straight over to the other case explicitly and let it process the score tick.
                            goto case JudgeState.ActiveOn;
                        }
                        else break;

                        if (HasStateTicks)
                            nextStateTick = NextStateTick;
                        else break;
                    }
                }
                break;

                case JudgeState.ActiveOn:
                case JudgeState.ActiveOff:
                {
                    var segmentCheck = m_currentStateTick.SegmentEntity;
                    while (segmentCheck != null && segmentCheck.AbsoluteEndPosition < position && segmentCheck.NextConnected is AnalogEntity next)
                        segmentCheck = next;

                    m_desiredCursorPosition = segmentCheck.SampleValue(position);

                    if (m_direction != 0)
                        m_lockTimer -= timeDelta * m_lockTimerSpeed;
                    if (m_lockTimer < 0) m_lockTimer = 0;

                    if (AutoPlay)
                        CursorPosition = m_desiredCursorPosition;

                    var nextStateTick = NextStateTick;
                    while (position - (nextStateTick.Position + JudgementOffset) >= 0)
                    {
                        if (nextStateTick.State == JudgeState.LaserEnd)
                        {
                            AdvanceStateTick();

                            OnHideCursor?.Invoke();

                            if (AutoPlay)
                                CursorPosition = m_desiredCursorPosition = nextStateTick.SegmentEntity.FinalValue;

                            m_state = JudgeState.Idle;
                            m_currentStateTick = null;
                        }
                        else if (nextStateTick.State == JudgeState.SwitchDirection)
                        {
                            if (position > (nextStateTick.Position + JudgementOffset))
                            {
                                if (position - (nextStateTick.Position + JudgementOffset) >= m_directionChangeRadius)
                                {
                                    AdvanceStateTick();

                                    m_direction = nextStateTick.SegmentEntity.DirectionSign;
                                    m_currentStateTick = nextStateTick;

                                    //Logger.Log($"Direction Switch ({ (m_direction == 1 ? "->" : (m_direction == -1 ? "<-" : "|")) }) Missed (by { position - (nextStateTick.Position + JudgementOffset) }): { nextStateTick.SegmentEntity.Position } ({ nextStateTick.SegmentEntity.AbsolutePosition })");

                                    m_lockTimer = 0.0;
                                }
                                else
                                {
                                    m_canControlCursorMovement = false;
                                    //Logger.Log($"Prevented automatic cursor moving while locked: direction switch origin passed without being played ({ nextStateTick.SegmentEntity.Position })");

                                    break;
                                }
                            }
                        }
                        else if (nextStateTick.State == JudgeState.SameDirectionSlam && position - (nextStateTick.Position + JudgementOffset) >= m_directionChangeRadius)
                        {
                            AdvanceStateTick();
                        }
                        else break;

                        if (HasStateTicks)
                            nextStateTick = NextStateTick;
                        else break;
                    }

                    if (IsLocked && m_canControlCursorMovement)
                        CursorPosition = m_desiredCursorPosition;

                    IsBeingPlayed = IsLocked || MathL.Abs(m_desiredCursorPosition - CursorPosition) <= m_cursorActiveRange;
                    if (IsBeingPlayed && m_direction == 0)
                    {
                        m_lockTimer = m_lockDuration; // NOTE(local): don't SetLocked, keep the timer decay speed
                        if (m_canControlCursorMovement)
                            CursorPosition = m_desiredCursorPosition;
                    }

                    if (HasScoreTicks)
                    {
                        var nextScoreTick = NextScoreTick;
                        if (position - (nextScoreTick.Position + JudgementOffset) >= 0)
                        {
                            var resultKind = IsBeingPlayed ? JudgeKind.Passive : JudgeKind.Miss;
                            OnTickProcessed?.Invoke(nextScoreTick.Entity, nextScoreTick.Position, new JudgeResult(0, resultKind));

                            AdvanceScoreTick();
                        }
                    }
                }
                break;
            }

            if (HasStateTicks && position - (NextStateTick.Position + JudgementOffset) >= 0)
            {
                //Logger.Log($"{ NextStateTick.State } :: { NextStateTick.SegmentEntity.Position } or { NextStateTick.Position }");
            }
        }

        private void AdvanceStateTick() => m_stateIndex++;
        private void AdvanceScoreTick() => m_scoreIndex++;

        private void SetLocked()
        {
            m_canControlCursorMovement = true;

            m_lockTimer = m_lockDuration;
            m_lockTimerSpeed = 1.0;
        }

        public void UserInput(float amount, time_t position)
        {
            if (!HasStateTicks && !HasScoreTicks) return;

            if (m_state == JudgeState.Idle) return;
            int inputDir = MathL.Sign(amount);

            if (HasStateTicks)
            {
                var nextStateTick = NextStateTick;
                if (nextStateTick.State == JudgeState.SwitchDirection)
                {
                    time_t radius = MathL.Abs((double)(position - (NextStateTick.Position + JudgementOffset)));
                    if (radius < m_directionChangeRadius && (inputDir == nextStateTick.SegmentEntity.DirectionSign || nextStateTick.SegmentEntity.DirectionSign == 0))
                    {
                        if (m_lockTimer > 0.0 || IsBeingPlayed)
                        {
                            AdvanceStateTick();

                            m_direction = nextStateTick.SegmentEntity.DirectionSign;
                            m_currentStateTick = nextStateTick;

                            //Logger.Log($"Direction Switch ({ (m_direction == 1 ? "->" : (m_direction == -1 ? "<-" : "|")) }) Hit: { nextStateTick.SegmentEntity.Position } ({ nextStateTick.SegmentEntity.AbsolutePosition })");

                            if (nextStateTick.IsSlam)
                            {
                                OnSlamHit?.Invoke(position, nextStateTick.SegmentEntity);
                                //Logger.Log($"  Direction Switch on Slam: { CursorPosition }, { nextStateTick.SegmentEntity.InitialValue } ({ MathL.Abs(CursorPosition - nextStateTick.SegmentEntity.InitialValue) } <? { m_cursorActiveRange })");

                                // If the cursor was near the head of the laser, we lock it regardless of previous locked status.
                                if (MathL.Abs(CursorPosition - nextStateTick.SegmentEntity.InitialValue) < m_cursorActiveRange)
                                {
                                    //Logger.Log($"  Direction Switch on Slam triggered Lock");
                                    SetLocked();
                                }
                            }
                            else if (IsLocked)
                                SetLocked();
                        }
                    }
                }
                else if (nextStateTick.State == JudgeState.SameDirectionSlam)
                {
                    time_t radius = MathL.Abs((double)(position - (NextStateTick.Position + JudgementOffset)));
                    if (radius < m_directionChangeRadius && inputDir == m_direction)
                    {
                        Debug.Assert(nextStateTick.SegmentEntity.IsInstant);

                        AdvanceStateTick();
                        OnSlamHit?.Invoke(position, nextStateTick.SegmentEntity);
                    }
                }
            }

            if (IsLocked)
            {
                if (inputDir == m_direction)
                    SetLocked();
                else m_lockTimerSpeed = 2.5;
            }
            else
            {
                if (m_direction == 0)
                {
                    // If we input at all, that's an active role by the user.
                    // If the user inputs and the cursor happens to be really close, just consider it good!
                    // They shouldn't need to pay so close attention to the cursor, this value can be tweaked
                    //  so that a wider radius is accepted.
                    if (MathL.Abs(m_desiredCursorPosition - CursorPosition) < m_cursorActiveRange * 0.1f)
                        SetLocked();
                    else if (CursorPosition < m_desiredCursorPosition)
                    {
                        if (inputDir == 1)
                        {
                            CursorPosition = MathL.Min(CursorPosition + amount, m_desiredCursorPosition);
                            if (CursorPosition == m_desiredCursorPosition)
                                SetLocked();
                        }
                        else CursorPosition = MathL.Max(0, CursorPosition + amount);
                    }
                    else if (CursorPosition > m_desiredCursorPosition)
                    {
                        if (inputDir == -1)
                        {
                            CursorPosition = MathL.Max(CursorPosition + amount, m_desiredCursorPosition);
                            if (CursorPosition == m_desiredCursorPosition)
                                SetLocked();
                        }
                        else CursorPosition = MathL.Min(1, CursorPosition + amount);
                    }
                }
                else
                {
                    // Same as above for non-directional lasers, if the player is actively
                    //  trying to play in the correct direction and the cursor happens to be in about
                    //  the right location we let them lock and keep going.
                    if (inputDir == m_direction &&
                        MathL.Abs(m_desiredCursorPosition - CursorPosition) < m_cursorActiveRange * 0.1f)
                    {
                        SetLocked();
                    }
                    else if (m_direction == 1)
                    {
                        if (inputDir == 1)
                        {
                            if (CursorPosition < m_desiredCursorPosition)
                                CursorPosition = MathL.Min(CursorPosition + amount, m_desiredCursorPosition);
                            else if (CursorPosition > m_desiredCursorPosition)
                                CursorPosition = MathL.Min(1, CursorPosition + amount);

                            if (CursorPosition == m_desiredCursorPosition)
                                SetLocked();
                        }
                        else CursorPosition = MathL.Max(0, CursorPosition + amount);
                    }
                    else if (m_direction == -1)
                    {
                        if (inputDir == -1)
                        {
                            if (CursorPosition > m_desiredCursorPosition)
                                CursorPosition = MathL.Max(CursorPosition + amount, m_desiredCursorPosition);
                            else if (CursorPosition < m_desiredCursorPosition)
                                CursorPosition = MathL.Max(0, CursorPosition + amount);

                            if (CursorPosition == m_desiredCursorPosition)
                                SetLocked();
                        }
                        else CursorPosition = MathL.Min(1, CursorPosition + amount);
                    }
                }
            }
        }
    }
}
