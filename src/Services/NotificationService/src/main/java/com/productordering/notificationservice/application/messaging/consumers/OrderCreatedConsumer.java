package com.productordering.notificationservice.application.messaging.consumers;

import com.productordering.notificationservice.application.messaging.events.OrderCreatedEvent;
import com.productordering.notificationservice.application.services.NotificationService;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Slf4j
@Component
@RequiredArgsConstructor
public class OrderCreatedConsumer {
    
    private final NotificationService notificationService;
    
    @RabbitListener(queues = "${rabbitmq.queues.order-created}")
    public void handleOrderCreated(OrderCreatedEvent event) {
        log.info("Received OrderCreatedEvent for Order ID: {}, Customer ID: {}, Total: {}", 
            event.getOrderId(), event.getCustomerId(), event.getTotalAmount());
        
        try {
            notificationService.sendOrderConfirmationEmail(event);
        } catch (Exception ex) {
            log.error("Error processing OrderCreatedEvent for Order ID: {}", 
                event.getOrderId(), ex);
            throw ex; // Re-throw to trigger retry mechanism
        }
    }
}
