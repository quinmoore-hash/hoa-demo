namespace HoaDemo.Web.Services;

// Records mirror the HoaDemo.Api JSON (camelCase via System.Text.Json web defaults).

public record AssociationDto(
    int Id,
    string Name,
    string? Address,
    int GracePeriodDays,
    decimal LateFeeFlatAmount,
    decimal LateFeePercent,   // fraction: 0.05 = 5%
    decimal LateFeeMax);      // 0 = no cap

public record HomeownerDto(
    int Id,
    int AssociationId,
    string FirstName,
    string LastName,
    string? Email,
    string PropertyAddress)
{
    public string FullName => $"{FirstName} {LastName}";
}

public record AssessmentDto(
    int Id,
    int HomeownerId,
    string Description,
    decimal Amount,
    DateTime DueDate);

/// <summary>GET /api/assessments/{id}: adds the balance and late fee as of today.</summary>
public record AssessmentDetailDto(
    int Id,
    int HomeownerId,
    string Description,
    decimal Amount,
    DateTime DueDate,
    decimal Balance,
    decimal LateFee,
    decimal TotalDue,
    List<PaymentDto> Payments);

public record PaymentDto(
    int Id,
    int AssessmentId,
    decimal Amount,
    DateTime PaidOn,
    string? Method,
    string? ReferenceNumber);

/// <summary>POST /api/payments body; a null PaidOn is omitted so the API defaults it to today.</summary>
public record NewPayment(
    int AssessmentId,
    decimal Amount,
    DateTime? PaidOn,
    string? Method,
    string? ReferenceNumber);
