using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int keyCount = 0;

    public bool HasKey => keyCount > 0;

    public void AddKey() => keyCount++;

    // Returnerar true om det fanns en nyckel
    public bool UseKey(bool consume = true)
    {
        if (keyCount <= 0) return false;
        if (consume) keyCount--;
        return true;
    }
}