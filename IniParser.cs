using System.Collections;
using System.IO;
using System.Linq;

namespace AutoDice
{
    public class IniParser
    {
        private readonly string _iniFilePath;
        private readonly Hashtable _keyPairs = new Hashtable();

        /// <summary>
        ///     Opens the INI file at the given path ;and enumerates the values in the IniParser.
        /// </summary>
        /// <param name="iniPath">Full path to INI file.&lt;</param>
        public IniParser(string iniPath)
        {
            TextReader iniFile = null;
            string currentRoot = null;

            _iniFilePath = iniPath;

            if (File.Exists(iniPath))
            {
                try
                {
                    iniFile = new StreamReader(iniPath);

                    var strLine = iniFile.ReadLine();

                    while (strLine != null)
                    {
                        strLine = strLine.Trim();

                        if (strLine != "")
                        {
                            if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                            {
                                currentRoot = strLine.Substring(1, strLine.Length - 2).Trim();
                            }
                            else
                            {
                                var keyPair = strLine.Split(new[] {'='}, 2);

                                SectionPair sectionPair;
                                string value = null;

                                if (currentRoot == null)
                                    currentRoot = "ROOT";

                                sectionPair.Section = currentRoot;
                                sectionPair.Key = keyPair[0].Trim();

                                if (keyPair.Length > 1)
                                    value = keyPair[1].Trim();

                                _keyPairs.Add(sectionPair, value);
                            }
                        }

                        strLine = iniFile.ReadLine();
                    }
                }
                finally
                {
                    if (iniFile != null)
                        iniFile.Close();
                }
            }
            else
                throw new FileNotFoundException("Unable to locate " + iniPath);
        }

        /// <summary>
        ///     Returns the ;value for the given section, key pair.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="settingName">Key name.</param>
        public string GetSetting(string sectionName, string settingName)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName;
            sectionPair.Key = settingName;

            return (string) _keyPairs[sectionPair];
        }

        /// <summary>
        ///     Enumerates alllines for given section.
        /// </summary>
        /// <param name="sectionName">Section to enum.</param>
        /// ;
        public string[] EnumSection(string sectionName)
        {
            var tmpArray = new ArrayList();

            foreach (var pair in _keyPairs.Keys.Cast<SectionPair>().Where(pair => pair.Section == sectionName))
            {
                tmpArray.Add(pair.Key);
            }

            return (string[]) tmpArray.ToArray(typeof (string));
        }

        /// <summary>
        ///     Adds or replaces a setting to the tableto be saved.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        /// <param name="settingValue">Value of key.</param>
        public void AddSetting(string sectionName, string settingName, string settingValue)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName;
            sectionPair.Key = settingName;

            if (_keyPairs.ContainsKey(sectionPair))
                _keyPairs.Remove(sectionPair);

            _keyPairs.Add(sectionPair, settingValue);
        }

        /// <summary>
        ///     Adds or replaces a setting to the tableto be saved with a nullvalue.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        public void AddSetting(string sectionName, string settingName)
        {
            AddSetting(sectionName, settingName, null);
        }

        /// <summary>
        ///     Remove a setting.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        public void DeleteSetting(string sectionName, string settingName)
        {
            SectionPair sectionPair;
            sectionPair.Section = sectionName;
            sectionPair.Key = settingName;

            if (_keyPairs.ContainsKey(sectionPair))
                _keyPairs.Remove(sectionPair);
        }

        /// <summary>
        ///     Save settingsto new file.
        /// </summary>
        /// <param name="newFilePath">New file path.</param>
        public void SaveSettings(string newFilePath)
        {
            var sections = new ArrayList();
            var strToSave = "";

            foreach (
                var sectionPair in
                    _keyPairs.Keys.Cast<SectionPair>().Where(sectionPair => !sections.Contains(sectionPair.Section)))
            {
                sections.Add(sectionPair.Section);
            }

            foreach (string section in sections)
            {
                strToSave += ("[" + section + "]\r\n");

                foreach (SectionPair sectionPair in _keyPairs.Keys)
                {
                    if (sectionPair.Section != section) continue;
                    var tmpValue = (string) _keyPairs[sectionPair];

                    if (tmpValue != null)
                        tmpValue = "=" + tmpValue;

                    strToSave += (sectionPair.Key + tmpValue + "\r\n");
                }

                strToSave += "\r\n";
            }

            TextWriter tw = new StreamWriter(newFilePath);
            tw.Write(strToSave);
            tw.Close();
        }

        /// <summary>
        ///     Save settingsback to ini file.
        /// </summary>
        public void SaveSettings()
        {
            SaveSettings(_iniFilePath);
        }

        private struct SectionPair
        {
            public string Key;
            public string Section;
        }
    }
}