using UnityEngine;
using System;

// Developer-only keyboard tester used to verify backend CRUD without UI.
// Press A/E/C/T/H/D/X/L in Play Mode and watch the Console.
//
// All test strings below are SerializeFields so QA / reviewers can change
// them (or empty them) from the Inspector without touching the script.
// They are NOT preset content for end users - this component should be
// disabled or removed before shipping.
public class NoteManagerTester : MonoBehaviour
{
    [Header("Test data (editable from Inspector)")]
    [SerializeField] private string addTitle      = "Backend Test Note";
    [SerializeField] private string addContent    = "This note was created by pressing A.";
    [SerializeField] private string editTitle     = "Edited Backend Test Note";
    [SerializeField] private string editContent   = "This note was edited by pressing E.";
    [SerializeField] private string checklistText = "First checklist item";

    private string testNoteId;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            NoteData note = NoteManager.Instance.AddNote(addTitle, addContent);
            testNoteId = note.noteId;
            Debug.Log("TEST: Created note with ID: " + testNoteId);

            System.DateTime targetTime = System.DateTime.Now.AddSeconds(10);
            string formattedTime = targetTime.ToString("yyyy-MM-dd HH:mm:ss");
            NoteManager.Instance.SetNoteReminder(testNoteId, formattedTime);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (string.IsNullOrEmpty(testNoteId))
            {
                Debug.LogWarning("TEST: No test note selected. Press A first.");
                return;
            }

            NoteManager.Instance.EditNote(testNoteId, editTitle, editContent);

            Debug.Log("TEST: Edited note with ID: " + testNoteId);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (string.IsNullOrEmpty(testNoteId))
            {
                Debug.LogWarning("TEST: No test note selected. Press A first.");
                return;
            }

            NoteManager.Instance.AddChecklistItem(testNoteId, checklistText);

            Debug.Log("TEST: Added checklist item to note with ID: " + testNoteId);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (string.IsNullOrEmpty(testNoteId))
            {
                Debug.LogWarning("TEST: No test note selected. Press A first.");
                return;
            }

            NoteManager.Instance.ToggleChecklistItem(testNoteId, 0);

            Debug.Log("TEST: Toggled checklist item for note with ID: " + testNoteId);
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (string.IsNullOrEmpty(testNoteId))
            {
                Debug.LogWarning("TEST: No test note selected. Press A first.");
                return;
            }

            NoteManager.Instance.ToggleNoteVisibility(testNoteId);

            NoteData note = NoteManager.Instance.FindNoteById(testNoteId);

            if (note != null)
            {
                Debug.Log("TEST: Note visibility is now: " + note.isVisible);
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            if (string.IsNullOrEmpty(testNoteId))
            {
                Debug.LogWarning("TEST: No test note selected. Press A first.");
                return;
            }

            NoteManager.Instance.DeleteNote(testNoteId);

            Debug.Log("TEST: Deleted note with ID: " + testNoteId);

            testNoteId = "";
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            NoteManager.Instance.ClearAllNotes();

            testNoteId = "";

            Debug.Log("TEST: Cleared all notes.");
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("TEST: Current number of notes: " + NoteManager.Instance.GetAllNotes().Count);

            foreach (NoteData note in NoteManager.Instance.GetAllNotes())
            {
                Debug.Log(
                    "NOTE: " + note.noteId +
                    " | Title: " + note.title +
                    " | Content: " + note.content +
                    " | Visible: " + note.isVisible +
                    " | Checklist Count: " + note.checklistItems.Count
                );
            }
        }
    }
}
