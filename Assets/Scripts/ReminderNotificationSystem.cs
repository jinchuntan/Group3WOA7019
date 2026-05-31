using System;
using System.Collections.Generic;
using UnityEngine;

public class ReminderNotificationSystem : MonoBehaviour
{
    [SerializeField] private float checkInterval = 2.0f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            CheckActiveReminders();
        }
    }

    private void CheckActiveReminders()
    {
        if (NoteManager.Instance == null) return;

        List<NoteData> activeNotes = NoteManager.Instance.GetAllNotes();

        foreach (NoteData note in activeNotes)
        {
            if (string.IsNullOrEmpty(note.reminderTime) || note.reminderDismissed)
                continue;

            if (DateTime.TryParse(note.reminderTime, out DateTime targetDeadline))
            {
                TimeSpan timeRemaining = targetDeadline - DateTime.Now;

                if (note.IsOverdue())
                {
                    TriggerOverdueAlert(note);
                }
                else
                {
                    TriggerDueSoonWarning(note, timeRemaining);
                }
            }
            else
            {
                Debug.LogError($"[SYSTEM] Failed to parse reminder time string format for note: {note.title}");
            }
        }
    }

    private void TriggerOverdueAlert(NoteData note)
    {
        Debug.LogWarning($"[NOTIFICATION PUSH / ALARM] Note: '{note.title}' is overdue! Deadline was at {note.reminderTime}.");

        NoteManager.Instance.DismissNoteReminder(note.noteId);
    }

    private void TriggerDueSoonWarning(NoteData note, TimeSpan timeRemaining)
    {
        int hours = Mathf.Max(0, timeRemaining.Hours + (timeRemaining.Days * 24));
        int minutes = Mathf.Max(0, timeRemaining.Minutes);
        int seconds = Mathf.Max(0, timeRemaining.Seconds);

        string countdownStr;
        if (hours > 0)
        {
            countdownStr = $"{hours}h {minutes}m {seconds}s";
        }
        else if (minutes > 0)
        {
            countdownStr = $"{minutes}m {seconds}s";
        }
        else
        {
            countdownStr = $"{seconds}s";
        }

        Debug.Log($"[SYSTEM Ticker] Note: '{note.title}' — Time Remaining: {countdownStr}");
    }
}