namespace Rehably.API.Authorization;

public sealed record Permission(string Resource, string Action)
{
    public override string ToString() => $"{Resource}.{Action}";

    public static class Clinics
    {
        public static readonly Permission View = new("clinics", "view");
        public static readonly Permission Create = new("clinics", "create");
        public static readonly Permission Update = new("clinics", "update");
        public static readonly Permission Delete = new("clinics", "delete");
    }

    public static class Patients
    {
        public static readonly Permission View = new("patients", "view");
        public static readonly Permission Create = new("patients", "create");
        public static readonly Permission Update = new("patients", "update");
        public static readonly Permission Delete = new("patients", "delete");
    }

    public static class Appointments
    {
        public static readonly Permission View = new("appointments", "view");
        public static readonly Permission Create = new("appointments", "create");
        public static readonly Permission Update = new("appointments", "update");
        public static readonly Permission Delete = new("appointments", "delete");
    }

    public static class Invoices
    {
        public static readonly Permission View = new("invoices", "view");
        public static readonly Permission Create = new("invoices", "create");
        public static readonly Permission Update = new("invoices", "update");
        public static readonly Permission Delete = new("invoices", "delete");
    }

    public static class Payments
    {
        public static readonly Permission View = new("payments", "view");
        public static readonly Permission Create = new("payments", "create");
        public static readonly Permission Refund = new("payments", "refund");
    }

    public static class Reports
    {
        public static readonly Permission View = new("reports", "view");
        public static readonly Permission Export = new("reports", "export");
    }

    public static class Users
    {
        public static readonly Permission View = new("users", "view");
        public static readonly Permission Create = new("users", "create");
        public static readonly Permission Update = new("users", "update");
        public static readonly Permission Delete = new("users", "delete");
    }

    public static class Roles
    {
        public static readonly Permission View = new("roles", "view");
        public static readonly Permission Create = new("roles", "create");
        public static readonly Permission Update = new("roles", "update");
        public static readonly Permission Delete = new("roles", "delete");
    }

    public static class Settings
    {
        public static readonly Permission View = new("settings", "view");
        public static readonly Permission Update = new("settings", "update");
    }

    public static class Platform
    {
        public static readonly Permission ManageFeatures = new("platform", "manage_features");
        public static readonly Permission ManagePackages = new("platform", "manage_packages");
        public static readonly Permission ManageSubscriptions = new("platform", "manage_subscriptions");
        public static readonly Permission ManageFeatureCategories = new("platform", "manage_feature_categories");
        public static readonly Permission ViewUsageStats = new("platform", "view_usage_stats");
        public static readonly Permission ManageClinics = new("platform", "manage_clinics");
        public static readonly Permission All = new("platform", "*");
    }

    public static class Wildcards
    {
        public static readonly Permission All = new("*", "*");
        public static readonly Permission ClinicsAll = new("clinics", "*");
        public static readonly Permission PatientsAll = new("patients", "*");
        public static readonly Permission AppointmentsAll = new("appointments", "*");
        public static readonly Permission InvoicesAll = new("invoices", "*");
        public static readonly Permission PaymentsAll = new("payments", "*");
        public static readonly Permission ReportsAll = new("reports", "*");
        public static readonly Permission UsersAll = new("users", "*");
        public static readonly Permission RolesAll = new("roles", "*");
        public static readonly Permission SettingsAll = new("settings", "*");
    }
}
