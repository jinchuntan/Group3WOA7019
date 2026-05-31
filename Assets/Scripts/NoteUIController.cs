using TMPro;
using UnityEngine;
using System;
using System.Collections.Generic;

public class NoteUIController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField titleInput;
    [SerializeField] private TMP_InputField contentInput;
    [SerializeField] private TMP_InputField checklistInput;
    [SerializeField] private TMP_InputField reminderInput;

    private string selectedNoteId;

    public void AddNoteFromUI()
    {
        if (titleInput == null || contentInput == null)
        {
            Debug.LogWarning("Input fields are not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(titleInput.text))
        {
            Debug.LogWarning("Title cannot be empty.");
            return;
        }

        NoteData note = NoteManager.Instance.AddNote(
            titleInput.text,
            contentInput.text
        );

        selectedNoteId = note.noteId;

        Debug.Log("UI: Added note with ID: " + selectedNoteId);
    }

    public void EditSelectedNote()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            Debug.LogWarning("UI: No note selected for editing. Add a note first.");
            return;
        }

        NoteManager.Instance.EditNote(
            selectedNoteId,
            titleInput.text,
            contentInput.text
        );

        Debug.Log("UI: Edited note with ID: " + selectedNoteId);
    }

    public void DeleteSelectedNote()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            Debug.LogWarning("UI: No note selected for deletion. Add a note first.");
            return;
        }

        NoteManager.Instance.DeleteNote(selectedNoteId);

        Debug.Log("UI: Deleted note with ID: " + selectedNoteId);

        selectedNoteId = "";
        ClearInputs();
    }

    public void ToggleSelectedNoteVisibility()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            Debug.LogWarning("UI: No note selected for visibility toggle. Add a note first.");
            return;
        }

        NoteManager.Instance.ToggleNoteVisibility(selectedNoteId);

        NoteData note = NoteManager.Instance.FindNoteById(selectedNoteId);

        if (note != null)
        {
            Debug.Log("UI: Note visibility is now: " + note.isVisible);
        }
    }

    public void AddChecklistItemFromUI()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            Debug.LogWarning("UI: No note selected for checklist item. Add a note first.");
            return;
        }

        if (checklistInput == null)
        {
            Debug.LogWarning("Checklist input field is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(checklistInput.text))
        {
            Debug.LogWarning("Checklist item cannot be empty.");
            return;
        }

        NoteManager.Instance.AddChecklistItem(
            selectedNoteId,
            checklistInput.text
        );

        Debug.Log("UI: Added checklist item to note with ID: " + selectedNoteId);

        checklistInput.text = "";
    }

    public void ToggleFirstChecklistItem()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            Debug.LogWarning("UI: No note selected for checklist toggle. Add a note first.");
            return;
        }

        NoteData note = NoteManager.Instance.FindNoteById(selectedNoteId);

        if (note == null)
        {
            Debug.LogWarning("UI: Selected note could not be found.");
            return;
        }

        if (note.checklistItems.Count == 0)
        {
            Debug.LogWarning("UI: No checklist item available. Add a checklist item first.");
            return;
        }

        NoteManager.Instance.ToggleChecklistItem(selectedNoteId, 0);

        Debug.Log("UI: Toggled first checklist item for note with ID: " + selectedNoteId);
    }

public void SetReminderFromUI()
    {
        if (string.IsNullOrEmpty(selectedNoteId))
        {
            List<NoteData> allNotes = NoteManager.Instance.GetAllNotes();
            if (allNotes != null && allNotes.Count > 0)
            {
                selectedNoteId = allNotes[allNotes.Count - 1].noteId;
            }
            else
            {
                NoteData dummy = NoteManager.Instance.AddNote("UI Input Test Note", "Created automatically.");
                selectedNoteId = dummy.noteId;
            }
        }

        string timeToSchedule = "";
        if (reminderInput != null && !string.IsNullOrWhiteSpace(reminderInput.text))
        {
            timeToSchedule = reminderInput.text;
            Debug.Log($"[UI] Using user custom time input: {timeToSchedule}");
        }
        else
        {
            DateTime targetTime = DateTime.Now.AddSeconds(10);
            timeToSchedule = targetTime.ToString("yyyy-MM-dd HH:mm:ss");
            if (reminderInput != null) reminderInput.text = timeToSchedule;
            Debug.Log($"[UI] Input empty! Defaulting to a quick 10-second alarm: {timeToSchedule}");
        }

        NoteManager.Instance.SetNoteReminder(selectedNoteId, timeToSchedule);
    }

    private void ClearInputs()
    {
        titleInput.text = "";
        contentInput.text = "";
        checklistInput.text = "";
        if (reminderInput != null) reminderInput.text = "";
    }
}