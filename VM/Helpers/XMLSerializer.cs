using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace StoryManager.VM.Helpers
{
    public static class XMLSerializer
    {
        ///<summary>Serializes the given object to an XML string</summary>
        public static string SerializeToString<T>(T dataObject)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            using (StringWriter stringOutput = new StringWriter())
            {
                using (XmlTextWriter textWriter = new XmlTextWriter(stringOutput) { Formatting = Formatting.Indented })
                {
                    serializer.WriteObject(textWriter, dataObject);
                    return stringOutput.ToString();
                }
            }
        }

        /// <summary>Deserializes the given Xml string into an object of the given T Type</summary>
        public static T DeserializeFromString<T>(string xmlString)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(T));
                return (T)serializer.ReadObject(reader);
            }
        }

        ///<summary>Serializes this object to the given file.  Warning: this will overwrite existing files.</summary>
        public static void Serialize<T>(T obj, string filePath, out bool success, out Exception error)
        {
            string folderPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            DataContractSerializer serializer = new DataContractSerializer(typeof(T));
            XmlWriterSettings writerSettings = new XmlWriterSettings { Indent = true };

            //  Failsafe - ensure the object is properly serializable by trying to serialize to a string first.
            //  If this fails, assume the object is bad data and that we shouldn't overwrite an existing file with it.
            try
            {
                using (var stringOutput = new StringWriter())
                {
                    using (var textWriter = new XmlTextWriter(stringOutput) { Formatting = Formatting.Indented })
                    {
                        serializer.WriteObject(textWriter, obj);
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                error = ex;
                return;
            }

            using (var writer = XmlWriter.Create(filePath, writerSettings))
            {
                serializer.WriteObject(writer, obj);
            }

            success = true;
            error = null;
        }

        public static T Deserialize<T>(string filePath, bool throwExceptionIfFileNotFound = false)
        {
            T obj = default;

            if (throwExceptionIfFileNotFound && !File.Exists(filePath))
                throw new FileNotFoundException("No file found to deserialize at: " + filePath);

            try
            {
                if (File.Exists(filePath))
                {
                    using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas());
                        DataContractSerializer ser = new DataContractSerializer(typeof(T));
                        obj = (T)ser.ReadObject(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }

            return obj;
        }

        public static bool TrySerializeToBytes<T>(T data, string tempFilePrefix, string fileExt, out byte[] fileBytes, out Exception error)
            where T : class
        {
            try
            {
                string tempFilename = string.Format("{0} tmp file - {1}{2}", tempFilePrefix, DateTime.Now.ToString("yyyy_MM_dd_hhmmss_fff_tt"), fileExt);
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFilename);
                try
                {
                    Serialize(data, tempFilePath, out bool fSuccess, out Exception pLocalSerializationError);
                    if (!fSuccess)
                    {
                        fileBytes = null;
                        error = pLocalSerializationError;
                        return false;
                    }
                    else
                    {
                        fileBytes = GetFileBytes(tempFilePath);
                        error = null;
                        return true;
                    }
                }
                finally
                {
                    try { File.Delete(tempFilePath); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                fileBytes = null;
                error = ex;
                return false;
            }
        }

        public static bool TryDeserializeFromBytes<T>(byte[] data, string tempFilePrefix, string fileExt, out T result, out Exception error)
            where T : class
        {
            try
            {
                if (data == null || data.Length <= 0)
                    throw new InvalidDataException(string.Format("Byte[] data cannot be null or empty."));

                string tempFilename = string.Format("{0} tmp file - {1}{2}", tempFilePrefix, DateTime.Now.ToString("yyyy_MM_dd_hhmmss__fff_tt"), fileExt);
                string tempFilePath = Path.Combine(Path.GetTempPath(), tempFilename);
                try
                {
                    //  Write the bytes to a temp xml file, then deserialize the file
                    File.WriteAllBytes(tempFilePath, data);
                    result = Deserialize<T>(tempFilePath, true);
                    if (result == null)
                    {
                        error = new InvalidDataException();
                        return false;
                    }
                    else
                    {
                        error = null;
                        return true;
                    }
                }
                finally
                {
                    try { File.Delete(tempFilePath); }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                error = ex;
                result = null;
                return false;
            }
        }

        public static byte[] GetFileBytes(string filePath)
        {
            byte[] bytes;
            using (FileStream pFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                bytes = new byte[pFileStream.Length];
                pFileStream.Read(bytes, 0, (int)pFileStream.Length);
            }
            return bytes;
        }
    }
}