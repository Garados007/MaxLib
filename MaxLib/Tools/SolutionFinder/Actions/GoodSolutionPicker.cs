using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class GoodSolutionPicker<Problem, Solution> : IGoodSolutionPicker<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func func;

        public GoodSolutionPicker(Func func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public bool Pick(Problem problem, IEnumerable<Solution> solutions, out Solution goodSolution)
            => func(problem, solutions, out goodSolution);

        public delegate bool Func(Problem problem, IEnumerable<Solution> solutions, out Solution goodSolution);
    }
}
