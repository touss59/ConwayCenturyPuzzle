using System;
using System.Collections.Generic;
using System.Text;

namespace ConwayCenturyPuzzle
{
    public class Solver
    {
        public static List<char[,]> Solve(char[,] grid)
        {
            var firstPosition = StockGrid(grid);
            (string historique, string positionNow) positionNow = (firstPosition, firstPosition);

            Queue<(string historique, string positionNow)> nodesToExplore = new Queue<(string historique, string positionNow)> { };
            Dictionary<string, string> nodesExplored = new Dictionary<string, string> { };
            HashSet<string> positionVisited = new HashSet<string> { };
            nodesToExplore.Enqueue(positionNow);
            bool isSolve = false;

            nodesExplored.Add(firstPosition, "start");
            positionVisited.Add(SavePosition(firstPosition));

            while (nodesToExplore.Count > 0 && !isSolve)
            {
                positionNow = nodesToExplore.Dequeue();

                grid = GetGrid(positionNow.positionNow);

                List<char> pieceVisited = new List<char> { };
                List<string> newPositions = new List<string> { };

                for (var y = 0; y < grid.GetLength(0); y++)
                {
                    for (var x = 0; x < grid.GetLength(1); x++)
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
                    if (!positionVisited.Contains(SavePosition(newPos)) && !positionVisited.Contains(GetSymmetry(SavePosition(newPos))))
                    {
                        (string historique, string positionNow) position = (positionNow.positionNow, newPos);
                        nodesExplored.Add(newPos, positionNow.positionNow);
                        positionVisited.Add(SavePosition(newPos));
                        nodesToExplore.Enqueue(position);

                        if (newPos[13] == 'J' && newPos[14] == 'J' && newPos[17] == 'J' && newPos[18] == 'J')
                        {
                            nodesExplored.Add("win", newPos);
                            isSolve = true;
                            break;
                        }


                    }
                }
            }

            string key = "win";

            List<char[,]> response = new List<char[,]> { };

            while (1 == 1)
            {
                key = nodesExplored[key];

                char[,] step = new char[5, 4];

                if (key == "start")
                {
                    break;
                }

                var index = 0;
                for (var y = 0; y < grid.GetLength(0); y++)
                {
                    for (var x = 0; x < grid.GetLength(1); x++)
                    {
                        step[y, x] = key[index];
                        index++;
                    }
                }

                response.Add(step);
            }
            return response;
        }

        public static (int xMin, int xMax, int yMin, int yMax) GetDimPiece(char piece, char[,] grid)
        {
            (int xMin, int xMax, int yMin, int yMax) = (999, 0, 999, 0);
            for (var y = 0; y < grid.GetLength(0); y++)
            {
                for (var x = 0; x < grid.GetLength(1); x++)
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
            if (dimension.xMax + 1 < grid.GetLength(1))
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
            if (dimension.yMax + 1 < grid.GetLength(0))
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

        public static string Move((int xMin, int xMax, int yMin, int yMax) dimension, char[,] grid, char direction)
        {
            char[,] gridMoved = CopieGrid(grid);
            if (direction == 'U')
            {
                for (var x = dimension.xMin; x <= dimension.xMax; x++)
                {
                    gridMoved[dimension.yMin - 1, x] = grid[dimension.yMin, x];
                    gridMoved[dimension.yMax, x] = 'A';
                }
                return StockGrid(gridMoved);
            }

            if (direction == 'D')
            {
                for (var x = dimension.xMin; x <= dimension.xMax; x++)
                {
                    gridMoved[dimension.yMin, x] = 'A';
                    gridMoved[dimension.yMax + 1, x] = grid[dimension.yMax, x];
                }
                return StockGrid(gridMoved);
            }

            if (direction == 'L')
            {
                for (var y = dimension.yMin; y <= dimension.yMax; y++)
                {
                    gridMoved[y, dimension.xMin - 1] = grid[y, dimension.xMin];
                    gridMoved[y, dimension.xMax] = 'A';
                }
                return StockGrid(gridMoved);
            }

            if (direction == 'R')
            {
                for (var y = dimension.yMin; y <= dimension.yMax; y++)
                {
                    gridMoved[y, dimension.xMax + 1] = grid[y, dimension.xMax];
                    gridMoved[y, dimension.xMin] = 'A';
                }
                return StockGrid(gridMoved);
            }

            return StockGrid(gridMoved);
        }

        public static char[,] CopieGrid(char[,] grid)
        {
            char[,] newGrid = new char[grid.GetLength(0), grid.GetLength(1)];

            for (var y = 0; y < grid.GetLength(0); y++)
            {
                for (var x = 0; x < grid.GetLength(1); x++)
                {
                    newGrid[y, x] = grid[y, x];
                }
            }
            return newGrid;
        }

        public static string StockGrid(char[,] grid)
        {
            string result = "";

            foreach (var piece in grid)
            {
                result += piece;
            }
            return result;
        }

        public static char[,] GetGrid(string gridStocked)
        {
            char[,] grid = new char[5, 4];
            int i = 0;

            for (var y = 0; y < grid.GetLength(0); y++)
            {
                for (var x = 0; x < grid.GetLength(1); x++)
                {
                    grid[y, x] = gridStocked[i];
                    i++;
                }
            }
            return grid;
        }

        public static string GetSymmetry(string pos)
        {
            char[,] symmetry = GetGrid(pos);

            for (var y = 0; y < symmetry.GetLength(0); y++)
            {
                List<char> pieces = new List<char> { };
                for (var x = 0; x < symmetry.GetLength(1); x++)
                {
                    pieces.Add(symmetry[y, x]);
                }

                symmetry[y, 0] = pieces[3];
                symmetry[y, 1] = pieces[2];
                symmetry[y, 2] = pieces[0];
                symmetry[y, 3] = pieces[1];
            }

            return StockGrid(symmetry);
        }

        public static string SavePosition(string pos)
        {
            string savePos = "";
            for (var i = 0; i < pos.Length; i++)
            {
                if (pos[i] == 'X' || pos[i] == 'W')
                {
                    savePos += "V";
                }
                else if (pos[i] == 'r' || pos[i] == 'K' || pos[i] == 'k')
                {
                    savePos += 'R';
                }
                else if (pos[i] == 'Y')
                {
                    savePos += "M";
                }
                else
                {
                    savePos += pos[i];
                }
            }
            return savePos;
        }


        public static List<(char shapeToMove, char direction)> ShowMove(char[,] grid)
        {
            List<char[,]> response = Solve(grid);
            response.Reverse();
            List<(char,char)> moves = new List<(char,char)> { };


            for (var i = 1; i < response.Count; i++)
            {
                moves.Add(GetMove(response[i - 1], response[i]));
            }
            return moves;
        }


        public static (char shapeMove, char direction) GetMove(char[,] previous, char[,] now)
        {
            char shapeMove = GetShape(previous, now);

            (int xMin, int xMax, int yMin, int yMax) dimP = GetDimPiece(shapeMove, previous);
            (int xMin, int xMax, int yMin, int yMax) dimN = GetDimPiece(shapeMove, now);

            char direction = GetDirection(dimP, dimN);

            return (shapeMove, direction);

        }

        public static char GetShape(char[,] previous, char[,] now)
        {
            for (var y = 0; y < previous.GetLength(0); y++)
            {
                for (var x = 0; x < previous.GetLength(1); x++)
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
            if (dimN.xMin > dimP.xMin)
            {
                return 'R';
            }

            if (dimN.yMin < dimP.yMin)
            {
                return 'U';
            }
            else
            {
                return 'D';
            }
        }
    }
}
