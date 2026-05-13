namespace Alerto.Api.Security;

public static class AuthPolicies
{
    public const string Admin = "AdminPolicy";
    public const string Operator = "OperatorPolicy";
    public const string Analyst = "AnalystPolicy";
    public const string Auditor = "AuditorPolicy";

    public const string AlertReaders = "AlertReaders";
    public const string AlertOperators = "AlertOperators";
    public const string AlertCreators = "AlertCreators";
    public const string AlertApprovers = "AlertApprovers";
    public const string CitizenConfirmers = "CitizenConfirmers";
    public const string ConfirmationReaders = "ConfirmationReaders";
    public const string Dispatchers = "Dispatchers";
    public const string GeofenceReaders = "GeofenceReaders";
    public const string GeofenceManagers = "GeofenceManagers";
    public const string UserAdministrators = "UserAdministrators";
    public const string AdminsOnly = "AdminsOnly";
}
