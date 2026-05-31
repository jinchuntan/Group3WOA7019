using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class NoteStorage
{
    private static string FilePath
    {
        get
        {
            return Path.Combine(Application.persistentDataPath, "notes.json");
        }
    }

    public static void SaveNotes(List<NoteData> notes)
    {
        NoteListWrapper wrapper = new NoteListWrapper();
        wrapper.notes = notes;

        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(FilePath, json);

        Debug.Log("Notes saved to: " + FilePath);
    }

    public static List<NoteData> LoadNotes()
    {
        if (!File.Exists(FilePath))
        {
            Debug.Log("No saved notes found. Starting with empty note list.");
            return new List<NoteData>();
        }

        string json = File.ReadAllText(FilePath);
        NoteListWrapper wrapper = JsonUtility.FromJson<NoteListWrapper>(json);

        if (wrapper == null || wrapper.notes == null)
        {
            Debug.LogWarning("Saved notes file exists, but could not be read properly.");
            return new List<NoteData>();
        }

        Debug.Log("Notes loaded from: " + FilePath);
        return wrapper.notes;
    }

    public static void ClearNotes()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
            Debug.Log("Saved notes file deleted.");
        }
    }
}

[System.Serializable]
public class NoteListWrapper
{
    public List<NoteData> notes;
}