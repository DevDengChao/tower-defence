using Core.UI;
using TowerDefense.Game;
using UnityEngine.UI;

namespace TowerDefense.UI
{
    /// <inheritdoc />
    /// <summary>
    /// Simple options menu for setting volumes 
    /// </summary>
    public class OptionsMenu : SimpleMainMenuPage
    {
        public Slider masterSlider;

        public Slider sfxSlider;

        public Slider musicSlider;

        /// <summary>
        /// Event fired when sliders change
        /// </summary>
        public void UpdateVolumes()
        {
            GetSliderVolumes(out var master, out var sfx, out var music);

            if (GameManager.InstanceExists)
            {
                GameManager.Instance.SetVolumes(master, sfx, music, false);
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Set initial slider values
        /// </summary>
        public override void Show()
        {
            if (GameManager.InstanceExists)
            {
                GameManager.Instance.GetVolumes(out var master, out var sfx, out var music);

                if (masterSlider != null) masterSlider.value = master;

                if (sfxSlider != null) sfxSlider.value = sfx;

                if (musicSlider != null) musicSlider.value = music;
            }

            base.Show();
        }

        /// <inheritdoc />
        /// <summary>
        /// Persist volumes to data store
        /// </summary>
        public override void Hide()
        {
            GetSliderVolumes(out var master, out var sfx, out var music);

            if (GameManager.InstanceExists) GameManager.Instance.SetVolumes(master, sfx, music, true);

            base.Hide();
        }

        /// <summary>
        /// Retrieve values from sliders
        /// </summary>
        private void GetSliderVolumes(out float masterVolume, out float sfxVolume, out float musicVolume)
        {
            masterVolume = masterSlider != null ? masterSlider.value : 1;
            sfxVolume = sfxSlider != null ? sfxSlider.value : 1;
            musicVolume = musicSlider != null ? musicSlider.value : 1;
        }
    }
}