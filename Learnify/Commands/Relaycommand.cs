﻿using System;
using System.Windows.Input;

namespace Learnify
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object parameter)
        {
            return true; // Đơn giản ở đây, luôn có thể thực thi
        }

        public void Execute(object parameter)
        {
            _execute();
        }
    }
}
