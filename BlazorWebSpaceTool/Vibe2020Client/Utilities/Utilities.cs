using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Vibe2020Client
{
    public static class Utilities
    {

        public static X509Certificate2 GetClientCertificate(string path, string pass)
        {
            string configPath = Path.GetFullPath(Path.GetFullPath(path));
            return new X509Certificate2(configPath, pass);
        }

        public static X509Certificate2 GetServerCertificate(string path, string pass)
        {
            string configPath = Path.GetFullPath(path);
            return new X509Certificate2(configPath, pass);
        }
    }
}
