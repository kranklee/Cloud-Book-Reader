using System.Windows;

namespace CloudBookReader
{
    public partial class App : Application
    {
        public App()
        {
            // The Syncfusion key is read from an environment variable so it is not saved in the repository.
            string? licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
        }
    }
}
