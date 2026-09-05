#if UNITY_EDITOR
using MmoTemplate.Network;
using MmoTemplate.Player;
using MmoTemplate.Rpg;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// / <summary>
// / One-click scaffolding for the MMORPG template: builds a Player prefab and
// / the Bootstrap/World scenes with everything wired (NetworkManager, UI,
// / script references), then registers both scenes in Build Settings.
// /
// / This exists because Unity scenes/prefabs are Editor-managed assets that
// / can't be hand-authored as plain text the way the rest of this template's
// / scripts are — so we build them once, here, instead of asking you to wire
// / a dozen Inspector references by hand.
// /
// / Run it from the menu: MMORPG Template ▸ Build Playable Scenes.
// / Safe to re-run — it overwrites Bootstrap.unity, World.unity and
// / Player.prefab each time.
// / </summary>
public static class SceneBuilder
{
    private const string ScenesDir = "Assets/Scenes";
    private const string PrefabsDir = "Assets/Prefabs";
    private const string PlayerPrefabPath = PrefabsDir + "/Player.prefab";
    private const string MaterialsDir = "Assets/Materials";

    [MenuItem("MMORPG Template/Build Playable Scenes")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(ScenesDir);
        System.IO.Directory.CreateDirectory(PrefabsDir);

        ClassicContentBuilder.Build();
        GameObject playerPrefab = BuildPlayerPrefab();
        GameObject enemyPrefab = BuildEnemyPrefab();
        BuildBootstrapScene(playerPrefab, enemyPrefab);
        BuildWorldScene(enemyPrefab);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesDir}/Bootstrap.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/World.unity", true),
        };

        EditorSceneManager.OpenScene($"{ScenesDir}/Bootstrap.unity");
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode) EditorUtility.DisplayDialog(
            "MMORPG Template",
            "Player prefab and both scenes are built and added to Build Settings.\n\n" +
            "Press Play in Bootstrap.unity, then click \"Host + Play\".\n\n" +
            "To test with a second player: Window ▸ Multiplayer Play Mode ▸ " +
            "Add Virtual Player, then Play again and click \"Join\" (127.0.0.1).",
            "Got it");
    }

    // ------------------------------------------------------------------
    // Player prefab
    // ------------------------------------------------------------------

    private static GameObject BuildPlayerPrefab()
    {
        var go = new GameObject("Player");
        var cc = go.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 1f, 0);
        cc.height = 2f;
        cc.radius = 0.4f;

        // Real character model (CC0 Quaternius wayfarer). Falls back to a capsule
        // if the art hasn't imported yet, so the template still builds bare.
        var model = LoadArt("Characters/wayfarer");
        if (model != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = Vector3.zero;
            ApplyClassicMaterials(visual);
        }
        else
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(new Color(0.85f, 0.85f, 0.9f));
        }

        go.AddComponent<NetworkObject>();
        go.AddComponent<NetworkTransform>();
        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerStats>();
        go.AddComponent<PlayerCombat>();
        go.layer = 2;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------
    // Enemy prefab — the trial guardians
    // ------------------------------------------------------------------

    private static GameObject BuildEnemyPrefab()
    {
        var go = new GameObject("Guardian");

        var model = LoadArt("Characters/toma");
        if (model != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            ApplyClassicMaterials(visual);
        }
        else
        {
            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            Object.DestroyImmediate(visual.GetComponent<CapsuleCollider>());
            visual.transform.SetParent(go.transform, false);
            visual.transform.localPosition = new Vector3(0, 1f, 0);
            visual.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(new Color(0.4f, 0.45f, 0.35f));
        }

        go.layer = 2;
        var capsule = go.AddComponent<CharacterController>();
        capsule.center = new Vector3(0, 1f, 0);
        capsule.height = 2f;
        capsule.radius = 0.45f;

        go.AddComponent<NetworkObject>();
        go.AddComponent<NetworkTransform>();
        go.AddComponent<Enemy>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsDir + "/Guardian.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------
    // Bootstrap.unity — login / connect screen
    // ------------------------------------------------------------------

    private static void BuildBootstrapScene(GameObject playerPrefab, GameObject enemyPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 1, -5);

        var netGo = new GameObject("NetworkManager");
        var nm = netGo.AddComponent<NetworkManager>();
        var transport = netGo.AddComponent<UnityTransport>();
        nm.NetworkConfig.NetworkTransport = transport;
        nm.NetworkConfig.PlayerPrefab = playerPrefab;
        nm.NetworkConfig.ConnectionApproval = false;
        if (enemyPrefab != null)
            nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = enemyPrefab });
        nm.NetworkConfig.EnableSceneManagement = true; // server drives scene loads, keeps in-scene NetworkObjects in sync

        MakeEventSystem();
        var canvas = MakeCanvas("BootstrapCanvas");
        var panel = MakePanel(canvas.transform, new Vector2(420, 370));

        MakeText(panel.transform, "Title", "AZEROTH · THE OATHFIRE", 22, TextAnchor.MiddleCenter);
        var nameField = MakeInputField(panel.transform, "NameField", "Character name");
        var addressField = MakeInputField(panel.transform, "AddressField", "Server address");
        var portField = MakeInputField(panel.transform, "PortField", "Port");
        var hostButton = MakeButton(panel.transform, "HostButton", "Enter the world · Solo / Host");
        var joinButton = MakeButton(panel.transform, "JoinButton", "Join");
        var dedicatedButton = MakeButton(panel.transform, "DedicatedButton", "Start Dedicated Server");
        var status = MakeText(panel.transform, "Status", "", 14, TextAnchor.MiddleCenter);

        var bootGo = new GameObject("Bootstrap");
        var boot = bootGo.AddComponent<NetworkBootstrap>();
        var so = new SerializedObject(boot);
        so.FindProperty("nameField").objectReferenceValue = nameField;
        so.FindProperty("addressField").objectReferenceValue = addressField;
        so.FindProperty("portField").objectReferenceValue = portField;
        so.FindProperty("hostButton").objectReferenceValue = hostButton;
        so.FindProperty("joinButton").objectReferenceValue = joinButton;
        so.FindProperty("dedicatedButton").objectReferenceValue = dedicatedButton;
        so.FindProperty("statusLabel").objectReferenceValue = status;
        so.FindProperty("worldSceneName").stringValue = "World";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, $"{ScenesDir}/Bootstrap.unity");
    }

    // ------------------------------------------------------------------
    // World.unity — the actual play space
    // ------------------------------------------------------------------

    private static void BuildWorldScene(GameObject enemyPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Invisible collision floor under the decorative tiles.
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GroundCollision";
        ground.transform.localScale = new Vector3(2.4f, 1, 2.0f);
        ground.GetComponent<MeshRenderer>().enabled = false;

        var lightGo = new GameObject("Directional Light", typeof(Light));
        lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
        var light = lightGo.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.55f;
        light.color = new Color(0.75f, 0.82f, 1f);
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.29f, 0.34f);

        // Default look-angle, so the camera frames the play space even before a
        // player has spawned and CameraFollow has a target to lock onto.
        var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 6, -7);
        camGo.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

        // Managers. QuestManager and ChatRelay are NetworkBehaviours, so they
        // need a NetworkObject; as in-scene objects they're spawned for everyone
        // by Netcode's scene management.
        var gm = new GameObject("GameManager");
        gm.AddComponent<PlayerSpawner>();
        gm.AddComponent<NetworkObject>();
        var spawner = gm.AddComponent<EnemySpawner>();
        var quest = gm.AddComponent<QuestManager>();
        gm.AddComponent<ChatRelay>();

        var spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("enemyPrefab").objectReferenceValue = enemyPrefab;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        var questSo = new SerializedObject(quest);
        questSo.FindProperty("spawner").objectReferenceValue = spawner;
        questSo.ApplyModifiedPropertiesWithoutUndo();

        // The HUD builds its own UI at runtime — nothing to wire, nothing to
        // serialise, so it can't come back broken.
        new GameObject("RpgHud").AddComponent<RpgHud>();

        BuildScenery();

        EditorSceneManager.SaveScene(scene, $"{ScenesDir}/World.unity");
    }

    // ------------------------------------------------------------------
    // Scenery — a furnished 20 x 16 m hall assembled from the CC0 KayKit
    // Dungeon Remastered props and Quaternius characters in Assets/Art.
    // Layout mirrors the Oathfire Chamber from the Godot version of this project.
    // ------------------------------------------------------------------

    private static void BuildScenery()
    {
        var room = new GameObject("Chamber");

        // Floor: 5 x 4 grid of 4m tiles => a 20 x 16 m hall. Wood on the west
        // (living quarter), stone tile on the east (the proving circle).
        foreach (int x in new[] { -8, -4, 0, 4, 8 })
        foreach (int z in new[] { -6, -2, 2, 6 })
            Art(room.transform, x < -2 ? "Props/floor_wood_large" : "Props/floor_tile_large",
                new Vector3(x, 0, z));

        // Back wall, with arched windows letting moonlight in.
        foreach (int x in new[] { -8, -4, 0, 4, 8 })
        {
            Art(room.transform, (x == -4 || x == 4) ? "Props/wall_archedwindow_open" : "Props/wall",
                new Vector3(x, 0, -8));
            Collider(room.transform, new Vector3(x, 2, -8), new Vector3(4, 4, 1));
        }

        // Side walls.
        foreach (int x in new[] { -10, 10 })
        foreach (int z in new[] { -6, -2, 2, 6 })
        {
            Art(room.transform, (x == -10 && z == -2) ? "Props/wall_shelves" : "Props/wall",
                new Vector3(x, 0, z), 90f);
            Collider(room.transform, new Vector3(x, 2, z), new Vector3(1, 4, 4));
        }

        // Front is left open (cutaway) so the camera can see in — just a barrier.
        Collider(room.transform, new Vector3(0, 2, 8), new Vector3(20, 4, 1));

        foreach (float x in new[] { -9.4f, -3.4f, 3.4f, 9.4f })
            Art(room.transform, "Props/pillar_decorated", new Vector3(x, 0, -7.3f));

        // Hearth / living corner.
        Art(room.transform, "Props/table_long_decorated_A", new Vector3(-6.4f, 0, 2.5f), 90f);
        Collider(room.transform, new Vector3(-6.4f, 0.55f, 2.5f), new Vector3(2.8f, 1.1f, 1.4f));
        Art(room.transform, "Props/chair", new Vector3(-6.4f, 0, 4.0f), 180f);
        Art(room.transform, "Props/stool", new Vector3(-8.3f, 0, 2.5f));
        Art(room.transform, "Props/candle_triple", new Vector3(-6.4f, 1.12f, 2.5f));
        Art(room.transform, "Props/plate_food_A", new Vector3(-5.6f, 1.12f, 2.5f));
        Art(room.transform, "Props/bottle_A_green", new Vector3(-7.1f, 1.12f, 2.6f));
        Art(room.transform, "Props/bed_decorated", new Vector3(-7.8f, 0, -0.5f));
        Art(room.transform, "Props/shelf_large", new Vector3(-8.8f, 0, -3.3f), 90f);
        Art(room.transform, "Props/table_small_decorated_A", new Vector3(-6.4f, 0, -4.6f));
        Art(room.transform, "Props/shelf_small_candles", new Vector3(-9.0f, 0, 5.2f), 90f);

        // Storage.
        Art(room.transform, "Props/barrel_large_decorated", new Vector3(-8.2f, 0, -6.3f));
        Art(room.transform, "Props/barrel_small_stack", new Vector3(-8.6f, 0, 6.5f));
        Art(room.transform, "Props/crates_stacked", new Vector3(8.1f, 0, 6.3f));
        Collider(room.transform, new Vector3(8.1f, 0.8f, 6.3f), new Vector3(2, 1.6f, 1.4f));

        // Heraldry on the back wall.
        foreach (int x in new[] { -7, 7 })
            Art(room.transform, "Props/banner_patternA_red", new Vector3(x, 2.0f, -7.35f));
        Art(room.transform, "Props/sword_shield", new Vector3(0, 2.4f, -7.32f));
        Art(room.transform, "Props/banner_shield_red", new Vector3(3.4f, 2.0f, -7.35f));

        // Torches + their warm pools of light.
        foreach (int x in new[] { -9, 9 })
        foreach (int z in new[] { -5, 3 })
        {
            Art(room.transform, "Props/torch_mounted", new Vector3(x, 1.9f, z), x < 0 ? -90f : 90f);
            WarmLight(room.transform, new Vector3(x * 0.94f, 2.8f, z), 1.5f, 8f);
        }
        WarmLight(room.transform, new Vector3(-6.4f, 2.2f, 2.5f), 1.2f, 7f);

        // Two NPCs standing in the hall, both interactable with E.
        var mira = Art(room.transform, "Characters/mira", new Vector3(-4.1f, 0, 1.0f), 160f);
        MakeInteractable(mira, InteractableKind.QuestGiver, "Mira", "Speak to Mira");

        var toma = Art(room.transform, "Characters/toma", new Vector3(-6.2f, 0, -2.6f), 20f);
        MakeInteractable(toma, InteractableKind.Merchant, "Toma", "Trade with Toma");

        // The Oathfire brazier at the back of the hall.
        var brazier = new GameObject("Oathfire");
        brazier.transform.SetParent(room.transform, false);
        brazier.transform.position = new Vector3(0, 0, -6.1f);

        var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bowl.name = "Bowl";
        bowl.transform.SetParent(brazier.transform, false);
        bowl.transform.localPosition = new Vector3(0, 0.5f, 0);
        bowl.transform.localScale = new Vector3(0.9f, 0.5f, 0.9f);
        bowl.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(new Color(0.42f, 0.35f, 0.24f));

        var flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Flame";
        flame.transform.SetParent(brazier.transform, false);
        flame.transform.localPosition = new Vector3(0, 1.25f, 0);
        flame.transform.localScale = new Vector3(0.45f, 0.7f, 0.45f);
        Object.DestroyImmediate(flame.GetComponent<SphereCollider>());
        flame.GetComponent<MeshRenderer>().sharedMaterial = MakeMaterial(new Color(0.95f, 0.6f, 0.25f));

        WarmLight(brazier.transform, new Vector3(0, 1.6f, -6.1f), 1.6f, 9f);
        MakeInteractable(brazier, InteractableKind.Brazier, "The Oathfire", "Rekindle the Oathfire");

        // Moonlight through the arched windows.
        var moon = new GameObject("Moonlight", typeof(Light));
        moon.transform.SetParent(room.transform, false);
        moon.transform.position = new Vector3(4, 3.7f, -7);
        var ml = moon.GetComponent<Light>();
        ml.type = LightType.Spot;
        ml.color = new Color(0.63f, 0.74f, 0.91f);
        ml.intensity = 3.5f;
        ml.range = 16;
        ml.spotAngle = 60;
        ml.shadows = LightShadows.Soft;
        moon.transform.LookAt(new Vector3(2, 0, 1));
    }

    /// <summary>Turns a scene object into an E-to-interact NPC/object. Needs a
    /// NetworkObject because the server addresses it by NetworkObjectId.</summary>
    private static void MakeInteractable(GameObject go, InteractableKind kind, string displayName, string prompt)
    {
        if (go == null) return;
        go.layer = 2;
        go.AddComponent<NetworkObject>();
        go.AddComponent<QuestMarker>();
        var interactable = go.AddComponent<Interactable>();
        var so = new SerializedObject(interactable);
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.FindProperty("displayName").stringValue = displayName;
        so.FindProperty("prompt").stringValue = prompt;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Instantiates an imported art asset; no-op if it hasn't imported.</summary>
    private static GameObject Art(Transform parent, string artPath, Vector3 pos, float yaw = 0f)
    {
        var asset = LoadArt(artPath);
        if (asset == null) return null;
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0, yaw, 0);
        ApplyClassicMaterials(go);
        return go;
    }


    private static void ApplyClassicMaterials(GameObject go)
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            var materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                var source = materials[i]; if (source == null) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId);
                string path = MaterialsDir + "/Classic_" + guid + "_" + localId.ToString().Replace("-", "n") + ".mat";
                var converted = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (converted == null)
                {
                    converted = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    converted.color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                    Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                    if (texture != null) converted.SetTexture("_BaseMap", texture);
                    System.IO.Directory.CreateDirectory(MaterialsDir);
                    AssetDatabase.CreateAsset(converted, path);
                }
                materials[i] = converted;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static GameObject LoadArt(string artPath)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/{artPath}.glb");
    }

    /// <summary>Invisible box collider — the art models have no collision of their own.</summary>
    private static void Collider(Transform parent, Vector3 pos, Vector3 size)
    {
        var go = new GameObject("Collision", typeof(BoxCollider));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.GetComponent<BoxCollider>().size = size;
    }

    private static void WarmLight(Transform parent, Vector3 pos, float intensity, float range)
    {
        var go = new GameObject("TorchLight", typeof(Light));
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var l = go.GetComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.75f, 0.5f);
        l.intensity = intensity;
        l.range = range;
    }

    /// <summary>Creates and saves a material asset so scenes reference a real
    /// asset rather than a leaked in-memory instance.</summary>
    private static Material MakeMaterial(Color color)
    {
        System.IO.Directory.CreateDirectory(MaterialsDir);
        string name = $"Mat_{ColorUtility.ToHtmlStringRGB(color)}";
        string path = $"{MaterialsDir}/{name}.mat";

        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) { existing.shader = Shader.Find("Universal Render Pipeline/Unlit"); existing.color = color; EditorUtility.SetDirty(existing); return existing; }

        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ------------------------------------------------------------------
    // UI helpers
    // ------------------------------------------------------------------

    private static Canvas MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        return canvas;
    }

    private static void MakeEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static GameObject MakePanel(Transform parent, Vector2 size)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        var v = go.GetComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(18, 18, 18, 18);
        v.spacing = 10;
        v.childControlHeight = false;
        v.childControlWidth = true;
        v.childForceExpandHeight = false;
        v.childForceExpandWidth = true;
        return go;
    }

    private static GameObject MakeCornerPanel(Transform parent, Vector2 size)
    {
        var go = MakePanel(parent, size);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(20, 20);
        var v = go.GetComponent<VerticalLayoutGroup>();
        v.childForceExpandHeight = true;
        return go;
    }

    private static Text MakeText(Transform parent, string name, string value, int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = value;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = alignment;
        go.GetComponent<LayoutElement>().minHeight = fontSize + 10;
        return t;
    }

    private static InputField MakeInputField(Transform parent, string name, string placeholder)
    {
        // InputField is added LAST (below): adding it up front makes it cache a
        // null textComponent in OnEnable and render incorrectly.
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(1, 1, 1, 0.08f);
        go.GetComponent<LayoutElement>().minHeight = 32;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.supportRichText = false;
        StretchFull(textGo.GetComponent<RectTransform>(), 6);

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(go.transform, false);
        var placeholderText = placeholderGo.GetComponent<Text>();
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.text = placeholder;
        placeholderText.fontStyle = FontStyle.Italic;
        placeholderText.color = new Color(1, 1, 1, 0.4f);
        StretchFull(placeholderGo.GetComponent<RectTransform>(), 6);

        var field = go.AddComponent<InputField>();
        field.textComponent = text;
        field.placeholder = placeholderText;
        field.targetGraphic = go.GetComponent<Image>();
        return field;
    }

    private static Button MakeButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.25f, 0.35f, 0.55f, 1f);
        go.GetComponent<LayoutElement>().minHeight = 36;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        StretchFull(textGo.GetComponent<RectTransform>(), 0);

        return go.GetComponent<Button>();
    }

    private static void StretchFull(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}
#endif
