using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace ClinicAppointmentManager
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel tableMain;
        private Panel pnlInput;
        private FlowLayoutPanel flowSearch;
        private DataGridView dgvAppointments;
        private MenuStrip menuStrip1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblStatusCount;

        private TextBox txtAppointmentId, txtPatientName, txtPhoneNumber, txtReason;
        private DateTimePicker dtpAppointmentDate;
        private ComboBox cmbDoctor, cmbStatus;
        private Button btnAdd, btnUpdate, btnDelete, btnClear;
        private TextBox txtSearch;
        private ComboBox cmbFilterStatus;
        private DateTimePicker dtpFilterDate;
        private CheckBox chkEnableDateFilter;

        private ErrorProvider errorProvider1;

        private BindingList<Appointment> appointmentList;
        private BindingSource bindingSource;

        public Form1()
        {
            InitializeComponentCustom();
            LoadInitialData();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Clinic Appointment Manager";
            this.Width = 1100;
            this.Height = 750;
            this.StartPosition = FormStartPosition.CenterScreen;

            errorProvider1 = new ErrorProvider();

            // 1. MenuStrip (Yêu cầu 6)
            menuStrip1 = new MenuStrip();
            ToolStripMenuItem menuFile = new ToolStripMenuItem("File");
            ToolStripMenuItem menuExport = new ToolStripMenuItem("Xuất ra CSV");
            menuExport.Click += MenuExport_Click;
            menuFile.DropDownItems.Add(menuExport);
            menuStrip1.Items.Add(menuFile);
            this.MainMenuStrip = menuStrip1;

            // 2. StatusStrip (Yêu cầu 6)
            statusStrip1 = new StatusStrip();
            lblStatusCount = new ToolStripStatusLabel();
            statusStrip1.Items.Add(lblStatusCount);

            // 3. TableLayoutPanel chính chia giao diện co giãn (Yêu cầu 1)
            tableMain = new TableLayoutPanel();
            tableMain.Dock = DockStyle.Fill;
            tableMain.ColumnCount = 1;
            tableMain.RowCount = 3;
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // --- KHU VỰC NHẬP LIỆU ---
            pnlInput = new Panel();
            pnlInput.Dock = DockStyle.Fill;
            pnlInput.BorderStyle = BorderStyle.FixedSingle;

            AddLabelAndControl(pnlInput, "Mã lịch hẹn:", txtAppointmentId = new TextBox(), 20, 20);
            AddLabelAndControl(pnlInput, "Họ tên BN:", txtPatientName = new TextBox(), 20, 60);
            AddLabelAndControl(pnlInput, "Số điện thoại:", txtPhoneNumber = new TextBox(), 20, 100);
            AddLabelAndControl(pnlInput, "Ngày giờ hẹn:", dtpAppointmentDate = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" }, 20, 140);

            AddLabelAndControl(pnlInput, "Bác sĩ:", cmbDoctor = new ComboBox(), 550, 20);
            cmbDoctor.Items.AddRange(new string[] { "Dr. Nguyễn Văn A", "Dr. Trần Thị B", "Dr. Lê Văn C" });
            cmbDoctor.DropDownStyle = ComboBoxStyle.DropDownList;

            AddLabelAndControl(pnlInput, "Lý do khám:", txtReason = new TextBox(), 550, 60);
            AddLabelAndControl(pnlInput, "Trạng thái:", cmbStatus = new ComboBox(), 550, 100);
            cmbStatus.Items.AddRange(new string[] { "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy" });
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;

            // Các nút chức năng (Yêu cầu 4)
            btnAdd = new Button { Text = "Thêm", Left = 550, Top = 140, Width = 75 };
            btnUpdate = new Button { Text = "Cập nhật", Left = 635, Top = 140, Width = 75 };
            btnDelete = new Button { Text = "Xóa", Left = 720, Top = 140, Width = 75 };
            btnClear = new Button { Text = "Làm mới", Left = 805, Top = 140, Width = 75 };

            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click += BtnClear_Click;

            pnlInput.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });

            // --- KHU VỰC TÌM KIẾM VÀ LỌC (Yêu cầu 4) ---
            flowSearch = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10) };

            txtSearch = new TextBox { Width = 200 };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            Label lblSearchHint = new Label { Text = "Tìm tên/SĐT:", AutoSize = true, Padding = new Padding(0, 5, 0, 0) };

            cmbFilterStatus = new ComboBox { Width = 130, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterStatus.Items.Add("Tất cả trạng thái");
            cmbFilterStatus.Items.AddRange(new string[] { "Chờ khám", "Đang khám", "Hoàn tất", "Đã hủy" });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += (s, e) => ApplyFilter();

            chkEnableDateFilter = new CheckBox { Text = "Lọc theo ngày:", AutoSize = true, Checked = false };
            chkEnableDateFilter.CheckedChanged += (s, e) => ApplyFilter();

            dtpFilterDate = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110 };
            dtpFilterDate.ValueChanged += (s, e) => ApplyFilter();

            flowSearch.Controls.AddRange(new Control[] { lblSearchHint, txtSearch,
                new Label { Text = "Trạng thái:", AutoSize = true, Padding = new Padding(10, 5, 0, 0) }, cmbFilterStatus,
                chkEnableDateFilter, dtpFilterDate });

            // --- DATAGRIDVIEW HIỂN THỊ DANH SÁCH (Yêu cầu 3) ---
            dgvAppointments = new DataGridView();
            dgvAppointments.Dock = DockStyle.Fill;
            dgvAppointments.AutoGenerateColumns = false;
            dgvAppointments.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAppointments.ReadOnly = true;
            dgvAppointments.MultiSelect = false;
            dgvAppointments.CellClick += DgvAppointments_CellClick;

            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AppointmentId", HeaderText = "Mã Lịch Hẹn", Width = 100 });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PatientName", HeaderText = "Họ Tên Bệnh Nhân", Width = 160 });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhoneNumber", HeaderText = "Số Điện Thoại", Width = 120 });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "AppointmentDate", HeaderText = "Ngày Giờ Hẹn", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy HH:mm" } });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Doctor", HeaderText = "Bác Sĩ", Width = 150 });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Reason", HeaderText = "Lý Do Khám", Width = 180 });
            dgvAppointments.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Trạng Thái", Width = 110 });

            tableMain.Controls.Add(pnlInput, 0, 0);
            tableMain.Controls.Add(flowSearch, 0, 1);
            tableMain.Controls.Add(dgvAppointments, 0, 2);

            this.Controls.Add(tableMain);
            this.Controls.Add(statusStrip1);
            this.Controls.Add(menuStrip1);
        }

        private void AddLabelAndControl(Panel panel, string labelText, Control control, int left, int top)
        {
            Label lbl = new Label { Text = labelText, Left = left, Top = top + 3, Width = 90, AutoSize = true };
            control.Left = left + 90;
            control.Top = top;
            control.Width = 160;
            panel.Controls.Add(lbl);
            panel.Controls.Add(control);
        }

        private void LoadInitialData()
        {
            appointmentList = new BindingList<Appointment>
            {
                new Appointment { AppointmentId = "LH001", PatientName = "Nguyễn Văn Nam", PhoneNumber = "0987654321", AppointmentDate = DateTime.Now.AddHours(2), Doctor = "Dr. Nguyễn Văn A", Reason = "Đau đầu chóng mặt", Status = "Chờ khám" },
                new Appointment { AppointmentId = "LH002", PatientName = "Trần Thị Hoa", PhoneNumber = "0912345678", AppointmentDate = DateTime.Now.AddDays(1), Doctor = "Dr. Trần Thị B", Reason = "Khám tổng quát", Status = "Chờ khám" }
            };

            bindingSource = new BindingSource();
            bindingSource.DataSource = appointmentList;
            dgvAppointments.DataSource = bindingSource;
            UpdateStatusCount();
        }

        // --- KIỂM TRA DỮ LIỆU & RÀNG BUỘC (Yêu cầu 5) ---
        private bool ValidateData(bool isNew)
        {
            bool isValid = true;
            errorProvider1.Clear();

            if (string.IsNullOrWhiteSpace(txtAppointmentId.Text))
            {
                errorProvider1.SetError(txtAppointmentId, "Mã lịch hẹn không được để trống!");
                isValid = false;
            }
            else if (isNew && appointmentList.Any(x => x.AppointmentId == txtAppointmentId.Text.Trim()))
            {
                errorProvider1.SetError(txtAppointmentId, "Mã lịch hẹn phải là duy nhất!");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtPatientName.Text))
            {
                errorProvider1.SetError(txtPatientName, "Tên bệnh nhân bắt buộc phải có!");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtPhoneNumber.Text) || !System.Text.RegularExpressions.Regex.IsMatch(txtPhoneNumber.Text.Trim(), @"^0\d{9,10}$"))
            {
                errorProvider1.SetError(txtPhoneNumber, "Số điện thoại không hợp lệ (bắt đầu bằng 0, có 10-11 chữ số)!");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtReason.Text))
            {
                errorProvider1.SetError(txtReason, "Lý do khám không được để trống!");
                isValid = false;
            }

            if (dtpAppointmentDate.Value < DateTime.Now)
            {
                errorProvider1.SetError(dtpAppointmentDate, "Ngày hẹn không được nằm trong quá khứ!");
                isValid = false;
            }

            if (cmbDoctor.SelectedIndex == -1)
            {
                errorProvider1.SetError(cmbDoctor, "Vui lòng chọn bác sĩ!");
                isValid = false;
            }

            if (cmbStatus.SelectedIndex == -1)
            {
                errorProvider1.SetError(cmbStatus, "Vui lòng chọn trạng thái!");
                isValid = false;
            }

            return isValid;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateData(true)) return;

            var newApp = new Appointment
            {
                AppointmentId = txtAppointmentId.Text.Trim(),
                PatientName = txtPatientName.Text.Trim(),
                PhoneNumber = txtPhoneNumber.Text.Trim(),
                AppointmentDate = dtpAppointmentDate.Value,
                Doctor = cmbDoctor.SelectedItem.ToString(),
                Reason = txtReason.Text.Trim(),
                Status = cmbStatus.SelectedItem.ToString()
            };

            appointmentList.Add(newApp);
            ClearInputs();
            UpdateStatusCount();
            MessageBox.Show("Thêm lịch hẹn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvAppointments.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn lịch hẹn cần sửa trên bảng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateData(false)) return;

            var selectedApp = (Appointment)dgvAppointments.CurrentRow.DataBoundItem;
            selectedApp.PatientName = txtPatientName.Text.Trim();
            selectedApp.PhoneNumber = txtPhoneNumber.Text.Trim();
            selectedApp.AppointmentDate = dtpAppointmentDate.Value;
            selectedApp.Doctor = cmbDoctor.SelectedItem.ToString();
            selectedApp.Reason = txtReason.Text.Trim();
            selectedApp.Status = cmbStatus.SelectedItem.ToString();

            bindingSource.ResetBindings(false);
            UpdateStatusCount();
            MessageBox.Show("Cập nhật lịch hẹn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvAppointments.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn lịch hẹn cần xóa trên bảng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa lịch hẹn này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                var selectedApp = (Appointment)dgvAppointments.CurrentRow.DataBoundItem;
                appointmentList.Remove(selectedApp);
                ClearInputs();
                UpdateStatusCount();
                MessageBox.Show("Đã xóa lịch hẹn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            txtAppointmentId.Clear();
            txtPatientName.Clear();
            txtPhoneNumber.Clear();
            txtReason.Clear();
            dtpAppointmentDate.Value = DateTime.Now;
            if (cmbDoctor.Items.Count > 0) cmbDoctor.SelectedIndex = -1;
            if (cmbStatus.Items.Count > 0) cmbStatus.SelectedIndex = -1;
            errorProvider1.Clear();
            txtAppointmentId.Enabled = true;
        }

        private void DgvAppointments_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var app = (Appointment)dgvAppointments.Rows[e.RowIndex].DataBoundItem;
                txtAppointmentId.Text = app.AppointmentId;
                txtAppointmentId.Enabled = false;
                txtPatientName.Text = app.PatientName;
                txtPhoneNumber.Text = app.PhoneNumber;
                dtpAppointmentDate.Value = app.AppointmentDate;
                cmbDoctor.SelectedItem = app.Doctor;
                txtReason.Text = app.Reason;
                cmbStatus.SelectedItem = app.Status;
            }
        }

        private void ApplyFilter()
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            string selectedStatus = cmbFilterStatus.SelectedIndex > 0 ? cmbFilterStatus.SelectedItem.ToString() : null;
            DateTime filterDate = dtpFilterDate.Value.Date;
            bool useDateFilter = chkEnableDateFilter.Checked;

            var filtered = appointmentList.Where(x =>
                (string.IsNullOrEmpty(keyword) || x.PatientName.ToLower().Contains(keyword) || x.PhoneNumber.Contains(keyword)) &&
                (selectedStatus == null || x.Status == selectedStatus) &&
                (!useDateFilter || x.AppointmentDate.Date == filterDate)
            ).ToList();

            bindingSource.DataSource = new BindingList<Appointment>(filtered);
            UpdateStatusCount();
        }

        private void MenuExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "ClinicAppointments.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("AppointmentId,PatientName,PhoneNumber,AppointmentDate,Doctor,Reason,Status");
                            foreach (var item in dgvAppointments.Rows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).Cast<Appointment>())
                            {
                                sw.WriteLine($"{item.AppointmentId},{item.PatientName},{item.PhoneNumber},{item.AppointmentDate:yyyy-MM-dd HH:mm},{item.Doctor},{item.Reason},{item.Status}");
                            }
                        }
                        MessageBox.Show("Xuất file CSV thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xuất file: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UpdateStatusCount()
        {
            lblStatusCount.Text = $" Tổng số lịch hẹn hiển thị: {dgvAppointments.Rows.Count}";
        }
    }
}
