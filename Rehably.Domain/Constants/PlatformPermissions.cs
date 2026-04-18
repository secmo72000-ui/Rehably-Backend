namespace Rehably.Domain.Constants;

/// <summary>
/// Defines all available permissions for Platform Admin roles.
/// These are the permissions that can be assigned when creating custom platform roles.
/// </summary>
public static class PlatformPermissions
{
    /// <summary>
    /// All platform resources with their available actions
    /// </summary>
    public static readonly List<PlatformResource> Resources = new()
    {
        new PlatformResource
        {
            Key = "dashboard",
            NameEn = "Dashboard",
            NameAr = "الرئيسية",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "clinics",
            NameEn = "Clinic Management",
            NameAr = "ادارة العيادات",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "subscriptions",
            NameEn = "Subscriptions",
            NameAr = "الاشتراكات",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "invoices",
            NameEn = "Invoices",
            NameAr = "الفواتير",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "audit_logs",
            NameEn = "Login Records",
            NameAr = "تسجيلات الدخول",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "settings",
            NameEn = "Settings",
            NameAr = "الاعدادات",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "platform_users",
            NameEn = "Platform Admins",
            NameAr = "مديرين المنصة",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "roles",
            NameEn = "Roles",
            NameAr = "الأدوار",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "packages",
            NameEn = "Packages",
            NameAr = "الباقات",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        },
        new PlatformResource
        {
            Key = "features",
            NameEn = "Features",
            NameAr = "الميزات",
            Actions = new List<PlatformAction>
            {
                new("view", "View", "قراءة"),
                new("create", "Create", "انشاء"),
                new("update", "Update", "تعديل"),
                new("delete", "Delete", "ازالة")
            }
        }
    };

    /// <summary>
    /// Get all permission strings (e.g., "clinics.view", "users.create")
    /// </summary>
    public static List<string> GetAllPermissionStrings()
    {
        var permissions = new List<string>();
        foreach (var resource in Resources)
        {
            foreach (var action in resource.Actions)
            {
                permissions.Add($"{resource.Key}.{action.Key}");
            }
        }
        return permissions;
    }

    /// <summary>
    /// Validate if a permission string is valid
    /// </summary>
    public static bool IsValidPermission(string permission)
    {
        if (string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        if (permission == "*" || permission == "*.*")
        {
            return true;
        }

        var parts = permission.Split('.');
        if (parts.Length != 2)
        {
            return false;
        }

        var resource = parts[0];
        var action = parts[1];

        if (action == "*")
        {
            return Resources.Any(r => r.Key == resource);
        }

        var resourceObj = Resources.FirstOrDefault(r => r.Key == resource);
        if (resourceObj == null)
        {
            return false;
        }

        return resourceObj.Actions.Any(a => a.Key == action);
    }
}
