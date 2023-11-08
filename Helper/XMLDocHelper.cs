using Smartstore;
using System.Xml.Linq;
using System.Xml;
using System.IO;
using Smartstore.Utilities;
using System.Collections;
using System.Resources;
using NUglify;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using Org.BouncyCastle.Asn1.Ocsp;
using AngleSharp.Dom;

namespace ML.CMS.Helpers
{
    public class XMLDocHelper
    {
        public XDocument Content { get; set; }
        //Concstructor
        public XMLDocHelper(XDocument XDocument)
        {
            Content = XDocument;
        }

        public static string GetElementFullPath(XElement element)
        {
            string path = element.Name.LocalName;

            while (element.Parent != null && element.Parent.NodeType == System.Xml.XmlNodeType.Element)
            {
                element = element.Parent as XElement;
                path = element.Name.LocalName + "/" + path;
            }

            return path;
        }

        public string GetValue(string Name)
        {
            XElement element = Content.Descendants()
                .FirstOrDefault(e => e.Attribute("Name")?.Value == Name);
            return element?.Element("Value").Value;
        }

        public void ChangeValue(string elementName, string newValue)
        {
            XElement element = Content.Descendants()
                .FirstOrDefault(e => e.Attribute("Name")?.Value == elementName);

            if (element != null)
            {
                // Update the existing "Value" element with the new value
                XElement valueElement = element.Element("Value");
                if (valueElement != null)
                {
                    valueElement.SetValue(newValue);
                }
                else
                {
                    element.Add(new XElement("Value", newValue));
                }
            }
            else
            {
                // Create a new element if it doesn't exist
                Content.Root.Add(new XElement("LocaleResource",
                    new XAttribute("Name", elementName),
                    new XAttribute("AppendRootKey", false),
                    new XElement("Value", newValue)));
            }
        }

        public void sortElements()
        {
            IEnumerable<XElement> sortedElements = Content.Root.Elements()
                .Where(e => e.Attribute("Name") != null)
                .OrderBy(e => (string)e.Attribute("Name"));
            Content.Root.ReplaceNodes(sortedElements);
        }

        private List<XElement> FlattenChildrenName(XElement node)
        {
            List <XElement> flattenedChildren = new List<XElement>();
            string parentName = node.Parent.Attribute("Name")?.Value ?? string.Empty;
            if (!string.IsNullOrEmpty(parentName))
            {
                parentName += ".";
            }
            var children = node.Elements();
            foreach (XElement child in children)
            {
                string childName = child.Attribute("Name")?.Value ?? string.Empty;
                child.Attribute("Name").Value = parentName + childName;
                flattenedChildren.Add(child);
            }
            return flattenedChildren;
        }

        public void SetAppendRoot(bool setroot = false)
        {
            Guard.NotNull(Content);

            XAttribute AppendRootKeyAttribute = new XAttribute("AppendRootKey", setroot);
            foreach (var element in Content.Descendants("LocaleResource")) {
                //check if the attribute already exists
                if (element.Attribute("AppendRootKey") == null) {
                    element.Add(AppendRootKeyAttribute);
                }
            }
        }

        public void FlattenResourceFile()
        {
            Guard.NotNull(Content);

            int childrenCount = Content.Descendants("Children").Count();

            if (childrenCount == 0)
            {
                return;
            }

            XDocument flatSource = new XDocument(Content);

            int maxIterations = childrenCount;
            int counter = 0;
            while (flatSource.Descendants("Children").Any() && counter < maxIterations)
            {
                XElement node = flatSource.Descendants("Children").FirstOrDefault();
                XElement nodeParent = node.Parent;
                List<XElement> flattenedChildren = FlattenChildrenName(node);
                nodeParent.Parent.Add(flattenedChildren);
                nodeParent.Remove();
                counter++;
            }

            Content = flatSource;
        }

        public List<XElement> GetDuplicates()
        {
            Guard.NotNull(Content);

            var duplicates = Content.Descendants()
                .Where(x => x.Attribute("Name") != null)
                .GroupBy(x => x.Attribute("Name").Value)
                .Where(g => g.Count() > 1)
                .Select(g => g.First());

            return duplicates.ToList();
        }

    }

}

