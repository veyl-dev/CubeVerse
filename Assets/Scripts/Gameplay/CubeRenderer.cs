using System;
using System.Collections;
using System.Collections.Generic;
using CubeVerse.Engine;
using UnityEngine;

namespace CubeVerse.Gameplay
{
    public sealed class CubeRenderer : MonoBehaviour
    {
        public const float Spacing = 1.08f;
        private const float BodySize = 0.965f;
        private readonly Dictionary<int, Transform> visuals = new Dictionary<int, Transform>();
        private readonly Dictionary<StickerColor, Material> stickerMaterials = new Dictionary<StickerColor, Material>();
        private Material bodyMaterial;
        private CubeModel model;
        private float turnDuration = 0.14f;

        public Transform Root { get; private set; }
        public float TurnDuration { get => turnDuration; set => turnDuration = Mathf.Clamp(value, 0.02f, 0.6f); }

        public void Initialize(CubeModel cubeModel)
        {
            model = cubeModel;
            if (Root == null)
            {
                var holder = new GameObject("Cube · turntable");
                Root = holder.transform;
                Root.SetParent(transform, false);
            }
            Root.localScale = Vector3.one * Mathf.Clamp(3.75f / (model.Size * Spacing), 0.68f, 1.38f);
            Build();
        }

        public void SetSize(CubeModel cubeModel)
        {
            model = cubeModel;
            Build();
        }

        public void ResetOrientation() { if (Root != null) Root.localRotation = Quaternion.Euler(-18f, 26f, 0f); }

        public bool TryPick(Ray ray, out RaycastHit hit)
        {
            if (Physics.Raycast(ray, out hit, 50f))
                return hit.transform != null && hit.transform.IsChildOf(Root);
            return false;
        }

        public int LayerAt(Vector3 worldPoint, CubeAxis axis)
        {
            Vector3 p = Root.InverseTransformPoint(worldPoint);
            float center = (model.Size - 1) * 0.5f;
            float value = axis == CubeAxis.X ? p.x : axis == CubeAxis.Y ? p.y : p.z;
            return Mathf.Clamp(Mathf.RoundToInt(value / Spacing + center), 0, model.Size - 1);
        }

        public Int3 FaceNormal(Vector3 worldNormal)
        {
            Vector3 n = Root.InverseTransformDirection(worldNormal).normalized;
            int x = Mathf.Abs(n.x) > 0.7f ? (n.x > 0 ? 1 : -1) : 0;
            int y = Mathf.Abs(n.y) > 0.7f ? (n.y > 0 ? 1 : -1) : 0;
            int z = Mathf.Abs(n.z) > 0.7f ? (n.z > 0 ? 1 : -1) : 0;
            return new Int3(x, y, z);
        }

        public void Turn(CubeMove move, Action completed = null, bool animate = true)
        {
            if (model == null) { completed?.Invoke(); return; }
            var affected = new List<Transform>();
            foreach (CubePiece piece in model.Pieces)
            {
                if (move.Selects(CubeModel.Coordinate(piece.Position, move.Axis)) && visuals.TryGetValue(piece.Id, out Transform visual))
                    affected.Add(visual);
            }
            model.Apply(move);
            if (!animate || affected.Count == 0)
            {
                SnapAll();
                completed?.Invoke();
                return;
            }
            StartCoroutine(AnimateTurn(move, affected, completed));
        }

        public void SnapAll()
        {
            if (model == null) return;
            foreach (CubePiece piece in model.Pieces)
                if (visuals.TryGetValue(piece.Id, out Transform visual))
                {
                    visual.localPosition = ToLocal(piece.Position, model.Size);
                    visual.localRotation = RotationFor(piece);
                }
        }

        private void Build()
        {
            if (Root == null || model == null) return;
            foreach (Transform child in Root) Destroy(child.gameObject);
            visuals.Clear();
            if (bodyMaterial != null) Destroy(bodyMaterial);
            foreach (Material material in stickerMaterials.Values) if (material != null) Destroy(material);
            stickerMaterials.Clear();
            bodyMaterial = MakeMaterial(new Color(0.025f, 0.035f, 0.055f));
            foreach (StickerColor color in Enum.GetValues(typeof(StickerColor)))
                stickerMaterials[color] = MakeMaterial(ColorFor(color));

            foreach (CubePiece piece in model.Pieces)
            {
                if (piece.Stickers.Count == 0) continue;
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Cubie " + piece.Id;
                body.transform.SetParent(Root, false);
                body.transform.localPosition = ToLocal(piece.Position, model.Size);
                body.transform.localRotation = RotationFor(piece);
                body.transform.localScale = Vector3.one * BodySize;
                body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;
                foreach (CubeSticker sticker in piece.Stickers)
                    AddSticker(body.transform, new CubeSticker(SourceNormal(sticker.Color), sticker.Color));
                visuals[piece.Id] = body.transform;
            }
        }

        private IEnumerator AnimateTurn(CubeMove move, List<Transform> affected, Action completed)
        {
            GameObject pivotObject = new GameObject("Turn · " + move.Notation);
            Transform pivot = pivotObject.transform;
            pivot.SetParent(Root, false);
            pivot.localPosition = Vector3.zero;
            pivot.localRotation = Quaternion.identity;
            foreach (Transform visual in affected) visual.SetParent(pivot, true);

            Vector3 axis = move.Axis == CubeAxis.X ? Vector3.right : move.Axis == CubeAxis.Y ? Vector3.up : Vector3.forward;
            float angle = 90f * move.QuarterTurns;
            float elapsed = 0f;
            while (elapsed < turnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / turnDuration);
                float eased = t * t * (3f - 2f * t);
                pivot.localRotation = Quaternion.AngleAxis(angle * eased, axis);
                yield return null;
            }
            pivot.localRotation = Quaternion.AngleAxis(angle, axis);
            foreach (Transform visual in affected)
            {
                visual.SetParent(Root, true);
                int id = FindPieceId(visual);
                if (visuals.TryGetValue(id, out Transform registered))
                {
                    CubePiece piece = model.GetPiece(id);
                    registered.localPosition = ToLocal(piece.Position, model.Size);
                    registered.localRotation = RotationFor(piece);
                }
            }
            Destroy(pivotObject);
            completed?.Invoke();
        }

        private int FindPieceId(Transform visual)
        {
            foreach (KeyValuePair<int, Transform> entry in visuals)
                if (entry.Value == visual) return entry.Key;
            return -1;
        }

        private static Vector3 ToLocal(Int3 position, int size)
        {
            float center = (size - 1) * 0.5f;
            return new Vector3((position.x - center) * Spacing, (position.y - center) * Spacing, (position.z - center) * Spacing);
        }

        private static Quaternion RotationFor(CubePiece piece)
        {
            bool haveFirst = false;
            Vector3 sourceFirst = Vector3.zero, targetFirst = Vector3.zero;
            foreach (CubeSticker sticker in piece.Stickers)
            {
                Vector3 source = ToVector(SourceNormal(sticker.Color));
                Vector3 target = ToVector(sticker.Normal);
                if (!haveFirst)
                {
                    sourceFirst = source;
                    targetFirst = target;
                    haveFirst = true;
                    continue;
                }
                if (Vector3.Cross(sourceFirst, source).sqrMagnitude < 0.5f) continue;
                Quaternion sourceBasis = Quaternion.LookRotation(Vector3.Cross(sourceFirst, source), source);
                Quaternion targetBasis = Quaternion.LookRotation(Vector3.Cross(targetFirst, target), target);
                return targetBasis * Quaternion.Inverse(sourceBasis);
            }
            return haveFirst ? Quaternion.FromToRotation(sourceFirst, targetFirst) : Quaternion.identity;
        }

        private static Vector3 ToVector(Int3 value) => new Vector3(value.x, value.y, value.z);

        private static Int3 SourceNormal(StickerColor color)
        {
            switch (color)
            {
                case StickerColor.Red: return new Int3(1, 0, 0);
                case StickerColor.Orange: return new Int3(-1, 0, 0);
                case StickerColor.White: return new Int3(0, 1, 0);
                case StickerColor.Yellow: return new Int3(0, -1, 0);
                case StickerColor.Green: return new Int3(0, 0, 1);
                default: return new Int3(0, 0, -1);
            }
        }

        private void AddSticker(Transform parent, CubeSticker sticker)
        {
            GameObject face = GameObject.CreatePrimitive(PrimitiveType.Cube);
            face.name = sticker.Color + " sticker";
            face.transform.SetParent(parent, false);
            Vector3 normal = new Vector3(sticker.Normal.x, sticker.Normal.y, sticker.Normal.z);
            if (normal.x != 0)
            {
                face.transform.localPosition = normal * (BodySize * 0.5f + 0.004f);
                face.transform.localScale = new Vector3(0.018f, 0.79f, 0.79f);
            }
            else if (normal.y != 0)
            {
                face.transform.localPosition = normal * (BodySize * 0.5f + 0.004f);
                face.transform.localScale = new Vector3(0.79f, 0.018f, 0.79f);
            }
            else
            {
                face.transform.localPosition = normal * (BodySize * 0.5f + 0.004f);
                face.transform.localScale = new Vector3(0.79f, 0.79f, 0.018f);
            }
            Collider collider = face.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            face.GetComponent<Renderer>().sharedMaterial = stickerMaterials[sticker.Color];
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.18f);
            return material;
        }

        private static Color ColorFor(StickerColor color)
        {
            switch (color)
            {
                case StickerColor.White: return new Color(0.93f, 0.95f, 0.98f);
                case StickerColor.Yellow: return new Color(1.00f, 0.73f, 0.16f);
                case StickerColor.Red: return new Color(0.94f, 0.22f, 0.29f);
                case StickerColor.Orange: return new Color(1.00f, 0.43f, 0.19f);
                case StickerColor.Blue: return new Color(0.22f, 0.51f, 0.99f);
                default: return new Color(0.10f, 0.78f, 0.58f);
            }
        }
    }
}
