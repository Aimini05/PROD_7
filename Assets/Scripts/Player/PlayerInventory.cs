using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public bool hasKey = false;

    // Anropas av dörren när nyckeln används
    public void UseKey()
    {
        hasKey = false;
    }
}
