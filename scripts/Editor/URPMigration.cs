#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPMigration
{
    const string PipelineAssetPath = "Assets/Settings/URPAsset.asset";
    const string RendererAssetPath = "Assets/Settings/URPRenderer.asset";
    const string MaterialsFolder   = "Assets/Materials";

    [MenuItem("Why Gary/Setup URP")]
    public static void SetupURP()
    {
        SetupPipelineAsset();
        MigrateMaterials();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=lime><b>URP setup complete!</b> Re-enable OpenXR in XR Plug-in Management, then hit Play.</color>");
    }

    static void SetupPipelineAsset()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
        {
            Debug.Log("[URPMigration] URP pipeline asset already assigned — skipping.");
            return;
        }

        EnsureFolder("Assets/Settings");

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, RendererAssetPath);

        var urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(urpAsset, PipelineAssetPath);
        AssetDatabase.SaveAssets();

        GraphicsSettings.defaultRenderPipeline = urpAsset;
        Debug.Log($"[URPMigration] Pipeline asset created at {PipelineAssetPath}.");
    }

    static void MigrateMaterials()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogError("[URPMigration] URP/Lit shader not found — is the URP package installed?");
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        int count = 0;
        foreach (var guid in guids)
        {
            var path = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)) is Material mat ? AssetDatabase.GUIDToAssetPath(guid) : null;
            if (path == null) continue;
            mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat.shader == shader) continue;

            var color    = mat.HasProperty("_Color")         ? mat.GetColor("_Color")         : Color.white;
            var emission = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            bool emissive = mat.IsKeywordEnabled("_EMISSION");

            mat.shader = shader;
            mat.SetColor("_BaseColor", color);

            if (emissive)
            {
                mat.SetColor("_EmissionColor", emission);
                mat.EnableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(mat);
            count++;
        }
        Debug.Log($"[URPMigration] Migrated {count} material(s) to URP/Lit.");
    }

    static void EnsureFolder(string path)
    {
        var parts   = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
