using FastAccountingSoftware.Models;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FastAccountingSoftware.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            LoadHmoProviders();
            LoadBirthdayTemplate();
        }

        private void LoadBirthdayTemplate()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var profile = dbContext.CompanyProfiles.FirstOrDefault();
                    if (profile != null)
                    {
                        BirthdayTemplateBox.Text = profile.BirthdayTemplate;
                        DisableCmsCheck.IsChecked = profile.DisableCms;
                        DisablePosCheck.IsChecked = profile.DisablePos;
                        DisableHrmsCheck.IsChecked = profile.DisableHrms;
                    }
                }
            }
            catch { }
        }

        private void SaveTemplate_Click(object sender, RoutedEventArgs e)
        {
            string template = BirthdayTemplateBox.Text.Trim();
            if (string.IsNullOrEmpty(template))
            {
                CustomMessageBox.Show("Please enter a template message.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var profile = dbContext.CompanyProfiles.FirstOrDefault();
                    if (profile != null)
                    {
                        profile.BirthdayTemplate = template;
                        dbContext.CompanyProfiles.Update(profile);
                        dbContext.SaveChanges();
                        CustomMessageBox.Show("Birthday message template saved successfully!", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to save birthday template: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHmoProviders()
        {
            try
            {
                using (var dbContext = new AppDbContext())
                {
                    HmoList.ItemsSource = dbContext.HmoProviders.ToList();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error loading HMO providers: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDemoDataButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Quick check if demo data already exists
                bool alreadyLoaded;
                using (var checkCtx = new AppDbContext())
                {
                    alreadyLoaded = checkCtx.Customers.Any();
                }

                if (!alreadyLoaded)
                {
                    StatusMessage.Visibility = Visibility.Visible;
                    StatusMessage.Text = "Generating above 1,500 data records, please wait...";
                    StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkBlue);

                    // Create a NEW context inside the async call — never pass a disposed one
                    Dispatcher.InvokeAsync(() =>
                    {
                        using (var dbContext = new AppDbContext())
                        {
                            GenerateAndSaveData(dbContext);
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    StatusMessage.Text = "Demo data is already loaded.";
                    StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.OrangeRed);
                    StatusMessage.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error: {ex.Message}";
                StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                StatusMessage.Visibility = Visibility.Visible;
            }
        }

        private void GenerateAndSaveData(AppDbContext dbContext)
        {
            try
            {
                var rng = new Random(42); // fixed seed for reproducibility

                // ── Helper arrays ────────────────────────────────────────
                string[] firstNames = { "Amara","Chidi","Ngozi","Emeka","Kemi","Yusuf","Fatima","Ibrahim","Chioma","Adaeze",
                    "Babajide","Oluwaseun","Adeola","Tunde","Sola","Bisi","Funmi","Seyi","Tobi","Wale",
                    "James","David","Grace","Sarah","Michael","Daniel","Ruth","Joseph","Mary","Peter",
                    "Emmanuel","Blessing","Faith","Victor","Mercy","Samuel","Esther","Joshua","Deborah","Elijah",
                    "Zainab","Hauwa","Musa","Aminu","Halima","Usman","Aisha","Garba","Safiya","Kabiru" };
                string[] lastNames = { "Okafor","Nwachukwu","Adeyemi","Musa","Johnson","Williams","Adeola","Bello","Osei","Mensah",
                    "Adesanya","Okonkwo","Eze","Chukwu","Nwosu","Ogbonna","Anyanwu","Udeh","Onyeka","Obinna",
                    "Aliyu","Abubakar","Suleiman","Danladi","Umar","Hassan","Ibrahim","Shehu","Yahya","Dabo",
                    "Taylor","Anderson","Thomas","Jackson","White","Harris","Martin","Thompson","Garcia","Martinez",
                    "Obi","Ikenna","Uchenna","Chinonso","Kelechi","Ifeanyi","Chukwuemeka","Uzoma","Nnaemeka","Obiora" };
                string[] companySuffixes = { "Ltd","Plc","Nigeria","Group","Industries","Holdings","Enterprises","Solutions","Services","Global",
                    "Technologies","Logistics","Energy","Finance","Healthcare","Media","Retail","Construction","Consulting","International" };
                string[] bizTypes = { "Foods","Tech","Logistics","Energy","Finance","Healthcare","Media","Retail","Construction","Consulting",
                    "Manufacturing","Exports","Imports","Distribution","Engineering","Publishing","Trading","Ventures","Partners","Associates" };
                string[] domains = { "ng","com","net","org","africa" };
                string[] departments = { "Finance","Operations","Human Resources","IT Support","Marketing","Administration","Management","Customer Support","Logistics","Sales","Procurement","Legal","Security","Facilities" };
                string[] roles = { "Senior Accountant","Warehouse Lead","HR Partner","IT Support Lead","Marketing Specialist","Operations Officer",
                    "Security Supervisor","Front Desk Executive","General Manager","Customer Success Lead","Finance Analyst","Procurement Officer",
                    "Legal Advisor","Sales Executive","Software Engineer","Data Analyst","Logistics Coordinator","Brand Manager","Audit Supervisor","Admin Officer" };
                string[] otherIncomeDescs = { "Consulting Service • {0}","Technical Support Retainer • {0}",
                    "Product Sales • {0}","Software License • {0}","Maintenance Contract • {0}","Annual Retainer • {0}","Project Milestone • {0}" };
                string[] expenseDescs = { "Office Supplies (OfficeMax)","Generator Fuel (Enyo Retail)","Workspace Lease Rent (Vanguard)",
                    "Internet Service Subscription (MainOne)","Electricity Utility Bill","Logistics Dispatch Fee",
                    "Catering & Refreshments","Vehicle Maintenance","Legal Retainer Fee","IT Equipment Purchase",
                    "Cleaning Services","Security Services","Staff Training","Marketing Campaign","Courier Services",
                    "Bank Charges","Printing & Stationery","Travel & Accommodation","Repairs & Maintenance","Miscellaneous Expenses" };
                string[] hmoPlans = { "Bronze","Silver","Gold","Platinum","Diamond" };
                string[] hmoNames = { "Hygeia HMO","Reliance HMO","AXA Mansard Health","Leadway Health","MediPlan HMO",
                    "Total Health Trust","ClearLine HMO","Avon HMO","LifeWorth HMO","Prepaid Medicare","PHC HMO","First Guarantee HMO" };

                // ── 1. Staff user ─────────────────────────────────────────
                if (!dbContext.Users.Any(u => u.Username == "staff"))
                    dbContext.Users.Add(new User { Username = "staff", PasswordHash = "staff", Role = UserRole.Staff });

                if (!dbContext.Users.Any(u => u.Username == "hr"))
                    dbContext.Users.Add(new User { Username = "hr", PasswordHash = "hr", Role = UserRole.Hr });

                // ── 2. 6,000 Customers ────────────────────────────────────
                var statuses = new[] { CustomerStatus.Current, CustomerStatus.Current, CustomerStatus.Current, CustomerStatus.Pending, CustomerStatus.Overdue };
                string[] areas = { "Ikeja", "Victoria Island", "Lekki Phase 1", "Ikoyi", "Yaba", "Surulere", "Apapa", "Maryland", "Gbagada" };
                string[] streets = { "Adeniran Ogunsanya St", "Bode Thomas St", "Herbert Macaulay Way", "Adetokunbo Ademola St", "Joel Ogunnaike St", "Allen Avenue", "Isaac John St", "Kingsway Road", "Ozumba Mbadiwe Ave" };
                var customers = new List<Customer>();
                for (int i = 0; i < 12000; i++)
                {
                    string fn = firstNames[rng.Next(firstNames.Length)];
                    string ln = lastNames[rng.Next(lastNames.Length)];
                    string biz = bizTypes[rng.Next(bizTypes.Length)];
                    string suf = companySuffixes[rng.Next(companySuffixes.Length)];
                    string name = $"{fn} {ln} {biz} {suf}";
                    string emailBase = $"{fn.ToLower()}.{ln.ToLower()}{i}";
                    string domain = domains[rng.Next(domains.Length)];
                    CustomerStatus status = statuses[rng.Next(statuses.Length)];
                    double balance = status == CustomerStatus.Current ? 0 : rng.Next(50000, 5000000);
                    DateTime since = DateTime.Now.AddDays(-rng.Next(365, 3650));
                    DateTime lastInv = DateTime.Now.AddDays(-rng.Next(1, 180));

                    string phone = $"+234 {rng.Next(802, 809)} {rng.Next(100, 999)} {rng.Next(1000, 9999)}";
                    string address = $"{rng.Next(1, 199)} {streets[rng.Next(streets.Length)]}, {areas[rng.Next(areas.Length)]}, Lagos, Nigeria";

                    DateTime? birthday = (i < 5) 
                        ? new DateTime(DateTime.Now.Year - rng.Next(20, 50), DateTime.Now.Month, DateTime.Now.Day)
                        : DateTime.Now.AddYears(-rng.Next(20, 60)).AddDays(-rng.Next(1, 360));

                    customers.Add(new Customer
                    {
                        Name = name,
                        Email = $"{emailBase}@{emailBase.Substring(0, Math.Min(8, emailBase.Length))}.{domain}",
                        Phone = phone,
                        Address = address,
                        Balance = balance,
                        LastInvoiceDate = lastInv,
                        Status = status,
                        CustomerSince = since,
                        Birthday = birthday
                    });
                }
                dbContext.Customers.AddRange(customers);

                // ── 3. 30 Staff Members ───────────────────────────────────
                var staffList = new List<StaffMember>();
                var usedStaffIds = new System.Collections.Generic.HashSet<string>();
                for (int i = 0; i < 30; i++)
                {
                    string fn = firstNames[rng.Next(firstNames.Length)];
                    string ln = lastNames[rng.Next(lastNames.Length)];
                    string dept = departments[rng.Next(departments.Length)];
                    string role = roles[rng.Next(roles.Length)];
                    string staffId;
                    do { staffId = $"SF-{rng.Next(1000, 9999)}"; } while (usedStaffIds.Contains(staffId));
                    usedStaffIds.Add(staffId);
                    double pay = rng.Next(150000, 900000);
                    StaffStatus status = rng.Next(0, 10) < 8 ? StaffStatus.Active : StaffStatus.OnLeave;
                    staffList.Add(new StaffMember { Name = $"{fn} {ln}", StaffId = staffId, Role = role, Department = dept, MonthlyPay = pay, Status = status });
                }
                dbContext.Staff.AddRange(staffList);

                // Create user accounts for all staff
                foreach (var s in staffList)
                {
                    string cleanName = s.Name.ToLower().Replace(" ", "");
                    UserRole userRole = UserRole.Staff;
                    if (s.Role.Contains("HR", StringComparison.OrdinalIgnoreCase) || 
                        s.Department.Contains("HR", StringComparison.OrdinalIgnoreCase) || 
                        s.Department.Contains("Human Resources", StringComparison.OrdinalIgnoreCase))
                    {
                        userRole = UserRole.Hr;
                    }

                    dbContext.Users.Add(new User { Username = s.StaffId.ToLower(), PasswordHash = "password", Role = userRole });
                    dbContext.Users.Add(new User { Username = cleanName, PasswordHash = "password", Role = userRole });
                }

                // ── 4. 30 Payroll Cycles ─────────────────────────────────
                DateTime payrollStart = DateTime.Now.AddMonths(-30);
                double basePayroll = staffList.Sum(s => s.MonthlyPay);
                for (int i = 0; i < 30; i++)
                {
                    DateTime cycleDate = payrollStart.AddMonths(i);
                    int staffPaid = rng.Next(20, 30);
                    double total = basePayroll * (0.9 + rng.NextDouble() * 0.2); // ±10% variance
                    dbContext.PayrollRuns.Add(new PayrollRun
                    {
                        Name = cycleDate.ToString("MMMM yyyy"),
                        Date = cycleDate,
                        StaffPaidCount = staffPaid,
                        TotalAmount = Math.Round(total, 0),
                        Status = cycleDate < DateTime.Now ? PayrollStatus.Completed : PayrollStatus.Pending
                    });
                }

                // ── 5. 12 HMO Providers ───────────────────────────────────
                for (int i = 0; i < 12; i++)
                {
                    dbContext.HmoProviders.Add(new HmoProvider
                    {
                        Name = hmoNames[i],
                        PlanType = hmoPlans[rng.Next(hmoPlans.Length)],
                        MonthlyPremium = rng.Next(8000, 65000)
                    });
                }

                // ── 6. Transactions (5,000 Income & 1,200 Expenses) ───────
                DateTime txStart = DateTime.Now.AddYears(-2);
                
                // Income: 5,000 total (1,500 of which are Invoices)
                for (int i = 0; i < 10000; i++)
                {
                    DateTime date = txStart.AddMinutes(rng.Next(1, 365 * 2 * 24 * 60));
                    double amount = rng.Next(2000, 750000);
                    var cust = customers[rng.Next(customers.Count)];
                    
                    string description;
                    if (i < 3000)
                    {
                        // Exactly 1,500 invoices
                        description = $"Invoice #INV-{rng.Next(2100, 9999)} • {cust.Name}";
                    }
                    else
                    {
                        // Other income descriptions
                        string template = otherIncomeDescs[rng.Next(otherIncomeDescs.Length)];
                        description = string.Format(template, cust.Name);
                    }

                    dbContext.Transactions.Add(new Transaction 
                    { 
                        Description = description, 
                        Amount = amount, 
                        Type = TransactionType.Income, 
                        Date = new DateTimeOffset(date) 
                    });
                }

                // Expenses: 1,200 total
                for (int i = 0; i < 3000; i++)
                {
                    DateTime date = txStart.AddMinutes(rng.Next(1, 365 * 2 * 24 * 60));
                    double amount = rng.Next(2000, 150000);
                    string description = expenseDescs[rng.Next(expenseDescs.Length)];
                    dbContext.Transactions.Add(new Transaction 
                    { 
                        Description = description, 
                        Amount = amount, 
                        Type = TransactionType.Expense, 
                        Date = new DateTimeOffset(date) 
                    });
                }

                // ── 7. 2,800 Inventory Items ─────────────────────────────
                string[] productNames = { "HP ProBook Laptop", "Dell UltraSharp Monitor", "Logitech Wireless Mouse", "Mechanical Keyboard", "USB-C Hub Multiport",
                    "Ergonomic Office Chair", "Standing Desk", "Noise Cancelling Headphones", "Uninterruptible Power Supply (UPS)", "Laser Jet Printer",
                    "Network Switch 24-Port", "External Hard Drive 2TB", "Bluetooth Conference Speaker", "Whiteboard 4x3 Ft", "LED Desk Lamp" };
                
                var inventoryList = new List<InventoryItem>();
                for (int i = 0; i < 5000; i++)
                {
                    string baseName = productNames[rng.Next(productNames.Length)];
                    string finalName = $"{baseName} Model-{rng.Next(100, 999)} #{i+1}";
                    int reorder = rng.Next(5, 15);
                    // 30% chance of needing restock
                    int qty = (rng.Next(0, 10) < 3) ? rng.Next(0, reorder + 1) : rng.Next(reorder + 1, 100);
                    
                    double cost = rng.Next(5000, 150000);
                    double markup = 1.15 + (rng.NextDouble() * 0.35); // 15% to 50% markup
                    double selling = Math.Round(cost * markup, 0);

                    inventoryList.Add(new InventoryItem
                    {
                        Name = finalName,
                        Quantity = qty,
                        CostPrice = cost,
                        SellingPrice = selling,
                        ReorderLevel = reorder
                    });
                }
                dbContext.InventoryItems.AddRange(inventoryList);

                dbContext.SaveChanges();

                StatusMessage.Text = $"✅ Success! Loaded 12,000 customers · 30 staff · 12 HMOs · 30 payroll cycles · 10,000 income (incl. 3,000 invoices) · 3,000 expenses · 5,000 products (Total ~30,000 database records).";
                StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                StatusMessage.Visibility = Visibility.Visible;

                LoadHmoProviders();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Database Seeding Error: {ex.Message}";
                StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                StatusMessage.Visibility = Visibility.Visible;
            }
        }


        private void DeleteDemoDataButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = CustomMessageBox.Show(
                "Are you sure you want to completely wipe all dynamic data? This will delete all transactions, staff records, customer accounts, HMO settings, and payroll runs from the database.",
                "Confirm Database Wipe",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    dbContext.Database.EnsureDeleted();
                    dbContext.Database.EnsureCreated();

                    // Re-add default admin user
                    if (!dbContext.Users.Any())
                    {
                        dbContext.Users.Add(new User { Username = "admin", PasswordHash = "admin", Role = UserRole.Admin });
                        dbContext.SaveChanges();
                    }

                    // Re-add default company profile
                    if (!dbContext.CompanyProfiles.Any())
                    {
                        dbContext.CompanyProfiles.Add(new CompanyProfile
                        {
                            Name = "LedgerFlow Tech",
                            Email = "finance@ledgerflow.com",
                            Address = "100 Commercial Avenue, Lagos, Nigeria"
                        });
                        dbContext.SaveChanges();
                    }
                }

                StatusMessage.Text = "Database completely wiped! All stats are back to ₦0 / 0 active staff.";
                StatusMessage.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.DarkRed);
                StatusMessage.Visibility = Visibility.Visible;
                
                LoadHmoProviders();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error wiping database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddHmo_Click(object sender, RoutedEventArgs e)
        {
            string name = HmoNameBox.Text.Trim();
            string plan = (HmoPlanCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Bronze";
            string premiumStr = HmoPremiumBox.Text.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(premiumStr))
            {
                CustomMessageBox.Show("Please enter a provider name and premium amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(premiumStr, out double premium))
            {
                CustomMessageBox.Show("Please enter a valid numeric premium amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    dbContext.HmoProviders.Add(new HmoProvider
                    {
                        Name = name,
                        PlanType = plan,
                        MonthlyPremium = premium
                    });
                    dbContext.SaveChanges();
                }

                HmoNameBox.Clear();
                HmoPremiumBox.Clear();
                LoadHmoProviders();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error adding HMO provider: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SavePreferences_Click(object sender, RoutedEventArgs e)
        {
            string currency = (CurrencyCombo.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "₦";
            string taxRate = TaxRateBox.Text;
            bool disableCms = DisableCmsCheck.IsChecked ?? false;
            bool disablePos = DisablePosCheck.IsChecked ?? false;
            bool disableHrms = DisableHrmsCheck.IsChecked ?? false;

            try
            {
                using (var dbContext = new AppDbContext())
                {
                    var profile = dbContext.CompanyProfiles.FirstOrDefault();
                    if (profile != null)
                    {
                        profile.DisableCms = disableCms;
                        profile.DisablePos = disablePos;
                        profile.DisableHrms = disableHrms;
                        dbContext.CompanyProfiles.Update(profile);
                        dbContext.SaveChanges();
                    }
                }

                if (App.CurrentWindow.AppFrame.Content is MainPage mainPage)
                {
                    mainPage.ReloadModuleVisibility();
                }

                CustomMessageBox.Show(
                    $"Financial Preferences Saved!\n\nCurrency symbol: {currency}\nTax Rate: {taxRate}%\nDisable CMS: {disableCms}\nDisable POS: {disablePos}\nDisable HRMS: {disableHrms}",
                    "Settings Saved",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Failed to save preferences: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
