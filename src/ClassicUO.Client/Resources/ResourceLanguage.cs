// SPDX-License-Identifier: BSD-2-Clause

using System;
using System.Collections.Generic;
using System.Globalization;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Resources
{
    /// <summary>
    /// Switches the client's own texts (options, prompts, errors) to the translation matching the
    /// UO language code that also selects the cliloc file (e.g. CHS -> Cliloc.chs).
    /// </summary>
    internal static class ResourceLanguage
    {
        // UO language code -> culture of a bundled translation (ResGumps.zh-CN.resx, ...).
        // Region-style names on purpose: in invariant globalization mode script subtags are
        // normalized to upper case ("zh-HANS"), which then misses the "zh-Hans" satellite
        // folder on case-sensitive file systems (iOS).
        private static readonly Dictionary<string, string> Translations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["CHS"] = "zh-CN"
        };

        /// <returns>The culture applied, or null when the client keeps its English texts.</returns>
        public static CultureInfo Apply(string uoLanguage)
        {
            CultureInfo culture = null;

            if (!string.IsNullOrEmpty(uoLanguage) && Translations.TryGetValue(uoLanguage, out string name))
            {
                try
                {
                    culture = CultureInfo.GetCultureInfo(name);
                }
                catch (CultureNotFoundException ex)
                {
                    Log.Warn($"Translation '{name}' unavailable, using English texts: {ex.Message}");
                }
            }

            // Invariant (the English originals) rather than null: null would follow the OS language
            // and could mix a translated UI with English client texts
            CultureInfo applied = culture ?? CultureInfo.InvariantCulture;
            ResGumps.Culture = applied;
            ResGeneral.Culture = applied;
            ResErrorMessages.Culture = applied;
            Log.Trace($"Client texts: '{applied.Name}' (sample: {ResGumps.Cancel})");

            return culture;
        }
    }
}
