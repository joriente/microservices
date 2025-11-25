package com.productordering.notificationservice;

import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.TestPropertySource;

@SpringBootTest
@TestPropertySource(properties = {
    "spring.data.mongodb.uri=mongodb://localhost:27017/test",
    "spring.rabbitmq.host=localhost",
    "sendgrid.enabled=false"
})
class NotificationServiceApplicationTests {

    @Test
    void contextLoads() {
        // Basic test to ensure Spring context loads
    }
}
