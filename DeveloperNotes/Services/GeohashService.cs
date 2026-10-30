using DeveloperNotes.DTOs;
using StackExchange.Redis;

namespace DeveloperNotes.Services
{
    public class GeohashService(IDatabase db)
    {
        private readonly IDatabase _db = db;

        public async Task<UserAndLocationResponse> AddUserAndLocation(AddUserAndLocationRequest request, CancellationToken ct)
        {
            const string UserKey = "users:locations";
            string formattedName = $"user:{request.Name}";

            await _db.GeoAddAsync(UserKey, request.Coordinates.Longitude, request.Coordinates.Latitude, formattedName);

            await _db.HashSetAsync($"user:{request.Name}", new HashEntry[]
            {
                new("name", request.Name),
                new("location", $"{request.Coordinates.Longitude},{request.Coordinates.Latitude}"),
                new("workload", request.CurrentWorkload)
            });

            return new UserAndLocationResponse()
            {
                Name = request.Name,
                Coordinates = request.Coordinates,
                CurrentWorkload = request.CurrentWorkload
            };
        }

        public async Task<UserAndLocationResponse> AddTargetAndLocation(AddUserAndLocationRequest request, CancellationToken ct)
        {
            const string UserKey = "targets:locations";

            await _db.GeoAddAsync(UserKey, request.Coordinates.Longitude, request.Coordinates.Latitude, request.Name);

            await _db.HashSetAsync($"target:{request.Name}", new HashEntry[]
            {
                new("name", request.Name),
                new("location", $"{request.Coordinates.Longitude},{request.Coordinates.Latitude}")
            });

            return new UserAndLocationResponse()
            {
                Name = request.Name,
                Coordinates = request.Coordinates
            };
        }

        // FT.AGGREGATE idx:users "@location:[121.0244 14.5547 25 km]"
        // LOAD 3 @name @location @workload
        // APPLY "geodistance(@location, 121.0244, 14.5547) / 1000" AS distanceKm
        // APPLY "0.6 * (1 / (1 + @distanceKm)) + 0.4 * (1 - (@workload / 100))" AS score
        // SORTBY 2 @score DESC LIMIT 5


        public async Task<List<UserAndLocationResponse>> GetNearbyUsers(
            NearbyUsersRequest request,
            CancellationToken ct)
        {
            var longitude = request.Coordinates.Longitude;
            var latitude = request.Coordinates.Latitude;
            var radius = request.DistanceRadius;

            var query = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                $"@location:[{longitude} {latitude} {radius} km]",
                longitude,
                latitude);

            var results = await _db.ExecuteAsync(
                "FT.AGGREGATE",
                "idx:users",
                query,
                "LOAD", "3", "@name", "@location", "@workload",
                "APPLY", $"geodistance(@location, {longitude}, {latitude}) / 1000",
                "AS", "distanceKm",
                "FILTER", "@workload < 10",
                "APPLY", $"1 - (@distanceKm / {radius})",
                "AS", "distanceScore",
                "APPLY", "1 - (@workload / 10)",
                "AS", "availabilityScore",
                "APPLY", "0.5 * @distanceScore + 0.5 * @availabilityScore",
                "AS", "score",
                "SORTBY", "2", "@score", "DESC",
                "LIMIT", "0", request.Limit.ToString()
            );

            var response = (RedisResult[])results;
            RedisResult[]? resultRows = null;

            for (var i = 0; i + 1 < response.Length; i += 2)
            {
                if (response[i].ToString() == "results")
                {
                    resultRows = (RedisResult[])response[i + 1];
                    break;
                }
            }

            if (resultRows == null)
            {
                throw new InvalidOperationException("Redis response does not contain a results array.");
            }

            Console.WriteLine($"Returned rows: {resultRows.Length}");

            var nearbyUsers = new List<UserAndLocationResponse>();

            foreach (var row in resultRows)
            {
                var fields = (RedisResult[])row;
                RedisResult[]? attributes = null;

                for (var i = 0; i + 1 < fields.Length; i += 2)
                {
                    if (fields[i].ToString() == "extra_attributes")
                    {
                        attributes = (RedisResult[])fields[i + 1];
                        break;
                    }
                }

                if (attributes == null)
                {
                    Console.WriteLine("Skipped row: extra_attributes not found.");
                    continue;
                }

                var values = new Dictionary<string, string>();

                for (var i = 0; i + 1 < attributes.Length; i += 2)
                {
                    values[attributes[i].ToString()] =
                        attributes[i + 1].ToString();
                }

                if (!values.TryGetValue("name", out var name) ||
                    !values.TryGetValue("location", out var location) ||
                    !values.TryGetValue("workload", out var workloadText))
                {
                    Console.WriteLine("Skipped row: required user data missing.");
                    continue;
                }

                var locationParts = location.Split(',');

                if (locationParts.Length != 2)
                {
                    Console.WriteLine($"Invalid location: {location}");
                    continue;
                }

                if (!double.TryParse(
                        locationParts[0],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var userLongitude) ||
                    !double.TryParse(
                        locationParts[1],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var userLatitude))
                {
                    Console.WriteLine($"Invalid coordinates: {location}");
                    continue;
                }

                if (!int.TryParse(workloadText, out var workload))
                {
                    Console.WriteLine($"Invalid workload: {workloadText}");
                    continue;
                }

                nearbyUsers.Add(new UserAndLocationResponse
                {
                    Name = name,
                    Coordinates = new Coordinates
                    {
                        Longitude = userLongitude,
                        Latitude = userLatitude
                    },
                    CurrentWorkload = workload
                });
            }

            Console.WriteLine($"Parsed users: {nearbyUsers.Count}");

            return nearbyUsers;
        }




        public async Task<UserAndLocationResponse> GetUserOrTargetLocation(string name)
        {
            var userKey = $"user:{name}";
            var targetKey = $"target:{name}";
            var userExists = await _db.KeyExistsAsync(userKey);
            var targetExists = await _db.KeyExistsAsync(targetKey);

            if (!userExists && !targetExists)
            {
                throw new Exception($"User or target '{name}' not found.");
            }

            var keyToUse = userExists ? userKey : targetKey;
            var hashEntries = await _db.HashGetAllAsync(keyToUse);
            var locationEntry = hashEntries.FirstOrDefault(entry => entry.Name == "location");
            var workloadEntry = hashEntries.FirstOrDefault(entry => entry.Name == "workload");

            if (locationEntry.Equals(new HashEntry()))
            {
                throw new Exception($"Location for '{name}' not found.");
            }

            var locationParts = locationEntry.Value.ToString().Split(',');
            var coordinates = new Coordinates
            {
                Longitude = double.Parse(locationParts[0]),
                Latitude = double.Parse(locationParts[1])
            };

            int? workload = null;
            if (workloadEntry != null && workloadEntry.Value.HasValue)
            {
                workload = (int)workloadEntry.Value;
            }
            return new UserAndLocationResponse
            {
                Name = name,
                Coordinates = coordinates,
                CurrentWorkload = workload
            };
        }

        public async Task<string> GetGeohash(GeohashRequest request)
        {
            var key = request.KeyType == "user" ? "users:locations" : "targets:locations";

            var geohash = await _db.GeoHashAsync(key, $"user:{request.Name}");

            if (geohash == null || geohash.Length == 0)
            {
                throw new Exception($"User not found.");
            }

            return geohash;
        }

        private double CalculateFinalScore(double distanceKm, int assignedTargets)
        {
            const int SEARCH_RADIUS = 25;
            const int MAX_WORKLOAD = 10;

            var DistanceScore = 1 - (distanceKm / SEARCH_RADIUS);
            var WorkloadScore = 1 - (assignedTargets / MAX_WORKLOAD);

            var FinalScore = 0.6 * DistanceScore + 0.4 * WorkloadScore;

            return FinalScore;
        }
    }
}
