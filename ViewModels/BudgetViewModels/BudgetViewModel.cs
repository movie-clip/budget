using System.Collections.Generic;
using System.Windows.Input;

namespace HomeCharts.ViewModels
{
    public class BudgetViewModel : ViewModelBase
    {
        private ViewModelBase _currentChildView;
        
        public ViewModelBase CurrentChildView
        {
            get => _currentChildView;
            set
            {
                _currentChildView = value;
                OnPropertyChanged(nameof(CurrentChildView));
            }
        }
        
        public ICommand ShowExpensesViewCommand { get; }
        public ICommand ShowIncomeViewCommand { get; }
        
        public BudgetViewModel()
        {
            ShowExpensesViewCommand = new ViewModelCommand(ExecuteShowExpensesViewCommand);
            ShowIncomeViewCommand = new ViewModelCommand(ExecuteShowIncomeViewCommand);
            
            ExecuteShowExpensesViewCommand(null);
        }
        
        private void ExecuteShowExpensesViewCommand(object obj)
        {
            CurrentChildView = new ExpensesViewModel();
        }
        
        private void ExecuteShowIncomeViewCommand(object obj)
        {
            CurrentChildView = new IncomeViewModel();
        }
    }
}