using System;
using System.Collections.Generic;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class SolutionSorter<Problem, Solution> : ISolutionSorter<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func<Problem, IEnumerable<Solution>, IEnumerable<Solution>> func;

        public SolutionSorter(Func<Problem, IEnumerable<Solution>, IEnumerable<Solution>> func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public IEnumerable<Solution> Sort(Problem problem, IEnumerable<Solution> solutions)
            => func(problem, solutions);
    }
}
