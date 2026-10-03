using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PhasmophobiaTools;

public sealed class DebugOverlayBehaviour : MonoBehaviour
{
    private const int MaxSceneObjects = 5000;

    private static readonly List<SceneObjectEntry> SceneObjects = new();

    private static bool _isVisible;
    private static bool _cursorStateCaptured;
    private static bool _wasTruncated;
    private static bool _imguiErrorLogged;
    private static CursorLockMode _previousCursorLockMode;
    private static bool _previousCursorVisible;
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

        if (!ImguiBridge.EnsureInitialized())
        {
            if (!_imguiErrorLogged)
            {
                _imguiErrorLogged = true;
                Plugin.Logger.LogError(
                    $"Unable to initialize Unity IMGUI: {ImguiBridge.InitializationError}");
            }

            return;
        }

        try
        {
            DrawOverlay();
        }
        catch (Exception exception)
        {
            if (!_imguiErrorLogged)
            {
                _imguiErrorLogged = true;
                Plugin.Logger.LogError($"Unable to draw debug overlay: {exception}");
            }
        }
    }

    private static void DrawOverlay()
    {
        var outerRect = new Rect(20f, 20f, 720f, 620f);
        var contentRect = new Rect(32f, 48f, 696f, 580f);

        ImguiBridge.Box(outerRect, "Phasmophobia Tools");
        ImguiBridge.BeginArea(contentRect);

        ImguiBridge.Label($"Unity: {Application.unityVersion}");
        ImguiBridge.Label($"Scene: {_sceneName}");

        ImguiBridge.BeginHorizontal();

        if (ImguiBridge.Button("Refresh"))
        {
            RefreshSceneObjects();
        }

        ImguiBridge.Label($"Objects: {SceneObjects.Count}");

        if (_wasTruncated)
        {
            ImguiBridge.Label($"Showing first {MaxSceneObjects} objects.");
        }

        ImguiBridge.FlexibleSpace();

        if (ImguiBridge.Button("Close"))
        {
            Hide();
        }

        ImguiBridge.EndHorizontal();

        ImguiBridge.Space(6f);

        ImguiBridge.BeginHorizontal();
        ImguiBridge.Label("Filter:");
        _filter = ImguiBridge.TextField(_filter);

        if (ImguiBridge.Button("Clear"))
        {
            _filter = string.Empty;
        }

        ImguiBridge.EndHorizontal();

        ImguiBridge.Space(6f);

        _scrollPosition = ImguiBridge.BeginScrollView(_scrollPosition);

        foreach (var entry in SceneObjects)
        {
            if (!MatchesFilter(entry.Name))
            {
                continue;
            }

            var indentation = new string(' ', Math.Min(entry.Depth, 30) * 2);
            var state = entry.IsActive ? "[+]" : "[-]";
            ImguiBridge.Label($"{indentation}{state} {entry.Name}");
        }

        ImguiBridge.EndScrollView();
        ImguiBridge.EndArea();
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
