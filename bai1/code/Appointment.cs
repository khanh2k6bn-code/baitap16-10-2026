using System;

public class Appointment
{
    public string AppointmentId { get; set; } // Mã lịch hẹn (Duy nhất)
    public string PatientName { get; set; }   // Họ tên bệnh nhân
    public string PhoneNumber { get; set; }   // Số điện thoại
    public DateTime AppointmentDate { get; set; } // Ngày giờ hẹn
    public string Doctor { get; set; }        // Bác sĩ
    public string Reason { get; set; }        // Lý do khám
    public string Status { get; set; }        // Trạng thái: Chờ khám, Đang khám, Hoàn tất, Đã hủy
}
