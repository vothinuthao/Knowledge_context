# Kiến Trúc và Logic Đơn Vị Game Chiến Thuật

## Sơ đồ Flow Tổng Quan

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     GAME LOOP / UPDATE CYCLE                             │
└───────────────────────────────┬─────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         ECS ARCHITECTURE                                 │
│                                                                         │
│  ┌───────────────┐       ┌───────────────┐       ┌───────────────┐     │
│  │    Entities   │       │   Components  │       │    Systems    │     │
│  │ ┌───────────┐ │       │ ┌───────────┐ │       │ ┌───────────┐ │     │
│  │ │   Units   │ │       │ │ Transform │ │       │ │   State   │ │     │
│  │ └───────────┘ │       │ └───────────┘ │       │ │ Management│ │     │
│  │ ┌───────────┐ │       │ ┌───────────┐ │       │ └───────────┘ │     │
│  │ │ Commanders │ │       │ │  Health   │ │       │ ┌───────────┐ │     │
│  │ └───────────┘ │       │ └───────────┘ │       │ │Tactical AI│ │     │
│  │ ┌───────────┐ │       │ ┌───────────┐ │       │ └───────────┘ │     │
│  │ │   Squads  │ │       │ │  Behavior │ │       │ ┌───────────┐ │     │
│  │ └───────────┘ │       │ └───────────┘ │       │ │ Movement  │ │     │
│  └───────────────┘       └───────────────┘       │ └───────────┘ │     │
│                                                  │ ┌───────────┐ │     │
│                                                  │ │  Combat   │ │     │
│                                                  │ └───────────┘ │     │
│                                                  └───────────────┘     │
└─────────────────────────────┬─────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                       DECISION HIERARCHY                                 │
│                                                                         │
│  ┌─────────────────┐    ┌─────────────────┐    ┌──────────────────┐    │
│  │  Tactical Layer │    │   Squad Layer   │    │  Individual Layer │    │
│  │                 │───►│                 │───►│                   │    │
│  │  Battle Analysis│    │Formation Control│    │  State Machine    │    │
│  │  Target Priority│    │ Role Assignment │    │  Behavior System  │    │
│  └─────────────────┘    └─────────────────┘    └──────────────────┘    │
└─────────────────────────────┬─────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         STATE MACHINE                                    │
│                                                                         │
│       ┌──────────┐                             ┌──────────┐             │
│       │   Idle   │◄───────────────────────────►│   Aggro  │             │
│       └────┬─────┘                             └─────┬────┘             │
│            │                                         │                  │
│            │             ┌─────────────┐             │                  │
│            └────────────►│  Knockback  │◄────────────┘                  │
│                          └──────┬──────┘                                │
│                                 │                                       │
│                                 ▼                                       │
│                          ┌─────────────┐                                │
│                          │    Stun     │                                │
│                          └─────────────┘                                │
└─────────────────────────────┬─────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     WEIGHTED BEHAVIOR SYSTEM                             │
│                                                                         │
│  ┌─────────────────┐    ┌─────────────────┐    ┌──────────────────┐    │
│  │ Movement Behaviors│   │Combat Behaviors │    │Formation Behaviors│    │
│  │                 │    │                 │    │                   │    │
│  │ - Move (Seek)   │    │ - Attack        │    │ - Phalanx         │    │
│  │ - Strafe        │    │ - Idle Attack   │    │ - Testudo         │    │
│  │ - Ambush Move   │    │ - Jump Attack   │    │ - Charge          │    │
│  │                 │    │ - Surround      │    │ - Protect/Cover   │    │
│  └────────┬────────┘    └────────┬────────┘    └─────────┬────────┘    │
│           │                      │                       │              │
│           └──────────────────────┼───────────────────────┘              │
│                                  │                                      │
│                                  ▼                                      │
│                        ┌───────────────────┐                            │
│                        │Behavior Selection │                            │
│                        │  (Based on Weight)│                            │
│                        └─────────┬─────────┘                            │
│                                  │                                      │
└─────────────────────────────┬────┴────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                         STEERING SYSTEM                                  │
│                                                                         │
│  ┌─────────────────┐    ┌─────────────────┐    ┌──────────────────┐    │
│  │  Basic Forces   │    │ Formation Forces │    │   Final Motion   │    │
│  │                 │    │                  │    │                   │    │
│  │ - Seek          │    │ - FormationAlign │    │    Weighted      │    │
│  │ - Flee          │    │ - PathSeeking    │    │    Combination   │    │
│  │ - Arrive        │    │ - ObstacleAvoid  │    │    of Forces     │    │
│  │ - Separate      │    │ - CollectiveThreat│   │                   │    │
│  └─────────────────┘    └─────────────────┘    └──────────────────┘    │
└─────────────────────────────┬─────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    EXECUTION & FEEDBACK                                  │
│                                                                         │
│  ┌─────────────────┐    ┌─────────────────┐    ┌──────────────────┐    │
│  │  Animations     │    │  Visual Effects  │    │   Sound Effects   │    │
│  └─────────────────┘    └─────────────────┘    └──────────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

## 1. Kiến trúc ECS (Entity-Component-System)

### Entity
- **Units**: Mỗi lính là một Entity riêng biệt trong hệ thống
- **Commanders**: Entity đặc biệt gắn liền với mỗi đội quân
- **Squad Entity**: Entity ảo đại diện cho một nhóm đơn vị cùng loại
- **Entity ID**: Mỗi đơn vị có ID duy nhất để theo dõi và quản lý

### Components
- **TransformComponent**: Lưu trữ vị trí, hướng, và tỷ lệ của đơn vị
- **HealthComponent**: Quản lý trạng thái sức khỏe, điểm máu và khả năng phục hồi
- **NavigationComponent**: Chứa thông tin đường đi và điểm đến
- **CombatComponent**: Định nghĩa thông số tấn công, tầm đánh và thời gian hồi chiêu
- **FormationComponent**: Xác định vị trí tương đối trong đội hình
- **UnitTypeComponent**: Xác định loại đơn vị (Infantry, Archer, Pike)
- **AnimationComponent**: Lưu trữ trạng thái hoạt ảnh hiện tại
- **BehaviorComponent**: Kết nối đến cây hành vi/state machine của đơn vị
- **SteeringComponent**: Chứa các tham số và trọng số cho hành vi di chuyển
- **StateComponent**: Lưu trạng thái hiện tại (Idle, Aggro, Stun, Knockback)
- **SquadAwarenessComponent**: Lưu thông tin về vị trí và vai trò trong squad
- **TacticalComponent**: Lưu trữ các chỉ thị chiến thuật từ AI cấp cao hơn
- **WeightedBehaviorComponent**: Quản lý trọng số cho các hành vi khác nhau
- **StealthComponent**: Quản lý khả năng ẩn nấp (cho Ambush Move)

### Systems
- **StateManagementSystem**: Quản lý chuyển đổi giữa các trạng thái
- **MovementSystem**: Xử lý di chuyển và đội hình
- **CombatSystem**: Quản lý tấn công và phòng thủ
- **AIDecisionSystem**: Hệ thống quyết định hành động
- **AnimationSystem**: Điều khiển hoạt ảnh theo trạng thái
- **FormationSystem**: Duy trì đội hình cho nhóm đơn vị
- **InteractionSystem**: Xử lý tương tác với môi trường và đơn vị khác
- **SteeringSystem**: Tính toán các lực di chuyển phức tạp
- **SquadCoordinationSystem**: Điều phối hành động giữa các đơn vị trong squad
- **TacticalAnalysisSystem**: Phân tích chiến thuật và đưa ra quyết định cấp cao
- **WeightedBehaviorSystem**: Tính toán hành vi nào được thực thi dựa trên trọng số
- **AggroDetectionSystem**: Xử lý việc phát hiện đơn vị đối phương

## 2. State Machine

### Trạng thái cơ bản
1. **Idle State**
   - Trigger: Khi không thực hiện bất kỳ behavior nào
   - Hành vi: Đơn vị đứng yên, quan sát xung quanh
   - Chuyển đổi: Chuyển sang Aggro khi phát hiện kẻ địch

2. **Aggro State**
   - Trigger: Khi phát hiện kẻ địch trong tầm aggro_range
   - Hành vi: Xoay hướng nhìn về phía mục tiêu, chuẩn bị tấn công
   - Chuyển đổi: Thực thi các behavior attack khi mục tiêu vào tầm tấn công

3. **Knockback State**
   - Trigger: Khi nhận sát thương có chỉ số knockback, va chạm với unit khác
   - Hành vi: Bị đẩy lùi, không kiểm soát được chuyển động
   - Chuyển đổi: Trở về trạng thái trước đó sau khi hoàn thành

4. **Stun State**
   - Trigger: Khi nhận sát thương lớn hơn 1 ngưỡng
   - Hành vi: Đứng yên, không thực hiện bất kỳ hành động nào
   - Chuyển đổi: Trở về trạng thái trước đó sau khi hết thời gian choáng

## 3. Hệ thống Behavior với trọng số (Weighted Behavior System)

Các behavior được đánh giá và chọn lựa dựa trên trọng số (weight), cho phép đơn vị linh hoạt lựa chọn hành vi phù hợp nhất với tình huống.

### Hành vi di chuyển
1. **Move (Seek)**
   - Trigger: Khi vị trí squad mới khác với vị trí hiện tại
   - Hành vi: Di chuyển đến vị trí mới bằng steering behavior Seek
   - Trọng số: Phụ thuộc vào khoảng cách với vị trí squad (distance_squad)

2. **Strafe**
   - Trigger: Khi đang trong Aggro State nhưng chưa sẵn sàng tấn công
   - Hành vi: Né tránh mục tiêu gần nhất, di chuyển sang ngang
   - Trọng số: Tỷ lệ thuận với khoảng cách đến kẻ địch (distance_enemy)

3. **Ambush Move**
   - Trigger: Khi ở trạng thái Idle một thời gian
   - Hành vi: Di chuyển chậm, không kích hoạt Aggro của kẻ địch
   - Trọng số: Cố định, nhưng mất hiệu lực khi bị tấn công

### Hành vi tấn công
4. **Attack**
   - Trigger: Khi ở Aggro State và sẵn sàng tấn công
   - Hành vi: Tiếp cận và tấn công mục tiêu gần nhất
   - Trọng số: Tỷ lệ nghịch với khoảng cách đến kẻ địch (distance_enemy)

5. **Idle Attack**
   - Trigger: Khi ở Aggro State và sẵn sàng tấn công
   - Hành vi: Đứng yên và tấn công mục tiêu từ xa
   - Trọng số: Cao cho Archer, thấp cho các đơn vị khác

6. **Jump Attack**
   - Trigger: Khi mục tiêu trong tầm nhảy
   - Hành vi: Nhảy vào mục tiêu, gây sát thương ngay lập tức
   - Trọng số: Rất cao khi điều kiện kích hoạt thỏa mãn, 0 nếu không thỏa mãn

### Hành vi hợp tác đội nhóm
7. **Surround**
   - Trigger: Khi nhận lệnh di chuyển đến ô có kẻ địch
   - Hành vi: Phân tán đội hình bao vây mục tiêu
   - Trọng số: Cao khi kẻ địch đơn lẻ, thấp khi nhiều kẻ địch

8. **Protect**
   - Trigger: Khi có đồng minh cần bảo vệ trong tầm nhìn
   - Hành vi: Che chắn đồng minh khỏi kẻ địch gần nhất
   - Trọng số: Cao hơn cho các đơn vị Infantry, thấp hơn cho các đơn vị khác

9. **Cover**
   - Trigger: Khi có đơn vị Protect gần đó
   - Hành vi: Đứng sau đơn vị Protect để được bảo vệ
   - Trọng số: Cao cho Archer và đơn vị yếu, thấp cho các đơn vị khác

### Hành vi đội hình chiến đấu
10. **Charge**
    - Trigger: Khi nhận lệnh tấn công
    - Hành vi: Xếp thành đội hình thẳng, tăng tốc, lao vào mục tiêu
    - Trọng số: Cao cho các đơn vị cận chiến, thay đổi theo khoảng cách mục tiêu

11. **Phalanx**
    - Trigger: Khi nhận lệnh đội hình phòng thủ
    - Hành vi: Xếp thành đội hình với giáo chĩa ra, chống lại tấn công trực diện
    - Trọng số: Cao cho Pike units, tăng khi ở điểm thắt nút địa hình

12. **Testudo**
    - Trigger: Khi đối mặt với nhiều đơn vị tấn công tầm xa
    - Hành vi: Xếp thành đội hình phòng thủ với khiên che chắn mọi hướng
    - Trọng số: Cao cho Infantry, thấp cho các đơn vị khác

## 4. Kiến trúc phân tầng ra quyết định

### Tầng chiến thuật (Tactical Layer)
- **TacticalAnalysisSystem**: Đánh giá toàn cảnh chiến trường
- **ThreatAssessmentLogic**: Đánh giá mức độ nguy hiểm từ kẻ địch
- **ResourceAllocationStrategy**: Phân bổ đơn vị dựa trên ưu tiên chiến thuật
- **TerrainAdvantageCalculator**: Phân tích lợi thế địa hình
- **ObjectiveManager**: Quản lý mục tiêu ưu tiên (bảo vệ/tấn công)

### Tầng đội (Squad Layer)
- **SquadCoordinationSystem**: Điều phối hành động giữa các đơn vị
- **FormationManager**: Quản lý và duy trì đội hình chiến đấu
- **RoleAssignmentLogic**: Phân công vai trò cho từng đơn vị
- **SquadStateManager**: Quản lý trạng thái của cả đội
- **TacticalExecutionController**: Thực thi chiến thuật từ tầng chiến thuật

### Tầng đơn vị (Unit Layer)
- **UnitBehaviorSystem**: Điều khiển hành vi của từng đơn vị
- **WeightedBehaviorSelector**: Chọn hành vi dựa trên trọng số
- **CombatExecutionController**: Thực hiện các hành động chiến đấu
- **LocalSensingModule**: Nhận thức môi trường xung quanh
- **UnitAnimationController**: Điều khiển hoạt ảnh của đơn vị

## 5. Tích hợp Steering Behaviors

### Steering Behaviors cơ bản
- **Seek**: Sử dụng trong Move, Attack để di chuyển đến mục tiêu
- **Flee**: Sử dụng trong Strafe để tránh xa mục tiêu
- **Arrive**: Sử dụng khi di chuyển đến vị trí đội hình chính xác
- **Avoid**: Tích hợp vào mọi hành vi di chuyển để tránh chướng ngại vật
- **Separate**: Giữ khoảng cách giữa các đơn vị, tránh va chạm
- **Cohesion**: Duy trì kết nối với nhóm, quan trọng trong đội hình

### Steering Behaviors phức tạp cho đội hình
- **FormationFollowing**: Giữ vị trí tương đối trong đội hình
- **PathSeeking**: Tìm đường đi tối ưu trong khi vẫn duy trì đội hình
- **ObstacleAvoidanceInFormation**: Tránh chướng ngại vật trong khi duy trì đội hình
- **CollectiveThreatResponse**: Phản ứng đồng thời của cả đội trước mối đe dọa

## 6. Triển khai các hành vi đặc biệt

### Ambush Move
- **Tính năng**: Cho phép đơn vị di chuyển chậm và lẩn tránh hệ thống phát hiện của kẻ địch
- **Triển khai**: Sử dụng StealthComponent mới và sửa đổi AggroDetectionSystem để kiểm tra trạng thái ẩn nấp
- **Tương tác**: Mất hiệu lực khi nhận sát thương, tạo hệ thống bắt sự kiện DamageTakenEvent
- **Tính toán trọng số**: Cao khi kẻ địch không nhận thức được sự hiện diện, giảm dần khi di chuyển gần kẻ địch

### Surround
- **Tính năng**: Tự động phân bố đơn vị để bao vây mục tiêu
- **Triển khai**: Áp dụng thuật toán chia đều vị trí xung quanh mục tiêu
- **Thách thức**: Xử lý trường hợp mục tiêu di chuyển, cần cập nhật liên tục vị trí
- **Tương tác**: Cần điều phối giữa các đơn vị để tránh xung đột vị trí

### Phalanx và Testudo
- **Tính năng**: Đội hình chiến đấu phức tạp với lợi thế phòng thủ đặc biệt
- **Triển khai**: Tạo FormationTemplate cho mỗi đội hình, xác định vị trí tương đối
- **Thách thức**: Duy trì đội hình khi di chuyển, xử lý chướng ngại vật
- **Bộ chỉnh giảm**: Giảm tốc độ di chuyển, tăng khả năng phòng thủ

### Charge
- **Tính năng**: Tăng tốc tấn công theo đội hình thẳng hàng
- **Triển khai**: Áp dụng lực tăng tốc, điều chỉnh đội hình thành hàng ngang
- **Tương tác**: Tăng sát thương tỷ lệ với tốc độ, thêm knockback effect
- **Cân bằng**: Sau khi charge, tạo thời gian hồi chiêu dài (cooldown)

### Protect và Cover
- **Tính năng**: Hành vi bảo vệ và tìm chỗ ẩn nấp
- **Triển khai**: Sử dụng ThreatPerceptionSystem để xác định hướng nguy hiểm
- **Thách thức**: Cần xác định đúng vị trí tương đối giữa bảo vệ và được bảo vệ
- **Tương tác**: Áp dụng trọng số cao hơn cho Infantry (Protect) và Archer (Cover)

## 7. Quy trình triển khai

### Giai đoạn 1: Nền tảng ECS và State Machine
1. Thiết lập cấu trúc ECS cơ bản với các component và system thiết yếu
2. Triển khai State Machine với 4 trạng thái cơ bản (Idle, Aggro, Knockback, Stun)
3. Xây dựng giao diện chung cho hệ thống Behavior

### Giai đoạn 2: Hành vi cơ bản với trọng số
1. Triển khai WeightedBehaviorSystem và các hành vi cơ bản
2. Thêm hành vi Move và Attack đơn giản
3. Tích hợp Steering Behaviors đơn giản (Seek, Flee, Arrive)
4. Kiểm thử và cân bằng trọng số cơ bản

### Giai đoạn 3: Đặc thù các loại đơn vị
1. Triển khai logic riêng cho Infantry, Archer và Pike
2. Thêm hành vi đặc biệt cho mỗi loại (ShieldBlock, RangedAttack, FormationLock)
3. Kiểm thử và cân bằng khả năng của mỗi loại

### Giai đoạn 4: Hành vi đội nhóm và đội hình
1. Triển khai hệ thống điều phối Squad
2. Thêm các hành vi đội nhóm (Surround, Protect/Cover)
3. Xây dựng các đội hình chiến đấu (Phalanx, Testudo, Charge)
4. Kiểm thử và tối ưu hóa hiệu suất

### Giai đoạn 5: Tinh chỉnh và mở rộng
1. Cải thiện phản hồi trực quan và âm thanh
2. Tối ưu hóa hiệu suất hệ thống AI
3. Thêm các hành vi đặc biệt (Ambush Move, Jump Attack)
4. Cân bằng tổng thể hệ thống

## 8. Tối ưu hóa hiệu suất

### Tối ưu Logic
- **BehaviorComputationPooling**: Sử dụng pool cho tính toán hành vi
- **DecisionThrottling**: Giảm tần suất quyết định AI cho đơn vị xa khỏi người chơi
- **LODForBehavior**: Giảm độ phức tạp AI cho đơn vị không quan trọng
- **SpatialPartitioning**: Chia không gian thành các vùng để tối ưu tìm kiếm

### Tối ưu ECS
- **ComponentChunking**: Nhóm các component tương tự
- **SystemScheduling**: Lập lịch thực thi hệ thống hiệu quả
- **EntityArchetypes**: Sử dụng mẫu entity cho tạo đơn vị nhanh
- **JobSystem**: Tận dụng xử lý đa luồng cho các tính toán AI phức tạp

---

*Tài liệu này mô tả kiến trúc và logic để tạo nên đơn vị chiến đấu thông minh trong game chiến thuật thời gian thực, tích hợp các hành vi từ tài liệu Troop Behavior. Kiến trúc kết hợp giữa hệ thống ECS, Weighted Behavior và State Machine giúp triển khai hiệu quả các hành vi phức tạp và đa dạng. Cấu trúc này phù hợp cho các game chiến thuật thời gian thực như Bad North.*