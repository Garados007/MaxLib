using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class FinishDetector<Problem, Solution> : IFinishDetector<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func<Problem,  bool> func;

        public FinishDetector(Func<Problem, bool> func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public bool IsFinished(Problem problem)
            => func(problem);
    }
}
