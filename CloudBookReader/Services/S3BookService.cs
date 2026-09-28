using System.IO;
using Amazon.S3;
using Amazon.S3.Model;

namespace CloudBookReader.Services
{
    public class S3BookService
    {
        public const string BucketName = "cloudshelf-cem-comp306-2026";

        private readonly AmazonS3Client _client;

        public S3BookService()
        {
            _client = AwsClientFactory.CreateS3Client();
        }

        public async Task<MemoryStream> GetBookStreamAsync(string s3Key)
        {
            var request = new GetObjectRequest
            {
                BucketName = BucketName,
                Key = s3Key
            };

            using GetObjectResponse response = await _client.GetObjectAsync(request);

            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return memoryStream;
        }
    }
}
