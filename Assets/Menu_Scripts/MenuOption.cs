using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MenuOption
{
    public string label;              // just for your own reference in the Inspector
    public AudioClip focusClip;       // plays when this item is navigated to
    public AudioClip activateClip;    // plays when this item is selected/confirmed
    public UnityEvent onSelect;       // what happens when activated
}