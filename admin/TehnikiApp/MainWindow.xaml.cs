using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TehnikiApp.Models;

namespace TehnikiApp
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:8090/") };

        public MainWindow()
        {
            InitializeComponent();
            ApplyInputConstraints();
        }

        private bool _isPasswordVisible = false;
        private bool _isSyncing = false;

        private void PassLogin_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            if (PassLoginTxt != null)
            {
                PassLoginTxt.Text = PassLogin.Password;
            }
            _isSyncing = false;
        }

        private void PassLoginTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncing) return;
            _isSyncing = true;
            if (PassLogin != null)
            {
                PassLogin.Password = PassLoginTxt.Text;
            }
            _isSyncing = false;
        }

        private void TogglePass_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                PassLogin.Visibility = Visibility.Collapsed;
                PassLoginTxt.Visibility = Visibility.Visible;
                PassLoginTxt.Focus();
                PassLoginTxt.SelectionStart = PassLoginTxt.Text.Length;
                
                if (EyeIconPath != null)
                {
                    EyeIconPath.Data = Geometry.Parse("M2 4.27L3.27 3L21 20.73L19.73 22L16.27 18.54C14.93 19.16 13.5 19.5 12 19.5C7 19.5 2.73 16.39 1 12C1.94 9.61 3.59 7.64 5.73 6.34L2 4.27ZM12 4.5C17 4.5 21.27 7.61 23 12C22.25 13.76 21 15.26 19.46 16.38L18.02 14.94C18.64 14.07 19 13.07 19 12C19 8.13 15.87 5 12 5C10.93 5 9.93 5.36 9.06 5.98L7.62 4.54C8.94 3.91 10.42 3.5 12 3.5V4.5ZM12 7C14.76 7 17 9.24 17 12C17 12.63 15.88 14.12 15.25 14.75L9.25 8.75C9.88 8.12 11.37 7 12 7ZM12 17C10.63 17 9.12 15.88 8.49 15.25L13.75 10C13.12 10.63 12 12.12 12 12.75C12 13.38 12.63 14.5 13.25 15.12L12 17Z");
                    EyeIconPath.Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                }
            }
            else
            {
                PassLogin.Visibility = Visibility.Visible;
                PassLoginTxt.Visibility = Visibility.Collapsed;
                PassLogin.Focus();
                
                if (EyeIconPath != null)
                {
                    EyeIconPath.Data = Geometry.Parse("M12 4.5C7 4.5 2.73 7.61 1 12C2.73 16.39 7 19.5 12 19.5C17 19.5 21.27 16.39 23 12C21.27 7.61 17 4.5 12 4.5ZM12 17C9.24 17 7 14.76 7 12C7 9.24 9.24 7 12 7C14.76 7 17 9.24 17 12C17 14.76 14.76 17 12 17ZM12 9C10.34 9 9 10.34 9 12C9 13.66 10.34 15 12 15C13.66 15 15 13.66 15 12C15 10.34 13.66 9 12 9Z");
                    EyeIconPath.Fill = new SolidColorBrush(Color.FromRgb(100, 116, 139));
                }
            }
        }

        private void ApplyInputConstraints()
        {
            if (EmailLogin != null)
            {
                EmailLogin.MaxLength = 64;
                EmailLogin.KeyDown += (s, e) => { if (e.Key == Key.Space) e.Handled = true; };
                EmailLogin.TextChanged += (s, e) =>
                {
                    if (EmailLogin.Text != null && EmailLogin.Text.Contains(" "))
                    {
                        int sel = EmailLogin.SelectionStart;
                        EmailLogin.Text = EmailLogin.Text.Replace(" ", "");
                        EmailLogin.SelectionStart = Math.Min(sel, EmailLogin.Text.Length);
                    }
                };
            }

            if (PassLogin != null)
            {
                PassLogin.MaxLength = 32;
                PassLogin.KeyDown += (s, e) => { if (e.Key == Key.Space) e.Handled = true; };
            }

            if (PassLoginTxt != null)
            {
                PassLoginTxt.MaxLength = 32;
                PassLoginTxt.KeyDown += (s, e) => { if (e.Key == Key.Space) e.Handled = true; };
                PassLoginTxt.TextChanged += (s, e) =>
                {
                    if (PassLoginTxt.Text != null && PassLoginTxt.Text.Contains(" "))
                    {
                        int sel = PassLoginTxt.SelectionStart;
                        PassLoginTxt.Text = PassLoginTxt.Text.Replace(" ", "");
                        PassLoginTxt.SelectionStart = Math.Min(sel, PassLoginTxt.Text.Length);
                    }
                };
            }
        }

        private System.Collections.Generic.IEnumerable<Control> GetAllTextBoxes(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is TextBox || child is PasswordBox) yield return (Control)child;
                foreach (var recursiveChild in GetAllTextBoxes(child)) yield return recursiveChild;
            }
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailLogin.Text.Trim();
            string password = PassLogin.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль");
                return;
            }

            try
            {
                var response = await _client.PostAsync($"api/Users/login?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}", null);

                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    if (user == null)
                    {
                        MessageBox.Show("Ошибка: Получен некорректный ответ от сервера при авторизации.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    if (user.Role != null && (user.Role.Equals("admin", StringComparison.OrdinalIgnoreCase) || user.Role.Equals("Администратор", StringComparison.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show($"Добро пожаловать, {user.Name}!");
                        AdminWindow aw = new AdminWindow(user);
                        aw.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Вход только для админов");
                    }
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка подключения: " + ex.Message);
            }
        }
    }
}