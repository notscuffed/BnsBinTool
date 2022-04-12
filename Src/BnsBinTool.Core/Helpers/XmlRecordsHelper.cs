using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using BnsBinTool.Core.DataStructs;
using NameTable = BnsBinTool.Core.Models.NameTable;

namespace BnsBinTool.Core.Helpers
{
    public class XmlRecordsHelper
    {
        public static void LoadAliasesToRebuilder(NameTable.Rebuilder rebuilder, string extractedXmlDatPath, string extractedLocalDatPath)
        {
            if (extractedXmlDatPath != null)
            {
                EnumerateRecords("xml.dat", extractedXmlDatPath, "quest/questdata*.xml", "quest", record =>
                {
                    if (record.TryGetValue("alias", out var alias)
                        && record.TryGetValue("id", out var id_str)
                        && int.TryParse(id_str, out var id))
                    {
                        rebuilder.AddAliasManually(
                            "quest:" + alias.ToLowerInvariant(),
                            new Ref(id)
                        );
                    }
                });

                {
                    var autoId = 1;
                    EnumerateRecords("xml.dat", extractedXmlDatPath, "tutorialskillsequencedata*.xml", "tutorialSkillSequence", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            rebuilder.AddAliasManually(
                                "tutorialskillsequence:" + alias.ToLowerInvariant(),
                                new Ref(autoId)
                            );
                        }

                        autoId++;
                    });
                }

                {
                    var autoId = 1;
                    EnumerateRecords("xml.dat", extractedXmlDatPath, "summonedsequencedata*.xml", "summoned-sequence", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            rebuilder.AddAliasManually(
                                "summoned-sequence:" + alias.ToLowerInvariant(),
                                new Ref(autoId)
                            );
                        }

                        autoId++;
                    });
                }

                {
                    var autoId = 1;
                    EnumerateRecords("xml.dat", extractedXmlDatPath, "skilltrainingsequencedata*.xml", "skill-training-sequence", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            rebuilder.AddAliasManually(
                                "skill-training-sequence:" + alias.ToLowerInvariant(),
                                new Ref(autoId)
                            );
                        }

                        autoId++;
                    });
                }

                {
                    var autoId = 1;
                    EnumerateRecords("xml.dat", extractedXmlDatPath, "skill3_contextscriptdata*.xml", "contextscript", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            rebuilder.AddAliasManually(
                                "contextscript:" + alias.ToLowerInvariant(),
                                new Ref(autoId)
                            );
                        }

                        autoId++;
                    });
                }
            }

            if (extractedLocalDatPath != null)
            {
                {
                    var autoId = 1;
                    EnumerateRecords("local.dat", extractedLocalDatPath, "outsource/surveyquestions.x16", "surveyQuestion", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            rebuilder.AddAliasManually(
                                "surveyquestions:" + alias.ToLowerInvariant(),
                                new Ref(autoId)
                            );
                        }

                        autoId++;
                    });
                }
            }
        }

        public static void EnumerateRecords(string datName, string extractedDatRootPath, string pattern, string elementName, Action<Dictionary<string, string>> processRecord)
        {
            extractedDatRootPath = Path.GetFullPath(extractedDatRootPath);
            
            foreach (var fullFilePath in Directory.EnumerateFiles(extractedDatRootPath, pattern, SearchOption.AllDirectories))
            {
                var filePath = datName + Path.DirectorySeparatorChar + fullFilePath[(extractedDatRootPath.Length + 1)..];
                
                using var reader = new XmlFileReader(fullFilePath, filePath)
                {
                    WhitespaceHandling = WhitespaceHandling.None
                };

                if (!reader.Read() || reader.NodeType != XmlNodeType.XmlDeclaration)
                    reader.ThrowException("Failed to read xml declaration");

                while (!reader.Read() || reader.Name != "table")
                {
                    if (reader.NodeType == XmlNodeType.Comment)
                        continue;

                    reader.ThrowException("Failed to read table element");
                }

                reader.Read();

                var record = new Dictionary<string, string>();

                while (!reader.EOF)
                {
                    if (reader.NodeType == XmlNodeType.EndElement)
                        break;

                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == elementName)
                        {
                            if (reader.MoveToFirstAttribute())
                            {
                                record.Clear();
                                do
                                {
                                    record[reader.Name] = reader.Value;
                                } while (reader.MoveToNextAttribute());

                                processRecord(record);
                            }
                        }

                        reader.Skip();
                    }
                    else
                        reader.Read();
                }
            }
        }
    }
}