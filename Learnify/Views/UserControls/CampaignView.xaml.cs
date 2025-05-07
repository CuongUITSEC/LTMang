using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Learnify.Views
{

    public partial class CampaignView : UserControl
    {
        private DateTime targetDate;
        private DispatcherTimer timer;
        public CampaignView()
        {
            InitializeComponent();
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void AddEvent_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventNameTextBox.Text) || !EventDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Vui lòng nhập tên chiến dịch và chọn ngày.");
                return;
            }

            targetDate = EventDatePicker.SelectedDate.Value;
            EventTitle.Text = EventNameTextBox.Text;
            EventDateText.Text = $"Ngày: {targetDate:dd/MM/yyyy}";

            InputPanel.Visibility = Visibility.Collapsed;
            CountdownPanel.Visibility = Visibility.Visible;
            timer.Start();
        }

        private void CancelEvent_Click(object sender, RoutedEventArgs e)
        {
            EventNameTextBox.Clear();
            EventDatePicker.SelectedDate = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            var diff = targetDate - now;

            if (diff.TotalSeconds <= 0)
            {
                DaysText.Text = HoursText.Text = MinutesText.Text = SecondsText.Text = "00";
                timer.Stop();
                return;
            }

            DaysText.Text = ((int)diff.TotalDays).ToString("00");
            HoursText.Text = diff.Hours.ToString("00");
            MinutesText.Text = diff.Minutes.ToString("00");
            SecondsText.Text = diff.Seconds.ToString("00");
        }

        private void ShowInput_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            CountdownPanel.Visibility = Visibility.Collapsed;
            InputPanel.Visibility = Visibility.Visible;
            EventNameTextBox.Clear();
            EventDatePicker.SelectedDate = null;
        }

        private void ClearCountdown_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
            CountdownPanel.Visibility = Visibility.Collapsed;
            InputPanel.Visibility = Visibility.Visible;
            EventNameTextBox.Clear();
            EventDatePicker.SelectedDate = null;
        }
    }
}