using System;
using System.Collections.Generic;
using HomeCharts.Models;
using HomeCharts.Models.Budget;
using HomeCharts.Repositories;

namespace HomeCharts.Services
{
    public class AccountService
    {
        private static AccountService _instance;

        public static AccountService Instance
        {
            get
            {
                if (_instance == null)
                {
                    return new AccountService();
                }

                return _instance;
            }
        }

        public AccountService()
        {
            _instance = this;
            _accountRepository = new AccountDBRepository();
            _account = new AccountModel();
        }

        private IAccountRepository _accountRepository;
        private AccountModel _account;

        public void LoadAccountFromRepository()
        {
            var account = _accountRepository.LoadAccount();
            _account.BudgetModel = account;
        }

        public List<BudgetTransaction> GetAccountTransactions()
        {
            return _account.BudgetModel.Transactions;
        }
        
        public DateTime GetAccountFirstTransactionDate()
        {
            return _account.BudgetModel.FirstDate;
        }
        
        public DateTime GetAccountLastTransactionDate()
        {
            return _account.BudgetModel.LastDate;
        }
        
        public void SaveAccountToRepository()
        {
            _accountRepository.SaveAccount(_account.BudgetModel.Transactions);
        }
        
        public void RemoveAccountTransactions()
        {
            _account.BudgetModel.Transactions.Clear();
            _accountRepository.RemoveAllTransactions();
        }

        public void SetAccountTransactions(BudgetModel budget)
        {
            _account.BudgetModel = budget;
        }
    }
}