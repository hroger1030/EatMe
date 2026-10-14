using System;
using System.Collections.Generic;

namespace EatMeLibrary
{
    /// <summary>
    /// What a bot remembers between cell centres, so it sticks to a plan instead of changing its mind every step:
    /// its goal and how long it keeps it, whether it is fleeing and from where, the cells it keeps away from after
    /// fleeing, and how long until it may turn back between centres again. Update counts the timers down.
    /// </summary>
    public class BotMemory
    {
        /// <summary>
        /// Shortest time, in ms, a bot keeps a goal before picking again (unless the goal is done or something urgent
        /// comes up).
        /// </summary>
        public const int GOAL_COMMIT_MIN_MS = 2000;

        /// <summary>
        /// Longest time, in ms, a bot keeps a goal. Each goal gets a random time between the two, so bots don't all
        /// pick again together.
        /// </summary>
        public const int GOAL_COMMIT_MAX_MS = 4000;

        /// <summary>
        /// Once a bot starts fleeing it keeps fleeing for at least this many ms.
        /// </summary>
        public const int FLEE_MIN_MS = 1000;

        /// <summary>
        /// How long, in ms, a bot keeps away from where its threats were after it stops fleeing.
        /// </summary>
        public const int AVOID_MS = 3000;

        /// <summary>
        /// Cells around each threat's last cell, either way, that a bot keeps out of while avoiding.
        /// </summary>
        public const int AVOID_RADIUS = 2;

        /// <summary>
        /// After turning back between cell centres, a bot can't do it again for this many ms.
        /// </summary>
        public const int TURN_BACK_COOLDOWN_MS = 500;

        private readonly List<(int X, int Y)> _ThreatCells = [];
        private bool[,] _AvoidMask = new bool[0, 0];

        public eBotGoal Goal { get; private set; }

        /// <summary>
        /// ms left before the bot picks a new goal.
        /// </summary>
        public int GoalMsLeft { get; private set; }

        public bool IsFleeing { get; private set; }

        /// <summary>
        /// ms left of the shortest flee (FLEE_MIN_MS).
        /// </summary>
        public int FleeMsLeft { get; private set; }

        /// <summary>
        /// Where the threats were the last time the bot fled from them.
        /// </summary>
        public IReadOnlyList<(int X, int Y)> ThreatCells => _ThreatCells;

        /// <summary>
        /// ms left of keeping away from the cells in AvoidMask.
        /// </summary>
        public int AvoidMsLeft { get; private set; }

        public bool IsAvoiding => AvoidMsLeft > 0;

        /// <summary>
        /// Cells the bot keeps out of while IsAvoiding ([x, y], map sized); for PathMap.Fill.
        /// </summary>
        public bool[,] AvoidMask => _AvoidMask;

        public int TurnBackMsLeft { get; private set; }

        public bool CanTurnBack => TurnBackMsLeft <= 0;

        /// <summary>
        /// True if the goal should be kept rather than picked again: there is one and its time isn't up.
        /// </summary>
        public bool IsCommitted => Goal != eBotGoal.None && GoalMsLeft > 0;

        /// <summary>
        /// Counts every timer down by ms. Avoiding ends when its time is up.
        /// </summary>
        public void Update(int ms)
        {
            GoalMsLeft = Math.Max(0, GoalMsLeft - ms);
            FleeMsLeft = Math.Max(0, FleeMsLeft - ms);
            AvoidMsLeft = Math.Max(0, AvoidMsLeft - ms);
            TurnBackMsLeft = Math.Max(0, TurnBackMsLeft - ms);
        }

        /// <summary>
        /// Forgets everything, for a bot that died or a new level.
        /// </summary>
        public void Reset()
        {
            Goal = eBotGoal.None;
            GoalMsLeft = 0;
            IsFleeing = false;
            FleeMsLeft = 0;
            _ThreatCells.Clear();
            AvoidMsLeft = 0;
            TurnBackMsLeft = 0;
        }

        /// <summary>
        /// Takes on a new goal for a random time from GOAL_COMMIT_MIN_MS to GOAL_COMMIT_MAX_MS.
        /// </summary>
        public void SetGoal(eBotGoal goal, Random random)
        {
            Goal = goal;
            GoalMsLeft = random.Next(GOAL_COMMIT_MIN_MS, GOAL_COMMIT_MAX_MS + 1);
        }

        /// <summary>
        /// Drops the goal, so the next cell centre picks again.
        /// </summary>
        public void ClearGoal()
        {
            Goal = eBotGoal.None;
            GoalMsLeft = 0;
        }

        /// <summary>
        /// Flees from threats at these cells; starting a flee also starts its FLEE_MIN_MS.
        /// </summary>
        public void Flee(IReadOnlyList<(int X, int Y)> threats)
        {
            if (!IsFleeing)
            {
                IsFleeing = true;
                FleeMsLeft = FLEE_MIN_MS;
            }

            if (!ReferenceEquals(threats, _ThreatCells))
            {
                _ThreatCells.Clear();
                _ThreatCells.AddRange(threats);
            }
        }

        /// <summary>
        /// Stops fleeing: drops the goal (it may have led into the danger) and keeps away from the cells within
        /// AVOID_RADIUS of where the threats were for AVOID_MS.
        /// </summary>
        public void StopFleeing(int width, int height)
        {
            IsFleeing = false;
            FleeMsLeft = 0;
            ClearGoal();

            if (_AvoidMask.GetLength(0) != width || _AvoidMask.GetLength(1) != height)
                _AvoidMask = new bool[width, height];
            else
                Array.Clear(_AvoidMask);

            foreach (var (tx, ty) in _ThreatCells)
                for (var x = Math.Max(0, tx - AVOID_RADIUS); x <= Math.Min(width - 1, tx + AVOID_RADIUS); x++)
                    for (var y = Math.Max(0, ty - AVOID_RADIUS); y <= Math.Min(height - 1, ty + AVOID_RADIUS); y++)
                        _AvoidMask[x, y] = true;

            AvoidMsLeft = _ThreatCells.Count > 0 ? AVOID_MS : 0;
        }

        /// <summary>
        /// Notes a turn back between cell centres, starting its TURN_BACK_COOLDOWN_MS.
        /// </summary>
        public void TurnedBack()
        {
            TurnBackMsLeft = TURN_BACK_COOLDOWN_MS;
        }
    }
}
