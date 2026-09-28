using System.ComponentModel;
using System.Windows;
using CloudBookReader.Models;
using CloudBookReader.Services;

namespace CloudBookReader
{
    public partial class ReaderWindow : Window
    {
        private readonly BookItem book;
        private readonly DynamoDbService dynamo = new DynamoDbService();
        private MemoryStream? bookStream;
        private bool documentLoaded;
        private bool firstPageJumpDone;
        private bool closeSaveDone;

        public ReaderWindow(BookItem book)
        {
            InitializeComponent();
            this.book = book;
            Title = "Cloud Book Reader - " + book.Title;
            BookInfoText.Text = book.Title + " by " + book.Author;
            StatusText.Text = "Saved page: " + book.CurrentPage;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Loading book...";
                S3BookService s3 = new S3BookService();
                bookStream = await s3.GetBookStreamAsync(book.S3Key);
                bookStream.Position = 0;
                PdfViewer.Load(bookStream);
            }
            catch
            {
                MessageBox.Show("The book could not be loaded from Amazon S3.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
        {
            documentLoaded = true;
            SaveBookmarkButton.IsEnabled = true;

            if (!firstPageJumpDone)
            {
                firstPageJumpDone = true;
                if (book.CurrentPage > 1 && book.CurrentPage <= PdfViewer.PageCount)
                {
                    PdfViewer.GotoPage(book.CurrentPage);
                }
            }

            StatusText.Text = "Saved page: " + book.CurrentPage;
        }

        private int GetCurrentPage()
        {
            // CurrentPageIndex starts from 1
            int page = PdfViewer.CurrentPageIndex;
            if (page < 1)
            {
                page = 1;
            }
            return page;
        }

        private async Task SaveBookmark()
        {
            int page = GetCurrentPage();
            await dynamo.SaveBookmarkAsync(book, page);
            StatusText.Text = "Bookmark saved on page " + page + ".";
        }

        private async void SaveBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!documentLoaded)
            {
                return;
            }

            SaveBookmarkButton.IsEnabled = false;
            try
            {
                await SaveBookmark();
            }
            catch
            {
                MessageBox.Show("The bookmark could not be saved.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            SaveBookmarkButton.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (closeSaveDone || !documentLoaded)
            {
                return;
            }

            e.Cancel = true;
            closeSaveDone = true;
            SaveBookmarkButton.IsEnabled = false;
            CloseButton.IsEnabled = false;
            StatusText.Text = "Saving bookmark...";

            try
            {
                await SaveBookmark();
            }
            catch
            {
                MessageBox.Show("The bookmark could not be saved.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            try
            {
                PdfViewer.Unload();
            }
            catch
            {
            }

            if (bookStream != null)
            {
                bookStream.Dispose();
                bookStream = null;
            }
        }
    }
}
