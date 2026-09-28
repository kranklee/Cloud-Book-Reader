# Five-Minute Video Demonstration Script

## 0:00 - 0:30 Introduction

"This is Cloud Book Reader, a WPF desktop application built with C# and .NET 8. It reads PDF books from a private Amazon S3 bucket and stores users, books and bookmarks in Amazon DynamoDB."

Show the solution in Visual Studio: `CloudBookReader.sln`, the project `CloudBookReader`, the three windows, `Models` and `Services`.

## 0:30 - 1:30 AWS Setup

1. S3 console: open bucket `cloudshelf-cem-comp306-2026`, show the `books/` folder with three PDF files. Show that Block Public Access is on.
2. DynamoDB console: open table `Bookshelf`. Show the keys `UserId` and `RecordId`.
3. Show a `USER` item with `PasswordHash` (no plain password).
4. Show a `BOOK#...` item with `ShelfUserId`, `CurrentPage` and `BookmarkTime`.
5. Indexes tab: show `UserBookmarkIndex` (`ShelfUserId`, `BookmarkTime`, projection All).
6. IAM: show the minimal policy attached to the `cloudshelf-lab` user.

## 1:30 - 2:15 Code Walkthrough

- `AwsClientFactory.cs`: the profile `cloudshelf-lab` and region `eu-central-1`, no keys in the code.
- `DynamoDbService.cs`: `ValidateUserAsync` hashes the password with SHA-256; `GetBooksAsync` queries `UserBookmarkIndex` with `ScanIndexForward = false`; `SaveBookmarkAsync` updates `CurrentPage`, `BookmarkTime` and `ShelfUserId`.
- `S3BookService.cs`: `GetObjectAsync` and copy to a `MemoryStream`, nothing saved to disk.

## 2:15 - 2:45 Login

1. Press F5.
2. Try `student1` with a wrong password: "Invalid user ID or password."
3. Log in with `student1` / `Reader1!`.

## 2:45 - 3:15 Book List and Search

1. Point out the order: newest bookmark first (The Two Towers on top).
2. Type `fellowship` in the search box, then `tolkien`, then clear it.

## 3:15 - 4:15 Reading and Bookmark

1. Double-click "The Two Towers". The PDF opens on the saved page 12.
2. Go to page 20 and press Save Bookmark. Show the message.
3. Refresh the DynamoDB item in the console: `CurrentPage` = 20 and the new `BookmarkTime`.
4. Go to page 25 and close the reader.

## 4:15 - 4:45 Reload and Reopen

1. The list reloads: "The Two Towers" shows page 25.
2. Open "The Return of the King", go to page 5, close it. It moves to the top of the list.
3. Reopen "The Two Towers": it opens on page 25.

## 4:45 - 5:00 Closing

1. Press Logout.
2. "Users, books and bookmarks are in DynamoDB, the PDF files are in a private S3 bucket and are only read into memory, and the application uses a local AWS profile with a minimal IAM policy. Thank you."
