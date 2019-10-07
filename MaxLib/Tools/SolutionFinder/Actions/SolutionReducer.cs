using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class SolutionReducer<Problem, Solution> : ISolutionReducer<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func<Problem, IEnumerable<Solution>, IEnumerable<Solution>> func;

        public SolutionReducer(Func<Problem, IEnumerable<Solution>, IEnumerable<Solution>> func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public IEnumerable<Solution> Reduce(Problem problem, IEnumerable<Solution> solutions)
            => func(problem, solutions);
    }
}
