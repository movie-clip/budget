using System.Threading;
using System.Windows;
using System.Windows.Input;
using FontAwesome.Sharp;
using HomeCharts.Models;
using HomeCharts.Repositories;
using HomeCharts.Services;
using HomeCharts.ViewModels.Dashboard;

namespace HomeCharts.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private UserAccountModel _currentUserAccount;
        private IUserRepository _userRepository;
        private ViewModelBase _currentChildView;
        private string _caption;
        private IconChar _icon;

        public UserAccountModel CurrentUserAccount
        {
            get => _currentUserAccount;
            set
            {
                _currentUserAccount = value;
                OnPropertyChanged(nameof(CurrentUserAccount));
            }
        }

        public ViewModelBase CurrentChildView
        {
            get => _currentChildView;
            set
            {
                _currentChildView = value;
                OnPropertyChanged(nameof(CurrentChildView));
            }
        }

        public string Caption
        {
            get => _caption;
            set
            {
                _caption = value;
                OnPropertyChanged(nameof(Caption));
            }
        }

        public IconChar Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                OnPropertyChanged(nameof(Icon));
            }
        }

        public ICommand ShowDashboardViewCommand { get; }
        public ICommand ShowBudgetViewCommand { get; }
        public ICommand ShowImportViewCommand { get; }
        public MainViewModel()
        {
            _userRepository = new UserRepository();
            CurrentUserAccount = new UserAccountModel();

            ShowDashboardViewCommand = new ViewModelCommand(ExecuteShowDashboardViewCommand);
            ShowBudgetViewCommand = new ViewModelCommand(ExecuteShowBudgetViewCommand);
            ShowImportViewCommand = new ViewModelCommand(ExecuteShowImportViewCommand);
            
            AccountService.Instance.LoadAccountFromRepository();
            
            ExecuteShowDashboardViewCommand(null);
        }

        private void ExecuteShowImportViewCommand(object obj)
        {
            CurrentChildView = new ImportExpensesViewModel();
            Caption = "Import";
            Icon = IconChar.FileImport;
        }

        public void ExecuteShowBudgetViewCommand(object obj)
        {
            CurrentChildView = new BudgetViewModel();
            Caption = "Budget";
            Icon = IconChar.UserGroup;
            
        }

        private void ExecuteShowDashboardViewCommand(object obj)
        {
            CurrentChildView = new DashboardViewModel();
            Caption = "Dashboard";
            Icon = IconChar.Home;
        }

        private void LoadCurrentUserData()
        {
            var user = _userRepository.GetByUserName(Thread.CurrentPrincipal.Identity.Name);
            if (user == null)
            {
                MessageBox.Show("Invalid user");
                Application.Current.Shutdown();
                return;
            }

            CurrentUserAccount = new UserAccountModel
            {
                UserName = user.UserName,
                DisplayName = $"Welcome {user.Name} {user.LastName}"
            };
        }
    }
}