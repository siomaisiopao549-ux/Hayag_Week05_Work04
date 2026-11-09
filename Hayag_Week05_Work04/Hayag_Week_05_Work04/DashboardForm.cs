namespace ProductFormApp;

public class DashboardForm : Form
{
    // ---------- Controls ----------
    private readonly Label lblTotal = new();
    private readonly Label lblActive = new();
    private readonly Label lblInactive = new();
    private readonly Label lblAvgMarkup = new();
    private readonly Label lblOrigin = new();
    private readonly CategoryChart chart = new();
    private readonly TextBox txtSearch = new() { Width = 220, PlaceholderText = "Search name or barcode" };
    private readonly ComboBox cboFilter = new() { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button btnAdd = new() { Text = "+ Add Product", AutoSize = true };
    private readonly Button btnDelete = new() { Text = "Delete Selected", AutoSize = true };
    private readonly Button btnRefresh = new() { Text = "Refresh", AutoSize = true };
    private readonly Label lblCount = new() { AutoSize = true, Margin = new Padding(12, 8, 0, 0) };
    private readonly DataGridView grid = new();

    // ---------- Data ----------
    private List<Product> _all = new();
    private bool _loading;
    private const string AllCategories = "All Categories";

    public DashboardForm()
    {
        BuildLayout();
        WireEvents();
        LoadData();
    }

    // =====================================================
    //  LAYOUT
    // =====================================================
    private void BuildLayout()
    {
        Text = "Product Dashboard";
        ClientSize = new Size(1000, 640);
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(245, 247, 250);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // title
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));   // cards
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));  // chart
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));   // toolbar
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // grid

        // Title
        root.Controls.Add(new Label
        {
            Text = "Product Dashboard",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        // Summary cards
        var cards = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        cards.Controls.Add(MakeCard("Total Products", lblTotal, Color.SteelBlue));
        cards.Controls.Add(MakeCard("Active", lblActive, Color.SeaGreen));
        cards.Controls.Add(MakeCard("Inactive", lblInactive, Color.Gray));
        cards.Controls.Add(MakeCard("Average Markup", lblAvgMarkup, Color.DarkOrange));
        cards.Controls.Add(MakeCard("Local / Abroad", lblOrigin, Color.MediumPurple));
        root.Controls.Add(cards, 0, 1);

        // Chart
        chart.Dock = DockStyle.Fill;
        root.Controls.Add(chart, 0, 2);

        // Toolbar
        var bar = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        bar.Controls.AddRange(new Control[] { txtSearch, cboFilter, btnAdd, btnDelete, btnRefresh, lblCount });
        root.Controls.Add(bar, 0, 3);

        // Grid
        grid.Dock = DockStyle.Fill;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.MultiSelect = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.RowHeadersVisible = false;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(40, 70, 110);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(240, 244, 250);
        root.Controls.Add(grid, 0, 4);

        Controls.Add(root);
    }

    private static Panel MakeCard(string title, Label value, Color accent)
    {
        var card = new Panel
        {
            Width = 185,
            Height = 76,
            Margin = new Padding(0, 4, 12, 4),
            BackColor = Color.White
        };
        value.Dock = DockStyle.Fill;
        value.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        value.ForeColor = accent;
        value.TextAlign = ContentAlignment.MiddleLeft;
        value.Padding = new Padding(10, 0, 0, 0);
        value.Text = "0";

        var lbl = new Label
        {
            Text = title,
            Dock = DockStyle.Top,
            Height = 24,
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.DimGray,
            Padding = new Padding(10, 6, 0, 0)
        };

        var strip = new Panel { Dock = DockStyle.Left, Width = 5, BackColor = accent };

        card.Controls.Add(value);   // fill first
        card.Controls.Add(lbl);
        card.Controls.Add(strip);
        return card;
    }

    // =====================================================
    //  EVENTS
    // =====================================================
    private void WireEvents()
    {
        txtSearch.TextChanged += (_, _) => ApplyFilter();
        cboFilter.SelectedIndexChanged += (_, _) => { if (!_loading) ApplyFilter(); };
        btnRefresh.Click += (_, _) => LoadData();
        btnAdd.Click += BtnAdd_Click;
        btnDelete.Click += BtnDelete_Click;
        grid.DataBindingComplete += (_, _) => StyleGrid();
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var form = new ProductForm();
        form.ProductSaved += (_, _) => LoadData();   // dashboard updates right after each save
        form.ShowDialog(this);
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (grid.CurrentRow?.DataBoundItem is not Product selected)
        {
            MessageBox.Show("Select a product to delete first.", "Delete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var r = MessageBox.Show($"Delete \"{selected.Name}\" ({selected.Barcode})?\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (r != DialogResult.Yes) return;

        try
        {
            _all.RemoveAll(p => p.Barcode == selected.Barcode);
            ProductRepository.SaveAll(_all);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Could not delete the product.\n\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        LoadData();
    }

    // =====================================================
    //  DATA
    // =====================================================
    private void LoadData()
    {
        _all = ProductRepository.Load();

        // Rebuild the category filter but keep the current choice if possible
        _loading = true;
        string previous = cboFilter.SelectedItem as string ?? AllCategories;
        cboFilter.Items.Clear();
        cboFilter.Items.Add(AllCategories);
        foreach (var c in _all.Select(p => p.Category).Distinct().OrderBy(c => c))
            cboFilter.Items.Add(c);
        int idx = cboFilter.Items.IndexOf(previous);
        cboFilter.SelectedIndex = idx >= 0 ? idx : 0;
        _loading = false;

        UpdateSummary();
        ApplyFilter();
    }

    private void UpdateSummary()
    {
        lblTotal.Text = _all.Count.ToString();
        lblActive.Text = _all.Count(p => p.Status == "Active").ToString();
        lblInactive.Text = _all.Count(p => p.Status == "Inactive").ToString();
        lblAvgMarkup.Text = (_all.Count == 0 ? 0m : _all.Average(p => p.Markup)).ToString("N2") + "%";
        lblOrigin.Text = $"{_all.Count(p => p.Origin == "Local")} / {_all.Count(p => p.Origin == "Abroad")}";

        chart.Data = _all.GroupBy(p => p.Category)
                         .OrderByDescending(g => g.Count())
                         .ToDictionary(g => g.Key, g => g.Count());
    }

    private void ApplyFilter()
    {
        string term = txtSearch.Text.Trim();
        string cat = cboFilter.SelectedItem as string ?? AllCategories;

        var filtered = _all.Where(p =>
                (cat == AllCategories || p.Category == cat) &&
                (term.Length == 0 ||
                 p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                 p.Barcode.Contains(term, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        grid.DataSource = filtered;
        lblCount.Text = $"Showing {filtered.Count} of {_all.Count}";
    }

    private void StyleGrid()
    {
        void Fmt(string name, string header, bool money)
        {
            if (grid.Columns[name] is { } col)
            {
                col.HeaderText = header;
                if (money)
                {
                    col.DefaultCellStyle.Format = "N2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }
        Fmt("Cost", "Cost", true);
        Fmt("Price", "Price", true);
        Fmt("Markup", "Markup (%)", true);

        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.DataBoundItem is Product p && p.Status == "Inactive")
                row.DefaultCellStyle.ForeColor = Color.Gray;
        }
    }

    // =====================================================
    //  Simple bar chart: products per category
    // =====================================================
    private sealed class CategoryChart : Panel
    {
        private Dictionary<string, int> _data = new();

        public Dictionary<string, int> Data
        {
            get => _data;
            set { _data = value; Invalidate(); }
        }

        public CategoryChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Margin = new Padding(0, 4, 0, 4);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            using var titleFont = new Font("Segoe UI", 9, FontStyle.Bold);
            using var font = new Font("Segoe UI", 8);
            g.DrawString("Products by Category", titleFont, Brushes.DimGray, 10, 6);

            if (_data.Count == 0)
            {
                g.DrawString("No products yet. Click \"+ Add Product\" to get started.", font, Brushes.Gray, 10, 40);
                return;
            }

            int top = 28;
            int rowH = Math.Max(12, Math.Min(20, (Height - top - 4) / _data.Count));
            int labelW = 120;
            int max = _data.Values.Max();
            int barMax = Width - labelW - 60;

            using var barBrush = new SolidBrush(Color.SteelBlue);
            int y = top;
            foreach (var kv in _data)
            {
                if (y + rowH > Height) break;
                g.DrawString(kv.Key, font, Brushes.Black, 10, y + 1);
                int w = Math.Max(2, (int)((double)kv.Value / max * barMax));
                g.FillRectangle(barBrush, labelW, y + 2, w, rowH - 5);
                g.DrawString(kv.Value.ToString(), font, Brushes.Black, labelW + w + 6, y + 1);
                y += rowH;
            }
        }
    }
}
