using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MoreLinq;
using System.Diagnostics;

namespace ConwayCenturyPuzzle
{
    public static class Solver
    {
        private static int gridLenghtY = 0;
        private static int gridLenghtX = 0;

        public static LinkedList<(char rectangleMoved, char direction)> Solve(char[,] grid)
        {
            var nodesToExplore = new Queue<char[,]>();
            var nodesExplored = new Dictionary<char[,], (char[,] position, char rectangleMoved, char direction)>();
            var positionVisited = new HashSet<string>();
            var isSolve = false;

            gridLenghtY = grid.GetLength(0);
            gridLenghtX = grid.GetLength(1);

            var rectanglesType = AssignTypeToEachRectangle(grid);

            nodesToExplore.Enqueue(grid);
            nodesExplored.Add(grid, (null, '_', '_'));
            var (pos, symPos) = SavePositionWithHerSymmetry(grid, rectanglesType);
            positionVisited.Add(pos);
            positionVisited.Add(symPos);

            while (nodesToExplore.Count > 0 && !isSolve)
            {
                grid = nodesToExplore.Dequeue();

                foreach (var (position, rectangleMoved, direction) in GeAllNextPositions(grid))
                {
                    (pos, symPos) = SavePositionWithHerSymmetry(position, rectanglesType);
                    if (positionVisited.Contains(pos) || positionVisited.Contains(symPos)) continue;
                    nodesToExplore.Enqueue(position);
                    nodesExplored.Add(position, (grid, rectangleMoved, direction));
                    positionVisited.Add(pos);
                    positionVisited.Add(symPos);

                    if (!position.IsWinningPosition()) continue;
                    grid = position;
                    isSolve = true;
                    break;
                }
            }

            return isSolve
                ? GetSolutionFromDictionary(nodesExplored, grid)
                : new LinkedList<(char rectangleMoved, char direction)>();
        }

        private static IEnumerable<(char[,] position, char rectangleMoved, char direction)> GeAllNextPositions(
            char[,] grid)
        {
            var rectangleVisited = new HashSet<char>();
            var newPositions = new List<(char[,] position, char rectangleMoved, char direction)>();

            for (var y = 0; y < gridLenghtY; y++)
            for (var x = 0; x < gridLenghtX; x++)
            {
                if (rectangleVisited.Contains(grid[y, x]) || grid[y, x].Equals('A')) continue;
                rectangleVisited.Add(grid[y, x]);
                var dimRectangle = GetRectangleDim(grid[y, x], grid);
                newPositions.AddRange(GetNextPositionsByMovingOneRectangle(dimRectangle, grid)
                    .Select(p => (p.position, grid[y, x], p.direction)));
            }

            return newPositions;
        }

        private static IEnumerable<(char[,] position, char direction)> GetNextPositionsByMovingOneRectangle(RectangleDimension rectangleDimension, char[,] grid)
        {
            var NextPositions = new List<(char[,] position, char direction)>();

            if (CanGoLeft(rectangleDimension, grid))
            {
                var nextGrid = (char[,]) grid.Clone();
                for (var y = rectangleDimension.YMin; y <= rectangleDimension.YMax; y++)
                {
                    nextGrid[y, rectangleDimension.XMin - 1] = nextGrid[y, rectangleDimension.XMin];
                    nextGrid[y, rectangleDimension.XMax] = 'A';

                    NextPositions.Add((nextGrid, 'L'));
                }
            }

            if (CanGoRight(rectangleDimension, grid))
            {
                var nextGrid = (char[,]) grid.Clone();
                for (var y = rectangleDimension.YMin; y <= rectangleDimension.YMax; y++)
                {
                    nextGrid[y, rectangleDimension.XMax + 1] = nextGrid[y, rectangleDimension.XMax];
                    nextGrid[y, rectangleDimension.XMin] = 'A';
                    NextPositions.Add((nextGrid, 'R'));
                }
            }

            if (CanGoUp(rectangleDimension, grid))
            {
                var nextGrid = (char[,]) grid.Clone();
                for (var x = rectangleDimension.XMin; x <= rectangleDimension.XMax; x++)
                {
                    nextGrid[rectangleDimension.YMin - 1, x] = nextGrid[rectangleDimension.YMin, x];
                    nextGrid[rectangleDimension.YMax, x] = 'A';
                    NextPositions.Add((nextGrid, 'U'));
                }
            }

            if (CanGoDown(rectangleDimension, grid))
            {
                var nextGrid = (char[,]) grid.Clone();
                for (var x = rectangleDimension.XMin; x <= rectangleDimension.XMax; x++)
                {
                    nextGrid[rectangleDimension.YMax + 1, x] = nextGrid[rectangleDimension.YMax, x];
                    nextGrid[rectangleDimension.YMin, x] = 'A';
                    NextPositions.Add((nextGrid, 'D'));
                }
            }

            return NextPositions;
        }

        #region checkMovesMethods

        private static bool CanGoLeft(RectangleDimension dimension, char[,] grid)
        {
            if (dimension.XMin - 1 < 0) return false;
            for (var y = dimension.YMin; y <= dimension.YMax; y++)
            {
                if (grid[y, dimension.XMin - 1] != 'A')
                {
                    return false;
                }
            }
            return true;

        }

        private static bool CanGoRight(RectangleDimension dimension, char[,] grid)
        {
            if (dimension.XMax + 1 >= gridLenghtX) return false;
            for (var y = dimension.YMin; y <= dimension.YMax; y++)
            {
                if (grid[y, dimension.XMax + 1] != 'A')
                {
                    return false;
                }
            }
            return true;

        }

        private static bool CanGoUp(RectangleDimension dimension, char[,] grid)
        {
            if (dimension.YMin - 1 < 0) return false;
            for (var x = dimension.XMin; x <= dimension.XMax; x++)
            {
                if (grid[dimension.YMin - 1, x] != 'A')
                {
                    return false;
                }
            }
            return true;

        }

        private static bool CanGoDown(RectangleDimension dimension, char[,] grid)
        {
            if (dimension.YMax + 1 >= gridLenghtY) return false;
            for (var x = dimension.XMin; x <= dimension.XMax; x++)
            {
                if (grid[dimension.YMax + 1, x] != 'A')
                {
                    return false;
                }
            }
            return true;

        }
        #endregion

        private static (string, string) SavePositionWithHerSymmetry(char[,] pos,
            IReadOnlyDictionary<char, char> rectanglesType)
        {
            var str = new StringBuilder();
            var str2 = new StringBuilder();

            for (var y = 0; y < gridLenghtY; y++)
            for (var x = 0; x < gridLenghtX; x++)
            {
                str.Append(rectanglesType[pos[y, x]]);
                str2.Append(rectanglesType[pos[y, gridLenghtX - 1 - x]]);
            }

            return (str.ToString(), str2.ToString());
        }

        private static RectangleDimension GetRectangleDim(char rectangle, char[,] grid)
        {
            var dimRect = new RectangleDimension();

            var firstPosition = true;

            for (var y = 0; y < gridLenghtY; y++)
            for (var x = 0; x < gridLenghtX; x++)
            {
                if (grid[y, x] != rectangle) continue;
                if (firstPosition)
                {
                    firstPosition = false;
                    dimRect.XMin = x;
                    dimRect.YMin = y;
                }

                dimRect.XMax = x;
                dimRect.YMax = y;
            }

            return dimRect;
        }

        private static Dictionary<char,char> AssignTypeToEachRectangle(char[,] grid)
        {
            var rectangleType = 'A';
            var ConvertTypeToChar = new Dictionary<string, char>();

            return grid.Cast<char>().Distinct()
                .Select(rectangle =>
                    {
                        var dimP = GetRectangleDim(rectangle, grid);
                        var type = rectangle.Equals('A')
                            ? rectangle.ToString()
                            : $"{dimP.XMax - dimP.XMin}{dimP.YMax - dimP.YMin}";
                        if (!ConvertTypeToChar.ContainsKey(type))
                            ConvertTypeToChar.Add(type, rectangleType++);

                        return (rectangle, ConvertTypeToChar[type]);
                    }
                )
                .ToDictionary();
        }

        private static bool IsWinningPosition(this char[,] position)
        {
            return position[3, 1] == 'J' && position[3, 2] == 'J' && position[4, 1] == 'J' && position[4, 2] == 'J';
        }

        private static LinkedList<(char rectangleMoved, char direction)> GetSolutionFromDictionary(
            IReadOnlyDictionary<char[,], (char[,] position, char rectangleMoved, char direction)> dic,
            char[,] winningPosition)
        {
            var node = dic[winningPosition];
            var solution = new LinkedList<(char rectangleMoved, char direction)>();

            while (node.position != null)
            {
                solution.AddFirst((node.rectangleMoved, node.direction));
                node = dic[node.position];
            }

            return solution;
        }

        private class RectangleDimension
        {
            public int XMin { get; set; }
            public int XMax { get; set; }
            public int YMin { get; set; }
            public int YMax { get; set; }
        }
    }
}