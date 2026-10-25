using Microsoft.AspNetCore.Mvc;
using UserProfileService.Models;
using System.Collections.Concurrent;

namespace UserProfileService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private static readonly ConcurrentDictionary<int, UserProfile>
            Users = new();

        private static int _nextId = 0;

        // GET: api/users
        [HttpGet]
        public IActionResult GetAll()
        {
            return Ok(Users.Values.OrderBy(u => u.Id));
        }

        // GET: api/users/1
        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            if (!Users.TryGetValue(id, out var user))
                return NotFound("Користувача не знайдено");

            return Ok(user);
        }

        // POST: api/users
        [HttpPost]
        public IActionResult Create([FromBody] UserProfile user)
        {
            if (string.IsNullOrWhiteSpace(user.Name) ||
                !IsValidParameters(user.Height, user.Weight))
            {
                return BadRequest("Некоректні дані користувача");
            }

            var id = Interlocked.Increment(ref _nextId);

            var newUser = new UserProfile
            {
                Id = id,
                Name = user.Name,
                Height = user.Height,
                Weight = user.Weight
            };

            Users[id] = newUser;

            return CreatedAtAction(
                nameof(GetById),
                new { id },
                newUser
            );
        }

        // PUT: api/users/1
        [HttpPut("{id:int}")]
        public IActionResult Update(
            int id,
            [FromBody] UserProfile updatedUser)
        {
            if (string.IsNullOrWhiteSpace(updatedUser.Name) ||
                !IsValidParameters(
                    updatedUser.Height,
                    updatedUser.Weight))
            {
                return BadRequest("Некоректні дані користувача");
            }

            if (!Users.ContainsKey(id))
                return NotFound("Користувача не знайдено");

            var user = new UserProfile
            {
                Id = id,
                Name = updatedUser.Name,
                Height = updatedUser.Height,
                Weight = updatedUser.Weight
            };

            Users[id] = user;

            return Ok(user);
        }

        // DELETE: api/users/1
        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            if (!Users.TryRemove(id, out _))
                return NotFound("Користувача не знайдено");

            return NoContent();
        }

        private static bool IsValidParameters(
            double height,
            double weight)
        {
            return double.IsFinite(height) &&
                   double.IsFinite(weight) &&
                   height > 0 &&
                   weight > 0;
        }
    }
}
