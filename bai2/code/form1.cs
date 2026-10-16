using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace LabEquipmentCheckout
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel tableMain;
        private Panel pnlInput;
        private FlowLayoutPanel flowSearch;
        private DataGridView dgvLoans;
        private MenuStrip menuStrip1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblStatusInfo;

        private TextBox txtLoanId, txtStudentId, txtStudentName, txtQuantity;
        private ComboBox cmbEquipment, cmbStatus;
        private DateTimePicker dtpBorrowDate, dtpDueDate, dtpReturnDate;
        private Button btnAdd, btnUpdate, btnDelete, btnClear;

        private TextBox txtSearch;
        private ComboBox cmbFilterStatus;

        private ErrorProvider errorProvider1;

        private BindingList<EquipmentLoan> loanList;
        private BindingSource bindingSource;

        public Form1()
        {
            InitializeComponentCustom();
            LoadInitialData();
            CheckOverdueStatus();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Lab Equipment Checkout Manager";
            this.Width = 1200;
            this.Height = 750;
            this.StartPosition = FormStartPosition.CenterScreen;

            errorProvider1 = new ErrorProvider();

            // 1. MenuStrip
            menuStrip1 = new MenuStrip();
            ToolStripMenuItem menuFile = new ToolStripMenuItem("File");
            ToolStripMenuItem menuExport = new ToolStripMenuItem("Xuất danh sách lọc ra CSV");
            menuExport.Click += MenuExport_Click;
            menuFile.DropDownItems.Add(menuExport);
            menuStrip1.Items.Add(menuFile);
            this.MainMenuStrip = menuStrip1;

            // 2. StatusStrip
            statusStrip1 = new StatusStrip();
            lblStatusInfo = new ToolStripStatusLabel();
            statusStrip1.Items.Add(lblStatusInfo);

            // 3. TableLayoutPanel co giãn
            tableMain = new TableLayoutPanel();
            tableMain.Dock = DockStyle.Fill;
            tableMain.ColumnCount = 1;
            tableMain.RowCount = 3;
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // --- KHU VỰC NHẬP LIỆU ---
            pnlInput = new Panel();
            pnlInput.Dock = DockStyle.Fill;
            pnlInput.BorderStyle = BorderStyle.FixedSingle;

            AddLabelAndControl(pnlInput, "Mã phiếu:", txtLoanId = new TextBox(), 20, 20);
            AddLabelAndControl(pnlInput, "Mã sinh viên:", txtStudentId = new TextBox(), 20, 60);
            AddLabelAndControl(pnlInput, "Họ tên SV:", txtStudentName = new TextBox(), 20, 100);
            AddLabelAndControl(pnlInput, "Số lượng:", txtQuantity = new TextBox(), 20, 140);

            AddLabelAndControl(pnlInput, "Thiết bị:", cmbEquipment = new ComboBox(), 440, 20);
            cmbEquipment.Items.AddRange(new string[] { "Máy hiện sóng (Oscilloscope)", "Bộ nguồn DC", "Máy hàn điện tử", "Đồng hồ vạn năng số", "Kit vi điều khiển ARM" });
            cmbEquipment.DropDownStyle = ComboBoxStyle.DropDownList;

            AddLabelAndControl(pnlInput, "Ngày mượn:", dtpBorrowDate = new DateTimePicker { Format = DateTimePickerFormat.Short }, 440, 60);
            AddLabelAndControl(pnlInput, "Hạn trả:", dtpDueDate = new DateTimePicker { Format = DateTimePickerFormat.Short }, 440, 100);
            AddLabelAndControl(pnlInput, "Ngày trả thực tế:", dtpReturnDate = new DateTimePicker { Format = DateTimePickerFormat.Short }, 440, 140);

            AddLabelAndControl(pnlInput, "Trạng thái:", cmbStatus = new ComboBox(), 860, 20);
            cmbStatus.Items.AddRange(new string[] { "Đang mượn", "Đã trả", "Quá hạn" });
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.SelectedIndex = 0;

            btnAdd = new Button { Text = "Thêm", Left = 860, Top = 80, Width = 85, Height = 30 };
            btnUpdate = new Button { Text = "Cập nhật", Left = 955, Top = 80, Width = 85, Height = 30 };
            btnDelete = new Button { Text = "Xóa", Left = 860, Top = 120, Width = 85, Height = 30 };
            btnClear = new Button { Text = "Làm mới", Left = 955, Top = 120, Width = 85, Height = 30 };

            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click += BtnClear_Click;

            pnlInput.Controls.AddRange(new Control[] { btnAdd, btnUpdate, btnDelete, btnClear });

            // --- KHU VỰC TÌM KIẾM VÀ LỌC ---
            flowSearch = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10) };

            txtSearch = new TextBox { Width = 220 };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cmbFilterStatus = new ComboBox { Width = 140, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterStatus.Items.Add("Tất cả trạng thái");
            cmbFilterStatus.Items.AddRange(new string[] { "Đang mượn", "Đã trả", "Quá hạn" });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += (s, e) => ApplyFilter();

            flowSearch.Controls.AddRange(new Control[] {
                new Label { Text = "Tìm kiếm (Mã SV/Thiết bị):", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, txtSearch,
                new Label { Text = "Lọc trạng thái:", AutoSize = true, Padding = new Padding(15, 5, 0, 0) }, cmbFilterStatus
            });

            // --- DATAGRIDVIEW ---
            dgvLoans = new DataGridView();
            dgvLoans.Dock = DockStyle.Fill;
            dgvLoans.AutoGenerateColumns = false;
            dgvLoans.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvLoans.ReadOnly = true;
            dgvLoans.MultiSelect = false;
            dgvLoans.CellClick += DgvLoans_CellClick;

            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "LoanId", HeaderText = "Mã Phiếu", Width = 90 });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StudentId", HeaderText = "Mã SV", Width = 100 });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "StudentName", HeaderText = "Họ Tên Sinh Viên", Width = 160 });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EquipmentName", HeaderText = "Thiết Bị", Width = 180 });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Quantity", HeaderText = "Số Lượng", Width = 80 });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "BorrowDate", HeaderText = "Ngày Mượn", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DueDate", HeaderText = "Hạn Trả", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReturnDate", HeaderText = "Ngày Trả Thực Tế", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvLoans.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Trạng Thái", Width = 110 });

            tableMain.Controls.Add(pnlInput, 0, 0);
            tableMain.Controls.Add(flowSearch, 0, 1);
            tableMain.Controls.Add(dgvLoans, 0, 2);

            this.Controls.Add(tableMain);
            this.Controls.Add(statusStrip1);
            this.Controls.Add(menuStrip1);
        }

        private void AddLabelAndControl(Panel panel, string labelText, Control control, int left, int top)
        {
            Label lbl = new Label { Text = labelText, Left = left, Top = top + 3, Width = 90, AutoSize = true };
            control.Left = left + 95;
            control.Top = top;
            control.Width = 165;
            panel.Controls.Add(lbl);
            panel.Controls.Add(control);
        }

        private void LoadInitialData()
        {
            loanList = new BindingList<EquipmentLoan>
            {
                new EquipmentLoan { LoanId = "P001", StudentId = "B22DCCN001", StudentName = "Nguyễn Văn An", EquipmentName = "Máy hiện sóng (Oscilloscope)", Quantity = 1, BorrowDate = DateTime.Now.AddDays(-5), DueDate = DateTime.Now.AddDays(-1), ReturnDate = null, Status = "Đang mượn" },
                new EquipmentLoan { LoanId = "P002", StudentId = "B22DCCN002", StudentName = "Trần Thị Bình", EquipmentName = "Đồng hồ vạn năng số", Quantity = 2, BorrowDate = DateTime.Now.AddDays(-2), DueDate = DateTime.Now.AddDays(3), ReturnDate = null, Status = "Đang mượn" }
            };

            bindingSource = new BindingSource { DataSource = loanList };
            dgvLoans.DataSource = bindingSource;
            UpdateStatusSummary();
        }

        private void CheckOverdueStatus()
        {
            foreach (var item in loanList)
            {
                if (item.Status == "Đang mượn" && item.DueDate.Date < DateTime.Now.Date)
                {
                    item.Status = "Quá hạn";
                }
            }
            if (bindingSource != null) bindingSource.ResetBindings(false);
        }

        private bool ValidateData(bool isNew)
        {
            bool isValid = true;
            errorProvider1.Clear();

            if (string.IsNullOrWhiteSpace(txtLoanId.Text))
            {
                errorProvider1.SetError(txtLoanId, "Mã phiếu không được để trống!");
                isValid = false;
            }
            else if (isNew && loanList.Any(x => x.LoanId == txtLoanId.Text.Trim()))
            {
                errorProvider1.SetError(txtLoanId, "Mã phiếu không được trùng lặp!");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtStudentId.Text))
            {
                errorProvider1.SetError(txtStudentId, "Mã sinh viên không được để trống!");
                isValid = false;
            }

            if (cmbEquipment.SelectedIndex == -1)
            {
                errorProvider1.SetError(cmbEquipment, "Vui lòng chọn thiết bị mượn!");
                isValid = false;
            }

            if (!int.TryParse(txtQuantity.Text.Trim(), out int qty) || qty <= 0)
            {
                errorProvider1.SetError(txtQuantity, "Số lượng phải là số nguyên lớn hơn 0!");
                isValid = false;
            }

            if (dtpDueDate.Value.Date < dtpBorrowDate.Value.Date)
            {
                errorProvider1.SetError(dtpDueDate, "Hạn trả không được trước ngày mượn!");
                isValid = false;
            }

            return isValid;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateData(true)) return;

            string status = cmbStatus.SelectedItem.ToString();
            if (status == "Đang mượn" && dtpDueDate.Value.Date < DateTime.Now.Date)
            {
                status = "Quá hạn";
            }

            var newLoan = new EquipmentLoan
            {
                LoanId = txtLoanId.Text.Trim(),
                StudentId = txtStudentId.Text.Trim(),
                StudentName = txtStudentName.Text.Trim(),
                EquipmentName = cmbEquipment.SelectedItem.ToString(),
                Quantity = int.Parse(txtQuantity.Text.Trim()),
                BorrowDate = dtpBorrowDate.Value,
                DueDate = dtpDueDate.Value,
                ReturnDate = status == "Đã trả" ? (DateTime?)dtpReturnDate.Value : null,
                Status = status
            };

            loanList.Add(newLoan);
            ClearInputs();
            UpdateStatusSummary();
            MessageBox.Show("Thêm phiếu mượn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvLoans.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu mượn cần cập nhật!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateData(false)) return;

            var selected = (EquipmentLoan)dgvLoans.CurrentRow.DataBoundItem;
            selected.StudentId = txtStudentId.Text.Trim();
            selected.StudentName = txtStudentName.Text.Trim();
            selected.EquipmentName = cmbEquipment.SelectedItem.ToString();
            selected.Quantity = int.Parse(txtQuantity.Text.Trim());
            selected.BorrowDate = dtpBorrowDate.Value;
            selected.DueDate = dtpDueDate.Value;

            string status = cmbStatus.SelectedItem.ToString();
            if (status == "Đã trả")
            {
                selected.ReturnDate = dtpReturnDate.Value;
                selected.Status = "Đã trả";
            }
            else
            {
                selected.ReturnDate = null;
                selected.Status = dtpDueDate.Value.Date < DateTime.Now.Date ? "Quá hạn" : "Đang mượn";
            }

            bindingSource.ResetBindings(false);
            UpdateStatusSummary();
            MessageBox.Show("Cập nhật phiếu thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvLoans.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn phiếu mượn cần xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa phiếu mượn này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                var selected = (EquipmentLoan)dgvLoans.CurrentRow.DataBoundItem;
                loanList.Remove(selected);
                ClearInputs();
                UpdateStatusSummary();
                MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e) => ClearInputs();

        private void ClearInputs()
        {
            txtLoanId.Clear();
            txtStudentId.Clear();
            txtStudentName.Clear();
            txtQuantity.Clear();
            dtpBorrowDate.Value = DateTime.Now;
            dtpDueDate.Value = DateTime.Now;
            if (cmbEquipment.Items.Count > 0) cmbEquipment.SelectedIndex = -1;
            cmbStatus.SelectedIndex = 0;
            txtLoanId.Enabled = true;
            errorProvider1.Clear();
        }

        private void DgvLoans_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var item = (EquipmentLoan)dgvLoans.Rows[e.RowIndex].DataBoundItem;
                txtLoanId.Text = item.LoanId;
                txtLoanId.Enabled = false;
                txtStudentId.Text = item.StudentId;
                txtStudentName.Text = item.StudentName;
                cmbEquipment.SelectedItem = item.EquipmentName;
                txtQuantity.Text = item.Quantity.ToString();
                dtpBorrowDate.Value = item.BorrowDate;
                dtpDueDate.Value = item.DueDate;
                if (item.ReturnDate.HasValue) dtpReturnDate.Value = item.ReturnDate.Value;
                cmbStatus.SelectedItem = item.Status == "Quá hạn" ? "Đang mượn" : item.Status;
            }
        }

        private void ApplyFilter()
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            string statusFilter = cmbFilterStatus.SelectedIndex > 0 ? cmbFilterStatus.SelectedItem.ToString() : null;

            var filtered = loanList.Where(x =>
                (string.IsNullOrEmpty(keyword) || x.StudentId.ToLower().Contains(keyword) || x.EquipmentName.ToLower().Contains(keyword)) &&
                (statusFilter == null || x.Status == statusFilter)
            ).ToList();

            bindingSource.DataSource = new BindingList<EquipmentLoan>(filtered);
            UpdateStatusSummary();
        }

        private void MenuExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "EquipmentLoans.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("LoanId,StudentId,StudentName,EquipmentName,Quantity,BorrowDate,DueDate,ReturnDate,Status");
                            foreach (var item in dgvLoans.Rows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).Cast<EquipmentLoan>())
                            {
                                string retDate = item.ReturnDate.HasValue ? item.ReturnDate.Value.ToString("yyyy-MM-dd") : "";
                                sw.WriteLine($"{item.LoanId},{item.StudentId},{item.StudentName},{item.EquipmentName},{item.Quantity},{item.BorrowDate:yyyy-MM-dd},{item.DueDate:yyyy-MM-dd},{retDate},{item.Status}");
                            }
                        }
                        MessageBox.Show("Xuất file CSV thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void UpdateStatusSummary()
        {
            int total = dgvLoans.Rows.Count;
            int overdueCount = loanList.Count(x => x.Status == "Quá hạn");
            lblStatusInfo.Text = $" Tổng số phiếu hiển thị: {total} | Số phiếu quá hạn tổng hệ thống: {overdueCount}";
        }
    }
}
