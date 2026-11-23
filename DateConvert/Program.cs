using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();

var app = builder.Build();

// Serve index.html from the wwwroot folder
app.UseDefaultFiles();
app.UseStaticFiles();

// API endpoint to convert a Gregorian date into a Hebrew date via Hebcal
app.MapGet("/api/convert", async (string date, IHttpClientFactory httpClientFactory) =>
{
    if (!DateTime.TryParse(date, out DateTime parsedDate))
    {
        return Results.BadRequest(new { error = "Invalid date format." });
    }

    string formattedDate = parsedDate.ToString("yyyy-MM-dd");
    string hebcalUrl = $"https://www.hebcal.com/converter?cfg=json&date={formattedDate}&g2h=1&strict=1";

    var client = httpClientFactory.CreateClient();

    try
    {
        var response = await client.GetAsync(hebcalUrl);
        if (!response.IsSuccessStatusCode)
        {
            return Results.StatusCode((int)response.StatusCode);
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        var hebcalData = JsonSerializer.Deserialize<HebcalResponse>(jsonString);

        if (hebcalData == null)
        {
            return Results.Problem("Failed to parse response from Hebcal API.");
        }

        return Results.Ok(new
        {
            gregorianDate = formattedDate,
            hebrewDate = hebcalData.Hebrew,
            hebrewParts = $"{hebcalData.HMonth} {hebcalData.HYear} {hebcalData.HDay}",
            events = hebcalData.Events ?? new List<string>()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error communicating with external service: {ex.Message}");
    }
});

app.Run();

// Model matching the JSON returned by the Hebcal converter API
public class HebcalResponse
{
    [JsonPropertyName("hebrew")]
    public string Hebrew { get; set; } = string.Empty;

    [JsonPropertyName("hy")]
    public int HYear { get; set; }

    [JsonPropertyName("hm")]
    public string HMonth { get; set; } = string.Empty;

    [JsonPropertyName("hd")]
    public int HDay { get; set; }

    [JsonPropertyName("events")]
    public List<string>? Events { get; set; }
}