using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace TgLlmBot.Services.OpenAIClient.Costs;

public class DefaultCostContextStorage : ICostContextStorage
{
    private static readonly AsyncLocal<InternalCostStorage?> Cost = new();

    public bool TryGetCost([NotNullWhen(true)] out decimal? cost)
    {
        var storage = Cost.Value;
        if (storage is not null && storage.TryGetCost(out var storageValue))
        {
            cost = storageValue.Value;
            return true;
        }

        cost = null;
        return false;
    }

    public void SetCost(decimal cost)
    {
        Cost.Value ??= new();
        Cost.Value.SetValue(cost);
    }

    public void Initialize()
    {
        Cost.Value ??= new();
    }

    private sealed class InternalCostStorage
    {
        private decimal _cost;
        private bool _valueSet;

        public void SetValue(decimal cost)
        {
            _cost = cost;
            _valueSet = true;
        }

        public bool TryGetCost([NotNullWhen(true)] out decimal? cost)
        {
            if (_valueSet)
            {
                cost = _cost;
                return true;
            }

            cost = null;
            return false;
        }
    }
}
