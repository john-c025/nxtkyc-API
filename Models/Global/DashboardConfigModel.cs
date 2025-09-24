namespace KYCAPI.Models.Global
{
    public class DashboardConfig
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string CompanyId { get; set; }
        public DashboardConfigData DashboardConfigData { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string Version { get; set; }
    }

    public class DashboardConfigData
    {
        public DashboardLayout Layout { get; set; }
        public DashboardPreferences Preferences { get; set; }
        public DashboardWidgetSettings WidgetSettings { get; set; }
        public DashboardMetadata Metadata { get; set; }
    }

    public class DashboardLayout
    {
        public List<DashboardWidget> TopRow { get; set; } = new();
        public List<DashboardWidget> MainContent { get; set; } = new();
    }

    public class DashboardWidget
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Width { get; set; }
        public string Icon { get; set; }
        public int Position { get; set; }
        public bool IsVisible { get; set; }
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class DashboardPreferences
    {
        public string Theme { get; set; }
        public int RefreshInterval { get; set; }
        public string DefaultView { get; set; }
        public DashboardNotifications Notifications { get; set; }
    }

    public class DashboardNotifications
    {
        public bool Email { get; set; }
        public bool Push { get; set; }
        public bool Sms { get; set; }
    }

    public class DashboardWidgetSettings
    {
        public Dictionary<string, string> ChartColors { get; set; } = new();
        public string DateRange { get; set; }
        public string Currency { get; set; }
        public string Timezone { get; set; }
    }

    public class DashboardMetadata
    {
        public DateTime LastModified { get; set; }
        public string Version { get; set; }
        public string CreatedBy { get; set; }
    }

    // Request/Response DTOs
    public class DashboardConfigRequest
    {
        public string UserId { get; set; }
        public string CompanyId { get; set; }
        public DashboardConfigData DashboardConfigData { get; set; }
        public bool? IsCompanyWideUpdate { get; set; } // New flag to determine update type
        public string UpdateType { get; set; } // "user" or "company" - alternative approach
    }

    public class DashboardConfigsResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public IEnumerable<DashboardConfig> Data { get; set; }
    }

    public class DashboardConfigResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DashboardConfig Data { get; set; }
    }
}
