using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Events;
using Nop.Core.Plugins;
using Nop.Services.Events;
using Nop.Services.Orders;
using TestSendSms;

namespace Nop.Plugin.SMS.Notification
{
    public class EventsConsumer : 
        IConsumer<OrderPlacedEvent>,
        IConsumer<OrderCancelledEvent>,
        IConsumer<EntityInserted<Customer>>,
        IConsumer<EntityInserted<Shipment>>
    {
        private readonly NotificationSettings _pluginSettings;
        private readonly IPluginFinder _pluginFinder;
        private readonly IOrderService _orderService;
        private readonly IStoreContext _storeContext;

        public EventsConsumer(NotificationSettings pluginSettings,
            IPluginFinder pluginFinder, 
            IOrderService orderService,
            IStoreContext storeContext)
        {
            this._pluginSettings = pluginSettings;
            this._pluginFinder = pluginFinder;
            this._orderService = orderService;
            this._storeContext = storeContext;
        }

        public void HandleEvent(OrderPlacedEvent eventMessage)
        {
            if (!_pluginSettings.Enabled)
                return;

            var order = eventMessage.Order;
            var number = GetUserPhoneNumber(order);
            if (string.IsNullOrEmpty(number))
            {
                return;
            }

            var message = _pluginSettings.MsgNewOrder;
            message = ReplaceUserData(message, order.Customer);
            message = ReplaceOrderData(message, order);

            new XmlSmsProvider(_pluginSettings.UserName, _pluginSettings.Password)
                .Send(_pluginSettings.Sender, message, number);
        }

        public void HandleEvent(OrderCancelledEvent eventMessage)
        {
            if (!_pluginSettings.Enabled)
                return;

            var order = eventMessage.Order;
            var number = GetUserPhoneNumber(order);
            if (string.IsNullOrEmpty(number))
            {
                return;
            }

            var message = _pluginSettings.MsgCancelledOrder;
            message = ReplaceUserData(message, order.Customer);
            message = ReplaceOrderData(message, order);

            new XmlSmsProvider(_pluginSettings.UserName, _pluginSettings.Password)
                .Send(_pluginSettings.Sender, message, number);
        }

        public void HandleEvent(EntityInserted<Customer> eventMessage)
        {
            //var user = eventMessage.Entity;
            //var number = GetUserPhoneNumber(user);
            //if (string.IsNullOrEmpty(number))
            //{
            //    return;
            //}
            
            //var message = _pluginSettings.MsgNewUser;
            //message = ReplaceUserData(message, user);

            //new XmlSmsProvider(_pluginSettings.UserName, _pluginSettings.Password)
            //    .Send(_pluginSettings.Sender, message, number);
        }

        public void HandleEvent(EntityInserted<Shipment> eventMessage)
        {
            var shipment = eventMessage.Entity;
            var number = GetUserPhoneNumber(shipment.Order);
            if (string.IsNullOrEmpty(number))
            {
                return;
            }
            
            var trackingNumber = shipment.TrackingNumber;
            var message = _pluginSettings.MsgShipment;
            message = message.Replace("{trackingNumber}", trackingNumber);
            message = ReplaceOrderData(message, shipment.Order);
            message = ReplaceUserData(message, shipment.Order.Customer);

            new XmlSmsProvider(_pluginSettings.UserName, _pluginSettings.Password)
                .Send(_pluginSettings.Sender, message, number);
        }

        private static string GetUserPhoneNumber(Customer customer)
        {
            var phoneNumber = customer.Addresses.Where(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .Select(x => x.PhoneNumber).FirstOrDefault();
            return phoneNumber ?? string.Empty;
        }

        private static string GetUserPhoneNumber(Order order)
        {
            var number = order.BillingAddress.PhoneNumber;
            if (string.IsNullOrEmpty(number))
            {
                number = GetUserPhoneNumber(order.Customer);
            }
            return number;
        }

        private static string ReplaceUserData(string msg, Customer customer)
        {
            return msg.Replace("{login}", customer.Username)
                .Replace("{systemName}", customer.SystemName)
                .Replace("{email}", customer.Email);
        }

        public static string ReplaceOrderData(string msg, Order order)
        {
            return msg.Replace("{orderId}", order.Id.ToString())
                .Replace("{orderItems}", order.OrderItems.Count.ToString())
                .Replace("{orderPrice}", order.OrderTotal.ToString());
        }
    }
}