namespace SampleApi.Contracts;

public record Customer(
    int CustomerKey,
    string FirstName,
    string LastName,
    string? CompanyName,
    string CityName,
    string StateProvinceName,
    string RegionCountryName,
    string ContinentName,
    string Gender,
    string MaritalStatus,
    string Education,
    string Occupation);