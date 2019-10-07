using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// Detects if the current problem is finished and there could not any good solution found
    /// </summary>
    /// <typeparam name="Problem">the problem to solve</typeparam>
    /// <typeparam name="Solution">the solution to solve the problem</typeparam>
    public interface IFinishDetector<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Check if the given problem is finished.
        /// </summary>
        /// <param name="problem">the problem to check</param>
        /// <returns>true if the problem is finished.</returns>
        bool IsFinished(Problem problem);
    }
}
