using AntdUI;
using Logistics.Client.Application.Localization;
using Logistics.Client.Infrastructure.Exceptions;
using Logistics.Client.WinForms.Helpers;
using Logistics.Client.WinForms.Themes;
using Label = System.Windows.Forms.Label;

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

public sealed record ModalComboOption(object Value, string Display)
{
    public override string ToString() => Display;
}

public sealed class ModalFieldSpec
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public ModalFieldKind Kind { get; init; } = ModalFieldKind.Text;
    public object? InitialValue { get; init; }
    public bool Required { get; init; }
    public bool ReadOnly { get; init; }
    public IReadOnlyList<ModalComboOption>? Options { get; init; }
    public decimal MinValue { get; init; } = 0;
    public decimal MaxValue { get; init; } = 9999999999m;
    public int DecimalPlaces { get; init; } = 2;
    public string? Placeholder { get; init; }
}

/// <summary>
/// AntdUI Borderless Window modal dialog supporting typed inputs, inline field validation,
/// API validation error mapping, loading submit states, and Light/Dark theming.
/// </summary>
public sealed class ModalFormDialog : AntdUI.Window, IThemeAware
{
    private readonly Dictionary<string, Control> _inputs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _fieldErrors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ModalFieldSpec> _specs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Func<IReadOnlyDictionary<string, object?>, Task>? _onSubmitAsync;
    private readonly AntdUI.PageHeader _header;
    private readonly AntdUI.Alert _bannerAlert;
    private readonly AntdUI.Button _saveBtn;
    private readonly AntdUI.Button _cancelBtn;
    private readonly System.Windows.Forms.Panel _footer;

    public ModalFormDialog(
        string title,
        IEnumerable<ModalFieldSpec> fields,
        Func<IReadOnlyDictionary<string, object?>, Task>? onSubmitAsync = null,
        string? submitText = null,
        int width = 580)
    {
        _onSubmitAsync = onSubmitAsync;
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.None;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        Resizable = true;
        MinimumSize = new Size(440, 300);
        var targetWidth = Math.Clamp(width, 440, 920);
        Size = new Size(targetWidth, Math.Clamp(220 + fields.Count() * 58, 320, 740));
        Mode = AppTheme.IsDark ? TAMode.Dark : TAMode.Light;

        _header = new AntdUI.PageHeader
        {
            Dock = DockStyle.Top,
            Height = 52,
            Text = title,
            Font = AppTheme.SectionTitleFont,
            CancelButton = true,
            MinimizeBox = false,
            MaximizeBox = false,
            DividerShow = true,
            DividerThickness = 1F,
            DragMove = true
        };

        _bannerAlert = new AntdUI.Alert
        {
            Dock = DockStyle.Top,
            Visible = false,
            Height = 46,
            Icon = TType.Error,
            Font = AppTheme.SmallFont,
            BorderWidth = 0F,
            Radius = 0
        };

        var scroll = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 14, 20, 12),
            BackColor = Color.Transparent
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            BackColor = Color.Transparent
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 165));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        var row = 0;
        foreach (var field in fields)
        {
            _specs[field.Key] = field;
            var lbl = new Label
            {
                Text = field.Required ? $"{field.Label} *" : field.Label,
                Font = AppTheme.BodyBoldFont,
                TextAlign = ContentAlignment.TopLeft,
                AutoSize = false,
                Height = 32,
                Padding = new Padding(0, 8, 8, 0),
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var inputWrap = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 4, 4, 8),
                BackColor = Color.Transparent
            };
            inputWrap.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inputWrap.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            inputWrap.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var input = CreateInputControl(field);
            _inputs[field.Key] = input;

            var errLabel = new Label
            {
                AutoSize = true,
                Visible = false,
                Font = AppTheme.SmallFont,
                ForeColor = AppTheme.DangerRed,
                Margin = new Padding(2, 2, 0, 0),
                BackColor = Color.Transparent
            };
            _fieldErrors[field.Key] = errLabel;

            inputWrap.Controls.Add(input, 0, 0);
            inputWrap.Controls.Add(errLabel, 0, 1);

            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(lbl, 0, row);
            table.Controls.Add(inputWrap, 1, row);
            row++;
        }

        scroll.Controls.Add(table);

        _footer = new System.Windows.Forms.Panel
        {
            Dock = DockStyle.Bottom,
            Height = 56,
            Padding = new Padding(18, 10, 18, 10)
        };

        var btnFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        _cancelBtn = AppTheme.CreateSecondaryButton(AppStrings.T("Cancel"), 98);
        _cancelBtn.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        _saveBtn = AppTheme.CreatePrimaryButton(submitText ?? AppStrings.T("Save"), 136);
        _saveBtn.Click += async (_, _) => await HandleSubmitAsync();

        btnFlow.Controls.Add(_saveBtn);
        btnFlow.Controls.Add(_cancelBtn);
        _footer.Controls.Add(btnFlow);

        Controls.Add(scroll);
        Controls.Add(_bannerAlert);
        Controls.Add(_footer);
        Controls.Add(_header);

        CancelButton = _cancelBtn;

        Load += (_, _) => ClampModalToScreen();
        Resize += (_, _) =>
        {
            if (table.ColumnStyles.Count > 0 && ClientSize.Width > 0)
            {
                var labelColW = ClientSize.Width < 500 ? 132F : 165F;
                if (Math.Abs(table.ColumnStyles[0].Width - labelColW) > 1F)
                    table.ColumnStyles[0].Width = labelColW;
            }
        };
        ApplyTheme();
    }

    private void ClampModalToScreen()
    {
        var host = (Control?)Owner ?? this;
        var workArea = Screen.FromControl(host).WorkingArea;
        if (workArea.Width <= 0 || workArea.Height <= 0) return;

        var minW = Math.Min(440, workArea.Width - 24);
        var minH = Math.Min(300, workArea.Height - 24);
        var maxW = Math.Max(minW, Math.Min(920, workArea.Width - 40));
        var maxH = Math.Max(minH, workArea.Height - 40);

        MinimumSize = new Size(minW, minH);
        MaximumSize = new Size(maxW, maxH);
        Size = new Size(Math.Clamp(Width, minW, maxW), Math.Clamp(Height, minH, maxH));
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
                return new AntdUI.Input
                {
                    Dock = DockStyle.Fill,
                    Height = 34,
                    Radius = 6,
                    UseSystemPasswordChar = true,
                    Text = f.InitialValue?.ToString() ?? string.Empty,
                    ReadOnly = f.ReadOnly,
                    PlaceholderText = f.Placeholder ?? "••••••••",
                    Font = AppTheme.BodyFont
                };

            case ModalFieldKind.Multiline:
                return new AntdUI.Input
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    WordWrap = true,
                    Height = 76,
                    Radius = 6,
                    Text = f.InitialValue?.ToString() ?? string.Empty,
                    ReadOnly = f.ReadOnly,
                    PlaceholderText = f.Placeholder ?? string.Empty,
                    Font = AppTheme.BodyFont
                };

            case ModalFieldKind.Decimal:
            case ModalFieldKind.Integer:
                var num = new AntdUI.InputNumber
                {
                    Dock = DockStyle.Fill,
                    Height = 34,
                    Radius = 6,
                    Minimum = f.MinValue,
                    Maximum = f.MaxValue,
                    DecimalPlaces = f.Kind == ModalFieldKind.Integer ? 0 : f.DecimalPlaces,
                    ThousandsSeparator = f.Kind == ModalFieldKind.Decimal,
                    Enabled = !f.ReadOnly,
                    Font = AppTheme.BodyFont
                };
                if (f.InitialValue != null && decimal.TryParse(f.InitialValue.ToString(), out var initNum))
                    num.Value = Math.Clamp(initNum, f.MinValue, f.MaxValue);
                return num;

            case ModalFieldKind.Date:
                DateTime initDate = f.InitialValue switch
                {
                    DateTime dt => dt,
                    DateOnly dOnly => dOnly.ToDateTime(TimeOnly.MinValue),
                    _ => DateTime.Today
                };
                return new AntdUI.DatePicker
                {
                    Dock = DockStyle.Fill,
                    Height = 34,
                    Radius = 6,
                    Format = "dd/MM/yyyy",
                    Value = initDate,
                    Enabled = !f.ReadOnly,
                    Font = AppTheme.BodyFont
                };

            case ModalFieldKind.ComboBox:
                var select = new AntdUI.Select
                {
                    Dock = DockStyle.Fill,
                    Height = 34,
                    Radius = 6,
                    List = true,
                    Enabled = !f.ReadOnly,
                    PlaceholderText = f.Placeholder ?? "-- Chọn giá trị --",
                    Font = AppTheme.BodyFont
                };
                if (f.Options != null)
                {
                    int matchedIdx = -1;
                    for (int i = 0; i < f.Options.Count; i++)
                    {
                        var opt = f.Options[i];
                        select.Items.Add(new SelectItem(opt.Display, opt.Value));
                        if (matchedIdx < 0 && f.InitialValue != null &&
                            (Equals(opt.Value, f.InitialValue) ||
                             string.Equals(opt.Value?.ToString(), f.InitialValue.ToString(), StringComparison.OrdinalIgnoreCase)))
                        {
                            matchedIdx = i;
                        }
                    }

                    if (matchedIdx >= 0)
                        select.SelectedIndex = matchedIdx;
                    else if (f.Options.Count > 0 && f.Required)
                        select.SelectedIndex = 0;
                }
                return select;

            case ModalFieldKind.CheckBox:
                return new AntdUI.Checkbox
                {
                    Dock = DockStyle.Fill,
                    Height = 32,
                    Text = f.Placeholder ?? f.Label,
                    Checked = f.InitialValue is bool b && b,
                    Enabled = !f.ReadOnly,
                    Font = AppTheme.BodyFont
                };

            default:
                return new AntdUI.Input
                {
                    Dock = DockStyle.Fill,
                    Height = 34,
                    Radius = 6,
                    AllowClear = !f.ReadOnly,
                    Text = f.InitialValue?.ToString() ?? string.Empty,
                    ReadOnly = f.ReadOnly,
                    PlaceholderText = f.Placeholder ?? string.Empty,
                    Font = AppTheme.BodyFont
                };
        }
    }

    private IReadOnlyDictionary<string, object?> ReadValues()
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, ctrl) in _inputs)
        {
            dict[key] = ctrl switch
            {
                AntdUI.InputNumber num => _specs[key].Kind == ModalFieldKind.Integer ? (int)num.Value : num.Value,
                AntdUI.DatePicker dp => dp.Value ?? DateTime.Today,
                AntdUI.Select sel => ReadSelectValue(sel),
                AntdUI.Checkbox cb => cb.Checked,
                AntdUI.Input txt => txt.Text.Trim(),
                _ => ctrl.Text.Trim()
            };
        }
        return dict;
    }

    private static object? ReadSelectValue(AntdUI.Select sel)
    {
        if (sel.SelectedIndex >= 0 && sel.SelectedIndex < sel.Items.Count && sel.Items[sel.SelectedIndex] is SelectItem item)
            return item.Tag;
        return sel.SelectedValue;
    }

    private void ClearErrors()
    {
        _bannerAlert.Visible = false;
        foreach (var (key, errLbl) in _fieldErrors)
        {
            errLbl.Visible = false;
            if (_inputs.TryGetValue(key, out var ctrl) && ctrl is AntdUI.Input inp)
                inp.Status = TType.None;
        }
    }

    private void SetFieldError(string key, string message)
    {
        if (_fieldErrors.TryGetValue(key, out var lbl))
        {
            lbl.Text = message;
            lbl.Visible = true;
        }
        if (_inputs.TryGetValue(key, out var ctrl) && ctrl is AntdUI.Input inp)
            inp.Status = TType.Error;
    }

    private async Task HandleSubmitAsync()
    {
        ClearErrors();
        var valid = true;

        foreach (var (key, spec) in _specs)
        {
            if (!spec.Required) continue;
            var ctrl = _inputs[key];
            var isMissing = ctrl switch
            {
                AntdUI.Select sel => ReadSelectValue(sel) == null,
                AntdUI.Input txt => string.IsNullOrWhiteSpace(txt.Text),
                _ => false
            };

            if (isMissing)
            {
                SetFieldError(key, "Trường này là bắt buộc.");
                valid = false;
            }
        }

        if (!valid) return;

        if (_onSubmitAsync == null)
        {
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        try
        {
            _saveBtn.Loading = true;
            _saveBtn.Enabled = false;
            _cancelBtn.Enabled = false;
            UseWaitCursor = true;

            await _onSubmitAsync(ReadValues());
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ApiException apiEx)
        {
            var msg = UiAsyncRunner.FormatApiErrorMessage(apiEx);
            _bannerAlert.Text = msg;
            _bannerAlert.Visible = true;

            if (apiEx.ValidationErrors != null)
            {
                foreach (var kv in apiEx.ValidationErrors)
                {
                    var match = _inputs.Keys.FirstOrDefault(k => string.Equals(k, kv.Key, StringComparison.OrdinalIgnoreCase));
                    if (match != null)
                        SetFieldError(match, string.Join("; ", kv.Value));
                }
            }
        }
        catch (Exception ex)
        {
            _bannerAlert.Text = ex.Message;
            _bannerAlert.Visible = true;
        }
        finally
        {
            if (!IsDisposed)
            {
                UseWaitCursor = false;
                _saveBtn.Loading = false;
                _saveBtn.Enabled = true;
                _cancelBtn.Enabled = true;
            }
        }
    }

    public void ApplyTheme()
    {
        Mode = AppTheme.IsDark ? TAMode.Dark : TAMode.Light;
        BackColor = AppTheme.SurfaceBg;
        ForeColor = AppTheme.TextPrimary;
        BorderColor = AppTheme.BorderSubtle;
        _header.BackColor = AppTheme.CardBg;
        _header.ForeColor = AppTheme.TextPrimary;
        _header.DividerColor = AppTheme.BorderSubtle;
        _footer.BackColor = AppTheme.RowAltBg;
    }
}
