#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NineKingsPrototype.V2
{
    [CreateAssetMenu(menuName = "NineKings/V2 Content Database", fileName = "NineKingsV2ContentDatabase")]
    public sealed class ContentDatabase : ScriptableObject
    {
        public List<KingDefinition> kings = new();
        public List<CardDefinition> cards = new();
        public List<CardCombatConfig> combatConfigs = new();
        public List<CardPresentationConfig> presentationConfigs = new();
        public List<UnitArchetypeDefinition> unitArchetypes = new();
        public List<SpawnPatternSpec> spawnPatterns = new();
        public List<WeaponFXSpec> weaponFx = new();
        public List<StackDisplayRule> stackDisplayRules = new();
        public List<LootPoolDefinition> lootPools = new();
        public List<YearEventDefinition> yearEvents = new();
        public BattleCurveDefinition battleCurve = new();

        [NonSerialized] private Dictionary<string, KingDefinition>? _kingsById;
        [NonSerialized] private Dictionary<string, CardDefinition>? _cardsById;
        [NonSerialized] private Dictionary<string, CardCombatConfig>? _combatById;
        [NonSerialized] private Dictionary<string, CardPresentationConfig>? _presentationById;

        private void OnEnable()
        {
            RebuildIndexes();
        }

        public void RebuildIndexes()
        {
            _kingsById = kings.Where(king => !string.IsNullOrEmpty(king.kingId)).ToDictionary(king => king.kingId, StringComparer.Ordinal);
            _cardsById = cards.Where(card => !string.IsNullOrEmpty(card.cardId)).ToDictionary(card => card.cardId, StringComparer.Ordinal);
            _combatById = combatConfigs.Where(config => !string.IsNullOrEmpty(config.cardId)).ToDictionary(config => config.cardId, StringComparer.Ordinal);
            _presentationById = presentationConfigs.Where(config => !string.IsNullOrEmpty(config.cardId)).ToDictionary(config => config.cardId, StringComparer.Ordinal);
        }

        public KingDefinition? GetKing(string kingId)
        {
            _kingsById ??= new Dictionary<string, KingDefinition>(StringComparer.Ordinal);
            return _kingsById.TryGetValue(kingId, out var king) ? king : null;
        }

        public CardDefinition? GetCard(string cardId)
        {
            _cardsById ??= new Dictionary<string, CardDefinition>(StringComparer.Ordinal);
            return _cardsById.TryGetValue(cardId, out var card) ? card : null;
        }

        public CardCombatConfig? GetCombatConfig(string cardId)
        {
            _combatById ??= new Dictionary<string, CardCombatConfig>(StringComparer.Ordinal);
            return _combatById.TryGetValue(cardId, out var config) ? config : null;
        }

        public CardPresentationConfig? GetPresentationConfig(string cardId)
        {
            _presentationById ??= new Dictionary<string, CardPresentationConfig>(StringComparer.Ordinal);
            return _presentationById.TryGetValue(cardId, out var config) ? config : null;
        }

        public IReadOnlyList<string> Validate()
        {
            var errors = new List<string>();
            ValidateDuplicates(kings.Select(king => king.kingId), "kingId", errors);
            ValidateDuplicates(cards.Select(card => card.cardId), "cardId", errors);
            ValidateDuplicates(unitArchetypes.Select(item => item.unitArchetypeId), "unitArchetypeId", errors);
            ValidateDuplicates(spawnPatterns.Select(item => item.spawnPatternId), "spawnPatternId", errors);
            ValidateDuplicates(weaponFx.Select(item => item.weaponFxId), "weaponFxId", errors);
            ValidateDuplicates(stackDisplayRules.Select(item => item.stackDisplayRuleId), "stackDisplayRuleId", errors);
            ValidateDuplicates(lootPools.Select(item => item.lootPoolId), "lootPoolId", errors);

            foreach (var king in kings)
            {
                if (king.factionType == KingFactionType.Player && (string.IsNullOrEmpty(king.baseCardId) || GetCard(king.baseCardId) == null))
                {
                    errors.Add($"King '{king.kingId}' has invalid base card '{king.baseCardId}'.");
                }

                foreach (var cardId in king.cardIds)
                {
                    if (GetCard(cardId) == null)
                    {
                        errors.Add($"King '{king.kingId}' references missing card '{cardId}'.");
                    }
                }
            }

            foreach (var card in cards)
            {
                var combat = GetCombatConfig(card.cardId);
                var presentation = GetPresentationConfig(card.cardId);
                if (combat == null)
                {
                    errors.Add($"Card '{card.cardId}' is missing combat config.");
                }

                if (presentation == null)
                {
                    errors.Add($"Card '{card.cardId}' is missing presentation config.");
                }
            }

            foreach (var combat in combatConfigs)
            {
                if (combat.spawnsUnits && !unitArchetypes.Any(item => string.Equals(item.unitArchetypeId, combat.unitArchetypeId, StringComparison.Ordinal)))
                {
                    errors.Add($"Combat config '{combat.cardId}' references missing unit archetype '{combat.unitArchetypeId}'.");
                }

                if (!string.IsNullOrEmpty(combat.spawnPatternId) && !spawnPatterns.Any(item => string.Equals(item.spawnPatternId, combat.spawnPatternId, StringComparison.Ordinal)))
                {
                    errors.Add($"Combat config '{combat.cardId}' references missing spawn pattern '{combat.spawnPatternId}'.");
                }

                if (combat.levels.Count == 0)
                {
                    errors.Add($"Combat config '{combat.cardId}' has no levels.");
                }
            }

            foreach (var presentation in presentationConfigs)
            {
                if (!string.IsNullOrEmpty(presentation.stackDisplayRuleId) && !stackDisplayRules.Any(item => string.Equals(item.stackDisplayRuleId, presentation.stackDisplayRuleId, StringComparison.Ordinal)))
                {
                    errors.Add($"Presentation config '{presentation.cardId}' references missing stack rule '{presentation.stackDisplayRuleId}'.");
                }

                if (!string.IsNullOrEmpty(presentation.weaponFxId) && !weaponFx.Any(item => string.Equals(item.weaponFxId, presentation.weaponFxId, StringComparison.Ordinal)))
                {
                    errors.Add($"Presentation config '{presentation.cardId}' references missing weapon fx '{presentation.weaponFxId}'.");
                }
            }

            return errors;
        }

        public string GetDefaultEnemyKingId(string playerKingId)
        {
            var playerKing = GetKing(playerKingId);
            if (playerKing != null && playerKing.enemyKingIds.Count > 0)
            {
                return playerKing.enemyKingIds[0];
            }

            return kings.FirstOrDefault(king => king.factionType == KingFactionType.Enemy)?.kingId ?? string.Empty;
        }

        private static void ValidateDuplicates(IEnumerable<string> ids, string label, List<string> errors)
        {
            foreach (var group in ids.Where(id => !string.IsNullOrEmpty(id)).GroupBy(id => id, StringComparer.Ordinal))
            {
                if (group.Count() > 1)
                {
                    errors.Add($"Duplicate {label}: {group.Key}");
                }
            }
        }
    }

    [Serializable]
    public sealed class YearEventDefinition
    {
        public int year;
        public List<YearEventType> events = new();
    }
}
