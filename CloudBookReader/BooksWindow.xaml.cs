using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CloudBookReader.Models;
using CloudBookReader.Services;

namespace CloudBookReader
{
    public partial class BooksWindow : Window
    {
        private string userId;
        private List<BookItem> allBooks = new List<BookItem>();

        public BooksWindow(string userId)
        {
            InitializeComponent();
            this.userId = userId;
            UserTextBlock.Text = "Logged in as: " + userId;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBooksAsync();
        }

        private async Task LoadBooksAsync()
        {
            StatusTextBlock.Text = "Loading books...";
            try
            {
                DynamoDbService db = new DynamoDbService();
                allBooks = await db.GetBooksAsync(userId);
                ShowBooks();
            }
            catch (Exception)
            {
                allBooks = new List<BookItem>();
                BooksListBox.Items.Clear();
                StatusTextBlock.Text = "Could not load books.";
                MessageBox.Show("Could not load books. Please check your AWS profile and internet connection.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowBooks()
        {
            string search = SearchTextBox.Text.Trim().ToLowerInvariant();
            BooksListBox.Items.Clear();

            foreach (BookItem book in allBooks)
            {
                string title = (book.Title ?? "").ToLowerInvariant();
                string author = (book.Author ?? "").ToLowerInvariant();

                if (search == "" || title.Contains(search) || author.Contains(search))
                {
                    BooksListBox.Items.Add(book);
                }
            }

            StatusTextBlock.Text = BooksListBox.Items.Count + " book(s) found.";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ShowBooks();
        }

        private void OpenSelectedBook()
        {
            BookItem? book = BooksListBox.SelectedItem as BookItem;
            if (book == null)
            {
                MessageBox.Show("Please select a book first.", "Cloud Book Reader");
                return;
            }

            try
            {
                ReaderWindow reader = new ReaderWindow(book) { Owner = this };
                reader.ShowDialog();
            }
            catch (Exception)
            {
                MessageBox.Show("Could not open the book.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            allBooks = allBooks.OrderByDescending(b => b.BookmarkTime, StringComparer.Ordinal).ToList();
            ShowBooks();

            if (BooksListBox.Items.Count > 0)
            {
                BooksListBox.SelectedIndex = 0;
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenSelectedBook();
        }

        private void BooksListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsControl.ContainerFromElement(BooksListBox, (DependencyObject)e.OriginalSource) is ListBoxItem)
            {
                OpenSelectedBook();
            }
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadBooksAsync();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow login = new MainWindow();
            login.Show();
            this.Close();
        }
    }
}
