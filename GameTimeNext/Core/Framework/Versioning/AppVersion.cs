using System.Reflection;

namespace GameTimeNext.Core.Framework.Versioning
{
    public class AppVersion
    {
        private string _informationalVersion = string.Empty;
        private string _versionText = string.Empty;
        private string _versionBuild = string.Empty;
        private bool _isBeta = false;
        private Version _version = new Version(0, 0, 0);

        public string InformationalVersion
        {
            get { return _informationalVersion; }
        }

        public string VersionText
        {
            get { return _versionText; }
        }

        public string Build
        {
            get { return _versionBuild; }
        }

        public bool IsBeta
        {
            get { return _isBeta; }
        }

        public Version Version
        {
            get { return _version; }
        }

        public void Get()
        {
            string raw = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0";

            FillFromInformationalVersion(raw);
        }

        public void Get(string informationalVersion)
        {
            FillFromInformationalVersion(informationalVersion);
        }

        public void SetAppVersionInConfig()
        {
            if (AppEnvironment.GetAppConfig().AppVersion == _informationalVersion)
                return;

            AppEnvironment.GetAppConfig().AppVersion = _informationalVersion;
            AppEnvironment.SaveAppConfig();
        }

        public bool IsBiggerThan(Version version)
        {
            return _version > version;
        }

        public bool IsSmallerThan(Version version)
        {
            return _version < version;
        }

        public bool IsBiggerThan(string version)
        {
            return _version > Version.Parse(version);
        }

        public bool IsSmallerThan(string version)
        {
            return _version < Version.Parse(version);
        }

        public bool IsBiggerThan(AppVersion version)
        {
            return _version > version.Version;
        }

        public bool IsSmallerThan(AppVersion version)
        {
            return _version < version.Version;
        }

        public bool IsEqualOrBiggerThan(Version version)
        {
            return _version >= version;
        }

        public bool IsEqualOrSmallerThan(Version version)
        {
            return _version <= version;
        }

        public bool IsEqualOrBiggerThan(string version)
        {
            return _version >= Version.Parse(version);
        }

        public bool IsEqualOrSmallerThan(string version)
        {
            return _version <= Version.Parse(version);
        }

        public bool IsEqualOrBiggerThan(AppVersion version)
        {
            return _version >= version.Version;
        }

        public bool IsEqualOrSmallerThan(AppVersion version)
        {
            return _version <= version.Version;
        }

        public bool IsEqualTo(Version version)
        {
            return _version == version;
        }

        public bool IsEqualTo(string version)
        {
            return _version == Version.Parse(version);
        }

        public bool IsEqualTo(AppVersion version)
        {
            return _version == version.Version;
        }

        public bool NeedsMigrationFrom(AppVersion version)
        {
            return IsBiggerThan(version);
        }

        public bool NeedsMigrationFrom(string informationalVersion)
        {
            AppVersion version = new AppVersion();
            version.Get(informationalVersion);

            return IsBiggerThan(version);
        }

        public bool NeedsMigrationFrom(string informationalVersion, string maxVersion)
        {
            AppVersion input = new AppVersion();
            input.Get(informationalVersion);

            AppVersion upperBound = new AppVersion();
            upperBound.Get(maxVersion);

            return IsBiggerThan(input) && input.IsSmallerThan(upperBound);
        }

        private void FillFromInformationalVersion(string raw)
        {
            raw = string.IsNullOrWhiteSpace(raw) ? "0.0.0" : raw;

            _informationalVersion = CleanInformationalVersion(raw);
            _versionText = ExtractVersionText(_informationalVersion);
            _versionBuild = ExtractBuild(_informationalVersion);
            _isBeta = ExtractIsBeta(_informationalVersion);
            _version = Version.Parse(_versionText);
        }

        private string CleanInformationalVersion(string raw)
        {
            string[] parts = raw.Split('+', 2);

            if (parts.Length < 2)
                return raw;

            string metadata = parts[1];
            int dotIndex = metadata.IndexOf('.');

            if (dotIndex > 0)
                metadata = metadata.Substring(0, dotIndex);

            return parts[0] + "+" + metadata;
        }

        private string ExtractVersionText(string raw)
        {
            string main = raw.Split('+')[0];
            return main.Split('-')[0];
        }

        private string ExtractBuild(string raw)
        {
            string[] parts = raw.Split('+', 2);
            return parts.Length > 1 ? parts[1] : string.Empty;
        }

        private bool ExtractIsBeta(string raw)
        {
            string main = raw.Split('+')[0];
            return main.Contains("beta", StringComparison.OrdinalIgnoreCase);
        }
    }
}