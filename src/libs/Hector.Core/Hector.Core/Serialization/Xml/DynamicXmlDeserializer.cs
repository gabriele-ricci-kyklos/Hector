using Hector.Core.Support;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Hector.Core.Serialization.Xml
{
    public class DynamicXmlDeserializer : DynamicObject
    {
        XElement _root;

        private DynamicXmlDeserializer(XElement root)
        {
            _root = root;
        }

        public static DynamicXmlDeserializer FromString(string xmlString)
        {
            XDocument doc = XDocument.Parse(xmlString);
            return new DynamicXmlDeserializer(doc.Root);
        }

        public static DynamicXmlDeserializer FromFile(string filename)
        {
            XDocument doc = XDocument.Load(filename);
            return new DynamicXmlDeserializer(doc.Root);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = null;

            XAttribute att = _root.Attribute(binder.Name);
            if (att.IsNotNull())
            {
                result = att.Value;
                return true;
            }

            var nodes = _root.Elements(binder.Name);
            if (nodes.Count() > 1)
            {
                result = 
                    nodes
                        .Select(n => n.HasElements ? (object)new DynamicXmlDeserializer(n) : n.Value)
                        .ToList();

                return true;
            }

            XElement node = _root.Element(binder.Name);
            if (node.IsNotNull())
            {
                result = node.HasElements ? (object)new DynamicXmlDeserializer(node) : node.Value;
                return true;
            }

            return true;
        }
    }
}
