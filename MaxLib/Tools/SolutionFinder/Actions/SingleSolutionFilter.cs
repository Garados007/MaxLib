using System;

namespace MaxLib.Tools.SolutionFinder.Actions
{
    public class SingleSolutionFilter<Problem, Solution> : ISingleSolutionFilter<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        readonly Func<Problem, Solution, bool> func;

        public SingleSolutionFilter(Func<Problem, Solution, bool> func)
            => this.func = func ?? throw new ArgumentNullException(nameof(func));

        public bool AcceptedSolution(Problem problem, Solution solution)
            => func(problem, solution);
    }
}
