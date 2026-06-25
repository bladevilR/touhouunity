using UnityEngine;

namespace TouhouMigration.Runtime.Audio
{
    // The E7 audio playback shell (Godot AudioManager): a looping BGM AudioSource that auto-plays the
    // active scene's track on start, plus name-addressed one-shot SFX with pitch jitter. Routing comes
    // from MigrationAudioCatalog; the clips are loaded from Resources and are left to Codex/image2 — every
    // play is null-safe (no clip -> silent no-op), so scenes run clean before the audio art lands.
    public sealed class MigrationAudioManager : MonoBehaviour
    {
        [SerializeField] private float bgmVolume = 0.55f;
        [SerializeField] private float sfxVolume = 0.9f;
        [SerializeField] private bool autoPlaySceneBgm = true;

        private readonly MigrationAudioCatalog catalog = new MigrationAudioCatalog();
        private AudioSource bgmSource;
        private string currentTrack = string.Empty;

        public string CurrentTrack => currentTrack;
        public MigrationAudioCatalog Catalog => catalog;

        private void Awake()
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
        }

        private void Start()
        {
            if (autoPlaySceneBgm)
            {
                PlayBgmForScene(gameObject.scene.name);
            }
        }

        // Switch BGM to a track name; a no-op if already on it. Unknown track or missing clip -> silent.
        public void PlayBgm(string trackName)
        {
            if (!string.IsNullOrEmpty(trackName) && trackName == currentTrack)
            {
                return;
            }

            string key = catalog.GetBgmKey(trackName);
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            AudioClip clip = Resources.Load<AudioClip>(key);
            currentTrack = trackName;
            if (clip == null || bgmSource == null)
            {
                return; // clip not yet provided (Codex/image2) — routing is recorded, playback is silent.
            }

            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        public void StopBgm()
        {
            currentTrack = string.Empty;
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        // Play the BGM mapped to a scene, or stop if the scene has none.
        public void PlayBgmForScene(string sceneName)
        {
            string track = catalog.ResolveSceneTrack(sceneName);
            if (string.IsNullOrEmpty(track))
            {
                StopBgm();
                return;
            }

            PlayBgm(track);
        }

        // Fire a one-shot SFX by name (UI/feedback). Missing clip -> silent no-op.
        public void PlaySfx(string sfxName)
        {
            string key = catalog.ResolveSfxKey(sfxName, n => Random.Range(0, n));
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            AudioClip clip = Resources.Load<AudioClip>(key);
            if (clip == null)
            {
                return;
            }

            AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, sfxVolume);
        }
    }
}
