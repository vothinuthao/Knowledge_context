# Thiết kế hệ thống Behavior & Chỉ số cho Unit/Troop trong Unity

Dựa trên tài liệu "Troop Behavior" được cung cấp, tôi sẽ tổng hợp một hệ thống thiết kế toàn diện cho việc triển khai behavior và các chỉ số của đơn vị trong Unity. Mục tiêu là xây dựng một framework linh hoạt, dễ chỉnh sửa và thân thiện với người thiết kế game.

## Phần 1: Kiến trúc hệ thống State Machine

Hệ thống trạng thái (State Machine) là nền tảng cốt lõi điều khiển hành vi của đơn vị. Mỗi đơn vị luôn tồn tại trong một trong bốn trạng thái cơ bản này:

### Trạng thái Idle (Nghỉ ngơi)
Đây là trạng thái mặc định khi đơn vị không thực hiện bất kỳ hành vi nào. Trong trạng thái này, đơn vị đứng yên và quan sát xung quanh để phát hiện các mối đe dọa tiềm tàng. Trạng thái Idle hoạt động như một điểm khởi đầu cho tất cả các hành vi khác và là nơi đơn vị quay về khi hoàn thành các nhiệm vụ.

### Trạng thái Aggro (Hung hăng)
Khi một kẻ địch xuất hiện trong phạm vi aggro_range, đơn vị chuyển sang trạng thái Aggro. Trong trạng thái này, đơn vị sẽ xoay hướng nhìn về phía mục tiêu gần nhất và chuẩn bị cho việc tấn công. Khi mục tiêu vào tầm tấn công (attack_range), đơn vị sẽ kích hoạt các behavior tấn công phù hợp.

### Trạng thái Knockback (Bị đẩy lùi)
Trạng thái này được kích hoạt khi đơn vị nhận sát thương có chỉ số knockback hoặc va chạm với các đơn vị khác. Mức độ đẩy lùi phụ thuộc vào chỉ số knockback của nguồn tấn công và chỉ số mass (khối lượng) của đơn vị bị tấn công. Trong lúc bị knockback, đơn vị mất hoàn toàn khả năng kiểm soát và không thể thực hiện bất kỳ hành động nào.

### Trạng thái Stun (Choáng váng)
Khi nhận sát thương vượt quá một ngưỡng nhất định, đơn vị rơi vào trạng thái Stun. Trong trạng thái này, đơn vị bị choáng và không thể thực hiện bất kỳ hành động nào cho đến khi hết thời gian choáng. Đây là cơ chế cân bằng quan trọng để tạo ra các khoảnh khắc tấn công hiệu quả.

## Phần 2: Hệ thống Weighted Behavior (Hành vi có trọng số)

Hệ thống behavior được thiết kế dựa trên nguyên tắc trọng số, cho phép đơn vị linh hoạt lựa chọn hành vi phù hợp nhất với tình huống hiện tại. Mỗi behavior được đánh giá một trọng số dựa trên các yếu tố môi trường, và behavior có trọng số cao nhất sẽ được thực thi.

### Nhóm Behavior Di chuyển Cơ bản

**Move (Seek) - Di chuyển tìm kiếm**
Behavior này được kích hoạt bất cứ khi nào vị trí squad mới khác với vị trí hiện tại của đơn vị. Trọng số của Move phụ thuộc vào khoảng cách distance_squad từ vị trí unit đến vị trí squad. Khoảng cách càng xa thì trọng số càng cao, thúc đẩy đơn vị nhanh chóng trở về đội hình.

**Strafe - Né tránh**
Khi đơn vị đang trong trạng thái Aggro nhưng còn delay chưa thể tiếp tục tấn công, behavior Strafe sẽ được kích hoạt. Đơn vị sẽ tìm cách tránh xa mục tiêu gần nhất để tránh sát thương. Trọng số của Strafe tỷ lệ thuận với khoảng cách đến kẻ địch - càng gần kẻ địch thì việc né tránh càng quan trọng.

**Ambush Move - Di chuyển mai phục**
Đây là một behavior đặc biệt được kích hoạt khi đơn vị ở trạng thái Idle trong một thời gian ngắn. Trong chế độ Ambush Move, đơn vị di chuyển chậm nhưng không kích hoạt aggro của kẻ địch khi ở trong tầm phát hiện. Tuy nhiên, nếu bị trúng sát thương, đơn vị sẽ mất ngay trạng thái mai phục này.

### Nhóm Behavior Tấn công

**Attack - Tấn công chủ động**
Khi đơn vị đang trong trạng thái Aggro và thời gian attack đã reset, behavior Attack sẽ được kích hoạt. Đơn vị sẽ tìm cách tiếp cận mục tiêu gần nhất và thực hiện tấn công. Trọng số của Attack phụ thuộc nghịch đảo với khoảng cách đến kẻ địch - càng gần thì trọng số càng cao.

**Idle Attack - Tấn công tại chỗ**
Tương tự như Attack nhưng đơn vị sẽ đứng yên tại chỗ để tấn công mục tiêu mà không di chuyển. Behavior này đặc biệt phù hợp cho các đơn vị tấn công từ xa hoặc các đơn vị có vai trò phòng thủ. Trọng số tính toán tương tự Attack.

**Jump Attack - Tấn công nhảy vọt**
Khi mục tiêu nằm trong tầm jump của đơn vị, behavior này sẽ được ưu tiên cao. Đơn vị sẽ nhảy vào và gây sát thương ngay lập tức cho mục tiêu. Đây là một behavior có trọng số rất cao khi điều kiện thỏa mãn, nhưng bằng 0 khi không thỏa mãn điều kiện kích hoạt.

### Nhóm Behavior Đồng đội và Chiến thuật

**Surround - Bao vây**
Các đơn vị có cùng behavior Surround khi nhận lệnh di chuyển vào tile có enemy sẽ tự động phân chia đội hình để bao vây xung quanh mục tiêu. Nếu mục tiêu di chuyển, các đơn vị sẽ điều chỉnh vị trí theo để duy trì vòng vây.

**Charge - Đột kích**
Khi tấn công, các đơn vị thuộc squad sẽ xếp lại thành đội hình đường thẳng, tăng tốc và lao vào mục tiêu. Sát thương sẽ đạt maximum ở tốc độ cao nhất. Sau khi lao vào, đơn vị sẽ vòng lại và tiếp tục thực hiện đột kích. Đây là behavior dành cho đơn vị tấn công, nhưng các đơn vị này sẽ trở nên bị động và yếu nếu bị tấn công hoặc phải phòng thủ.

**Protect - Bảo vệ**
Đơn vị sẽ tìm đồng minh xung quanh bán kính vị trí đứng và che chắn cho đồng minh đó khỏi kẻ thù gần nhất. Hệ thống có độ ưu tiên che chắn, với ưu tiên cao nhất dành cho các đơn vị có behavior Cover.

**Cover - Tìm chỗ ẩn nấp**
Đơn vị Cover sẽ tìm cách đứng sau đơn vị Protect để được bảo vệ. Đây là behavior bổ trợ hoạt động cùng với Protect để tạo thành một hệ thống phòng thủ hoàn chỉnh.

### Nhóm Behavior Đội hình Chiến đấu

**Phalanx - Đội hình Phalanx**
Các đơn vị đứng thành đội hình với mũi giáo hướng ra phía trước. Đội hình này có thể vừa di chuyển vừa giữ hình dạng với giáo luôn hướng về trước, nhưng tốc độ di chuyển cực kỳ chậm. Ưu điểm của Phalanx là chống knockback tốt và chống jump attack hiệu quả, nhưng dễ bị tấn công từ xa hoặc bị tấn công cánh.

**Testudo - Đội hình Rùa**
Đơn vị hướng khiên ra 3 hướng và bên trên, tạo thành một "lớp vỏ" bảo vệ. Có thể vừa di chuyển vừa giữ đội hình, nhưng di chuyển chậm và tầm đánh ngắn. Ưu điểm là chống knockback tốt và có thể dùng khiên để knockback kẻ địch.

## Phần 3: Cấu trúc dữ liệu ScriptableObject

Để triển khai hệ thống này trong Unity một cách thân thiện với người thiết kế game, chúng ta sử dụng ScriptableObject làm nền tảng lưu trữ dữ liệu.

### Unit Type Definition (Định nghĩa loại đơn vị)
Mỗi loại đơn vị được định nghĩa bởi một ScriptableObject chứa tất cả thông số cơ bản như hitpoints, shield hitpoints, mass, damage, move speed, hit speed, load time, range, projectile range, deploy time, detection range, và các thông tin về khả năng đặc biệt.

### Behavior Definition (Định nghĩa hành vi)
Mỗi behavior được định nghĩa bởi một ScriptableObject riêng, chứa thông tin về điều kiện kích hoạt, công thức tính trọng số, các tham số đặc biệt, và logic thực thi. Điều này cho phép người thiết kế game dễ dàng tạo ra các behavior mới hoặc chỉnh sửa các behavior hiện có mà không cần viết code.

### Formation Template (Mẫu đội hình)
Các đội hình như Phalanx, Testudo, và Charge được lưu trữ dưới dạng template, định nghĩa vị trí tương đối của từng đơn vị, các modifier về tốc độ và sát thương, cũng như các điều kiện kích hoạt.

## Phần 4: Công cụ Editor tùy chỉnh

### Behavior Weight Visualizer
Một công cụ trong Unity Editor cho phép người thiết kế game xem trực quan trọng số của các behavior trong thời gian thực. Điều này giúp debug và cân bằng hệ thống behavior một cách hiệu quả.

### Formation Designer
Một editor tool cho phép thiết kế các đội hình mới bằng cách kéo thả các đơn vị vào vị trí mong muốn. Tool này cũng cho phép preview đội hình trong scene và test tính khả thi của đội hình.

### State Machine Debugger
Một công cụ debug cho phép theo dõi các chuyển đổi trạng thái của đơn vị trong thời gian thực, giúp phát hiện các lỗi logic hoặc các chuyển đổi không mong muốn.

## Phần 5: Triển khai và Tối ưu hóa

### Component Architecture
Sử dụng kiến trúc component-based với các component như StateComponent, BehaviorComponent, FormationComponent, và WeightedBehaviorComponent. Mỗi component chịu trách nhiệm cho một khía cạnh cụ thể của hành vi đơn vị.

### Performance Optimization
- Sử dụng object pooling cho các calculation behavior
- Implement LOD system cho AI (đơn vị xa người chơi sử dụng AI đơn giản hơn)
- Batching các calculation cùng loại để tận dụng cache locality
- Sử dụng job system cho các tính toán song song

### Balancing Tools
Tạo các công cụ tự động để phân tích hiệu quả của các behavior và đưa ra gợi ý cân bằng. Ví dụ, nếu một behavior hiếm khi được sử dụng, hệ thống có thể gợi ý tăng trọng số hoặc thay đổi điều kiện kích hoạt.

Hệ thống này tạo ra một framework linh hoạt và mạnh mẽ cho việc thiết kế AI đơn vị trong game chiến thuật, đồng thời đảm bảo tính dễ sử dụng cho người thiết kế game thông qua các công cụ editor tùy chỉnh.