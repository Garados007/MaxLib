using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// Describe a specific problem that can have multiple or none solutions. The goal is to find the best solution.
    /// </summary>
    /// <typeparam name="Problem">The inherited problem</typeparam>
    /// <typeparam name="Solution">the solution that can modify this problem to a new problem</typeparam>
    public interface IProblem<Problem, Solution> : ICloneable
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Modify this problem and return the new problem. This method should be deterministic.
        /// </summary>
        /// <param name="solution">the solution that should be applied</param>
        /// <returns>the new problem instance</returns>
        Problem Modify(Solution solution);

        /// <summary>
        /// clone this problem instance
        /// </summary>
        /// <returns>the independent clone of this problem</returns>
        new Problem Clone();

        /// <summary>
        /// Copy the date from the other problem.
        /// </summary>
        /// <param name="source">the data source</param>
        void CloneFrom(Problem source);
    }
}
