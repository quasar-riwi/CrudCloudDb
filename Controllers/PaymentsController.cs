using CrudCloud.api.Models;
using CrudCloud.api.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace CrudCloud.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IMercadoPagoService _mercadoPagoService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IMercadoPagoService mercadoPagoService, ILogger<PaymentsController> logger)
    {
        _mercadoPagoService = mercadoPagoService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a subscription preference for the authenticated user.
    /// </summary>
    [HttpPost("subscribe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated." });

            var subscription = await _mercadoPagoService.CreateSubscriptionAsync(userId.Value, request.Plan);
            
            return Ok(new
            {
                message = "Subscription preference created successfully. Redirect user to initPointUrl.",
                initPointUrl = subscription.MercadoPagoSubscriptionId, 
                plan = subscription.Plan,
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request while creating subscription.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating subscription for user {UserId}.", GetCurrentUserId());
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Gets the current user's most recent subscription.
    /// </summary>
    [HttpGet("subscription")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated." });

            var subscription = await _mercadoPagoService.GetUserSubscriptionAsync(userId.Value);
            
            if (subscription == null)
                return NotFound(new { message = "No subscription found for this user." });

            return Ok(subscription!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subscription for user {UserId}.", GetCurrentUserId());
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

    /// <summary>
    /// Creates a preference for a one-time payment.
    /// </summary>
    [HttpPost("one-time-payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOneTimePayment([FromBody] CreateSubscriptionRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated." });

            var paymentUrl = await _mercadoPagoService.CreateOneTimePaymentAsync(userId.Value, request.Plan);
            var planConfig = _mercadoPagoService.GetPlanConfiguration(request.Plan);
            
            return Ok(new
            {
                message = "Payment preference created successfully. Redirect user to paymentUrl.",
                paymentUrl,
                plan = request.Plan,
                amount = planConfig.Price
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Bad request while creating one-time payment.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating one-time payment for user {UserId}.", GetCurrentUserId());
            return StatusCode(500, new { message = "An internal error occurred." });
        }
    }

/// <summary>
    /// Webhook endpoint for Mercado Pago to send notifications.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            _logger.LogInformation("--> Received MercadoPago raw webhook: {Body}", body); // Log del RAW Body

            // 1. Deserializar el JSON a un JsonDocument genérico para inspeccionarlo
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            
            // 2. Extraer el 'type' o 'topic' de forma segura
            string notificationType = string.Empty;
            if (root.TryGetProperty("type", out var typeElement))
            {
                notificationType = typeElement.GetString() ?? string.Empty;
            }
            else if (root.TryGetProperty("topic", out var topicElement))
            {
                // Para el caso de 'merchant_order' o 'invoice'
                notificationType = topicElement.GetString() ?? string.Empty;
            }

            // 3. Si el tipo no es 'payment' o 'preapproval', lo ignoramos.
            if (notificationType != "payment" && notificationType != "preapproval")
            {
                _logger.LogInformation("--> Webhook ignored: Type '{Type}'.", notificationType);
                return Ok(); // Devolver 200 OK es CRUCIAL para evitar reintentos.
            }
            
            // 4. Deserializar el body a nuestro modelo DENTRO del switch/if (No, es muy complejo).
            // Lo más simple es pasar el body completo y dejar que el servicio lo filtre.
            
            // 5. Deserializa el body a nuestro modelo (que ahora tiene la lógica de JsonPropertyNames)
            var notification = JsonSerializer.Deserialize<PaymentNotification>(body);

            if (notification == null)
            {
                _logger.LogError("--> Critical Error: Could not deserialize body to PaymentNotification.");
                return StatusCode(500);
            }

            var success = await _mercadoPagoService.ProcessPaymentNotificationAsync(notification);
            
            _logger.LogInformation("--> Webhook processed. Success: {Success}", success);
            return Ok(); // Siempre devolver 200 OK.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "--> CRITICAL: Unhandled exception processing MercadoPago webhook.");
            return StatusCode(500); // Esto causará reintentos, pero es el último recurso.
        }
    }

    /// <summary>
    /// Test webhook simulation (for development)
    /// </summary>
    [HttpPost("test-webhook")]
    [Authorize]
    public async Task<IActionResult> TestWebhook([FromBody] PaymentNotification testNotification)
    {
        _logger.LogInformation("--> Simulating webhook: {Data}", JsonSerializer.Serialize(testNotification));
        var success = await _mercadoPagoService.ProcessPaymentNotificationAsync(testNotification);
        return Ok(new { message = "Test webhook processed", success });
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}