using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.IO;
using Path = System.IO.Path;

namespace ConwayCenturyPuzzle
{
    public partial class MainWindow : Window
    {
        private readonly char[,] position = new char[5, 4];
        private const int SquareUnit = 100;
        private Point offset;
        private Rectangle dragRectangle;
        private readonly List<Rectangle> rectangles;
        private readonly DispatcherTimer dispatcherTimer;
        private LinkedListNode<(char rectangleToMove, char direction)> move;
        private LinkedList<(char rectangleToMove, char direction)> moves;

        public MainWindow()
        {
            InitializeComponent();
            rectangles = canvasMain.Children.OfType<UIElement>()
                .Where(c => c.GetType().Name.Equals(nameof(Rectangle)))
                .Select(c => c as Rectangle)
                .ToList();

            PuzzleSettings.SetRectanglesPosition(rectangles);

            var filePathSaveState = System.IO.Path.Combine(Environment.CurrentDirectory, "saveState.txt");

            if (File.Exists(filePathSaveState))
            {
                using var fs = File.OpenRead(filePathSaveState);
                using var sr = new StreamReader(fs);

                var lines = sr.ReadLine()?.Split(';');

                foreach (var line in lines)
                {
                    var (name, left, top) = (line.Split(',')[0], Convert.ToInt32(line.Split(',')[1]), Convert.ToInt32(line.Split(',')[2]));
                    var rectangle = rectangles.FirstOrDefault(r => r.Name.Equals(name));
                    Canvas.SetLeft(rectangle, left);
                    Canvas.SetTop(rectangle, top);
                }

            }

            dispatcherTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 50)
            };

            dispatcherTimer.Tick += new EventHandler(ShowSolution);
        }

        private void ExecuteSolution(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    FillArrayWithInitValue(position, 'A');
                    FillArrayWithRectangles();
                    moves = Solver.Solve(position);
                    move = moves.Find(moves.First());
                    dispatcherTimer.Start();
                    return;
                case Key.R:
                    dispatcherTimer.Stop();
                    ResetPositions();
                    return;
                default:
                    dispatcherTimer.Stop();
                    break;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var rtfFile = Path.Combine(Environment.CurrentDirectory, "saveState.txt");
            var fileStream = File.Create(rtfFile);
            using var streamWriter = new StreamWriter(fileStream);
            var state = string
                .Join(';', rectangles
                    .Where(rectangle => rectangle.Name.Length == 1)
                    .Select(rectangle => rectangle.Name + ',' + Math.Round(Canvas.GetLeft(rectangle)) + ',' +
                                         Math.Round(Canvas.GetTop(rectangle))));

            streamWriter.Write(state);
        }


        private void ShowSolution(object sender, EventArgs e)
        {
 
            if (move != null)
            {
                var rectangle = rectangles.FirstOrDefault(r => r.Name.Equals(move.Value.rectangleToMove.ToString()));

                switch (move.Value.direction)
                {
                    case 'L':
                        Canvas.SetLeft(rectangle, Canvas.GetLeft(rectangle) - SquareUnit);
                        break;
                    case 'U':
                        Canvas.SetTop(rectangle, Canvas.GetTop(rectangle) - SquareUnit);
                        break;
                    case 'R':
                        Canvas.SetLeft(rectangle, Canvas.GetLeft(rectangle) + SquareUnit);
                        break;
                    case 'D':
                        Canvas.SetTop(rectangle, Canvas.GetTop(rectangle) + SquareUnit);
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }
                move = move.Next;
                return;
            }
            dispatcherTimer.Stop();
        }

        private void UserControlPreviewMouseDown(object sender, MouseEventArgs e)
        {
            dragRectangle = sender as Rectangle;
            if (dragRectangle == null) return;
            offset = e.GetPosition(canvasMain);
            offset.Y -= Canvas.GetTop(dragRectangle);
            offset.X -= Canvas.GetLeft(dragRectangle);
            canvasMain.CaptureMouse();
        }

        private void CanvasMainMouseUp(object sender, MouseEventArgs e)
        {
            dragRectangle = null;

            canvasMain.ReleaseMouseCapture();
        }

        private void CanvasMainMouseMove(object sender, MouseEventArgs e)
        {
            var newPosition = e.GetPosition(sender as IInputElement);

            if (dragRectangle == null || MouseLeaveDragRectangle(newPosition) || dispatcherTimer.IsEnabled)
            {
                if (dispatcherTimer.IsEnabled)
                    Mouse.OverrideCursor = Cursors.Arrow;
                dragRectangle = null;
                return;
            }

            Mouse.OverrideCursor = Cursors.ScrollAll;
            var (colX, colY) = CheckCollisionWithOtherRectangles(newPosition, dragRectangle);

            if (!colX) Canvas.SetLeft(dragRectangle, newPosition.X - offset.X);

            if (!colY) Canvas.SetTop(dragRectangle, newPosition.Y - offset.Y);
        }

        private void MouseHand(object sender, MouseEventArgs e) => Mouse.OverrideCursor = Cursors.Hand;

        private void MouseArrow(object sender, MouseEventArgs e) => Mouse.OverrideCursor = Cursors.Arrow;

        private bool MouseLeaveDragRectangle(Point mouse)
        {
            var newRectanglePosition = new Rect(Canvas.GetLeft(dragRectangle), Canvas.GetTop(dragRectangle), dragRectangle.Width, dragRectangle.Height);
            var mousePosition = new Rect(mouse.X, mouse.Y, 10, 10);

            return !newRectanglePosition.IntersectsWith(mousePosition);
        }

        private (bool colX, bool colY) CheckCollisionWithOtherRectangles(Point newPosition, Rectangle dragRectangle)
        {
            var collisionX = false;
            var collisionY = false;
            foreach (var r in rectangles)
            {
                if (r == dragRectangle || r.Name.Equals("Exit") && dragRectangle.Name.Equals("J")) continue;

                var thisRectX = new Rect(newPosition.X - offset.X + 6, Canvas.GetTop(dragRectangle) + 6,
                    dragRectangle.Width - 12, dragRectangle.Height - 12);

                var thisRectY = new Rect(Canvas.GetLeft(dragRectangle) + 6, newPosition.Y - offset.Y + 6,
                    dragRectangle.Width - 12, dragRectangle.Height - 12);

                var otherRect = new Rect(Canvas.GetLeft(r), Canvas.GetTop(r), r.Width, r.Height);

                collisionX = collisionX ? collisionX : thisRectX.IntersectsWith(otherRect);

                collisionY = collisionY ? collisionY : thisRectY.IntersectsWith(otherRect);
            }

            return (collisionX, collisionY);

        }

        private void FillArrayWithRectangles()
        {
            rectangles.ForEach(rect => FillArrayWithThisRectangle(
                new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect), rect.Width, rect.Height)
                , rect));
        }

        private void FillArrayWithThisRectangle(Rect rect, Rectangle rectangle)
        {
            var minX = Convert.ToInt32(rect.TopLeft.X / SquareUnit) - 1;
            var minY = Convert.ToInt32(rect.TopLeft.Y / SquareUnit) - 1;

            var maxX = Convert.ToInt32(rect.BottomRight.X / SquareUnit) - 2;
            var maxY = Convert.ToInt32(rect.BottomRight.Y / SquareUnit) - 2;

            //Rearrange the rectangles correctly before running the solution
            Canvas.SetLeft(rectangle, minX * 100 + 100);
            Canvas.SetTop(rectangle, minY * 100 + 100);
            /****************************************************************/

            for (var y = minY; y <= maxY; y++)
            for (var x = minX; x <= maxX; x++)
                position[y, x] = rectangle.Name[0];
        }

        private void ResetPositions()
            => PuzzleSettings.SetRectanglesPosition(rectangles);

        private static void FillArrayWithInitValue(char[,] array, char c)
        {
            for (var y = 0; y < array.GetLength(0); y++)
            for (var x = 0; x < array.GetLength(1); x++)
                array[y, x] = c;
        }

    }


    public static class PuzzleSettings
    {
        private static readonly Dictionary<string, (int x, int y)> rectanglesInitialCoords =
            new Dictionary<string, (int x, int y)>
            {
                //Rectangle ▯ 1x2
                ["V"] = (0, 1),
                ["X"] = (1, 2),
                ["W"] = (3, 1),

                //Square □ 1x1
                ["R"] = (0, 0),
                ["r"] = (3, 0),
                ["k"] = (0, 3),
                ["K"] = (3, 3),

                //Rectangle ▭ 2x1
                ["Y"] = (0, 4),
                ["M"] = (2, 4),

                //Big Square □ 2x2
                ["J"] = (1, 0)
            };

        public static void SetRectanglesPosition(IEnumerable<Rectangle> rectangles)
        {
            foreach (var rectangle in rectangles)
            {
                if (!rectanglesInitialCoords.TryGetValue(rectangle.Name, out var coord)) continue;
                Canvas.SetLeft(rectangle, coord.x * 100 + 100);
                Canvas.SetTop(rectangle, coord.y * 100 + 100);
            }
        }
    }
}