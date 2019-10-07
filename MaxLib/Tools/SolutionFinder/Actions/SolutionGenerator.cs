using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class SolutionGenerator<Problem, Solution> : ISolutionGenerator<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func<Problem, IEnumerable<Solution>> func;

        public SolutionGenerator(Func<Problem, IEnumerable<Solution>> func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public IEnumerable<Solution> GetSolutions(Problem problem)
            => func(problem);
    }
}
