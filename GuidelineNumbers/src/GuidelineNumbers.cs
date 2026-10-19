using System;
using System.Collections.Generic;
using System.Reflection;
using Bindito.Core;
using HarmonyLib;
using Timberborn.CameraSystem;
using Timberborn.ConstructionGuidelines;
using Timberborn.ModManagerScene;
using Timberborn.SingletonSystem;
using UnityEngine;
using UnityEngine.Rendering;

namespace Local.GuidelineNumbers
{
    public sealed class GuidelineNumbersModStarter : IModStarter
    {
        public void StartMod(IModEnvironment modEnvironment)
        {
            new Harmony("gem0303.GuidelineNumbers").PatchAll();
        }
    }

    [Context("Game")]
    public sealed class GuidelineNumbersConfigurator : Configurator
    {
        protected override void Configure()
        {
            Bind<GuidelineNumbersService>().AsSingleton();
        }
    }

    public sealed class GuidelineNumbersService : IPostLoadableSingleton, ILateUpdatableSingleton, IDisposable
    {
        private const float HeightOffset = 0.022f;
        private const float PixelSize = 0.095f;

        // Row-major 3x5 bitmap glyphs for digits 0 through 9.
        private static readonly string[] Digits =
        {
            "111101101101111", "010110010010111", "111001111100111", "111001111001111", "101101111001001",
            "111100111001111", "111100111101111", "111001001001001", "111101111101111", "111101111001111"
        };

        private readonly CameraService _cameraService;
        private readonly Dictionary<Vector2Int, Vector3> _topmost = new Dictionary<Vector2Int, Vector3>();
        private readonly Dictionary<int, Mesh> _meshes = new Dictionary<int, Mesh>();
        private readonly List<Label> _pool = new List<Label>();

        private GameObject _root;
        private Material _material;
        private Texture2D _pixel;
        private int _visibleCount;
        private float _rotation = float.NaN;

        internal static GuidelineNumbersService Instance { get; private set; }

        [Inject]
        public GuidelineNumbersService(CameraService cameraService)
        {
            _cameraService = cameraService;
        }

        public void PostLoad()
        {
            _root = new GameObject("GuidelineNumbers");
            _pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _pixel.name = "GuidelineNumbers.Pixel";
            _pixel.SetPixel(0, 0, Color.white);
            _pixel.Apply();

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("Guideline Numbers could not find the Sprites/Default shader.");
            }

            _material = new Material(shader);
            _material.name = "GuidelineNumbers.Material";
            _material.mainTexture = _pixel;
            _material.color = new Color(1f, 1f, 1f, 0.82f);
            Instance = this;
        }

        internal void SetTiles(
            IList<Matrix4x4> sameLevel,
            IList<Matrix4x4> below,
            IList<Matrix4x4> above,
            Vector3Int center)
        {
            _topmost.Clear();
            AddTopmost(sameLevel);
            AddTopmost(below);
            AddTopmost(above);

            EnsurePool(_topmost.Count);
            _visibleCount = 0;
            Quaternion rotation = CurrentRotation();

            foreach (KeyValuePair<Vector2Int, Vector3> tile in _topmost)
            {
                if (tile.Key.x != center.x && tile.Key.y != center.y)
                {
                    continue;
                }

                int number = Math.Abs(tile.Key.x - center.x) + Math.Abs(tile.Key.y - center.y) + 1;
                Label label = _pool[_visibleCount++];
                label.Filter.sharedMesh = GetNumberMesh(number);
                label.Object.transform.position = tile.Value + Vector3.up * HeightOffset;
                label.Object.transform.rotation = rotation;
                label.Object.SetActive(true);
            }

            HideUnused();
        }

        internal void Clear()
        {
            _visibleCount = 0;
            HideUnused();
        }

        public void LateUpdateSingleton()
        {
            if (_visibleCount == 0)
            {
                return;
            }

            float rotation = SnappedRotation();
            if (Mathf.Approximately(rotation, _rotation))
            {
                return;
            }

            _rotation = rotation;
            Quaternion quaternion = Quaternion.Euler(90f, rotation, 0f);
            for (int i = 0; i < _visibleCount; i++)
            {
                _pool[i].Object.transform.rotation = quaternion;
            }
        }

        public void Dispose()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            foreach (Mesh mesh in _meshes.Values)
            {
                UnityEngine.Object.Destroy(mesh);
            }

            if (_material != null)
            {
                UnityEngine.Object.Destroy(_material);
            }

            if (_pixel != null)
            {
                UnityEngine.Object.Destroy(_pixel);
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
            }
        }

        private void AddTopmost(IList<Matrix4x4> matrices)
        {
            for (int i = 0; i < matrices.Count; i++)
            {
                Vector3 position = matrices[i].GetColumn(3);
                Vector2Int coordinates = new Vector2Int(
                    Mathf.FloorToInt(position.x),
                    Mathf.FloorToInt(position.z));
                Vector3 current;
                if (!_topmost.TryGetValue(coordinates, out current) || position.y > current.y)
                {
                    _topmost[coordinates] = position;
                }
            }
        }

        private void EnsurePool(int count)
        {
            while (_pool.Count < count)
            {
                GameObject gameObject = new GameObject("GuidelineNumber");
                gameObject.transform.SetParent(_root.transform, false);
                MeshFilter filter = gameObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = _material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                gameObject.SetActive(false);
                _pool.Add(new Label(gameObject, filter));
            }
        }

        private void HideUnused()
        {
            for (int i = _visibleCount; i < _pool.Count; i++)
            {
                if (_pool[i].Object.activeSelf)
                {
                    _pool[i].Object.SetActive(false);
                }
            }
        }

        private Quaternion CurrentRotation()
        {
            if (float.IsNaN(_rotation))
            {
                _rotation = SnappedRotation();
            }

            return Quaternion.Euler(90f, _rotation, 0f);
        }

        private float SnappedRotation()
        {
            return Mathf.Floor((Mathf.Repeat(_cameraService.HorizontalAngle, 360f) + 22.5f) / 90f) * 90f;
        }

        private Mesh GetNumberMesh(int number)
        {
            Mesh mesh;
            if (_meshes.TryGetValue(number, out mesh))
            {
                return mesh;
            }

            string text = number.ToString();
            int width = text.Length * 3 + text.Length - 1;
            float left = -width * PixelSize * 0.5f;
            float bottom = -2.5f * PixelSize;
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            for (int digitIndex = 0; digitIndex < text.Length; digitIndex++)
            {
                string glyph = Digits[text[digitIndex] - '0'];
                for (int row = 0; row < 5; row++)
                {
                    for (int column = 0; column < 3; column++)
                    {
                        if (glyph[row * 3 + column] == '1')
                        {
                            float x = left + (digitIndex * 4 + column) * PixelSize;
                            float y = bottom + (4 - row) * PixelSize;
                            AddPixel(vertices, triangles, x, y);
                        }
                    }
                }
            }

            mesh = new Mesh();
            mesh.name = "GuidelineNumber." + number;
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            _meshes[number] = mesh;
            return mesh;
        }

        private static void AddPixel(List<Vector3> vertices, List<int> triangles, float x, float y)
        {
            int first = vertices.Count;
            vertices.Add(new Vector3(x, y, 0f));
            vertices.Add(new Vector3(x + PixelSize, y, 0f));
            vertices.Add(new Vector3(x, y + PixelSize, 0f));
            vertices.Add(new Vector3(x + PixelSize, y + PixelSize, 0f));
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 1);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        private sealed class Label
        {
            public readonly GameObject Object;
            public readonly MeshFilter Filter;

            public Label(GameObject gameObject, MeshFilter filter)
            {
                Object = gameObject;
                Filter = filter;
            }
        }
    }

    [HarmonyPatch(typeof(ConstructionGuidelinesRenderingService), "GetGuidelinesFromMousePosition")]
    internal static class CursorGuidelinesUpdatedPatch
    {
        // CrossParameters is internal in the shipped game assembly, so only its
        // public Center property is resolved at runtime.
        private static readonly PropertyInfo CenterProperty = AccessTools.Property(
            AccessTools.TypeByName("Timberborn.ConstructionGuidelines.CrossParameters"),
            "Center");

        [HarmonyPostfix]
        private static void Postfix(
            List<Matrix4x4> ____tilesAtSameLevel,
            List<Matrix4x4> ____tilesBelow,
            List<Matrix4x4> ____tilesAbove,
            object ____lastCrossParameters)
        {
            GuidelineNumbersService service = GuidelineNumbersService.Instance;
            if (service == null)
            {
                return;
            }

            Vector3Int center = (Vector3Int)CenterProperty.GetValue(____lastCrossParameters, null);
            service.SetTiles(____tilesAtSameLevel, ____tilesBelow, ____tilesAbove, center);
        }
    }

    [HarmonyPatch]
    internal static class GuidelinesClearedPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(
                AccessTools.TypeByName("Timberborn.ConstructionGuidelines.CrossParameters"),
                "Reset");
        }

        [HarmonyPostfix]
        private static void Postfix()
        {
            GuidelineNumbersService service = GuidelineNumbersService.Instance;
            if (service != null)
            {
                service.Clear();
            }
        }
    }
}
