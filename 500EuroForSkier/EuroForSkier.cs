using JetBrains.Annotations;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace EuroForSkier;

[UsedImplicitly]
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 10)]
public class EuroForSkier(DatabaseServer databaseServer, ISptLogger<EuroForSkier> logger) : IOnLoad
{
    private double? _exchangeRate;
    private readonly MongoId _euroTradeId = new("686e340e6c2a18ed6b0e8c8c");
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
        _exchangeRate = barterScheme[_euroTradeId][0][0].Count;
        logger.Info($"ExchangeRate: {_exchangeRate}");
    }

    private double? ExchangeCurrency(double? amt)
    {
        return amt / _exchangeRate;
    }

    private void ConvertBarterCurrency(Trader trader)
    {
        foreach (var pair in trader.Assort.BarterScheme)
        {
            if (pair.Key == _euroTradeId) continue;
            if (pair.Value[0][0].Template != ItemTpl.MONEY_ROUBLES) continue;
            var originalItemCost = pair.Value[0][0].Count;
            pair.Value[0][0].Count = ExchangeCurrency(originalItemCost);
            pair.Value[0][0].Template = ItemTpl.MONEY_EUROS;
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