using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TalesOfTheBrave.Graphics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TalesOfTheBrave.Unity.UI;

[CustomEditor(typeof(ExternalGraphicsCatalog))]
public sealed class ExternalGraphicsCatalogEditor : Editor
{
    [InitializeOnLoadMethod]
    private static void RescanOpenSceneAfterScriptsReload()
    {
        EditorApplication.delayCall += PrepareOpenScene;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode) PrepareOpenScene();
    }

    private static void PrepareOpenScene()
    {
        if (EditorApplication.isPlaying) return;
        var catalog = FindFirstObjectByType<ExternalGraphicsCatalog>();
        if (catalog != null) Scan(catalog);

        var bootstrap = FindFirstObjectByType<GameBootstrap>();
        if (bootstrap == null) return;
        var serializedBootstrap = new SerializedObject(bootstrap);
        var frameProperty = serializedBootstrap.FindProperty("locationWindowFrame");
        if (frameProperty.objectReferenceValue == null)
            frameProperty.objectReferenceValue = AssetDatabase.LoadAllAssetsAtPath("Assets/Graphics/window-location.png")
                .OfType<Sprite>()
                .OrderByDescending(sprite => sprite.rect.width * sprite.rect.height)
                .FirstOrDefault();
        var materialProperty = serializedBootstrap.FindProperty("locationEntityraitMaterial");
        if (materialProperty.objectReferenceValue == null)
            materialProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/Graphics/LocationPortrait.mat");
        serializedBootstrap.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Scan Project Sprites")) Scan((ExternalGraphicsCatalog)target);
    }

    [MenuItem("Tales of the Brave/Graphics/Rebuild External Graphics Catalog")]
    private static void RebuildSceneCatalog()
    {
        var catalog = FindFirstObjectByType<ExternalGraphicsCatalog>();
        if (catalog == null)
            throw new InvalidOperationException("The open scene has no ExternalGraphicsCatalog component.");
        Scan(catalog);
    }

    internal static void Scan(ExternalGraphicsCatalog catalog)
    {
        var entries = new List<ExternalGraphicsCatalog.Entry>();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var paths = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct()
            .OrderBy(path => path, StringComparer.Ordinal);

        foreach (var path in paths)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            var assetName = Path.GetFileNameWithoutExtension(path);
            var isMultiple = sprites.Length > 1;
            foreach (var sprite in sprites)
            {
                var name = SpriteCatalogNaming.CreateName(assetName, sprite.name, isMultiple);
                if (!usedNames.Add(name))
                    throw new InvalidOperationException($"Multiple sprites generate the external name '{name}'.");
                entries.Add(new ExternalGraphicsCatalog.Entry(name, sprite));
            }
        }

        Undo.RecordObject(catalog, "Scan Project Sprites");
        catalog.ReplaceEntries(entries);
        EditorUtility.SetDirty(catalog);
        if (catalog.gameObject.scene.IsValid()) EditorSceneManager.MarkSceneDirty(catalog.gameObject.scene);
        Debug.Log($"External graphics catalog updated with {entries.Count} sprites.", catalog);
    }
}
