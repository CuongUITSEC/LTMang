using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Learnify.Views.UserControls
{
    /// <summary>
    /// Interaction logic for CalendarView.xaml
    /// </summary>
    public partial class CalendarView : UserControl
    {
        private ObservableCollection<string> tasks = new ObservableCollection<string>();

        public CalendarView()
        {
            InitializeComponent();
            TaskList.ItemsSource = tasks;
        }

        private void AddTask_Click(object sender, RoutedEventArgs e)
        {
            string newTask = TaskInput.Text.Trim();
            if (!string.IsNullOrEmpty(newTask))
            {
                tasks.Add(newTask);
                TaskInput.Text = ""; // Clear textbox
            }
        }
    }

}
