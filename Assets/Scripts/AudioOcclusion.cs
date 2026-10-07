using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioLowPassFilter))]
public class AudioOcclusion : MonoBehaviour
{
    [Header("Wall Detection")]
    public Transform listener;
    public LayerMask wallLayer;

    [Header("Occluded Sound")]
    [Range(0f, 1f)]
    public float volumeMultiplier = 0.3f;

    public float muffledCutoff = 2000f;

    [Header("Transition")]
    public float transitionSpeed = 5f;

    private AudioSource audioSource;
    private AudioLowPassFilter lowPass;
    private float originalVolume;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        lowPass = GetComponent<AudioLowPassFilter>();

        originalVolume = audioSource.volume;
    }

    void Update()
    {
        if (listener == null)
            return;

        Vector3 direction = listener.position - transform.position;
        float distance = direction.magnitude;

        bool wallBetween = Physics.Raycast(
            transform.position,
            direction.normalized,
            distance,
            wallLayer
        );

        float targetVolume = wallBetween
            ? originalVolume * volumeMultiplier
            : originalVolume;

        float targetCutoff = wallBetween
            ? muffledCutoff
            : 22000f;

        audioSource.volume = Mathf.Lerp(
            audioSource.volume,
            targetVolume,
            transitionSpeed * Time.deltaTime
        );

        lowPass.cutoffFrequency = Mathf.Lerp(
            lowPass.cutoffFrequency,
            targetCutoff,
            transitionSpeed * Time.deltaTime
        );
    }
}