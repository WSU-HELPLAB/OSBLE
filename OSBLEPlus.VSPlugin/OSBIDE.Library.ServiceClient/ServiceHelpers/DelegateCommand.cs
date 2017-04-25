using System;
using System.Windows.Input;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged = delegate { };
        private readonly Func<object, bool> _canExecute;
        private readonly Action<object> _executeAction;
        private bool _canExecuteCache;

        public DelegateCommand(Action<object> executeAction, Func<object, bool> canExecute)
        {
            _executeAction = executeAction;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            var temp = _canExecute(parameter);
            if (_canExecuteCache != temp)
            {
                _canExecuteCache = temp;
                if (CanExecuteChanged != null)
                {
                    CanExecuteChanged(this, EventArgs.Empty);
                }
            }
            return _canExecuteCache;
        }

        public void Execute(object parameter)
        {
            _executeAction(parameter);
        }
    }
}
