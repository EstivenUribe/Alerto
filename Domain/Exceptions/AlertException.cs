namespace Alerto.Domain.Exceptions;

/// <summary>
/// Excepción base para errores de lógica de negocio relacionados con alertas.
/// </summary>
public class AlertException : Exception
{
    public AlertException(string message) : base(message) { }
    public AlertException(string message, Exception innerException) 
        : base(message, innerException) { }
}

/// <summary>Excepción cuando se intenta operar sobre una alerta en estado inválido</summary>
public class AlertStatusException : AlertException
{
    public AlertStatusException(string message) : base(message) { }
}

/// <summary>Excepción cuando se excede el timeout de aprobación (3 minutos)</summary>
public class ApprovalTimeoutException : AlertException
{
    public ApprovalTimeoutException(string message) : base(message) { }
}

/// <summary>Excepción cuando la geocerca no existe o no está activa</summary>
public class GeocercaInvalidException : AlertException
{
    public GeocercaInvalidException(string message) : base(message) { }
}
