using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Tools.SolutionFinder.Strategies
{
    /// <summary>
    /// Performs the deep search strategy
    /// </summary>
    public class DeepSearch<Problem, Solution> : IGuessStrategy<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        private readonly Stack<(Problem problem, Solution[] solutions, int index)> stack;

        public DeepSearch()
        {
            stack = new Stack<(Problem, Solution[], int)>();
        }

        public int BufferCount
        {
            get => stack.Sum(entry => Math.Max(0, entry.solutions.Length - entry.index));
        }

        public void CommitFail(Problem problem)
        {
        }

        public void CommitMultiSolution(Problem problem, IEnumerable<Solution> solutions)
        {
            stack.Push((problem, solutions.ToArray(), 0));
        }

        public void CommitSingleSolution(Problem problem, Solution solution)
        {
            stack.Push((problem, new[] { solution }, 0));
        }

        public bool HasNextStep() => stack.Count > 0;

        public (Problem problem, Solution solution)? NextStep()
        {
            if (stack.Count == 0)
                return null;
            var entry = stack.Pop();
            while (entry.index >= entry.solutions.Length)
                if (stack.Count == 0)
                    return null;
                else entry = stack.Pop();
            var step = (entry.problem.Clone(), entry.solutions[entry.index]);
            stack.Push((entry.problem, entry.solutions, entry.index + 1));
            return step;
        }

        public void Reset()
        {
            stack.Clear();
        }
    }
}
