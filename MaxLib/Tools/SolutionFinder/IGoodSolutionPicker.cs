using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// This class will pick the best solution if the set of solution contains a best solution. Sometimes this 
    /// will happen under specific circumstances. If the best solution is found the solver will ignore the rest
    /// of solutions and will continue with the best one.
    /// </summary>
    /// <typeparam name="Problem">the problem to solve</typeparam>
    /// <typeparam name="Solution">the solution to solve the problem</typeparam>
    public interface IGoodSolutionPicker<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Pick the best solution out of the set of possible solutions if it contains one. If a very good
        /// solution was found it will return it.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <param name="solutions">all current solutions to use</param>
        /// <param name="goodSolution">the best solution if found</param>
        /// <returns>true if the best solution was found</returns>
        bool Pick(Problem problem, IEnumerable<Solution> solutions, out Solution goodSolution);
    }
}
