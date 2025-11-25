package com.productordering.notificationservice.domain.entities;

import com.productordering.notificationservice.domain.enums.NotificationStatus;
import com.productordering.notificationservice.domain.enums.NotificationType;
import lombok.Data;
import org.springframework.data.annotation.Id;
import org.springframework.data.mongodb.core.mapping.Document;

import java.time.Instant;
import java.util.UUID;

@Data
@Document(collection = "notifications")
public class Notification {
    @Id
    private String id;
    private UUID userId;
    private UUID orderId;
    private NotificationType type;
    private NotificationStatus status;
    private String recipient;
    private String subject;
    private String body;
    private String errorMessage;
    private Instant createdAt;
    private Instant sentAt;
    
    public static Notification create(
            UUID userId, 
            UUID orderId, 
            NotificationType type,
            String recipient, 
            String subject, 
            String body) {
        
        Notification notification = new Notification();
        notification.id = UUID.randomUUID().toString();
        notification.userId = userId;
        notification.orderId = orderId;
        notification.type = type;
        notification.status = NotificationStatus.PENDING;
        notification.recipient = recipient;
        notification.subject = subject;
        notification.body = body;
        notification.createdAt = Instant.now();
        return notification;
    }
    
    public void markAsSent() {
        this.status = NotificationStatus.SENT;
        this.sentAt = Instant.now();
    }
    
    public void markAsFailed(String errorMessage) {
        this.status = NotificationStatus.FAILED;
        this.errorMessage = errorMessage;
    }
}
