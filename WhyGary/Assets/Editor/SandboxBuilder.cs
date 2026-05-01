#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public static class SandboxBuilder
{
    const string MatPath = "Assets/Materials";
    static readonly string[] CustomTags = { "NPCHead", "NPCTorso", "NPCArm", "NPCEscort" };

    [MenuItem("Why Gary/Build Sandbox Scene")]
    public static void BuildSandbox()
    {
        EnsureTags();
        EnsureFolder(MatPath);
        var m = BuildMaterials();

        CreateRoom(m);
        var player = CreatePlayerBody(m);
        var npc    = CreateNPC(m);
        CreateTable(m);
        CreateCorkGun(m);
        var gesture = CreateGestureSystem();

        WirePlayerBodyDriver(player);
        WireWaveDetector(gesture, player, npc.reaction);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=lime><b>Why Gary: Sandbox scene built!</b> One remaining step: assign CorkGun.corkPrefab in the Inspector after creating the cork prefab.</color>");
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    static void EnsureTags()
    {
        var so   = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = so.FindProperty("tags");
        foreach (var tag in CustomTags)
        {
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (!exists)
            {
                tags.arraySize++;
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
        }
        so.ApplyModifiedProperties();
    }

    // ── Materials ─────────────────────────────────────────────────────────────

    struct Mats
    {
        public Material player, npc, npcDead, room, gun, cork;
    }

    static Mats BuildMaterials() => new Mats
    {
        player  = Mat("PlayerMaterial",  new Color(0.20f, 0.40f, 0.80f)),
        npc     = Mat("NPCMaterial",     new Color(0.50f, 0.50f, 0.50f)),
        npcDead = Mat("NPCDeadMaterial", new Color(0.70f, 0.15f, 0.10f)),
        room    = Mat("RoomMaterial",    new Color(0.70f, 0.70f, 0.65f)),
        gun     = Mat("GunMaterial",     new Color(0.20f, 0.20f, 0.20f)),
        cork    = Mat("CorkMaterial",    new Color(0.85f, 0.70f, 0.50f)),
    };

    static Material Mat(string name, Color color)
    {
        string path = $"{MatPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ── Room ──────────────────────────────────────────────────────────────────

    static void CreateRoom(Mats m)
    {
        var room = Empty("Room");

        // Plane default is 10×10m, so scale 0.8 = 8×8m
        var floor = Prim("Floor", PrimitiveType.Plane, room);
        floor.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        SetMat(floor, m.room);

        RoomWall("WallNorth", room, new Vector3(0,    1.5f, -4f), new Vector3(8f,    3f, 0.2f), m.room);
        RoomWall("WallSouth", room, new Vector3(0,    1.5f,  4f), new Vector3(8f,    3f, 0.2f), m.room);
        RoomWall("WallEast",  room, new Vector3( 4f,  1.5f,  0 ), new Vector3(0.2f,  3f, 8f  ), m.room);
        RoomWall("WallWest",  room, new Vector3(-4f,  1.5f,  0 ), new Vector3(0.2f,  3f, 8f  ), m.room);
    }

    static void RoomWall(string name, GameObject parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var w = Prim(name, PrimitiveType.Cube, parent);
        w.transform.position   = pos;
        w.transform.localScale = scale;
        SetMat(w, mat);
    }

    // ── Player Body ───────────────────────────────────────────────────────────

    struct PlayerParts
    {
        public GameObject root, head, torso, leftHand, rightHand;
    }

    static PlayerParts CreatePlayerBody(Mats m)
    {
        var root = Empty("PlayerBody");

        var head  = Part("Head",         root, PrimitiveType.Sphere,   new Vector3(0.24f, 0.24f, 0.24f), new Vector3(0,      1.65f, 0), m.player);
        var torso = Part("Torso",         root, PrimitiveType.Capsule,  new Vector3(0.35f, 0.55f, 0.25f), new Vector3(0,      1.20f, 0), m.player);
        Part("LeftUpperArm",  root, PrimitiveType.Capsule,  new Vector3(0.12f, 0.28f, 0.12f), new Vector3(-0.28f, 1.45f, 0), m.player);
        Part("LeftForearm",   root, PrimitiveType.Capsule,  new Vector3(0.10f, 0.25f, 0.10f), new Vector3(-0.28f, 1.10f, 0), m.player);
        var lHand = Part("LeftHand",  root, PrimitiveType.Cube,     new Vector3(0.12f, 0.08f, 0.16f), new Vector3(-0.28f, 0.85f, 0), m.player);
        Part("RightUpperArm", root, PrimitiveType.Capsule,  new Vector3(0.12f, 0.28f, 0.12f), new Vector3( 0.28f, 1.45f, 0), m.player);
        Part("RightForearm",  root, PrimitiveType.Capsule,  new Vector3(0.10f, 0.25f, 0.10f), new Vector3( 0.28f, 1.10f, 0), m.player);
        var rHand = Part("RightHand", root, PrimitiveType.Cube,     new Vector3(0.12f, 0.08f, 0.16f), new Vector3( 0.28f, 0.85f, 0), m.player);

        // No physics on the player body — purely visual
        foreach (var c in root.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        root.AddComponent<PlayerBodyDriver>();

        return new PlayerParts
        {
            root      = root,
            head      = head.gameObject,
            torso     = torso.gameObject,
            leftHand  = lHand.gameObject,
            rightHand = rHand.gameObject,
        };
    }

    // ── NPC ───────────────────────────────────────────────────────────────────

    struct NPCParts { public GameObject root; public NPCReaction reaction; }

    static NPCParts CreateNPC(Mats m)
    {
        var root = Empty("NPC_Dummy");
        root.transform.position = new Vector3(0, 0, 3);
        root.transform.rotation = Quaternion.Euler(0, 180, 0);

        // Torso = ragdoll root
        var torsoGo = Part("Torso", root, PrimitiveType.Capsule, new Vector3(0.35f, 0.55f, 0.25f), new Vector3(0, 1.20f, 0), m.npc).gameObject;
        torsoGo.tag = "NPCTorso";
        var torsoRb = RagdollRb(torsoGo, 10f);

        var headGo = Part("Head", root, PrimitiveType.Sphere, new Vector3(0.24f, 0.24f, 0.24f), new Vector3(0, 1.65f, 0), m.npc).gameObject;
        headGo.tag = "NPCHead";
        RagdollRb(headGo, 4f);
        Joint(headGo, torsoRb, 40f);

        var lUARb = Limb("LeftUpperArm",  root, PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3(-0.28f, 1.45f, 0), m.npc, torsoRb, 80f);
        var lFARb = Limb("LeftForearm",   root, PrimitiveType.Capsule, new Vector3(0.10f, 0.25f, 0.10f), new Vector3(-0.28f, 1.10f, 0), m.npc, lUARb,  90f);
                   Limb("LeftHand",      root, PrimitiveType.Cube,    new Vector3(0.12f, 0.08f, 0.16f), new Vector3(-0.28f, 0.85f, 0), m.npc, lFARb,  60f);
        var rUARb = Limb("RightUpperArm", root, PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3( 0.28f, 1.45f, 0), m.npc, torsoRb, 80f);
        var rFARb = Limb("RightForearm",  root, PrimitiveType.Capsule, new Vector3(0.10f, 0.25f, 0.10f), new Vector3( 0.28f, 1.10f, 0), m.npc, rUARb,  90f);
                   Limb("RightHand",     root, PrimitiveType.Cube,    new Vector3(0.12f, 0.08f, 0.16f), new Vector3( 0.28f, 0.85f, 0), m.npc, rFARb,  60f);

        foreach (string n in new[]{"LeftUpperArm","LeftForearm","LeftHand","RightUpperArm","RightForearm","RightHand"})
            root.transform.Find(n).gameObject.tag = "NPCArm";

        // Scripts
        root.AddComponent<NPCHealth>();
        var ragdoll  = root.AddComponent<NPCRagdoll>();
        var reaction = root.AddComponent<NPCReaction>();
        ragdoll.deadMaterial = m.npcDead;
        reaction.npcHead     = headGo.transform;

        // XRGrabInteractable on Torso (grab event auto-wired in NPCRagdoll.Start)
        var grab = torsoGo.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        return new NPCParts { root = root, reaction = reaction };
    }

    static Rigidbody Limb(string name, GameObject parent, PrimitiveType type, Vector3 scale, Vector3 pos, Material mat, Rigidbody connectedTo, float swing)
    {
        var go = Part(name, parent, type, scale, pos, mat).gameObject;
        var rb = RagdollRb(go, 2f);
        Joint(go, connectedTo, swing);
        return rb;
    }

    static Rigidbody RagdollRb(GameObject go, float mass)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass                  = mass;
        rb.isKinematic           = true;
        rb.interpolation         = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return rb;
    }

    static void Joint(GameObject go, Rigidbody connectedTo, float swing)
    {
        var j = go.AddComponent<CharacterJoint>();
        j.connectedBody  = connectedTo;
        j.swing1Limit    = new SoftJointLimit { limit =  swing };
        j.swing2Limit    = new SoftJointLimit { limit =  swing * 0.5f };
        j.lowTwistLimit  = new SoftJointLimit { limit = -swing * 0.5f };
        j.highTwistLimit = new SoftJointLimit { limit =  swing * 0.5f };
    }

    // ── Table ─────────────────────────────────────────────────────────────────

    static void CreateTable(Mats m)
    {
        var table = Empty("Table");
        table.transform.position = new Vector3(1.2f, 0, 1.5f);

        var top = Prim("Tabletop", PrimitiveType.Cube, table);
        top.transform.localPosition = new Vector3(0, 0.75f, 0);
        top.transform.localScale    = new Vector3(1.0f, 0.05f, 0.6f);
        SetMat(top, m.room);

        (Vector3 pos, string name)[] legs = {
            (new Vector3(-0.45f, 0.375f,  0.25f), "Leg_FL"),
            (new Vector3( 0.45f, 0.375f,  0.25f), "Leg_FR"),
            (new Vector3(-0.45f, 0.375f, -0.25f), "Leg_BL"),
            (new Vector3( 0.45f, 0.375f, -0.25f), "Leg_BR"),
        };
        foreach (var (pos, name) in legs)
        {
            var leg = Prim(name, PrimitiveType.Cube, table);
            leg.transform.localPosition = pos;
            leg.transform.localScale    = new Vector3(0.06f, 0.75f, 0.06f);
            SetMat(leg, m.room);
        }
    }

    // ── Cork Gun ──────────────────────────────────────────────────────────────

    static void CreateCorkGun(Mats m)
    {
        var gun = Empty("CorkGun");
        gun.transform.position = new Vector3(1.2f, 0.83f, 1.5f);
        gun.transform.rotation = Quaternion.Euler(0, 90, 0);

        var body = Prim("GunBody", PrimitiveType.Cube, gun);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale    = new Vector3(0.08f, 0.15f, 0.25f);
        SetMat(body, m.gun);

        var barrel = Prim("Barrel", PrimitiveType.Cylinder, gun);
        barrel.transform.localPosition = new Vector3(0, 0.04f, 0.18f);
        barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
        barrel.transform.localScale    = new Vector3(0.04f, 0.09f, 0.04f);
        SetMat(barrel, m.gun);

        var cork = Prim("Cork", PrimitiveType.Sphere, gun);
        cork.transform.localPosition = new Vector3(0, 0.04f, 0.33f);
        cork.transform.localScale    = new Vector3(0.05f, 0.05f, 0.05f);
        SetMat(cork, m.cork);

        var firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(gun.transform, false);
        firePoint.transform.localPosition = new Vector3(0, 0.04f, 0.36f);

        var rb  = gun.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        var col = gun.AddComponent<BoxCollider>();
        col.size = new Vector3(0.08f, 0.15f, 0.25f);

        var grab = gun.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        var script = gun.AddComponent<CorkGun>();
        script.firePoint  = firePoint.transform;
        script.corkVisual = cork;
        // script.corkPrefab — set this in Inspector after creating the CorkProjectile prefab
    }

    // ── Gesture System ────────────────────────────────────────────────────────

    static GameObject CreateGestureSystem()
    {
        var gs = Empty("GestureSystem");
        gs.AddComponent<WaveDetector>();
        return gs;
    }

    // ── Wiring ────────────────────────────────────────────────────────────────

    static void WirePlayerBodyDriver(PlayerParts p)
    {
        var driver = p.root.GetComponent<PlayerBodyDriver>();
        var xrOrigin = GameObject.Find("XR Origin (XR Rig)");
        if (xrOrigin == null) { Debug.LogWarning("XR Origin not found — wire PlayerBodyDriver manually."); return; }

        var camOffset = xrOrigin.transform.Find("Camera Offset");
        driver.hmdTransform             = camOffset?.Find("Main Camera");
        driver.leftControllerTransform  = camOffset?.Find("Left Controller");
        driver.rightControllerTransform = camOffset?.Find("Right Controller");
        driver.bodyRoot                 = p.root.transform;
        driver.headTarget               = p.head.transform;
        driver.torsoTarget              = p.torso.transform;
        driver.leftHandTarget           = p.leftHand.transform;
        driver.rightHandTarget          = p.rightHand.transform;
        EditorUtility.SetDirty(driver);
    }

    static void WireWaveDetector(GameObject gs, PlayerParts player, NPCReaction reaction)
    {
        var detector  = gs.GetComponent<WaveDetector>();
        var xrOrigin  = GameObject.Find("XR Origin (XR Rig)");
        var camOffset = xrOrigin?.transform.Find("Camera Offset");
        detector.hmdTransform       = camOffset?.Find("Main Camera");
        detector.leftHandTransform  = player.leftHand.transform;
        detector.rightHandTransform = player.rightHand.transform;

        // Wave → NPC nod is wired at runtime in NPCReaction.Start()
        EditorUtility.SetDirty(detector);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject Empty(string name)
    {
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, name);
        return go;
    }

    static GameObject Prim(string name, PrimitiveType type, GameObject parent)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        Undo.RegisterCreatedObjectUndo(go, name);
        return go;
    }

    static Transform Part(string name, GameObject parent, PrimitiveType type, Vector3 scale, Vector3 localPos, Material mat)
    {
        var go = Prim(name, type, parent);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        SetMat(go, mat);
        return go.transform;
    }

    static void SetMat(GameObject go, Material mat)
    {
        var r = go.GetComponent<MeshRenderer>();
        if (r != null) r.sharedMaterial = mat;
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
