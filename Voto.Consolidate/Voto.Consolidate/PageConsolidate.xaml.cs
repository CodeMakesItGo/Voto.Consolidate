using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using File.Consolidate;

namespace Voto.Consolidate
{
    /// <summary>
    /// Interaction logic for PageConsolidate.xaml
    /// </summary>
    public partial class PageConsolidate : Page
    {
        private int _lastChildIndex = 0;
        private List<double> bpsList;
        private const int _maxColumns = 100;
        private double _maxBps = Double.MinValue;
        private double _movingAverage = 0;

        public PageConsolidate()
        {
            InitializeComponent();

            bpsList = new List<double>();
        }

        delegate void UpdateProgessDelegate(FileBase.ProgressReport report);

        public void ProgressReport(FileBase.ProgressReport report)
        {
            UpdateProgress(report);
        }

        public void UpdateProgress(FileBase.ProgressReport report)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new UpdateProgessDelegate(UpdateProgress), report);
                return;
            }

            labelCount.Content = report.Completed.ToString();
            labelSuccess.Content = report.Successful.ToString();
            labelFailed.Content = report.Failed.ToString();
            labelSkipped.Content = report.Skipped.ToString();
            labelSpeed.Content = report.Bps.ToString("N1");
            labelTotalBytes.Content = report.TotalBytes.ToString("N");
            labelComplete.Content = report.PercentComplete.ToString("P");

            textBlockText.Text = report.FileName;
            //textBlockText2.Text = report.Report;
            ProgressBarComplete.Maximum = report.Total;
            ProgressBarComplete.Minimum = 0;
            ProgressBarComplete.Value = report.Completed;

            ProgressBarRunning.IsIndeterminate = true;

            UpdateCanvas(report);
        }

        private void UpdateCanvas(FileBase.ProgressReport report)
        {
            var height = CanvasMain.ActualHeight;
            var width = CanvasMain.ActualWidth;
            var mbps = report.Bps / 1000000.0;

            if (double.IsPositiveInfinity(report.Bps) || double.IsNegativeInfinity(report.Bps) ||
                double.IsNaN(report.Bps) || Math.Abs(report.Bps) < 0.001) return;

            _movingAverage = .1 * mbps + (1.0 - .1) * _movingAverage;

            bpsList.Add(mbps);

            while (bpsList.Count > _maxColumns)
            {
                bpsList.RemoveAt(0);
            }

            var stepX = width / bpsList.Count;
            var startX = 0.0;

            //Remove the previous bps values but not the background lines
            CanvasMain.Children.RemoveRange(_lastChildIndex, CanvasMain.Children.Count - _lastChildIndex);


            foreach (var bps in bpsList)
            {
                _maxBps = Math.Max(bps, _maxBps);

                var percent = 1.0 - bps / _maxBps;

                var startY = height * percent;

                AddLine(new Point(startX + (stepX / 2.0), height), new Point(startX + (stepX / 2.0), startY),
                    Brushes.MediumPurple, stepX, .33);

                startX += stepX;
            }

            var p = 1.0 - _movingAverage / _maxBps;

            var y = height * p;

            AddLine(new Point(0, y), new Point(width, y), Brushes.Purple, 1.0, .75);

            AddText(width / 2.0, y, $"{_movingAverage:N1}Mb\\s", Colors.DimGray);
        }

        private void CreateCanvas()
        {
            const int countX = 10;
            const int countY = 10;

            CanvasMain.Children.Clear();

            var height = CanvasMain.ActualHeight;
            var width = CanvasMain.ActualWidth;

            var stepX = width / countX;
            var stepY = height / countY;

            var startX = 0.0;
            var startY = 0.0;


            for (var i = 0; i <= countX; ++i)
            {
                AddLine(new Point(startX, 0), new Point(startX, height), Brushes.LightGray, 1.0);
                startX += stepX;
            }

            for (var i = 0; i <= countY; ++i)
            {
                AddLine(new Point(0, startY), new Point(width, startY), Brushes.LightGray, 1.0);
                startY += stepY;
            }

            _lastChildIndex = CanvasMain.Children.Count;
        }

        private void AddLine(Point p1, Point p2, Brush Fill, double thickness = 1, double opacity = 1.0,
            PenLineCap cap = PenLineCap.Flat)
        {
            CanvasMain.Children.Add(new Line()
            {
                X1 = p1.X,
                X2 = p2.X,
                Y1 = p1.Y,
                Y2 = p2.Y,
                StrokeThickness = thickness,
                StrokeDashCap = cap,
                StrokeEndLineCap = cap,
                StrokeStartLineCap = cap,
                Stroke = Fill,
                StrokeLineJoin = PenLineJoin.Round,
                Opacity = opacity
            });
        }

        private void AddText(double x, double y, string text, Color color)
        {
            var textBlock = new TextBlock();

            textBlock.Text = text;

            textBlock.Foreground = new SolidColorBrush(color);

            Canvas.SetLeft(textBlock, x);

            Canvas.SetTop(textBlock, y);

            CanvasMain.Children.Add(textBlock);
        }


        public void Complete()
        {
            textBlockText.Text = "Finished!";
            ProgressBarRunning.IsIndeterminate = false;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CreateCanvas();
        }
    }
}