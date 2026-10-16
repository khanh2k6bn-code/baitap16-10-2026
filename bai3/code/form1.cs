using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RepairServiceTracker
{
    public partial class Form1 : Form
    {
        private TableLayoutPanel tableMain;
        private Panel pnlInput;
        private FlowLayoutPanel flowSearch;
        private DataGridView dgvOrders;
        private MenuStrip menuStrip1;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblStatusInfo;

        private TextBox txtOrderId, txtCustomerName, txtPhoneNumber, txtCost, txtDescription;
        private ComboBox cmbDeviceType, cmbStatus;
        private DateTimePicker dtpReceiveDate;
        private PictureBox picDevice;
        private Button btnBrowseImage, btnAdd, btnUpdate, btnDelete, btnClear;

        private TextBox txtSearch;
        private ComboBox cmbFilterStatus;

        private ErrorProvider errorProvider1;
        private BindingList<RepairOrder> orderList;
        private BindingSource bindingSource;
        private string currentImagePath = string.Empty;

        public Form1()
        {
            InitializeComponentCustom();
            LoadInitialData();
        }

        private void InitializeComponentCustom()
        {
            this.Text = "Repair Service Tracker";
            this.Width = 1250;
            this.Height = 800;
            this.StartPosition = FormStartPosition.CenterScreen;

            errorProvider1 = new ErrorProvider();

            // 1. MenuStrip (Yêu cầu 6)
            menuStrip1 = new MenuStrip();
            ToolStripMenuItem menuFile = new ToolStripMenuItem("File");
            ToolStripMenuItem menuExport = new ToolStripMenuItem("Xuất danh sách ra CSV");
            ToolStripMenuItem menuExit = new ToolStripMenuItem("Thoát ứng dụng");
            menuExport.Click += MenuExport_Click;
            menuExit.Click += (s, e) => this.Close();
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuExport, new ToolStripSeparator(), menuExit });
            menuStrip1.Items.Add(menuFile);
            this.MainMenuStrip = menuStrip1;

            // 2. StatusStrip (Yêu cầu 6)
            statusStrip1 = new StatusStrip();
            lblStatusInfo = new ToolStripStatusLabel();
            statusStrip1.Items.Add(lblStatusInfo);

            // 3. TableLayoutPanel bố cục responsive (Yêu cầu 1)
            tableMain = new TableLayoutPanel();
            tableMain.Dock = DockStyle.Fill;
            tableMain.ColumnCount = 1;
            tableMain.RowCount = 3;
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 310)); // Khu vực nhập liệu & ảnh
            tableMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // Tìm kiếm & lọc
            tableMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // DataGridView

            // --- KHU VỰC NHẬP LIỆU (Yêu cầu 2) ---
            pnlInput = new Panel();
            pnlInput.Dock = DockStyle.Fill;
            pnlInput.BorderStyle = BorderStyle.FixedSingle;

            AddLabelAndControl(pnlInput, "Mã đơn:", txtOrderId = new TextBox(), 20, 20);
            AddLabelAndControl(pnlInput, "Tên khách hàng:", txtCustomerName = new TextBox(), 20, 60);
            AddLabelAndControl(pnlInput, "Số điện thoại:", txtPhoneNumber = new TextBox(), 20, 100);
            AddLabelAndControl(pnlInput, "Loại thiết bị:", cmbDeviceType = new ComboBox(), 20, 140);
            cmbDeviceType.Items.AddRange(new string[] { "Laptop", "Điện thoại", "Máy tính bảng", "Tivi", "Quạt điện/Điện gia dụng" });
            cmbDeviceType.DropDownStyle = ComboBoxStyle.DropDownList;

            AddLabelAndControl(pnlInput, "Ngày tiếp nhận:", dtpReceiveDate = new DateTimePicker { Format = DateTimePickerFormat.Short }, 20, 180);
            AddLabelAndControl(pnlInput, "Chi phí dự kiến:", txtCost = new TextBox(), 20, 220);

            AddLabelAndControl(pnlInput, "Trạng thái:", cmbStatus = new ComboBox(), 450, 20);
            cmbStatus.Items.AddRange(new string[] { "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy" });
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.SelectedIndex = 0;

            AddLabelAndControl(pnlInput, "Mô tả lỗi:", txtDescription = new TextBox { Multiline = true, Height = 60 }, 450, 60);

            // PictureBox hiển thị ảnh thiết bị (Yêu cầu 1, 4)
            Label lblPic = new Label { Text = "Ảnh thiết bị:", Left = 450, Top = 140, AutoSize = true };
            picDevice = new PictureBox
            {
                Left = 560,
                Top = 140,
                Width = 140,
                Height = 110,
                SizeMode = PictureBoxSizeMode.Zoom, // Hiển thị đúng tỷ lệ, không méo (Yêu cầu 4)
                BorderStyle = BorderStyle.FixedSingle
            };
            btnBrowseImage = new Button { Text = "Chọn ảnh...", Left = 715, Top = 220, Width = 95, Height = 30 };
            btnBrowseImage.Click += BtnBrowseImage_Click;

            // Các nút chức năng (Yêu cầu 4)
            btnAdd = new Button { Text = "Thêm", Left = 850, Top = 20, Width = 110, Height = 32 };
            btnUpdate = new Button { Text = "Cập nhật", Left = 850, Top = 65, Width = 110, Height = 32 };
            btnDelete = new Button { Text = "Xóa", Left = 850, Top = 110, Width = 110, Height = 32 };
            btnClear = new Button { Text = "Làm mới", Left = 850, Top = 155, Width = 110, Height = 32 };

            btnAdd.Click += BtnAdd_Click;
            btnUpdate.Click += BtnUpdate_Click;
            btnDelete.Click += BtnDelete_Click;
            btnClear.Click += BtnClear_Click;

            pnlInput.Controls.AddRange(new Control[] { lblPic, picDevice, btnBrowseImage, btnAdd, btnUpdate, btnDelete, btnClear });

            // --- KHU VỰC TÌM KIẾM VÀ LỌC (Yêu cầu 4) ---
            flowSearch = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(10) };
            txtSearch = new TextBox { Width = 220 };
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            cmbFilterStatus = new ComboBox { Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbFilterStatus.Items.Add("Tất cả trạng thái");
            cmbFilterStatus.Items.AddRange(new string[] { "Mới tiếp nhận", "Đang sửa", "Chờ linh kiện", "Hoàn tất", "Đã hủy" });
            cmbFilterStatus.SelectedIndex = 0;
            cmbFilterStatus.SelectedIndexChanged += (s, e) => ApplyFilter();

            flowSearch.Controls.AddRange(new Control[] {
                new Label { Text = "Tìm kiếm (Mã/Tên/SĐT):", AutoSize = true, Padding = new Padding(0, 5, 0, 0) }, txtSearch,
                new Label { Text = "Lọc trạng thái:", AutoSize = true, Padding = new Padding(15, 5, 0, 0) }, cmbFilterStatus
            });

            // --- DATAGRIDVIEW (Yêu cầu 3) ---
            dgvOrders = new DataGridView();
            dgvOrders.Dock = DockStyle.Fill;
            dgvOrders.AutoGenerateColumns = false;
            dgvOrders.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvOrders.ReadOnly = true;
            dgvOrders.MultiSelect = false;
            dgvOrders.CellClick += DgvOrders_CellClick;

            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "OrderId", HeaderText = "Mã Đơn", Width = 90 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "CustomerName", HeaderText = "Khách Hàng", Width = 150 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "PhoneNumber", HeaderText = "Số Điện Thoại", Width = 110 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "DeviceType", HeaderText = "Loại Thiết Bị", Width = 130 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Description", HeaderText = "Mô Tả Lỗi", Width = 180 });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ReceiveDate", HeaderText = "Ngày Tiếp Nhận", Width = 110, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd/MM/yyyy" } });
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "EstimatedCost", HeaderText = "Chi Phí Dự Kiến", Width = 130, DefaultCellStyle = new DataGridViewCellStyle { Format = "N0" } }); // Định dạng N0 (Yêu cầu 3)
            dgvOrders.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "Trạng Thái", Width = 120 });

            tableMain.Controls.Add(pnlInput, 0, 0);
            tableMain.Controls.Add(flowSearch, 0, 1);
            tableMain.Controls.Add(dgvOrders, 0, 2);

            this.Controls.Add(tableMain);
            this.Controls.Add(statusStrip1);
            this.Controls.Add(menuStrip1);
        }

        private void AddLabelAndControl(Panel panel, string labelText, Control control, int left, int top)
        {
            Label lbl = new Label { Text = labelText, Left = left, Top = top + 3, Width = 105, AutoSize = true };
            control.Left = left + 110;
            control.Top = top;
            control.Width = 180;
            panel.Controls.Add(lbl);
            panel.Controls.Add(control);
        }

        private void LoadInitialData()
        {
            orderList = new BindingList<RepairOrder>
            {
                new RepairOrder { OrderId = "DH001", CustomerName = "Lê Văn Hùng", PhoneNumber = "0901234567", DeviceType = "Laptop", Description = "Không lên nguồn, chập cháy", ReceiveDate = DateTime.Now.AddDays(-2), EstimatedCost = 500000, Status = "Đang sửa", ImagePath = "" },
                new RepairOrder { OrderId = "DH002", CustomerName = "Phạm Thị Mai", PhoneNumber = "0988888888", DeviceType = "Điện thoại", Description = "Vỡ màn hình cảm ứng", ReceiveDate = DateTime.Now.AddDays(-1), EstimatedCost = 1200000, Status = "Mới tiếp nhận", ImagePath = "" }
            };

            bindingSource = new BindingSource { DataSource = orderList };
            dgvOrders.DataSource = bindingSource;
            UpdateStatusSummary();
        }

        // Chọn ảnh bằng OpenFileDialog (Yêu cầu 4)
        private void BtnBrowseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    currentImagePath = ofd.FileName;
                    picDevice.Image = Image.FromFile(currentImagePath);
                }
            }
        }

        // --- KIỂM TRA DỮ LIỆU & RÀNG BUỘC (Yêu cầu 5) ---
        private bool ValidateData(bool isNew)
        {
            bool isValid = true;
            errorProvider1.Clear();

            if (string.IsNullOrWhiteSpace(txtOrderId.Text))
            {
                errorProvider1.SetError(txtOrderId, "Mã đơn không được để trống!");
                isValid = false;
            }
            else if (isNew && orderList.Any(x => x.OrderId == txtOrderId.Text.Trim()))
            {
                errorProvider1.SetError(txtOrderId, "Mã đơn không được trùng lặp!");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                errorProvider1.SetError(txtCustomerName, "Tên khách hàng không được để trống!");
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtPhoneNumber.Text))
            {
                errorProvider1.SetError(txtPhoneNumber, "Số điện thoại không được để trống!");
                isValid = false;
            }

            if (cmbDeviceType.SelectedIndex == -1)
            {
                errorProvider1.SetError(cmbDeviceType, "Vui lòng chọn loại thiết bị!");
                isValid = false;
            }

            // Mô tả lỗi bắt buộc (Yêu cầu 5)
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                errorProvider1.SetError(txtDescription, "Mô tả lỗi là bắt buộc!");
                isValid = false;
            }

            // Chi phí dự kiến phải là số không âm (Yêu cầu 5)
            if (!decimal.TryParse(txtCost.Text.Trim(), out decimal cost) || cost < 0)
            {
                errorProvider1.SetError(txtCost, "Chi phí dự kiến phải là số lớn hơn hoặc bằng 0!");
                isValid = false;
            }

            return isValid;
        }

        // --- CHỨC NĂNG THÊM (Yêu cầu 4) ---
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateData(true)) return;

            var newOrder = new RepairOrder
            {
                OrderId = txtOrderId.Text.Trim(),
                CustomerName = txtCustomerName.Text.Trim(),
                PhoneNumber = txtPhoneNumber.Text.Trim(),
                DeviceType = cmbDeviceType.SelectedItem.ToString(),
                Description = txtDescription.Text.Trim(),
                ReceiveDate = dtpReceiveDate.Value,
                EstimatedCost = decimal.Parse(txtCost.Text.Trim()),
                Status = cmbStatus.SelectedItem.ToString(),
                ImagePath = currentImagePath
            };

            orderList.Add(newOrder);
            ClearInputs();
            UpdateStatusSummary();
            MessageBox.Show("Thêm đơn sửa chữa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- CHỨC NĂNG CẬP NHẬT (Yêu cầu 4) ---
        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần cập nhật trên bảng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateData(false)) return;

            var selected = (RepairOrder)dgvOrders.CurrentRow.DataBoundItem;
            selected.CustomerName = txtCustomerName.Text.Trim();
            selected.PhoneNumber = txtPhoneNumber.Text.Trim();
            selected.DeviceType = cmbDeviceType.SelectedItem.ToString();
            selected.Description = txtDescription.Text.Trim();
            selected.ReceiveDate = dtpReceiveDate.Value;
            selected.EstimatedCost = decimal.Parse(txtCost.Text.Trim());
            selected.Status = cmbStatus.SelectedItem.ToString();
            selected.ImagePath = currentImagePath;

            bindingSource.ResetBindings(false);
            UpdateStatusSummary();
            MessageBox.Show("Cập nhật đơn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // --- CHỨC NĂNG XÓA CÓ XÁC NHẬN (Yêu cầu 4) ---
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (dgvOrders.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn đơn cần xóa!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show("Bạn có chắc chắn muốn xóa đơn sửa chữa này không?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                var selected = (RepairOrder)dgvOrders.CurrentRow.DataBoundItem;
                orderList.Remove(selected);
                ClearInputs();
                UpdateStatusSummary();
                MessageBox.Show("Đã xóa thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e) => ClearInputs();

        private void ClearInputs()
        {
            txtOrderId.Clear();
            txtCustomerName.Clear();
            txtPhoneNumber.Clear();
            txtDescription.Clear();
            txtCost.Clear();
            dtpReceiveDate.Value = DateTime.Now;
            if (cmbDeviceType.Items.Count > 0) cmbDeviceType.SelectedIndex = -1;
            cmbStatus.SelectedIndex = 0;
            picDevice.Image = null;
            currentImagePath = string.Empty;
            txtOrderId.Enabled = true;
            errorProvider1.Clear();
        }

        // Chọn dòng nạp lên Form (Yêu cầu 3)
        private void DgvOrders_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var item = (RepairOrder)dgvOrders.Rows[e.RowIndex].DataBoundItem;
                txtOrderId.Text = item.OrderId;
                txtOrderId.Enabled = false; // Khóa mã khi sửa
                txtCustomerName.Text = item.CustomerName;
                txtPhoneNumber.Text = item.PhoneNumber;
                cmbDeviceType.SelectedItem = item.DeviceType;
                txtDescription.Text = item.Description;
                dtpReceiveDate.Value = item.ReceiveDate;
                txtCost.Text = item.EstimatedCost.ToString("0");
                cmbStatus.SelectedItem = item.Status;

                currentImagePath = item.ImagePath;
                if (!string.IsNullOrEmpty(currentImagePath) && File.Exists(currentImagePath))
                {
                    picDevice.Image = Image.FromFile(currentImagePath);
                }
                else
                {
                    picDevice.Image = null;
                }
            }
        }

        // --- TÌM KIẾM VÀ LỌC TRẠNG THÁI (Yêu cầu 4) ---
        private void ApplyFilter()
        {
            string keyword = txtSearch.Text.Trim().ToLower();
            string statusFilter = cmbFilterStatus.SelectedIndex > 0 ? cmbFilterStatus.SelectedItem.ToString() : null;

            var filtered = orderList.Where(x =>
                (string.IsNullOrEmpty(keyword) || x.OrderId.ToLower().Contains(keyword) || x.CustomerName.ToLower().Contains(keyword) || x.PhoneNumber.Contains(keyword)) &&
                (statusFilter == null || x.Status == statusFilter)
            ).ToList();

            bindingSource.DataSource = new BindingList<RepairOrder>(filtered);
            UpdateStatusSummary();
        }

        // --- XUẤT CSV (Yêu cầu 6) ---
        private void MenuExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "RepairOrders.csv" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine("OrderId,CustomerName,PhoneNumber,DeviceType,Description,ReceiveDate,EstimatedCost,Status");
                            foreach (var item in dgvOrders.Rows.Cast<DataGridViewRow>().Select(r => r.DataBoundItem).Cast<RepairOrder>())
                            {
                                sw.WriteLine($"{item.OrderId},{item.CustomerName},{item.PhoneNumber},{item.DeviceType},\"{item.Description}\",{item.ReceiveDate:yyyy-MM-dd},{item.EstimatedCost},{item.Status}");
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

        // --- HIỂN THỊ STATUSSTRIP (Yêu cầu 6) ---
        // Tổng số đơn theo bộ lọc VÀ tổng chi phí dự kiến của các đơn CHƯA HOÀN TẤT
        private void UpdateStatusSummary()
        {
            int totalFiltered = dgvOrders.Rows.Count;
            // Tổng chi phí dự kiến các đơn chưa hoàn tất (trạng thái khác "Hoàn tất" và "Đã hủy")
            decimal totalUncompletedCost = orderList
                .Where(x => x.Status != "Hoàn tất" && x.Status != "Đã hủy")
                .Sum(x => x.EstimatedCost);

            lblStatusInfo.Text = $" Số đơn hiển thị: {totalFiltered} | Tổng chi phí dự kiến (chưa hoàn tất): {totalUncompletedCost:N0} VNĐ";
        }
    }
}
