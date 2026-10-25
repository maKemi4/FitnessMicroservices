using Microsoft.AspNetCore.Mvc;
using LoadCalculationService.Models;
using System.Net;
using System.Net.Http.Json;

namespace LoadCalculationService.Controllers
{
    [ApiController]
    [Route("api/load")]
    public class LoadCalculationController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        public LoadCalculationController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient(
                "UserProfileService");
        }

        // GET: api/load/1
        [HttpGet("{userId:int}")]
        public async Task<IActionResult> Calculate(int userId)
        {
            if (userId <= 0)
                return BadRequest("Некоректний ID користувача");

            UserProfile? user;

            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/users/{userId}");

                if (response.StatusCode == HttpStatusCode.NotFound)
                    return NotFound("Користувача не знайдено");

                response.EnsureSuccessStatusCode();

                user = await response.Content
                    .ReadFromJsonAsync<UserProfile>();
            }
            catch (HttpRequestException)
            {
                return StatusCode(
                    503,
                    "UserProfileService недоступний");
            }
            catch (TaskCanceledException)
            {
                return StatusCode(
                    504,
                    "Час очікування відповіді вичерпано");
            }

            if (user == null ||
                !double.IsFinite(user.Height) ||
                !double.IsFinite(user.Weight) ||
                user.Height <= 0 ||
                user.Weight <= 0)
            {
                return BadRequest(
                    "Некоректні фізичні параметри користувача");
            }

            double heightMeters = user.Height / 100.0;

            double bmi = user.Weight /
                (heightMeters * heightMeters);

            bmi = Math.Round(bmi, 2);

            string loadLevel;
            int minutes;
            string recommendation;

            // Спрощені демонстраційні правила для лабораторної
            if (bmi < 18.5)
            {
                loadLevel = "Low";
                minutes = 15;
                recommendation = "Легка фізична активність";
            }
            else if (bmi < 25)
            {
                loadLevel = "Moderate";
                minutes = 30;
                recommendation = "Помірна фізична активність";
            }
            else
            {
                loadLevel = "Low";
                minutes = 20;
                recommendation = "Прогулянка у комфортному темпі";
            }

            var plan = new TrainingPlan
            {
                UserId = user.Id,
                BMI = bmi,
                LoadLevel = loadLevel,
                RecommendedMinutes = minutes,
                Recommendation = recommendation,
                CreatedAt = DateTime.UtcNow
            };

            Response.Headers["X-Service-Instance"] = Environment.MachineName;

            return Ok(plan);
        }
    }
}