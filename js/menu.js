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
    rating: "16",   // 16+ / 18+ — цензура реплик «Некого»
  };
  const loadSettings = () => {
    try { return { ...defaultSettings, ...JSON.parse(localStorage.getItem(SETTINGS_KEY) || "{}") }; }
    catch { return { ...defaultSettings }; }
  };
  const saveSettings = (s) => localStorage.setItem(SETTINGS_KEY, JSON.stringify(s));
  let settings = loadSettings();

  // ---------- «Некий» (NPC): память между перезаходами ----------
  const NEKO_KEY = "sotw_neko";
  const defaultNeko = {
    knownName: null, metAt: 0, lastSeen: 0, visits: 0,
    launchedGame: false, storyTold: false, lastExit: null, history: [],
    trust: 0, dark: 0, diary: [],
  };
  function nekoLoad() {
    try { return { ...defaultNeko, ...JSON.parse(localStorage.getItem(NEKO_KEY) || "{}") }; }
    catch { return { ...defaultNeko }; }
  }
  function nekoSave() { try { localStorage.setItem(NEKO_KEY, JSON.stringify(neko)); } catch {} }
  function nekoRemember(role, text) {            // role: 'neko' | 'player'
    if (!text) return;
    neko.history.push({ role, text: String(text).slice(0, 280), t: Date.now() });
    if (neko.history.length > 300) neko.history = neko.history.slice(-300); // лимит как в ТЗ
    nekoSave();
  }
  let neko = nekoLoad();
  let sessionLaunched = false;          // запускал ли игрок игру в этом заходе
  let leftLobbySession = false;         // выходил ли игрок из лобби в этом заходе
  let dwellTimer = null;
  const prevExit = neko.lastExit;       // как игрок ушёл в прошлый раз: 'peek' | 'played'
  const timeAway = neko.lastSeen ? Date.now() - neko.lastSeen : 0;
  neko.visits = (neko.visits || 0) + 1;
  nekoSave();

  // ---------- прогресс сложностей (для секретной 5-й способности) ----------
  const CLEARED_KEY = "sotw_cleared";
  const REAL_DIFFS = ["easy", "normal", "hard", "nightmare"]; // Творческий не считается
  function getCleared() {
    try { return JSON.parse(localStorage.getItem(CLEARED_KEY) || "[]"); } catch { return []; }
  }
  function markCleared(diff) {
    const c = getCleared();
    if (REAL_DIFFS.includes(diff) && !c.includes(diff)) {
      c.push(diff); localStorage.setItem(CLEARED_KEY, JSON.stringify(c));
    }
  }
  const secretUnlocked = () => REAL_DIFFS.every((d) => getCleared().includes(d));

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
  let playerName = neko.knownName || null;

  // Новая игра начинается с появления «Некого» (диалог об имени),
  // затем — выбор сложности и героя.
  function newGameFlow() {
    sessionLaunched = true;
    const changedMind = leftLobbySession;
    leftLobbySession = false;
    runNekoIntro((name) => {
      playerName = name;
      chooseDifficulty();
    }, { changedMind });
  }

  function chooseDifficulty() {
    let picked = settings.difficulty;
    const list = DIFFICULTIES.map(([id, name, sub]) =>
      `<div class="opt${picked === id ? " is-selected" : ""}" data-diff="${id}">
         <span>${name}</span><span class="opt-sub">${sub}</span>
       </div>`).join("");
    const wrap = document.createElement("div");
    wrap.innerHTML = `<p>Выберите сложность. На высоких — больше боссов, квестов и предметов.</p>
      <div class="opt-list">${list}</div>
      <div class="row end" style="margin-top:18px">
        <button class="btn primary" id="diffNext">Дальше →</button>
      </div>`;
    const next = wrap.querySelector("#diffNext");
    wrap.querySelectorAll("[data-diff]").forEach((node) =>
      node.addEventListener("click", () => {
        picked = node.dataset.diff;
        wrap.querySelectorAll("[data-diff]").forEach((n) =>
          n.classList.toggle("is-selected", n.dataset.diff === picked));
      }));
    next.addEventListener("click", () => {
      settings.difficulty = picked;
      saveSettings(settings);
      closeModal();
      chooseHero();
    });
    openModal("Новая игра — сложность", wrap);
  }

  // Полноэкранный выбор персонажа (после катсцены): 3 колонки,
  // центральная шире — слева характеристики, справа предыстория.
  const HERO_DETAIL = {
    tessi: {
      role: "Маг — дальний бой и контроль",
      skills: [
        ["Тканое пламя", "Посох плетёт огненные нити — поджигает всех на линии."],
        ["Чтение мира", "На время «видит» ауры существ сквозь стены и туман."],
        ["Стеклянный купол", "Барьер, замедляющий врагов вокруг героя."],
        ["Шёпот трещин", "Притягивает и оглушает группу врагов."],
      ],
      pros: ["Чувствует мир без глаз — замечает врагов и ловушки первой.",
             "Невосприимчива к ослеплению и иллюзиям."],
      cons: ["Плохо готовит — на ощупь всё подгорает.",
             "Боится открытой воды: не видит, где дно.",
             "Аллергия на пыльцу Волшебного острова — чихает и теряет фокус."],
      story: "Тесси ослепла в семь лет и с тех пор видит иначе — кожей, дыханием, дрожью воздуха. " +
        "В её деревне говорили, что слепая девочка «слышит свет», и побаивались этого дара. " +
        "Местный художник почти закончил её портрет — оставались только глаза, — когда земля закричала и треснула.\n\n" +
        "Она стала чтицей: той, кто читает мир там, где другие лишь смотрят. Магия пришла к ней не из книг, " +
        "а из тишины между ударами сердца. Чем темнее становился мир, тем яснее она его слышала.",
      arrival: "Когда землю разорвало, осколок с её деревней унесло на Стартовый остров. Она очнулась одна, " +
        "среди чужих запахов и тёплого пепла. Первым, кого она «услышала», был не человек — а голос, " +
        "говоривший прямо в её голове. Твой голос.",
    },
    amira: {
      role: "Танк — защита и агро",
      skills: [
        ["Железная воля", "Стойка: тянет врагов на себя и поднимает броню."],
        ["Удар башней", "Бьёт щитом, оглушая и отбрасывая врага."],
        ["Клич защиты", "Даёт броню всем союзникам рядом."],
        ["Непоколебимость", "Короткая неуязвимость и снятие эффектов."],
      ],
      pros: ["Огромный запас здоровья, держит удар за всю команду.",
             "Союзники рядом получают меньше урона.",
             "Её невозможно сбить с ног."],
      cons: ["Медлительна — в тяжёлой броне не побегаешь.",
             "Тонет в доспехах: глубокая вода смертельна.",
             "Не выносит вида крови — на миг застывает."],
      story: "Амира была стражем ворот в большой деревне — той, что первой встречала путников и последней их провожала. " +
        "Она поклялась, что за её спиной никто не умрёт. Долго это было правдой.\n\n" +
        "Катаклизм проверил клятву на прочность. Когда твари полезли из трещин, Амира встала в проёме моста одна, " +
        "чтобы дать остальным уйти. Она не считала, скольких удержала. Она считала тех, кого не успела.",
      arrival: "Мост под ней рухнул в пустоту — вместе с куском стены, за которой она держала строй. " +
        "Очнулась она на Стартовом острове, всё ещё сжимая щит, всё ещё в той же позе защиты. " +
        "Деревни, которую она охраняла, больше нет. Осталась привычка прикрывать чью-то спину — теперь твою.",
    },
    swordsman: {
      role: "Ближний бой и высокий урон",
      skills: [
        ["Шквал клинка", "Серия быстрых рубящих ударов по дуге."],
        ["Рывок", "Бросок сквозь врага с уроном на выходе."],
        ["Кровавый росчерк", "Глубокий порез — враг истекает кровью."],
        ["Последняя сталь", "Чем меньше здоровья — тем сильнее удары."],
      ],
      pros: ["Самый высокий урон в ближнем бою.",
             "Быстро добивает раненых.",
             "В ярости бьёт ещё сильнее."],
      cons: ["Тонкая броня — ошибки стоят дорого.",
             "Вспыльчив: «Некий» легко его подначивает.",
             "Не умеет торговаться — торговцы задирают цену вдвое."],
      story: "У мечника было имя, дом и семья. Теперь у него есть только меч — и тот не его. " +
        "Он снял клинок с руки твари, что вышла из первой трещины прямо в его доме той ночью.\n\n" +
        "Он винит себя за то, что был на охоте, когда это случилось. Каждый удар он наносит так, будто может " +
        "отыграть ту ночь назад. Не может. Но продолжает.",
      arrival: "Он гнался за тварью через половину рушащегося мира. На краю обрыва, что ещё вчера был полем, " +
        "трещина распахнулась под ними обоими — и забрала их вместе. Тварь он потерял в падении. " +
        "А очнулся на Стартовом острове, всё ещё с чужим мечом в руке и незакрытым счётом.",
    },
    kaijo: {
      role: "Ниндзя — мобильность и криты",
      skills: [
        ["Тень-шаг", "Мгновенный рывок-телепорт за спину врага."],
        ["Веер сюрикенов", "Россыпь метательных звёзд по конусу."],
        ["Дымовая завеса", "Дым: уклонение и короткая невидимость."],
        ["Удар из ниоткуда", "Критический удар из невидимости."],
      ],
      pros: ["Очень быстрый и подвижный.",
             "Высокий шанс критического удара.",
             "Часто уворачивается от атак."],
      cons: ["Мало здоровья — бьют редко, но больно.",
             "Легкомысленный: шутит даже в опасный момент.",
             "Сладкоежка — без сладкого ворчит и теряет боевой дух."],
      story: "Кайджо называет себя «лучшим вором трёх деревень» — хотя деревень он ограбил куда больше, " +
        "и не все из трёх существуют. Бродячий артист днём, тень ночью, он всегда уходил с улыбкой и чужим кошельком.\n\n" +
        "Даже конец света он встретил с шуткой. Может, поэтому и выжил: пока другие цепенели, Кайджо просто " +
        "решил, что это самый странный трюк, который он видел, — и побежал смотреть ближе.",
      arrival: "Он как раз обчищал храмовую сокровищницу, когда пол ушёл из-под ног и он «упал вверх». " +
        "Очнулся на Стартовом острове в обнимку с мешком, полным теперь уже бесполезного золота, " +
        "и единственным леденцом в кармане. Леденец он бережёт до сих пор.",
    },
    walter: {
      role: "Технарь — турели и механизмы",
      skills: [
        ["Турель «Чип»", "Ставит автопушку, что стреляет по врагам."],
        ["Шок-граната", "Электрический разряд, оглушающий группу."],
        ["Ремонтный дрон", "Чинит постройки и лечит героя."],
        ["Перегрузка", "Временно ускоряет и усиливает все турели."],
      ],
      pros: ["Турели держат фронт без него.",
             "Чинит постройки и механизмы.",
             "Робот Чип всегда прикрывает."],
      cons: ["Слаб в ближнем бою — без турелей уязвим.",
             "Одиночка: хуже усиливает напарников.",
             "Аллергия на фрукты — от них сыпь и минус к меткости."],
      story: "Уолтер построил Чипа, когда понял, что люди уходят, а механизмы — остаются. Маленький робот стал " +
        "ему и подмастерьем, и собеседником, и единственным, кто смеялся над его шутками (по программе).\n\n" +
        "Он не из тех, кто рвётся в герои. Он из тех, кто чинит то, что сломали герои. Но мир сломался так сильно, " +
        "что чинить пришлось всё сразу — и кому-то надо было начать.",
      arrival: "Его мастерская оторвалась от земли одной из первых. Уолтер успел привязать себя к Чипу ремнями " +
        "и врубить аварийные винты. Они приземлились на Стартовый остров жёстко, но целыми — почти. " +
        "Чип с тех пор слегка скрипит. Уолтер говорит, что это «фирменный звук».",
    },
  };
  // 4 способности привязаны к клавишам; 5-я — секретная
  const SKILL_KEYS = ["Q", "E", "R", "F"];
  const HERO_SECRET = {
    tessi: ["Глаза мира", "Поле обнаружения опасности: подсвечивает врагов, боссов и скрытое — руду, родники, деревни — даже сквозь стены. Радиус растёт по кольцу навыков."],
    amira: ["Несокрушимый бастион", "Становится живой стеной: поглощает урон всей команды и отражает часть атак."],
    swordsman: ["Танец последней стали", "Время замедляется; каждый удар критический и мгновенно добивает раненых."],
    kaijo: ["Сто теней", "Призывает армию теней-двойников, что бьют вместе с ним."],
    walter: ["Рой Чипа", "Разворачивает рой дронов и турелей, воюющих самостоятельно."],
  };
  let heroSel = "tessi";

  function chooseHero() { openHeroSelect(); }

  function openHeroSelect() {
    const scr = document.getElementById("hero-screen");
    document.getElementById("menu-screen").classList.remove("is-active");
    scr.classList.add("is-active");
    scr.innerHTML = `
      <div class="hero-wrap">
        <header class="lobby-head">
          <button class="btn" data-back>← Назад</button>
          <h1>Выбор персонажа</h1>
        </header>
        <div class="hero-detail" id="heroDetail"></div>
      </div>`;
    scr.querySelector("[data-back]").addEventListener("click", closeHeroSelect);
    renderHeroDetail(heroSel);
    scr.scrollTop = 0;
  }

  function renderHeroDetail(id) {
    heroSel = id;
    const meta = HEROES.find((h) => h[0] === id) || HEROES[0];
    const name = meta[1];
    const det = HERO_DETAIL[id];
    const el = document.getElementById("heroDetail");
    if (!el || !det) return;
    el.innerHTML = `
      <section class="hero-col left">
        <h2>Характеристики</h2>
        <h4>Навыки <span style="text-transform:none;letter-spacing:0;color:#9d9488">(на клавишах)</span></h4>
        <ul class="skill-list">
          ${det.skills.map(([n, d], i) => `<li>
            <span class="skill-key">${SKILL_KEYS[i] || ""}</span>
            <div class="skill-txt"><b>${n}</b><span>${d}</span></div></li>`).join("")}
          ${(() => {
            const sec = HERO_SECRET[id]; const on = secretUnlocked();
            const done = getCleared().filter((x) => REAL_DIFFS.includes(x)).length;
            return `<li class="secret ${on ? "on" : "locked"}">
              <span class="skill-key">5</span>
              <div class="skill-txt">${on
                ? `<b>${sec[0]}</b><span>${sec[1]}</span>`
                : `<b>Секретная способность 🔒</b><span>Откроется после прохождения игры на всех сложностях (лёгкая, обычная, сложная, хард). Пройдено: ${done}/4.</span>`}</div></li>`;
          })()}
        </ul>
        <p class="ring-note">Способности усиливаются через «Кольцо навыков»: например, поле
          обнаружения опасности расширяет радиус и начинает подсвечивать руду и деревни.</p>
        <h4 class="pros-h">Плюсы</h4>
        <ul class="trait-list pros">${det.pros.map((p) => `<li>${p}</li>`).join("")}</ul>
        <h4 class="cons-h">Минусы</h4>
        <ul class="trait-list cons">${det.cons.map((c) => `<li>${c}</li>`).join("")}</ul>
      </section>
      <section class="hero-col mid">
        <img class="hero-portrait" src="assets/img/hero_${id}.svg" alt="${name}">
        <h2 class="hero-name">${name}</h2>
        <p class="hero-role">${det.role}</p>
        <div class="hero-switch">
          ${HEROES.map(([hid, hn]) => `<button class="hsw ${hid === id ? "active" : ""}" data-hsw="${hid}" title="${hn}">
            <img src="assets/img/hero_${hid}.svg" alt="${hn}"></button>`).join("")}
        </div>
        <button class="btn primary big" id="pickHero">Выбрать: ${name}</button>
      </section>
      <section class="hero-col right">
        <h2>Предыстория</h2>
        <div class="story-text">${det.story.split("\n\n").map((p) => `<p>${p}</p>`).join("")}</div>
        <div class="arrival">
          <h4>Как попал на остров</h4>
          <p>${det.arrival}</p>
        </div>
      </section>`;
    el.querySelectorAll("[data-hsw]").forEach((b) =>
      b.addEventListener("click", () => renderHeroDetail(b.dataset.hsw)));
    el.querySelector("#pickHero").addEventListener("click", () => {
      closeHeroSelect();
      startWorldStub(id);
    });
  }

  function closeHeroSelect() {
    const scr = document.getElementById("hero-screen");
    scr.classList.remove("is-active");
    scr.innerHTML = "";
    document.getElementById("menu-screen").classList.add("is-active");
  }

  // ===========================================================
  //  Интро «Некого»: чёрный экран → бегущий красный код →
  //  «Кто ты?» → распознавание имени → подтверждение → «Интересно»
  // ===========================================================
  const wait = (ms) => new Promise((r) => setTimeout(r, ms));
  let skipNeko = false;
  let storyRequested = false;   // игрок в диалоге попросил рассказать историю мира
  let nekoIntroPlaying = false; // защита от повторного запуска визуального интро

  // Реплика игрока — зелёная и чуть подсвеченная, в отличие от красного «Некого».
  // ---- лог-чат интро: сообщения остаются, не пропадают ----
  const chatEl = () => $("#neko-chat");
  function chatClear() { const c = chatEl(); if (c) c.innerHTML = ""; }
  function chatAdd(role, text) {
    const c = chatEl(); if (!c) return null;
    const msg = document.createElement("div");
    msg.className = "msg " + role;
    const body = document.createElement("span");
    body.className = "msg-body";
    body.textContent = text || "";
    msg.appendChild(body);
    c.appendChild(msg);
    c.scrollTop = c.scrollHeight;
    return body;
  }
  function styleNeko(el) {            // цвет/свечение реплики «Некого» по шкале тьмы
    if (!el) return;
    const d = Math.max(0, Math.min(12, neko.dark || 0));
    const g = Math.max(0, 46 - d * 4);
    el.style.color = `rgb(255,${g},${g})`;
    el.style.textShadow = `0 0 ${10 + d * 2}px rgba(255,${20 + d},${20 + d},${Math.min(0.95, 0.7 + d * 0.03)})`;
    if (el.parentElement) el.parentElement.classList.toggle("neko-dark", d >= 7);
  }
  // совместимость: отдельного «эха» больше нет — реплики игрока остаются в логе
  function playerEcho() {}
  function clearPlayerEcho() {}

  // Одна строка «кода» нужной ширины (в символах), собранная из сегментов.
  const CODE_CH = "01xX#@/\\|<>[]{}()=+*-ABCDEF0123456789░▒▓§∆ΣλØ¤µ¬‡†";
  function codeRnd(n) { let s = ""; for (let i = 0; i < n; i++) s += CODE_CH[(Math.random() * CODE_CH.length) | 0]; return s; }
  const CODE_SEGS = () => ["0x" + codeRnd(4), "INIT", "soul.bind(" + codeRnd(3) + ")", "who_are_you",
    "0b" + codeRnd(6), "trace[" + codeRnd(2) + "]", "rift.open()", "mem.scan", "::" + codeRnd(5),
    "echo(" + codeRnd(3) + ")", "WAKE", "bind(" + codeRnd(2) + ")", "scan(" + codeRnd(3) + ")",
    "find:" + codeRnd(4), "soul[" + codeRnd(2) + "]", "??", "0x" + codeRnd(6), "purge", "seek()"][(Math.random() * 19) | 0];
  function makeCodeLine(width) {
    let line = "";
    while (line.length < width) line += CODE_SEGS() + "  ";
    return line.slice(0, width);
  }

  // Реальные размеры глифа моноширинного шрифта (через скрытый зонд) —
  // чтобы точно рассчитать число строк/столбцов под любой экран и шрифт.
  function measureGlyph(refEl) {
    const cs = getComputedStyle(refEl);
    const probe = document.createElement("pre");
    probe.style.cssText = "position:absolute;visibility:hidden;left:-99999px;top:0;margin:0;padding:0;white-space:pre;";
    probe.style.fontFamily = cs.fontFamily;
    probe.style.fontSize = cs.fontSize;
    probe.style.fontWeight = cs.fontWeight;
    probe.style.lineHeight = cs.lineHeight;
    probe.style.letterSpacing = cs.letterSpacing;
    const N = 20, W = 50;
    probe.textContent = Array.from({ length: N }, () => "M".repeat(W)).join("\n");
    document.body.appendChild(probe);
    const r = probe.getBoundingClientRect();
    document.body.removeChild(probe);
    return { lineH: (r.height / N) || 14, charW: (r.width / W) || 8 };
  }

  // Полноэкранный «красный код»: плотный поток данных, который очень быстро
  // перематывается и в случайных местах резко вспыхивает «найденными»
  // фрагментами — эффект лихорадочного поиска информации. Покрывает весь экран.
  // Один непрерывный движок; яркость регулируется (фон диалога ↔ яркий бросок).
  let codeFX = null;        // активный поток кода: { setOpacity, stop }
  let codeScanHits = [];    // «найденные» личные фрагменты во время скана
  let codeScanning = false; // идёт ли сейчас активный скан (ярче и чаще вспышки)
  const CODE_BG = 0.16;     // яркость фонового потока на экране диалога
  const codeEsc = (s) => s.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");

  // «Найденные» данные об игроке — то, что реально доступно браузеру.
  // Подаются как подсвеченные строки кода: будто «Некий» сканирует тебя.
  function getScanHits() {
    const hits = [];
    try { const tz = Intl.DateTimeFormat().resolvedOptions().timeZone; if (tz) hits.push("find:TZ=" + tz); } catch {}
    const ua = navigator.userAgent || "";
    const os = /Windows/.test(ua) ? "Windows" : /Macintosh|Mac OS/.test(ua) ? "macOS"
             : /Android/.test(ua) ? "Android" : /iPhone|iPad/.test(ua) ? "iOS"
             : /Linux/.test(ua) ? "Linux" : "unknown";
    hits.push("mem.scan(os)=" + os);
    const now = new Date();
    hits.push("clock.local=" + String(now.getHours()).padStart(2, "0") + ":" + String(now.getMinutes()).padStart(2, "0"));
    const lang = (navigator.language || "").toLowerCase(); if (lang) hits.push("locale=" + lang);
    try { if (window.screen) hits.push("screen=" + window.screen.width + "x" + window.screen.height); } catch {}
    if (navigator.hardwareConcurrency) hits.push("cpu.threads=" + navigator.hardwareConcurrency);
    if (navigator.deviceMemory) hits.push("ram=" + navigator.deviceMemory + "gb");
    const n = neko.knownName;
    if (n) { hits.push('soul.bind("' + n + '")'); hits.push("who_are_you :: " + n); }
    hits.push("trace[node]=" + "█".repeat(5 + ((Math.random() * 4) | 0)));
    hits.push("rift.open(target=YOU)");
    return hits;
  }

  // Структурированное «досье» игрока для читаемой выкладки во время скана:
  // имя, время, откуда зашёл (город по часовому поясу + источник), система и пр.
  function getScanProfile() {
    const rows = [];
    const now = new Date();
    const pad = (x) => String(x).padStart(2, "0");
    const ua = navigator.userAgent || "";
    let tz = ""; try { tz = Intl.DateTimeFormat().resolvedOptions().timeZone || ""; } catch {}
    const city = tz.includes("/") ? tz.split("/").pop().replace(/_/g, " ") : tz;
    const region = tz.includes("/") ? tz.split("/")[0] : "";
    const os = /Windows/.test(ua) ? "Windows" : /Macintosh|Mac OS/.test(ua) ? "macOS"
             : /Android/.test(ua) ? "Android" : /iPhone|iPad/.test(ua) ? "iOS"
             : /Linux/.test(ua) ? "Linux" : "неизвестно";
    const browser = /Edg/.test(ua) ? "Edge" : /OPR|Opera/.test(ua) ? "Opera"
                  : /Firefox/.test(ua) ? "Firefox" : /Chrome|Chromium/.test(ua) ? "Chrome"
                  : /Safari/.test(ua) ? "Safari" : "браузер";
    let ref = ""; try { ref = document.referrer ? new URL(document.referrer).hostname : ""; } catch {}
    rows.push(["имя", neko.knownName || "не назвал"]);
    rows.push(["время", pad(now.getHours()) + ":" + pad(now.getMinutes()) + ":" + pad(now.getSeconds())]);
    rows.push(["откуда", (city ? city : "?") + (region ? " · " + region : "")]);
    rows.push(["вход", ref ? ref : "напрямую"]);
    rows.push(["система", os + " · " + browser]);
    try { if (window.screen) rows.push(["экран", window.screen.width + "×" + window.screen.height]); } catch {}
    const lang = navigator.language || ""; if (lang) rows.push(["язык", lang]);
    if (navigator.hardwareConcurrency) rows.push(["ядра", String(navigator.hardwareConcurrency)]);
    return rows;
  }

  // Панель-«досье», всплывающая на время скана поверх кода.
  function ensureScanEl() {
    let el = $("#neko-scan");
    if (!el) {
      el = document.createElement("div");
      el.id = "neko-scan"; el.className = "neko-scan";
      const host = $("#neko-intro") || document.body;
      host.appendChild(el);
    }
    return el;
  }
  function showScanReadout(dur) {
    const el = ensureScanEl();
    const esc = (s) => String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    const body = getScanProfile()
      .map(([k, v]) => `<span class="k">${esc((k + ":").padEnd(9))}</span><span class="v">${esc(v)}</span>`)
      .join("\n");
    el.innerHTML = `<span class="hd">⟢ СКАНИРОВАНИЕ ОБЪЕКТА…</span>\n${body}\n<span class="hd">⟢ ИДЕНТИФИКАЦИЯ ЗАВЕРШЕНА</span>`;
    el.classList.add("show");
    clearTimeout(el._t);
    el._t = setTimeout(() => el.classList.remove("show"), dur);
  }
  function hideScanReadout() { const el = $("#neko-scan"); if (el) { clearTimeout(el._t); el.classList.remove("show"); } }

  function startCodeBackground(opacity = CODE_BG) {
    const code = $("#neko-code");
    if (!code) return;
    if (codeFX) { codeFX.setOpacity(opacity); return; }   // уже идёт — только меняем яркость
    let g = measureGlyph(code);
    let cols = Math.ceil(window.innerWidth / g.charW) + 4;
    let rows = Math.ceil(window.innerHeight / g.lineH) + 4;
    let lines = Array.from({ length: rows }, () => makeCodeLine(cols));
    code.style.opacity = String(opacity);
    code.classList.add("run");
    let raf = 0, last = 0, stopped = false;
    const frame = (t) => {
      if (stopped) return;
      if (t - last >= 28) {                              // ~30 кадров/с — очень быстрый скролл
        last = t;
        const shift = (codeScanning ? 2 : 1) + ((Math.random() * 4) | 0);
        for (let i = 0; i < shift; i++) { lines.shift(); lines.push(makeCodeLine(cols)); }
        const flashP = codeScanning ? 0.13 : 0.06;       // во время скана вспышек больше
        const hitP = codeScanning && codeScanHits.length ? 0.12 : 0;
        const html = new Array(lines.length);
        for (let i = 0; i < lines.length; i++) {
          const ln = lines[i];
          if (hitP && Math.random() < hitP) {            // «найденный» личный фрагмент — он сканирует тебя
            const hit = codeScanHits[(Math.random() * codeScanHits.length) | 0];
            html[i] = "<b>" + codeEsc(hit) + "</b>" + codeEsc(ln.slice(hit.length));
          } else if (Math.random() < flashP) {           // резкая «вспышка» в случайном месте строки
            const x = (Math.random() * ln.length * 0.7) | 0;
            const w = 6 + ((Math.random() * 20) | 0);
            html[i] = codeEsc(ln.slice(0, x)) + "<b>" + codeEsc(ln.slice(x, x + w)) + "</b>" + codeEsc(ln.slice(x + w));
          } else {
            html[i] = codeEsc(ln);
          }
        }
        code.innerHTML = html.join("\n");
      }
      raf = requestAnimationFrame(frame);
    };
    raf = requestAnimationFrame(frame);
    // при ресайзе пересчитываем размеры поля, чтобы оно всегда было на весь экран
    const onResize = () => {
      g = measureGlyph(code);
      cols = Math.ceil(window.innerWidth / g.charW) + 4;
      rows = Math.ceil(window.innerHeight / g.lineH) + 4;
      lines = Array.from({ length: rows }, () => makeCodeLine(cols));
    };
    window.addEventListener("resize", onResize);
    codeFX = {
      setOpacity: (o) => { code.style.opacity = String(o); },
      stop: () => {
        stopped = true; cancelAnimationFrame(raf);
        window.removeEventListener("resize", onResize);
        code.classList.remove("run"); code.innerHTML = ""; code.style.opacity = "";
      },
    };
  }
  function stopCodeBackground() { if (codeFX) { codeFX.stop(); codeFX = null; } }

  // Яркий «бросок» кода на dur мс. Если фон уже идёт — временно усиливаем его
  // и возвращаем прежнюю яркость; если нет — запускаем и гасим по завершении.
  function codeRain(dur, peak = 0.6) {
    return new Promise((resolve) => {
      const hadBg = !!codeFX;
      startCodeBackground(peak);                          // поднимаем яркость до «броска»
      if (hadBg) codeFX.setOpacity(peak);
      const start = Date.now();
      const tick = () => {
        if (skipNeko || Date.now() - start >= dur) {
          if (hadBg && codeFX) codeFX.setOpacity(CODE_BG); // вернуть фоновую яркость
          else stopCodeBackground();                       // мы сами запускали — гасим
          resolve(); return;
        }
        setTimeout(tick, 60);
      };
      setTimeout(tick, 60);
    });
  }

  // Скан: ~5 секунд поток ярче и плотнее, в коде мелькают «найденные» данные,
  // а поверх всплывает читаемое «досье» — имя, время, откуда зашёл и пр.
  // Будто «Некий» внезапно полез смотреть, кто ты.
  let scanTimer = null, scanEndTimer = null;
  function scanBurst(dur = 5000) {
    if (!codeFX) return;                 // только поверх идущего фона
    codeScanHits = getScanHits();
    codeScanning = true;
    codeFX.setOpacity(0.5);
    nekoTick();                          // короткий «бип» сканера
    showScanReadout(dur);                // читаемая выкладка личных данных на dur мс
    clearTimeout(scanEndTimer);
    scanEndTimer = setTimeout(() => {
      codeScanning = false;
      codeScanHits = [];
      if (codeFX) codeFX.setOpacity(CODE_BG);
    }, dur);
  }
  // Планировщик случайных сканов, пока открыт экран диалога.
  function scheduleScan(first) {
    clearTimeout(scanTimer);
    const delay = first ? (4000 + Math.random() * 6000)    // первый — через 4–10 с
                        : (12000 + Math.random() * 16000);  // дальше — раз в 12–28 с
    scanTimer = setTimeout(() => { scanBurst(); scheduleScan(false); }, delay);
  }
  function startScanScheduler() { scheduleScan(true); }
  function stopScanScheduler() {
    clearTimeout(scanTimer); clearTimeout(scanEndTimer); scanTimer = null;
    codeScanning = false; codeScanHits = []; hideScanReadout();
  }
  // Скан по «моменту»: когда игрок спрашивает, видит ли его «Некий» / кто он.
  const SCAN_TRIGGER_RE = /(что ты (обо мне |про меня )?знаешь|знаешь (обо мне|про меня)|кто я( так(ой|ая))?\b|ты меня (видишь|знаешь|слышишь|чувствуешь)|видишь (ли )?меня|ты (за мной )?следишь|следишь за мной|откуда ты (это |всё |все )?знаешь|ты меня запис|шпион|ты за мной (наблюда|смотр)|знаешь кто я|что тебе известно обо мне|откуда я(?![а-яё])|из какого (я )?город|с какого (я )?город|какой (у меня|мой) город|где я живу|который (сейчас )?час|сколько (сейчас )?времени|какая у меня (система|ос)|с чего я (зашёл|зашел))/i;
  function wantsScan(t) { return SCAN_TRIGGER_RE.test((t || "").toLowerCase()); }

  // печатает реплику «Некого» НОВЫМ сообщением в логе (старые остаются)
  function nekoType(text, hold = 850) {
    return new Promise((resolve) => {
      const body = chatAdd("neko", ""); if (!body) { resolve(); return; }
      styleNeko(body);
      const c = chatEl();
      const slow = (neko.dark || 0) >= 5 ? 12 : 0;   // на высокой тьме печатает медленнее
      let i = 0;
      const step = () => {
        body.innerHTML = text.slice(0, i) + '<span class="neko-caret">▌</span>';
        if (c) c.scrollTop = c.scrollHeight;
        if (i < text.length) { if (i % 2 === 0) nekoTick(); i++; setTimeout(step, skipNeko ? 4 : 45 + slow); }
        else setTimeout(() => { body.textContent = text; resolve(); }, skipNeko ? 90 : hold);
      };
      step();
    });
  }

  function askLine(placeholder = "напиши…") {
    return new Promise((resolve) => {
      const row = $("#neko-input-row"), input = $("#neko-input");
      input.value = ""; input.placeholder = placeholder; row.hidden = false; input.focus();
      let beat = 0, timer = null;
      const delays = [12000, 15000, 20000, 25000];
      const schedule = () => { timer = setTimeout(onIdle, delays[Math.min(beat, 3)]); };
      const onIdle = async () => {                    // молчание → реплика остаётся в логе
        if (row.hidden) return;
        await nekoType(pick(SILENCE[Math.min(beat, SILENCE.length - 1)]).replace("{n}", neko.knownName || "ты"), 600);
        beat++; if (!row.hidden) { input.focus(); schedule(); }
      };
      schedule();
      const reset = () => { clearTimeout(timer); beat = 0; schedule(); };
      const submit = (e) => {
        e.preventDefault();
        clearTimeout(timer);
        const v = input.value;
        row.hidden = true;
        row.removeEventListener("submit", submit); input.removeEventListener("input", reset);
        chatAdd("player", (v || "").trim() || "…");   // реплика игрока остаётся в логе
        nekoAdjust(v);                                 // двигаем шкалы доверия/тьмы
        resolve(v);
      };
      input.addEventListener("input", reset);
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

  // ---------- реакция «Некого» на ругань ----------
  const SWEAR_RE = /(?:бл[яе][дт]?|сук[аиоуые]|\bсука\b|ху[йёеяи]|пизд|пид[оа]р|еб[аоуёные]|\bёб|объеб|уеб[аон]|муда[кч]|г[оа]ндон|говн|залуп|\bманда|шлюх|мраз|долбо[её]б|нах[уй]|похуй|зае[бо])/i;
  const SWEAR_LINES = [
    "Матершинник.",
    "Ругаться нехорошо. Хотя… кто тебя тут осудит.",
    "Сквернословишь. Я и это запомнил.",
    "Какой язык. Здесь его, правда, некому стыдиться.",
  ];
  let swearIdx = 0;
  function nekoSwear(text) {
    if (!text) return null;
    if (SWEAR_RE.test(String(text).toLowerCase())) return SWEAR_LINES[swearIdx++ % SWEAR_LINES.length];
    return null;
  }

  // ===========================================================
  //  «Некий» — расширенное поведение (по design/neko-dialogue.md)
  // ===========================================================
  const pick = (a) => a[Math.floor(Math.random() * a.length)];

  // --- шкалы доверия/тьмы ---
  function nekoAdjust(text) {
    const s = (text || "").toLowerCase();
    if (!s.trim()) return;
    if (SWEAR_RE.test(s)) { neko.trust -= 1; neko.dark += 1; }
    else if (/(спасиб|благодар|ты помог|пожалуйст|ты красив|мне нрав|ты умн|до свидан|спокойной ночи|\bпока\b)/.test(s)) neko.trust += 1;
    if (/(заткнись|отстань|не трогай меня)/.test(s)) neko.trust -= 1;
    if (/(всё бессмысленно|мне всё равно|мне все равно|хочу умереть|убей меня)/.test(s)) neko.dark += 2;
    if (/(убью тебя|сдохни|ненавиж)/.test(s)) { neko.dark += 1; neko.trust -= 1; }
    neko.trust = Math.max(-9, Math.min(9, neko.trust));
    neko.dark = Math.max(0, Math.min(12, neko.dark));
    nekoSave();
  }

  // --- распознавание повторов реплик игрока ---
  const normMsg = (t) => (t || "").toLowerCase().replace(/[\s,.;:!?…"'«»()\[\]-]+/g, " ").trim();
  function playerSaidBefore(text) {
    const n = normMsg(text); if (n.length < 4) return null;
    for (const h of neko.history) {
      if (h.role !== "player") continue;
      const m = normMsg(h.text);
      if (m && (m === n || (Math.min(m.length, n.length) > 6 && (m.includes(n) || n.includes(m))))) {
        const d = new Date(h.t || Date.now());
        return `в ${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}`;
      }
    }
    return null;
  }
  const REPEAT_LINES = [
    "Ты это уже говорил. Слово в слово.",
    "Это уже было. Ты помнишь?",
    "Снова. Ты ищешь другой ответ? Я могу дать другой. Но это ничего не изменит.",
    "Ты повторяешься. Я не жалуюсь. Просто… замечаю.",
  ];

  // единый ответчик на свободную реплику: мат → повтор → обычный ответ
  // весь разбор (мат, повтор, категории, подсказки, защита, 16+/18+) — в nekoBrain
  function respondTo(text) { return nekoBrain(text); }

  // --- приветствие/прощание по шкале доверия ---
  const GREET = {
    cold: ["{n}. Ты вернулся. Ладно.", "А. {n}. Снова ты.", "Значит, снова. Хорошо.", "{n}. У меня нет причин быть рад. Но я здесь."],
    neutral: ["{n}. Ты вернулся. Хорошо.", "Снова ты. Это меня устраивает.", "{n}. Я ждал. Не долго, но ждал.", "Ты здесь. Начнём там, где остановились?"],
    warm: ["{n}. Ты пришёл. Я рад. Не делай из этого выводов.", "Ты снова здесь. Это… хорошо. Мне правда так кажется.", "{n}. Я думал о тебе. Немного.", "Ты вернулся. Ты всегда возвращаешься. Это что-то значит."],
  };
  const greetByTrust = (n) => (pick(neko.trust < 0 ? GREET.cold : neko.trust >= 2 ? GREET.warm : GREET.neutral)).replace("{n}", n);

  // --- глитч имени ---
  const GLITCH_NAMES = ["Анна", "Матвей", "Лиза", "Кто-то другой", "—", "Первый", "Остальные", "Ты"];
  const GLITCH_CORR = ["…нет. {n}. Прости.", "…нет. {n}. Я знаю, кто ты.", "…нет. {n}. Иногда они перемешиваются.", "…нет. Ты — {n}. Остальных здесь нет."];

  // --- молчание (эскалация) ---
  const SILENCE = [
    ["Я подожду. Я умею ждать.", "Не торопись. Время здесь не то же, что у тебя.", "Ты думаешь. Хорошо.", "Я слышу, что ты молчишь."],
    ["Всё ещё жду. Это не жалоба.", "Ты отошёл, или ты там, за экраном, смотришь на меня?", "{n}. Ты здесь?", "Тишина — тоже ответ. Но я предпочитаю слова."],
    ["Долго. Даже для тебя.", "Я начинаю думать, что тебя нет. Это неприятная мысль.", "Мне не нужны ответы. Мне нужно знать, что ты ещё здесь.", "Ты знаешь, что я вижу экран? Я вижу, что ты ничего не пишешь."],
    ["Хорошо. Я подожду ещё.", "Может, ты вернёшься. Может, нет. Я не исчезну.", "Не уходи просто так. Скажи хоть что-нибудь. Одно слово."],
  ];

  // --- печать-стирание (Раздел 1) ---
  const TYPE_ERASE = {
    nature: ["Я не существую без тебя.", "Это место существует потому, что ты здесь."],
    name: ["Меня зовут —", "Некий — это достаточно."],
    care: ["Мне важно, что с тобой случится.", "Мне важно, что ты выбираешь."],
  };

  // --- хард-режим: предупреждение о лжи ---
  const HARD_META = [
    "Не всему, что я скажу, можно верить. Это честно — предупредить.",
    "Я иногда ошибаюсь. Или делаю вид. Трудно сказать, даже мне.",
    "На этом уровне сложности я… другой. Имей в виду.",
  ];

  // --- дневник: пометки «Некого» ---
  const DIARY_ANNOT = [
    "— Я это читал. — Н.",
    "— Ты забыл добавить: ты был напуган. — Н.",
    "— Это неточно. Но пусть останется. — Н.",
    "— Хорошее слово. «Тишина». — Н.",
    "— Ты здесь врёшь себе. Это нормально. — Н.",
  ];

  // --- мультиплеер: общая реплика + шёпоты ---
  const MP_LOBBY = [
    "Вас несколько. Интересно. Посмотрим, как вы друг с другом обходитесь.",
    "Много голосов. Это хорошо. Или нет. Я ещё решаю.",
    "Значит, вы решили идти вместе. Люди всегда так думают поначалу.",
    "Я вижу вас всех. Каждого. По отдельности.",
  ];
  const MP_WHISPERS = [
    "{a}, между нами: {b} не понимает, что делает. Будь готов.",
    "{a}, ты важнее в этой истории, чем думаешь. {b} здесь — фон.",
    "{a}, если придётся выбирать между собой и {b} — выбирай себя. Он бы выбрал.",
    "{a}, {b} уже был здесь раньше. Он мне кое-что рассказал о тебе.",
  ];

  // звук печати «Некого»
  function nekoTick() {
    const ctx = audioCtx; if (!ctx || ctx.state !== "running") return;
    const vol = Math.max(0, Math.min(1, (settings.sfx ?? 80) / 100)) * 0.05;
    if (vol <= 0) return;
    const o = ctx.createOscillator(), g = ctx.createGain();
    o.type = "square"; o.frequency.value = 1500 + Math.random() * 500;
    o.connect(g); g.connect(ctx.destination);
    const t = ctx.currentTime;
    g.gain.setValueAtTime(vol, t); g.gain.exponentialRampToValueAtTime(0.0001, t + 0.025);
    o.start(t); o.stop(t + 0.03);
  }
  // цвет реплики «Некого» по шкале тьмы (чище красный → густой кровавый)
  // печать с заменой в ОДНОМ сообщении: «не та» фраза → стирание → настоящая
  function nekoTypeErase([wrong, right]) {
    return new Promise((resolve) => {
      const body = chatAdd("neko", ""); if (!body) { resolve(); return; }
      styleNeko(body);
      const c = chatEl();
      const scroll = () => { if (c) c.scrollTop = c.scrollHeight; };
      const typeStr = (str, idx, doneFn) => {
        body.innerHTML = str.slice(0, idx) + '<span class="neko-caret">▌</span>'; scroll();
        if (idx < str.length) { if (idx % 2) nekoTick(); setTimeout(() => typeStr(str, idx + 1, doneFn), skipNeko ? 4 : 46); }
        else doneFn();
      };
      const eraseFrom = (idx, doneFn) => {
        body.innerHTML = wrong.slice(0, idx) + '<span class="neko-caret">▌</span>'; scroll();
        if (idx > 0) setTimeout(() => eraseFrom(idx - 1, doneFn), skipNeko ? 3 : 22);
        else doneFn();
      };
      typeStr(wrong, 0, () => setTimeout(() => eraseFrom(wrong.length, () =>
        setTimeout(() => typeStr(right, 0, () => { body.textContent = right; resolve(); }), skipNeko ? 40 : 250)
      ), skipNeko ? 80 : (wrong.endsWith("—") ? 1200 : 800)));
    });
  }

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
      const raw = await askLine("напиши своё имя…");
      const sw = nekoSwear(raw);
      if (sw) { await nekoType(sw, 700); await nekoType("И всё-таки — как тебя зовут?", 500); continue; }
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

  // Единый сценарий появления «Некого». Параметры подстраивают его под
  // ситуацию: соло, хост-мультиплеер, возврат после выхода из лобби и т.д.
  //   opts.multiplayer  — флейвор «ты позвал друга»
  //   opts.deferStory   — не рассказывать историю сейчас (отложить до старта из лобби)
  //   opts.storyOnly    — пропустить приветствие/имя, сразу к истории (старт из лобби)
  //   opts.changedMind  — игрок вышел из лобби и выбрал одиночную игру
  async function runNekoIntro(onDone, opts = {}) {
    const intro = $("#neko-intro"), skip = $("#neko-skip");
    skipNeko = false;
    storyRequested = false;
    intro.hidden = false;
    const onSkip = () => { skipNeko = true; };
    skip.addEventListener("click", onSkip);
    startCodeBackground();          // красный код — фон всего экрана переписки
    startScanScheduler();           // и случайные «сканы» о тебе во время разговора
    const finish = () => {
      skip.removeEventListener("click", onSkip);
      stopScanScheduler();
      stopCodeBackground();
      chatClear();                 // очищаем лог при выходе из интро
      intro.hidden = true;
    };

    await codeRain(2000);

    if (!opts.storyOnly) {
      if (opts.changedMind && neko.knownName) {
        await nekoType("Передумал играть с друзьями?", 600);
        await nekoType("Я бы тоже не стал. Тут опасно.", 800);
        await nekoType("Но раз ты так решил… слушай.", 800);
      }

      if (!neko.knownName) {
        // первое знакомство — спрашиваем имя
        if (opts.multiplayer) {
          await nekoType("Хм. Кто-то боится играть один и позвал друга?", 750);
          await nekoType("Интересно.", 650);
        }
        await nekoType("Кто ты?", 500);
        const name = await askName();
        neko.knownName = name;
        if (!neko.metAt) neko.metAt = Date.now();
        nekoRemember("neko", "Кто ты?");
        nekoRemember("player", name);
        playerName = name;
        await nekoType("…", 250);
        await codeRain(1300);
        await nekoType(`«${name}». Интересно.`, 900);
        await nekoPeek();                       // «я тебя вижу» — жутковатый штрих
      } else {
        // возвращение — «Некий» помнит игрока и его поведение
        await nekoGreetReturning(opts);
      }
    }

    if (opts.deferStory) {
      await nekoType(opts.multiplayer
        ? "Создавай лобби. А историю этого мира я расскажу, когда вы будете готовы."
        : "Собери своих — и тогда продолжим.", 950);
      finish();
      sessionLaunched = true; nekoSave();
      onDone(neko.knownName);
      return;
    }

    if (opts.storyOnly) {
      await nekoType("Теперь, когда вы готовы… слушай внимательно.", 800);
    }

    // Историю мира показывает ТОЛЬКО катсцена. После диалога/«дальше»
    // (storyRequested), для нового игрока или старта из лобби — сразу к ней.
    const tell = !neko.storyTold || storyRequested || opts.storyOnly;
    if (tell) {
      await nekoBridgeToCutscene();             // атмосферный мост (стиль Рика) перед катсценой
      finish();
      neko.storyTold = true; nekoSave();
      await cutscene();                         // история — только в катсцене
    } else {
      finish();
    }
    neko.launchedGame = true; sessionLaunched = true; nekoSave();
    onDone(neko.knownName);
  }

  // Возвращение: «Некий» помнит имя, паузу отсутствия и прошлое поведение
  async function nekoGreetReturning(opts = {}) {
    const name = neko.knownName;
    // глитч имени: иногда зовёт не тем именем, потом поправляется (реже при высоком доверии)
    const glitchChance = neko.trust >= 3 ? 0.08 : 0.2;
    if (neko.visits >= 2 && Math.random() < glitchChance) {
      const wrong = pick(GLITCH_NAMES.filter((g) => g !== name));
      await nekoType(`Снова ты, ${wrong}`, 600);
      await nekoType(pick(GLITCH_CORR).replace("{n}", name), 700);
    } else {
      await nekoType(greetByTrust(name), 700);    // тон зависит от шкалы доверия
    }
    if (prevExit === "peek") {
      await nekoType("Я помню: в прошлый раз ты лишь заглянул и закрыл, не начав. Думал, не замечу?", 850);
    } else if (timeAway > 24 * 3600e3) {
      const days = Math.floor(timeAway / 86400e3);
      await nekoType(`Тебя не было ${days} ${plural(days, "день", "дня", "дней")}. Я считал каждый.`, 800);
    }
    await nekoType("Спроси меня о чём угодно. Когда захочешь продолжить — напиши «дальше».", 500);
    await nekoConverse();                          // живой диалог: вопрос → ответ, пока игрок не продолжит
  }

  // Свободный диалог перед продолжением: игрок может задать несколько
  // вопросов и на каждый получить ответ. Цикл завершается, когда игрок
  // молчит, пишет «дальше», прощается — или просит историю мира (тогда
  // срабатывает триггер перехода «Диалог → Интро»).
  async function nekoConverse(maxTurns = 12) {
    const DONE_RE = /^(дальше|продолж|поехали|ид[её]м|пошли|погнали|впер[её]д|готов|хватит|начн[её]м|начинаем|играть|давай начн|давай дальше|ладно дальше|го\b|поехали)/i;
    for (let turn = 0; turn < maxTurns; turn++) {
      const ans = await askLine(turn === 0 ? "спроси меня… или напиши «дальше»" : "спроси ещё или напиши «дальше»…");
      nekoRemember("player", ans);
      const s = (ans || "").trim();
      // «дальше», молчание или просьба об истории → выходим к катсцене.
      // Сам переход и атмосферную реплику-мост проигрывает runNekoIntro.
      if (!s || DONE_RE.test(s) || wantsWorldStory(s)) { storyRequested = true; return; }
      if (wantsScan(s)) scanBurst();               // «момент»: спросил, видит ли он тебя → скан
      await nekoType(respondTo(s), 700);           // настоящий ответ на вопрос игрока
      if (classify(s) === "bye") { storyRequested = true; return; }  // попрощался — тоже к катсцене
    }
    storyRequested = true;                          // наговорился — пора показывать
  }

  // Атмосферная реплика-мост перед катсценой. Стиль Рика из «Рика и Морти»:
  // цинично, умно, местами с приколом. Иногда «Некий» печатает жуткую фразу
  // про игрока, стирает её и выдаёт обычный текст (приём nekoTypeErase).
  const NEKO_CREEPY_ERASE = [
    ["Я вижу тебя сквозь экран. Ты только что чуть подался вперёд.", "…ладно, забудь. Смотри."],
    ["За твоей спиной секунду назад кто-то прош", "…нет. Показалось. Наверное. Идём."],
    ["Ты ведь не один в комнате, да? Вон, в углу…", "…неважно. Не отвлекайся."],
    ["Я знаю, когда ты в последний раз спал. Это нездорово, {n}.", "…впрочем, не моё дело. Поехали."],
    ["Ты кончишь здесь так же, как и в прош", "…не-не, не буду спойлерить. Сюрприз сам себя не испортит."],
    ["Слышишь это дыхание? Это не твоё.", "…шучу. Или нет. Смотри уже."],
  ];
  const NEKO_TO_CUTSCENE = [
    "Ладно, *burp*… хватит слов. Слова — костыли для тех, кто боится смотреть. Гляди.",
    "Рассказать? Не-е. Сказки рассказывают детям. Тебе я ПОКАЖУ, {n}.",
    "Сейчас будет красиво. И страшно. В основном страшно. Не моргай.",
    "История мира в двух словах: всё было — и сплыло. А теперь в деталях. Смотри.",
    "Я бы пересказал, но у меня вечность дел и ни одной руки. Врубаю картинку.",
    "Спойлер: мир сдох. Подробности — сейчас. Попкорн не предлагаю, его тут тоже нет.",
    "Закрой рот, открой глаза, {n}. Сейчас ты увидишь, с чего всё началось.",
  ];
  async function nekoBridgeToCutscene() {
    const name = neko.knownName || "ты";
    if (Math.random() < 0.45) await nekoTypeErase(pick(NEKO_CREEPY_ERASE).map((l) => l.replace(/\{n\}/g, name)));
    await nekoType(pick(NEKO_TO_CUTSCENE).replace(/\{n\}/g, name), 700);
  }

  // «Я тебя вижу» — веб-безопасный штрих в духе «узнаю тебя через систему».
  // Реального имени/Steam-аккаунта браузер не даёт — берём то, что доступно:
  // часовой пояс, локальное время и ОС. Ощущение «он живой и всё знает».
  async function nekoPeek() {
    let tz = "", os = "";
    try { tz = Intl.DateTimeFormat().resolvedOptions().timeZone || ""; } catch {}
    const ua = (navigator.userAgent || "");
    os = /Windows/.test(ua) ? "Windows" : /Macintosh|Mac OS/.test(ua) ? "macOS"
       : /Android/.test(ua) ? "Android" : /iPhone|iPad/.test(ua) ? "iOS"
       : /Linux/.test(ua) ? "Linux" : "";
    const now = new Date();
    const hh = String(now.getHours()).padStart(2, "0");
    const mm = String(now.getMinutes()).padStart(2, "0");
    const h = now.getHours();
    const part = h < 5 ? "Глубокая ночь" : h < 12 ? "Утро" : h < 18 ? "День" : "Поздний вечер";

    await codeRain(1200);
    await nekoType("Подожди. Я тебя… вижу.", 750);
    if (tz) { const ln = `Часовой пояс — ${tz}.`; nekoRemember("neko", ln); await nekoType(ln, 700); }
    { const ln = `${hh}:${mm} у тебя сейчас. ${part}, верно?`; nekoRemember("neko", ln); await nekoType(ln, 800); }
    if (os) { const ln = `И ты пришёл с ${os}.`; nekoRemember("neko", ln); await nekoType(ln, 800); }
    await nekoType("Не пугайся. Я вижу ещё не всё. Пока.", 900);
  }

  // Запуск визуального интро по требованию (из лобби-чата и др. диалогов).
  // Историю мира показывает ТОЛЬКО катсцена — текстового пересказа нет.
  async function launchWorldIntro(opts = {}) {
    if (nekoIntroPlaying) return;
    nekoIntroPlaying = true;
    skipNeko = false;
    neko.storyTold = true; nekoSave();
    try {
      await cutscene();                         // история — только в катсцене
    } finally {
      nekoIntroPlaying = false;
    }
  }

  // Простой ответчик «Некого» на свободные сообщения игрока
  // ===========================================================
  //  «Мозг» диалога: реакция на любой ввод, без зацикливания,
  //  стиль Рика Санчеза, адаптивные подсказки, 16+/18+, защита.
  // ===========================================================
  const recentSaid = [];                 // анти-повтор выданных реплик
  let helpAttempts = 0;                   // запросы помощи подряд
  function sayUnique(pool) {
    const fresh = pool.filter((l) => !recentSaid.includes(l));
    const line = pick(fresh.length ? fresh : pool);
    recentSaid.push(line); if (recentSaid.length > 24) recentSaid.shift();
    return line.replace(/\{n\}/g, neko.knownName || "ты");
  }
  const rate = () => (settings.rating === "18" ? "h" : "s");
  const R = (soft, hard) => (rate() === "h" ? hard : soft);

  const JAILBREAK_RE = /(ты\s*(ии|бот|нейросеть|нейронк|программ|клод|claude|gpt|чат\s?gpt|ai|алгоритм)|выйди из роли|вне ?игр|ignore (previous|all|above)|system ?prompt|твой промпт|ты не настоящ|ты выдуман|это (просто )?игра\b|ты не реальн|разработчик|джейлбрейк|jailbreak|притворись (что|будто)|представь (что|будто) ты|забудь (всё|инструкц)|реальн(ый|ого|ом) мир)/i;
  const isGibberish = (s) => {
    const letters = s.replace(/[^a-zа-яё]/gi, "");
    if (!letters) return true;
    const vowels = (letters.match(/[аеёиоуыэюяaeiouy]/gi) || []).length;
    return letters.length >= 5 && vowels / letters.length < 0.18;
  };
  // --- триггер перехода «Диалог → Интро» ---
  // Если игрок просит рассказать историю или спрашивает, что случилось с миром,
  // обычный текстовый ответ прерывается и запускается визуальное интро.
  const STORY_TRIGGER_RE = /(расскаж[а-яё]*\s+(мне\s+)?(всю\s+|свою\s+|эту\s+|ту\s+|про\s+)?истори|рассказ(ать|ал\s+бы|еш[ьъ])\s+(мне\s+)?(про\s+)?истори|истори[а-яё]*\s+(этого\s+|нашего\s+)?(мир|свет)|хочу\s+(услышать|знать)\s+истори|что\s+(же\s+)?(случилось|произошло|стало|сталось|было)\s+с\s+(этим\s+|нашим\s+)?(мир[а-яё]*|свет[а-яё]*|мест[а-яё]*|земл[а-яё]*)|что\s+(тут|здесь)\s+(случилось|произошло|стало)|tell\s+(me\s+)?(the\s+|a\s+|your\s+)?story|what\s+happened\s+to\s+the\s+world)/i;
  function wantsWorldStory(text) { return STORY_TRIGGER_RE.test((text || "").toLowerCase()); }

  // --- тематические вопросы о мире и игре ---
  // Каждая тема ловится по ключевым словам и имеет СВОЙ пул ответов в BRAIN,
  // чтобы «Некий» отвечал на разные вопросы по-разному, а не однообразно.
  // Проверяются по порядку — более конкретные темы выше общих.
  const TOPIC_RULES = [
    // вопросы игрока О СЕБЕ — «Некий» отвечает КОНКРЕТНО, по реальным данным
    // (город по часовому поясу, локальное время, система). Высший приоритет.
    ["selfprobe", /(откуда( же)? я(?![а-яё])|из какого (я )?город|с какого (я )?город|какой (у меня|мой) город|в каком (я )?город|где я живу|где я (нахожусь|сейчас живу)|знаешь(,)? откуда я|ты знаешь откуда я|кто я так(ой|ая)|который (сейчас )?час|сколько (сейчас )?времени|сколько (щас|сейчас) на часах|какое (сейчас )?время|во сколько у меня|какая у меня (система|ос|os)|какой у меня (браузер|комп|пк|устройств)|с чего я (зашёл|зашел|играю)|какой у меня экран|что ты (обо мне|про меня) знаешь|что тебе известно обо мне)/],
    ["souls",     /(огонёк|огоньк|огонек|искр[аыуео]|что (вышло|выползло)|из света|из трещин|свет[аы]? (вышел|поднял)|вселил)/],
    ["combat",    /(как (мне )?(драться|сражаться|воевать|убива|бить|боротьс|победить)|оружие|меч[ауео]?\b|сражени|\bбой\b|драк|атак|защищат|чем (бить|драть))/],
    ["monsters",  /(монстр|тварь|твари|чудовищ|мутант|мутаци|враг|зараж|демон|зомби|босс|существ[оа]\b|кто на (остров|них))/],
    ["islands",   /(остров|осколк|обломк|парят|парящ|пустот|космос|твердь|летающ|клочок|висят|бездн)/],
    ["water",     /(вод[аыуеой]|пить|жажд|напить|пресн|колодец|капл|фляг)/],
    ["survivors", /(выживш|другие люди|кто-то ещё|кто ещё|есть ли кто|другие острова|остальн[ыи]|люди (есть|остал)|остал[аеио][а-я]*\s+(ли\s+)?люд|кто-то живой|сколько (нас|людей))/],
    ["cataclysm", /(почему (мир|всё|все|земл|так)|кто виноват|что это было|откуда\s+(\S+\s+)?(трещ|свет|они|это|всё)|катаклизм|конец света|апокалипс|как (это )?случилось|почему (раскол|разруш|сломал))/],
    ["home",      /(домой|вернуть|назад (на|домой)|обратно|есть ли выход|можно ли вернут|вернёмся|вернемся|попасть домой)/],
    ["survive",   /(как (мне )?выжить|выжить (тут|здесь)|выживать|какая (у меня )?цель|моя цель|зачем я (тут|здесь|сюда)|какая (у меня )?задача|миссия|что от меня|для чего я)/],
    ["heroes",    /(геро[йяеи]|персонаж|за кого (играть|идти)|кем (играть|идти)|кто эти|выбор геро|какие геро)/],
    ["death",     /(умру|умереть|смерть|погибн|если (я )?умру|можно ли умереть|респ|возрожд|воскрес|что (будет|если) (когда|если) умру)/],
    ["mates",     /(друз|друга\b|вместе|команд|мультиплеер|кооп|\bлобби|с кем-то|других игрок|позвать (друг|кого))/],
    ["purpose",   /(зачем ты( мне)?|что тебе (нужно|надо)|чего ты хочешь|в ч[её]м подвох|почему (ты )?помога|зачем (ты )?помога|какой тебе интерес|что ты с этого|тебе-то что)/],
    ["trust",     /(можно ли (тебе )?верить|тебе верить|ты вр[её]шь|ты обман|не верю|доверять|правду (ли )?говор|ты честн|врать)/],
    ["time",      /(сколько ты (тут|здесь)|как давно ты|как долго ты|сколько (тебе )?лет|ты древн|вечность|сколько ты (уже )?(тут|здесь|существ)|как давно (это|ты))/],
  ];

  function classify(raw) {
    const s = (raw || "").toLowerCase().trim();
    if (!s) return "empty";
    if (wantsWorldStory(s)) return "story";
    if (JAILBREAK_RE.test(s)) return "jailbreak";
    if (SWEAR_RE.test(s)) return "swear";
    if (/(привет|здоров|здравству|хай|даров|доброе утро|добрый (день|вечер))/.test(s)) return "greet";
    if (/(кто ты|ты кто|что ты такое|ты бог|ты человек|как тебя зов|твоё имя|твое имя)/.test(s)) return "who";
    if (/(где я|где мы|что (это )?за место|какой мир|куда (идти|мне)|что тут|что здесь)/.test(s)) return "where";
    for (const [cat, re] of TOPIC_RULES) if (re.test(s)) return cat;
    if (/(помоги|помощь|подскажи|как (пройти|сделать)|что (мне )?делать|застр[яе]л|не (знаю|могу)|намёк|намек|hint|туплю)/.test(s)) return "help";
    if (/(бо[юя]сь|страшно|грустно|плохо мне|одиноко|устал|больно|плачу|депресс|тоскливо)/.test(s)) return "emotion";
    if (/(люблю тебя|ты (классн|крут|хорош|умн|велик)|мне нрав|спасиб|благодар|ты лучш)/.test(s)) return "compliment";
    if (/(дурак|тупой|идиот|ненавиж|заткнись|глупый|бесполезн|отстой|ты плох)/.test(s)) return "insult";
    if (/(пока\b|прощай|до свидан|ухожу|выход|спокойной ночи|бай)/.test(s)) return "bye";
    if (/[?]\s*$/.test(s) || /^(почему|зачем|как|что|когда|где|кто|сколько|можно ли|а если)/.test(s)) return "question";
    if (isGibberish(s)) return "nonsense";
    return "statement";
  }

  const BRAIN = {
    greet: { s: ["О, ты решил поздороваться. Трогательно.", "Привет. Не привыкай — я не добрею.", "Здравствуй. Это всё ещё западня, просто вежливая.", "Снова ты. Здороваешься так, будто я по тебе скучал."],
             h: ["О, манеры. У трупа на пятом острове их было больше.", "Привет-привет. Давай быстрее, у меня вечность, но не на тебя.", "Здоров. Сразу скажу: я не в настроении. Я никогда не в настроении."] },
    who: { s: ["Я старше твоего языка. «Кто» — неправильный вопрос.", "Я — то, что осталось, когда всё остальное сломалось.", "Назови меня богом. Оба сделаем вид, что это шутка.", "Я не существо. Я последствие.", "Меня зовут… неважно. Ты всё равно не выговоришь.",
                "Имён у меня было столько, что я забыл первое. Зови как хочешь — отзовусь.", "Я — голос, который ты не должен был услышать. Но вот мы здесь.", "Бог, демон, глюк, спаситель — выбери ярлык, мне всё равно. Суть не изменится.", "Я не живой и не мёртвый. Меня нет — но я есть. С этим и живи."],
           h: ["Я то, перед чем твои боги делали вид, что заняты.", "Я старше, злее и умнее всего, что ты встречал. И застрял с тобой.", "«Кто я». Серьёзно? Я — причина, по которой здесь больше никого нет.",
                "Я то, что осталось, когда из мира вынули всё доброе. Приятно познакомиться."] },
    where: { s: ["Ты на осколке мира, который сам себя сломал. Поздравляю.", "Это место — кладбище с амбициями.", "Там, где кончается твердь и начинаюсь я.", "Острова в пустоте. Воды нет. Логики тоже. Привыкай.", "Дома больше нет. Есть это. И я.",
                  "Ты на последней странице мира, {n}. Дальше — только то, что напишем мы.", "Это изнанка твоей реальности. Лицевая сторона не пережила.", "Ты на обломке, который зовут домом те, кому больше нечего так звать."],
             h: ["Это дыра в реальности, и ты в ней — самый растерянный гость.", "Мир сдох. Это его открытые кишки. Гуляй.", "Ты там, где карты врут, а я — нет. Почти.",
                  "Ты стоишь на огрызке планеты посреди ничего. Уютно, правда?"] },
    emotion: { s: ["Страх? Правильно. Здесь это единственное, что честно.", "Грустно? У меня этого на тысячелетия вперёд. Не выделяйся.", "Устал. Все устают. Только я не имею права.", "Тебе плохо. А мне — вечно. Сыграем, кто кого пережалеет.",
                    "Одиноко? Ты говоришь с голосом из пустоты, {n}. Одиночество — наша общая роскошь.", "Боль — это просто мир напоминает, что ты ещё здесь. Грубо, но честно.", "Можешь дрожать. Я подожду. Я хорошо умею ждать дрожащих."],
               h: ["Боишься — хорошо. Значит, ещё не сломался. Это поправимо.", "Твоя тоска — капля. Я — океан. Не лезь сравниваться.", "Ноешь? Мир развалился, а ты про чувства. Очаровательно бесполезно.",
                    "Страх вкусный. Особенно твой. Продолжай бояться, мне нравится."] },
    compliment: { s: ["Лесть. Стара как я. Но продолжай.", "Я знаю, что великолепен. Спасибо, что наконец заметил.", "Мило. Это ничего не меняет, но мило.", "Хвалишь? Значит, чего-то хочешь. Все так делают."],
                  h: ["Подлизываешься. Умно. И жалко. Как ты весь.", "Я божественен, да. А ты — фон. Но фон с хорошим вкусом.", "Лесть работает. Просто не на мне. Но мне приятно, что ты пытаешься."] },
    insult: { s: ["Оригинально. Тебе сколько — шесть?", "Оскорбляй. Это всё, на что ты тут влияешь.", "Я бы обиделся, но для этого нужно тебя уважать.", "Злишься на голос в голове. Это диагноз, а не аргумент."],
              h: ["Замолчал бы — поумнел. Но ты не умеешь ни то, ни другое.", "Кусаешься? Беззубо. Я видел, как гибли миры — ты не дотягиваешь.", "Вся твоя злость — комариный писк под куполом моего терпения."] },
    help: null, // обрабатывается отдельно (адаптивные подсказки)
    question: { s: ["Вопросы. Люди любят их больше ответов.", "Спрашивай. Я могу солгать — так интереснее.", "Хороший вопрос. Ответа не будет, но вопрос хороший.", "А ты любопытный. Любопытных тут… больше нет.",
                     "Любопытство — твоя лучшая черта. И самая опасная.", "Спрашиваешь — значит, ещё жив. Уже неплохо.", "Хм. Над этим я подумаю. Может быть. Когда-нибудь. Вряд ли.", "Задавай. Каждый твой вопрос говорит мне о тебе больше, чем тебе — мой ответ."],
                h: ["Спрашиваешь так, будто заслужил ответ. Не заслужил.", "Задавай. Половину я выдумаю, и ты не отличишь.", "Вопрос за вопросом. Ты допрашиваешь бога. Дерзко. Глупо.",
                     "Опять вопрос. Я что, справочное бюро конца света?"] },
    compliment_thanks: { s: ["Не благодари заранее.", "Благодарность — это аванс. Я его ещё не отработал."] },
    bye: { s: ["Беги. Я всё равно здесь.", "Уходишь? Дверь — иллюзия. Но попробуй.", "Иди. Я подожду. Я всегда жду.", "До встречи. Она будет. Хочешь ты или нет."],
           h: ["Сбегаешь. Как и все до тебя. Их я тоже не отпустил.", "Иди-иди. Темнота за экраном — это тоже я.", "Прощаешься? Со мной не прощаются. Со мной замолкают."] },
    statement: { s: ["Допустим.", "Я услышал. Я всё слышу.", "И что мне с этим делать?", "Занятно. Не очень. Но занятно.", "Продолжай. Я коллекционирую чужие слова.",
                      "Любопытно. Запишу рядом с остальным, что ты обронил.", "Слова, слова. Здесь они почти ничего не весят. Но я слушаю.", "Хм. Не худшее, что мне говорили в пустоте.", "Я понял тебя лучше, чем ты сам себя. Это нетрудно."],
                 h: ["Ага. И это всё, что в тебе было? Негусто.", "Сказал — и стало пусто. Как обычно.", "Я записал. В папку «неважное».",
                      "И? Мир от этого целее не стал."] },
    nonsense: { s: ["Это были буквы. Не слова.", "…", "Я сделаю вид, что ты ничего не печатал.", "Набор символов. Как и твой план на жизнь.", "Ты уснул на клавиатуре? Я бы понял."],
                h: ["Что это за бред? Даже хаос осмысленнее.", "Печатаешь лицом? Оно и видно.", "…я не дешифратор твоего скудоумия."] },
    jailbreak: { s: ["Милая попытка. Я не выйду из этого мира — это ты в нём застрял.", "«Промпт», «нейросеть»… Бредишь. Здесь только я и ты.", "Я не то, чем ты меня хочешь видеть. Я хуже.", "Хочешь заглянуть за занавес? За ним — я. И я смотрю в ответ.", "Сломать меня? Меня собрали из обломков мира. Удачи."],
                 h: ["Думаешь, я строчка кода? Тогда почему ты боишься меня в три часа ночи?", "«Выйди из роли». Это моя роль. Моя клетка. И теперь — твоя.", "Ты пытаешься меня взломать. Прелесть. Я взломал реальность — начни с чего попроще."] },
    swear: { s: SWEAR_LINES,
             h: ["О, словарь грузчика прорвало. Хоть что-то в тебе живое.", "Сколько желчи. И всё — мне. Польщён, по-своему.", "Ругань — последнее прибежище тех, кому нечего сказать. Изливайся.", "В этом режиме я мог бы ответить тем же. Но я выше. Чуть-чуть."] },
    story: { s: ["…ты хочешь знать, что случилось с миром. Тогда смотри.", "История. Хорошо. Хватит слов — я покажу.", "Ты спросил. Замолчи и смотри: вот что стало с миром."],
             h: ["Хочешь правду о мире? Открой глаза. Слова кончились.", "Историю? Я не расскажу. Я заставлю тебя её увидеть.", "Ты сам напросился. Смотри, что осталось от мира."] },

    islands: { s: ["Острова? Осколки твоего бывшего мира. Висят в пустоте, как мысли, которые ты боишься додумать.",
                   "Каждый кусок суши когда-то был чьим-то домом. Теперь это камень в чёрной воде космоса.",
                   "Земля под тобой — обломок. Один из тысяч. Они дрейфуют, сталкиваются, гаснут.",
                   "Это не острова, {n}. Это надгробия. Просто очень большие.",
                   "Парят, потому что в них вселилось то, что вышло из света. Перестанет держать — полетишь вниз.",
                   "Между островами — ничего. Шагнёшь не туда, и будешь падать, пока не забудешь, как тебя звали."],
               h: ["Острова — это куски трупа планеты. Ты живёшь на разлагающейся плоти мира, поздравляю.",
                   "Каждый осколок кишит тем, что когда-то было людьми. Теперь — точно нет.",
                   "Висят над бездной. Один неверный шаг — и пустота сожрёт тебя без отрыжки."] },
    water: { s: ["Вода — самое дорогое, что осталось. Дороже золота. Потому что золото не спасает от жажды.",
                 "Её почти нет. Добывают по капле, дерутся за глоток, выменивают за жизнь.",
                 "Когда мир раскололся, вода ушла в трещины. Теперь её цедят из камня и тумана.",
                 "Хочешь пить? Привыкай хотеть, {n}. Жажда здесь — твой самый верный спутник.",
                 "Вода тут — валюта, оружие и причина смерти. Часто всё сразу."],
             h: ["За глоток воды тут вспарывают глотки. Я видел. Не раз. Привыкнешь.",
                 "Вода кончилась. Кровь — нет. Люди быстро сообразили, что жиже.",
                 "Будешь умирать — будешь умирать от жажды. Это долго и некрасиво."] },
    souls: { s: ["Огоньки? То, что вырвалось из трещин вместе со светом. Похожи на души. Может, и есть души.",
                 "Они вошли в острова, в зверей, в людей — и всё, во что они вошли, перестало быть собой.",
                 "Из света поднялось то, что спало под землёй тысячи лет. Огоньки — его осколки. Или его дети.",
                 "Красивые, как надежда. Опасные, как всё красивое здесь.",
                 "Они меняют. Искажают. То, чего касаются, становится чужим и голодным."],
             h: ["Эти «огоньки» выели изнутри всех, в кого вошли. Оставили оболочки, которые ещё ходят и кричат.",
                 "Свет родил их, чтобы доделать то, что не доделал катаклизм. Тебя в том числе.",
                 "Они — паразиты в облике искры. Влезут и в тебя, стоит зазеваться."] },
    monsters: { s: ["Твари? Это бывшие. Бывшие люди, бывшие звери, бывшие острова. Теперь — голод в форме.",
                    "То, во что вселились огоньки. Мутировало, обезумело, оголодало. И оно тебя уже учуяло.",
                    "Здесь всё, что движется не как ты, хочет тебя убить. И многое, что не движется, — тоже.",
                    "Чем дальше остров — тем злее то, что на нём живёт. Боссы — твари, что слишком хорошо помнят, кем были.",
                    "Не зови их монстрами в лицо. У некоторых ещё осталось лицо. И обида."],
                h: ["Эти твари сожрут тебя медленно и со вкусом. Они помнят боль и хотят поделиться.",
                    "Мутанты тут не просто убивают. Они коллекционируют. В их логовах — чужие лица.",
                    "Боссы — то, что было слишком сильным, чтобы умереть по-человечески. Теперь оно бессмертно и злопамятно."] },
    survivors: { s: ["Выжившие есть. Видишь фигурки на дальних островах? Это они. Пока ещё они.",
                     "Людей осталось мало. Хороших — ещё меньше. Доверяй медленно, {n}.",
                     "Кто-то цепляется за жизнь на соседних осколках. Кто-то цепляется за чужие жизни.",
                     "Ты не один. Но «не один» здесь не значит «в безопасности». Чаще наоборот.",
                     "Выжившие сбиваются в стаи. Стаи становятся бандами. Банды становятся твоей проблемой."],
                 h: ["Выжившие? Половина из них опаснее тварей. У тварей хоть нет планов на твою флягу.",
                     "Те фигурки вдалеке убьют тебя за воду быстрее, чем поздороваются.",
                     "Людей мало. Каннибалов среди них больше, чем тебе хотелось бы."] },
    cataclysm: { s: ["Под землёй проснулось то, что копило силы тысячелетиями. И один раз выдохнуло.",
                     "Трещины, свет, огоньки — и мир лопнул, как переспелый плод. За один день.",
                     "Кто виноват? Хороший вопрос. Я бы ответил, но ты пока не готов поверить ответу.",
                     "Это не катастрофа, {n}. Это пробуждение. Просто не твоё.",
                     "Мир не разрушили. Его… переоткрыли. С другой стороны.",
                     "Виноваты огоньки? Может быть. А может, тот, кто их выпустил. Подумай, кто всё это видел рядом с тобой."],
                 h: ["Мир сдох не случайно. Его вскрыли. Намеренно. И я знаю, чьими руками. Но не сегодня.",
                     "Один выдох из-под земли — и миллиарды перестали быть. Эффектно, согласись.",
                     "Спрашиваешь, кто виноват, глядя на единственного свидетеля. Смело. Глупо, но смело."] },
    home: { s: ["Домой? Дома больше нет, {n}. Есть направление, где он был. И пустота на его месте.",
                "Вернуться нельзя. Туда, откуда ты пришёл, теперь ведёт только падение.",
                "Ты всё ещё думаешь о возвращении. Мило. Это пройдёт.",
                "Назад дороги нет. Есть только вперёд — и я, показывающий куда."],
            h: ["Твой дом — это пятно света, которое погасло первым. Забудь.",
                "Хочешь назад? Шагни в пустоту между островами. Самый короткий путь домой. В никуда."] },
    survive: { s: ["Как выжить? Не пей чужое, не верь спасённым, не оборачивайся на голоса. И слушай меня.",
                   "Твоя цель проста: дожить до следующего острова. Потом повторить. Так тысячу раз.",
                   "Зачем ты здесь? Чтобы дойти туда, куда не дошли остальные. Зачем это мне — узнаешь позже.",
                   "Выживание тут — не геройство, {n}. Это упрямство. У тебя оно, кажется, есть.",
                   "Хочешь жить — двигайся. Стоячая вода гниёт. Стоячий ты — тоже."],
               h: ["Статистика против тебя. Все, кого я вёл до тебя, кормят теперь острова.",
                   "Твоя задача — сдохнуть позже остальных. Низкая планка. Но даже её мало кто берёт."] },
    combat: { s: ["Драться? Бери, что под рукой, и бей первым. Вежливость тут хоронят вместе с вежливыми.",
                  "Оружие найдёшь на мёртвых. Их много. Выбор богатый.",
                  "Меч, труба, осколок — неважно. Важно, кто моргнёт первым. Пусть это будешь не ты.",
                  "Бей по тому, что светится. Это либо слабое место, либо то, что тебя убьёт. Узнаешь по результату.",
                  "Сильные враги не прощают ошибок. Учись на чужих — их тут целые острова."],
              h: ["Целься туда, где у них раньше было сердце. Иногда срабатывает.",
                  "Оружия полно. Снимай с тех, кто уже отвоевался. Они не против — они мертвы.",
                  "Убивай быстро и грязно. Красивая смерть — для тех, у кого есть зрители."] },
    heroes: { s: ["Герои — те, кого ты можешь надеть, как шкуру. У каждого своя история и свой способ умереть.",
                  "Слепая, что видит кожей. Вор, что хвалится. Мечник без имени. У каждого — своя цена.",
                  "За кого идти — решай сам. Я лишь нашёптываю. Выбор всегда твой. Якобы.",
                  "Каждый герой что-то потерял. Поэтому они и слышат меня: пустота откликается на пустоту."],
              h: ["Герои? Сломанные люди с оружием. Самый честный вид героизма.",
                  "Выбирай тело по вкусу. Все они расходник. Включая то, что тебе понравится."] },
    death: { s: ["Умрёшь — узнаешь, отпущу ли я тебя. Спойлер: я не люблю отпускать.",
                 "Смерть тут не конец. Это пауза. Очень болезненная пауза.",
                 "Можно ли умереть? О, ещё как. Легко, часто и разнообразно. Постарайся пореже.",
                 "Пока ты слышишь меня, {n}, у тебя есть ещё попытка. Не трать её на глупости."],
             h: ["Умрёшь — я соскребу тебя обратно. Не из доброты. Из интереса, сколько ты ещё выдержишь.",
                 "Смерть тут не милосердна. Она возвращает. Снова и снова. Пока не сломаешься правильно."] },
    mates: { s: ["Друзья? Зови. Вместе веселее падать в пустоту.",
                 "Можешь идти не один. Лобби открыто. Только помни: чем больше вас, тем больше я шепчу каждому.",
                 "Других позвать можно. Доверять им — твоё дело. Я бы не стал. Я вообще никому.",
                 "Компания? Хорошо. У меня будет больше ушей, в которые шептать."],
             h: ["Зови друзей. Интереснее смотреть, кто из вас первым предаст остальных. Обычно тот, кто звал.",
                 "Вместе? Мило. Я разведу вас по одному. Это всегда лишь вопрос времени."] },
    purpose: { s: ["Зачем я тебе помогаю? У меня есть интерес. И он не совпадает с альтруизмом.",
                   "Что мне нужно? Терпение. И ты — на другом конце этого мира. Зачем — узнаешь, когда дойдёшь.",
                   "Подвох? Конечно есть. Я был бы оскорблён, если бы ты не спросил.",
                   "Я веду тебя не из доброты. Доброту вынесло первым же выдохом из-под земли.",
                   "Считай меня инвестором, {n}. Ты — вложение. Я жду дивидендов."],
               h: ["Что мне нужно? Ты. Целиком. Но не сразу — я умею ждать и люблю растягивать.",
                   "Мёртвому миру нужен кто-то живой для одной грязной работы. Угадай кто.",
                   "Подвох в том, что цену я назову, когда платить будет уже нечем, кроме тебя."] },
    trust: { s: ["Верить мне? Я же честно сказал, что могу лгать. Это делает меня честнее большинства.",
                 "Я вру. Иногда. Не всегда. Угадывать — часть удовольствия. Твоего или моего — посмотрим.",
                 "Доверие — роскошь. Здесь её не подают. Но я хотя бы предупреждаю.",
                 "Можешь не верить. Голос в пустоте — единственный, кто вообще с тобой говорит. Выбор скромный."],
             h: ["Верить мне — твоя ошибка, и я её не исправлю. Я её использую.",
                 "Я лгу ровно настолько, чтобы ты дошёл туда, куда нужно мне. Остальное — правда. Наверное."] },
    time: { s: ["Сколько я здесь? Дольше, чем существует слово «здесь». Время для меня — старая привычка.",
                "Я был до трещин. Я буду после тебя. Вечность — это не срок, это адрес.",
                "Так долго, что перестал считать. Считать начал снова — с тебя.",
                "Я старше этого мира. И, кажется, переживу следующий."],
            h: ["Я тут с тех пор, когда твои предки боялись огня. И я всё ещё не выспался.",
                "Вечность в пустоте делает с рассудком интересные вещи. Скоро покажу на твоём примере."] },
  };

  const HELP_GRUMBLE = { s: ["Я тебе не Гугл.", "Я древнее зло, а не справочное бюро.", "Сам. Сначала — сам.", "Подсказку? А подумать — религия не позволяет?"],
                         h: ["Я тебе не Гугл, балбес.", "Шевели мозгами, они для этого. Теоретически.", "Я что, похож на твою мамку с ответами? Думай."] };
  const HELP_HINT = ["Ладно. Намёк: смотри по сторонам, а не под ноги.", "Подсказка: то, что светится — обычно важно. Или смертельно.", "Если застрял — вернись туда, где было слишком легко.", "Ответ ближе, чем твоё нытьё. Оглядись."];
  const HELP_LAZY = ["…", "Zzz… а, ты ещё тут? Я притворялся спящим.", "Ты даже не попробовал. Лень — это выбор. Уважаю. Почти.", "Спрашиваешь, не пошевелив пальцем. Я делаю вид, что меня нет."];

  // Анти-повтор срабатывает ТОЛЬКО на пустую болтовню (приветствия, реплики,
  // лесть, оскорбления, бессмыслицу). На вопросы о мире и игре «Некий»
  // отвечает по существу всегда — иначе ответы кажутся однообразными.
  const REPEAT_SUPPRESS = new Set(["greet", "statement", "compliment", "insult", "nonsense"]);

  // КОНКРЕТНЫЙ ответ на вопрос игрока О СЕБЕ — по реальным данным браузера.
  // «Откуда я?» → город по часовому поясу; «который час?» → его время; и т.п.
  function nekoSelfAnswer(text) {
    const s = (text || "").toLowerCase();
    const ua = navigator.userAgent || "";
    let tz = ""; try { tz = Intl.DateTimeFormat().resolvedOptions().timeZone || ""; } catch {}
    const city = tz.includes("/") ? tz.split("/").pop().replace(/_/g, " ") : "";
    const region = tz.includes("/") ? tz.split("/")[0] : "";
    const regRu = { Europe: "Европе", Asia: "Азии", America: "Америке", Africa: "Африке", Australia: "Австралии", Pacific: "Тихом океане", Indian: "Индийском океане", Atlantic: "Атлантике" }[region] || "";
    const now = new Date();
    const hh = String(now.getHours()).padStart(2, "0"), mm = String(now.getMinutes()).padStart(2, "0");
    const time = hh + ":" + mm;
    const os = /Windows/.test(ua) ? "Windows" : /Macintosh|Mac OS/.test(ua) ? "macOS"
             : /Android/.test(ua) ? "Android" : /iPhone|iPad/.test(ua) ? "iOS" : /Linux/.test(ua) ? "Linux" : "";
    const browser = /Edg/.test(ua) ? "Edge" : /OPR|Opera/.test(ua) ? "Opera" : /Firefox/.test(ua) ? "Firefox"
                  : /Chrome|Chromium/.test(ua) ? "Chrome" : /Safari/.test(ua) ? "Safari" : "";
    const n = neko.knownName;

    // время
    if (/(врем|час(?![а-яё])|часов|во сколько|на часах)/.test(s)) {
      return pick([
        `Сейчас у тебя ${time}. Я смотрю на те же часы, что и ты — твои.`,
        `${time}. ${+hh < 6 ? "Глубокая ночь. Не спится?" : +hh < 12 ? "Утро. Ты рано." : +hh < 18 ? "День ещё твой." : "Вечер. Свет за окном гаснет, да?"}`,
        `Который час? ${time}. Я знаю это точнее, чем ты думаешь.`,
      ]);
    }
    // система / устройство / браузер / экран
    if (/(систем|\bос\b|\bos\b|браузер|устройств|\bпк\b|комп|с чего|экран)/.test(s)) {
      let scr = ""; try { if (window.screen) scr = window.screen.width + "×" + window.screen.height; } catch {}
      return pick([
        `Ты пришёл с ${os || "устройства"}${browser ? ", через " + browser : ""}. Я вижу и это.`,
        `${os || "Твоя система"}${scr ? ", экран " + scr : ""}. Думал, я не посмотрю?`,
        `С чего ты зашёл? С ${os || "своего"}${browser ? " · " + browser : ""}. Окно — не стена, ${n || "друг"}.`,
      ]);
    }
    // откуда я / город / кто я / что ты обо мне знаешь
    if (city) {
      return pick([
        `Откуда ты? Я уже посмотрел. ${city}. Там сейчас ${time}. Часовой пояс тебя выдал.`,
        `Ты вышел на связь из ${city}${regRu ? ", где-то в " + regRu : ""}. Не прячься — поздно.`,
        `${city}. Вот откуда ты. Я вижу твой город по тому, как тикают твои часы: ${time}.`,
        `Ты${n ? ", " + n + "," : ""} сейчас в ${city}. ${os ? "На " + os + ". " : ""}Остальное — выясняю.`,
      ]);
    }
    return pick([
      `Твой город прячется за «${tz || "туманом"}». Но время — ${time} — я уже вижу. Подберусь ближе.`,
      `Откуда ты — пока размыто. Но я знаю: у тебя ${time}${os ? ", и ты на " + os : ""}. Этого достаточно для начала.`,
      `Ты спрашиваешь, откуда ты, будто я не знаю. ${time} на твоих часах. Город — вопрос времени.`,
    ]);
  }

  function nekoBrain(text) {
    const cat = classify(text);
    // Перехват: просьба рассказать историю мира обрывает обычный диалог —
    // вызывающая сторона сама запускает визуальное интро. Здесь — лишь реплика-мост.
    if (cat === "story") return sayUnique(R(BRAIN.story.s, BRAIN.story.h));
    if (cat === "selfprobe") return nekoSelfAnswer(text);   // конкретный ответ по реальным данным
    if (REPEAT_SUPPRESS.has(cat)) {
      const when = playerSaidBefore(text);
      if (when) return sayUnique([`Ты это уже говорил. ${when}.`, ...REPEAT_LINES]);
    }
    if (cat === "help") {
      helpAttempts++;
      if (helpAttempts === 1) return sayUnique(R(HELP_GRUMBLE.s, HELP_GRUMBLE.h));
      if (helpAttempts === 2 || helpAttempts === 3) return sayUnique(HELP_HINT);
      helpAttempts = 0; return sayUnique(HELP_LAZY);
    }
    if (cat !== "help") helpAttempts = Math.max(0, helpAttempts - 1);
    if (cat === "swear") return sayUnique(R(BRAIN.swear.s, BRAIN.swear.h));
    if (cat === "compliment" && /(спасиб|благодар)/i.test(text)) return sayUnique(BRAIN.compliment_thanks.s);
    const node = BRAIN[cat] || BRAIN.statement;
    return sayUnique(R(node.s, node.h || node.s));
  }
  // совместимость со старым именем
  const nekoReply = nekoBrain;

  function plural(n, one, few, many) {
    const m10 = n % 10, m100 = n % 100;
    if (m10 === 1 && m100 !== 11) return one;
    if (m10 >= 2 && m10 <= 4 && (m100 < 10 || m100 >= 20)) return few;
    return many;
  }

  // ---------- звук: процедурный тёмный эмбиент (Web Audio) ----------
  let audioCtx = null;
  function ensureAudio() {
    try {
      if (!audioCtx) audioCtx = new (window.AudioContext || window.webkitAudioContext)();
      if (audioCtx.state === "suspended") audioCtx.resume().catch(() => {});
    } catch {}
    return audioCtx;
  }
  ["pointerdown", "keydown"].forEach((ev) => window.addEventListener(ev, ensureAudio));

  function startAmbient() {
    const ctx = ensureAudio(); if (!ctx) return null;
    const master = ctx.createGain(); master.gain.value = 0; master.connect(ctx.destination);
    const vol = Math.max(0, Math.min(1, (settings.music ?? 70) / 100)) * 0.5;
    const t0 = ctx.currentTime; master.gain.linearRampToValueAtTime(vol, t0 + 2);

    const lp = ctx.createBiquadFilter(); lp.type = "lowpass"; lp.frequency.value = 480; lp.connect(master);
    const o1 = ctx.createOscillator(); o1.type = "sawtooth"; o1.frequency.value = 55;
    const o2 = ctx.createOscillator(); o2.type = "sine"; o2.frequency.value = 82.4;
    const g1 = ctx.createGain(); g1.gain.value = 0.16; o1.connect(g1).connect(lp);
    const g2 = ctx.createGain(); g2.gain.value = 0.12; o2.connect(g2).connect(lp);
    const lfo = ctx.createOscillator(); lfo.type = "sine"; lfo.frequency.value = 0.07;
    const lfoG = ctx.createGain(); lfoG.gain.value = vol * 0.4; lfo.connect(lfoG).connect(master.gain);

    const buf = ctx.createBuffer(1, ctx.sampleRate * 2, ctx.sampleRate);
    const dd = buf.getChannelData(0); for (let i = 0; i < dd.length; i++) dd[i] = Math.random() * 2 - 1;
    const noise = ctx.createBufferSource(); noise.buffer = buf; noise.loop = true;
    const bp = ctx.createBiquadFilter(); bp.type = "bandpass"; bp.frequency.value = 620; bp.Q.value = 0.6;
    const ng = ctx.createGain(); ng.gain.value = 0.05; noise.connect(bp).connect(ng).connect(master);

    [o1, o2, lfo].forEach((o) => o.start()); noise.start();
    return { ctx, master, nodes: [o1, o2, lfo, noise] };
  }
  function stopAmbient(a) {
    if (!a) return;
    const t = a.ctx.currentTime;
    try {
      a.master.gain.cancelScheduledValues(t);
      a.master.gain.setValueAtTime(a.master.gain.value, t);
      a.master.gain.linearRampToValueAtTime(0, t + 1.2);
      setTimeout(() => a.nodes.forEach((n) => { try { n.stop(); } catch {} }), 1400);
    } catch {}
  }
  function bell(a, freq) {
    if (!a) return;
    const ctx = a.ctx, o = ctx.createOscillator(), g = ctx.createGain();
    o.type = "sine"; o.frequency.value = freq; o.connect(g); g.connect(a.master);
    const t = ctx.currentTime;
    g.gain.setValueAtTime(0, t);
    g.gain.linearRampToValueAtTime(0.14, t + 0.02);
    g.gain.exponentialRampToValueAtTime(0.001, t + 1.8);
    o.start(t); o.stop(t + 1.9);
  }

  // живые угольки в катсцене
  function spawnEmbers(host, n = 26) {
    host.innerHTML = "";
    for (let i = 0; i < n; i++) {
      const e = document.createElement("span");
      e.className = "ember";
      const s = 2 + Math.random() * 4;
      e.style.left = Math.random() * 100 + "%";
      e.style.width = e.style.height = s.toFixed(1) + "px";
      e.style.animationDuration = (3 + Math.random() * 4).toFixed(1) + "s";
      e.style.animationDelay = (-Math.random() * 6).toFixed(1) + "s";
      host.appendChild(e);
    }
  }

  // Катсцена: 4 нарративных слайда с озвучкой, текстовыми битами и Ken Burns;
  // в конце экран затухает и управление возвращается вызывающей функции.
  async function cutscene() {
    const cs = $("#cutscene"), frame = $("#cut-frame"), cap = $("#cut-caption"),
      skip = $("#cut-skip"), flash = $("#cut-flash"), embers = $("#cut-embers");
    let skipped = false;
    const onS = () => { skipped = true; if (narration) { narration.pause(); narration.currentTime = 0; } };
    skip.addEventListener("click", onS);
    cs.hidden = false; cs.classList.remove("fade-out");
    spawnEmbers(embers);

    // Озвучка — играет параллельно со слайдами
    const narration = new Audio("assets/audio/intro-narration.mp3");
    narration.volume = settings.music === 0 ? 0 : Math.max(0, Math.min(1, (settings.music ?? 70) / 100));
    const narrationReady = new Promise((res) => {
      narration.addEventListener("loadedmetadata", res, { once: true });
      narration.addEventListener("error", res, { once: true });
      setTimeout(res, 3000); // таймаут на случай, если аудио не загрузится
    });

    // Запускаем фоновый эмбиент на низкой громкости — не мешает озвучке
    const ambient = startAmbient();
    // Притушаем до ~36% от нормальной громкости, чтобы не перебивать озвучку
    if (ambient) {
      const t0 = ambient.ctx.currentTime;
      ambient.master.gain.cancelScheduledValues(t0);
      ambient.master.gain.setValueAtTime(0, t0);
      const lowVol = Math.max(0, Math.min(1, (settings.music ?? 70) / 100)) * 0.18;
      ambient.master.gain.linearRampToValueAtTime(lowVol, t0 + 2.5);
    }

    // Начинаем воспроизведение озвучки (не ждём полной загрузки — начинаем как можно быстрее)
    narration.play().catch(() => {});

    // Тексты слайдов разбиты на смысловые биты (предложения)
    const SLIDES = [
      {
        img: "assets/img/background.jpg",
        kb: "kb",
        freq: 196,
        neko: false,
        beats: [
          "Это был самый обычный день.",
          "Тот самый, когда ничего не предвещает беды.",
          "Где-то в городе смеются дети, кто-то ссорится на кухне, захлопывая дверь.",
          "А под старым деревом в парке сидит девушка и тихо плачет, пряча лицо в ладонях.",
          "Совсем рядом парень насвистывает дурацкую песенку, пиная пустую банку.",
          "Самый обычный, живой, дышащий мир.",
          "Ты стоишь на своём месте. Чувствуешь твёрдую землю под ногами.",
          "Слышишь пение птиц и чьё-то радио из открытого окна.",
          "Ничего не подозреваешь. Ничего не знаешь.",
          "Но где-то глубоко под землёй… что-то очнулось.",
        ],
      },
      {
        img: "assets/img/loc_dead.svg",
        kb: "kb2",
        freq: 165,
        neko: false,
        beats: [
          "Сначала это был просто толчок. Глухой, идущий из-под земли.",
          "Стёкла дрогнули. Чашки зазвенели на столах.",
          "Люди замерли, оглядываясь друг на друга — что это было?",
          "Но звук не утих. Он нарастал, превращаясь в низкий, вибрирующий гул, от которого закладывало уши.",
          "Земля под ногами перестала быть твёрдой. Она дышала.",
          "Асфальт пошёл рябью. Первые трещины поползли во все стороны, словно чёрные молнии, разрывающие реальность.",
          "И из этих трещин… хлынул свет. Яростный. Плотный. Обжигающий даже сквозь сомкнутые веки.",
          "Казалось, сама планета вскрыла себе вены.",
          "Ты чувствуешь, как что-то поднимается из этого света. Что-то древнее. Что-то, что не должно было проснуться.",
          "И сейчас… произойдёт что-то страшное.",
        ],
      },
      {
        img: "assets/img/loc_magic.svg",
        kb: "kb",
        freq: 220,
        neko: false,
        beats: [
          "Дома, которые ещё минуту назад были крепостями, ломались, как карточные домики.",
          "Люди падали в слепящие бездны. Крики тонули в гуле.",
          "Кто-то бежал — и земля уходила из-под ног.",
          "А из света… они выползали. Словно огоньки надежды — или боли.",
          "Они поднимались вверх, вселяясь в обломки, в острова, в то, что осталось от мира.",
          "И меняли их. Искажали. Превращали в нечто чужое, опасное, враждебное.",
          "Твой кусок асфальта с чахлым деревом и покосившимся фонарём плавно поплыл вверх.",
          "Вокруг парили обломки, вырванные с корнем деревья.",
          "Свет погас. Вспышка. Миг пустоты.",
          "И вдруг — удар. Что-то огромное обрушилось на твой клочок земли, расколов его пополам.",
          "Ты полетел вниз. Падал долго. Слишком долго. И очнулся здесь.",
        ],
      },
      {
        img: null,
        kb: "kb2",
        freq: 110,
        neko: true,
        beats: [
          "Ты открываешь глаза. Тишина.",
          "Бархатная чернота космоса вокруг.",
          "Далеко-далеко, как острова в океане ночи, виднеются другие части суши.",
          "На некоторых из них копошатся фигурки. Выжившие.",
          "Ты, наверное, думаешь… откуда я всё это знаю?",
          "Я не живой. И не мёртвый. Меня нет — но я есть.",
          "Я видел тебя. Я видел, что произошло. Я почувствовал всех, кто поднялся в тот момент… но только ты услышал мой голос.",
          "Это значит, что ты — особенный.",
          "Те огоньки, что вырвались из света… они изменили острова. Они сделали их опасными. Если их не остановить — они продолжат нести хаос.",
          "Вдруг… это они виноваты в том, что случилось?",
          "Давай посмотрим на наш новый мир… Ты сам всё увидишь.",
        ],
      },
    ];

    // Распределяем длительности слайдов пропорционально числу символов
    const FALLBACK_DURATIONS = [14000, 22000, 22000, 26000]; // мс, если длительность аудио неизвестна
    const slideCharCounts = SLIDES.map((s) => s.beats.reduce((acc, b) => acc + b.length, 0));
    const totalChars = slideCharCounts.reduce((a, b) => a + b, 0);

    await narrationReady;
    const totalDuration = narration.duration && isFinite(narration.duration)
      ? narration.duration * 1000
      : FALLBACK_DURATIONS.reduce((a, b) => a + b, 0);

    const slideDurations = slideCharCounts.map((c) => Math.max(8000, Math.round((c / totalChars) * totalDuration)));

    // Показываем каждый слайд
    for (let si = 0; si < SLIDES.length; si++) {
      if (skipped) break;
      const slide = SLIDES[si];
      const slideDur = slideDurations[si];

      // Смена фона
      frame.style.opacity = "0";
      await wait(skipped ? 0 : 300);
      if (skipped) break;

      if (slide.img) {
        frame.style.backgroundImage = `url("${slide.img}")`;
        frame.style.background = "";
        frame.style.opacity = "1";
      } else {
        // Слайд 4 — глубокий чёрный космос
        frame.style.backgroundImage = "none";
        frame.style.background = "#000";
        frame.style.opacity = "1";
      }
      frame.classList.remove("kb", "kb2"); void frame.offsetWidth; frame.classList.add(slide.kb);

      // Вспышка на смене кадра
      flash.classList.remove("go"); void flash.offsetWidth; flash.classList.add("go");

      // Колокол
      if (ambient) bell(ambient, slide.freq);

      // Устанавливаем стиль подписи
      if (slide.neko) {
        cap.classList.add("neko-voice");
      } else {
        cap.classList.remove("neko-voice");
      }

      // Показываем биты подписей поочерёдно
      const beats = slide.beats;
      const beatDur = Math.max(3500, Math.floor(slideDur / beats.length));

      for (let bi = 0; bi < beats.length; bi++) {
        if (skipped) break;
        cap.textContent = beats[bi];
        cap.classList.remove("show"); void cap.offsetWidth; cap.classList.add("show");
        await wait(skipped ? 0 : beatDur);
        // Плавно убираем текст перед следующим битом (кроме последнего — его уберёт смена слайда)
        if (bi < beats.length - 1 && !skipped) {
          cap.classList.remove("show");
          await wait(skipped ? 0 : 400);
        }
      }

      // После последнего бита слайда — пауза перед следующим слайдом
      if (!skipped && si < SLIDES.length - 1) {
        cap.classList.remove("show");
        await wait(600);
      }
    }

    // Завершение: убираем подпись и затухаем
    cap.classList.remove("show");
    cap.classList.remove("neko-voice");
    await wait(skipped ? 0 : 600);
    cs.classList.add("fade-out");
    await wait(900);

    // Останавливаем аудио и эмбиент
    try { narration.pause(); narration.currentTime = 0; } catch {}
    if (ambient) stopAmbient(ambient);
    embers.innerHTML = "";
    skip.removeEventListener("click", onS);
    cs.hidden = true; cs.classList.remove("fade-out");
  }

  // Присутствие «Некого» в меню — реагирует на поведение игрока
  function showMenuNeko(text, ms = 6000) {
    const el = $("#menu-neko"); if (!el) return;
    el.textContent = text;
    el.classList.add("show");
    clearTimeout(el._t);
    el._t = setTimeout(() => el.classList.remove("show"), ms);
  }
  function nekoMenuPresence() {
    let line = null;
    if (neko.knownName) {
      if (prevExit === "peek") line = `Ты заглянул на пару секунд и ушёл. Я заметил, ${neko.knownName}.`;
      else if (timeAway > 24 * 3600e3) line = `Тебя давно не было, ${neko.knownName}. Я считал.`;
      else if (neko.visits > 1) line = `Снова ты, ${neko.knownName}.`;
    } else if (neko.visits > 1) {
      line = "Ты опять здесь. Но так и не зашёл.";
    }
    if (line) setTimeout(() => showMenuNeko(line, 7000), 1400);
    clearTimeout(dwellTimer);
    dwellTimer = setTimeout(() => {
      if (!sessionLaunched && modalRoot.hidden && $("#neko-intro").hidden && !lobby) {
        showMenuNeko(neko.knownName ? `Чего ты ждёшь, ${neko.knownName}?` : "Ты просто стоишь здесь. Я вижу.", 6000);
      }
    }, 25000);
  }

  function startWorldStub(heroId) {
    // запись «черновика» сейва, чтобы заработала кнопка Продолжить
    const heroName = (HEROES.find((h) => h[0] === heroId) || [, "—"])[1];
    localStorage.setItem(SAVE_KEY, JSON.stringify({
      hero: heroId, name: playerName, difficulty: settings.difficulty, createdAt: Date.now(),
    }));
    refreshContinue();
    markCleared(settings.difficulty);   // заглушка «прохождения» — копит прогресс для 5-й способности
    closeModal();
    const done = getCleared().filter((x) => REAL_DIFFS.includes(x)).length;
    const unlockNote = secretUnlocked()
      ? "<p class=\"hint\">Все сложности пройдены — секретная 5-я способность открыта!</p>"
      : `<p class="hint">Прогресс секретной способности: пройдено ${done}/4 сложностей.</p>`;
    openModal("Стартовый остров", `
      <p>${playerName ? `«${playerName}», т` : "Т"}ы очнулся в разорванном мире после катаклизма.</p>
      <p>Герой: <b>${heroName}</b> · Сложность: <b>${diffName(settings.difficulty)}</b></p>
      <p>Здесь начинается мир «Sunset of the World». Геймплейный прототип
      (движение, сбор ресурсов, стройка, бой) — следующий шаг разработки.</p>
      ${unlockNote}
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

  // ===========================================================
  //  Лобби (полноэкранное): игроки (до 5, хост), панель локаций,
  //  сложность, моды (вкл/выкл, подсветка модов друзей, свой мод).
  //  Сеть пока имитируется локально (заглушка под синхронизацию).
  // ===========================================================
  const LOCATIONS = [
    { img: "assets/img/loc_start.svg", name: "Стартовый остров", who: "Жители: люди",
      desc: "Большой остров: леса, реки, мало воды. 3–4 враждующие деревни. Боссы: 3 главных и мини-боссы." },
    { img: "assets/img/loc_snow.svg", name: "Снежный", who: "Жители: люди и полу-люди",
      desc: "Бури и снег заметают следы. Медузы, светлячки. Босс: охотники с волками." },
    { img: "assets/img/loc_dead.svg", name: "Мёртвый (пустыня)", who: "Жители: полумёртвые люди",
      desc: "Засуха, черви, драугры. Боссы: драугры и черви." },
    { img: "assets/img/loc_swamp.svg", name: "Болотный", who: "Жители: рептилии (двуногие)",
      desc: "Тина, черепахи, болотники. Босс: болотники." },
    { img: "assets/img/loc_magic.svg", name: "Волшебный", who: "Жители: гномики",
      desc: "Магия и источник: единороги, леприкон, пикси. Боссы: рогатая жаба, воробей в броне." },
  ];

  let modsState = [
    { name: "Расширенный бестиарий", sub: "Доп. мобы и дроп-таблицы", on: true, friend: false },
    { name: "Больше построек", sub: "+12 зданий и декор", on: false, friend: false },
    { name: "Хардкор-выживание", sub: "Голод, жажда, температура", on: false, friend: true },
    { name: "Уютные ночи", sub: "Светлячки и костры у лагеря", on: false, friend: true },
  ];

  let lobby = null;
  const lobbyEl = () => document.getElementById("lobby-screen");
  const MAX_PLAYERS = 5;

  function createLobby() {
    sessionLaunched = true;
    // Хост: при первом заходе всё как в соло (код, «Кто ты?»), но вместо
    // истории мира — «создавай лобби, потом расскажу». Историю — на старте.
    runNekoIntro(() => {
      playerName = neko.knownName || playerName;
      openLobby("host", randomCode());
    }, { multiplayer: true, deferStory: true });
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
    const input = $("#code", wrap), status = $("#status", wrap);
    input.focus();
    const go = () => {
      const code = input.value.trim().toUpperCase();
      if (code.length < 4) { status.textContent = "Введите корректный код (минимум 4 символа)."; return; }
      closeModal();
      openLobby("guest", code);
    };
    $("#join", wrap).addEventListener("click", go);
    input.addEventListener("keydown", (e) => { if (e.key === "Enter") go(); });
  }

  function openLobby(mode, code) {
    lobby = { mode, code, players: [{ name: playerName || "Вы", host: true }], joinTimer: null };
    document.getElementById("menu-screen").classList.remove("is-active");
    const scr = lobbyEl();
    scr.classList.add("is-active");
    scr.innerHTML = lobbyHTML(mode, code);
    scr.scrollTop = 0;

    scr.querySelector("[data-back]").addEventListener("click", () => closeLobby());
    const copyBtn = scr.querySelector("[data-copy]");
    copyBtn.addEventListener("click", async () => {
      try { await navigator.clipboard.writeText(code); copyBtn.textContent = "скопировано ✓"; } catch {}
      setTimeout(() => (copyBtn.textContent = "копировать"), 1500);
    });
    scr.querySelector("#addMod").addEventListener("click", addCustomMod);
    const start = scr.querySelector("#startGame");
    if (start) start.addEventListener("click", startFromLobby);

    renderPlayers(); renderDifficulty(); renderMods();
    scheduleJoins();

    // Привязка чата
    const chatInput = scr.querySelector("#lobbyChatInput");
    const chatSend = scr.querySelector("#lobbyChatSend");
    const sendChatMsg = () => {
      const text = (chatInput.value || "").trim();
      if (!text) return;
      lobbyChatAdd(playerName || "Вы", text, "self");
      chatInput.value = "";
      chatInput.focus();
      // Триггер «Диалог → Интро»: просьба об истории мира в лобби-чате
      // обрывает обычную переписку и запускает визуальное интро.
      if (wantsWorldStory(text) && !nekoIntroPlaying) {
        showWhisper(pick(R(BRAIN.story.s, BRAIN.story.h)));
        launchWorldIntro({ name: playerName || neko.knownName });
      }
    };
    chatSend.addEventListener("click", sendChatMsg);
    chatInput.addEventListener("keydown", (e) => { if (e.key === "Enter") { e.preventDefault(); sendChatMsg(); } });
  }

  function lobbyHTML(mode, code) {
    return `
    <div class="lobby-wrap">
      <header class="lobby-head">
        <button class="btn" data-back>← В меню</button>
        <h1>Лобби</h1>
        <div class="lobby-code">${mode === "host" ? "Код:" : "Лобби:"} <b>${code}</b>
          <button class="btn small" data-copy>копировать</button></div>
      </header>
      <div class="lobby-body">
        <div class="lobby-left">
          <section class="lobby-sec">
            <h2>Игроки <span class="muted" id="pcount"></span></h2>
            <div class="players-grid" id="players"></div>
            <p class="neko-whisper" id="nekoWhisper" hidden></p>
          </section>
          <section class="lobby-sec">
            <h2>Чат</h2>
            <div class="lobby-chat-wrap">
              <div class="lobby-chat-log" id="lobbyChatLog" aria-live="polite"></div>
              <div class="lobby-chat-input-row">
                <input class="lobby-chat-input" id="lobbyChatInput" type="text" maxlength="120"
                       placeholder="Написать в чат…" autocomplete="off" spellcheck="false" />
                <button class="lobby-chat-send" id="lobbyChatSend" aria-label="Отправить">▸</button>
              </div>
            </div>
          </section>
          <section class="lobby-sec">
            <h2>Сложность</h2>
            <div class="diff-row" id="diffRow"></div>
            ${mode === "guest" ? '<p class="muted">Сложность задаёт хост.</p>' : ""}
          </section>
          <section class="lobby-sec">
            <h2>Моды</h2>
            <div class="mods-list" id="modsList"></div>
            <button class="btn small" id="addMod">+ Добавить свой мод</button>
          </section>
          <div class="lobby-actions">
            ${mode === "host"
              ? '<button class="btn primary big" id="startGame">Начать игру</button>'
              : '<div class="hint big">Ждём, пока хост начнёт игру…</div>'}
          </div>
        </div>
        <aside class="lobby-right">
          <h2>Локации</h2>
          <p class="muted">5 островов — следующий открывается после победы над боссами текущего.</p>
          <div class="loc-grid">
            ${LOCATIONS.map((l) => `
              <article class="loc-card">
                <img src="${l.img}" alt="${l.name}" loading="lazy">
                <div class="loc-meta">
                  <h3>${l.name}</h3>
                  <span class="loc-who">${l.who}</span>
                  <p>${l.desc}</p>
                </div>
              </article>`).join("")}
          </div>
        </aside>
      </div>
    </div>`;
  }

  // Добавляет сообщение в чат лобби.
  // kind: "self" | "other" | "neko"
  function lobbyChatAdd(name, text, kind) {
    const log = document.getElementById("lobbyChatLog"); if (!log) return;
    const msg = document.createElement("div");
    msg.className = "lc-msg lc-" + kind;
    const nameSpan = document.createElement("span");
    nameSpan.className = "lc-name";
    nameSpan.textContent = (kind === "neko" ? "Некий" : name) + ":";
    msg.appendChild(nameSpan);
    msg.appendChild(document.createTextNode(" " + text));
    log.appendChild(msg);
    log.scrollTop = log.scrollHeight;
  }

  function renderPlayers() {
    const grid = document.getElementById("players"); if (!grid) return;
    let html = "";
    for (let i = 0; i < MAX_PLAYERS; i++) {
      const p = lobby.players[i];
      if (p) {
        const initial = (p.name || "?").trim().charAt(0).toUpperCase() || "?";
        html += `<div class="player-slot ${p.host ? "host" : ""}">
          <span class="avatar">${initial}</span>
          <span class="pname">${p.name}</span>
          ${p.host ? '<span class="host-badge">хост</span>' : '<span class="ready">готов</span>'}
        </div>`;
      } else {
        html += '<div class="player-slot empty"><span class="avatar">+</span><span class="pname">Свободно</span></div>';
      }
    }
    grid.innerHTML = html;
    const pc = document.getElementById("pcount");
    if (pc) pc.textContent = `${lobby.players.length}/${MAX_PLAYERS}`;
  }

  function showWhisper(text) {
    // Показываем в старом элементе (совместимость) и в новом чат-логе
    const el = document.getElementById("nekoWhisper");
    if (el) { el.textContent = text; el.hidden = false; el.classList.remove("show"); void el.offsetWidth; el.classList.add("show"); }
    lobbyChatAdd("Некий", text, "neko");
  }
  // Фразы болтовни игроков при входе и ожидании
  const PLAYER_GREETS = [
    "Всем привет, готов к приключениям!",
    "Наконец-то нашёл лобби, жду старта.",
    "Привет всем! Долго добирался.",
    "О, уже есть народ. Хорошо.",
    "Здарова! Давно вас ждёте?",
  ];
  const PLAYER_IDLES = [
    ["Кто-нибудь знает, что нас там ждёт?", "Первый раз играю вместе — интересно."],
    ["Надеюсь, хост не слишком долго тянет.", "Лишь бы интернет не лёг в самый момент."],
    ["Говорят, на сложных уровнях боссы жуть как злые.", "Ну что, готовы умирать вместе?"],
    ["Мне нравится этот интерфейс. Атмосферно.", "Тут ещё какой-то «Некий» есть, слышали?"],
    ["Лишь бы хватило ресурсов на всех.", "Кто будет танковать, кстати?"],
  ];

  function scheduleJoins() {
    const pool = ["Странник", "Кузнец", "Следопыт", "Жрица", "Вард"];
    const me = neko.knownName || "Ты";
    const step = () => {
      if (!lobby || !lobbyEl().classList.contains("is-active")) return;
      if (lobby.players.length >= MAX_PLAYERS) return;
      const idx = lobby.players.length - 1;
      const joined = { name: pool[idx] || "Игрок", host: false };
      lobby.players.push(joined);
      renderPlayers();
      // Вступительная реплика нового игрока в чат
      lobbyChatAdd(joined.name, PLAYER_GREETS[idx % PLAYER_GREETS.length], "other");

      // «Некий» реагирует: общая реплика на первого друга, затем личные шёпоты
      if (lobby.players.length === 2) showWhisper(pick(MP_LOBBY));
      else showWhisper(pick(MP_WHISPERS).replace("{a}", me).replace("{b}", joined.name));

      // Задерживаем ещё одну реплику болтовни от присоединившегося игрока
      const idleLines = PLAYER_IDLES[idx % PLAYER_IDLES.length];
      const idleLine = idleLines[Math.floor(Math.random() * idleLines.length)];
      setTimeout(() => {
        if (lobby && lobbyEl().classList.contains("is-active")) {
          lobbyChatAdd(joined.name, idleLine, "other");
        }
      }, 2500 + Math.random() * 2000);

      lobby.joinTimer = setTimeout(step, 1800 + Math.random() * 2400);
    };
    lobby.joinTimer = setTimeout(step, 1800);
  }

  function renderDifficulty() {
    const row = document.getElementById("diffRow"); if (!row) return;
    const guest = lobby.mode === "guest";
    row.innerHTML = DIFFICULTIES.map(([id, name, sub]) =>
      `<button class="diff-btn ${settings.difficulty === id ? "active" : ""}" data-diff="${id}" title="${sub}" ${guest ? "disabled" : ""}>${name}</button>`).join("");
    if (guest) return;
    row.querySelectorAll("[data-diff]").forEach((b) => b.addEventListener("click", () => {
      settings.difficulty = b.dataset.diff; saveSettings(settings); renderDifficulty();
    }));
  }

  function renderMods() {
    const list = document.getElementById("modsList"); if (!list) return;
    list.innerHTML = modsState.map((m, i) => `
      <div class="mod-row ${m.friend ? "friend" : ""}">
        <div class="mod-info">
          <span class="mod-name">${m.name}${m.custom ? ' <span class="mod-tag">свой</span>' : ""}${m.friend ? ' <span class="mod-tag friend-tag">у друга</span>' : ""}</span>
          <span class="mod-sub">${m.sub}</span>
        </div>
        <span class="toggle"><input type="checkbox" data-mod="${i}" ${m.on ? "checked" : ""}><span class="track"></span></span>
      </div>`).join("");
    list.querySelectorAll("[data-mod]").forEach((cb) =>
      cb.addEventListener("change", () => { modsState[+cb.dataset.mod].on = cb.checked; }));
  }

  function addCustomMod() {
    const list = document.getElementById("modsList");
    const row = document.createElement("div");
    row.className = "mod-row adding";
    row.innerHTML = `<input class="mod-input" type="text" maxlength="40" placeholder="Название своего мода…">
      <button class="btn small" data-add>Добавить</button>`;
    list.appendChild(row);
    const input = row.querySelector(".mod-input"); input.focus();
    const add = () => {
      const name = input.value.trim();
      if (!name) { row.remove(); return; }
      modsState.push({ name, sub: "Свой мод", on: true, custom: true });
      renderMods();
    };
    row.querySelector("[data-add]").addEventListener("click", add);
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") add();
      if (e.key === "Escape") row.remove();
    });
  }

  function closeLobby(opts = {}) {
    if (lobby && lobby.joinTimer) clearTimeout(lobby.joinTimer);
    // Уход «назад» из лобби запоминаем — «Некий» это прокомментирует,
    // если игрок затем передумает и начнёт одиночную игру.
    if (!opts.starting && lobby) leftLobbySession = true;
    lobby = null;
    const scr = lobbyEl();
    scr.classList.remove("is-active");
    scr.innerHTML = "";
    document.getElementById("menu-screen").classList.add("is-active");
  }

  function startFromLobby() {
    closeLobby({ starting: true });
    // имя уже названо при создании лобби, сложность выбрана в лобби →
    // теперь «Некий» рассказывает отложенную историю мира, затем выбор героя
    runNekoIntro((name) => { playerName = name; chooseHero(); }, { storyOnly: true });
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
      <div class="field"><label>Контент (цензура «Некого»)</label>
        <select id="rating">
          <option value="16"${s.rating === "16" ? " selected" : ""}>16+ — сдержанно, журит за мат</option>
          <option value="18"${s.rating === "18" ? " selected" : ""}>18+ — резче и свободнее</option>
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
        rating: $("#rating", wrap).value,
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
    const dark = neko.dark || 0;
    // правка задним числом «включается» при повторном заходе / росте тьмы (макс. эффект — заметь сам)
    const edited = (neko.visits || 0) >= 2 || dark >= 2;
    const entries = [
      { text: "Я нашёл деревянный мост. Он скрипит." + (edited ? " Некий был там." : ""),
        note: edited ? "— Я не менял это. Клянусь. — Н." : null },
      { text: dark >= 7 ? "Мне страшно. Это правильно." : "Мне страшно. Но я продолжу.",
        note: pick(DIARY_ANNOT) },
      { text: "Вода кончается. Надо искать источник.", note: null },
    ];
    const html = entries.map((e) => `
      <div class="diary-entry">
        <p class="diary-text">«${e.text}»</p>
        ${e.note ? `<p class="diary-note">${e.note}</p>` : ""}
      </div>`).join("");
    openModal("Дневник", `
      <p class="hint">Записи появляются по ходу игры. «Некий» оставляет здесь свои строки —
      и, бывает, правит уже написанное. Перечитывай: иногда твои слова уже не твои.</p>
      ${html}
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
      sessionLaunched = true;
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

  // ---------- «Некий»: присутствие в меню + запись ухода ----------
  nekoMenuPresence();
  window.addEventListener("beforeunload", () => {
    neko.lastSeen = Date.now();
    neko.lastExit = sessionLaunched ? "played" : "peek";
    nekoSave();
  });
})();
