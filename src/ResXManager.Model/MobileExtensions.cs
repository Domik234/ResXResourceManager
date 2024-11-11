namespace ResXManager.Model;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

public static partial class MobileExtensions
{
    private const string CONST_NEUTRAL = "neutral";

    #region Externally Called Methods
    public static void ExportAndroidFiles(this ResourceManager resourceManager, string directoryPath)
    {
        var data = ExtractData(resourceManager);

        foreach (var dict in data)
        {
            //Prepare final's folder name (values-<lang>)
            var destFolder = "values";
            if (dict.Key != CONST_NEUTRAL)
            {
                destFolder += "-" + dict.Key;
            }

            //Be sure of existence final folder for export
            if (!Directory.Exists(Path.Combine(directoryPath, destFolder)))
                Directory.CreateDirectory(Path.Combine(directoryPath, destFolder));

            SerializeForAndroid(dict.Value, Path.Combine(directoryPath, destFolder, "strings.xml"));
        }
    }

    public static void ExportiOSFiles(this ResourceManager resourceManager, string directoryPath)
    {
        var data = ExtractData(resourceManager);

        foreach (var dict in data)
        {
            //Prepare final's folder name (<lang>.lproj)
            var destFolder = dict.Key == CONST_NEUTRAL ? "Base.lproj" : dict.Key + ".lproj";

            //Be sure of existence final folder for export
            if (!Directory.Exists(Path.Combine(directoryPath, destFolder)))
                Directory.CreateDirectory(Path.Combine(directoryPath, destFolder));

            SerializeForiOS(dict.Value, Path.Combine(directoryPath, destFolder, "strings.xml"));
        }
    }

    #endregion Externally Called Methods

    #region Private Methods
    private static Dictionary<string, Dictionary<string, string>> ExtractData(ResourceManager resourceManager)
    {
        Dictionary<string, Dictionary<string, string>> languageDictionaries = [];

        //Extract Data
        foreach (var entity in resourceManager.ResourceEntities)
        {
            foreach (var lang in entity.Languages)
            {
                var langTwoLetterISO = lang.Culture?.TwoLetterISOLanguageName ?? CONST_NEUTRAL;
                if (!languageDictionaries.ContainsKey(langTwoLetterISO))
                    languageDictionaries.Add(langTwoLetterISO, []);
            }

            foreach (var entry in entity.Entries)
            {
                foreach (var lang in entry.Languages)
                {
                    languageDictionaries[lang.Culture?.TwoLetterISOLanguageName ?? CONST_NEUTRAL].Add(entry.Key, entry.Values.GetValue(lang?.Culture) ?? "");
                }
            }
        }

        return languageDictionaries;
    }

    private static void SerializeForAndroid(Dictionary<string, string> dict, string directoryPath)
    {
        try
        {
            var data = new AndroidXmlResourcesModel();

            foreach (var keyValue in dict)
            {
                if (!string.IsNullOrWhiteSpace(keyValue.Value))
                    data.KeyValues.Add(new AndroidXmlResourcesModel.KeyValue() { Key = keyValue.Key, Value = keyValue.Value });
            }

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };

            using (var file = File.CreateText(directoryPath)) //Serializace ItemGroup do XML souboru
            {
                using (var writer = XmlWriter.Create(file, settings))
                {
                    var ser = new XmlSerializer(typeof(AndroidXmlResourcesModel));
                    var emptyNamespaces = new XmlSerializerNamespaces([XmlQualifiedName.Empty]);

                    ser.Serialize(writer, data, emptyNamespaces);
                }
            }
            //return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("XML Serialization Error - " + ex.Message);
            //return false;
        }
    }

    private static void SerializeForiOS(Dictionary<string, string> dict, string filePath)
    {
        try
        {
            var sb = new StringBuilder();
            foreach (var keyValue in dict)
            {
                if (!string.IsNullOrWhiteSpace(keyValue.Value))
                {
                    sb.Append($"\"{keyValue.Key}\" = \"{keyValue.Value}\";");
                    sb.AppendLine();
                }
            }
            sb.AppendLine();
            File.WriteAllText(filePath, sb.ToString());
            //return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("iOS Serialization Error - " + ex.Message);
            //return false;
        }
    }

    #endregion Private Methods

    #region Models
    private enum NativeMobileOS
    {
        Android,
        iOS
    }

    [XmlRoot("resources")]
    public class AndroidXmlResourcesModel
    {
        [XmlElement("string")]
        public List<KeyValue> KeyValues = [];

        public class KeyValue
        {
            [XmlAttribute("name")]
            public string? Key { get; set; }

            [XmlText]
            public string? Value { get; set; }
        }
    }
    #endregion Models
}
