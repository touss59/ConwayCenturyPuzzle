using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using MoreLinq;
using System.Diagnostics;

namespace ConwayCenturyPuzzle
{
    public class Solver
    {
        private static readonly char[,] valueStartingNode = new char[1, 1] { { 'S' } };
        private static readonly char[,] keyWinningNode = new char[1, 1] { { 'W' } };
        private static int gridLenghtY = 0;
        private static int gridLenghtX = 0;
        private static Dictionary<char, char> shapesType = new Dictionary<char, char>();
        private static readonly StringBuilder str = new StringBuilder();
        private static readonly StringBuilder str2 = new StringBuilder();
        public static List<char[,]> Solve(char[,] grid)
        {

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Queue<char[,]> nodesToExplore = new Queue<char[,]>();
            Dictionary<char[,], char[,]> nodesExplored = new Dictionary<char[,], char[,]>();
            HashSet<string> positionVisited = new HashSet<string>();
            bool isSolve = false;

            gridLenghtY = grid.GetLength(0);
            gridLenghtX = grid.GetLength(1);

            if (shapesType.Count == 0)
                AssignTypeToEachShape(grid);

            nodesToExplore.Enqueue(grid);
            nodesExplored.Add(grid, valueStartingNode);
            var posSave = SavePosition(grid);
            positionVisited.Add(posSave.Item1);
            positionVisited.Add(posSave.Item2);

            HashSet<char> pieceVisited = new HashSet<char>();
            List<char[,]> newPositions = new List<char[,]> { };

            while (nodesToExplore.Count > 0 && !isSolve)
            {
                pieceVisited.Clear();
                newPositions.Clear();

                grid = nodesToExplore.Dequeue();

                for (var y = 0; y < gridLenghtY; y++)
                {
                    for (var x = 0; x < gridLenghtX; x++)
                    {
                        if (!pieceVisited.Contains(grid[y, x]) && grid[y, x] != 'A')
                        {
                            pieceVisited.Add(grid[y, x]);
                            var dimP = GetDimPiece(grid[y, x], grid);
                            var (left, right, up, down) = WhereItCanMove(dimP, grid);

                            if (left)
                            {
                                newPositions.Add(Move(dimP, grid, 'L'));
                            }
                            if (right)
                            {
                                newPositions.Add(Move(dimP, grid, 'R'));
                            }
                            if (up)
                            {
                                newPositions.Add(Move(dimP, grid, 'U'));
                            }
                            if (down)
                            {
                                newPositions.Add(Move(dimP, grid, 'D'));
                            }
                        }
                    }
                }
                foreach (var newPos in newPositions)
                {
                    posSave = SavePosition(newPos);
                    if (!positionVisited.Contains(posSave.Item1) && !positionVisited.Contains(posSave.Item2))
                    {
                        nodesToExplore.Enqueue(newPos);
                        nodesExplored.Add(newPos, grid);
                        positionVisited.Add(posSave.Item1);
                        positionVisited.Add(posSave.Item2);

                        if (IsWinPosition(newPos))
                        {
                            nodesExplored.Add(keyWinningNode, newPos);
                            isSolve = true;
                            break;
                        }


                    }
                }
            }

            grid = nodesExplored[keyWinningNode];
            List<char[,]> response = new List<char[,]> { };

            while (grid != valueStartingNode)
            {
                response.Add(grid);
                grid = nodesExplored[grid];
            }
            stopWatch.Stop();
            var t = stopWatch.ElapsedMilliseconds;
            return response;
        }

        public static (int xMin, int xMax, int yMin, int yMax) GetDimPiece(char piece, char[,] grid)
        {
            (int xMin, int xMax, int yMin, int yMax) = (999, 0, 999, 0);
            for (var y = 0; y < gridLenghtY; y++)
            {
                for (var x = 0; x < gridLenghtX; x++)
                {
                    if (grid[y, x] == piece)
                    {
                        xMin = xMin <= x ? xMin : x;
                        xMax = xMax >= x ? xMax : x;
                        yMin = yMin <= y ? yMin : y;
                        yMax = yMax >= y ? yMax : y;
                    }
                }
            }

            return (xMin, xMax, yMin, yMax);
        }

        public static (bool left, bool right, bool up, bool down) WhereItCanMove((int xMin, int xMax, int yMin, int yMax) dimension, char[,] grid)
        {
            (bool left, bool right, bool up, bool down) = (true, true, true, true);

            //check move left
            if (dimension.xMin - 1 >= 0)
            {
                for (var y = dimension.yMin; y <= dimension.yMax; y++)
                {
                    if (grid[y, dimension.xMin - 1] != 'A')
                    {
                        left = false;
                    }
                }
            }
            else
            {
                left = false;
            }

            //check move right
            if (dimension.xMax + 1 < gridLenghtX)
            {
                for (var y = dimension.yMin; y <= dimension.yMax; y++)
                {
                    if (grid[y, dimension.xMax + 1] != 'A')
                    {
                        right = false;
                    }
                }
            }
            else
            {
                right = false;
            }

            //check move up
            if (dimension.yMin - 1 >= 0)
            {
                for (var x = dimension.xMin; x <= dimension.xMax; x++)
                {
                    if (grid[dimension.yMin - 1, x] != 'A')
                    {
                        up = false;
                    }
                }
            }
            else
            {
                up = false;
            }

            //check move down
            if (dimension.yMax + 1 < gridLenghtY)
            {
                for (var x = dimension.xMin; x <= dimension.xMax; x++)
                {
                    if (grid[dimension.yMax + 1, x] != 'A')
                    {
                        down = false;
                    }
                }
            }
            else
            {
                down = false;
            }

            return (left, right, up, down);
        }

        public static char[,] Move((int xMin, int xMax, int yMin, int yMax) dimension, char[,] grid, char direction)
        {
            char[,] gridMoved = CopieGrid(grid);

            switch (direction)
            {
                case 'U':
                    for (var x = dimension.xMin; x <= dimension.xMax; x++)
                    {
                        gridMoved[dimension.yMin - 1, x] = grid[dimension.yMin, x];
                        gridMoved[dimension.yMax, x] = 'A';
                    }
                    break;
                case 'D':
                    for (var x = dimension.xMin; x <= dimension.xMax; x++)
                    {
                        gridMoved[dimension.yMax + 1, x] = grid[dimension.yMax, x];
                        gridMoved[dimension.yMin, x] = 'A';
                    }
                    break;
                case 'L':
                    for (var y = dimension.yMin; y <= dimension.yMax; y++)
                    {
                        gridMoved[y, dimension.xMin - 1] = grid[y, dimension.xMin];
                        gridMoved[y, dimension.xMax] = 'A';
                    }
                    break;
                default:
                    for (var y = dimension.yMin; y <= dimension.yMax; y++)
                    {
                        gridMoved[y, dimension.xMax + 1] = grid[y, dimension.xMax];
                        gridMoved[y, dimension.xMin] = 'A';
                    }
                    break;
            }
            return gridMoved;
        }

        public static char[,] CopieGrid(char[,] grid)
        {
            char[,] newGrid = new char[gridLenghtY, gridLenghtX];

            for (var y = 0; y < gridLenghtY; y++)
            {
                for (var x = 0; x < gridLenghtX; x++)
                {
                    newGrid[y, x] = grid[y, x];
                }
            }
            return newGrid;
        }

        public static (string, string) SavePosition(char[,] pos)
        {
            str.Clear();
            str2.Clear();
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

        public static List<(char shapeToMove, char direction)> ShowMoves(char[,] grid)
        {
            List<char[,]> response = Solve(grid);
            response.Reverse();
            List<(char, char)> moves = new List<(char, char)> { };


            for (var i = 1; i < response.Count; i++)
            {
                moves.Add(GetMove(response[i - 1], response[i]));
            }
            return moves;
        }


        public static (char shapeMove, char direction) GetMove(char[,] previous, char[,] now)
        {
            char shapeMove = GetShapeMoved(previous, now);

            (int xMin, int xMax, int yMin, int yMax) dimP = GetDimPiece(shapeMove, previous);
            (int xMin, int xMax, int yMin, int yMax) dimN = GetDimPiece(shapeMove, now);

            char direction = GetDirection(dimP, dimN);

            return (shapeMove, direction);

        }

        public static char GetShapeMoved(char[,] previous, char[,] now)
        {
            for (var y = 0; y < gridLenghtY; y++)
            {
                for (var x = 0; x < gridLenghtX; x++)
                {
                    if (previous[y, x] != now[y, x] && now[y, x] != 'A')
                    {
                        return now[y, x];
                    }
                }
            }
            return 'T';
        }

        public static char GetDirection((int xMin, int xMax, int yMin, int yMax) dimP, (int xMin, int xMax, int yMin, int yMax) dimN)
        {
            if (dimN.xMin < dimP.xMin)
            {
                return 'L';
            }
            else if (dimN.xMin > dimP.xMin)
            {
                return 'R';
            }
            else if (dimN.yMin < dimP.yMin)
            {
                return 'U';
            }
            else
            {
                return 'D';
            }
        }

        private static void AssignTypeToEachShape(char[,] grid)
        {
            Dictionary<string, List<char>> keyValuePairs = new Dictionary<string, List<char>>();
            HashSet<char> a = new HashSet<char>() { 'A' };
            char t = 'A';

            foreach (var c in grid)
            {
                if (!a.Contains(c))
                {
                    var (xMin, xMax, yMin, yMax) = GetDimPiece(c, grid);
                    var type = $"{xMax - xMin}{yMax - yMin}";
                    if (keyValuePairs.ContainsKey(type))
                    {
                        keyValuePairs[type].Add(c);
                    }
                    else
                    {
                        keyValuePairs.Add(type, new List<char> { c });
                    }
                    a.Add(c);
                }
            }
            shapesType.Add('A', 'A');

            keyValuePairs.ForEach(k =>
            {
                t += 'A';
                k.Value.ForEach(v =>
                {
                    shapesType.Add(v, t);
                });

            });

        }

        private static bool IsWinPosition(char[,] newPos)
        {
            return newPos[3, 1] == 'J' && newPos[3, 2] == 'J' && newPos[4, 1] == 'J' && newPos[4, 2] == 'J';
        }
    }
}
