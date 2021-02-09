using System;
using System.IO;

namespace AutoVersion {
    public static class Program {
        public static int Main(string[] args) {
            var location = typeof(Program).Assembly.Location;
            string fileNameVersionTxt = string.Empty;
            do {
                location = Path.GetDirectoryName(location);
                fileNameVersionTxt = Path.Combine(location, "version.txt");
            } while (
                (!string.IsNullOrEmpty(location)) && !File.Exists(fileNameVersionTxt)
            );
            //
            if (File.Exists(fileNameVersionTxt)) {
                var lines = File.ReadAllLines(fileNameVersionTxt);
                var content = (lines.Length > 0) ? lines[0] : string.Empty;
                if (Version.TryParse(content, out var version)) {
                    version = new Version(version.Major, version.Minor, version.Build, version.Revision + 1);
                } else {
                    version = new Version(1, 0, 0, 0);
                }
                File.WriteAllText(fileNameVersionTxt, version.ToString());
            }
            return 0;
        }
    }
}
