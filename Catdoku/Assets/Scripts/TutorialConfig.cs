using System;
using UnityEngine;

namespace ColorCubeShooter
{
    public enum TutorialAdvanceMode
    {
        /// <summary>Player taps Understand to move to the next step.</summary>
        OnUnderstandClicked,

        /// <summary>Advances after the player interacts with an allowed cell.</summary>
        OnCellInteraction,

        /// <summary>Completes when the tutorial level is won.</summary>
        OnLevelComplete
    }

    /// <summary>
    /// Board coordinates: x = column (left → right), y = row (top → bottom).
    /// e.g. (0,0) top-left, (3,0) top-right, (2,0) row 0 col 2.
    /// </summary>
    [Serializable]
    public struct TutorialCell
    {
        public int x;
        public int y;

        public TutorialCell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public int Col => x;
        public int Row => y;

        public Vector2Int ToVector2Int() => new(x, y);
    }

    public enum TutorialStepType
    {
        Standard,
        Step1PlaceFirstCat,
        Step2ColorRule,
        Step3RowColumnRule,
        Step4PlaceLastCat,
        Step5ExcludeAdjacent,
        Step6FindLastCat,
        TutorialComplete
    }

    [Serializable]
    public class TutorialStep
    {
        public TutorialStepType stepType;
        [TextArea(2, 5)]
        [Tooltip("Main tutorial message shown on the board text area.")]
        public string boardMessage;

        [Tooltip("Understand button label. Leave empty to use the config default.")]
        public string understandLabel;

        [Tooltip("Show the Understand button for this step.")]
        public bool showUnderstandButton = true;

        [Tooltip("Dim the board with TutorialMask and block non-target cells.")]
        public bool showMask = true;

        [Tooltip("Cells the player may interact with. Empty = all cells on the board.")]
        public TutorialCell[] interactableCells;

        [Tooltip("Target cell for Step1/Step4 double-tap (x = column, y = row).")]
        public TutorialCell targetCell = new(2, 0);

        [Tooltip("Cells shown above the mask (x = column, y = row).")]
        public TutorialCell[] highlightCells;

        [Tooltip("Hand anchor for Step5 (x = column, y = row).")]
        public TutorialCell handCell;

        public TutorialAdvanceMode advanceMode = TutorialAdvanceMode.OnUnderstandClicked;
    }

    /// <summary>
    /// ScriptableObject holding tutorial copy and step flow.
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "PixelFlow/Tutorial Config")]
    public class TutorialConfig : ScriptableObject
    {
        public const string DefaultResourcePath = "Config/TutorialConfig";

        [Header("Defaults")]
        [Tooltip("Fallback label for the Understand button.")]
        public string defaultUnderstandLabel = "Understand!!!";

        [Header("Steps")]
        public TutorialStep[] steps;
    }
}
