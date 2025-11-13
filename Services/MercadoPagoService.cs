using CrudCloud.api.Data;
using CrudCloud.api.Data.Entities;
using CrudCloud.api.Models;
using CrudCloud.api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace CrudCloud.api.Services;

public class MercadoPagoService : IMercadoPagoService
{
    private readonly HttpClient _httpClient;
    private readonly MercadoPagoSettings _mpSettings;
    private readonly AppDbContext _context;
    private readonly ILogger<MercadoPagoService> _logger;
    private readonly IDiscordWebhookService _discordWebhookService;
    private readonly IEmailService _emailService;

    public MercadoPagoService(
        HttpClient httpClient,
        IOptions<MercadoPagoSettings> mpSettings,
        AppDbContext context,
        ILogger<MercadoPagoService> logger,
        IDiscordWebhookService discordWebhookService,
        IEmailService emailService)
    {
        _httpClient = httpClient;
        _mpSettings = mpSettings.Value;
        _context = context;
        _logger = logger;
        _discordWebhookService = discordWebhookService;
        _emailService = emailService;

        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_mpSettings.AccessToken}");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CrudCloud-API/1.0");
    }

    #region One-Time Payments (Pagos Únicos)

    public async Task<string> CreateOneTimePaymentAsync(int userId, string plan)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException($"User with ID {userId} not found.");

            var planConfig = GetPlanConfiguration(plan);
            var amount = planConfig.Price;
            var payerEmail = user.Correo;

            var paymentData = new
            {
                items = new[]
                {
                    new
                    {
                        title = $"Pago único plan {plan} - CrudCloud",
                        description = $"Acceso plan {plan} - Usuario: {userId}",
                        quantity = 1,
                        currency_id = "COP",
                        unit_price = amount
                    }
                },
                payer = new { email = payerEmail, name = $"{user.Nombre} {user.Apellido}" },
                back_urls = new
                {
                    success = $"{_mpSettings.FrontendBaseUrl}/payment/PaymentSuccess",
                    failure = $"{_mpSettings.FrontendBaseUrl}/payment/PaymentFailure",
                    pending = $"{_mpSettings.FrontendBaseUrl}/payment/PaymentPending"
                },
                auto_return = "approved",
                notification_url = $"{_mpSettings.WebhookBaseUrl}/api/payments/webhook",
                statement_descriptor = "CrudCloud",
                metadata = new
                {
                    user_id = userId,
                    plan = plan,
                    type = "one_time",
                    created_at = DateTime.UtcNow.ToString("o")
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(paymentData), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{_mpSettings.BaseUrl}/checkout/preferences", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Error creando preferencia de pago en MP: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new Exception($"Failed to create payment preference: {responseContent}");
            }

            var preference = JsonDocument.Parse(responseContent).RootElement;
            var initPoint = preference.GetProperty("init_point").GetString() ?? string.Empty;
            _logger.LogInformation("✅ Preferencia de pago único creada para usuario {UserId}", userId);
            return initPoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error crítico creando pago único para usuario {UserId}", userId);
            throw;
        }
    }

    #endregion

    #region Subscriptions (Suscripciones Recurrentes)

    public async Task<Subscription> CreateSubscriptionAsync(int userId, string plan)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found");

            var planConfig = GetPlanConfiguration(plan);
            var payerEmail = user.Correo;

            // ✅ CORRECCIÓN: Usamos el endpoint de Checkout Preferences.
            var checkoutData = new
            {
                reason = $"Suscripción Plan {plan} - CrudCloud",
                payer_email = payerEmail,
                // ✅ ITEMS ES OBLIGATORIO PARA CUALQUIER CHECKOUT
                items = new[] 
                {
                    new 
                    {
                        title = $"Suscripción Plan {plan} - CrudCloud",
                        description = $"Primer cobro de suscripción mensual",
                        quantity = 1,
                        currency_id = "COP", // o ARS, MXN, etc.
                        unit_price = planConfig.Price
                    }
                },
                back_urls = new 
                {
                    success = $"{_mpSettings.FrontendBaseUrl}/payment/PaymentSuccess",
                    failure = $"{_mpSettings.FrontendBaseUrl}/payment/PaymentFailure",
                    pending = $"{_mpSettings.FrontendBaseUrl}/payment/PaymentPending"
                },
                notification_url = $"{_mpSettings.WebhookBaseUrl}/api/payments/webhook",
                // ✅ LA CLAVE: Este objeto le dice al Checkout que cree una suscripción.
                auto_recurring = new
                {
                    frequency = 1,
                    frequency_type = "months",
                    transaction_amount = planConfig.Price,
                    currency_id = "COP"
                },
                metadata = new { user_id = userId, plan = plan, type = "subscription" }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(checkoutData), Encoding.UTF8, "application/json");
            
            // ✅ CORRECCIÓN: Apuntamos al endpoint de preferencias.
            var response = await _httpClient.PostAsync($"{_mpSettings.BaseUrl}/checkout/preferences", jsonContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("❌ Error de MP API al crear preferencia de suscripción: {StatusCode} - {Content}", response.StatusCode, responseContent);
                throw new Exception($"Failed to create subscription preference: {responseContent}");
            }
            
            var preference = JsonDocument.Parse(responseContent).RootElement;
            var preferenceId = preference.GetProperty("id").GetString() ?? string.Empty;
            var initPoint = preference.GetProperty("init_point").GetString() ?? string.Empty;

            var newSubscription = new Subscription
            {
                UserId = userId,
                // Guardamos el ID de la preferencia como referencia inicial
                MercadoPagoSubscriptionId = preferenceId,
                Status = "pending",
                Plan = plan,
                MonthlyPrice = planConfig.Price,
                StartDate = DateTime.UtcNow
            };
            _context.Subscriptions.Add(newSubscription);
            await _context.SaveChangesAsync();

            await _discordWebhookService.SendPaymentCreatedAsync(user.Correo, userId.ToString(), plan, planConfig.Price);
            
            newSubscription.MercadoPagoSubscriptionId = initPoint;
            return newSubscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error creando suscripción para usuario {UserId}", userId);
            throw;
        }
    }

    public async Task<Subscription?> GetUserSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    #endregion

    #region Webhook Processing (Lógica Común)

    public async Task<bool> ProcessPaymentNotificationAsync(PaymentNotification notification)
    {
        try
        {
            _logger.LogInformation("🔔 Procesando notificación de tipo: {Type}", notification.Type);
        
            switch (notification.Type)
            {
                case "payment":
                    return await ProcessPaymentWebhookAsync(notification);
            
                case "preapproval":
                    return await ProcessSubscriptionWebhookAsync(notification);
            
                default:
                    _logger.LogInformation("ℹ️ Notificación de tipo '{Type}' recibida y omitida (no requiere acción).", notification.Type);
                    return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error raíz al procesar la notificación de MercadoPago.");
            return false;
        }
    }

private async Task<bool> ProcessPaymentWebhookAsync(PaymentNotification notification)
{
    // 1. OBTENER PAYMENT ID DE FORMA SEGURA (Maneja string o number)
    string paymentId;
    
    if (notification.Data.ValueKind == JsonValueKind.Object && notification.Data.TryGetProperty("id", out var idElement))
    {
        if (idElement.ValueKind == JsonValueKind.String)
        {
            paymentId = idElement.GetString() ?? string.Empty;
        }
        else if (idElement.ValueKind == JsonValueKind.Number)
        {
            paymentId = idElement.GetInt64().ToString();
        }
        else
        {
            _logger.LogError("❌ No se pudo extraer payment_id: el valor no es string ni number.");
            return false;
        }
    }
    else
    {
        _logger.LogError("❌ No se pudo extraer el objeto 'data' o 'id' de la notificación.");
        return false;
    }

    if (string.IsNullOrEmpty(paymentId))
    {
        _logger.LogError("❌ Payment ID vacío después de la extracción.");
        return false;
    }

    try
    {
        // 2. CONSULTAR ESTADO REAL DEL PAGO EN MP
        var paymentResponse = await _httpClient.GetAsync($"{_mpSettings.BaseUrl}/v1/payments/{paymentId}");
        if (!paymentResponse.IsSuccessStatusCode)
        {
            _logger.LogError("❌ Error al consultar pago en MP API: {StatusCode} | PaymentId: {PaymentId}", paymentResponse.StatusCode, paymentId);
            return false;
        }
        
        var paymentData = await paymentResponse.Content.ReadAsStringAsync();
        var paymentInfo = JsonDocument.Parse(paymentData).RootElement;
        
        // 3. EXTRAER METADATA
        if (!paymentInfo.TryGetProperty("metadata", out var metadata) || !metadata.TryGetProperty("user_id", out var userIdElement))
        {
             _logger.LogError("❌ El pago {PaymentId} no tiene metadata o 'user_id' es inválido.", paymentId);
            return false;
        }
        
        var userId = userIdElement.GetInt32();
        var plan = metadata.TryGetProperty("plan", out var p) ? p.GetString() ?? "desconocido" : "desconocido";
        var status = paymentInfo.GetProperty("status").GetString() ?? "unknown";

        // 4. CREAR/ACTUALIZAR REGISTRO DE PAGO EN DB
        var existingPayment = await _context.Payments.FirstOrDefaultAsync(p => p.MercadoPagoPaymentId == paymentId);
        if (existingPayment != null)
        {
            if (existingPayment.Status == status) return true;
            existingPayment.Status = status;
            existingPayment.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Crea un nuevo registro
            _context.Payments.Add(new Payment
            {
                UserId = userId,
                MercadoPagoPaymentId = paymentId,
                Status = status ?? "unknown",
                Plan = plan ?? "desconocido",
                Amount = paymentInfo.GetProperty("transaction_amount").GetDecimal(),
                Currency = paymentInfo.GetProperty("currency_id").GetString() ?? "COP",
                Description = paymentInfo.GetProperty("description").GetString() ?? string.Empty,
                PaymentMethod = paymentInfo.GetProperty("payment_method_id").GetString() ?? "unknown",
                PaymentType = paymentInfo.GetProperty("payment_type_id").GetString() ?? "unknown",
            });
        }
        
        await _context.SaveChangesAsync();
        
        // 5. MANEJO DE ESTADO DE USUARIO Y NOTIFICACIONES
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.LogError("❌ Usuario {UserId} no encontrado. No se puede actualizar el plan.", userId);
            return true;
        }

        if (status == "approved")
        {
            // Se ejecuta en el PRIMER PAGO o si es un cobro recurrente EXITOSO.
            if (user.Plan != plan)
            {
                var oldPlan = user.Plan;
                user.Plan = plan;
                await _context.SaveChangesAsync();
                await NotifySuccess(user, oldPlan, plan ?? "desconocido", paymentInfo.GetProperty("transaction_amount").GetDecimal(), paymentId);
            }
        }
        else if (status == "rejected" || status == "chargeback")
        {
            // Se ejecuta si es un cobro recurrente FALLIDO.
            if (user.Plan != "gratis")
            {
                var oldPlan = user.Plan;
                user.Plan = "suspendido";
                await _context.SaveChangesAsync();
                
                await _discordWebhookService.SendPaymentRejectedAsync(user.Correo, user.Id.ToString(), plan ?? "desconocido", paymentInfo.GetProperty("transaction_amount").GetDecimal());
            }
        }
        
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error procesando webhook de pago con ID: {PaymentId}", paymentId);
        return false;
    }
}
    
    private async Task<bool> ProcessSubscriptionWebhookAsync(PaymentNotification notification)
    {
        if (!notification.Data.TryGetProperty("id", out var idElement)) return false;
        var subscriptionId = idElement.GetString();
        if (string.IsNullOrEmpty(subscriptionId)) return false;

        try
        {
            var response = await _httpClient.GetAsync($"{_mpSettings.BaseUrl}/preapproval/{subscriptionId}");
            if(!response.IsSuccessStatusCode)
            {
                _logger.LogError("Error al consultar suscripción en MP API: {StatusCode}", response.StatusCode);
                return false;
            }
            var responseData = await response.Content.ReadAsStringAsync();
            var subscriptionInfo = JsonDocument.Parse(responseData).RootElement;
            
            if (!subscriptionInfo.TryGetProperty("metadata", out var metadata) || !subscriptionInfo.TryGetProperty("user_id", out var userIdElement))
            {
                _logger.LogError("La suscripción {SubscriptionId} no tiene 'user_id' en metadata.", subscriptionId);
                return false;
            }
            
            var userId = userIdElement.GetInt32();
            var status = subscriptionInfo.GetProperty("status").GetString() ?? "unknown";
            
            var localSubscription = await _context.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
            if(localSubscription != null)
            {
                localSubscription.MercadoPagoSubscriptionId = subscriptionId;
                localSubscription.Status = status;
                localSubscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Suscripción local actualizada a estado '{Status}' para el usuario {UserId}", status, userId);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de SUSCRIPCIÓN con ID: {SubscriptionId}", subscriptionId);
            return false;
        }
    }
    
    private async Task NotifySuccess(User user, string oldPlan, string newPlan, decimal amount, string paymentId)
    {
        try
        {
            // Nota: Estos métodos deben existir en IDiscordWebhookService e IEmailService
            await _discordWebhookService.SendPlanUpdatedAsync(user.Correo, user.Id.ToString(), oldPlan, newPlan, amount);
            await _emailService.SendPaymentConfirmationAsync(user.Correo, user.Nombre, newPlan, amount, paymentId);
            await _emailService.SendPlanUpgradeNotificationAsync(user.Correo, user.Nombre, oldPlan, newPlan);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "⚠️ Error al enviar notificaciones para el usuario {UserId}, pero el pago fue procesado.", user.Id);
        }
    }

    #endregion

    #region Helper Methods

    public (string MpPlanId, decimal Price) GetPlanConfiguration(string plan)
    {
        // ✅ USANDO LOS IDS FINALES DE PRODUCCIÓN PARA EL VENDEDOR DE PRUEBA
        return plan.ToLowerInvariant() switch
        {
            "intermedio" => ("054c4834a79a458788af359413ba9b10", 1600.00m),
            "avanzado"   => ("5a2aadafe4db42aa9c2a73d76bd68109", 1600.00m),
            _ => throw new ArgumentException($"El plan '{plan}' es inválido.")
        };
    }
    
    public async Task<Payment?> GetPaymentByMpIdAsync(string mercadoPagoPaymentId)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.MercadoPagoPaymentId == mercadoPagoPaymentId);
    }

    #endregion
}