# Cloud Book Reader

Cloud Book Reader is a simple WPF desktop application for reading PDF books stored in a private Amazon S3 bucket. Users log in with an account stored in Amazon DynamoDB, see their books sorted by the latest bookmark, search by title or author, and continue reading from the last saved page.

| Item | Name |
|---|---|
| Solution | `CloudBookReader.sln` |
| Project | `CloudBookReader/CloudBookReader.csproj` |
| Root namespace / assembly | `CloudBookReader` |
| Application title | Cloud Book Reader |

## Technology

- C#, .NET 8, WPF (code-behind)
- AWS SDK for .NET (`AWSSDK.S3`, `AWSSDK.DynamoDBv2`)
- Amazon S3 (PDF files), Amazon DynamoDB (users, books, bookmarks)
- Syncfusion PDF Viewer for WPF (`Syncfusion.PdfViewer.WPF`, `Syncfusion.Licensing`)

## Project Structure

```
CloudBookReader.sln
CloudBookReader/
  CloudBookReader.csproj
  App.xaml / App.xaml.cs                 Application start, Syncfusion license
  MainWindow.xaml / .cs                  Login window
  BooksWindow.xaml / .cs                 Book list and search
  ReaderWindow.xaml / .cs                PDF reader and bookmark
  Models/BookItem.cs                     One book record
  Services/AwsClientFactory.cs           Creates AWS clients from the local profile
  Services/DynamoDbService.cs            Login, book list, bookmark update
  Services/S3BookService.cs              Reads a PDF from S3 into memory
aws/
  create-table.json                      DynamoDB table and index definition
  seed-data.json                         3 users with 3 books each
  iam-policy.json                        Minimal IAM policy for the application
docs/
  TESTING.md                             Manual testing checklist
  DEMO_SCRIPT.md                         Five-minute video demonstration script
  TURKCE_ACIKLAMA.md                     Turkish explanation of every file
```

## AWS Configuration

| Setting | Value |
|---|---|
| AWS profile | `cloudshelf-lab` |
| Region | `eu-central-1` (Frankfurt) |
| S3 bucket | `cloudshelf-cem-comp306-2026` |
| DynamoDB table | `Bookshelf` |
| DynamoDB index | `UserBookmarkIndex` |

The AWS resource names were created before the application was renamed, so they do not match the application name. They are used as they are.

### 1. AWS profile

The application never contains access keys. It reads credentials from the local profile `cloudshelf-lab`:

```
aws configure --profile cloudshelf-lab
```

This writes to `%USERPROFILE%\.aws\credentials` on Windows. Never commit that file.

### 2. DynamoDB table and index

| Key | Attribute | Type |
|---|---|---|
| Table partition key | `UserId` | String |
| Table sort key | `RecordId` | String |
| GSI `UserBookmarkIndex` partition key | `ShelfUserId` | String |
| GSI `UserBookmarkIndex` sort key | `BookmarkTime` | String |
| GSI projection | ALL | |

Skip this step if the table already exists. To create it (use an administrator profile, not the application profile):

```
aws dynamodb create-table --cli-input-json file://aws/create-table.json --region eu-central-1
aws dynamodb wait table-exists --table-name Bookshelf --region eu-central-1
```

Console alternative: DynamoDB → Create table → name `Bookshelf`, partition key `UserId` (String), sort key `RecordId` (String), capacity mode On-demand. Then Indexes → Create index → partition key `ShelfUserId` (String), sort key `BookmarkTime` (String), name `UserBookmarkIndex`, projection All.

### 3. Data model

One table stores both users and books.

| Record | UserId | RecordId | Other attributes |
|---|---|---|---|
| User | `student1` | `USER` | `PasswordHash` (SHA-256, lowercase hex), `DisplayName` |
| Book | `student1` | `BOOK#two-towers` | `ShelfUserId`, `Title`, `Author`, `S3Key`, `CurrentPage` (Number), `BookmarkTime` (ISO-8601 UTC) |

User records do not have `ShelfUserId`, so they never appear in `UserBookmarkIndex`. Only books appear in the index. `BookmarkTime` is saved as `yyyy-MM-ddTHH:mm:ssZ`, so text order is also time order. The application queries the index with `ScanIndexForward = false` to get the newest bookmark first.

### 4. Load the demo data

`aws/seed-data.json` contains 3 users with 3 books each (12 items). Loading it again overwrites these items and resets the bookmarks.

```
aws dynamodb batch-write-item --request-items file://aws/seed-data.json --region eu-central-1
```

### 5. Upload the PDF files

The bucket must stay private (Block Public Access on). PDF files are not included in this repository. Upload legally owned PDFs or your own demonstration PDFs with these exact keys:

```
books/the-fellowship-of-the-ring.pdf
books/the-two-towers.pdf
books/the-return-of-the-king.pdf
```

```
aws s3 cp the-fellowship-of-the-ring.pdf s3://cloudshelf-cem-comp306-2026/books/the-fellowship-of-the-ring.pdf
aws s3 cp the-two-towers.pdf s3://cloudshelf-cem-comp306-2026/books/the-two-towers.pdf
aws s3 cp the-return-of-the-king.pdf s3://cloudshelf-cem-comp306-2026/books/the-return-of-the-king.pdf
```

Each demo PDF should have at least 40 pages, because the demo data contains saved pages up to page 40. If a saved page is larger than the PDF page count, the reader opens page 1.

### 6. Minimal IAM policy

Attach `aws/iam-policy.json` to the IAM user used by the `cloudshelf-lab` profile. The application only needs:

| Action | Resource |
|---|---|
| `dynamodb:GetItem` | table `Bookshelf` (login) |
| `dynamodb:UpdateItem` | table `Bookshelf` (bookmark) |
| `dynamodb:Query` | index `Bookshelf/index/UserBookmarkIndex` (book list) |
| `s3:GetObject` | `cloudshelf-cem-comp306-2026/books/*` (PDF files) |

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Sid": "ReadUserAndBookItems", "Effect": "Allow", "Action": ["dynamodb:GetItem"], "Resource": "arn:aws:dynamodb:eu-central-1:*:table/Bookshelf" },
    { "Sid": "SaveBookmarks", "Effect": "Allow", "Action": ["dynamodb:UpdateItem"], "Resource": "arn:aws:dynamodb:eu-central-1:*:table/Bookshelf" },
    { "Sid": "QueryBookmarkIndex", "Effect": "Allow", "Action": ["dynamodb:Query"], "Resource": "arn:aws:dynamodb:eu-central-1:*:table/Bookshelf/index/UserBookmarkIndex" },
    { "Sid": "ReadBookFiles", "Effect": "Allow", "Action": ["s3:GetObject"], "Resource": "arn:aws:s3:::cloudshelf-cem-comp306-2026/books/*" }
  ]
}
```

You can replace `*` in the DynamoDB ARNs with your 12-digit account ID.

## NuGet Packages

The packages are already listed in `CloudBookReader.csproj` and are restored automatically when the solution is built. To install them manually in the Visual Studio Package Manager Console:

```
Install-Package AWSSDK.S3
Install-Package AWSSDK.DynamoDBv2
Install-Package Syncfusion.PdfViewer.WPF
Install-Package Syncfusion.Licensing
```

`Syncfusion.PdfViewer.WPF` and `Syncfusion.Licensing` must have the same version.

## Syncfusion License

The license key is not stored in the repository. The application reads it from the `SYNCFUSION_LICENSE_KEY` environment variable at startup. Set it once in Windows (Command Prompt), then restart Visual Studio:

```
setx SYNCFUSION_LICENSE_KEY "your-key-here"
```

Without a key, Syncfusion shows a license message, but the viewer still works for testing.

## Build and Run in Visual Studio

1. Install Visual Studio 2022 (17.8 or later) with the ".NET desktop development" workload.
2. Open `CloudBookReader.sln`.
3. Build → Rebuild Solution. NuGet packages are restored automatically.
4. Make sure `CloudBookReader` is the startup project.
5. Press F5.

Command line alternative:

```
dotnet build CloudBookReader.sln
dotnet run --project CloudBookReader
```

## Test Login Credentials

| User ID | Password |
|---|---|
| student1 | Reader1! |
| student2 | Reader2! |
| student3 | Reader3! |

Only SHA-256 hashes of these passwords are stored in DynamoDB.

## Security Notes

- No AWS access keys or Syncfusion keys are in the source code.
- Credentials are loaded from the local `cloudshelf-lab` profile.
- Error messages never show credentials.
- The S3 bucket is private. The PDF is read with `GetObjectAsync` and copied to a `MemoryStream`. It is never saved to Downloads, Temp or any other local file.
- Passwords are stored as SHA-256 hashes. This is enough for this assignment. A real product would use a salted, slow hash (for example PBKDF2) or a service such as Amazon Cognito.
- The IAM policy only allows the four actions the application uses.

## Testing

See [docs/TESTING.md](docs/TESTING.md) for the manual testing checklist and [docs/DEMO_SCRIPT.md](docs/DEMO_SCRIPT.md) for the video demonstration script.

## AWS Cost and Cleanup

The application only uses S3 and DynamoDB. No EC2, RDS or other paid services are used.

- DynamoDB on-demand: a few hundred reads and writes during testing cost almost nothing, and 25 GB of storage is in the free tier.
- S3: three PDF files cost a fraction of a cent per month. `GetObject` requests and data transfer for a few test downloads are very small.

After the project is graded, clean up:

```
aws s3 rm s3://cloudshelf-cem-comp306-2026/books/ --recursive
aws s3 rb s3://cloudshelf-cem-comp306-2026
aws dynamodb delete-table --table-name Bookshelf --region eu-central-1
```

Then delete the access key of the `cloudshelf-lab` IAM user (or the whole user) in IAM, and remove the `[cloudshelf-lab]` section from your local `.aws/credentials` file.
