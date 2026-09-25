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
                    UpdateAnalytics();
                }
            }
        }

        private void UpdateAnalytics()
        {
            if (_allReceipts == null || _allProducts == null || _categories == null) return;

            decimal totalSales = _allReceipts.Sum(r => r.TotalPrice);
            int totalOrders = _allReceipts.Count;
            decimal avgCheck = totalOrders > 0 ? totalSales / totalOrders : 0;
            CardAvgCheckTxt.Text = $"{avgCheck:N0}₽";

            int totalItemsSold = _allReceipts.SelectMany(r => r.ReceiptItems).Sum(i => i.Quantity);
            CardTotalUnitsSoldTxt.Text = $"{totalItemsSold:N0} шт";

            var categoryRevenue = _allReceipts.SelectMany(r => r.ReceiptItems)
                .Where(i => i.Product != null)
                .GroupBy(i => i.Product.Category)
                .Select(g => new { 
                    CategoryId = g.Key, 
                    Revenue = g.Sum(i => i.Quantity * i.DisplayPrice) 
                })
                .OrderByDescending(x => x.Revenue)
                .FirstOrDefault();

            if (categoryRevenue != null)
            {
                var cat = _categories.FirstOrDefault(c => c.Id == categoryRevenue.CategoryId);
                CardTopCategoryTxt.Text = cat?.Title ?? "Техника";
                CardTopCategoryRevenueTxt.Text = $"{categoryRevenue.Revenue:N0}₽ дохода";
            }
            else
            {
                CardTopCategoryTxt.Text = "Нет данных";
                CardTopCategoryRevenueTxt.Text = "0₽ дохода";
            }

            DrawSalesChart();
            PopulateCategoryBreakdown();
        }

        private void PopulateCategoryBreakdown()
        {
            if (CategoryStatsPanel == null) return;
            CategoryStatsPanel.Children.Clear();

            if (_allReceipts == null || _categories == null || _categories.Count == 0) return;

            var salesByCategory = _allReceipts.SelectMany(r => r.ReceiptItems)
                .Where(i => i.Product != null)
                .GroupBy(i => i.Product.Category)
                .Select(g => new {
                    CategoryId = g.Key,
                    CategoryName = _categories.FirstOrDefault(c => c.Id == g.Key)?.Title ?? "Другие",
                    Revenue = g.Sum(i => i.Quantity * i.DisplayPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            if (salesByCategory.Count == 0)
            {
                CategoryStatsPanel.Children.Add(new TextBlock 
                { 
                    Text = "Нет данных о продажах.", 
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

                var txtName = new TextBlock
                {
                    Text = item.CategoryName,
                    Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                    FontSize = 14,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(txtName, 0);
                labelGrid.Children.Add(txtName);

                var txtValue = new TextBlock
                {
                    Text = $"{item.Revenue:N0}₽",
                    Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                    FontSize = 14,
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

        private void DrawSalesChart()
        {
            if (SalesChartCanvas == null || _allReceipts == null) return;

            double canvasWidth = SalesChartCanvas.ActualWidth;
            double canvasHeight = SalesChartCanvas.ActualHeight;

            if (canvasWidth < 50) canvasWidth = 500;
            if (canvasHeight < 50) canvasHeight = 280;

            SalesChartCanvas.Children.Clear();

            var dates = Enumerable.Range(0, 7)
                .Select(offset => DateTime.Today.AddDays(-6 + offset))
                .ToList();

            var salesByDay = _allReceipts
                .Where(r => r.DateTime.Date >= dates.First() && r.DateTime.Date <= dates.Last())
                .GroupBy(r => r.DateTime.Date)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.TotalPrice));

            var chartData = dates.Select(date => new {
                Date = date,
                Label = date.ToString("dd.MM"),
                Value = salesByDay.ContainsKey(date) ? salesByDay[date] : 0m
            }).ToList();

            double paddingLeft = 70;
            double paddingRight = 30;
            double paddingTop = 20;
            double paddingBottom = 40;

            double graphWidth = canvasWidth - paddingLeft - paddingRight;
            double graphHeight = canvasHeight - paddingTop - paddingBottom;

            decimal maxValue = chartData.Max(d => d.Value);
            if (maxValue == 0) maxValue = 10000;

            double maxValDouble = (double)maxValue;
            double step = Math.Pow(10, Math.Floor(Math.Log10(maxValDouble)));
            if (step < 1) step = 1;
            if (maxValDouble / step < 3) step /= 2;
            double yAxisMax = Math.Ceiling(maxValDouble / step) * step;
            if (yAxisMax == 0) yAxisMax = 10000;

            int gridLineCount = 4;
            Brush gridBrush = (Brush)Application.Current.Resources["ThemeBorderLight"];
            Brush textBrush = (Brush)Application.Current.Resources["ThemeTextSecondary"];

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

                var label = new TextBlock
                {
                    Text = $"{labelValue:N0}₽",
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

            var points = new List<Point>();
            for (int i = 0; i < chartData.Count; i++)
            {
                double x = paddingLeft + graphWidth * ((double)i / (chartData.Count - 1));
                double y = paddingTop + graphHeight * (1.0 - (double)chartData[i].Value / yAxisMax);
                points.Add(new Point(x, y));
            }

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

            var accentBrush = (Brush)Application.Current.Resources["ThemeAccent"];
            Color accentColor = accentBrush is SolidColorBrush scb ? scb.Color : Color.FromRgb(37, 99, 235);

            var areaGradient = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1)
            };
            areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(85, accentColor.R, accentColor.G, accentColor.B), 0.0));
            areaGradient.GradientStops.Add(new GradientStop(Color.FromArgb(0, accentColor.R, accentColor.G, accentColor.B), 1.0));

            areaPolygon.Fill = areaGradient;
            SalesChartCanvas.Children.Add(areaPolygon);

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

            Brush cardBgBrush = (Brush)Application.Current.Resources["ThemeCardBg"];

            for (int i = 0; i < chartData.Count; i++)
            {
                var pt = points[i];
                var dataItem = chartData[i];

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

                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = cardBgBrush,
                    Stroke = accentBrush,
                    StrokeThickness = 2.5,
                    Cursor = Cursors.Hand,
                    ToolTip = new ToolTip
                    {
                        Content = $"{dataItem.Date:dd MMMM yyyy}\nВыручка: {dataItem.Value:N0}₽",
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Background = cardBgBrush,
                        Foreground = (Brush)Application.Current.Resources["ThemeTextPrimary"],
                        BorderBrush = gridBrush,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8, 5, 8, 5)
                    }
                };

                dot.MouseEnter += (s, e) => {
                    if (s is System.Windows.Shapes.Ellipse el)
                    {
                        el.Width = 14;
                        el.Height = 14;
                        Canvas.SetLeft(el, pt.X - 7);
                        Canvas.SetTop(el, pt.Y - 7);
                        el.StrokeThickness = 3.5;
                    }
                };
                dot.MouseLeave += (s, e) => {
                    if (s is System.Windows.Shapes.Ellipse el)
                    {
                        el.Width = 10;
                        el.Height = 10;
                        Canvas.SetLeft(el, pt.X - 5);
                        Canvas.SetTop(el, pt.Y - 5);
                        el.StrokeThickness = 2.5;
                    }
                };

                Canvas.SetLeft(dot, pt.X - 5);
                Canvas.SetTop(dot, pt.Y - 5);
                SalesChartCanvas.Children.Add(dot);
            }
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
