using System.Collections.Generic;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// Detect if the current set solutions will not solve the problem. This will warn the solver
    /// to adjust the strategy.
    /// </summary>
    /// <typeparam name="Problem">the problem to solve</typeparam>
    /// <typeparam name="Solution">the solution to solve the problem</typeparam>
    public interface IFailDetector<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Detect if the current set of solutions will not solve the problem.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <param name="solutions">the current set of solutions</param>
        /// <returns>true if the solutions will not solve the problem</returns>
        bool IsFail(Problem problem, IEnumerable<Solution> solutions);
    }
}
