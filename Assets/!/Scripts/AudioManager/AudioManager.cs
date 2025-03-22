using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance; // Static instance variable

    [SerializeField] private AudioSource _musicSource;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Ensure the game object persists across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy the duplicate game object
        }
    }

    private void Start()
    {
        if (_musicSource != null && !_musicSource.isPlaying)
        {
            _musicSource.Play();
        }
    }
}