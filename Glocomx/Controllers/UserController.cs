﻿using Glocomx.DTOs.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;


namespace Glocomx.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly IConfiguration _configuration;


        public UserController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this._configuration = configuration;
        }

        // POST api/<UserController>
        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Post([FromBody] LoginDTO model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    userId = user.Id,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    profilePic = user.ProfilePic,
                    email = user.Email,
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromForm] RegisterDTO model)
        {
            var userExists = await userManager.FindByNameAsync(model.Email);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User already exists!" });


            string dbFilePath = "";
            if(Request.Form.Files.Count > 0)
            {
                try
                {
                    var file = Request.Form.Files[0];
                    var folderName = Path.Combine("Resources", "Images", "ProfilePics");
                    var pathToSave = Path.Combine(Directory.GetCurrentDirectory(), folderName);
                    if (file.Length > 0)
                    {
                        var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                        var fullPath = Path.Combine(pathToSave, fileName);
                        dbFilePath = Path.Combine(folderName, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }
                    }
                    else
                    {
                        return BadRequest();
                    }

                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex}");
                }
            }
           



            ApplicationUser user = new ApplicationUser()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.FirstName + model.LastName,
                ProfilePic = dbFilePath
            };

            bool adminRoleExists = await roleManager.RoleExistsAsync(model.Role);
            if (!adminRoleExists)
            {
                var newRole = await roleManager.CreateAsync(new IdentityRole(model.Role));
                if (newRole.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, model.Role);
                }
            }
          

            try
            {
                var result = await userManager.CreateAsync(user, model.Password);
                //if (!result.Succeeded)
                //    return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed! Please check user details and try again." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = ex.Message});
            }
           
    
            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }



    }
}
