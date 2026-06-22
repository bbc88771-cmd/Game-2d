/* ===========================================================
   Sunset of the World — логика главного меню (веб-прототип)
   Покрывает кнопки из ТЗ + мультиплеер/моды из присланного фото:
   Новая игра · Продолжить · Создать лобби · Ввести код ·
   Настройки · Дневник · Выход · Моды.
   Сетевой код пока имитируется локально (заглушки лобби) — это
   каркас под будущую реальную синхронизацию.
   =========================================================== */
(() => {
  "use strict";

  const $ = (sel, root = document) => root.querySelector(sel);
  const SAVE_KEY = "sotw_save";
  const SETTINGS_KEY = "sotw_settings";

  // ---------- настройки (persist) ----------
  const defaultSettings = {
    music: 70, sfx: 80, lang: "ru", difficulty: "normal", fullscreen: false,
  };
  const loadSettings = () => {
    try { return { ...defaultSettings, ...JSON.parse(localStorage.getItem(SETTINGS_KEY) || "{}") }; }
    catch { return { ...defaultSettings }; }
  };
  const saveSettings = (s) => localStorage.setItem(SETTINGS_KEY, JSON.stringify(s));
  let settings = loadSettings();

  const DIFFICULTIES = [
    ["creative", "Творческий", "Без угроз — стройка и исследование"],
    ["easy", "Лёгкий", "Мягкие враги, без доп-боссов (~30 ч)"],
    ["normal", "Обычный", "Базовый баланс игры"],
    ["hard", "Сложный", "Доп. мини-боссы, квесты, предметы"],
    ["nightmare", "Хард", "Максимум: редкая 5-я способность, секреты"],
  ];

  const HEROES = [
    ["tessi", "Тесси", "Маг — дальний бой, контроль. Слепая, чувствует мир"],
    ["amira", "Амира", "Танк — защита, агро. Заботится о других"],
    ["swordsman", "Мечник", "Ближний бой, урон. Личная трагедия"],
    ["kaijo", "Кайджо", "Ниндзя — мобильность, криты. Лёгкий, с юмором"],
    ["walter", "Уолтер", "Технарь — турели, механизмы. С роботом Чипом"],
  ];

  // ===========================================================
  //  Модальная система
  // ===========================================================
  const modalRoot = $("#modal-root");
  const modalTitle = $("#modal-title");
  const modalBody = $("#modal-body");

  function openModal(title, html) {
    modalTitle.textContent = title;
    modalBody.innerHTML = "";
    if (typeof html === "string") modalBody.innerHTML = html;
    else if (html) modalBody.appendChild(html);
    modalRoot.hidden = false;
  }
  function closeModal() { modalRoot.hidden = true; }

  modalRoot.addEventListener("click", (e) => {
    if (e.target.matches("[data-close]")) closeModal();
  });
  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") {
      if (!$("#neko-intro").hidden) return; // интро пропускается своей кнопкой
      closeModal();
    }
  });

  // ===========================================================
  //  Новая игра → сложность → герой → интро «Некого»
  // ===========================================================
  let playerName = null;

  // Новая игра начинается с появления «Некого» (диалог об имени),
  // затем — выбор сложности и героя.
  function newGameFlow() {
    nekoNameIntro((name) => {
      playerName = name;
      chooseDifficulty();
    });
  }

  function chooseDifficulty() {
    const list = DIFFICULTIES.map(([id, name, sub]) =>
      `<div class="opt${settings.difficulty === id ? " is-selected" : ""}" data-diff="${id}">
         <span>${name}</span><span class="opt-sub">${sub}</span>
       </div>`).join("");
    const wrap = document.createElement("div");
    wrap.innerHTML = `<p>Выберите сложность. На высоких — больше боссов, квестов и предметов.</p>
      <div class="opt-list">${list}</div>`;
    wrap.querySelectorAll("[data-diff]").forEach((node) =>
      node.addEventListener("click", () => {
        settings.difficulty = node.dataset.diff;
        saveSettings(settings);
        chooseHero();
      }));
    openModal("Новая игра — сложность", wrap);
  }

  function chooseHero() {
    const list = HEROES.map(([id, name, sub]) =>
      `<div class="opt" data-hero="${id}">
         <span>${name}</span><span class="opt-sub">${sub}</span>
       </div>`).join("");
    const wrap = document.createElement("div");
    wrap.innerHTML = `<p>Выберите героя. У каждого своя история, навыки и реплики «Некого».</p>
      <div class="opt-list">${list}</div>`;
    wrap.querySelectorAll("[data-hero]").forEach((node) =>
      node.addEventListener("click", () => {
        closeModal();
        startWorldStub(node.dataset.hero);
      }));
    openModal("Новая игра — герой", wrap);
  }

  // ===========================================================
  //  Интро «Некого»: чёрный экран → бегущий красный код →
  //  «Кто ты?» → распознавание имени → подтверждение → «Интересно»
  // ===========================================================
  const wait = (ms) => new Promise((r) => setTimeout(r, ms));
  let skipNeko = false;

  function makeCode(lines) {
    const ch = "01xX#@/\\|<>[]{}()=+*-ABCDEF0123456789░▒▓§∆ΣλØ";
    const rnd = (n) => Array.from({ length: n }, () => ch[Math.floor(Math.random() * ch.length)]).join("");
    const seg = () => ["0x" + rnd(4), "INIT", "soul.bind(" + rnd(3) + ")", "who_are_you",
      "0b" + rnd(6), "trace[" + rnd(2) + "]", "rift.open()", "mem.scan", "::" + rnd(5),
      "echo(" + rnd(3) + ")", "WAKE", "bind(" + rnd(2) + ")"][Math.floor(Math.random() * 12)];
    const out = [];
    for (let i = 0; i < lines; i++) {
      let line = "";
      while (line.length < 78) line += seg() + "  ";
      out.push(line);
    }
    return out.join("\n");
  }

  function codeRain(dur) {
    return new Promise((resolve) => {
      const code = $("#neko-code");
      code.textContent = makeCode(30);
      code.classList.remove("run"); void code.offsetWidth; code.classList.add("run");
      const start = Date.now();
      const tick = () => {
        if (skipNeko || Date.now() - start >= dur) {
          code.classList.remove("run"); code.textContent = ""; resolve();
        } else setTimeout(tick, 80);
      };
      setTimeout(tick, 80);
    });
  }

  function nekoType(text, hold = 850) {
    return new Promise((resolve) => {
      const el = $("#neko-line");
      let i = 0;
      const step = () => {
        el.innerHTML = text.slice(0, i) + '<span class="neko-caret">▌</span>';
        if (i++ < text.length) setTimeout(step, skipNeko ? 4 : 45);
        else setTimeout(() => { el.textContent = text; resolve(); }, skipNeko ? 90 : hold);
      };
      step();
    });
  }

  function askLine() {
    return new Promise((resolve) => {
      const row = $("#neko-input-row"), input = $("#neko-input");
      input.value = ""; row.hidden = false; input.focus();
      const submit = (e) => {
        e.preventDefault();
        const v = input.value;
        row.hidden = true; row.removeEventListener("submit", submit);
        resolve(v);
      };
      row.addEventListener("submit", submit);
    });
  }

  function askConfirm() {
    return new Promise((resolve) => {
      const box = $("#neko-confirm");
      const yes = box.querySelector("[data-yes]"), no = box.querySelector("[data-no]");
      box.hidden = false;
      const done = (val) => {
        box.hidden = true;
        yes.removeEventListener("click", oy); no.removeEventListener("click", on);
        resolve(val);
      };
      const oy = () => done(true), on = () => done(false);
      yes.addEventListener("click", oy); no.addEventListener("click", on);
    });
  }

  // Слова, которые точно не имя (для распознавания имени в фразе)
  const NEKO_STOP = new Set(("я ты он она оно мы вы меня тебя себя нас вас зовут звать имя это как кто " +
    "что чё че а и но или о ну да нет не привет здравствуй здравствуйте эй мой моё мое моя мне тебе " +
    "называй можешь блин нихуя нифига себе вот так тут здесь is my name the").split(/\s+/));
  const SASS = ["Я спросил: КТО ТЫ.", "Слишком много текста. Просто напиши своё имя.", "Имя."];

  function parseName(raw) {
    const s = (raw || "").trim();
    if (!s) return { ok: false, msg: "Имя." };
    const words = s.split(/[\s,.;:!?…"'«»()\[\]]+/).filter(Boolean);
    const looksName = (w) => /[A-Za-zА-Яа-яЁё]/.test(w) && /^[\wА-Яа-яЁё-]{1,24}$/.test(w);
    const cand = words.filter((w) => looksName(w) && !NEKO_STOP.has(w.toLowerCase()));
    if (s.length > 40 || words.length > 4) return { ok: false, sass: true };
    if (cand.length === 0) return { ok: false, sass: true };
    let name = cand.find((w) => /^[A-ZА-ЯЁ]/.test(w)) || cand[0];
    name = name.charAt(0).toUpperCase() + name.slice(1);
    return { ok: true, name };
  }

  async function askName() {
    let sassIdx = 0;
    while (true) {
      const raw = await askLine();
      const res = parseName(raw);
      if (!res.ok) {
        await nekoType(res.sass ? SASS[sassIdx++ % SASS.length] : res.msg, 650);
        continue;
      }
      await nekoType(`${res.name}? Так тебя зовут?`, 400);
      if (await askConfirm()) return res.name;
      await nekoType("Тогда кто?", 550);
    }
  }

  async function nekoNameIntro(onDone) {
    const intro = $("#neko-intro"), skip = $("#neko-skip");
    skipNeko = false;
    intro.hidden = false;
    const onSkip = () => { skipNeko = true; };
    skip.addEventListener("click", onSkip);

    await codeRain(2000);
    await nekoType("Кто ты?", 500);
    const name = await askName();
    await nekoType("…", 250);
    await codeRain(1500);
    await nekoType(`«${name}». Интересно.`, 1000);
    await wait(skipNeko ? 150 : 750);

    skip.removeEventListener("click", onSkip);
    $("#neko-line").textContent = "";
    intro.hidden = true;
    onDone(name);
  }

  function startWorldStub(heroId) {
    // запись «черновика» сейва, чтобы заработала кнопка Продолжить
    const heroName = (HEROES.find((h) => h[0] === heroId) || [, "—"])[1];
    localStorage.setItem(SAVE_KEY, JSON.stringify({
      hero: heroId, name: playerName, difficulty: settings.difficulty, createdAt: Date.now(),
    }));
    refreshContinue();
    closeModal();
    openModal("Стартовый остров", `
      <p>${playerName ? `«${playerName}», т` : "Т"}ы очнулся в разорванном мире после катаклизма.</p>
      <p>Герой: <b>${heroName}</b> · Сложность: <b>${diffName(settings.difficulty)}</b></p>
      <p>Здесь начинается мир «Sunset of the World». Геймплейный прототип
      (движение, сбор ресурсов, стройка, бой) — следующий шаг разработки.</p>
      <div class="row end"><button class="btn primary" data-close>В меню</button></div>`);
  }
  const diffName = (id) => (DIFFICULTIES.find((d) => d[0] === id) || [, "—"])[1];

  // ===========================================================
  //  Мультиплеер — Создать лобби / Ввести код  (заглушка сети)
  // ===========================================================
  function randomCode() {
    const a = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    return Array.from({ length: 6 }, () => a[Math.floor(Math.random() * a.length)]).join("");
  }

  function createLobby() {
    const code = randomCode();
    const wrap = document.createElement("div");
    wrap.innerHTML = `
      <p>Поделитесь кодом с друзьями (до 5 игроков). Когда все зайдут — начинайте.</p>
      <div class="code-box">${code}</div>
      <div class="row"><button class="btn" id="copy">Скопировать код</button>
        <span class="hint" id="copied" style="opacity:0">скопировано ✓</span></div>
      <div class="players" id="players">
        <span class="player-chip host"><span class="dot"></span>Вы (хост)</span>
      </div>
      <p class="hint" style="margin-top:18px">Сетевой слой пока имитируется локально —
      это каркас под реальную синхронизацию мира, стройки и боя.</p>
      <div class="row end"><button class="btn primary" id="start" disabled>Ожидание игроков…</button></div>`;
    openModal("Создать лобби", wrap);

    $("#copy", wrap).addEventListener("click", async () => {
      try { await navigator.clipboard.writeText(code); } catch {}
      const c = $("#copied", wrap); c.style.opacity = 1; setTimeout(() => (c.style.opacity = 0), 1500);
    });

    // имитация подключения игроков
    const players = $("#players", wrap);
    const names = ["Странник", "Кузнец", "Следопыт", "Жрица"];
    let joined = 0;
    const startBtn = $("#start", wrap);
    const tick = () => {
      if (modalRoot.hidden || joined >= names.length) {
        if (joined > 0) { startBtn.disabled = false; startBtn.textContent = "Начать игру"; }
        return;
      }
      joined++;
      const chip = document.createElement("span");
      chip.className = "player-chip";
      chip.innerHTML = `<span class="dot"></span>${names[joined - 1]}`;
      players.appendChild(chip);
      startBtn.disabled = false;
      startBtn.textContent = "Начать игру";
      if (joined < names.length) setTimeout(tick, 1400 + Math.random() * 1600);
    };
    setTimeout(tick, 1600);
    startBtn.addEventListener("click", () => { closeModal(); newGameFlow(); });
  }

  function joinLobby() {
    const wrap = document.createElement("div");
    wrap.innerHTML = `
      <p>Введите код лобби друга, чтобы войти в общий мир.</p>
      <div class="field">
        <label for="code">Код лобби</label>
        <input id="code" type="text" maxlength="6" placeholder="ABC123"
               autocomplete="off" spellcheck="false"
               style="text-transform:uppercase;letter-spacing:.3em;text-align:center;font-family:'DejaVu Sans Mono',monospace" />
      </div>
      <p class="hint" id="status"></p>
      <div class="row end">
        <button class="btn" data-close>Отмена</button>
        <button class="btn primary" id="join">Войти</button>
      </div>`;
    openModal("Ввести код", wrap);
    const input = $("#code", wrap);
    const status = $("#status", wrap);
    input.focus();
    $("#join", wrap).addEventListener("click", () => {
      const code = input.value.trim().toUpperCase();
      if (code.length < 4) { status.textContent = "Введите корректный код (6 символов)."; return; }
      status.textContent = `Подключение к лобби ${code}…`;
      setTimeout(() => {
        status.textContent = "Подключено. Ожидаем старт от хоста…";
      }, 1200);
    });
    input.addEventListener("keydown", (e) => { if (e.key === "Enter") $("#join", wrap).click(); });
  }

  // ===========================================================
  //  Настройки
  // ===========================================================
  function openSettings() {
    const s = settings;
    const wrap = document.createElement("div");
    wrap.innerHTML = `
      <div class="field"><label>Музыка</label><input type="range" min="0" max="100" id="music" value="${s.music}"></div>
      <div class="field"><label>Звуки</label><input type="range" min="0" max="100" id="sfx" value="${s.sfx}"></div>
      <div class="field"><label>Язык</label>
        <select id="lang">
          <option value="ru"${s.lang === "ru" ? " selected" : ""}>Русский</option>
          <option value="en"${s.lang === "en" ? " selected" : ""}>English (перевод позже)</option>
        </select></div>
      <div class="field"><label>Сложность по умолчанию</label>
        <select id="difficulty">
          ${DIFFICULTIES.map(([id, name]) => `<option value="${id}"${s.difficulty === id ? " selected" : ""}>${name}</option>`).join("")}
        </select></div>
      <div class="row" style="justify-content:space-between;margin:6px 0 18px">
        <label>Полноэкранный режим</label>
        <span class="toggle"><input type="checkbox" id="fs"${s.fullscreen ? " checked" : ""}><span class="track"></span></span>
      </div>
      <div class="row end"><button class="btn primary" id="apply">Сохранить</button></div>`;
    openModal("Настройки", wrap);
    $("#apply", wrap).addEventListener("click", () => {
      settings = {
        music: +$("#music", wrap).value, sfx: +$("#sfx", wrap).value,
        lang: $("#lang", wrap).value, difficulty: $("#difficulty", wrap).value,
        fullscreen: $("#fs", wrap).checked,
      };
      saveSettings(settings);
      if (settings.fullscreen && document.documentElement.requestFullscreen) {
        document.documentElement.requestFullscreen().catch(() => {});
      } else if (!settings.fullscreen && document.fullscreenElement) {
        document.exitFullscreen?.();
      }
      closeModal();
    });
  }

  // ===========================================================
  //  Дневник
  // ===========================================================
  function openJournal() {
    openModal("Дневник", `
      <p>Дневник заполняется по ходу игры: события, боссы, выборы. Стиль записей
      меняется вместе с состоянием героя — а иногда записи появляются <i>до</i>
      событий или меняются задним числом. «Некий» тоже оставляет здесь свои строки.</p>
      <p class="hint">Пока пусто — начните новую игру, чтобы появились первые записи.</p>
      <div class="row end"><button class="btn primary" data-close>Закрыть</button></div>`);
  }

  // ===========================================================
  //  Моды
  // ===========================================================
  function openMods() {
    const mods = [
      ["Расширенный бестиарий", "Доп. мобы и дроп-таблицы", true],
      ["Больше построек", "+12 зданий и декор", false],
      ["Хардкорная выживалка", "Голод, жажда, температура", false],
    ];
    const rows = mods.map(([name, sub, on], i) => `
      <div class="opt" style="cursor:default">
        <span>${name}<br><span class="opt-sub">${sub}</span></span>
        <span class="toggle"><input type="checkbox" data-mod="${i}"${on ? " checked" : ""}><span class="track"></span></span>
      </div>`).join("");
    openModal("Моды", `
      <p>Включайте и отключайте моды. На своих модах можно играть всегда; в общем
      лобби действуют моды хоста.</p>
      <div class="opt-list">${rows}</div>
      <p class="hint" style="margin-top:16px">Добавить свой мод — положите папку в
      <code>mods/</code> (поддержка загрузки появится позже).</p>
      <div class="row end"><button class="btn primary" data-close>Готово</button></div>`);
  }

  // ===========================================================
  //  Выход
  // ===========================================================
  function exitGame() {
    openModal("Выход", `
      <p>Выйти из игры?</p>
      <div class="row end">
        <button class="btn" data-close>Остаться</button>
        <button class="btn primary" id="confirm-exit">Выйти</button>
      </div>`);
    $("#confirm-exit").addEventListener("click", () => {
      window.open("", "_self");
      window.close();
      closeModal();
      openModal("Выход", `<p>В браузере вкладку закрывает сам игрок. В десктоп-сборке
        (Steam) кнопка закроет приложение.</p>
        <div class="row end"><button class="btn primary" data-close>Ок</button></div>`);
    });
  }

  // ===========================================================
  //  Привязка кнопок меню
  // ===========================================================
  const ACTIONS = {
    "new": newGameFlow,
    "continue": () => {
      const save = localStorage.getItem(SAVE_KEY);
      if (!save) return;
      const d = JSON.parse(save);
      playerName = d.name || null;
      startWorldStub(d.hero);
    },
    "create-lobby": createLobby,
    "join-lobby": joinLobby,
    "settings": openSettings,
    "journal": openJournal,
    "exit": exitGame,
    "mods": openMods,
  };

  document.querySelectorAll("[data-action]").forEach((btn) =>
    btn.addEventListener("click", () => ACTIONS[btn.dataset.action]?.()));

  // ---------- состояние кнопки «Продолжить» ----------
  function refreshContinue() {
    const btn = document.querySelector('[data-action="continue"]');
    if (btn) btn.disabled = !localStorage.getItem(SAVE_KEY);
  }
  refreshContinue();
})();
