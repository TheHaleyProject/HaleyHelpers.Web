using Haley.Abstractions;
using Haley.Enums;
using Haley.Utils;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System.Net.Mime;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Haley.Models {
    public class EnforcePolicySchemeRequirement : IAuthorizationRequirement {
      
    }
}
