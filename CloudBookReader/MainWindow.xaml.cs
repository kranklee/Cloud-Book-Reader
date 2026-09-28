using System;
using System.Windows;
using CloudBookReader.Services;

namespace CloudBookReader
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string userId = UserIdTextBox.Text.Trim();
            string password = PasswordInput.Password;

            if (userId == "" || password == "")
            {
                MessageTextBlock.Text = "Please enter your user ID and password.";
                return;
            }

            LoginButton.IsEnabled = false;
            MessageTextBlock.Text = "Checking...";

            bool valid;
            try
            {
                DynamoDbService db = new DynamoDbService();
                valid = await db.ValidateUserAsync(userId, password);
            }
            catch (Exception)
            {
                MessageTextBlock.Text = "Could not connect to AWS. Please check your AWS profile and internet connection.";
                LoginButton.IsEnabled = true;
                return;
            }

            if (valid)
            {
                BooksWindow booksWindow = new BooksWindow(userId);
                booksWindow.Show();
                this.Close();
            }
            else
            {
                MessageTextBlock.Text = "Invalid user ID or password.";
                LoginButton.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            UserIdTextBox.Text = "";
            PasswordInput.Password = "";
            MessageTextBlock.Text = "";
        }
    }
}
