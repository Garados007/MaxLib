using System;
using System.Collections.Generic;
using System.Linq;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// Defines tha basic implementation of a solver. This class cannot create instances. 
    /// Use one of its derivations.
    /// </summary>
    /// <typeparam name="Problem">The problem to solve</typeparam>
    /// <typeparam name="Solution">The solution that would solve the problem</typeparam>
    public abstract class SolverBase<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        /// <summary>
        /// The Generator that create the possible solutions for a specific problem
        /// </summary>
        public virtual ISolutionGenerator<Problem, Solution> SolutionGenerator { get; protected set; }

        /// <summary>
        /// Filters that select valid solutions
        /// </summary>
        public virtual IEnumerable<ISingleSolutionFilter<Problem, Solution>> SingleSolutionFilters { get; protected set; }

        /// <summary>
        /// Detectors that will nofity if there are the solutions cannot solve the problem
        /// </summary>
        public virtual IEnumerable<IFailDetector<Problem, Solution>> FailDetectors { get; protected set; }

        /// <summary>
        /// Picker that can definitly find very good solutions of a set of solutions that should
        /// be continued with.
        /// </summary>
        public virtual IEnumerable<IGoodSolutionPicker<Problem, Solution>> GoodSolutionPickers { get; protected set; }

        /// <summary>
        /// Recuser that look on all solutions and filteres only good ones.
        /// </summary>
        public virtual IEnumerable<ISolutionReducer<Problem, Solution>> SolutionReducers { get; protected set; }

        /// <summary>
        /// Sorter that will sort the solutions descending their chance to solve the problem.
        /// </summary>
        public virtual ISolutionSorter<Problem, Solution> SolutionSorter { get; protected set; }

        /// <summary>
        /// Detector if the problem is solved
        /// </summary>
        public virtual IFinishDetector<Problem, Solution> FinishDetector { get; protected set; }

        /// <summary>
        /// The current strategy to follow to solve the problem
        /// </summary>
        public virtual IGuessStrategy<Problem, Solution> Strategy { get; protected set; }

        private IEnumerable<T> Empty<T>()
        {
            yield break;
        }

        /// <summary>
        /// Perform <see cref="ISolutionGenerator{Problem, Solution}"/>
        /// </summary>
        protected virtual IEnumerable<Solution> GetBasicSolutions(Problem problem)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            return SolutionGenerator?.GetSolutions(problem) ?? Empty<Solution>();
        }

        /// <summary>
        /// Perform <see cref="ISingleSolutionFilter{Problem, Solution}"/>
        /// </summary>
        protected virtual IEnumerable<Solution> SingleFilterSolutions(Problem problem, IEnumerable<Solution> solutions)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));
            if (solutions == null) solutions = Empty<Solution>();

            if (SingleSolutionFilters != null)
                foreach (var filter in SingleSolutionFilters)
                    if (filter != null)
                        solutions = solutions.Where(s => filter.AcceptedSolution(problem, s));

            return solutions;
        }

        /// <summary>
        /// Perform <see cref="IFailDetector{Problem, Solution}"/>
        /// </summary>
        protected virtual bool IsFail(Problem problem, IEnumerable<Solution> solutions)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));
            if (solutions == null) solutions = Empty<Solution>();

            if (FailDetectors != null)
                foreach (var detector in FailDetectors)
                    if (detector?.IsFail(problem, solutions) ?? false)
                        return true;

            return false;
        }

        /// <summary>
        /// Perform <see cref="IGoodSolutionPicker{Problem, Solution}"/>
        /// </summary>
        protected virtual bool HasGoodSolution(Problem problem, IEnumerable<Solution> solutions, out Solution goodSolution)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));
            if (solutions == null) solutions = Empty<Solution>();

            if (GoodSolutionPickers != null)
                foreach (var picker in GoodSolutionPickers)
                    if (picker != null && picker.Pick(problem, solutions, out Solution solution))
                    {
                        goodSolution = solution;
                        return true;
                    }

            goodSolution = default;
            return false;
        }

        /// <summary>
        /// Perfrom <see cref="ISolutionReducer{Problem, Solution}"/>
        /// </summary>
        protected virtual IEnumerable<Solution> ReduceSolutions(Problem problem, IEnumerable<Solution> solutions)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));
            if (solutions == null) solutions = Empty<Solution>();

            if (SolutionReducers != null)
                foreach (var reducer in SolutionReducers)
                    solutions = reducer?.Reduce(problem, solutions) ?? Empty<Solution>();

            return solutions;
        }

        /// <summary>
        /// Perform <see cref="ISolutionSorter{Problem, Solution}"
        protected virtual IEnumerable<Solution> Sort(Problem problem, IEnumerable<Solution> solutions)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));
            if (solutions == null) solutions = Empty<Solution>();

            return SolutionSorter?.Sort(problem, solutions) ?? solutions;
        }

        /// <summary>
        /// Perform <see cref="IFinishDetector{Problem, Solution}"/>
        /// </summary>
        protected virtual bool IsFinished(Problem problem)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            return FinishDetector?.IsFinished(problem) ?? true;
        }

        /// <summary>
        /// Commit the result to <see cref="Strategy"/>
        /// </summary>
        protected virtual void Commit(Problem problem, IEnumerable<Solution> solutions)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));
            if (solutions == null) solutions = Empty<Solution>();

            var finite = solutions.ToArray();

            if (finite.Length == 0)
                Strategy?.CommitFail(problem);
            else if (finite.Length == 1)
                Strategy?.CommitSingleSolution(problem, finite[0]);
            else Strategy?.CommitMultiSolution(problem, finite);
        }

        /// <summary>
        /// <see cref="IGuessStrategy{Problem, Solution}.BufferCount"/>
        /// </summary>
        protected virtual int Buffercount => Strategy?.BufferCount ?? 0;

        /// <summary>
        /// <see cref="IGuessStrategy{Problem, Solution}.HasNextStep"/>
        /// </summary>
        protected virtual bool HasNextStep => Strategy?.HasNextStep() ?? false;

        /// <summary>
        /// <see cref="IGuessStrategy{Problem, Solution}.NextStep"/>
        /// </summary>
        /// <returns></returns>
        protected virtual (Problem problem, Solution solution)? NextStep()
        {
            return Strategy?.NextStep();
        }

        /// <summary>
        /// <see cref="IGuessStrategy{Problem, Solution}.Reset"/>
        /// </summary>
        protected virtual void Reset() => Strategy?.Reset();

        /// <summary>
        /// Try to solve a single step of the problem. It will return the resulting
        /// set of solutions. If no solutions are found null will be returned.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <returns>the resulting solutions</returns>
        protected virtual IEnumerable<Solution> SolveSingleStep(Problem problem)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            var basic = GetBasicSolutions(problem);
            var filtered = SingleFilterSolutions(problem, basic).ToArray();
            if (HasGoodSolution(problem, filtered, out Solution best1))
                return new[] { best1 };
            if (IsFail(problem, filtered))
                return null;

            var reduced = ReduceSolutions(problem, filtered).ToArray();
            if (HasGoodSolution(problem, reduced, out Solution best2))
                return new[] { best2 };
            if (IsFail(problem, reduced))
                return null;

            return Sort(problem, reduced);
        }

        /// <summary>
        /// Calls <see cref="Problem.Modify(Solution)"/>.
        /// </summary>
        protected virtual Problem Modify(Problem problem, Solution solution)
        {
            return problem == null ? default : problem.Modify(solution);
        }

        /// <summary>
        /// Try to solve the whole problem. If no solution could be found
        /// it will return the default value of <typeparamref name="Problem"/>.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <returns>The solved problem</returns>
        public virtual Problem Solve(Problem problem)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            Reset();

            while (problem != null)
            {
                if (IsFinished(problem))
                    return problem;

                var solutions = SolveSingleStep(problem);
                Commit(problem, solutions);

                if (HasNextStep)
                {
                    var next = NextStep();
                    if (next != null)
                    {
                        problem = Modify(next.Value.problem, next.Value.solution);
                    }
                    else return default;
                }
                else return default;
            }

            return default;
        }

        /// <summary>
        /// Try to solve a single step and apply the first solution to it. If 
        /// this problem cannot be solved the default value of 
        /// <typeparamref name="Problem"/> will be returned. The <see cref="Strategy"/>
        /// will not be applied.
        /// </summary>
        /// <param name="problem">the problem to solve</param>
        /// <returns>the modified problem</returns>
        public virtual Problem SolveStep(Problem problem)
        {
            if (problem == null) throw new ArgumentNullException(nameof(problem));

            if (IsFinished(problem))
                return problem;

            var solutions = SolveSingleStep(problem).Take(1).ToArray();

            if (solutions.Length == 0)
                return default;

            return Modify(problem, solutions[0]);
        }
    }
}
