// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ClassicUO.Assets
{
    /// <summary>
    /// Translates names the server sends as plain text: NPC and creature names, their titles and the
    /// random personal names shards draw from (e.g. ModernUO's names.json). Unlike cliloc text these
    /// have no number, so a cliloc pack cannot reach them.
    ///
    /// The pack (e.g. Names.chs.txt next to the client files) has one entry per line: the English
    /// text, a tab, the translation. A full name like "Alita the noble" is looked up whole first,
    /// then as a name plus a title ("Alita" and "the noble"), so personal names and titles combine
    /// without listing every pair.
    /// </summary>
    public sealed class NameTranslation
    {
        private static readonly Regex HtmlText = new Regex(@"(?<=>)[^<>]+(?=<)|^[^<>]+(?=<)|(?<=>)[^<>]+$", RegexOptions.Compiled);

        private static readonly (string English, string Translated)[] StateSuffixes =
        {
            (" (tame)", " (已驯服)"),
            (" (summoned)", " (召唤)"),
            (" (bonded)", " (已绑定)")
        };

        private readonly Dictionary<string, string> _entries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public int Count => _entries.Count;

        public void Clear() => _entries.Clear();

        /// <returns>The number of entries read.</returns>
        public int Parse(IEnumerable<string> lines)
        {
            int added = 0;

            foreach (string line in lines)
            {
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                int tab = line.IndexOf('\t');

                if (tab <= 0 || tab == line.Length - 1)
                {
                    continue;
                }

                _entries[line.Substring(0, tab).Trim()] = string.Intern(line.Substring(tab + 1).Trim());
                added++;
            }

            return added;
        }

        /// <summary>Returns the translated name, or the text unchanged when nothing in it is known.</summary>
        public string Translate(string text)
        {
            if (_entries.Count == 0 || string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            // OPL name lines can carry colour tags; translate only the text between them
            if (text.IndexOf('<') >= 0)
            {
                return HtmlText.Replace(text, match => TranslatePadded(match.Value));
            }

            return TranslatePadded(text);
        }

        private string TranslatePadded(string text)
        {
            string trimmed = text.Trim();

            if (trimmed.Length == 0)
            {
                return text;
            }

            string translated = TranslateName(trimmed);

            if (ReferenceEquals(translated, trimmed))
            {
                return text;
            }

            int start = text.IndexOf(trimmed, StringComparison.Ordinal);

            return text.Substring(0, start) + translated + text.Substring(start + trimmed.Length);
        }

        private string TranslateName(string name)
        {
            if (_entries.TryGetValue(name, out string exact))
            {
                return exact;
            }

            foreach (var (english, translated) in StateSuffixes)
            {
                if (name.Length > english.Length && name.EndsWith(english, StringComparison.OrdinalIgnoreCase))
                {
                    string core = name.Substring(0, name.Length - english.Length);
                    string coreTranslated = TranslateName(core);

                    return ReferenceEquals(coreTranslated, core) ? name : coreTranslated + translated;
                }
            }

            // "<name> the <title>"; the title keeps its article in the pack ("the noble")
            int the = name.IndexOf(" the ", StringComparison.OrdinalIgnoreCase);

            if (the > 0)
            {
                string person = name.Substring(0, the);
                string title = name.Substring(the + 1);
                bool personKnown = _entries.TryGetValue(person, out string personTranslated);
                bool titleKnown = _entries.TryGetValue(title, out string titleTranslated);

                if (personKnown || titleKnown)
                {
                    return (personKnown ? personTranslated : person) + " " + (titleKnown ? titleTranslated : title);
                }
            }

            return name;
        }
    }
}
