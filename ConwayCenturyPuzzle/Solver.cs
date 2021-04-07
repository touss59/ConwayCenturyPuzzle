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

        public static LinkedList<(char pieceMoved, char direction)> Solve(char[,] grid)
        {

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var nodesToExplore = new Queue<char[,]>();
            var nodesExplored = new Dictionary<char[,], (char[,] position, char pieceMoved, char direction)>();
            var positionVisited = new HashSet<string>();
            bool isSolve = false;

            gridLenghtY = grid.GetLength(0);
            gridLenghtX = grid.GetLength(1);

            var shapesType = AssignTypeToEachShape(grid);

            nodesToExplore.Enqueue(grid);
            nodesExplored.Add(grid, (null, '_', '_'));
            var posSave = SavePositionWithHerSimmetry(grid,shapesType);
            positionVisited.Add(posSave.Item1);
            positionVisited.Add(posSave.Item2);

            while (nodesToExplore.Count > 0 && !isSolve)
            {
                var pieceVisited = new HashSet<char>();
                var newPositions = new List<(char[,] position, char pieceMoved, char direction)> { };

                grid = nodesToExplore.Dequeue();

                for (var y = 0; y < gridLenghtY; y++)
                {
                    for (var x = 0; x < gridLenghtX; x++)
                    {
                        if (!pieceVisited.Contains(grid[y, x]) && !grid[y, x].Equals('A'))
                        {
                            pieceVisited.Add(grid[y, x]);
                            var dimPiece = GetPieceDim(grid[y, x], grid);
                            var moves = GetPossibleMoves(dimPiece, grid);

                            foreach (var move in moves)
                            {
                                newPositions.Add((Move(dimPiece, (char[,])grid.Clone(), move), grid[y, x], move));
                            }
                        }
                    }
                }
                foreach (var (position, pieceMoved, direction) in newPositions)
                {
                    posSave = SavePositionWithHerSimmetry(position, shapesType);
                    if (!positionVisited.Contains(posSave.Item1) && !positionVisited.Contains(posSave.Item2))
                    {
                        nodesToExplore.Enqueue(position);
                        nodesExplored.Add(position, (grid, pieceMoved, direction));
                        positionVisited.Add(posSave.Item1);
                        positionVisited.Add(posSave.Item2);

                        if (position.IsWinPosition())
                        {
                            grid = position;
                            isSolve = true;
                            break;
                        }


                    }
                }
            }

            var node = nodesExplored[grid];
            var response = new LinkedList<(char pieceMoved, char direction)>();

            while (node.position != null)
            {
                response.AddFirst((node.pieceMoved, node.direction));
                node = nodesExplored[node.position];
            }
            stopWatch.Stop();
            var t = stopWatch.ElapsedMilliseconds;
            return response;
        }

        private static (string, string) SavePositionWithHerSimmetry(char[,] pos, Dictionary<char, char> shapesType)
        {
            var str = new StringBuilder();
            var str2 = new StringBuilder();
            for (var y = 0; y < gridLenghtY; y++)
            {
                for (var x = 0; x < gridLenghtX; x++)
                {
                    str.Append(shapesType[pos[y, x]]);
                    str2.Append(shapesType[pos[y, gridLenghtX - 1 - x]]);
                }
            }

            return (str.ToString(), str2.ToString());
        }

        private static List<char> GetPossibleMoves(PieceDimension dimension, char[,] grid)
        {
            var PossibleMoves = new List<char>();

            if (CanGoLeft(dimension, grid))
                PossibleMoves.Add('L');

            if (CanGoRight(dimension, grid))
                PossibleMoves.Add('R');

            if (CanGoUp(dimension, grid))
                PossibleMoves.Add('U');

            if (CanGoDown(dimension, grid))
                PossibleMoves.Add('D');

            return PossibleMoves;
        }

        #region checkMovesMethods

        private static bool CanGoLeft(PieceDimension dimension, char[,] grid)
        {
            var goLeft = false;
            if (dimension.XMin - 1 >= 0)
            {
                for (var y = dimension.YMin; y <= dimension.YMax; y++)
                {
                    if (grid[y, dimension.XMin - 1] != 'A')
                    {
                        return goLeft;
                    }
                }
                goLeft = true;
            }
            return goLeft;

        }

        private static bool CanGoRight(PieceDimension dimension, char[,] grid)
        {
            var goRight = false;
            if (dimension.XMax + 1 < gridLenghtX)
            {
                for (var y = dimension.YMin; y <= dimension.YMax; y++)
                {
                    if (grid[y, dimension.XMax + 1] != 'A')
                    {
                        return goRight;
                    }
                }
                goRight = true;
            }
            return goRight;

        }

        private static bool CanGoUp(PieceDimension dimension, char[,] grid)
        {
            var goUp = false;
            if (dimension.YMin - 1 >= 0)
            {
                for (var x = dimension.XMin; x <= dimension.XMax; x++)
                {
                    if (grid[dimension.YMin - 1, x] != 'A')
                    {
                        return goUp;
                    }
                }
                goUp = true;
            }
            return goUp;

        }

        private static bool CanGoDown(PieceDimension dimension, char[,] grid)
        {
            var goDown = false;
            if (dimension.YMax + 1 < gridLenghtY)
            {
                for (var x = dimension.XMin; x <= dimension.XMax; x++)
                {
                    if (grid[dimension.YMax + 1, x] != 'A')
                    {
                        return goDown;
                    }
                }
                goDown = true;
            }
            return goDown;

        }
        #endregion

        private static char[,] Move(PieceDimension dimension, char[,] grid, char direction)
        {
            switch (direction)
            {
                case 'U':
                    for (var x = dimension.XMin; x <= dimension.XMax; x++)
                    {
                        grid[dimension.YMin - 1, x] = grid[dimension.YMin, x];
                        grid[dimension.YMax, x] = 'A';
                    }
                    break;
                case 'D':
                    for (var x = dimension.XMin; x <= dimension.XMax; x++)
                    {
                        grid[dimension.YMax + 1, x] = grid[dimension.YMax, x];
                        grid[dimension.YMin, x] = 'A';
                    }
                    break;
                case 'L':
                    for (var y = dimension.YMin; y <= dimension.YMax; y++)
                    {
                        grid[y, dimension.XMin - 1] = grid[y, dimension.XMin];
                        grid[y, dimension.XMax] = 'A';
                    }
                    break;
                default:
                    for (var y = dimension.YMin; y <= dimension.YMax; y++)
                    {
                        grid[y, dimension.XMax + 1] = grid[y, dimension.XMax];
                        grid[y, dimension.XMin] = 'A';
                    }
                    break;
            }
            return grid;
        }

        private static PieceDimension GetPieceDim(char piece, char[,] grid)
        {
            PieceDimension dimP = new PieceDimension(999, 0, 999, 0);
            for (var y = 0; y < gridLenghtY; y++)
            {
                for (var x = 0; x < gridLenghtX; x++)
                {
                    if (grid[y, x] == piece)
                    {
                        dimP.XMin = dimP.XMin <= x ? dimP.XMin : x;
                        dimP.XMax = dimP.XMax >= x ? dimP.XMax : x;
                        dimP.YMin = dimP.YMin <= y ? dimP.YMin : y;
                        dimP.YMax = dimP.YMax >= y ? dimP.YMax : y;
                    }
                }
            }

            return dimP;
        }

        private static Dictionary<char,char> AssignTypeToEachShape(char[,] grid)
        {
            var ShapeType = 'A';
            var ConvertTypeToChar = new Dictionary<string, char>();

            return grid.GetDistinctShapes()
                       .Select(shape =>
                           {
                               var dimP = GetPieceDim(shape, grid);
                               var type = shape.Equals('A')? shape.ToString() : $"{dimP.XMax - dimP.XMin}{dimP.YMax - dimP.YMin}";
                               if (!ConvertTypeToChar.ContainsKey(type))
                                   ConvertTypeToChar.Add(type,ShapeType++);

                               return (shape, ConvertTypeToChar[type]);
                           }
                       )
                       .ToDictionary();
        }

        private static bool IsWinPosition(this char[,] position)
        {
            return position[3, 1] == 'J' && position[3, 2] == 'J' && position[4, 1] == 'J' && position[4, 2] == 'J';
        }

        private static IEnumerable<char> GetDistinctShapes(this char[,] array)
        {
            var distinctChars = new List<char>();
            
            foreach(var c in array)
            {
                distinctChars.Add(c);
            }

            return distinctChars.Distinct();
        }

        private class PieceDimension
        {
            public int XMin { get; set; }
            public int XMax { get; set; }
            public int YMin { get; set; }
            public int YMax { get; set; }

            public PieceDimension(int xMin, int xMax, int yMin, int yMax)
            {
                this.XMin = xMin;
                this.XMax = xMax;
                this.YMin = yMin;
                this.YMax = yMax;
            }
        }
    }
}