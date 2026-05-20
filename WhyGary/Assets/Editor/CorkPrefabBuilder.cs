#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CorkPrefabBuilder
{
    const string PrefabPath = "Assets/Prefabs/CorkProjectile.prefab";
    const string MatPath    = "Assets/Materials/CorkMaterial.mat";

    [MenuItem("Why Gary/Create Cork Prefab")]
    public static void CreateCorkPrefab()
    {
        EnsureFolder("Assets/Prefabs");

        // Build the prefab source object
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "CorkProjectile";
        go.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        else
            Debug.LogWarning("CorkMaterial not found — run 'Build Sandbox Scene' first, or assign manually.");

        var rb = go.AddComponent<Rigidbody>();
        rb.mass                   = 0.02f;
        rb.linearDamping          = 0.8f;
        rb.useGravity             = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;

        // SphereCollider already added by CreatePrimitive; just shrink to fit
        var col = go.GetComponent<SphereCollider>();
        col.radius = 0.5f; // local-space radius; world radius = 0.05 * 0.5 = 0.025m

        go.AddComponent<CorkProjectile>();

        // Set layer to "Cork" if it exists
        int corkLayer = LayerMask.NameToLayer("Cork");
        if (corkLayer != -1) go.layer = corkLayer;

        try
        {
            // Save as prefab
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("Cork Prefab Builder",
                    $"A prefab already exists at {PrefabPath}. Overwrite it?",
                    "Overwrite", "Cancel"))
                {
                    Object.DestroyImmediate(go);
                    go = null;
                    return;
                }
            }

            bool success;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath, out success);

            if (!success)
            {
                Debug.LogError("Failed to save CorkProjectile prefab.");
                return;
            }

            // Auto-assign to GunController in the active scene if one exists
            var gun = Object.FindAnyObjectByType<GunController>();
            if (gun != null && gun.bulletPrefab == null)
            {
                gun.bulletPrefab = prefab;
                EditorUtility.SetDirty(gun);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
                Debug.Log("<color=lime><b>Bullet prefab created and assigned to GunController.</b> You're ready to shoot!</color>");
            }
            else
            {
                Debug.Log($"<color=lime><b>Bullet prefab created at {PrefabPath}.</b> Drag it onto GunController.bulletPrefab in the Inspector.</color>");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
