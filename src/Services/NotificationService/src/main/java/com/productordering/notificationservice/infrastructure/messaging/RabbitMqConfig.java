package com.productordering.notificationservice.infrastructure.messaging;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.springframework.amqp.core.*;
import org.springframework.amqp.rabbit.config.SimpleRabbitListenerContainerFactory;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitMqConfig {
    
    @Value("${rabbitmq.queues.order-created}")
    private String orderCreatedQueue;
    
    @Value("${rabbitmq.queues.payment-processed}")
    private String paymentProcessedQueue;
    
    @Value("${rabbitmq.queues.payment-failed}")
    private String paymentFailedQueue;
    
    @Value("${rabbitmq.exchanges.order-created}")
    private String orderCreatedExchange;
    
    @Value("${rabbitmq.exchanges.payment-processed}")
    private String paymentProcessedExchange;
    
    @Value("${rabbitmq.exchanges.payment-failed}")
    private String paymentFailedExchange;
    
    @Bean
    public Jackson2JsonMessageConverter messageConverter(ObjectMapper objectMapper) {
        // Use custom MassTransit message converter to handle MassTransit envelope format
        return new MassTransitMessageConverter(objectMapper);
    }
    
    @Bean
    public RabbitTemplate rabbitTemplate(ConnectionFactory connectionFactory, ObjectMapper objectMapper) {
        RabbitTemplate template = new RabbitTemplate(connectionFactory);
        template.setMessageConverter(messageConverter(objectMapper));
        return template;
    }
    
    @Bean
    public SimpleRabbitListenerContainerFactory rabbitListenerContainerFactory(
            ConnectionFactory connectionFactory, 
            ObjectMapper objectMapper) {
        SimpleRabbitListenerContainerFactory factory = new SimpleRabbitListenerContainerFactory();
        factory.setConnectionFactory(connectionFactory);
        factory.setMessageConverter(messageConverter(objectMapper));
        return factory;
    }
    
    // Order Created Queue and Exchange
    @Bean
    public Queue orderCreatedQueue() {
        return new Queue(orderCreatedQueue, true);
    }
    
    @Bean
    public FanoutExchange orderCreatedExchange() {
        return new FanoutExchange(orderCreatedExchange);
    }
    
    @Bean
    public Binding orderCreatedBinding() {
        return BindingBuilder.bind(orderCreatedQueue())
                .to(orderCreatedExchange());
    }
    
    // Payment Processed Queue and Exchange
    @Bean
    public Queue paymentProcessedQueue() {
        return new Queue(paymentProcessedQueue, true);
    }
    
    @Bean
    public FanoutExchange paymentProcessedExchange() {
        return new FanoutExchange(paymentProcessedExchange);
    }
    
    @Bean
    public Binding paymentProcessedBinding() {
        return BindingBuilder.bind(paymentProcessedQueue())
                .to(paymentProcessedExchange());
    }
    
    // Payment Failed Queue and Exchange
    @Bean
    public Queue paymentFailedQueue() {
        return new Queue(paymentFailedQueue, true);
    }
    
    @Bean
    public FanoutExchange paymentFailedExchange() {
        return new FanoutExchange(paymentFailedExchange);
    }
    
    @Bean
    public Binding paymentFailedBinding() {
        return BindingBuilder.bind(paymentFailedQueue())
                .to(paymentFailedExchange());
    }
}
