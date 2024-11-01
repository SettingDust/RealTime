namespace RealTime.Config
{
    using System;
    using System.IO;
    using System.Xml.Serialization;
    using RealTime.Patches;
    using SkyTools.Tools;

    [XmlRoot("AcademicYearConfig")]
    public class AcademicYearConfig
    {
        // Settings file name.
        [XmlIgnore]
        private static readonly string SettingsFileName = "AcademicYearConfig.xml";

        // User settings directory.
        [XmlIgnore]
        private static readonly string UserSettingsDir = ColossalFramework.IO.DataLocation.localApplicationData;

        // Full userdir settings file name.
        [XmlIgnore]
        private static readonly string SettingsFile = Path.Combine(UserSettingsDir, SettingsFileName);

        [XmlElement("DidLastYearEnd")]
        public bool DidLastYearEnd
        {
            get => EventManagerPatch.didLastYearEnd;

            set => EventManagerPatch.didLastYearEnd = value;
        }

        [XmlElement("ActualEndFrame")]
        public uint ActualEndFrame
        {
            get => AcademicYearAIPatch.actualEndFrame;

            set => AcademicYearAIPatch.actualEndFrame = value;
        }

        /// <summary>
        /// Load settings from XML file.
        /// </summary>
        internal static void Load()
        {
            try
            {
                // Attempt to read new settings file (in user settings directory).
                string fileName = SettingsFile;
                if (!File.Exists(fileName))
                {
                    // No settings file in user directory; use application directory instead.
                    fileName = SettingsFileName;

                    if (!File.Exists(fileName))
                    {
                        Log.Info("no settings file found");
                        return;
                    }
                }

                // Read settings file.
                using var reader = new StreamReader(fileName);
                var xmlSerializer = new XmlSerializer(typeof(AcademicYearConfig));
                if (xmlSerializer.Deserialize(reader) is not AcademicYearConfig settingsFile)
                {
                    Log.Error("couldn't deserialize settings file");
                }

            }
            catch (Exception e)
            {
                Log.Error("exception saving XML settings file " + e);
            }
        }


        /// <summary>
        /// Save settings to XML file.
        /// </summary>
        internal static void Save()
        {
            try
            {
                // Pretty straightforward.
                using (var writer = new StreamWriter(SettingsFile))
                {
                    var xmlSerializer = new XmlSerializer(typeof(AcademicYearConfig));
                    xmlSerializer.Serialize(writer, new AcademicYearConfig());
                }

                // Cleaning up after ourselves - delete any old config file in the application direcotry.
                if (File.Exists(SettingsFileName))
                {
                    File.Delete(SettingsFileName);
                }
            }
            catch (Exception e)
            {
                Log.Error("exception saving XML settings file " + e);
            }
        }
    }
}
