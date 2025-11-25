package com.productordering.notificationservice.application.messaging.consumers;

import com.productordering.notificationservice.application.messaging.events.PaymentFailedEvent;
import com.productordering.notificationservice.application.services.NotificationService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
public class PaymentFailedConsumer {
    
    private final NotificationService notificationService;
    
    @RabbitListener(queues = "${rabbitmq.queues.payment-failed}")
    public void handlePaymentFailed(PaymentFailedEvent event) {
        log.info("Received PaymentFailedEvent for Order ID: {}, Payment ID: {}, Reason: {}", 
            event.getOrderId(), event.getPaymentId(), event.getReason());
        
        try {
            notificationService.sendPaymentFailedEmail(event);
        } catch (Exception ex) {
            log.error("Error processing PaymentFailedEvent for Order ID: {}", 
                event.getOrderId(), ex);
            throw ex;
        }
    }
}
