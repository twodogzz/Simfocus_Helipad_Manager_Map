using System;
using System.Windows.Input;

namespace Simfocus_Helipad_Manager_Map.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Func<object?, bool>? _canExecute;
        private readonly Action<object?> _execute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

        public void Execute(object? parameter) => _execute(parameter);
    }
}