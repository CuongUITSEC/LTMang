using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;
using Learnify.Commands;
using Learnify.Services;

namespace Learnify.ViewModels
{
    public class TimerModeViewModel : ViewModelBase
    {
        private int _hours;
        public int Hours
        {
            get => _hours;
            private set
            {
                _hours = value;
                OnPropertyChanged(nameof(Hours));
            }
        }

        private int _minutes;
        public int Minutes
        {
            get => _minutes;
            private set
            {
                _minutes = value;
                OnPropertyChanged(nameof(Minutes));
            }
        }

        private int _seconds;
        public int Seconds
        {
            get => _seconds;
            private set
            {
                _seconds = value;
                OnPropertyChanged(nameof(Seconds));
            }
        }

        private readonly Timer _timer;
        private bool _isRunning;
        private bool _isPaused;
        private readonly FirebaseService _firebaseService;
        private string _currentUserId;
        private string _currentUsername;

        // Command để bắt đầu
        public ICommand StartCommand { get; }

        // Command cho nút thứ hai: tạm dừng hoặc đặt lại
        public ICommand PauseOrResetCommand { get; }

        public TimerModeViewModel()
        {
            _firebaseService = new FirebaseService();
            
            // Khởi tạo Timer, 1s tick
            _timer = new Timer(1000) { AutoReset = true };
            _timer.Elapsed += OnTimerElapsed;

            // RelayCommand với CanExecute
            StartCommand = new RelayCommand(StartTimer, () => !_isRunning);
            PauseOrResetCommand = new RelayCommand(PauseOrResetTimer, () => _isRunning || _isPaused);

            // Khởi tạo trạng thái ban đầu
            _isRunning = false;
            _isPaused = false;
            ResetValues();

            // Kiểm tra xác thực và lấy thông tin người dùng
            _currentUserId = AuthService.GetUserId();
            LoadUsernameAsync(_currentUserId);
        }

        private async void LoadUsernameAsync(string userId)
        {
            _currentUsername = await _firebaseService.GetUsernameAsync(userId);
        }

        // Nội dung của nút thứ hai, binding Content
        public string PauseOrResetButtonText =>
            _isPaused ? "ĐẶT LẠI" : "TẠM DỪNG";

        private void StartTimer()
        {
            if (_isRunning) return;

            _timer.Start();
            _isRunning = true;
            _isPaused = false;

            // Cập nhật nút và trạng thái Command
            OnPropertyChanged(nameof(PauseOrResetButtonText));
            RaiseCanExecuteChanged();
        }

        private async void PauseOrResetTimer()
        {
            if (_isRunning)
            {
                // Tạm dừng
                _timer.Stop();
                _isRunning = false;
                _isPaused = true;
            }
            else if (_isPaused)
            {
                // Hiển thị thông báo số giờ đã học
                string timeStudied = $"{Hours:D2}:{Minutes:D2}:{Seconds:D2}";
                var sessionTime = new TimeSpan(Hours, Minutes, Seconds);

                try
                {
                    // Lưu thời gian học lên Firebase
                    bool success = await _firebaseService.SaveStudyTimeAsync(_currentUserId, sessionTime);
                    
                    if (success)
                    {
                        // Lấy thời gian học tổng cộng
                        var totalStudyTime = await _firebaseService.GetStudyTimeAsync(_currentUserId);
                        string message = $"Chúc mừng {_currentUsername}!\n" +
                                       $"Bạn đã học trong {timeStudied}.\n" +
                                       $"Tổng thời gian học: {totalStudyTime.Hours} giờ {totalStudyTime.Minutes} phút";
                        
                        MessageBox.Show(message, "Thời gian học", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Không thể lưu thời gian học. Vui lòng kiểm tra kết nối mạng và thử lại.", 
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Đã xảy ra lỗi: {ex.Message}", 
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // Đặt lại đồng hồ
                _timer.Stop();
                _isRunning = false;
                _isPaused = false;
                ResetValues();
            }

            // Cập nhật nút và trạng thái Command
            OnPropertyChanged(nameof(PauseOrResetButtonText));
            RaiseCanExecuteChanged();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Seconds++;
            if (Seconds >= 60)
            {
                Seconds = 0;
                Minutes++;
            }
            if (Minutes >= 60)
            {
                Minutes = 0;
                Hours++;
            }
        }

        private void ResetValues()
        {
            Hours = 0;
            Minutes = 0;
            Seconds = 0;
        }

        private void RaiseCanExecuteChanged()
        {
            (StartCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PauseOrResetCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
