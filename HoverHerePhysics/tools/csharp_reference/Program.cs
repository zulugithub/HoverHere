// Reference tool for the HoverHere physics port.
//
//   schema-seed --out <file.json>
//       Dumps the complete default parameter tree of Helicopter_Parameter.cs (structure, defaults, min/max, units,
//       hints, XmlIgnore flags, drop-down options) as JSON. Used once to seed schema/*.yaml.
//
//   golden --kind heli|scenery --file <parameter.xml> --out <golden.txt> [--merged <merged.xml>]
//       Loads a reduced parameter file exactly like the Unity application (XmlMerging over the full default tree,
//       XmlSerializer) and writes every leaf value as text. The C++ XML loader is tested against these files
//       (tests/data/golden). --merged keeps the merged XML for inspection.
//       Section "@input" holds the values as loaded, section "@calculated" the leaves flagged as calculated after
//       Update_Calculated_Parameter().
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;
using P = global::Parameter;

namespace HoverHere.Reference
{
    public static class Program
    {
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        public static int Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    return Usage();
                }

                Dictionary<string, string> options = ParseOptions(args.Skip(1).ToArray());
                switch (args[0])
                {
                    case "schema-seed":
                        return SchemaSeed(Require(options, "out"));
                    case "golden":
                        return Golden(Require(options, "kind"), Require(options, "file"), Require(options, "out"),
                                      options.TryGetValue("merged", out string merged) ? merged : null);
                    default:
                        return Usage();
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error: " + e.Message);
                return 1;
            }
        }

        private static int Usage()
        {
            Console.Error.WriteLine("usage: csharp_reference schema-seed --out <file.json>");
            Console.Error.WriteLine("       csharp_reference golden --kind heli|scenery --file <parameter.xml> --out <golden.txt> [--merged <merged.xml>]");
            return 2;
        }

        private static Dictionary<string, string> ParseOptions(string[] args)
        {
            var result = new Dictionary<string, string>();
            for (int i = 0; i + 1 < args.Length; i += 2)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal))
                {
                    throw new ArgumentException("unexpected argument " + args[i]);
                }
                result[args[i].Substring(2)] = args[i + 1];
            }
            return result;
        }

        private static string Require(Dictionary<string, string> options, string key)
        {
            if (!options.TryGetValue(key, out string value))
            {
                throw new ArgumentException("missing option --" + key);
            }
            return value;
        }

        // ------------------------------------------------------------------------------------------------------
        // reflection over the parameter classes
        // ------------------------------------------------------------------------------------------------------
        private static IEnumerable<PropertyInfo> OrderedProperties(Type type)
        {
            // base class members first, then the members of the derived class, each in declaration order
            var chain = new List<Type>();
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
            {
                chain.Insert(0, t);
            }
            foreach (Type t in chain)
            {
                foreach (PropertyInfo p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    if (p.GetIndexParameters().Length == 0 && p.CanRead && p.CanWrite)
                    {
                        yield return p;
                    }
                }
            }
        }

        private static bool IsXmlIgnored(PropertyInfo p)
        {
            return p.GetCustomAttribute<XmlIgnoreAttribute>() != null;
        }

        private static string Num(float v)
        {
            return v.ToString("R", Inv);
        }

        // ------------------------------------------------------------------------------------------------------
        // schema-seed
        // ------------------------------------------------------------------------------------------------------
        private static int SchemaSeed(string outPath)
        {
            var root = new P.Parameter();
            var doc = new JsonObject
            {
                ["source"] = "Helicopter_Parameter.cs (constructor defaults, Update_Calculated_Parameter() NOT applied)",
                ["root"] = SeedGroup(root, "Parameter"),
            };
            var json = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals,
            };
            File.WriteAllText(outPath, doc.ToJsonString(json) + "\n", new UTF8Encoding(false));
            Console.WriteLine("wrote " + outPath);
            return 0;
        }

        private static JsonObject SeedGroup(object group, string name)
        {
            var node = new JsonObject
            {
                ["name"] = name,
                ["kind"] = "group",
                ["csharp_type"] = group.GetType().Name,
            };
            var children = new JsonArray();
            foreach (PropertyInfo p in OrderedProperties(group.GetType()))
            {
                object value = p.GetValue(group);
                if (value == null)
                {
                    continue;
                }
                JsonObject child;
                if (value is P.stru_hint hint)
                {
                    child = SeedLeaf(hint, p.Name);
                }
                else if (p.PropertyType.IsClass && p.PropertyType != typeof(string))
                {
                    child = SeedGroup(value, p.Name);
                }
                else
                {
                    continue;
                }
                if (IsXmlIgnored(p))
                {
                    child["xml_ignore"] = true;
                }
                children.Add(child);
            }
            node["children"] = children;
            return node;
        }

        private static JsonObject SeedLeaf(P.stru_hint leaf, string name)
        {
            var n = new JsonObject { ["name"] = name, ["csharp_type"] = leaf.GetType().Name };
            switch (leaf)
            {
                case P.stru_float f:
                    n["kind"] = "float"; n["val"] = f.val; n["min"] = f.min; n["max"] = f.max;
                    break;
                case P.stru_int i:
                    n["kind"] = "int"; n["val"] = i.val; n["min"] = i.min; n["max"] = i.max;
                    break;
                case P.stru_bool b:
                    n["kind"] = "bool"; n["val"] = b.val;
                    break;
                case P.stru_list l:
                    n["kind"] = "list"; n["val"] = l.val;
                    n["options"] = new JsonArray((l.str ?? new List<string>()).Select(s => (JsonNode)JsonValue.Create(s)).ToArray());
                    break;
                case P.stru_Vector3 v:
                    n["kind"] = "vec3";
                    n["val"] = new JsonArray(JsonValue.Create(v.vect3.x), JsonValue.Create(v.vect3.y), JsonValue.Create(v.vect3.z));
                    break;
                case P.stru_Vector3_list vl:
                    n["kind"] = "vec3_list";
                    n["val"] = new JsonArray((vl.vect3 ?? new List<Vector3>()).Select(
                        e => (JsonNode)new JsonArray(JsonValue.Create(e.x), JsonValue.Create(e.y), JsonValue.Create(e.z))).ToArray());
                    break;
                default:
                    throw new InvalidOperationException("unknown leaf type " + leaf.GetType().Name);
            }
            n["unit"] = leaf.unit;
            n["hint"] = leaf.hint;
            n["comment"] = leaf.comment;
            n["calculated"] = leaf.calculated;
            n["show_in_sim"] = leaf.show_in_sim;
            n["save_under_player_prefs"] = leaf.save_under_player_prefs;
            return n;
        }

        // ------------------------------------------------------------------------------------------------------
        // golden
        // ------------------------------------------------------------------------------------------------------
        private sealed class Row
        {
            public string Path;
            public string Text;
            public bool Calculated;
        }

        private static int Golden(string kind, string file, string outPath, string keepMergedXml)
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "hoverhere_csharp_reference_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            try
            {
                Type rootType;
                object defaultTree;
                switch (kind)
                {
                    case "heli":
                        rootType = typeof(P.stru_transmitter_and_helicopter);
                        defaultTree = new P.stru_transmitter_and_helicopter();
                        break;
                    case "scenery":
                        rootType = typeof(P.stru_scenery);
                        defaultTree = new P.stru_scenery();
                        break;
                    default:
                        throw new ArgumentException("--kind must be heli or scenery");
                }

                // same sequence as Helicopter_Main.cs: serialize the full default tree, merge the reduced file over it,
                // deserialize the merged file (Helper.IO_XML_Deserialize)
                string defaultXml = Path.Combine(tempDir, "default_parameter.xml");
                string mergedXml = Path.Combine(tempDir, "merged_parameter.xml");
                var serializer = new XmlSerializer(rootType);
                using (var writer = new StreamWriter(defaultXml))
                {
                    serializer.Serialize(writer.BaseStream, defaultTree);
                }
                XmlMerging.MergeXmlFiles(file, defaultXml, mergedXml);
                if (keepMergedXml != null)
                {
                    File.Copy(mergedXml, keepMergedXml, true);
                }

                var xmlDocument = new XmlDocument();
                xmlDocument.Load(mergedXml);
                object loaded;
                using (var reader = new StringReader(xmlDocument.OuterXml))
                {
                    loaded = serializer.Deserialize(reader);
                }

                var input = new List<Row>();
                Flatten(loaded, "", input);

                if (loaded is P.stru_transmitter_and_helicopter tah)
                {
                    tah.Update_Calculated_Parameter();
                }
                var afterCalculation = new List<Row>();
                Flatten(loaded, "", afterCalculation);

                var sb = new StringBuilder();
                sb.Append("# Reference values written by tools/csharp_reference (golden). Do not edit.\n");
                sb.Append("# kind: ").Append(kind).Append('\n');
                sb.Append("# columns: path <TAB> value [<TAB> value ...]; lists of vec3: all components flattened\n");
                sb.Append("@input\n");
                foreach (Row r in input)
                {
                    sb.Append(r.Path).Append('\t').Append(r.Text).Append('\n');
                }
                sb.Append("@calculated\n");
                foreach (Row r in afterCalculation.Where(r => r.Calculated))
                {
                    sb.Append(r.Path).Append('\t').Append(r.Text).Append('\n');
                }
                File.WriteAllText(outPath, sb.ToString(), new UTF8Encoding(false));
                Console.WriteLine("wrote " + outPath + " (" + input.Count + " leaves)");
                return 0;
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        private static void Flatten(object group, string prefix, List<Row> rows)
        {
            foreach (PropertyInfo p in OrderedProperties(group.GetType()))
            {
                object value = p.GetValue(group);
                if (value == null || IsXmlIgnored(p) && !(value is P.stru_hint))
                {
                    continue;
                }
                string path = prefix.Length == 0 ? p.Name : prefix + "/" + p.Name;
                if (value is P.stru_hint leaf)
                {
                    string text = LeafText(leaf);
                    if (text != null)
                    {
                        rows.Add(new Row { Path = path, Text = text, Calculated = leaf.calculated || IsXmlIgnored(p) });
                    }
                }
                else if (p.PropertyType.IsClass && p.PropertyType != typeof(string))
                {
                    Flatten(value, path, rows);
                }
            }
        }

        private static string LeafText(P.stru_hint leaf)
        {
            switch (leaf)
            {
                case P.stru_float f: return Num(f.val);
                case P.stru_int i: return i.val.ToString(Inv);
                case P.stru_bool b: return b.val ? "1" : "0";
                case P.stru_list l: return l.val.ToString(Inv);
                case P.stru_Vector3 v: return Num(v.vect3.x) + "\t" + Num(v.vect3.y) + "\t" + Num(v.vect3.z);
                case P.stru_Vector3_list vl:
                    return string.Join("\t", (vl.vect3 ?? new List<Vector3>()).Select(e => Num(e.x) + "\t" + Num(e.y) + "\t" + Num(e.z)));
                default: return null;
            }
        }
    }
}
