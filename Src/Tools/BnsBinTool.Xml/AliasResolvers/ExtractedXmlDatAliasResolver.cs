using BnsBinTool.Core.DataStructs;
using BnsBinTool.Core.Definitions;
using BnsBinTool.Core.Helpers;

namespace BnsBinTool.Xml.AliasResolvers
{
    public static class ExtractedXmlDatAliasResolver
    {
        public static void Resolve(ResolvedAliases resolvedAliases, DatafileDefinition datafileDef, string extractedXmlDatPath, string extractedLocalDatPath)
        {
            if (extractedXmlDatPath != null)
            {
                {
                    var byAlias = resolvedAliases.ByAlias[datafileDef["quest"].Type];
                    var byRef = resolvedAliases.ByRef[datafileDef["quest"].Type];
                    XmlRecordsHelper.EnumerateRecords("xml.dat", extractedXmlDatPath, "quest/questdata*.xml", "quest", record =>
                    {
                        if (record.TryGetValue("alias", out var alias)
                            && record.TryGetValue("id", out var id_str)
                            && int.TryParse(id_str, out var id))
                        {
                            byAlias[alias] = new Ref(id);
                            byRef[new Ref(id)] = alias;
                        }
                    });
                }

                if (datafileDef.TryGetValue("tutorialskillsequence", out _))
                {
                    var byAlias = resolvedAliases.ByAlias[datafileDef["tutorialskillsequence"].Type];
                    var byRef = resolvedAliases.ByRef[datafileDef["tutorialskillsequence"].Type];
                    var autoId = 1;
                    XmlRecordsHelper.EnumerateRecords("xml.dat", extractedXmlDatPath, "tutorialskillsequencedata*.xml", "tutorialSkillSequence", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            byAlias[alias] = new Ref(autoId);
                            byRef[new Ref(autoId)] = alias;
                        }

                        autoId++;
                    });
                }

                {
                    var byAlias = resolvedAliases.ByAlias[datafileDef["summoned-sequence"].Type];
                    var byRef = resolvedAliases.ByRef[datafileDef["summoned-sequence"].Type];
                    var autoId = 1;
                    XmlRecordsHelper.EnumerateRecords("xml.dat", extractedXmlDatPath, "summonedsequencedata*.xml", "summoned-sequence", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            byAlias[alias] = new Ref(autoId);
                            byRef[new Ref(autoId)] = alias;
                        }

                        autoId++;
                    });
                }

                if (datafileDef.TryGetValue("skill-training-sequence", out _))
                {
                    var byAlias = resolvedAliases.ByAlias[datafileDef["skill-training-sequence"].Type];
                    var byRef = resolvedAliases.ByRef[datafileDef["skill-training-sequence"].Type];
                    var autoId = 1;
                    XmlRecordsHelper.EnumerateRecords("xml.dat", extractedXmlDatPath, "skilltrainingsequencedata*.xml", "skill-training-sequence", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            byAlias[alias] = new Ref(autoId);
                            byRef[new Ref(autoId)] = alias;
                        }

                        autoId++;
                    });
                }

                {
                    var byAlias = resolvedAliases.ByAlias[datafileDef["contextscript"].Type];
                    var byRef = resolvedAliases.ByRef[datafileDef["contextscript"].Type];
                    var autoId = 1;
                    XmlRecordsHelper.EnumerateRecords("xml.dat", extractedXmlDatPath, "skill3_contextscriptdata*.xml", "contextscript", record =>
                    {
                        if (record.TryGetValue("alias", out var alias))
                        {
                            byAlias[alias] = new Ref(autoId);
                            byRef[new Ref(autoId)] = alias;
                        }

                        autoId++;
                    });
                }
            }

            if (extractedLocalDatPath != null && datafileDef.TryGetValue("surveyquestions", out _))
            {
                var byAlias = resolvedAliases.ByAlias[datafileDef["surveyquestions"].Type];
                var byRef = resolvedAliases.ByRef[datafileDef["surveyquestions"].Type];
                var autoId = 1;
                XmlRecordsHelper.EnumerateRecords("local.dat", extractedLocalDatPath, "outsource/surveyquestions.x16", "surveyQuestion", record =>
                {
                    if (record.TryGetValue("alias", out var alias))
                    {
                        byAlias[alias] = new Ref(autoId);
                        byRef[new Ref(autoId)] = alias;
                    }

                    autoId++;
                });
            }
        }
    }
}