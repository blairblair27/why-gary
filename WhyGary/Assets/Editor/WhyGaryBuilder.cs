#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.XR.CoreUtils;

// Builds the entire WhyGary.unity scene in one menu click.
// Requires: XR Origin (XR Rig) prefab already in the scene (drag from Starter Assets).
public static class WhyGaryBuilder
{
    const string MatPath    = "Assets/Materials";
    const string PrefabPath = "Assets/Prefabs";

    const string TargetDummyPath = "Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Blue.prefab";
    const string EscortDummyPath = "Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Green.prefab";

    const string XROriginName        = "XR Origin (XR Rig)";
    const string CameraOffsetName    = "Camera Offset";
    const string MainCameraName      = "Main Camera";
    const string LeftControllerName  = "Left Controller";
    const string RightControllerName = "Right Controller";

    // 10 phones at 0.68 m spacing. Phone 7 from the right = index 3 from the left (0-based).
    // z positions: leftmost = -3.06, rightmost = +3.06, player phone = -1.02
    const int   PhoneCount   = 10;
    const float PhoneSpacing = 0.68f;
    const int   PlayerPhoneIndexFromLeft = 3;   // phone 7 from right = index 3 from left

    static readonly string[] RequiredTags = { "NPCHead", "NPCTorso", "NPCArm", "NPCEscort" };

    struct Mats
    {
        public Material player, npc, npcDead, escort, hat;
        public Material floor, room, ceiling, wallBlue;
        public Material gun, cork;
        public Material phoneHousing, phoneFace, phoneDisplay, phoneKeypad, phoneSilver, phoneCord;
        public Material fluorescent;
    }

    struct PlayerParts { public GameObject root, head, torso, leftHand, rightHand; }

    struct EscortData
    {
        public NPCHealth     targetNPC;
        public EscortAgent[] escorts;
        public Transform     groupRoot;
        public NPCGroupPatrol patrol;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    static readonly string[] BuilderRoots =
    {
        "Room", "PayphoneBank", "PlayerBody", "CorkGun", "GestureSystem",
        "EscortGroup", "PatrolPath", "ScenarioManagerObject", "WhyGaryScenarioController"
    };

    [MenuItem("Why Gary/Clean and Rebuild Scene")]
    public static void CleanAndRebuild()
    {
        foreach (var name in BuilderRoots)
        {
            GameObject go;
            while ((go = GameObject.Find(name)) != null)
                Undo.DestroyObjectImmediate(go);
        }
        BuildWhyGaryScene();
    }

    [MenuItem("Why Gary/Build Why Gary Scene")]
    public static void BuildWhyGaryScene()
    {
        if (GameObject.Find("Room") != null)
        {
            if (!EditorUtility.DisplayDialog("Why Gary Builder",
                "Scene objects already exist. Rebuild anyway? This will create duplicates.",
                "Rebuild", "Cancel"))
                return;
        }

        EnsureTags();
        EnsureFolder(MatPath);
        EnsureFolder(PrefabPath);
        var m = BuildMaterials();

        CreateRoom(m);
        CreatePayphoneBank(m);
        var player     = CreatePlayerBody(m);
        CreateCorkGun(m);
        CreateGestureSystem();
        var escortData = CreateEscortGroup(m);
        var waypoints  = CreatePatrolPath();
        CreateScenarioInfrastructure(escortData, waypoints);

        PositionXROrigin();
        WirePlayerBodyDriver(player);
        WireGestureSystem(player);
        RegisterSceneInBuildSettings();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=lime><b>Why Gary scene built!</b> Run 'Create Cork Prefab' if not done yet, then hit Play.</color>");
    }

    // ── Tags ──────────────────────────────────────────────────────────────────

    static void EnsureTags()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0) { Debug.LogError("TagManager.asset not found."); return; }
        var so   = new SerializedObject(assets[0]);
        so.UpdateIfRequiredOrScript();
        var tags = so.FindProperty("tags");
        foreach (var tag in RequiredTags)
        {
            bool exists = false;
            for (int i = 0; i < tags.arraySize; i++)
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
            if (!exists)
            {
                Undo.RecordObject(assets[0], $"Add tag {tag}");
                tags.arraySize++;
                tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
            }
        }
        so.ApplyModifiedProperties();
    }

    // ── Materials ─────────────────────────────────────────────────────────────

    static Mats BuildMaterials() => new Mats
    {
        player       = Mat("PlayerMaterial",      new Color(0.20f, 0.40f, 0.80f)),
        npc          = Mat("NPCMaterial",          new Color(0.50f, 0.50f, 0.50f)),
        npcDead      = Mat("NPCDeadMaterial",      new Color(0.70f, 0.15f, 0.10f)),
        escort       = Mat("EscortMaterial",       new Color(0.42f, 0.38f, 0.34f)),
        hat          = Mat("HatMaterial",          new Color(0.62f, 0.50f, 0.22f)),
        floor        = Mat("FloorMaterial",          new Color(0.40f, 0.38f, 0.35f)),
        room         = Mat("RoomMaterial",         new Color(0.72f, 0.72f, 0.70f)),
        ceiling      = Mat("CeilingMaterial",      new Color(0.88f, 0.88f, 0.86f)),
        wallBlue     = Mat("WallBlueMaterial",     new Color(0.12f, 0.20f, 0.68f)),
        gun          = Mat("GunMaterial",          new Color(0.20f, 0.20f, 0.20f)),
        cork         = Mat("CorkMaterial",         new Color(0.85f, 0.70f, 0.50f)),
        phoneHousing = Mat("PhoneHousingMaterial", new Color(0.22f, 0.21f, 0.16f)),
        phoneFace    = Mat("PhoneFaceMaterial",    new Color(0.30f, 0.29f, 0.22f)),
        phoneDisplay = Mat("PhoneDisplayMaterial", new Color(0.05f, 0.06f, 0.08f)),
        phoneKeypad  = Mat("PhoneKeypadMaterial",  new Color(0.22f, 0.32f, 0.75f)),
        phoneSilver  = Mat("PhoneSilverMaterial",  new Color(0.52f, 0.52f, 0.55f)),
        phoneCord    = Mat("PhoneCordMaterial",    new Color(0.15f, 0.14f, 0.12f)),
        fluorescent  = MatEmissive("FluorescentMaterial", new Color(1.0f, 0.97f, 0.90f)),
    };

    static Material Mat(string name, Color color)
    {
        string path = $"{MatPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material MatEmissive(string name, Color color)
    {
        string path = $"{MatPath}/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 1.8f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ── Room ──────────────────────────────────────────────────────────────────
    // Corridor: 12 m north-south (Z, z=-6 to z=+6), 6 m east-west (X), 3 m tall.
    // Escort enters from the right/south (high Z), walks left/north (low Z).
    // West wall (x=-3) is split: white entry on the right side, blue phone wall in the middle.

    static void CreateRoom(Mats m)
    {
        var room = Empty("Room");

        // Floor: Unity Plane default 10×10 m, scaled to 14×8 m
        var floorGo = Prim("Floor", PrimitiveType.Plane, room);
        floorGo.transform.localScale = new Vector3(1.4f, 1f, 0.8f);
        SetMat(floorGo, m.floor);

        // Ceiling
        var ceil = Prim("Ceiling", PrimitiveType.Cube, room);
        ceil.transform.position   = new Vector3(0f, 3.05f, 0f);
        ceil.transform.localScale = new Vector3(8.4f, 0.1f, 14.4f);
        SetMat(ceil, m.ceiling);
        Object.DestroyImmediate(ceil.GetComponent<BoxCollider>());

        // End walls
        RoomWall("WallNorth", room, new Vector3(0f,  1.5f, -7f), new Vector3(8.4f, 3f, 0.2f),  m.room);
        RoomWall("WallSouth", room, new Vector3(0f,  1.5f,  7f), new Vector3(8.4f, 3f, 0.2f),  m.room);
        // East wall (far side — escort walks along here)
        RoomWall("WallEast",  room, new Vector3( 4f, 1.5f,  0f), new Vector3(0.2f, 3f, 14.4f), m.room);

        // West wall in three sections (x=-4):
        //   Right/entry section (z = +3.4 → +7): white — "white wall before payphones appear"
        //   Phone section      (z = -3.4 → +3.4): deep blue — payphone backdrop
        //   Left/exit section  (z = -7   → -3.4): blue (continuous with phone section)
        RoomWall("WallWest_Entry",  room, new Vector3(-4f, 1.5f,  5.2f), new Vector3(0.2f, 3f, 3.6f),  m.room);
        RoomWall("WallWest_Phones", room, new Vector3(-4f, 1.5f,  0f),   new Vector3(0.2f, 3f, 6.8f),  m.wallBlue);
        RoomWall("WallWest_Exit",   room, new Vector3(-4f, 1.5f, -5.2f), new Vector3(0.2f, 3f, 3.6f),  m.wallBlue);

        // Fluorescent ceiling strips (two parallel runs along the corridor length)
        var lightsParent = Empty("CeilingLights");
        lightsParent.transform.SetParent(room.transform, false);

        foreach (float x in new[] { -0.5f, 0.5f })
        {
            var strip = Prim("FluorescentStrip", PrimitiveType.Cube, lightsParent);
            strip.transform.position   = new Vector3(x, 2.97f, 0f);
            strip.transform.localScale = new Vector3(0.13f, 0.04f, 13.6f);
            SetMat(strip, m.fluorescent);
            Object.DestroyImmediate(strip.GetComponent<BoxCollider>());
        }

        // Point lights spaced along the corridor
        foreach (float z in new[] { -4.5f, 0f, 4.5f })
        {
            var lGo = new GameObject("CeilingLight");
            lGo.transform.SetParent(lightsParent.transform, false);
            lGo.transform.localPosition = new Vector3(0f, 2.9f, z);
            var l = lGo.AddComponent<Light>();
            l.type      = LightType.Point;
            l.range     = 7f;
            l.intensity = 1.2f;
            l.color     = new Color(1.0f, 0.97f, 0.90f);
        }
    }

    static void RoomWall(string name, GameObject parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var w = Prim(name, PrimitiveType.Cube, parent);
        w.transform.position   = pos;
        w.transform.localScale = scale;
        SetMat(w, mat);
    }

    // ── Payphone bank ─────────────────────────────────────────────────────────
    // 10 phones along the blue west wall. Phone 7 from the right = index 3 from the left.
    // Escort enters from the right (high Z), so phone 1 is rightmost (z=+3.06).

    static void CreatePayphoneBank(Mats m)
    {
        var bank = Empty("PayphoneBank");
        bank.transform.position = new Vector3(-3.87f, 0f, 0f);

        float startZ = -((PhoneCount - 1) * PhoneSpacing) / 2f;   // = -3.06
        for (int i = 0; i < PhoneCount; i++)
        {
            float z       = startZ + i * PhoneSpacing;
            bool  isPlayer = (i == PlayerPhoneIndexFromLeft);
            BuildPayphone(bank, z, m, isPlayer);
        }
    }

    static void BuildPayphone(GameObject bank, float z, Mats m, bool isPlayerPhone)
    {
        string id    = isPlayerPhone ? "Player" : $"Phone_{z:F2}";
        var phone    = Empty(id);
        phone.transform.SetParent(bank.transform, false);
        phone.transform.localPosition = new Vector3(0f, 0f, z);

        const string PhonePrefabPath = "Assets/Payphone/Prefabs/payphone_request.prefab";
        var phonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PhonePrefabPath);
        if (phonePrefab != null)
        {
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(phonePrefab, phone.transform);
            vis.name = "PhoneModel";
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);   // face east toward player
            Undo.RegisterCreatedObjectUndo(vis, "PhoneModel");
            var horn = vis.transform.Find("payphone_horn");
            if (horn != null)
            {
                var hornRb = horn.gameObject.AddComponent<Rigidbody>();
                hornRb.mass = 0.1f; hornRb.isKinematic = true;
                hornRb.interpolation = RigidbodyInterpolation.Interpolate;
                var hornGrab = horn.gameObject.AddComponent<XRGrabInteractable>();
                hornGrab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
                horn.gameObject.AddComponent<AudioSource>();
                horn.gameObject.AddComponent<PayphoneHandset>();
            }
            return;
        }

        // ── Primitive fallback ────────────────────────────────────────────────
        // Outer housing: dark metal box (keeps collider — part of scene geometry)
        Go("Housing", PrimitiveType.Cube, phone,
            new Vector3(0.52f, 1.05f, 0.22f), new Vector3(0f, 0.90f, 0f), m.phoneHousing);

        // Top hood / overhang
        NoCol(Go("Hood", PrimitiveType.Cube, phone,
            new Vector3(0.58f, 0.06f, 0.26f), new Vector3(0f, 1.455f, 0.015f), m.phoneHousing));

        // Face plate (slightly lighter shade)
        NoCol(Go("FacePlate", PrimitiveType.Cube, phone,
            new Vector3(0.44f, 0.95f, 0.015f), new Vector3(0f, 0.90f, 0.12f), m.phoneFace));

        // Display / instructions area
        NoCol(Go("Display", PrimitiveType.Cube, phone,
            new Vector3(0.34f, 0.16f, 0.01f), new Vector3(0f, 1.18f, 0.128f), m.phoneDisplay));

        // Coin return slot
        NoCol(Go("CoinSlot", PrimitiveType.Cube, phone,
            new Vector3(0.10f, 0.026f, 0.01f), new Vector3(0.06f, 1.024f, 0.128f), m.phoneDisplay));

        // Keypad: blue panel
        NoCol(Go("Keypad", PrimitiveType.Cube, phone,
            new Vector3(0.32f, 0.28f, 0.01f), new Vector3(0f, 0.75f, 0.128f), m.phoneKeypad));

        // Small coin box bump
        NoCol(Go("CoinBox", PrimitiveType.Cube, phone,
            new Vector3(0.20f, 0.10f, 0.02f), new Vector3(0f, 0.50f, 0.128f), m.phoneHousing));

        // Handset cradle / hook
        NoCol(Go("HandsetCradle", PrimitiveType.Cylinder, phone,
            new Vector3(0.035f, 0.07f, 0.035f), new Vector3(-0.19f, 1.04f, 0.08f), m.phoneSilver,
            Quaternion.Euler(0f, 0f, 25f)));

        // Cord
        NoCol(Go("Cord", PrimitiveType.Cylinder, phone,
            new Vector3(0.014f, 0.09f, 0.014f), new Vector3(-0.18f, 0.76f, 0.11f), m.phoneCord));

        // ── Interactive handset ───────────────────────────────────────────────
        // Collider stays on. Rigidbody (kinematic at rest) + XRGrabInteractable + PayphoneHandset.
        var handset = Go("Handset", PrimitiveType.Capsule, phone,
            new Vector3(0.055f, 0.175f, 0.055f), new Vector3(-0.21f, 0.94f, 0.11f), m.phoneSilver,
            Quaternion.Euler(0f, 0f, 15f));

        var rb             = handset.AddComponent<Rigidbody>();
        rb.mass            = 0.1f;
        rb.isKinematic     = true;           // XRI un-kinematics it on grab
        rb.interpolation   = RigidbodyInterpolation.Interpolate;

        var grab           = handset.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.VelocityTracking;

        handset.AddComponent<AudioSource>();     // required by PayphoneHandset
        handset.AddComponent<PayphoneHandset>();
    }

    // ── Player body ───────────────────────────────────────────────────────────

    static PlayerParts CreatePlayerBody(Mats m)
    {
        var root = Empty("PlayerBody");

        var head  = Part("Head",        root, PrimitiveType.Sphere,  new Vector3(0.24f, 0.24f, 0.24f), new Vector3(0,      1.65f, 0), m.player);
        var torso = Part("Torso",        root, PrimitiveType.Capsule, new Vector3(0.35f, 0.55f, 0.25f), new Vector3(0,      1.20f, 0), m.player);
        Part("LeftUpperArm",  root, PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3(-0.28f, 1.45f, 0), m.player);
        Part("LeftForearm",   root, PrimitiveType.Capsule, new Vector3(0.10f, 0.25f, 0.10f), new Vector3(-0.28f, 1.10f, 0), m.player);
        var lHand = Part("LeftHand",  root, PrimitiveType.Cube, new Vector3(0.12f, 0.08f, 0.16f), new Vector3(-0.28f, 0.85f, 0), m.player);
        Part("RightUpperArm", root, PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3( 0.28f, 1.45f, 0), m.player);
        Part("RightForearm",  root, PrimitiveType.Capsule, new Vector3(0.10f, 0.25f, 0.10f), new Vector3( 0.28f, 1.10f, 0), m.player);
        var rHand = Part("RightHand", root, PrimitiveType.Cube, new Vector3(0.12f, 0.08f, 0.16f), new Vector3( 0.28f, 0.85f, 0), m.player);

        foreach (var c in root.GetComponentsInChildren<Collider>())
            Object.DestroyImmediate(c);

        lHand.GetComponent<MeshRenderer>().enabled = false;
        rHand.GetComponent<MeshRenderer>().enabled = false;

        root.AddComponent<PlayerBodyDriver>();
        return new PlayerParts { root = root, head = head.gameObject, torso = torso.gameObject, leftHand = lHand.gameObject, rightHand = rHand.gameObject };
    }

    // ── Cork gun ──────────────────────────────────────────────────────────────

    static void CreateCorkGun(Mats m)
    {
        const string GunPrefabPath = "Assets/Reichsrevolver_M1879/Prefabs/ReichsrevolverM1879.prefab";

        var gun = Empty("CorkGun");
        gun.transform.position = new Vector3(-3.5f, 0.92f, -0.65f);
        gun.transform.rotation = Quaternion.Euler(0, -90, 0);

        var gunPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GunPrefabPath);
        if (gunPrefab != null)
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(gunPrefab, gun.transform);
            model.name = "GunModel";
            Undo.RegisterCreatedObjectUndo(model, "GunModel");
            foreach (var existingRb  in model.GetComponentsInChildren<Rigidbody>(true))
                Object.DestroyImmediate(existingRb);
            foreach (var existingCol in model.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(existingCol);
        }
        else
        {
            var body = Prim("GunBody", PrimitiveType.Cube, gun);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = new Vector3(0.05f, 0.14f, 0.22f);
            SetMat(body, m.gun);
            Debug.LogWarning($"[WhyGaryBuilder] Revolver prefab not found at '{GunPrefabPath}' — import 'Reichsrevolver M-1879' from Asset Store first.");
        }

        // FirePoint at approximate barrel tip — adjust in Inspector if shots appear offset
        var fp = new GameObject("FirePoint");
        fp.transform.SetParent(gun.transform, false);
        fp.transform.localPosition = new Vector3(0f, 0.034f, 0.188f);

        var rb  = gun.AddComponent<Rigidbody>();
        rb.mass = 0.8f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var col    = gun.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.005f, 0.045f);
        col.size   = new Vector3(0.06f, 0.22f, 0.25f);

        var grab = gun.AddComponent<XRGrabInteractable>();
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;

        var script        = gun.AddComponent<GunController>();
        script.firePoint   = fp.transform;
        script.shootSpeed  = 25f;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabPath}/CorkProjectile.prefab");
        if (prefab != null) script.bulletPrefab = prefab;
    }

    // ── Gesture system ────────────────────────────────────────────────────────

    static void CreateGestureSystem()
    {
        Empty("GestureSystem").AddComponent<WaveDetector>();
    }

    // ── Escort group ──────────────────────────────────────────────────────────
    // Escort enters from the right/south (z=+5.5) and walks left/north.

    static EscortData CreateEscortGroup(Mats m)
    {
        var groupRoot = Empty("EscortGroup");
        groupRoot.transform.position = new Vector3(2.5f, 0f, 6.5f);  // starts at south/right entry

        var targetGo     = BuildHumanoid("Target", groupRoot, Vector3.zero, m.npc, isEscort: false);
        var targetHealth = targetGo.GetComponent<NPCHealth>();

        (string name, Vector3 offset)[] formation =
        {
            ("Escort_Front", new Vector3( 0f,    0f,  0.72f)),
            ("Escort_Back",  new Vector3( 0f,    0f, -0.72f)),
            ("Escort_Left",  new Vector3(-0.55f, 0f,  0f   )),
            ("Escort_Right", new Vector3( 0.55f, 0f,  0f   )),
        };

        var agents = new EscortAgent[formation.Length];
        for (int i = 0; i < formation.Length; i++)
        {
            var go   = BuildHumanoid(formation[i].name, groupRoot, formation[i].offset, m.escort, isEscort: true);
            AttachCowboyHat(go, m.hat);
            agents[i] = go.AddComponent<EscortAgent>();
        }

        var patrol       = groupRoot.AddComponent<NPCGroupPatrol>();
        patrol.groupRoot = groupRoot.transform;
        patrol.enabled   = false;   // WhyGaryScenario.OnScenarioStart() enables this

        return new EscortData { targetNPC = targetHealth, escorts = agents, groupRoot = groupRoot.transform, patrol = patrol };
    }

    static GameObject BuildHumanoid(string bodyName, GameObject parent, Vector3 localPos, Material bodyMat, bool isEscort)
    {
        var root = Empty(bodyName);
        root.transform.SetParent(parent.transform, false);
        root.transform.localPosition = localPos;

        string tTag = isEscort ? "NPCEscort" : "NPCTorso";
        string hTag = isEscort ? "NPCEscort" : "NPCHead";
        string aTag = isEscort ? "NPCEscort" : "NPCArm";

        var torsoGo = Part("Torso", root, PrimitiveType.Capsule, new Vector3(0.35f, 0.55f, 0.25f), new Vector3(0, 1.20f, 0), bodyMat).gameObject;
        torsoGo.tag  = tTag;
        var torsoRb  = RagdollRb(torsoGo, 10f);

        var headGo = Part("Head", root, PrimitiveType.Sphere, new Vector3(0.24f, 0.24f, 0.24f), new Vector3(0, 1.65f, 0), bodyMat).gameObject;
        headGo.tag   = hTag;
        RagdollRb(headGo, 4f);
        JoinTo(headGo, torsoRb, 40f);

        var lUARb = Limb("LeftUpperArm",  root, PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3(-0.28f, 1.45f, 0), bodyMat, torsoRb, 80f);
        var lFARb = Limb("LeftForearm",   root, PrimitiveType.Capsule, new Vector3(0.10f, 0.25f, 0.10f), new Vector3(-0.28f, 1.10f, 0), bodyMat, lUARb,  90f);
                   Limb("LeftHand",      root, PrimitiveType.Cube,    new Vector3(0.12f, 0.08f, 0.16f), new Vector3(-0.28f, 0.85f, 0), bodyMat, lFARb,  60f);
        var rUARb = Limb("RightUpperArm", root, PrimitiveType.Capsule, new Vector3(0.12f, 0.28f, 0.12f), new Vector3( 0.28f, 1.45f, 0), bodyMat, torsoRb, 80f);
        var rFARb = Limb("RightForearm",  root, PrimitiveType.Capsule, new Vector3(0.10f, 0.25f, 0.10f), new Vector3( 0.28f, 1.10f, 0), bodyMat, rUARb,  90f);
                   Limb("RightHand",     root, PrimitiveType.Cube,    new Vector3(0.12f, 0.08f, 0.16f), new Vector3( 0.28f, 0.85f, 0), bodyMat, rFARb,  60f);

        foreach (string n in new[] { "LeftUpperArm", "LeftForearm", "LeftHand", "RightUpperArm", "RightForearm", "RightHand" })
            root.transform.Find(n).gameObject.tag = aTag;

        var ragdoll = root.AddComponent<NPCRagdoll>();

        if (!isEscort)
        {
            ragdoll.deadMaterial = Mat("NPCDeadMaterial", new Color(0.70f, 0.15f, 0.10f));
            root.AddComponent<NPCHealth>();
            var reaction     = root.AddComponent<NPCReaction>();
            reaction.npcHead = headGo.transform;
            var grab          = torsoGo.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        }

        var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(isEscort ? EscortDummyPath : TargetDummyPath);
        if (dummyPrefab != null)
        {
            var vis = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab, root.transform);
            vis.name = "Visual";
            Undo.RegisterCreatedObjectUndo(vis, "NPC Visual");
            foreach (var c in vis.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
            ragdoll.visual = vis;
            var anim = vis.GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = CreateOrLoadNPCController();
        }
        else
        {
            Debug.LogWarning("[WhyGaryBuilder] Human dummy prefab not found — import 'Human Character Dummy' from Asset Store first.");
        }
        root.AddComponent<NPCAnimator>();

        return root;
    }

    static void AttachCowboyHat(GameObject npcRoot, Material hatMat)
    {
        var head = npcRoot.transform.Find("Head");
        if (head == null) { Debug.LogWarning($"[WhyGaryBuilder] Could not find Head on '{npcRoot.name}' — cowboy hat not attached."); return; }

        var hat = Empty("CowboyHat");
        hat.transform.SetParent(head, false);
        hat.transform.localPosition = new Vector3(0f, 0.14f, 0f);

        var brim = Prim("HatBrim", PrimitiveType.Cylinder, hat);
        brim.transform.localPosition = Vector3.zero;
        brim.transform.localScale    = new Vector3(0.40f, 0.025f, 0.40f);
        SetMat(brim, hatMat);
        Object.DestroyImmediate(brim.GetComponent<CapsuleCollider>());

        var crown = Prim("HatCrown", PrimitiveType.Cylinder, hat);
        crown.transform.localPosition = new Vector3(0f, 0.13f, 0f);
        crown.transform.localScale    = new Vector3(0.24f, 0.18f, 0.24f);
        SetMat(crown, hatMat);
        Object.DestroyImmediate(crown.GetComponent<CapsuleCollider>());
    }

    // ── NPC animation controller ──────────────────────────────────────────────

    static RuntimeAnimatorController CreateOrLoadNPCController()
    {
        const string ControllerPath = "Assets/Animations/NPCLocomotion.controller";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (existing != null) return existing;

        EnsureFolder("Assets/Animations");

        const string AnimBase = "Assets/Kevin Iglesias/Human Animations/Male";
        var idleClip = LoadFirstClip($"{AnimBase}/Idles/HumanM@Idle01.fbx");
        var walkClip = LoadFirstClip($"{AnimBase}/Movement/Walk/HumanM@Walk01_Forward.fbx");

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        var sm = controller.layers[0].stateMachine;

        if (idleClip != null && walkClip != null)
        {
            var locoState = controller.CreateBlendTreeInController("Locomotion", out var blendTree, 0);
            blendTree.blendParameter = "Speed";
            blendTree.AddChild(idleClip, 0f);
            blendTree.AddChild(walkClip, 1.5f);
            sm.defaultState = locoState;
        }
        else
        {
            var idleState = sm.AddState("Idle");
            var walkState = sm.AddState("Walk");
            if (idleClip != null) idleState.motion = idleClip;
            if (walkClip != null) walkState.motion = walkClip;
            var i2w = idleState.AddTransition(walkState);
            i2w.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            i2w.hasExitTime = false; i2w.duration = 0.15f;
            var w2i = walkState.AddTransition(idleState);
            w2i.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            w2i.hasExitTime = false; w2i.duration = 0.15f;
            if (idleClip == null || walkClip == null)
                Debug.LogWarning("[WhyGaryBuilder] Animation clips missing — import 'Human Basic Motions FREE' then re-run Clean and Rebuild.");
        }

        AssetDatabase.SaveAssets();
        return controller;
    }

    static AnimationClip LoadFirstClip(string fbxPath)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        return null;
    }

    // ── Patrol path ───────────────────────────────────────────────────────────
    // Escort enters from the right/south (high Z) and exits left/north (low Z).

    static Transform[] CreatePatrolPath()
    {
        var path = Empty("PatrolPath");

        var wp0 = new GameObject("Waypoint_0"); wp0.transform.SetParent(path.transform, false); wp0.transform.position = new Vector3(2.5f, 0f,  6.5f);
        var wp1 = new GameObject("Waypoint_1"); wp1.transform.SetParent(path.transform, false); wp1.transform.position = new Vector3(2.5f, 0f, -6.5f);

        return new[] { wp0.transform, wp1.transform };
    }

    // ── Scenario infrastructure ───────────────────────────────────────────────

    static void CreateScenarioInfrastructure(EscortData ed, Transform[] waypoints)
    {
        Transform hmd = FindHMD();

        ed.patrol.waypoints = waypoints;

        var smGo         = Empty("ScenarioManagerObject");
        var sm           = smGo.AddComponent<ScenarioManager>();
        sm.playerHMD     = hmd;
        sm.outcomeCanvas  = BuildOutcomeUI(smGo);
        sm.outcomeText    = sm.outcomeCanvas.GetComponentInChildren<TextMeshProUGUI>();

        var wgGo       = Empty("WhyGaryScenarioController");
        var wg         = wgGo.AddComponent<WhyGaryScenario>();
        wg.targetNPC   = ed.targetNPC;
        wg.escorts     = ed.escorts;
        wg.patrolGroup = ed.patrol;
        wg.playerHMD   = hmd;

        if (hmd == null)
            Debug.LogWarning("XR Origin not found — assign playerHMD on ScenarioManager and WhyGaryScenario manually.");
    }

    static GameObject BuildOutcomeUI(GameObject parent)
    {
        var canvasGo = new GameObject("OutcomeCanvas");
        canvasGo.transform.SetParent(parent.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 1.6f, 1.5f);
        canvasGo.transform.localScale    = new Vector3(0.005f, 0.005f, 0.005f);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvasGo.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();
        canvasGo.GetComponent<RectTransform>().sizeDelta = new Vector2(600f, 200f);

        var bg      = new GameObject("Background");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgImg   = bg.AddComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.78f);
        var bgRect  = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one; bgRect.sizeDelta = Vector2.zero;

        var textGo  = new GameObject("OutcomeText");
        textGo.transform.SetParent(canvasGo.transform, false);
        var tmp     = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = "WHY GARY WHY";
        tmp.fontSize  = 80f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        var tr        = textGo.GetComponent<RectTransform>();
        tr.anchorMin  = Vector2.zero; tr.anchorMax = Vector2.one; tr.sizeDelta = Vector2.zero;

        canvasGo.SetActive(false);
        return canvasGo;
    }

    // ── XR Origin ────────────────────────────────────────────────────────────

    static void PositionXROrigin()
    {
        var xrOrigin = GameObject.Find(XROriginName);
        if (xrOrigin == null)
        {
            Debug.LogWarning("<color=yellow><b>XR Origin not found.</b> Drag 'XR Origin (XR Rig)' from XRI Starter Assets into the scene, then re-run Build Why Gary Scene. Player position and wiring were skipped.</color>");
            return;
        }
        // Stand at phone 7 from the right (z=-1.02), facing east toward the escort path
        xrOrigin.transform.position = new Vector3(-3.4f, 0f, -1.02f);
        xrOrigin.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        var xrOriginComp = xrOrigin.GetComponent<XROrigin>();
        if (xrOriginComp != null)
            xrOriginComp.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        else
            Debug.LogWarning("XROrigin component not found on XR Origin — set Tracking Origin Mode to Floor manually.");
    }

    static void RegisterSceneInBuildSettings()
    {
        var scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogWarning("Save the scene first (Ctrl+S), then re-run the builder to register it in Build Settings.");
            return;
        }

        var existing = EditorBuildSettings.scenes;
        foreach (var s in existing)
            if (s.path == scene.path) return;

        var updated = new EditorBuildSettingsScene[existing.Length + 1];
        existing.CopyTo(updated, 0);
        updated[existing.Length] = new EditorBuildSettingsScene(scene.path, true);
        EditorBuildSettings.scenes = updated;
        Debug.Log($"<color=lime>Registered '{scene.path}' in Build Settings.</color>");
    }

    static void WirePlayerBodyDriver(PlayerParts p)
    {
        var driver    = p.root.GetComponent<PlayerBodyDriver>();
        var camOffset = FindCameraOffset();
        if (camOffset == null) { Debug.LogWarning("XR Origin not found — wire PlayerBodyDriver manually."); return; }

        driver.hmdTransform             = camOffset.Find(MainCameraName);
        driver.leftControllerTransform  = camOffset.Find(LeftControllerName);
        driver.rightControllerTransform = camOffset.Find(RightControllerName);
        driver.bodyRoot        = p.root.transform;
        driver.headTarget      = p.head.transform;
        driver.torsoTarget     = p.torso.transform;
        driver.leftHandTarget  = p.leftHand.transform;
        driver.rightHandTarget = p.rightHand.transform;
    }

    static void WireGestureSystem(PlayerParts player)
    {
        var gs = GameObject.Find("GestureSystem");
        if (gs == null) { Debug.LogWarning("GestureSystem not found — skipping WaveDetector wiring."); return; }
        var detector = gs.GetComponent<WaveDetector>();
        if (detector == null) { Debug.LogWarning("WaveDetector not found on GestureSystem — skipping wiring."); return; }
        var camOffset = FindCameraOffset();
        if (camOffset == null) { Debug.LogWarning("Camera Offset not found — assign WaveDetector.hmdTransform manually."); return; }
        detector.hmdTransform       = camOffset.Find(MainCameraName);
        detector.leftHandTransform  = player.leftHand.transform;
        detector.rightHandTransform = player.rightHand.transform;
    }

    // ── Ragdoll helpers ───────────────────────────────────────────────────────

    static Rigidbody Limb(string name, GameObject parent, PrimitiveType type, Vector3 scale, Vector3 pos, Material mat, Rigidbody connectedTo, float swing)
    {
        var go = Part(name, parent, type, scale, pos, mat).gameObject;
        var rb = RagdollRb(go, 2f);
        JoinTo(go, connectedTo, swing);
        return rb;
    }

    static Rigidbody RagdollRb(GameObject go, float mass)
    {
        var rb = go.AddComponent<Rigidbody>();
        rb.mass                   = mass;
        rb.isKinematic            = true;
        rb.interpolation          = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        return rb;
    }

    static void JoinTo(GameObject go, Rigidbody connectedTo, float swing)
    {
        var j = go.AddComponent<CharacterJoint>();
        j.connectedBody  = connectedTo;
        j.swing1Limit    = new SoftJointLimit { limit =  swing };
        j.swing2Limit    = new SoftJointLimit { limit =  swing * 0.5f };
        j.lowTwistLimit  = new SoftJointLimit { limit = -swing * 0.5f };
        j.highTwistLimit = new SoftJointLimit { limit =  swing * 0.5f };
    }

    // ── Generic helpers ───────────────────────────────────────────────────────

    static Transform FindCameraOffset()
    {
        return GameObject.Find(XROriginName)?.transform.Find(CameraOffsetName);
    }

    static Transform FindHMD()
    {
        return FindCameraOffset()?.Find(MainCameraName);
    }

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

    static GameObject Go(string name, PrimitiveType type, GameObject parent, Vector3 scale, Vector3 localPos, Material mat, Quaternion? localRot = null)
    {
        var go = Prim(name, type, parent);
        go.transform.localPosition = localPos;
        go.transform.localScale    = scale;
        if (localRot.HasValue) go.transform.localRotation = localRot.Value;
        SetMat(go, mat);
        return go;
    }

    static GameObject NoCol(GameObject go)
    {
        foreach (var c in go.GetComponents<Collider>())
            Object.DestroyImmediate(c);
        return go;
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
