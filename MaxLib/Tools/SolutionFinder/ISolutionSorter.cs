using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// Sort the resulting set of solutions with a descending order of quality. The first items
    /// will have a high chance to solve to problem and the last ones the lowest.
    /// </summary>
    /// <typeparam name="Problem">the problem to solve</typeparam>
    /// <typeparam name="Solution">the solution to solve the problem</typeparam>
    public interface ISolutionSorter<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Will sort the set of solution with a descending order of quality. The first items
        /// will have a high chance to solve to problem and the last ones the lowest.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <param name="solutions">the set of solution that could solve the solutions</param>
        /// <returns>the sorted solutions</returns>
        IEnumerable<Solution> Sort(Problem problem, IEnumerable<Solution> solutions);
    }
}
