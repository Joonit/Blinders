using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Blinders
{
    /// <summary>
    /// Localization. English is the default and the fallback for any missing key.
    /// Every language is an ini file (src\lang\xx.ini) embedded into the exe at build time.
    /// Extra languages (or overrides) can be dropped next to the exe as "lang\xx.ini" without rebuilding.
    /// </summary>
    static class L
    {
        public class Lang
        {
            public string Code, Name;
            public Dictionary<string, string> Map = new Dictionary<string, string>();
        }

        // Display order of the built-in languages (roughly by number of speakers). Others are appended.
        static readonly string[] Order = {
            "en", "zh", "zh-TW", "hi", "es", "fr", "ar", "bn", "pt", "ru", "ja", "de", "ko", "tr", "vi",
            "it", "th", "id", "pl", "uk", "nl", "fa", "he", "sv", "cs", "ro", "el", "hu", "da", "fi" };

        // Languages written right to left.
        static readonly string[] Rtl = { "ar", "fa", "he", "ur" };

        const string ResPrefix = "Blinders.lang.";

        static List<Lang> langs;
        static Lang cur, en;
        static List<string> keyOrder = new List<string>();

        /// <summary>Where user language files go (next to the settings file, so it is always writable).</summary>
        public static string LangDir
        {
            get { return Path.Combine(Settings.DataDir, "lang"); }
        }

        static IEnumerable<string> ExternalDirs()
        {
            string a = Path.Combine(Settings.ExeDir, "lang"), b = LangDir;
            yield return a;
            if (!string.Equals(a, b, StringComparison.OrdinalIgnoreCase)) yield return b;
        }

        public static string CurrentCode { get { return cur == null ? "en" : cur.Code; } }

        public static bool IsRtl
        {
            get { return Array.IndexOf(Rtl, CurrentCode.Split('-')[0].ToLowerInvariant()) >= 0; }
        }

        public static List<Lang> Available
        {
            get { if (langs == null) Init("auto"); return langs; }
        }

        /// <summary>setting: "auto" (system language, English if unsupported) or a language code.</summary>
        public static void Init(string setting)
        {
            langs = new List<Lang>();
            keyOrder.Clear();
            LoadBuiltIn();
            LoadExternal();
            langs.Sort(delegate(Lang a, Lang b)
            {
                int ia = Array.IndexOf(Order, a.Code), ib = Array.IndexOf(Order, b.Code);
                if (ia < 0) ia = 1000; if (ib < 0) ib = 1000;
                return ia != ib ? ia.CompareTo(ib) : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            en = Find("en");

            Lang pick = null;
            bool auto = string.IsNullOrEmpty(setting) || setting == "auto";
            if (!auto) pick = Find(setting);
            if (pick == null && auto)
            {
                CultureInfo ci = CultureInfo.CurrentUICulture;
                string n = ci.Name;
                if (n.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                {
                    bool trad = n.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0
                             || n.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0
                             || n.IndexOf("MO", StringComparison.OrdinalIgnoreCase) >= 0
                             || n.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0;
                    pick = Find(trad ? "zh-TW" : "zh");
                }
                else pick = Find(n) ?? Find(ci.TwoLetterISOLanguageName);
            }
            cur = pick ?? en;
        }

        static Lang Find(string code)
        {
            foreach (Lang l in langs)
                if (string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase)) return l;
            return null;
        }

        static void ParseInto(Lang l, IEnumerable<string> lines, bool recordKeys)
        {
            foreach (string raw in lines)
            {
                string line = raw.Trim();
                int eq = line.IndexOf('=');
                if (eq <= 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                string k = line.Substring(0, eq).Trim();
                string v = line.Substring(eq + 1).Trim().Replace("\\n", "\n");
                if (k == "name") l.Name = v;
                else
                {
                    l.Map[k] = v;
                    if (recordKeys) keyOrder.Add(k);
                }
            }
        }

        static void LoadBuiltIn()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            foreach (string res in asm.GetManifestResourceNames())
            {
                if (!res.StartsWith(ResPrefix) || !res.EndsWith(".ini")) continue;
                string code = res.Substring(ResPrefix.Length, res.Length - ResPrefix.Length - 4);
                Lang l = new Lang();
                l.Code = code; l.Name = code;
                List<string> lines = new List<string>();
                using (StreamReader sr = new StreamReader(asm.GetManifestResourceStream(res), Encoding.UTF8, true))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null) lines.Add(line);
                }
                ParseInto(l, lines, code == "en");
                langs.Add(l);
            }
        }

        static void LoadExternal()
        {
            foreach (string dir in ExternalDirs())
            try
            {
                if (!Directory.Exists(dir)) continue;
                foreach (string file in Directory.GetFiles(dir, "*.ini"))
                {
                    string code = Path.GetFileNameWithoutExtension(file);
                    if (code.StartsWith("_") || code.Equals("template", StringComparison.OrdinalIgnoreCase)) continue;
                    Lang l = Find(code);
                    bool isNew = l == null;
                    if (isNew) { l = new Lang(); l.Code = code; l.Name = code; }
                    ParseInto(l, File.ReadAllLines(file, Encoding.UTF8), false);
                    if (isNew) langs.Add(l);
                }
            }
            catch { }
        }

        public static string T(string key, params object[] args)
        {
            string s;
            if (cur == null || !cur.Map.TryGetValue(key, out s))
            {
                if (en == null || !en.Map.TryGetValue(key, out s)) s = key;
            }
            if (args != null && args.Length > 0)
            {
                try { s = string.Format(s, args); } catch { }
            }
            return s;
        }

        /// <summary>Writes lang\template.ini with every key and its English text.</summary>
        public static string ExportTemplate()
        {
            Directory.CreateDirectory(LangDir);
            string path = Path.Combine(LangDir, "template.ini");
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; Blinders language file. Save as <code>.ini (for example pt-BR.ini) in this folder.");
            sb.AppendLine("; Translate the text after '='. Keep {0}, {1} placeholders. Use \\n for a line break.");
            sb.AppendLine("; Missing keys fall back to English.");
            sb.AppendLine("name=Your language name");
            foreach (string k in keyOrder)
            {
                string v;
                if (en != null && en.Map.TryGetValue(k, out v)) sb.AppendLine(k + "=" + v.Replace("\n", "\\n"));
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            return path;
        }
    }
}
