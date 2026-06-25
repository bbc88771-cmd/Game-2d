using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Запас ресурсов игрока (древесина/камень/ветки/глина и др.). Чистая логика —
    /// без зависимостей от движка, тестируемо. Используется при сборе ресурсов и при
    /// внесении их в стройплощадку.
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<string, int> _amounts = new Dictionary<string, int>();

        public int Get(string id)
            => id != null && _amounts.TryGetValue(id, out var v) ? v : 0;

        public void Add(string id, int n)
        {
            if (string.IsNullOrEmpty(id) || n <= 0) return;
            _amounts[id] = Get(id) + n;
        }

        public bool Has(string id, int n) => Get(id) >= n;

        /// <summary>Списать ровно n (если хватает). Возвращает успех.</summary>
        public bool TryConsume(string id, int n)
        {
            if (n <= 0) return true;
            if (Get(id) < n) return false;
            _amounts[id] = Get(id) - n;
            return true;
        }

        /// <summary>Взять до n единиц; возвращает сколько реально взято (для частичного внесения).</summary>
        public int Take(string id, int n)
        {
            if (n <= 0) return 0;
            int take = Math.Min(n, Get(id));
            if (take > 0) _amounts[id] = Get(id) - take;
            return take;
        }
    }
}
