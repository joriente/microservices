package com.productordering.notificationservice.application.messaging.consumers;

import com.productordering.notificationservice.application.messaging.events.PaymentProcessedEvent;
import com.productordering.notificationservice.application.services.NotificationService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
public class PaymentProcessedConsumer {
    
    private final NotificationService notificationService;
    
    @RabbitListener(queues = "${rabbitmq.queues.payment-processed}")
    public void handlePaymentProcessed(PaymentProcessedEvent event) {
        log.info("Received PaymentProcessedEvent for Order ID: {}, Payment ID: {}, Amount: {} {}", 
            event.getOrderId(), event.getPaymentId(), event.getAmount(), event.getCurrency());
        
        try {
            notificationService.sendPaymentSuccessEmail(event);
        } catch (Exception ex) {
            log.error("Error processing PaymentProcessedEvent for Order ID: {}", 
                event.getOrderId(), ex);
            throw ex;
        }
    }
}
