// Verbatim copy of Common.Helper.XmlMerging (Helicopter_Common.cs, "merge XML - CODE by Wizbit",
// https://stackoverflow.com/questions/1892336/merge-two-xelements). It defines how the Unity application overlays a
// reduced helicopter/scenery parameter file (lhs, master) on the full default parameter tree (rhs).
// Only the doc comments were removed. Keep in sync with the original if that class ever changes.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace HoverHere.Reference
{
    public class XmlMerging
    {
        public static bool TryMergeXmlFiles(string lhsPath, string rhsPath,
            string resultPath, SaveOptions options = SaveOptions.OmitDuplicateNamespaces)
        {
            try
            {
                MergeXmlFiles(lhsPath, rhsPath, resultPath);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public static void MergeXmlFiles(string lhsPath, string rhsPath,
            string resultPath, SaveOptions options = SaveOptions.OmitDuplicateNamespaces)
        {
            XElement result =
                MergeElements(XElement.Load(lhsPath), XElement.Load(rhsPath));
            result.Save(resultPath, options);
        }

        public static XElement MergeElements(XElement lhs, XElement rhs)
        {
            if (rhs == null) return lhs;
            if (lhs == null) return rhs;

            XElement result = new XElement(lhs.Name);

            MergeAttributes(result, lhs.Attributes(), rhs.Attributes());

            MergeSubElements(result, lhs, rhs);
            return result;
        }

        private static void MergeAttributes(XElement elementToUpdate,
            IEnumerable<XAttribute> lhs, IEnumerable<XAttribute> rhs)
        {
            elementToUpdate.Add(lhs);

            List<XName> lhsAttributeNames =
                lhs.Select(attribute => attribute.Name).ToList();
            elementToUpdate.Add(rhs.Where(attribute =>
                !lhsAttributeNames.Contains(attribute.Name)));
        }

        private static void MergeSubElements(XElement elementToUpdate,
            XElement lhs, XElement rhs)
        {
            if (lhs.Elements().Count() > 0)
            {
                List<XName> lhsElementNames =
                    lhs.Elements().Select(element => element.Name).ToList();

                elementToUpdate.Add(
                    lhs.Elements().Select(
                        lhsElement =>
                            MergeElements(lhsElement, rhs.Element(lhsElement.Name))));

                elementToUpdate.Add(rhs.Elements().Where(element =>
                    !lhsElementNames.Contains(element.Name)));
            }
            else
            {
                if (!string.IsNullOrEmpty(lhs.Value))
                {
                    elementToUpdate.Value = lhs.Value;
                }
                else if (rhs.Elements().Count() > 0)
                {
                    elementToUpdate.Add(rhs.Elements());
                }
                else
                {
                    elementToUpdate.Value = rhs.Value;
                }
            }
        }
    }
}
