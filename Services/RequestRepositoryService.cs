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
                // Create a new entry if not found
                var newRequest = new Request
                {
                    Uuid = uuid,
                    Status = status,
                    
                    FirstName = string.Empty,
                    SecondName = string.Empty,
                    LastName = string.Empty,
                    PhoneNumber = string.Empty,
                    Message = string.Empty,
                    SendTime = DateTime.UtcNow.ToString("HH:mm dd.MM.yy")
                };

                _context.Requests.Add(newRequest);
            }
            else
            {
                // Update the status of existing record
                request.Status = status;
                _context.Requests.Update(request);
            }

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

            // We get only those records whose UUID is in the input data.
            var existingRequests = await _context.Requests
                .Where(r => requests.Select(x => x.Uuid).Contains(r.Uuid))
                .ToListAsync();

            var newRequests = new List<Request>();

            foreach (var req in requests)
            {
                var existing = existingRequests.FirstOrDefault(r => r.Uuid == req.Uuid);


                if (existing == null)
                {
                    // There is no UUID -> create a new record
                    newRequests.Add(new Request
                    {
                        FirstName = req.FirstName,
                        SecondName = req.SecondName,
                        LastName = req.LastName,
                        PhoneNumber = req.PhoneNumber,
                        Uuid = req.Uuid,
                        Status = req.Status,
                        Message = req.Message,
                        SendTime = req.SendTime
                    });
                }
                else if (string.IsNullOrEmpty(existing.PhoneNumber))
                {
                    // UUID matched -> updating all fields except Status
                    existing.FirstName = req.FirstName;
                    existing.SecondName = req.SecondName;
                    existing.LastName = req.LastName;
                    existing.PhoneNumber = req.PhoneNumber;
                    existing.Message = req.Message;
                    existing.SendTime = req.SendTime;

                    _context.Requests.Update(existing);
                }
            }

            if (newRequests.Count > 0)
            {
                await _context.Requests.AddRangeAsync(newRequests);
            }

            var affected = await _context.SaveChangesAsync();

            return affected;
        }

        public async Task<List<Request>> GetRequestsUpdatedSinceAsync(long sinceTimestamp)
        {
            return await _context.Requests
                .Where(r => r.UpdatedAt > sinceTimestamp)
                .ToListAsync();
        }
    }
}