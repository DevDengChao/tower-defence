using System.Collections.Generic;
using System.Linq;
using Core.Health;
using TowerDefense.Affectors;
using TowerDefense.Towers.Data;
using TowerDefense.UI.HUD;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <summary>
    /// An individual level of a tower
    /// </summary>
    /// <inheritdoc cref="MonoBehaviour" />
    /// <inheritdoc cref="ISerializationCallbackReceiver" />
    [DisallowMultipleComponent]
    public sealed class TowerLevel : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// The prefab for communicating placement in the scene
        /// </summary>
        public TowerPlacementGhost towerGhostPrefab;

        /// <summary>
        /// Build effect gameObject to instantiate on start
        /// </summary>
        public GameObject buildEffectPrefab;

        /// <summary>
        /// Reference to script-able object with level data on it
        /// </summary>
        public TowerLevelData levelData;

        /// <summary>
        /// The parent tower controller of this tower
        /// </summary>
        private Tower _mParentTower;

        /// <summary>
        /// The list of effects attached to the tower
        /// </summary>
        private Affector[] _mAffectors;

        /// <summary>
        /// Gets the list of effects attached to the tower
        /// </summary>
        private IEnumerable<Affector> Affectors {
            get { return _mAffectors ?? (_mAffectors = GetComponentsInChildren<Affector>()); }
        }

        /// <summary>
        /// The physics layer mask that the tower searches on
        /// </summary>
        private LayerMask _mask;

        /// <summary>
        /// Gets the cost value
        /// </summary>
        public int Cost {
            get { return levelData.cost; }
        }

        /// <summary>
        /// Gets the sell value
        /// </summary>
        public int Sell {
            get { return levelData.sell; }
        }

        /// <summary>
        /// Gets the max health
        /// </summary>
        public int MaxHealth {
            get { return levelData.maxHealth; }
        }

        /// <summary>
        /// Gets the starting health
        /// </summary>
        public int StartingHealth {
            get { return levelData.startingHealth; }
        }

        /// <summary>
        /// Gets the tower description
        /// </summary>
        public string Description {
            get { return levelData.description; }
        }

        /// <summary>
        /// Gets the tower description
        /// </summary>
        public string UpgradeDescription {
            get { return levelData.upgradeDescription; }
        }

        /// <summary>
        /// Initialises the Effects attached to this object
        /// </summary>
        public void Initialize(Tower tower, LayerMask enemyMask, IAlignmentProvider alignment)
        {
            _mask = enemyMask;

            foreach (var effect in Affectors)
            {
                effect.Initialize(alignment, _mask);
            }

            _mParentTower = tower;
        }

        /// <summary>
        /// A method for activating or deactivating the attached <see cref="Affectors"/>
        /// </summary>
        public void SetAffectorState(bool state)
        {
            foreach (var item in Affectors)
            {
                if (item != null)
                {
                    item.enabled = state;
                }
            }
        }

        /// <summary>
        /// Returns a list of affectors that implement ITowerRadiusVisualizer
        /// </summary>
        /// <returns>ITowerRadiusVisualizers of tower</returns>
        public List<ITowerRadiusProvider> GetRadiusVisualizers()
        {
            return Affectors.OfType<ITowerRadiusProvider>().ToList();
        }

        /// <summary>
        /// Returns the dps of the tower
        /// </summary>
        /// <returns>The dps of the tower</returns>
        public float GetTowerDps()
        {
            return Affectors.OfType<AttackAffector>()
                .Where(attack => attack.damagerProjectile != null)
                .Sum(attack => attack.GetProjectileDamage() * attack.fireRate);
        }

        public void Kill()
        {
            _mParentTower.KillTower();
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            // Setting this member to null is required because we are setting this value on a prefab which will 
            // persists post run in editor, so we null this member to ensure it is repopulated every run
            _mAffectors = null;
        }

        /// <summary>
        /// Instantiate the build particle effect object
        /// </summary>
        private void Start()
        {
            if (buildEffectPrefab != null)
            {
                Instantiate(buildEffectPrefab, transform);
            }
        }
    }
}