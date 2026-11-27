using AntdUI;

namespace Logistics.Client.WinForms.Themes;

/// <summary>
/// Maps domain workflow statuses, financial states, and flags to AntdUI semantic tags.
/// </summary>
public static class StatusTagHelper
{
    public static TTypeMini ResolveType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return TTypeMini.Default;

        var normalized = value.Trim();

        // Success states
        if (normalized.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Paid", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Confirmed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Settled", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Reconciled", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Matched", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Succeeded", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Sent", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Passed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("True", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Healthy", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Available", StringComparison.OrdinalIgnoreCase))
        {
            return TTypeMini.Success;
        }

        // Active / In-progress / Processing states
        if (normalized.Equals("InTransit", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("OutForDelivery", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("InSortingHub", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("PickedUp", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("ReadyForPickup", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("ReadyToShip", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Allocated", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Picking", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Packed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Processing", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Booked", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Assigned", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Submitted", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Collected", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Published", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Received", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Dispatched", StringComparison.OrdinalIgnoreCase))
        {
            return TTypeMini.Primary;
        }

        // Info states
        if (normalized.Equals("Open", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("New", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Read", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Info", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Scheduled", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Standard", StringComparison.OrdinalIgnoreCase))
        {
            return TTypeMini.Info;
        }

        // Warning / Attention states
        if (normalized.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("PartiallyPaid", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Partial", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Returning", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("OnHold", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Warning", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Warn", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("LowStock", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Unread", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Requested", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Retry", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("PendingReview", StringComparison.OrdinalIgnoreCase))
        {
            return TTypeMini.Warn;
        }

        // Error / Failure / Rejected states
        if (normalized.Equals("DeliveryFailed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Failed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Canceled", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Overdue", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Disputed", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Discrepancy", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Shortage", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Void", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Voided", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Locked", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Damaged", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Expired", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("False", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("No", StringComparison.OrdinalIgnoreCase))
        {
            return TTypeMini.Error;
        }

        // Neutral / Draft / Inactive / Returned
        return TTypeMini.Default;
    }

    public static CellTag CreateCellTag(object? rawValue)
    {
        var text = FormatDisplay(rawValue);
        return new CellTag(text, ResolveType(text));
    }

    public static AntdUI.Tag CreateBadgeTag(string? text, TTypeMini? overrideType = null)
    {
        var display = string.IsNullOrWhiteSpace(text) ? "-" : text.Trim();
        return new AntdUI.Tag
        {
            Text = display,
            Type = overrideType ?? ResolveType(display),
            Radius = 6,
            AutoSize = true,
            Font = AppTheme.SmallFont
        };
    }

    public static bool IsStatusColumn(string propertyName, Type propertyType)
    {
        var underlying = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (underlying == typeof(bool))
            return true;

        return propertyName.Contains("Status", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("State", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("Decision", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("Severity", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("Health", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("Environment", StringComparison.OrdinalIgnoreCase) ||
               propertyName.Equals("Direction", StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatDisplay(object? value)
    {
        if (value == null) return "-";
        if (value is bool b) return b ? "Yes" : "No";
        return value.ToString()?.Trim() ?? "-";
    }
}
