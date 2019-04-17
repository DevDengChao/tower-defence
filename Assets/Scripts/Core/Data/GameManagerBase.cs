using System;
using Core.Utilities;
using UnityEngine;
using UnityEngine.Audio;

namespace Core.Data
{
    /// <inheritdoc />
    /// <summary>
    ///     Base game manager<br />
    ///     Controls each volume (master, sfx, background music) and the persistent of user record each scenes.
    /// </summary>
    public abstract class GameManagerBase<TGameManager, TDataStore> :
        PersistentSingleton<TGameManager> where TDataStore : GameDataStoreBase,
        new()
        where TGameManager : GameManagerBase<TGameManager, TDataStore>
    {
        /// <summary>
        ///     File name of saved game
        /// </summary>
        private const string KSavedGameFile = "save";

        /// <summary>
        ///     The serialization implementation for persistence
        /// </summary>
        private JsonSaver<TDataStore> _mDataSaver;

        /// <summary>
        ///     Reference to audio mixer for volume changing
        /// </summary>
        public AudioMixer gameMixer;

        /// <summary>
        ///     Master volume parameter on the mixer
        /// </summary>
        public string masterVolumeParameter;

        /// <summary>
        ///     The object used for persistence
        /// </summary>
        protected TDataStore MDataStore;

        /// <summary>
        ///     Music volume parameter on the mixer
        /// </summary>
        public string musicVolumeParameter;

        /// <summary>
        ///     SFX volume parameter on the mixer
        /// </summary>
        public string sfxVolumeParameter;

        /// <summary>
        ///     Retrieve volumes from data store
        /// </summary>
        public void GetVolumes(out float master, out float sfx, out float music)
        {
            master = MDataStore.masterVolume;
            sfx = MDataStore.sfxVolume;
            music = MDataStore.musicVolume;
        }

        /// <summary>
        ///     Set and persist game volumes
        /// </summary>
        public void SetVolumes(float master, float sfx, float music, bool save)
        {
            // Early out if no mixer set
            if (gameMixer == null) return;

            // Transform 0-1 into logarithmic -80-0
            if (masterVolumeParameter != null)
                gameMixer.SetFloat(masterVolumeParameter, LogarithmicDbTransform(Mathf.Clamp01(master)));

            if (sfxVolumeParameter != null)
                gameMixer.SetFloat(sfxVolumeParameter, LogarithmicDbTransform(Mathf.Clamp01(sfx)));

            if (musicVolumeParameter != null)
                gameMixer.SetFloat(musicVolumeParameter, LogarithmicDbTransform(Mathf.Clamp01(music)));

            if (!save) return;
            // Apply to save data too
            MDataStore.masterVolume = master;
            MDataStore.sfxVolume = sfx;
            MDataStore.musicVolume = music;
            SaveData();
        }

        /// <summary>
        ///     Load data
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            LoadData();
        }

        /// <summary>
        ///     Initialize volumes. We cannot change mixer params on awake
        /// </summary>
        protected void Start()
        {
            SetVolumes(MDataStore.masterVolume, MDataStore.sfxVolume, MDataStore.musicVolume, false);
        }

        /// <summary>
        ///     Set up persistence
        /// </summary>
        private void LoadData()
        {
            // If it is in Unity Editor use the standard JSON (human readable for debugging) otherwise encrypt it for deployed version
#if UNITY_EDITOR
            _mDataSaver = new JsonSaver<TDataStore>(KSavedGameFile);
#else
			_mDataSaver = new EncryptedJsonSaver<TDataStore>(KSavedGameFile);
#endif

            try
            {
                if (_mDataSaver.Load(out MDataStore)) return;
                MDataStore = new TDataStore();
                SaveData();
            }
            catch (Exception)
            {
                Debug.Log("Failed to load data, resetting");
                MDataStore = new TDataStore();
                SaveData();
            }
        }

        /// <summary>
        ///     Saves the gamme
        /// </summary>
        protected void SaveData()
        {
            _mDataSaver.Save(MDataStore);
        }

        /// <summary>
        ///     Transform volume from linear to logarithmic
        /// </summary>
        private static float LogarithmicDbTransform(float volume)
        {
            volume = Mathf.Log(89 * volume + 1) / Mathf.Log(90) * 80;
            return volume - 80;
        }
    }
}