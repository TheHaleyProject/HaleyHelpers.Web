using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Haley.Abstractions;

namespace Haley.Models {
    public class EmailData : IEmailData {
        public string[]? To { get; set; }
        public string[]? CC { get; set; }
        public string[]? BCC { get; set; }
        public string[]? ReplyTo { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string? From { get; set; }
        public bool IsHtml { get; set; } = true;
        public EmailData() { }
    }
}