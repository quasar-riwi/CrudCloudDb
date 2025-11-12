using CrudCloud.api.Data.Entities;
using CrudCloud.api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrudCloud.api.Services.Interfaces;

public interface IMercadoPagoService
{
    Task<Subscription> CreateSubscriptionAsync(int userId, string plan);
    
    Task<bool> ProcessPaymentNotificationAsync(PaymentNotification notification);
    
    Task<Subscription> GetUserSubscriptionAsync(int userId);
    
    Task<string> CreateOneTimePaymentAsync(int userId, string plan);

    Task<Payment> GetPaymentByMpIdAsync(string mercadoPagoPaymentId);
    (string MpPlanId, decimal Price) GetPlanConfiguration(string plan);
}