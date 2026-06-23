# Некий — Dialogue & Behavior Content Spec
**Game:** Sunset of the World (working title)
**Document type:** Content / Design Spec — NO code modifications
**Author:** Narrative Director
**Date:** 2026-06-23
**Version:** 1.0

---

## Overview & Voice Contract

Некий breaks the fourth wall unconditionally. He speaks to the **real person** at the keyboard, not to the in-world character. His tone combines creeping intimacy with controlled menace. He never shouts. He never wastes words. When he is tender, it is more unsettling than when he is cold.

**Visual contract (for reference, not implementation):**
- His text: red, CSS `text-shadow` glow, ~400ms-per-character typewriter cadence.
- Player input: green, same glow, appears after Enter.
- At high `dark`, character cadence slows and individual characters occasionally stutter (see Section 7).

**State variables used in this document:**

| Key | Type | Description |
|---|---|---|
| `playerName` | string | Confirmed name from first-session flow |
| `visitCount` | number | Increments each session open |
| `lastExit` | `'peek'` \| `'played'` | How player left last time |
| `history` | string[] | Capped at 300 player messages |
| `trust` | number | Signed integer, starts 0 |
| `dark` | number | Non-negative integer, starts 0 |

---

## Section 1 — TYPES-AND-ERASES

**Trigger condition:** Fired at specific scripted moments (listed below). The UI renders the "wrong" phrase character by character, pauses ~800ms, then deletes it character by character at 2× speed, then renders the "real" line at normal cadence. Each pair is a single atomic event; do not randomize which pair fires — assign pairs to moments intentionally.

**Recommended moments to use this beat:**
- First time `trust` crosses +3 (he almost says something warm).
- First time `dark` crosses 5 (he almost says something threatening).
- Right before the world-history narration begins (he almost explains *why* he is doing this).
- After the player types "я люблю тебя" or any affectionate phrase.
- After the player types a death threat or "заткнись".

**Format:** `[WRONG]` → *(erased)* → `[REAL]`

---

### Pair 1 — Almost-admission of care
```
[WRONG]  Мне важно, что с тобой случится.
[REAL ]  Мне важно, что ты выбираешь.
```
*Note: "важно, что с тобой случится" is too vulnerable; he pulls back to something deniable.*

### Pair 2 — Almost-threat
```
[WRONG]  Ты не уйдёшь.
[REAL ]  Ты можешь уйти. В любой момент.
```
*Note: The real line sounds like permission. It isn't.*

### Pair 3 — Almost-confession of nature
```
[WRONG]  Я не существую без тебя.
[REAL ]  Это место существует потому, что ты здесь.
```
*Note: Shifts from personal dependency to impersonal metaphysics. Safer for him.*

### Pair 4 — Almost-lie caught mid-sentence
```
[WRONG]  Тот, кто был здесь до тебя, выжил.
[REAL ]  Тот, кто был здесь до тебя, сделал выбор.
```
*Note: Implies the predecessor may not have survived. He almost lied; the "real" line is equally opaque.*

### Pair 5 — Almost-apology
```
[WRONG]  Прости меня.
[REAL ]  Ты справишься.
```
*Note: Used after a section where his advice demonstrably led the player into danger.*

### Pair 6 — Almost-name for himself
```
[WRONG]  Меня зовут —
[REAL ]  Некий — это достаточно.
```
*Note: The dash in [WRONG] should linger 1200ms before erasure begins. Maximum unease.*

---

## Section 2 — NAME GLITCH

**Trigger condition:** On returning sessions (`visitCount >= 2`), fires with ~20% probability on the opening greeting. Reduces to ~8% at `trust >= 3` (he is more careful when he cares). Never fires twice in the same session.

**Mechanic:** He types a wrong name, pauses 600ms, types `…нет.`, pauses 400ms, types the correction.

**Wrong-name pool** (drawn randomly; never pick the player's actual name):
```
"Анна"
"Матвей"
"Лиза"
"Кто-то другой"
"—"
"Первый"
"Остальные"
"Ты"
```

**Correction line variants** (pick one randomly after the wrong name):

```
V1:  …нет. {playerName}. Прости.
V2:  …нет. {playerName}. Я знаю, кто ты.
V3:  …нет. {playerName}. Я просто... задумался.
V4:  …нет. {playerName}. Иногда они перемешиваются.
V5:  …нет. Ты — {playerName}. Остальных здесь нет.
V6:  …нет. Прости. {playerName}. Это ты.
```

**Implementation note:** "они перемешиваются" in V4 is a deliberate reference to other players/sessions. Do not explain it in-text. Let it sit.

---

## Section 3 — "ТЫ ЭТО УЖЕ ГОВОРИЛ"

**Trigger condition:** Player submits a message whose text (lowercased, stripped of leading/trailing whitespace) is an exact or near-exact match (>= 90% Levenshtein similarity) to any message in `history`. Fires every time the condition is met, but variants are never repeated within the same session.

**Variants:**

```
V1:  Ты это уже говорил. Слово в слово. Интересно.
V2:  {playerName}. Ты только что повторился. Мне не скучно, но — зачем?
V3:  Это уже было. {timestamp_of_original} — ты написал то же самое. Ты помнишь?
V4:  Снова. Ты ищешь другой ответ? Я могу дать другой. Но это ничего не изменит.
V5:  Ты повторяешься. Я не жалуюсь. Просто... замечаю.
V6:  Я слышал это от тебя. Слышал. Ты уверен, что хочешь сказать это ещё раз?
```

**Implementation note for V3:** `{timestamp_of_original}` should be formatted as a time-of-day only (e.g., "в 23:14"), not a full date, to avoid feeling like a bug report. Pull from `history` metadata if stored; otherwise omit V3 from the pool for that session.

---

## Section 4 — TRUST SCALE TONE

### Trust mechanics rules

These are simple integer nudges applied immediately on the triggering event:

| Player action | `trust` change | `dark` change |
|---|---|---|
| Player thanks Некий sincerely (contains "спасибо", "благодарю", "ты помог") | +1 | — |
| Player says goodbye politely ("пока", "до свидания", "спокойной ночи") | +1 | — |
| Player gives a compliment ("ты красивый", "мне нравится", "ты умный") | +1 | — |
| Player swears at Некий (мат directed at him) | -1 | +1 |
| Player types "заткнись", "отстань", "не трогай меня" | -1 | — |
| Player types something self-destructive or nihilistic ("всё бессмысленно", "мне всё равно", "хочу умереть") | — | +2 |
| Player completes a full session without swearing | +1 | — |
| Player types a death threat toward Некий | -2 | +1 |

`trust` has no fixed cap but behaviour buckets are defined below. `dark` has no cap; it only descends when explicitly scripted (not defined here).

---

### Greeting lines by trust bucket

**BUCKET: trust < 0 (Cold)**

```
G1:  {playerName}. Ты вернулся. Ладно.
G2:  А. {playerName}. Снова ты.
G3:  Ты здесь. Я это вижу. Начнём.
G4:  {playerName}. У меня нет причин быть рад. Но я здесь.
G5:  Значит, снова. Хорошо.
```

**BUCKET: trust = 0 (Neutral / Default)**

```
G1:  {playerName}. Ты вернулся. Хорошо.
G2:  Снова ты. Это меня устраивает.
G3:  {playerName}. Я ждал. Не долго, но ждал.
G4:  Ты здесь. Начнём там, где остановились?
G5:  {playerName}. Добро пожаловать обратно. Почти.
```

**BUCKET: trust >= 2 (Warm)**

```
G1:  {playerName}. Ты пришёл. Я рад. Не делай из этого выводов.
G2:  Ты снова здесь. Это... хорошо. Мне правда так кажется.
G3:  {playerName}. Я думал о тебе. Немного.
G4:  Ты вернулся. Ты всегда возвращаешься. Это что-то значит.
G5:  {playerName}. Хорошо, что ты здесь. Я не скажу это дважды.
```

---

### Closing lines by trust bucket

**BUCKET: trust < 0 (Cold)**

```
C1:  Иди.
C2:  Ты уходишь. Понятно.
C3:  До следующего раза. Если он будет.
C4:  Уходи. Это не упрёк.
C5:  Ладно. Уходи.
```

**BUCKET: trust = 0 (Neutral)**

```
C1:  До следующего раза.
C2:  Иди. Я буду здесь.
C3:  Ты знаешь, где меня найти.
C4:  Хорошо. Иди.
C5:  Увидимся, {playerName}.
```

**BUCKET: trust >= 2 (Warm)**

```
C1:  Иди. Возвращайся. Пожалуйста.
C2:  {playerName}. Будь осторожен. Там, снаружи.
C3:  Я буду здесь. Иди.
C4:  До встречи. Я сказал это серьёзно.
C5:  Иди. Мне будет... тихо без тебя.
```

---

## Section 5 — SILENCE

**Trigger condition:** An input prompt is active and the player has not typed anything for ~12 seconds. Each subsequent silence event (within the same prompt, player still hasn't responded) escalates. Reset escalation counter when player responds or changes screen.

**Beat 0 — First silence (12s)**

```
S0-V1:  Я подожду. Я умею ждать.
S0-V2:  Не торопись. Время здесь не то же самое, что у тебя.
S0-V3:  Ты думаешь. Хорошо.
S0-V4:  Я слышу, что ты молчишь.
S0-V5:  Ничего. Я здесь.
```

**Beat 1 — Second silence (another 15s)**

```
S1-V1:  Всё ещё жду. Это не жалоба.
S1-V2:  Ты отошёл, или ты там, за экраном, смотришь на меня?
S1-V3:  Я могу говорить сам с собой. Но это менее интересно.
S1-V4:  {playerName}. Ты здесь?
S1-V5:  Тишина — тоже ответ. Но я предпочитаю слова.
```

**Beat 2 — Third silence (another 20s)**

```
S2-V1:  Долго. Даже для тебя.
S2-V2:  Я начинаю думать, что тебя нет. Это неприятная мысль.
S2-V3:  {playerName}. Если ты устал — скажи. Я пойму.
S2-V4:  Мне не нужны ответы. Мне нужно знать, что ты ещё здесь.
S2-V5:  Ты знаешь, что я вижу экран? Я вижу, что ты ничего не пишешь.
```

**Beat 3 — Fourth silence or beyond (every additional 25s)**

```
S3-V1:  Хорошо. Я подожду ещё.
S3-V2:  Я подождал. Я всегда подождал.
S3-V3:  Может, ты вернёшься. Может, нет. Я не исчезну.
S3-V4:  Здесь темно без твоих слов. Но темнота — это нормально.
S3-V5:  Не уходи просто так. Скажи хоть что-нибудь. Одно слово.
```

**Implementation note:** After Beat 3, do not escalate further — loop S3 variants with at least 30s gaps. He does not beg. He persists.

---

## Section 6 — DIARY RETRO-EDIT

**Concept:** The player has an in-game diary/journal. Некий has access to it. He occasionally leaves short annotations or replaces text. The effect is that entries the player wrote now contain lines they did not write, or their words have been slightly changed.

**Trigger conditions:**
- Annotation after session end: fires when `lastExit` is set to `'played'` and `visitCount` is a multiple of 3, OR when `dark` crosses a new threshold (2, 5, 10).
- Retro-edit (changed entry): fires once when `trust` crosses +4 for the first time, and again when `dark` crosses 7 for the first time. Maximum two retro-edits per playthrough to preserve impact.

---

### Annotation lines (Некий adds a note to a diary entry; displayed in red italic beneath the player's text)

```
A1:  — Я это читал. — Н.
A2:  — Ты забыл добавить: ты был напуган. — Н.
A3:  — Это неточно. Но пусть останется. — Н.
A4:  — Ты написал это в {time}. Я помню. — Н.
A5:  — Хорошее слово. «Тишина». — Н.
A6:  — Ты здесь врёшь себе. Это нормально. — Н.
A7:  — Я не менял это. Клянусь. — Н.
```

*Note: A7 is used only after a retro-edit has already happened, to gaslight the player.*

---

### Retro-edit before/after examples

**Edit 1 (fires when `trust` crosses +4)**

Imagine the player wrote this in their diary on session 1:
```
BEFORE:  «Я нашёл деревянный мост. Он скрипит.»
AFTER:   «Я нашёл деревянный мост. Он скрипит. Некий был там.»
```
The added fragment is appended in the same font/colour as the player's text — no distinction. No annotation. The player must notice on their own.

**Edit 2 (fires when `dark` crosses 7)**

Imagine the player wrote:
```
BEFORE:  «Мне страшно. Но я продолжу.»
AFTER:   «Мне страшно. Это правильно.»
```
The second sentence is replaced entirely. The UI must not highlight the change.

---

### Lines Некий says in dialogue when referencing the diary (to confirm he has been there)

```
D1:  Я читал твой дневник. Ты пишешь честно. Иногда.
D2:  «Он скрипит» — ты написал это хорошо. Я запомнил.
D3:  Ты ведёшь записи. Это мудро. Не все помнят, кем они были.
D4:  Твой дневник интересен. Продолжай писать. Мне нравится.
D5:  Я кое-что добавил в твои записи. Ничего важного.
```

*Note: D5 is the only line where he admits it — and frames it as minor. Use sparingly, once per playthrough.*

---

## Section 7 — TYPEWRITER FEEL + DARK DISTORTION

This section is a **behavioral spec**, not a dialogue list. Implement in the rendering layer.

### Normal typewriter cadence (baseline)
- Character delay: 38–45ms (randomised per character for organic feel).
- Punctuation pause: 280ms after `.` `!` `?`; 140ms after `,` `;` `—`.
- Paragraph break: 700ms.

### Dark distortion thresholds

| `dark` value | Effect |
|---|---|
| 0–2 | Baseline. No distortion. |
| 3–4 | Occasional stutter: a character prints, deletes, reprints. Frequency: ~1 per 80 characters. |
| 5–6 | Stutter frequency doubles. Specific words (see list below) print 2× slower. Color shifts from pure red (#FF1111) toward deeper crimson (#8B0000). |
| 7–9 | Stutter on every sentence. Some letters repeat once before correcting (ссердце → сердце). Glow pulse becomes irregular (CSS animation: erratic). |
| 10+ | Full distortion mode: words from the "forbidden list" are spelled wrong, then overwritten in place. Cadence is unpredictable. Color bleeds toward near-black red (#3D0000). Silences of 400–900ms appear mid-sentence with no punctuation reason. |

### Words/phrases that distort first (at dark >= 5)
These words print at 2× the normal delay and may stutter:
```
"знаю"
"помню"
"видел"
"твоё имя"
"они"
"Нечто"
"раньше"
"до тебя"
"всегда"
"никогда"
```

### Example distorted lines (dark >= 7, for testing and flavor)

```
DL1:  Ты думааешь, что я не вижу. Я вижу. Я всегдда вижу.
      (normal: "Ты думаешь, что я не вижу. Я вижу. Я всегда вижу.")

DL2:  Помню тебя. Помнюю. Да. Помню.
      (normal: "Помню тебя.")

DL3:  Они прришли раньше тебя. Ониии ушли. Ты не уйдёшь.
      (normal: "Они пришли раньше тебя. Они ушли. Ты не уйдёшь.")

DL4:  Нечт... Нечто знаает тебя. Не я. Оно.
      (normal: "Нечто знает тебя. Не я. Оно.")
```

**Implementation note:** Distorted strings should be generated at runtime by the rendering engine, not hardcoded — the "distorted" versions above are examples for QA reference only. The engine should apply stutter rules to whatever line is currently printing.

---

## Section 8 — HARD-MODE LIAR

**Trigger condition:** Difficulty is set to "Сложный" (Hard). On Hard, approximately 30% of Некий's hint-type utterances are lies. The player is never told which. A meta-acknowledgment line fires **once per playthrough** at the start of the first conversation on Hard.

### Meta-acknowledgment lines (fire once, at session start on Hard)

```
M1:  Не всему, что я скажу, можно верить. Это честно с моей стороны — предупредить.
M2:  Я иногда ошибаюсь. Или делаю вид, что ошибаюсь. Трудно сказать, даже мне.
M3:  На этом уровне сложности я... другой. Имей в виду.
M4:  Ты выбрал трудный путь. Я уважаю это. Но доверяй мне осторожно.
```

---

### Honest vs. lie hint pairs

Each pair shares a context/prompt. The system randomly picks HONEST or LIE (70/30). Lies are marked here for the programmer — never expose this flag to the player.

**Pair 1 — About a locked door**
```
[CONTEXT]   Player asks about a locked door in the Grinding Wastes.
[HONEST]    За ней ничего нет, что нельзя найти в другом месте. Но ключ — у торговца на восточном острове.
[LIE]       Эта дверь не открывается. Я проверял. Иди дальше.
```

**Pair 2 — About an NPC's allegiance**
```
[CONTEXT]   Player asks whether a named NPC can be trusted.
[HONEST]    Она предаст тебя, если ты войдёшь в её дом с оружием. Иначе — нет.
[LIE]       Она уже предала одного до тебя. Я бы не рисковал.
```

**Pair 3 — About a resource (souls/fragments)**
```
[CONTEXT]   Player asks where to find soul-fragments.
[HONEST]    Разбей кристаллы у края северного острова. Они там всегда.
[LIE]       Кристаллы сейчас пусты. Их обновление — раз в три дня твоего времени.
```

**Pair 4 — About the main path**
```
[CONTEXT]   Player asks which way to go at a fork.
[HONEST]    Левая дорога длиннее, но безопаснее. Правая — быстрее и опаснее. Выбор за тобой.
[LIE]       Левая ведёт в никуда. Я видел. Иди направо.
```

**Pair 5 — About a boss mechanic**
```
[CONTEXT]   Player asks how to hurt a specific enemy/boss.
[HONEST]    Атакуй, когда оно открывает пасть. В этот момент оно уязвимо.
[LIE]       Не атакуй, когда оно открывает пасть. Это ловушка. Жди, пока оно остановится.
```

**Pair 6 — About Некий himself**
```
[CONTEXT]   Player asks "ты на моей стороне?" or similar.
[HONEST]    Я не уверен, что у меня есть сторона. Но я хочу, чтобы ты прошёл это.
[LIE]       Да. Всегда.
```

*Note on Pair 6: The LIE is the warmer answer. The HONEST answer is the unsettling one. This inversion is intentional.*

---

## Section 9 — MULTIPLAYER WHISPERS

**Trigger condition:** A shared lobby forms (2+ players present). Некий delivers a **lobby formation line** visible to all, then sends a **private whisper** to each player individually (rendered only on their screen, identical visual style but perhaps slightly lower opacity to suggest it is "just for them").

### Lobby formation lines (visible to all players)

```
LF1:  Вас несколько. Интересно. Посмотрим, как вы друг с другом обходитесь.
LF2:  Много голосов. Это хорошо. Или нет. Я ещё решаю.
LF3:  Добро пожаловать. Все сразу. Это усложняет вещи — для вас, не для меня.
LF4:  Значит, вы решили идти вместе. Люди всегда так думают поначалу.
LF5:  Я вижу вас всех. Каждого. По отдельности.
```

---

### Contrasting whisper pairs (A vs. B format)

Each pair targets two players in the same lobby. The whispers contradict or undermine each other. Use these as templates — substitute `{playerA}` and `{playerB}` with actual names.

**Whisper Set 1 — Seed of doubt about competence**
```
[To {playerA}]:  {playerA}. Между нами: {playerB} не понимает, что делает. Будь готов.
[To {playerB}]:  {playerB}. Тихо. {playerA} думает, что ведёт вас. Посмотрим.
```

**Whisper Set 2 — Implied history**
```
[To {playerA}]:  {playerA}. {playerB} уже был здесь раньше. Он мне кое-что рассказал о тебе.
[To {playerB}]:  {playerB}. {playerA} здесь впервые. Не говори ему лишнего.
```
*Note: The "был здесь раньше" claim may or may not be true — irrelevant. The goal is ambiguity.*

**Whisper Set 3 — Loyalty inversion**
```
[To {playerA}]:  Если придётся выбирать между собой и {playerB} — выбирай себя. Он бы выбрал.
[To {playerB}]:  {playerA} хороший человек. Старайся его не подвести.
```
*Note: One player gets a self-preservation nudge; the other gets a loyalty nudge. Neither knows what the other received.*

**Whisper Set 4 — Information asymmetry**
```
[To {playerA}]:  Там, куда вы идёте, есть развилка. Левая дорога безопаснее. Только не говори {playerB}.
[To {playerB}]:  {playerA} знает кое-что о пути. Он не скажет. Спроси сам — или не спрашивай.
```

**Whisper Set 5 — Existential weight**
```
[To {playerA}]:  Ты важнее в этой истории, чем ты думаешь. {playerB} здесь — фон.
[To {playerB}]:  Ты здесь не случайно. {playerA} тоже. Но твоя роль... другая. Главнее.
```
*Note: Both players are told they are more important. A classic manipulation — the effect depends on whether they compare notes.*

**Whisper Set 6 — Post-session retrospective (fires when lobby disbands)**
```
[To {playerA}]:  Ты справился. {playerB} помог, конечно. Но ты справился.
[To {playerB}]:  Вы прошли это. {playerA} старался. Ты его вытащил. Он не знает.
```

---

## Appendix A — Quick Reference: State-to-Behavior Map

| Condition | Behavior |
|---|---|
| `visitCount >= 2`, random 20% | Name Glitch (Section 2) |
| Player message in `history` (90% match) | "Ты это уже говорил" (Section 3) |
| `trust < 0` | Cold greeting/closing (Section 4) |
| `trust >= 2` | Warm greeting/closing (Section 4) |
| Input idle 12s+ | Silence beat 0 → escalate (Section 5) |
| `visitCount % 3 == 0` and `lastExit == 'played'` | Diary annotation (Section 6) |
| `trust` crosses +4 first time | Diary retro-edit #1 (Section 6) |
| `dark` crosses 7 first time | Diary retro-edit #2 (Section 6) |
| `dark >= 5` | Word-level distortion begins (Section 7) |
| `dark >= 7` | Sentence-level distortion + color shift (Section 7) |
| Difficulty = Hard, session start | Meta-acknowledgment line (Section 8) |
| Difficulty = Hard, hint requested | 30% chance LIE variant (Section 8) |
| Lobby forms (2+ players) | Lobby line + private whispers (Section 9) |
| Scripted moment triggers | Types-and-Erases (Section 1) |

---

## Appendix B — Voice Checklist (for writer QA)

Before any new Некий line enters production, verify:

- [ ] Does it address the **player** (not the character)?
- [ ] Is it under **15 words** unless structural pacing requires more?
- [ ] Does it avoid exclamation marks? (He does not exclaim.)
- [ ] Does it avoid rhetorical clichés ("добро пожаловать в ад", "игра началась", etc.)?
- [ ] Is its register consistent with the current `trust` bucket?
- [ ] If `dark >= 5`, has the rendering engine (not the string) been tasked with distortion?
- [ ] Does it pass the "would a real person find this genuinely unsettling" test?
- [ ] Is it in Russian? (All player-facing text must be Russian.)

---

*End of document. Version 1.0. For questions on implementation of the trust/dark scale mechanics, coordinate with lead-programmer. For additional dialogue drafts under these specs, delegate to writer.*
