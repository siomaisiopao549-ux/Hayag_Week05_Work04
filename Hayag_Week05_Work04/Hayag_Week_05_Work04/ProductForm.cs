using System.Globalization;

namespace ProductFormApp;

public class ProductForm : Form
{
    // ---------- Controls ----------
    private readonly Label lblBarcode = new() { Text = "Barcode" };
    private readonly Label lblName = new() { Text = "Name" };
    private readonly Label lblDescription = new() { Text = "Description" };
    private readonly Label lblCategory = new() { Text = "Category" };
    private readonly Label lblOrigin = new() { Text = "Origin" };
    private readonly Label lblStatus = new() { Text = "Status" };
    private readonly Label lblCost = new() { Text = "Cost" };
    private readonly Label lblPrice = new() { Text = "Price" };
    private readonly Label lblMarkup = new() { Text = "Markup (%)" };
    private readonly Label lblBarcodeStatus = new();

    private readonly TextBox txtBarcode = new() { MaxLength = 13 };
    private readonly TextBox txtName = new() { MaxLength = 50 };
    private readonly TextBox txtDescription = new()
    {
        Multiline = true,
        ScrollBars = ScrollBars.Vertical,
        AcceptsReturn = true,
        MaxLength = 250
    };
    private readonly ComboBox cboCategory = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly GroupBox grpOrigin = new() { Text = "Manufactured" };
    private readonly RadioButton rdoLocal = new() { Text = "Local" };
    private readonly RadioButton rdoAbroad = new() { Text = "Abroad" };
    private readonly CheckBox chkActive = new() { Text = "Active", Checked = true };
    private readonly TextBox txtCost = new();
    private readonly TextBox txtPrice = new();
    private readonly TextBox txtMarkup = new() { ReadOnly = true, TabStop = false };
    private readonly Button btnCancel = new() { Text = "Cancel" };
    private readonly Button btnClear = new() { Text = "Clear" };
    private readonly Button btnSave = new() { Text = "Save" };
    private readonly ErrorProvider errorProvider = new();

    // ---------- Data ----------
    private readonly HashSet<string> _barcodes = new();
    private readonly string _csvPath =
        Path.Combine(AppContext.BaseDirectory, "products.csv");

    /// <summary>Raised after a product is saved so the dashboard can refresh.</summary>
    public event EventHandler? ProductSaved;

    public ProductForm()
    {
        BuildLayout();
        WireEvents();
        LoadCategories();
        LoadExistingBarcodes();
        UpdateBarcodeStatus();
    }

    // =====================================================
    //  UI LAYOUT (matches the Product Form in the slides)
    // =====================================================
    private void BuildLayout()
    {
        Text = "Product Form";
        ClientSize = new Size(390, 440);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = SystemColors.Control;

        errorProvider.ContainerControl = this;
        errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;

        Place(lblBarcode, 12, 18);
        Place(txtBarcode, 105, 14, 265, 23);
        lblBarcodeStatus.SetBounds(105, 40, 265, 16);
        lblBarcodeStatus.Font = new Font(Font, FontStyle.Italic);

        Place(lblName, 12, 66);
        Place(txtName, 105, 62, 265, 23);

        Place(lblDescription, 12, 94);
        Place(txtDescription, 105, 90, 265, 70);

        Place(lblCategory, 12, 172);
        Place(cboCategory, 105, 168, 265, 23);

        Place(lblOrigin, 12, 204);
        grpOrigin.SetBounds(105, 198, 265, 52);
        rdoLocal.SetBounds(18, 20, 70, 22);
        rdoAbroad.SetBounds(120, 20, 80, 22);
        grpOrigin.Controls.AddRange(new Control[] { rdoLocal, rdoAbroad });

        Place(lblStatus, 12, 268);
        chkActive.SetBounds(105, 264, 100, 22);

        Place(lblCost, 12, 304);
        Place(txtCost, 12, 324, 100, 23);
        Place(lblPrice, 122, 304);
        Place(txtPrice, 122, 324, 100, 23);
        Place(lblMarkup, 260, 304);
        Place(txtMarkup, 260, 324, 110, 23);

        btnCancel.SetBounds(12, 380, 100, 32);
        btnClear.SetBounds(122, 380, 100, 32);
        btnSave.SetBounds(270, 380, 100, 32);
        btnSave.Font = new Font(Font, FontStyle.Bold);

        Controls.AddRange(new Control[]
        {
            lblBarcode, txtBarcode, lblBarcodeStatus,
            lblName, txtName, lblDescription, txtDescription,
            lblCategory, cboCategory, lblOrigin, grpOrigin,
            lblStatus, chkActive,
            lblCost, txtCost, lblPrice, txtPrice, lblMarkup, txtMarkup,
            btnCancel, btnClear, btnSave
        });

        AcceptButton = btnSave;   // ENTER = Save
        CancelButton = btnCancel; // ESC   = Cancel
    }

    private static void Place(Control c, int x, int y, int w = 0, int h = 0)
    {
        c.Location = new Point(x, y);
        if (w > 0 && h > 0) c.Size = new Size(w, h);
        else c.AutoSize = true;
    }

    // =====================================================
    //  EVENTS
    // =====================================================
    private void WireEvents()
    {
        // Barcode: digits only + live availability check
        txtBarcode.KeyPress += DigitsOnly_KeyPress;
        txtBarcode.TextChanged += (_, _) => { errorProvider.SetError(txtBarcode, ""); UpdateBarcodeStatus(); };

        // Clear error icons as the user fixes the fields
        txtName.TextChanged += (_, _) => errorProvider.SetError(txtName, "");
        cboCategory.SelectedIndexChanged += (_, _) => errorProvider.SetError(cboCategory, "");
        rdoLocal.CheckedChanged += (_, _) => errorProvider.SetError(grpOrigin, "");
        rdoAbroad.CheckedChanged += (_, _) => errorProvider.SetError(grpOrigin, "");

        // Money fields: numbers only + auto-compute markup
        txtCost.KeyPress += Decimal_KeyPress;
        txtPrice.KeyPress += Decimal_KeyPress;
        txtCost.TextChanged += (_, _) => { errorProvider.SetError(txtCost, ""); ComputeMarkup(); };
        txtPrice.TextChanged += (_, _) => { errorProvider.SetError(txtPrice, ""); ComputeMarkup(); };
        txtCost.Leave += (_, _) => FormatMoney(txtCost);
        txtPrice.Leave += (_, _) => FormatMoney(txtPrice);

        // Buttons
        btnSave.Click += BtnSave_Click;
        btnClear.Click += BtnClear_Click;
        btnCancel.Click += BtnCancel_Click;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (!ValidateInputs()) return;

        decimal cost = ParseDecimal(txtCost.Text);
        decimal price = ParseDecimal(txtPrice.Text);
        decimal markup = cost == 0 ? 0 : (price - cost) / cost * 100m;
        string origin = rdoLocal.Checked ? "Local" : "Abroad";
        string status = chkActive.Checked ? "Active" : "Inactive";

        try
        {
            SaveToCsv(origin, status, cost, price, markup);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not save the product.\n\n{ex.Message}",
                "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _barcodes.Add(txtBarcode.Text.Trim());
        ProductSaved?.Invoke(this, EventArgs.Empty);

        MessageBox.Show(
            $"Product saved successfully!\n\n" +
            $"Barcode:  {txtBarcode.Text.Trim()}\n" +
            $"Name:     {txtName.Text.Trim()}\n" +
            $"Category: {cboCategory.Text}\n" +
            $"Origin:   {origin}\n" +
            $"Status:   {status}\n" +
            $"Cost:     {cost:N2}\n" +
            $"Price:    {price:N2}\n" +
            $"Markup:   {markup:N2}%\n" +
            $"Profit:   {(price - cost):N2}",
            "Product Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

        ClearForm();
    }

    private void BtnClear_Click(object? sender, EventArgs e)
    {
        if (HasInput())
        {
            var r = MessageBox.Show("Clear all fields?", "Confirm Clear",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r != DialogResult.Yes) return;
        }
        ClearForm();
    }

    private void BtnCancel_Click(object? sender, EventArgs e)
    {
        if (HasInput())
        {
            var r = MessageBox.Show("Discard your changes and close the form?",
                "Confirm Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (r != DialogResult.Yes) return;
        }
        Close();
    }

    // =====================================================
    //  VALIDATION
    // =====================================================
    private bool ValidateInputs()
    {
        errorProvider.Clear();
        Control? firstInvalid = null;

        void Fail(Control c, string msg)
        {
            errorProvider.SetError(c, msg);
            firstInvalid ??= c;
        }

        string barcode = txtBarcode.Text.Trim();
        if (barcode.Length == 0) Fail(txtBarcode, "Barcode is required.");
        else if (!IsValidBarcodeFormat(barcode)) Fail(txtBarcode, "Barcode must be 8 to 13 digits.");
        else if (_barcodes.Contains(barcode)) Fail(txtBarcode, "This barcode already exists.");

        string name = txtName.Text.Trim();
        if (name.Length == 0) Fail(txtName, "Name is required.");
        else if (name.Length < 2) Fail(txtName, "Name must be at least 2 characters.");

        if (cboCategory.SelectedIndex < 0) Fail(cboCategory, "Please select a category.");

        if (!rdoLocal.Checked && !rdoAbroad.Checked) Fail(grpOrigin, "Please choose Local or Abroad.");

        bool costOk = TryParseDecimal(txtCost.Text, out decimal cost);
        if (!costOk) Fail(txtCost, "Enter a valid cost.");
        else if (cost <= 0) Fail(txtCost, "Cost must be greater than 0.");

        bool priceOk = TryParseDecimal(txtPrice.Text, out decimal price);
        if (!priceOk) Fail(txtPrice, "Enter a valid price.");
        else if (price <= 0) Fail(txtPrice, "Price must be greater than 0.");
        else if (costOk && price < cost) Fail(txtPrice, "Price cannot be lower than cost.");

        if (firstInvalid != null)
        {
            firstInvalid.Focus();
            MessageBox.Show("Please fix the highlighted fields.", "Validation",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
        return true;
    }

    private static bool IsValidBarcodeFormat(string s) =>
        s.Length is >= 8 and <= 13 && s.All(char.IsDigit);

    private void UpdateBarcodeStatus()
    {
        string b = txtBarcode.Text.Trim();
        if (b.Length == 0)
        {
            lblBarcodeStatus.Text = "";
        }
        else if (!IsValidBarcodeFormat(b))
        {
            lblBarcodeStatus.ForeColor = Color.Firebrick;
            lblBarcodeStatus.Text = "Barcode must be 8 to 13 digits.";
        }
        else if (_barcodes.Contains(b))
        {
            lblBarcodeStatus.ForeColor = Color.Firebrick;
            lblBarcodeStatus.Text = "Barcode already exists.";
        }
        else
        {
            lblBarcodeStatus.ForeColor = Color.SeaGreen;
            lblBarcodeStatus.Text = "Barcode is available.";
        }
    }

    // ----- key filters -----
    private static void DigitsOnly_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true;
    }

    private static void Decimal_KeyPress(object? sender, KeyPressEventArgs e)
    {
        if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar)) return;
        // allow a single decimal point
        if (e.KeyChar == '.' && sender is TextBox tb && !tb.Text.Contains('.')) return;
        e.Handled = true;
    }

    // =====================================================
    //  CALCULATIONS
    // =====================================================
    private void ComputeMarkup()
    {
        if (TryParseDecimal(txtCost.Text, out decimal cost) &&
            TryParseDecimal(txtPrice.Text, out decimal price) && cost > 0)
        {
            decimal markup = (price - cost) / cost * 100m;   // Markup % = (Price - Cost) / Cost x 100
            txtMarkup.Text = markup.ToString("N2", CultureInfo.InvariantCulture);
            txtMarkup.ForeColor = markup < 0 ? Color.Firebrick : SystemColors.WindowText;
        }
        else
        {
            txtMarkup.Text = "";
        }
    }

    private static void FormatMoney(TextBox tb)
    {
        if (TryParseDecimal(tb.Text, out decimal v))
            tb.Text = v.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static bool TryParseDecimal(string s, out decimal value) =>
        decimal.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);

    private static decimal ParseDecimal(string s) =>
        decimal.Parse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

    // =====================================================
    //  HELPERS
    // =====================================================
    private void LoadCategories()
    {
        cboCategory.Items.AddRange(new object[]
        {
            "Beverages", "Snacks", "Groceries", "Personal Care",
            "Household", "Electronics", "School Supplies", "Others"
        });
    }

    private bool HasInput() =>
        txtBarcode.Text.Length > 0 || txtName.Text.Length > 0 ||
        txtDescription.Text.Length > 0 || cboCategory.SelectedIndex >= 0 ||
        rdoLocal.Checked || rdoAbroad.Checked ||
        txtCost.Text.Length > 0 || txtPrice.Text.Length > 0;

    private void ClearForm()
    {
        errorProvider.Clear();
        txtBarcode.Clear();
        txtName.Clear();
        txtDescription.Clear();
        cboCategory.SelectedIndex = -1;
        rdoLocal.Checked = false;
        rdoAbroad.Checked = false;
        chkActive.Checked = true;
        txtCost.Clear();
        txtPrice.Clear();
        txtMarkup.Clear();
        UpdateBarcodeStatus();
        txtBarcode.Focus();
    }

    // ----- simple CSV storage so barcodes persist between runs -----
    private void SaveToCsv(string origin, string status, decimal cost, decimal price, decimal markup)
    {
        bool newFile = !File.Exists(_csvPath);
        using var w = new StreamWriter(_csvPath, append: true);
        if (newFile) w.WriteLine("Barcode,Name,Description,Category,Origin,Status,Cost,Price,Markup");
        w.WriteLine(string.Join(",",
            Csv(txtBarcode.Text.Trim()), Csv(txtName.Text.Trim()), Csv(txtDescription.Text.Trim()),
            Csv(cboCategory.Text), origin, status,
            cost.ToString("0.00", CultureInfo.InvariantCulture),
            price.ToString("0.00", CultureInfo.InvariantCulture),
            markup.ToString("0.00", CultureInfo.InvariantCulture)));
    }

    private static string Csv(string s) =>
        "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";

    private void LoadExistingBarcodes()
    {
        if (!File.Exists(_csvPath)) return;
        foreach (var line in File.ReadLines(_csvPath).Skip(1))
        {
            var parts = line.Split(',');
            if (parts.Length > 0) _barcodes.Add(parts[0].Trim('"'));
        }
    }
}
