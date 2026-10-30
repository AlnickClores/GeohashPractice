using MongoDB.Driver;
using Microsoft.Extensions.Options;
using DeveloperNotes.Settings;
using DeveloperNotes.Models;

namespace DeveloperNotes.Services
{
    public class MongoDbService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<DeveloperNote> _developerNotes;

        public MongoDbService(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            var database = client.GetDatabase(settings.Value.ConnectionString);

            _developerNotes = database.GetCollection<DeveloperNote>("DeveloperNotes");
        }

        public async Task CreateDeveloperNote(DeveloperNote developerNote)
        {
            await _developerNotes.InsertOneAsync(developerNote);
        }
    }
}
