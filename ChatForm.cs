using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace ChatGPTNative
{
    public class ChatForm : Form
    {
        private TextBox inputBox;
        private Button sendButton;
        private ListBox chatList;

        private List<Message> messages = new();
        private HttpClient http = new HttpClient();

        private const string API_KEY = "PUT_YOUR_KEY_HERE";

        public ChatForm()
        {
            this.Text = "ChatGPT Native Lite";
            this.Width = 600;
            this.Height = 800;

            chatList = new ListBox
            {
                Dock = DockStyle.Fill
            };

            inputBox = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 40
            };

            sendButton = new Button
            {
                Text = "Send",
                Dock = DockStyle.Bottom,
                Height = 40
            };

            sendButton.Click += async (s, e) => await SendMessage();

            this.Controls.Add(chatList);
            this.Controls.Add(sendButton);
            this.Controls.Add(inputBox);
        }

        private async System.Threading.Tasks.Task SendMessage()
        {
            var text = inputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AddMessage("You", text);
            inputBox.Clear();

            var reply = await CallAPI(text);
            AddMessage("AI", reply);
        }

        private void AddMessage(string role, string content)
        {
            messages.Add(new Message(role, content));

            // عرض آخر 50 رسالة فقط (Virtualization بسيط)
            chatList.Items.Clear();
            int start = Math.Max(0, messages.Count - 50);

            for (int i = start; i < messages.Count; i++)
            {
                chatList.Items.Add($"{messages[i].Role}: {messages[i].Content}");
            }

            chatList.TopIndex = chatList.Items.Count - 1;
        }

        private async System.Threading.Tasks.Task<string> CallAPI(string userInput)
        {
            try
            {
                var request = new
                {
                    model = "gpt-4.1",
                    input = BuildMessages(userInput)
                };

                var json = JsonSerializer.Serialize(request);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                http.DefaultRequestHeaders.Clear();
                http.DefaultRequestHeaders.Add("Authorization", $"Bearer {API_KEY}");

                var response = await http.PostAsync("https://api.openai.com/v1/responses", content);
                var result = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(result);
                return doc.RootElement
                          .GetProperty("output")[0]
                          .GetProperty("content")[0]
                          .GetProperty("text")
                          .GetString() ?? "No response";
            }
            catch (Exception ex)
            {
                return "Error: " + ex.Message;
            }
        }

        private List<object> BuildMessages(string newInput)
        {
            var list = new List<object>
            {
                new {
                    role = "system",
                    content = "You are a helpful assistant. Continue conversation naturally."
                }
            };

            // آخر 20 رسالة فقط (تحكم بالسياق)
            int start = Math.Max(0, messages.Count - 20);

            for (int i = start; i < messages.Count; i++)
            {
                list.Add(new {
                    role = messages[i].Role == "You" ? "user" : "assistant",
                    content = messages[i].Content
                });
            }

            list.Add(new {
                role = "user",
                content = newInput
            });

            return list;
        }
    }

    public class Message
    {
        public string Role { get; }
        public string Content { get; }

        public Message(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }
}
