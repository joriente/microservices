package com.productordering.notificationservice.presentation.controllers;

import com.productordering.notificationservice.domain.entities.Notification;
import com.productordering.notificationservice.domain.repositories.NotificationRepository;
import lombok.RequiredArgsConstructor;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.UUID;

@RestController
@RequestMapping("/api/notifications")
@RequiredArgsConstructor
public class NotificationController {
    
    private final NotificationRepository notificationRepository;
    
    @GetMapping("/user/{userId}")
    public ResponseEntity<List<Notification>> getNotificationsByUser(
            @PathVariable UUID userId) {
        List<Notification> notifications = notificationRepository.findByUserId(userId);
        return ResponseEntity.ok(notifications);
    }
    
    @GetMapping("/order/{orderId}")
    public ResponseEntity<List<Notification>> getNotificationsByOrder(
            @PathVariable UUID orderId) {
        List<Notification> notifications = notificationRepository.findByOrderId(orderId);
        return ResponseEntity.ok(notifications);
    }
    
    @GetMapping
    public ResponseEntity<List<Notification>> getAllNotifications() {
        List<Notification> notifications = notificationRepository.findAll();
        return ResponseEntity.ok(notifications);
    }
}
