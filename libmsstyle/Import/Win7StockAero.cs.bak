using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace libmsstyle.Import
{
    /// <summary>
    /// Stock windows 7 aero.msstyles, compiled into libmsstyle so the importer only needs the
    /// custom style. VisualStyle loads through LoadLibraryEx, which wants a file, so the bytes
    /// are written to %TEMP% once and reused.
    /// </summary>
    public static class Win7StockAero
    {
        private const string ResourceName = "libmsstyle.Import.w7_aero.msstyles";

        /// <summary>
        /// The font strings from stock windows 7's en-US aero.msstyles.mui. The embedded style is a
        /// lone .msstyles, so VisualStyle.Load can't resolve them. Without the text, a font whose
        /// string id is unchanged (8.x reuses ids, and a custom mui can rewrite the text behind an
        /// id) looks identical to stock and the import silently drops it.
        /// </summary>
        private static readonly Dictionary<int, string> StockFontStrings = new Dictionary<int, string>
        {
            { 501, "Segoe UI, 9, Quality:ClearType" },
            { 502, "Segoe UI, 12, Quality:ClearType" },
            { 503, "Segoe UI, 9, Bold, Quality:ClearType" },
            { 504, "Segoe UI, 9, Underline, Quality:ClearType" },
            { 505, "Segoe UI, 12, Underline, Quality:ClearType" },
            { 506, "Segoe UI, 10, Quality:ClearType" },
            { 507, "Segoe UI, 9, Italic, Quality:ClearType" },
        };

        /// <summary>
        /// Gives the embedded base the font table its .mui would have provided. Call this only for
        /// the built-in base: a user-supplied base loads its own companion .mui.
        /// </summary>
        public static void AddStockFontStrings(VisualStyle style)
        {
            if (style == null) throw new ArgumentNullException(nameof(style));
            if (style.StringTables.Any(t => t.Value.Count > 0)) return;

            var table = new Dictionary<int, string>(StockFontStrings);
            style.StringTables[1033] = table; // en-US, the only language stock aero ships
            foreach (var kv in table) style.PreferredStringTable[kv.Key] = kv.Value;
        }

        public static string GetPath()
        {
            using (var s = typeof(Win7StockAero).Assembly.GetManifestResourceStream(ResourceName))
            {
                if (s == null) throw new FileNotFoundException("stock win7 aero isn't embedded in this libmsstyle build");
                var ms = new MemoryStream();
                s.CopyTo(ms);
                byte[] data = ms.ToArray();

                // hash in the name: a different embedded copy never reuses a stale file.
                string sha = ResourceInventory.Sha1Hex(data).Substring(0, 12);
                string dir = Path.Combine(Path.GetTempPath(), "msstyleEditor");
                string path = Path.Combine(dir, $"w7_aero-{sha}.msstyles");
                if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(data)) return path;

                Directory.CreateDirectory(dir);
                string tmp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                File.WriteAllBytes(tmp, data);
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    File.Move(tmp, path);
                }
                catch (IOException)
                {
                    // another instance won the race and may have it mapped; its copy is identical.
                    File.Delete(tmp);
                    if (!File.Exists(path)) throw;
                }
                return path;
            }
        }
    }
}
