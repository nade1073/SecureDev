﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Vladi2.Models
{
    public class UserAccount
    {
        [Required(ErrorMessage = "UserName required")]
        public string UserName { get; set; }

        public UserAccount()
        {

        }

        public UserAccount (string userRegister,string PasswordRegister,string email,string phoneNumber,string firstName,string lastName)
        {
            UserName = userRegister;
            Password = PasswordRegister;
            Email = email;
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;

        }

        [Required(ErrorMessage = "Password required")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[$@$!%*?&])[A-Za-z\d$@$!%*?&]{8,10}",ErrorMessage= "Minimum eight and maximum 10 characters, at least one uppercase letter, one lowercase letter, one number and one special character:")]
        public string Password { get; set; }

        [Required(ErrorMessage = "נדרש כתובת אימייל")]
        [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ErrorMessage = "פורמט אימייל לא תקין")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [Required(ErrorMessage = "FirstName required")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "LastName required")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "PhoneNumber required")]
        public string PhoneNumber { get; set; }

        public string PictureUser { get; set; }

        public int Amount { get; set; }

        public override string ToString()
        {
            return string.Format(
@"#UserName {0} 
#Password {1} 
#Email {2} 
#FirstName {3} 
#LastName {4} 
#PhoneNumber {5} 
#PictureUser {6}
#Amount {7}", this.UserName, this.Password, this.Email, this.FirstName, this.LastName, this.PhoneNumber, this.PictureUser, this.Amount);
        }
    }
}