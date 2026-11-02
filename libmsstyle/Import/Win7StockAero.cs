using System;
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
        private const string MuiResourceName = "libmsstyle.Import.w7_aero.msstyles.mui";
        private const string MuiLanguage = "en-US"; // the only language stock aero ships

        /// <summary>
        /// The bytes also include stock windows 7's en-US aero.msstyles.mui, written next to the
        /// style under the style's own file name. Without it the lone .msstyles can't resolve its
        /// font strings: an 8.x source reuses the same string ids and may rewrite the text behind
        /// one, and a font that looks unchanged by id would then be dropped silently. The mui keeps
        /// the font table the real file reads, instead of a table hardcoded here.
        /// </summary>
        public static string GetPath()
        {
            byte[] data = ReadResource(ResourceName);
            byte[] mui = ReadResource(MuiResourceName);

            // hash in the name: a different embedded copy never reuses a stale file.
            string sha = ResourceInventory.Sha1Hex(data).Substring(0, 12);
            string dir = Path.Combine(Path.GetTempPath(), "msstyleEditor");
            string path = Path.Combine(dir, $"w7_aero-{sha}.msstyles");
            string muiPath = Path.Combine(dir, MuiLanguage, Path.GetFileName(path) + ".mui");

            WriteIfDifferent(path, data);
            WriteIfDifferent(muiPath, mui);
            return path;
        }

        private static byte[] ReadResource(string name)
        {
            using (var s = typeof(Win7StockAero).Assembly.GetManifestResourceStream(name))
            {
                if (s == null) throw new FileNotFoundException("stock win7 aero isn't embedded in this libmsstyle build");
                var ms = new MemoryStream();
                s.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private static void WriteIfDifferent(string path, byte[] data)
        {
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(data)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(path));
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
        }
    }
}
