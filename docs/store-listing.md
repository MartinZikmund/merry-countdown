# Store listing

The text the stores show, in English and Czech. Partner Center is the main source of truth; keep this page in step
when you change it there. The Google Play and App Store fields take the same text, trimmed to their limits.

## App

| Field | English | Czech |
| --- | --- | --- |
| Name | Merry Countdown | Merry Countdown |
| Subtitle (Apple, 30) | Advent Calendar & Wish List | Adventní kalendář a přání |
| Category | Kids & family (Windows: Kids & family; Play: Parenting, Designed for Families) | |
| Privacy policy | <https://mzikmund.dev/Apps/MerryCountdown/PrivacyPolicy> | same |
| Support contact | appstore@zikmund.dev | same |

### Description (English)

How many sleeps until Christmas? Merry Countdown answers it in a big, cosy animated scene that kids can read before
they can read.

Open a door every day of advent and find a joke with a hidden punchline, a festive fact, a little mission, a kind deed
or a Christmas video. The doors shuffle themselves every year and wait patiently if you miss a day.

Every kid gets their own wish list. Write a letter to Santa or Ježíšek, or pick pictures if you can't write yet, sign
it and watch it fly away.

Leave Family Display running on a TV or tablet all advent: the scene, the number of sleeps, the clock and a card for
every kid, with the screen kept awake. On Windows, a small always-on-top window keeps the countdown in the corner of
your screen.

Made for little ones:
• No ads, no accounts, no tracking. Everything stays on your device.
• Purchases, videos and sharing sit behind a grown-up check.
• Pictures and big buttons for kids who can't read yet.
• English and Czech, with Christmas Eve or Christmas Day as the big day.

Merry Countdown Pro unlocks three more scenes (northern lights, fireplace and toy train), your own family photos as a
slideshow, personal messages behind doors, family events and an exact arrival time. One purchase, no subscription.

### Popis (Czech)

Kolikrát se ještě vyspíme do Vánoc? Merry Countdown to ukáže ve velké, útulné animované scéně, které rozumí i děti,
které ještě neumí číst.

Každý den adventu otevřete jedna dvířka a najdete vtip se schovanou pointou, vánoční zajímavost, malý úkol, dobrý
skutek nebo vánoční video. Dvířka se každý rok zamíchají a když nějaký den vynecháte, počkají na vás.

Každé dítě má svůj vlastní seznam přání. Napište dopis Ježíškovi nebo Santovi, nebo vyberte obrázky, pokud ještě
neumíte psát, podepište ho a podívejte se, jak odletí.

Rodinný displej nechte běžet na televizi nebo tabletu celý advent: scéna, počet spaní, hodiny a kartička pro každé
dítě, a obrazovka nezhasne. Na Windows drží odpočet v rohu obrazovky malé okno, které je vždy navrchu.

Pro nejmenší:
• Žádné reklamy, účty ani sledování. Vše zůstává ve vašem zařízení.
• Nákupy, videa a sdílení jsou za kontrolou pro dospělé.
• Obrázky a velká tlačítka pro děti, které ještě neumí číst.
• Čeština i angličtina, velký den na Štědrý večer nebo na Boží hod.

Merry Countdown Pro odemkne tři další scény (polární záři, krb a vláček), vaše rodinné fotky jako prezentaci, vlastní
vzkazy za dvířky, rodinné události a přesný čas příchodu. Jednorázový nákup, žádné předplatné.

### Features (Windows, up to 20 × 200)

| English | Czech |
| --- | --- |
| A big animated countdown kids understand without reading | Velký animovaný odpočet, kterému děti rozumí i bez čtení |
| 24 advent doors with jokes, facts, missions, kind deeds and videos | 24 adventních dvířek s vtipy, zajímavostmi, úkoly, dobrými skutky a videi |
| A wish list for every kid, with pictures for little ones | Seznam přání pro každé dítě, s obrázky pro nejmenší |
| Family Display for a TV or tablet all advent long | Rodinný displej pro televizi nebo tablet po celý advent |
| Always-on-top mini countdown on Windows | Malý odpočet vždy navrchu na Windows |
| Santa or Ježíšek, Christmas Eve or Christmas Day | Ježíšek nebo Santa, Štědrý večer nebo Boží hod |
| No ads, no accounts, no tracking | Žádné reklamy, účty ani sledování |

### Search terms (Windows, up to 7)

English: advent calendar, christmas countdown, christmas, advent, wish list, santa, kids

Czech: adventní kalendář, vánoce, advent, odpočet, ježíšek, přání, děti

## Add-on: Merry Countdown Pro

| Field | English | Czech |
| --- | --- | --- |
| Product ID | `dev.mzikmund.merrycountdown.pro` | |
| Type | Durable, lifetime Forever (Play and Apple: non-consumable) | |
| Price | US$2.99, converted per market; launch sale US$1.79 (−40%) until 2 December 2026, 00:00 UTC | |
| Title | Merry Countdown Pro | Merry Countdown Pro |
| Description (200) | Three more scenes, your family photos as a slideshow, personal messages behind doors, family events and an exact arrival time. One purchase, no subscription. | Tři další scény, rodinné fotky jako prezentace, vlastní vzkazy za dvířky, rodinné události a přesný čas příchodu. Jednorázový nákup, žádné předplatné. |
| Age rating | Doesn't require more restrictive ratings than its parent product | |

## Google Play

| Field | English | Czech |
| --- | --- | --- |
| App name (30) | Merry Countdown | Merry Countdown |
| Short description (80) | Count the sleeps to Christmas: advent calendar doors and a wish list for kids | Počítejte spaní do Vánoc: adventní kalendář s dvířky a seznam přání pro děti |
| Full description (4000) | The description above, without the sentence about the always-on-top window on Windows | Popis výše, bez věty o okně vždy navrchu na Windows |
| Category | Parenting; target audience under 13, so the Families policy applies | |
| Tags | Christmas, Advent calendar, Kids, Family | |

The in-app product uses the same id, title, description and price as the Microsoft Store add-on above. Play splits it into
the product, a purchase option `lifetime` (Buy, backwards compatible, which RevenueCat needs) and the `launch-sale` discount offer.

### Graphics

Made from the same templates as the Microsoft Store set (see below), in the dark theme to match.

| Asset | Size | Files |
| --- | --- | --- |
| App icon | 512 × 512 | `icon-512.png` |
| Feature graphic | 1024 × 500 | `feature-en.png`, `feature-cs.png` |
| Phone screenshots | 6 × 2160 × 3840 (9:16) | `phone-<lang>-1..6.png` |
| 7" and 10" tablet screenshots | 7 × 3840 × 2160 (16:9) | `tablet-<lang>-1..7.png` (the Windows mini-window frame is left out) |

The sources are git-ignored, in `.screenshots/store/`: `compose-phone.html` frames the phone captures from
`raw-phone/` (Pixel 9 Pro emulator), `compose.html?dir=raw-tablet` the tablet captures from `raw-tablet/` (Pixel Tablet
emulator), `art.html?kind=feature` draws the feature graphic and `tile.html?s=512` the icon. Output goes to `out-play/`.
The captures come from a Debug build with `-p:AppChannel=Prod -p:UnoDisableHotDesign=true` (no DEV badge, no Hot Design
toolbar), seeded through `run-as` with `family.json`, `devlang.txt`, `devdate.txt` (2026-12-12) and
`simulate-store.txt` (`owned`), and Android's status bar demo mode.
