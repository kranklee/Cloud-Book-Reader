using System.ComponentModel;
using System.IO;
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
        private bool windowClosed;
        private bool saving;

        public ReaderWindow(BookItem book)
        {
            InitializeComponent();

            if (PdfViewer.ToolbarSettings == null)
            {
                PdfViewer.ToolbarSettings = new Syncfusion.Windows.PdfViewer.PdfViewerToolbarSettings();
            }
            PdfViewer.ToolbarSettings.ShowFileTools = false;

            this.book = book;
            Title = "Cloud Book Reader - " + book.Title;
            BookInfoText.Text = book.Title + " by " + book.Author;
            BookInfoText.ToolTip = BookInfoText.Text;
            StatusText.Text = "Saved page: " + book.CurrentPage;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Loading book...";
                S3BookService s3 = new S3BookService();
                MemoryStream stream = await s3.GetBookStreamAsync(book.S3Key);

                if (windowClosed)
                {
                    stream.Dispose();
                    return;
                }

                bookStream = stream;
                bookStream.Position = 0;
                PdfViewer.Load(bookStream);
            }
            catch
            {
                if (windowClosed)
                {
                    return;
                }

                MessageBox.Show("The book could not be loaded from Amazon S3.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private void PdfViewer_DocumentLoaded(object sender, EventArgs args)
        {
            documentLoaded = true;
            BookmarkButton.IsEnabled = true;

            if (!firstPageJumpDone)
            {
                firstPageJumpDone = true;
                if (book.CurrentPage > 1 && book.CurrentPage <= PdfViewer.PageCount)
                {
                    PdfViewer.GotoPage(book.CurrentPage);
                }
            }

            StatusText.Text = "Saved page: " + book.CurrentPage + " of " + PdfViewer.PageCount;
        }

        private int GetCurrentPage()
        {
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
            int totalPages = PdfViewer.PageCount;
            await dynamo.SaveBookmarkAsync(book, page, totalPages);
            StatusText.Text = "Bookmark saved on page " + page + " of " + totalPages + ".";
        }

        private async void BookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!documentLoaded || saving)
            {
                return;
            }

            saving = true;
            BookmarkButton.IsEnabled = false;
            try
            {
                await SaveBookmark();
            }
            catch
            {
                MessageBox.Show("The bookmark could not be saved.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                saving = false;
            }
            BookmarkButton.IsEnabled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (saving)
            {
                e.Cancel = true;
                return;
            }

            if (closeSaveDone || !documentLoaded)
            {
                return;
            }

            e.Cancel = true;
            closeSaveDone = true;
            BookmarkButton.IsEnabled = false;
            CloseButton.IsEnabled = false;
            StatusText.Text = "Saving bookmark...";

            saving = true;
            try
            {
                await SaveBookmark();
            }
            catch
            {
                MessageBox.Show("The bookmark could not be saved.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                saving = false;
            }

            Close();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            windowClosed = true;

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
