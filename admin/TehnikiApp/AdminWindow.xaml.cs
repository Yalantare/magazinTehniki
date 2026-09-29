using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TehnikiApp.Models;

namespace TehnikiApp
{
    public partial class AdminWindow : Window
    {
        private readonly User _currentUser;
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:8090/") };

        private List<Product> _allProducts = new List<Product>();
        private List<Receipt> _allReceipts = new List<Receipt>();
        private List<User> _allUsers = new List<User>();

        private Product? _editingProduct;
        private string _currentTab = "Products";
        private List<Categorye> _categories = new List<Categorye>();

        private Brush ActiveTabBrush => (Brush)Application.Current.Resources["ThemeTextPrimary"];
        private Brush InactiveTabBrush => (Brush)Application.Current.Resources["ThemeTextSecondary"];

        public AdminWindow(User user)
        {
            InitializeComponent();
            _currentUser = user ?? new User { Name = "Администратор", Role = "admin" };
            UpdateThemeIcon();

            if (AdminNameTxt != null)
            {
                AdminNameTxt.Text = !string.IsNullOrWhiteSpace(_currentUser.Name) ? _currentUser.Name.Split(' ')[0] : "Администратор";
            }
            if (AdminInitialsTxt != null)
            {
                AdminInitialsTxt.Text = !string.IsNullOrWhiteSpace(_currentUser.Name) ? _currentUser.Name[0].ToString().ToUpper() : "A";
            }

            if (ModalCategoryCombo != null)
            {
                ModalCategoryCombo.ItemsSource = _categories;
            }

            _isUpdatingAnalyticsDates = true;
            if (AnalyticsStartDatePicker != null) AnalyticsStartDatePicker.SelectedDate = _analyticsStartDate;
            if (AnalyticsEndDatePicker != null) AnalyticsEndDatePicker.SelectedDate = _analyticsEndDate;
            _isUpdatingAnalyticsDates = false;

            LoadAllData();
        }

        public List<Status> StatusesList { get; set; } = new List<Status>();

        private async void LoadAllData()
        {
            try
            {
                await Task.WhenAll(LoadProducts(), LoadReceipts(), LoadUsers(), LoadCategories(), LoadStatuses());
                UpdateDashboardStats();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProducts()
        {
            try
            {
                var products = await _client.GetFromJsonAsync<List<Product>>("api/Products");
                if (products != null)
                {
                    _allProducts = products;
                    FilterProducts();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading products: " + ex.Message);
            }
        }

        private async Task LoadReceipts()
        {
            try
            {
                var receipts = await _client.GetFromJsonAsync<List<Receipt>>("api/Receipts/get_all_receipts");
                if (receipts != null)
                {
                    _allReceipts = receipts.Where(r => r.OrderStatus > 0).ToList(); 
                    if (_allReceipts.Count > 0 && _analyticsPeriod == "All")
                    {
                        _analyticsStartDate = _allReceipts.Min(r => r.DateTime.Date);
                        _analyticsEndDate = DateTime.Today;
                        _isUpdatingAnalyticsDates = true;
                        if (AnalyticsStartDatePicker != null) AnalyticsStartDatePicker.SelectedDate = _analyticsStartDate;
                        if (AnalyticsEndDatePicker != null) AnalyticsEndDatePicker.SelectedDate = _analyticsEndDate;
                        _isUpdatingAnalyticsDates = false;
                    }
                    FilterSales();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading receipts: " + ex.Message);
            }
        }

        private async Task LoadUsers()
        {
            try
            {
                var users = await _client.GetFromJsonAsync<List<User>>("api/Users");
                if (users != null)
                {
                    _allUsers = users;
                    FilterUsers();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading users: " + ex.Message);
            }
        }

        private async Task LoadStatuses()
        {
            try
            {
                var statuses = await _client.GetFromJsonAsync<List<Status>>("api/Receipts/statuses");
                if (statuses != null && statuses.Count > 0)
                {
                    StatusesList = statuses;
                    
                    if (SalesStatusFilter != null)
                    {
                        SalesStatusFilter.Items.Clear();
                        
                        var allItem = new ComboBoxItem { Content = "Все статусы", IsSelected = true };
                        SalesStatusFilter.Items.Add(allItem);
                        
                        foreach (var status in statuses)
                        {
                            SalesStatusFilter.Items.Add(new ComboBoxItem { Content = status.Title });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading statuses: " + ex.Message);
            }
        }

        private async Task LoadCategories()
        {
            try
            {
                var categories = await _client.GetFromJsonAsync<List<Categorye>>("api/Products/categories");
                if (categories != null && categories.Count > 0)
                {
                    _categories = categories;
                    if (ModalCategoryCombo != null)
                    {
                        ModalCategoryCombo.ItemsSource = null;
                        ModalCategoryCombo.ItemsSource = _categories;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error loading categories: " + ex.Message);
            }
        }

        private void UpdateDashboardStats()
        {
            
            int totalProducts = _allProducts.Count;
            int outOfStock = _allProducts.Count(p => p.Stock == 0);
            CardTotalProductsTxt.Text = totalProducts.ToString();
            CardProductsAlertTxt.Text = $"{outOfStock} нет в наличии";
            TabProductsBadge.Text = totalProducts.ToString();

            
            int totalOrders = _allReceipts.Count;
            int inProgress = _allReceipts.Count(r => r.Status == 1); 
            CardTotalOrdersTxt.Text = totalOrders.ToString();
            CardOrdersProgressTxt.Text = $"{inProgress} в процессе";
            TabSalesBadge.Text = totalOrders.ToString();

            
            decimal totalSalesSum = _allReceipts.Sum(r => r.TotalPrice);
            CardTotalSalesTxt.Text = $"{totalSalesSum:N0}₽";

            
            int totalUsers = _allUsers.Count;
            CardTotalUsersTxt.Text = totalUsers.ToString();
            TabUsersBadge.Text = totalUsers.ToString();

            if (_currentTab == "Analytics")
            {
                UpdateAnalytics();
            }
        }

        
        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tabName)
            {
                _currentTab = tabName;

                
                PanelProducts.Visibility = tabName == "Products" ? Visibility.Visible : Visibility.Collapsed;
                PanelSales.Visibility = tabName == "Sales" ? Visibility.Visible : Visibility.Collapsed;
                PanelUsers.Visibility = tabName == "Users" ? Visibility.Visible : Visibility.Collapsed;
                PanelAnalytics.Visibility = tabName == "Analytics" ? Visibility.Visible : Visibility.Collapsed;

                
                TabProductsText.Foreground = tabName == "Products" ? ActiveTabBrush : InactiveTabBrush;
                TabProductsBadge.Foreground = tabName == "Products" ? ActiveTabBrush : InactiveTabBrush;
                
                TabSalesText.Foreground = tabName == "Sales" ? ActiveTabBrush : InactiveTabBrush;
                TabSalesBadge.Foreground = tabName == "Sales" ? ActiveTabBrush : InactiveTabBrush;

                TabUsersText.Foreground = tabName == "Users" ? ActiveTabBrush : InactiveTabBrush;
                TabUsersBadge.Foreground = tabName == "Users" ? ActiveTabBrush : InactiveTabBrush;

                TabAnalyticsText.Foreground = tabName == "Analytics" ? ActiveTabBrush : InactiveTabBrush;

                if (tabName == "Analytics")
                {
                    if (_analyticsPeriod == "All" && _allReceipts.Count > 0)
                    {
                        SetAnalyticsPeriod("All");
                    }
                    else
                    {
                        UpdateAnalytics();
                    }
                }
            }
        }

        // === Dynamic Analytics State ===
        private string _analyticsPeriod = "All"; // "All", "Year", "Month", "30Days", "7Days", "Custom"
        private DateTime _analyticsStartDate = DateTime.Today.AddMonths(-6);
        private DateTime _analyticsEndDate = DateTime.Today;
        private string _analyticsMetric = "Revenue"; // "Revenue", "Orders", "Units", "AvgCheck"
        private string _analyticsChartType = "Area"; // "Area", "Bar"
        private string _analyticsBreakdownTab = "Categories"; // "Categories", "Products"
        private bool _isUpdatingAnalyticsDates = false;

        private class ChartDataPoint
        {
            public DateTime Date { get; set; }
            public DateTime EndDate { get; set; }
            public string Label { get; set; } = "";
            public string FullPeriodLabel { get; set; } = "";
            public decimal Revenue { get; set; }
            public int OrdersCount { get; set; }
            public int UnitsSold { get; set; }
            public decimal AvgCheck => OrdersCount > 0 ? Revenue / OrdersCount : 0;
            public double Value { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double BarWidth { get; set; }
            public double BarHeight { get; set; }
        }

        private List<ChartDataPoint> _currentChartPoints = new List<ChartDataPoint>();
        private Border? _chartHoverBadge = null;
        private System.Windows.Shapes.Line? _chartHoverLine = null;
        private System.Windows.Shapes.Ellipse? _chartHoverDot = null;

        private static readonly Brush _activePeriodBrush = new SolidColorBrush(Color.FromRgb(37, 99, 235));

        private void AnalyticsPeriod_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                SetAnalyticsPeriod(tag);
            }
        }

        private void SetAnalyticsPeriod(string periodTag)
        {
            _analyticsPeriod = periodTag;

            var periodButtons = new[] { PeriodAllBtn, PeriodYearBtn, PeriodMonthBtn, Period30Btn, Period7Btn, PeriodCustomBtn };
            var inactiveTextBrush = (Brush)Application.Current.Resources["ThemeTextSecondary"];

            foreach (var b in periodButtons)
            {
                if (b == null) continue;
                bool isSelected = (string)b.Tag == periodTag;
                b.Background = isSelected ? _activePeriodBrush : Brushes.Transparent;
                b.Foreground = isSelected ? Brushes.White : inactiveTextBrush;
            }

            DateTime today = DateTime.Today;
            DateTime start = today.AddDays(-6);
            DateTime end = today;

            switch (periodTag)
            {
                case "All":
                    start = _allReceipts.Count > 0 ? _allReceipts.Min(r => r.DateTime.Date) : today.AddMonths(-6);
                    end = today;
                    break;
                case "Year":
                    start = new DateTime(today.Year, 1, 1);
                    end = today;
                    break;
                case "Month":
                    start = new DateTime(today.Year, today.Month, 1);
                    end = today;
                    break;
                case "30Days":
                    start = today.AddDays(-29);
                    end = today;
                    break;
                case "7Days":
                    start = today.AddDays(-6);
                    end = today;
                    break;
                case "Custom":
                    start = AnalyticsStartDatePicker?.SelectedDate ?? today.AddMonths(-1);
                    end = AnalyticsEndDatePicker?.SelectedDate ?? today;
                    break;
            }

            if (start > end) start = end;

            _analyticsStartDate = start;
            _analyticsEndDate = end;

            _isUpdatingAnalyticsDates = true;
            if (AnalyticsStartDatePicker != null) AnalyticsStartDatePicker.SelectedDate = start;
            if (AnalyticsEndDatePicker != null) AnalyticsEndDatePicker.SelectedDate = end;
            _isUpdatingAnalyticsDates = false;

            UpdateAnalytics();
        }

        private void AnalyticsDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingAnalyticsDates) return;

            DateTime start = AnalyticsStartDatePicker?.SelectedDate ?? DateTime.Today.AddMonths(-1);
            DateTime end = AnalyticsEndDatePicker?.SelectedDate ?? DateTime.Today;

            if (start > end) start = end;

            _analyticsStartDate = start;
            _analyticsEndDate = end;
            _analyticsPeriod = "Custom";

            var periodButtons = new[] { PeriodAllBtn, PeriodYearBtn, PeriodMonthBtn, Period30Btn, Period7Btn, PeriodCustomBtn };
            var inactiveTextBrush = (Brush)Application.Current.Resources["ThemeTextSecondary"];

            foreach (var b in periodButtons)
            {
                if (b == null) continue;
                bool isSelected = (string)b.Tag == "Custom";
                b.Background = isSelected ? _activePeriodBrush : Brushes.Transparent;
                b.Foreground = isSelected ? Brushes.White : inactiveTextBrush;
            }

            UpdateAnalytics();
        }

        private void AnalyticsMetricCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnalyticsMetricCombo?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                _analyticsMetric = tag;
                DrawSalesChart();
            }
        }

        private void AnalyticsChartType_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _analyticsChartType = tag;
                var inactiveTextBrush = (Brush)Application.Current.Resources["ThemeTextSecondary"];

                if (ChartTypeAreaBtn != null)
                {
                    bool isArea = tag == "Area";
                    ChartTypeAreaBtn.Background = isArea ? _activePeriodBrush : Brushes.Transparent;
                    ChartTypeAreaBtn.Foreground = isArea ? Brushes.White : inactiveTextBrush;
                }
                if (ChartTypeBarBtn != null)
                {
                    bool isBar = tag == "Bar";
                    ChartTypeBarBtn.Background = isBar ? _activePeriodBrush : Brushes.Transparent;
                    ChartTypeBarBtn.Foreground = isBar ? Brushes.White : inactiveTextBrush;
                }

                DrawSalesChart();
            }
        }

        private void BreakdownTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                _analyticsBreakdownTab = tag;
                var inactiveTextBrush = (Brush)Application.Current.Resources["ThemeTextSecondary"];

                if (BreakdownTabCatsBtn != null)
                {
                    bool isCats = tag == "Categories";
                    BreakdownTabCatsBtn.Background = isCats ? _activePeriodBrush : Brushes.Transparent;
                    BreakdownTabCatsBtn.Foreground = isCats ? Brushes.White : inactiveTextBrush;
                }
                if (BreakdownTabProdsBtn != null)
                {
                    bool isProds = tag == "Products";
                    BreakdownTabProdsBtn.Background = isProds ? _activePeriodBrush : Brushes.Transparent;
                    BreakdownTabProdsBtn.Foreground = isProds ? Brushes.White : inactiveTextBrush;
                }

                if (CategoryStatsPanel != null)
                    CategoryStatsPanel.Visibility = tag == "Categories" ? Visibility.Visible : Visibility.Collapsed;
                if (TopProductsStatsPanel != null)
                    TopProductsStatsPanel.Visibility = tag == "Products" ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateAnalytics()
        {
            if (_allReceipts == null || _allProducts == null || _categories == null) return;

            DateTime startDate = _analyticsStartDate.Date;
            DateTime endDate = _analyticsEndDate.Date.AddDays(1).AddTicks(-1);

            var filteredReceipts = _allReceipts
                .Where(r => r.DateTime >= startDate && r.DateTime <= endDate)
                .ToList();

            decimal periodRevenue = filteredReceipts.Sum(r => r.TotalPrice);
            int periodOrders = filteredReceipts.Count;
            decimal avgCheck = periodOrders > 0 ? periodRevenue / periodOrders : 0;

            var allItems = filteredReceipts.SelectMany(r => r.ReceiptItems).ToList();
            int totalItemsSold = allItems.Sum(i => i.Quantity);
            int uniqueItemsSold = allItems.Select(i => i.ProductId).Distinct().Count();

            if (CardPeriodRevenueTxt != null)
                CardPeriodRevenueTxt.Text = $"{periodRevenue:N0}₽";
            if (CardPeriodOrdersTxt != null)
                CardPeriodOrdersTxt.Text = $"{periodOrders} {GetOrdersWord(periodOrders)}";

            if (CardAvgCheckTxt != null)
                CardAvgCheckTxt.Text = $"{avgCheck:N0}₽";

            if (CardTotalUnitsSoldTxt != null)
                CardTotalUnitsSoldTxt.Text = $"{totalItemsSold:N0} шт";
            if (CardUniqueItemsTxt != null)
                CardUniqueItemsTxt.Text = $"{uniqueItemsSold} {GetItemsWord(uniqueItemsSold)}";

            var categoryRevenue = allItems
                .Where(i => i.Product != null)
                .GroupBy(i => i.Product.Category)
                .Select(g => new { 
                    CategoryId = g.Key, 
                    Revenue = g.Sum(i => i.Quantity * i.DisplayPrice) 
                })
                .OrderByDescending(x => x.Revenue)
                .FirstOrDefault();

            if (categoryRevenue != null && CardTopCategoryTxt != null && CardTopCategoryRevenueTxt != null)
            {
                var cat = _categories.FirstOrDefault(c => c.Id == categoryRevenue.CategoryId);
                CardTopCategoryTxt.Text = cat?.Title ?? "Техника";
                double share = periodRevenue > 0 ? (double)(categoryRevenue.Revenue / periodRevenue) * 100 : 0;
                CardTopCategoryRevenueTxt.Text = $"{categoryRevenue.Revenue:N0}₽ ({share:F0}% от выручки)";
            }
            else if (CardTopCategoryTxt != null && CardTopCategoryRevenueTxt != null)
            {
                CardTopCategoryTxt.Text = "Нет данных";
                CardTopCategoryRevenueTxt.Text = "0₽ дохода";
            }

            DrawSalesChart();
            PopulateCategoryBreakdown(filteredReceipts, periodRevenue);
            PopulateTopProductsBreakdown(allItems, periodRevenue);
        }

        private void PopulateCategoryBreakdown(List<Receipt> filteredReceipts, decimal periodTotalRevenue)
        {
            if (CategoryStatsPanel == null) return;
            CategoryStatsPanel.Children.Clear();

            if (_categories == null || _categories.Count == 0 || filteredReceipts.Count == 0)
            {
                CategoryStatsPanel.Children.Add(new TextBlock 
                { 
                    Text = "Нет данных о продажах за период.", 
                    Foreground = (Brush)Application.Current.Resources["ThemeTextSecondary"],
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            var salesByCategory = filteredReceipts.SelectMany(r => r.ReceiptItems)
                .Where(i => i.Product != null)
                .GroupBy(i => i.Product.Category)
                .Select(g => new {
                    CategoryId = g.Key,
                    CategoryName = _categories.FirstOrDefault(c => c.Id == g.Key)?.Title ?? "Другие",
                    Revenue = g.Sum(i => i.Quantity * i.DisplayPrice),
                    Units = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            if (salesByCategory.Count == 0)
            {
                CategoryStatsPanel.Children.Add(new TextBlock 
                { 
                    Text = "Нет проданных товаров за период.", 
                    Foreground = (Brush)Application.Current.Resources["ThemeTextSecondary"],
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            decimal maxRevenue = salesByCategory.Max(x => x.Revenue);
            if (maxRevenue == 0) maxRevenue = 1;

            foreach (var item in salesByCategory)
            {
                var itemGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
                itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                itemGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var labelGrid = new Grid();
                labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                labelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                double percentOfTotal = periodTotalRevenue > 0 ? (double)(item.Revenue / periodTotalRevenue) * 100 : 0;

                var txtName = new TextBlock
                {
                    Text = $"{item.CategoryName} ({percentOfTotal:F0}%)",
                    Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtName, 0);
                labelGrid.Children.Add(txtName);

                var txtValue = new TextBlock
                {
                    Text = $"{item.Revenue:N0}₽ • {item.Units} шт",
                    Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtValue, 1);
                labelGrid.Children.Add(txtValue);

                Grid.SetRow(labelGrid, 0);
                itemGrid.Children.Add(labelGrid);

                var trackBorder = new Border
                {
                    Background = (Brush)Application.Current.Resources["ThemeBorderLight"],
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 8, 0, 0)
                };

                var barGrid = new Grid();
                double percent = (double)(item.Revenue / maxRevenue);
                if (percent < 0.02) percent = 0.02;

                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(percent, GridUnitType.Star) });
                barGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.0 - percent, GridUnitType.Star) });

                var filledBorder = new Border
                {
                    Background = (Brush)Application.Current.Resources["ThemeAccent"],
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                Grid.SetColumn(filledBorder, 0);
                barGrid.Children.Add(filledBorder);

                trackBorder.Child = barGrid;
                Grid.SetRow(trackBorder, 1);
                itemGrid.Children.Add(trackBorder);

                CategoryStatsPanel.Children.Add(itemGrid);
            }
        }

        private void PopulateTopProductsBreakdown(List<ReceiptItem> allItems, decimal periodTotalRevenue)
        {
            if (TopProductsStatsPanel == null) return;
            TopProductsStatsPanel.Children.Clear();

            if (allItems == null || allItems.Count == 0)
            {
                TopProductsStatsPanel.Children.Add(new TextBlock 
                { 
                    Text = "Нет проданных товаров за период.", 
                    Foreground = (Brush)Application.Current.Resources["ThemeTextSecondary"],
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                });
                return;
            }

            var topProducts = allItems
                .Where(i => i.Product != null)
                .GroupBy(i => i.ProductId)
                .Select(g => new {
                    ProductId = g.Key,
                    ProductTitle = g.First().Product?.Title ?? $"Товар #{g.Key}",
                    Manufacturer = g.First().Product?.Manufacturer ?? "",
                    Units = g.Sum(i => i.Quantity),
                    Revenue = g.Sum(i => i.Quantity * i.DisplayPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();

            decimal maxProdRevenue = topProducts.Count > 0 ? topProducts.Max(p => p.Revenue) : 1;
            if (maxProdRevenue == 0) maxProdRevenue = 1;

            int rank = 1;
            foreach (var prod in topProducts)
            {
                var card = new Border
                {
                    Background = (Brush)Application.Current.Resources["ThemePanelBg"],
                    BorderBrush = (Brush)Application.Current.Resources["ThemeBorderLight"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 10, 12, 10),
                    Margin = new Thickness(0, 0, 0, 10)
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var rankTxt = new TextBlock
                {
                    Text = $"#{rank++}",
                    Foreground = (Brush)Application.Current.Resources["ThemeAccent"],
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(rankTxt, 0);
                grid.Children.Add(rankTxt);

                var titleStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                var titleTxt = new TextBlock
                {
                    Text = prod.ProductTitle,
                    Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var subTxt = new TextBlock
                {
                    Text = $"{prod.Units} шт продано",
                    Foreground = (Brush)Application.Current.Resources["ThemeTextSecondary"],
                    FontSize = 11,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                titleStack.Children.Add(titleTxt);
                titleStack.Children.Add(subTxt);
                Grid.SetColumn(titleStack, 1);
                grid.Children.Add(titleStack);

                var revTxt = new TextBlock
                {
                    Text = $"{prod.Revenue:N0}₽",
                    Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(revTxt, 2);
                grid.Children.Add(revTxt);

                card.Child = grid;
                TopProductsStatsPanel.Children.Add(card);
            }
        }

        private void DrawSalesChart()
        {
            if (SalesChartCanvas == null || _allReceipts == null) return;

            double canvasWidth = SalesChartCanvas.ActualWidth;
            double canvasHeight = SalesChartCanvas.ActualHeight;

            if (canvasWidth < 50) canvasWidth = 600;
            if (canvasHeight < 50) canvasHeight = 300;

            SalesChartCanvas.Children.Clear();
            _currentChartPoints.Clear();

            DateTime start = _analyticsStartDate.Date;
            DateTime end = _analyticsEndDate.Date;
            if (start > end) start = end;

            double totalDays = (end - start).TotalDays + 1;
            var chartData = new List<ChartDataPoint>();

            if (totalDays <= 31)
            {
                // По дням
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    var dayOrders = _allReceipts.Where(r => r.DateTime.Date == d).ToList();
                    var dayItems = dayOrders.SelectMany(r => r.ReceiptItems).ToList();

                    chartData.Add(new ChartDataPoint
                    {
                        Date = d,
                        EndDate = d,
                        Label = d.ToString("dd.MM"),
                        FullPeriodLabel = d.ToString("dd MMMM yyyy"),
                        Revenue = dayOrders.Sum(r => r.TotalPrice),
                        OrdersCount = dayOrders.Count,
                        UnitsSold = dayItems.Sum(i => i.Quantity)
                    });
                }
            }
            else if (totalDays <= 120)
            {
                // По интервалам (недели / несколько дней)
                int stepDays = (int)Math.Ceiling(totalDays / 14);
                if (stepDays < 2) stepDays = 2;

                for (var d = start; d <= end; d = d.AddDays(stepDays))
                {
                    var dEnd = d.AddDays(stepDays - 1);
                    if (dEnd > end) dEnd = end;

                    var intervalOrders = _allReceipts.Where(r => r.DateTime.Date >= d && r.DateTime.Date <= dEnd).ToList();
                    var intervalItems = intervalOrders.SelectMany(r => r.ReceiptItems).ToList();

                    chartData.Add(new ChartDataPoint
                    {
                        Date = d,
                        EndDate = dEnd,
                        Label = $"{d:dd.MM}",
                        FullPeriodLabel = $"{d:dd.MM.yyyy} — {dEnd:dd.MM.yyyy}",
                        Revenue = intervalOrders.Sum(r => r.TotalPrice),
                        OrdersCount = intervalOrders.Count,
                        UnitsSold = intervalItems.Sum(i => i.Quantity)
                    });
                }
            }
            else
            {
                // По месяцам
                var cur = new DateTime(start.Year, start.Month, 1);
                var lastMonth = new DateTime(end.Year, end.Month, 1);

                while (cur <= lastMonth)
                {
                    var monthEnd = cur.AddMonths(1).AddDays(-1);
                    var rangeStart = cur < start ? start : cur;
                    var rangeEnd = monthEnd > end ? end : monthEnd;

                    var monthOrders = _allReceipts.Where(r => r.DateTime.Date >= rangeStart && r.DateTime.Date <= rangeEnd).ToList();
                    var monthItems = monthOrders.SelectMany(r => r.ReceiptItems).ToList();

                    chartData.Add(new ChartDataPoint
                    {
                        Date = rangeStart,
                        EndDate = rangeEnd,
                        Label = cur.ToString("MMM yy"),
                        FullPeriodLabel = cur.ToString("MMMM yyyy"),
                        Revenue = monthOrders.Sum(r => r.TotalPrice),
                        OrdersCount = monthOrders.Count,
                        UnitsSold = monthItems.Sum(i => i.Quantity)
                    });

                    cur = cur.AddMonths(1);
                }
            }

            if (chartData.Count == 0)
            {
                chartData.Add(new ChartDataPoint
                {
                    Date = start,
                    EndDate = end,
                    Label = start.ToString("dd.MM"),
                    FullPeriodLabel = start.ToString("dd MMMM yyyy"),
                    Revenue = 0,
                    OrdersCount = 0,
                    UnitsSold = 0
                });
            }

            // Назначаем текущее значение Value в зависимости от метрики
            foreach (var pt in chartData)
            {
                switch (_analyticsMetric)
                {
                    case "Orders":
                        pt.Value = pt.OrdersCount;
                        break;
                    case "Units":
                        pt.Value = pt.UnitsSold;
                        break;
                    case "AvgCheck":
                        pt.Value = (double)pt.AvgCheck;
                        break;
                    case "Revenue":
                    default:
                        pt.Value = (double)pt.Revenue;
                        break;
                }
            }

            string metricName = _analyticsMetric switch
            {
                "Orders" => "Динамика количества заказов",
                "Units" => "Динамика проданных товаров",
                "AvgCheck" => "Динамика среднего чека",
                _ => "Динамика выручки"
            };
            if (ChartTitleTxt != null) ChartTitleTxt.Text = metricName;

            decimal totalMetricVal = _analyticsMetric switch
            {
                "Orders" => chartData.Sum(p => p.OrdersCount),
                "Units" => chartData.Sum(p => p.UnitsSold),
                "AvgCheck" => chartData.Count > 0 ? (decimal)chartData.Average(p => p.Value) : 0,
                _ => chartData.Sum(p => p.Revenue)
            };

            string totalFormatted = _analyticsMetric switch
            {
                "Orders" => $"{totalMetricVal:N0} заказов",
                "Units" => $"{totalMetricVal:N0} шт",
                "AvgCheck" => $"{totalMetricVal:N0}₽",
                _ => $"{totalMetricVal:N0}₽"
            };

            if (ChartSubtitleTxt != null)
                ChartSubtitleTxt.Text = $"Период: {start:dd.MM.yyyy} — {end:dd.MM.yyyy} • {chartData.Count} {GetIntervalWord(chartData.Count)}";
            if (ChartQuickSummaryTxt != null)
                ChartQuickSummaryTxt.Text = $"Итого: {totalFormatted}";

            double paddingLeft = 70;
            double paddingRight = 30;
            double paddingTop = 25;
            double paddingBottom = 45;

            double graphWidth = canvasWidth - paddingLeft - paddingRight;
            double graphHeight = canvasHeight - paddingTop - paddingBottom;
            if (graphWidth < 50) graphWidth = 50;
            if (graphHeight < 50) graphHeight = 50;

            double maxValue = chartData.Max(d => d.Value);
            if (maxValue <= 0)
            {
                maxValue = (_analyticsMetric == "Orders" || _analyticsMetric == "Units") ? 5 : 10000;
            }

            double step = Math.Pow(10, Math.Floor(Math.Log10(maxValue)));
            if (step < 1) step = 1;
            if (maxValue / step < 3) step /= 2;
            double yAxisMax = Math.Ceiling(maxValue / step) * step;
            if (yAxisMax <= 0) yAxisMax = 10;

            int gridLineCount = 4;
            Brush gridBrush = (Brush)Application.Current.Resources["ThemeBorderLight"];
            Brush textBrush = (Brush)Application.Current.Resources["ThemeTextSecondary"];

            // Горизонтальная сетка
            for (int i = 0; i < gridLineCount; i++)
            {
                double ratio = (double)i / (gridLineCount - 1);
                double y = paddingTop + graphHeight * (1.0 - ratio);
                double labelValue = yAxisMax * ratio;

                var line = new System.Windows.Shapes.Line
                {
                    X1 = paddingLeft,
                    Y1 = y,
                    X2 = paddingLeft + graphWidth,
                    Y2 = y,
                    Stroke = gridBrush,
                    StrokeThickness = 1,
                    StrokeDashArray = new DoubleCollection { 4, 4 }
                };
                SalesChartCanvas.Children.Add(line);

                string yLabelStr = (_analyticsMetric == "Orders" || _analyticsMetric == "Units")
                    ? $"{labelValue:N0}"
                    : $"{labelValue:N0}₽";

                var label = new TextBlock
                {
                    Text = yLabelStr,
                    Foreground = textBrush,
                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right,
                    Width = paddingLeft - 10
                };
                Canvas.SetLeft(label, 0);
                Canvas.SetTop(label, y - 8);
                SalesChartCanvas.Children.Add(label);
            }

            var accentBrush = (Brush)Application.Current.Resources["ThemeAccent"];
            Color accentColor = accentBrush is SolidColorBrush scb ? scb.Color : Color.FromRgb(37, 99, 235);
            Brush cardBgBrush = (Brush)Application.Current.Resources["ThemeCardBg"];

            if (_analyticsChartType == "Bar")
            {
                // Рендеринг столбчатой диаграммы (Bar Chart)
                double slotWidth = graphWidth / chartData.Count;
                double barWidth = Math.Max(10, Math.Min(48, slotWidth * 0.65));

                for (int i = 0; i < chartData.Count; i++)
                {
                    var item = chartData[i];
                    double barHeight = graphHeight * (item.Value / yAxisMax);
                    if (barHeight < 3 && item.Value > 0) barHeight = 3;

                    double x = paddingLeft + (i + 0.5) * slotWidth - barWidth / 2;
                    double y = paddingTop + graphHeight - barHeight;

                    item.X = x + barWidth / 2;
                    item.Y = y;
                    item.BarWidth = barWidth;
                    item.BarHeight = barHeight;

                    // Фон столбца (тень)
                    var bgSlot = new System.Windows.Shapes.Rectangle
                    {
                        Width = barWidth,
                        Height = graphHeight,
                        RadiusX = 5,
                        RadiusY = 5,
                        Fill = (Brush)Application.Current.Resources["ThemePanelBg"],
                        Opacity = 0.3
                    };
                    Canvas.SetLeft(bgSlot, x);
                    Canvas.SetTop(bgSlot, paddingTop);
                    SalesChartCanvas.Children.Add(bgSlot);

                    // Сам столбец с градиентом
                    var barGradient = new LinearGradientBrush
                    {
                        StartPoint = new Point(0.5, 0),
                        EndPoint = new Point(0.5, 1)
                    };
                    barGradient.GradientStops.Add(new GradientStop(Color.FromArgb(240, 56, 189, 248), 0.0));
                    barGradient.GradientStops.Add(new GradientStop(Color.FromArgb(200, accentColor.R, accentColor.G, accentColor.B), 1.0));

                    var bar = new System.Windows.Shapes.Rectangle
                    {
                        Width = barWidth,
                        Height = Math.Max(2, barHeight),
                        RadiusX = 5,
                        RadiusY = 5,
                        Fill = barGradient,
                        Cursor = Cursors.Hand
                    };

                    Canvas.SetLeft(bar, x);
                    Canvas.SetTop(bar, y);
                    SalesChartCanvas.Children.Add(bar);

                    // Значение над столбцом, если помещается
                    if (item.Value > 0 && barWidth >= 20)
                    {
                        string valText = (_analyticsMetric == "Orders" || _analyticsMetric == "Units")
                            ? $"{item.Value:N0}"
                            : $"{item.Value / 1000:0.#}k";

                        var topLabel = new TextBlock
                        {
                            Text = valText,
                            Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                            FontSize = 10,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Width = barWidth + 20
                        };
                        Canvas.SetLeft(topLabel, x - 10);
                        Canvas.SetTop(topLabel, Math.Max(0, y - 16));
                        SalesChartCanvas.Children.Add(topLabel);
                    }

                    // Подпись оси X
                    bool showLabel = chartData.Count <= 16 || i % Math.Ceiling(chartData.Count / 14.0) == 0 || i == chartData.Count - 1;
                    if (showLabel)
                    {
                        var xLabel = new TextBlock
                        {
                            Text = item.Label,
                            Foreground = textBrush,
                            FontSize = 11,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Width = Math.Max(50, slotWidth)
                        };
                        Canvas.SetLeft(xLabel, x + barWidth / 2 - Math.Max(50, slotWidth) / 2);
                        Canvas.SetTop(xLabel, paddingTop + graphHeight + 10);
                        SalesChartCanvas.Children.Add(xLabel);
                    }

                    _currentChartPoints.Add(item);
                }
            }
            else
            {
                // Рендеринг линейного графика с градиентной заливкой (Area Chart)
                var points = new List<Point>();
                for (int i = 0; i < chartData.Count; i++)
                {
                    double x = chartData.Count > 1
                        ? paddingLeft + graphWidth * ((double)i / (chartData.Count - 1))
                        : paddingLeft + graphWidth / 2;

                    double y = paddingTop + graphHeight * (1.0 - chartData[i].Value / yAxisMax);

                    chartData[i].X = x;
                    chartData[i].Y = y;
                    points.Add(new Point(x, y));
                    _currentChartPoints.Add(chartData[i]);
                }

                if (points.Count > 1)
                {
                    // Область заливки
                    var areaPoints = new PointCollection();
                    areaPoints.Add(new Point(points.First().X, paddingTop + graphHeight));
                    foreach (var pt in points)
                    {
                        areaPoints.Add(pt);
                    }
                    areaPoints.Add(new Point(points.Last().X, paddingTop + graphHeight));

                    var areaPolygon = new System.Windows.Shapes.Polygon
                    {
                        Points = areaPoints
                    };

                    var areaGradient = new LinearGradientBrush
                    {
                        StartPoint = new Point(0.5, 0),
                        EndPoint = new Point(0.5, 1)
                    };
                    areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(90, accentColor.R, accentColor.G, accentColor.B), 0.0));
                    areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0, accentColor.R, accentColor.G, accentColor.B), 1.0));

                    areaPolygon.Fill = areaGradient;
                    SalesChartCanvas.Children.Add(areaPolygon);

                    // Линия графика
                    var linePoints = new PointCollection();
                    foreach (var pt in points)
                    {
                        linePoints.Add(pt);
                    }

                    var polyline = new System.Windows.Shapes.Polyline
                    {
                        Points = linePoints,
                        Stroke = accentBrush,
                        StrokeThickness = 3,
                        StrokeLineJoin = PenLineJoin.Round
                    };
                    SalesChartCanvas.Children.Add(polyline);
                }

                // Точки и подписи
                for (int i = 0; i < chartData.Count; i++)
                {
                    var pt = points[i];
                    var dataItem = chartData[i];

                    bool showLabel = chartData.Count <= 16 || i % Math.Ceiling(chartData.Count / 14.0) == 0 || i == chartData.Count - 1;
                    if (showLabel)
                    {
                        var xLabel = new TextBlock
                        {
                            Text = dataItem.Label,
                            Foreground = textBrush,
                            FontSize = 11,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            Width = 60
                        };
                        Canvas.SetLeft(xLabel, pt.X - 30);
                        Canvas.SetTop(xLabel, paddingTop + graphHeight + 10);
                        SalesChartCanvas.Children.Add(xLabel);
                    }

                    var dot = new System.Windows.Shapes.Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = cardBgBrush,
                        Stroke = accentBrush,
                        StrokeThickness = 2.5,
                        Cursor = Cursors.Hand
                    };

                    Canvas.SetLeft(dot, pt.X - 5);
                    Canvas.SetTop(dot, pt.Y - 5);
                    SalesChartCanvas.Children.Add(dot);
                }
            }
        }

        private void SalesChartCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentChartPoints.Count == 0 || SalesChartCanvas == null) return;

            Point mousePos = e.GetPosition(SalesChartCanvas);

            // Ищем ближайшую точку по оси X
            ChartDataPoint? closest = _currentChartPoints
                .OrderBy(p => Math.Abs(p.X - mousePos.X))
                .FirstOrDefault();

            if (closest == null) return;

            double canvasHeight = SalesChartCanvas.ActualHeight;
            if (canvasHeight < 50) canvasHeight = 300;

            // Вертикальная линия трекинга (crosshair)
            if (_chartHoverLine == null)
            {
                _chartHoverLine = new System.Windows.Shapes.Line
                {
                    Stroke = (Brush)Application.Current.Resources["ThemeAccent"],
                    StrokeThickness = 1.5,
                    StrokeDashArray = new DoubleCollection { 3, 3 },
                    Opacity = 0.8
                };
            }

            if (!SalesChartCanvas.Children.Contains(_chartHoverLine))
            {
                SalesChartCanvas.Children.Add(_chartHoverLine);
            }

            _chartHoverLine.X1 = closest.X;
            _chartHoverLine.Y1 = 20;
            _chartHoverLine.X2 = closest.X;
            _chartHoverLine.Y2 = canvasHeight - 35;

            // Подсвеченная точка
            if (_chartHoverDot == null)
            {
                _chartHoverDot = new System.Windows.Shapes.Ellipse
                {
                    Width = 14,
                    Height = 14,
                    Fill = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                    Stroke = Brushes.White,
                    StrokeThickness = 2.5
                };
            }

            if (!SalesChartCanvas.Children.Contains(_chartHoverDot))
            {
                SalesChartCanvas.Children.Add(_chartHoverDot);
            }

            Canvas.SetLeft(_chartHoverDot, closest.X - 7);
            Canvas.SetTop(_chartHoverDot, closest.Y - 7);

            // Плавающий бейдж (HUD)
            if (_chartHoverBadge == null)
            {
                _chartHoverBadge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(230, 15, 23, 42)),
                    BorderBrush = (Brush)Application.Current.Resources["ThemeBorderLight"],
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12, 8, 12, 8),
                    IsHitTestVisible = false
                };
            }

            var badgeStack = new StackPanel();
            badgeStack.Children.Add(new TextBlock
            {
                Text = closest.FullPeriodLabel,
                Foreground = (Brush)Application.Current.Resources["ThemeTextSecondary"],
                FontSize = 11,
                FontWeight = FontWeights.Medium,
                Margin = new Thickness(0, 0, 0, 4)
            });

            string metricValueStr = _analyticsMetric switch
            {
                "Orders" => $"{closest.OrdersCount} заказов",
                "Units" => $"{closest.UnitsSold} товаров",
                "AvgCheck" => $"{closest.AvgCheck:N0}₽",
                _ => $"{closest.Revenue:N0}₽"
            };

            badgeStack.Children.Add(new TextBlock
            {
                Text = metricValueStr,
                Foreground = Brushes.White,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            });

            badgeStack.Children.Add(new TextBlock
            {
                Text = $"Выручка: {closest.Revenue:N0}₽ • Заказов: {closest.OrdersCount} • Товаров: {closest.UnitsSold} шт",
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                FontSize = 11
            });

            _chartHoverBadge.Child = badgeStack;

            if (!SalesChartCanvas.Children.Contains(_chartHoverBadge))
            {
                SalesChartCanvas.Children.Add(_chartHoverBadge);
            }

            double badgeX = closest.X + 15;
            double badgeY = Math.Max(10, closest.Y - 40);

            // Если бейдж выходит за правую границу
            if (badgeX + 190 > SalesChartCanvas.ActualWidth)
            {
                badgeX = closest.X - 200;
            }

            Canvas.SetLeft(_chartHoverBadge, Math.Max(10, badgeX));
            Canvas.SetTop(_chartHoverBadge, badgeY);
        }

        private void SalesChartCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (SalesChartCanvas == null) return;

            if (_chartHoverLine != null)
                SalesChartCanvas.Children.Remove(_chartHoverLine);
            if (_chartHoverDot != null)
                SalesChartCanvas.Children.Remove(_chartHoverDot);
            if (_chartHoverBadge != null)
                SalesChartCanvas.Children.Remove(_chartHoverBadge);
        }

        private static string GetOrdersWord(int n)
        {
            int mod100 = n % 100;
            int mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 19) return "заказов";
            if (mod10 == 1) return "заказ";
            if (mod10 >= 2 && mod10 <= 4) return "заказа";
            return "заказов";
        }

        private static string GetItemsWord(int n)
        {
            int mod100 = n % 100;
            int mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 19) return "позиций";
            if (mod10 == 1) return "позиция";
            if (mod10 >= 2 && mod10 <= 4) return "позиции";
            return "позиций";
        }

        private static string GetIntervalWord(int n)
        {
            int mod100 = n % 100;
            int mod10 = n % 10;
            if (mod100 >= 11 && mod100 <= 19) return "интервалов";
            if (mod10 == 1) return "интервал";
            if (mod10 >= 2 && mod10 <= 4) return "интервала";
            return "интервалов";
        }

        private void SalesChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_currentTab == "Analytics")
            {
                DrawSalesChart();
            }
        }

        
        private void FilterProducts()
        {
            if (ProductSearchInput == null || ProductsTableListBox == null) return;

            string query = ProductSearchInput.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                ProductsTableListBox.ItemsSource = null;
                ProductsTableListBox.ItemsSource = _allProducts;
            }
            else
            {
                var filtered = _allProducts.Where(p => p.Title.ToLower().Contains(query) || p.Manufacturer.ToLower().Contains(query)).ToList();
                ProductsTableListBox.ItemsSource = null;
                ProductsTableListBox.ItemsSource = filtered;
            }
        }

        private void FilterSales()
        {
            if (SalesSearchInput == null || SalesTableListBox == null || SalesStatusFilter == null) return;

            string query = SalesSearchInput.Text.Trim().ToLower();
            var filtered = _allReceipts;

            
            if (SalesStatusFilter.SelectedItem is ComboBoxItem selectedItem)
            {
                string statusText = selectedItem.Content?.ToString() ?? "";
                if (statusText != "Все статусы")
                {
                    filtered = filtered.Where(r => r.StatusNavigation?.Title == statusText).ToList();
                }
            }

            
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(r => r.ReceiptId.ToString().Contains(query) || 
                                             (r.User != null && r.User.Name != null && r.User.Name.ToLower().Contains(query))).ToList();
            }

            SalesTableListBox.ItemsSource = null;
            SalesTableListBox.ItemsSource = filtered;
        }

        private void FilterUsers()
        {
            if (UserSearchInput == null || UsersTableListBox == null) return;

            string query = UserSearchInput.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(query))
            {
                UsersTableListBox.ItemsSource = null;
                UsersTableListBox.ItemsSource = _allUsers;
            }
            else
            {
                var filtered = _allUsers.Where(u => (u.Name != null && u.Name.ToLower().Contains(query)) || 
                                                 (u.Email != null && u.Email.ToLower().Contains(query)) ||
                                                 (u.Phone != null && u.Phone.Contains(query))).ToList();
                UsersTableListBox.ItemsSource = null;
                UsersTableListBox.ItemsSource = filtered;
            }
        }

        private void ProductSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterProducts();
        private void SalesSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterSales();
        private void UserSearch_TextChanged(object sender, TextChangedEventArgs e) => FilterUsers();
        
        private void SalesStatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => FilterSales();

        
        private void ReceiptNo_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBlock tb && tb.DataContext is Receipt receipt)
            {
                
                string customerName = receipt.User?.Name ?? "Покупатель";
                string address = string.IsNullOrEmpty(receipt.Adress) ? "Самовывоз" : receipt.Adress;
                MessageBox.Show($"Квитанция: #TF-2024-{receipt.ReceiptId:D5}\nПокупатель: {customerName}\nСумма: {receipt.TotalPrice:N0}₽\nАдрес доставки: {address}", "Детали заказа", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        
        private async void OrderStatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo && combo.DataContext is Receipt receipt && combo.SelectedValue != null)
            {
                if (int.TryParse(combo.SelectedValue.ToString(), out int newStatusId))
                {
                    if (receipt.Status == newStatusId) return;

                    try
                    {
                        var response = await _client.PutAsJsonAsync($"api/Receipts/update_order_status/{receipt.ReceiptId}", newStatusId);
                        if (response.IsSuccessStatusCode)
                        {
                            receipt.Status = newStatusId;
                            
                            var receipts = await _client.GetFromJsonAsync<List<Receipt>>("api/Receipts/get_all_receipts");
                            if (receipts != null)
                            {
                                _allReceipts = receipts.Where(r => r.OrderStatus > 0).ToList();
                            }
                            UpdateDashboardStats();
                            FilterSales();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось обновить статус заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка соединения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        
        private void AddProductBtn_Click(object sender, RoutedEventArgs e)
        {
            _editingProduct = null;
            ModalTitle.Text = "Добавить новый продукт";
            ModalSaveBtnText.Text = "Добавить продукт";
            ModalSaveBtn.Visibility = Visibility.Visible;

            ProductModalBorder.Width = 550;
            VariationsColumn.Width = new GridLength(0);
            VariationsPanel.Visibility = Visibility.Collapsed;

            ModalNameInput.IsEnabled = true;
            ModalNameInput.Text = "";
            ModalManufacturerInput.IsEnabled = true;
            ModalManufacturerInput.Text = "";
            ModalCategoryCombo.IsEnabled = true;
            ModalCategoryCombo.SelectedIndex = 0;
            ModalPriceInput.IsEnabled = true;
            ModalPriceInput.Text = "0,00";
            ModalStockInput.IsEnabled = true;
            ModalStockInput.Text = "0";
            ModalImageInput.IsEnabled = true;
            ModalImageInput.Text = "";
            ModalDescInput.IsEnabled = true;
            ModalDescInput.Text = "";

            ModalOverlay.Visibility = Visibility.Visible;
        }

        private void EditProductBtn_Click(object sender, RoutedEventArgs e)
        {
            Product? prod = null;
            if (sender is Button btn && btn.DataContext is Product p)
            {
                prod = p;
            }
            else if (ProductsTableListBox.SelectedItem is Product selected)
            {
                prod = selected;
            }

            if (prod == null) return;

            _editingProduct = prod;
            ModalTitle.Text = "Редактировать продукт";
            ModalSaveBtnText.Text = "Сохранить изменения";
            ModalSaveBtn.Visibility = Visibility.Visible;

            ProductModalBorder.Width = 925;
            VariationsColumn.Width = new GridLength(400);
            VariationsPanel.Visibility = Visibility.Visible;
            VarNameInput.Text = "";
            VarPriceInput.Text = "";
            VarStockInput.Text = "0";

            ModalNameInput.IsEnabled = true;
            ModalNameInput.Text = prod.Title;
            ModalManufacturerInput.IsEnabled = true;
            ModalManufacturerInput.Text = prod.Manufacturer;
            ModalCategoryCombo.IsEnabled = true;
            ModalCategoryCombo.SelectedValue = prod.Category;
            ModalPriceInput.IsEnabled = true;
            ModalPriceInput.Text = $"{prod.Price:F2}";
            ModalStockInput.IsEnabled = true;
            ModalStockInput.Text = prod.Stock.ToString();
            ModalImageInput.IsEnabled = true;
            ModalImageInput.Text = prod.Photo;
            ModalDescInput.IsEnabled = true;
            ModalDescInput.Text = prod.Description;

            LoadVariationsForEditingProduct();

            ModalOverlay.Visibility = Visibility.Visible;
        }

        private async void LoadVariationsForEditingProduct()
        {
            if (_editingProduct == null) return;
            try
            {
                var vars = await _client.GetFromJsonAsync<List<ProductVariation>>($"api/Products/{_editingProduct.Articul}/variations");
                VariationsListBox.ItemsSource = vars;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private async void AddVariation_Click(object sender, RoutedEventArgs e)
        {
            if (_editingProduct == null) return;

            string name = VarNameInput.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название вариации!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(VarPriceInput.Text.Replace(".", ","), out decimal price) &&
                !decimal.TryParse(VarPriceInput.Text.Replace(",", "."), out price))
            {
                MessageBox.Show("Введите корректную цену вариации!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(VarStockInput.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Введите корректное количество!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var newVar = new ProductVariation
            {
                ProductId = _editingProduct.Articul,
                Name = name,
                Price = price,
                Stock = stock
            };

            try
            {
                var response = await _client.PostAsJsonAsync("api/Products/variations", newVar);
                if (response.IsSuccessStatusCode)
                {
                    VarNameInput.Text = "";
                    VarPriceInput.Text = "";
                    VarStockInput.Text = "0";
                    LoadVariationsForEditingProduct();
                }
                else
                {
                    MessageBox.Show("Не удалось добавить вариацию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка соединения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteVariation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null && int.TryParse(btn.Tag.ToString(), out int variationId))
            {
                var result = MessageBox.Show("Удалить эту вариацию?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await _client.DeleteAsync($"api/Products/variations/{variationId}");
                        if (response.IsSuccessStatusCode)
                        {
                            LoadVariationsForEditingProduct();
                        }
                        else
                        {
                            MessageBox.Show("Не удалось удалить вариацию.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ViewProductBtn_Click(object sender, RoutedEventArgs e)
        {
            Product? prod = null;
            if (sender is Button btn && btn.DataContext is Product p)
            {
                prod = p;
            }
            else if (ProductsTableListBox.SelectedItem is Product selected)
            {
                prod = selected;
            }

            if (prod == null) return;

            ModalTitle.Text = "Просмотр продукта";
            ModalSaveBtn.Visibility = Visibility.Collapsed;

            ProductModalBorder.Width = 550;
            VariationsColumn.Width = new GridLength(0);
            VariationsPanel.Visibility = Visibility.Collapsed;

            ModalNameInput.IsEnabled = false;
            ModalNameInput.Text = prod.Title;
            ModalManufacturerInput.IsEnabled = false;
            ModalManufacturerInput.Text = prod.Manufacturer;
            ModalCategoryCombo.IsEnabled = false;
            ModalCategoryCombo.SelectedValue = prod.Category;
            ModalPriceInput.IsEnabled = false;
            ModalPriceInput.Text = $"{prod.Price:F2}";
            ModalStockInput.IsEnabled = false;
            ModalStockInput.Text = prod.Stock.ToString();
            ModalImageInput.IsEnabled = false;
            ModalImageInput.Text = prod.Photo;
            ModalDescInput.IsEnabled = false;
            ModalDescInput.Text = prod.Description;

            ModalOverlay.Visibility = Visibility.Visible;
        }

        private async void DeleteProductBtn_Click(object sender, RoutedEventArgs e)
        {
            Product? prod = null;
            if (sender is Button btn && btn.DataContext is Product p)
            {
                prod = p;
            }
            else if (ProductsTableListBox.SelectedItem is Product selected)
            {
                prod = selected;
            }

            if (prod == null) return;

            var result = MessageBox.Show($"Вы действительно хотите удалить продукт '{prod.Title}'?", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var response = await _client.DeleteAsync($"api/Products/{prod.Articul}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Продукт успешно удален!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        await LoadProducts();
                        UpdateDashboardStats();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить продукт.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка соединения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseModalBtn_Click(object sender, RoutedEventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        private void ModalNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int length = ModalNameInput.Text.Length;
            NameCharCount.Text = $"{length}/60";
            NameCharCount.Foreground = length >= 60 ? Brushes.Red : new SolidColorBrush(Color.FromRgb(248, 113, 113));
        }

        
        private void SpinnerMinus_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ModalStockInput.Text, out int qty) && qty > 0)
            {
                ModalStockInput.Text = (qty - 1).ToString();
            }
        }

        private void SpinnerPlus_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(ModalStockInput.Text, out int qty))
            {
                ModalStockInput.Text = (qty + 1).ToString();
            }
            else
            {
                ModalStockInput.Text = "1";
            }
        }

        
        private async void SaveModalProductBtn_Click(object sender, RoutedEventArgs e)
        {
            
            string title = ModalNameInput.Text.Trim();
            string manufacturer = ModalManufacturerInput.Text.Trim();
            
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(manufacturer) || ModalCategoryCombo.SelectedValue == null)
            {
                MessageBox.Show("Заполните обязательные поля (*)!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(ModalPriceInput.Text.Replace(".", ","), out decimal price) && 
                !decimal.TryParse(ModalPriceInput.Text.Replace(",", "."), out price))
            {
                MessageBox.Show("Введите корректную цену товара!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ModalStockInput.Text, out int stock) || stock < 0)
            {
                MessageBox.Show("Введите корректное количество товара!", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int categoryId = (int)ModalCategoryCombo.SelectedValue;
            string photo = string.IsNullOrWhiteSpace(ModalImageInput.Text) ? "default.png" : ModalImageInput.Text.Trim();
            string description = ModalDescInput.Text.Trim();

            if (_editingProduct == null)
            {
                
                var newProduct = new Product
                {
                    Title = title,
                    Manufacturer = manufacturer,
                    Category = categoryId,
                    Price = price,
                    Stock = stock,
                    Photo = photo,
                    Description = description
                };

                try
                {
                    var response = await _client.PostAsJsonAsync("api/Products", newProduct);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Продукт успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ModalOverlay.Visibility = Visibility.Collapsed;
                        await LoadProducts();
                        UpdateDashboardStats();
                    }
                    else
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка сервера: {err}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка соединения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                
                _editingProduct.Title = title;
                _editingProduct.Manufacturer = manufacturer;
                _editingProduct.Category = categoryId;
                _editingProduct.Price = price;
                _editingProduct.Stock = stock;
                _editingProduct.Photo = photo;
                _editingProduct.Description = description;

                try
                {
                    var response = await _client.PutAsJsonAsync($"api/Products/{_editingProduct.Articul}", _editingProduct);
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Изменения успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        ModalOverlay.Visibility = Visibility.Collapsed;
                        await LoadProducts();
                        UpdateDashboardStats();
                    }
                    else
                    {
                        string err = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Ошибка сервера: {err}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка соединения: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы действительно хотите выйти из системы?", "Выход", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                MainWindow loginWindow = new MainWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void ThemeToggle_Click(object sender, RoutedEventArgs e)
        {
            App.SetTheme(!App.IsLightTheme);
            UpdateThemeIcon();
            if (_currentTab == "Analytics")
            {
                UpdateAnalytics();
            }
        }

        private void UpdateThemeIcon()
        {
            if (ThemeIconPath == null) return;
            if (App.IsLightTheme)
            {
                ThemeIconPath.Data = Geometry.Parse("M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,2A1,1 0 0,1 13,3V5A1,1 0 0,1 12,6A1,1 0 0,1 11,5V3A1,1 0 0,1 12,2M12,18A1,1 0 0,1 13,19V21A1,1 0 0,1 12,22A1,1 0 0,1 11,21V19A1,1 0 0,1 12,18M20,11A1,1 0 0,1 21,12V13H19A1,1 0 0,1 18,12A1,1 0 0,1 19,11H21M6,11A1,1 0 0,1 7,12A1,1 0 0,1 6,13H4A1,1 0 0,1 3,12A1,1 0 0,1 4,11H6M18.36,5.64A1,1 0 0,1 18.36,7.05L16.95,8.46A1,1 0 0,1 15.54,8.46A1,1 0 0,1 15.54,7.05L16.95,5.64A1,1 0 0,1 18.36,5.64M7.05,16.95A1,1 0 0,1 7.05,18.36L5.64,19.77A1,1 0 0,1 4.23,19.77A1,1 0 0,1 4.23,18.36L5.64,17.05A1,1 0 0,1 7.05,16.95M18.36,18.36A1,1 0 0,1 16.95,19.77L15.54,18.36A1,1 0 0,1 15.54,16.95A1,1 0 0,1 16.95,16.95L18.36,18.36M7.05,5.64A1,1 0 0,1 8.46,7.05L7.05,8.46A1,1 0 0,1 5.64,8.46A1,1 0 0,1 5.64,7.05L7.05,5.64Z");
            }
            else
            {
                ThemeIconPath.Data = Geometry.Parse("M12,18C11.11,18 10.26,17.8 9.5,17.45C11.56,16.5 13,14.42 13,12C13,9.58 11.56,7.5 9.5,6.55C10.26,6.2 11.11,6 12,6A6,6 0 0,1 18,12A6,6 0 0,1 12,18M20,8.69V4H15.31L12,0.69L8.69,4H4V8.69L0.69,12L4,15.31V20H8.69L12,23.31L15.31,20H20V15.31L23.31,12L20,8.69Z");
            }
        }
    }
}
