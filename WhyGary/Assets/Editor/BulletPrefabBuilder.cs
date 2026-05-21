#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class BulletPrefabBuilder
{
    const string PrefabPath = "Assets/Prefabs/BulletProjectile.prefab";
    const string MatPath    = "Assets/Materials/BulletMaterial.mat";

    [MenuItem("Why Gary/Create Bullet Prefab")]
    public static void CreateBulletPrefab() => BuildPrefab(interactive: true);

    // Called by scene builders to auto-create without dialog prompts.
    public static GameObject BuildPrefab(bool interactive = false)
    {
        EnsureFolder("Assets/Prefabs");

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "BulletProjectile";
        go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        else
            Debug.LogWarning("BulletMaterial not found — run 'Build Why Gary Scene' first, or assign manually.");

        var rb = go.AddComponent<Rigidbody>();
        rb.mass                   = 0.02f;
        rb.linearDamping          = 0.8f;
        rb.useGravity             = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;

        // SphereCollider already added by CreatePrimitive; just shrink to fit
        var col = go.GetComponent<SphereCollider>();
        col.radius = 0.5f; // local-space radius; world radius = 0.05 * 0.5 = 0.025m

        go.AddComponent<BulletProjectile>();

        try
        {
            if (interactive && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Bullet Prefab Builder",
                    $"A prefab already exists at {PrefabPath}. Overwrite it?",
                    "Overwrite", "Cancel"))
                {
                    Object.DestroyImmediate(go);
                    go = null;
                    return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                }
            }

            bool success;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out success);

            if (!success)
            {
                Debug.LogError("Failed to save BulletProjectile prefab.");
                return null;
            }

            var gun = Object.FindAnyObjectByType<GunController>();
            if (gun != null && gun.bulletPrefab == null)
            {
                gun.bulletPrefab = prefab;
                EditorUtility.SetDirty(gun);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }

            if (interactive)
                Debug.Log($"<color=lime><b>Bullet prefab created at {PrefabPath}.</b></color>");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }
        finally
        {
            if (go != null) Object.DestroyImmediate(go);
        }
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
