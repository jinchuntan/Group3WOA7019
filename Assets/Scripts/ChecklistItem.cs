using System;

[Serializable]
public class ChecklistItem
{
    public string itemText;
    public bool isCompleted;

    public ChecklistItem(string itemText)
    {
        this.itemText = itemText;
        this.isCompleted = false;
    }
}