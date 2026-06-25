using System;
using System.Collections.Generic;

namespace Sunset.Core
{
    /// <summary>
    /// Стройплощадка: сколько ресурсов уже внесено в постройку против требуемого.
    /// Ресурсы вносятся постепенно или сразу; при заполнении всех ячеек постройка
    /// готова. Чистая логика — тестируемо; визуал (табличка, анимация) — в Game.
    /// </summary>
    public class ConstructionSite
    {
        public readonly BuildingDef def;
        private readonly Dictionary<string, int> _deposited = new Dictionary<string, int>();

        public ConstructionSite(BuildingDef def)
        {
            this.def = def ?? throw new ArgumentNullException(nameof(def));
        }

        public int Required(string res) => def.Cost(res);
        public int Deposited(string res) => _deposited.TryGetValue(res, out var v) ? v : 0;
        public int Remaining(string res) => Math.Max(0, Required(res) - Deposited(res));

        /// <summary>Внести до <paramref name="amount"/> ресурса; возвращает реально внесённое.</summary>
        public int Deposit(string res, int amount)
        {
            if (amount <= 0) return 0;
            int room = Remaining(res);
            int put = Math.Min(amount, room);
            if (put > 0) _deposited[res] = Deposited(res) + put;
            return put;
        }

        public bool IsComplete
        {
            get
            {
                foreach (var c in def.cost) if (Deposited(c.res) < c.amount) return false;
                return true;
            }
        }

        /// <summary>Доля готовности 0..1 (по сумме внесённых ресурсов).</summary>
        public float Progress
        {
            get
            {
                int total = def.TotalCost();
                if (total <= 0) return 1f;
                int dep = 0;
                foreach (var c in def.cost) dep += Math.Min(Deposited(c.res), c.amount);
                return (float)dep / total;
            }
        }
    }
}
