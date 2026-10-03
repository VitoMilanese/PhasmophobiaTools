using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhasmophobiaTools;

public sealed class DebugOverlayBehaviour : MonoBehaviour
{
    private const int WindowId = 0x42575054;
    private const int MaxSceneObjects = 5000;

    private static readonly List<SceneObjectEntry> SceneObjects = new();

    private static bool _isVisible;
    private static bool _cursorStateCaptured;
    private static bool _wasTruncated;
    private static CursorLockMode _previousCursorLockMode;
    private static bool _previousCursorVisible;
    private static Rect _windowRect = new(20f, 20f, 720f, 620f);
    private static Vector2 _scrollPosition;
    private static string _filter = string.Empty;
    private static string _sceneName = "<not loaded>";

    public DebugOverlayBehaviour(IntPtr pointer)
        : base(pointer)
    {
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Toggle();
        }

        if (!_isVisible)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        var activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeSceneName, _sceneName, StringComparison.Ordinal))
        {
            RefreshSceneObjects();
        }
    }

    public void OnGUI()
    {
        if (!_isVisible)
        {
            return;
        }

        _windowRect = GUILayout.Window(
            WindowId,
            _windowRect,
            (GUI.WindowFunction)DrawWindow,
            "Phasmophobia Tools");
    }

    private static void DrawWindow(int windowId)
    {
        GUILayout.Label($"Unity: {Application.unityVersion}");
        GUILayout.Label($"Scene: {_sceneName}");

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
        {
            RefreshSceneObjects();
        }

        GUILayout.Label($"Objects: {SceneObjects.Count}", GUILayout.Width(120f));

        if (_wasTruncated)
        {
            GUILayout.Label($"Showing first {MaxSceneObjects} objects.");
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Close", GUILayout.Width(90f)))
        {
            Hide();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Filter:", GUILayout.Width(45f));
        _filter = GUILayout.TextField(_filter);

        if (GUILayout.Button("Clear", GUILayout.Width(60f)))
        {
            _filter = string.Empty;
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(6f);

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        foreach (var entry in SceneObjects)
        {
            if (!MatchesFilter(entry.Name))
            {
                continue;
            }

            var indentation = new string(' ', Math.Min(entry.Depth, 30) * 2);
            var state = entry.IsActive ? "[+]" : "[-]";
            GUILayout.Label($"{indentation}{state} {entry.Name}");
        }

        GUILayout.EndScrollView();

        GUI.DragWindow(new Rect(0f, 0f, _windowRect.width, 24f));
    }

    private static bool MatchesFilter(string objectName)
    {
        return string.IsNullOrWhiteSpace(_filter)
            || objectName.Contains(_filter.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static void Toggle()
    {
        if (_isVisible)
        {
            Hide();
            return;
        }

        Show();
    }

    private static void Show()
    {
        if (!_cursorStateCaptured)
        {
            _previousCursorLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _cursorStateCaptured = true;
        }

        _isVisible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        RefreshSceneObjects();
        Plugin.Logger.LogInfo("Debug overlay opened.");
    }

    private static void Hide()
    {
        _isVisible = false;

        if (_cursorStateCaptured)
        {
            Cursor.lockState = _previousCursorLockMode;
            Cursor.visible = _previousCursorVisible;
            _cursorStateCaptured = false;
        }

        Plugin.Logger.LogInfo("Debug overlay closed.");
    }

    private static void RefreshSceneObjects()
    {
        SceneObjects.Clear();
        _wasTruncated = false;

        var scene = SceneManager.GetActiveScene();
        _sceneName = scene.IsValid() ? scene.name : "<invalid scene>";

        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        foreach (var rootObject in scene.GetRootGameObjects())
        {
            AddHierarchy(rootObject.transform, 0);

            if (_wasTruncated)
            {
                break;
            }
        }

        Plugin.Logger.LogInfo(
            $"Scene inspector refreshed for '{_sceneName}'. Objects: {SceneObjects.Count}.");
    }

    private static void AddHierarchy(Transform transform, int depth)
    {
        if (SceneObjects.Count >= MaxSceneObjects)
        {
            _wasTruncated = true;
            return;
        }

        var gameObject = transform.gameObject;
        SceneObjects.Add(
            new SceneObjectEntry(
                gameObject.name ?? "<unnamed>",
                depth,
                gameObject.activeInHierarchy));

        for (var index = 0; index < transform.childCount; index++)
        {
            AddHierarchy(transform.GetChild(index), depth + 1);

            if (_wasTruncated)
            {
                return;
            }
        }
    }

    private readonly struct SceneObjectEntry
    {
        public SceneObjectEntry(string name, int depth, bool isActive)
        {
            Name = name;
            Depth = depth;
            IsActive = isActive;
        }

        public string Name { get; }

        public int Depth { get; }

        public bool IsActive { get; }
    }
}
