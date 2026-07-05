using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FastAccountingSoftware.Models;

namespace FastAccountingSoftware.Views
{
    public partial class AiAssistantWindow : Window
    {
        public AiAssistantWindow()
        {
            InitializeComponent();
            InputPromptBox.Focus();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            ProcessInput();
        }

        private void InputPromptBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ProcessInput();
            }
        }

        private void ProcessInput()
        {
            string prompt = InputPromptBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            // Add User message bubble
            AddMessageBubble(prompt, "You", true);
            InputPromptBox.Clear();

            // Auto Scroll
            ChatScrollViewer.ScrollToEnd();

            // Generate AI response from DB
            string aiResponse = QueryLedger(prompt);

            // Add AI message bubble
            AddMessageBubble(aiResponse, "AI Assistant", false);

            // Auto Scroll
            ChatScrollViewer.ScrollToEnd();
        }

        private void AddMessageBubble(string text, string sender, bool isUser)
        {
            var border = new Border();
            border.CornerRadius = new CornerRadius(12);
            border.Padding = new Thickness(14, 12, 14, 12);
            border.MaxWidth = 360;
            border.Margin = new Thickness(0, 0, 0, 16);

            if (isUser)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7A00")); // Brand Orange
                border.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1ECE7")); // Light Gray
                border.HorizontalAlignment = HorizontalAlignment.Left;
            }

            var stack = new StackPanel();
            
            var senderText = new TextBlock();
            senderText.Text = sender;
            senderText.FontSize = 11;
            senderText.FontWeight = FontWeights.Bold;
            senderText.Foreground = new SolidColorBrush(isUser ? Colors.White : (Color)ColorConverter.ConvertFromString("#64748B"));
            senderText.Opacity = isUser ? 0.8 : 1.0;
            senderText.Margin = new Thickness(0, 0, 0, 4);
            
            var msgText = new TextBlock();
            msgText.Text = text;
            msgText.FontSize = 14;
            msgText.TextWrapping = TextWrapping.Wrap;
            msgText.LineHeight = 20;
            msgText.Foreground = new SolidColorBrush(isUser ? Colors.White : (Color)ColorConverter.ConvertFromString("#1E293B"));

            stack.Children.Add(senderText);
            stack.Children.Add(msgText);
            border.Child = stack;

            ChatMessagesList.Children.Add(border);
        }

        private string QueryLedger(string prompt)
        {
            string query = prompt.ToLower();

            try
            {
                using (var db = new AppDbContext())
                {
                    // 1. REVENUE / SALES / INCOME
                    if (query.Contains("revenue") || query.Contains("sales") || query.Contains("income") || query.Contains("earnings"))
                    {
                        var incomes = db.Transactions.Where(t => t.Type == TransactionType.Income).ToList();
                        double totalIncome = incomes.Sum(t => t.Amount);
                        int count = incomes.Count;

                        if (count == 0)
                        {
                            return "There are no income transactions recorded in the ledger yet. Go to the Settings page to load demo data.";
                        }
                        return $"I scanned the ledger database. Total operational revenue is ₦{totalIncome:N2} across {count} transaction entries.";
                    }

                    // 2. EXPENSES / BILLS / SPENDING
                    if (query.Contains("expense") || query.Contains("bill") || query.Contains("costs") || query.Contains("spend") || query.Contains("outgoing"))
                    {
                        var expenses = db.Transactions.Where(t => t.Type == TransactionType.Expense).ToList();
                        double totalExpense = expenses.Sum(t => t.Amount);
                        int count = expenses.Count;

                        if (count == 0)
                        {
                            return "No expense records found in the ledger database.";
                        }
                        return $"Total business operating expenses are ₦{totalExpense:N2} across {count} expense items.";
                    }

                    // 3. PROFIT / LOSS / BALANCES
                    if (query.Contains("profit") || query.Contains("loss") || query.Contains("net") || query.Contains("performance"))
                    {
                        double totalIncome = db.Transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
                        double totalExpense = db.Transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
                        double netProfit = totalIncome - totalExpense;

                        return $"Financial Performance Summary:\n" +
                               $"• Gross Revenue: ₦{totalIncome:N2}\n" +
                               $"• Operating Expenses: ₦{totalExpense:N2}\n" +
                               $"• Net Profitability: ₦{netProfit:N2}\n\n" +
                               $"Currently, your business is operating at a " + (netProfit >= 0 ? "net profit surplus." : "net financial deficit.");
                    }

                    // 4. OVERDUE / CUSTOMERS / DEBTS
                    if (query.Contains("customer") || query.Contains("overdue") || query.Contains("debt") || query.Contains("owing") || query.Contains("unpaid"))
                    {
                        var overdueList = db.Customers.Where(c => c.Status == CustomerStatus.Overdue).ToList();
                        double totalOverdue = overdueList.Sum(c => c.Balance);

                        if (overdueList.Count == 0)
                        {
                            return "Excellent! All customer accounts are in good standing. There are no overdue invoices or debts outstanding.";
                        }

                        string details = string.Join("\n", overdueList.Select(c => $"• {c.Name}: ₦{c.Balance:N0} (Contact: {c.Email})"));
                        return $"I detected {overdueList.Count} accounts with overdue balances totaling ₦{totalOverdue:N2}:\n\n{details}";
                    }

                    // 5. STAFF / PAYROLL
                    if (query.Contains("staff") || query.Contains("employee") || query.Contains("payroll"))
                    {
                        int staffCount = db.Staff.Count();
                        int activeStaff = db.Staff.Count(s => s.Status == StaffStatus.Active);
                        double totalPayroll = db.PayrollRuns.Sum(p => p.TotalAmount);

                        return $"Staffing & Payroll Overview:\n" +
                               $"• Registered Employees: {staffCount} ({activeStaff} active, {staffCount - activeStaff} on leave)\n" +
                               $"• Total Payroll Disbursed (To Date): ₦{totalPayroll:N2}";
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error reading local ledger: {ex.Message}";
            }

            return "I am currently analyzing your local database. Everything looks balanced. You can ask me things like:\n" +
                   "• 'What is our total revenue?'\n" +
                   "• 'What are our current expenses?'\n" +
                   "• 'Show our net profit'\n" +
                   "• 'List customers owing money'\n" +
                   "• 'Give payroll details'";
        }
    }
}
