# 🖥️ Cyber Defender 

## 📖 Giới thiệu
Đây là một tựa game thể loại **Top-down Shooter / Roguelite Survival** được phát triển bằng Unity 2D. Trong game, người chơi sẽ hóa thân thành một chương trình diệt virus (Anti-virus) với nhiệm vụ sống còn: Bảo vệ **Máy chủ lõi (Server Core)** khỏi các đợt tấn công liên tục của các loại mã độc, virus và lỗi hệ thống (Syntax Error, Logic Bug, Memory Leak...).

## ✨ Tính năng nổi bật
* **Bảo vệ Server (Tower Defense lai Survivor):** Di chuyển tự do, bắn súng tiêu diệt quái vật để ngăn chúng tiếp cận và phá hủy Server.
* **Hệ thống Wave & Stage:** Vượt qua 10 Stage căng thẳng với độ khó tăng dần (Máu, sát thương và số lượng quái tăng theo cấp số nhân). Chạm trán các **Boss** khổng lồ tại các mốc Stage 6, 9 và 10.
* **Tiến trình Roguelite (Run Progress):** 
  * Cột mốc Checkpoint an toàn tại các Stage 3, 6, 9. 
  * Nếu thất bại, người chơi sẽ quay lại Checkpoint gần nhất thay vì chơi lại từ đầu, đồng thời giữ lại được tài nguyên (DataPack).
* **Nâng cấp sức mạnh (Upgrades & Power-ups):**
  * **Shop (Meta-progression):** Dùng DataPack thu thập được để mua các chỉ số vĩnh viễn (Starter Damage, Fire Rate, Server Armor, Extra Bullets).
  * **Level-up trong game:** Lựa chọn nâng cấp các kỹ năng tạm thời khi lên cấp để chống lại lượng quái ngày càng đông (Overclock CPU, Upgrade RAM, Firewall, Double Shot, Explosive, Piercing Beam).

## 🎮 Cơ chế & Cốt truyện
Máy chủ lõi đang bị xâm nhập! Kẻ thù của bạn không phải là quái vật thông thường mà là các đoạn mã lỗi (`SyntaxError`), rò rỉ bộ nhớ (`MemoryLeak`), và các con bọ logic (`LogicBug`). Thu thập `DataPack` rơi ra từ chúng để củng cố bức tường lửa và nâng cấp hỏa lực. Bạn phải trụ vững cho đến khi tiến trình Build (Build Progress) đạt 100% để dọn dẹp hoàn toàn hệ thống!

## 🛠️ Kỹ thuật & Kiến trúc mã nguồn
Dự án được xây dựng với các tiêu chuẩn lập trình Game tối ưu:
* **Engine:** Unity (2D)
* **Ngôn ngữ:** C#
* **Tối ưu hóa (Object Pooling):** Tái sử dụng đạn và quái vật để đảm bảo hiệu năng mượt mà khi số lượng entity trên màn hình cực lớn.
* **Data-driven (Scriptable Objects):** Quản lý chỉ số sức mạnh của quái (EnemyConfig) và vũ khí (WeaponConfig) qua các file data độc lập, dễ dàng tinh chỉnh cân bằng game.
* **Event-driven (Observer Pattern):** Sử dụng hệ thống `GameEvents` và `PlayerEvents` để các thành phần UI, âm thanh và tiến trình game giao tiếp với nhau mà không bị phụ thuộc code chéo (Decoupling).

## 📂 Cấu trúc thư mục chính
* `Assets/Scripts/`: Chứa toàn bộ logic C# (GameManager, EnemySpawner, ServerCore, ShopManager...).
* `Assets/Prefabs/`: Các đối tượng GameObjects đã thiết lập sẵn (Player, Quái, Boss, Đạn, Hiệu ứng...).
* `Assets/Data/`: Các file cấu hình ScriptableObject.
* `Assets/Scenes/`: Các màn chơi (MainMenu, GameScene...).

