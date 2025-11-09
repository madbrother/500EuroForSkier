using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace EuroForSkier;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class EuroForSkier(DatabaseServer databaseServer, ISptLogger<EuroForSkier> logger) : IOnLoad
{
    private double? _exchangeRate;
    public Task OnLoad()
    {
        var traders = databaseServer.GetTables().Traders;
        if (!traders.TryGetValue(Traders.SKIER, out var skier))
        {
            logger.Error($"Couldn't find Trader: {Traders.SKIER}");
            return Task.CompletedTask;
        }
        GetExchangeRate(skier);
        skier.Base.Currency = CurrencyType.EUR;
        ConvertBarterCurrency(skier);
        var quests = databaseServer.GetTables().Templates.Quests;
        foreach (var quest in quests.Where(quest => quest.Value.TraderId == Traders.SKIER))
        {
            ConvertQuestCurrency(quest.Value);
        }
        
        return Task.CompletedTask;
    }
    
    private void GetExchangeRate(Trader trader)
    {
        var barterScheme = trader.Assort.BarterScheme;
        foreach (var scheme in barterScheme.SelectMany(barter => barter.Value.SelectMany(schemes => schemes.Where(scheme => scheme.Template == ItemTpl.MONEY_EUROS))))
        {
            if (scheme.Count != null)
                _exchangeRate = scheme.Count.Value;
        }
        logger.Info($"ExchangeRate: {_exchangeRate}");
    }

    private double? ExchangeCurrency(double? amt)
    {
        return amt / _exchangeRate;
    }

    private void ConvertBarterCurrency(Trader trader)
    {
        foreach (var scheme in from schemePair in trader.Assort.BarterScheme from schemes in schemePair.Value from scheme in schemes where scheme.Template == ItemTpl.MONEY_ROUBLES select scheme)
        {
            if (scheme.Count == null) continue;
            var orig = scheme.Count.Value;
            scheme.Count = ExchangeCurrency(orig);
            scheme.Template = ItemTpl.MONEY_EUROS;
        }
    }

    private void ConvertQuestCurrency(Quest quest)
    {
        if (quest.Rewards == null) return;
        foreach (var reward in quest.Rewards.Values.SelectMany(rewardList => rewardList.Where(reward => reward.Type == RewardType.Item)))
        {
            if (reward.Items == null) return;
            foreach (var item in reward.Items)
            {
                if (item.Template != ItemTpl.MONEY_ROUBLES) continue;
                var original = reward.Value;
                reward.Value = ExchangeCurrency(original);
                item.Template = ItemTpl.MONEY_EUROS;
            }
        }
    }
}