﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;

namespace Dicom2Volume
{
    public struct SerializeTest
    {
        public Config.KeepFilesFlags Flag;
    }

    public class Config
    {
        [Flags]
        public enum KeepFilesFlags
        {
            None = 0,
            XmlImages = 1,
            XmlImagesSorted = 2, 
            XmlVolume = 4,
            RawVolume = 8,
            DdsVolume = 16,
            TarRawVolume = 32,
            TgzRawVolume = 64,
            TarDdsVolume = 128,
            TgzDdsVolume = 256,
            OutputPath = 512,
        }

        public static int SkipEveryNSlices { get; set; }
        public static bool OpenExplorerOnCompletion { get; set; }
        public static Logger.LogLevelType LogLevel { get; set; }
        public static KeepFilesFlags KeepFilesFlag { get; set; }
        public static bool WaitForEnterToExit { get; set; }

        public static string RootDirectory { get; set; }
        public static string RelativeXmlImagesOutputPath { get; set; }
        public static string RelativeXmlImagesSortedOutputPath { get; set; }
        public static string RelativeVolumeOutputPath { get; set; }
        
        public static Dictionary<uint, ElementTag> DicomDictionary { get; set; }

        static Config()
        {
            SkipEveryNSlices = int.Parse(ConfigurationManager.AppSettings["SkipEveryNSlices"] ?? "1");
            OpenExplorerOnCompletion = bool.Parse(ConfigurationManager.AppSettings["OpenExplorerOnCompletion"] ?? "true");
            LogLevel = (Logger.LogLevelType)Enum.Parse(typeof(Logger.LogLevelType), ConfigurationManager.AppSettings["LogLevel"] ?? "Info");
            KeepFilesFlag = (KeepFilesFlags)Enum.Parse(typeof(KeepFilesFlags), ConfigurationManager.AppSettings["KeepFilesFlag"] ?? "Images, SortedImages, VolumeXml, RawVolume, DdsVolume, RelativeOutputPath, CompressedDds, CompressedRaw");
            WaitForEnterToExit = bool.Parse(ConfigurationManager.AppSettings["WaitForEnterToExit"] ?? "false");
            VolumeOutputName = ConfigurationManager.AppSettings["VolumeOutputName"] ?? "volume";
            RelativeVolumeOutputPath = Path.Combine(RelativeOutputPath, "dcm2vol/volume");
            RelativeImagesOutputPath = Path.Combine(RelativeOutputPath, "dcm2vol/images");
            RelativeImagesSortedOutputPath = Path.Combine(RelativeOutputPath, "dcm2vol/sorted");

            DicomDictionary = new Dictionary<uint, ElementTag>();
            var section = DicomConfigSection.GetConfigSection();
            if (section == null) return;

            foreach (var tag in section.Tags)
            {
                var configTag = (DicomTagConfigElement) tag;
                Logger.Debug("DICOM dictionary  : " + configTag.ElementTag.Name + " - " + configTag.CombinedId);
                DicomDictionary[configTag.CombinedId] = configTag.ElementTag;
            }
        }
    }

    public class DicomTagConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("groupId", IsRequired = true)]
        public string GroupIdString { get { return (string)this["groupId"]; } }

        [ConfigurationProperty("elementId", IsRequired = true)]
        public string ElementIdString { get { return (string)this["elementId"]; } }

        [ConfigurationProperty("name", IsRequired = true)]
        public string TagName { get { return (string)this["name"]; } }

        [ConfigurationProperty("type", IsRequired = true)]
        public string TagType { get { return (string)this["type"]; } }

        public ushort GroupId { get { return Convert.ToUInt16(GroupIdString, 16); } }
        public ushort ElementId { get { return Convert.ToUInt16(ElementIdString, 16); } }
        public uint CombinedId { get { return ElementTag.GetCombinedId(GroupId, ElementId); } }
        public ElementTag ElementTag { get { return new ElementTag { Name = TagName, VR = (ValueTypes)Enum.Parse(typeof(ValueTypes), TagType) }; } }
    }

    public class DicomTagConfigElementCollection : ConfigurationElementCollection
    {
        public DicomTagConfigElement this[int index] { get { return BaseGet(index) as DicomTagConfigElement; } }

        protected override ConfigurationElement CreateNewElement()
        {
            return new DicomTagConfigElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((DicomTagConfigElement)(element)).TagName;
        }
    }

    public class DicomConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("tags")]
        public DicomTagConfigElementCollection Tags { get { return this["tags"] as DicomTagConfigElementCollection; } }

        public static DicomConfigSection GetConfigSection()
        {
            return ConfigurationManager.GetSection("dicom") as DicomConfigSection;
        }
    }
}
