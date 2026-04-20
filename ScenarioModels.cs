using System.Collections.Generic;

namespace MyShowController
{
    public class Scenario
    {
        public string Name { get; set; }
        public int UsageCount { get; set; } = 0;
        public List<ScenarioAction> Actions { get; set; } = new List<ScenarioAction>();
    }

    public class ScenarioAction
    {
        public int DelayMs { get; set; }
        public string Type { get; set; } // "os2l", "midi" ou "vdj"
        public string CommandName { get; set; }
        public int Note { get; set; }
        public int Velocity { get; set; }
    }

    // --- MODÈLE POUR LES MACROS VIRTUAL DJ ---
    public class VdjMacro
    {
        public string Name { get; set; }
        public string Command { get; set; }
    }

    // --- LA CLASSE QUI MANQUAIT POUR LE DESIGN ---
    public class DesignSettings
    {
        // Page
        public string PageBgType { get; set; } = "color";
        public string PageBgValue { get; set; } = "#121212";

        // Boutons
        public string BtnNormalType { get; set; } = "color";
        public string BtnNormalValue { get; set; } = "#00ff88";
        public string BtnPressedType { get; set; } = "color";
        public string BtnPressedValue { get; set; } = "#00cc66";

        public string BtnTextColor { get; set; } = "#000000";
        public string BtnShape { get; set; } = "10px";

        // Ombres et Lueurs
        public string BtnShadow { get; set; } = "none";
        public string BtnShadowColor { get; set; } = "#000000";
        public string BtnGlowColor { get; set; } = "#00ff88";

        // Polices
        public string BtnFont { get; set; } = "Arial, sans-serif";

        public List<string> SavedFonts { get; set; } = new List<string>
        {
            "Arial, sans-serif", "'Verdana', sans-serif", "'Courier New', monospace",
            "'Georgia', serif", "'Impact', sans-serif", "'Trebuchet MS', sans-serif", "'Comic Sans MS', cursive"
        };

        public List<string> CustomFontFiles { get; set; } = new List<string>();
    }
}