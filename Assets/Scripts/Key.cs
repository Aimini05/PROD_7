using UnityEngine;

namespace AmazingMaze.Player
{
    public sealed class Key : Interactable
    {
        [Header("Audio")]
        public AudioSource audioSource;
        public AudioClip shimmerSound;  // loopas tills nyckeln plockas upp
        public AudioClip pickupSound;   // spelas en gång

        private bool pickedUp = false;

        void Start()
        {
            if (audioSource != null && shimmerSound != null)
            {
                audioSource.clip = shimmerSound;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        public override bool Interact(GridPlayerController player)
        {
            if (pickedUp || player == null) return false;

            PlayerInventory inventory = player.GetComponentInParent<PlayerInventory>();
            if (inventory == null) inventory = player.GetComponentInChildren<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogWarning("Key: hittar ingen PlayerInventory på spelaren.");
                return false;
            }

            pickedUp = true;
            inventory.AddKey();
            PlaytestLogger.KeyCollected(transform, player.transform.position, inventory.keyCount);

            float destroyDelay = 0f;
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.loop = false;

                if (pickupSound != null)
                {
                    audioSource.PlayOneShot(pickupSound);
                    destroyDelay = pickupSound.length;
                }
            }

            foreach (Renderer r in GetComponentsInChildren<Renderer>())
                r.enabled = false;
            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            Destroy(gameObject, destroyDelay);
            return true;
        }
    }
}
