using System.Windows;

namespace CloudBookReader
{
    public partial class App : Application
    {
        public App()
        {
            string? licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
            if (!string.IsNullOrWhiteSpace(licenseKey))
            {
                Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(licenseKey);
            }
        }
    }
}
