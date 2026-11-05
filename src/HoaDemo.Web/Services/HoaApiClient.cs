using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HoaDemo.Web.Services;

/// <summary>Raised when the API answers 400 { "message": "..." }; the message is shown in the UI.</summary>
public class HoaApiException(string message) : Exception(message);

/// <summary>Typed client for HoaDemo.Api (BaseAddress from HoaApi:BaseUrl).</summary>
public class HoaApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public Task<List<AssociationDto>> GetAssociationsAsync() => GetAsync<List<AssociationDto>>("api/associations");
    public Task<AssociationDto?> GetAssociationAsync(int id) => GetOrNullAsync<AssociationDto>($"api/associations/{id}");
    public Task<AssociationDto> CreateAssociationAsync(AssociationDto association) => PostAsync<AssociationDto, AssociationDto>("api/associations", association);

    public Task<List<HomeownerDto>> GetHomeownersAsync(int? associationId = null) =>
        GetAsync<List<HomeownerDto>>(associationId is null ? "api/homeowners" : $"api/homeowners?associationId={associationId}");
    public Task<HomeownerDto?> GetHomeownerAsync(int id) => GetOrNullAsync<HomeownerDto>($"api/homeowners/{id}");
    public Task<HomeownerDto> CreateHomeownerAsync(HomeownerDto homeowner) => PostAsync<HomeownerDto, HomeownerDto>("api/homeowners", homeowner);

    public Task<List<AssessmentDto>> GetAssessmentsAsync(int? homeownerId = null) =>
        GetAsync<List<AssessmentDto>>(homeownerId is null ? "api/assessments" : $"api/assessments?homeownerId={homeownerId}");
    public Task<AssessmentDetailDto?> GetAssessmentAsync(int id) => GetOrNullAsync<AssessmentDetailDto>($"api/assessments/{id}");
    public Task<AssessmentDto> CreateAssessmentAsync(AssessmentDto assessment) => PostAsync<AssessmentDto, AssessmentDto>("api/assessments", assessment);

    public Task<PaymentDto> CreatePaymentAsync(NewPayment payment) => PostAsync<NewPayment, PaymentDto>("api/payments", payment);

    private async Task<T> GetAsync<T>(string url)
    {
        using var response = await http.GetAsync(url);
        return await ReadAsync<T>(response);
    }

    private async Task<T?> GetOrNullAsync<T>(string url) where T : class
    {
        using var response = await http.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        return await ReadAsync<T>(response);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string url, TRequest body)
    {
        using var response = await http.PostAsJsonAsync(url, body, Json);
        return await ReadAsync<TResponse>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var error = await response.Content.ReadFromJsonAsync<ApiError>(Json);
            throw new HoaApiException(error?.ToDisplayMessage() ?? "The request is invalid.");
        }

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private sealed record ApiError(string? Message, Dictionary<string, string[]>? ModelState)
    {
        public string ToDisplayMessage()
        {
            var details = ModelState?.SelectMany(kv => kv.Value).Where(m => !string.IsNullOrEmpty(m)).ToList();
            return details is { Count: > 0 } ? $"{Message} {string.Join(" ", details)}" : Message ?? "The request is invalid.";
        }
    }
}
