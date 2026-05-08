using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using System.IO;
using System.Text;

namespace PlugInputPack.Editor
{
    /// <summary>
    /// Observa mudanças em qualquer .inputactions e regenera InputNames.cs automaticamente.
    /// Só escreve no disco se o conteúdo mudou — evita domain reload desnecessário.
    /// (Trocar binding sem renomear action = sem reload.)
    /// </summary>
    public class PlugInputNamesWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool relevant = false;
            foreach (string path in imported)
                if (path.EndsWith(".inputactions")) { relevant = true; break; }

            if (!relevant) return;

            // Encontra todos os PlugInputReader no projeto
            string[] readerGuids = AssetDatabase.FindAssets("t:PlugInputReader");
            if (readerGuids.Length == 0) return;

            foreach (string guid in readerGuids)
            {
                string readerPath = AssetDatabase.GUIDToAssetPath(guid);
                var reader = AssetDatabase.LoadAssetAtPath<PlugInputReader>(readerPath);
                if (reader?.InputActionAsset == null) continue;

                // Só regenera se o .inputactions importado pertence a este reader
                string assetPath = AssetDatabase.GetAssetPath(reader.InputActionAsset);
                bool matchesImport = false;
                foreach (string imp in imported)
                    if (imp == assetPath) { matchesImport = true; break; }
                if (!matchesImport) continue;

                // Salva InputNames.cs na mesma pasta do PlugInputReader
                string dir      = Path.GetDirectoryName(readerPath);
                string outPath  = Path.Combine(dir, "InputNames.cs").Replace('\\', '/');

                string newContent = BuildContent(reader.InputActionAsset);

                // Só escreve (e recompila) se o conteúdo mudou
                if (File.Exists(outPath) && File.ReadAllText(outPath) == newContent)
                    continue;

                File.WriteAllText(outPath, newContent, Encoding.UTF8);
                AssetDatabase.ImportAsset(outPath);
                Debug.Log($"[PlugInput] InputNames.cs atualizado em: {outPath}");
            }
        }

        private static string BuildContent(InputActionAsset asset)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// Gerado automaticamente por Plug Input Pack.");
            sb.AppendLine("// Atualizado sempre que o .inputactions mudar — não edite manualmente.");
            sb.AppendLine();
            sb.AppendLine("namespace PlugInputPack");
            sb.AppendLine("{");
            sb.AppendLine("    public static class InputNames");
            sb.AppendLine("    {");

            foreach (var map in asset.actionMaps)
            {
                if (asset.actionMaps.Count > 1)
                    sb.AppendLine($"        // {map.name}");

                foreach (var action in map.actions)
                {
                    string id = ToIdentifier(action.name);
                    sb.AppendLine($"        public const string {id} = \"{action.name}\";");
                }

                if (asset.actionMaps.Count > 1) sb.AppendLine();
            }

            sb.AppendLine("    }");
            sb.Append("}");
            return sb.ToString();
        }

        private static string ToIdentifier(string name)
        {
            var sb = new StringBuilder(name.Length);
            bool cap = false;
            foreach (char c in name)
            {
                if (c == ' ' || c == '-' || c == '_') { cap = true; continue; }
                if (!char.IsLetterOrDigit(c)) continue;
                if (sb.Length == 0 && char.IsDigit(c)) sb.Append('_');
                sb.Append(cap ? char.ToUpper(c) : c);
                cap = false;
            }
            return sb.Length > 0 ? sb.ToString() : "_" + name.GetHashCode().ToString("X");
        }
    }
}