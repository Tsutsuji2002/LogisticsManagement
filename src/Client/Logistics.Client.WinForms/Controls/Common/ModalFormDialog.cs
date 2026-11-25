using Logistics.Client.Application.Localization;
using Logistics.Client.Infrastructure.Exceptions;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;

namespace Logistics.Client.WinForms.Controls.Common;

public enum ModalFieldKind
{
    Text,
    Password,
    Multiline,
    Decimal,
    Integer,
    Date,
    ComboBox,
    CheckBox
}

public sealed record ModalComboOption(object? Value, string Label)
{
    public override string ToString() => Label;
}

public sealed class ModalFieldSpec
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public ModalFieldKind Kind { get; init; } = ModalFieldKind.Text;
    public object? InitialValue { get; init; }
    public bool Required { get; init; }
    public bool ReadOnly { get; init; }
    public IReadOnlyList<ModalComboOption>? Options { get; init; }
}

/// <summary>
/// Resizable modal dialog generator with built-in required-field validation,
/// backend ValidationErrors mapping onto <see cref="ErrorProvider"/>, and async duplicate-submit prevention.
/// </summary>
public sealed class ModalFormDialog : Form
{
    private readonly ErrorProvider _errorProvider = new();
    private readonly Dictionary<string, Control> _inputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ModalFieldSpec> _specs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<IReadOnlyDictionary<string, object?>, Task>? _asyncSubmit;
    private readonly Label _bannerErrorLabel;
    private readonly Button _saveBtn;

    public ModalFormDialog(
        string title,
        IReadOnlyList<ModalFieldSpec> fields,
        Func<IReadOnlyDictionary<string, object?>, Task>? asyncSubmit = null,
        string? submitText = null)
    {
        _asyncSubmit = asyncSubmit;

        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(460, 260);
        Size = new Size(540, Math.Clamp(165 + fields.Count * 46, 260, 680));
        BackColor = Color.White;
        Font = AppTheme.BodyFont;

        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18) };
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Margin = new Padding(0)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36F));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64F));

        _bannerErrorLabel = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(490, 0),
            ForeColor = AppTheme.DangerRed,
            BackColor = Color.FromArgb(254, 242, 242),
            Padding = new Padding(10),
            Visible = false
        };

        int row = 0;
        foreach (var field in fields)
        {
            _specs[field.Key] = field;
            var lbl = new Label
            {
                Text = field.Required ? $"{field.Label} *" : field.Label,
                AutoSize = true,
                ForeColor = AppTheme.TextSecondary,
                Font = AppTheme.BodyBoldFont,
                Margin = new Padding(0, 8, 8, 4)
            };

            var input = CreateInputControl(field);
            _inputs[field.Key] = input;

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(lbl, 0, row);
            table.Controls.Add(input, 1, row);
            row++;
        }

        scroll.Controls.Add(table);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16, 8, 16, 8),
            BackColor = AppTheme.RowAltBg
        };

        var cancelBtn = AppTheme.CreateSecondaryButton(AppStrings.T("Cancel"), 96);
        cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _saveBtn = AppTheme.CreatePrimaryButton(submitText ?? AppStrings.T("Save"), 130);
        _saveBtn.Click += async (_, _) => await HandleSubmitAsync();

        footer.Controls.Add(cancelBtn);
        footer.Controls.Add(_saveBtn);

        Controls.Add(scroll);
        Controls.Add(_bannerErrorLabel);
        Controls.Add(footer);
        AcceptButton = _saveBtn;
    }

    public IReadOnlyDictionary<string, object?> Values => ReadValues();

    public string GetString(string key) => ReadValues().TryGetValue(key, out var v) ? v?.ToString()?.Trim() ?? string.Empty : string.Empty;
    public long? GetLong(string key) => ReadValues().TryGetValue(key, out var v) && v != null && long.TryParse(v.ToString(), out var parsed) ? parsed : null;
    public decimal GetDecimal(string key) => ReadValues().TryGetValue(key, out var v) && v != null && decimal.TryParse(v.ToString(), out var parsed) ? parsed : 0m;
    public int GetInt(string key) => ReadValues().TryGetValue(key, out var v) && v != null && int.TryParse(v.ToString(), out var parsed) ? parsed : 0;
    public bool GetBool(string key) => ReadValues().TryGetValue(key, out var v) && v is bool b && b;
    public DateTime GetDateTime(string key) => ReadValues().TryGetValue(key, out var v) && v is DateTime dt ? dt : DateTime.UtcNow;

    private Control CreateInputControl(ModalFieldSpec f)
    {
        switch (f.Kind)
        {
            case ModalFieldKind.Password:
                return new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true, Text = f.InitialValue?.ToString() ?? string.Empty, Enabled = !f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
            case ModalFieldKind.Multiline:
                return new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 64, ScrollBars = ScrollBars.Vertical, Text = f.InitialValue?.ToString() ?? string.Empty, ReadOnly = f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
            case ModalFieldKind.Decimal:
                var dec = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 2, Maximum = 999_999_999_999m, Minimum = -999_999_999m, Enabled = !f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
                if (f.InitialValue != null && decimal.TryParse(f.InitialValue.ToString(), out var dv)) dec.Value = Math.Clamp(dv, dec.Minimum, dec.Maximum);
                return dec;
            case ModalFieldKind.Integer:
                var num = new NumericUpDown { Dock = DockStyle.Fill, DecimalPlaces = 0, Maximum = 999_999_999m, Minimum = -999_999m, Enabled = !f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
                if (f.InitialValue != null && decimal.TryParse(f.InitialValue.ToString(), out var iv)) num.Value = Math.Clamp(iv, num.Minimum, num.Maximum);
                return num;
            case ModalFieldKind.Date:
                var dtp = new DateTimePicker { Dock = DockStyle.Fill, Format = DateTimePickerFormat.Short, Enabled = !f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
                if (f.InitialValue is DateTime dtVal) dtp.Value = dtVal;
                else if (f.InitialValue is DateOnly doVal) dtp.Value = doVal.ToDateTime(TimeOnly.MinValue);
                return dtp;
            case ModalFieldKind.ComboBox:
                var cb = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, Enabled = !f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
                if (f.Options != null)
                {
                    foreach (var opt in f.Options) cb.Items.Add(opt);
                    var match = f.Options.FirstOrDefault(o => Equals(o.Value, f.InitialValue) || string.Equals(o.Value?.ToString(), f.InitialValue?.ToString(), StringComparison.OrdinalIgnoreCase));
                    if (match != null) cb.SelectedItem = match;
                    else if (cb.Items.Count > 0) cb.SelectedIndex = 0;
                }
                return cb;
            case ModalFieldKind.CheckBox:
                return new CheckBox { Checked = f.InitialValue is true, AutoSize = true, Enabled = !f.ReadOnly, Margin = new Padding(0, 8, 16, 6) };
            default:
                return new TextBox { Dock = DockStyle.Fill, Text = f.InitialValue?.ToString() ?? string.Empty, ReadOnly = f.ReadOnly, Margin = new Padding(0, 4, 16, 6) };
        }
    }

    private Dictionary<string, object?> ReadValues()
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, ctrl) in _inputs)
        {
            dict[key] = ctrl switch
            {
                TextBox tb => tb.Text.Trim(),
                NumericUpDown n => n.DecimalPlaces == 0 ? (long)n.Value : n.Value,
                DateTimePicker dt => dt.Value,
                ComboBox cb => (cb.SelectedItem as ModalComboOption)?.Value,
                CheckBox chk => chk.Checked,
                _ => null
            };
        }
        return dict;
    }

    private async Task HandleSubmitAsync()
    {
        _errorProvider.Clear();
        _bannerErrorLabel.Visible = false;

        foreach (var (key, spec) in _specs)
        {
            if (!spec.Required) continue;
            var ctrl = _inputs[key];
            bool empty = ctrl switch
            {
                TextBox tb => string.IsNullOrWhiteSpace(tb.Text),
                ComboBox cb => (cb.SelectedItem as ModalComboOption)?.Value == null,
                _ => false
            };

            if (empty)
            {
                _errorProvider.SetError(ctrl, $"{spec.Label} là bắt buộc.");
                _bannerErrorLabel.Text = $"Vui lòng nhập đầy đủ trường bắt buộc ({spec.Label}).";
                _bannerErrorLabel.Visible = true;
                return;
            }
        }

        if (_asyncSubmit == null)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        try
        {
            _saveBtn.Enabled = false;
            UseWaitCursor = true;
            await _asyncSubmit(ReadValues());
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ApiException ex)
        {
            if (ex.ValidationErrors != null)
            {
                foreach (var (field, errs) in ex.ValidationErrors)
                {
                    if (_inputs.TryGetValue(field, out var target))
                        _errorProvider.SetError(target, string.Join("; ", errs));
                }
            }
            _bannerErrorLabel.Text = UiAsyncRunner.FormatApiErrorMessage(ex);
            _bannerErrorLabel.Visible = true;
        }
        catch (Exception ex)
        {
            _bannerErrorLabel.Text = ex.Message;
            _bannerErrorLabel.Visible = true;
        }
        finally
        {
            if (!IsDisposed)
            {
                UseWaitCursor = false;
                _saveBtn.Enabled = true;
            }
        }
    }
}
