package com.productordering.notificationservice.infrastructure.messaging;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.amqp.core.Message;
import org.springframework.amqp.core.MessageProperties;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConversionException;

/**
 * Custom message converter that handles MassTransit message envelope format.
 * MassTransit wraps messages in an envelope with metadata. This converter
 * extracts the actual message payload from the "message" property.
 */
public class MassTransitMessageConverter extends Jackson2JsonMessageConverter {
    
    private static final Logger log = LoggerFactory.getLogger(MassTransitMessageConverter.class);
    private final ObjectMapper objectMapper;
    
    public MassTransitMessageConverter(ObjectMapper objectMapper) {
        super(objectMapper);
        this.objectMapper = objectMapper;
    }
    
    @Override
    public Object fromMessage(Message message, Object conversionHint) throws MessageConversionException {
        try {
            // Read the raw message as JSON
            String json = new String(message.getBody());
            log.info("Received RabbitMQ message: {}", json);
            
            JsonNode rootNode = objectMapper.readTree(json);
            
            // Check if this is a MassTransit envelope (has "message" property)
            if (rootNode.has("message")) {
                log.info("Detected MassTransit envelope format");
                // Extract the actual message payload
                JsonNode messageNode = rootNode.get("message");
                log.info("Extracted message payload: {}", messageNode.toString());
                log.info("ConversionHint type: {}, value: {}", 
                    conversionHint != null ? conversionHint.getClass().getName() : "null", 
                    conversionHint);
                
                // Get the target class
                Class<?> targetClass = null;
                if (conversionHint instanceof Class) {
                    targetClass = (Class<?>) conversionHint;
                } else {
                    // Try to get class from message properties
                    MessageProperties properties = message.getMessageProperties();
                    if (properties != null && properties.getInferredArgumentType() != null) {
                        java.lang.reflect.Type type = properties.getInferredArgumentType();
                        if (type instanceof Class) {
                            targetClass = (Class<?>) type;
                            log.info("Got target class from MessageProperties: {}", targetClass);
                        }
                    }
                }
                
                if (targetClass != null) {
                    Object result = objectMapper.treeToValue(messageNode, targetClass);
                    log.info("Successfully converted to {}: {}", targetClass.getName(), result);
                    return result;
                } else {
                    log.warn("Could not determine target class, falling back to default conversion");
                }
            } else {
                log.info("Not a MassTransit envelope, using default conversion");
            }
            
            // If not a MassTransit envelope, fall back to default behavior
            return super.fromMessage(message, conversionHint);
            
        } catch (Exception ex) {
            log.error("Failed to convert MassTransit message", ex);
            throw new MessageConversionException("Failed to convert MassTransit message", ex);
        }
    }
}
