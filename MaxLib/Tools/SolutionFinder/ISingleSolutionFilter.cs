using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// Determines if a solution can be applied for a specific problems.
    /// </summary>
    /// <typeparam name="Problem">the problem</typeparam>
    /// <typeparam name="Solution">the solution for the problem</typeparam>
    public interface ISingleSolutionFilter<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Check if the solution can be applied for a specific problem.
        /// </summary>
        /// <param name="problem">the problem that should be solved</param>
        /// <param name="solution">the solution that should be checked with local rules</param>
        /// <returns>true if the solution is allowed to apply</returns>
        bool AcceptedSolution(Problem problem, Solution solution);
    }
}
