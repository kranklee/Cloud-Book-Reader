# Manual Testing Checklist

Before testing: the `cloudshelf-lab` profile is configured, the `Bookshelf` table contains `aws/seed-data.json`, and the three PDF files are uploaded to S3.

Expected book order for `student1` after loading the demo data:

1. The Two Towers (page 12 of 60)
2. The Fellowship of the Ring (page 40 of 60)
3. The Return of the King (page 1 of 60)

## Login

- [ ] The application starts and the window title is "Cloud Book Reader".
- [ ] The cursor is already in the User ID box.
- [ ] Login with empty fields shows "Please enter your user ID and password."
- [ ] Login with `student1` and a wrong password shows "Invalid user ID or password."
- [ ] Login with an unknown user ID shows "Invalid user ID or password."
- [ ] The Clear button empties both fields and the message.
- [ ] Login with `student1` / `Reader1!` opens the book list and closes the login window.
- [ ] Pressing Enter in the password box also logs in.

## Book List

- [ ] "Logged in as: student1" is shown.
- [ ] Three books are shown, newest bookmark first (order above).
- [ ] Each book shows its progress, for example "Page 12 of 60, 20%" for The Two Towers.
- [ ] The status line shows "3 book(s) found."
- [ ] Typing `towers` in the search box shows only "The Two Towers".
- [ ] Typing `tolkien` shows all three books.
- [ ] Typing `TOLKIEN` in capital letters also shows all three books (also on a Turkish Windows).
- [ ] Typing `xyz` shows no books and "0 book(s) found."
- [ ] Clearing the search box shows all three books again.
- [ ] Open Book with no selection shows "Please select a book first."
- [ ] Double-clicking the empty area below the books does not open a book.
- [ ] Refresh reloads the list.

## Reader

- [ ] Double-clicking "The Two Towers" opens the reader window.
- [ ] The Open Book button opens the selected book as well.
- [ ] Continue Reading opens the book at the top of the list (the most recently read one) without selecting it, on its saved page.
- [ ] The PDF is displayed inside the window.
- [ ] The reader opens on the saved page (page 12 for The Two Towers).
- [ ] Go to another page (for example page 20) and press Bookmark.
- [ ] The reader status shows "Bookmark saved on page 20 of 60."
- [ ] In the DynamoDB console, the item `student1` / `BOOK#two-towers` has `CurrentPage` = 20, `TotalPages` = 60 and a new `BookmarkTime`.
- [ ] Go to page 25 and close the window with the X button or the Close button.
- [ ] The book list updates and "The Two Towers" shows page 25 at the top, selected.
- [ ] Reopen "The Two Towers": it opens on page 25.
- [ ] Open "The Return of the King", move to page 5 and close. It moves to the top of the list.

## Other Checks

- [ ] Logout returns to the login window.
- [ ] Login as `student2` / `Reader2!` shows student2's books in a different order.
- [ ] No PDF file appears in Downloads or `%TEMP%` after reading a book.
- [ ] Rename the `cloudshelf-lab` profile temporarily: login shows "Could not connect to AWS..." and no key values.
- [ ] After closing the reader on page 25, the list shows "Page 25 of 60, 41%".
- [ ] All windows use Arial, the cream background and the green/red buttons from `App.xaml`.
- [ ] All visible text is English.
