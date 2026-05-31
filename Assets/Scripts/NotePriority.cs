// Priority levels used by AReminder notes.
//
// The colour mapping (High = Red, Medium = Yellow, Low = Green) mirrors the
// "Priority Colour System" described in the technical report (section 4.9 / Table 6).
// The numeric values are kept stable so the integer is safe to serialise to JSON.
public enum NotePriority
{
    Low = 0,
    Medium = 1,
    High = 2,
}
