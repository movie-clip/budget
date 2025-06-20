using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using HomeCharts.ViewModels;

namespace HomeCharts.Models.Budget
{
    public class CategoryFilterItemModel : ViewModelBase
    {
        public string Id { get; set; }
        public DateTime Date { get; set; }
        public string Vendor { get; set; }
        public float Expenses { get; set; }

        public IEnumerable<BudgetCategory> Categories
            => Enum.GetValues(typeof(BudgetCategory))
                .Cast<BudgetCategory>();
        
        private BudgetCategory _selectedCategory;
        public BudgetCategory SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (_selectedCategory == value) return;
                _selectedCategory = value;
                OnPropertyChanged();
            }
        }
        
        public string ActionText => Expenses == 0 ? "Remove" : "Apply";
        
        public CategoryFilterItemModel()
        {
            SelectedCategory = Categories.First();
        }
    }
    
    public class RelayCommand<T> : ICommand
    {
        readonly Action<T> _execute;
        readonly Func<T,bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (_ => true);
        }

        public bool CanExecute(object parameter) => _canExecute.Invoke((T)parameter);

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter) => _execute((T)parameter);
    }
}