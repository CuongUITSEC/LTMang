﻿using System;
using Learnify.Commands;

namespace Learnify.ViewModels.Login
{
    public class StartViewModel : ViewModelBase
    {
        public ViewModelCommand StartCommand { get; set; }
        public ViewModelCommand Sign_InCommand { get; set; }
        public ViewModelCommand Sign_UpCommand { get; set; }
        public ViewModelCommand Forgot_PWCommand { get; set; }

        public START_ViewModel StartVm { get; set; }
        public SIGN_IN_ViewModel Sign_InVm { get; set; }
        public SIGN_UP_ViewModel Sign_UpVm { get; set; }
        public FORGOT_PW_Viewmodel Forgot_PWVm { get; set; }


        private ViewModelBase _currentView;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(nameof(CurrentView));
            }
        }


        public event Action LoginSucceeded;

        public StartViewModel()
        {
            StartVm = new START_ViewModel();
            // Truyền callback khi đăng nhập thành công
            Sign_InVm = new SIGN_IN_ViewModel(OnLoginSuccess);
            Sign_UpVm = new SIGN_UP_ViewModel(OnLoginSuccess);
            Forgot_PWVm = new FORGOT_PW_Viewmodel();

            CurrentView = StartVm;

            StartCommand = new ViewModelCommand(o => { CurrentView = StartVm; });
            Sign_InCommand = new ViewModelCommand(o => { CurrentView = Sign_InVm; });
            Sign_UpCommand = new ViewModelCommand(o => { CurrentView = Sign_UpVm; });
            Forgot_PWCommand = new ViewModelCommand(o => { CurrentView = Forgot_PWVm; });
        }

        private void OnLoginSuccess()
        {
            LoginSucceeded?.Invoke();
        }


    }
}
