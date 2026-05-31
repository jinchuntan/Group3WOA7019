using UnityEngine;

// Centralised catalogue of the visual styles supported by AReminder notes.
//
// All UI / AR scripts ask this class for colours, icon glyphs and priority
// data so that customising the app's look-and-feel only requires editing
// one file. Values are kept simple (Color / string) on purpose so they can
// be safely shown both in 2D UI (TextMeshPro) and on the 3D AR sticky note.
public static class NoteStyleCatalog
{
    // ---- Colour labels ----------------------------------------------------
    // Each colour label has a stable id (saved to JSON) plus a human-readable
    // name and the actual Color used to tint the note background.
    public static readonly ColorLabel[] ColorLabels = new ColorLabel[]
    {
        new ColorLabel("yellow",  "Yellow",  new Color(1.00f, 0.93f, 0.55f)),
        new ColorLabel("pink",    "Pink",    new Color(1.00f, 0.78f, 0.85f)),
        new ColorLabel("blue",    "Blue",    new Color(0.69f, 0.85f, 1.00f)),
        new ColorLabel("green",   "Green",   new Color(0.74f, 0.93f, 0.78f)),
        new ColorLabel("purple",  "Purple",  new Color(0.84f, 0.78f, 0.98f)),
        new ColorLabel("orange",  "Orange",  new Color(1.00f, 0.80f, 0.55f)),
        new ColorLabel("white",   "White",   new Color(0.98f, 0.98f, 0.98f)),
        new ColorLabel("graphite","Graphite",new Color(0.32f, 0.34f, 0.40f)),
    };

    public static ColorLabel GetColorLabel(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return ColorLabels[0];
        }

        for (int i = 0; i < ColorLabels.Length; i++)
        {
            if (ColorLabels[i].id == id)
            {
                return ColorLabels[i];
            }
        }

        return ColorLabels[0];
    }

    // ---- Icons ------------------------------------------------------------
    // We use Basic-Latin glyphs (letters / digits) that are guaranteed to
    // exist in the default LiberationSans SDF font shipped with TextMeshPro.
    // Geometric / dingbat unicode characters (e.g. U+25C9, U+2605) are NOT
    // included in LiberationSans, so we deliberately avoid them to keep the
    // console free of "character not found" warnings. The visuals rely on
    // the colored circular icon button background + a single bold initial,
    // which reads cleanly at small sizes (Material Design style).
    public static string GetIconGlyph(NoteIcon icon)
    {
        switch (icon)
        {
            case NoteIcon.Note:     return "N";  // memo
            case NoteIcon.Star:     return "F";  // favorite (star)
            case NoteIcon.Alarm:    return "A";  // alarm
            case NoteIcon.Heart:    return "L";  // love
            case NoteIcon.Shopping: return "S";  // shopping
            case NoteIcon.Work:     return "W";  // work
            case NoteIcon.Study:    return "B";  // book
            case NoteIcon.Home:     return "H";  // home
            default:                 return string.Empty;
        }
    }

    // Per-icon accent color used to tint the round icon button.
    // Picked from the Material 500 palette for clarity & contrast.
    public static Color GetIconAccent(NoteIcon icon)
    {
        switch (icon)
        {
            case NoteIcon.Note:     return new Color(0.26f, 0.55f, 0.96f); // blue
            case NoteIcon.Star:     return new Color(0.98f, 0.75f, 0.18f); // amber
            case NoteIcon.Alarm:    return new Color(0.95f, 0.39f, 0.27f); // orange-red
            case NoteIcon.Heart:    return new Color(0.91f, 0.30f, 0.50f); // pink
            case NoteIcon.Shopping: return new Color(0.30f, 0.69f, 0.31f); // green
            case NoteIcon.Work:     return new Color(0.47f, 0.33f, 0.28f); // brown
            case NoteIcon.Study:    return new Color(0.40f, 0.23f, 0.72f); // deep purple
            case NoteIcon.Home:     return new Color(0.00f, 0.59f, 0.53f); // teal
            default:                 return new Color(0.45f, 0.50f, 0.55f); // slate
        }
    }

    public static string GetIconLabel(NoteIcon icon)
    {
        switch (icon)
        {
            case NoteIcon.None:     return "None";
            case NoteIcon.Note:     return "Note";
            case NoteIcon.Star:     return "Star";
            case NoteIcon.Alarm:    return "Alarm";
            case NoteIcon.Heart:    return "Heart";
            case NoteIcon.Shopping: return "Shopping";
            case NoteIcon.Work:     return "Work";
            case NoteIcon.Study:    return "Study";
            case NoteIcon.Home:     return "Home";
            default:                 return "";
        }
    }

    // ---- Priority ---------------------------------------------------------
    // The colours match Table 6 of the technical report.
    public static Color GetPriorityColor(NotePriority priority)
    {
        switch (priority)
        {
            case NotePriority.High:   return new Color(0.90f, 0.27f, 0.27f); // red
            case NotePriority.Medium: return new Color(0.95f, 0.75f, 0.20f); // amber
            case NotePriority.Low:    return new Color(0.30f, 0.72f, 0.42f); // green
            default:                  return Color.gray;
        }
    }

    public static string GetPriorityLabel(NotePriority priority)
    {
        switch (priority)
        {
            case NotePriority.High:   return "High";
            case NotePriority.Medium: return "Medium";
            case NotePriority.Low:    return "Low";
            default:                  return "Low";
        }
    }

    // Returns "!", "!!", "!!!" so the urgency is also visible to anyone
    // who relies on shape/contrast rather than colour alone (accessibility).
    public static string GetPriorityMarker(NotePriority priority)
    {
        switch (priority)
        {
            case NotePriority.High:   return "!!!";
            case NotePriority.Medium: return "!!";
            case NotePriority.Low:    return "!";
            default:                  return "";
        }
    }

    // Picks black or white text so that whatever colour the note uses the
    // title remains readable. Based on the standard luminance formula.
    public static Color GetReadableTextColor(Color background)
    {
        float luminance = (0.299f * background.r) + (0.587f * background.g) + (0.114f * background.b);
        return luminance > 0.6f ? new Color(0.10f, 0.12f, 0.16f) : Color.white;
    }

    public struct ColorLabel
    {
        public string id;
        public string displayName;
        public Color  color;

        public ColorLabel(string id, string displayName, Color color)
        {
            this.id = id;
            this.displayName = displayName;
            this.color = color;
        }
    }
}
