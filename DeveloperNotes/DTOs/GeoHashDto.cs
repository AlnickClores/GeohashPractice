namespace DeveloperNotes.DTOs
{
    public class Coordinates
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }

    public class CreateCollectorRequest
    {
        public required string Name { get; set; }
        public required Coordinates Coordinates { get; set; }
    }

    public class CreateCollectorResponse
    {
        public required string CollectorId { get; set; }
        public required string Name { get; set; }
        public required Coordinates Coordinates { get; set; }
        public int CurrentWorkload { get; set; }
    }

    public class CreateTargetRequest
    {
        public required string Name { get; set; }
        public required Coordinates Coordinates { get; set; }
    }

    public class CreateTargetResponse
    {
        public required string TargetId { get; set; }
        public required string Name { get; set; }
        public required Coordinates Coordinates { get; set; }
        public bool IsAssigned { get; set; }
        public string? AssignedUserId { get; set; }
    }

    public class NearbyCollectorsRequest
    {
        public required string TargetId { get; set; }
        public int? RadiusKm { get; set; } = 25;
    }

    public class NearbyCollectorsResponse
    {
        public CollectorDetails CollectorDetails { get; set; } = null!;
        public TargetDetails TargetDetails { get; set; } = null!;
        public double DistanceKm { get; set; }
        public double Score { get; set; }
        public int Rank { get; set; }
    }

    public class CollectorDetails
    {
        public string CollectorId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Coordinates Coordinates { get; set; } = null!;
        public int CurrentWorkload { get; set; }
    }

    public class TargetDetails
    {
        public string TargetId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Coordinates Coordinates { get; set; } = null!;
    }

    public class AssignTargetRequest
    {
        public string? CollectorId { get; set; }
    }

    public class AssignTargetResponse
    {
        public string TargetId { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
        public string CollectorId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int UpdatedWorkload { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class GeohashRequest
    {
        public required string KeyType { get; set; }
        public required string Name { get; set; }
    }

}
