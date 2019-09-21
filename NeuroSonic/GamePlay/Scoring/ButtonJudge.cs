using System;
using System.Collections.Generic;
using System.Diagnostics;

using theori;
using theori.Charting;

using NeuroSonic.Charting;

namespace NeuroSonic.GamePlay.Scoring
{
    public delegate void SpawnKeyBeam(LaneLabel label, JudgeKind kind, bool isEarly);
    public delegate void DespawnKeyBeam(LaneLabel label);

    public sealed class ButtonJudge : StreamJudge
    {
        enum JudgeState
        {
            Idle,

            ChipAwaitPress,
            HoldAwaitPress,
            HoldAwaitRelease,

            HoldOff,
            HoldOn,
        }

        enum TickKind
        {
            Chip,
            Hold
        }

        class StateTick
        {
            public readonly ButtonEntity Entity;

            public readonly time_t Position;
            public readonly JudgeState State;

            public StateTick(ButtonEntity entity, time_t pos, JudgeState state)
            {
                Entity = entity;
                Position = pos;
                State = state;
            }
        }

        class ScoreTick
        {
            public readonly ButtonEntity Entity;

            public readonly time_t Position;
            public readonly TickKind Kind;

            public ScoreTick(ButtonEntity entity, time_t pos, TickKind kind)
            {
                Entity = entity;
                Position = pos;
                Kind = kind;
            }
        }

        private readonly time_t m_chipPerfectRadius = 25 / 1000.0;
        private readonly time_t m_chipCriticalRadius = 50 / 1000.0;
        private readonly time_t m_chipNearRadius = 100 / 1000.0;
        private readonly time_t m_chipMissRadius = 150 / 1000.0;

        private readonly time_t m_holdActivateRadius = 100 / 1000.0;

        private readonly List<StateTick> m_stateTicks = new List<StateTick>();
        private readonly List<ScoreTick> m_scoreTicks = new List<ScoreTick>();

        private JudgeState m_state = JudgeState.Idle;
        private StateTick? m_currentStateTick;

        private int m_stateIndex = 0, m_scoreIndex = 0;

        private bool HasStateTicks => m_stateIndex < m_stateTicks.Count;
        private StateTick NextStateTick => m_stateTicks[m_stateIndex];

        private bool HasScoreTicks => m_scoreIndex < m_scoreTicks.Count;
        private ScoreTick NextScoreTick => m_scoreTicks[m_scoreIndex];

        public event Action<time_t, Entity>? OnChipPressed;

        public event Action<time_t, Entity>? OnHoldPressed;
        public event Action<time_t, Entity>? OnHoldReleased;

        public event Action<Entity, time_t, JudgeResult>? OnTickProcessed;

        public SpawnKeyBeam? SpawnKeyBeam;
        public DespawnKeyBeam? DespawnKeyBeam;

        public ButtonJudge(Chart chart, LaneLabel label)
            : base(chart, label)
        {
            tick_t tickStep = (Chart.MaxBpm >= 255 ? 2.0 : 1.0) / (4 * 4);
            tick_t tickMargin = 2 * tickStep;

            foreach (var entity in chart[label])
            {
                var button = (ButtonEntity)entity;

                if (button.IsInstant)
                {
                    m_stateTicks.Add(new StateTick(button, button.AbsolutePosition, JudgeState.ChipAwaitPress));
                    m_scoreTicks.Add(new ScoreTick(button, button.AbsolutePosition, TickKind.Chip));
                }
                else
                {
                    m_stateTicks.Add(new StateTick(button, button.AbsolutePosition, JudgeState.HoldAwaitPress));
                    // end state is placed at the last score tick

                    int numTicks = MathL.FloorToInt((double)(button.Duration - tickMargin) / (double)tickStep);
                    if (numTicks <= 0)
                    {
                        m_scoreTicks.Add(new ScoreTick(button, button.AbsolutePosition + button.AbsoluteDuration / 2, TickKind.Hold));
                        m_stateTicks.Add(new StateTick(button, button.AbsolutePosition + button.AbsoluteDuration / 2, JudgeState.HoldAwaitRelease));
                    }
                    else for (int i = 0; i < numTicks; i++)
                    {
                        tick_t pos = button.Position + tickMargin + tickStep * i;
                        m_scoreTicks.Add(new ScoreTick(button, chart.CalcTimeFromTick(pos), TickKind.Hold));

                        if (i == numTicks - 1)
                            m_stateTicks.Add(new StateTick(button, chart.CalcTimeFromTick(pos), JudgeState.HoldAwaitRelease));
                    }
                }
            }
        }

        public override int CalculateNumScorableTicks() => m_scoreTicks.Count;

        public override int[] GetCategorizedTicks()
        {
            int[] result = new int[2];
            foreach (var tick in m_scoreTicks)
            {
                if (tick.Entity.IsInstant)
                    result[0]++;
                else result[1]++;
            }
            return result;
        }

        protected override void AdvancePosition(time_t position)
        {
            // This check just makes sure that we can process ticks.
            // If there are no state ticks, there should never be score ticks left.
            if (HasStateTicks)
            {
                Debug.Assert(HasScoreTicks);
            }
            else return;

            // Now, if we have ticks we can continue

            switch (m_state)
            {
                case JudgeState.Idle:
                {
                    var nextStateTick = NextStateTick;

                    time_t difference = position - (nextStateTick.Position + JudgementOffset);

                    // check missed chips
                    if (nextStateTick.State == JudgeState.ChipAwaitPress && difference > m_chipMissRadius)
                    {
                        var scoreTick = NextScoreTick;

                        Debug.Assert(scoreTick.Kind == TickKind.Chip);
                        Debug.Assert(scoreTick.Position == nextStateTick.Position);

                        OnTickProcessed?.Invoke(scoreTick.Entity, position, new JudgeResult(m_chipMissRadius, JudgeKind.Miss));

                        AdvanceStateTick();
                        AdvanceScoreTick();

                        IsBeingPlayed = false;
                    }
                    else if (nextStateTick.State == JudgeState.HoldAwaitPress && difference > 0)
                    {
                        m_state = JudgeState.HoldOff;

                        AdvanceStateTick();
                        m_currentStateTick = nextStateTick;

                        IsBeingPlayed = false;
                    }
                } break;

                case JudgeState.HoldOn:
                case JudgeState.HoldOff:
                {
                    var nextScoreTick = NextScoreTick;
                    Debug.Assert(nextScoreTick.Entity == m_currentStateTick.Entity);

                    if (position - (nextScoreTick.Position + JudgementOffset) >= 0)
                    {
                        var resultKind = IsBeingPlayed ? JudgeKind.Passive : JudgeKind.Miss;
                        OnTickProcessed?.Invoke(nextScoreTick.Entity, nextScoreTick.Position, new JudgeResult(0, resultKind));

                        AdvanceScoreTick();
                    }

                    var nextStateTick = NextStateTick;
                    if (nextStateTick.State == JudgeState.HoldAwaitRelease && position - (nextStateTick.Position + JudgementOffset) >= 0)
                    {
                        AdvanceStateTick();

                        m_state = JudgeState.Idle;
                        m_currentStateTick = null;
                    }
                } break;
            }
        }

        private void AdvanceStateTick() => m_stateIndex++;
        private void AdvanceScoreTick() => m_scoreIndex++;

        public JudgeResult? UserPressed(time_t position)
        {
            // This check just makes sure that we can process ticks.
            // If there are no state ticks, there should never be score ticks left.
            if (HasStateTicks)
            {
                Debug.Assert(HasScoreTicks);
            }
            else
            {
                SpawnKeyBeam?.Invoke(Label, JudgeKind.Passive, false);
                return null;
            }

            switch (m_state)
            {
                case JudgeState.Idle:
                {
                    var nextStateTick = NextStateTick;

                    time_t difference = position - (nextStateTick.Position + JudgementOffset);
                    time_t absDifference = Math.Abs((double)difference);

                    if (nextStateTick.State == JudgeState.ChipAwaitPress && absDifference <= m_chipMissRadius)
                    {
                        var scoreTick = NextScoreTick;

                        Debug.Assert(scoreTick.Kind == TickKind.Chip);
                        Debug.Assert(scoreTick.Position == nextStateTick.Position);

                        // `difference` applies to both ticks, don't recalculate

                        JudgeResult result;
                        if (absDifference <= m_chipPerfectRadius)
                            result = new JudgeResult(difference, JudgeKind.Perfect);
                        else if (absDifference <= m_chipCriticalRadius)
                            result = new JudgeResult(difference, JudgeKind.Critical);
                        else if (absDifference <= m_chipNearRadius)
                            result = new JudgeResult(difference, JudgeKind.Near);
                        // TODO(local): Is this how we want to handle misses?
                        else result = new JudgeResult(difference, JudgeKind.Bad);

                        OnTickProcessed?.Invoke(scoreTick.Entity, position, result);
                        OnChipPressed?.Invoke(position, scoreTick.Entity);
                        SpawnKeyBeam?.Invoke(scoreTick.Entity.Lane, result.Kind, difference < 0);

                        AdvanceStateTick();
                        AdvanceScoreTick();

                        // state stays idle after a chip press, chips are instantaneous

                        IsBeingPlayed = true;

                        return result;
                    }
                    else if (nextStateTick.State == JudgeState.HoldAwaitPress && absDifference <= m_holdActivateRadius)
                    {
                        OnHoldPressed?.Invoke(position, nextStateTick.Entity);

                        AdvanceStateTick();
                        // No need to advance a score tick, we haven't judged anything

                        // state is `hold on` because ofc we pressed the hold!
                        m_state = JudgeState.HoldOn;
                        m_currentStateTick = nextStateTick;

                        IsBeingPlayed = true;
                    }

                    // do nothing when pressed otherwise
                    else SpawnKeyBeam?.Invoke(Label, JudgeKind.Passive, false);
                } break;

                case JudgeState.HoldOff:
                {
                    OnHoldPressed?.Invoke(position, m_currentStateTick.Entity);

                    m_state = JudgeState.HoldOn;

                    IsBeingPlayed = true;
                } break;

                case JudgeState.HoldOn: throw new InvalidOperationException();
            }

            return null;
        }

        public void UserReleased(time_t position)
        {
            DespawnKeyBeam?.Invoke(Label);

            switch (m_state)
            {
                case JudgeState.Idle:
                case JudgeState.HoldOff: break; // do nothing when released

                case JudgeState.HoldOn:
                {
                    OnHoldReleased?.Invoke(position, m_currentStateTick.Entity);

                    m_state = JudgeState.HoldOff;

                    IsBeingPlayed = false;
                } break;
            }
        }
    }
}
