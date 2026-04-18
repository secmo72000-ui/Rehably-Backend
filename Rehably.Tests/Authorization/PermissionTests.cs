using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Rehably.API.Authorization;
using Xunit;

namespace Rehably.Tests.Authorization;

public class PermissionTests
{
    public class PermissionDefinitionTests
    {
        [Fact]
        public void Permission_Ctor_CreatesCorrectPermission()
        {
            var permission = new Permission("patients", "view");

            permission.Resource.Should().Be("patients");
            permission.Action.Should().Be("view");
        }

        [Fact]
        public void Permission_ToString_ReturnsDotNotation()
        {
            var permission = new Permission("clinics", "create");
            var result = permission.ToString();

            result.Should().Be("clinics.create");
        }

        [Fact]
        public void Permission_Equality_SameResourceAndAction_ReturnsTrue()
        {
            var p1 = new Permission("patients", "view");
            var p2 = new Permission("patients", "view");

            p1.Should().Be(p2);
        }

        [Fact]
        public void Permission_Equality_DifferentResource_ReturnsFalse()
        {
            var p1 = new Permission("patients", "view");
            var p2 = new Permission("clinics", "view");

            p1.Should().NotBe(p2);
        }

        [Fact]
        public void Permission_GetHashCode_SamePermission_ReturnsSameHash()
        {
            var p1 = new Permission("patients", "view");
            var p2 = new Permission("patients", "view");

            p1.GetHashCode().Should().Be(p2.GetHashCode());
        }
    }

    public class PermissionStaticClassesTests
    {
        [Fact]
        public void Clinics_Class_HasAllCRUDPermissions()
        {
            Permission.Clinics.View.Should().NotBeNull();
            Permission.Clinics.Create.Should().NotBeNull();
            Permission.Clinics.Update.Should().NotBeNull();
            Permission.Clinics.Delete.Should().NotBeNull();
        }

        [Fact]
        public void Patients_Class_HasAllCRUDPermissions()
        {
            Permission.Patients.View.Should().NotBeNull();
            Permission.Patients.Create.Should().NotBeNull();
            Permission.Patients.Update.Should().NotBeNull();
            Permission.Patients.Delete.Should().NotBeNull();
        }

        [Fact]
        public void Appointments_Class_HasAllCRUDPermissions()
        {
            Permission.Appointments.View.Should().NotBeNull();
            Permission.Appointments.Create.Should().NotBeNull();
            Permission.Appointments.Update.Should().NotBeNull();
            Permission.Appointments.Delete.Should().NotBeNull();
        }

        [Fact]
        public void Platform_Class_HasAllPlatformPermissions()
        {
            Permission.Platform.ManageFeatures.Should().NotBeNull();
            Permission.Platform.ManagePackages.Should().NotBeNull();
            Permission.Platform.ManageSubscriptions.Should().NotBeNull();
            Permission.Platform.ManageFeatureCategories.Should().NotBeNull();
            Permission.Platform.ViewUsageStats.Should().NotBeNull();
            Permission.Platform.ManageClinics.Should().NotBeNull();
        }
    }
}
