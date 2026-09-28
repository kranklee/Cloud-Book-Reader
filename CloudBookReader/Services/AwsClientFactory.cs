using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.S3;

namespace CloudBookReader.Services
{
    public static class AwsClientFactory
    {
        public const string ProfileName = "cloudshelf-lab";
        public static readonly RegionEndpoint Region = RegionEndpoint.EUCentral1;

        public static AmazonDynamoDBClient CreateDynamoDbClient()
        {
            AWSCredentials credentials = LoadCredentials();
            return new AmazonDynamoDBClient(credentials, Region);
        }

        public static AmazonS3Client CreateS3Client()
        {
            AWSCredentials credentials = LoadCredentials();
            return new AmazonS3Client(credentials, Region);
        }

        private static AWSCredentials LoadCredentials()
        {
            var chain = new CredentialProfileStoreChain();

            if (!chain.TryGetAWSCredentials(ProfileName, out AWSCredentials credentials))
            {
                throw new Exception($"The AWS profile '{ProfileName}' was not found on this computer.");
            }

            return credentials;
        }
    }
}
