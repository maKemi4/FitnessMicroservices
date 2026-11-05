using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserProfileService.Data;
using UserProfileService.Models;

namespace UserProfileService.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(ProfilesDbContext db) : ControllerBase
{
    // GET: api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await db.Users.AsNoTracking()
            .OrderBy(user => user.Id)
            .ToListAsync();

        return Ok(users);
    }

    // GET: api/users/1
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
            return NotFound("Користувача не знайдено");

        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserProfile user)
    {
        if (!IsValid(user))
            return BadRequest("Некоректні дані користувача");

        var newUser = new UserProfile
        {
            Name = user.Name,
            Height = user.Height,
            Weight = user.Weight
        };

        db.Users.Add(newUser);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = newUser.Id }, newUser);
    }

    // PUT: api/users/1
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UserProfile updatedUser)
    {
        if (!IsValid(updatedUser))
            return BadRequest("Некоректні дані користувача");

        var changedRows = await db.Users
            .Where(user => user.Id == id)
            .ExecuteUpdateAsync(update => update
                .SetProperty(user => user.Name, updatedUser.Name)
                .SetProperty(user => user.Height, updatedUser.Height)
                .SetProperty(user => user.Weight, updatedUser.Weight));

        if (changedRows == 0)
            return NotFound("Користувача не знайдено");

        updatedUser.Id = id;
        return Ok(updatedUser);
    }

    // DELETE: api/users/1
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deletedRows = await db.Users
            .Where(user => user.Id == id)
            .ExecuteDeleteAsync();

        if (deletedRows == 0)
            return NotFound("Користувача не знайдено");

        return NoContent();
    }

    private static bool IsValid(UserProfile user)
    {
        return !string.IsNullOrWhiteSpace(user.Name)
            && double.IsFinite(user.Height)
            && double.IsFinite(user.Weight)
            && user.Height > 0
            && user.Weight > 0;
    }
}