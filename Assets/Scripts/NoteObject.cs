using UnityEngine;

public class NoteObject : MonoBehaviour
{
    [SerializeField] private string noteId;

    // Optional renderer used to tint the note so it matches the user-selected
    // colour label. If left null we fall back to the first MeshRenderer in
    // the children. Public so the prefab can have it wired up in the Inspector.
    [SerializeField] private MeshRenderer bodyRenderer;

    // Optional priority strip + icon TMP. Both are optional: if the prefab
    // does not provide them we simply skip styling.
    [SerializeField] private MeshRenderer priorityStrip;
    [SerializeField] private TMPro.TextMeshPro iconText;
    [SerializeField] private TMPro.TextMeshPro titleText;

    public string NoteId
    {
        get { return noteId; }
    }

    private void Awake()
    {
        // Fallback so the prefab still styles itself even if the inspector
        // references were not set up yet.
        if (bodyRenderer == null)
        {
            bodyRenderer = GetComponentInChildren<MeshRenderer>();
        }
    }

    private void OnEnable()
    {
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNotesChanged += RefreshStyle;
        }
        RefreshStyle();
    }

    private void OnDisable()
    {
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNotesChanged -= RefreshStyle;
        }
    }

    public void Initialize(string id)
    {
        noteId = id;
        Debug.Log("NoteObject initialized with noteId: " + noteId);
        RefreshStyle();
    }

    public void DeleteThisNote()
    {
        if (string.IsNullOrEmpty(noteId))
        {
            Debug.LogWarning("Cannot delete note because noteId is empty.");
            return;
        }

        NoteManager.Instance.DeleteNote(noteId);
        Destroy(gameObject);
    }

    public void ToggleVisibility()
    {
        if (string.IsNullOrEmpty(noteId))
        {
            Debug.LogWarning("Cannot toggle visibility because noteId is empty.");
            return;
        }

        NoteManager.Instance.ToggleNoteVisibility(noteId);

        NoteData note = NoteManager.Instance.FindNoteById(noteId);

        if (note != null)
        {
            gameObject.SetActive(note.isVisible);
        }
    }

    // Reads the latest styling for this note and applies it to the renderers.
    // Safe to call repeatedly; called automatically when notes change.
    public void RefreshStyle()
    {
        if (string.IsNullOrEmpty(noteId) || NoteManager.Instance == null)
        {
            return;
        }

        NoteData note = NoteManager.Instance.FindNoteById(noteId);
        if (note == null)
        {
            return;
        }

        Color body = NoteStyleCatalog.GetColorLabel(note.colorLabelId).color;
        Color text = NoteStyleCatalog.GetReadableTextColor(body);

        if (bodyRenderer != null && bodyRenderer.material != null)
        {
            // Use sharedMaterial copy via material to avoid leaking instances
            // when called frequently; Unity already handles the duplication.
            bodyRenderer.material.color = body;
        }

        if (priorityStrip != null && priorityStrip.material != null)
        {
            priorityStrip.material.color = NoteStyleCatalog.GetPriorityColor(note.Priority);
        }

        if (iconText != null)
        {
            iconText.text  = NoteStyleCatalog.GetIconGlyph(note.Icon);
            iconText.color = text;
        }

        if (titleText != null)
        {
            titleText.text = CompactTitle(string.IsNullOrEmpty(note.title) ? "(untitled)" : note.title);
            titleText.color = text;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.enableWordWrapping = true;
            titleText.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            titleText.maxVisibleLines = 2;
        }
    }

    private static string CompactTitle(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 48)
        {
            return value;
        }

        return value.Substring(0, 45) + "...";
    }
}
