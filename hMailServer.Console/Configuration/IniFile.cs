using System;
using System.Runtime.InteropServices;
using System.Text;

namespace hMailServer.Configuration
{
    public class IniFile
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        private string _fullPath;

        public IniFile(string iniPath)
        {
            if (iniPath== null)
                throw new ArgumentNullException(nameof(iniPath));

            _fullPath = iniPath;
        }

        public string Read(string section, string key)
        {
            if (section == null)
                throw new ArgumentNullException(nameof(section));
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var value = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", value, 255, _fullPath);
            return value.ToString();
        }

    }
}
