namespace VikingRaven.Units.Components
{
    /// <summary>
    /// Định nghĩa các loại đội hình có thể được sử dụng
    /// </summary>
    public enum FormationType
    {
        /// <summary>
        /// Không có đội hình cụ thể
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Đội hình hàng ngang - các đơn vị xếp thành một hàng bên cạnh nhau
        /// </summary>
        Line = 1,
        
        /// <summary>
        /// Đội hình hàng dọc - các đơn vị xếp thành một cột trước sau
        /// </summary>
        Column = 2,
        
        /// <summary>
        /// Đội hình lưới hình vuông - các đơn vị xếp thành đội hình vuông/chữ nhật chặt chẽ
        /// </summary>
        Phalanx = 3,
        
        /// <summary>
        /// Đội hình rùa - các đơn vị xếp thành lưới chặt hơn, tập trung phòng thủ
        /// </summary>
        Testudo = 4,
        
        /// <summary>
        /// Đội hình tròn - các đơn vị xếp thành vòng tròn để bảo vệ tâm
        /// </summary>
        Circle = 5,
        
        /// <summary>
        /// Đội hình chuẩn 3x3 - lưới 3x3 cố định với 9 vị trí có khoảng cách đều
        /// </summary>
        Normal = 6
    }
}