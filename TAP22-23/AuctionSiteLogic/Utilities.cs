using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TAP22_23.AuctionSite.Interface;

namespace Conti {

    public class Utilities {
        public static void CheckValidProperties(Object obg) {
            var vc = new ValidationContext(obg);
            try {
                Validator.ValidateObject(obg, vc, true);
            } catch (ValidationException e) {
                if (e.ValidationAttribute!.GetType() == typeof(MinLengthAttribute))
                    throw new AuctionSiteArgumentException("Value too short", e.ValidationResult.MemberNames.First(), e);

                if (e.ValidationAttribute!.GetType() == typeof(MaxLengthAttribute))
                    throw new AuctionSiteArgumentException("Value too long", e.ValidationResult.MemberNames.First(), e);

                if (e.ValidationAttribute!.GetType() == typeof(RequiredAttribute))
                    throw new AuctionSiteArgumentNullException($"{e.ValidationResult.MemberNames.First()} cannot be null", e);

                if (e.ValidationAttribute.GetType() == typeof(RangeAttribute))
                    throw new AuctionSiteArgumentOutOfRangeException("Value out of range", e.ValidationResult.MemberNames.First(), e);

            }

        }

        public static string hashPassword(string password) {
            if (password == null)
                throw new AuctionSiteArgumentNullException($"{nameof(password)} cannot be null");

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, 350000, HashAlgorithmName.SHA512, 16);
            var hexSalt = Convert.ToHexString(salt);
            var hexHash = Convert.ToHexString(hash);
            return $"{hexHash}{hexSalt}";
        }

        public static bool verifyPassword(string hashPass, string password) {
            if (password == null || hashPass == null)
                throw new AuctionSiteArgumentNullException($"{nameof(password)} and {nameof(password)}  cannot be null");
            var hashBytes = Convert.FromHexString(hashPass);
            var hash = hashBytes[..16];
            var salt = hashBytes[16..];
            var newHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 350000, HashAlgorithmName.SHA512, 16);
            return newHash.SequenceEqual(hash);
        }
    }

}
