using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CubeVerse.Engine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CubeVerse.Gameplay
{
    public enum GameMode { Classic, Speedcubing, Practice }

    public sealed class CubeVerseGame : MonoBehaviour
    {
        private CubeModel model;
        private CubeRenderer cubeRenderer;
        private Camera gameCamera;
        private GameUI ui;
        private AudioSource audioSource;
        private AudioClip moveTone;
        private readonly Stopwatch timer = new Stopwatch();
        private readonly Stopwatch inspection = new Stopwatch();
        private readonly List<CubeMove> moveHistory = new List<CubeMove>();
        private readonly Stack<CubeMove> redoStack = new Stack<CubeMove>();
        private LocalStatistics stats;
        private GameMode mode = GameMode.Classic;
        private int cubeSize = 3;
        private string scramble = string.Empty;
        private bool hasScramble;
        private bool attemptFinished;
        private bool attemptInvalidated;
        private bool isInspecting;
        private bool turnInProgress;
        private bool pointerDown;
        private bool beganOnCube;
        private bool draggingView;
        private bool pointerOverUI;
        private int activeFinger = int.MinValue;
        private Vector2 pressPoint;
        private Vector2 previousPoint;
        private RaycastHit pickedHit;
        private bool soundEnabled;
        private bool hapticsEnabled;
        private bool pausedForBackground;
        private bool timerWasRunning;
        private bool inspectionWasRunning;
        private double elapsedOffsetSeconds;
        private double inspectionOffsetSeconds;
        private float backgroundMotionSensitivity = 0.35f;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            stats = LocalStatistics.Load();
            soundEnabled = PlayerPrefs.GetInt("CubeVerse.Sound", 0) == 1;
            hapticsEnabled = PlayerPrefs.GetInt("CubeVerse.Haptics", 0) == 1;
            CreateWorld();
            CreateGameUI();
            NewCube(cubeSize, false);
            cubeRenderer.ResetOrientation();
            RefreshUI("Choose a size, scramble, then drag across a face to turn a layer.");
        }

        private void Update()
        {
            ui?.SetClock(isInspecting ? Mathf.Max(0f, 15f - (float)(inspectionOffsetSeconds + inspection.Elapsed.TotalSeconds)) : elapsedOffsetSeconds + timer.Elapsed.TotalSeconds,
                mode == GameMode.Speedcubing && isInspecting);
        }

        private void LateUpdate() { ReadPointer(); }

        private void CreateWorld()
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("CubeVerse Camera");
                gameCamera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }
            gameCamera.transform.position = new Vector3(0f, 0f, -10.5f);
            gameCamera.transform.rotation = Quaternion.identity;
            gameCamera.fieldOfView = 39f;
            gameCamera.nearClipPlane = 0.05f;
            gameCamera.farClipPlane = 100f;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = new Color(0.025f, 0.035f, 0.06f);
            if (FindObjectsOfType<Light>().Length == 0)
            {
                var lightObject = new GameObject("CubeVerse key light");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.15f;
                light.color = new Color(0.83f, 0.90f, 1f);
                lightObject.transform.rotation = Quaternion.Euler(34f, -32f, 0f);
            }
            cubeRenderer = gameObject.AddComponent<CubeRenderer>();
            var audioObject = new GameObject("CubeVerse move feedback");
            audioObject.transform.SetParent(transform, false);
            audioSource = audioObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            moveTone = CreateMoveTone();
        }

        private void CreateGameUI()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
            var canvasObject = new GameObject("CubeVerse Interface");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            ui = canvasObject.AddComponent<GameUI>();
            ui.Initialize(OnSizeSelected, OnModeSelected, Scramble, RestartScramble, Undo, Redo,
                ToggleSound, ToggleHaptics, ShowStatistics);
            ui.SetToggles(soundEnabled, hapticsEnabled);
        }

        private void NewCube(int size, bool makeScramble)
        {
            cubeSize = Mathf.Clamp(size, 2, 5);
            model = new CubeModel(cubeSize);
            cubeRenderer.Initialize(model);
            moveHistory.Clear();
            redoStack.Clear();
            timer.Reset();
            inspection.Reset();
            elapsedOffsetSeconds = 0;
            inspectionOffsetSeconds = 0;
            isInspecting = false;
            hasScramble = false;
            attemptFinished = false;
            attemptInvalidated = false;
            scramble = string.Empty;
            if (makeScramble) Scramble();
            RefreshUI(null);
        }

        private void OnSizeSelected(int size)
        {
            if (turnInProgress) return;
            NewCube(size, true);
            cubeRenderer.ResetOrientation();
            Haptic();
        }

        private void OnModeSelected(GameMode selected)
        {
            if (turnInProgress || mode == selected) return;
            mode = selected;
            NewCube(cubeSize, true);
            cubeRenderer.ResetOrientation();
        }

        private void Scramble()
        {
            if (turnInProgress || model == null) return;
            model.Reset();
            moveHistory.Clear();
            redoStack.Clear();
            timer.Reset();
            inspection.Reset();
            elapsedOffsetSeconds = 0;
            inspectionOffsetSeconds = 0;
            isInspecting = false;
            attemptFinished = false;
            attemptInvalidated = false;
            int count = cubeSize == 2 ? 18 : cubeSize == 3 ? 25 : cubeSize == 4 ? 35 : 45;
            List<CubeMove> moves = model.CreateScramble(count);
            model.Apply(moves);
            scramble = string.Join(" ", moves.Select(m => m.Notation));
            hasScramble = true;
            cubeRenderer.SnapAll();
            if (mode == GameMode.Speedcubing)
            {
                inspection.Start();
                inspectionOffsetSeconds = 0;
                isInspecting = true;
            }
            RefreshUI(mode == GameMode.Speedcubing ? "15-second inspection · your first turn starts the solve." : "Scrambled · your solve begins with the first turn.");
            Haptic();
        }

        private void RestartScramble()
        {
            if (turnInProgress || !hasScramble) { Scramble(); return; }
            model.Reset();
            if (model.ApplyAlgorithm(scramble, out string error))
            {
                moveHistory.Clear();
                redoStack.Clear();
                timer.Reset();
                inspection.Reset();
                elapsedOffsetSeconds = 0;
                inspectionOffsetSeconds = 0;
                isInspecting = mode == GameMode.Speedcubing;
                attemptFinished = false;
                attemptInvalidated = false;
                if (isInspecting) inspection.Start();
                cubeRenderer.SnapAll();
                RefreshUI("Scramble restored · start a fresh attempt.");
            }
            else RefreshUI(error);
        }

        private void Move(CubeMove move)
        {
            if (turnInProgress || model == null || !hasScramble || attemptFinished) return;
            if (isInspecting)
            {
                isInspecting = false;
                inspection.Stop();
                timer.Start();
            }
            else if (mode != GameMode.Practice && !timer.IsRunning) timer.Start();
            moveHistory.Add(move);
            redoStack.Clear();
            turnInProgress = true;
            cubeRenderer.Turn(move, () =>
            {
                turnInProgress = false;
                PlayMoveFeedback();
                if (model.IsSolved()) CompleteSolve();
                else RefreshUI(null);
            });
        }

        private void Undo()
        {
            if (turnInProgress || moveHistory.Count == 0) return;
            if (timer.IsRunning) timer.Stop();
            isInspecting = false;
            attemptInvalidated = true;
            CubeMove move = moveHistory[moveHistory.Count - 1];
            moveHistory.RemoveAt(moveHistory.Count - 1);
            redoStack.Push(move);
            turnInProgress = true;
            cubeRenderer.Turn(move.Inverse(), () => { turnInProgress = false; RefreshUI("Move undone."); });
        }

        private void Redo()
        {
            if (turnInProgress || redoStack.Count == 0) return;
            CubeMove move = redoStack.Pop();
            moveHistory.Add(move);
            turnInProgress = true;
            cubeRenderer.Turn(move, () => { turnInProgress = false; RefreshUI("Move restored."); });
        }

        private void CompleteSolve()
        {
            bool eligible = hasScramble && !attemptInvalidated && mode != GameMode.Practice && elapsedOffsetSeconds + timer.Elapsed.TotalSeconds > 0;
            timer.Stop();
            isInspecting = false;
            attemptFinished = true;
            if (eligible)
            {
                float seconds = (float)(elapsedOffsetSeconds + timer.Elapsed.TotalSeconds);
                stats.Record(seconds);
                RefreshUI($"Solved · {FormatTime(seconds)} · saved to this device.");
                PlayMoveFeedback(true);
            }
            else RefreshUI("Solved. Start a new scramble when you're ready.");
        }

        private void ShowStatistics()
        {
            string best = stats.bestSingle < 0 ? "—" : FormatTime(stats.bestSingle);
            string ao5 = stats.bestAo5 < 0 ? "Need five solves" : FormatTime(stats.bestAo5);
            RefreshUI($"LOCAL RECORDS  ·  {stats.totalSolves} solves  ·  best {best}  ·  best Ao5 {ao5}");
        }

        private void RefreshUI(string message)
        {
            if (ui == null) return;
            ui.Refresh(mode, cubeSize, moveHistory.Count, scramble, message,
                moveHistory.Count > 0, redoStack.Count > 0, soundEnabled, hapticsEnabled, stats.totalSolves);
            ui.SetClock(isInspecting ? Mathf.Max(0f, 15f - (float)(inspectionOffsetSeconds + inspection.Elapsed.TotalSeconds)) : elapsedOffsetSeconds + timer.Elapsed.TotalSeconds,
                mode == GameMode.Speedcubing && isInspecting);
        }

        private void ReadPointer()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) PointerBegin(touch.position, touch.fingerId);
                else if (touch.fingerId == activeFinger && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)) PointerMove(touch.position);
                else if (touch.fingerId == activeFinger && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)) PointerEnd(touch.position);
                return;
            }
            if (Input.GetMouseButtonDown(0)) PointerBegin(Input.mousePosition, -1);
            if (pointerDown && Input.GetMouseButton(0)) PointerMove(Input.mousePosition);
            if (pointerDown && Input.GetMouseButtonUp(0)) PointerEnd(Input.mousePosition);
        }

        private void PointerBegin(Vector2 position, int finger)
        {
            if (turnInProgress) return;
            activeFinger = finger;
            pointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(finger);
            if (pointerOverUI) return;
            pointerDown = true;
            pressPoint = previousPoint = position;
            beganOnCube = cubeRenderer.TryPick(gameCamera.ScreenPointToRay(position), out pickedHit);
            draggingView = false;
        }

        private void PointerMove(Vector2 position)
        {
            if (!pointerDown || pointerOverUI) return;
            Vector2 delta = position - previousPoint;
            if (!beganOnCube)
            {
                if (delta.sqrMagnitude > 1f) draggingView = true;
                if (draggingView && cubeRenderer.Root != null)
                {
                    cubeRenderer.Root.Rotate(Vector3.up, delta.x * backgroundMotionSensitivity, Space.World);
                    cubeRenderer.Root.Rotate(gameCamera.transform.right, -delta.y * backgroundMotionSensitivity, Space.World);
                }
            }
            previousPoint = position;
        }

        private void PointerEnd(Vector2 position)
        {
            if (!pointerDown) { activeFinger = int.MinValue; return; }
            pointerDown = false;
            activeFinger = int.MinValue;
            if (pointerOverUI) return;
            Vector2 swipe = position - pressPoint;
            if (beganOnCube && swipe.sqrMagnitude > 144f && hasScramble)
            {
                Vector3 screenDrag = gameCamera.transform.right * swipe.x + gameCamera.transform.up * swipe.y;
                Vector3 localDrag = cubeRenderer.Root.InverseTransformDirection(screenDrag).normalized;
                Int3 normalInt = cubeRenderer.FaceNormal(pickedHit.normal);
                Vector3 normal = new Vector3(normalInt.x, normalInt.y, normalInt.z);
                Vector3 axisVector = Vector3.Cross(normal, localDrag);
                CubeAxis axis = Mathf.Abs(axisVector.x) > Mathf.Abs(axisVector.y)
                    ? (Mathf.Abs(axisVector.x) > Mathf.Abs(axisVector.z) ? CubeAxis.X : CubeAxis.Z)
                    : (Mathf.Abs(axisVector.y) > Mathf.Abs(axisVector.z) ? CubeAxis.Y : CubeAxis.Z);
                float axisAmount = axis == CubeAxis.X ? axisVector.x : axis == CubeAxis.Y ? axisVector.y : axisVector.z;
                if (Mathf.Abs(axisAmount) > 0.2f)
                {
                    int layer = cubeRenderer.LayerAt(pickedHit.point, axis);
                    Move(new CubeMove(axis, layer, layer, axisAmount > 0 ? 1 : -1,
                        $"{axis}{layer}{(axisAmount > 0 ? "+" : "-")}"));
                }
            }
            pointerOverUI = false;
        }

        private void ToggleSound()
        {
            soundEnabled = !soundEnabled;
            PlayerPrefs.SetInt("CubeVerse.Sound", soundEnabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshUI(soundEnabled ? "Move feedback enabled." : "Move feedback muted.");
        }

        private void ToggleHaptics()
        {
            hapticsEnabled = !hapticsEnabled;
            PlayerPrefs.SetInt("CubeVerse.Haptics", hapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            RefreshUI(hapticsEnabled ? "Haptic feedback enabled." : "Haptic feedback off.");
            Haptic();
        }

        private void PlayMoveFeedback(bool solved = false)
        {
            if (soundEnabled && audioSource != null && moveTone != null)
            {
                audioSource.pitch = solved ? 1.5f : 1f;
                audioSource.PlayOneShot(moveTone, solved ? 0.22f : 0.1f);
            }
            Haptic();
        }

        private void Haptic()
        {
            if (hapticsEnabled && Application.isMobilePlatform) Handheld.Vibrate();
        }

        private static AudioClip CreateMoveTone()
        {
            const int rate = 22050, count = 1200;
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float fade = 1f - (float)i / count;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 520f * i / rate) * fade * fade * 0.28f;
            }
            AudioClip clip = AudioClip.Create("CubeVerse · soft turn", count, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                timerWasRunning = timer.IsRunning;
                elapsedOffsetSeconds += timer.Elapsed.TotalSeconds;
                timer.Stop();
                inspectionWasRunning = inspection.IsRunning;
                inspectionOffsetSeconds += inspection.Elapsed.TotalSeconds;
                inspection.Stop();
                pausedForBackground = true;
            }
            else if (pausedForBackground)
            {
                if (timerWasRunning)
                {
                    timer.Reset();
                    timer.Start();
                }
                if (inspectionWasRunning) { inspection.Reset(); inspection.Start(); }
                pausedForBackground = false;
            }
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalMinutes >= 1 ? $"{(int)t.TotalMinutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}" : $"{t.Seconds:00}.{t.Milliseconds / 10:00}";
        }

        private void OnDestroy()
        {
            if (moveTone != null) Destroy(moveTone);
        }
    }
}
