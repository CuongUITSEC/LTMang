using Learnify.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Learnify
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            StudyTimeService.LoadFromFile();

            // Tự động fix username cho toàn bộ user khi khởi động app
            var firebaseService = new FirebaseService();
            await firebaseService.FixMissingUsernamesAsync();
        }
    }
}
