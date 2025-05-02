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

        /// <summary>1→đầy, 0→rỗng</summary>
        public double Progress
        {
            get => _progress;
            private set
            {
                _progress = Math.Max(0, Math.Min(1, value));
                OnPropertyChanged(nameof(Progress));
                OnPropertyChanged(nameof(PomodoroArc));
                OnPropertyChanged(nameof(BreakArc));
            }
        }

        /// <summary>Cung Pomodoro: 150° từ -90° → 60°</summary>
        public Geometry PomodoroArc
        {
            get
            {
                // nếu đang Break, ẩn Pomodoro
                if (_isBreakTime) return Geometry.Empty;
                // span giảm dần: 150° × Progress
                return CreateArc(-90, 150 * Progress);
            }
        }

        /// <summary>Cung Break: 30° từ 60° → 90°</summary>
        public Geometry BreakArc
        {
            get
            {
                if (!_isBreakTime) return Geometry.Empty;
                return CreateArc(60, 30 * Progress);
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
            // Giảm 1 giây
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime -= TimeSpan.FromSeconds(500);
            }
            else if (!_isBreakTime)
            {
                // chuyển qua Break
                _isBreakTime = true;
                _remainingTime = _breakTime;
            }
            else
            {
                // reset Pomodoro
                _timer.Stop();
                _isRunning = false;
                _isBreakTime = false;
                _remainingTime = _pomodoroTime;
            }

            UpdateTimeDisplay();

            // Tính Progress = remaining / total
            double total = _isBreakTime ? _breakTime.TotalSeconds : _pomodoroTime.TotalSeconds;
            Progress = _remainingTime.TotalSeconds / total;
        }

        private void UpdateTimeDisplay()
        {
            TimeDisplay = _remainingTime.ToString(@"mm\:ss");
        }

        /// <summary>
        /// Tạo PathGeometry cho cung:
        /// - baseAngle: góc bắt đầu,
        /// - spanDeg: độ dài cung theo chiều kim đồng hồ.
        /// </summary>
        private Geometry CreateArc(double baseAngle, double spanDeg)
        {
            double radius = 100;
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
