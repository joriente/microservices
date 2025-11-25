package com.productordering.notificationservice.application.messaging.events;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;

import java.math.BigDecimal;
import java.time.Instant;
import java.util.UUID;

@Data
public class PaymentProcessedEvent {
    @JsonProperty("paymentId")
    private UUID paymentId;
    
    @JsonProperty("orderId")
    private UUID orderId;
    
    @JsonProperty("stripePaymentIntentId")
    private String stripePaymentIntentId;
    
    @JsonProperty("amount")
    private BigDecimal amount;
    
    @JsonProperty("currency")
    private String currency;
    
    @JsonProperty("processedAt")
    private Instant processedAt;
}
