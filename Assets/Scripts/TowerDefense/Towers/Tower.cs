using System;
using ActionGameFramework.Health;
using Core.Utilities;
using TowerDefense.Level;
using TowerDefense.Towers.Placement;
using TowerDefense.UI.HUD;
using UnityEngine;

namespace TowerDefense.Towers
{
    /// <inheritdoc />
    /// <summary>
    /// Common functionality for all types of towers
    /// </summary>
    public sealed class Tower : Targetable
    {
        /// <summary>
        /// The tower levels associated with this tower
        /// </summary>
        public TowerLevel[] levels;

        /// <summary>
        /// A generalised name common to a levels
        /// </summary>
        public string towerName;

        /// <summary>
        /// The size of the tower's footprint
        /// </summary>
        public IntVector2 dimensions;

        /// <summary>
        /// The physics mask the tower searches on
        /// </summary>
        public LayerMask enemyLayerMask;

        /// <summary>
        /// The current level of the tower
        /// </summary>
        public int currentLevel;

        /// <summary>
        /// Reference to the data of the current level
        /// </summary>
        private TowerLevel _currentTowerLevel;

        /// <summary>
        /// Gets whether the tower can level up anymore
        /// </summary>
        public bool IsAtMaxLevel {
            get { return currentLevel == levels.Length - 1; }
        }

        /// <summary>
        /// Gets the first level tower ghost prefab
        /// </summary>
        public TowerPlacementGhost TowerGhostPrefab {
            get { return levels[currentLevel].towerGhostPrefab; }
        }

        /// <summary>
        /// Gets the grid position for this tower on the <see cref="_placementArea"/>
        /// </summary>
        private IntVector2 _gridPosition;

        /// <summary>
        /// The placement area we've been built on
        /// </summary>
        private IPlacementArea _placementArea;

        /// <summary>
        /// The purchase cost of the tower
        /// </summary>
        public int PurchaseCost {
            get { return levels[0].Cost; }
        }

        /// <summary>
        /// Provide the tower with data to initialize with
        /// </summary>
        /// <param name="targetArea">The placement area configuration</param>
        /// <param name="destination">The destination position</param>
        public void Initialize(IPlacementArea targetArea, IntVector2 destination)
        {
            _placementArea = targetArea;
            _gridPosition = destination;

            if (targetArea != null)
            {
                var t = transform;
                t.position = _placementArea.GridToWorld(destination, dimensions);
                t.rotation = _placementArea.transform.rotation;
                targetArea.Occupy(destination, dimensions);
            }

            SetLevel(0);
            if (LevelManager.instanceExists)
            {
                LevelManager.instance.levelStateChanged += OnLevelStateChanged;
            }
        }

        /// <summary>
        /// Provides information on the cost to upgrade
        /// </summary>
        /// <returns>Returns -1 if the towers is already at max level, other returns the cost to upgrade</returns>
        public int GetCostForNextLevel()
        {
            if (IsAtMaxLevel)
            {
                return -1;
            }

            return levels[currentLevel + 1].Cost;
        }

        /// <summary>
        /// Kills this tower
        /// </summary>
        public void KillTower()
        {
            // Invoke base kill method
            Kill();
        }

        /// <summary>
        /// Provides the value received for selling this tower
        /// </summary>
        /// <returns>A sell value of the tower</returns>
        public int GetSellLevel()
        {
            return GetSellLevel(currentLevel);
        }

        /// <summary>
        /// Provides the value received for selling this tower of a particular level
        /// </summary>
        /// <param name="level">Level of tower</param>
        /// <returns>A sell value of the tower</returns>
        public int GetSellLevel(int level)
        {
            // sell for full price if waves haven't started yet
            if (LevelManager.instance.levelState != LevelState.Building) return levels[currentLevel].Sell;
            var cost = 0;
            for (var i = 0; i <= level; i++)
            {
                cost += levels[i].Cost;
            }

            return cost;
        }

        /// <summary>
        /// Used to (try to) upgrade the tower data
        /// </summary>
        public void UpgradeTower()
        {
            if (IsAtMaxLevel) return;

            SetLevel(currentLevel + 1);
        }

        public void Sell()
        {
            Remove();
        }

        /// <inheritdoc />
        /// <summary>
        /// Removes tower from placement area and destroys it
        /// </summary>
        public override void Remove()
        {
            base.Remove();

            _placementArea.Clear(_gridPosition, dimensions);
            Destroy(gameObject);
        }

        /// <summary>
        /// unsubscribe when necessary
        /// </summary>
        private void OnDestroy()
        {
            if (LevelManager.instanceExists)
            {
                LevelManager.instance.levelStateChanged += OnLevelStateChanged;
            }
        }

        /// <summary>
        /// Cache and update often used data
        /// </summary>
        private void SetLevel(int level)
        {
            if (level < 0 || level >= levels.Length)
            {
                return;
            }

            currentLevel = level;
            if (_currentTowerLevel != null) Destroy(_currentTowerLevel.gameObject);

            // instantiate the visual representation
            _currentTowerLevel = Instantiate(levels[currentLevel], transform);

            // initialize TowerLevel
            _currentTowerLevel.Initialize(this, enemyLayerMask, configuration.alignmentProvider);

            // health data
            ScaleHealth();

            // disable affectors
            var levelState = LevelManager.instance.levelState;
            var initialise = levelState == LevelState.AllEnemiesSpawned || levelState == LevelState.SpawningEnemies;
            _currentTowerLevel.SetAffectorState(initialise);
        }

        /// <summary>
        /// Scales the health based on the previous health
        /// Requires override when the rules for scaling health on upgrade changes
        /// </summary>
        private void ScaleHealth()
        {
            configuration.SetMaxHealth(_currentTowerLevel.MaxHealth);

            if (currentLevel == 0)
            {
                configuration.SetHealth(_currentTowerLevel.MaxHealth);
            }
            else
            {
                var currentHealth = Mathf.FloorToInt(configuration.normalisedHealth * _currentTowerLevel.MaxHealth);
                configuration.SetHealth(currentHealth);
            }
        }

        /// <summary>
        /// Initialises affectors based on the level state
        /// </summary>
        private void OnLevelStateChanged(LevelState previous, LevelState current)
        {
            var initialise = current == LevelState.AllEnemiesSpawned || current == LevelState.SpawningEnemies;
            _currentTowerLevel.SetAffectorState(initialise);
        }
    }
}