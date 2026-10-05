using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum LevelEditorMode
{
    View,
    Edit,
    CreateNew
}

public enum CreateTool
{
    Paint,
    Cat
}

public class LevelEditorWindow : EditorWindow
{
    const float SidebarWidth = 220f;
    const float MinCellSize = 18f;
    const float MaxCellSize = 44f;
    const int MinBoardSize = 4;
    const int MaxBoardSize = 12;

    LevelEditorMode _mode = LevelEditorMode.View;
    List<string> _allLevels = new();
    List<string> _filteredLevels = new();
    string _searchText = "";
    int _selectedIndex = -1;
    LevelData _currentLevel;
    string _currentLevelName;
    string _loadError;

    Vector2 _listScroll;
    Vector2 _hintScroll;

    int _hintStepIndex = -1;
    bool _showHintPanel;
    bool _showHintHighlight;
    bool _showColorIndex = true;
    float _cellSize = 32f;
    bool _viewLevelDirty;

    Texture2D _catIconTexture;
    const string CatIconPath = "Assets/Resources/Images/GameView/IconCat.png";

    int _createBoardSize = 5;
    int _createPaintColor = 1;
    CreateTool _createTool = CreateTool.Paint;
    int[][] _createColorMap;
    string[][] _createSolution;
    bool[][] _createCatRevealed;
    List<string> _createValidationErrors = new();
    bool _createIsValid;
    string _createLevelName;
    Vector2 _createValidationScroll;
    bool _showCatSuggestions;
    HashSet<long> _catSuggestions = new();

    HintStep[] _editSourceHintPlan;
    bool _draftDirty;

    string _pendingLevelName;
    PendingSidebarAction _pendingSidebarAction;

    enum PendingSidebarAction
    {
        None,
        ViewLoad,
        DraftLoad
    }

    [MenuItem("Meowdoku/Level Editor")]
    public static void Open()
    {
        var window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(720f, 480f);
        window.Show();
    }

    void OnEnable()
    {
        RefreshLevelList();
        RefreshCreateLevelName();
        InitializeCreateBoard();
        _catIconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CatIconPath);
        LevelColorConfigUtility.EnsureConfigAssetExists();
        LevelPalette.Reload();
    }

    void OnDestroy()
    {
        EditorApplication.delayCall -= ProcessPendingSidebarAction;
    }

    void RefreshLevelList()
    {
        _allLevels = LevelLoader.GetAllLevelFileNames();
        ApplyFilter();
    }

    void ApplyFilter()
    {
        _filteredLevels.Clear();
        var query = _searchText.Trim();
        foreach (var name in _allLevels)
        {
            if (string.IsNullOrEmpty(query) || name.Contains(query, System.StringComparison.OrdinalIgnoreCase))
                _filteredLevels.Add(name);
        }

        if (_selectedIndex >= _filteredLevels.Count)
            _selectedIndex = _filteredLevels.Count - 1;

        if (_mode != LevelEditorMode.View)
            return;

        if (_selectedIndex >= 0 && _selectedIndex < _filteredLevels.Count)
            QueueViewLevelLoad(_filteredLevels[_selectedIndex]);
        else
            ClearLevel();
    }

    void ClearLevel()
    {
        _currentLevel = null;
        _currentLevelName = null;
        _loadError = null;
        _hintStepIndex = -1;
    }

    void LoadLevel(string levelName)
    {
        _currentLevelName = levelName;
        _loadError = null;
        _hintStepIndex = -1;

        _currentLevel = LevelLoader.LoadLevelByName(levelName);
        _viewLevelDirty = false;
        if (_currentLevel == null || _currentLevel.Size == 0)
            _loadError = $"Could not load level: {levelName}";
    }

    void OnGUI()
    {
        DrawToolbar();

        EditorGUILayout.BeginHorizontal();
        DrawSidebar();
        DrawMainPanel();
        EditorGUILayout.EndHorizontal();
    }

    void QueueViewLevelLoad(string levelName)
    {
        _pendingLevelName = levelName;
        _pendingSidebarAction = PendingSidebarAction.ViewLoad;
        SchedulePendingSidebarAction();
    }

    void QueueDraftLevelLoad(string levelName)
    {
        _pendingLevelName = levelName;
        _pendingSidebarAction = PendingSidebarAction.DraftLoad;
        SchedulePendingSidebarAction();
    }

    void SchedulePendingSidebarAction()
    {
        EditorApplication.delayCall -= ProcessPendingSidebarAction;
        EditorApplication.delayCall += ProcessPendingSidebarAction;
    }

    void ProcessPendingSidebarAction()
    {
        if (_pendingSidebarAction == PendingSidebarAction.None)
            return;

        var levelName = _pendingLevelName;
        var action = _pendingSidebarAction;
        _pendingLevelName = null;
        _pendingSidebarAction = PendingSidebarAction.None;

        switch (action)
        {
            case PendingSidebarAction.ViewLoad:
            {
                var index = _filteredLevels.IndexOf(levelName);
                if (index >= 0)
                    _selectedIndex = index;
                LoadLevel(levelName);
                break;
            }
            case PendingSidebarAction.DraftLoad:
                if (_draftDirty && !ConfirmDiscardDraftChanges())
                    return;
                LoadLevelIntoDraft(levelName);
                break;
        }

        Repaint();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        var newMode = (LevelEditorMode)EditorGUILayout.EnumPopup(_mode, EditorStyles.toolbarDropDown, GUILayout.Width(100f));
        if (EditorGUI.EndChangeCheck() && TrySwitchToMode(newMode))
            _mode = newMode;

        GUILayout.Space(8f);
        var modeLabel = _mode switch
        {
            LevelEditorMode.View => "Browse levels in Resources/Levels",
            LevelEditorMode.Edit => $"Editing: {_createLevelName}",
            _ => $"Creating: {_createLevelName}"
        };
        EditorGUILayout.LabelField(modeLabel, EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            RefreshLevelList();

        EditorGUILayout.EndHorizontal();
    }

    bool TrySwitchToMode(LevelEditorMode newMode)
    {
        if (newMode == _mode) return false;

        if (_mode == LevelEditorMode.View && _viewLevelDirty)
        {
            if (!ConfirmDiscardViewChanges())
                return false;
            _viewLevelDirty = false;
            if (!string.IsNullOrEmpty(_currentLevelName))
                LoadLevel(_currentLevelName);
        }

        if (IsDraftEditingMode() && _draftDirty && newMode != _mode)
        {
            if (!ConfirmDiscardDraftChanges())
                return false;
        }

        ApplyModeChange(newMode);
        return true;
    }

    void ApplyModeChange(LevelEditorMode newMode)
    {
        if (newMode == LevelEditorMode.CreateNew)
        {
            _createTool = CreateTool.Paint;
            RefreshCreateLevelName();
            InitializeCreateBoard();
            _createValidationErrors.Clear();
            _editSourceHintPlan = null;
            return;
        }

        if (newMode == LevelEditorMode.Edit)
        {
            _createTool = CreateTool.Paint;
            _createValidationErrors.Clear();

            if (!string.IsNullOrEmpty(_currentLevelName))
                LoadLevelIntoDraft(_currentLevelName);
            else if (_selectedIndex >= 0 && _selectedIndex < _filteredLevels.Count)
                LoadLevelIntoDraft(_filteredLevels[_selectedIndex]);
            return;
        }

        if (newMode == LevelEditorMode.View &&
            _selectedIndex >= 0 && _selectedIndex < _filteredLevels.Count)
        {
            LoadLevel(_filteredLevels[_selectedIndex]);
        }
    }

    bool ConfirmDiscardDraftChanges()
    {
        if (!_draftDirty) return true;
        return EditorUtility.DisplayDialog(
            "Unsaved changes",
            "Discard unsaved draft changes?",
            "Discard",
            "Stay");
    }

    bool ConfirmDiscardViewChanges()
    {
        if (!_viewLevelDirty) return true;
        return EditorUtility.DisplayDialog(
            "Unsaved changes",
            "Discard cat toggle changes in View mode?",
            "Discard",
            "Stay");
    }

    void RefreshCreateLevelName()
    {
        _createLevelName = $"level_{LevelLoader.GetNextLevelNumber()}";
    }

    void InitializeCreateBoard()
    {
        _createTool = CreateTool.Paint;
        _createColorMap = new int[_createBoardSize][];
        _createSolution = new string[_createBoardSize][];
        _createCatRevealed = new bool[_createBoardSize][];
        for (var row = 0; row < _createBoardSize; row++)
        {
            _createColorMap[row] = Enumerable.Repeat(1, _createBoardSize).ToArray();
            _createSolution[row] = Enumerable.Repeat(".", _createBoardSize).ToArray();
            _createCatRevealed[row] = new bool[_createBoardSize];
        }

        _createPaintColor = Mathf.Clamp(_createPaintColor, 1, _createBoardSize);
        _createValidationErrors.Clear();
        _createIsValid = false;
        _editSourceHintPlan = null;
        _draftDirty = false;
        HideCatSuggestions();
    }

    void LoadLevelIntoDraft(string levelName)
    {
        var level = LevelLoader.LoadLevelByName(levelName);
        if (level == null || level.RowCount == 0)
        {
            EditorUtility.DisplayDialog("Load failed", $"Could not load level: {levelName}", "OK");
            return;
        }

        EnsureCatRevealed(level);

        _createLevelName = levelName;
        _createBoardSize = level.RowCount;
        _createColorMap = CloneIntMatrix(level.colorMap);
        _createSolution = CloneStringMatrix(level.solution);
        _createCatRevealed = CloneBoolMatrix(level.catRevealed, level.RowCount, level.ColCount);
        _editSourceHintPlan = level.hintPlan;
        _createPaintColor = Mathf.Clamp(1, 1, _createBoardSize);
        _createValidationErrors.Clear();
        _createIsValid = false;
        _draftDirty = false;
        HideCatSuggestions();
        _currentLevelName = levelName;
        _selectedIndex = _filteredLevels.IndexOf(levelName);
    }

    bool IsDraftEditingMode() => _mode == LevelEditorMode.CreateNew || _mode == LevelEditorMode.Edit;

    LevelData BuildCreateLevelData()
    {
        return new LevelData
        {
            colorMap = _createColorMap,
            solution = _createSolution,
            catRevealed = _createCatRevealed,
            hintPlan = new HintStep[0]
        };
    }

    LevelData BuildCreateLevelDataSnapshot()
    {
        return new LevelData
        {
            colorMap = CloneIntMatrix(_createColorMap),
            solution = CloneStringMatrix(_createSolution),
            catRevealed = CloneBoolMatrix(_createCatRevealed, _createBoardSize, _createBoardSize),
            hintPlan = new HintStep[0]
        };
    }

    void DrawSidebar()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(SidebarWidth));

        if (IsDraftEditingMode())
        {
            DrawDraftSidebar();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.LabelField("Levels", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _searchText = EditorGUILayout.TextField(_searchText, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarSearchField);
        if (EditorGUI.EndChangeCheck())
            ApplyFilter();

        EditorGUILayout.LabelField($"{_filteredLevels.Count} / {_allLevels.Count} levels", EditorStyles.miniLabel);
        DrawLevelList(draftMode: false);

        EditorGUILayout.EndVertical();
    }

    void DrawLevelList(bool draftMode)
    {
        const float itemHeight = 20f;
        var viewHeight = draftMode
            ? Mathf.Max(100f, position.height - 280f)
            : Mathf.Max(120f, position.height - 120f);

        _listScroll = EditorGUILayout.BeginScrollView(
            _listScroll,
            false,
            true,
            GUILayout.Height(viewHeight));

        for (var i = 0; i < _filteredLevels.Count; i++)
        {
            var name = _filteredLevels[i];
            var isSelected = draftMode
                ? name == _createLevelName
                : i == _selectedIndex;
            var style = isSelected ? EditorStyles.toolbarButton : EditorStyles.miniButton;

            if (GUILayout.Button(name, style, GUILayout.Height(itemHeight)))
            {
                if (draftMode)
                    QueueDraftLevelLoad(name);
                else
                    QueueViewLevelLoad(name);
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawCreateSidebar()
    {
        EditorGUILayout.LabelField("Create New Level", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(_createLevelName, EditorStyles.largeLabel);
        EditorGUILayout.LabelField($"After {LevelLoader.GetAllLevelFileNames().Count} existing levels", EditorStyles.miniLabel);
        DrawDraftRules();
    }

    void DrawDraftSidebar()
    {
        if (_mode == LevelEditorMode.Edit)
        {
            EditorGUILayout.LabelField("Edit Level", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_createLevelName, EditorStyles.largeLabel);
            if (_draftDirty)
                EditorGUILayout.LabelField("● Unsaved", EditorStyles.miniLabel);

            var hintCount = _editSourceHintPlan?.Length ?? 0;
            EditorGUILayout.LabelField($"Hints preserved: {hintCount}", EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);
            DrawDraftLevelList();
            EditorGUILayout.Space(6f);
            DrawDraftRules();
            return;
        }

        DrawCreateSidebar();
    }

    void DrawDraftRules()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Save rules:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Exactly 1 cat per color", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Cats cannot share a row or column", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Cats cannot be adjacent", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"• {_createBoardSize} distinct colors on a {_createBoardSize}x{_createBoardSize} board", EditorStyles.miniLabel);
    }

    void DrawDraftLevelList()
    {
        EditorGUILayout.LabelField("Select level", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _searchText = EditorGUILayout.TextField(_searchText, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarSearchField);
        if (EditorGUI.EndChangeCheck())
            ApplyFilter();

        DrawLevelList(draftMode: true);
    }

    void DrawMainPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));

        if (IsDraftEditingMode())
            DrawCreateMainPanelContent();
        else
            DrawViewMainPanelContent();

        EditorGUILayout.EndVertical();
    }

    void DrawViewMainPanelContent()
    {
        if (_currentLevel == null)
        {
            EditorGUILayout.HelpBox(
                string.IsNullOrEmpty(_loadError)
                    ? "Select a level from the list on the left."
                    : _loadError,
                string.IsNullOrEmpty(_loadError) ? MessageType.Info : MessageType.Error);
            return;
        }

        DrawLevelHeader();
        DrawViewOptions();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Board (colorMap)", EditorStyles.boldLabel);
        DrawGrid();
        DrawColorLegend();

        if (_showHintPanel)
        {
            EditorGUILayout.Space(8f);
            DrawHintPanel();
        }
    }

    void DrawLevelHeader()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(_currentLevelName, EditorStyles.largeLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Open JSON", GUILayout.Width(90f)))
        {
            var path = Path.Combine(LevelLoader.LevelsFolderPath, _currentLevelName + ".json");
            EditorUtility.RevealInFinder(path);
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(path));
        }

        if (GUILayout.Button("Edit", GUILayout.Width(50f)))
        {
            if (TrySwitchToMode(LevelEditorMode.Edit))
                _mode = LevelEditorMode.Edit;
        }

        if (_viewLevelDirty && GUILayout.Button("Save", GUILayout.Width(60f)))
            SaveViewLevel();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Size: {_currentLevel.RowCount}x{_currentLevel.ColCount}  |  " +
            $"Colors: {_currentLevel.ColorCount}  |  " +
            $"Queens: {_currentLevel.QueenCount}  |  " +
            $"Revealed cats: {_currentLevel.RevealedCatCount}  |  " +
            $"Hints: {_currentLevel.hintPlan?.Length ?? 0}",
            EditorStyles.miniLabel);
    }

    void SaveViewLevel()
    {
        if (_currentLevel == null || string.IsNullOrEmpty(_currentLevelName)) return;
        LevelLoader.PrepareLevelForSave(_currentLevel);
        if (!LevelLoader.SaveLevel(_currentLevelName, _currentLevel))
        {
            EditorUtility.DisplayDialog("Save failed", "Could not write level file.", "OK");
            return;
        }

        ReimportLevelAsset(_currentLevelName);
        _viewLevelDirty = false;
        RefreshLevelList();
    }

    static void ReimportLevelAsset(string levelName)
    {
        AssetDatabase.ImportAsset(LevelLoader.GetLevelFilePath(levelName), ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
    }

    void DrawViewOptions()
    {
        EditorGUILayout.BeginHorizontal();
        _showColorIndex = EditorGUILayout.ToggleLeft("Color index", _showColorIndex, GUILayout.Width(95f));
        _showHintPanel = EditorGUILayout.ToggleLeft("Hint Plan", _showHintPanel, GUILayout.Width(85f));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Cell size", GUILayout.Width(55f));
        _cellSize = EditorGUILayout.Slider(_cellSize, MinCellSize, MaxCellSize, GUILayout.Width(160f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            "Click a Q cell to toggle cat revealed (Cat icon ↔ Q label).",
            EditorStyles.miniLabel);

        if (_showHintPanel)
        {
            EditorGUILayout.BeginHorizontal();
            _showHintHighlight = EditorGUILayout.ToggleLeft("Highlight hint cells", _showHintHighlight, GUILayout.Width(140f));
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            _showHintHighlight = false;
            _hintStepIndex = -1;
        }
    }

    void DrawGrid()
    {
        var rowCount = _currentLevel.RowCount;
        var colCount = _currentLevel.ColCount;
        if (rowCount == 0 || colCount == 0) return;

        var gridWidth = colCount * _cellSize;
        var gridHeight = rowCount * _cellSize;
        var rect = GUILayoutUtility.GetRect(gridWidth + 16f, gridHeight + 16f, GUILayout.ExpandWidth(false));
        var gridRect = new Rect(rect.x + 8f, rect.y + 8f, gridWidth, gridHeight);
        var e = Event.current;
        var highlightCells = GetActiveHintCells();

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var cellRect = new Rect(
                    gridRect.x + col * _cellSize,
                    gridRect.y + row * _cellSize,
                    _cellSize - 1f,
                    _cellSize - 1f);

                var colorId = _currentLevel.GetColorAt(row, col);
                var cellColor = LevelPalette.GetColor(colorId);
                var hasQueen = _currentLevel.HasQueenAt(row, col);
                var catRevealed = _currentLevel.IsCatRevealedAt(row, col);

                if (e.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(cellRect, cellColor);

                    if (_showHintHighlight && highlightCells.Contains(CellKey(row, col)))
                    {
                        var hint = _currentLevel.hintPlan[_hintStepIndex];
                        EditorGUI.DrawRect(cellRect, LevelPalette.GetHintHighlightColor(hint.action));
                    }

                    DrawRegionBorder(cellRect, row, col, rowCount, colCount);
                    DrawCellCatOrLabel(cellRect, colorId, cellColor, hasQueen, catRevealed);
                }

                if (e.type == EventType.MouseDown && e.button == 0 && hasQueen && cellRect.Contains(e.mousePosition))
                {
                    ToggleCatRevealed(_currentLevel, row, col);
                    _viewLevelDirty = true;
                    e.Use();
                    Repaint();
                }
            }
        }

        if (e.type == EventType.Repaint)
        {
            Handles.color = new Color(0f, 0f, 0f, 0.6f);
            Handles.DrawLine(new Vector3(gridRect.x, gridRect.yMax), new Vector3(gridRect.xMax, gridRect.yMax));
            Handles.DrawLine(new Vector3(gridRect.xMax, gridRect.y), new Vector3(gridRect.xMax, gridRect.yMax));
        }
    }

    void DrawRegionBorder(Rect cellRect, int row, int col, int rowCount, int colCount)
    {
        var colorId = _currentLevel.GetColorAt(row, col);
        var borderColor = new Color(0f, 0f, 0f, 0.45f);

        if (row == 0 || _currentLevel.GetColorAt(row - 1, col) != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.xMax, cellRect.y));
        }

        if (col == 0 || _currentLevel.GetColorAt(row, col - 1) != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x, cellRect.yMax));
        }

        if (row == rowCount - 1 || _currentLevel.GetColorAt(row + 1, col) != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.yMax), new Vector3(cellRect.xMax, cellRect.yMax));
        }

        if (col == colCount - 1 || _currentLevel.GetColorAt(row, col + 1) != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.xMax, cellRect.y), new Vector3(cellRect.xMax, cellRect.yMax));
        }
    }

    void DrawCellCatOrLabel(Rect cellRect, int colorId, Color cellColor, bool hasQueen, bool catRevealed)
    {
        if (hasQueen && catRevealed)
        {
            DrawCatIcon(cellRect);
            return;
        }

        if (hasQueen)
        {
            DrawQueenQLabel(cellRect, cellColor);
            return;
        }

        if (_showColorIndex)
            DrawColorIndexLabel(cellRect, colorId, cellColor);
    }

    void EnsureCatRevealed(LevelData level)
    {
        if (level?.colorMap == null) return;
        if (level.catRevealed != null && level.catRevealed.Length == level.RowCount) return;

        level.catRevealed = new bool[level.RowCount][];
        for (var row = 0; row < level.RowCount; row++)
            level.catRevealed[row] = new bool[level.ColCount];
    }

    void ToggleCatRevealed(LevelData level, int row, int col)
    {
        if (!level.HasQueenAt(row, col)) return;
        EnsureCatRevealed(level);
        level.catRevealed[row][col] = !level.catRevealed[row][col];
    }

    void DrawCatIcon(Rect cellRect)
    {
        if (_catIconTexture == null)
            return;

        var padding = cellRect.width * 0.1f;
        var iconRect = new Rect(
            cellRect.x + padding,
            cellRect.y + padding,
            cellRect.width - padding * 2f,
            cellRect.height - padding * 2f);
        GUI.DrawTexture(iconRect, _catIconTexture, ScaleMode.ScaleToFit, true);
    }

    static void DrawQueenQLabel(Rect cellRect, Color cellColor)
    {
        var fontSize = Mathf.Clamp(Mathf.RoundToInt(cellRect.width * 0.48f), 10, 20);
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            normal = { textColor = LevelPalette.GetContrastingTextColor(cellColor) }
        };
        GUI.Label(cellRect, "Q", labelStyle);
    }

    static void DrawColorIndexLabel(Rect cellRect, int colorId, Color cellColor)
    {
        var textColor = LevelPalette.GetContrastingTextColor(cellColor);
        var fontSize = Mathf.Clamp(Mathf.RoundToInt(cellRect.width * 0.42f), 9, 18);
        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            normal = { textColor = textColor }
        };
        GUI.Label(cellRect, colorId.ToString(), labelStyle);
    }

    void DrawColorLegend()
    {
        if (_currentLevel?.colorMap == null) return;

        var usedColors = new SortedSet<int>();
        foreach (var row in _currentLevel.colorMap)
        {
            if (row == null) continue;
            foreach (var colorId in row)
                if (colorId > 0) usedColors.Add(colorId);
        }

        if (usedColors.Count == 0) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Legend:", GUILayout.Width(50f));
        foreach (var colorId in usedColors)
        {
            var color = LevelPalette.GetColor(colorId);
            var swatchRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(swatchRect, color);
                Handles.color = new Color(0f, 0f, 0f, 0.5f);
                Handles.DrawLine(new Vector3(swatchRect.x, swatchRect.y), new Vector3(swatchRect.xMax, swatchRect.y));
                Handles.DrawLine(new Vector3(swatchRect.x, swatchRect.y), new Vector3(swatchRect.x, swatchRect.yMax));
                Handles.DrawLine(new Vector3(swatchRect.xMax, swatchRect.y), new Vector3(swatchRect.xMax, swatchRect.yMax));
                Handles.DrawLine(new Vector3(swatchRect.x, swatchRect.yMax), new Vector3(swatchRect.xMax, swatchRect.yMax));
            }

            var textStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = color }
            };
            EditorGUILayout.LabelField(colorId.ToString(), textStyle, GUILayout.Width(22f));
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawHintPanel()
    {
        var hints = _currentLevel.hintPlan;
        if (hints == null || hints.Length == 0)
        {
            EditorGUILayout.HelpBox("This level has no hintPlan.", MessageType.None);
            return;
        }

        EditorGUILayout.LabelField($"Hint Plan ({hints.Length} steps)", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("|<", GUILayout.Width(28f))) _hintStepIndex = -1;
        if (GUILayout.Button("<", GUILayout.Width(28f))) _hintStepIndex = Mathf.Max(-1, _hintStepIndex - 1);
        if (GUILayout.Button(">", GUILayout.Width(28f))) _hintStepIndex = Mathf.Min(hints.Length - 1, _hintStepIndex + 1);
        if (GUILayout.Button(">|", GUILayout.Width(28f))) _hintStepIndex = hints.Length - 1;

        var sliderLabel = _hintStepIndex < 0 ? "Solution only" : $"Step {_hintStepIndex + 1} / {hints.Length}";
        EditorGUILayout.LabelField(sliderLabel, GUILayout.Width(120f));

        EditorGUI.BeginChangeCheck();
        var sliderValue = _hintStepIndex < 0 ? 0 : _hintStepIndex + 1;
        sliderValue = EditorGUILayout.IntSlider(sliderValue, 0, hints.Length, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
            _hintStepIndex = sliderValue == 0 ? -1 : sliderValue - 1;

        EditorGUILayout.EndHorizontal();

        if (_hintStepIndex >= 0 && _hintStepIndex < hints.Length)
        {
            var hint = hints[_hintStepIndex];
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Rule: {hint.rule}   |   Action: {hint.action}   |   Depth: {hint.depth}");
            EditorGUILayout.LabelField(hint.reason, EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField($"Cells: {FormatCells(hint.cells)}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }

        _hintScroll = EditorGUILayout.BeginScrollView(_hintScroll, GUILayout.Height(140f));
        for (var i = 0; i < hints.Length; i++)
        {
            var hint = hints[i];
            var prefix = i == _hintStepIndex ? "▶ " : "  ";
            if (GUILayout.Button($"{prefix}{i + 1}. [{hint.rule}] {hint.action} — {hint.reason}", EditorStyles.label))
                _hintStepIndex = i;
        }
        EditorGUILayout.EndScrollView();
    }

    HashSet<long> GetActiveHintCells()
    {
        var set = new HashSet<long>();
        if (!_showHintHighlight || _currentLevel?.hintPlan == null || _hintStepIndex < 0) return set;

        var hint = _currentLevel.hintPlan[_hintStepIndex];
        if (hint.cells == null) return set;

        foreach (var cell in hint.cells)
        {
            if (cell == null || cell.Length < 2) continue;
            set.Add(CellKey(cell[0], cell[1]));
        }
        return set;
    }

    static long CellKey(int row, int col) => ((long)row << 32) | (uint)col;

    static string FormatCells(int[][] cells)
    {
        if (cells == null || cells.Length == 0) return "-";
        var parts = new List<string>(cells.Length);
        foreach (var c in cells)
        {
            if (c != null && c.Length >= 2)
                parts.Add($"({c[0]},{c[1]})");
        }
        return string.Join(", ", parts);
    }

    void DrawCreateMainPanelContent()
    {
        DrawCreateToolbar();
        DrawCreateCatOptions();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Board (colorMap + cats)", EditorStyles.boldLabel);
        DrawCreateGrid();
        DrawCreateColorLegend();

        EditorGUILayout.Space(8f);
        DrawCreateValidationPanel();
    }

    void DrawCreateToolbar()
    {
        EditorGUILayout.BeginHorizontal();
        if (_mode == LevelEditorMode.CreateNew)
        {
            EditorGUILayout.LabelField("Board size", GUILayout.Width(70f));
            EditorGUI.BeginChangeCheck();
            _createBoardSize = EditorGUILayout.IntSlider(_createBoardSize, MinBoardSize, MaxBoardSize, GUILayout.Width(180f));
            if (EditorGUI.EndChangeCheck())
            {
                RefreshCreateLevelName();
                InitializeCreateBoard();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Board size", GUILayout.Width(70f));
            EditorGUILayout.LabelField($"{_createBoardSize}x{_createBoardSize}", EditorStyles.boldLabel, GUILayout.Width(180f));
        }

        GUILayout.Space(12f);
        EditorGUI.BeginChangeCheck();
        _createTool = (CreateTool)GUILayout.Toolbar((int)_createTool, new[] { "Paint", "Cat (Q)" }, GUILayout.Width(160f));
        if (EditorGUI.EndChangeCheck() && _createTool != CreateTool.Cat)
            HideCatSuggestions();
        EditorGUILayout.EndHorizontal();

        if (_createTool == CreateTool.Cat)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(70f);
            if (GUILayout.Button("Suggest cats", GUILayout.Width(100f)))
                ShowCatSuggestions();

            if (_showCatSuggestions)
                EditorGUILayout.LabelField($"{_catSuggestions.Count} hint cell(s)", EditorStyles.miniLabel);
            else
                EditorGUILayout.LabelField("Hints hidden — click Suggest cats to show", EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Paint color", GUILayout.Width(70f));
        for (var colorId = 1; colorId <= _createBoardSize; colorId++)
        {
            var color = LevelPalette.GetColor(colorId);
            var isSelected = _createPaintColor == colorId;
            var swatchRect = GUILayoutUtility.GetRect(28f, 22f, GUILayout.Width(28f));
            if (GUI.Button(swatchRect, GUIContent.none, GUIStyle.none))
                _createPaintColor = colorId;

            if (Event.current.type == EventType.Repaint)
                DrawPaintColorSwatch(swatchRect, color, colorId, isSelected);
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Clear cats", GUILayout.Width(80f)))
            ClearCreateCats();

        if (GUILayout.Button("Reset board", GUILayout.Width(90f)))
        {
            if (_mode == LevelEditorMode.Edit)
            {
                if (EditorUtility.DisplayDialog("Reset board", $"Restore {_createLevelName} from file?", "Reset", "Cancel"))
                    LoadLevelIntoDraft(_createLevelName);
            }
            else if (EditorUtility.DisplayDialog("Reset board", "Reset the entire board?", "Reset", "Cancel"))
            {
                InitializeCreateBoard();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cell size", GUILayout.Width(70f));
        _cellSize = EditorGUILayout.Slider(_cellSize, MinCellSize, MaxCellSize, GUILayout.Width(180f));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Validate", GUILayout.Width(80f)))
            RunCreateValidation();

        var saveLabel = _mode == LevelEditorMode.Edit ? "Save Changes" : "Save Level";
        if (GUILayout.Button(saveLabel, GUILayout.Width(100f)))
            SaveCreateLevel();
        EditorGUILayout.EndHorizontal();
    }

    void DrawCreateCatOptions()
    {
        EditorGUILayout.LabelField(
            "Cat (Q): click empty = Q · click again = Cat · click again = Q · Shift+click = remove Q",
            EditorStyles.miniLabel);
    }

    void DrawCreateGrid()
    {
        if (_createColorMap == null || _createSolution == null) return;

        var rowCount = _createBoardSize;
        var colCount = _createBoardSize;
        var gridWidth = colCount * _cellSize;
        var gridHeight = rowCount * _cellSize;
        var rect = GUILayoutUtility.GetRect(gridWidth + 16f, gridHeight + 16f, GUILayout.ExpandWidth(false));
        var gridRect = new Rect(rect.x + 8f, rect.y + 8f, gridWidth, gridHeight);
        var e = Event.current;

        for (var row = 0; row < rowCount; row++)
        {
            for (var col = 0; col < colCount; col++)
            {
                var cellRect = new Rect(
                    gridRect.x + col * _cellSize,
                    gridRect.y + row * _cellSize,
                    _cellSize - 1f,
                    _cellSize - 1f);

                var colorId = _createColorMap[row][col];
                var cellColor = LevelPalette.GetColor(colorId);

                if (e.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(cellRect, cellColor);
                    DrawCreateRegionBorder(cellRect, row, col, rowCount, colCount);

                    if (_showCatSuggestions && _createTool == CreateTool.Cat &&
                        _catSuggestions.Contains(CellKey(row, col)) &&
                        _createSolution[row][col] != "Q")
                        DrawSuggestedQueenMark(cellRect);

                    var hasQueen = _createSolution[row][col] == "Q";
                    var catRevealed = hasQueen && _createCatRevealed[row][col];
                    DrawCellCatOrLabel(cellRect, colorId, cellColor, hasQueen, catRevealed);
                }

                if (e.type == EventType.MouseDown && e.button == 0 && cellRect.Contains(e.mousePosition))
                {
                    HandleCreateCellInput(row, col);
                    e.Use();
                    Repaint();
                }
                else if (e.type == EventType.MouseDrag && e.button == 0 && _createTool == CreateTool.Paint &&
                         cellRect.Contains(e.mousePosition))
                {
                    HandleCreateCellInput(row, col);
                    e.Use();
                    Repaint();
                }
            }
        }

        if (e.type == EventType.Repaint)
        {
            Handles.color = new Color(0f, 0f, 0f, 0.6f);
            Handles.DrawLine(new Vector3(gridRect.x, gridRect.yMax), new Vector3(gridRect.xMax, gridRect.yMax));
            Handles.DrawLine(new Vector3(gridRect.xMax, gridRect.y), new Vector3(gridRect.xMax, gridRect.yMax));
        }
    }

    void DrawCreateRegionBorder(Rect cellRect, int row, int col, int rowCount, int colCount)
    {
        var colorId = _createColorMap[row][col];
        var borderColor = new Color(0f, 0f, 0f, 0.45f);

        if (row == 0 || _createColorMap[row - 1][col] != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.xMax, cellRect.y));
        }

        if (col == 0 || _createColorMap[row][col - 1] != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.y), new Vector3(cellRect.x, cellRect.yMax));
        }

        if (row == rowCount - 1 || _createColorMap[row + 1][col] != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.x, cellRect.yMax), new Vector3(cellRect.xMax, cellRect.yMax));
        }

        if (col == colCount - 1 || _createColorMap[row][col + 1] != colorId)
        {
            Handles.color = borderColor;
            Handles.DrawLine(new Vector3(cellRect.xMax, cellRect.y), new Vector3(cellRect.xMax, cellRect.yMax));
        }
    }

    void HandleCreateCellInput(int row, int col)
    {
        if (_createTool == CreateTool.Paint)
        {
            _createColorMap[row][col] = _createPaintColor;
            _createIsValid = false;
            _draftDirty = true;
            HideCatSuggestions();
            return;
        }

        if (Event.current.shift && _createSolution[row][col] == "Q")
        {
            _createSolution[row][col] = ".";
            _createCatRevealed[row][col] = false;
        }
        else if (_createSolution[row][col] != "Q")
        {
            _createSolution[row][col] = "Q";
            _createCatRevealed[row][col] = false;
        }
        else
        {
            _createCatRevealed[row][col] = !_createCatRevealed[row][col];
        }

        _createIsValid = false;
        _draftDirty = true;
        HideCatSuggestions();
    }

    void ShowCatSuggestions()
    {
        _catSuggestions = LevelCatSuggester.GetSuggestions(_createColorMap, _createSolution, _createBoardSize);
        _showCatSuggestions = true;
        Repaint();
    }

    void HideCatSuggestions()
    {
        _showCatSuggestions = false;
        _catSuggestions.Clear();
    }

    static void DrawSuggestedQueenMark(Rect cellRect)
    {
        EditorGUI.DrawRect(cellRect, new Color(1f, 1f, 1f, 0.22f));

        var center = cellRect.center;
        var radius = cellRect.width * 0.28f;
        Handles.color = new Color(0.15f, 0.1f, 0.05f, 0.28f);
        Handles.DrawSolidDisc(center, Vector3.forward, radius);

        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(cellRect.width * 0.45f),
            normal = { textColor = new Color(0.2f, 0.15f, 0.1f, 0.38f) }
        };
        GUI.Label(cellRect, "Q", style);
    }

    static void DrawPaintColorSwatch(Rect rect, Color color, int colorId, bool selected)
    {
        EditorGUI.DrawRect(rect, color);

        if (selected)
        {
            DrawRectOutline(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), Color.black, 1f);
            DrawRectOutline(rect, Color.white, 2f);
        }
        else
            DrawRectOutline(rect, new Color(0f, 0f, 0f, 0.4f), 1f);

        var labelStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            normal = { textColor = LevelPalette.GetContrastingTextColor(color) }
        };
        GUI.Label(rect, colorId.ToString(), labelStyle);
    }

    static void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    void ClearCreateCats()
    {
        for (var row = 0; row < _createBoardSize; row++)
        for (var col = 0; col < _createBoardSize; col++)
        {
            _createSolution[row][col] = ".";
            _createCatRevealed[row][col] = false;
        }
        _createIsValid = false;
        _draftDirty = true;
        HideCatSuggestions();
        Repaint();
    }

    void DrawCreateColorLegend()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Legend:", GUILayout.Width(50f));
        for (var colorId = 1; colorId <= _createBoardSize; colorId++)
        {
            var color = LevelPalette.GetColor(colorId);
            var swatchRect = GUILayoutUtility.GetRect(18f, 18f, GUILayout.Width(18f));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(swatchRect, color);
                Handles.color = new Color(0f, 0f, 0f, 0.5f);
                Handles.DrawLine(new Vector3(swatchRect.x, swatchRect.y), new Vector3(swatchRect.xMax, swatchRect.y));
                Handles.DrawLine(new Vector3(swatchRect.x, swatchRect.y), new Vector3(swatchRect.x, swatchRect.yMax));
                Handles.DrawLine(new Vector3(swatchRect.xMax, swatchRect.y), new Vector3(swatchRect.xMax, swatchRect.yMax));
                Handles.DrawLine(new Vector3(swatchRect.x, swatchRect.yMax), new Vector3(swatchRect.xMax, swatchRect.yMax));
            }

            EditorGUILayout.LabelField(colorId.ToString(), GUILayout.Width(22f));
        }
        EditorGUILayout.EndHorizontal();
    }

    void DrawCreateValidationPanel()
    {
        if (_createIsValid)
        {
            var saveHint = _mode == LevelEditorMode.Edit ? "Save Changes" : "Save Level";
            EditorGUILayout.HelpBox($"Validation passed — you can {saveHint}.", MessageType.Info);
            return;
        }

        if (_createValidationErrors.Count == 0)
        {
            EditorGUILayout.HelpBox("Click Validate to check rules before saving.", MessageType.Info);
            return;
        }

        EditorGUILayout.HelpBox("Level is invalid — fix the following:", MessageType.Warning);
        _createValidationScroll = EditorGUILayout.BeginScrollView(_createValidationScroll, GUILayout.MaxHeight(120f));
        foreach (var error in _createValidationErrors)
            EditorGUILayout.LabelField("• " + error, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    void RunCreateValidation()
    {
        _createValidationErrors = LevelValidator.Validate(BuildCreateLevelData());
        _createIsValid = _createValidationErrors.Count == 0;
        Repaint();
    }

    void SaveCreateLevel()
    {
        RunCreateValidation();
        if (_createValidationErrors.Count > 0)
        {
            EditorUtility.DisplayDialog("Cannot save", "Level did not pass validation.", "OK");
            return;
        }

        var isEdit = _mode == LevelEditorMode.Edit;
        if (!isEdit)
            RefreshCreateLevelName();

        var savedName = _createLevelName;
        var level = BuildCreateLevelDataSnapshot();
        if (isEdit)
            level.hintPlan = _editSourceHintPlan ?? new HintStep[0];

        LevelLoader.PrepareLevelForSave(level);
        if (!LevelLoader.SaveLevel(savedName, level))
        {
            EditorUtility.DisplayDialog("Save failed", "Could not write level file.", "OK");
            return;
        }

        ReimportLevelAsset(savedName);
        _draftDirty = false;
        RefreshLevelList();

        if (isEdit)
        {
            _viewLevelDirty = false;
            LoadLevelIntoDraft(savedName);
            LoadLevel(savedName);
            EditorUtility.DisplayDialog("Saved", $"Saved {savedName}.json", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog(
                "Level saved",
                $"Saved {_createLevelName}.json\nOpen the new level?",
                "Open",
                "Continue"))
        {
            _mode = LevelEditorMode.View;
            var index = _filteredLevels.IndexOf(_createLevelName);
            if (index >= 0)
            {
                _selectedIndex = index;
                LoadLevel(_createLevelName);
            }
        }
        else
        {
            RefreshCreateLevelName();
            InitializeCreateBoard();
        }
    }

    static int[][] CloneIntMatrix(int[][] source)
    {
        if (source == null) return null;

        var result = new int[source.Length][];
        for (var row = 0; row < source.Length; row++)
            result[row] = source[row] != null ? (int[])source[row].Clone() : null;
        return result;
    }

    static string[][] CloneStringMatrix(string[][] source)
    {
        if (source == null) return null;

        var result = new string[source.Length][];
        for (var row = 0; row < source.Length; row++)
            result[row] = source[row] != null ? (string[])source[row].Clone() : null;
        return result;
    }

    static bool[][] CloneBoolMatrix(bool[][] source, int rows, int cols)
    {
        var result = new bool[rows][];
        for (var row = 0; row < rows; row++)
        {
            result[row] = new bool[cols];
            if (source != null && row < source.Length && source[row] != null)
            {
                for (var col = 0; col < cols && col < source[row].Length; col++)
                    result[row][col] = source[row][col];
            }
        }

        return result;
    }
}
