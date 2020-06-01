using System.Collections.Generic;

namespace MaxLib.Tools.SolutionFinder
{
    /// <summary>
    /// A implementation of a problem solver.
    /// </summary>
    /// <typeparam name="Problem">The problem to solve</typeparam>
    /// <typeparam name="Solution">The solution that would solve the problem</typeparam>
    public class Solver<Problem, Solution> : SolverBase<Problem, Solution>
        where Problem : IProblem<Problem, Solution>, new()
    {
        public virtual new ISolutionGenerator<Problem, Solution> SolutionGenerator
        {
            get => base.SolutionGenerator;
            set => base.SolutionGenerator = value;
        }

        public virtual new IList<ISingleSolutionFilter<Problem, Solution>> SingleSolutionFilters
            => (IList<ISingleSolutionFilter<Problem, Solution>>)base.SingleSolutionFilters;

        public virtual new IList<IFailDetector<Problem, Solution>> FailDetectors
            => (IList<IFailDetector<Problem, Solution>>)base.FailDetectors;

        public virtual new IList<IGoodSolutionPicker<Problem, Solution>> GoodSolutionPickers
            => (IList<IGoodSolutionPicker<Problem, Solution>>)base.GoodSolutionPickers;

        public virtual new IList<ISolutionReducer<Problem, Solution>> SolutionReducers
            => (IList<ISolutionReducer<Problem, Solution>>)base.SolutionReducers;

        public virtual new ISolutionSorter<Problem, Solution> SolutionSorter
        {
            get => base.SolutionSorter;
            set => base.SolutionSorter = value;
        }

        public virtual new IFinishDetector<Problem, Solution> FinishDetector
        {
            get => base.FinishDetector;
            set => base.FinishDetector = value;
        }

        public virtual new IGuessStrategy<Problem, Solution> Strategy
        {
            get => base.Strategy;
            set => base.Strategy = value;
        }

        /// <summary>
        /// creates a new instance of a problem solver
        /// </summary>
        public Solver()
        {
            base.SingleSolutionFilters = new List<ISingleSolutionFilter<Problem, Solution>>();
            base.FailDetectors = new List<IFailDetector<Problem, Solution>>();
            base.GoodSolutionPickers = new List<IGoodSolutionPicker<Problem, Solution>>();
            base.SolutionReducers = new List<ISolutionReducer<Problem, Solution>>();
        }
    }
}
