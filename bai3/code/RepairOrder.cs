using System;

public class RepairOrder
{
    public string OrderId { get; set; }          // Mã đơn (Duy nhất)[cite: 6]
    public string CustomerName { get; set; }     // Tên khách hàng[cite: 6]
    public string PhoneNumber { get; set; }      // Số điện thoại[cite: 6]
    public string DeviceType { get; set; }       // Loại thiết bị[cite: 6]
    public string Description { get; set; }      // Mô tả lỗi[cite: 6]
    public DateTime ReceiveDate { get; set; }    // Ngày tiếp nhận[cite: 6]
    public decimal EstimatedCost { get; set; }   // Chi phí dự kiến[cite: 6]
    public string Status { get; set; }           // Trạng thái: Mới tiếp nhận, Đang sửa, Chờ linh kiện, Hoàn tất, Đã hủy[cite: 6]
    public string ImagePath { get; set; }        // Đường dẫn ảnh thiết bị[cite: 6]
}
