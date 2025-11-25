package com.productordering.notificationservice.application.messaging.events;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.List;
import java.util.UUID;

@Data
public class OrderCreatedEvent {
    @JsonProperty("orderId")
    private UUID orderId;
    
    @JsonProperty("customerId")
    private UUID customerId;
    
    @JsonProperty("items")
    private List<OrderItemDto> items;
    
    @JsonProperty("totalAmount")
    private BigDecimal totalAmount;
    
    @JsonProperty("createdAt")
    private Instant createdAt;
    
    @Data
    public static class OrderItemDto {
        @JsonProperty("productId")
        private UUID productId;
        
        @JsonProperty("quantity")
        private int quantity;
        
        @JsonProperty("unitPrice")
        private BigDecimal unitPrice;
    }
}
