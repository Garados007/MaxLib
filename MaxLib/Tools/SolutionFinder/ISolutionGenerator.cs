using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// This class can generator the possible solutions for a specific problem instance.
    /// The result can contain bad solutions too!
    /// </summary>
    /// <typeparam name="Problem">the problem that should be solved</typeparam>
    /// <typeparam name="Solution">the solutions to solve the problem</typeparam>
    public interface ISolutionGenerator<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Get the possible solutions for a specific problem. 
        /// </summary>
        /// <param name="problem">the problem for that the solutions should be generated</param>
        /// <returns>all possible solutions</returns>
        IEnumerable<Solution> GetSolutions(Problem problem);
    }
}
