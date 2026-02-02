using NUnit.Framework;
using UnityEngine;

namespace InkSim.Tests
{
    /// <summary>
    /// TDD tests for Species → Faction auto-assignment system.
    /// Tests written BEFORE implementation - should fail until feature is complete.
    /// </summary>
    [TestFixture]
    public class SpeciesFactionTests
    {
        private GameObject _testObject;

        [SetUp]
        public void SetUp()
        {
            FactionRegistry.ClearCache();
            ReputationSystem.ClearForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
                Object.DestroyImmediate(_testObject);
            FactionRegistry.ClearCache();
        }

        #region FactionRegistry.GetOrCreate Tests

        [Test]
        public void FactionRegistry_GetOrCreate_ReturnsExistingFaction()
        {
            // Arrange - Ghost faction exists in Resources/Factions
            var existingFaction = FactionRegistry.GetByName("Ghost");
            Assert.IsNotNull(existingFaction, "Precondition: Ghost faction should exist");

            // Act
            var result = FactionRegistry.GetOrCreate("Ghost");

            // Assert
            Assert.AreSame(existingFaction, result, "Should return the existing faction, not create a new one");
        }

        [Test]
        public void FactionRegistry_GetOrCreate_CreatesRuntimeFaction()
        {
            // Arrange - ensure faction doesn't exist
            var existing = FactionRegistry.GetByName("BrandNewSpecies");
            Assert.IsNull(existing, "Precondition: BrandNewSpecies faction should not exist");

            // Act
            var result = FactionRegistry.GetOrCreate("BrandNewSpecies");

            // Assert
            Assert.IsNotNull(result, "Should create a new faction when one doesn't exist");
        }

        [Test]
        public void FactionRegistry_GetOrCreate_RuntimeFactionHasCorrectId()
        {
            // Act
            var faction = FactionRegistry.GetOrCreate("TestSpeciesAlpha");

            // Assert
            Assert.AreEqual("TestSpeciesAlpha", faction.id, "Runtime faction should have id matching the name");
        }

        [Test]
        public void FactionRegistry_GetOrCreate_RuntimeFactionHasCorrectDisplayName()
        {
            // Act
            var faction = FactionRegistry.GetOrCreate("TestSpeciesBeta");

            // Assert
            Assert.AreEqual("TestSpeciesBeta", faction.displayName, "Runtime faction should have displayName matching the name");
        }

        [Test]
        public void FactionRegistry_GetOrCreate_RuntimeFactionIsRegistered()
        {
            // Act
            var created = FactionRegistry.GetOrCreate("UniqueTestFaction");

            // Assert - should be findable by both GetByName and GetById
            var byName = FactionRegistry.GetByName("UniqueTestFaction");
            var byId = FactionRegistry.GetById("UniqueTestFaction");

            Assert.AreSame(created, byName, "Created faction should be findable by name");
            Assert.AreSame(created, byId, "Created faction should be findable by id");
        }

        [Test]
        public void FactionRegistry_GetOrCreate_CalledTwice_ReturnsSameInstance()
        {
            // Act
            var first = FactionRegistry.GetOrCreate("RepeatedSpecies");
            var second = FactionRegistry.GetOrCreate("RepeatedSpecies");

            // Assert
            Assert.AreSame(first, second, "Multiple calls should return the same instance");
        }

        #endregion

        #region SpeciesMember DefaultFaction Tests

        [Test]
        public void SpeciesMember_EnsuresDefaultFaction_WhenNull()
        {
            // Arrange - create species with null defaultFaction
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.id = "test_species";
            species.displayName = "TestSpeciesGamma";
            species.defaultFaction = null;

            _testObject = new GameObject("TestEntity");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = species;

            // Act - trigger Awake by enabling
            speciesMember.enabled = false;
            speciesMember.enabled = true;
            // Call EnsureDefaultFaction directly since Awake may have already run
            speciesMember.EnsureDefaultFaction();

            // Assert
            Assert.IsNotNull(species.defaultFaction, "defaultFaction should be set after EnsureDefaultFaction");

            // Cleanup
            Object.DestroyImmediate(species);
        }

        [Test]
        public void SpeciesMember_EnsuresDefaultFaction_MatchesSpeciesDisplayName()
        {
            // Arrange
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.id = "delta_species";
            species.displayName = "DeltaCreature";
            species.defaultFaction = null;

            _testObject = new GameObject("TestEntity");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = species;

            // Act
            speciesMember.EnsureDefaultFaction();

            // Assert
            Assert.IsNotNull(species.defaultFaction);
            Assert.AreEqual("DeltaCreature", species.defaultFaction.displayName, 
                "Auto-assigned faction should match species displayName");

            // Cleanup
            Object.DestroyImmediate(species);
        }

        [Test]
        public void SpeciesMember_EnsuresDefaultFaction_PreservesExistingFaction()
        {
            // Arrange - species with existing defaultFaction
            var existingFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            existingFaction.id = "existing_faction";
            existingFaction.displayName = "ExistingFaction";

            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.id = "epsilon_species";
            species.displayName = "EpsilonCreature";
            species.defaultFaction = existingFaction;

            _testObject = new GameObject("TestEntity");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = species;

            // Act
            speciesMember.EnsureDefaultFaction();

            // Assert
            Assert.AreSame(existingFaction, species.defaultFaction, 
                "Should not replace an existing defaultFaction");

            // Cleanup
            Object.DestroyImmediate(species);
            Object.DestroyImmediate(existingFaction);
        }

        #endregion

        #region FactionMember Integration Tests

        [Test]
        public void FactionMember_InheritsFactionFromSpecies()
        {
            // Arrange
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.id = "zeta_species";
            species.displayName = "ZetaCreature";
            species.defaultFaction = null;

            _testObject = new GameObject("TestEntity");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = species;
            
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = null; // Explicitly null

            // Act
            speciesMember.EnsureDefaultFaction();
            factionMember.ApplyRank();

            // Assert
            Assert.IsNotNull(factionMember.faction, "FactionMember should have a faction after ApplyRank");
            Assert.AreEqual("ZetaCreature", factionMember.faction.displayName,
                "FactionMember should inherit faction from species");

            // Cleanup
            Object.DestroyImmediate(species);
        }

        [Test]
        public void Integration_EntityWithSpecies_AlwaysHasFaction()
        {
            // Arrange - simulate a complete entity setup
            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.id = "complete_test";
            species.displayName = "CompleteTestCreature";
            species.defaultFaction = null;

            _testObject = new GameObject("CompleteEntity");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = species;
            
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = null;

            // Act - simulate the lifecycle
            speciesMember.EnsureDefaultFaction();
            factionMember.ApplyRank();

            // Assert - entity must have a faction
            Assert.IsNotNull(factionMember.faction, "Entity with species must always have a faction");
            Assert.IsNotNull(species.defaultFaction, "Species must have defaultFaction set");
            Assert.AreSame(species.defaultFaction, factionMember.faction, 
                "FactionMember.faction should be the same as species.defaultFaction");

            // Cleanup
            Object.DestroyImmediate(species);
        }

        [Test]
        public void Integration_ExistingFactionUsed_WhenSpeciesMatchesFactionName()
        {
            // Arrange - Ghost species should use existing Ghost faction
            var ghostFaction = FactionRegistry.GetByName("Ghost");
            Assert.IsNotNull(ghostFaction, "Precondition: Ghost faction must exist");

            var species = ScriptableObject.CreateInstance<SpeciesDefinition>();
            species.id = "ghost";
            species.displayName = "Ghost"; // Matches existing faction
            species.defaultFaction = null;

            _testObject = new GameObject("GhostEntity");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = species;

            // Act
            speciesMember.EnsureDefaultFaction();

            // Assert
            Assert.AreSame(ghostFaction, species.defaultFaction,
                "Should use existing Ghost faction, not create a new one");

            // Cleanup
            Object.DestroyImmediate(species);
        }

        #region Reputation Change De-escalation Tests

        [Test]
        public void EnemyAI_RetaliationTarget_ClearedWhenFactionBecomesFriendly()
        {
            // Arrange - Create enemy with retaliation target
            var ghostFaction = FactionRegistry.GetByName("Ghost");
            Assert.IsNotNull(ghostFaction, "Precondition: Ghost faction must exist");

            // Set faction hostile initially
            ReputationSystem.SetRep(ghostFaction.id, -50);

            _testObject = new GameObject("GhostEnemy");
            var enemy = _testObject.AddComponent<EnemyAI>();
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = ghostFaction;

            var playerObj = new GameObject("Player");
            var player = playerObj.AddComponent<PlayerController>();

            // Set retaliation target
            enemy.SetRetaliationTarget(player);

            // Act - Make faction friendly
            ReputationSystem.SetRep(ghostFaction.id, 50);

            // Enemy should clear retaliation target when it's no longer hostile
            // This happens during FindTarget() or via subscription to rep changes
            var target = enemy.FindTargetPublic(); // We'll need to expose this for testing

            // Assert - Should NOT chase friendly player
            Assert.IsNull(target, 
                "Enemy should not target player when faction became Friendly");

            // Cleanup
            Object.DestroyImmediate(playerObj);
        }

        [Test]
        public void EnemyAI_StopsChasing_WhenReputationImproves()
        {
            // Arrange
            var ghostFaction = FactionRegistry.GetByName("Ghost");
            ReputationSystem.SetRep(ghostFaction.id, -50); // Hostile

            _testObject = new GameObject("GhostEnemy");
            _testObject.transform.position = Vector3.zero;
            var enemy = _testObject.AddComponent<EnemyAI>();
            enemy.aggroRange = 10;
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = ghostFaction;

            var playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(2, 0, 0);
            var player = playerObj.AddComponent<PlayerController>();

            // Verify initially hostile
            Assert.IsTrue(HostilityService.IsHostile(enemy, player),
                "Precondition: Should be hostile initially");

            // Act - Make faction friendly
            ReputationSystem.SetRep(ghostFaction.id, 50);

            // Assert - No longer hostile
            Assert.IsFalse(HostilityService.IsHostile(enemy, player),
                "Should not be hostile after reputation improved");

            // FindTarget should return null
            var target = enemy.FindTargetPublic();
            Assert.IsNull(target,
                "Enemy should not find target when player is friendly");

            // Cleanup
            Object.DestroyImmediate(playerObj);
        }

        [Test]
        public void EnemyAI_RetaliationTarget_NotClearedIfStillHostile()
        {
            // Arrange - Create enemy with retaliation target
            var ghostFaction = FactionRegistry.GetByName("Ghost");
            ReputationSystem.SetRep(ghostFaction.id, -50); // Keep hostile

            _testObject = new GameObject("GhostEnemy");
            var enemy = _testObject.AddComponent<EnemyAI>();
            enemy.aggroRange = 10;
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = ghostFaction;

            var playerObj = new GameObject("Player");
            var player = playerObj.AddComponent<PlayerController>();

            // Set retaliation target
            enemy.SetRetaliationTarget(player);

            // Act - Find target (should still return player)
            var target = enemy.FindTargetPublic();

            // Assert - Should still target hostile player
            Assert.AreSame(player, target,
                "Enemy should still target player when faction is hostile");

            // Cleanup
            Object.DestroyImmediate(playerObj);
        }

        #endregion

        
#region Cross-Species Faction Membership Tests

        [Test]
        public void FactionMember_ExplicitFaction_NotOverwrittenBySpecies()
        {
            // Arrange - Slime species but explicitly set to Ghost faction
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime";
            slimeSpecies.displayName = "Slime";
            slimeSpecies.defaultFaction = null;

            var ghostFaction = FactionRegistry.GetByName("Ghost");
            Assert.IsNotNull(ghostFaction, "Precondition: Ghost faction must exist");

            _testObject = new GameObject("SlimeInGhostFaction");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = slimeSpecies;
            
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = ghostFaction; // Explicitly set BEFORE ApplyRank

            // Act
            speciesMember.EnsureDefaultFaction(); // Sets slimeSpecies.defaultFaction to "Slime"
            factionMember.ApplyRank();

            // Assert - should stay Ghost, not become Slime
            Assert.AreSame(ghostFaction, factionMember.faction,
                "Explicit faction should NOT be overwritten by species default");
            Assert.AreEqual("Ghost", factionMember.faction.displayName);

            // Cleanup
            Object.DestroyImmediate(slimeSpecies);
        }

        [Test]
        public void FactionMember_SetFaction_CanChangeToDifferentFactionThanSpecies()
        {
            // Arrange - create entity with Slime species, initially gets Slime faction
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime_defector";
            slimeSpecies.displayName = "SlimeDefector";
            slimeSpecies.defaultFaction = null;

            _testObject = new GameObject("DefectorSlime");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = slimeSpecies;
            speciesMember.EnsureDefaultFaction();

            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.ApplyRank();

            // Verify initially has SlimeDefector faction
            Assert.AreEqual("SlimeDefector", factionMember.faction.displayName);

            // Act - defect to Ghost faction
            var ghostFaction = FactionRegistry.GetByName("Ghost");
            factionMember.SetFaction(ghostFaction);

            // Assert - now in Ghost faction, not SlimeDefector
            Assert.AreSame(ghostFaction, factionMember.faction,
                "SetFaction should change faction regardless of species");
            Assert.AreEqual("Ghost", factionMember.faction.displayName);

            // Species is still SlimeDefector
            Assert.AreEqual("SlimeDefector", speciesMember.species.displayName,
                "Species should remain unchanged when faction changes");

            // Cleanup
            Object.DestroyImmediate(slimeSpecies);
        }

        [Test]
        public void FactionMember_CrossSpeciesMember_UsedForHostilityChecks()
        {
            // Arrange - Two entities: Slime in Ghost faction, Snake in Snake faction
            // Ghost and Snake should be hostile based on faction, not species
            
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "hostile_test_slime";
            slimeSpecies.displayName = "HostileTestSlime";
            
            var snakeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            snakeSpecies.id = "hostile_test_snake";
            snakeSpecies.displayName = "HostileTestSnake";

            var ghostFaction = FactionRegistry.GetOrCreate("GhostTestFaction");
            var snakeFaction = FactionRegistry.GetOrCreate("SnakeTestFaction");

            // Set inter-faction hostility
            ReputationSystem.SetInterRep(ghostFaction.id, snakeFaction.id, -50);

            _testObject = new GameObject("SlimeInGhostFaction");
            var slimeEntity = _testObject.AddComponent<GridEntity>();
            var slimeSpeciesMember = _testObject.AddComponent<SpeciesMember>();
            slimeSpeciesMember.species = slimeSpecies;
            var slimeFactionMember = _testObject.AddComponent<FactionMember>();
            slimeFactionMember.faction = ghostFaction; // Slime species but Ghost faction

            var snakeObj = new GameObject("SnakeInSnakeFaction");
            var snakeEntity = snakeObj.AddComponent<GridEntity>();
            var snakeSpeciesMember = snakeObj.AddComponent<SpeciesMember>();
            snakeSpeciesMember.species = snakeSpecies;
            var snakeFactionMember = snakeObj.AddComponent<FactionMember>();
            snakeFactionMember.faction = snakeFaction;

            // Act - check hostility (should use faction, not species)
            bool isHostile = HostilityService.IsHostile(slimeEntity, snakeEntity);

            // Assert - hostility based on Ghost→Snake inter-rep (-50), not species
            Assert.IsTrue(isHostile,
                "Hostility should be determined by faction membership, not species");

            // Cleanup
            Object.DestroyImmediate(snakeObj);
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(snakeSpecies);
        }

        [Test]
        public void FactionMember_SameSpecies_DifferentFactions_CanBeHostile()
        {
            // Arrange - Two Slimes, one in Slime faction, one defected to Ghost faction
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "civil_war_slime";
            slimeSpecies.displayName = "CivilWarSlime";

            var slimeFaction = FactionRegistry.GetOrCreate("LoyalistSlimes");
            var ghostFaction = FactionRegistry.GetOrCreate("DefectorSlimes");

            // Set inter-faction hostility between loyalists and defectors
            ReputationSystem.SetInterRep(slimeFaction.id, ghostFaction.id, -50);
            ReputationSystem.SetInterRep(ghostFaction.id, slimeFaction.id, -50);

            _testObject = new GameObject("LoyalistSlime");
            var loyalist = _testObject.AddComponent<GridEntity>();
            var loyalistSpecies = _testObject.AddComponent<SpeciesMember>();
            loyalistSpecies.species = slimeSpecies;
            var loyalistFaction = _testObject.AddComponent<FactionMember>();
            loyalistFaction.faction = slimeFaction;

            var defectorObj = new GameObject("DefectorSlime");
            var defector = defectorObj.AddComponent<GridEntity>();
            var defectorSpecies = defectorObj.AddComponent<SpeciesMember>();
            defectorSpecies.species = slimeSpecies; // Same species!
            var defectorFactionMember = defectorObj.AddComponent<FactionMember>();
            defectorFactionMember.faction = ghostFaction; // Different faction

            // Act
            bool loyalistHostileToDefector = HostilityService.IsHostile(loyalist, defector);
            bool defectorHostileToLoyalist = HostilityService.IsHostile(defector, loyalist);

            // Assert - same species but different factions = hostile
            Assert.IsTrue(loyalistHostileToDefector,
                "Same species in different factions should be hostile");
            Assert.IsTrue(defectorHostileToLoyalist,
                "Same species in different factions should be hostile (bidirectional)");

            // Cleanup
            Object.DestroyImmediate(defectorObj);
            Object.DestroyImmediate(slimeSpecies);
        }

        [Test]
        public void FactionMember_DifferentSpecies_SameFaction_NotHostile()
        {
            // Arrange - Slime and Snake both in the same "Alliance" faction
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "allied_slime";
            slimeSpecies.displayName = "AlliedSlime";

            var snakeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            snakeSpecies.id = "allied_snake";
            snakeSpecies.displayName = "AlliedSnake";

            var allianceFaction = FactionRegistry.GetOrCreate("GrandAlliance");

            _testObject = new GameObject("AlliedSlime");
            var slimeEntity = _testObject.AddComponent<GridEntity>();
            var slimeSpeciesMember = _testObject.AddComponent<SpeciesMember>();
            slimeSpeciesMember.species = slimeSpecies;
            var slimeFactionMember = _testObject.AddComponent<FactionMember>();
            slimeFactionMember.faction = allianceFaction;

            var snakeObj = new GameObject("AlliedSnake");
            var snakeEntity = snakeObj.AddComponent<GridEntity>();
            var snakeSpeciesMember = snakeObj.AddComponent<SpeciesMember>();
            snakeSpeciesMember.species = snakeSpecies;
            var snakeFactionMember = snakeObj.AddComponent<FactionMember>();
            snakeFactionMember.faction = allianceFaction; // Same faction!

            // Act
            bool isHostile = HostilityService.IsHostile(slimeEntity, snakeEntity);

            // Assert - different species but same faction = NOT hostile
            Assert.IsFalse(isHostile,
                "Different species in the same faction should NOT be hostile");

            // Cleanup
            Object.DestroyImmediate(snakeObj);
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(snakeSpecies);
        }

        #endregion

        
#endregion
    
        #region Cross-Species Faction Membership Tests

        [Test]
        public void FactionMember_ExplicitFaction_NotOverriddenBySpecies()
        {
            // Arrange - Slime species but explicitly set to a different faction
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime";
            slimeSpecies.displayName = "Slime";
            slimeSpecies.defaultFaction = null;

            var deerFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            deerFaction.id = "deer";
            deerFaction.displayName = "Deer";

            _testObject = new GameObject("SlimeInDeerFaction");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = slimeSpecies;
            
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = deerFaction; // Explicitly set BEFORE ApplyRank

            // Act
            speciesMember.EnsureDefaultFaction(); // Sets slimeSpecies.defaultFaction to "Slime" faction
            factionMember.ApplyRank();

            // Assert - faction should still be Deer, not overridden by species
            Assert.AreEqual("Deer", factionMember.faction.displayName,
                "Explicit faction should not be overridden by species default");
            Assert.AreEqual("Slime", speciesMember.species.displayName,
                "Species should remain Slime");

            // Cleanup
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(deerFaction);
        }

        [Test]
        public void FactionMember_SetFaction_ChangesToDifferentFaction()
        {
            // Arrange - Start as Slime faction
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime";
            slimeSpecies.displayName = "SlimeCreature";
            slimeSpecies.defaultFaction = null;

            var deerFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            deerFaction.id = "deer_faction";
            deerFaction.displayName = "DeerHerd";

            _testObject = new GameObject("DefectingSlime");
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = slimeSpecies;
            speciesMember.EnsureDefaultFaction();
            
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.ApplyRank(); // Now in SlimeCreature faction

            Assert.AreEqual("SlimeCreature", factionMember.faction.displayName,
                "Precondition: Should start in species faction");

            // Act - Defect to Deer faction
            factionMember.SetFaction(deerFaction);

            // Assert
            Assert.AreEqual("DeerHerd", factionMember.faction.displayName,
                "SetFaction should change to the new faction");
            Assert.AreEqual("SlimeCreature", speciesMember.species.displayName,
                "Species should remain unchanged after faction change");

            // Cleanup
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(deerFaction);
        }

        [Test]
        public void Integration_SpeciesAndFaction_IndependentIdentities()
        {
            // Arrange - A Slime that joins the Wolf Pack
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime_species";
            slimeSpecies.displayName = "Slime";
            slimeSpecies.defaultFaction = null;

            var wolfFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            wolfFaction.id = "wolf_pack";
            wolfFaction.displayName = "Wolf Pack";

            _testObject = new GameObject("SlimeWolfAlly");
            
            var speciesMember = _testObject.AddComponent<SpeciesMember>();
            speciesMember.species = slimeSpecies;
            speciesMember.EnsureDefaultFaction();
            
            var factionMember = _testObject.AddComponent<FactionMember>();
            factionMember.faction = wolfFaction; // Join wolves instead of slimes
            factionMember.ApplyRank();

            // Assert - Species is Slime, Faction is Wolf Pack
            Assert.AreEqual("Slime", speciesMember.species.displayName,
                "Species identity should be Slime");
            Assert.AreEqual("Wolf Pack", factionMember.faction.displayName,
                "Faction allegiance should be Wolf Pack");
            
            // Species still has its own default faction
            Assert.IsNotNull(slimeSpecies.defaultFaction,
                "Species should still have a default faction");
            Assert.AreEqual("Slime", slimeSpecies.defaultFaction.displayName,
                "Species default faction should be Slime");

            // Cleanup
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(wolfFaction);
        }

        [Test]
        public void Integration_TwoEntitiesSameSpecies_DifferentFactions()
        {
            // Arrange - Two slimes, one loyal, one defector
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime";
            slimeSpecies.displayName = "Slime";
            slimeSpecies.defaultFaction = null;

            var humanFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            humanFaction.id = "humans";
            humanFaction.displayName = "Human Kingdom";

            // Loyal slime
            var loyalSlime = new GameObject("LoyalSlime");
            var loyalSpecies = loyalSlime.AddComponent<SpeciesMember>();
            loyalSpecies.species = slimeSpecies;
            loyalSpecies.EnsureDefaultFaction();
            var loyalFaction = loyalSlime.AddComponent<FactionMember>();
            loyalFaction.ApplyRank(); // Uses species default

            // Defector slime
            var defectorSlime = new GameObject("DefectorSlime");
            var defectorSpecies = defectorSlime.AddComponent<SpeciesMember>();
            defectorSpecies.species = slimeSpecies;
            var defectorFaction = defectorSlime.AddComponent<FactionMember>();
            defectorFaction.faction = humanFaction; // Joins humans
            defectorFaction.ApplyRank();

            // Assert
            Assert.AreEqual("Slime", loyalFaction.faction.displayName,
                "Loyal slime should be in Slime faction");
            Assert.AreEqual("Human Kingdom", defectorFaction.faction.displayName,
                "Defector slime should be in Human Kingdom faction");
            
            // Both are still Slimes
            Assert.AreEqual(slimeSpecies, loyalSpecies.species);
            Assert.AreEqual(slimeSpecies, defectorSpecies.species);

            // Cleanup
            Object.DestroyImmediate(loyalSlime);
            Object.DestroyImmediate(defectorSlime);
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(humanFaction);
        }

        [Test]
        public void Integration_HostilityBasedOnFaction_NotSpecies()
        {
            // Arrange - Slime defects to Player-friendly faction
            // Another slime stays hostile
            var slimeSpecies = ScriptableObject.CreateInstance<SpeciesDefinition>();
            slimeSpecies.id = "slime";
            slimeSpecies.displayName = "Slime";
            slimeSpecies.defaultFaction = null;

            var friendlyFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            friendlyFaction.id = "friendly_faction";
            friendlyFaction.displayName = "Friendly";
            friendlyFaction.defaultReputation = 50; // Friendly to player

            var hostileFaction = ScriptableObject.CreateInstance<FactionDefinition>();
            hostileFaction.id = "hostile_faction";
            hostileFaction.displayName = "Hostile";
            hostileFaction.defaultReputation = -50; // Hostile to player

            // Friendly slime
            var friendlySlime = new GameObject("FriendlySlime");
            var friendlySpecies = friendlySlime.AddComponent<SpeciesMember>();
            friendlySpecies.species = slimeSpecies;
            var friendlyMember = friendlySlime.AddComponent<FactionMember>();
            friendlyMember.faction = friendlyFaction;
            friendlyMember.ApplyRank();

            // Hostile slime  
            var hostileSlime = new GameObject("HostileSlime");
            var hostileSpecies = hostileSlime.AddComponent<SpeciesMember>();
            hostileSpecies.species = slimeSpecies;
            var hostileMember = hostileSlime.AddComponent<FactionMember>();
            hostileMember.faction = hostileFaction;
            hostileMember.ApplyRank();

            // Player
            var player = new GameObject("Player");
            var playerController = player.AddComponent<PlayerController>();

            // Act & Assert - Check hostility is based on faction, not species
            int friendlyRep = ReputationSystem.GetRep(friendlyFaction.id);
            int hostileRep = ReputationSystem.GetRep(hostileFaction.id);

            Assert.Greater(friendlyRep, HostilityService.HostileThreshold,
                "Friendly faction should have good reputation");
            Assert.LessOrEqual(hostileRep, HostilityService.HostileThreshold,
                "Hostile faction should have bad reputation");

            // Both are Slimes but have different hostility
            Assert.AreEqual("Slime", friendlySpecies.species.displayName);
            Assert.AreEqual("Slime", hostileSpecies.species.displayName);
            Assert.AreEqual("Friendly", friendlyMember.faction.displayName);
            Assert.AreEqual("Hostile", hostileMember.faction.displayName);

            // Cleanup
            Object.DestroyImmediate(friendlySlime);
            Object.DestroyImmediate(hostileSlime);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(slimeSpecies);
            Object.DestroyImmediate(friendlyFaction);
            Object.DestroyImmediate(hostileFaction);
        }

        #endregion
}
}
