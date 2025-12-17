package com.productordering.notificationservice.application.services;

import com.productordering.notificationservice.application.messaging.events.OrderCreatedEvent;
import com.productordering.notificationservice.application.messaging.events.PaymentFailedEvent;
import com.productordering.notificationservice.application.messaging.events.PaymentProcessedEvent;
import com.productordering.notificationservice.domain.entities.Notification;
import com.productordering.notificationservice.domain.enums.NotificationType;
import com.productordering.notificationservice.domain.repositories.NotificationRepository;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Async;
import org.springframework.stereotype.Service;
import org.thymeleaf.TemplateEngine;
import org.thymeleaf.context.Context;

@Slf4j
@Service
@RequiredArgsConstructor
public class NotificationService {
    
    private final EmailService emailService;
    private final NotificationRepository notificationRepository;
    private final TemplateEngine templateEngine;
    
    @Async
    public void sendOrderConfirmationEmail(OrderCreatedEvent event) {
        log.info("Sending order confirmation email for Order: {}", event.getOrderId());
        
        try {
            // Build email content using Thymeleaf template
            Context context = new Context();
            context.setVariable("orderId", event.getOrderId());
            context.setVariable("totalAmount", event.getTotalAmount());
            context.setVariable("itemCount", event.getItems().size());
            
            String htmlContent = templateEngine.process("order-confirmation", context);
            
            String subject = "Order Confirmation - Order #" + event.getOrderId();
            String recipient = "customer@example.com"; // TODO: Get from user service
            
            // Create notification record
            Notification notification = Notification.create(
                event.getCustomerId(),
                event.getOrderId(),
                NotificationType.ORDER_CONFIRMATION,
                recipient,
                subject,
                htmlContent
            );
            
            try {
                notificationRepository.save(notification);
            } catch (Exception dbEx) {
                log.warn("Failed to save notification record to database: {}", dbEx.getMessage());
                // Continue anyway - email is more important than record
            }
            
            // Send email
            emailService.sendEmail(recipient, subject, htmlContent);
            
            // Mark as sent
            try {
                notification.markAsSent();
                notificationRepository.save(notification);
            } catch (Exception dbEx) {
                log.warn("Failed to update notification status: {}", dbEx.getMessage());
            }
            
            log.info("Successfully sent order confirmation email for Order: {}", event.getOrderId());
            
        } catch (Exception ex) {
            log.error("Failed to send order confirmation email for Order: {}", 
                event.getOrderId(), ex);
        }
    }
    
    @Async
    public void sendPaymentSuccessEmail(PaymentProcessedEvent event) {
        log.info("Sending payment success email for Order: {}", event.getOrderId());
        
        try {
            Context context = new Context();
            context.setVariable("orderId", event.getOrderId());
            context.setVariable("paymentId", event.getPaymentId());
            context.setVariable("amount", event.getAmount());
            context.setVariable("currency", event.getCurrency());
            
            String htmlContent = templateEngine.process("payment-success", context);
            
            String subject = "Payment Successful - Order #" + event.getOrderId();
            String recipient = "customer@example.com"; // TODO: Get from user service
            
            Notification notification = Notification.create(
                null, // TODO: Get userId from order
                event.getOrderId(),
                NotificationType.PAYMENT_SUCCESS,
                recipient,
                subject,
                htmlContent
            );
            
            try {
                notificationRepository.save(notification);
            } catch (Exception dbEx) {
                log.warn("Failed to save notification record to database: {}", dbEx.getMessage());
            }
            
            emailService.sendEmail(recipient, subject, htmlContent);
            
            try {
                notification.markAsSent();
                notificationRepository.save(notification);
            } catch (Exception dbEx) {
                log.warn("Failed to update notification status: {}", dbEx.getMessage());
            }
            
            log.info("Successfully sent payment success email for Order: {}", event.getOrderId());
            
        } catch (Exception ex) {
            log.error("Failed to send payment success email for Order: {}", 
                event.getOrderId(), ex);
        }
    }
    
    @Async
    public void sendPaymentFailedEmail(PaymentFailedEvent event) {
        log.info("Sending payment failed email for Order: {}", event.getOrderId());
        
        try {
            Context context = new Context();
            context.setVariable("orderId", event.getOrderId());
            context.setVariable("paymentId", event.getPaymentId());
            context.setVariable("reason", event.getReason());
            
            String htmlContent = templateEngine.process("payment-failed", context);
            
            String subject = "Payment Failed - Order #" + event.getOrderId();
            String recipient = "customer@example.com"; // TODO: Get from user service
            
            Notification notification = Notification.create(
                null, // TODO: Get userId from order
                event.getOrderId(),
                NotificationType.PAYMENT_FAILED,
                recipient,
                subject,
                htmlContent
            );
            
            try {
                notificationRepository.save(notification);
            } catch (Exception dbEx) {
                log.warn("Failed to save notification record to database: {}", dbEx.getMessage());
            }
            
            emailService.sendEmail(recipient, subject, htmlContent);
            
            try {
                notification.markAsSent();
                notificationRepository.save(notification);
            } catch (Exception dbEx) {
                log.warn("Failed to update notification status: {}", dbEx.getMessage());
            }
            
            log.info("Successfully sent payment failed email for Order: {}", event.getOrderId());
            
        } catch (Exception ex) {
            log.error("Failed to send payment failed email for Order: {}", 
                event.getOrderId(), ex);
        }
    }
}
