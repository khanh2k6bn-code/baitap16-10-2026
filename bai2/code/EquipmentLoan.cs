using System;

public class EquipmentLoan
{
    public string LoanId { get; set; }           // Mã phiếu (Duy nhất)
    public string StudentId { get; set; }        // Mã sinh viên[cite: 5]
    public string StudentName { get; set; }      // Họ tên sinh viên[cite: 5]
    public string EquipmentName { get; set; }    // Tên thiết bị[cite: 5]
    public int Quantity { get; set; }            // Số lượng[cite: 5]
    public DateTime BorrowDate { get; set; }     // Ngày mượn[cite: 5]
    public DateTime DueDate { get; set; }        // Hạn trả[cite: 5]
    public DateTime? ReturnDate { get; set; }    // Ngày trả thực tế (có thể trống)[cite: 5]
    public string Status { get; set; }           // Trạng thái: Đang mượn, Đã trả, Quá hạn[cite: 5]
}
