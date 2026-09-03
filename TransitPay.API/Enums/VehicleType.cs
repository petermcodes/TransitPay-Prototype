namespace TransitPay.API.Enums;

/// <summary>
/// The vehicle mode a fare rule or trip applies to.
/// The current scope of the system operates BUS routes; the other modes are
/// reserved for future expansion of the fare matrix.
/// </summary>
public enum VehicleType
{
    /// <summary>Bus / jeepney transit (the currently operational mode).</summary>
    BUS,

    /// <summary>Train / rail transit (reserved for future use).</summary>
    TRAIN,

    /// <summary>Metro / subway transit (reserved for future use).</summary>
    METRO,

    /// <summary>Ferry / water transit (reserved for future use).</summary>
    FERRY
}