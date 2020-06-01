using MaxLib.Tools.SolutionFinder.Actions;
using MaxLib.Tools.SolutionFinder.Strategies;
using System;
using System.Collections.Generic;
using System.Text;

namespace MaxLib.Tools.SolutionFinder.Problems.Sudoku
{
    public class SudokuData : IProblem<SudokuData, SudokuStep>
    {
        public int Width { get; private set; }

        public int BlockWidth { get; private set; }

        public int BlockHeight { get; private set; }

        private int[,] Data { get; set; }

        private List<SudokuStep>[,] grid = null; //only used for solving

        public int this[int x, int y]
        {
            get => Data[x, y];
            set
            {
                grid = null;
                Data[x, y] = value;
            }
        }

        public SudokuData()
        {
            Width = BlockWidth = BlockHeight = 0;
            Data = new int[0, 0];
        }

        public SudokuData(int width, int blockWidth, int blockHeight)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (blockWidth <= 0) throw new ArgumentOutOfRangeException(nameof(blockWidth));
            if (blockHeight <= 0) throw new ArgumentOutOfRangeException(nameof(blockHeight));

            if (width % blockWidth != 0) throw new ArgumentException("width is not a multiple of value", nameof(blockWidth));
            if (width % blockWidth != 0) throw new ArgumentException("width is not a multiple of value", nameof(blockHeight));
            if (blockWidth * blockHeight != width) throw new ArgumentException($"{nameof(blockWidth)} * {nameof(blockHeight)} must be {nameof(width)}");

            Width = width;
            BlockWidth = blockWidth;
            BlockHeight = blockHeight;

            Data = new int[width, width];
        }

        public SudokuData Clone()
        {
            var clone = new SudokuData();
            clone.CloneFrom(this);
            return clone;
        }

        public void CloneFrom(SudokuData source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Width = source.Width;
            BlockWidth = source.BlockWidth;
            BlockHeight = source.BlockHeight;
            Data = (int[,])source.Data.Clone();
            grid = null;
        }

        public SudokuData Modify(SudokuStep solution)
        {
            var clone = Clone();
            if (solution.X < 0 || solution.X >= Width)
                throw new ArgumentOutOfRangeException(nameof(solution.X));
            if (solution.Y < 0 || solution.Y >= Width)
                throw new ArgumentOutOfRangeException(nameof(solution.Y));
            if (solution.Value < 0 || solution.Value > Width)
                throw new ArgumentException($"value must be between 0 and {Width}", nameof(solution.Value));

            clone[solution.X, solution.Y] = solution.Value;
            return clone;
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public override string ToString()
            => $"Width={Width}";

        public string Text
        {
            get
            {
                var bar = new StringBuilder();
                var digit = Width.ToString().Length;
                for (int i = 0; i<Width; ++i)
                {
                    if (i % BlockWidth == 0)
                        bar.Append('+');
                    else bar.Append('-');
                    bar.Append(new string('-', digit));
                }
                bar.Append('+');

                var text = new StringBuilder();
                for (int y = 0; y < Width; ++y)
                {
                    if (y % BlockHeight == 0)
                        text.AppendLine(bar.ToString());
                    for (int x = 0; x < Width; ++x)
                    {
                        if (x % BlockWidth == 0)
                            text.Append('|');
                        else text.Append(' ');
                        text.Append(Data[x, y].ToString().PadLeft(digit));
                    }
                    text.AppendLine("|");
                }
                text.Append(bar);
                return text.ToString();
            }
        }

        public static Solver<SudokuData, SudokuStep> Solver
        {
            get => new Solver<SudokuData, SudokuStep>
            {
                SolutionGenerator = new SolutionGenerator<SudokuData, SudokuStep>(GetAllPossibleSteps),
                SingleSolutionFilters =
                {
                    new SingleSolutionFilter<SudokuData, SudokuStep>(FilterSetted),
                    new SingleSolutionFilter<SudokuData, SudokuStep>(FilterRow),
                    new SingleSolutionFilter<SudokuData, SudokuStep>(FilterColumn),
                    new SingleSolutionFilter<SudokuData, SudokuStep>(FilterBlock),
                },
                FailDetectors =
                {
                    FailDetector<SudokuData, SudokuStep>.IfEmpty,
                    new FailDetector<SudokuData, SudokuStep>(IsFail),
                },
                GoodSolutionPickers =
                {
                    new GoodSolutionPicker<SudokuData, SudokuStep>(GoodSolution),
                },
                SolutionReducers =
                {
                    new SolutionReducer<SudokuData, SudokuStep>(ReduceSteps),
                },
                FinishDetector = new FinishDetector<SudokuData, SudokuStep>(IsFinished),
                Strategy = new DeepSearch<SudokuData, SudokuStep>(),
            };
        }

        public static IEnumerable<SudokuStep> GetAllPossibleSteps(SudokuData sudoku)
        {
            sudoku.grid = null;
            for (int x = 0; x < sudoku.Width; ++x)
                for (int y = 0; y < sudoku.Width; ++y)
                    for (int v = 1; v <= sudoku.Width; ++v)
                        yield return new SudokuStep(x, y, v);
        }

        public static bool FilterSetted(SudokuData sudoku, SudokuStep step)
        {
            return sudoku[step.X, step.Y] == 0;
        }

        public static bool FilterRow(SudokuData sudoku, SudokuStep step)
        {
            for (int x = 0; x < sudoku.Width; ++x)
                if (sudoku[x, step.Y] == step.Value)
                    return false;
            return true;
        }

        public static bool FilterColumn(SudokuData sudoku, SudokuStep step)
        {
            for (int y = 0; y < sudoku.Width; ++y)
                if (sudoku[step.X, y] == step.Value)
                    return false;
            return true;
        }

        public static bool FilterBlock(SudokuData sudoku, SudokuStep step)
        {
            var xs = sudoku.BlockWidth * (step.X / sudoku.BlockWidth);
            var ys = sudoku.BlockHeight * (step.Y / sudoku.BlockHeight);
            for (int x = xs; x < xs + sudoku.BlockWidth; ++x)
                for (int y = ys; y < ys + sudoku.BlockHeight; ++y)
                    if (sudoku[x, y] == step.Value)
                        return false;
            return true;
        }

        public static bool IsFinished(SudokuData sudoku)
        {
            for (int x = 0; x < sudoku.Width; ++x)
                for (int y = 0; y < sudoku.Width; ++y)
                    if (sudoku[x, y] == 0)
                        return false;
            return true;
        }

        private static List<SudokuStep>[,] Group(SudokuData sudoku, IEnumerable<SudokuStep> steps)
        {
            if (sudoku.grid != null)
                return sudoku.grid;

            var grid = new List<SudokuStep>[sudoku.Width, sudoku.Width];
            for (int x = 0; x < sudoku.Width; ++x)
                for (int y = 0; y < sudoku.Width; ++y)
                    grid[x, y] = new List<SudokuStep>();

            foreach (var step in steps)
                grid[step.X, step.Y].Add(step);

            return sudoku.grid = grid;
        }

        public static bool IsFail(SudokuData sudoku, IEnumerable<SudokuStep> steps)
        {
            var grid = Group(sudoku, steps);

            for (int x = 0; x < sudoku.Width; ++x)
                for (int y = 0; y < sudoku.Width; ++y)
                    if (grid[x, y].Count == 0 && sudoku[x, y] == 0)
                        return true;

            return false;
        }

        public static bool GoodSolution(SudokuData sudoku, IEnumerable<SudokuStep> steps, out SudokuStep best)
        {
            var grid = Group(sudoku, steps);

            foreach (var cell in grid) 
                if (cell.Count == 1)
                {
                    best = cell[0];
                    return true;
                }

            best = default;
            return false;
        }

        public static IEnumerable<SudokuStep> ReduceSteps(SudokuData sudoku, IEnumerable<SudokuStep> steps)
        {
            var grid = Group(sudoku, steps);

            foreach (var cell in grid)
                if (cell.Count > 0)
                    return cell;

            return steps;
        }
    }

    public struct SudokuStep
    {
        public int X { get; set; }

        public int Y { get; set; }

        public int Value { get; set; }

        public SudokuStep(int x, int y, int value)
        {
            X = x;
            Y = y;
            Value = value;
        }

        public override string ToString()
            => $"({X}, {Y}) = {Value}";
    }
}
