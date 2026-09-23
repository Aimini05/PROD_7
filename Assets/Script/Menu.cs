using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows.Speech;

public class Menu : MonoBehaviour
{

    [System.Serializable]
    public class MenuOption
    {
        public string lable;
        public UnityEngine.Events.UnityEvent onSelect;
    }

    public List<MenuOption> options = new List<MenuOption>();
    private int currentIndex = 0;
    private bool menuActive = true; 

    
    void Start()
    {
        // Announce the menu itself first, then the first item
        Speech.Interrupt("Main menu");
        Speech.Queue(options[currentIndex].lable);
    }

    
    void Update()
    {
        if (!menuActive) return;

        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            Move(1);
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            Move(-1);
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            Activate();
    }

    void Move(int direction)
    {
        currentIndex = (currentIndex + direction + options.Count) % options.Count;
        Speech.Interrupt(options[currentIndex].lable);
        // play a soft "tick" sound here too, non-speech redundancy
    }
    void Activate()
    {
        var chosen = options[currentIndex];
        Speech.Interrupt(chosen.lable + "selected");
        chosen.onSelect.Invoke();
    }
}
