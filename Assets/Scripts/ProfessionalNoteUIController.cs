using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Drop-in professional UI for the AReminder project.
//
// Rewritten to use Unity's auto-layout system (VerticalLayoutGroup,
// HorizontalLayoutGroup, LayoutElement, ContentSizeFitter) so the entire
// composer + note list flow inside a single scroll view and adapts to any
// screen size / safe area. Material-Design inspired spacing (8 px grid),
// rounded surfaces (faked with shadow + outline), and a consistent typography
// scale give the UI a professional feel.
//
// Usage: attach this script to ANY empty GameObject in the scene. The
// legacy Canvas/NoteUIController still works untouched.
[DisallowMultipleComponent]
public class ProfessionalNoteUIController : MonoBehaviour
{
    // ----- Layout / theme constants ----------------------------------------
    // Surface palette (Material dark theme)
    private static readonly Color ColBackdrop    = new Color(0.05f, 0.06f, 0.08f, 1f);
    private static readonly Color ColSurface     = new Color(0.13f, 0.14f, 0.17f, 1f);
    private static readonly Color ColSurfaceAlt  = new Color(0.17f, 0.18f, 0.22f, 1f);
    private static readonly Color ColSurfaceHi   = new Color(0.21f, 0.23f, 0.27f, 1f);
    private static readonly Color ColAccent      = new Color(0.36f, 0.62f, 1.00f, 1f);
    private static readonly Color ColAccentDim   = new Color(0.36f, 0.62f, 1.00f, 0.18f);
    private static readonly Color ColTextHi      = new Color(0.96f, 0.97f, 0.99f, 1f);
    private static readonly Color ColTextMd      = new Color(0.78f, 0.81f, 0.86f, 1f);
    private static readonly Color ColTextLo      = new Color(0.55f, 0.59f, 0.66f, 1f);
    private static readonly Color ColInputBg     = new Color(0.10f, 0.11f, 0.13f, 1f);
    private static readonly Color ColInputBorder = new Color(0.27f, 0.30f, 0.36f, 1f);
    private static readonly Color ColDanger      = new Color(0.93f, 0.34f, 0.34f, 1f);
    private static readonly Color ColSuccess     = new Color(0.30f, 0.72f, 0.42f, 1f);
    private static readonly Color ColWarning     = new Color(0.95f, 0.65f, 0.20f, 1f);
    private static readonly Color ColDivider     = new Color(1f, 1f, 1f, 0.06f);
    private static readonly Color ColTransparent = new Color(0f, 0f, 0f, 0f);

    // Spacing tokens (8 px grid)
    private const float S2  = 4f;
    private const float S4  = 8f;
    private const float S6  = 12f;
    private const float S8  = 16f;
    private const float S10 = 20f;
    private const float S12 = 24f;
    private const float S16 = 32f;

    // Type scale
    private const int FontDisplay  = 34;
    private const int FontTitle    = 22;
    private const int FontBody     = 18;
    private const int FontLabel    = 14;
    private const int FontCaption  = 13;
    private const int FontIcon     = 22;

    // ----- Runtime state ---------------------------------------------------
    private string selectedNoteId;
    private TMP_InputField titleInput;
    private TMP_InputField contentInput;
    private TMP_InputField checklistInput;
    private RectTransform checklistListContent;
    private TextMeshProUGUI checklistEmptyHint;
    private TMP_Dropdown reminderDateDropdown;
    private TMP_Dropdown reminderHourDropdown;
    private TMP_Dropdown reminderMinuteDropdown;
    private RectTransform notesListContent;
    private ScrollRect mainScroll;
    private RectTransform mainScrollContent;
    private TextMeshProUGUI emptyHint;
    private TextMeshProUGUI selectedNoteCaption;
    private TextMeshProUGUI subtitleLabel;
    private TextMeshProUGUI voiceStatusLabel;

    private NotePriority? draftPriority = null;
    private string draftColorId = string.Empty;
    private NoteIcon? draftIcon = null;

    private readonly List<(Button btn, NotePriority p)> priorityButtons = new List<(Button, NotePriority)>();
    private readonly List<(Button btn, string id)>      colorButtons    = new List<(Button, string)>();
    private readonly List<(Button btn, NoteIcon i)>     iconButtons     = new List<(Button, NoteIcon)>();

    private static readonly Dictionary<NoteIcon, Sprite> iconSpriteCache = new Dictionary<NoteIcon, Sprite>();

    private void Start()
    {
        BuildUI();
        RefreshStyleSelectors();
        RefreshNoteList();
        StartCoroutine(FixLayoutNextFrame());

        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNotesChanged += RefreshNoteList;
        }
    }

    private System.Collections.IEnumerator FixLayoutNextFrame()
    {
        // Unity evaluates layout groups after the first frame. Without this,
        // ScrollRect can keep the generated Content at height 0 on the first
        // play run, which leaves only the AppBar visible.
        yield return null;

        Canvas.ForceUpdateCanvases();
        if (mainScrollContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(mainScrollContent);
        }
        Canvas.ForceUpdateCanvases();

        if (mainScroll != null)
        {
            mainScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void OnDestroy()
    {
        if (NoteManager.Instance != null)
        {
            NoteManager.Instance.OnNotesChanged -= RefreshNoteList;
        }
    }

    // =======================================================================
    // ROOT CANVAS + APP BAR
    // =======================================================================

    private void BuildUI()
    {
        // ---- root canvas -------------------------------------------------
        GameObject canvasGo = new GameObject("ProfessionalNoteCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // Full-screen backdrop
        Image backdrop = AddImage(canvasGo.transform, "Backdrop", ColBackdrop);
        Stretch(backdrop.rectTransform);

        // ---- safe-area aware root --------------------------------------
        // We pad the top a generous amount (~108) so the AppBar clears the
        // notch / dynamic island on common phones. On a tablet this just
        // appears as additional breathing room and looks intentional.
        RectTransform safeArea = AddRect(canvasGo.transform, "SafeArea");
        safeArea.anchorMin = Vector2.zero;
        safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = new Vector2(0, 0);
        safeArea.offsetMax = new Vector2(0, 0);

        BuildAppBar(safeArea);
        BuildScrollContent(safeArea);
    }

    private void BuildAppBar(RectTransform parent)
    {
        // Elevated app bar
        Image appBar = AddImage(parent, "AppBar", ColSurface);
        var ab = appBar.rectTransform;
        ab.anchorMin = new Vector2(0, 1);
        ab.anchorMax = new Vector2(1, 1);
        ab.pivot     = new Vector2(0.5f, 1f);
        ab.anchoredPosition = Vector2.zero;
        ab.sizeDelta = new Vector2(0, 168);

        // Subtle shadow under the bar
        var sh = appBar.gameObject.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.45f);
        sh.effectDistance = new Vector2(0, -4);

        // Inner row, padded to clear the notch and keep title/subtitle apart
        var row = AddRect(appBar.transform, "Row");
        row.anchorMin = Vector2.zero; row.anchorMax = Vector2.one;
        row.offsetMin = new Vector2(S12, S6);
        row.offsetMax = new Vector2(-S12, -88); // ~88 px reserved for status bar

        var hl = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childForceExpandWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandHeight = true;
        hl.spacing = S8;

        // Title
        var title = AddText(row, "Title", "AReminder", FontDisplay, FontStyles.Bold, ColTextHi);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        var leTitle = title.gameObject.AddComponent<LayoutElement>();
        leTitle.flexibleWidth = 1f;

        // Subtitle (right aligned, fixed width)
        subtitleLabel = AddText(row, "Subtitle", "AR Sticky Notes  ·  Custom Styling",
            FontBody, FontStyles.Normal, ColTextLo);
        subtitleLabel.alignment = TextAlignmentOptions.MidlineRight;
        var leSub = subtitleLabel.gameObject.AddComponent<LayoutElement>();
        leSub.preferredWidth = 460f;
        leSub.flexibleWidth  = 0f;
    }

    // =======================================================================
    // SCROLL CONTENT (composer + note list inside one scroll view)
    // =======================================================================

    private void BuildScrollContent(RectTransform parent)
    {
        // The composer is taller than one phone screen, especially after adding
        // checklist rows and reminder dropdowns, so keep it in one vertical
        // ScrollRect. The content has an explicit starting height to avoid the
        // first-frame zero-height issue seen earlier.
        GameObject scrollGo = new GameObject("MainScroll",
            typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(parent, false);
        var scrollRT = scrollGo.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.offsetMin = new Vector2(0, 0);
        scrollRT.offsetMax = new Vector2(0, -168f);
        scrollGo.GetComponent<Image>().color = ColTransparent;
        scrollGo.GetComponent<Image>().raycastTarget = true;

        mainScroll = scrollGo.GetComponent<ScrollRect>();
        mainScroll.horizontal = false;
        mainScroll.vertical = true;
        mainScroll.movementType = ScrollRect.MovementType.Elastic;
        mainScroll.scrollSensitivity = 35f;

        GameObject viewportGo = new GameObject("Viewport",
            typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRT = viewportGo.GetComponent<RectTransform>();
        Stretch(viewportRT);
        viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
        viewportGo.GetComponent<Image>().raycastTarget = false;
        mainScroll.viewport = viewportRT;

        GameObject contentGo = new GameObject("MainContent",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);

        var rt = contentGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 1900f);
        mainScrollContent = rt;

        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)S12, (int)S12, (int)S12, (int)S16);
        vlg.spacing = S16;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var fitter = contentGo.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        mainScroll.content = rt;

        BuildComposerCard(contentGo.transform);
        BuildVoiceNoteCard(contentGo.transform);
        BuildNotesListCard(contentGo.transform);
    }

    // =======================================================================
    // COMPOSER CARD
    // =======================================================================

    private void BuildComposerCard(Transform parent)
    {
        var card = AddCard(parent, "ComposerCard", 1220f);

        AddCardHeader(card, "Compose Note", "Create or edit your AR sticky note");

        // Selected caption
        selectedNoteCaption = AddText(card, "SelectedNote",
            "New note", FontCaption, FontStyles.Italic, ColTextLo);
        selectedNoteCaption.alignment = TextAlignmentOptions.MidlineLeft;
        AddFixedHeight(selectedNoteCaption.gameObject, 22f);

        // Title input
        AddFieldLabel(card, "TITLE");
        titleInput = AddInput(card, "TitleInput", "Enter note title", false, 64f);

        // Content input
        AddFieldLabel(card, "CONTENT");
        contentInput = AddInput(card, "ContentInput",
            "Enter note content / annotations", true, 132f);

        AddDivider(card);

        // Priority
        AddFieldLabel(card, "PRIORITY");
        BuildPrioritySelector(card);

        // Color
        AddFieldLabel(card, "COLOR LABEL");
        BuildColorSelector(card);

        // Icon
        AddFieldLabel(card, "ICON");
        BuildIconSelector(card);

        AddDivider(card);

        // Checklist
        AddFieldLabel(card, "CHECKLIST");
        BuildChecklistRow(card);
        BuildChecklistList(card);

        // Reminder: keep the date/time input and its action in one row so
        // they line up visually and cannot be overlapped by the next card.
        AddFieldLabel(card, "REMINDER  (YYYY-MM-DD HH:MM:SS)");
        BuildReminderRow(card);

        AddSpacer(card, S16);

        // Action buttons (two rows for breathing room)
        BuildActionButtonsRow1(card);
        AddSpacer(card, S8);
        BuildActionButtonsRow2(card);
        AddSpacer(card, S16);
        AddSpacer(card, S8);
    }

    private void BuildPrioritySelector(Transform parent)
    {
        priorityButtons.Clear();
        var row = AddHorizontalRow(parent, "PriorityRow", S8, 56f);
        NotePriority[] order = { NotePriority.Low, NotePriority.Medium, NotePriority.High };
        foreach (var p in order)
        {
            string label = NoteStyleCatalog.GetPriorityLabel(p);
            Color tint   = NoteStyleCatalog.GetPriorityColor(p);
            Button b = AddPriorityChip(row, "Pri_" + p, label, tint, () => SelectPriority(p));
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            priorityButtons.Add((b, p));
        }
    }

    private void BuildColorSelector(Transform parent)
    {
        colorButtons.Clear();
        var row = AddHorizontalRow(parent, "ColorRow", S6, 56f);
        foreach (var label in NoteStyleCatalog.ColorLabels)
        {
            string lid = label.id;
            Button b = AddSwatchButton(row, "Col_" + lid, label.color, () => SelectColor(lid));
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 56f;
            le.preferredHeight = 56f;
            le.flexibleWidth = 1f;
            colorButtons.Add((b, lid));
        }
    }

    private void BuildIconSelector(Transform parent)
    {
        iconButtons.Clear();
        var row = AddHorizontalRow(parent, "IconRow", S6, 64f);
        NoteIcon[] icons = { NoteIcon.Note, NoteIcon.Star, NoteIcon.Alarm,
                             NoteIcon.Heart, NoteIcon.Shopping, NoteIcon.Work,
                             NoteIcon.Study, NoteIcon.Home };
        foreach (var ic in icons)
        {
            NoteIcon captured = ic;
            Button b = AddIconChip(row, "Icn_" + ic,
                ic,
                NoteStyleCatalog.GetIconAccent(ic),
                () => SelectIcon(captured));
            var le = b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 64f;
            le.preferredHeight = 64f;
            le.flexibleWidth = 1f;
            iconButtons.Add((b, ic));
        }
    }

    private void BuildChecklistRow(Transform parent)
    {
        var row = AddHorizontalRow(parent, "ChecklistRow", S8, 56f);

        checklistInput = AddInput(row, "ChecklistInput",
            "Add a checklist item", false, 56f);
        var le1 = checklistInput.gameObject.AddComponent<LayoutElement>();
        le1.flexibleWidth = 1f;

        Button add = AddSolidButton(row, "AddChecklistBtn",
            "+ Add Item", ColAccent, ColTextHi, OnAddChecklist);
        var le2 = add.gameObject.AddComponent<LayoutElement>();
        le2.preferredWidth = 180f;
    }

    private void BuildChecklistList(Transform parent)
    {
        GameObject listGo = new GameObject("ChecklistList",
            typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(RectMask2D));
        listGo.transform.SetParent(parent, false);
        listGo.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.55f);
        SetOutline(listGo, new Color(1, 1, 1, 0.08f));
        AddFixedHeight(listGo, 112f);

        var vlg = listGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)S6, (int)S6, (int)S4, (int)S4);
        vlg.spacing = S4;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;

        checklistListContent = listGo.GetComponent<RectTransform>();
        checklistEmptyHint = AddText(listGo.transform, "ChecklistEmptyHint",
            "No checklist items for the selected note.", FontCaption, FontStyles.Italic, ColTextLo);
        checklistEmptyHint.alignment = TextAlignmentOptions.Center;
        AddFixedHeight(checklistEmptyHint.gameObject, 32f);
    }

    private void BuildReminderRow(Transform parent)
    {
        var row = AddHorizontalRow(parent, "ReminderRow", S8, 56f);

        System.DateTime defaultTime = RoundUpToFiveMinutes(System.DateTime.Now.AddMinutes(10));

        reminderDateDropdown = AddDropdown(row, "ReminderDateDropdown", BuildDateOptions(14), 0, 56f);
        SetPreferredWidth(reminderDateDropdown, 270f, 1f);

        reminderHourDropdown = AddDropdown(row, "ReminderHourDropdown", BuildNumberOptions(0, 23, 1), defaultTime.Hour, 56f);
        SetPreferredWidth(reminderHourDropdown, 96f, 0f);

        reminderMinuteDropdown = AddDropdown(row, "ReminderMinuteDropdown", BuildNumberOptions(0, 55, 5), defaultTime.Minute / 5, 56f);
        SetPreferredWidth(reminderMinuteDropdown, 96f, 0f);

        SetReminderDropdownsFromDate(defaultTime);

        Button alarm = AddSolidButton(row, "AlarmBtn", "Set Reminder",
            ColWarning, ColTextHi, OnSetReminder);
        var le = alarm.gameObject.GetComponent<LayoutElement>()
                 ?? alarm.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 190f;
        le.flexibleWidth = 0f;
    }

    private void BuildActionButtonsRow1(Transform parent)
    {
        var row = AddHorizontalRow(parent, "ActionsRow1", S8, 64f);

        var addBtn  = AddSolidButton(row, "AddBtn",  "Add Note",   ColAccent,  ColTextHi, OnAdd);
        var editBtn = AddSolidButton(row, "EditBtn", "Save Edit",  ColSuccess, ColTextHi, OnEdit);

        Flex(addBtn);
        Flex(editBtn);
    }

    private void BuildActionButtonsRow2(Transform parent)
    {
        var row = AddHorizontalRow(parent, "ActionsRow2", S8, 56f);

        var hideBtn = AddOutlineButton(row, "HideBtn", "Hide / Show",
            ColSurfaceHi, ColTextMd, OnToggleVisibility);
        var delBtn  = AddSolidButton(row, "DelBtn", "Delete",
            ColDanger, ColTextHi, OnDelete);

        Flex(hideBtn);
        Flex(delBtn);
    }

    private void OnSetReminder()
    {
        UnityEngine.Debug.Log("Processing reminder string target");
        var notes = (NoteManager.Instance != null) ? NoteManager.Instance.GetAllNotes() : null;
        string targetNoteId = "";

        if (!string.IsNullOrEmpty(selectedNoteId))
        {
            targetNoteId = selectedNoteId;
        }
        else if (notes != null && notes.Count > 0)
        {
            targetNoteId = notes[notes.Count - 1].noteId;
        }
        else
        {
            NoteData dummyNote = NoteManager.Instance.AddNote(
                "UI Timestamp Note", "Automatically generated container.");
            targetNoteId = dummyNote.noteId;
        }

        string timeToSchedule = GetReminderDropdownValue();
        UnityEngine.Debug.Log($"[SYSTEM] Registering dropdown reminder target: {timeToSchedule}");

        NoteManager.Instance.SetNoteReminder(targetNoteId, timeToSchedule);
    }

    // =======================================================================
    // VOICE NOTE CARD
    // =======================================================================

    private void BuildVoiceNoteCard(Transform parent)
    {
        var card = AddCard(parent, "VoiceCard", 320f);
        AddCardHeader(card, "Voice Note", "Capture a quick spoken note");

        voiceStatusLabel = AddText(card, "VoiceStatus", "Ready. Press Record, then Stop, then Play.", FontCaption, FontStyles.Italic, ColTextLo);
        voiceStatusLabel.alignment = TextAlignmentOptions.MidlineLeft;
        AddFixedHeight(voiceStatusLabel.gameObject, 22f);

        AddSpacer(card, S8);

        var row = AddHorizontalRow(card, "VoiceRow", S8, 56f);

        var rec = AddSolidButton(row, "RecordVoiceBtn", "Record",
            ColSuccess, ColTextHi, () =>
            {
                bool ok = EnsureVoiceNoteManager().StartRecording();
                SetVoiceStatus(ok ? "Recording... speak now, then press Stop." : "No microphone available or permission denied.", ok ? ColSuccess : ColDanger);
            });
        var stop = AddSolidButton(row, "StopVoiceBtn", "Stop",
            ColDanger, ColTextHi, () =>
            {
                bool ok = EnsureVoiceNoteManager().StopRecording();
                SetVoiceStatus(ok ? "Recording saved. Press Play to hear it." : "No audio captured. Check microphone permission/input.", ok ? ColAccent : ColDanger);
            });
        var play = AddSolidButton(row, "PlayVoiceBtn", "Play",
            ColAccent, ColTextHi, () =>
            {
                bool ok = EnsureVoiceNoteManager().PlayRecording();
                SetVoiceStatus(ok ? "Playing recording..." : "Nothing to play. Record and Stop first.", ok ? ColAccent : ColDanger);
            });

        Flex(rec); Flex(stop); Flex(play);

        AddSpacer(card, S8);
        var saveRow = AddHorizontalRow(card, "VoiceSaveRow", S8, 52f);
        var save = AddSolidButton(saveRow, "SaveVoiceBtn", "Save Voice", ColSuccess, ColTextHi, OnSaveVoiceNote);
        var remove = AddSolidButton(saveRow, "RemoveVoiceBtn", "Remove Voice", ColDanger, ColTextHi, OnRemoveVoiceNote);
        Flex(save); Flex(remove);
    }

    private void OnSaveVoiceNote()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            SetVoiceStatus("Select or create a note before saving voice.", ColDanger);
            return;
        }

        VoiceNoteManager voice = EnsureVoiceNoteManager();
        string path = voice.SaveRecording(selectedNoteId);
        if (string.IsNullOrEmpty(path))
        {
            SetVoiceStatus("No recording to save. Record, Stop, then Save Voice.", ColDanger);
            return;
        }

        NoteManager.Instance.SetNoteVoicePath(selectedNoteId, path);
        SetVoiceStatus("Voice note saved for this note.", ColSuccess);
    }

    private void OnRemoveVoiceNote()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            SetVoiceStatus("Select a note before removing voice.", ColDanger);
            return;
        }

        NoteManager.Instance.RemoveNoteVoice(selectedNoteId);
        EnsureVoiceNoteManager().ClearRecording();
        SetVoiceStatus("Voice note removed.", ColDanger);
    }

    private VoiceNoteManager EnsureVoiceNoteManager()
    {
        VoiceNoteManager voice = FindObjectOfType<VoiceNoteManager>();
        if (voice != null)
        {
            return voice;
        }

        GameObject go = new GameObject("VoiceNoteManager", typeof(AudioSource), typeof(VoiceNoteManager));
        voice = go.GetComponent<VoiceNoteManager>();
        voice.audioSource = go.GetComponent<AudioSource>();
        return voice;
    }

    private void LoadSelectedVoiceNote(NoteData note)
    {
        if (note == null || string.IsNullOrEmpty(note.voiceNotePath))
        {
            EnsureVoiceNoteManager().ClearRecording();
            SetVoiceStatus("No saved voice for this note. Record, Stop, then Save Voice.", ColTextLo);
            return;
        }

        bool loaded = EnsureVoiceNoteManager().LoadRecording(note.voiceNotePath);
        SetVoiceStatus(loaded ? "Saved voice loaded. Press Play to hear it." : "Saved voice file missing. Record again.", loaded ? ColAccent : ColDanger);
    }

    private void SetVoiceStatus(string message, Color color)
    {
        if (voiceStatusLabel == null) return;
        voiceStatusLabel.text = message;
        voiceStatusLabel.color = color;
    }

    // =======================================================================
    // NOTES LIST CARD
    // =======================================================================

    private void BuildNotesListCard(Transform parent)
    {
        var card = AddCard(parent, "NotesCard", 360f);
        AddCardHeader(card, "Your Notes", "Tap a card to edit");

        // Empty hint placeholder
        emptyHint = AddText(card, "EmptyHint",
            "No notes yet. Use the form above to create your first AR sticky note.",
            FontBody, FontStyles.Italic, ColTextLo);
        emptyHint.alignment = TextAlignmentOptions.Center;
        AddFixedHeight(emptyHint.gameObject, 56f);

        // Fixed-height, clipped list region so note rows stay inside the
        // Notes card instead of spilling over the rest of the screen.
        GameObject contentGo = new GameObject("List",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement), typeof(RectMask2D));
        contentGo.transform.SetParent(card, false);
        AddFixedHeight(contentGo, 210f);
        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 0, 0);
        vlg.spacing = S6;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        notesListContent = contentGo.GetComponent<RectTransform>();
    }

    // =======================================================================
    // SELECTOR LOGIC
    // =======================================================================

    private void SelectPriority(NotePriority p)
    {
        draftPriority = p;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
            NoteManager.Instance.SetNotePriority(selectedNoteId, p);
        RefreshStyleSelectors();
    }

    private void SelectColor(string id)
    {
        draftColorId = id;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
            NoteManager.Instance.SetNoteColor(selectedNoteId, id);
        RefreshStyleSelectors();
    }

    private void SelectIcon(NoteIcon i)
    {
        draftIcon = i;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
            NoteManager.Instance.SetNoteIcon(selectedNoteId, i);
        RefreshStyleSelectors();
    }

    private void RefreshStyleSelectors()
    {
        // Priority chips: selected -> filled, unselected -> faint outline
        foreach (var (btn, p) in priorityButtons)
        {
            bool sel = draftPriority.HasValue && (p == draftPriority.Value);
            Color tint = NoteStyleCatalog.GetPriorityColor(p);
            var img = btn.GetComponent<Image>();
            Color bg = sel ? tint : DarkenColor(tint, 0.12f);
            img.color = bg;

            var t = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (t != null) t.color = GetContrastTextColor(bg);

            SetOutline(btn.gameObject, sel ? Color.white : new Color(1, 1, 1, 0.20f));
        }

        // Color swatches: white outline when selected
        foreach (var (btn, id) in colorButtons)
        {
            bool sel = !string.IsNullOrEmpty(draftColorId) && (id == draftColorId);
            SetOutline(btn.gameObject, sel ? Color.white : new Color(1, 1, 1, 0.10f));
            SetOutlineThickness(btn.gameObject, sel ? 3f : 1f);
        }

        // Icon chips: selected -> accent ring + brighter background
        foreach (var (btn, ic) in iconButtons)
        {
            bool sel = draftIcon.HasValue && (ic == draftIcon.Value);
            Color tint = NoteStyleCatalog.GetIconAccent(ic);
            var img = btn.GetComponent<Image>();
            img.color = sel ? tint : new Color(tint.r, tint.g, tint.b, 0.20f);

            SetIconGraphicColor(btn.transform, sel ? Color.white : LightenColor(tint, 0.12f));

            SetOutline(btn.gameObject, sel ? Color.white : new Color(1, 1, 1, 0.08f));
            SetOutlineThickness(btn.gameObject, sel ? 3f : 1f);
        }
    }

    // =======================================================================
    // CRUD ACTIONS
    // =======================================================================

    private void OnAdd()
    {
        if (titleInput == null || NoteManager.Instance == null) return;
        if (string.IsNullOrWhiteSpace(titleInput.text))
        {
            UnityEngine.Debug.LogWarning("Title cannot be empty.");
            return;
        }

        NoteData note = NoteManager.Instance.AddNote(titleInput.text, contentInput.text);
        if (draftPriority.HasValue)
            NoteManager.Instance.SetNotePriority(note.noteId, draftPriority.Value);
        if (!string.IsNullOrEmpty(draftColorId))
            NoteManager.Instance.SetNoteColor(note.noteId, draftColorId);
        if (draftIcon.HasValue)
            NoteManager.Instance.SetNoteIcon(note.noteId, draftIcon.Value);
        SelectNote(note.noteId);
    }

    private void OnEdit()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.EditNote(selectedNoteId, titleInput.text, contentInput.text);
    }

    private void OnDelete()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.DeleteNote(selectedNoteId);
        SelectNote(null);
        titleInput.text = ""; contentInput.text = ""; checklistInput.text = "";
    }

    private void OnToggleVisibility()
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.ToggleNoteVisibility(selectedNoteId);
    }

    private void OnAddChecklist()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            UnityEngine.Debug.LogWarning("Select or create a note before adding checklist items.");
            return;
        }
        if (string.IsNullOrWhiteSpace(checklistInput.text)) return;
        NoteManager.Instance.AddChecklistItem(selectedNoteId, checklistInput.text);
        checklistInput.text = "";
        RefreshSelectedChecklist();
    }

    private void OnToggleChecklistItem(int index)
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.ToggleChecklistItem(selectedNoteId, index);
        RefreshSelectedChecklist();
    }

    private void OnRemoveChecklistItem(int index)
    {
        if (string.IsNullOrEmpty(selectedNoteId)) return;
        NoteManager.Instance.RemoveChecklistItem(selectedNoteId, index);
        RefreshSelectedChecklist();
    }

    private void SelectNote(string id)
    {
        selectedNoteId = id;
        if (string.IsNullOrEmpty(id))
        {
            selectedNoteCaption.text = "New note";
            draftPriority = null;
            draftColorId  = string.Empty;
            draftIcon     = null;
            EnsureVoiceNoteManager().ClearRecording();
            SetVoiceStatus("Ready. Press Record, then Stop, then Save Voice.", ColTextLo);
            RefreshStyleSelectors();
            RefreshSelectedChecklist();
            return;
        }
        NoteData n = NoteManager.Instance.FindNoteById(id);
        if (n == null)
        {
            selectedNoteCaption.text = "New note";
            return;
        }
        titleInput.text   = n.title;
        contentInput.text = n.content;
        draftPriority = n.Priority;
        draftColorId  = n.colorLabelId;
        draftIcon     = n.Icon;
        if (!string.IsNullOrEmpty(n.reminderTime) && System.DateTime.TryParse(n.reminderTime, out System.DateTime savedReminder))
        {
            SetReminderDropdownsFromDate(savedReminder);
        }
        LoadSelectedVoiceNote(n);
        selectedNoteCaption.text = "Editing: " + (string.IsNullOrEmpty(n.title) ? "(untitled)" : n.title);
        RefreshStyleSelectors();
        RefreshSelectedChecklist();
    }

    private void RefreshSelectedChecklist()
    {
        if (checklistListContent == null) return;

        for (int i = checklistListContent.childCount - 1; i >= 0; i--)
        {
            Transform child = checklistListContent.GetChild(i);
            if (checklistEmptyHint != null && child == checklistEmptyHint.transform) continue;
            Destroy(child.gameObject);
        }

        NoteData note = null;
        if (!string.IsNullOrEmpty(selectedNoteId) && NoteManager.Instance != null)
        {
            note = NoteManager.Instance.FindNoteById(selectedNoteId);
        }

        bool empty = note == null || note.checklistItems == null || note.checklistItems.Count == 0;
        if (checklistEmptyHint != null)
        {
            checklistEmptyHint.gameObject.SetActive(empty);
            checklistEmptyHint.text = string.IsNullOrEmpty(selectedNoteId)
                ? "Select or create a note to manage checklist items."
                : "No checklist items for this note.";
        }
        if (empty) return;

        for (int i = 0; i < note.checklistItems.Count; i++)
        {
            BuildChecklistItemRow(note.checklistItems[i], i);
        }
    }

    private void BuildChecklistItemRow(ChecklistItem item, int index)
    {
        GameObject row = new GameObject("ChecklistItem_" + index,
            typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(checklistListContent, false);
        row.GetComponent<Image>().color = item.isCompleted
            ? new Color(0.30f, 0.72f, 0.42f, 0.20f)
            : new Color(1f, 1f, 1f, 0.05f);
        var le = row.GetComponent<LayoutElement>();
        le.minHeight = 36f;
        le.preferredHeight = 36f;

        int capturedIndex = index;

        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset((int)S4, (int)S4, 0, 0);
        hlg.spacing = S6;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = false;
        hlg.childControlHeight = true;
        hlg.childForceExpandHeight = false;

        Button toggle = AddOutlineButton(row.transform, "Toggle", item.isCompleted ? "[x]" : "[ ]",
            item.isCompleted ? new Color(0.30f, 0.72f, 0.42f, 0.35f) : ColSurfaceHi,
            item.isCompleted ? ColSuccess : ColTextMd,
            () => OnToggleChecklistItem(capturedIndex));
        var toggleLE = toggle.gameObject.GetComponent<LayoutElement>() ?? toggle.gameObject.AddComponent<LayoutElement>();
        toggleLE.preferredWidth = 48f;
        toggleLE.minWidth = 48f;
        toggleLE.preferredHeight = 30f;
        toggleLE.minHeight = 30f;

        var label = AddText(row.transform, "Label", item.itemText, FontCaption, item.isCompleted ? FontStyles.Strikethrough : FontStyles.Normal,
            item.isCompleted ? ColTextLo : ColTextHi);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        var labelLE = label.gameObject.AddComponent<LayoutElement>();
        labelLE.flexibleWidth = 1f;

        Button remove = AddSolidButton(row.transform, "Remove", "Remove", ColDanger, ColTextHi,
            () => OnRemoveChecklistItem(capturedIndex));
        var removeLE = remove.gameObject.GetComponent<LayoutElement>() ?? remove.gameObject.AddComponent<LayoutElement>();
        removeLE.preferredWidth = 96f;
        removeLE.minWidth = 96f;
        removeLE.preferredHeight = 30f;
        removeLE.minHeight = 30f;
    }

    // =======================================================================
    // NOTE LIST RENDERING
    // =======================================================================

    private void RefreshNoteList()
    {
        if (notesListContent == null) return;

        for (int i = notesListContent.childCount - 1; i >= 0; i--)
            Destroy(notesListContent.GetChild(i).gameObject);

        var notes = (NoteManager.Instance != null) ? NoteManager.Instance.GetAllNotes() : null;
        bool empty = (notes == null || notes.Count == 0);
        if (emptyHint != null) emptyHint.gameObject.SetActive(empty);
        if (empty)
        {
            RefreshSelectedChecklist();
            return;
        }

        for (int i = 0; i < notes.Count; i++)
            BuildNoteRow(notes[i]);

        RefreshSelectedChecklist();
    }

    private void BuildNoteRow(NoteData note)
    {
        Color bg  = NoteStyleCatalog.GetColorLabel(note.colorLabelId).color;
        Color fg  = NoteStyleCatalog.GetReadableTextColor(bg);
        Color pri = NoteStyleCatalog.GetPriorityColor(note.Priority);

        GameObject row = new GameObject("Note_" + note.noteId,
            typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button),
            typeof(HorizontalLayoutGroup), typeof(RectMask2D));
        row.transform.SetParent(notesListContent, false);
        var img = row.GetComponent<Image>();
        img.color = bg;

        var le = row.GetComponent<LayoutElement>();
        le.minHeight = 104; le.preferredHeight = 104;

        var btn = row.GetComponent<Button>();
        string capturedId = note.noteId;
        btn.onClick.AddListener(() => SelectNote(capturedId));

        SetOutline(row, new Color(0, 0, 0, 0.10f));
        var sh = row.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.35f);
        sh.effectDistance = new Vector2(0, -3);

        var hl = row.GetComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset((int)S8, (int)S8, (int)S6, (int)S6);
        hl.spacing = S8;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childForceExpandWidth = false;
        hl.childControlHeight = true;
        hl.childForceExpandHeight = false;

        // Priority strip on the left
        Image strip = AddImage(row.transform, "Strip", pri);
        var leStrip = strip.gameObject.AddComponent<LayoutElement>();
        leStrip.preferredWidth = 6f;
        leStrip.minWidth = 6f;
        leStrip.preferredHeight = 80f;
        leStrip.minHeight = 80f;

        // Round icon
        var iconBg = AddImage(row.transform, "IconBg",
            NoteStyleCatalog.GetIconAccent(note.Icon));
        var leIcon = iconBg.gameObject.AddComponent<LayoutElement>();
        leIcon.preferredWidth = 56f;
        leIcon.preferredHeight = 56f;
        SetOutline(iconBg.gameObject, new Color(1, 1, 1, 0.25f));
        Image rowIcon = AddImage(iconBg.transform, "IconGraphic", Color.white);
        rowIcon.sprite = GetIconSprite(note.Icon);
        rowIcon.type = Image.Type.Simple;
        rowIcon.preserveAspect = true;
        rowIcon.raycastTarget = false;
        var rowIconRT = rowIcon.rectTransform;
        rowIconRT.anchorMin = Vector2.zero;
        rowIconRT.anchorMax = Vector2.one;
        rowIconRT.offsetMin = new Vector2(12f, 12f);
        rowIconRT.offsetMax = new Vector2(-12f, -12f);

        // Text column
        GameObject col = new GameObject("Col",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        col.transform.SetParent(row.transform, false);
        var leCol = col.AddComponent<LayoutElement>();
        leCol.flexibleWidth = 1f;
        leCol.preferredHeight = 64f;
        leCol.minHeight = 64f;
        var cv = col.GetComponent<VerticalLayoutGroup>();
        cv.padding = new RectOffset(0, 0, 0, 0);
        cv.spacing = 2f;
        cv.childAlignment = TextAnchor.MiddleLeft;
        cv.childControlWidth = true;
        cv.childForceExpandWidth = true;
        cv.childControlHeight = true;
        cv.childForceExpandHeight = false;

        var titleT = AddText(col.transform, "Title",
            string.IsNullOrEmpty(note.title) ? "(untitled)" : note.title,
            FontTitle, FontStyles.Bold, fg);
        titleT.alignment = TextAlignmentOptions.MidlineLeft;
        titleT.enableWordWrapping = false;
        titleT.overflowMode = TextOverflowModes.Ellipsis;
        AddFixedHeight(titleT.gameObject, 30f);

        string body = note.content ?? "";
        if (note.checklistItems != null && note.checklistItems.Count > 0)
        {
            int done = 0;
            for (int i = 0; i < note.checklistItems.Count; i++)
                if (note.checklistItems[i].isCompleted) done++;
            string sep = string.IsNullOrEmpty(body) ? "" : "  ·  ";
            body = body + sep + done + "/" + note.checklistItems.Count + " done";
        }
        var bodyT = AddText(col.transform, "Body",
            body, FontBody, FontStyles.Normal, new Color(fg.r, fg.g, fg.b, 0.80f));
        bodyT.alignment = TextAlignmentOptions.MidlineLeft;
        bodyT.overflowMode = TextOverflowModes.Ellipsis;
        bodyT.enableWordWrapping = false;
        AddFixedHeight(bodyT.gameObject, 26f);

        // Right side chip
        Image chip = AddImage(row.transform, "Chip", pri);
        var leChip = chip.gameObject.AddComponent<LayoutElement>();
        leChip.preferredWidth = 110f;
        leChip.minWidth = 110f;
        leChip.preferredHeight = 34f;
        leChip.minHeight = 34f;
        SetOutline(chip.gameObject, new Color(1, 1, 1, 0.30f));
        var chipText = AddText(chip.transform, "ChipTxt",
            NoteStyleCatalog.GetPriorityLabel(note.Priority).ToUpper(),
            FontLabel, FontStyles.Bold, Color.white);
        Stretch(chipText.rectTransform);
        chipText.alignment = TextAlignmentOptions.Center;
        chipText.characterSpacing = 4f;

        if (!note.isVisible)
        {
            var hidden = AddText(row.transform, "HiddenTag",
                "HIDDEN", FontCaption, FontStyles.Bold, ColTextLo);
            var leH = hidden.gameObject.AddComponent<LayoutElement>();
            leH.preferredWidth = 64f;
        }
    }

    // =======================================================================
    // GENERIC UI HELPERS
    // =======================================================================

    private RectTransform AddRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private Image AddImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private TextMeshProUGUI AddText(Transform parent, string name, string text,
        float size, FontStyles style, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = style;
        t.color = color;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        return t;
    }

    private TMP_InputField AddInput(Transform parent, string name,
        string placeholder, bool isMultiLine, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var bg = go.GetComponent<Image>();
        bg.color = ColInputBg;
        SetOutline(go, ColInputBorder);
        SetOutlineThickness(go, 1.5f);

        AddFixedHeight(go, height);

        var input = go.AddComponent<TMP_InputField>();
        input.lineType = isMultiLine
            ? TMP_InputField.LineType.MultiLineNewline
            : TMP_InputField.LineType.SingleLine;

        GameObject ta = new GameObject("TextArea",
            typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(go.transform, false);
        var taRT = ta.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(S6, S2);
        taRT.offsetMax = new Vector2(-S6, -S2);

        var ph = AddText(ta.transform, "Placeholder",
            placeholder, FontBody, FontStyles.Italic,
            new Color(0.55f, 0.59f, 0.66f, 1f));
        Stretch(ph.rectTransform);

        var txt = AddText(ta.transform, "Text",
            "", FontBody, FontStyles.Normal, ColTextHi);
        Stretch(txt.rectTransform);

        input.textViewport = taRT;
        input.textComponent = txt;
        input.placeholder = ph;

        return input;
    }

    private TMP_Dropdown AddDropdown(Transform parent, string name, List<string> options, int defaultValue, float height)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = ColInputBg;
        SetOutline(go, ColInputBorder);
        SetOutlineThickness(go, 1.5f);
        AddFixedHeight(go, height);

        TMP_Dropdown dropdown = go.GetComponent<TMP_Dropdown>();

        TextMeshProUGUI caption = AddText(go.transform, "Caption", "", FontBody, FontStyles.Bold, ColTextHi);
        caption.alignment = TextAlignmentOptions.MidlineLeft;
        var capRT = caption.rectTransform;
        capRT.anchorMin = Vector2.zero;
        capRT.anchorMax = Vector2.one;
        capRT.offsetMin = new Vector2(S6, 0);
        capRT.offsetMax = new Vector2(-34f, 0);

        TextMeshProUGUI arrow = AddText(go.transform, "Arrow", "v", FontBody, FontStyles.Bold, ColTextLo);
        arrow.alignment = TextAlignmentOptions.Center;
        var arrowRT = arrow.rectTransform;
        arrowRT.anchorMin = new Vector2(1, 0);
        arrowRT.anchorMax = new Vector2(1, 1);
        arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.sizeDelta = new Vector2(30f, 0);
        arrowRT.anchoredPosition = new Vector2(-4f, 0);

        RectTransform template = CreateDropdownTemplate(go.transform, options.Count);
        TextMeshProUGUI itemText = template.GetComponentInChildren<Toggle>(true).GetComponentInChildren<TextMeshProUGUI>(true);

        dropdown.template = template;
        dropdown.captionText = caption;
        dropdown.itemText = itemText;
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.value = Mathf.Clamp(defaultValue, 0, Mathf.Max(0, options.Count - 1));
        dropdown.RefreshShownValue();

        return dropdown;
    }

    private RectTransform CreateDropdownTemplate(Transform parent, int optionCount)
    {
        const float itemHeight = 42f;
        float visibleHeight = (Mathf.Min(Mathf.Max(optionCount, 1), 3) * (itemHeight + 2f)) + 8f;
        float contentHeight = (Mathf.Max(optionCount, 1) * (itemHeight + 2f)) + 8f;

        GameObject templateGo = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templateGo.transform.SetParent(parent, false);
        var templateRT = templateGo.GetComponent<RectTransform>();
        // Open upward so the reminder dropdown does not cover the action buttons below it.
        templateRT.anchorMin = new Vector2(0, 1);
        templateRT.anchorMax = new Vector2(1, 1);
        templateRT.pivot = new Vector2(0.5f, 0f);
        templateRT.anchoredPosition = new Vector2(0, 2f);
        templateRT.sizeDelta = new Vector2(0, visibleHeight);
        templateGo.GetComponent<Image>().color = ColSurfaceHi;
        SetOutline(templateGo, new Color(1, 1, 1, 0.16f));

        ScrollRect scroll = templateGo.GetComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewportGo.transform.SetParent(templateGo.transform, false);
        var viewportRT = viewportGo.GetComponent<RectTransform>();
        Stretch(viewportRT);
        viewportGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.001f);
        viewportGo.GetComponent<Image>().raycastTarget = false;
        scroll.viewport = viewportRT;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRT = contentGo.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.anchoredPosition = Vector2.zero;
        contentRT.sizeDelta = new Vector2(0, contentHeight);
        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(0, 0, 4, 4);
        vlg.spacing = 2f;
        vlg.childControlWidth = true;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandHeight = false;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
        scroll.content = contentRT;

        GameObject itemGo = new GameObject("Item", typeof(RectTransform), typeof(Toggle), typeof(Image), typeof(LayoutElement));
        itemGo.transform.SetParent(contentGo.transform, false);
        var itemRT = itemGo.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 1);
        itemRT.anchorMax = new Vector2(1, 1);
        itemRT.pivot = new Vector2(0.5f, 1f);
        itemRT.sizeDelta = new Vector2(0, itemHeight);
        itemGo.GetComponent<Image>().color = ColSurfaceAlt;
        var itemLE = itemGo.GetComponent<LayoutElement>();
        itemLE.minHeight = itemHeight;
        itemLE.preferredHeight = itemHeight;
        var toggle = itemGo.GetComponent<Toggle>();
        toggle.targetGraphic = itemGo.GetComponent<Image>();

        TextMeshProUGUI label = AddText(itemGo.transform, "Item Label", "Option", FontBody, FontStyles.Bold, Color.white);
        label.alignment = TextAlignmentOptions.MidlineLeft;
        var labelRT = label.rectTransform;
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(S6, 0);
        labelRT.offsetMax = new Vector2(-S6, 0);

        templateGo.SetActive(false);
        return templateRT;
    }

    private static List<string> BuildDateOptions(int days)
    {
        List<string> options = new List<string>();
        System.DateTime today = System.DateTime.Today;
        for (int i = 0; i < days; i++)
        {
            System.DateTime d = today.AddDays(i);
            string prefix = i == 0 ? "Today" : (i == 1 ? "Tomorrow" : d.ToString("ddd"));
            options.Add(prefix + " " + d.ToString("MM/dd"));
        }
        return options;
    }

    private static List<string> BuildNumberOptions(int start, int end, int step)
    {
        List<string> options = new List<string>();
        for (int v = start; v <= end; v += step)
        {
            options.Add(v.ToString("00"));
        }
        return options;
    }

    private static System.DateTime RoundUpToFiveMinutes(System.DateTime value)
    {
        int minute = ((value.Minute + 4) / 5) * 5;
        if (minute >= 60)
        {
            value = value.AddHours(1);
            minute = 0;
        }
        return new System.DateTime(value.Year, value.Month, value.Day, value.Hour, minute, 0);
    }

    private string GetReminderDropdownValue()
    {
        System.DateTime today = System.DateTime.Today;
        int dateOffset = reminderDateDropdown != null ? reminderDateDropdown.value : 0;
        int hour = reminderHourDropdown != null ? reminderHourDropdown.value : System.DateTime.Now.Hour;
        int minute = reminderMinuteDropdown != null ? reminderMinuteDropdown.value * 5 : 0;
        System.DateTime selected = today.AddDays(dateOffset).AddHours(hour).AddMinutes(minute);
        return selected.ToString("yyyy-MM-dd HH:mm:ss");
    }

    private void SetReminderDropdownsFromDate(System.DateTime value)
    {
        if (reminderDateDropdown == null || reminderHourDropdown == null || reminderMinuteDropdown == null) return;

        int dateOffset = Mathf.Clamp((value.Date - System.DateTime.Today).Days, 0, reminderDateDropdown.options.Count - 1);
        reminderDateDropdown.value = dateOffset;
        reminderHourDropdown.value = Mathf.Clamp(value.Hour, 0, reminderHourDropdown.options.Count - 1);
        reminderMinuteDropdown.value = Mathf.Clamp(Mathf.RoundToInt(value.Minute / 5f), 0, reminderMinuteDropdown.options.Count - 1);

        reminderDateDropdown.RefreshShownValue();
        reminderHourDropdown.RefreshShownValue();
        reminderMinuteDropdown.RefreshShownValue();
    }

    private void SetPreferredWidth(Component c, float preferredWidth, float flexibleWidth)
    {
        var le = c.gameObject.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = preferredWidth;
        le.flexibleWidth = flexibleWidth;
    }

    private Button AddSolidButton(Transform parent, string name, string text,
        Color bg, Color fg, System.Action onClick)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = bg;
        var label = AddText(go.transform, "Label",
            text, FontBody, FontStyles.Bold, fg);
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());

        // Subtle pressed feedback via ColorBlock
        var cb = b.colors;
        cb.highlightedColor = LightenColor(bg, 0.08f);
        cb.pressedColor = DarkenColor(bg, 0.12f);
        cb.selectedColor = bg;
        b.colors = cb;

        AddFixedHeight(go, 56f);
        return b;
    }

    private Button AddOutlineButton(Transform parent, string name, string text,
        Color bg, Color fg, System.Action onClick)
    {
        var b = AddSolidButton(parent, name, text, bg, fg, onClick);
        SetOutline(b.gameObject, new Color(1, 1, 1, 0.15f));
        return b;
    }

    private Button AddPriorityChip(Transform parent, string name, string text,
        Color tint, System.Action onClick)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Color bg = DarkenColor(tint, 0.12f);
        go.GetComponent<Image>().color = bg;
        var label = AddText(go.transform, "Label",
            text.ToUpper(), FontBody, FontStyles.Bold, GetContrastTextColor(bg));
        Stretch(label.rectTransform);
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 4;

        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());
        SetOutline(go, new Color(1, 1, 1, 0.20f));
        return b;
    }

    private Button AddSwatchButton(Transform parent, string name, Color color,
        System.Action onClick)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        SetOutline(go, new Color(1, 1, 1, 0.10f));
        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());
        return b;
    }

    private Button AddIconChip(Transform parent, string name,
        NoteIcon icon, Color tint, System.Action onClick)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(tint.r, tint.g, tint.b, 0.20f);

        Image iconImage = AddImage(go.transform, "IconGraphic", LightenColor(tint, 0.12f));
        iconImage.sprite = GetIconSprite(icon);
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        var iconRT = iconImage.rectTransform;
        iconRT.anchorMin = Vector2.zero;
        iconRT.anchorMax = Vector2.one;
        iconRT.offsetMin = new Vector2(14f, 14f);
        iconRT.offsetMax = new Vector2(-14f, -14f);

        Button b = go.GetComponent<Button>();
        b.onClick.AddListener(() => onClick());
        SetOutline(go, new Color(1, 1, 1, 0.08f));
        return b;
    }

    // ----- Card primitives ------------------------------------------------

    private Transform AddCard(Transform parent, string name, float preferredHeight)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(LayoutElement),
            typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = ColSurface;
        SetOutline(go, new Color(1, 1, 1, 0.06f));

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = preferredHeight;
        le.preferredHeight = preferredHeight;
        le.flexibleHeight = 0f;

        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.5f);
        sh.effectDistance = new Vector2(0, -6);

        var vlg = go.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset((int)S12, (int)S12, (int)S12, (int)S12);
        vlg.spacing = S4;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        return go.transform;
    }

    private void AddCardHeader(Transform parent, string title, string subtitle)
    {
        var t = AddText(parent, "CardTitle", title,
            FontTitle + 4, FontStyles.Bold, ColTextHi);
        AddFixedHeight(t.gameObject, 30f);
        if (!string.IsNullOrEmpty(subtitle))
        {
            var st = AddText(parent, "CardSub", subtitle,
                FontCaption, FontStyles.Normal, ColTextLo);
            AddFixedHeight(st.gameObject, 18f);
        }
        AddSpacer(parent, S2);
    }

    private void AddFieldLabel(Transform parent, string text)
    {
        var t = AddText(parent, "Lbl_" + text, text,
            FontLabel, FontStyles.Bold, ColTextMd);
        t.characterSpacing = 6;
        AddFixedHeight(t.gameObject, 18f);
    }

    private void AddDivider(Transform parent)
    {
        var img = AddImage(parent, "Divider", ColDivider);
        AddFixedHeight(img.gameObject, 1f);
    }

    private void AddSpacer(Transform parent, float h)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        AddFixedHeight(go, h);
    }

    private void AddFixedHeight(GameObject go, float h)
    {
        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;
        le.flexibleHeight = 0f;
    }

    private Transform AddHorizontalRow(Transform parent, string name,
        float spacing, float height)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var hl = go.GetComponent<HorizontalLayoutGroup>();
        hl.padding = new RectOffset(0, 0, 0, 0);
        hl.spacing = spacing;
        hl.childAlignment = TextAnchor.MiddleLeft;
        hl.childControlWidth = true;
        hl.childForceExpandWidth = false;
        hl.childControlHeight = true;
        hl.childForceExpandHeight = true;
        var le = go.GetComponent<LayoutElement>();
        le.minHeight = height;
        le.preferredHeight = height;
        return go.transform;
    }

    private void Flex(Component c)
    {
        var le = c.gameObject.GetComponent<LayoutElement>()
                  ?? c.gameObject.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;
    }

    private void SetOutline(GameObject go, Color color)
    {
        var ol = go.GetComponent<Outline>();
        if (color.a == 0)
        {
            if (ol != null) ol.enabled = false;
            return;
        }
        if (ol == null) ol = go.AddComponent<Outline>();
        ol.enabled = true;
        ol.effectColor = color;
        ol.effectDistance = new Vector2(1.5f, -1.5f);
    }

    private void SetOutlineThickness(GameObject go, float thickness)
    {
        var ol = go.GetComponent<Outline>();
        if (ol == null) return;
        ol.effectDistance = new Vector2(thickness, -thickness);
    }

    private void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static Color GetContrastTextColor(Color bg)
    {
        float luminance = (0.299f * bg.r) + (0.587f * bg.g) + (0.114f * bg.b);
        return luminance > 0.55f ? new Color(0.08f, 0.09f, 0.11f, 1f) : Color.white;
    }

    private static void SetIconGraphicColor(Transform root, Color color)
    {
        Transform icon = root.Find("IconGraphic");
        if (icon == null) return;
        Image img = icon.GetComponent<Image>();
        if (img != null) img.color = color;
    }

    private static Sprite GetIconSprite(NoteIcon icon)
    {
        if (iconSpriteCache.TryGetValue(icon, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 white = new Color32(255, 255, 255, 255);
        Color32[] pixels = new Color32[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;
        tex.SetPixels32(pixels);

        switch (icon)
        {
            case NoteIcon.Note:
                DrawRect(tex, 16, 10, 34, 44, white, 3);
                DrawLine(tex, 23, 40, 43, 40, white, 3);
                DrawLine(tex, 23, 31, 43, 31, white, 3);
                DrawLine(tex, 23, 22, 37, 22, white, 3);
                break;
            case NoteIcon.Star:
                DrawStar(tex, 32, 32, 24f, 10f, white, 3);
                break;
            case NoteIcon.Alarm:
                DrawCircle(tex, 32, 30, 18, white, 3);
                DrawLine(tex, 32, 30, 32, 42, white, 3);
                DrawLine(tex, 32, 30, 43, 24, white, 3);
                DrawLine(tex, 18, 48, 10, 54, white, 3);
                DrawLine(tex, 46, 48, 54, 54, white, 3);
                DrawLine(tex, 22, 10, 16, 4, white, 3);
                DrawLine(tex, 42, 10, 48, 4, white, 3);
                break;
            case NoteIcon.Heart:
                DrawPolyline(tex, white, 3,
                    new Vector2Int(32, 13), new Vector2Int(12, 31), new Vector2Int(11, 45),
                    new Vector2Int(20, 52), new Vector2Int(32, 44), new Vector2Int(44, 52),
                    new Vector2Int(53, 45), new Vector2Int(52, 31), new Vector2Int(32, 13));
                break;
            case NoteIcon.Shopping:
                DrawLine(tex, 12, 45, 18, 45, white, 3);
                DrawLine(tex, 18, 45, 24, 20, white, 3);
                DrawRect(tex, 24, 20, 28, 20, white, 3);
                DrawCircle(tex, 28, 12, 4, white, 3);
                DrawCircle(tex, 48, 12, 4, white, 3);
                break;
            case NoteIcon.Work:
                DrawRect(tex, 12, 18, 40, 28, white, 3);
                DrawRect(tex, 25, 46, 14, 8, white, 3);
                DrawLine(tex, 12, 34, 52, 34, white, 3);
                break;
            case NoteIcon.Study:
                DrawLine(tex, 32, 15, 32, 50, white, 3);
                DrawPolyline(tex, white, 3,
                    new Vector2Int(32, 18), new Vector2Int(14, 23), new Vector2Int(14, 50), new Vector2Int(32, 45));
                DrawPolyline(tex, white, 3,
                    new Vector2Int(32, 18), new Vector2Int(50, 23), new Vector2Int(50, 50), new Vector2Int(32, 45));
                break;
            case NoteIcon.Home:
                DrawPolyline(tex, white, 3,
                    new Vector2Int(10, 30), new Vector2Int(32, 52), new Vector2Int(54, 30));
                DrawRect(tex, 17, 12, 30, 25, white, 3);
                DrawRect(tex, 29, 12, 8, 14, white, 3);
                break;
            default:
                DrawCircle(tex, 32, 32, 18, white, 3);
                break;
        }

        tex.Apply(false, true);
        sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 64f);
        iconSpriteCache[icon] = sprite;
        return sprite;
    }

    private static void SetPixelSafe(Texture2D tex, int x, int y, Color32 color)
    {
        if (x < 0 || x >= 64 || y < 0 || y >= 64) return;
        tex.SetPixel(x, y, color);
    }

    private static void DrawPoint(Texture2D tex, int x, int y, Color32 color, int thickness)
    {
        int r = Mathf.Max(1, thickness) / 2;
        for (int yy = -r; yy <= r; yy++)
            for (int xx = -r; xx <= r; xx++)
                SetPixelSafe(tex, x + xx, y + yy, color);
    }

    private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color32 color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            DrawPoint(tex, x0, y0, color, thickness);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawRect(Texture2D tex, int x, int y, int w, int h, Color32 color, int thickness)
    {
        DrawLine(tex, x, y, x + w, y, color, thickness);
        DrawLine(tex, x + w, y, x + w, y + h, color, thickness);
        DrawLine(tex, x + w, y + h, x, y + h, color, thickness);
        DrawLine(tex, x, y + h, x, y, color, thickness);
    }

    private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color32 color, int thickness)
    {
        int steps = 96;
        Vector2Int prev = new Vector2Int(cx + radius, cy);
        for (int i = 1; i <= steps; i++)
        {
            float a = (Mathf.PI * 2f * i) / steps;
            Vector2Int next = new Vector2Int(
                Mathf.RoundToInt(cx + Mathf.Cos(a) * radius),
                Mathf.RoundToInt(cy + Mathf.Sin(a) * radius));
            DrawLine(tex, prev.x, prev.y, next.x, next.y, color, thickness);
            prev = next;
        }
    }

    private static void DrawPolyline(Texture2D tex, Color32 color, int thickness, params Vector2Int[] points)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(tex, points[i].x, points[i].y, points[i + 1].x, points[i + 1].y, color, thickness);
        }
    }

    private static void DrawStar(Texture2D tex, int cx, int cy, float outer, float inner, Color32 color, int thickness)
    {
        Vector2Int[] points = new Vector2Int[11];
        for (int i = 0; i < 10; i++)
        {
            float radius = (i % 2 == 0) ? outer : inner;
            float angle = Mathf.Deg2Rad * (-90f + i * 36f);
            points[i] = new Vector2Int(
                Mathf.RoundToInt(cx + Mathf.Cos(angle) * radius),
                Mathf.RoundToInt(cy + Mathf.Sin(angle) * radius));
        }
        points[10] = points[0];
        DrawPolyline(tex, color, thickness, points);
    }

    private static Color LightenColor(Color c, float amount)
    {
        return new Color(
            Mathf.Clamp01(c.r + amount),
            Mathf.Clamp01(c.g + amount),
            Mathf.Clamp01(c.b + amount),
            c.a);
    }

    private static Color DarkenColor(Color c, float amount)
    {
        return new Color(
            Mathf.Clamp01(c.r - amount),
            Mathf.Clamp01(c.g - amount),
            Mathf.Clamp01(c.b - amount),
            c.a);
    }
}
