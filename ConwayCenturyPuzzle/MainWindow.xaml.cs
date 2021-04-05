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

namespace ConwayCenturyPuzzle
{
    public partial class MainWindow : Window
    {
        private readonly char[,] position = new char[5, 4];
        private readonly int SquareUnit = 100;
        private Point offset;
        private Rectangle dragRectangle = null;
        private readonly List<Rectangle> rectangles;
        private readonly DispatcherTimer dispatcherTimer;
        private LinkedListNode<(char rectangleToMove, char direction)> move;
        private LinkedList<(char rectangleToMove, char direction)> moves;

        public MainWindow()
        {
            InitializeComponent();
            rectangles = canvasMain.Children.OfType<UIElement>()
                .Where(c => c.GetType().Name == nameof(Rectangle))
                .Select(c => c as Rectangle)
                .ToList();

            PuzzleSettings.SetRectanglesPosition(rectangles);

            var filePathSaveState = System.IO.Path.Combine(Environment.CurrentDirectory, "saveState.txt");

            if (File.Exists(filePathSaveState))
            {
                using var fs = File.OpenRead(filePathSaveState);
                using var sr = new StreamReader(fs);

                string[] lines = sr.ReadLine().Split(';');

                foreach (var line in lines)
                {
                    (string name, double left, double top) = (line.Split(',')[0], Convert.ToInt32(line.Split(',')[1]), Convert.ToInt32(line.Split(',')[2]));
                    var rectangle = rectangles.Where(r => r.Name == name).FirstOrDefault();
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

        public void ExecuteSolution(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FillArrayWithInitValue(position, 'A');
                FillArrayWithRectangles();
                moves = Solver.Solve(position);
                move = moves.Find(moves.First());
                dispatcherTimer.Start();
                return;
            }
            else if (e.Key == Key.R)
            {
                dispatcherTimer.Stop();
                ResetPositions();
                return;
            }
            dispatcherTimer.Stop();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            string rtfFile = System.IO.Path.Combine(Environment.CurrentDirectory, "saveState.txt");
            var fileStream = File.Create(rtfFile);
            using var streamWriter = new StreamWriter(fileStream);
            var state = string
                .Join(';', rectangles
                    .Where(r => r.Name.Length == 1)
                    .Select(r => r.Name + ',' + Math.Round(Canvas.GetLeft(r)).ToString() + ',' + Math.Round(Canvas.GetTop(r)).ToString()));

            streamWriter.Write(state);

        }


        private void ShowSolution(object sender, EventArgs e)
        {
 
            if (move != null)
            {
                var shape = rectangles.Where(r => r.Name == move.Value.rectangleToMove.ToString()).FirstOrDefault();

                switch (move.Value.direction)
                {
                    case 'L':
                        Canvas.SetLeft(shape, Canvas.GetLeft(shape) - SquareUnit);
                        break;
                    case 'U':
                        Canvas.SetTop(shape, Canvas.GetTop(shape) - SquareUnit);
                        break;
                    case 'R':
                        Canvas.SetLeft(shape, Canvas.GetLeft(shape) + SquareUnit);
                        break;
                    case 'D':
                        Canvas.SetTop(shape, Canvas.GetTop(shape) + SquareUnit);
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
            this.dragRectangle = sender as Rectangle;
            if (this.dragRectangle != null)
            {
                this.offset = e.GetPosition(this.canvasMain);
                this.offset.Y -= Canvas.GetTop(this.dragRectangle);
                this.offset.X -= Canvas.GetLeft(this.dragRectangle);
                this.canvasMain.CaptureMouse();
            }
        }

        private void CanvasMainMouseUp(object sender, MouseEventArgs e)
        {
            this.dragRectangle = null;

            this.canvasMain.ReleaseMouseCapture();
        }

        private void CanvasMainMouseMove(object sender, MouseEventArgs e)
        {
            var newPosition = e.GetPosition(sender as IInputElement);

            if (this.dragRectangle == null || MouseLeaveDragRectangle(newPosition) || dispatcherTimer.IsEnabled)
            {
                if(dispatcherTimer.IsEnabled)
                    Mouse.OverrideCursor = Cursors.Arrow;
                this.dragRectangle = null;
                return;
            }
            Mouse.OverrideCursor = Cursors.ScrollAll;
            (bool colX, bool colY) = CheckCollisionWithOtherRectangles(newPosition, this.dragRectangle);

            if (!colX)
            {
                Canvas.SetLeft(this.dragRectangle, newPosition.X - this.offset.X);
            }

            if (!colY)
            {
                Canvas.SetTop(this.dragRectangle, newPosition.Y - this.offset.Y);
            }
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
                if (r != dragRectangle && !(r.Name == "Exit" && dragRectangle.Name == "J"))
                {
                    var thisRectX = new Rect(newPosition.X - this.offset.X + 6, Canvas.GetTop(dragRectangle) + 6, dragRectangle.Width - 12, dragRectangle.Height - 12);
                    var thisRectY = new Rect(Canvas.GetLeft(dragRectangle) + 6, newPosition.Y - this.offset.Y + 6, dragRectangle.Width - 12, dragRectangle.Height - 12);
                    var otherRect = new Rect(Canvas.GetLeft(r), Canvas.GetTop(r), r.Width, r.Height);

                    collisionX = collisionX == true ? collisionX : thisRectX.IntersectsWith(otherRect);

                    collisionY = collisionY == true ? collisionY : thisRectY.IntersectsWith(otherRect);

                }

            }

            return (collisionX, collisionY);

        }

        private void FillArrayWithRectangles()
            => rectangles.ForEach(rect => FillArrayWithThisRectangle(new Rect(Canvas.GetLeft(rect), Canvas.GetTop(rect), rect.Width, rect.Height)
                                              , rect));

        private void FillArrayWithThisRectangle(Rect rect, Rectangle rectangle)
        {
            int minX = Convert.ToInt32(rect.TopLeft.X / SquareUnit) - 1;
            int minY = Convert.ToInt32(rect.TopLeft.Y / SquareUnit) - 1;

            int maxX = Convert.ToInt32(rect.BottomRight.X / SquareUnit) - 2;
            int maxY = Convert.ToInt32(rect.BottomRight.Y / SquareUnit) - 2;

            //Rearrange the rectangles correctly before showing the solution
            Canvas.SetLeft(rectangle, minX * 100 + 100);
            Canvas.SetTop(rectangle, minY * 100 + 100);
            /****************************************************************/

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    position[y, x] = rectangle.Name[0];
                }
            }
        }

        private void ResetPositions()
            => PuzzleSettings.SetRectanglesPosition(rectangles);

        private static void FillArrayWithInitValue(char[,] array, char c)
        {
            for (var y = 0; y < array.GetLength(0); y++)
            {
                for (var x = 0; x < array.GetLength(1); x++)
                {
                    array[y, x] = c;
                }
            }
        }

    }


    public static class PuzzleSettings
    {
        private static readonly Dictionary<string, (int x, int y)> rectanglesInitialCoords = new Dictionary<string, (int x, int y)>
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
                if (rectanglesInitialCoords.TryGetValue(rectangle.Name, out var coord))
                {
                    Canvas.SetLeft(rectangle, coord.x * 100 + 100);
                    Canvas.SetTop(rectangle, coord.y * 100 + 100);
                }
            }
        }
    }
}