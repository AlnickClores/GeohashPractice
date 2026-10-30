namespace DeveloperNotes.DTOs
{
    public class AddUserAndLocationRequest
    {
        public required string Name { get; set; }
        public required Coordinates Coordinates { get; set; }
        public int? CurrentWorkload { get; set; } = 0;
    }

    public class UserAndLocationResponse
    {
        public required string Name { get; set; }
        public required Coordinates Coordinates { get; set; }
        public int? CurrentWorkload { get; set; }
    }

    public class Coordinates
    {
        public required double Latitude { get; set; }
        public required double Longitude { get; set; }
    }

    public class GeohashRequest
    {
        public required string KeyType { get; set; }
        public required string Name { get; set; }
    }

    public class NearbyUsersRequest
    {
        public required Coordinates Coordinates { get; set; }
        public int? Limit { get; set; } = 5;
        public int? DistanceRadius { get; set; } = 25;
    }

}
