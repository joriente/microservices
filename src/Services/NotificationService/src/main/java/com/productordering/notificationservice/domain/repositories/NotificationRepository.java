package com.productordering.notificationservice.domain.repositories;

import com.productordering.notificationservice.domain.entities.Notification;
import org.springframework.data.mongodb.repository.MongoRepository;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.UUID;

@Repository
public interface NotificationRepository extends MongoRepository<Notification, String> {
    List<Notification> findByUserId(UUID userId);
    List<Notification> findByOrderId(UUID orderId);
}
