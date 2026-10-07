using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using CloudBookReader.Models;

namespace CloudBookReader.Services
{
    public class DynamoDbService
    {
        public const string TableName = "Bookshelf";
        public const string IndexName = "UserBookmarkIndex";

        private readonly AmazonDynamoDBClient _client;

        public DynamoDbService()
        {
            _client = AwsClientFactory.CreateDynamoDbClient();
        }

        public static string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public async Task<bool> ValidateUserAsync(string userId, string password)
        {
            var request = new GetItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = userId } },
                    { "RecordId", new AttributeValue { S = "USER" } }
                }
            };

            GetItemResponse response = await _client.GetItemAsync(request);

            if (response.Item == null || !response.Item.TryGetValue("PasswordHash", out AttributeValue? hashValue))
            {
                return false;
            }

            string storedHash = hashValue.S ?? "";
            return storedHash == HashPassword(password);
        }

        public async Task<List<BookItem>> GetBooksAsync(string userId)
        {
            var books = new List<BookItem>();
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var request = new QueryRequest
                {
                    TableName = TableName,
                    IndexName = IndexName,
                    KeyConditionExpression = "ShelfUserId = :uid",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        { ":uid", new AttributeValue { S = userId } }
                    },
                    ScanIndexForward = false
                };

                if (lastKey != null && lastKey.Count > 0)
                {
                    request.ExclusiveStartKey = lastKey;
                }

                QueryResponse response = await _client.QueryAsync(request);

                if (response.Items != null)
                {
                    foreach (var item in response.Items)
                    {
                        books.Add(ToBookItem(item));
                    }
                }

                lastKey = response.LastEvaluatedKey;
            }
            while (lastKey != null && lastKey.Count > 0);

            return books;
        }

        public async Task SaveBookmarkAsync(BookItem book, int currentPage, int totalPages)
        {
            string time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

            var request = new UpdateItemRequest
            {
                TableName = TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    { "UserId", new AttributeValue { S = book.UserId } },
                    { "RecordId", new AttributeValue { S = book.RecordId } }
                },
                UpdateExpression = "SET CurrentPage = :page, TotalPages = :total, BookmarkTime = :time, ShelfUserId = :uid",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    { ":page", new AttributeValue { N = currentPage.ToString(CultureInfo.InvariantCulture) } },
                    { ":total", new AttributeValue { N = totalPages.ToString(CultureInfo.InvariantCulture) } },
                    { ":time", new AttributeValue { S = time } },
                    { ":uid", new AttributeValue { S = book.UserId } }
                }
            };

            await _client.UpdateItemAsync(request);

            book.CurrentPage = currentPage;
            book.TotalPages = totalPages;
            book.BookmarkTime = time;
        }

        private static BookItem ToBookItem(Dictionary<string, AttributeValue> item)
        {
            var book = new BookItem
            {
                UserId = GetString(item, "UserId"),
                RecordId = GetString(item, "RecordId"),
                Title = GetString(item, "Title"),
                Author = GetString(item, "Author"),
                S3Key = GetString(item, "S3Key"),
                BookmarkTime = GetString(item, "BookmarkTime")
            };

            if (item.TryGetValue("CurrentPage", out AttributeValue? pageValue) && int.TryParse(pageValue.N, out int page))
            {
                book.CurrentPage = page;
            }

            if (item.TryGetValue("TotalPages", out AttributeValue? totalValue) && int.TryParse(totalValue.N, out int total))
            {
                book.TotalPages = total;
            }

            return book;
        }

        private static string GetString(Dictionary<string, AttributeValue> item, string name)
        {
            if (item.TryGetValue(name, out AttributeValue? value) && value.S != null)
            {
                return value.S;
            }

            return "";
        }
    }
}
