package com.productordering.notificationservice.application.messaging.events;

import com.fasterxml.jackson.annotation.JsonProperty;
import lombok.Data;

import java.time.Instant;
import java.util.UUID;

@Data
public class PaymentFailedEvent {
    @JsonProperty("paymentId")
    private UUID paymentId;
    
    @JsonProperty("orderId")
    private UUID orderId;
    
    @JsonProperty("reason")
    private String reason;
    
    @JsonProperty("failedAt")
    private Instant failedAt;
}
