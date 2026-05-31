using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceNote : MonoBehaviour
{
    [SerializeField] private GameObject notePrefab;

    // Optional fallback label shown when the user has not given the note a
    // title yet. Left empty by default so we do not inject any preset
    // content into the user's data; the UI's NoteObject will render
    // "(untitled)" for empty titles.
    [SerializeField] private string emptyTitleFallback = "";
    [SerializeField] private string emptyContentFallback = "";

    private ARRaycastManager raycastManager;
    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    private void Update()
    {
        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            GameObject noteObject = Instantiate(notePrefab, pose.position, pose.rotation);

            // Create the note with empty fields by default so the user is
            // not bombarded with hard-coded "New Note" placeholders.
            NoteData newNote = NoteManager.Instance.AddNote(
                emptyTitleFallback,
                emptyContentFallback
            );

            NoteObject noteComponent = noteObject.GetComponent<NoteObject>();

            if (noteComponent != null)
            {
                noteComponent.Initialize(newNote.noteId);
            }
            else
            {
                Debug.LogWarning("NoteObject script is missing from NotePrefab.");
            }
        }
    }
}
