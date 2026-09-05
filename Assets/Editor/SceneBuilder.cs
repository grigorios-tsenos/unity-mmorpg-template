#if UNITY_EDITOR
using MmoTemplate.Player;
using MmoTemplate.Rpg;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Rebuilds the authored single-player chamber and its two character prefabs.</summary>
public static class SceneBuilder
{
    private const string ScenesDir = "Assets/Scenes";
    private const string PrefabsDir = "Assets/Prefabs";
    private const string PlayerPrefabPath = PrefabsDir + "/Player.prefab";
    private const string MaterialsDir = "Assets/Materials";

    [MenuItem("Azeroth04/Build Oathfire Room")]
    public static void Build()
    {
        System.IO.Directory.CreateDirectory(ScenesDir);
        System.IO.Directory.CreateDirectory(PrefabsDir);

        ClassicContentBuilder.Build();
        GameObject playerPrefab = BuildPlayerPrefab();
        GameObject enemyPrefab = BuildEnemyPrefab();

        BuildWorldScene(playerPrefab, enemyPrefab);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesDir}/World.unity", true),
        };

        EditorSceneManager.OpenScene($"{ScenesDir}/World.unity");
        AssetDatabase.SaveAssets();

        if (!Application.isBatchMode) EditorUtility.DisplayDialog("The Oathfire Chamber", "The single-player room is ready. Press Play to explore.", "Enter");
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

        go.AddComponent<PlayerStats>();
        go.AddComponent<PlayerController>();
        go.AddComponent<PlayerCombat>();
        go.AddComponent<CharacterVisual>();
        go.layer = 2;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PlayerPrefabPath);
        PrefabUtility.SavePrefabAsset(prefab);
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

        go.AddComponent<Enemy>();
        go.AddComponent<CharacterVisual>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsDir + "/Guardian.prefab");
        PrefabUtility.SavePrefabAsset(prefab);
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------------
    // Bootstrap.unity — login / connect screen
    // ------------------------------------------------------------------

    private static void BuildWorldScene(GameObject playerPrefab, GameObject enemyPrefab)
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

        // Local quest and encounter services.
        var gm = new GameObject("GameManager");
        var spawner = gm.AddComponent<EnemySpawner>();
        var quest = gm.AddComponent<QuestManager>();

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
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.SetPositionAndRotation(new Vector3(-2, .2f, 4), Quaternion.Euler(0,180,0));
        var camera = camGo.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.07f,.085f,.11f);
        camera.fieldOfView = 58;
        new GameObject("Chamber ambience").AddComponent<ChamberAmbience>();

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

        BuildProvingCircle(room.transform);

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
        brazier.AddComponent<OathfireVisual>();

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

    /// <summary>Adds a local interaction and its presentation to a room prop.</summary>
    private static void MakeInteractable(GameObject go, InteractableKind kind, string displayName, string prompt)
    {
        if (go == null) throw new System.InvalidOperationException("A required chamber art asset failed to import.");
        go.layer = 2;
        go.AddComponent<QuestMarker>();
        go.AddComponent<CharacterVisual>();
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
        if (asset == null) throw new System.InvalidOperationException("Missing art asset: " + artPath);
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
                    converted = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                    converted.color = source.HasProperty("_BaseColor") ? source.GetColor("_BaseColor") : source.color;
                    Texture texture = source.HasProperty("_BaseMap") ? source.GetTexture("_BaseMap") : source.mainTexture;
                    if (texture != null) converted.SetTexture("_BaseMap", texture);
                    System.IO.Directory.CreateDirectory(MaterialsDir);
                    AssetDatabase.CreateAsset(converted, path);
                }
                converted.shader = Shader.Find("Universal Render Pipeline/Simple Lit");
                converted.SetFloat("_SpecularHighlights", 0);
                converted.DisableKeyword("_SPECGLOSSMAP");
                var diffuse = ClassicTextureImporter.Diffuse(source);
                if (diffuse != null) converted.SetTexture("_BaseMap", diffuse);
                EditorUtility.SetDirty(converted);
                materials[i] = converted;
            }
            renderer.sharedMaterials = materials;
        }
    }


    private static void BuildProvingCircle(Transform parent)
    {
        var ring=new GameObject("Eastern proving circle");ring.transform.SetParent(parent,false);ring.transform.position=new Vector3(4.8f,.035f,-1.5f);
        var line=ring.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=64;line.widthMultiplier=.07f;
        line.sharedMaterial=MakeMaterial(new Color(.55f,.39f,.16f));
        for(int i=0;i<64;i++){float angle=i*Mathf.PI*2/64;line.SetPosition(i,new Vector3(Mathf.Cos(angle)*2.6f,0,Mathf.Sin(angle)*2.6f));}
        foreach(float x in new[]{2f,7.6f})
        {
            Art(parent,"Props/pillar_decorated",new Vector3(x,0,-5.8f));
            Art(parent,"Props/banner_patternA_red",new Vector3(x,2,-5.65f));
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
        var go = new GameObject("TorchLight", typeof(Light), typeof(TorchFlicker));
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
        if (existing != null) { existing.shader = Shader.Find("Universal Render Pipeline/Simple Lit"); existing.color = color; EditorUtility.SetDirty(existing); return existing; }

        var mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit")) { color = color };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

}
#endif
