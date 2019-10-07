using System;
using System.Collections.Generic;
using System.Text;
using MaxLib.Tools.SolutionFinder.Problems.Sudoku;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace MaxLib.Test.Tools.SolutionFinder.Problems.Sudoku
{
    [TestClass]
    [TestCategory("tools > solutions > sudoku")]
    public class SudokuTest
    {
        [TestMethod]
        public void TestIfSuccessfulyResult()
        {
            var solver = SudokuData.Solver;
            var sudoku = new SudokuData(9, 3, 3);
            var result = solver.Solve(sudoku);
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void TestPredefined()
        {
            var given = new[]
            {
                " 92|6 7|4 5",
                "5 8|  4|2  ",
                " 3 |9 5| 78",

                " 19| 43|5  ",
                "72 |56 | 13",
                "  3|21 |94 ",

                " 81|  6|7 4",
                "9 7|45 | 82",
                "3 5|872| 91"
            };
            var solver = SudokuData.Solver;
            var sudoku = new SudokuData(9, 3, 3);
            for (int y = 0; y  <9; ++y)
            {
                int x = 0;
                for (int i = 0; i < given[y].Length; ++i)
                    if (given[y][i] == ' ')
                        x++;
                    else if (given[y][i] >= '0' && given[y][i] <= '9')
                        sudoku[x++, y] = given[y][i] - '0';
            }
            var result = solver.Solve(sudoku);
            var control = new string(new[]
            {
                "192|687|435",
                "578|134|269",
                "436|925|178",

                "819|743|526",
                "724|569|813",
                "653|218|947",

                "281|396|754",
                "967|451|382",
                "345|872|691",
            }.SelectMany(s => s).Where(c => c >= '0' && c <= '9').ToArray());
            var text = new string(result.Text.Where(c => c >= '0' && c <= '9').ToArray());
            Assert.AreEqual(control, text);
        }
    }
}
