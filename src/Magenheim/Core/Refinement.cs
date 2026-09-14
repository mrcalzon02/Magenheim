using System;

namespace Magenheim.Core
{
    public sealed class RefinementOutcome
    {
        public bool Success { get; private set; }
        public string Element { get; private set; }
        public string OutputTier { get; private set; }
        public int Shards { get; private set; }
        public double Experience { get; private set; }
        public double FailureChance { get; private set; }
        internal RefinementOutcome(bool success, string element, RefinementStep step, double chance)
        { Success = success; Element = element; OutputTier = success ? step.Target : null;
          Shards = success ? 0 : step.Shards; Experience = step.Experience; FailureChance = chance; }
    }

    // Pure decision logic; inventory consumption and RPC authority belong to a future transaction adapter.
    public static class Refinement
    {
        public static double FailureChance(double baseFailure, double skill, double maximumReduction)
        {
            Range(baseFailure, 0, 1, "baseFailure");
            Range(skill, 0, 100, "skill");
            Range(maximumReduction, 0.5, 1, "maximumReduction");
            return baseFailure * (1 - skill / 100 * maximumReduction);
        }
        public static RefinementOutcome Evaluate(DefinitionSnapshot data, string element, string source,
            int stationLevel, double skill, double roll)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (!data.Elements.Contains(element)) throw new ArgumentException("Unknown element: " + element);
            var step = data.Step(source);
            if (stationLevel < step.StationLevel) throw new InvalidOperationException("Workstation level " + step.StationLevel + " required");
            Range(roll, 0, 1, "roll");
            if (roll == 1) throw new ArgumentOutOfRangeException("roll", "Random sample must be less than 1");
            double failure = FailureChance(step.BaseFailure, skill, data.MaximumFailureReduction);
            return new RefinementOutcome(roll >= failure, element, step, failure);
        }
        private static void Range(double value, double min, double max, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
