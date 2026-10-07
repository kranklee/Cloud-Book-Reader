# Dosyaların Türkçe Açıklaması

Bu belge, projedeki her C# ve XAML dosyasının ne işe yaradığını basit bir dille anlatır.

## Genel Akış

1. Program açılır, `MainWindow` (giriş ekranı) görünür.
2. Kullanıcı ID ve şifre girer. Şifre SHA-256 ile özetlenir (hash) ve DynamoDB'deki `PasswordHash` ile karşılaştırılır.
3. Giriş başarılıysa `BooksWindow` açılır. Kitaplar DynamoDB'deki `UserBookmarkIndex` indeksinden, en son okunan kitap en üstte olacak şekilde gelir.
4. Kullanıcı bir kitabı çift tıklar, "Open Book" butonuna basar ya da "Continue Reading" ile en son okuduğu kitabı açar. `ReaderWindow` açılır.
5. PDF dosyası S3'ten belleğe (`MemoryStream`) indirilir ve ekranda gösterilir. Diske hiçbir dosya yazılmaz.
6. Kitap kaydedilmiş sayfadan açılır. "Bookmark" butonu ve pencerenin kapanması, o anki sayfayı ve zamanı DynamoDB'ye kaydeder.

## CloudBookReader.sln ve CloudBookReader.csproj

- `.sln`: Visual Studio'nun açtığı çözüm (solution) dosyasıdır. İçinde tek bir proje vardır.
- `.csproj`: Projenin ayarlarıdır.
  - `net8.0-windows` ve `UseWPF` projenin .NET 8 WPF uygulaması olduğunu söyler.
  - `RootNamespace` ve `AssemblyName` `CloudBookReader` olarak ayarlanmıştır.
  - `PackageReference` satırları kullanılan dört NuGet paketini gösterir.
  - `EnableWindowsTargeting` sadece projenin Windows dışında da derlenebilmesi içindir. Programın çalışmasını etkilemez.

## App.xaml ve App.xaml.cs

- `App.xaml`: Uygulamanın başlangıç noktasıdır. `StartupUri="MainWindow.xaml"` program açılınca ilk hangi pencerenin gösterileceğini belirtir.
- `Application.Resources` bölümü projenin basit stil dosyasıdır. WPF'te CSS yoktur, onun yerine `Style` kullanılır. Her stilin bir adı (`x:Key`) vardır ve CSS sınıfı gibi çalışır:
  - `NormalWindow`: Arial yazı tipi, 13 punto, krem arka plan. Bütün pencereler bunu kullanır.
  - `TitleText`: Koyu yeşil, kalın başlık yazısı.
  - `ErrorText`: Koyu kırmızı hata mesajı.
  - `GrayButton`: Normal buton boyutu (100x30) ve boşluğu.
  - `GreenButton` ve `RedButton`: `BasedOn` ile `GrayButton` stilini alır, sadece rengi değiştirir.
  - Bir kontrol stili `Style="{StaticResource GreenButton}"` şeklinde kullanır. Böylece aynı renk ve boyut her pencerede tekrar yazılmaz.
  - Stiller bilerek isimli (`x:Key`) yapıldı. İsimsiz stil bütün butonlara uygulanırdı ve PDF görüntüleyicinin kendi araç çubuğu butonlarını da bozabilirdi.
- `App.xaml.cs`: `App()` yapıcı metodu (constructor) program başlarken bir kez çalışır.
  - Syncfusion lisans anahtarını `SYNCFUSION_LICENSE_KEY` ortam değişkeninden okur ve `RegisterLicense` ile kaydeder.
  - Anahtar koda yazılmaz, böylece GitHub'a gizli bilgi gitmez.

## MainWindow.xaml (Giriş Ekranı)

- `Window`: Başlığı "Cloud Book Reader" olan pencere. `Style="{StaticResource NormalWindow}"` ile Arial yazı tipini ve krem arka planı `App.xaml` dosyasından alır.
- `StackPanel`: İçindeki elemanları alt alta dizer.
- `Grid`: Satır ve sütunlardan oluşan bir tablo gibidir. Solda `Label` (etiket), sağda giriş kutuları vardır.
- `TextBox` (`UserIdTextBox`): Kullanıcı ID'sinin yazıldığı kutu.
- `PasswordBox` (`PasswordInput`): Şifre kutusu. Yazılanlar nokta olarak görünür.
- `Button` "Login": Yeşil giriş butonu (`GreenButton` stili). `IsDefault="True"` sayesinde Enter tuşu da bu butona basar. `Click="LoginButton_Click"` tıklanınca çalışacak metodu gösterir.
- `Button` "Clear": Kırmızı temizleme butonu (`RedButton` stili).
- `TextBlock` (`MessageTextBlock`): Hata mesajlarının koyu kırmızı renkle gösterildiği yazı alanı (`ErrorText` stili).

## MainWindow.xaml.cs

- `InitializeComponent()`: XAML'da tanımlanan kontrolleri oluşturur. Her pencerenin yapıcı metodunda olmak zorundadır.
- `LoginButton_Click`:
  - Alanlar boşsa uyarı gösterir.
  - `async` ve `await` kullanır, böylece AWS'den cevap beklenirken pencere donmaz.
  - `DynamoDbService.ValidateUserAsync` ile kullanıcıyı kontrol eder.
  - Doğruysa `BooksWindow` açılır ve giriş penceresi kapanır.
  - `try/catch` bağlantı hatalarını yakalar ve basit bir İngilizce mesaj gösterir. Hata detayı gösterilmez, böylece gizli bilgi ekrana çıkmaz.
- `ClearButton_Click`: Kutuları ve mesajı temizler.
- Program açılınca imleç doğrudan `UserIdTextBox` kutusuna gelir, kullanıcı tıklamadan yazmaya başlayabilir.

## BooksWindow.xaml (Kitap Listesi)

- `UserTextBlock`: "Logged in as: ..." yazısı.
- `SearchTextBox`: Arama kutusu. `TextChanged` olayı her harf yazıldığında listeyi filtreler.
- `ListBox` (`BooksListBox`): Kitapların listesi. Her satırda `BookItem.ToString()` sonucu görünür. `MouseDoubleClick` çift tıklamayı yakalar.
- `StatusTextBlock`: "3 book(s) found." gibi durum yazısı.
- Butonlar: "Continue Reading" (yeşil), "Open Book" (yeşil), "Refresh" (standart gri), "Logout" (kırmızı).

## BooksWindow.xaml.cs

- `userId`: Giriş yapan kullanıcının ID'si. Yapıcı metoda parametre olarak gelir.
- `allBooks`: DynamoDB'den gelen tüm kitapların listesi. Arama bu liste üzerinde yapılır, böylece her harfte AWS'ye tekrar istek gönderilmez.
- `Window_Loaded`: Pencere açılınca kitapları yükler.
- `LoadBooksAsync`: `DynamoDbService.GetBooksAsync` ile kitapları çeker ve `ShowBooks` metodunu çağırır.
- `ShowBooks`: Arama metnini `ToLowerInvariant()` ile küçük harfe çevirir. Normal `ToLower()` Türkçe Windows'ta büyük "I" harfini "ı" yapardı ve "TOLKIEN" araması sonuç vermezdi. Başlıkta veya yazarda geçen kitapları listeye ekler. Sıra değişmez, yani en yeni yer imi hep en üsttedir.
- `OpenSelectedBook`: Seçili kitap yoksa uyarı verir, varsa `OpenBook` metodunu çağırır.
- `OpenBook`:
  - `ReaderWindow` penceresini `ShowDialog()` ile açar. `ShowDialog` okuyucu kapanana kadar bekler.
  - Okuyucu kapanınca liste bellekteki yeni bilgilerle `BookmarkTime` alanına göre yeniden sıralanır ve en üstteki kitap seçilir.
  - Listeyi hemen DynamoDB'den tekrar okumak yerine bu yapılır, çünkü indeks (GSI) birkaç an geç güncellenebilir ve eski sayfa görünebilirdi. Refresh butonu listeyi yine DynamoDB'den okur.
- `ContinueButton_Click`: "Continue Reading" butonu. `allBooks` listesinin ilk kitabını, yani en son okunan kitabı seçim yapmadan `OpenBook` ile açar. Arama filtresinden etkilenmez. Hiç kitap yoksa "You have no books yet." mesajı verir. Ödevdeki bookshelf.vitalsource.com örneğindeki Continue Reading butonunun karşılığıdır.
- `OpenButton_Click` ve `BooksListBox_MouseDoubleClick`: İkisi de `OpenSelectedBook` metodunu çağırır.
  - Çift tıklama sadece bir kitabın üzerine yapılırsa çalışır. Listenin boş yerine çift tıklamak kitap açmaz.
- `RefreshButton_Click`: Listeyi yeniden yükler.
- `LogoutButton_Click`: Giriş penceresini tekrar açar ve bu pencereyi kapatır.

## ReaderWindow.xaml (PDF Okuyucu)

- `xmlns:syncfusion=...`: Syncfusion kütüphanesini XAML'da kullanabilmek için eklenen tanım.
- Üst satırda (`StackPanel`) şunlar vardır: kitap adı (`BookInfoText`), durum yazısı (`StatusText`), "Bookmark" (yeşil) ve "Close" (kırmızı) butonları. Buton adı ödevdeki gibi "Bookmark".
- `BookInfoText` en fazla 400 piksel genişliktedir (`MaxWidth`). Uzun bir kitap adı `TextTrimming` ile "..." olarak kesilir, böylece butonlar ekrandan taşmaz. Fare üzerine gelince tam ad görünür (`ToolTip`).
- `syncfusion:PdfViewerControl` (`PdfViewer`): PDF'i gösteren hazır kontrol. Kendi araç çubuğu vardır: sayfa değiştirme, yakınlaştırma gibi. Dosya araçları (Open, Save, Save As, Print) kodda `ShowFileTools = false` ile gizlenir. Böylece PDF diske kaydedilemez ve başka bir PDF açılamaz.
- `Loaded`, `Closing` ve `Closed`: Pencere açılırken, kapanmadan hemen önce ve kapandıktan sonra çalışan olaylar.

## ReaderWindow.xaml.cs

- Alanlar:
  - `book`: Açılan kitap.
  - `dynamo`: Yer imini kaydetmek için kullanılan `DynamoDbService` nesnesi.
  - `bookStream`: PDF'in bellekteki kopyası. Pencere açık kaldığı sürece silinmemelidir.
  - `documentLoaded`: PDF'in yüklenip yüklenmediğini gösterir.
  - `firstPageJumpDone`: Kayıtlı sayfaya gitme işleminin sadece bir kez yapılmasını sağlar.
  - `saving`: Kayıt devam ederken pencerenin kapanmasını ve ikinci bir kaydın başlamasını engeller.
  - `closeSaveDone`: Kapanışta kaydın iki kez yapılmasını önler.
  - `windowClosed`: Kullanıcı pencereyi PDF inerken kapatırsa, inen PDF'in kapalı pencereye yüklenmesini önler.
- `Window_Loaded`: `S3BookService.GetBookStreamAsync` ile PDF'i S3'ten belleğe alır ve `PdfViewer.Load(bookStream)` ile gösterir. Hata olursa mesaj verip pencereyi kapatır.
- `PdfViewer_DocumentLoaded`: PDF tamamen yüklendiğinde çalışır. Kayıtlı sayfa 1'den büyükse ve PDF'te o sayfa varsa `GotoPage` ile o sayfaya gider. Sayfaya gitme ancak PDF yüklendikten sonra yapılabildiği için bu olayda yapılır.
- `GetCurrentPage`: `PdfViewer.CurrentPageIndex` ile o an görünen sayfa numarasını alır. Sayfalar 1'den başlar.
- `SaveBookmark`: `DynamoDbService.SaveBookmarkAsync` ile sayfayı, toplam sayfa sayısını (`PdfViewer.PageCount`) ve zamanı kaydeder.
- `BookmarkButton_Click`: "Bookmark" butonuna basınca kayıt yapar.
- `Window_Closing`, pencere kapanırken kayıt yapar:
  1. Önce `e.Cancel = true` ile kapanmayı durdurur.
  2. Kaydı bekler (`await`).
  3. Sonra `Close()` ile pencereyi tekrar kapatır.
  - İkinci kapanışta `closeSaveDone` true olduğu için tekrar kaydetmez.
  - PDF hiç yüklenmediyse kaydetmez.
- `Window_Closed`: Pencere kapandıktan sonra `PdfViewer.Unload()` ile PDF'i bırakır ve `bookStream.Dispose()` ile belleği temizler.

## Models/BookItem.cs

- Bir kitabın bilgilerini tutan basit bir sınıf. Özellikler (property):
  - `UserId`: Kitabın sahibi.
  - `RecordId`: Kaydın kimliği, örneğin `BOOK#two-towers`.
  - `Title` ve `Author`: Kitabın adı ve yazarı.
  - `S3Key`: PDF'in S3'teki yolu.
  - `CurrentPage`: Kaydedilen sayfa.
  - `TotalPages`: PDF'in toplam sayfa sayısı. 0 ise henüz bilinmiyor demektir.
  - `BookmarkTime`: Kaydedilme zamanı.
- `ToString()`: Listede görünen yazıyı oluşturur, örneğin "The Two Towers - J.R.R. Tolkien (Page 12 of 60, 20%, Last read: 2026-09-20 21:30)".
  - Okuma ilerlemesi (projeye eklenen kişisel özellik): yüzde `CurrentPage * 100 / TotalPages` ile hesaplanır.
  - `TotalPages` 0 ise sadece "Page 12" yazar.
  - DynamoDB'de zaman UTC olarak saklanır, ama listede bilgisayarın yerel saatiyle gösterilir. Türkiye için UTC+3, bu yüzden 18:30 UTC ekranda 21:30 olarak görünür.
  - `CultureInfo.InvariantCulture` tarihin bilgisayarın dil ayarından etkilenmemesini sağlar.

## Services/AwsClientFactory.cs

- `ProfileName = "cloudshelf-lab"` ve `Region = EUCentral1` (Frankfurt) sabit ayarlardır.
- `LoadCredentials`: `CredentialProfileStoreChain` ile bilgisayardaki `.aws/credentials` dosyasından profili okur. Anahtarlar kodda hiç yazmaz. Profil yoksa basit bir hata fırlatır.
- `CreateDynamoDbClient` ve `CreateS3Client`: Bu bilgilerle DynamoDB ve S3 istemcilerini (client) oluşturur.
- `static` olduğu için nesne oluşturmadan `AwsClientFactory.CreateS3Client()` şeklinde çağrılır.

## Services/DynamoDbService.cs

- `TableName = "Bookshelf"`, `IndexName = "UserBookmarkIndex"`.
- `HashPassword`: Şifreyi UTF-8 byte dizisine çevirir, SHA-256 ile özetler ve küçük harfli hex metne dönüştürür. DynamoDB'de düz şifre değil, bu özet saklanır.
- `ValidateUserAsync`:
  - `GetItem` ile `UserId = kullanıcı` ve `RecordId = "USER"` kaydını okur.
  - Kayıttaki `PasswordHash` değerini girilen şifrenin özetiyle karşılaştırır.
- `GetBooksAsync`:
  - `Query` ile `UserBookmarkIndex` indeksinde `ShelfUserId = kullanıcı` olan kayıtları çeker.
  - `ScanIndexForward = false` sonuçları `BookmarkTime` alanına göre azalan sırada, yani en yeni en üstte olacak şekilde getirir.
  - Kullanıcı kayıtlarında `ShelfUserId` olmadığı için sonuçlarda sadece kitaplar gelir.
  - `LastEvaluatedKey` sonuçlar birden fazla sayfaya bölünürse devam etmeyi sağlar.
- `SaveBookmarkAsync`: `UpdateItem` ile `CurrentPage`, `TotalPages`, `BookmarkTime` (şimdiki UTC zamanı) ve `ShelfUserId` alanlarını günceller. Toplam sayfa sayısı okuyucu penceresinden (`PdfViewer.PageCount`) gelir. Sonra bellekteki `BookItem` nesnesini de günceller.
- `ToBookItem` ve `GetString`: DynamoDB'den gelen `AttributeValue` sözlüğünü `BookItem` nesnesine çeviren yardımcı metotlar.
  - `S` metin değerini, `N` sayı değerini tutar.
  - DynamoDB sayıları da metin olarak gönderir, bu yüzden `int.TryParse` kullanılır.

## Services/S3BookService.cs

- `BucketName = "cloudshelf-cem-comp306-2026"`.
- `GetBookStreamAsync`:
  1. `GetObjectAsync` ile PDF'i özel (private) bucket'tan ister. Yetki IAM politikasından gelir.
  2. `response.ResponseStream` akışını `CopyToAsync` ile yeni bir `MemoryStream` nesnesine kopyalar.
  3. `Position = 0` ile okumanın baştan başlamasını sağlar.
  - Dosya hiçbir zaman diske (Downloads, Temp) kaydedilmez.
  - `using` ifadesi S3 cevabını iş bitince kapatır. Bellekteki kopya ise okuyucu penceresi kapanana kadar yaşar.

## aws/ Klasöründeki JSON Dosyaları

- `create-table.json`: `Bookshelf` tablosunu ve `UserBookmarkIndex` indeksini oluşturmak için AWS CLI girdisi.
- `seed-data.json`: 3 kullanıcı ve her birine 3 kitap (toplam 12 kayıt). `batch-write-item` ile yüklenir.
- `iam-policy.json`: Uygulamanın sadece ihtiyacı olan dört izin: `GetItem`, `UpdateItem`, `Query` ve `s3:GetObject`.
