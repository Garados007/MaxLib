using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// If the problem cannot be solved with the found solution (or solutions) this will define the
    /// strategy to find the optimal one
    /// </summary>
    /// <typeparam name="Problem">the problem to solve</typeparam>
    /// <typeparam name="Solution">the solution to solve the problem</typeparam>
    public interface IGuessStrategy<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// Commit a node with more than one solutions. The solutions are given in order of 
        /// chance of solving the problem
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <param name="solutions">the resulting collection of solutions that could solve the problem</param>
        void CommitMultiSolution(Problem problem, IEnumerable<Solution> solutions);

        /// <summary>
        /// In the current step there is only one solution found but this will not solve the problem directly.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <param name="solution">the only solution in the current step</param>
        void CommitSingleSolution(Problem problem, Solution solution);

        /// <summary>
        /// Commit that the current strategy was a fail and tell the <see cref="IGuessStrategy{Problem, Solution}"/>
        /// to find a better one for the next step.
        /// </summary>
        /// <param name="problem">the problem that couldn't be solved</param>
        void CommitFail(Problem problem);

        /// <summary>
        /// The current amount of solutions that are stored to try later.
        /// </summary>
        int BufferCount { get; }

        /// <summary>
        /// Returns true if a next step exists to check. This check could be faster than checking
        /// <see cref="BufferCount"/> is larger than 0.
        /// </summary>
        /// <returns></returns>
        bool HasNextStep();

        /// <summary>
        /// Returns the next branche that should be be tried to solve.
        /// </summary>
        /// <returns></returns>
        (Problem problem, Solution solution)? NextStep();

        /// <summary>
        /// Resets the buffer
        /// </summary>
        void Reset();
    }
}
