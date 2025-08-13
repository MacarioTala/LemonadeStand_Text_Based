using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Good", menuName = "LemonadeStandAssets/Good", order = 1)]
public class Good : ScriptableObject
{
    public string GoodName;

    private decimal _price;
    private decimal _minAskPrice;

    public decimal GetCostOfGood(Recipe recipe)
    {
        if (_minAskPrice == 0)
        {
            // If no minimum ask price is set, calculate it based on the recipe's ingredients
            _minAskPrice = recipe.GetIngredients()
                .Sum(ingredient => ingredient.Good.GetPrice() * ingredient.Quantity_needed);
            return _minAskPrice;
        }
        return _minAskPrice;
    }
    private decimal Price
    {
        get => _price;
        set => _price = Math.Round(value, 2);
    }

    //Demand
    public Dictionary<ElasticityTypeEnum, float> Elasticities = new();
    public void AddElasticity(ElasticityTypeEnum key, float value) => Elasticities.Add(key, value);
    public bool isDemandInelastic
    {
        get {
            return Elasticities.Count == 0
            ||
            Elasticities.Where(x=>x.Value!=0).Count()==0
            ;
        }
    }

    private decimal price_increment_rate;
    private PriceBand PriceBand;

    public int ExpiresAfterPeriods { get; set; } = int.MaxValue;
    private RarityEnum _rarity;

    public bool IsProducedGood { get; set; } = false;
    public int price_increase_threshold; //Might not need this. Are there any good-specific price thresholds?
    public int price_decrease_threshold; //ibid

    [SerializeField] private readonly List<Good> _substitute_goods = new();
    #region Effects
    List<GoodEffect> _effects = new();

    public List<MetricEnum> AffectsMetrics()
    {
        return (from metric in _effects
                select metric.AffectsMetric).ToList();
    }

    public ReductionResult ReducesMetric(MetricEnum metric)
    {
        var isReducing = _effects.Any(effect => effect.AffectsMetric == metric && effect.IsReduce);
        var reductionAmount = _effects
            .Where(effect => effect.AffectsMetric == metric && effect.IsReduce)
            .FirstOrDefault()
            ?.Magnitude ?? 0f;
        return new ReductionResult(isReducing, reductionAmount);
    }

    public bool HasEffectOn(MetricEnum metric)
    {
        return _effects.Any(effect => effect.AffectsMetric == metric);
    }

    public void AddEffect(GoodEffect effect)
    {
        _effects.Add(effect);
    }
    public void RemoveEffect(GoodEffect effect)
    {
        _effects.Remove(effect);
    }
    public void ApplyEffects(PopulationAgent company, float percentageOfEffectToApply=1f)
    {
        foreach (var effect in _effects)
        {
            effect.Apply(company,percentageOfEffectToApply);
        }
    }
    #endregion

    public static Good CreateInstance(string good_name,
                                        PriceBand price_band = null,
                                        RarityEnum rarity = RarityEnum.Common)
    {
        var good = CreateInstance<Good>();
        good.Initialize(good_name, price_band, rarity);
        return good;
    }

    private void Initialize(string good_name,
                            PriceBand price_band,
                            RarityEnum rarity = RarityEnum.Common)
    {
        this.GoodName = good_name;
        PriceBand = price_band;
        _rarity = rarity;
        //Initial price will be determined based on price_band
        Price = Generate_initial_price();
        Set_initial_price_thresholds();
        Set_initial_price_increment_rate();
    }

    public PriceBand Get_price_band() => PriceBand;
    public decimal GetPrice() => Price;

    internal void SetPrice(decimal new_price) => Price = new_price;

    public RarityEnum GetRarity() => _rarity;
    public void SetRarity(RarityEnum rarity) => _rarity = rarity;
    public decimal Get_price_increment_rate() => price_increment_rate;

    private decimal Generate_initial_price()
    {
        var randomFloat = UnityEngine.Random.value;
        var price_range = PriceBand.max - PriceBand.min;
        return PriceBand.min + (decimal)randomFloat * price_range;
    }

    public void Add_substitute_good(Good good) => _substitute_goods.Add(good);

    private void Set_price_thresholds(int increase_threshold, int decrease_threshold) {
        price_increase_threshold = increase_threshold;
        price_decrease_threshold = decrease_threshold;
    }
    private void Set_initial_price_thresholds()
    {
        const int common_increase_threshold = 500;
        const int common_decrease_threshold = 100;
        const int uncommon_increase_threshold = 250;
        const int uncommon_decrease_threshold = 50;
        const int rare_increase_threshold = 50;
        const int rare_decrease_threshold = 10;
        const int very_rare_increase_threshold = 5;
        const int very_rare_decrease_threshold = 1;
        if (_rarity == RarityEnum.Common) {
            Set_price_thresholds(common_increase_threshold, common_decrease_threshold);
        }
        if (_rarity == RarityEnum.Uncommon) {
            Set_price_thresholds(uncommon_increase_threshold, uncommon_decrease_threshold);
        }
        if (_rarity == RarityEnum.Rare) {
            Set_price_thresholds(rare_increase_threshold, rare_decrease_threshold);
        }
        if (_rarity == RarityEnum.Very_Rare) {
            Set_price_thresholds(very_rare_increase_threshold, very_rare_decrease_threshold);
        }
    }

    private void Set_initial_price_increment_rate()
    {
        const decimal common_price_increment_rate = 0.1m;
        const decimal uncommon_price_increment_rate = 0.15m;
        const decimal rare_price_increment_rate = 0.3m;
        const decimal very_rare_price_increment_rate = 0.4m;
        if (_rarity == RarityEnum.Common) {
            price_increment_rate = common_price_increment_rate;
        }
        if (_rarity == RarityEnum.Uncommon) {
            price_increment_rate = uncommon_price_increment_rate;
        }
        if (_rarity == RarityEnum.Rare) {
            price_increment_rate = rare_price_increment_rate;
        }
        if (_rarity == RarityEnum.Very_Rare) {
            price_increment_rate = very_rare_price_increment_rate;
        }
    }
    #region Overrides
    public override bool Equals(object obj)
    {
        if (obj is Good other)
        {
            return GoodName == other.GoodName;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return GoodName?.GetHashCode() ?? 0;
    }

    public override string ToString()
    {
        return GoodName;
    }
    #endregion
}

public enum RarityEnum
{
    Common,
    Uncommon,
    Rare,
    Very_Rare,
    Unique
}

[System.Serializable]
public class PriceBand{
    public readonly decimal min;
    public readonly decimal max;

    public PriceBand(decimal lower_bound=0, decimal upper_bound=0){
        min = lower_bound;
        max = upper_bound;
    }
}

public class ReductionResult
{
    public readonly bool IsReducing;
    public readonly float ReductionAmount;
    public ReductionResult(bool isReducing, float reductionAmount)
    {
        IsReducing = isReducing;
        ReductionAmount = reductionAmount;
    }

    public float By()=> IsReducing ? ReductionAmount : 0f;
}
