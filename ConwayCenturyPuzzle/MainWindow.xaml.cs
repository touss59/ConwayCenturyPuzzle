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
using System.Windows.Documents;
using System.Text;

namespace ConwayCenturyPuzzle
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Rectangle dragObject = null;
        private Point offset;
        private readonly List<Rectangle> rectangles;
        private readonly DispatcherTimer dispatcherTimer;
        private readonly char[,] position = new char[5, 4];
        private int index = 0;
        private List<(char shapeToMove, char direction)> response;

        public object JsonConvert { get; private set; }

        public MainWindow()
        {
            InitializeComponent();
            rectangles = canvasMain.Children.OfType<UIElement>()
                .Where(c => c.GetType().Name == nameof(Rectangle))
                .Select(c => c as Rectangle)
                .ToList();

            var fileName = System.IO.Path.Combine(Environment.CurrentDirectory, "saveState.txt");

            if (File.Exists(fileName))
            {
                using FileStream fs = File.OpenRead(fileName);
                using var sr = new StreamReader(fs);

                string[] line = sr.ReadLine().Split(';');

                foreach (var l in line)
                {
                    (string name, double left, double top) = (l.Split(',')[0], Convert.ToInt32(l.Split(',')[1]), Convert.ToInt32(l.Split(',')[2]));
                    var rectangle = rectangles.Where(r => r.Name == name).FirstOrDefault();
                    Canvas.SetLeft(rectangle, left);
                    Canvas.SetTop(rectangle, top);
                }

            }


            dispatcherTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 100)
            };

            dispatcherTimer.Tick += new EventHandler(ShowSolution);
        }

        //Methods links to solution
        private void ShowSolution(object sender, EventArgs e)
        {
            if (index < response.Count)
            {
                var (shapeToMove, direction) = response[index];
                Rectangle shape = rectangles.Where(r => r.Name == shapeToMove.ToString()).FirstOrDefault();

                switch (direction)
                {
                    case 'L':
                        Canvas.SetLeft(shape, Canvas.GetLeft(shape) - 100);
                        break;
                    case 'U':
                        Canvas.SetTop(shape, Canvas.GetTop(shape) - 100);
                        break;
                    case 'R':
                        Canvas.SetLeft(shape, Canvas.GetLeft(shape) + 100);
                        break;
                    case 'D':
                        Canvas.SetTop(shape, Canvas.GetTop(shape) + 100);
                        break;
                    default:
                        Console.WriteLine("Default case");
                        break;
                }
                index++;
            }
            else
            {
                dispatcherTimer.Stop();
            }
        }

        public void Window_Closing(object sender, CancelEventArgs e)
        {
            string rtfFile = System.IO.Path.Combine(Environment.CurrentDirectory, "saveState.txt");
            FileStream fileStream = File.Create(rtfFile);
            using var sr = new StreamWriter(fileStream);
            var state = string.Join(';', rectangles.Where(r => !string.IsNullOrEmpty(r.Name) && r.Name != "Exit").Select(r => r.Name + ',' + Math.Round(Canvas.GetLeft(r)).ToString() + ',' + Math.Round(Canvas.GetTop(r)).ToString()));
            sr.Write(state);

        }

        public void ExecuteSolution(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Enter))
            {
                for (var y = 0; y < position.GetLength(0); y++)
                {
                    for (var x = 0; x < position.GetLength(1); x++)
                    {
                        position[y, x] = 'A';
                    }
                }
                index = 0;
                GetPositionRectangles();
                response = Solver.ShowMove(position);
                dispatcherTimer.Start();
            } else if (e.Key == Key.R)
            {
                dispatcherTimer.Stop();
                ResetPositions();
            }
        }

        private void GetPositionRectangles()
        {

            foreach (var rectangle in rectangles)
            {
                Rect rect = new Rect(Canvas.GetLeft(rectangle), Canvas.GetTop(rectangle), rectangle.Width, rectangle.Height);
                GetPositionRectangle(rect, rectangle.Name);
            }
        }

        private void GetPositionRectangle(Rect rect, string Name)
        {
            int minX = Convert.ToInt32(rect.TopLeft.X / 100) - 1;
            int minY = Convert.ToInt32(rect.TopLeft.Y / 100) - 1;

            int maxX = Convert.ToInt32(rect.BottomRight.X / 100) - 2;
            int maxY = Convert.ToInt32(rect.BottomRight.Y / 100) - 2;

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    position[y, x] = Name[0];
                }
            }
        }
        /*********************************************************************/



        private void UserControlPreviewMouseDown(object sender, MouseEventArgs e)
        {
            this.dragObject = sender as Rectangle;
            this.offset = e.GetPosition(this.canvasMain);
            this.offset.Y -= Canvas.GetTop(this.dragObject);
            this.offset.X -= Canvas.GetLeft(this.dragObject);
            this.canvasMain.CaptureMouse();

        }

        private void CanvasMainMouseUp(object sender, MouseEventArgs e)
        {
            this.dragObject = null;
            this.canvasMain.ReleaseMouseCapture();
        }




        //Methods links with drag and drop shapes
        private void CanvasMainMouseMove(object sender, MouseEventArgs e)
        {
            if (this.dragObject == null)
            {
                return;
            }

            var newPosition = e.GetPosition(sender as IInputElement);

            if (!checkMouseOnDragObject(newPosition)) {
                this.dragObject = null;
                return;
            }



            (bool colX, bool colY) = CheckCollisionWithOtherRectangles(newPosition, this.dragObject);

            if (!colX)
            {
                Canvas.SetLeft(this.dragObject, newPosition.X - this.offset.X);
            }

            if (!colY)
            {
                Canvas.SetTop(this.dragObject, newPosition.Y - this.offset.Y);
            }
        }

        private bool checkMouseOnDragObject(Point mouse)
        {
            Rect dragO = new Rect(Canvas.GetLeft(dragObject), Canvas.GetTop(dragObject), dragObject.Width, dragObject.Height);
            Rect m = new Rect(mouse.X, mouse.Y, 10, 10);

            return dragO.IntersectsWith(m);
        }

        private (bool colX, bool colY) CheckCollisionWithOtherRectangles(Point newPosition, Rectangle dragObject)
        {
            bool collisionY = false;
            bool collisionX = false;
            foreach (var r in rectangles)
            {
                if (r != dragObject && !(r.Name == "Exit" && dragObject.Name == "J"))
                {
                    Rect thisRectX = new Rect(newPosition.X - this.offset.X + 6, Canvas.GetTop(dragObject) + 6, dragObject.Width - 12, dragObject.Height - 12);
                    Rect thisRectY = new Rect(Canvas.GetLeft(dragObject) + 6, newPosition.Y - this.offset.Y + 6, dragObject.Width - 12, dragObject.Height - 12);
                    Rect otherRect = new Rect(Canvas.GetLeft(r), Canvas.GetTop(r), r.Width, r.Height);

                    collisionX = collisionX == true ? collisionX : thisRectX.IntersectsWith(otherRect);

                    collisionY = collisionY == true ? collisionY : thisRectY.IntersectsWith(otherRect);

                }

            }

            return (collisionX, collisionY);

        }

        private void ResetPositions()
        {
            Canvas.SetLeft(V, 100);
            Canvas.SetTop(V, 200);

            Canvas.SetLeft(X, 200);
            Canvas.SetTop(X, 300);

            Canvas.SetLeft(W, 400);
            Canvas.SetTop(W, 200);

            Canvas.SetLeft(R, 100);
            Canvas.SetTop(R, 100);

            Canvas.SetLeft(r, 400);
            Canvas.SetTop(r, 100);

            Canvas.SetLeft(k, 100);
            Canvas.SetTop(k, 400);

            Canvas.SetLeft(K, 400);
            Canvas.SetTop(K, 400);

            Canvas.SetLeft(M, 300);
            Canvas.SetTop(M, 500);

            Canvas.SetLeft(Y, 100);
            Canvas.SetTop(Y, 500);

            Canvas.SetLeft(J, 200);
            Canvas.SetTop(J, 100);

        }


    }
}
