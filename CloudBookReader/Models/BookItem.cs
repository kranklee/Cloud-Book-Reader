using System.Globalization;

namespace CloudBookReader.Models
{
    public class BookItem
    {
        public string UserId { get; set; } = "";
        public string RecordId { get; set; } = "";
        public string Title { get; set; } = "";
        public string Author { get; set; } = "";
        public string S3Key { get; set; } = "";
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 0;
        public string BookmarkTime { get; set; } = "";

        public override string ToString()
        {
            string lastRead = "never";

            if (DateTime.TryParse(BookmarkTime, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime time))
            {
                lastRead = time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }

            if (TotalPages > 0)
            {
                int percent = CurrentPage * 100 / TotalPages;

                if (percent > 100)
                {
                    percent = 100;
                }

                return $"{Title} - {Author} (Page {CurrentPage} of {TotalPages}, {percent}%, Last read: {lastRead})";
            }

            return $"{Title} - {Author} (Page {CurrentPage}, Last read: {lastRead})";
        }
    }
}
