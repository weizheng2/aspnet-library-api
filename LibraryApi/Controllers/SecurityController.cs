namespace LibraryApi.Controllers
{
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using LibraryApi.Data;
    using LibraryApi.Models;
    using LibraryApi.Services;

    [Route("api/security")]
    [ApiController]
    public class SecurityController : ControllerBase
    {
        private readonly IDataProtector protector;
        private readonly ITimeLimitedDataProtector limitedTimeprotector;
        private readonly IHashService _hashService;
        public SecurityController(IDataProtectionProvider protectionProvider, IHashService hashService)
        {
            protector = protectionProvider.CreateProtector("SecurityController");

            limitedTimeprotector = protector.ToTimeLimitedDataProtector();

            _hashService = hashService;
        }

        [HttpGet("hash")]
        public ActionResult Hash(string plainText)
        {
            var hash1 = _hashService.Hash(plainText);
            var hash2 = _hashService.Hash(plainText);
            var hash3 = _hashService.Hash(plainText, hash2.Salt);

            return Ok(new { plainText, hash1, hash2, hash3 });
        }

        [HttpGet("encrypt")]
        public ActionResult Encrypt(string plainText)
        {
            string encryptedText = protector.Protect(plainText);
            return Ok(new { encryptedText });
        }

        [HttpGet("decrypt")]
        public ActionResult Decrypt(string encryptedText)
        {
            string plainText = protector.Unprotect(encryptedText);
            return Ok(new { plainText });
        }

        [HttpGet("time-limited-encrypt")]
        public ActionResult TimeLimitedEncrypt(string plainText)
        {
            string encryptedText = limitedTimeprotector.Protect(plainText,
             lifetime: TimeSpan.FromSeconds(30));
            return Ok(new { encryptedText });
        }

        [HttpGet("time-limited-decrypt")]
        public ActionResult TimeLimitedDecrypt(string encryptedText)
        {
            string plainText = limitedTimeprotector.Unprotect(encryptedText);
            return Ok(new { plainText });
        }
    }
}