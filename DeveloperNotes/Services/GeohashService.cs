using DeveloperNotes.DTOs;
using StackExchange.Redis;

namespace DeveloperNotes.Services
{
    public class GeohashService(IDatabase db)
    {
        private readonly IDatabase _db = db;

        public static class GeohashConstants
        {
            public const int MAX_WORKLOAD = 5;
            public const int DEFAULT_NEARBY_COLLECTORS_LIMIT = 3;
        }

        public async Task<CreateCollectorResponse> AddCollector(CreateCollectorRequest request, CancellationToken ct)
        {
            var collectorId = Guid.NewGuid().ToString();

            var key = $"collector:{collectorId}";

            await _db.HashSetAsync(key, new HashEntry[]
            {
                new("id", collectorId),
                new("name", request.Name),
                new("latitude", request.Coordinates.Latitude),
                new("longitude", request.Coordinates.Longitude),
                new("location", $"{request.Coordinates.Longitude},{request.Coordinates.Latitude}"),
                new("workload", 0)
            });

            return new CreateCollectorResponse()
            {
                CollectorId = collectorId,
                Name = request.Name,
                Coordinates = request.Coordinates,
                CurrentWorkload = 0
            };
        }

        public async Task<CreateTargetResponse> AddTarget(CreateTargetRequest request, CancellationToken ct)
        {
            var targetId = Guid.NewGuid().ToString();

            var key = $"target:{targetId}";

            await _db.HashSetAsync(key, new HashEntry[]
            {
                new("id", targetId),
                new("name", request.Name),
                new("latitude", request.Coordinates.Latitude),
                new("longitude", request.Coordinates.Longitude),
                new("location", $"{request.Coordinates.Longitude},{request.Coordinates.Latitude}"),
                new("isAssigned", false),
                new("assignedUserId", "")
            });

            return new CreateTargetResponse()
            {
                TargetId = targetId,
                Name = request.Name,
                Coordinates = request.Coordinates,
                IsAssigned = false,
                AssignedUserId = null
            };
        }

        // FT.AGGREGATE idx:users "@location:[121.0244 14.5547 25 km]"
        // LOAD 3 @name @location @workload
        // APPLY "geodistance(@location, 121.0244, 14.5547) / 1000" AS distanceKm
        // APPLY "0.6 * (1 / (1 + @distanceKm)) + 0.4 * (1 - (@workload / 100))" AS score
        // SORTBY 2 @score DESC LIMIT 5

        public async Task<List<NearbyCollectorsResponse>> GetNearbyCollectors(NearbyCollectorsRequest request, CancellationToken ct)
        {
            var targetKey = $"target:{request.TargetId}";
            var targetData = await _db.HashGetAllAsync(targetKey);

            if (targetData.Length == 0)
            {
                throw new KeyNotFoundException($"Target with ID '{request.TargetId}' was not found.");
            }

            var target = targetData.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

            var longitude = double.Parse(target["longitude"], System.Globalization.CultureInfo.InvariantCulture);

            var latitude = double.Parse(target["latitude"], System.Globalization.CultureInfo.InvariantCulture);

            var targetDetails = new TargetDetails
            {
                TargetId = request.TargetId,
                Name = target["name"],
                Coordinates = new Coordinates
                {
                    Longitude = longitude,
                    Latitude = latitude
                }
            };

            if (request.RadiusKm <= 0)
            {
                throw new ArgumentException("Radius must be greater than zero.");
            }

            var query = FormattableString.Invariant($"@location:[{longitude} {latitude} {request.RadiusKm} km]");

            var results = await _db.ExecuteAsync(
                "FT.AGGREGATE",
                "idx:collectors",
                query,
                "LOAD", "4",
                "@id", "@name", "@location", "@workload",
                "APPLY", $"geodistance(@location, {longitude}, {latitude}) / 1000",
                "AS", "distanceKm",
                "FILTER", $"@workload < {GeohashConstants.MAX_WORKLOAD}",
                "APPLY", $"1 - (@distanceKm / {request.RadiusKm})",
                "AS", "distanceScore",
                "APPLY", $"1 - (@workload / {GeohashConstants.MAX_WORKLOAD})",
                "AS", "availabilityScore",
                "APPLY", "0.5 * @distanceScore + 0.5 * @availabilityScore",
                "AS", "score",
                "SORTBY", "2", "@score", "DESC",
                "LIMIT", "0", GeohashConstants.DEFAULT_NEARBY_COLLECTORS_LIMIT.ToString()
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

            var nearbyUsers = new List<NearbyCollectorsResponse>();
            var rank = 1;

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
                    continue;

                var values = new Dictionary<string, string>();

                for (var i = 0; i + 1 < attributes.Length; i += 2)
                {
                    values[attributes[i].ToString()] = attributes[i + 1].ToString();
                }

                var locationParts = values["location"].Split(',');

                var userLongitude = double.Parse(locationParts[0], System.Globalization.CultureInfo.InvariantCulture);

                var userLatitude = double.Parse(locationParts[1], System.Globalization.CultureInfo.InvariantCulture);

                var workload = int.Parse(values["workload"], System.Globalization.CultureInfo.InvariantCulture);

                nearbyUsers.Add(new NearbyCollectorsResponse
                {
                    Rank = rank++,
                    DistanceKm = double.Parse(values["distanceKm"], System.Globalization.CultureInfo.InvariantCulture),
                    Score = double.Parse(values["score"], System.Globalization.CultureInfo.InvariantCulture),

                    CollectorDetails = new CollectorDetails
                    {
                        CollectorId = values["id"],
                        Name = values["name"],
                        Coordinates = new Coordinates
                        {
                            Longitude = userLongitude,
                            Latitude = userLatitude
                        },
                        CurrentWorkload = workload
                    },

                    TargetDetails = targetDetails
                });
            }

            return nearbyUsers;
        }

        public async Task<AssignTargetResponse> AssignTarget(string targetId, AssignTargetRequest request, CancellationToken ct)
        {
            var targetKey = $"target:{targetId}";
            var collectorKey = $"collector:{request.CollectorId}";
            var collectorTargetsKey = $"collector:{request.CollectorId}:targets";

            const string script = """
                local targetExists = redis.call('EXISTS', KEYS[1])
                local collectorExists = redis.call('EXISTS', KEYS[2])

                if targetExists == 0 then
                    return {0, 'Target not found'}
                end

                if collectorExists == 0 then
                    return {0, 'Collector not found'}
                end

                local isAssigned = redis.call('HGET', KEYS[1], 'isAssigned')

                if isAssigned == 'true' then
                    return {0, 'Target is already assigned'}
                end

                local workload = tonumber(redis.call('HGET', KEYS[2], 'workload') or '0')

                if workload >= tonumber(ARGV[1]) then
                    return {0, 'Collector has reached maximum workload'}
                end

                local updatedWorkload = workload + 1

                redis.call('HSET', KEYS[1],
                    'isAssigned', 'true',
                    'assignedUserId', ARGV[2])

                redis.call('HSET', KEYS[2],
                    'workload', updatedWorkload)

                redis.call('SADD', KEYS[3], ARGV[3])

                return {
                    1,
                    updatedWorkload,
                    redis.call('HGET', KEYS[1], 'name'),
                    redis.call('HGET', KEYS[2], 'name')
                }
                """;

            var result = (RedisResult[])await _db.ScriptEvaluateAsync(script, new RedisKey[]
            {
                targetKey, collectorKey, collectorTargetsKey
            }, new RedisValue[]
            {
                GeohashConstants.MAX_WORKLOAD, request.CollectorId, targetId
            });

            var status = (int)result[0];

            if (status == 0)
            {
                var message = result[1].ToString();

                throw new InvalidOperationException(message);
            }

            return new AssignTargetResponse
            {
                TargetId = targetId,
                TargetName = result[2].ToString(),
                CollectorId = request.CollectorId,
                Name = result[3].ToString(),
                UpdatedWorkload = (int)result[1],
                IsAssigned = true
            };
        }

        public async Task<List<TargetsResponse>> GetTargets(CancellationToken ct)
        {
            var targets = new List<TargetsResponse>();

            var endpoint = _db.Multiplexer.GetEndPoints().First();
            var server = _db.Multiplexer.GetServer(endpoint);

            var targetKeys = server.Keys(database: _db.Database, pattern: "target:*");

            foreach (var key in targetKeys)
            {
                ct.ThrowIfCancellationRequested();

                var targetData = await _db.HashGetAllAsync(key);

                if (targetData.Length == 0) continue;

                var data = targetData.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

                targets.Add(new TargetsResponse
                {
                    TargetId = data.TryGetValue("id", out var id) ? id : key.ToString()["target:".Length..],

                    Name = data["name"],

                    Coordinates = new Coordinates
                    {
                        Longitude = double.Parse(data["longitude"], System.Globalization.CultureInfo.InvariantCulture),

                        Latitude = double.Parse(data["latitude"], System.Globalization.CultureInfo.InvariantCulture)
                    },

                    IsAssigned = data["isAssigned"] == "1" || data["isAssigned"].Equals("true", StringComparison.OrdinalIgnoreCase),

                    AssignedUserId = data.TryGetValue("assignedUserId", out var userId) && !string.IsNullOrWhiteSpace(userId) ? userId : null
                });
            }

            return targets;
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
    }
}
