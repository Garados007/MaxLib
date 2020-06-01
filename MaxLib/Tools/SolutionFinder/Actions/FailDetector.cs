using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class FailDetector<Problem, Solution> : IFailDetector<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func<Problem, IEnumerable<Solution>, bool> func;

        public FailDetector(Func<Problem, IEnumerable<Solution>, bool> func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public bool IsFail(Problem problem, IEnumerable<Solution> solutions)
        {
            return func(problem, solutions);
        }

        public static FailDetector<Problem, Solution> IfEmpty
            => new FailDetector<Problem, Solution>((_, s) => s.Count() == 0);
    }
}
