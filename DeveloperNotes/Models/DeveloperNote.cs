using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes; 

namespace DeveloperNotes.Models
{
    public class DeveloperNote
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]

        public String Id { get; set; } = String.Empty;
        public String Title { get; set; } = String.Empty;
        public String Note { get; set; } = String.Empty;
        public DateTime DateCreated { get; set; }
    }
}
