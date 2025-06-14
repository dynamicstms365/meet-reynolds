using Microsoft.Graph;
using Microsoft.Graph.Authentication;
using Microsoft.Graph.Models;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

namespace CopilotAgent.Services
{
    public interface IReynoldsTeamsChatService
    {
        Task<string> CreateNewChatAsync(string userEmail, string topic = "Reynolds Coordination Chat");
        Task<bool> SendDirectMessageAsync(string userEmail, string message);
Task<bool> SendProactiveCoordinationMessageAsync(string userEmail, string prNumber, string repository);
        Task<string> InitiateCoordinationChatAsync(List<string> userEmails, string topic, string initialMessage);
    }

    public class ReynoldsTeamsChatService : IReynoldsTeamsChatService
    {
        private readonly ILogger<ReynoldsTeamsChatService> _logger;
        private readonly IConfiguration _configuration;
        private readonly GraphServiceClient _graphClient;

        public ReynoldsTeamsChatService(ILogger<ReynoldsTeamsChatService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _graphClient = CreateGraphClient();
        }

        public async Task<string> CreateNewChatAsync(string userEmail, string topic = "Reynolds Coordination Chat")
        {
            try
            {
                _logger.LogInformation("Creating new chat with {UserEmail}, topic: {Topic}", userEmail, topic);

                // Create a 1:1 chat
                var chat = new Chat
                {
                    ChatType = ChatType.OneOnOne,
                    Topic = topic,
                    Members = new List<ConversationMember>
                    {
                        new AadUserConversationMember
                        {
                            Roles = new List<string> { "owner" },
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users('{userEmail}')"
                            }
                        },
                        new AadUserConversationMember
                        {
                            Roles = new List<string> { "owner" },
                            AdditionalData = new Dictionary<string, object>
                            {
                                ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users('{GetBotUserEmail()}')"
                            }
                        }
                    }
                };

                var createdChat = await _graphClient.Chats.PostAsync(chat);
                
                _logger.LogInformation("Successfully created chat {ChatId}", createdChat?.Id);
                return createdChat?.Id ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chat with {UserEmail}", userEmail);
                throw;
            }
        }

        public async Task<bool> SendDirectMessageAsync(string userEmail, string message)
        {
            try
            {
                _logger.LogInformation("Sending direct message to {UserEmail}", userEmail);

                // Create chat first
                var chatId = await CreateNewChatAsync(userEmail);

                if (string.IsNullOrEmpty(chatId))
                {
                    _logger.LogError("Failed to create chat for direct message");
                    return false;
                }

                // Send message to chat
                var chatMessage = new ChatMessage
                {
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = message
                    }
                };

                await _graphClient.Chats[chatId].Messages.PostAsync(chatMessage);
                
                _logger.LogInformation("Successfully sent direct message to {UserEmail}", userEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send direct message to {UserEmail}", userEmail);
                return false;
            }
        }

        public async Task<string> InitiateCoordinationChatAsync(List<string> userEmails, string topic, string initialMessage)
        {
            try
            {
                _logger.LogInformation("Creating coordination chat with {UserCount} users: {Topic}", 
                    userEmails.Count, topic);

                // Create group chat
                var members = new List<ConversationMember>();
                
                // Add bot as owner
                members.Add(new AadUserConversationMember
                {
                    Roles = new List<string> { "owner" },
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users('{GetBotUserEmail()}')"
                    }
                });

                // Add users as members
                foreach (var email in userEmails)
                {
                    members.Add(new AadUserConversationMember
                    {
                        Roles = new List<string> { "member" },
                        AdditionalData = new Dictionary<string, object>
                        {
                            ["user@odata.bind"] = $"https://graph.microsoft.com/v1.0/users('{email}')"
                        }
                    });
                }

                var chat = new Chat
                {
                    ChatType = ChatType.Group,
                    Topic = topic,
                    Members = members
                };

                var createdChat = await _graphClient.Chats.PostAsync(chat);

                if (createdChat?.Id != null && !string.IsNullOrEmpty(initialMessage))
                {
                    // Send initial message
                    var chatMessage = new ChatMessage
                    {
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = initialMessage
                        }
                    };

                    await _graphClient.Chats[createdChat.Id].Messages.PostAsync(chatMessage);
                }

                _logger.LogInformation("Successfully created coordination chat {ChatId}", createdChat?.Id);
                return createdChat?.Id ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create coordination chat with topic {Topic}", topic);
                throw;
            }
        }

        public async Task<bool> SendProactiveCoordinationMessageAsync(string userEmail, string prNumber, string repository)
        {
            try
            {
                _logger.LogInformation("Reynolds sending proactive scope creep alert for PR #{PrNumber} in {Repository} to {UserEmail}", prNumber, repository, userEmail);

                var message = $@"🚨 **Reynolds Scope Creep Alert**

Hey there! I've detected some scope evolution in PR #{prNumber} that's growing faster than my collection of name deflection strategies.

**Repository:** {repository}
**PR Number:** #{prNumber}
**Analysis:** This PR has evolved beyond its original scope - time for some strategic coordination!

Should we Aviation Gin this into manageable pieces? I'm here to help coordinate with my signature diplomatic approach! 🍸

*Maximum Effort on scope management. Just Reynolds.*";

                var success = await SendDirectMessageAsync(userEmail, message);
                
                if (success)
                {
                    _logger.LogInformation("Reynolds successfully sent scope creep alert for PR #{PrNumber}", prNumber);
                }
                else
                {
                    _logger.LogWarning("Reynolds failed to send scope creep alert for PR #{PrNumber}", prNumber);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending proactive coordination message for PR #{PrNumber}", prNumber);
                return false;
            }
        }

        private GraphServiceClient CreateGraphClient()
        {
            try
            {
                var clientId = _configuration["Teams:AppId"];
                var clientSecret = _configuration["Teams:AppPassword"];
                var tenantId = _configuration["Teams:TenantId"];

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(tenantId))
                {
                    throw new InvalidOperationException("Teams configuration missing required settings");
                }

                var options = new ClientSecretCredentialOptions
                {
                    AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
                };

                var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
                
                return new GraphServiceClient(clientSecretCredential);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create Graph client");
                throw;
            }
        }

        private string GetBotUserEmail()
        {
            // For now, return a placeholder - this should be configured
            return _configuration["Teams:BotUserEmail"] ?? "reynolds@yourdomain.com";
        }
    }
}