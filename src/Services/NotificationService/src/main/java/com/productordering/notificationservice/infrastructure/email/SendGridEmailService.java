package com.productordering.notificationservice.infrastructure.email;

import com.productordering.notificationservice.application.services.EmailService;
import com.sendgrid.*;
import com.sendgrid.helpers.mail.Mail;
import com.sendgrid.helpers.mail.objects.Content;
import com.sendgrid.helpers.mail.objects.Email;
import lombok.extern.slf4j.Slf4j;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.io.IOException;

@Slf4j
@Service
public class SendGridEmailService implements EmailService {
    
    @Value("${sendgrid.api-key}")
    private String sendGridApiKey;
    
    @Value("${sendgrid.from-email}")
    private String fromEmail;
    
    @Value("${sendgrid.from-name}")
    private String fromName;
    
    @Value("${sendgrid.enabled:false}")
    private boolean enabled;
    
    @Override
    public void sendEmail(String to, String subject, String htmlContent) {
        if (!enabled) {
            log.warn("SendGrid is disabled. Email would be sent to: {} with subject: {}", to, subject);
            log.debug("Email content: {}", htmlContent);
            return;
        }
        
        Email from = new Email(fromEmail, fromName);
        Email toEmail = new Email(to);
        Content content = new Content("text/html", htmlContent);
        Mail mail = new Mail(from, subject, toEmail, content);
        
        SendGrid sg = new SendGrid(sendGridApiKey);
        Request request = new Request();
        
        try {
            request.setMethod(Method.POST);
            request.setEndpoint("mail/send");
            request.setBody(mail.build());
            
            Response response = sg.api(request);
            
            if (response.getStatusCode() >= 200 && response.getStatusCode() < 300) {
                log.info("Email sent successfully to: {}", to);
            } else {
                log.error("Failed to send email. Status: {}, Body: {}", 
                        response.getStatusCode(), response.getBody());
                throw new RuntimeException("Failed to send email: " + response.getBody());
            }
        } catch (IOException ex) {
            log.error("Error sending email to: {}", to, ex);
            throw new RuntimeException("Error sending email", ex);
        }
    }
}
