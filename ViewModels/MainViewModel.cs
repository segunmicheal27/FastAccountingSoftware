using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastAccountingSoftware.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FastAccountingSoftware.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly AppDbContext _dbContext;

        [ObservableProperty]
        private ObservableCollection<Transaction> transactions = new();

        [ObservableProperty]
        private double totalIncome;

        [ObservableProperty]
        private double totalExpense;

        [ObservableProperty]
        private double balance;

        public string TotalIncomeText => TotalIncome.ToString("C");
        public string TotalExpenseText => TotalExpense.ToString("C");
        public string BalanceText => Balance.ToString("C");

        [ObservableProperty]
        private string newDescription = string.Empty;

        [ObservableProperty]
        private string newAmountText = string.Empty;

        [ObservableProperty]
        private int newTypeIndex = 0; // 0 for Income, 1 for Expense

        [ObservableProperty]
        private DateTimeOffset newDate = DateTimeOffset.Now;

        public MainViewModel()
        {
            _dbContext = new AppDbContext();
        }

        public async Task LoadDataAsync()
        {
            var rawData = await _dbContext.Transactions.ToListAsync();
            var data = rawData.OrderByDescending(t => t.Date).ToList();
            Transactions = new ObservableCollection<Transaction>(data);
            UpdateSummary();
        }

        [RelayCommand]
        private async Task AddTransactionAsync()
        {
            if (string.IsNullOrWhiteSpace(NewDescription) || !double.TryParse(NewAmountText, out double amount) || amount <= 0)
            {
                // Basic validation
                return;
            }

            var transaction = new Transaction
            {
                Description = NewDescription,
                Amount = amount,
                Type = NewTypeIndex == 0 ? TransactionType.Income : TransactionType.Expense,
                Date = NewDate
            };

            _dbContext.Transactions.Add(transaction);
            await _dbContext.SaveChangesAsync();

            // Add to top of the list
            Transactions.Insert(0, transaction);

            // Reset form
            NewDescription = string.Empty;
            NewAmountText = string.Empty;
            NewTypeIndex = 0;
            NewDate = DateTimeOffset.Now;

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            TotalIncome = Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            TotalExpense = Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            Balance = TotalIncome - TotalExpense;

            OnPropertyChanged(nameof(TotalIncomeText));
            OnPropertyChanged(nameof(TotalExpenseText));
            OnPropertyChanged(nameof(BalanceText));
        }
    }
}
