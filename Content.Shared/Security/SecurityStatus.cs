namespace Content.Shared.Security;

/// <summary>
/// Status used in Criminal Records.
///
/// None - the default value
/// Wanted - the person is being wanted by security
/// Detained - the person is detained by security
/// </summary>
public enum SecurityStatus : byte
{
    None,
    Released, // WD EDIT
    Suspected, // WD EDIT
    Wanted,
    Detained
}
