using Smartstore.Core.Identity;
using Smartstore.Core.Security;

namespace ML.CMS
{
    /// <summary>
    /// All permissions provided by this module. Recommended to use singular for names, <see cref="Permissions"/>.
    /// "devtools" is the root permission (by convention, doesn't contain any dot). Localization key is "Modules.Permissions.DisplayName.DevTools".
    /// "devtools.read" and "devtools.update" do not need localization because they are contained in core, <see cref="PermissionService._displayNameResourceKeys"/>.
    /// </summary>
    internal static class CMSPermissions
    {
        public const string Self = "CMS";
        public const string Read = "CMS.read";
        public const string Update = "CMS.update";
    }

    internal class CMSPermissionProvider : IPermissionProvider
    {
        public IEnumerable<PermissionRecord> GetPermissions()
        {
            // Get all permissions from above static class.
            var permissionSystemNames = PermissionHelper.GetPermissions(typeof(CMSPermissions));
            var permissions = permissionSystemNames.Select(x => new PermissionRecord { SystemName = x });

            return permissions;
        }

        public IEnumerable<DefaultPermissionRecord> GetDefaultPermissions()
        {
            // Allow root permission for admin by default.
            return new[]
            {
                new DefaultPermissionRecord
                {
                    CustomerRoleSystemName = SystemCustomerRoleNames.Administrators,
                    PermissionRecords = new[]
                    {
                        new PermissionRecord { SystemName = CMSPermissions.Self }
                    }
                }
            };
        }
    }
}
