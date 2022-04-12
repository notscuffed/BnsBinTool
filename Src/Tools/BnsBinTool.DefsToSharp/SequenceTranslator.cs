using System.Collections.Generic;

namespace BnsBinTool.DefsToSharp
{
    public class SequenceTranslator
    {
        private static readonly Dictionary<string, string> _translations = new Dictionary<string, string>
        {
            // Genders
            {"남", "Male"},
            {"여", "Female"},
            {"중", "Neutral"},
            
            // Races
            {"린", "Lyn"},
            {"건", "Yun"},
            {"곤", "Gon"},
            {"진", "Jin"},
            
            // Jobs
            {"권사", "KungFuMaster"},
            {"역사", "Destroyer"},
            {"기공사", "ForceMaster"},
            {"검사", "BladeMaster"},
            {"암살자", "Assassin"},
            {"소환사", "Summoner"},
            {"귀검사", "BladeDancer"},
            {"주술사", "Warlock"},
            {"기권사", "SoulFighter"},
            {"격사", "Shooter"},
            {"투사", "Warrior"},
            {"궁사", "Archer"},
            {"뇌전술사", "Thunderer"}
        };
        
        public bool Translate(string input, out string translated)
        {
            return _translations.TryGetValue(input, out translated);
        }
    }
}