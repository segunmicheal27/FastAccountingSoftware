using FastAccountingSoftware.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Linq;

namespace FastAccountingSoftware.Views
{
    public partial class SplashPage : Page
    {
        public SplashPage()
        {
            this.InitializeComponent();
            this.Loaded += SplashPage_Loaded;
        }

        private async void SplashPage_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeBackgroundWaves();

            // Simulate loading progress
            for (int i = 0; i <= 100; i += 2)
            {
                LoadingProgressBar.Value = i;
                ProgressText.Text = $"{i}%";

                // Initialize DB at 50%
                if (i == 50)
                {
                    try
                    {
                        using (var dbContext = new AppDbContext())
                        {
                            await dbContext.Database.EnsureCreatedAsync();
                            // Test query to detect schema discrepancy
                            _ = dbContext.Customers.FirstOrDefault();
                            _ = dbContext.CompanyProfiles.FirstOrDefault();
                        }
                    }
                    catch
                    {
                        try
                        {
                            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                            string dbPath = System.IO.Path.Combine(folder, "fast_accounting.db");
                            if (System.IO.File.Exists(dbPath)) System.IO.File.Delete(dbPath);
                        }
                        catch { }
                    }

                    using (var dbContext = new AppDbContext())
                    {
                        await dbContext.Database.EnsureCreatedAsync();

                        if (!dbContext.Users.Any())
                        {
                            dbContext.Users.Add(new User { Username = "admin", PasswordHash = "admin", Role = UserRole.Admin });
                            await dbContext.SaveChangesAsync();
                        }

                        if (!dbContext.CompanyProfiles.Any())
                        {
                            string[] companyNames = new string[] 
                            { 
                                "LedgerFlow Tech", 
                                "Apex Ventures Ltd", 
                                "Zirconia Partners", 
                                "Savannah Logistics", 
                                "Sovereign Capital", 
                                "Vanguard Group NGR", 
                                "Beacon Digital Services", 
                                "Altus Manufacturing" 
                            };
                            var random = new Random();
                            string randomName = companyNames[random.Next(companyNames.Length)];
                            
                            dbContext.CompanyProfiles.Add(new CompanyProfile
                            {
                                Name = randomName,
                                Email = $"finance@{randomName.ToLower().Replace(" ", "").Replace("ltd", "").Replace("group", "")}.com",
                                Address = $"{random.Next(1, 150)} Commercial Avenue, Lagos, Nigeria"
                            });
                            await dbContext.SaveChangesAsync();
                        }
                    }
                }

                await Task.Delay(20); // 20ms per 2% = 1 second total animation
            }

            await Task.Delay(200); // Slight pause at 100%

            // Navigate to Login Page
            App.CurrentWindow.AppFrame.Navigate(new LoginPage());
        }

        private void InitializeBackgroundWaves()
        {
            if (WaveCanvas == null) return;
            WaveCanvas.Children.Clear();
            
            int lineCount = 35;
            
            // Group A: Left to Right waves (taller, flowing between Y=160 and Y=280)
            for (int i = 0; i < lineCount; i++)
            {
                var path = new Path();
                path.Stroke = new SolidColorBrush(Color.FromArgb(
                    (byte)(100 - i * 2.8), 
                    244, 169, 154)); // #F4A99A
                path.StrokeThickness = 0.8 - (i * 0.015);
                path.Opacity = 0.6 - (i * 0.012);
                
                double yOffset = i * 1.8;
                
                var geometry = Geometry.Parse($"M 0,{160 + yOffset} C 200,{110 + yOffset} 400,{210 + yOffset} 650,{140 + yOffset} C 850,{90 + yOffset} 1050,{180 + yOffset} 1250,{120 + yOffset} C 1310,{110 + yOffset} 1350,{120 + yOffset} 1366,{115 + yOffset}");
                path.Data = geometry;
                WaveCanvas.Children.Add(path);
            }

            // Group B: Crossing waves
            for (int i = 0; i < lineCount; i++)
            {
                var path = new Path();
                path.Stroke = new SolidColorBrush(Color.FromArgb(
                    (byte)(80 - i * 2.2), 
                    249, 196, 184)); // #F9C4B8
                path.StrokeThickness = 0.7 - (i * 0.012);
                path.Opacity = 0.5 - (i * 0.01);
                
                double yOffset = i * 2.0;
                
                var geometry = Geometry.Parse($"M 0,{110 + yOffset} C 200,{210 + yOffset} 400,{70 + yOffset} 680,{170 + yOffset} C 860,{250 + yOffset} 1080,{120 + yOffset} 1260,{200 + yOffset} C 1310,{220 + yOffset} 1350,{200 + yOffset} 1366,{205 + yOffset}");
                path.Data = geometry;
                WaveCanvas.Children.Add(path);
            }
        }
    }
}
