namespace TransitPay.API.Enums;

/// <summary>
/// Authorization roles in the system.
/// Values are the seeded database role IDs (1, 2, 3). Roles are assigned server-side
/// only — never trusted from client-supplied values.
/// </summary>
public enum RoleName
{
    /// <summary>Passenger role (seeded role_id = 1). Self-service registration always assigns this.</summary>
    Passenger = 1,

    /// <summary>Driver/conductor role (seeded role_id = 2). Accounts are created by Admins.</summary>
    Driver = 2,

    /// <summary>Administrator role (seeded role_id = 3). Accounts are created by other Admins or the initial bootstrap.</summary>
    Admin = 3
}
