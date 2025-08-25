using Microsoft.EntityFrameworkCore;
using VSMSWebServer.Data;
using VSMSWebServer.Models;

namespace VSMSWebServer.Services
{
    public class RequestRepositoryService
    {
        private readonly AppDbContext _context;

        public RequestRepositoryService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRequestAsync(Request request) 
        {
            _context.Requests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<Request?> GetRequestByUuidAsync(string uuid)
        {
            return await _context.Requests.FirstOrDefaultAsync(r => r.Uuid == uuid);
        }

        public async Task UpdateRequestAsync(Request request)
        {
            _context.Requests.Update(request);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Request>> GetAllRequestsAsync()
        {
            return await _context.Requests.ToListAsync();
        }

        public async Task<bool> UpdateRequestStatusAsync(string uuid, string status)
        {
            // Find a record by UUID
            var request = await _context.Requests
                .FirstOrDefaultAsync(r => r.Uuid == uuid);

            if (request == null)
            {
                return false; // Record not found
            }

            // Update the status
            request.Status = status;

            // Save changes to the database
            await _context.SaveChangesAsync();

            return true; 
        }

        public async Task<bool> AddRequestIfNotExistsAsync(Request request)
        {
            // Check if a record with such UUID exists
            var existingRequest = await _context.Requests
                .FirstOrDefaultAsync(r => r.Uuid == request.Uuid);

            if (existingRequest != null)
            {
                return false; // The entry already exists
            }

            // Create a new entry without ID (will be generated automatically)
            var newRequest = new Request
            {
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Uuid = request.Uuid,
                Status = request.Status,
                Message = request.Message,
                SendTime = request.SendTime
            };

            _context.Requests.Add(newRequest);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> AddBulkRequestsIfNotExistAsync(List<Request> requests)
        {
            if (requests == null || !requests.Any())
                return 0;

            // Get all existing UUIDs
            var existingUuids = await _context.Requests
                .Where(r => requests.Select(x => x.Uuid).Contains(r.Uuid))
                .Select(r => r.Uuid)
                .ToListAsync();

            // Filter only new entries
            var newRequests = requests
                .Where(r => !existingUuids.Contains(r.Uuid))
                .Select(r => new Request
                {
                    FirstName = r.FirstName,
                    SecondName = r.SecondName,
                    LastName = r.LastName,
                    PhoneNumber = r.PhoneNumber,
                    Uuid = r.Uuid,
                    Status = r.Status,
                    Message = r.Message,
                    SendTime = r.SendTime
                })
                .ToList();

            if (newRequests.Count != 0)
            {
                await _context.Requests.AddRangeAsync(newRequests);
                await _context.SaveChangesAsync();
            }

            return newRequests.Count;
        }
    }
}