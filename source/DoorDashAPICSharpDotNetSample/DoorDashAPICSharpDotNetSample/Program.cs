using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

// Credentials provided from https://developer.doordash.com/portal/integration/drive/credentials
// TODO: Replace placeholders with credential values
var accessKey = new Dictionary<string, string>{
  {"developer_id", "UPDATE_WITH_DEVELOPER_ID"}, // TODO: Update value with Developer ID
  {"key_id", "UPDATE_WITH_KEY_ID"}, // TODO: Update value with Key ID
  {"signing_secret", "UPDATE_WITH_DEVELOPER_ID"} // TODO: Update value with Signing Secret
};

// Signing Secret is Base64Encoded when generated on the Credentials page, need to decode to use
var decodedSecret = Base64UrlEncoder.DecodeBytes(accessKey["signing_secret"]);
var securityKey = new SymmetricSecurityKey(decodedSecret);
var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
var header = new JwtHeader(credentials);

// DoorDash header used to identify DoorDash JWT version
header["dd-ver"] = "DD-JWT-V1";

var payload = new JwtPayload(
    issuer: accessKey["developer_id"],
    audience: "doordash",
    claims: new List<Claim> { new Claim("kid", accessKey["key_id"]) },
    notBefore: null,
    expires: System.DateTime.UtcNow.AddMinutes(30),
    issuedAt: System.DateTime.UtcNow);

var securityToken = new JwtSecurityToken(header, payload);
var token = new JwtSecurityTokenHandler().WriteToken(securityToken);

// Write the DoorDash API JWT
Console.WriteLine("DoorDash API JWT: " + token);

// Generate Unique ID for Delivery
var deliverId = Guid.NewGuid().ToString(); // TODO: Replace with generated system ID

// Create data needed to create a new delivery  
var jsonContent = JsonSerializer.Serialize(new
{
    external_delivery_id = deliverId,
    pickup_address = "901 Market Street 6th Floor San Francisco, CA 94103",
    pickup_business_name = "Wells Fargo SF Downtown",
    pickup_phone_number = "+16505555555",
    pickup_instructions = "Enter gate code 1234 on the callbox.",
    pickup_reference_tag = "Order number 61",
    dropoff_address = "901 Market Street 6th Floor San Francisco, CA 94103",
    dropoff_business_name = "Wells Fargo SF Downtown",
    dropoff_phone_number = "+16505555555",
    dropoff_instructions = "Enter gate code 1234 on the callbox."
});

var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

// Note: In a production system don't create a new HttpClient per request
// see https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines for guidance 
using HttpClient client = new();

client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
var result = await client.PostAsync("https://openapi.doordash.com/drive/v2/deliveries", content);

var status = result.StatusCode;
var resultString = await result.Content.ReadAsStringAsync();

Console.WriteLine("");
Console.WriteLine("Result Status: " + status);
Console.WriteLine("Result Response: " + resultString);