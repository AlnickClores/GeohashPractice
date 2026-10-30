using DeveloperNotes.DTOs;
using DeveloperNotes.Models;
using DeveloperNotes.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeveloperNotes.Controllers
{
    [ApiController]
    [Route("api/developer-notes")]
    public class DeveloperNotes
    {
        public class DeveloperNotesController : ControllerBase
        {
            private readonly MongoDbService _mongoDbService;

            public DeveloperNotesController(MongoDbService mongoDbService) 
            {
                _mongoDbService = mongoDbService;
            }

            [HttpPost]
             
            public async Task<IActionResult>Create(CreateNoteDto dto)
            {
                var developerNote = new DeveloperNote
                {
                    Title = dto.Title,
                    Note = dto.Note
                };

                await _mongoDbService.CreateDeveloperNote(developerNote);

                return Created("", developerNote);
            }
        }
    }
}
