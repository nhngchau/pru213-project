# Ke Hoach Hoan Thien Game - The Senior Defender

## 1. Muc Tieu San Pham

Bien prototype hien tai thanh mot game 2D top-down defender hoan chinh de demo/nop bai:

- Nguoi choi hieu muc tieu trong 30 giay dau.
- Gameplay co vong lap ro: ban bug, bao ve server, nhat DataPack, nang cap, song sot den build 100%.
- Hieu ung, am thanh, UI phan hoi ro moi hanh dong quan trong.
- Enemy spawn nhieu nhung khong giat frame nho object pool.
- Tat ca chi so chinh co the can chinh qua config thay vi sua code.

## 2. Tom Tat Core Loop

Core loop chuan cua game:

1. Nguoi choi vao Main Menu.
2. Bam Play de vao GameScene.
3. Game bat dau Wave 1, server o giua map, bug spawn tu cac diem ngoai vung choi.
4. Nguoi choi di chuyen bang WASD/Arrow, nham chuot, click trai de ban.
5. Bug co gang tan cong server; neu server HP ve 0 thi thua.
6. Khi bug chet, nguoi choi nhan DataPack.
7. Het moi wave, game pause va mo Upgrade Panel.
8. Nguoi choi dung DataPack de nang CPU/RAM/Firewall, sau do bam Continue.
9. Sau 6 wave hoac build progress dat 100%, nguoi choi thang.
10. Man hinh Win/Lose hien ket qua va cho Restart/Main Menu.

## 3. User Flow Chi Tiet

### 3.1 Lan Dau Mo Game

Trang thai mong muon:

- Nguoi choi thay ten game lon: `The Senior Defender`.
- Co 3 nut ro rang: `Play`, `Guide`, `Quit`.
- Guide mo panel ngan gon gom:
  - Muc tieu: bao ve server den khi Build Progress dat 100%.
  - Dieu khien: WASD/Arrow de di chuyen, chuot trai de ban, chuot phai de ultimate.
  - DataPack: diet bug de mua upgrade sau moi wave.

Tieu chi dat:

- Khong can doc README van biet cach choi.
- Tu Main Menu vao game khong qua 2 click.

### 3.2 Flow Trong 30 Giay Dau

Trang thai mong muon:

- HUD hien ngay Server HP, Player HP, Build Progress, Wave, DataPack.
- Wave 1 spawn it enemy, toc do vua phai de nguoi choi hoc ban.
- Enemy dau tien chet nhanh de nguoi choi thay DataPack tang.
- Server bi danh lan dau co flash/sound nhe de nguoi choi hieu do la muc can bao ve.

Tieu chi dat:

- Nguoi choi moi biet "phai bao ve server" trong 10-15 giay.
- Nguoi choi biet "diet bug duoc tien nang cap" truoc upgrade break dau tien.

### 3.3 Flow Giua Wave

Trang thai mong muon:

- Khi wave ket thuc, Time.timeScale = 0 va Upgrade Panel hien len.
- Panel co 3 lua chon:
  - CPU: tang damage.
  - RAM: tang fire rate.
  - Firewall: tang HP server.
- Moi upgrade hien:
  - Level hien tai.
  - Chi so hien tai -> chi so sau khi mua.
  - Gia DataPack.
  - Trang thai khong du tien hoac max level.
- Nut Continue nam ro rang, chi bam khi nguoi choi san sang.

Tieu chi dat:

- Nguoi choi hieu upgrade nao anh huong cai gi.
- Khong co nut bi disable ma khong biet ly do.

### 3.4 Flow Ket Thuc

Win screen nen hien:

- `BUILD COMPLETE`
- Thoi gian song sot.
- So bug da diet.
- DataPack da thu/da tieu.
- So lan player bi ha.
- Nut `Restart` va `Main Menu`.

Lose screen nen hien:

- `SYSTEM CRASHED`
- Build Progress dat duoc bao nhieu phan tram.
- Wave thua.
- Ly do thua: Server HP ve 0.
- Nut `Restart` va `Main Menu`.

Tieu chi dat:

- Nguoi choi biet minh thang/thua vi sao.
- Nguoi choi co dong luc choi lai de dat ket qua tot hon.

## 4. Chi So Gameplay Can Co

### 4.1 Chi So Player

De xuat ban dau:

| Chi so | Gia tri de xuat | Ghi chu |
|:---|:---:|:---|
| Player HP | 100 | Du de bi cham vai lan nhung khong qua trau |
| Move Speed | 5 | Dang co trong code, on cho top-down |
| Respawn Penalty | 5s | Du gay ap luc vi server bi bo mac |
| Invulnerable Duration | 2s | Tranh player vua hoi sinh da chet lai |
| Bullet Damage | 10 | Base cho CPU upgrade |
| Fire Rate | 0.15s hoac 0.35-0.5s neu muon kho hon | Code hien dang 0.15, upgrade manager base la 0.5 nen can thong nhat |
| Bullets Per Shot | 1-3 | 3 vien vui hon, 1 vien de can bang hon |
| Ultimate Cooldown | 8s | Nen tang 10-15s neu ultimate qua manh |

Can theo doi khi playtest:

- Player chet trung binh bao nhieu lan moi run.
- Player co cam giac qua yeu hay qua bat tu khong.
- Co dung ultimate thuong xuyen hay quen mat no.

### 4.2 Chi So Server

De xuat ban dau:

| Chi so | Gia tri de xuat | Ghi chu |
|:---|:---:|:---|
| Server Max HP | 1000 | Dang co trong code |
| Firewall Bonus | +100 HP/lv | Dang co trong code |
| Low HP Warning | < 30% | UI doi mau + sound canh bao |
| Critical HP Warning | < 15% | Man hinh/camera can phan hoi ro hon |

Can theo doi khi playtest:

- Server mat bao nhieu HP moi wave.
- Nguoi choi thua vi server bi swarm hay vi khong hieu muc tieu.
- Firewall co dang de mua khong, hay bi bo qua.

### 4.3 Chi So Enemy

De xuat ban dau:

| Enemy | HP | Speed | Damage Server | Damage Player | Reward | Vai tro |
|:---|---:|---:|---:|---:|---:|:---|
| Syntax Error | 20 | 3.5 | 10 | 10 | 5 | Linh nho, dong, tao ap luc so luong |
| Logic Bug | 40 | 2.5 | 15 | 15 | 10 | Ne dan/zigzag, gay kho khi nham |
| Memory Leak | 150 | 1.2 | 30 | 20 | 25 | Tank cham, de lai Sludge |

Can theo doi khi playtest:

- Time-to-kill tung enemy la bao nhieu giay.
- Enemy nao bi nguoi choi uu tien diet truoc.
- Memory Leak co that su tao ap luc hay chi la bao cat.
- Logic Bug zigzag co kho chiu qua khong.

### 4.4 Chi So Wave

Game hien co `buildDuration = 180s`, `numberOfWaves = 6`, moi wave khoang 30s.

De xuat pacing:

| Wave | Thoi luong | Enemy chinh | Muc tieu cam giac |
|:---|---:|:---|:---|
| 1 | 30s | Syntax Error | Hoc dieu khien va ban |
| 2 | 30s | Syntax + it Logic | Bat dau can nham tot |
| 3 | 30s | Syntax + Logic | Tang ap luc, co upgrade dau tien ro tac dung |
| 4 | 30s | Them Memory Leak | Gioi thieu tank/sludge |
| 5 | 30s | Hon hop dong | Cao trao, can dung ultimate |
| 6 | 30s | Dong hon hop | Final wave, ap luc lon nhat |

Chi so spawn de xuat:

| Giai doan | Spawn Interval | Group Size | Active Enemy muc tieu |
|:---|---:|---:|---:|
| Early | 2.0s | 2-4 | 10-20 |
| Mid | 1.5s | 3-5 | 20-40 |
| Late | 1.0s | 4-7 | 40-80 |
| Stress test | 0.5s | 6-10 | 100-150 |

Can theo doi khi playtest:

- FPS khi active enemy 40/80/120.
- Co wave nao qua de hoac spike do kho dot ngot khong.
- Nguoi choi co kip mua upgrade co y nghia sau moi wave khong.

## 5. Chi So Ky Thuat Can Do

### 5.1 Performance

Muc tieu:

| Chi so | Muc tieu |
|:---|:---|
| FPS gameplay thuong | 60 FPS |
| FPS khi 100 enemy | >= 45 FPS |
| Spike khi spawn group | Khong thay khung hinh dung ro |
| GC Alloc trong combat | Cang thap cang tot, tranh alloc lap lai moi frame |
| Enemy instantiate runtime | Gan 0 sau khi pool da warm up |
| Bullet instantiate runtime | Gan 0 sau khi pool da warm up |

Huong kiem tra:

1. Mo Unity Profiler.
2. Play GameScene.
3. Tang group size/spawn interval de tao 80-120 enemy.
4. Xem CPU Usage, GC Alloc, FPS.
5. Neu spike xuat hien khi enemy chet, pool them death effect/hit effect.

### 5.2 Stability

Checklist:

- Khong co NullReferenceException khi vao GameScene.
- Restart sau win/lose khong bi Time.timeScale = 0.
- Cursor hien lai khi quay ve menu/thoat scene.
- Enemy pooled respawn khong giu animation chet.
- Bullet pooled khong giu velocity cu.
- Upgrade panel khong hien sai tien hoac level.

## 6. He Thong Config Nen Co

### 6.1 EnemyConfig

Da co script `EnemyConfig`.

Can tao asset:

- `Assets/Configs/Enemies/SyntaxErrorConfig.asset`
- `Assets/Configs/Enemies/LogicBugConfig.asset`
- `Assets/Configs/Enemies/MemoryLeakConfig.asset`

Gan vao prefab tuong ung:

- `SyntaxError.prefab`
- `LogicBug.prefab`
- `MemoryLeak.prefab`

Du lieu can chinh trong Inspector:

- `maxHP`
- `moveSpeed`
- `damageToServer`
- `damageToPlayer`
- `damageInterval`
- `dataPackValue`
- `deathDelay`
- `onDeathEffectPrefab`

### 6.2 WeaponConfig

Da co script `WeaponConfig`.

Can tao asset:

- `Assets/Configs/Weapons/BaseCodeGun.asset`

Du lieu can chinh:

- `fireRate`
- `bulletDamage`
- `bulletsPerShot`
- `spreadAngle`
- `recoilDistance`
- `recoilReturnSpeed`
- `defaultCapacity`
- `maxPoolSize`

### 6.3 WaveConfig De Xuat Them Sau

Nen them `WaveConfig` khi can can bang nghiem tuc.

Du lieu nen co:

- Wave number.
- Duration.
- Spawn interval.
- Min/max group size.
- Enemy weight: Syntax/Logic/Memory.
- Max active enemy.
- Break duration hoac upgrade break mode.

Vi du:

| Wave | Syntax | Logic | Memory |
|:---|---:|---:|---:|
| 1 | 100% | 0% | 0% |
| 2 | 80% | 20% | 0% |
| 3 | 65% | 35% | 0% |
| 4 | 60% | 25% | 15% |
| 5 | 50% | 35% | 15% |
| 6 | 45% | 35% | 20% |

## 7. UI/UX Can Hoan Thien

### 7.1 HUD Trong Game

Nen bo tri:

- Goc trai tren: Player HP.
- Giua tren: Build Progress + Wave.
- Goc phai tren: DataPack.
- Gan server hoac duoi man hinh: Server HP, vi day la muc tieu quan trong nhat.
- Goc trai duoi: Ultimate cooldown.

Trang thai can co:

- Server HP > 30%: mau binh thuong.
- Server HP 15-30%: mau vang/cam, sound warning nhe.
- Server HP < 15%: mau do, flash nhe, camera shake nhe khi bi danh.
- Ultimate ready: icon sang len/nhap nhay nhe.
- Ultimate cooldown: radial fill hoac so dem nguoc.

### 7.2 Upgrade Panel

Moi upgrade card nen co:

- Ten upgrade.
- Icon.
- Level hien tai.
- Stat hien tai -> stat sau mua.
- Cost.
- Nut Buy.

Noi dung cu the:

- CPU: `Damage 10 -> 15`, `Cost 50 DP`.
- RAM: `Fire Rate 0.50s -> 0.45s`, `Cost 75 DP`.
- Firewall: `Server HP +100`, `Cost 100 DP`.

Disabled state:

- Khong du tien: hien `Need 25 more DP`.
- Max level: hien `MAX`.
- Firewall lap lai nen khong co max neu muon game de hon.

### 7.3 Main Menu

Can co:

- Title lon.
- Background co server/office/coding theme.
- Button: Play, Guide, Quit.
- Audio hover/click.
- Guide panel ngan gon, khong qua 1 man hinh.

### 7.4 Win/Lose UI

Nen them thong ke:

- Bugs defeated.
- DataPack earned.
- Upgrades purchased.
- Player deaths.
- Server HP remaining khi win.
- Build percent khi lose.

Muc dich:

- Tao cam giac thanh tuu.
- Tao ly do choi lai.
- Ho tro team can bang bang du lieu that.

## 8. Effect Va Game Feel

### 8.1 Ban Sung

Can them:

- Muzzle flash tai firePoint.
- Bullet trail nhe.
- Recoil firePoint da co, co the tang/giam qua WeaponConfig.
- Sound ban co random pitch 0.95-1.05 de bot lap.
- Camera shake rat nhe khi ban nhieu vien.

Chi so de xuat:

| Effect | Gia tri |
|:---|:---|
| Muzzle flash lifetime | 0.05-0.08s |
| Bullet trail lifetime | 0.1-0.2s |
| Shoot shake amplitude | 0.05-0.1 |
| Shoot shake duration | 0.05s |

### 8.2 Enemy Hit/Death

Can them:

- Flash trang/do khi bi trung dan.
- Hit particle nho.
- Death particle theo tung loai enemy.
- DataPack popup nho `+5`, `+10`, `+25`.

Chi so de xuat:

| Effect | Gia tri |
|:---|---:|
| Hit flash | 0.08s |
| Death effect lifetime | 0.5-1.2s |
| DataPack popup lifetime | 0.8s |
| Knockback | 0.1-0.4 unit |

### 8.3 Server Hit

Can them:

- Server flash do.
- HP bar shake nhe.
- Sound impact rieng.
- Neu server HP critical, them alarm loop nhe.

Chi so de xuat:

| Effect | Gia tri |
|:---|---:|
| Server hit flash | 0.12s |
| Server shake duration | 0.15s |
| Low HP warning threshold | 30% |
| Critical warning threshold | 15% |

### 8.4 Ultimate

Ultimate hien co shockwave ring. Nen nang cap:

- Ring co glow/sprite material dep hon.
- Enemy trong wave bi hit co flash ngay.
- Sound charge/release.
- Camera pulse luc kich hoat.
- UI icon chay cooldown va flash khi ready.

Chi so de xuat:

| Chi so | Gia tri |
|:---|---:|
| Cooldown | 10-15s |
| Damage multiplier | 3x bullet damage |
| Max radius | Du quet gan het man hinh |
| Expand speed | 18-24 units/s |

## 9. Balancing Va Playtest

### 9.1 Chi So Can Ghi Lai Moi Run

Nen tao tam bang playtest:

| Run | Ket qua | Wave ket thuc | Server HP | Player deaths | Bugs killed | DataPack earned | FPS min | Ghi chu |
|:---|:---|:---|---:|---:|---:|---:|---:|:---|

Can danh gia:

- Neu nguoi moi thang ngay lan dau voi server HP cao: game qua de.
- Neu nguoi moi thua truoc Wave 3: game qua kho hoac UI khong ro.
- Neu DataPack thua qua nhieu: cost upgrade qua re hoac reward qua cao.
- Neu khong mua duoc upgrade nao sau Wave 1/2: reward qua thap hoac cost qua cao.

### 9.2 Muc Tieu Can Bang

Muc tieu cho ban demo:

- Nguoi moi co the thang sau 2-3 lan thu.
- Run full dai 3 phut, khong qua dai.
- Player nen mua duoc 1 upgrade sau Wave 1 hoac Wave 2.
- Final wave nen lam server con 10-40% HP neu nguoi choi kha.
- Ultimate nen duoc dung 2-4 lan trong mot run.

### 9.3 Cong Thuc Dieu Chinh Nhanh

Neu game qua kho:

- Giam spawn rate.
- Giam damageToServer.
- Tang server HP.
- Tang DataPack reward.
- Giam upgrade cost.
- Tang bullet damage.

Neu game qua de:

- Tang spawn rate tu Wave 3 tro di.
- Tang Memory Leak HP/damage.
- Giam DataPack reward.
- Tang cost CPU/RAM.
- Tang ultimate cooldown.

Neu game bi roi/khong ro:

- Tang effect server hit.
- Lam enemy silhouette khac nhau.
- Them warning arrow neu bug gan server.
- Lam Build Progress va Server HP noi bat hon.

## 10. Roadmap Trien Khai De Hoan Thien

### Buoc 1 - On Dinh Gameplay

- Tao config asset cho enemy/weapon.
- Gan config vao prefab.
- Test DataPack reward.
- Test upgrade mua duoc.
- Test enemy pool khi spawn dong.

### Buoc 2 - Lam Enemy Khac Biet

- Syntax Error: nhanh, yeu.
- Logic Bug: zigzag/evasive.
- Memory Leak: tank, spawn Sludge khi chet.
- Them mau/effect rieng cho tung enemy.

### Buoc 3 - UI Day Du

- Sua HUD hierarchy.
- Them low HP warning.
- Nang cap Upgrade Panel.
- Them stat screen Win/Lose.

### Buoc 4 - Game Feel

- Muzzle flash.
- Hit flash.
- Death effect.
- Camera shake.
- Ultimate polish.
- Audio feedback.

### Buoc 5 - Polish Va Nop Bai

- Doi product name.
- Cap nhat README.
- Xoa asset demo khong dung neu can.
- Build Windows/WebGL.
- Quay demo clip 60-90 giay.

## 11. Definition Of Done

Game duoc xem la hoan thien khi:

- Main Menu -> Game -> Upgrade -> Win/Lose -> Restart chay tron ven.
- Khong co error runtime trong Console.
- Game chay on dinh voi 80 enemy active.
- Moi enemy co vai tro rieng.
- Moi upgrade co tac dung de cam nhan.
- UI du ro de nguoi moi tu choi.
- Effect/am thanh phan hoi du moi hanh dong quan trong.
- README co Unity version, controls, objective, cach chay.
