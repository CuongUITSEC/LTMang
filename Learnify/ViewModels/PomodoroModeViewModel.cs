using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Learnify.Commands;
using Learnify.Services;

namespace Learnify.ViewModels
{
    public class PomodoroModeViewModel : ViewModelBase
    {
        private string _timeDisplay;
        private bool _isRunning;
        private bool _isBreakTime;
        private TimeSpan _remainingTime;
        private double _progress;

        private readonly TimeSpan _pomodoroTime = TimeSpan.FromMinutes(25);
        private readonly TimeSpan _breakTime = TimeSpan.FromMinutes(5);
        private readonly DispatcherTimer _timer;



        public PomodoroModeViewModel()
        {
            StartCommand = new RelayCommand(StartTimer, () => !_isRunning);
            PauseCommand = new RelayCommand(PauseTimer, () => _isRunning);
            ResetCommand = new RelayCommand(ResetTimer);

            _remainingTime = _pomodoroTime;
            UpdateTimeDisplay();
            Progress = 1;

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
                OnPropertyChanged(nameof(BreakArc));
            }
        }
        public Geometry PomodoroArc
        {
            get
            {
                if (_isBreakTime) return Geometry.Empty;  // Vòng Pomodoro chỉ vẽ khi không phải thời gian nghỉ
                double startAngle = -90 + (150 * (1 - Progress));  // Cập nhật góc của Pomodoro
                return CreateArc(startAngle, 150 * Progress);
            }
        }

        public Geometry BreakArc
        {
            get
            {
                if (!_isBreakTime) return Geometry.Empty;  // Vòng Break chỉ vẽ khi đang nghỉ
                double startAngle = 60 + (30 * (1 - Progress));  // Cập nhật góc của Break
                return CreateArc(startAngle, 30 * Progress);
            }
        }



        public ICommand StartCommand { get; }
        public ICommand PauseCommand { get; }
        public ICommand ResetCommand { get; }

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

        private void ResetTimer()
        {
            _timer.Stop();
            _isRunning = false;
            _isBreakTime = false;
            _remainingTime = _pomodoroTime;
            Progress = 1;
            UpdateTimeDisplay();
            CommandManager.InvalidateRequerySuggested();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (_remainingTime.TotalSeconds > 0)
            {
                _remainingTime -= TimeSpan.FromSeconds(100); // Cập nhật mỗi giây
            }
            else if (!_isBreakTime)
            {
                // Khi Pomodoro kết thúc, chuyển sang vòng Break
                _isBreakTime = true;
                _remainingTime = _breakTime;
                Progress = 1; // Đặt lại progress của Break
            }
            else
            {
                // Kết thúc cả Pomodoro và Break
                _timer.Stop();
                _isRunning = false;
                ShowSessionCompletedMessage();

                // Reset trạng thái
                _isBreakTime = false;
                _remainingTime = _pomodoroTime;
                Progress = 1;
                UpdateTimeDisplay();
                CommandManager.InvalidateRequerySuggested();
            }

            // Cập nhật nếu đang chạy
            if (_isRunning)
            {
                UpdateTimeDisplay();

                // Cập nhật tiến trình cho Pomodoro hoặc Break tùy theo thời gian còn lại
                double total = _isBreakTime ? _breakTime.TotalSeconds : _pomodoroTime.TotalSeconds;
                Progress = _remainingTime.TotalSeconds / total;
            }
        }




        private void UpdateTimeDisplay()
        {
            TimeDisplay = _remainingTime.ToString(@"mm\:ss");
        }
        private void ShowSessionCompletedMessage()
        {
            string currentUser = StudyTimeService.GetCurrentUser();
            if (!string.IsNullOrEmpty(currentUser))
            {
                StudyTimeService.AddStudyTime(currentUser, _pomodoroTime);
                MessageBox.Show("Bạn đã hoàn thành 1 phiên Pomodoro!", "Hoàn thành", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Không thể lấy tên người dùng hiện tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }


        private Geometry CreateArc(double startAngle, double spanDeg)
        {
            const double radius = 200;
            var center = new Point(radius, radius);
            double toRad = Math.PI / 180;

            double endAngle = startAngle + spanDeg;

            var startPt = new Point(
                center.X + radius * Math.Cos(startAngle * toRad),
                center.Y + radius * Math.Sin(startAngle * toRad));
            var endPt = new Point(
                center.X + radius * Math.Cos(endAngle * toRad),
                center.Y + radius * Math.Sin(endAngle * toRad));

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