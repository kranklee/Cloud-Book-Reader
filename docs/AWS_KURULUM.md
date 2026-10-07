# AWS Kurulum Rehberi

Bu rehber, Cloud Book Reader uygulamasını çalıştırmak için AWS'de yapılması gereken her şeyi sırayla anlatır.

Bölge (region) her adımda **Europe (Frankfurt) eu-central-1** olmalıdır. AWS konsolunun sağ üst köşesinden bölgeyi kontrol et.

## Özet

| Adım | Ne yapılır | Nerede |
|---|---|---|
| 1 | AWS CLI kurulumu | Bilgisayar |
| 2 | Yönetici profili (`admin`) | IAM + bilgisayar |
| 3 | S3 bucket oluşturma | S3 |
| 4 | PDF dosyalarını yükleme | S3 |
| 5 | DynamoDB tablosu ve indeksi | DynamoDB |
| 6 | Demo verileri yükleme | DynamoDB |
| 7 | Uygulama kullanıcısı ve izinler (`cloudshelf-lab`) | IAM + bilgisayar |
| 8 | Kontrol | Konsol |
| 9 | Syncfusion anahtarı ve çalıştırma | Bilgisayar |
| 10 | Ödevden sonra temizlik | Konsol |

Tablo veya bucket zaten varsa o adımı atla. Var olan kaynakları silip yeniden oluşturma.

## 1. AWS CLI kurulumu

1. https://aws.amazon.com/cli/ adresinden Windows için AWS CLI v2 kurulum dosyasını indir ve kur.
2. Yeni bir Command Prompt aç ve kontrol et:

```
aws --version
```

Bir sürüm numarası görünmelidir, örneğin `aws-cli/2.x.x`.

## 2. Yönetici profili (admin)

Kurulum komutları (tablo, bucket, yükleme) için yönetici yetkili bir profil gerekir. Uygulamanın kendi profili (`cloudshelf-lab`) bu işleri yapamaz, çünkü sadece okuma ve yer imi kaydetme izni vardır.

1. AWS konsolu → **IAM** → **Users** → **Create user**.
2. Kullanıcı adı: `cloudshelf-admin`. **Next**.
3. **Attach policies directly** → `AdministratorAccess` seç. **Next** → **Create user**.
4. Kullanıcıya tıkla → **Security credentials** → **Create access key** → **Command Line Interface (CLI)** → onay kutusunu işaretle → **Create access key**.
5. **Access key** ve **Secret access key** değerlerini kopyala. Secret key sadece bir kez gösterilir.
6. Bilgisayarda:

```
aws configure --profile admin
```

| Soru | Cevap |
|---|---|
| AWS Access Key ID | Az önce kopyaladığın access key |
| AWS Secret Access Key | Az önce kopyaladığın secret key |
| Default region name | `eu-central-1` |
| Default output format | `json` |

Anahtarları hiçbir dosyaya, koda veya GitHub'a yazma. Sadece bu komutla kaydedilir.

## 3. S3 bucket oluşturma

Bucket zaten varsa bu adımı atla.

**Konsol ile:**

1. **S3** → **Create bucket**.
2. Bucket name: `cloudshelf-cem-comp306-2026`.
3. AWS Region: **Europe (Frankfurt) eu-central-1**.
4. **Block all public access** işaretli kalsın. Bucket özel (private) olmalı.
5. Diğer ayarları değiştirme → **Create bucket**.

**Veya CLI ile** (proje klasöründe, yani `CloudBookReader.sln` dosyasının olduğu klasörde):

```
aws s3 mb s3://cloudshelf-cem-comp306-2026 --profile admin --region eu-central-1
```

## 4. PDF dosyalarını yükleme

Zip içindeki `demo-pdfs` klasöründe üç tane 60 sayfalık demo PDF var. Dosya adları değişmemeli.

**Konsol ile:**

1. **S3** → `cloudshelf-cem-comp306-2026` → **Create folder** → klasör adı: `books` → **Create folder**.
2. `books/` klasörüne gir → **Upload** → üç PDF'i ekle → **Upload**.

**Veya CLI ile** (`demo-pdfs` klasöründe):

```
aws s3 cp the-fellowship-of-the-ring.pdf s3://cloudshelf-cem-comp306-2026/books/the-fellowship-of-the-ring.pdf --profile admin --region eu-central-1
aws s3 cp the-two-towers.pdf s3://cloudshelf-cem-comp306-2026/books/the-two-towers.pdf --profile admin --region eu-central-1
aws s3 cp the-return-of-the-king.pdf s3://cloudshelf-cem-comp306-2026/books/the-return-of-the-king.pdf --profile admin --region eu-central-1
```

Sonuçta bucket içinde tam olarak şu dosyalar olmalı:

```
books/the-fellowship-of-the-ring.pdf
books/the-two-towers.pdf
books/the-return-of-the-king.pdf
```

## 5. DynamoDB tablosu ve indeksi

Tablo zaten varsa, anahtarların ve indeksin aşağıdaki tabloyla aynı olduğunu kontrol et ve bu adımı atla.

| Ayar | Değer |
|---|---|
| Table name | `Bookshelf` |
| Partition key | `UserId` (String) |
| Sort key | `RecordId` (String) |
| Capacity mode | On-demand |
| Index name | `UserBookmarkIndex` |
| Index partition key | `ShelfUserId` (String) |
| Index sort key | `BookmarkTime` (String) |
| Index projection | All |

**Konsol ile:**

1. **DynamoDB** → **Tables** → **Create table**.
2. Table name: `Bookshelf`. Partition key: `UserId`, String. Sort key: `RecordId`, String.
3. **Table settings** → **Customize settings** → **Read/write capacity settings** → **On-demand**.
4. **Create table**. Durum **Active** olana kadar bekle.
5. Tabloya tıkla → **Indexes** sekmesi → **Create index**.
6. Partition key: `ShelfUserId`, String. Sort key: `BookmarkTime`, String. Index name: `UserBookmarkIndex`. Attribute projections: **All**.
7. **Create index**. İndeks durumu **Active** olana kadar bekle (birkaç dakika sürebilir).

**Veya CLI ile** (proje klasöründe):

```
aws dynamodb create-table --cli-input-json file://aws/create-table.json --profile admin --region eu-central-1
aws dynamodb wait table-exists --table-name Bookshelf --profile admin --region eu-central-1
```

## 6. Demo verileri yükleme

`aws/seed-data.json` dosyasında 3 kullanıcı ve her kullanıcı için 3 kitap var (toplam 12 kayıt). Proje klasöründe:

```
aws dynamodb batch-write-item --request-items file://aws/seed-data.json --profile admin --region eu-central-1
```

Çıktı şu olmalı:

```
{
    "UnprocessedItems": {}
}
```

İçinde kayıt listeleniyorsa aynı komutu bir kez daha çalıştır.

Bu komut tekrar çalıştırılırsa kayıtların üzerine yazar ve yer imleri başlangıç değerlerine döner. Videodan önce temiz bir başlangıç için kullanılabilir.

Test kullanıcıları:

| User ID | Password |
|---|---|
| student1 | Reader1! |
| student2 | Reader2! |
| student3 | Reader3! |

## 7. Uygulama kullanıcısı ve izinler (cloudshelf-lab)

Uygulama sadece ihtiyacı olan dört izne sahip ayrı bir kullanıcı ile çalışır.

1. **IAM** → **Policies** → **Create policy** → **JSON** sekmesi.
2. İçindeki her şeyi sil, `aws/iam-policy.json` dosyasının içeriğini yapıştır. **Next**.
3. Policy name: `CloudBookReaderPolicy` → **Create policy**.
4. **IAM** → **Users** → **Create user** → kullanıcı adı: `cloudshelf-lab` → **Next**.
5. **Attach policies directly** → `CloudBookReaderPolicy` seç → **Next** → **Create user**.
6. Kullanıcıya tıkla → **Security credentials** → **Create access key** → **Command Line Interface (CLI)** → **Create access key** → iki anahtarı kopyala.
7. Bilgisayarda:

```
aws configure --profile cloudshelf-lab
```

Bölge olarak `eu-central-1`, format olarak `json` gir.

Bu politikanın izinleri:

| İzin | Ne için |
|---|---|
| `dynamodb:GetItem` | Giriş (kullanıcı kaydını okuma) |
| `dynamodb:Query` | Kitap listesi (`UserBookmarkIndex`) |
| `dynamodb:UpdateItem` | Yer imini kaydetme |
| `s3:GetObject` | `books/` klasöründeki PDF'leri okuma |

## 8. Kontrol

Uygulama profili ile şu komutları çalıştır. Hata vermemeleri gerekir:

```
aws dynamodb get-item --table-name Bookshelf --key "{\"UserId\":{\"S\":\"student1\"},\"RecordId\":{\"S\":\"USER\"}}" --profile cloudshelf-lab --region eu-central-1
aws dynamodb query --table-name Bookshelf --index-name UserBookmarkIndex --key-condition-expression "ShelfUserId = :u" --expression-attribute-values "{\":u\":{\"S\":\"student1\"}}" --no-scan-index-forward --profile cloudshelf-lab --region eu-central-1
aws s3api head-object --bucket cloudshelf-cem-comp306-2026 --key books/the-two-towers.pdf --profile cloudshelf-lab --region eu-central-1
```

- İlk komut `student1` kullanıcısını ve `PasswordHash` değerini gösterir.
- İkinci komut 3 kitabı gösterir. İlki `The Two Towers` olmalıdır.
- Üçüncü komut PDF'in bilgilerini gösterir.

`AccessDenied` hatası görürsen 7. adımdaki politikanın `cloudshelf-lab` kullanıcısına bağlı olduğunu kontrol et.

## 9. Syncfusion anahtarı ve çalıştırma

1. Syncfusion hesabından (ücretsiz Community License) WPF için lisans anahtarı al.
2. Command Prompt'ta:

```
setx SYNCFUSION_LICENSE_KEY "lisans-anahtarin"
```

3. Visual Studio açıksa kapatıp yeniden aç.
4. `CloudBookReader.sln` dosyasını aç → **Build** → **Rebuild Solution** → **F5**.
5. `student1` / `Reader1!` ile giriş yap ve `docs/TESTING.md` listesini sırayla dene.

## 10. Maliyet ve ödevden sonra temizlik

Uygulama sadece S3 ve DynamoDB (on-demand) kullanır. EC2, RDS veya başka ücretli servis kullanılmaz. Test sırasında maliyet birkaç sentin altında kalır.

İsteğe bağlı: **Billing** → **Budgets** → **Create budget** → **Zero spend budget** ile beklenmeyen bir ücret olursa e-posta alabilirsin.

Ödev notlandıktan sonra:

```
aws s3 rm s3://cloudshelf-cem-comp306-2026/books/ --recursive --profile admin --region eu-central-1
aws s3 rb s3://cloudshelf-cem-comp306-2026 --profile admin --region eu-central-1
aws dynamodb delete-table --table-name Bookshelf --profile admin --region eu-central-1
```

Sonra IAM'de `cloudshelf-lab` ve `cloudshelf-admin` kullanıcılarının access key'lerini sil (veya kullanıcıları tamamen sil). Bilgisayardaki `%USERPROFILE%\.aws\credentials` dosyasından `[cloudshelf-lab]` ve `[admin]` bölümlerini kaldır.
