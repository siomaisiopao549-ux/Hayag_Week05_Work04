# Product Form + Dashboard (C# WinForms)

Recreates the Product Form from the "C# Common Controls" slides and adds a dashboard as the main screen.

## Dashboard
- Summary cards: total, active, inactive, average markup, local vs abroad
- "Products by Category" bar chart
- Product table (DataGridView) with search by name/barcode and category filter
- Add Product (opens the Product Form), Delete Selected, Refresh
- Inactive products are shown in gray

## Product Form
- Live barcode check ("Barcode is available." / duplicate / invalid format)
- Input validation with ErrorProvider (required fields, 8-13 digit barcode, cost/price > 0, price >= cost)
- Numbers-only filtering on Barcode, Cost and Price
- Auto-calculated Markup (%) = (Price - Cost) / Cost x 100
- Save (writes to products.csv), Clear and Cancel with confirmations
- ENTER = Save, ESC = Cancel

## Run
```
dotnet run
```
Requires .NET 8 SDK (Windows).
