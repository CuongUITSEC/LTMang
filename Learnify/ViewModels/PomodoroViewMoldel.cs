using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Learnify.Commands;

namespace Learnify.ViewModels
{
    public class PomodoroViewModel : ViewModelBase
    {
        private string _timeDisplay;
        private bool _isRunning;
        private bool _isBreakTime;
        private TimeSpan _remainingTime;
        private double _progress;

        private readonly TimeSpan _pomodoroTime = TimeSpan.FromMinutes(25);
        private readonly TimeSpan _breakTime = TimeSpan.FromMinutes(5);
        private readonly DispatcherTimer _timer;

        public PomodoroViewModel()
        {
            StartCommand = new RelayCommand(StartTimer, () => !_isRunning);
            PauseCommand = new RelayCommand(PauseTimer, () => _isRunning);

            _remainingTime = _pomodoroTime;
            UpdateTimeDisplay();
            Progress = 1; // 1 = đầy, 0 = hết

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += Timer_Tick;
        }

        public string TimeDisplay
        {
            get => _timeDisplay;
            private set { _timeDisplay = value; OnPropertyChanged(nameof(TimeDisplay)); }
        }

        public double Progress
        {
            get => _progress;
            private set
            {
                _progress = Math.Max(0, Math.Min(1, value));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(PomodoroArc));
            }
        }

        public Geometry PomodoroArc
        {
            get
            {
                if (_isBreakTime) return Geometry.Empty;

                // Tính góc của cung di chuyển từ -90 độ tới 60 độ
                double startAngle = -90 + (150 * (1 - Progress)); // Di chuyển từ -90 độ tới 60 độ
                double endAngle = 60; // Điểm kết thúc cố định

                return CreateArc(startAngle, 150 * Progress); // Cung di chuyển từ -90 độ tới 60 độ
            }
        }

        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }

        private void StartTimer()
        {
            if (_isRunning) return;
            _timer.Start();
            _isRunning = true;
            CommandManager.InvalidateRequerySuggested();
        }

        private void PauseTimer()
        {
            if (!_isRunning) return;
            _timer.Stop();
            _isRunning = false;
            CommandManager.InvalidateRequerySuggested();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime -= TimeSpan.FromSeconds(100);
            }
            else if (!_isBreakTime)
            {
                _isBreakTime = true;
                _remainingTime = _breakTime;
            }
            else
            {
                _timer.Stop();
                _isRunning = false;
                _isBreakTime = false;
                _remainingTime = _pomodoroTime;
            }

            UpdateTimeDisplay();

            double total = _isBreakTime ? _breakTime.TotalSeconds : _pomodoroTime.TotalSeconds;
            Progress = _remainingTime.TotalSeconds / total;
        }

        private void UpdateTimeDisplay()
        {
            TimeDisplay = _remainingTime.ToString(@"mm\:ss");
        }

        private Geometry CreateArc(double baseAngle, double spanDeg)
        {
            double radius = 100; // Độ dài bán kính (hoặc kích thước)
            var center = new Point(radius, radius);
            double toRad = Math.PI / 180;

            double start = baseAngle;
            double end = baseAngle + spanDeg;

            var startPt = new Point(
                center.X + radius * Math.Cos(start * toRad),
                center.Y + radius * Math.Sin(start * toRad));
            var endPt = new Point(
                center.X + radius * Math.Cos(end * toRad),
                center.Y + radius * Math.Sin(end * toRad));

            bool isLargeArc = spanDeg > 180;
            var figure = new PathFigure { StartPoint = center };
            figure.Segments.Add(new LineSegment(startPt, true));
            figure.Segments.Add(new ArcSegment(endPt,
                                               new Size(radius, radius),
                                               0,
                                               isLargeArc,
                                               SweepDirection.Clockwise,
                                               true));
            figure.Segments.Add(new LineSegment(center, true));

            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            return geo;
        }
    }
}
