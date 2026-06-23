# Установленные наборы Claude Code (сторонние)

Эти инструменты подключены в репозиторий и активируются в **будущих** сессиях
Claude Code на этом проекте (скиллы/агенты читаются при старте, не на лету).

## 1. LibreUIUX — UI/UX (MIT)
- Источник: https://github.com/HermeticOrmus/LibreUIUX-Claude-Code
- Автор: Hermetic Ormus · Лицензия: MIT (`.claude/libreuiux/LICENSE`)
- Установлено:
  - `.claude/commands/ui-*.md` — слэш-команды: `/ui-review`, `/ui-critique`,
    `/ui-synth`, `/ui-modern`, `/ui-responsive`;
  - `.claude/agents/synthesis-master.md` — агент-дизайнер UI;
  - `.claude/libreuiux/resources/` и `.claude/libreuiux/templates/` — справочники
    и шаблоны дизайн-систем (используются как референс; светлый `modern-webapp`
    НЕ ставился корневым CLAUDE.md, чтобы не конфликтовать с тёмным стилем игры).

## 2. Claude Code Game Studios — геймдев (MIT)
- Источник: https://github.com/Donchitos/Claude-Code-Game-Studios
- Автор: Donchitos · Лицензия: MIT (`.claude/LICENSE.ccgs`)
- Установлено:
  - `.claude/skills/` — ~50 скиллов геймдева (brainstorm, prototype, quick-design,
    design-review, balance-check, playtest-report, qa-plan, milestone-review и др.);
  - `.claude/agents/` — 49 агентов студии (game-designer, narrative-director,
    art-director, ui-programmer, gameplay-programmer, level-designer,
    economy-designer, sound-designer и др.);
  - `.claude/docs/` — справочники.
  - Корневой `CLAUDE.md` пакета НЕ ставился (он требует выбрать Godot/Unity/Unreal,
    что конфликтует с нашим веб-прототипом). Engine-специфичные агенты (UE/Godot)
    можно игнорировать — нужны общие: дизайн, нарратив, арт, продакшн.

> Примечание: эти наборы не меняют поведение текущей сессии и не трогают
> harness-настройки. Их `settings.json`/hooks намеренно не копировались.
