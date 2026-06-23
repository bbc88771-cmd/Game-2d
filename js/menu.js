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

  // ---------- «Некий» (NPC): память между перезаходами ----------
  const NEKO_KEY = "sotw_neko";
  const defaultNeko = {
    knownName: null, metAt: 0, lastSeen: 0, visits: 0,
    launchedGame: false, storyTold: false, lastExit: null, history: [],
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

  // Реплика игрока — зелёная и чуть подсвеченная, в отличие от красного «Некого».
  function playerEcho(text) {
    const el = $("#neko-player"); if (!el) return;
    const t = (text || "").trim();
    el.textContent = t ? `«${t}»` : "…";
    el.classList.add("show");
  }
  function clearPlayerEcho() {
    const el = $("#neko-player"); if (!el) return;
    el.textContent = ""; el.classList.remove("show");
  }

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

  function askLine(placeholder = "напиши ответ…") {
    return new Promise((resolve) => {
      const row = $("#neko-input-row"), input = $("#neko-input");
      clearPlayerEcho();
      input.value = ""; input.placeholder = placeholder; row.hidden = false; input.focus();
      const submit = (e) => {
        e.preventDefault();
        const v = input.value;
        row.hidden = true; row.removeEventListener("submit", submit);
        playerEcho(v);
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
        playerEcho(val ? "Да" : "Нет");
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
    intro.hidden = false;
    const onSkip = () => { skipNeko = true; };
    skip.addEventListener("click", onSkip);
    const finish = () => {
      skip.removeEventListener("click", onSkip);
      clearPlayerEcho();
      $("#neko-line").textContent = "";
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

    // Рассказать историю мира. Если уже рассказывал — спросить, повторить ли.
    let tell = !neko.storyTold;
    if (neko.storyTold) {
      await nekoType("Историю этого мира я тебе уже рассказывал.", 800);
      await nekoType("Рассказать ещё раз?", 450);
      tell = await askConfirm();
      await nekoType(tell ? "Хорошо. Слушай снова." : "Как знаешь. Идём дальше.", 650);
    }

    if (tell) {
      await worldStory(neko.knownName);         // рассказ истории мира (с диалогом)
      neko.storyTold = true; nekoSave();
      finish();
      await cutscene();                         // плавно перетекает в катсцену
    } else {
      finish();
    }
    neko.launchedGame = true; sessionLaunched = true; nekoSave();
    onDone(neko.knownName);
  }

  // Возвращение: «Некий» помнит имя, паузу отсутствия и прошлое поведение
  async function nekoGreetReturning(opts = {}) {
    const name = neko.knownName;
    await nekoType(`Снова ты, ${name}.`, 700);
    if (prevExit === "peek") {
      await nekoType("Я помню: в прошлый раз ты лишь заглянул и закрыл, не начав. Думал, не замечу?", 850);
    } else if (timeAway > 24 * 3600e3) {
      const days = Math.floor(timeAway / 86400e3);
      await nekoType(`Тебя не было ${days} ${plural(days, "день", "дня", "дней")}. Я считал каждый.`, 800);
    } else if (timeAway > 3600e3) {
      await nekoType("Ты уходил. Но вернулся. Они всегда возвращаются.", 800);
    }
    await nekoType("Рад снова тебя видеть. Правда рад.", 700);
    await nekoType("Хочешь что-нибудь сказать, прежде чем продолжим?", 500);
    const ans = await askLine("…");
    nekoRemember("player", ans);
    await nekoType(nekoSwear(ans) || nekoReply(ans), 750);
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

  // История мира → один интерактивный момент → переход к катсцене
  async function worldStory(name) {
    const lines = [
      `Тогда слушай, ${name}.`,
      "Это был обычный мир. Средневековье. Камень, железо, молитвы.",
      "Пока глубоко под землёй не проснулось… Нечто. Оно копило силы тысячи лет.",
      "Пошли трещины. Из них вышли души — и вошли в острова, в зверей, в людей.",
      "Мутация. Безумие. Землю разорвало на куски, и они повисли в пустоте.",
      "Вода ушла почти отовсюду. Её теперь добывают по капле.",
    ];
    for (const l of lines) { await nekoType(l, 850); nekoRemember("neko", l); }
    await nekoType("Ты помнишь, что было до катаклизма?", 500);
    const ans = await askLine();
    nekoRemember("player", ans);
    const sw = nekoSwear(ans);
    if (sw) await nekoType(sw, 700);
    await nekoType(nekoReactMemory(ans), 800);
    await nekoType("Память — единственное, что я могу… поправить.", 900);
    await nekoType(`Идём, ${name}. Я покажу, что осталось.`, 800);
  }

  function nekoReactMemory(ans) {
    const s = (ans || "").toLowerCase();
    if (/(да|помн|конечно|ага)/.test(s)) return "Лжёшь. Никто не помнит. Это часть условия.";
    if (/(нет|не\b|никак)/.test(s)) return "Хорошо. Чистый лист удобнее. И тебе, и мне.";
    if (!s.trim()) return "Молчишь. Молчание я тоже запоминаю.";
    return "…Любопытный ответ. Я его запомню.";
  }

  // Простой ответчик «Некого» на свободные сообщения игрока
  function nekoReply(text) {
    const s = (text || "").toLowerCase().trim();
    if (!s) return "Молчишь. Что ж, идём.";
    if (/(кто ты|ты кто|что ты такое)/.test(s)) return "Я тот, кто говорит с тобой, когда некому больше.";
    if (/(где я|что за место|какой мир)/.test(s)) return "Ты на осколке мира, который сам себя сломал.";
    if (/(помоги|помощь|как мне)/.test(s)) return "Помогу. Или нет. Узнаешь по дороге.";
    if (/(спасиб|благодар)/.test(s)) return "Не благодари заранее.";
    if (/(ненавиж|тупо|дурак|идиот|заткнись)/.test(s)) return "Злись. Злость честнее улыбки.";
    if (/(да|готов|идём|идем|начн|поехали|продолж)/.test(s)) return "Тогда идём.";
    return "Я услышал тебя. И запомнил.";
  }

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

  // Катсцена: анимированные кадры (зум/пан + угольки/туман/вспышки) + текст + звук;
  // в конце экран затухает и открывается полноэкранный выбор персонажа.
  async function cutscene() {
    const cs = $("#cutscene"), frame = $("#cut-frame"), cap = $("#cut-caption"),
      skip = $("#cut-skip"), flash = $("#cut-flash"), embers = $("#cut-embers");
    let skipped = false;
    const onS = () => { skipped = true; };
    skip.addEventListener("click", onS);
    cs.hidden = false; cs.classList.remove("fade-out");
    spawnEmbers(embers);
    const audio = startAmbient();
    const frames = [
      ["assets/img/background.jpg", "Мир, который ты знал, уже закончился.", 196, "kb"],
      ["assets/img/loc_dead.svg", "Земля треснула и высохла. Вода ушла.", 165, "kb2"],
      ["assets/img/loc_magic.svg", "Из трещин пришли души — и заняли всё живое.", 220, "kb"],
      [null, "Осталось только это.\nИ ты.", 110, "kb2"],
    ];
    for (const [img, text, freq, kb] of frames) {
      if (skipped) break;
      frame.style.opacity = "0";
      await wait(skipped ? 0 : 320);                    // плавный переход между кадрами
      frame.style.backgroundImage = img ? `url("${img}")` : "none";
      frame.classList.remove("kb", "kb2"); void frame.offsetWidth; frame.classList.add(kb); // зум/пан
      frame.style.opacity = img ? "1" : "0";
      flash.classList.remove("go"); void flash.offsetWidth; flash.classList.add("go"); // вспышка на смене кадра
      cap.textContent = text;
      cap.classList.remove("show"); void cap.offsetWidth; cap.classList.add("show");
      bell(audio, freq);
      await wait(skipped ? 140 : 2700);
    }
    cap.classList.remove("show");
    cs.classList.add("fade-out");                       // затухание всего экрана
    await wait(900);
    stopAmbient(audio);
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

  function scheduleJoins() {
    const pool = ["Странник", "Кузнец", "Следопыт", "Жрица", "Вард"];
    const step = () => {
      if (!lobby || !lobbyEl().classList.contains("is-active")) return;
      if (lobby.players.length >= MAX_PLAYERS) return;
      lobby.players.push({ name: pool[lobby.players.length - 1] || "Игрок", host: false });
      renderPlayers();
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
