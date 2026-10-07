# Five-Minute Video Demonstration Script

Before recording, reset the demo data so the pages match this script (AWS CloudShell, Frankfurt):

```
aws dynamodb batch-write-item --request-items file://seed-data.json --region eu-central-1
```

## 0:00 - 0:20 Introduction

"This is Cloud Book Reader, a WPF desktop application that works like the bookshelf on bookshelf.vitalsource.com. Users log in, see the books on their bookshelf, read PDF books stored in Amazon S3 and continue from the page where they stopped. Users, books and bookmarks are stored in Amazon DynamoDB."

## 0:20 - 1:20 AWS Setup

1. DynamoDB console: open table `Bookshelf`, created with the AWS Management Console. Show the keys `UserId` and `RecordId`.
2. Explore table items: show a `USER` item with `PasswordHash` (no plain password).
3. Show a `BOOK#...` item with `ShelfUserId`, `CurrentPage`, `TotalPages` and `BookmarkTime`. Point out three users with three books each.
4. Indexes tab: show `UserBookmarkIndex` (`ShelfUserId`, `BookmarkTime`). "This index sorts each user's books by the latest bookmark."
5. S3 console: open bucket `cloudshelf-cem-comp306-2026`, show the `books/` folder with three PDF files. Show that Block Public Access is on: "The books are private and cannot be downloaded from the internet."

## 1:20 - 1:45 Login

1. Start the application.
2. Try `student1` with a wrong password: "Invalid user ID or password."
3. Log in with `student1` / `Reader1!`.

## 1:45 - 2:20 Bookshelf

1. Point out the order: the most recently read book is at the top (The Two Towers).
2. Point out the reading progress: "Page 12 of 60, 20%".
3. Type `fellowship` in the search box, then `tolkien`, then clear it.

## 2:20 - 3:20 Reading and Bookmark

1. Double-click "The Two Towers". The PDF opens on the saved page 12.
2. "The PDF is read from S3 into memory. The Open, Save and Print buttons are hidden, so the book cannot be downloaded."
3. Go to page 20 and press Bookmark. Show the message "Bookmark saved on page 20 of 60."
4. Switch to the DynamoDB console and refresh the item `BOOK#two-towers`: `CurrentPage` = 20 and a new `BookmarkTime`.

## 3:20 - 4:00 Saving When the Reader Closes

1. Back in the reader, go to page 25 and close the window without pressing Bookmark.
2. The list updates: "The Two Towers" shows "Page 25 of 60, 41%".
3. Open "The Return of the King", go to page 5 and close it. It moves to the top of the list.

## 4:00 - 4:45 Continue Reading After Logout

1. Press Logout.
2. Log in again as `student1`.
3. "The Return of the King" is at the top. Press Continue Reading: it opens exactly on page 5.
4. Close it. Log out and log in as `student2` / `Reader2!` to show that another user has a different bookshelf and order.

## 4:45 - 5:00 Closing

"Cloud Book Reader stores users, books and bookmarks in DynamoDB, keeps the PDF files in a private S3 bucket and reads them only into memory, sorts the bookshelf by the latest bookmark and saves the reading page with the Bookmark button and when the reader closes. Thank you."
