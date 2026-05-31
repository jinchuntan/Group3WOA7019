using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    public List<NoteData> notes = new List<NoteData>();

    // Fired whenever the note list changes so the UI / AR layers can refresh.
    public event Action OnNotesChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        notes = NoteStorage.LoadNotes();

        // Backfill defaults for notes loaded from old JSON files that did
        // not have priority / colour / icon yet. We only normalise nulls
        // (so the JSON stays valid) without injecting any specific user
        // styling - the StyleCatalog will pick a fallback colour at render
        // time when colorLabelId is empty.
        for (int i = 0; i < notes.Count; i++)
        {
            NoteData n = notes[i];
            if (n.colorLabelId == null)
            {
                n.colorLabelId = string.Empty;
            }
            // priority defaults to 0 (Low) and icon to 0 (None) when missing,
            // which is fine - both already have safe rendering paths.
        }
    }

    public NoteData AddNote(string title, string content)
    {
        NoteData newNote = new NoteData(title, content);
        notes.Add(newNote);
        SaveNotes();

        Debug.Log("Note added: " + newNote.noteId);
        RaiseChanged();
        return newNote;
    }

    public void EditNote(string noteId, string newTitle, string newContent)
    {
        NoteData note = FindNoteById(noteId);

        if (note == null)
        {
            Debug.LogWarning("Edit failed. Note not found: " + noteId);
            return;
        }

        note.title = newTitle;
        note.content = newContent;
        note.updatedAt = DateTime.Now.ToString();

        SaveNotes();
        Debug.Log("Note edited: " + noteId);
        RaiseChanged();
    }

    public void DeleteNote(string noteId)
    {
        NoteData note = FindNoteById(noteId);

        if (note == null)
        {
            Debug.LogWarning("Delete failed. Note not found: " + noteId);
            return;
        }

        DeleteVoiceFileIfExists(note.voiceNotePath);
        notes.Remove(note);
        SaveNotes();

        Debug.Log("Note deleted: " + noteId);
        RaiseChanged();
    }

    public void ToggleNoteVisibility(string noteId)
    {
        NoteData note = FindNoteById(noteId);

        if (note == null)
        {
            Debug.LogWarning("Visibility toggle failed. Note not found: " + noteId);
            return;
        }

        note.isVisible = !note.isVisible;
        note.updatedAt = DateTime.Now.ToString();

        SaveNotes();
        Debug.Log("Note visibility changed to: " + note.isVisible);
        RaiseChanged();
    }

    public void AddChecklistItem(string noteId, string itemText)
    {
        NoteData note = FindNoteById(noteId);

        if (note == null)
        {
            Debug.LogWarning("Add checklist failed. Note not found: " + noteId);
            return;
        }

        if (string.IsNullOrWhiteSpace(itemText))
        {
            Debug.LogWarning("Checklist item cannot be empty.");
            return;
        }

        note.checklistItems.Add(new ChecklistItem(itemText));
        note.updatedAt = DateTime.Now.ToString();

        SaveNotes();
        Debug.Log("Checklist item added to note: " + noteId);
        RaiseChanged();
    }

    public void ToggleChecklistItem(string noteId, int itemIndex)
    {
        NoteData note = FindNoteById(noteId);

        if (note == null)
        {
            Debug.LogWarning("Checklist toggle failed. Note not found: " + noteId);
            return;
        }

        if (itemIndex < 0 || itemIndex >= note.checklistItems.Count)
        {
            Debug.LogWarning("Invalid checklist item index: " + itemIndex);
            return;
        }

        note.checklistItems[itemIndex].isCompleted =
            !note.checklistItems[itemIndex].isCompleted;

        note.updatedAt = DateTime.Now.ToString();

        SaveNotes();
        Debug.Log(
            "Checklist item toggled for note: " + noteId +
            " | Completed: " + note.checklistItems[itemIndex].isCompleted
        );
        RaiseChanged();
    }

    public void RemoveChecklistItem(string noteId, int itemIndex)
    {
        NoteData note = FindNoteById(noteId);

        if (note == null)
        {
            Debug.LogWarning("Checklist remove failed. Note not found: " + noteId);
            return;
        }

        if (itemIndex < 0 || itemIndex >= note.checklistItems.Count)
        {
            Debug.LogWarning("Invalid checklist item index: " + itemIndex);
            return;
        }

        string removedText = note.checklistItems[itemIndex].itemText;
        note.checklistItems.RemoveAt(itemIndex);
        note.updatedAt = DateTime.Now.ToString();

        SaveNotes();
        Debug.Log("Checklist item removed from note: " + noteId + " | Item: " + removedText);
        RaiseChanged();
    }

    // ---- Custom styling API ------------------------------------------------

    public void SetNotePriority(string noteId, NotePriority priority)
    {
        NoteData note = FindNoteById(noteId);
        if (note == null) { Debug.LogWarning("SetPriority failed: " + noteId); return; }

        note.Priority = priority;
        note.updatedAt = DateTime.Now.ToString();
        SaveNotes();
        Debug.Log("Note priority set: " + noteId + " -> " + priority);
        RaiseChanged();
    }

    public void SetNoteColor(string noteId, string colorLabelId)
    {
        NoteData note = FindNoteById(noteId);
        if (note == null) { Debug.LogWarning("SetColor failed: " + noteId); return; }

        note.colorLabelId = colorLabelId;
        note.updatedAt = DateTime.Now.ToString();
        SaveNotes();
        Debug.Log("Note color set: " + noteId + " -> " + colorLabelId);
        RaiseChanged();
    }

    public void SetNoteIcon(string noteId, NoteIcon icon)
    {
        NoteData note = FindNoteById(noteId);
        if (note == null) { Debug.LogWarning("SetIcon failed: " + noteId); return; }

        note.Icon = icon;
        note.updatedAt = DateTime.Now.ToString();
        SaveNotes();
        Debug.Log("Note icon set: " + noteId + " -> " + icon);
        RaiseChanged();
    }

    // -----------------------------------------------------------------------

    public NoteData FindNoteById(string noteId)
    {
        return notes.Find(note => note.noteId == noteId);
    }

    public List<NoteData> GetAllNotes()
    {
        return notes;
    }

    public void SaveNotes()
    {
        NoteStorage.SaveNotes(notes);
    }

    public void ClearAllNotes()
    {
        notes.Clear();
        NoteStorage.ClearNotes();
        Debug.Log("All notes cleared.");
        RaiseChanged();
    }

    private void RaiseChanged()
    {
        if (OnNotesChanged != null)
        {
            OnNotesChanged();
        }
    }

    public void SetNoteReminder(string noteId, string dateTimeString)
    {
        NoteData note = FindNoteById(noteId);
        if (note == null) { Debug.LogWarning("SetNoteReminder failed: " + noteId); return; }

        note.reminderTime = dateTimeString;
        note.reminderDismissed = false; // Reset dismissal state for the new time
        note.updatedAt = System.DateTime.Now.ToString();
        SaveNotes();
        Debug.Log("Reminder scheduled for Note: " + noteId + " at " + dateTimeString);
        RaiseChanged();
    }

    public void DismissNoteReminder(string noteId)
    {
        NoteData note = FindNoteById(noteId);
        if (note != null)
        {
            note.reminderDismissed = true;
            SaveNotes();
            RaiseChanged();
        }
    }

    public void SetNoteVoicePath(string noteId, string voicePath)
    {
        NoteData note = FindNoteById(noteId);
        if (note == null) { Debug.LogWarning("SetNoteVoicePath failed: " + noteId); return; }

        note.voiceNotePath = voicePath ?? string.Empty;
        note.updatedAt = DateTime.Now.ToString();
        SaveNotes();
        Debug.Log("Voice note saved for note: " + noteId);
        RaiseChanged();
    }

    public void RemoveNoteVoice(string noteId)
    {
        NoteData note = FindNoteById(noteId);
        if (note == null) { Debug.LogWarning("RemoveNoteVoice failed: " + noteId); return; }

        DeleteVoiceFileIfExists(note.voiceNotePath);
        note.voiceNotePath = string.Empty;
        note.updatedAt = DateTime.Now.ToString();
        SaveNotes();
        Debug.Log("Voice note removed for note: " + noteId);
        RaiseChanged();
    }

    private void DeleteVoiceFileIfExists(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Failed to delete voice note file: " + ex.Message);
        }
    }
}
