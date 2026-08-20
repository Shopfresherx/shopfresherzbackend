namespace ShopFresherz.Domain.Enums;

/// <summary>Defines the access roles available for platform users.</summary>
public enum UserRole
{
    /// <summary>Standard registered customer.</summary>
    Customer = 0,

    /// <summary>Store administrator with product/order management access.</summary>
    Admin = 1,

    /// <summary>Super administrator with full platform access including role promotion.</summary>
    SuperAdmin = 2
}
