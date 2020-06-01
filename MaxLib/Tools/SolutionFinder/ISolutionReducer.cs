using System.Collections.Generic;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// this class will look at all solutions and return only good solutions to check for. Unimportant solutions
    /// will be removed.
    /// </summary>
    /// <typeparam name="Problem">the problem to solve</typeparam>
    /// <typeparam name="Solution">the solution to solve the problem</typeparam>
    public interface ISolutionReducer<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Reduce the set of solutions and returns only good ones. Unimportant solutions will be removed.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <param name="solutions">all current solutions that could solve the problem</param>
        /// <returns>good solutions that will solve the problem</returns>
        IEnumerable<Solution> Reduce(Problem problem, IEnumerable<Solution> solutions);
    }
}
