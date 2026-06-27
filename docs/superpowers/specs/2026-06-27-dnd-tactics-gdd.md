# Game Design Document — D&D Tactics

**Versi:** 1.0
**Tanggal:** 27 Juni 2026
**Engine:** Unity (C#)
**Status:** Dalam Pengembangan

---

## 1. Nama Aplikasi Permainan

**D&D Tactics** — *Turn-Based Tactical RPG*

---

## 2. Latar Belakang dan Motivasi

D&D Tactics lahir dari keinginan untuk menghadirkan esensi pertarungan *Dungeons & Dragons 5th Edition* ke dalam format *turn-based tactical RPG* berbasis grid. D&D sebagai *tabletop RPG* memiliki sistem pertarungan yang kaya — *initiative order*, *action economy*, *ability scores*, *armor class*, *dice rolls* (d20), dan *spell system* — namun format *tabletop* membutuhkan *Dungeon Master* dan pemain untuk mengelola banyak aturan secara manual.

Motivasi pengembangan:

1. **Adaptasi mekanik D&D 5e** ke format digital yang bisa dimainkan *single-player* tanpa DM
2. **Memperkenalkan taktik grid-based** dengan *movement range*, *attack range*, dan *line-of-sight* (implisit via BFS)
3. **Progression system** berupa *level scaling* — musuh makin kuat dan makin banyak tiap level
4. **Player class diversity** — 4 kelas (Fighter, Rogue, Wizard, Cleric) dengan peran dan *spell* yang berbeda
5. **Enemy AI** yang cerdas dengan prioritisasi target, *retreat behavior*, dan *defensive strategy*

---

## 3. Deskripsi Aplikasi Permainan

### a. Deskripsi Game

D&D Tactics adalah permainan *turn-based tactical RPG* di mana pemain mengendalikan 4 *hero* (Fighter, Rogue, Wizard, Cleric) melawan gelombang musuh yang semakin kuat di atas grid 16x9. Setiap *unit* memiliki *stats* ala D&D 5e (STR, DEX, CON, INT, WIS, CHA), *armor class* (AC), *hit points* (HP), *attack range*, *movement range*, dan *spell* unik per kelas.

Pertarungan menggunakan sistem *initiative* (roll d20 + Dexterity modifier) untuk menentukan urutan giliran, *action economy* (1 move + 1 action per turn), dan *dice roll* (d20) untuk menentukan *hit/miss/crit*.

### b. Storytelling / Cerita

Dunia dalam kegelapan. Pasukan *Goblin* dan *Skeleton* muncul dari bawah tanah, mengancam kerajaan. Sebuah kelompok petualang — *Fighter* pemberani, *Rogue* licik, *Wizard* agung, dan *Cleric* suci — bersatu untuk menghadapi ancaman ini.

Setiap level mewakili *depth* yang lebih dalam dari dungeon yang mereka jelajahi. Goblin (level awal) memberi jalan ke Skeleton (level menengah), dan tantangan yang lebih besar menanti di level-level puncak. Para pahlawan tidak hanya bertahan — mereka semakin kuat dengan setiap kemenangan, menghadapi musuh yang lebih cerdas dan lebih banyak jumlahnya.

*Catatan: Cerita saat ini bersifat minimalis dan fungsional — fokus pengembangan ada pada mekanik gameplay.*

### c. Genre

**Strategy RPG (Turn-Based Tactics)** dengan elemen **RPG (Role-Playing Game)**.

Kombinasi:
- **Strategy** — pemain harus memposisikan unit, memilih target, dan mengelola *action economy*
- **RPG** — *character stats*, *class system*, *spells*, *progression* (level scaling)
- **Tactical** — grid-based positioning, range calculation, *attack of opportunity* logic

### d. Competition Modes

**Single Player (Player vs Environment — PvE)**

Tidak ada *multiplayer* — pemain melawan AI yang mengontrol musuh.

### e. Level Permainan

Terdapat **10 level** dengan *scaling difficulty*:

| Level | Jumlah Musuh | HP Multiplier | Damage Bonus | AC Bonus | Catatan |
|-------|-------------|---------------|-------------|----------|---------|
| 1 | 3 | 1.0× | +0 | +0 | Goblin, pengenalan |
| 2 | 4 | 1.25× | +1 | +0 | Goblin + Skeleton |
| 3 | 4 | 1.5× | +2 | +1 | Skeleton mulai muncul |
| 4 | 5 | 1.75× | +3 | +1 | Campuran |
| 5 | 5 | 2.0× | +4 | +2 | Skeleton dominan |
| 6 | 6 | 2.25× | +5 | +2 | |
| 7 | 6 | 2.5× | +6 | +3 | |
| 8 | 7 | 2.75× | +7 | +3 | |
| 9 | 7 | 3.0× | +8 | +4 | |
| 10 | 8 | 3.25× | +9 | +4 | Puncak |

Pemain bisa **memilih level** (level select) setelah membukanya — tidak harus linear.

### f. Target Audience

| Dimensi | Target |
|---------|--------|
| **Usia** | 12+ (remaja hingga dewasa) |
| **Penggemar** | Turn-based strategy, D&D / tabletop RPG, tactical RPG seperti Fire Emblem / Final Fantasy Tactics / Into the Breach |
| **Platform** | PC (Windows, Mac, Linux via Unity) |
| **Kesulitan** | Medium — mudah dipelajari, menantang untuk dikuasai |
| **Sesi bermain** | 5–15 menit per battle |

### g. Platform Game

**PC Game** — dirancang untuk keyboard + mouse.

Resolusi referensi: 1920×1080 (dengan *Canvas Scaler* untuk skala dinamis).

### h. Game Bisnis Konsep

Konsep bisnis yang dapat dikembangkan:

1. **Level Pack DLC** — level tambahan dengan musuh dan tile set baru (misal: *Undead Expansion*, *Dragon's Lair*)
2. **Class Pack** — kelas tambahan (Paladin, Ranger, Bard, Druid) sebagai konten berbayar
3. **Cosmetic Skins** — variasi visual untuk unit (team colors, sprite style)
4. **Custom Campaign Builder** — memungkinkan pemain membuat dan membagikan level mereka sendiri
5. **Free-to-play + Progression Boost** — model *freemium* dengan *cosmetic microtransactions*

---

## 4. Algoritma yang Akan Digunakan

### Algoritma Utama dalam Game:

1. **BFS (Breadth-First Search)** — `GridManager.GetTilesInRange()` dan `GetDistance()` — untuk menghitung *movement range*, *attack range*, dan *spell range* pada grid. BFS dipilih karena grid bersifat *unweighted* (setiap tile cost 1).

2. **Pathfinding via BFS** — untuk menentukan jarak terpendek antara 2 tile (digunakan AI untuk memilih target terdekat)

3. **Dice Roll (RNG)** — `DiceRoller.RollDamage()` — sistem *random number generation* untuk d20, damage dice (d4/d6/d8/d10), dan *saving throws*

4. **Damage Calculation Algorithm** — `DamageCalculator.CalculateDamage()`:
   - Roll d20 + AttackBonus vs Target AC
   - Natural 20 → *Critical Hit* (double damage dice)
   - Natural 1 → *Automatic Miss*
   - Defender status *Defending* → +4 AC

5. **Enemy AI Prioritization** — `TurnManager.RunEnemyAI()`:
   - Hitung skor tiap target berdasarkan: HP rendah (skor tinggi), class squishy (Wizard > Cleric > Rogue > Fighter), jarak (pinalti)
   - Ambil target dengan skor tertinggi
   - Jika HP < 25% dan dalam jangkauan serang → *retreat + defend*
   - Jika dalam *attack range* → serang
   - Jika tidak → *move toward target* + *strategic defend*

6. **Level Scaling Formula** — `LevelManager`:
   - `EnemyCount = min(3 + (level - 1) × 1, 8)`
   - `HpMult = 1 + (level - 1) × 0.25`
   - `DmgBonus = (level - 1) × 1`
   - `AcBonus = floor((level - 1) / 2)`

7. **Spell Targeting** — `SpellSystem`:
   - AoE (Fireball): hit all units dalam radius tertentu, Dex save untuk half damage
   - *Auto-hit* (Magic Missile): 3 projectiles, tidak perlu roll to hit
   - *Heal* (Heal Wounds): restore HP ally
   - *Buff* (Bless/Shield): temporary stat boost

8. **Initiative Sorting** — `TurnManager.StartNewRound()`:
   - Roll d20 + Dexterity mod untuk semua unit
   - Sort descending, tie-break: player unit first

9. **Combat Log** — ring buffer (max 50 entries) untuk menampilkan riwayat pertarungan

---

## Bagian 2: Mekanik Spesifik

---

## 1. Mechanics and Players' Role

### Aturan dan Prosedur

**Aturan Dasar:**

1. Setiap battle dimulai dengan *level select* (atau *continue* dari level sebelumnya)
2. 4 *player units* (Fighter, Rogue, Wizard, Cleric) spawn di baris bawah grid
3. 3–8 *enemy units* (Goblin, Skeleton) spawn di baris atas grid
4. Semua unit *roll initiative* — urutan giliran ditentukan oleh d20 + Dex modifier
5. Dalam satu giliran, unit bisa melakukan:
   - **Move** — pindah ke tile dalam *movement range*
   - **Action** — Attack / Spell / Defend / Wait
6. Jika unit sudah *move* dan *act*, gilirannya berakhir otomatis

**Player Role:**

Pemain mengendalikan **semua 4 hero** dalam satu tim. Pemain:

- Memilih unit aktif (otomatis bergiliran sesuai *initiative*)
- Memutuskan aksi tiap unit — apakah move, attack, cast spell, defend, atau wait
- Memilih target serang dengan mengklik tile musuh
- Memilih tile tujuan untuk movement
- Menentukan strategi — posisi, prioritas target, penggunaan spell

### Prosedur Satu Giliran Player:

1. Tunggu giliran unit player aktif
2. Klik tile kosong untuk *move* (jika belum move)
3. Klik musuh untuk *attack* (jika belum act)
4. Atau klik tombol *Defend* / *Wait* untuk mengakhiri aksi
5. Atau klik tombol *Spell* lalu target tile
6. Setelah move + act, giliran berakhir otomatis

### Prosedur Giliran Enemy (AI):

1. AI menghitung target prioritas
2. Jika dalam range → attack
3. Jika tidak → move mendekat
4. Jika HP rendah → retreat + defend
5. Jika tidak bisa attack → strategic defend

---

## 2. GameFlow / Game Layout Chart

```
┌─────────────────────────────────────────────────────────────────────┐
│                         GAME FLOW CHART                              │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│   [Game Start]                                                       │
│       │                                                              │
│       ▼                                                              │
│   [Level Select Overlay] ─── Pilih level (Lv1 s.d. Lv unlocked)      │
│       │                                                              │
│       ▼                                                              │
│   [Battle Start]                                                     │
│       │                                                              │
│       ▼                                                              │
│   [Initiative Roll] ← semua unit roll d20 + Dex mod                  │
│       │                                                              │
│       ▼                                                              │
│   ┌─────────────────────────────────────────────────────┐            │
│   │          BATTLE LOOP (per giliran)                   │            │
│   │                                                     │            │
│   │  ┌───── Player Turn? ─────┐                        │            │
│   │  │         │               │                        │            │
│   │  │       Yes               No                       │            │
│   │  │         │               │                        │            │
│   │  │         ▼               ▼                        │            │
│   │  │  [Player Input]   [Enemy AI]                     │            │
│   │  │  • Move tile      • Hitung target                │            │
│   │  │  • Attack target  • Move / Attack                │            │
│   │  │  • Cast spell     • Defend / Wait                │            │
│   │  │  • Defend / Wait                                 │            │
│   │  │         │               │                        │            │
│   │  │         └───────┬───────┘                        │            │
│   │  │                 │                                │            │
│   │  │                 ▼                                │            │
│   │  │      [Dice Roll Animation]                       │            │
│   │  │      [Damage Application]                        │            │
│   │  │      [Camera Shake / Popup]                      │            │
│   │  │                 │                                │            │
│   │  │                 ▼                                │            │
│   │  │      [Check Victory/Defeat]                      │            │
│   │  │         │              │                         │            │
│   │  │      Battle Over    Next Turn                    │            │
│   │  └──────────────────────────────────────────────────┘            │
│   │                                                                  │
│   ▼                       ▼                                          │
│                    [Next Round]                                       │
│                                                                      │
│   ┌───── BATTLE OVER ─────┐                                          │
│   │                       │                                          │
│   ▼                       ▼                                          │
│ [Victory Screen]     [Defeat Screen]                                  │
│   • "Next Level →"    • "Restart" → Reset ke Lv1                     │
│   • Level Select                                                    │
│   • Restart Lv1                                                      │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 3. CoreLoop

```
┌─────────────────────────────────────────────────────────────┐
│                   CORE LOOP                                  │
│                                                             │
│   Select Level → Battle → Victory/Defeat → Next/Retry       │
│        ↑                              │                     │
│        └──────────────────────────────┘                     │
│                                                             │
│   Dalam Battle (micro loop):                                 │
│   ┌────────────────────────────────────────┐                │
│   │  Unit Active → Pilih Aksi →            │                │
│   │     → Roll Dice → Apply Result →       │                │
│   │     → Next Unit → (repeat)             │                │
│   └────────────────────────────────────────┘                │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Mikro Loop Detail:

1. **Active Unit selected** (dari *initiative queue*)
2. **If player-controlled:** Tunggu input (move/attack/spell/defend/wait)
3. **If enemy-controlled:** AI memutuskan aksi
4. **Execute action:**
   - Move → lerp animasi → update posisi grid
   - Attack → dice roll d20 → hit/miss/crit → damage → popup
   - Spell → targeting → resolve effect
   - Defend → +4 AC buff
   - Wait → skip
5. **Check win/lose condition**
6. **Next unit in queue** (atau next round)

---

## 4. Objective

### Tujuan Utama (Main Objective):

**Kalahkan semua musuh di setiap level untuk maju ke level berikutnya.**

### Tujuan Per Level:

| Aspek | Detail |
|-------|--------|
| **Win Condition** | Semua enemy units mati (HP ≤ 0) |
| **Lose Condition** | Semua player units mati (HP ≤ 0) |
| **Reward Win** | Buka level berikutnya (advance level) |
| **Penalty Lose** | Reset ke Level 1 (full restart) |
| **Progression** | Naik level → musuh lebih kuat & lebih banyak |

### Tujuan Tambahan (Implisit):

- Pelajari mekanik tiap kelas
- Efisienkan penggunaan *action economy*
- Lindungi unit squishy (Wizard, Cleric)
- Gunakan spell pada momen yang tepat

---

## 5. Player Roles and Mechanics Rules

### a. Player Roles

Pemain berperan sebagai **Commander / Tactician** yang mengendalikan 4 hero:

| Role | Kontrol |
|------|---------|
| **Tactician** | Memilih posisi, target, dan aksi tiap unit |
| **Resource Manager** | Mengelola *action economy* (move + act per turn) |
| **Strategist** | Mengatur prioritas target, penggunaan spell |

Setiap unit individu memiliki peran dalam tim:

| Class | Peran | Posisi |
|-------|-------|--------|
| **Fighter** | Tank / Melee DPS | Frontline, damage dealer |
| **Rogue** | Melee DPS / Finisher | Flank, target low-HP |
| **Wizard** | AoE Caster / Burst | Backline, area damage |
| **Cleric** | Healer / Buffer / Support | Backline, support |

### b. Mechanics Rules

#### D&D Stats & Modifiers:

| Stat | Range | Modifier Formula | Digunakan Untuk |
|------|-------|-----------------|-----------------|
| STR | 8-20 | (stat-10)/2 | Attack Bonus (Fighter/Cleric) |
| DEX | 8-20 | (stat-10)/2 | Attack Bonus (Rogue), Initiative |
| CON | 8-20 | (stat-10)/2 | HP |
| INT | 8-20 | (stat-10)/2 | Attack Bonus (Wizard) |
| WIS | 8-20 | (stat-10)/2 | Heal amount (Cleric) |
| CHA | 8-20 | (stat-10)/2 | — (cadangan) |

#### Action Economy:

| Aksi | Effect | Biaya |
|------|--------|-------|
| **Move** | Pindah 1 tile dalam *movement range* | Move action |
| **Attack** | Roll d20 vs AC → damage | Action |
| **Spell** | Cast spell → effect | Action |
| **Defend** | +4 AC sampai giliran berikutnya | Action |
| **Wait** | Skip sisa action | Action |
| **End Turn** | Akhiri giliran (otomatis jika move+act done) | — |

#### Combat Formula:

```
Attack Roll: d20 + AttackBonus
Hit: AttackRoll ≥ TargetAC (atau Natural 20)
Miss: Natural 1
Critical Hit: Natural 20 → double damage dice

Damage: 
  Normal: roll weapon damage (e.g. 1d8)
  Crit: roll dice 2× + bonus once (e.g. 2d8 + 3)
  
Defend Bonus: +4 AC (aktif sampai giliran berikutnya)
```

#### Movement & Range:

```
Movement: BFS dari posisi unit dalam movementRange tiles
Attack Range: Manhattan distance ≤ attackRange
Overlap: Tidak bisa menempati tile yang sudah di-occupy
```

### c. Win Condition

**Semua enemy units mati (IsAlive == false).**

Ketika kondisi ini terpenuhi:
1. Battle phase berubah menjadi `BattlePhase.Victory`
2. Victory overlay muncul
3. LevelManager.AdvanceLevel() dipanggil
4. Pemain bisa memilih: Next Level, Level Select, atau Restart

### d. Lose Condition

**Semua player units mati (IsAlive == false).**

Ketika kondisi ini terpenuhi:
1. Battle phase berubah menjadi `BattlePhase.Defeat`
2. Defeat overlay muncul
3. LevelManager.Reset() dipanggil
4. Pemain harus restart dari Level 1

---

## 6. Competition Modes

**Single Player — Player vs Environment (PvE)**

Tidak ada mode *multiplayer* atau *competitive*. Game murni *single-player experience* dengan *AI-controlled enemies*.

| Mode | Deskripsi | Status |
|------|-----------|--------|
| **Campaign** | 10 level linear dengan progressive difficulty | ✅ Implemented |
| **Level Select** | Pilih level yang sudah terbuka | ✅ Implemented |
| **Free Play** (future) | Battle dengan parameter kustom | ❌ Planned |
| **Endless Mode** | Infinite waves dengan scaling tak terbatas | ❌ Future |

---

## 7. Genre

**Turn-Based Tactics / Strategy RPG**

Karakteristik genre:

| Elemen | Implementasi |
|--------|-------------|
| Grid-based movement | Ya — 16×9 grid, Manhattan distance |
| Turn-based combat | Ya — initiative queue system |
| Character classes | Ya — Fighter, Rogue, Wizard, Cleric |
| Character stats | Ya — 6 D&D ability scores + derived modifiers |
| Level progression | Ya — 10 level dengan scaling |
| Dice rolls | Ya — d20 system, damage dice |
| Spell system | Ya — 5 spells across 2 caster classes |
| Equipment | Belum — future feature |
| Inventory | Belum — future feature |

---

## 8. Target Audience

### a. Usia

| Rentang | Keterangan |
|---------|------------|
| **Utama: 15–35 tahun** | Pemain yang familiar dengan turn-based strategy |
| **Minimum: 12+** | Mekanik cukup kompleks untuk anak di bawah 12 |
| **Maksimum: tanpa batas** | Aksesibel untuk semua umur |

### b. Penggemar Game/Genre

**Primary:** Penggemar genre:
- **Turn-Based Strategy** — Fire Emblem, Final Fantasy Tactics, Advance Wars
- **Tabletop RPG** — Dungeons & Dragons, Pathfinder
- **Tactical RPG** — XCOM, Into the Breach, Divinity: Original Sin

**Secondary:**
- Pemain yang mencari game *pick-up-and-play* dengan sesi pendek (5–15 menit)
- Penggemar *fantasy setting* (medieval, magic, dungeon)
- Pemain yang tertarik dengan *game design* dan mekanik D&D

---

## 9. Story Board

### Scene Sequence:

```
[SCENE 1: Level Select]
  Visual: Dark overlay dengan judul "⚔ D&D Tactics"
  → Grid level buttons (Lv1 s.d. Lv10)
  → Player klik level

[SCENE 2: Battle Start]
  Visual: Grid 16×9 muncul
  → Player unit spawn di baris bawah (biru/ally)
  → Enemy unit spawn di baris atas (merah/enemy)
  → Camera zoom ke tengah grid

[SCENE 3: Player Turn]
  Visual: Tile highlight warna:
    • Hijau → move range
    • Merah → attack range
    • Ungu → spell range
  → D20 dice animation (acak → hasil akhir)
  → Damage popup angka
  → Unit flash merah saat kena damage
  → Unit fade out saat mati

[SCENE 4: Enemy Turn]
  → Enemy bergerak (lerp animation)
  → Enemy attack (dice roll sama)
  → Player unit kena damage

[SCENE 5: Victory]
  → Overlay gelap
  → Teks "✦ VICTORY! ✦"
  → Tombol: Next Level, Level Select, Restart

[SCENE 6: Defeat]
  → Overlay gelap
  → Teks "✟ DEFEAT..."
  → Tombol: Restart (reset ke Lv1)
```

### Visual Style Notes:

- **Palette:** Dark earthy tones (coklat gelap, emas, krem) — tema *parchment & stone*
- **Unit:** 16×16 pixel-style sprite (procedurally generated via TextureFactory)
- **Grid:** Chessboard offset pattern (hitam/putih samar)
- **UI:** Gold trim, dark panel backgrounds, *LegacyRuntime* font (Unity default)

---

## 10. User Interface

### Layout:

```
┌─────────────────────────────────────────────────────────────────────┐
│ [TOP BAR]  ⚔ Battle — Level X                50px (fixed)          │
├──────────────────────────────────┬──────────────────────────────────┤
│                                  │         [SIDE PANEL]            │
│                                  │  ┌─────────────────────────┐    │
│                                  │  │    Unit Name (22)       │    │
│      G R I D  1 6 × 9           │  │    Class (16, gold)      │    │
│                                  │  │    ▓▓▓▓▓▓▓▓▓░░░ HP Bar  │    │
│                                  │  │    HP: XX/XX            │    │
│                                  │  │    AC: XX               │    │
│                                  │  │    STR: 16 (+3)         │    │
│                                  │  │    DEX: 14 (+2)         │    │
│                                  │  │    CON: 15 (+2)         │    │
│                                  │  │    INT: 10 (0)          │    │
│                                  │  │    WIS: 8 (-1)          │    │
│                                  │  │    CHA: 12 (+1)         │    │
│                                  │  └─────────────────────────┘    │
│                                  │        230px                    │
├──────────────────────────────────┴──────────────────────────────────┤
│ [SPELL BAR]  [Fireball] [Magic Missile]        (muncul per kelas)   │
├─────────────────────────────────────────────────────────────────────┤
│ [BOTTOM BAR]  [Move] [Attack] [Defend] [Wait] [End Turn]   90px    │
└─────────────────────────────────────────────────────────────────────┘
```

### UI Components:

| Komponen | Posisi | Fungsi |
|----------|--------|--------|
| **Top Bar** | Top, 50px | Menampilkan status battle (level, active unit) |
| **Side Panel** | Right, 230×320 | Info detail unit yang sedang aktif |
| **Bottom Bar** | Bottom, 90px | Tombol aksi (Move/Attack/Defend/Wait/End Turn) |
| **Spell Bar** | Above bottom, 38px | Tombol spell (muncul hanya untuk Wizard/Cleric) |
| **Combat Log** | Right-bottom, 230px | Riwayat pertarungan (scrollable, max 50 entries) |
| **Damage Popup** | Floating | Angka damage muncul di posisi unit |
| **Dice Roll Display** | Center | Animasi d20 + hasil hit/miss/crit |
| **Level Select** | Full overlay | Pilih level sebelum battle |
| **Victory/Defeat Overlay** | Full overlay | Hasil battle + opsi lanjutan |

---

## 11. Dynamics

### a. General Summary of Progression

```
Level 1-3 (Easy):
  • Musuh: Goblin (HP 10, AC 12)
  • Jumlah: 3-4 musuh
  • Fokus: Belajar mekanik dasar (move, attack, defend)

Level 4-6 (Medium):
  • Musuh: Goblin + Skeleton (HP 12-15, AC 13-14)
  • Jumlah: 5-6 musuh
  • Fokus: Penggunaan spell, target prioritization

Level 7-9 (Hard):
  • Musuh: Skeleton dominan (HP 16-20, AC 15-16)
  • Jumlah: 6-7 musuh
  • Fokus: Resource management, positioning

Level 10 (Boss Equivalent):
  • Musuh: 8 Skeleton elite (HP 32, AC 16)
  • Fokus: Optimalisasi semua tools yang ada
```

**Player Power Progression:**
- Player units TIDAK naik level secara individual (stats tetap).
- Progression = player skill improvement dan level unlock.
- Enemy scaling memastikan tantangan meningkat, bukan power creep.

### b. Level

**10 Levels** — lihat tabel di **3.e** untuk detail scaling.

Level di-track oleh `LevelManager` singleton:
- `_currentLevel` — level saat ini (1–10)
- `AdvanceLevel()` — increment, capping di MaxLevel
- `Reset()` — kembali ke level 1 (setelah defeat)
- `SetLevel(n)` — jump ke level tertentu (dari level select)

---

## 12. Core Mechanic

### a. Control

| Input | Aksi | Konteks |
|-------|------|---------|
| **Left Click** | Pilih tile / target | Grid |
| **Left Click (unit)** | Select unit | Grid (jika ada occupant) |
| **Button Click** | Move / Attack / Defend / Wait / Spell | Bottom bar |
| **Button Click** | Next Level / Level Select / Restart | Victory/Defeat overlay |
| **Button Click** | Level pilihan | Level select overlay |

*Tidak ada keyboard shortcut (belum diimplementasikan).*

### b. Object Game

#### Game Objects (Scene):

| Object | Tipe | Fungsi |
|--------|------|--------|
| **BattleController** | MonoBehaviour | Orchestrator utama battle flow |
| **TurnManager** | MonoBehaviour | Queue, initiative, phase management |
| **ActionSystem** | MonoBehaviour | Execute actions (move, attack, defend, wait) |
| **GridManager** | MonoBehaviour | Generate dan manage grid 16×9 |
| **LevelManager** | MonoBehaviour (DontDestroyOnLoad) | Tracking level + scaling formulas |
| **CameraController** | MonoBehaviour | Camera follow + shake |
| **BattleHUD** | MonoBehaviour | Semua UI overlay |
| **Unit** | MonoBehaviour (Prefab) | Karakter di grid (player + enemy) |

#### Data Objects (ScriptableObject):

| Object | Tipe | Fungsi |
|--------|------|--------|
| **UnitData** | ScriptableObject | Stats, class, weapon, damage notation |
| **SpellData** | [System.Serializable] | Spell parameters (type, range, damage, duration) |

#### Support Objects:

| Object | Fungsi |
|--------|--------|
| **Tile** | Representasi 1 cell grid |
| **AttackResult** | Hasil kalkulasi damage (struct) |
| **DiceRoller** | Static RNG utilities |
| **DamageCalculator** | Static attack resolution |
| **ActionRangeCalculator** | Static BFS range calculations |
| **SpellSystem** | Static spell definitions + casting |
| **DiceRollDisplay** | Animasi d20 roll UI |
| **DamagePopup** | Floating damage number |
| **CombatLog** | Riwayat pertarungan |
| **TileTooltip** | Hover info tile |
| **TextureFactory** | Procedural 16×16 sprite generation |
| **SpriteFactory** | Convert Texture2D → Sprite |
| **UIColorPalette** | Color constants |
| **UIHelper** | Shared UI utilities |

---

## 13. Penjelasan Konsep Algoritma yang Digunakan

### A. BFS (Breadth-First Search) untuk Range Calculation

```csharp
// GridManager.GetTilesInRange()
// Algoritma:
// 1. Mulai dari origin tile (queue)
// 2. Untuk setiap tile, cek 4 tetangga (up/down/left/right)
// 3. Jika tetangga walkable & belum dikunjungi → add ke queue
// 4. Track distance dari origin (BFS level)
// 5. Stop ketika distance > range
// 6. Kembalikan semua tile yang terjangkau

Kompleksitas: O(N) dimana N = jumlah tile dalam range (max ~25)
```

### B. Damage Calculation (D20 System)

```csharp
// DamageCalculator.CalculateDamage()
// 1. Roll d20 → attackRoll (1-20)
// 2. Hitung totalAttack = attackRoll + AttackBonus
// 3. Tentukan targetAC = defender.armorClass (+4 jika defending)
// 4. Evaluasi:
//    - attackRoll == 1  → MISS (damage = 0)
//    - attackRoll == 20 → CRIT (damage dice × 2)
//    - totalAttack >= targetAC → HIT
//    - else → MISS
// 5. Jika HIT/CRIT → roll damage dice + modifiers
```

### C. Enemy AI Prioritization

```csharp
// TurnManager.RunEnemyAI()
// 1. Kumpulkan semua player units yang alive
// 2. Untuk setiap player unit, hitung score:
//    score = (1 - hpRatio) × 20        // prioritaskan low HP
//          + classPriority              // Wizard > Cleric > Rogue > Fighter
//          - manhattanDistance          // pinalti jarak
// 3. Pilih target dengan score tertinggi
// 4. Decision tree:
//    IF HP < 25% AND near target → retreat + defend
//    ELSE IF in attack range → attack
//    ELSE → move toward target
//    IF can't reach → strategic defend
```

### D. Level Scaling

```csharp
// LevelManager
// EnemyCount   = 3 + (level - 1) × 1    (max 8)
// HpMultiplier = 1 + (level - 1) × 0.25  (level 10 = 3.25×)
// DamageBonus  = (level - 1) × 1         (level 10 = +9)
// AcBonus      = (level - 1) / 2         (level 10 = +4 AC)
```

### E. Initiative Sorting

```csharp
// TurnManager.StartNewRound()
// 1. Untuk setiap unit alive: roll d20 + DexterityMod
// 2. Sort descending by initiativeRoll
// 3. Tie-break: player unit first
// 4. Simpan dalam turnQueue
// 5. Iterasi queue → active unit = queue[currentIndex]
```

### F. Dice Notation Parsing

```csharp
// DiceRoller.RollDamage("2d6+3"):
// Regex: ^(\d+)d(\d+)([+-]\d+)?$
// Match groups: count=2, sides=6, bonus=+3
// Roll count × sides + bonus
// Contoh: 2d6+3 → roll 2× d6 (= 2-12) + 3 = 5-15
```

---

## Referensi Kode

Semua kode tersedia di `Assets/_Scripts/`:

| Direktori | File |
|-----------|------|
| Characters | `Unit.cs`, `UnitData.cs`, `UnitClass.cs`, `DiceRoller.cs` |
| Combat | `ActionSystem.cs`, `TurnManager.cs`, `DamageCalculator.cs`, `SpellSystem.cs`, `ActionRangeCalculator.cs` |
| Core | `BattleController.cs`, `GridManager.cs`, `CameraController.cs`, `LevelManager.cs`, `Tile.cs` |
| UI | `BattleHUD.cs`, `CombatLog.cs`, `DamagePopup.cs`, `DiceRollDisplay.cs`, `LayoutHelper.cs`, `SpriteFactory.cs`, `TextureFactory.cs`, `TileTooltip.cs`, `UIColorPalette.cs`, `UIFactory.cs`, `UIHelper.cs` |
| Editor | `CreateUnitDataAssets.cs` |

---

*Dokumen ini dibuat berdasarkan kode sumber D&D Tactics yang sedang dikembangkan di Unity.*
