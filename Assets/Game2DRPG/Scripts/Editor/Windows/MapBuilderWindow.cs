#nullable enable
/*
 * Copyright (c) 2026.
 */

using Game2DRPG.Map.Runtime;
using UnityEditor;
using UnityEngine;

namespace Game2DRPG.Map.Editor
{
    public sealed class MapBuilderWindow : EditorWindow
    {
        [SerializeField] private int seed = 20260307;
        [SerializeField] private ResourceCatalogAsset? catalog;
        [SerializeField] private TileLayerRuleAsset? rules;
        [SerializeField] private AmbientAnimationProfileAsset? ambientProfile;
        [SerializeField] private PCGProfileAsset? pcgProfile;
        private Vector2 _scroll;

        [MenuItem("Tools/Game2DRPG/Map Builder")]
        public static void ShowWindow()
        {
            var window = GetWindow<MapBuilderWindow>();
            window.titleContent = new GUIContent("Map Builder");
            window.minSize = new Vector2(520f, 640f);
            window.RefreshState();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshState();
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Catalog", EditorStyles.boldLabel);
            catalog = (ResourceCatalogAsset?)EditorGUILayout.ObjectField("Catalog Asset", catalog, typeof(ResourceCatalogAsset), false);
            if (GUILayout.Button("Scan Tiny Swords Catalog"))
            {
                MapBuilderController.ScanTinySwordsCatalog();
                RefreshState();
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Rules", EditorStyles.boldLabel);
            rules = (TileLayerRuleAsset?)EditorGUILayout.ObjectField("Tile Rules", rules, typeof(TileLayerRuleAsset), false);
            ambientProfile = (AmbientAnimationProfileAsset?)EditorGUILayout.ObjectField("Ambient Profile", ambientProfile, typeof(AmbientAnimationProfileAsset), false);

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Showcase", EditorStyles.boldLabel);
            if (GUILayout.Button("Build RoomChain Showcase"))
            {
                MapBuilderController.BuildRoomChainShowcase();
            }

            if (GUILayout.Button("Build OpenWorld Showcase"))
            {
                MapBuilderController.BuildOpenWorldShowcase();
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("PCG", EditorStyles.boldLabel);
            pcgProfile = (PCGProfileAsset?)EditorGUILayout.ObjectField("PCG Profile", pcgProfile, typeof(PCGProfileAsset), false);
            seed = EditorGUILayout.IntField("Seed", seed);
            if (GUILayout.Button("Generate RoomChain"))
            {
                MapBuilderController.GenerateRoomChain(seed);
            }

            if (GUILayout.Button("Generate OpenWorld"))
            {
                MapBuilderController.GenerateOpenWorld(seed);
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Persistence", EditorStyles.boldLabel);
            if (GUILayout.Button("Save Current Level Config"))
            {
                MapBuilderController.SaveCurrentLevelConfig();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load RoomChain Config"))
            {
                MapBuilderController.LoadLevelConfig(MapMode.RoomChain);
            }

            if (GUILayout.Button("Load OpenWorld Config"))
            {
                MapBuilderController.LoadLevelConfig(MapMode.OpenWorld);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();
        }

        private void RefreshState()
        {
            MapBuilderController.EnsureFoundationAssets();
            catalog = MapBuilderController.EnsureCatalogAsset();
            rules = MapBuilderController.EnsureRulesAsset();
            ambientProfile = MapBuilderController.EnsureAmbientProfile();
            pcgProfile = MapBuilderController.EnsurePcgProfile();
            Repaint();
        }
    }
}
