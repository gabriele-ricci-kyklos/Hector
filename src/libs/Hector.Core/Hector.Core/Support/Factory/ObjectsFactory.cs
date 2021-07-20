using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Hector.Core.Support.Factory
{
    public class ObjectsFactory
    {
        private FactoryObjectsCollection _objectList;
        private string _filePath;
        private string _fileContent;

        public static ObjectsFactory New(string filePath)
        {
            return new ObjectsFactory(filePath);
        }

        private ObjectsFactory(string filePath)
        {
            filePath.AssertNotNull("filePath");

            _objectList = new FactoryObjectsCollection();
            _filePath = filePath;
            _fileContent = ReadConfigurationFile();

            ParseConfigurationFile();
        }

        private string ReadConfigurationFile()
        {
            try
            {
                Uri uri = new Uri(_filePath);

                if (uri.Authority.IsNull())
                {
                    throw new ObjectsFactoryException("No assembly provided for the 'Plugins' resource");
                }

                if (uri.Segments.Length < 3)
                {
                    throw new ObjectsFactoryException("No namespace or file name provided for the 'Plugins' resource");
                }

                string namespaceName = uri.Segments[1].Replace("/", string.Empty);
                string fileName = uri.Segments[2];

                Assembly assemblyObj = Assembly.Load(uri.Authority);
                string result = string.Empty;

                using (Stream stream = assemblyObj.GetManifestResourceStream($"{namespaceName}.{fileName}"))
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        result = sr.ReadToEnd();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new ObjectsFactoryException(ex.Message, ex);
            }
        }

        private void ParseConfigurationFile()
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(_fileContent);

            XmlNode root = doc.DocumentElement.SelectSingleNode("/objects");
            if (root.IsNull())
            {
                throw new ObjectsFactoryException("Unable to find root 'objects' node");
            }

            foreach (XmlNode node in root.ChildNodes)
            {
                if (node.Name.ToLowerInvariant() != "object")
                {
                    throw new ObjectsFactoryException("Found unknown node '{0}'".FormatWith(node.Name));
                }

                string name = node.Attributes["name"].Value.ToString();
                string assembly = node.Attributes["assembly"].Value.ToString();
                string type = node.Attributes["type"].Value.ToString();

                if (name.IsNullOrEmpty())
                {
                    throw new ObjectsFactoryException("Found node missing 'name' attribute");
                }

                if (assembly.IsNullOrEmpty())
                {
                    throw new ObjectsFactoryException("Found node missing 'assembly' attribute");
                }

                if (type.IsNullOrEmpty())
                {
                    throw new ObjectsFactoryException("Found node missing 'type' attribute");
                }

                FactoryObject objDef =
                    new FactoryObject
                    {
                        Assembly = assembly,
                        Name = name,
                        Type = type
                    };

                _objectList.Add(objDef);
            }
        }

        public T GetObject<T>(string objName, params object[] args)
            where T : class
        {
            objName.AssertNotNull("objName");

            FactoryObject factoryObj = _objectList.GetValue(objName);

            if (factoryObj.IsNull())
            {
                throw new TypeLoadException("No configured objects found for name '{0}'".FormatWith(objName));
            }

            Assembly assemblyObj = Assembly.Load(factoryObj.Assembly);
            Type typeObj = assemblyObj.GetType(factoryObj.Type);
            object obj = Activator.CreateInstance(typeObj, args);
            return obj as T;
        }

        public bool ContainsObject<T>(string objName)
        {
            return _objectList.GetValue(objName).IsNotNull();
        }
    }
}
