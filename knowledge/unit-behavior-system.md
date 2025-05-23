# Thiết kế Behavior & Chỉ số cho Unit/Troop trong Unity

## 1. Cấu trúc dữ liệu đơn vị (Unit Data Structure)

### Chỉ số cơ bản (Base Stats)
| Thuộc tính | Mô tả |
|------------|-------|
| Hitpoints | Máu của đơn vị |
| Shield Hitpoints | Máu giáp (nếu có) |
| Mass | Khối lượng (ảnh hưởng đến việc bị đẩy lùi) |
| Damage | Sát thương gây ra |
| Move Speed | Tốc độ di chuyển (normal, fast, slow) |
| Hit Speed | Thời gian giữa các đòn tấn công (giây) |
| Load Time | Thời gian chuẩn bị trước khi tấn công (giây) |
| Range | Tầm đánh (Melee-Short, Melee-Long, hoặc giá trị số cho tầm xa) |
| Projectile Range | Tầm bắn của đạn (với unit tầm xa) |
| Deploy Time | Thời gian triển khai |
| Detection Range | Phạm vi phát hiện kẻ địch (tiles) |

### Chỉ số khả năng (Ability Stats)
| Thuộc tính | Mô tả |
|------------|-------|
| Ability | Loại khả năng đặc biệt |
| Ability Cost | Chi phí sử dụng khả năng |
| Ability Cooldown | Thời gian hồi chiêu (giây) |
| Ability Parameters | Các tham số đặc biệt của khả năng |

## 2. Kỹ thuật thiết kế trong Unity

### A. Scriptable Objects cho Unit Definition
- Tạo ScriptableObject riêng cho mỗi loại đơn vị
- Lưu trữ tất cả thông số, chỉ số và hành vi sẵn có
- Cho phép thiết kế game chỉnh sửa các thông số trong Unity Editor

### B. Custom Inspector và Editor Tools
- Tạo giao diện tùy chỉnh cho ScriptableObjects trong Inspector
- Phân loại các thông số thành các nhóm có ý nghĩa
- Thêm preview hành vi trực quan trong editor

### C. Component-based Behavior System
- Tạo các component hành vi tái sử dụng
- Cho phép kéo-thả các hành vi vào đơn vị
- Hỗ trợ tùy chỉnh tham số cho từng hành vi

### D. Behavior Trees/State Machines
- Sử dụng editor trực quan cho hành vi phức tạp
- Cho phép thiết kế các chuỗi hành vi mà không cần code
- Lưu trữ cấu trúc hành vi dưới dạng asset có thể tái sử dụng

## 3. Các loại đơn vị và hành vi đặc trưng

### Đơn vị cơ bản
| Loại | Mô tả | Chỉ số đặc trưng | Hành vi đặc trưng |
|------|-------|------------------|-------------------|
| Sword | Tấn công tầm gần | HP: 1700, Damage: 200, Hit Speed: 0.7s | Tấn công cận chiến, di chuyển bình thường |
| Bow | Tấn công tầm xa | HP: 300, Damage: 100, Hit Speed: 1.1s, Range: 7 | Tấn công từ xa, ưu tiên đứng yên khi tấn công |
| Pike | Phòng thủ tĩnh | HP: 1500, Damage: 200, Hit Speed: 0.1s, Range: Melee-Long | Đẩy lùi kẻ địch khi tấn công, không reset tấn công khi di chuyển |

### Đơn vị đặc biệt
| Loại | Mô tả | Chỉ số đặc trưng | Hành vi đặc trưng |
|------|-------|------------------|-------------------|
| Sword_Shield | Có khiên chặn đạn | HP: 1700, Shield: 400, Damage: 200, Hit Speed: 0.7s | Chặn đạn, khi mất khiên trở thành đơn vị thường |
| Tank_Unit_Slow | HP cao, tấn công yếu | HP: 2500, Damage: 150, Hit Speed: 2s, Speed: Slow | Đẩy lùi đối thủ, di chuyển chậm |
| Jumper | Nhảy vào mục tiêu | HP: 500, Damage: 200, Speed: Fast, Hit Speed: 0.5s | Ngay lập tức nhảy vào mục tiêu trong tầm tấn công |

### Đơn vị có khả năng đặc biệt
| Loại | Mô tả | Khả năng | Cooldown | 
|------|-------|----------|----------|
| Ranger_Melee | 2 loại tấn công | Throw (tấn công tầm xa tự động) | 20s |
| Snare | Trói chân đối thủ | Snare (làm chậm/đóng băng) | 20s |
| Summoner | Triệu hồi đơn vị | Summon (tạo đơn vị phụ) | 30s |
| Controller | Chiếm quyền điều khiển | Control (điều khiển đối thủ) | - |

### Đơn vị đội hình
| Loại | Mô tả | Hành vi đặc trưng |
|------|-------|-------------------|
| Phalanx | Đội hình phòng thủ | Giống Pike nhưng có thể di chuyển |
| Testudo | Đội hình rùa | Dựng khiên 3 hướng xung quanh, di chuyển chậm |
| Knight | Đội hình tấn công | Charge theo đường thẳng tấn công vào mục tiêu |

## 4. Triển khai các mẫu thiết kế

### Mẫu Weighted Behavior System
Hệ thống hành vi với trọng số cho phép đơn vị tự chọn hành vi phù hợp nhất:

1. **Trọng số cơ bản**: Mỗi hành vi có một trọng số cơ bản
2. **Bội số trọng số**: Trọng số được điều chỉnh dựa trên các điều kiện
3. **Lựa chọn hành vi**: Hành vi có trọng số cao nhất được thực thi

### Mẫu Formation System
Hệ thống đội hình cho phép quản lý nhóm đơn vị:

1. **Vị trí tương đối**: Mỗi đơn vị có vị trí tương đối trong đội hình
2. **Cấu hình đội hình**: Các template đội hình khác nhau (Line, Column, Phalanx, Testudo)
3. **Điều chỉnh hành vi**: Hành vi đơn vị được điều chỉnh dựa trên đội hình

### Mẫu State Machine
Hệ thống trạng thái cho phép đơn vị chuyển đổi giữa các hành vi:

1. **Trạng thái cơ bản**: Idle, Aggro, Knockback, Stun
2. **Chuyển đổi trạng thái**: Các điều kiện kích hoạt chuyển đổi
3. **Hành vi theo trạng thái**: Mỗi trạng thái có tập hợp hành vi riêng

## 5. Công cụ Editor cho Người thiết kế game

### Unit Type Editor
- Giao diện trực quan để tạo và chỉnh sửa loại đơn vị
- Preview trực tiếp các thông số và hành vi
- So sánh và cân bằng giữa các loại đơn vị

### Formation Editor
- Công cụ thiết kế đội hình trực quan
- Xem trước đội hình trong scene
- Điều chỉnh các tham số đội hình

### Behavior Tree Editor
- Giao diện kéo-thả để thiết kế hành vi
- Xem trước và kiểm tra hành vi trong editor
- Tái sử dụng các node hành vi

### Testing Tools
- Công cụ mô phỏng chiến đấu
- Phân tích hiệu suất của đơn vị
- Công cụ cân bằng tự động

## 6. Quy trình làm việc cho Người thiết kế game

1. Tạo unit type mới với các chỉ số cơ bản
2. Chọn các hành vi có sẵn từ thư viện
3. Điều chỉnh các tham số hành vi
4. Thiết kế các đội hình phù hợp
5. Kiểm tra trong môi trường mô phỏng
6. Cân bằng và điều chỉnh

Bằng cách kết hợp các công nghệ này, người thiết kế game có thể tạo và chỉnh sửa các đơn vị một cách dễ dàng mà không cần viết code, đồng thời đảm bảo tính module hóa và khả năng mở rộng của hệ thống.